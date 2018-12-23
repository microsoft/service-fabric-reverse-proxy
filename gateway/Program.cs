using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace startup
{
    class Program
    {
        public const string Env_Fabric_NodeIPOrFQDN = "Fabric_NodeIPOrFQDN";
        public const string Env_GatewayMode = "GatewayMode";
        public const string Env_Fabric_Endpoint_GatewayProxyResolverEndpoint = "Fabric_Endpoint_GatewayProxyResolverEndpoint";
        public const string Env_Gateway_Resolver_Uses_Dynamic_Port = "Gateway_Resolver_Uses_Dynamic_Port";

        public const string GatewayProxyResolverStaticPort = "19079";
        public const string LocalProxyResolverURI = "tcp://127.0.0.1:5000";

        public const string DiscoveryType_Static = "static";
        public const string DiscoveryType_LogicalDns = "logical_dns";
        public static readonly TimeSpan EndpointConnectTimeout = TimeSpan.FromSeconds(5);
        public const int ConnectRetryCount = 10;
        public static readonly TimeSpan DelayBetweenRetry = TimeSpan.FromSeconds(10);

        private static string fabricNodeIpOrFQDN;
        private static string gatewayMode;
        private static string proxyResolverEndpointPort;
        private static string isDynamicPortResolver;

        private static int indentLevel = 0;
        private static string indentSpaces = "";

        public enum IndentOperation
        {
            NoChange,
            BeginLevel,
            EndLevel
        }

        public static void LogMessage(string message, IndentOperation indentOp = IndentOperation.NoChange)
        {
            if (indentOp == IndentOperation.EndLevel)
            {
                indentLevel--;
                if (indentLevel < 0) indentLevel = 0;
                indentSpaces = new string(' ', indentLevel * 2);
            }
            // Get the local time zone and the current local time and year.
            DateTime currentDate = DateTime.UtcNow;
            System.Console.WriteLine("[{0}][info][startup] {2}{1}", currentDate.ToString("yyyy-MM-dd HH:mm:ss.fffZ"), message, indentSpaces);
            if (indentOp == IndentOperation.BeginLevel)
            {
                indentLevel++;
                indentSpaces = new string(' ', indentLevel * 2);
            }
        }

        private static string GetHostEntry(string hostname)
        {
            try
            {
                LogMessage(string.Format("Trying to get HostEntry for {0}", hostname));
                IPHostEntry host = Dns.GetHostEntry(hostname);

                LogMessage(string.Format("GetHostEntry({0}) returns:", hostname));

                foreach (IPAddress address in host.AddressList)
                {
                    LogMessage(string.Format("    {0}", address.ToString()));
                }
                return hostname;
            }
            catch (Exception e)
            {
                LogMessage(string.Format("Exception=    {0}", e.ToString()));
            }
            return null;
        }

        private static string GetDNSResolveableHostName(string hostname)
        {
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                var dnsSuffix = networkInterface.GetIPProperties().DnsSuffix;
                if (string.IsNullOrWhiteSpace(dnsSuffix))
                {
                    continue;
                }

                if (hostname.EndsWith(dnsSuffix))
                {
                    // We have a hostname that matches one of the DNS suffix. 
                    // Don't try appending other suffixes and resolve
                    break;
                }
                
                var newHostName = hostname + "." + dnsSuffix;
                if (GetHostEntry(newHostName) != null)
                {
                    LogMessage(string.Format("Reachable hostname: {0}", newHostName));
                    return newHostName;
                }
            }

            LogMessage(string.Format("Did not find a reachable hostname for: {0}", hostname));
#if (DEVELOPMENT_ENVIRONMENT)
            return null;
#else
            return hostname;
#endif
        }

        static string GetReachableIPAddress()
        {
            var resolverPort = int.Parse(proxyResolverEndpointPort);
            for (int i = 0; i < ConnectRetryCount; i++)
            {
                foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    foreach (var address in networkInterface.GetIPProperties().GatewayAddresses)
                    {
                        if (IPAddress.IsLoopback(address.Address) || address.Address.AddressFamily != AddressFamily.InterNetwork)
                        {
                            continue;
                        }

                        LogMessage(string.Format("Trying to connect to : {0}:{1}",
                            address.Address.ToString(),
                            proxyResolverEndpointPort));

                        Socket s = new Socket(
                            AddressFamily.InterNetwork,
                            SocketType.Stream,
                            ProtocolType.Tcp);
                        try
                        {
                            IAsyncResult result = s.BeginConnect(address.Address.ToString(), resolverPort, null, null);
                            result.AsyncWaitHandle.WaitOne(EndpointConnectTimeout, true);

                            if (s.Connected)
                            {
                                LogMessage(string.Format("Sucessfully connected : {0}:{1}",
                                    address.Address.ToString(),
                                    proxyResolverEndpointPort));

                                s.Close();
                                s = null;

                                return address.Address.ToString();
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    Task.Delay(DelayBetweenRetry);
                }
            }

            LogMessage(string.Format("Did not find a reachable IP for: {0}", fabricNodeIpOrFQDN));
            return null;
        }

        static string GetDiscoveryType()
        {
            var hostNameType = Uri.CheckHostName(fabricNodeIpOrFQDN);
            switch (hostNameType)
            {
                case UriHostNameType.IPv4:
                case UriHostNameType.IPv6:
                    return DiscoveryType_Static;

                case UriHostNameType.Dns:
                    if (GetHostEntry(fabricNodeIpOrFQDN) == null)
                    {
                        var hostname = GetDNSResolveableHostName(fabricNodeIpOrFQDN);
                        if (hostname != null)
                        {
                            fabricNodeIpOrFQDN = hostname;
                            return DiscoveryType_LogicalDns;
                        }
                    }

                    // Cannot use DNS
                    // Let's try and check if we can reach the end point on one of the
                    // network's Default Gateway IPs
                    var ipAddress = GetReachableIPAddress();
                    if (ipAddress != null)
                    {
                        fabricNodeIpOrFQDN = ipAddress;
                        return DiscoveryType_Static;
                    }
                    return DiscoveryType_LogicalDns;

                default:
                    return String.Empty;
            }
        }

        static string GetResolverURI()
        {
            if (!Convert.ToBoolean(gatewayMode))
            {
                LogMessage(string.Format("Proxy not running in GatewayMode. Use local resolver {0}", LocalProxyResolverURI));
                return LocalProxyResolverURI;
            }

            if (!Convert.ToBoolean(isDynamicPortResolver))
            {
                proxyResolverEndpointPort = GatewayProxyResolverStaticPort;
            }

            string resolverURI = String.Format("tcp://{0}:{1}", fabricNodeIpOrFQDN, proxyResolverEndpointPort);
            return resolverURI;
        }

        static void ReplaceContents(string inputFile, string outputFile)
        {
            var fileContents = System.IO.File.ReadAllText(inputFile);

            string discoveryType = GetDiscoveryType();
            string resolverUri = GetResolverURI();

            LogMessage(string.Format("discovery type: {0}, resolver URI: {1}", discoveryType, resolverUri));
            fileContents = fileContents.Replace("DISCOVERYTYPE", discoveryType);
            fileContents = fileContents.Replace("RESOLVERURI", resolverUri);

            System.IO.File.WriteAllText(outputFile, fileContents);
        }

        static int Main(string[] args)
        {
#if (DEVELOPMENT_ENVIRONMENT)
            LogMessage("Development environment");
#else
            LogMessage("Mesh environment");
#endif

            if (args.Length != 2)
            {
                LogMessage(string.Format("syntax: UpdateConfig.exe confile_template_file output_config_file"));
                return 0;
            }

            fabricNodeIpOrFQDN = Environment.GetEnvironmentVariable(Env_Fabric_NodeIPOrFQDN);
            gatewayMode = Environment.GetEnvironmentVariable(Env_GatewayMode);
            proxyResolverEndpointPort = Environment.GetEnvironmentVariable(Env_Fabric_Endpoint_GatewayProxyResolverEndpoint);
            isDynamicPortResolver = Environment.GetEnvironmentVariable(Env_Gateway_Resolver_Uses_Dynamic_Port);

            LogMessage(string.Format("Environment variable {0} = {1}", Env_Fabric_NodeIPOrFQDN, fabricNodeIpOrFQDN));
            LogMessage(string.Format("Environment variable {0} = {1}", Env_Fabric_Endpoint_GatewayProxyResolverEndpoint, proxyResolverEndpointPort));
            LogMessage(string.Format("Environment variable {0} = {1}", Env_GatewayMode, gatewayMode));
            LogMessage(string.Format("Environment variable {0} = {1}", Env_Gateway_Resolver_Uses_Dynamic_Port, isDynamicPortResolver));

            Program.ReplaceContents(args[0], args[1]);

            return 1;
        }
    }
}
