// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in 
//  the repo root for license information.
// ------------------------------------------------------------

using Cache;
using Envoy.Service.Discovery.V2;
using Grpc.Core;
using System;

namespace ManagementServer
{
    class Program
    {
        const int Port = 50051;

        public static void Main(string[] args)
        {
            var t = SF_Services.InitializePartitionData();
            t.Wait();

            var configWatcher = new SimpleCache<string>(new SingleNodeGroup());

            Server server = new Server
            {
                Services = { AggregatedDiscoveryService.BindService(new DiscoveryServer.ADSServer(configWatcher)) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("Management server listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
