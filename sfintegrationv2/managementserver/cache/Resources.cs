// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in 
//  the repo root for license information.
// ------------------------------------------------------------

using Envoy.Api.V2;
using Envoy.Api.V2.Listener2;
using Envoy.Config.Filter.Network.HttpConnectionManager.V2;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using static Envoy.Api.V2.Cluster.Types;

namespace Cache
{
    public class Resources
    {
        static string FILTER_HTTP_CONNECTION_MANAGER = "envoy.http_connection_manager";

        public const string CLUSTER_TYPE_URL = "type.googleapis.com/envoy.api.v2.Cluster";
        public const string ENDPOINT_TYPE_URL = "type.googleapis.com/envoy.api.v2.ClusterLoadAssignment";
        public const string LISTENER_TYPE_URL = "type.googleapis.com/envoy.api.v2.Listener";
        public const string ROUTE_TYPE_URL = "type.googleapis.com/envoy.api.v2.RouteConfiguration";

        public static readonly List<string> TYPE_URLS = new List<string>() {
      CLUSTER_TYPE_URL,
      ENDPOINT_TYPE_URL,
      LISTENER_TYPE_URL,
      ROUTE_TYPE_URL};

        /**
         * Returns the name of the given resource message.
         *
         * @param resource the resource message
         */
        public static string GetResourceName(IMessage resource)
        {
            switch (resource)
            {
                case Cluster cluster:
                    return cluster.Name;
                case ClusterLoadAssignment clusterLoadAssignment:
                    return clusterLoadAssignment.ClusterName;
                case Listener listener:
                    return listener.Name;
                case RouteConfiguration routeConfiguration:
                    return routeConfiguration.Name;
            }

            return string.Empty;
        }

        /**
         * Returns all resource names that are referenced by the given collection of resources.
         *
         * @param resources the resource whose dependencies we are calculating
         */
        public static ImmutableHashSet<string> GetResourceReferences(List<IMessage> resources)
        {
            ImmutableHashSet<string> refs = ImmutableHashSet.Create<string>();
            foreach (IMessage r in resources)
            {
                switch (r)
                {
                    case Cluster cluster:
                        // For EDS clusters, use the cluster name or the service name override.
                        if (cluster.Type == DiscoveryType.Eds)
                        {
                            if (!String.IsNullOrEmpty(cluster.EdsClusterConfig.ServiceName))
                            {
                                refs.Add(cluster.EdsClusterConfig.ServiceName);
                            }
                            else
                            {
                                refs.Add(cluster.Name);
                            }
                        }
                        break;

                    // Endpoints have no dependencies.
                    // References to clusters in routes (and listeners) are not included in the result, because the clusters are
                    // currently retrieved in bulk, and not by name.
                    case RouteConfiguration routeConfiguration:
                    case ClusterLoadAssignment clusterLoadAssignment:
                        break;
                    case Listener listener:
                        // Extract the route configuration names from the HTTP connection manager.
                        foreach (FilterChain chain in listener.FilterChains)
                        {
                            foreach (Filter filter in chain.Filters)
                            {
                                if (!filter.Name.Equals(FILTER_HTTP_CONNECTION_MANAGER))
                                {
                                    continue;
                                }

                                try
                                {
                                    HttpConnectionManager config = new HttpConnectionManager();

                                    structAsMessage(filter.Config, config);

                                    if (config.RouteSpecifierCase == HttpConnectionManager.RouteSpecifierOneofCase.Rds && !String.IsNullOrEmpty(config.Rds.RouteConfigName))
                                    {
                                        refs.Add(config.Rds.RouteConfigName);
                                    }
                                }
                                catch (InvalidProtocolBufferException e)
                                {
                                    Console.WriteLine(
                                        "Failed to convert HTTP connection manager config struct into protobuf message for listener {0}, exception:{1}",
                                        GetResourceName(listener),
                                        e);
                                }
                            }
                        }
                        break;
                }
            }

            return refs;
        }

        private static void structAsMessage(Struct s, IMessage messageBuilder)
        {
            JsonParser.Default.Parse(
                JsonFormatter.Default.Format(s), 
                messageBuilder.Descriptor);
        }

        private Resources() { }
    }
}
