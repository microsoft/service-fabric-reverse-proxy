// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.ReverseProxy.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;

    [Produces("application/json")]
    [Route("v1")]
    public class DiscoveryController : Controller
    {
        [HttpGet]
        public IActionResult GetAsync()
        {
            return Ok(
                new { partitions = SF_Services.partitions_ }
                );
        }

        [HttpGet("envoydata")]
        public IActionResult GetList()
        {
            List<EnvoyClustersInformation> ret = new List<EnvoyClustersInformation>();
            foreach (var pID in SF_Services.partitions_)
            {
                ret.AddRange(SF_Services.EnvoyInformationForPartition(pID.Key));
            }
            return Ok(
                new { clusters = ret }
                );
        }

        [HttpGet("clusters/{service_cluster}/{service_node}")]
        public IActionResult GetClusters(string service_cluster, string service_node)
        {
            List<EnvoyClusterModel> ret = new List<EnvoyClusterModel>();
            if (SF_Services.partitions_ == null)
            {
                return Ok(new { clusters = ret });
            }
            foreach (var pID in SF_Services.partitions_)
            {
                var info = SF_Services.EnvoyInformationForPartition(pID.Key);
                foreach (var service in info)
                {
                    ret.Add(service.cluster);
                }
            }
            return Ok(
                new { clusters = ret }
                );
        }

        [HttpGet("routes/{name}/{service_cluster}/{service_node}")]
        public IActionResult GetRoutes(string name, string service_cluster, string service_node)
        {
            List<EnvoyRouteModel> ret = new List<EnvoyRouteModel>();
            if (SF_Services.partitions_ == null)
            {
                return Ok(
                    new
                    {
                        virtual_hosts = new[]
                        {
                            new {
                                name = "reverse_proxy",
                                domains = new List<string>() { "*" },
                                routes = ret
                            }
                        }
                    });
            }
            foreach (var pID in SF_Services.partitions_)
            {
                var info = SF_Services.EnvoyInformationForPartition(pID.Key);
                foreach (var service in info)
                {
                    ret.AddRange(service.routes);
                }
            }
            return Ok(
                new
                {
                    virtual_hosts = new[]
                    {
                        new {
                            name = "reverse_proxy",
                            domains = new List<string>() { "*" },
                            routes = ret
                        }
                    },
                    response_headers_to_remove = EnvoyDefaults.response_headers_to_remove
                });
        }

        [HttpGet("registration/{routeConfig}")]
        public IActionResult GetHosts(string routeConfig)
        {
            List<EnvoyHostModel> ret = new List<EnvoyHostModel>();

            var nameSegements = routeConfig.Split('|');
            Guid pId = new Guid(nameSegements[0]);
            var info = SF_Services.EnvoyInformationForPartition(pId);
            foreach (var service in info)
            {
                if (service.cluster.name != routeConfig)
                {
                    continue;
                }
                ret.AddRange(service.hosts);
            }
            return Ok(
                new { hosts = ret }
                );
        }
    }
}
