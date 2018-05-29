// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in 
//  the repo root for license information.
// ------------------------------------------------------------

using Envoy.Api.V2;

namespace Cache
{
    /**
     * {@code ConfigWatcher} requests watches for configuration resources by type, node, last applied version identifier,
     * and resource names hint. The watch should send the responses when they are ready. The watch can be cancelled by the
     * consumer, in effect terminating the watch for the request. ConfigWatcher implementations must be thread-safe.
     */
    public interface ConfigWatcher
    {

        /**
         * Returns a new configuration resource {@link Watch} for the given discovery request.
         *
         * @param ads is the watch for an ADS request?
         * @param request the discovery request (node, names, etc.) to use to generate the watch
         */
        Watch CreateWatch(bool ads, DiscoveryRequest request);
    }
}
