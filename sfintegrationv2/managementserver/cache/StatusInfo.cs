// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in 
//  the repo root for license information.
// ------------------------------------------------------------

namespace Cache
{
    /**
     * {@code StatusInfo} tracks the state for remote envoy nodes.
     */
    public interface IStatusInfo<T>
    {

        /**
         * Returns the timestamp of the last discovery watch request.
         */
        long LastWatchRequestTime { get; }

        /**
         * Returns the node grouping represented by this status, generated via {@link NodeGroup#hash(Node)}.
         */
        T NodeGroup { get; }

        /**
         * Returns the number of open watches.
         */
        int NumWatches { get; }
    }
}
