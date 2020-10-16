using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using FluentFTP;
using Google.Protobuf;
using Newtonsoft.Json;
using Renci.SshNet;
using SquidBot_Sharp.Commands;
using SquidBot_Sharp.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Modules
{
    public static class MatchmakingModule
    {
        private static string[] numbersWritten = new string[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
        private const string randomizeMapEmoji = ":game_die:";
        private const string RANDOMIZE_RESULT = "RANDOMIZE";
        private const string ABORT_RESULT = "ABORT";
        private const int MAP_COUNT = 7;
        private const int NEEDED_FOR_RANDOMIZE = 3;
        private const int SECONDS_IN_MINUTE = 60;
        private const int SECONDS_UNTIL_TIMEOUT = SECONDS_IN_MINUTE * 5;
        private const int FREQUENCY_TO_CHECK_FOR_POSTGAME = 15;

        public static List<DiscordMember> PlayersInQueue = new List<DiscordMember>();
        public static List<string> CurrentSpectatorIds = new List<string>();
        public static DiscordMessage PreviousMessage = null;
        public static bool CanJoinQueue = true;
        public static bool Queueing = false;
        public static bool MatchPlaying = false;
        public static bool CaptainPick = false;
        private static bool SelectingMap = false;
        public static bool WasReset = false;
        private static Dictionary<DiscordMember, PlayerData> discordPlayerToGamePlayer = new Dictionary<DiscordMember, PlayerData>();
        private static Dictionary<PlayerData, DiscordMember> gamePlayerToDiscordPlayer = new Dictionary<PlayerData, DiscordMember>();



        public static async Task TimeOut(CommandContext ctx)
        {
            await Task.Delay(1000 * SECONDS_UNTIL_TIMEOUT);

            if(!MatchPlaying && !WasReset)
            {
                await Reset();
                await ctx.RespondAsync("CS:GO session queue timed out after " + SECONDS_UNTIL_TIMEOUT + " seconds with no joins");
            }
        }

        public static async Task Reset()
        {
            WasReset = true;
            SelectingMap = false;
            MatchPlaying = false;
            CanJoinQueue = true;
            Queueing = false;
            PlayersInQueue.Clear();
            discordPlayerToGamePlayer.Clear();
            gamePlayerToDiscordPlayer.Clear();
            CurrentSpectatorIds.Clear();


            if (PreviousMessage != null)
            {
                await PreviousMessage.DeleteAsync();
            }

            PreviousMessage = null;
        }

        public static async Task<bool> DoesPlayerHaveSteamIDRegistered(CommandContext ctx, DiscordMember member)
        {
            string id = await DatabaseModule.GetPlayerSteamIDFromDiscordID(member.Id.ToString());

            return id != string.Empty;
        }

        public static async Task<bool> JoinQueue(CommandContext ctx, DiscordMember member)
        {
            if(MatchPlaying)
            {
                return true;
            }

            if (!PlayersInQueue.Contains(member))
            {
                PlayersInQueue.Add(member);
            }

            var player = await DatabaseModule.GetPlayerMatchmakingStats(member.Id.ToString());

            if(player.ID == null)
            {
                //Create new entry
                player = new PlayerData()
                {
                    ID = member.Id.ToString(),
                    Name = member.DisplayName,
                    CurrentElo = 1000
                };

                await DatabaseModule.AddPlayerMatchmakingStat(player);
            }

            if(!discordPlayerToGamePlayer.ContainsKey(member))
            {
                discordPlayerToGamePlayer.Add(member, player);
            }
            discordPlayerToGamePlayer[member] = player;

            if(!gamePlayerToDiscordPlayer.ContainsKey(player))
            {
                gamePlayerToDiscordPlayer.Add(player, member);
            }
            gamePlayerToDiscordPlayer[player] = member;

            await UpdatePlayList(ctx);

            return PlayersInQueue.Count >= 4;
        }
        public static async Task LeaveQueue(CommandContext ctx, DiscordMember member)
        {
            if (MatchPlaying)
            {
                return;
            }
            
            if(PlayersInQueue.Contains(member))
            {
                var player = await DatabaseModule.GetPlayerMatchmakingStats(member.Id.ToString());

                if(player.Name != member.DisplayName)
                {
                    //Update player name from nickname
                    player.Name = member.DisplayName;
                    await DatabaseModule.DeletePlayerStats(player.ID);
                    await DatabaseModule.AddPlayerMatchmakingStat(player);
                }

                PlayersInQueue.Remove(member);
                discordPlayerToGamePlayer.Remove(member);
                gamePlayerToDiscordPlayer.Remove(player);

                await UpdatePlayList(ctx);
            }
            else
            {
                return;
            }
        }

        public static bool IsPlayerHost(DiscordMember member)
        {
            if (PlayersInQueue.Contains(member))
            {
                return PlayersInQueue[0] == member;
            }
            else
            {
                return false;
            }
        }

        public static async Task UpdatePlayList(CommandContext ctx)
        {
            bool readyToStart = PlayersInQueue.Count >= 4;
            string playersNeededText = "(" + (4 - PlayersInQueue.Count) + " Players Required)";
            if(readyToStart)
            {
                playersNeededText = "(Match Ready)";
            }
            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = "CS:GO Match Queue " + playersNeededText,
                Timestamp = DateTime.UtcNow,
            };

            PlayerData captain = null;
            PlayerData enemyCaptain = null;
            PlayerData Team1Player1 = null;
            PlayerData Team1Player2 = null;
            PlayerData Team2Player1 = null;
            PlayerData Team2Player2 = null;
            string team1Name = string.Empty;
            string team2Name = string.Empty;

            if (readyToStart)
            {
                MatchPlaying = true;
                List<PlayerData> players = new List<PlayerData>();
                for (int i = 0; i < PlayersInQueue.Count; i++)
                {
                    players.Add(discordPlayerToGamePlayer[PlayersInQueue[i]]);
                }

                var teams = await GetPlayerMatchups(ctx, players);
                Team1Player1 = teams.Item1.Item1;
                Team1Player2 = teams.Item1.Item2;
                Team2Player1 = teams.Item2.Item1;
                Team2Player2 = teams.Item2.Item2;

                captain = Team1Player1;
                enemyCaptain = Team2Player1;

                int t1p1L = (int)MathF.Floor((float)Team1Player1.Name.Length / 2f);
                int t1p2L = (int)MathF.Floor((float)Team1Player2.Name.Length / 2f);
                int t2p1L = (int)MathF.Floor((float)Team2Player1.Name.Length / 2f);
                int t2p2L = (int)MathF.Floor((float)Team2Player2.Name.Length / 2f);

                string team1Player1Half = Team1Player1.Name.Substring(0, t1p1L);
                string team1Player2Half = Team1Player2.Name.Substring(t1p2L, Team1Player2.Name.Length - t1p2L);
                string team2Player1Half = Team2Player1.Name.Substring(0, t2p1L);
                string team2Player2Half = Team2Player2.Name.Substring(t2p2L, Team2Player2.Name.Length - t2p2L);
                team1Name = team1Player1Half + team1Player2Half;
                team2Name = team2Player1Half + team2Player2Half;

                embed.AddField("Team " + team1Name, Team1Player1.Name + " (" + Team1Player1.CurrentElo +") & " + Team1Player2.Name + " (" + Team1Player2.CurrentElo + ")");
                embed.AddField("Team " + team2Name, Team2Player1.Name + " (" + Team2Player1.CurrentElo + ") & " + Team2Player2.Name + " (" + Team2Player2.CurrentElo + ")");
            }
            else
            {
                for (int i = 0; i < PlayersInQueue.Count; i++)
                {
                    string nameExtra = "";
                    if(i == 0)
                    {
                        nameExtra = " (Host)";
                    }
                    embed.AddField(PlayersInQueue[i].DisplayName + nameExtra, "Elo: " + discordPlayerToGamePlayer[PlayersInQueue[i]].CurrentElo);
                }
            }


            if (PreviousMessage != null)
            {
                await PreviousMessage.DeleteAsync();
            }

            Task<DiscordMessage> taskMsg = ctx.RespondAsync(embed: embed);
            PreviousMessage = taskMsg.Result;

            await taskMsg;

            if(readyToStart)
            {
                List<string> playerIds = new List<string>();
                List<PlayerData> players = new List<PlayerData>();
                for (int i = 0; i < PlayersInQueue.Count; i++)
                {
                    players.Add(discordPlayerToGamePlayer[PlayersInQueue[i]]);
                    playerIds.Add(discordPlayerToGamePlayer[PlayersInQueue[i]].ID);
                }

                await Task.Delay(2000);
                PreviousMessage = null;
                List<string> mapNames = await DatabaseModule.GetAllMapNames();

                do
                {
                    List<string> mapSelection = new List<string>();
                    Shuffle(mapNames);
                    for (int i = 0; i < MAP_COUNT; i++)
                    {
                        mapSelection.Add(mapNames[i]);
                    }

                    SelectingMap = true;
                    string mapSelectionResult = await StartMapSelection(ctx, mapSelection, captain, enemyCaptain, playerIds);

                    if(mapSelectionResult == ABORT_RESULT)
                    {
                        break;
                    }
                    else if(mapSelectionResult != RANDOMIZE_RESULT)
                    {
                        //Start map
                        // Get map ID
                        var mapid = await DatabaseModule.GetMapIDFromName(mapSelectionResult);
                        await StartMap(ctx, mapid, mapSelectionResult, team1: new List<PlayerData>() { Team1Player1, Team1Player2 }, team2: new List<PlayerData>() { Team2Player1, Team2Player2 }, team1Name, team2Name);
                        break; // Why did you not add a break here 7ark PLEASE

                        // Theoretically we replace this with like a "postgame"
                    }
                } while (true);

            }
        }

        private static string From64ToLegacy(string input64)
        {
            Int64 num64 = Int64.Parse(input64);
            string binary = Convert.ToString(num64, 2);
            binary = binary.PadLeft(64, '0');
            int legacy_x, legacy_y, legacy_z;
            string legacy_x_str = "";
            string legacy_y_str = "";
            string legacy_z_str = "";
            string accounttype = "";
            string accountinstance = "";
            for (int i = 0; i < 8; i++)
            {
                legacy_x_str += binary[i];
            }
            for (int i = 8; i < 12; i++)
            {
                accounttype += binary[i];
            }
            for (int i = 12; i < 32; i++)
            {
                accountinstance += binary[i];
            }
            for (int i = 32; i < 63; i++)
            {
                legacy_z_str += binary[i];
            }
            legacy_y_str += binary[63];

            legacy_x = Convert.ToInt32(legacy_x_str, 2);
            legacy_y = Convert.ToInt32(legacy_y_str, 2);
            legacy_z = Convert.ToInt32(legacy_z_str, 2);
            return $"STEAM_{legacy_x}:{legacy_y}:{legacy_z}";
        }

        private static async Task StartMap(CommandContext ctx, string mapID, string mapName, List<PlayerData> team1, List<PlayerData> team2, string team1Name, string team2Name)
        {

            var waitingembed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = $":gear: Please wait while the server is setup...",
                Timestamp = DateTime.UtcNow,            
            };

            var embedmessage = await ctx.RespondAsync(embed: waitingembed);


            var iteminfo = await SteamWorkshopModule.GetPublishedFileDetails(new List<string> { mapID });
            var bspname = iteminfo[0].Filename.Substring(iteminfo[0].Filename.LastIndexOf('/') + 1);

            var lastmatchid = await DatabaseModule.GetLastMatchID();

            for (int i = 0; i < CurrentSpectatorIds.Count; i++)
            {
                CurrentSpectatorIds[i] = From64ToLegacy(await DatabaseModule.GetPlayerSteamIDFromDiscordID(CurrentSpectatorIds[i]));
            }

            MatchConfigData configData = new MatchConfigData()
            {
                matchid = $"{lastmatchid + 1}",
                num_maps = 1,
                players_per_team = 2,
                min_players_to_ready = 2,
                min_spectators_to_ready = 0,
                skip_veto = true,
                veto_first = "team1",
                side_type = "standard",
                spectators = new PlayerJsonData()
                {
                    players = CurrentSpectatorIds
                },
                maplist = new List<string>()
                {
                    @$"workshop\{mapID}\{bspname}"
                },
                favored_percentage_team1 = 65,
                favored_percentage_text = "HLTV Bets",
                team1 = new TeamJsonData()
                {
                    name = team1Name,
                    tag = team1Name,
                    //flag = "FR",
                    //logo = "nv",
                    players = new List<string>()
                    {
                        From64ToLegacy(await DatabaseModule.GetPlayerSteamIDFromDiscordID(team1[0].ID)),
                        From64ToLegacy(await DatabaseModule.GetPlayerSteamIDFromDiscordID(team1[1].ID)),
                    }
                },
                team2 = new TeamJsonData()
                {
                    name = team2Name,
                    tag = team2Name,
                    //flag = "SE",
                    //logo = "fntc",
                    players = new List<string>()
                    {
                        From64ToLegacy(await DatabaseModule.GetPlayerSteamIDFromDiscordID(team2[0].ID)),
                        From64ToLegacy(await DatabaseModule.GetPlayerSteamIDFromDiscordID(team2[1].ID)),
                    }
                },
                cvars = new CvarsJsonData()
                {
                    hostname = "Match server #1"
                }
            };

            string json = JsonConvert.SerializeObject(configData, Formatting.Indented);

            //Grab FTP info from DB
            var testserver = await DatabaseModule.GetTestServerInfo("sc1");
            var connectaddressuri = new Uri($"ftp://" + GeneralUtil.GetIpEndPointFromString(testserver.Address));
            var connectaddress = connectaddressuri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Port, UriFormat.UriEscaped);
            string path = $"/match_configs/match_{lastmatchid + 1}.json";
            try
            {
                var request = (FtpWebRequest)WebRequest.Create(connectaddress + path);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(testserver.FtpUser, testserver.FtpPassword);
                using (var req = await request.GetRequestStreamAsync())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    await req.WriteAsync(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                // Oh god this is gonna fuck up the whole program if it gets here please don't happen oh god
                throw ex;
            }

            // If we've made it here, the match file should be written onto the server.
            // Maybe add in some redundancy so we know its actually there?
            var localrcon = RconInstance.RconModuleInstance;

            // I don't know why these have to get sent twice but it seems to only work this way

            await localrcon.WakeRconServer("sc1");
            await Task.Delay(2500);
            await localrcon.WakeRconServer("sc1");
            await Task.Delay(2500);
            await localrcon.RconCommand("sc1", "get5_endmatch");
            await Task.Delay(500);
            await localrcon.RconCommand("sc1", "get5_endmatch");
            await Task.Delay(500);
            await localrcon.RconCommand("sc1", $"get5_loadmatch \"{path}\"");
            await Task.Delay(500);
            await localrcon.RconCommand("sc1", $"get5_loadmatch \"{path}\"");
            await Task.Delay(500);
            await localrcon.RconCommand("sc1", $"host_workshop_map {mapID}");
            await Task.Delay(500);
            await localrcon.RconCommand("sc1", $"host_workshop_map {mapID}");
            await Task.Delay(500);
            await localrcon.RconCommand("sc1", $"get5_loadmatch \"{path}\"");
            await Task.Delay(500);



            var matchembed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = $"Server Ready!",
                Description = $"Connect Information: `connect sc1.quintonswenski.com`",
                Timestamp = DateTime.UtcNow,
                ImageUrl = iteminfo[0].PreviewURL,
            };
            matchembed.AddField($"Team {team1Name}", $"{team1[0].Name}\n{team1[1].Name}", true);
            matchembed.AddField($"Team {team2Name}", $"{team2[0].Name}\n{team2[1].Name}", true);
            matchembed.AddField(iteminfo[0].Title, "---------------------------------", false);

            //matchembed.WithFooter(iteminfo[0].Title);

            await embedmessage.ModifyAsync(string.Empty, (DiscordEmbed)matchembed);

            await MatchPostGame(ctx, lastmatchid + 1, mapName, team1, team2, team1Name, team2Name);
        }

        public static async Task MatchPostGame(CommandContext ctx, int matchId, string mapName, List<PlayerData> team1, List<PlayerData> team2, string team1Name, string team2Name)
        {
            while(true)
            {
                await Task.Delay(FREQUENCY_TO_CHECK_FOR_POSTGAME * 1000);
                bool currentstatus;
                currentstatus = await DatabaseModule.HasMatchEnded(matchId);
                Console.WriteLine($"CHECK END LOOP STATUS: {currentstatus}");
                if (currentstatus)
                {
                    break;
                }
            }

            Console.WriteLine("Got through RCON command");

            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = "CS:GO Match Finished! Calculating results...",
                Timestamp = DateTime.UtcNow,
            };

            Task<DiscordMessage> taskMsg = ctx.RespondAsync(embed: embed);
            PreviousMessage = taskMsg.Result;

            await taskMsg;

            await Task.Delay(5000);

            //Update stats and shit
            PlayerData t1p1Final = team1[0];
            PlayerData t1p2Final = team1[1];
            PlayerData t2p1Final = team2[0];
            PlayerData t2p2Final = team2[1];

            PlayerGameData team1player1Data = await DatabaseModule.GetPlayerStatsFromMatch(t1p1Final.ID, matchId, team1Name);
            PlayerGameData team1player2Data = await DatabaseModule.GetPlayerStatsFromMatch(t1p2Final.ID, matchId, team1Name);
            PlayerGameData team2player1Data = await DatabaseModule.GetPlayerStatsFromMatch(t2p1Final.ID, matchId, team2Name);
            PlayerGameData team2player2Data = await DatabaseModule.GetPlayerStatsFromMatch(t2p2Final.ID, matchId, team2Name);

            if(team1player1Data.TeamName != team1Name)
            {
                string temp = team1Name;
                team1Name = team2Name;
                team2Name = temp;
            }

            PlayerTeamMatch team1Data = new PlayerTeamMatch()
            {
                Player1 = t1p1Final,
                Player1MatchStats = team1player1Data,
                Player2 = t1p2Final,
                Player2MatchStats = team1player2Data,
                RoundsWon = team1player1Data.RoundsWon
            };

            PlayerTeamMatch team2Data = new PlayerTeamMatch()
            {
                Player1 = t2p1Final,
                Player1MatchStats = team2player1Data,
                Player2 = t2p2Final,
                Player2MatchStats = team2player2Data,
                RoundsWon = team2player1Data.RoundsWon
            };


            float t1p1Elo = Match.GetUpdatedPlayerEloWithMatchData(t1p1Final, team1Data, team2Data);
            float t1p2Elo = Match.GetUpdatedPlayerEloWithMatchData(t1p2Final, team1Data, team2Data);
            float t2p1Elo = Match.GetUpdatedPlayerEloWithMatchData(t2p1Final, team2Data, team1Data);
            float t2p2Elo = Match.GetUpdatedPlayerEloWithMatchData(t2p2Final, team2Data, team1Data);

            int t1p1EloDiff = (int)(t1p1Elo - t1p1Final.CurrentElo);
            int t1p2EloDiff = (int)(t1p2Elo - t1p2Final.CurrentElo);
            int t2p1EloDiff = (int)(t2p1Elo - t2p1Final.CurrentElo);
            int t2p2EloDiff = (int)(t2p2Elo - t2p2Final.CurrentElo);

            t1p1Final.CurrentElo = t1p1Elo;
            t1p2Final.CurrentElo = t1p2Elo;
            t2p1Final.CurrentElo = t2p1Elo;
            t2p2Final.CurrentElo = t2p2Elo;

            t1p1Final.UpdateWithGameData(team1player1Data);
            t1p2Final.UpdateWithGameData(team1player2Data);
            t2p1Final.UpdateWithGameData(team2player1Data);
            t2p2Final.UpdateWithGameData(team2player2Data);

            await DatabaseModule.DeletePlayerStats(t1p1Final.ID);
            await DatabaseModule.DeletePlayerStats(t1p2Final.ID);
            await DatabaseModule.DeletePlayerStats(t2p1Final.ID);
            await DatabaseModule.DeletePlayerStats(t2p2Final.ID);

            await DatabaseModule.AddPlayerMatchmakingStat(t2p1Final);
            await DatabaseModule.AddPlayerMatchmakingStat(t1p2Final);
            await DatabaseModule.AddPlayerMatchmakingStat(t1p1Final);
            await DatabaseModule.AddPlayerMatchmakingStat(t2p2Final);

            embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = "CS:GO Match Finished: " + (team1player1Data.WonGame ? team1Name : team2Name) + " Won! (" + (team1player1Data.WonGame ? team1player1Data.RoundsWon : team2player1Data.RoundsWon) + " / " + (team1player1Data.WonGame ? team1player1Data.RoundsLost : team2player1Data.RoundsLost) + ")",
                Timestamp = DateTime.UtcNow,
            };

            string diffVal = "";
            
            diffVal = t1p1EloDiff > 0 ? "+" : "";
            embed.AddField(t1p1Final.Name + " Elo: " + (int)(t1p1Final.CurrentElo) + " (" + diffVal + t1p1EloDiff + ")", "Kills: " + team1player1Data.KillCount + " | Assists: " + team1player1Data.AssistCount + " | Deaths: " + team1player1Data.DeathCount);

            diffVal = t1p2EloDiff > 0 ? "+" : "";
            embed.AddField(t1p2Final.Name + " Elo: " + (int)(t1p2Final.CurrentElo) + " (" + diffVal + t1p2EloDiff + ")", "Kills: " + team1player2Data.KillCount + " | Assists: " + team1player2Data.AssistCount + " | Deaths: " + team1player2Data.DeathCount);

            diffVal = t2p1EloDiff > 0 ? "+" : "";
            embed.AddField(t2p1Final.Name + " Elo: " + (int)(t2p1Final.CurrentElo) + " (" + diffVal + t2p1EloDiff + ")", "Kills: " + team2player1Data.KillCount + " | Assists: " + team2player1Data.AssistCount + " | Deaths: " + team2player1Data.DeathCount);

            diffVal = t2p2EloDiff > 0 ? "+" : "";
            embed.AddField(t2p2Final.Name + " Elo: " + (int)(t2p2Final.CurrentElo) + " (" + diffVal + t2p2EloDiff + ")", "Kills: " + team2player2Data.KillCount + " | Assists: " + team2player2Data.AssistCount + " | Deaths: " + team2player2Data.DeathCount);

            if (PreviousMessage != null)
            {
                await PreviousMessage.DeleteAsync();
            }

            taskMsg = ctx.RespondAsync(embed: embed);
            PreviousMessage = taskMsg.Result;

            await taskMsg;

            var localrcon = RconInstance.RconModuleInstance;
            try
            {
                await localrcon.RconCommand("sc1", "exit");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static async Task<Tuple<Tuple<PlayerData, PlayerData>, Tuple<PlayerData, PlayerData>>> GetPlayerMatchups(CommandContext ctx, List<PlayerData> players)
        {
            if(CaptainPick)
            {
                PreviousMessage = null;
                PlayerData captain = players[0];
                players.RemoveAt(0);

                PlayerData otherCaptain = await SelectPlayer(ctx, captain, players, "Select the enemy captain");
                players.Remove(otherCaptain);
                PlayerData otherPlayer = await SelectPlayer(ctx, otherCaptain, players, "Select your teammate");
                players.Remove(otherPlayer);

                return new Tuple<Tuple<PlayerData, PlayerData>, Tuple<PlayerData, PlayerData>>(
                new Tuple<PlayerData, PlayerData>(captain, players[0]),
                new Tuple<PlayerData, PlayerData>(otherCaptain, otherPlayer)
                );
            }
            else
            {
                return Match.GetMatchup(players.ToArray());
            }
        }

        private static async Task<PlayerData> SelectPlayer(CommandContext ctx, PlayerData selector, List<PlayerData> otherPlayers, string instructionText)
        {
            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = "Team Selector",
                Timestamp = DateTime.UtcNow
            };
            for (int i = 0; i < otherPlayers.Count; i++)
            {
                embed.AddField(otherPlayers[i].Name, ":" + numbersWritten[i + 1] + ":");
            }
            embed.AddField("Current Captain: " + selector.Name, instructionText);


            if (PreviousMessage != null)
            {
                await PreviousMessage.DeleteAsync();
            }

            Task<DiscordMessage> taskMsg = ctx.RespondAsync(embed: embed);
            PreviousMessage = taskMsg.Result;

            await taskMsg;

            for (int i = 0; i < otherPlayers.Count; i++)
            {
                await PreviousMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":" + numbersWritten[i + 1] + ":"));
            }

            int chosen = -1;
            bool waiting = true;
            while (waiting)
            {
                for (int i = 0; i < otherPlayers.Count; i++)
                {
                    var reacteds = await PreviousMessage.GetReactionsAsync(DiscordEmoji.FromName(ctx.Client, ":" + numbersWritten[i + 1] + ":"));
                    if (reacteds.Count > 1)
                    {
                        for (int j = 0; j < reacteds.Count; j++)
                        {
                            if (reacteds[j].Id.ToString() == selector.ID)
                            {
                                waiting = false;
                                chosen = i;
                            }
                        }
                    }
                }
                await Task.Delay(1000);
            }

            return otherPlayers[chosen];
        }

        public static async Task<string> StartMapSelection(CommandContext ctx, List<string> mapNames, PlayerData captain, PlayerData enemyCaptain, List<string> playerIds)
        {
            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = "Map Selector",
                Timestamp = DateTime.UtcNow,
            };

            for (int i = 0; i < mapNames.Count; i++)
            {
                embed.AddField(mapNames[i], ":" + numbersWritten[i + 1] + ":");
            }
            embed.AddField("Current Captain: " + captain.Name, "Select the map to remove it from the list of options");

            if (PreviousMessage != null)
            {
                await PreviousMessage.DeleteAsync();
            }

            Task<DiscordMessage> taskMsg = ctx.RespondAsync(embed: embed);
            PreviousMessage = taskMsg.Result; 

            await taskMsg;
            for (int i = 0; i < mapNames.Count; i++)
            {
                if(PreviousMessage == null)
                {
                    return ABORT_RESULT;
                }
                await PreviousMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":" + numbersWritten[i + 1] + ":"));
            }
            if (PreviousMessage == null)
            {
                return ABORT_RESULT;
            }
            await PreviousMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, randomizeMapEmoji));

            int chosen = -1;
            bool waiting = true;
            while(waiting)
            {
                if(!SelectingMap)
                {
                    break;
                }

                if (PreviousMessage == null)
                {
                    return ABORT_RESULT;
                }
                var randomizeReacts = await PreviousMessage.GetReactionsAsync(DiscordEmoji.FromName(ctx.Client, randomizeMapEmoji));
                if(randomizeReacts.Count >= NEEDED_FOR_RANDOMIZE + 1)
                {
                    int validReacts = 0;
                    for (int i = 0; i < randomizeReacts.Count; i++)
                    {
                        if(playerIds.Contains(randomizeReacts[i].Id.ToString()))
                        {
                            validReacts++;
                        }
                    }
                    if(validReacts >= NEEDED_FOR_RANDOMIZE)
                    {
                        return RANDOMIZE_RESULT;
                    }
                }
                for (int i = 0; i < mapNames.Count; i++)
                {
                    if (PreviousMessage == null)
                    {
                        return ABORT_RESULT;
                    }
                    var reacteds = await PreviousMessage.GetReactionsAsync(DiscordEmoji.FromName(ctx.Client, ":" + numbersWritten[i + 1] + ":"));
                    if (reacteds.Count > 1)
                    {
                        for (int j = 0; j < reacteds.Count; j++)
                        {
                            // COMMENTING THIS OUT MAKES THE BOT LET ANYONE VETO
                            if(reacteds[j].Id.ToString() == captain.ID)
                            {
                                waiting = false;
                                chosen = i;
                            }
                        }
                    }
                }
                await Task.Delay(1000);
            }
            if(SelectingMap)
            {
                mapNames.RemoveAt(chosen);

                if (mapNames.Count == 1)
                {
                    await PreviousMessage.DeleteAsync();
                    return mapNames[0];
                }
                else
                {
                    return await StartMapSelection(ctx, mapNames, enemyCaptain, captain, playerIds);
                }
            }
            else
            {
                return ABORT_RESULT;
            }
        }

        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }


    public static class Match
    {
        private const int STARTING_ELO = 1000;

        public static Tuple<Tuple<PlayerData, PlayerData>, Tuple<PlayerData, PlayerData>> GetMatchup(PlayerData[] dataEntries)
        {
            if (dataEntries.Length != 4)
            {
                Console.WriteLine("ERROR: Incorrect number of player entries sent");
                return null;
            }

            Array.Sort(dataEntries, (x, y) => { return x.CurrentElo.CompareTo(y.CurrentElo); });

            return new Tuple<Tuple<PlayerData, PlayerData>, Tuple<PlayerData, PlayerData>>(
                new Tuple<PlayerData, PlayerData>(dataEntries[0], dataEntries[3]),
                new Tuple<PlayerData, PlayerData>(dataEntries[1], dataEntries[2])
                );
        }

        public static float GetUpdatedPlayerEloWithMatchData(PlayerData player, PlayerTeamMatch friendlyTeam, PlayerTeamMatch enemyTeam)
        {
            const float ELO_SCALING_FACTOR = 400;

            const float BEGINNER_GAME_COUNT = 15;

            float allGamesPlayed = player.TotalGames - 1;
            float scalingRatio = 1 - MathF.Min(0.75f, allGamesPlayed / BEGINNER_GAME_COUNT);

            float GAME_WIN_SCALING_FACTOR = 20 * scalingRatio;
            float ROUND_SCALING_FACTOR = 20 * scalingRatio;
            float KILL_SCALING_FACTOR = 10 * scalingRatio;
            float ASSIST_SCALING_FACTOR = 1;
            float DEATH_SCALING_FACTOR = 4;
            float HEADSHOT_SCALING_FACTOR = 2;
            //float MVP_SCALING_FACTOR = 2;

            const float WIN_REDUCE = 1.5f;
            const float ROUND_REDUCE = 1.2f;
            const float KILL_REDUCE = 0.8f;
            const float ASSIST_REDUCE = 0.2f;
            const float DEATH_REDUCE = 0.6f;
            const float HEADSHOT_REDUCE = 0.4f;
            //const float MVP_REDUCE = 0.25f;

            float currentPlayerElo = player.CurrentElo;
            float friendlyElo = friendlyTeam.CombinedElo / 2f;
            float enemyElo = enemyTeam.CombinedElo / 2f;
            float roundsWon = friendlyTeam.RoundsWon;
            float roundsLost = enemyTeam.RoundsWon;
            int killCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.KillCount : friendlyTeam.Player2MatchStats.KillCount;
            int assistCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.AssistCount : friendlyTeam.Player2MatchStats.AssistCount;
            int deathCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.DeathCount : friendlyTeam.Player2MatchStats.DeathCount;
            int headshotCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.Headshots : friendlyTeam.Player2MatchStats.Headshots;
            //int mvpCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.MVPs : friendlyTeam.Player2MatchStats.MVPs;

            float expected = 1 / (1 + MathF.Pow(10, ((enemyElo - friendlyElo) / ELO_SCALING_FACTOR)));

            float winElo = 0;
            winElo += GAME_WIN_SCALING_FACTOR * ((roundsWon > roundsLost ? 1 : 0) - expected);
            winElo *= WIN_REDUCE;

            float roundElo = 0;
            for (int i = 0; i < roundsWon; i++)
            {
                roundElo += ROUND_SCALING_FACTOR * (1 - expected);
            }
            for (int i = 0; i < roundsLost; i++)
            {
                roundElo += ROUND_SCALING_FACTOR * (0 - expected);
            }

            roundElo *= ROUND_REDUCE;

            float killElo = 0;
            for (int i = 0; i < killCount; i++)
            {
                killElo += KILL_SCALING_FACTOR * (1 - expected);
            }
            killElo *= KILL_REDUCE;

            float assistElo = 0;
            for (int i = 0; i < assistCount; i++)
            {
                assistElo += ASSIST_SCALING_FACTOR * (1 - expected);
            }
            assistElo *= ASSIST_REDUCE;

            float deathElo = 0;
            for (int i = 0; i < deathCount; i++)
            {
                deathElo += DEATH_SCALING_FACTOR * (0 - expected);
            }
            deathElo *= DEATH_REDUCE;

            float headshotElo = 0;
            for (int i = 0; i < headshotCount; i++)
            {
                headshotElo += HEADSHOT_SCALING_FACTOR * (1 - expected);
            }
            headshotElo *= HEADSHOT_REDUCE;

            //float mvpElo = 0;
            //for (int i = 0; i < mvpCount; i++)
            //{
            //    mvpElo += MVP_SCALING_FACTOR * (1 - expected);
            //}
            //mvpElo *= MVP_REDUCE;


            float finalResult = currentPlayerElo
                + roundElo
                + winElo
                + killElo
                + assistElo
                + deathElo
                + headshotElo;
                //+ mvpElo;
            return finalResult;
        }
    }

    #region Player/Match Data

    public class PlayerData
    {
        //Cosmetic
        public string Name;

        //Data
        public string ID;
        public float CurrentElo;

        public int TotalGamesWon;
        public int TotalGamesLost;
        public int TotalRoundsWon;
        public int TotalRoundsLost;
        public int TotalKillCount;
        public int TotalAssistCount;
        public int TotalDeathCount;
        public int TotalHeadshotCount;
        //public int TotalMVPCount;

        public int TotalGames
        {
            get
            {
                return TotalGamesWon + TotalGamesLost;
            }
        }

        public void UpdateWithGameData(PlayerGameData gameData)
        {
            if(gameData.WonGame)
            {
                TotalGamesWon++;
            }
            else
            {
                TotalGamesLost++;
            }
            TotalRoundsWon += gameData.RoundsWon;
            TotalRoundsLost += gameData.RoundsLost;
            TotalKillCount += gameData.KillCount;
            TotalAssistCount += gameData.AssistCount;
            TotalDeathCount += gameData.DeathCount;
            TotalHeadshotCount += gameData.Headshots;
        }
    }

    public class PlayerGameData
    {
        public string TeamName;
        public int TeamNumber;

        public int RoundsWon;
        public int RoundsLost;

        public int KillCount;
        public int AssistCount;
        public int DeathCount;
        public int Headshots;
        //public int MVPs;

        public bool WonGame;

        public PlayerGameData() { }
        public PlayerGameData(PlayerGameData one, PlayerGameData two)
        {
            KillCount = one.KillCount + two.KillCount;
            AssistCount = one.AssistCount + two.AssistCount;
            DeathCount = one.DeathCount + two.DeathCount;
            Headshots = one.Headshots + two.Headshots;
            //MVPs = one.MVPs + two.MVPs;

            WonGame = one.WonGame;
        }
    }

    public struct PlayerTeamMatch
    {
        public int RoundsWon;

        public PlayerData Player1;
        public PlayerData Player2;

        public PlayerGameData Player1MatchStats;
        public PlayerGameData Player2MatchStats;

        public PlayerGameData CombinedMatchStats { get { return new PlayerGameData(Player1MatchStats, Player2MatchStats); } }
        public float CombinedElo { get { return Player1.CurrentElo + Player2.CurrentElo; } }

        public float AverageRoundsWon
        {
            get
            {
                return (float)(Player1.TotalRoundsWon + Player2.TotalRoundsWon) / (float)(Player1.TotalGames + Player2.TotalGames);
            }
        }
        public float AverageRoundsLost
        {
            get
            {
                return (float)(Player1.TotalRoundsLost + Player2.TotalRoundsLost) / (float)(Player1.TotalGames + Player2.TotalGames);
            }
        }
    }
    #endregion
}
