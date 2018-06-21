using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Fabric;
using ServiceFabric.Helpers;

namespace webapi.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    public class v1 : Controller
    {
        [HttpGet]
        public IActionResult GetAsync()
        {
            return Ok(
                new { partitions = SF_Services.partitions_, services = SF_Services.services_ }
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
            foreach (var service in SF_Services.services_)
            {
                if (!service.Value.StatefulService)
                {
                    continue;
                }
                EnvoyClusterModel info = new EnvoyClusterModel(service.Key);
                ret.Add(info);
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
            foreach (var service in SF_Services.services_)
            {
                if (!service.Value.StatefulService)
                {
                    continue;
                }
                string routeConfigForPartition = service.Value.Partitions[0].ToString() + "|" + service.Value.EndpointIndex.ToString() + "|0";
                var info = SF_Services.EnvoyInformationForPartition(service.Value.Partitions[0]);
                foreach (var serviceInfo in info)
                {
                    if (serviceInfo.cluster.name != routeConfigForPartition)
                    {
                        continue;
                    }
                    var routes = serviceInfo.routes;
                    foreach (var route in routes)
                    {
                        bool addRoute = true;
                        route.cluster = service.Key;
                        var headers = route.headers;
                        for (var i = headers.Count - 1; i >= 0; i--)
                        {
                            var header = headers[i];
                            var headerName = (string)header.GetValue("name");
                            if (headerName == "SecondaryReplicaIndex")
                            {
                                addRoute = false;
                                break;
                            }
                            if (headerName == "PartitionKey")
                            {
                                headers.RemoveAt(i);
                            }
                        }
                        route.prefix_rewrite = "/";
                        if (addRoute)
                        {
                            ret.Add(route);
                        }
                    }
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
            // Deal with service name cluster as opposed to a partition cluster
            if (nameSegements[2] == "-2")
            {
                var service = SF_Services.services_[routeConfig];
                foreach (var partition in service.Partitions)
                {
                    string routeConfigForPartition = partition.ToString() + "|" + service.EndpointIndex.ToString() + "|0";
                    var pId = partition;
                    var info = SF_Services.EnvoyInformationForPartition(pId);
                    foreach (var serviceInfo in info)
                    {
                        if (serviceInfo.cluster.name != routeConfigForPartition)
                        {
                            continue;
                        }
                        ret.AddRange(serviceInfo.hosts);
                    }
                }
            }
            else
            {
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
            }
            return Ok(
                new { hosts = ret }
                );
        }
    }
}
