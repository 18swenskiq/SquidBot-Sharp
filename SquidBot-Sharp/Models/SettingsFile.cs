using System;
using System.Collections.Generic;
using System.Text;

namespace SquidBot_Sharp.Models
{
    public static class SettingsFile
    {
        public static string botkey { get; set; }
        public static string databaseserver { get; set; }
        public static string databaseusername { get; set; }
        public static string databasename { get; set; }
        public static string databasepassword { get; set; }
        public static string faceitapikey { get; set; }
        public static string steamwebapikey { get; set; }
        public static string databasebackuplocation { get; set; }
    }

    public class SettingsFileDeserialize
    {
        public string botkey { get; set; }
        public string databaseserver { get; set; }
        public string databaseusername { get; set; }
        public string databasename { get; set; }
        public string databasepassword { get; set; }
        public string faceitapikey { get; set; }
        public string steamwebapikey { get; set; }
        public string databasebackuplocation { get; set; }
    }
}
