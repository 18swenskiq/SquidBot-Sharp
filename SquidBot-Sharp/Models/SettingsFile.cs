using System;
using System.Collections.Generic;
using System.Text;

namespace SquidBot_Sharp.Models
{
    public static class SettingsFile
    {
        public static string botkey { get; set; }
        public static string databaseurl { get; set; }
        public static string databaseusername { get; set; }
        public static string databasepassword { get; set; }
        public static string faceitapikey { get; set; }
    }

    public class SettingsFileDeserialize
    {
        public string botkey { get; set; }
        public string databaseurl { get; set; }
        public string databaseusername { get; set; }
        public string databasepassword { get; set; }
        public string faceitapikey { get; set; }
    }
}
