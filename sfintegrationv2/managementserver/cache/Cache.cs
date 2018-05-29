// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in 
//  the repo root for license information.
// ------------------------------------------------------------

namespace Cache
{
    /**
     * Cache is a generic config cache with support for watchers.
     */
    public interface Cache<T> : ConfigWatcher
    {

        /**
         * Returns the current StatusInfo for the given Node group.
         *
         * @param group the node group whose status is being fetched
         */
        IStatusInfo<T> statusInfo(T group);
    }
}
