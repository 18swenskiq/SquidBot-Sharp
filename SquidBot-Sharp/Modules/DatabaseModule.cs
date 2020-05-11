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
                        mylist.Add(rdr[0].ToString().Replace("SQUIDBOT_TOKEN_DQUOTE", "\"").Replace("SQUIDBOT_TOKEN_BACKSLASH", @"\"));
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
                    string sqlcommand = $"INSERT INTO UserMessages(UserID,Message) VALUES({userID},\"{message.Replace("\"", "SQUIDBOT_TOKEN_BACKSLASH").Replace("\"", "SQUIDBOT_TOKEN_DQUOTE")}\");";
                    MySqlCommand cmd = new MySqlCommand(sqlcommand, con);
                    cmd.ExecuteNonQuery();
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
