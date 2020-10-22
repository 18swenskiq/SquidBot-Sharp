using System;
using System.Collections.Generic;
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

        public static bool ListContainsCaseInsensitive(List<string> stringlist, string checkifin)
        {
            foreach (var item in stringlist)
            {
                var compareresult = string.Compare(item, checkifin, true);
                if (compareresult == 0)
                {
                    return true;
                }
            }
            return false;
        }


        public static string SteamIDFrom64ToLegacy(string input64)
        {
            Int64 num64 = Int64.Parse(input64);
            string binary = Convert.ToString(num64, 2);
            binary = binary.PadLeft(64, '0');
            int legacy_x, legacy_y, legacy_z;
            string legacy_x_str = "";
            string legacy_y_str = "";
            string legacy_z_str = "";
            string accounttype = "";
            string accountinstance = "";
            for (int i = 0; i < 8; i++)
            {
                legacy_x_str += binary[i];
            }
            for (int i = 8; i < 12; i++)
            {
                accounttype += binary[i];
            }
            for (int i = 12; i < 32; i++)
            {
                accountinstance += binary[i];
            }
            for (int i = 32; i < 63; i++)
            {
                legacy_z_str += binary[i];
            }
            legacy_y_str += binary[63];

            legacy_x = Convert.ToInt32(legacy_x_str, 2);
            legacy_y = Convert.ToInt32(legacy_y_str, 2);
            legacy_z = Convert.ToInt32(legacy_z_str, 2);
            return $"STEAM_{legacy_x}:{legacy_y}:{legacy_z}";
        }
    }
}
