// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in 
//  the repo root for license information.
// ------------------------------------------------------------

namespace Cache
{
    public interface SnapshotCache<T> : Cache<T>
    {

        /**
         * Set the Snapshot for the given node group. Snapshots should have distinct versions and be internally
         * consistent (i.e. all referenced resources must be included in the snapshot).
         *
         * @param group group identifier
         * @param snapshot a versioned collection of node config data
         */
        void SetSnapshot(T group, Snapshot snapshot);
    }
}