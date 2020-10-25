using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Crmf;
using Org.BouncyCastle.Bcpg;
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
            var dbresult = await GetDataRowCollection($"SELECT Message FROM UserMessages WHERE UserID='{userID}';");
            List<string> messages = new List<string>();
            foreach(DataRow message in dbresult)
            {
                messages.Add((string)message.ItemArray[0]);
            }
            return messages;
        }

        public static async Task<PlayerData> GetPlayerMatchmakingStats(string playerId)
        {
            var dbresult = await GetDataRowCollection($"SELECT DisplayName, PlayerID, CurrentELO, GamesWon, GamesLost, RoundsWon, RoundsLost, KillCount, AssistCount, DeathCount, Headshots, MVPCount FROM MatchmakingStats WHERE PlayerID='{playerId}';");
            return new PlayerData
            {
                Name = (string)dbresult[0].ItemArray[0],
                ID = (string)dbresult[0].ItemArray[1],
                CurrentElo = float.Parse((string)dbresult[0].ItemArray[2]),
                TotalGamesWon = int.Parse((string)dbresult[0].ItemArray[3]),
                TotalGamesLost = int.Parse((string)dbresult[0].ItemArray[4]),
                TotalRoundsWon = int.Parse((string)dbresult[0].ItemArray[5]),
                TotalRoundsLost = int.Parse((string)dbresult[0].ItemArray[6]),
                TotalKillCount = int.Parse((string)dbresult[0].ItemArray[7]),
                TotalAssistCount = int.Parse((string)dbresult[0].ItemArray[8]),
                TotalDeathCount = int.Parse((string)dbresult[0].ItemArray[9]),
                TotalHeadshotCount = int.Parse((string)dbresult[0].ItemArray[10])
            };
        }

        public static async Task<List<string>> GetPlayerMatchmakingStatsIds()
        {
            var dbresult = await GetDataRowCollection($"SELECT PlayerID, DisplayName FROM MatchmakingStats;");
            List<string> resultList = new List<string>();
            foreach(DataRow item in dbresult)
            {
                resultList.Add((string)item.ItemArray[0]);
            }
            return resultList;
        }

        public static async Task<string> GetPlayerSteamIDFromDiscordID(string discordID)
        {
            var dbresult = await GetDataRowCollection($"SELECT SteamID FROM IDLink WHERE DiscordID='{discordID}';");
            return (string)dbresult[0].ItemArray[0];
        }

        public static async Task<List<string>> GetAllMapNames()
        {
            var dbresult = await GetDataRowCollection($"SELECT MapName FROM MapData;");
            List<string> results = new List<string>();
            foreach(DataRow item in dbresult)
            {
                results.Add((string)item.ItemArray[0]);
            }
            return results;
        }

        public static async Task<string> GetMapIDFromName(string name)
        {
            var dbresult = await GetDataRowCollection($"SELECT SteamID FROM MapData WHERE MapName='{name}';");
            return (string)dbresult[0].ItemArray[0];
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

        public static async Task<bool> HasMatchEnded(int id)
        {
            var dbresult = await GetDataRowCollection($"SELECT end_time FROM get5_stats_matches WHERE matchid='{id}';");
            var result = (string)dbresult[0].ItemArray[0];
            return result != "";
        }

        public static async Task<List<string>> GetTeamNamesFromMatch(int matchId)
        {
            var dbresult = await GetDataRowCollection($"SELECT team1_name, team2_name FROM get5_stats_matches WHERE matchid='{matchId}';");
            List<string> result = new List<string>();
            result.Add((string)dbresult[0].ItemArray[0]);
            result.Add((string)dbresult[0].ItemArray[1]);
            return result;
        }

        public static async Task<long> GetPlayerSquidCoin(string discordId)
        {
            var dbresult = await GetDataRowCollection($"SELECT Coins FROM SquidCoinStats WHERE PlayerID='{discordId}';");
            string prelimresult = (string)dbresult[0].ItemArray[0];
            if(prelimresult == string.Empty)
            {
                return 0;
            }
            return long.Parse(prelimresult);
        }

        public static async Task<List<string>> GetPlayerSquidIds()
        {
            var dbresult = await GetDataRowCollection($"SELECT PlayerID FROM SquidCoinStats;");
            List<string> idList = new List<string>();
            foreach(DataRow item in dbresult)
            {
                idList.Add((string)item.ItemArray[0]);
            }
            return idList;
        }


        public static async Task AddSquidCoinPlayer(string discordId, long coin)
        {
            HitException = null;
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = con;

                    cmd.CommandText = "INSERT INTO SquidCoinStats(PlayerID, Coins) VALUES(@discordId, @coin);";
                    await cmd.PrepareAsync();

                    cmd.Parameters.AddWithValue("@discordId", discordId);
                    cmd.Parameters.AddWithValue("@coin", coin);

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

        public static async Task DeleteSquidCoinPlayer(string discordId)
        {
            HitException = null;
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = con;

                    cmd.CommandText = $"DELETE FROM SquidCoinStats WHERE PlayerID='{discordId}';";
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

        public static async Task<List<string>> GetPlayersFromMatch(int matchId, int team)
        {
            var dbresult = await GetDataRowCollection($"SELECT steamid64 FROM get5_stats_players WHERE (matchid='{matchId}' AND team='team{team}');");
            List<string> resultList = new List<string>();
            foreach(DataRow item in dbresult)
            {
                resultList.Add((string)item.ItemArray[0]);
            }
            return resultList;
        }

        public static async Task<PlayerGameData> GetPlayerStatsFromMatch(string discordId, int matchId, string teamName)
        {
            string steamId = await GetPlayerSteamIDFromDiscordID(discordId);

            var dbresult = await GetDataRowCollection($"SELECT team, kills, deaths, assists, headshot_kills FROM get5_stats_players WHERE (steamid64='{steamId}' AND matchid={matchId});");
            PlayerGameData gameData = new PlayerGameData();
            gameData.TeamNumber = (string)dbresult[0].ItemArray[0] == "team1" ? 1 : 2;
            gameData.KillCount = int.Parse((string)dbresult[0].ItemArray[1]);
            gameData.DeathCount = int.Parse((string)dbresult[0].ItemArray[2]);
            gameData.AssistCount = int.Parse((string)dbresult[0].ItemArray[3]);
            gameData.Headshots = int.Parse((string)dbresult[0].ItemArray[4]);

            var dbresult2 = await GetDataRowCollection($"SELECT winner, team1_score, team2_score FROM get5_stats_maps WHERE matchid='{matchId}';");
            gameData.WonGame = (string)dbresult2[0].ItemArray[0] == "none" ? true : gameData.TeamNumber == 1 && (string)dbresult2[0].ItemArray[0] == "team1";
            int team1Score = int.Parse((string)dbresult2[0].ItemArray[1]);
            int team2Score = int.Parse((string)dbresult2[0].ItemArray[2]);
            gameData.RoundsWon = gameData.TeamNumber == 1 ? team1Score : team2Score;
            gameData.RoundsLost = gameData.TeamNumber == 1 ? team2Score : team1Score;

            var dbresult3 = await GetDataRowCollection($"SELECT team{gameData.TeamNumber}_name FROM get5_stats_matches WHERE matchid='{matchId}';");
            gameData.TeamName = (string)dbresult3[0].ItemArray[0];

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
            var dbresult = await GetDataRowCollection($"SELECT Message FROM UserMessages WHERE UserId != '{565566309257969668}';");
            List<string> messageList = new List<string>();
            foreach(DataRow item in dbresult)
            {
                messageList.Add((string)item.ItemArray[0]);
            }
            return messageList;
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
            var dbresult = await GetDataRowCollection($"SELECT Id, ServerId, Description, Address, RconPassword, FtpUser, FtpPassword, FtpPath, FtpType, Game FROM Servers;");
            List<Server> foundServers = new List<Server>();
            foreach(DataRow item in dbresult)
            {
                var idstring = int.Parse((string)item.ItemArray[0]);
                foundServers.Add(new Server
                {
                    Id = int.Parse((string)item.ItemArray[0]),
                    ServerId = (string)item.ItemArray[1],
                    Description = (string)item.ItemArray[2],
                    Address = (string)item.ItemArray[3],
                    RconPassword = (string)item.ItemArray[4],
                    FtpUser = (string)item.ItemArray[5],
                    FtpPassword = (string)item.ItemArray[6],
                    FtpPath = (string)item.ItemArray[7],
                    FtpType = (string)item.ItemArray[8],
                    Game = (string)item.ItemArray[9]
                });
            }
            return foundServers;
        }

        public static async Task<int> GetLastMatchID()
        {
            var dbresult = await GetDataRowCollection("SELECT MAX(matchid) FROM get5_stats_matches;");
            return (int)(uint)dbresult[0].ItemArray[0];
        }

        public static async Task<Server> GetTestServerInfo(string serverID)
        {
            if(serverID == null)
            {
                return null;
            }

            serverID = GeneralUtil.GetServerCode(serverID);

            // This should ideally be using the serverID var but whatever
            var dbresult = await GetDataRowCollection("SELECT Id, ServerId, Description, Address, RconPassword, FtpUser, FtpPassword, FtpPath, FtpType, Game FROM Servers WHERE ServerId = 'sc1';");
            var idstring = dbresult[0].ItemArray[0];
            int idvar = (int)idstring;
            return new Server
            {
                Id = idvar,
                ServerId = (string)dbresult[0].ItemArray[1],
                Description = (string)dbresult[0].ItemArray[2],
                Address = (string)dbresult[0].ItemArray[3],
                RconPassword = (string)dbresult[0].ItemArray[4],
                FtpUser = (string)dbresult[0].ItemArray[5],
                FtpPassword = (string)dbresult[0].ItemArray[6],
                FtpPath = (string)dbresult[0].ItemArray[7],
                FtpType = (string)dbresult[0].ItemArray[8],
                Game = (string)dbresult[0].ItemArray[9]
            };
        }

        // ---------------------------------------------------------------------------------------------------------
        // -------------------------------------KETAL QUOTES--------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------

        public static async Task<List<KetalQuote>> GetKetalQuotes()
        {
            var dbresult = await GetDataRowCollection("SELECT QuoteNumber, Quote, Footer FROM KetalQuotes;");
            List<KetalQuote> quoteObjects = new List<KetalQuote>();
            foreach (DataRow row in dbresult)
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

        // ---------------------------------------------------------------------------------------------------------
        // -------------------------------------Time Zone Information-----------------------------------------------
        // ---------------------------------------------------------------------------------------------------------
        public static async Task AddTimeZoneData(string userid, string jsonstring)
        {
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = con;
                    cmd.CommandText = "INSERT INTO UserTimeZones(UserID, TimeZoneInfo) VALUES(@var1, @var2);";
                    await cmd.PrepareAsync();

                    cmd.Parameters.AddWithValue("@var1", userid);
                    cmd.Parameters.AddWithValue("@var2", jsonstring);
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

        public static async Task<Dictionary<string, string>> CheckGuildMembersHaveTimeZoneData(IReadOnlyDictionary<ulong, DiscordMember> memberdict)
        {
            var dbresult = await GetDataRowCollection("SELECT * FROM UserTimeZones;");
            var returndict = new Dictionary<string, string>();
            foreach (DataRow item in dbresult)
            {
                returndict.Add((string)item.ItemArray[0], (string)item.ItemArray[1]);
            }
            return returndict;
        }


        // ---------------------------------------------------------------------------------------------------------
        // ----------------------------------GetDataRowList---------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------

        private static async Task<DataRowCollection> GetDataRowCollection(string query)
        {
            HitException = null;
            string result = string.Empty;
            DataRowCollection rows = null;
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand(query, con);

                    DataTable mytable = new DataTable();
                    var adapter = new MySqlDataAdapter(cmd);
                    await adapter.FillAsync(mytable);
                    rows = mytable.Rows;
                    await con.CloseAsync();
                    return rows;
                }
                catch (Exception e)
                {
                    HitException = e;
                    Console.WriteLine($"Something happened getting stuff from the database {e}.");
                    return rows;
                }
            }
        }
    }
}
