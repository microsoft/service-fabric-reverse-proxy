// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in 
//  the repo root for license information.
// ------------------------------------------------------------

using Cache;
using Envoy.Api.V2;
using Envoy.Service.Discovery.V2;
using managementserver.subscriber;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ManagementServer
{
    public class DiscoveryServer
    {
        public DiscoveryServer()
        {
        }
        public class ADSServer : AggregatedDiscoveryService.AggregatedDiscoveryServiceBase
        {
            private long streamNonce = 0;
            private static long streamCount = 0; // TODO-kavyako Use Interlocked.Increment
            private static ConfigWatcher configWatcher;

            // size is constant : Resources.TYPE_URLS.Count
            private ConcurrentDictionary<string, Watch> watches = new ConcurrentDictionary<string, Watch>();
            private ConcurrentDictionary<string, string> nonces = new ConcurrentDictionary<string, string>();

            Grpc.Core.IServerStreamWriter<DiscoveryResponse> responseObserver;

            public static ConfigWatcher ADSConfigWatcher { get => configWatcher; set => configWatcher = value; }

            public ADSServer(ConfigWatcher configWatcher)
            {
                ADSServer.ADSConfigWatcher = configWatcher;
            }

            public override async Task StreamAggregatedResources(Grpc.Core.IAsyncStreamReader<DiscoveryRequest> requestStream,
                                                     Grpc.Core.IServerStreamWriter<DiscoveryResponse> responseStream,
                                                     Grpc.Core.ServerCallContext context)
            {
                CancellationTokenSource source = new CancellationTokenSource();
                responseObserver = responseStream;

                while (await requestStream.MoveNext(source.Token))
                {
                    var request = requestStream.Current;
                    Console.WriteLine("ADS: Received discovery request" + request);

                    string nonce = request.ResponseNonce;
                    string requestTypeUrl = request.TypeUrl;

                    if (String.IsNullOrEmpty(requestTypeUrl))
                    {
                        Console.WriteLine("Type URL is required for ADS");
                        return;
                    }

                    Console.WriteLine("request {1}[{2}] with nonce {3} from version {4}",
                        requestTypeUrl,
                        String.Concat(", ", request.ResourceNames),
                        nonce,
                        request.VersionInfo);

                    foreach (string typeUrl in Resources.TYPE_URLS)
                    {
                        string resourceNonce;
                        this.nonces.TryGetValue(typeUrl, out resourceNonce);

                        if (requestTypeUrl.Equals(typeUrl) && (String.IsNullOrEmpty(resourceNonce) || resourceNonce.Equals(nonce)))
                        {
                            Watch oldWatch;
                            // If oldWatch exists, cancel it
                            if (watches.TryGetValue(typeUrl, out oldWatch))
                            {
                                oldWatch.Cancel();
                            }
                            Watch newWatch = ADSConfigWatcher.CreateWatch(true /*ads */, request);

                            var d = new ResponseSubscriber(r =>
                            {
                                var nonceStr = send(r, typeUrl);
                                nonces.AddOrUpdate(typeUrl, nonceStr, (key, oldValue) => nonceStr);
                            });

                            newWatch.Value.Subscribe(d);
                            return;
                        }
                    }
                }
            }

            private string send(Response response, string typeUrl)
            {
                string nonce = Interlocked.Increment(ref streamNonce).ToString();

                DiscoveryResponse discoveryResponse = new DiscoveryResponse()
                {
                    VersionInfo = response.Version,
                    Nonce = nonce,
                    TypeUrl = typeUrl,
                };

                foreach (var res in response.Resources)
                    discoveryResponse.Resources.Add(Google.Protobuf.WellKnownTypes.Any.Pack(res));

                Console.WriteLine("[{0}] response {1} with nonce {2} version {3}", ADSServer.streamCount, typeUrl, nonce, response.Version);

                // The watch value streams are being observed on multiple threads, so we need to synchronize
                // here because StreamObserver instances are not thread-safe.
                responseObserver.WriteAsync(discoveryResponse).RunSynchronously();

                return nonce;
            }
        };
    }
}
