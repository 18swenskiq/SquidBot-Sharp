using System;
using System.Linq;
using System.Net;

namespace SquidBot_Sharp.Utilities
{
    public static class GeneralUtil
    {
        public static string GetServerCode(string fullServerAddress)
        {
            fullServerAddress = fullServerAddress.ToLower();
            if (fullServerAddress.Contains('.'))
            {
                return fullServerAddress.Substring(0, fullServerAddress.IndexOf(".", StringComparison.Ordinal));
            }

            return fullServerAddress;
        }

        public static IPEndPoint GetIpEndPointFromString(string address, ushort serverPort = 27015)
        {
            ushort sPort = serverPort;
            var targetDns = address;

            if (address.Contains(':'))
            {
                var splitServer = address.Split(':');
                targetDns = splitServer[0];

                if(!ushort.TryParse(splitServer[1], out sPort))
                {
                    throw new NullReferenceException("Malformed server port in address. Verify that it is stored as: subDomain.Domain.TLD:port");
                }
            }
            var ip = GetIPHost(targetDns).AddressList.FirstOrDefault();

            return new IPEndPoint(ip, sPort);
        }

        public static IPHostEntry GetIPHost(string address)
        {
            if (address.Contains(':')) address = address.Substring(0, address.IndexOf(":", StringComparison.Ordinal));
            IPHostEntry iPHostEntry = null;
            try
            {
                iPHostEntry = Dns.GetHostEntry(address);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to get iPHostEntry for {address}");
                Console.WriteLine(e);
                throw;
            }

            return iPHostEntry;
        }
    }
}
