using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SquidBot_Sharp.Models;
using SquidBot_Sharp.Utilities;
using SteamKit2.Internal;

namespace SquidBot_Sharp.Modules
{
    public static class DatabaseModule
    {
        private static string ConnectionString { get; set; }
        public static Exception HitException { get; set; }

        public static void SetUpMySQLConnection(string databaseserver, string databasename, string username, string password)
        {
            ConnectionString = $"server={databaseserver};user={username};database={databasename};port=3306;password={password};";
            HitException = null;
        }

        public static async Task BackupDatabase()
        {
            HitException = null;
            var backuplocation = SettingsFile.databasebackuplocation.Replace("DATETIME", DateTime.Now.ToString("yyyy'-'MM'-'dd'_'HH'-'mm'-'ss"));

            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        using (MySqlBackup mb = new MySqlBackup(cmd))
                        {
                            cmd.Connection = con;
                            await con.OpenAsync();
                            mb.ExportToFile(backuplocation);
                        }
                    }
                }
                catch (Exception ex)
                {
                    HitException = ex;
                }
                finally
                {
                    await con.CloseAsync();
                }
            }
            return;
        } 

        public static async Task<List<string>> GetUserMessages(ulong userID)
        {

            HitException = null;
            List<string> mylist = new List<string>();

            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT Message FROM UserMessages WHERE UserID='{userID}';";

                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    var rdr = await cmd.ExecuteReaderAsync();

                    while (await rdr.ReadAsync())
                    {
                        mylist.Add(rdr[0].ToString());
                    }
                    await rdr.CloseAsync();
                }
                catch (Exception ex)
                {
                    HitException = ex;
                }
                finally
                {
                    await con.CloseAsync();
                }
            }
            return mylist;
        }

        public static async Task<List<string>> GetAllMessages()
        {
            HitException = null;
            List<string> mylist = new List<string>();

            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT Message FROM UserMessages WHERE UserId != '{565566309257969668}';";

                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    var rdr = await cmd.ExecuteReaderAsync();

                    while (await rdr.ReadAsync())
                    {
                        mylist.Add(rdr[0].ToString());
                    }
                    await rdr.CloseAsync();
                }
                catch (Exception ex)
                {
                    HitException = ex;
                }
                finally
                {
                    await con.CloseAsync();
                }
            }
            return mylist;
        }

        public static async Task AddNewUserMessage(ulong userID, string message)
        {
            HitException = null;
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    string sqlcommand = $"INSERT INTO UserMessages(UserID,Message) VALUES({userID},\"{message}\");";
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = con;

                    cmd.CommandText = "INSERT INTO UserMessages(UserID,Message) VALUES(@number, @text);";
                    await cmd.PrepareAsync();

                    cmd.Parameters.AddWithValue("@number", userID);
                    cmd.Parameters.AddWithValue("@text", message);

                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    HitException = ex;
                }
                finally
                {
                    await con.CloseAsync();
                }
            }
            return;
        }

        public static async Task AddTestServer(Server server)
        {
            if (GetTestServerInfo(server.ServerId) != null)
            {
                Console.WriteLine("DB: Unable to add test server since one was found.");
                return;
            }

            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = con;
                    cmd.CommandText = "INSERT INTO Servers(Id, ServerId, Description, Address, RconPassword, FtpUser, FtpPassword, FtpPath, FtpType, Game) VALUES(@val1, @val2, @val3, @val4, @val5, @val6, @val7, @val8, @val9, @val10);";
                    await cmd.PrepareAsync();

                    cmd.Parameters.AddWithValue("@val1", server.Id);
                    cmd.Parameters.AddWithValue("@val2", server.ServerId);
                    cmd.Parameters.AddWithValue("@val3", server.Description);
                    cmd.Parameters.AddWithValue("@val4", server.Address);
                    cmd.Parameters.AddWithValue("@val5", server.RconPassword);
                    cmd.Parameters.AddWithValue("@val6", server.FtpUser);
                    cmd.Parameters.AddWithValue("@val7", server.FtpPassword);
                    cmd.Parameters.AddWithValue("@val8", server.FtpPath);
                    cmd.Parameters.AddWithValue("@val9", server.FtpType);
                    cmd.Parameters.AddWithValue("@val10", server.Game);

                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"DB: Something happened adding test server: {e}.");
                }
                finally
                {
                    await con.CloseAsync();
                }
            }
        }

        public static async Task<List<Server>> GetServerList()
        {
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                List<Server> foundServers = null;
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT Id, ServerId, Description, Address, RconPassword, FtpUser, FtpPassword, FtpPath, FtpType, Game FROM Servers;";

                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    DataTable temp = new DataTable();
                    var adapter = new MySqlDataAdapter(cmd);
                    await adapter.FillAsync(temp);
                    foundServers = new List<Server>();
                    foreach(DataRow row in temp.Rows)
                    {
                        var idstring = row.ItemArray[0];
                        int idvar = (int)idstring;
                        string serveridvar = (string)row.ItemArray[1];
                        string descriptionvar = (string)row.ItemArray[2];
                        string addressvar = (string)row.ItemArray[3];
                        string rconpasswordvar = (string)row.ItemArray[4];
                        string ftpuservar = (string)row.ItemArray[5];
                        string ftpuserpasswordvar = (string)row.ItemArray[6];
                        string ftppathvar = (string)row.ItemArray[7];
                        string ftptypevar = (string)row.ItemArray[8];
                        string gamevar = (string)row.ItemArray[9];
                        var myserver = new Server { Id = idvar, ServerId = serveridvar, Description = descriptionvar, Address = addressvar, RconPassword = rconpasswordvar, FtpUser = ftpuservar, FtpPassword = ftpuserpasswordvar, FtpPath = ftppathvar, FtpType = ftptypevar, Game = gamevar };
                        foundServers.Add(myserver);
                    }
                    return foundServers;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Something happened getting test server: {e}.");
                    return foundServers;
                }
            }
        }

        public static async Task<Server> GetTestServerInfo(string serverID)
        {
            if(serverID == null)
            {
                return null;
            }

            serverID = GeneralUtil.GetServerCode(serverID);

            Server foundServer = null;
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT Id, ServerId, Description, Address, RconPassword, FtpUser, FtpPassword, FtpPath, FtpType, Game FROM Servers WHERE ServerId = 'sc1';";

                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    // This can be optimized because we only get one server back
                    DataTable temp = new DataTable();
                    var adapter = new MySqlDataAdapter(cmd);
                    await adapter.FillAsync(temp);
                    var foundServers = new List<Server>();
                    foreach (DataRow row in temp.Rows)
                    {
                        var idstring = row.ItemArray[0];
                        int idvar = (int)idstring;
                        string serveridvar = (string)row.ItemArray[1];
                        string descriptionvar = (string)row.ItemArray[2];
                        string addressvar = (string)row.ItemArray[3];
                        string rconpasswordvar = (string)row.ItemArray[4];
                        string ftpuservar = (string)row.ItemArray[5];
                        string ftpuserpasswordvar = (string)row.ItemArray[6];
                        string ftppathvar = (string)row.ItemArray[7];
                        string ftptypevar = (string)row.ItemArray[8];
                        string gamevar = (string)row.ItemArray[9];
                        var myserver = new Server { Id = idvar, ServerId = serveridvar, Description = descriptionvar, Address = addressvar, RconPassword = rconpasswordvar, FtpUser = ftpuservar, FtpPassword = ftpuserpasswordvar, FtpPath = ftppathvar, FtpType = ftptypevar, Game = gamevar };
                        foundServers.Add(myserver);
                    }
                    foundServer = foundServers[0];
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Something happened getting test server: {e}.");
                    return foundServer;
                }
            }

            return foundServer;
        }
    }
}
