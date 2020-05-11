using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

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
    }
}
