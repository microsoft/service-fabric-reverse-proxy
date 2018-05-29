// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in 
//  the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;

namespace Cache
{
    /**
     * CacheStatusInfo provides a default implementation of StatusInfo for use in Cache
     * implementations.
     */
    public class CacheStatusInfo<T> : IStatusInfo<T>
    {
        // TODO: Current timeout for Acquire lock in this file = Infinite.
        // Set an appropriate timeout and handle timeout exception.
        private static ReaderWriterLock rwl = new ReaderWriterLock();

        // GuardedBy rwl
        private Dictionary<long, Watch> watches = new Dictionary<long, Watch>();

        // GuardedBy rwl
        private long lastWatchRequestTime;

        public CacheStatusInfo(T nodeGroup)
        {
            this.NodeGroup = nodeGroup;
        }
        public long LastWatchRequestTime
        {
            get
            {
                try
                {
                    rwl.AcquireReaderLock(Timeout.Infinite);
                    return lastWatchRequestTime;
                }
                catch (ApplicationException)
                {
                    // Log
                }
                finally
                {
                    rwl.ReleaseReaderLock();
                }

                return -1;
            }
        }

        public T NodeGroup { get; }

        public int NumWatches
        {
            get
            {
                try
                {
                    rwl.AcquireReaderLock(Timeout.Infinite);
                    return watches.Count;
                }
                catch (ApplicationException)
                {
                    // Log
                }
                finally
                {
                    rwl.ReleaseReaderLock();
                }
                return -1;
            }
        }
        /**
         * Removes the given watch from the tracked collection of watches.
         *
         * @param watchId the ID for the watch that should be removed
         */
        public void RemoveWatch(long watchId)
        {
            try
            {
                rwl.AcquireWriterLock(Timeout.Infinite);
                watches.Remove(watchId);
            }
            catch (ApplicationException)
            {
                // Log
            }
            finally
            {
                rwl.ReleaseWriterLock();
            }

        }

        /**
         * Sets the timestamp of the last discovery watch request.
         *
         * @param lastWatchRequestTime the latest watch request timestamp
         */
        public void SetLastWatchRequestTime(long lastWatchRequestTime)
        {
            try
            {
                rwl.AcquireWriterLock(Timeout.Infinite);
                this.lastWatchRequestTime = lastWatchRequestTime;
            }
            catch (ApplicationException)
            {
                // Log
            }
            finally
            {
                rwl.ReleaseWriterLock();
            }

        }

        /**
         * Adds the given watch to the tracked collection of watches.
         *
         * @param watchId the ID for the watch that should be added
         * @param watch the watch that should be added
         */
        public void SetWatch(long watchId, Watch watch)
        {
            try
            {
                rwl.AcquireWriterLock(Timeout.Infinite);
                watches.Add(watchId, watch);
            }
            catch (ApplicationException)
            {
                // Log
            }
            finally
            {
                rwl.ReleaseWriterLock();
            }
        }

        /**
         * Returns the set of IDs for all watches currently being tracked.
         */
        public ICollection<long> WatchIds()
        {
            try
            {
                rwl.AcquireReaderLock(Timeout.Infinite);
                return watches.Keys;
            }
            catch (ApplicationException)
            {
                // Log
            }
            finally
            {
                rwl.ReleaseReaderLock();
            }

            return new List<long>();
        }

        /**
         * Iterate over all tracked watches and execute the given function. If it returns {@code true}, then the watch is
         * removed from the tracked collection. If it returns {@code false}, then the watch is not removed.
         *
         * @param filter the function to execute on each watch
         */
        public void WatchesRemoveIf(Func<long, Watch, Boolean> filter)
        {
            try
            {
                rwl.AcquireWriterLock(Timeout.Infinite);
                foreach (var entry in watches)
                {
                    if (filter(entry.Key, entry.Value))
                    {
                        watches.Remove(entry.Key);
                    }
                }
            }
            catch (ApplicationException)
            {
                // Log
            }
            finally
            {
                rwl.ReleaseWriterLock();
            }
        }
    }
}
