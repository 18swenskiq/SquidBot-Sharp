using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;
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
            var payload = new Dictionary<string, object>
            {
                {"@discordId", discordId },
                {"@steamId", steamId }
            };
            await DBExecuteNonQuery("INSERT INTO IDLink(DiscordID, SteamID) VALUES(@discordId, @steamId);", payload);
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
            var payload = new Dictionary<string, object>
            {
                {"@discordId", discordId },
                {"@coin", coin }
            };
            await DBExecuteNonQuery("INSERT INTO SquidCoinStats(PlayerID, Coins) VALUES(@discordId, @coin);", payload);
        }

        public static async Task DeleteSquidCoinPlayer(string discordId)
        {
            await DBExecuteNonQuery($"DELETE FROM SquidCoinStats WHERE PlayerID='{discordId}';", null);
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
            await DBExecuteNonQuery($"DELETE FROM IDLink WHERE DiscordID='{discordId}';", null);
        }

        public static async Task UpdatePlayerSteamID(string discordId, string steamId)
        {
            var payload = new Dictionary<string, object>
            {
                {"@discordId", discordId },
                {"@steamId", steamId }
            };
            await DBExecuteNonQuery("UPDATE IDLink SET(DiscordID, SteamID) VALUES(@discordId, @steamId);", payload);
        }

        public static async Task DeletePlayerStats(string discordId)
        {
            await DBExecuteNonQuery($"DELETE FROM MatchmakingStats WHERE PlayerID='{discordId}';", null);
        }

        public static async Task AddPlayerMatchmakingStat(PlayerData player)
        {
            var payload = new Dictionary<string, object>
            {
                {"@name", player.Name },
                {"@id", player.ID },
                {"@elo", player.CurrentElo },
                {"@gamesWon", player.TotalGamesWon },
                {"@gamesLost", player.TotalGamesLost },
                {"@roundsWon", player.TotalRoundsWon },
                {"@roundsLost", player.TotalRoundsLost },
                {"@kill", player.TotalKillCount },
                {"@assist", player.TotalAssistCount },
                {"@death", player.TotalDeathCount },
                {"@headshots", player.TotalHeadshotCount }
            };
            await DBExecuteNonQuery("INSERT INTO MatchmakingStats(DisplayName,PlayerID,CurrentELO,GamesWon,GamesLost,RoundsWon,RoundsLost,KillCount,AssistCount,DeathCount,Headshots) VALUES(@name, @id, @elo, @gamesWon, @gamesLost, @roundsWon, @roundsLost, @kill, @assist, @death, @headshots);", payload);
        }

        public static async Task UpdatePlayerMatchmakingStat(PlayerData player)
        {
            var payload = new Dictionary<string, object>
            {
                {"@name", player.Name },
                {"@id", player.ID },
                {"@elo", player.CurrentElo },
                {"@gamesWon", player.TotalGamesWon },
                {"@gamesLost", player.TotalGamesLost },
                {"@roundsWon", player.TotalRoundsWon },
                {"@roundsLost", player.TotalRoundsLost },
                {"@kill", player.TotalKillCount },
                {"@assist", player.TotalAssistCount },
                {"@death", player.TotalDeathCount },
                {"@headshots", player.TotalHeadshotCount }
            };
            await DBExecuteNonQuery("UPDATE SET MatchmakingStats(DisplayName,PlayerID,CurrentELO,GamesWon,GamesLost,RoundsWon,RoundsLost,KillCount,AssistCount,DeathCount,Headshots) VALUES(@name, @id, @elo, @gamesWon, @gamesLost, @roundsWon, @roundsLost, @kill, @assist, @death, @headshots);", payload);
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
            var payload = new Dictionary<string, object>
            {
                {"@number", userID},
                {"@text", message }
            };
            await DBExecuteNonQuery("INSERT INTO UserMessages(UserID,Message) VALUES(@number, @text);", payload);
        }

        public static async Task AddTestServer(Server server)
        {
            if (GetTestServerInfo(server.ServerId) != null)
            {
                Console.WriteLine("DB: Unable to add test server since one was found.");
                return;
            }
            var payload = new Dictionary<string, object>
            {
                {"@val1", server.Id },
                {"@val2", server.ServerId},
                {"@val3", server.Description},
                {"@val4", server.Address},
                {"@val5", server.RconPassword},
                {"@val6", server.FtpUser },
                {"@val7", server.FtpPassword },
                {"@val8", server.FtpPath },
                {"@val9", server.FtpType },
                {"@val10", server.Game }
            };
            await DBExecuteNonQuery("INSERT INTO Servers(Id, ServerId, Description, Address, RconPassword, FtpUser, FtpPassword, FtpPath, FtpType, Game) VALUES(@val1, @val2, @val3, @val4, @val5, @val6, @val7, @val8, @val9, @val10);", payload);
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
            var payload = new Dictionary<string, object>
            {
                {"@quotenum", quotenumber },
                {"@quote", quote},
                {"@footer", footer}
            };
            await DBExecuteNonQuery("INSERT INTO KetalQuotes(QuoteNumber, Quote, Footer) VALUES(@quotenum, @quote, @footer);", payload);
        }

        // ---------------------------------------------------------------------------------------------------------
        // -------------------------------------Time Zone Information-----------------------------------------------
        // ---------------------------------------------------------------------------------------------------------
        public static async Task AddTimeZoneData(string userid, string jsonstring)
        {
            var payload = new Dictionary<string, object>
            {
                {"@var1", userid},
                {"@var2", jsonstring}
            };
            await DBExecuteNonQuery("INSERT INTO UserTimeZones(UserID, TimeZoneInfo) VALUES(@var1, @var2);", payload);
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
        // ----------------------------------Bottom Level Methods---------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------

        private static async Task DBExecuteNonQuery(string nonquery, Dictionary<string, object> vardict)
        {
            // Add option for delete statements
            HitException = null;
            using (MySqlConnection con = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await con.OpenAsync();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = con;
                    cmd.CommandText = nonquery;
                    await cmd.PrepareAsync();

                    // If vardict is null, we don't need to add any parameters so we can just skip it
                    if (vardict == null) goto SkipDict;
                    foreach(var member in vardict)
                    {
                        cmd.Parameters.AddWithValue(member.Key, member.Value);
                    }
                    SkipDict:
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception e)
                {
                    HitException = e;
                }
                finally
                {
                    await con.CloseAsync();
                }
            }
        }

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
                    await con.CloseAsync();
                    return rows;
                }
            }
        }
    }
}
