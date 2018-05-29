// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in 
//  the repo root for license information.
// ------------------------------------------------------------

using Cache;
using Envoy.Api.V2;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Threading;

/**
 * SimpleCache provides a default implementation of SnapshotCache. It maintains a single versioned
 * Snapshot per node group. For the protocol to work correctly in ADS mode, EDS/RDS requests are responded to
 * only when all resources in the snapshot xDS response are named as part of the request. It is expected that the CDS
 * response names all EDS clusters, and the LDS response names all RDS routes in a snapshot, to ensure that Envoy makes
 * the request for all EDS clusters or RDS routes eventually.
 *
 * <p>The snapshot can be partial, e.g. only include RDS or EDS resources.
 */
public class SimpleCache<T> : SnapshotCache<T>
{
    private INodeGroup<T> groups;

    private static ReaderWriterLock rwl = new ReaderWriterLock();

    //  @GuardedBy("lock")
    private Dictionary<T, Snapshot> snapshots = new Dictionary<T, Snapshot>();
    //  @GuardedBy("lock")
    private Dictionary<T, CacheStatusInfo<T>> statuses = new Dictionary<T, CacheStatusInfo<T>>();

    //@GuardedBy("lock")
    private long watchCount;

    /**
     * Constructs a simple cache.
     *
     * @param groups maps an envoy host to a node group
     */
    public SimpleCache(INodeGroup<T> groups)
    {
        this.groups = groups;
    }

    public Watch CreateWatch(bool ads, DiscoveryRequest request)
    {
        T group = groups.Hash(request.Node);

        try
        {
            rwl.AcquireWriterLock(Timeout.Infinite);
            if (!statuses.TryGetValue(group, out CacheStatusInfo<T> status))
            {
                status = new CacheStatusInfo<T>(group);
                statuses.Add(group, status);
            }

            status.SetLastWatchRequestTime(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            snapshots.TryGetValue(group, out Snapshot snapshot);

            Watch watch = new Watch(ads, request);

            // If the requested version is up-to-date or missing a response, leave an open watch.
            if (snapshot == null || request.VersionInfo.Equals(snapshot?.Version(request.TypeUrl)))
            {
                watchCount++;
                long watchId = watchCount;
                Console.WriteLine($"open watch {watchId} for {request.TypeUrl}[{string.Concat(", ", request.ResourceNames)}] from node {group} for version {request.VersionInfo}");
                status.SetWatch(watchId, watch);

                Thread newThread = new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        rwl.AcquireWriterLock(Timeout.Infinite);
                        status.RemoveWatch(watchId);
                    }
                    catch (ApplicationException)
                    {
                        // Log
                    }
                    finally
                    {
                        rwl.ReleaseWriterLock();
                    }
                }));

                watch.Stop = newThread;

                // TODO-kavyako: Is lock needed inside the thread?
                // watch.Stop = new Thread(() => status.RemoveWatch(watchId));
                return watch;
            }

            // Otherwise, the watch may be responded immediately
            Respond(watch, snapshot, group);

            return watch;

        }
        catch (ApplicationException)
        {
            // Log
        }
        finally
        {
            rwl.ReleaseWriterLock();
        }

        return null;
    }

    public void SetSnapshot(T group, Snapshot snapshot)
    {
        try
        {
            rwl.AcquireWriterLock(Timeout.Infinite);
            // Update the existing snapshot entry.
            snapshots[group] = snapshot;

            statuses.TryGetValue(group, out CacheStatusInfo<T> status);

            if (status == null)
            {
                return;
            }

            status.WatchesRemoveIf((id, watch) =>
            {
                string version = snapshot.Version(watch.Request.TypeUrl);

                if (!watch.Request.VersionInfo.Equals(version))
                {
                    Console.WriteLine("responding to open watch {id}[{string.Concat(", ", watch.Request.ResourceNames)}] with new version {version}");
                    Respond(watch, snapshot, group);

                    // Discard the watch. A new watch will be created for future snapshots once envoy ACKs the response.
                    return true;
                }

                // Do not discard the watch. The request version is the same as the snapshot version, so we wait to respond.
                return false;
            });
        }
        catch (ApplicationException)
        {
            // Log
        }
        finally
        {
            rwl.ReleaseWriterLock();
        }

        return;
    }

    public IStatusInfo<T> statusInfo(T group)
    {
        try
        {
            rwl.AcquireReaderLock(Timeout.Infinite);
            statuses.TryGetValue(group, out CacheStatusInfo<T> ret);
            return ret;
        }
        catch (ApplicationException)
        {
            // Log
        }
        finally
        {
            rwl.ReleaseReaderLock();
        }

        return null;
    }

    private Response CreateResponse(DiscoveryRequest request, Dictionary<string, IMessage> resources, string version)
    {
        List<IMessage> filtered = new List<IMessage>();
        foreach (var item in request.ResourceNames)
        {
            if (resources.TryGetValue(item, out IMessage value))
            {
                filtered.Add(value);
            }
        }

        return Response.Create(request, filtered, version);
    }

    private void Respond(Watch watch, Snapshot snapshot, T group)
    {
        Dictionary<string, IMessage> snapshotResources = snapshot.Resources(watch.Request.TypeUrl);

        if (watch.Request.ResourceNames.Count > 0 && watch.Ads)
        {
            List<string> missingNames = new List<string>();
            foreach (var name in watch.Request.ResourceNames)
            {
                if (!snapshotResources.ContainsKey(name))
                {
                    missingNames.Add(name);
                }
            }

            if (missingNames.Count > 0)
            {
                Console.WriteLine($"not responding in ADS mode " +
                    $"for {watch.Request.TypeUrl} from node {group} at " +
                    $"version {snapshot.Version(watch.Request.TypeUrl)} for " +
                    $"request [{string.Concat(", ", watch.Request.ResourceNames)}] " +
                    $"since [{string.Concat(", ", missingNames)}] not in snapshot");

                return;
            }
        }

        string version = snapshot.Version(watch.Request.TypeUrl);
        Console.WriteLine($"Responding for {watch.Request.TypeUrl} from node {group} at version {watch.Request.VersionInfo} with version {version}");

        Response response = CreateResponse(
                                           watch.Request,
                                           snapshotResources,
                                           version);

        watch.Value.OnNext(response);
    }
}
