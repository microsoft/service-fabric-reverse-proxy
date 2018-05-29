using Cache;
using Envoy.Api.V2;
using Envoy.Api.V2.Endpoint;
using Envoy.Api.V2.Listener2;
using Envoy.Api.V2.Route;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceFabric.Helpers;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ManagementServer
{
    public class cluster_ssl_context
    {
        static string ca_cert_file_path = "/var/lib/sfreverseproxycerts/servicecacert.pem";
        public cluster_ssl_context(string verify_certificate_hash, List<string> verify_subject_alt_name)
        {
            this.verify_certificate_hash = verify_certificate_hash;
            this.verify_subject_alt_name = verify_subject_alt_name;
            if (File.Exists(ca_cert_file_path)) // change to check for existance of file
            {
                ca_cert_file = ca_cert_file_path;
            }
        }

        [JsonProperty]
        public string cert_chain_file = "/var/lib/sfreverseproxycerts/reverseproxycert.pem";

        [JsonProperty]
        public string private_key_file = "/var/lib/sfreverseproxycerts/reverseproxykey.pem";

        [JsonProperty]
        public string verify_certificate_hash;
        public bool ShouldSerializeverify_certificate_hash()
        {
            return verify_certificate_hash != null;
        }

        [JsonProperty]
        public List<string> verify_subject_alt_name;
        public bool ShouldSerializeverify_subject_alt_name()
        {
            return verify_subject_alt_name != null && verify_subject_alt_name.Count != 0;
        }

        [JsonProperty]
        public string ca_cert_file;
        public bool ShouldSerializeca_cert_file()
        {
            return ca_cert_file != null;
        }
    }

    public class EnvoyDefaults
    {
        public static void LogMessage(string message)
        {
            // Get the local time zone and the current local time and year.
            DateTime currentDate = DateTime.Now;
            System.Console.WriteLine("{0}, {1}", currentDate.ToString("O"), message);
        }
        static EnvoyDefaults()
        {
            var connectTimeout = Environment.GetEnvironmentVariable("HttpRequestConnectTimeoutMs");
            if (connectTimeout != null)
            {
                try
                {
                    connect_timeout_ms = Convert.ToInt32(connectTimeout);
                }
                catch { }
            }

            var requestTimeout = Environment.GetEnvironmentVariable("DefaultHttpRequestTimeoutMs");
            if (requestTimeout != null)
            {
                try
                {
                    timeout_ms = Convert.ToInt32(requestTimeout);
                }
                catch { }
            }

            var removeResponseHeaders = Environment.GetEnvironmentVariable("RemoveServiceResponseHeaders");
            if (removeResponseHeaders != null)
            {
                try
                {
                    response_headers_to_remove.AddRange(removeResponseHeaders.Replace(" ", "").Split(','));
                }
                catch { }
            }

            var useHttps = Environment.GetEnvironmentVariable("UseHttps");
            if (useHttps != null && useHttps == "true")
            {
                var verify_certificate_hash = Environment.GetEnvironmentVariable("ServiceCertificateHash");

                var subjectAlternateName = Environment.GetEnvironmentVariable("ServiceCertificateAlternateNames");
                if (subjectAlternateName != null)
                {
                    try
                    {
                        verify_subject_alt_name.AddRange(subjectAlternateName.Replace(" ", "").Split(','));
                    }
                    catch { }
                }
                cluster_ssl_context = new cluster_ssl_context(verify_certificate_hash, verify_subject_alt_name);
            }

            host_ip = Environment.GetEnvironmentVariable("Fabric_NodeIPOrFQDN");
            if (host_ip == null || host_ip == "localhost")
            {
                bool runningInContainer = Environment.GetEnvironmentVariable("__STANDALONE_TESTING__") == null;
                if (runningInContainer)
                {
                    // running in a container outside of Service Fabric or 
                    // running in a container on local dev cluster
                    host_ip = GetInternalGatewayAddress();
                }
                else
                {
                    // running outside of Service Fabric or 
                    // running on local dev cluster
                    host_ip = GetIpAddress();
                }
            }
            var port = Environment.GetEnvironmentVariable("ManagementPort");
            if (port != null)
            {
                management_port = port;
            }

            client_cert_subject_name = Environment.GetEnvironmentVariable("SF_ClientCertCommonName");
            var issuer_thumbprints = Environment.GetEnvironmentVariable("SF_ClientCertIssuerThumbprints");
            if (issuer_thumbprints != null)
            {
                client_cert_issuer_thumbprints = issuer_thumbprints.Split(',');
            }
            var server_common_names = Environment.GetEnvironmentVariable("SF_ClusterCertCommonNames");
            if (server_common_names != null)
            {
                server_cert_common_names = server_common_names.Split(',');
            }
            var server_issuer_thumbprints = Environment.GetEnvironmentVariable("SF_ClusterCertIssuerThumbprints");
            if (server_issuer_thumbprints != null)
            {
                server_cert_issuer_thumbprints = server_issuer_thumbprints.Split(',');
            }

            LogMessage(String.Format("Management Endpoint={0}:{1}", host_ip, management_port));
            if (client_cert_subject_name != null)
            {
                LogMessage(String.Format("SF_ClientCertCommonName={0}", client_cert_subject_name));
            }
            if (issuer_thumbprints != null)
            {
                LogMessage(String.Format("SF_ClientCertIssuerThumbprints={0}", issuer_thumbprints));
            }
            if (server_cert_common_names != null)
            {
                LogMessage(String.Format("SF_ClusterCertCommonNames={0}", server_cert_common_names));
            }
            if (server_issuer_thumbprints != null)
            {
                LogMessage(String.Format("SF_ClusterCertIssuerThumbprints={0}", server_issuer_thumbprints));
            }
        }
        private static string GetInternalGatewayAddress()
        {
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.GetIPProperties().GatewayAddresses != null &&
                    networkInterface.GetIPProperties().GatewayAddresses.Count > 0)
                {
                    foreach (GatewayIPAddressInformation gatewayAddr in networkInterface.GetIPProperties().GatewayAddresses)
                    {
                        if (gatewayAddr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            return gatewayAddr.Address.ToString();
                        }
                    }
                }
            }
            throw new ArgumentNullException("internalgatewayaddress");
        }

        private static string GetIpAddress()
        {
            var hostname = Dns.GetHostName();
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = hostEntry.AddressList.FirstOrDefault(
                                                                       ip =>
                                                                       (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork));
            if (ipAddress != null)
            {
                return ipAddress.ToString();
            }
            throw new InvalidOperationException("HostIpAddress");
        }

        public static string LocalHostFixup(string host)
        {
            if (host == null || host == "localhost")
            {
                return host_ip;
            }
            return host;
        }

        public static int connect_timeout_ms = 5000;

        public static int timeout_ms = 120000;

        public static List<string> response_headers_to_remove = new List<string>();

        public static string verify_certificate_hash;

        public static List<string> verify_subject_alt_name = new List<string>();

        public static cluster_ssl_context cluster_ssl_context;

        public static string host_ip;

        public static string management_port = "19000";

        public static string client_cert_subject_name;

        public static string[] client_cert_issuer_thumbprints;

        public static string[] server_cert_common_names;

        public static string[] server_cert_issuer_thumbprints;
    }

    public class SF_Endpoint
    {
        public SF_Endpoint(ServiceEndpointRole role, Uri uri)
        {
            role_ = role;
            endpoint_ = uri;
        }

        [JsonProperty(PropertyName = "role")]
        public ServiceEndpointRole role_;

        [JsonProperty(PropertyName = "endpoint")]
        public Uri endpoint_;
    }
    public class SF_Partition
    {
        public SF_Partition(Uri serviceName, ServiceKind serviceKind, ServicePartitionInformation partitionInformation, ServiceEndpointsVersion version, Dictionary<string, List<SF_Endpoint>> listeners)
        {
            serviceName_ = serviceName;
            serviceKind_ = serviceKind;
            partitionInformation_ = partitionInformation;
            version_ = version;
            listeners_ = listeners;
        }

        [JsonProperty(PropertyName = "service_name")]
        public Uri serviceName_;

        [JsonProperty(PropertyName = "service_kind")]
        public ServiceKind serviceKind_;

        [JsonProperty(PropertyName = "partition_information")]
        public ServicePartitionInformation partitionInformation_;

        [JsonProperty(PropertyName = "version")]
        public ServiceEndpointsVersion version_;

        [JsonProperty(PropertyName = "listeners")]
        public Dictionary<string, List<SF_Endpoint>> listeners_;
    }

    public class SF_Services
    {
        public static Dictionary<Guid, SF_Partition> partitions_;

        // Temporary lists to handle initialization race
        static Dictionary<Guid, SF_Partition> partitionsAdd_ = new Dictionary<Guid, SF_Partition>();

        static Dictionary<Guid, SF_Partition> partitionsRemove_ = new Dictionary<Guid, SF_Partition>();

        static object lock_ = new object();

        static private FabricClient client;

        static long version_ = 0;
        static SF_Services()
        {

            try
            {
                partitions_ = null;

                if (EnvoyDefaults.client_cert_subject_name != null)
                {
                    X509Credentials creds = new X509Credentials();
                    creds.FindType = X509FindType.FindBySubjectName;
                    creds.FindValue = EnvoyDefaults.client_cert_subject_name;
                    if (EnvoyDefaults.client_cert_issuer_thumbprints != null)
                    {
                        foreach (var issuer in EnvoyDefaults.client_cert_issuer_thumbprints)
                        {
                            creds.IssuerThumbprints.Add(issuer);
                        }
                    }
                    if (EnvoyDefaults.server_cert_common_names != null)
                    {
                        foreach (var commonName in EnvoyDefaults.server_cert_common_names)
                        {
                            creds.RemoteCommonNames.Add(commonName);
                        }
                    }
                    else
                    {
                        creds.RemoteCommonNames.Add(EnvoyDefaults.client_cert_subject_name);
                    }
                    if (EnvoyDefaults.server_cert_issuer_thumbprints != null)
                    {
                        foreach (var issuer in EnvoyDefaults.server_cert_issuer_thumbprints)
                        {
                            creds.RemoteCertThumbprints.Add(issuer);
                        }
                    }
                    else if (EnvoyDefaults.client_cert_issuer_thumbprints != null)
                    {
                        foreach (var issuer in EnvoyDefaults.client_cert_issuer_thumbprints)
                        {
                            creds.RemoteCertThumbprints.Add(issuer);
                        }
                    }
                    creds.StoreLocation = StoreLocation.LocalMachine;
                    creds.StoreName = "/app/sfcerts";

                    client = new FabricClient(creds, new string[] { EnvoyDefaults.host_ip + ":" + EnvoyDefaults.management_port });
                }
                else
                {
                    client = new FabricClient(new string[] { EnvoyDefaults.host_ip + ":" + EnvoyDefaults.management_port });
                }

                EnableResolveNotifications.RegisterNotificationFilter("fabric:", client, Handler);
            }
            catch (Exception e)
            {
                EnvoyDefaults.LogMessage(String.Format("Error={0}", e));
            }
        }

        private static void Handler(Object sender, EventArgs eargs)
        {
            EnvoyDefaults.LogMessage("In Notification handler");
            List<string> clusterNameList = new List<string>();

            try
            {
                var notification = ((FabricClient.ServiceManagementClient.ServiceNotificationEventArgs)eargs).Notification;
                if (notification.Endpoints.Count == 0)
                {
                    Console.WriteLine("Obtaining remove lock in Handler");
                    //remove
                    lock (lock_)
                    {
                        if (partitions_ != null)
                        {
                            partitions_.Remove(notification.PartitionId);

                            {

                                long innerVersion = Interlocked.Increment(ref version_);
                                var innerSnapshot = EnvoyPartitionInfo(innerVersion.ToString());

                                var simpleCache = DiscoveryServer.ADSServer.ADSConfigWatcher as SimpleCache<string>;
                                simpleCache.SetSnapshot(SingleNodeGroup.GROUP, innerSnapshot);

                            }
                        }
                        else
                        {
                            partitionsAdd_.Remove(notification.PartitionId);
                            partitionsRemove_[notification.PartitionId] = null;
                        }
                        EnvoyDefaults.LogMessage(String.Format("Removed: {0}", notification.PartitionId));

                        return;
                    }
                }

                Dictionary<string, List<SF_Endpoint>> listeners = new Dictionary<string, List<SF_Endpoint>>();
                ServiceEndpointRole role = ServiceEndpointRole.Invalid;
                foreach (var notificationEndpoint in notification.Endpoints)
                {
                    if (notificationEndpoint.Address.Length == 0)
                    {
                        continue;
                    }
                    JObject addresses;
                    try
                    {
                        addresses = JObject.Parse(notificationEndpoint.Address);
                    }
                    catch
                    {
                        continue;
                    }

                    var notificationListeners = addresses["Endpoints"].Value<JObject>();
                    foreach (var notificationListener in notificationListeners)
                    {
                        if (!listeners.ContainsKey(notificationListener.Key))
                        {
                            listeners.Add(notificationListener.Key, new List<SF_Endpoint>());
                        }
                        try
                        {
                            var listenerAddressString = notificationListener.Value.ToString();
                            if (!listenerAddressString.StartsWith("http") &&
                                !listenerAddressString.StartsWith("https"))
                            {
                                continue;
                            }
                            var listenerAddress = new Uri(listenerAddressString);
                            listeners[notificationListener.Key].Add(new SF_Endpoint(notificationEndpoint.Role, listenerAddress));
                        }
                        catch (System.Exception e)
                        {
                            EnvoyDefaults.LogMessage(String.Format("Error={0}", e));
                        }
                    }
                    if (role == ServiceEndpointRole.Invalid)
                    {
                        role = notificationEndpoint.Role;
                    }
                }

                // Remove any listeners without active endpoints
                List<string> listenersToRemove = new List<string>();
                foreach (var listener in listeners)
                {
                    if (listener.Value.Count == 0)
                    {
                        listenersToRemove.Add(listener.Key);
                    }
                }

                //if (listenersToRemove.Count > 0)
                //    pushCDSLDSUpdate = true;

                foreach (var listener in listenersToRemove)
                {
                    listeners.Remove(listener);
                }

                // sort list of endpoints for each listener by its Uri. Tries to keep the index for secondaries stable when nothing changes
                foreach (var listener in listeners)
                {
                    listener.Value.Sort(delegate (SF_Endpoint a, SF_Endpoint b)
                                        {
                                            if (a.role_ == ServiceEndpointRole.StatefulPrimary)
                                            {
                                                return -1;
                                            }
                                            return Uri.Compare(a.endpoint_, b.endpoint_, UriComponents.AbsoluteUri, UriFormat.Unescaped,
                                                               StringComparison.InvariantCulture);
                                        });

                    string cluster = notification.PartitionId.ToString() + "|" + listener.Key;
                    Console.WriteLine("Received SF notification Handler callback for {0}", cluster);
                    clusterNameList.Add(cluster);
                }

                if (listeners.Count != 0)
                {
                    var partitionInfo = new SF_Partition(notification.ServiceName,
                                                         (role == ServiceEndpointRole.Stateless) ? ServiceKind.Stateless : ServiceKind.Stateful,
                                                         notification.PartitionInfo,
                                                         notification.Version,
                                                         listeners
                                                         );

                    lock (lock_)
                    {
                        if (partitions_ != null)
                        {
                            partitions_[notification.PartitionId] = partitionInfo;
                        }
                        else
                        {
                            partitionsRemove_.Remove(notification.PartitionId);
                            partitionsAdd_[notification.PartitionId] = partitionInfo;
                        }
                        EnvoyDefaults.LogMessage(String.Format("Added: {0}={1}", notification.PartitionId,
                                                               JsonConvert.SerializeObject(partitionInfo)));
                    }
                }

                long version = Interlocked.Increment(ref version_);
                var snapshot = EnvoyPartitionInfo(version.ToString(), clusterNameList);
            }
            catch (Exception e)
            {
                EnvoyDefaults.LogMessage(String.Format("Error={0}", e.Message));
                EnvoyDefaults.LogMessage(String.Format("Error={0}", e.StackTrace));
            }
        }

        /// <summary>
        /// This function gathers the state of the cluster on startup and caches the information
        /// Changes to cluster state are handled through notifications.
        /// 
        /// Capture information for each replica for every service running in the cluster.
        /// </summary>
        /// <returns></returns>
        public static async Task InitializePartitionData()
        {
            // Populate data locally
            Dictionary<Guid, SF_Partition> partitionData = new Dictionary<Guid, SF_Partition>();

            var queryManager = client.QueryManager;

            var applications = await queryManager.GetApplicationListAsync();
            foreach (var application in applications)
            {
                var services = await queryManager.GetServiceListAsync(application.ApplicationName);
                foreach (var service in services)
                {
                    var partitions = await queryManager.GetPartitionListAsync(service.ServiceName);
                    foreach (var partition in partitions)
                    {
                        if (service.ServiceKind == ServiceKind.Stateful && partition.PartitionInformation.Kind != ServicePartitionKind.Int64Range)
                        {
                            continue;
                        }

                        Dictionary<string, List<SF_Endpoint>> listeners = new Dictionary<string, List<SF_Endpoint>>();

                        var replicas = await queryManager.GetReplicaListAsync(partition.PartitionInformation.Id);
                        foreach (var replica in replicas)
                        {
                            if (replica.ReplicaAddress.Length == 0)
                            {
                                continue;
                            }
                            JObject addresses;
                            try
                            {
                                addresses = JObject.Parse(replica.ReplicaAddress);
                            }
                            catch
                            {
                                continue;
                            }

                            var replicaListeners = addresses["Endpoints"].Value<JObject>();
                            foreach (var replicaListener in replicaListeners)
                            {
                                var role = ServiceEndpointRole.Stateless;
                                if (partition.ServiceKind == ServiceKind.Stateful)
                                {
                                    var statefulRole = ((StatefulServiceReplica)replica).ReplicaRole;
                                    switch (statefulRole)
                                    {
                                        case ReplicaRole.Primary:
                                            role = ServiceEndpointRole.StatefulPrimary;
                                            break;
                                        case ReplicaRole.ActiveSecondary:
                                            role = ServiceEndpointRole.StatefulSecondary;
                                            break;
                                        default:
                                            role = ServiceEndpointRole.Invalid;
                                            break;
                                    }
                                }
                                if (!listeners.ContainsKey(replicaListener.Key))
                                {
                                    listeners[replicaListener.Key] = new List<SF_Endpoint>();
                                }
                                try
                                {
                                    var listenerAddressString = replicaListener.Value.ToString();
                                    if (!listenerAddressString.StartsWith("http") &&
                                        !listenerAddressString.StartsWith("https"))
                                    {
                                        continue;
                                    }
                                    var listenerAddress = new Uri(replicaListener.Value.ToString());
                                    listeners[replicaListener.Key].Add(new SF_Endpoint(role, listenerAddress));
                                }
                                catch (System.Exception e)
                                {
                                    EnvoyDefaults.LogMessage(String.Format("Error={0}", e));
                                }
                            }
                        }

                        // Remove any listeners without active endpoints
                        List<string> listenersToRemove = new List<string>();
                        foreach (var listener in listeners)
                        {
                            if (listener.Value.Count == 0)
                            {
                                listenersToRemove.Add(listener.Key);
                            }
                        }

                        foreach (var listener in listenersToRemove)
                        {
                            listeners.Remove(listener);
                        }

                        //  sort list of endpoints for each listener by its Uri.
                        // Tries to keep the index for secondaries stable when nothing changes
                        foreach (var listener in listeners)
                        {
                            listener.Value.Sort(delegate (SF_Endpoint a, SF_Endpoint b)
                                                {
                                                    if (a.role_ == ServiceEndpointRole.StatefulPrimary)
                                                    {
                                                        return -1;
                                                    }
                                                    return Uri.Compare(a.endpoint_, b.endpoint_, UriComponents.AbsoluteUri, UriFormat.Unescaped, StringComparison.InvariantCulture);
                                                });
                        }

                        if (listeners.Count == 0)
                        {
                            continue;
                        }

                        var partitionInfo = new SF_Partition(service.ServiceName,
                                                             service.ServiceKind,
                                                             partition.PartitionInformation,
                                                             null,
                                                             listeners);

                        partitionData[partition.PartitionInformation.Id] = partitionInfo;
                    }
                }
            }

            // Process changes received through notifications
            lock (lock_)
            {
                foreach (var partition in partitionsAdd_)
                {
                    partitionData[partition.Key] = partition.Value;
                }
                foreach (var partition in partitionsRemove_)
                {
                    partitionData.Remove(partition.Key);
                }

                // Finally update global state
                partitions_ = partitionData;
                foreach (var partition in partitionData)
                {
                    EnvoyDefaults.LogMessage(String.Format("Added: {0}={1}", partition.Key,
                                                           JsonConvert.SerializeObject(partition.Value)));
                }

                partitionsRemove_ = null;
                partitionsAdd_ = null;

                // TODO-kavyako: Lock anywhere?
                long version = Interlocked.Increment(ref version_);
                var snapshot = EnvoyPartitionInfo(version.ToString());
                var simpleCache = DiscoveryServer.ADSServer.ADSConfigWatcher as SimpleCache<string>;
                simpleCache.SetSnapshot(SingleNodeGroup.GROUP, snapshot);

            }
        }

        public static Snapshot EnvoyPartitionInfo(string version, List<string> resourceNameList = null)
        {
            // Caller has lock_ 
            if (partitions_ == null)
            {
                // No data to send.
                return null;
            }

            var clusterList = new List<Cluster>();
            var clusterLoadAssignmentList = new List<ClusterLoadAssignment>();
            var listenerList = new List<Listener>();
            var routeConfigList = new List<RouteConfiguration>();

            foreach (var partitionEntry in partitions_)
            {
                SF_Partition partition = partitionEntry.Value;
                var partitionId = partitionEntry.Key;

                if (partition.serviceKind_ != ServiceKind.Stateless)
                {
                    // TODO-kavyako: Ignore Stateful for now, handle later.
                    continue;
                }

                var keys = new List<string>(partition.listeners_.Keys);

                foreach (var entry in partition.listeners_)
                {
                    var ep = entry.Value;
                    string cluster = partitionId.ToString() + "|" + entry.Key;

                    // CDS Info
                    var apiConfigSource = new Envoy.Api.V2.Core.ApiConfigSource()
                    {
                        ApiType = Envoy.Api.V2.Core.ApiConfigSource.Types.ApiType.Grpc
                    };
                    apiConfigSource.ClusterNames.Add("xds_cluster");

                    Cluster c = new Cluster()
                    {
                        Name = cluster,
                        Type = Cluster.Types.DiscoveryType.Eds,
                        ConnectTimeout = Duration.FromTimeSpan(TimeSpan.FromMilliseconds(EnvoyDefaults.connect_timeout_ms)),
                        EdsClusterConfig = new Cluster.Types.EdsClusterConfig()
                        {
                            EdsConfig = new Envoy.Api.V2.Core.ConfigSource()
                            {
                                ApiConfigSource = apiConfigSource,
                            }
                        }
                    };

                    Console.WriteLine("Setting Cluster Data");
                    clusterList.Add(c);

                    // For EDS, send update if
                    // 1. resourceNameList is empty/null  (full update)
                    // 2. or if the cluster is mentioned in resource hint, send EDS update.
                    if (resourceNameList == null || !resourceNameList.Any() || resourceNameList.Contains(cluster))
                    {
                        ClusterLoadAssignment cla = new ClusterLoadAssignment()
                        {
                            ClusterName = cluster
                        };

                        var localityLbEndpoint = new LocalityLbEndpoints();

                        foreach (var sfepAddress in ep)
                        {
                            var lbEndpoint = new LbEndpoint()
                            {
                                Endpoint = new Endpoint()
                                {
                                    Address = new Envoy.Api.V2.Core.Address()
                                    {
                                        SocketAddress = new Envoy.Api.V2.Core.SocketAddress()
                                        {
                                            Protocol = Envoy.Api.V2.Core.SocketAddress.Types.Protocol.Tcp,
                                            Address = sfepAddress.endpoint_.Host.Equals("localhost") ? "0.0.0.0" : sfepAddress.endpoint_.Host,
                                            PortValue = (uint)sfepAddress.endpoint_.Port // TODO: Change Port to uint
                                        }
                                    }
                                },
                            };
                            localityLbEndpoint.LbEndpoints.Add(lbEndpoint);
                        }
                        cla.Endpoints.Add(localityLbEndpoint);
                        clusterLoadAssignmentList.Add(cla);
                    }

                    // For RDS: send update if 
                    // TODO: See if we need different notation for route and cluster
                    // 1. resourceNameList is empty/null  (full update)
                    // 2. or if the cluster is mentioned in resource hint, send RDS update.
                    if (resourceNameList == null || !resourceNameList.Any() || resourceNameList.Contains(cluster))
                    {
                        if (entry.Key != "")
                        {
                            // Todo add support for HeaderMatcher in route config., { "name" : ListenerName, "exact_match" : entry.Key}
                            Console.WriteLine("ListenerName present for cluster {0}", cluster);
                        }

                        RouteConfiguration routeConfig = new RouteConfiguration()
                        {
                            Name = cluster
                        };

                        VirtualHost vHost = new VirtualHost()
                        {
                            Name = "RdsService1"
                        };

                        vHost.Domains.Add("*");
                        vHost.Routes.Add(new Route()
                        {
                            Match = new RouteMatch()
                            {
                                Prefix = partition.serviceName_.AbsolutePath + (partition.serviceName_.AbsolutePath.EndsWith("/") ? string.Empty : "/")
                            },
                            Route_ = new RouteAction()
                            {
                                Cluster = cluster,
                                // Is ep[0]  guaranteed to exist?
                                PrefixRewrite = ep[0].endpoint_.AbsolutePath
                            }
                        });

                        routeConfig.VirtualHosts.Add(vHost);
                        routeConfigList.Add(routeConfig);
                        Console.WriteLine("Setting RDS   RouteConfiguration Data:");
                    }
                }
            }

            listenerList.Add(EnvoyListenerInformation(clusterList.Select(c => c.Name)));
            return Snapshot.Create(clusterList, clusterLoadAssignmentList, listenerList, routeConfigList, version);
        }

        public static Listener EnvoyListenerInformation(IEnumerable<string> routeNameList)
        {
            Console.WriteLine("Parsing String to WellKnowTypes.Struct");
            var filterChain = new FilterChain();
            filterChain.Filters.AddRange(
                routeNameList
                .Select(routeName => new Filter()
                {
                    Name = "envoy.http_connection_manager",
                    Config = Google.Protobuf.JsonParser.Default.Parse<Google.Protobuf.WellKnownTypes.Struct>(
                        string.Format(
                            "{{ \"stat_prefix\":\"reverse_proxy_http\",\"codec_type\" : \"AUTO\", \"rds\": {{ \"route_config_name\":\"{0}\", \"config_source\" : {{ \"api_config_source\" : {{ \"api_type\" : \"GRPC\", \"cluster_names\" : [ \"xds_cluster\" ] }} }} }}, \"http_filters\" : [ {{ \"name\" : \"envoy.router\" }} ] }}",
                            routeName))
                }
            ));

            Console.WriteLine("In lds.txt switch");
            Listener l = new Listener()
            {
                Name = "Listener_0",
                Address = new Envoy.Api.V2.Core.Address()
                {
                    SocketAddress = new Envoy.Api.V2.Core.SocketAddress()
                    {
                        Protocol = Envoy.Api.V2.Core.SocketAddress.Types.Protocol.Tcp,
                        Address = "0.0.0.0",
                        PortValue = 19081,
                    }
                }
            };

            l.FilterChains.Add(filterChain);
            return l;
        }
    }
}
