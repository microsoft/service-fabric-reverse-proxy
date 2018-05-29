// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in 
//  the repo root for license information.
// ------------------------------------------------------------

using System.Threading;
using Envoy.Api.V2;
using System;
using Reactor.Core;
using Reactive.Streams;
using managementserver.subscriber;

namespace Cache
{
    /**
     * {@code Watch} is a dedicated stream of configuration resources produced by the configuration cache and consumed by
     * the xDS server.
     */
    public class Watch
    {
        private AtomicBoolean isCancelled;
        private Thread stop;

        public Watch(bool ads, DiscoveryRequest request)
        {
            this.Ads = ads;
            this.Request = request;
            this.isCancelled = new AtomicBoolean();
            this.Value = new DirectProcessor<Response>();
        }

        /**
         * Returns boolean indicating whether or not the watch is for an ADS request.
         */
        public bool Ads { get; }

        /**
         * Cancel the watch. A watch must be cancelled in order to complete its resource stream and free resources. Cancel
         * may be called multiple times, with each subsequent call being a no-op.
         */
        public void Cancel()
        {
            if (isCancelled.CompareAndSet(false, true))
            {
                try
                {
                    Value.OnComplete();
                }
                catch (Exception e)
                {
                    // If the underlying exception was an IllegalStateException then we assume that means the stream was already
                    // closed elsewhere and ignore it, otherwise we re-throw.
                    if (e is InvalidOperationException)
                    {
                        // Log
                    }
                    else
                    {
                        throw e;
                    }
                }
            }

            if (stop != null)
            {
                stop.Start();
            }
        }

        /**
         * Returns the original request for the watch.
         */
        public DiscoveryRequest Request { get; }

        /**
         * Sets the callback method to be executed when the watch is cancelled. Even if cancel is executed multiple times, it
         * ensures that this stop callback is only executed once.
         */
        public Thread Stop
        {
            set { stop = value; }
        }

        /**
         * Returns the stream of response values.
         */
        public IProcessor<Response> Value { get; }
    }
}
