using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
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

        public static async Task<int> MapToggle(string mapname)
        {
            var maplist = await GetAllMapNames();
            if(!GeneralUtil.ListContainsCaseInsensitive(maplist, mapname))
            {
                return -1;
            }
            var dbresult = await GetDataRowCollection($"SELECT Enabled FROM MapData WHERE MapName='{mapname}'");
            if (ExtractRowInfo<byte>(dbresult[0], 0) == 0)
            {
                var payload = new Dictionary<string, object>
                {
                    {"@var1", 1}
                };
                await DBExecuteNonQuery($"UPDATE MapData SET enabled = @var1 WHERE MapName='{mapname}'", payload);
                return 0;
            }
            else
            {
                var payload = new Dictionary<string, object>
                {
                    {"@var1", 0}
                };
                await DBExecuteNonQuery($"UPDATE MapData SET enabled = @var1 WHERE MapName='{mapname}'", payload);
                return 1;
            }
        }

        public static async Task<List<string>> GetUserMessages(ulong userID)
        {
            var dbresult = await GetDataRowCollection($"SELECT Message FROM UserMessages WHERE UserID='{userID}';");
            List<string> messages = new List<string>();
            foreach(DataRow message in dbresult)
            {
                messages.Add(ExtractRowInfo<string>(message, 0));
            }
            return messages;
        }

        public static async Task<PlayerData> GetPlayerMatchmakingStats(string playerId)
        {
            var dbresult = await GetDataRowCollection($"SELECT DisplayName, PlayerID, CurrentELO, GamesWon, GamesLost, RoundsWon, RoundsLost, KillCount, AssistCount, DeathCount, Headshots, MVPCount FROM MatchmakingStats WHERE PlayerID='{playerId}';");
            if(dbresult.Count == 0)
            {
                return new PlayerData() { ID = null };
            }
            return new PlayerData
            {
                Name = ExtractRowInfo<string>(dbresult[0], 0),
                ID = ExtractRowInfo<string>(dbresult[0], 1),
                CurrentElo = ExtractRowInfo<float>(dbresult[0], 2),
                TotalGamesWon = ExtractRowInfo<uint>(dbresult[0], 3),
                TotalGamesLost = ExtractRowInfo<uint>(dbresult[0], 4),
                TotalRoundsWon = ExtractRowInfo<ulong>(dbresult[0], 5),
                TotalRoundsLost = ExtractRowInfo<ulong>(dbresult[0], 6),
                TotalKillCount = ExtractRowInfo<ulong>(dbresult[0], 7),
                TotalAssistCount = ExtractRowInfo<ulong>(dbresult[0], 8),
                TotalDeathCount = ExtractRowInfo<ulong>(dbresult[0], 9),
                TotalHeadshotCount = ExtractRowInfo<ulong>(dbresult[0], 10)
            };
        }

        public static async Task<List<string>> GetPlayerMatchmakingStatsIds()
        {
            var dbresult = await GetDataRowCollection($"SELECT PlayerID, DisplayName FROM MatchmakingStats;");
            List<string> resultList = new List<string>();
            foreach(DataRow item in dbresult)
            {
                resultList.Add(ExtractRowInfo<string>(item, 0));
            }
            return resultList;
        }

        public static async Task<string> GetPlayerSteamIDFromDiscordID(string discordID)
        {
            var dbresult = await GetDataRowCollection($"SELECT SteamID FROM IDLink WHERE DiscordID='{discordID}';");
            if(dbresult.Count == 0)
            {
                return string.Empty;
            }
            return ExtractRowInfo<string>(dbresult[0], 0);
        }

        public static async Task<List<string>> GetAllMapNames(bool getdisabled = false)
        {
            DataRowCollection dbresult;
            if(!getdisabled)
            {
                dbresult = await GetDataRowCollection($"SELECT MapName FROM MapData WHERE Enabled=1;");
            }
            else
            {
                dbresult = await GetDataRowCollection($"SELECT MapName FROM MapData;");
            }
            List<string> results = new List<string>();
            foreach(DataRow item in dbresult)
            {
                results.Add(ExtractRowInfo<string>(item, 0));
            }
            return results;
        }

        public static async Task<string> GetMapIDFromName(string name)
        {
            var dbresult = await GetDataRowCollection($"SELECT SteamID FROM MapData WHERE MapName='{name}';");
            if (dbresult.Count == 0)
            {
                return string.Empty;
            }
            return ExtractRowInfo<string>(dbresult[0], 0);
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
            #nullable enable
            DateTime? dtresult = dbresult.Count > 0 ? ExtractRowInfo<DateTime>(dbresult[0], 0) : default;
            string? result = dtresult.ToString();
            #nullable disable
            if(result == default || result == "1/1/0001 12:00:00 AM")
            {
                return false;
            }
            return true;
        }

        public static async Task<List<string>> GetTeamNamesFromMatch(int matchId)
        {
            var dbresult = await GetDataRowCollection($"SELECT team1_name, team2_name FROM get5_stats_matches WHERE matchid='{matchId}';");
            List<string> result = new List<string>();
            result.Add(ExtractRowInfo<string>(dbresult[0], 0));
            result.Add(ExtractRowInfo<string>(dbresult[0], 1));
            return result;
        }

        public static async Task<long> GetPlayerSquidCoin(string discordId)
        {
            var dbresult = await GetDataRowCollection($"SELECT Coins FROM SquidCoinStats WHERE PlayerID='{discordId}';");
            if(dbresult.Count == 0)
            {
                return 0;
            }
            long? longresult = ExtractRowInfo<long?>(dbresult[0], 0); 
            return (long)longresult;
        }

        public static async Task<List<string>> GetPlayerSquidIds()
        {
            var dbresult = await GetDataRowCollection($"SELECT PlayerID FROM SquidCoinStats;");
            List<string> idList = new List<string>();
            foreach(DataRow item in dbresult)
            {
                idList.Add(ExtractRowInfo<string>(item, 0));
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

        public static async Task AddBet(string discordId, string playerName, string betOnId, long amount, long matchId, bool won)
        {
            var payload = new Dictionary<string, object>
            {
                {"@discordId", discordId },
                {"@playerName", playerName },
                {"@betOnId", betOnId },
                {"@betAmount", amount },
                {"@matchId", matchId },
                {"@won", won ? (byte)1 : (byte)0 },
            };
            await DBExecuteNonQuery("INSERT INTO BetStats(PlayerID, PlayerName, BetOnID, BetAmount, MatchID, WonBet) VALUES(@discordId, @playerName, @betOnId, @betAmount, @matchId, @won);", payload);
        }

        public static async Task<Dictionary<string, MatchmakingModule.BetData>> GetBetsFromMatch(long matchId)
        {
            var dbresult = await GetDataRowCollection($"SELECT PlayerID, BetOnID, BetAmount, PlayerName FROM BetStats WHERE MatchID='{matchId}';");
            Dictionary<string, MatchmakingModule.BetData> betData = new Dictionary<string, MatchmakingModule.BetData>();
            foreach (DataRow item in dbresult)
            {
                betData.Add(ExtractRowInfo<string>(item, 0),
                    new MatchmakingModule.BetData()
                    {
                        UserToBetOn = ExtractRowInfo<string>(item, 1),
                        BetAmount = ExtractRowInfo<long>(item, 2),
                        Name = ExtractRowInfo<string>(item, 3)
                    });
            }
            return betData;
        }

        public static async Task AddSpectator(string discordId, long matchId)
        {
            var payload = new Dictionary<string, object>
            {
                {"@discordId", discordId },
                {"@matchId", matchId },
            };
            await DBExecuteNonQuery("INSERT INTO MatchSpectatorStats(PlayerID, MatchID) VALUES(@discordId, @matchId);", payload);
        }

        public static async Task<List<string>> GetSpectatorsFromMatch(long matchId)
        {
            var dbresult = await GetDataRowCollection($"SELECT PlayerID FROM MatchSpectatorStats WHERE MatchID='{matchId}';");
            List<string> idList = new List<string>();
            foreach (DataRow item in dbresult)
            {
                idList.Add(ExtractRowInfo<string>(item, 0));
            }
            return idList;
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
                resultList.Add(ExtractRowInfo<string>(item, 0));
            }
            return resultList;
        }

        public static async Task<PlayerGameData> GetPlayerStatsFromMatch(string discordId, int matchId, string teamName)
        {
            string steamId = await GetPlayerSteamIDFromDiscordID(discordId);

            var dbresult = await GetDataRowCollection($"SELECT team, kills, deaths, assists, headshot_kills FROM get5_stats_players WHERE (steamid64='{steamId}' AND matchid={matchId});");
            PlayerGameData gameData = new PlayerGameData();
            gameData.TeamNumber = ExtractRowInfo<string>(dbresult[0], 0) == "team1" ? (uint)1 : (uint)2;
            gameData.KillCount = ExtractRowInfo<ushort>(dbresult[0], 1);
            gameData.DeathCount = ExtractRowInfo<ushort>(dbresult[0], 2);
            gameData.AssistCount = ExtractRowInfo<ushort>(dbresult[0], 3);
            gameData.Headshots = ExtractRowInfo<ushort>(dbresult[0], 4);

            var dbresult2 = await GetDataRowCollection($"SELECT winner, team1_score, team2_score FROM get5_stats_maps WHERE matchid='{matchId}';");
            gameData.WonGame = ExtractRowInfo<string>(dbresult2[0], 0) == "none" ? true : gameData.TeamNumber == 1 && ExtractRowInfo<string>(dbresult2[0], 0) == "team1";
            uint team1Score = ExtractRowInfo<ushort>(dbresult2[0], 1);
            uint team2Score = ExtractRowInfo<ushort>(dbresult2[0], 2);
            gameData.RoundsWon = gameData.TeamNumber == 1 ? team1Score : team2Score;
            gameData.RoundsLost = gameData.TeamNumber == 1 ? team2Score : team1Score;

            var dbresult3 = await GetDataRowCollection($"SELECT team{gameData.TeamNumber}_name FROM get5_stats_matches WHERE matchid='{matchId}';");
            gameData.TeamName = ExtractRowInfo<string>(dbresult3[0], 0);

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
                foundServers.Add(new Server
                {
                    Id = ExtractRowInfo<int>(dbresult[0], 0),
                    ServerId = ExtractRowInfo<string>(dbresult[0], 1),
                    Description = ExtractRowInfo<string>(dbresult[0], 2),
                    Address = ExtractRowInfo<string>(dbresult[0], 3),
                    RconPassword = ExtractRowInfo<string>(dbresult[0], 4),
                    FtpUser = ExtractRowInfo<string>(dbresult[0], 5),
                    FtpPassword = ExtractRowInfo<string>(dbresult[0], 6),
                    FtpPath = ExtractRowInfo<string>(dbresult[0], 7),
                    FtpType = ExtractRowInfo<string>(dbresult[0], 8),
                    Game = ExtractRowInfo<string>(dbresult[0], 9)
                });
            }
            return foundServers;
        }

        public static async Task<int> GetLastMatchID()
        {
            var dbresult = await GetDataRowCollection("SELECT MAX(matchid) FROM get5_stats_matches;");
            return (int)(ExtractRowInfo<uint>(dbresult[0], 0));
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
            return new Server
            {
                Id = ExtractRowInfo<int>(dbresult[0], 0),
                ServerId = ExtractRowInfo<string>(dbresult[0], 1),
                Description = ExtractRowInfo<string>(dbresult[0], 2),
                Address = ExtractRowInfo<string>(dbresult[0], 3),
                RconPassword = ExtractRowInfo<string>(dbresult[0], 4),
                FtpUser = ExtractRowInfo<string>(dbresult[0], 5),
                FtpPassword = ExtractRowInfo<string>(dbresult[0], 6),
                FtpPath = ExtractRowInfo<string>(dbresult[0], 7),
                FtpType = ExtractRowInfo<string>(dbresult[0], 8),
                Game = ExtractRowInfo<string>(dbresult[0], 9)
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
                int quotenumvar = ExtractRowInfo<int>(row, 0);
                string quotevar = ExtractRowInfo<string>(row, 1);
                string footervar = ExtractRowInfo<string>(row, 2);
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
                returndict.Add(ExtractRowInfo<string>(item, 0), ExtractRowInfo<string>(item, 1));
            }
            return returndict;
        }


        // ---------------------------------------------------------------------------------------------------------
        // ----------------------------------Bottom Level Methods---------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------

        private static T ExtractRowInfo<T>(DataRow row, int colNum)
        {
           if(row.ItemArray.Length <= colNum)
            {
                return default(T);
            }

            if (row.ItemArray[colNum] is DBNull)
            {
                return default(T);
            }

            object returnobject = (T)(row.ItemArray[colNum]);

            if(!(returnobject is DBNull))
            {
                return (T)returnobject;
            }
            return default(T);
        }

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
