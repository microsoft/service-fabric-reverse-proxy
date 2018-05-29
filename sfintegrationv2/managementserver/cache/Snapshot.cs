using Envoy.Api.V2;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Cache
{
    /**
     * {@code Snapshot} is a data class that contains an internally consistent snapshot of xDS resources. Snapshots should
     * have distinct versions per node group.
     */
    public class Snapshot
    {

        /**
         * Returns a new {@link Snapshot} instance that is versioned uniformly across all resources.
         *
         * @param clusters the cluster resources in this snapshot
         * @param endpoints the endpoint resources in this snapshot
         * @param listeners the listener resources in this snapshot
         * @param routes the route resources in this snapshot
         * @param version the version associated with all resources in this snapshot
         */
        public static Snapshot Create(
            IEnumerable<Cluster> clusters,
            IEnumerable<ClusterLoadAssignment> endpoints,
            IEnumerable<Listener> listeners,
            IEnumerable<RouteConfiguration> routes,
            string version)
        {

            return new Snapshot(
                SnapshotResources<Cluster>.create(clusters, version),
                SnapshotResources<ClusterLoadAssignment>.create(endpoints, version),
                SnapshotResources<Listener>.create(listeners, version),
                SnapshotResources<RouteConfiguration>.create(routes, version));
        }

        /**
         * Returns a new {@link Snapshot} instance that has separate versions for each resource type.
         *
         * @param clusters the cluster resources in this snapshot
         * @param clustersVersion the version of the cluster resources
         * @param endpoints the endpoint resources in this snapshot
         * @param endpointsVersion the version of the endpoint resources
         * @param listeners the listener resources in this snapshot
         * @param listenersVersion the version of the listener resources
         * @param routes the route resources in this snapshot
         * @param routesVersion the version of the route resources
         */
        public static Snapshot Create(
            IEnumerable<Cluster> clusters,
            string clustersVersion,
            IEnumerable<ClusterLoadAssignment> endpoints,
            string endpointsVersion,
            IEnumerable<Listener> listeners,
            string listenersVersion,
            IEnumerable<RouteConfiguration> routes,
            string routesVersion)
        {

            return new Snapshot(
                SnapshotResources<Cluster>.create(clusters, clustersVersion),
                SnapshotResources<ClusterLoadAssignment>.create(endpoints, endpointsVersion),
                SnapshotResources<Listener>.create(listeners, listenersVersion),
                SnapshotResources<RouteConfiguration>.create(routes, routesVersion));
        }

        private Snapshot(
                         SnapshotResources<Cluster> clustersResource,
                         SnapshotResources<ClusterLoadAssignment> endpointsResource,
                         SnapshotResources<Listener> listenersResource,
                         SnapshotResources<RouteConfiguration> routesResource)
        {
            this.Clusters = clustersResource;
            this.Endpoints = endpointsResource;
            this.Listeners = listenersResource;
            this.Routes = routesResource;
        }

        /**
         * Returns all cluster items in the CDS payload.
         */
        public SnapshotResources<Cluster> Clusters { get; set; }

        /**
         * Returns all endpoint items in the EDS payload.
         */
        public SnapshotResources<ClusterLoadAssignment> Endpoints { get; set; }

        /**
         * Returns all listener items in the LDS payload.
         */
        public SnapshotResources<Listener> Listeners { get; set; }

        /**
         * Returns all route items in the RDS payload.
         */
        public SnapshotResources<RouteConfiguration> Routes { get; set; }

        /**
         * Asserts that all dependent resources are included in the snapshot. All EDS resources are listed by name in CDS
         * resources, and all RDS resources are listed by name in LDS resources.
         *
         * <p>Note that clusters and listeners are requested without name references, so Envoy will accept the snapshot list
         * of clusters as-is, even if it does not match all references found in xDS.
         *
         * @throws SnapshotConsistencyException if the snapshot is not consistent
         */
        public void ensureConsistent()
        {
            ImmutableHashSet<string> clusterEndpointRefs = Cache.Resources.GetResourceReferences(new List<IMessage>(Clusters.Resources.Values));

            EnsureAllResourceNamesExist(Cache.Resources.CLUSTER_TYPE_URL, Cache.Resources.ENDPOINT_TYPE_URL, clusterEndpointRefs, Endpoints.Resources);

            ImmutableHashSet<string> listenerRouteRefs = Cache.Resources.GetResourceReferences(new List<IMessage>(Listeners.Resources.Values));

            EnsureAllResourceNamesExist(Cache.Resources.LISTENER_TYPE_URL, Cache.Resources.ROUTE_TYPE_URL, listenerRouteRefs, Routes.Resources);
        }

        /**
         * Returns the resources with the given type.
         *
         * @param typeUrl the URL for the requested resource type
         */
        public Dictionary<string, IMessage> Resources(string typeUrl)
        {
            switch (typeUrl)
            {
                case Cache.Resources.CLUSTER_TYPE_URL:
                    return Clusters.Resources;
                case Cache.Resources.ENDPOINT_TYPE_URL:
                    return Endpoints.Resources;
                case Cache.Resources.LISTENER_TYPE_URL:
                    return Listeners.Resources;
                case Cache.Resources.ROUTE_TYPE_URL:
                    return Routes.Resources;
                default:
                    return new Dictionary<string, IMessage>();
            }
        }

        /**
         * Returns the version in this snapshot for the given resource type.
         *
         * @param typeUrl the URL for the requested resource type
         */
        public string Version(string typeUrl)
        {
            if (String.IsNullOrEmpty(typeUrl))
            {
                return String.Empty;
            }

            switch (typeUrl)
            {
                case Cache.Resources.CLUSTER_TYPE_URL:
                    return Clusters.Version;
                case Cache.Resources.ENDPOINT_TYPE_URL:
                    return Endpoints.Version;
                case Cache.Resources.LISTENER_TYPE_URL:
                    return Listeners.Version;
                case Cache.Resources.ROUTE_TYPE_URL:
                    return Routes.Version;
                default:
                    return string.Empty;
            }
        }

        /**
         * Asserts that all of the given resource names have corresponding values in the given resources collection.
         *
         * @param parentTypeUrl the type of the parent resources (source of the resource name refs)
         * @param dependencyTypeUrl the type of the given dependent resources
         * @param resourceNames the set of dependent resource names that must exist
         * @param resources the collection of resources whose names are being checked
         * @throws SnapshotConsistencyException if a name is given that does not exist in the resources collection
         */
        private static void EnsureAllResourceNamesExist(
            string parentTypeUrl,
            string dependencyTypeUrl,
            ImmutableHashSet<string> resourceNames,
            Dictionary<string, IMessage> resources)
        {

            if (resourceNames.Count != resources.Count)
            {
                throw new SnapshotConsistencyException(
                    String.Format(
                        "Mismatched %s -> %s reference and resource lengths, [%s] != %d",
                        parentTypeUrl,
                        dependencyTypeUrl,
                        String.Concat(", ", resourceNames),
                        resources.Count));
            }

            foreach (string name in resourceNames)
            {
                if (!resources.ContainsKey(name))
                {
                    throw new SnapshotConsistencyException(
                        String.Format(
                            "%s named '%s', referenced by a %s, not listed in [%s]",
                            dependencyTypeUrl,
                            name,
                            parentTypeUrl,
                            String.Concat(", ", resources.Keys)));
                }
            }
        }
    }
}
