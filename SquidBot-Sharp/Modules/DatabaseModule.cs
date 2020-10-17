using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using SquidBot_Sharp.Commands;
using SquidBot_Sharp.Models;
using SquidBot_Sharp.Utilities;

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

        public static async Task<PlayerData> GetPlayerMatchmakingStats(string playerId)
        {
            HitException = null;
            PlayerData playerData = new PlayerData();
            List<string> resultList = new List<string>();

            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT DisplayName, PlayerID, CurrentELO, GamesWon, GamesLost, RoundsWon, RoundsLost, KillCount, AssistCount, DeathCount, Headshots, MVPCount FROM MatchmakingStats WHERE PlayerID='{playerId}';";

                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    var rdr = await cmd.ExecuteReaderAsync();

                    while (await rdr.ReadAsync())
                    {
                        resultList.Add(rdr[0].ToString());
                        resultList.Add(rdr[1].ToString());
                        resultList.Add(rdr[2].ToString());
                        resultList.Add(rdr[3].ToString());
                        resultList.Add(rdr[4].ToString());
                        resultList.Add(rdr[5].ToString());
                        resultList.Add(rdr[6].ToString());
                        resultList.Add(rdr[7].ToString());
                        resultList.Add(rdr[8].ToString());
                        resultList.Add(rdr[9].ToString());
                        resultList.Add(rdr[10].ToString());
                        resultList.Add(rdr[11].ToString());
                    }

                    playerData.Name = resultList[0];
                    playerData.ID = resultList[1];
                    playerData.CurrentElo = System.Convert.ToSingle(resultList[2]);
                    playerData.TotalGamesWon = System.Convert.ToInt32(resultList[3]);
                    playerData.TotalGamesLost = System.Convert.ToInt32(resultList[4]);
                    playerData.TotalRoundsWon = System.Convert.ToInt32(resultList[5]);
                    playerData.TotalRoundsLost = System.Convert.ToInt32(resultList[6]);
                    playerData.TotalKillCount = System.Convert.ToInt32(resultList[7]);
                    playerData.TotalAssistCount = System.Convert.ToInt32(resultList[8]);
                    playerData.TotalDeathCount = System.Convert.ToInt32(resultList[9]);
                    playerData.TotalHeadshotCount = System.Convert.ToInt32(resultList[10]);
                    //playerData.TotalMVPCount = System.Convert.ToInt32(resultList[11]);

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
            return playerData;
        }

        public static async Task<List<string>> GetPlayerMatchmakingStatsIds()
        {
            HitException = null;
            List<string> resultList = new List<string>();

            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT PlayerID, DisplayName FROM MatchmakingStats;";

                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    var rdr = await cmd.ExecuteReaderAsync();

                    while (await rdr.ReadAsync())
                    {
                        resultList.Add(rdr[0].ToString());
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
            return resultList;
        }

        public static async Task<string> GetPlayerSteamIDFromDiscordID(string discordID)
        {
            string sqlquery = $"SELECT SteamID FROM IDLink WHERE DiscordID='{discordID}';";
            return await GetStringFromDatabase(sqlquery);
        }

        public static async Task<List<string>> GetAllMapNames()
        {
            HitException = null;
            List<string> result = new List<string>();

            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT MapName FROM MapData;";

                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    var rdr = await cmd.ExecuteReaderAsync();

                    while (await rdr.ReadAsync())
                    {
                        if(!result.Contains(rdr[0].ToString()))
                        {
                            result.Add(rdr[0].ToString());
                        }
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
            return result;
        }

        public static async Task<string> GetMapIDFromName(string name)
        {
            HitException = null;
            string result = string.Empty;

            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT SteamID FROM MapData WHERE MapName='{name}';";

                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    var rdr = await cmd.ExecuteReaderAsync();

                    while (await rdr.ReadAsync())
                    {
                        result = rdr[0].ToString();
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
            return result;
        }

        public static async Task AddPlayerSteamID(string discordId, string steamId)
        {
            HitException = null;
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = con;

                    cmd.CommandText = "INSERT INTO IDLink(DiscordID, SteamID) VALUES(@discordId, @steamId);";
                    await cmd.PrepareAsync();

                    cmd.Parameters.AddWithValue("@discordId", discordId);
                    cmd.Parameters.AddWithValue("@steamId", steamId);

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

        public static async Task AddKetalQuote(int quotenumber, string quote, string footer)
        {
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = con;
                    cmd.CommandText = "INSERT INTO KetalQuotes(QuoteNumber, Quote, Footer) VALUES(@quotenum, @quote, @footer);";
                    await cmd.PrepareAsync();

                    cmd.Parameters.AddWithValue("@quotenum", quotenumber);
                    cmd.Parameters.AddWithValue("@quote", quote);
                    cmd.Parameters.AddWithValue("@footer", footer);
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
        }

        public static async Task<bool> HasMatchEnded(int id)
        {
            string sqlquery = $"SELECT end_time FROM get5_stats_matches WHERE matchid='{id}';";
            var result = await GetStringFromDatabase(sqlquery);
            return result != "";
        }

        public static async Task<List<string>> GetTeamNamesFromMatch(int matchId)
        {
            HitException = null;
            List<string> result = new List<string>();

            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT team1_name, team2_name FROM get5_stats_matches WHERE matchid='{matchId}';";

                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    var rdr = await cmd.ExecuteReaderAsync();

                    while (await rdr.ReadAsync())
                    {
                        if (!result.Contains(rdr[0].ToString()))
                        {
                            result.Add(rdr[0].ToString());
                        }
                        if (!result.Contains(rdr[1].ToString()))
                        {
                            result.Add(rdr[1].ToString());
                        }
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
            return result;
        }

        public static async Task<List<string>> GetPlayersFromMatch(int matchId, int team)
        {
            HitException = null;
            List<string> result = new List<string>();

            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT steamid64 FROM get5_stats_players WHERE (matchid='{matchId}' AND team='team{team}');";

                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    var rdr = await cmd.ExecuteReaderAsync();

                    while (await rdr.ReadAsync())
                    {
                        if(!result.Contains(rdr[0].ToString()))
                        {
                            result.Add(rdr[0].ToString());
                        }
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
            return result;
        }

        public static async Task<PlayerGameData> GetPlayerStatsFromMatch(string discordId, int matchId, string teamName)
        {
            string steamId = await GetPlayerSteamIDFromDiscordID(discordId);
            
            HitException = null;
            PlayerData playerData = new PlayerData();
            List<string> resultList = new List<string>();

            PlayerGameData gameData = new PlayerGameData();

            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT team, kills, deaths, assists, headshot_kills FROM get5_stats_players WHERE (steamid64='{steamId}' AND matchid={matchId});";

                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    var rdr = await cmd.ExecuteReaderAsync();

                    while (await rdr.ReadAsync())
                    {
                        resultList.Add(rdr[0].ToString());
                        resultList.Add(rdr[1].ToString());
                        resultList.Add(rdr[2].ToString());
                        resultList.Add(rdr[3].ToString());
                        resultList.Add(rdr[4].ToString());
                    }

                    gameData.TeamNumber = resultList[0] == "team1" ? 1 : 2;
                    gameData.KillCount = System.Convert.ToInt32(resultList[1]);
                    gameData.DeathCount = System.Convert.ToInt32(resultList[2]);
                    gameData.AssistCount = System.Convert.ToInt32(resultList[3]);
                    gameData.Headshots = System.Convert.ToInt32(resultList[4]);

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

            resultList.Clear();

            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT winner, team1_score, team2_score FROM get5_stats_maps WHERE matchid='{matchId}';";

                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    var rdr = await cmd.ExecuteReaderAsync();

                    while (await rdr.ReadAsync())
                    {
                        resultList.Add(rdr[0].ToString());
                        resultList.Add(rdr[1].ToString());
                        resultList.Add(rdr[2].ToString());
                    }

                    gameData.WonGame = gameData.TeamNumber == 1 && resultList[0] == "team1";
                    int team1Score = System.Convert.ToInt32(resultList[1]);
                    int team2Score = System.Convert.ToInt32(resultList[2]);
                    gameData.RoundsWon = gameData.TeamNumber == 1 ? team1Score : team2Score;
                    gameData.RoundsLost = gameData.TeamNumber == 1 ? team2Score : team1Score;

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

            resultList.Clear();

            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT team{gameData.TeamNumber}_name FROM get5_stats_matches WHERE matchid='{matchId}';";

                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    var rdr = await cmd.ExecuteReaderAsync();

                    while (await rdr.ReadAsync())
                    {
                        resultList.Add(rdr[0].ToString());
                    }

                    gameData.TeamName = resultList[0];

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

            return gameData;
        }

        public static async Task DeletePlayerSteamID(string discordId)
        {
            HitException = null;
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = con;

                    cmd.CommandText = $"DELETE FROM IDLink WHERE DiscordID='{discordId}';";
                    await cmd.PrepareAsync();

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

        public static async Task UpdatePlayerSteamID(string discordId, string steamId)
        {
            HitException = null;
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = con;

                    cmd.CommandText = "UPDATE IDLink SET(DiscordID, SteamID) VALUES(@discordId, @steamId);";
                    await cmd.PrepareAsync();

                    cmd.Parameters.AddWithValue("@discordId", discordId);
                    cmd.Parameters.AddWithValue("@steamId", steamId);

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

        public static async Task DeletePlayerStats(string discordId)
        {
            HitException = null;
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = con;

                    cmd.CommandText = $"DELETE FROM MatchmakingStats WHERE PlayerID='{discordId}';";
                    await cmd.PrepareAsync();

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

        public static async Task AddPlayerMatchmakingStat(PlayerData player)
        {
            HitException = null;
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = con;

                    cmd.CommandText = "INSERT INTO MatchmakingStats(DisplayName,PlayerID,CurrentELO,GamesWon,GamesLost,RoundsWon,RoundsLost,KillCount,AssistCount,DeathCount,Headshots) VALUES(@name, @id, @elo, @gamesWon, @gamesLost, @roundsWon, @roundsLost, @kill, @assist, @death, @headshots);";
                    await cmd.PrepareAsync();

                    cmd.Parameters.AddWithValue("@name", player.Name);
                    cmd.Parameters.AddWithValue("@id", player.ID);
                    cmd.Parameters.AddWithValue("@elo", player.CurrentElo);
                    cmd.Parameters.AddWithValue("@gamesWon", player.TotalGamesWon);
                    cmd.Parameters.AddWithValue("@gamesLost", player.TotalGamesLost);
                    cmd.Parameters.AddWithValue("@roundsWon", player.TotalRoundsWon);
                    cmd.Parameters.AddWithValue("@roundsLost", player.TotalRoundsLost);
                    cmd.Parameters.AddWithValue("@kill", player.TotalKillCount);
                    cmd.Parameters.AddWithValue("@assist", player.TotalAssistCount);
                    cmd.Parameters.AddWithValue("@death", player.TotalDeathCount);
                    cmd.Parameters.AddWithValue("@headshots", player.TotalHeadshotCount);
                    //cmd.Parameters.AddWithValue("@mvps", player.TotalMVPCount);

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

        public static async Task UpdatePlayerMatchmakingStat(PlayerData player)
        {
            HitException = null;
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = con;

                    cmd.CommandText = "UPDATE SET MatchmakingStats(DisplayName,PlayerID,CurrentELO,GamesWon,GamesLost,RoundsWon,RoundsLost,KillCount,AssistCount,DeathCount,Headshots) VALUES(@name, @id, @elo, @gamesWon, @gamesLost, @roundsWon, @roundsLost, @kill, @assist, @death, @headshots);";
                    await cmd.PrepareAsync();

                    cmd.Parameters.AddWithValue("@name", player.Name);
                    cmd.Parameters.AddWithValue("@id", player.ID);
                    cmd.Parameters.AddWithValue("@elo", player.CurrentElo);
                    cmd.Parameters.AddWithValue("@gamesWon", player.TotalGamesWon);
                    cmd.Parameters.AddWithValue("@gamesLost", player.TotalGamesLost);
                    cmd.Parameters.AddWithValue("@roundsWon", player.TotalRoundsWon);
                    cmd.Parameters.AddWithValue("@roundsLost", player.TotalRoundsLost);
                    cmd.Parameters.AddWithValue("@kill", player.TotalKillCount);
                    cmd.Parameters.AddWithValue("@assist", player.TotalAssistCount);
                    cmd.Parameters.AddWithValue("@death", player.TotalDeathCount);
                    cmd.Parameters.AddWithValue("@headshots", player.TotalHeadshotCount);
                    //cmd.Parameters.AddWithValue("@mvps", player.TotalMVPCount);

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

        public static async Task<int> GetLastMatchID()
        {
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT MAX(matchid) FROM get5_stats_matches;";
                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    DataTable temp = new DataTable();
                    var adapter = new MySqlDataAdapter(cmd);
                    await adapter.FillAsync(temp);
                    object lastmatchidobject = temp.Rows[0].ItemArray[0];
                    int lastmatchid = (int)(uint)lastmatchidobject;
                    await con.CloseAsync();
                    return lastmatchid;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Something happened getting last match id: {e.Message}.");
                    return -1;
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

        public static async Task<List<KetalQuote>> GetKetalQuotes()
        {
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                List<KetalQuote> quoteObjects = null;
                try
                {
                    await con.OpenAsync();
                    string sqlquery = $"SELECT QuoteNumber, Quote, Footer FROM KetalQuotes;";

                    MySqlCommand cmd = new MySqlCommand(sqlquery, con);

                    DataTable temp = new DataTable();
                    var adapter = new MySqlDataAdapter(cmd);
                    await adapter.FillAsync(temp);
                    quoteObjects = new List<KetalQuote>();
                    foreach (DataRow row in temp.Rows)
                    {
                        var idstring = row.ItemArray[0];
                        int quotenumvar = (int)idstring;
                        string quotevar = (string)row.ItemArray[1];
                        string footervar = (string)row.ItemArray[2];
                        var myquote = new KetalQuote { QuoteNumber = quotenumvar, Quote = quotevar, Footer = footervar };
                        quoteObjects.Add(myquote);
                    }
                    return quoteObjects;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Something happened getting ketal quotes: {e}.");
                    return quoteObjects;
                }
            }
        }


        private static async Task<string> GetStringFromDatabase(string query)
        {
            HitException = null;
            string result = string.Empty;
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(query, con);
                    var rdr = await cmd.ExecuteReaderAsync();
                    while (await rdr.ReadAsync())
                    {
                        result = rdr[0].ToString();
                    }
                    await rdr.CloseAsync();
                }
                catch (Exception ex)
                {
                    HitException = ex;
                    Console.WriteLine("Hit exception in GetStringFromDatabase: " + ex.Message);
                }
                finally
                {
                    await con.CloseAsync();
                }
            }
            return result;
        }
    }
}
