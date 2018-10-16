using System;

namespace CheckHostName
{
    class Program
    {

        /// <summary>
        /// Check whether the provided string is an IP or DNS name.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>
        /// Returns:
        /// 1 - IP Address
        /// 2 - DNS name
        /// 3 - Failed to determine if input is IP or DNS.
        /// </returns>
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("IPAddress/FQDN empty.");
                return 0;
            }

            Console.WriteLine("Input string: {0}", args[0]);

            var hostNameType = Uri.CheckHostName(args[0]);
            switch (hostNameType)
            {
                case UriHostNameType.IPv4:
                case UriHostNameType.IPv6:
                    return 1;

                case UriHostNameType.Dns:
                    return 2;

                default:
                    return 0;
            }
        }
    }
}
