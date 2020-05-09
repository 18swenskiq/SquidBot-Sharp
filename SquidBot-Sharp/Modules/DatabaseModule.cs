using Renci.SshNet;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Net;

namespace SquidBot_Sharp.Modules
{
    public class DatabaseModule
    {
        private static SqlConnection cnn { get; set; }

        public DatabaseModule(string databaseserver, string databasename, string username, string password)
        {
            var sqlconnectionstring = new SqlConnectionStringBuilder
            {
                DataSource = databaseserver,
                InitialCatalog = databasename,
                UserID = username,
                Password = password,
                ConnectTimeout = 10,
            };
            cnn = new SqlConnection(sqlconnectionstring.ToString());
            cnn.Open();
            Console.WriteLine("Connection opened!");
            cnn.Close();
        }
    }
}
