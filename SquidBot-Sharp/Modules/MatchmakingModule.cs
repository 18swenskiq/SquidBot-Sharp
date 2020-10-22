using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Newtonsoft.Json;
using SquidBot_Sharp.Utilities;
using SteamKit2.GC.Artifact.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const int FREQUENCY_TO_CHECK_FOR_POSTGAME = 5;
        private const bool PRE_SETUP_ONLY = false;

        public static List<DiscordMember> PlayersInQueue = new List<DiscordMember>();
        public static List<string> CurrentSpectatorIds = new List<string>();
        public static DiscordMessage PreviousMessage = null;
        public static bool CanJoinQueue = true;
        public static bool Queueing = false;
        public static bool MatchPlaying = false;
        public static bool CaptainPick = false;
        public static ulong CurrentMapID;
        public static bool SelectingMap { get; private set; } = false;
        public static bool WasReset = false;
        private static Dictionary<DiscordMember, PlayerData> discordPlayerToGamePlayer = new Dictionary<DiscordMember, PlayerData>();
        private static Dictionary<PlayerData, DiscordMember> gamePlayerToDiscordPlayer = new Dictionary<PlayerData, DiscordMember>();
        private static Random rng = new Random();



        public static async Task TimeOut(CommandContext ctx)
        {
            await Task.Delay(1000 * SECONDS_UNTIL_TIMEOUT);

            if (!MatchPlaying && !WasReset)
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
            if (MatchPlaying)
            {
                return true;
            }

            if (!PlayersInQueue.Contains(member))
            {
                PlayersInQueue.Add(member);
            }

            var player = await DatabaseModule.GetPlayerMatchmakingStats(member.Id.ToString());

            if (player.ID == null)
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

            if (!discordPlayerToGamePlayer.ContainsKey(member))
            {
                discordPlayerToGamePlayer.Add(member, player);
            }
            discordPlayerToGamePlayer[member] = player;

            if (!gamePlayerToDiscordPlayer.ContainsKey(player))
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

            if (PlayersInQueue.Contains(member))
            {
                var player = await DatabaseModule.GetPlayerMatchmakingStats(member.Id.ToString());

                if (player.Name != member.DisplayName)
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
            if (readyToStart)
            {
                playersNeededText = "(Match Ready)";
            }
            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = "CS:GO Match Queue " + playersNeededText,
                Timestamp = DateTime.UtcNow,
                Footer = new DiscordEmbedBuilder.EmbedFooter() { Text = "Type >queue to join" }
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

                team1Name = GeneralUtil.GetHalfString(Team1Player1.Name, true) + GeneralUtil.GetHalfString(Team1Player2.Name, false);
                team2Name = GeneralUtil.GetHalfString(Team2Player1.Name, true) + GeneralUtil.GetHalfString(Team2Player2.Name, false);

                embed.AddField("Team " + team1Name, Team1Player1.Name + " (" + Team1Player1.CurrentElo + ") & " + Team1Player2.Name + " (" + Team1Player2.CurrentElo + ")");
                embed.AddField("Team " + team2Name, Team2Player1.Name + " (" + Team2Player1.CurrentElo + ") & " + Team2Player2.Name + " (" + Team2Player2.CurrentElo + ")");
            }
            else
            {
                for (int i = 0; i < PlayersInQueue.Count; i++)
                {
                    string nameExtra = "";
                    if (i == 0)
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

            if (readyToStart)
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

                // Add new methods for map selection
                var mapselectmodeembed = new DiscordEmbedBuilder
                {
                    Color = new DiscordColor(0x3277a8),
                    Title = "Map Veto Mode Selector",
                    Timestamp = DateTime.UtcNow,
                };
                mapselectmodeembed.AddField("Random Pool Veto", ":one:");
                mapselectmodeembed.AddField("All Pick/Random Selection", ":two:");
                mapselectmodeembed.AddField("Queue Leader Pick", ":three:");
                mapselectmodeembed.AddField("MechaSquidski's Pick", ":four:");
                mapselectmodeembed.AddField("Queue Leader: " + PlayersInQueue[0].Nickname, "Select your preferred option for map selection!");


                Task<DiscordMessage> taskMapMsg = ctx.RespondAsync(embed: mapselectmodeembed);
                PreviousMessage = taskMapMsg.Result;

                SelectingMap = true;
                await taskMapMsg;
                for(int i = 1; i <= 4; i++)
                {
                    await PreviousMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, $":{numbersWritten[i]}:"));
                }

                List<string> mapNames;

                while (true)
                {
                    var listoflists = new List<IReadOnlyList<DiscordUser>>();
                    for (int i = 1; i <= 4; i++)
                    {
                        listoflists.Add(await PreviousMessage.GetReactionsAsync(DiscordEmoji.FromName(ctx.Client, $":{numbersWritten[i]}:")));
                    }

                    var tempMapNames = await DatabaseModule.GetAllMapNames();

                    if (listoflists[0].Contains(PlayersInQueue[0]))
                    {
                        // Complete Random Veto
                        mapNames = tempMapNames;
                        break;
                    }
                    if (listoflists[1].Contains(PlayersInQueue[0]))
                    {

                        // BUG THIS
                        await ctx.RespondAsync($"All players, please type your map choice!");
                        var interactivity = ctx.Client.GetInteractivity();
                        var playerselectiondict = new Dictionary<DiscordUser, string>();
                        while (true)
                        {
                            if (playerselectiondict.Count == 4)
                            {
                                break;
                            }
                            var userinput = await interactivity.WaitForMessageAsync(us => us.Author == us.Author, TimeSpan.FromSeconds(5000));
                            if (PlayersInQueue.Contains(userinput.Result.Author) && !playerselectiondict.Keys.Contains(userinput.Result.Author))
                            {
                                var mapselectresult = GeneralUtil.ListContainsCaseInsensitive(tempMapNames, userinput.Result.Content);
                                if (mapselectresult)
                                {
                                    playerselectiondict.Add(userinput.Result.Author, userinput.Result.Content);
                                }
                                else
                                {
                                    await ctx.RespondAsync($"{userinput.Result.Author.Username}, your map selection was not found. Please try another name.");
                                }
                            }
                        }
                        var mapselectindex = rng.Next(0, 3);
                        mapNames = new List<string> { playerselectiondict.ElementAt(mapselectindex).Value };
                        break;

                    }
                    if (listoflists[2].Contains(PlayersInQueue[0]))
                    {
                        // Queue Leader Pick
                        await ctx.RespondAsync($"{PlayersInQueue[0].Nickname}, please type the name of a map to play!");
                        var interactivity = ctx.Client.GetInteractivity();
                        while (true)
                        {
                            var userinput = await interactivity.WaitForMessageAsync(us => us.Author == us.Author, TimeSpan.FromSeconds(5000));
                            if (userinput.Result.Author == PlayersInQueue[0])
                            {
                                var boolresult = GeneralUtil.ListContainsCaseInsensitive(tempMapNames, userinput.Result.Content);
                                if (boolresult)
                                {
                                    mapNames = new List<string> { userinput.Result.Content };
                                    break;
                                }
                                else
                                {
                                    await ctx.RespondAsync($"{userinput.Result.Author.Username}, your map selection was not found. Please try another name.");
                                }

                            }
                        }
                        break;

                    }
                    if (listoflists[3].Contains(PlayersInQueue[0]))
                    {
                        // MechaSquidski pick
                        mapNames = new List<string>
                        {
                            tempMapNames[rng.Next(tempMapNames.Count)]
                        };
                        break;
                    }
                }

                await PreviousMessage.DeleteAsync();
                PreviousMessage = null;

                do
                {
                    string mapSelectionResult = "";

                    // If we already know what map we want, just skip the map veto part
                    if (mapNames.Count == 1)
                    {
                        mapSelectionResult = mapNames[0];
                        goto StartMap;
                    }

                    List<string> mapSelection = new List<string>();
                    Shuffle(mapNames);
                    for (int i = 0; i < MAP_COUNT; i++)
                    {
                        mapSelection.Add(mapNames[i]);
                    }

                    SelectingMap = true;
                    mapSelectionResult = await StartMapSelection(ctx, mapSelection, captain, enemyCaptain, playerIds);

                StartMap:
                    if (mapSelectionResult == ABORT_RESULT)
                    {
                        break;
                    }
                    else if (mapSelectionResult != RANDOMIZE_RESULT)
                    {
                        //Start map
                        SelectingMap = false;
                        var mapid = await DatabaseModule.GetMapIDFromName(mapSelectionResult);
                        CurrentMapID = ulong.Parse(mapid);
                        await StartMap(ctx, mapid, mapSelectionResult, team1: new List<PlayerData>() { Team1Player1, Team1Player2 }, team2: new List<PlayerData>() { Team2Player1, Team2Player2 }, team1Name, team2Name);
                        break;
                    }
                } while (true);

            }
        }

        public static async Task<bool> ChangeNameIfRelevant(DiscordMember member)
        {
            PlayerData player = await DatabaseModule.GetPlayerMatchmakingStats(member.Id.ToString());
            if (player.ID != null)
            {
                if (player.Name != member.DisplayName)
                {
                    player.Name = member.DisplayName;

                    await DatabaseModule.DeletePlayerStats(player.ID);
                    await DatabaseModule.AddPlayerMatchmakingStat(player);

                    return true;
                }
            }

            return false;
        }

        private static async Task StartMap(CommandContext ctx, string mapID, string mapName, List<PlayerData> team1, List<PlayerData> team2, string team1Name, string team2Name)
        {

            var waitingembed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = $":gear: Please wait while the server is setup...",
                Timestamp = DateTime.UtcNow,
                Footer = new DiscordEmbedBuilder.EmbedFooter() { Text = "Map: " + mapName }
            };

            var embedmessage = await ctx.RespondAsync(embed: waitingembed);

            if (PRE_SETUP_ONLY)
            {
                await Reset();
                return;
            }


            var iteminfo = await SteamWorkshopModule.GetPublishedFileDetails(new List<string> { mapID });
            var bspname = iteminfo[0].Filename.Substring(iteminfo[0].Filename.LastIndexOf('/') + 1);

            var lastmatchid = await DatabaseModule.GetLastMatchID();

            for (int i = 0; i < CurrentSpectatorIds.Count; i++)
            {
                CurrentSpectatorIds[i] = GeneralUtil.SteamIDFrom64ToLegacy(await DatabaseModule.GetPlayerSteamIDFromDiscordID(CurrentSpectatorIds[i]));
            }

            MatchConfigData configData = new MatchConfigData($"{lastmatchid + 1}", CurrentSpectatorIds, @$"workshop\{mapID}\{bspname}", team1Name, team2Name, await DatabaseModule.GetPlayerSteamIDFromDiscordID(team1[0].ID), await DatabaseModule.GetPlayerSteamIDFromDiscordID(team1[1].ID), await DatabaseModule.GetPlayerSteamIDFromDiscordID(team2[0].ID), await DatabaseModule.GetPlayerSteamIDFromDiscordID(team2[1].ID));

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
            await localrcon.RconCommand("sc1", $"host_workshop_map {mapID}");
            await Task.Delay(2000);
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
                Footer = new DiscordEmbedBuilder.EmbedFooter() { Text = "Map: " + mapName }
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
            while (true)
            {
                bool currentstatus;
                currentstatus = await DatabaseModule.HasMatchEnded(matchId);
                Console.WriteLine($"CHECK END LOOP STATUS: {currentstatus}");
                if (currentstatus)
                {
                    break;
                }
                await Task.Delay(FREQUENCY_TO_CHECK_FOR_POSTGAME * 1000);
            }

            var localrcon = RconInstance.RconModuleInstance;
            try
            {
                await localrcon.RconCommand("sc1", "exit");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = "CS:GO Match Finished! Calculating results...",
                Timestamp = DateTime.UtcNow,
                Footer = new DiscordEmbedBuilder.EmbedFooter() { Text = "Map: " + mapName }
            };

            Task<DiscordMessage> taskMsg = ctx.RespondAsync(embed: embed);
            PreviousMessage = taskMsg.Result;

            await taskMsg;

            await Task.Delay(5000);

            var statsembed = await UpdateStatsPostGame(ctx, team1, team2, matchId, team1Name, team2Name);

            if (PreviousMessage != null)
            {
                await PreviousMessage.DeleteAsync();
            }

            taskMsg = ctx.RespondAsync(embed: statsembed);
            PreviousMessage = null;

            await taskMsg;

            await Reset();
            await localrcon.WakeRconServer("sc1");
        }

        public static async Task RecalculateAllElo(CommandContext ctx, Dictionary<string, string> steamIdToPlayerId)
        {
            int lastMatchId = await DatabaseModule.GetLastMatchID();

            int currentMatchId = 1;
            while (currentMatchId <= lastMatchId)
            {
                if (await DatabaseModule.HasMatchEnded(currentMatchId))
                {
                    await ctx.RespondAsync("Starting evaluation of match id: " + currentMatchId);
                    List<string> playerIdsTeam1 = await DatabaseModule.GetPlayersFromMatch(currentMatchId, 1);
                    List<string> playerIdsTeam2 = await DatabaseModule.GetPlayersFromMatch(currentMatchId, 2);

                    List<PlayerData> playersTeam1 = new List<PlayerData>();
                    for (int i = 0; i < playerIdsTeam1.Count; i++)
                    {
                        playersTeam1.Add(await DatabaseModule.GetPlayerMatchmakingStats(steamIdToPlayerId[playerIdsTeam1[i]]));
                    }

                    List<PlayerData> playersTeam2 = new List<PlayerData>();
                    for (int i = 0; i < playerIdsTeam2.Count; i++)
                    {
                        playersTeam2.Add(await DatabaseModule.GetPlayerMatchmakingStats(steamIdToPlayerId[playerIdsTeam2[i]]));
                    }

                    List<string> teamNames = await DatabaseModule.GetTeamNamesFromMatch(currentMatchId);

                    await MatchPostGame(ctx, currentMatchId, "WhoCares", playersTeam1, playersTeam2, teamNames[0], teamNames[1]);

                }
                currentMatchId++;
            }

            await ctx.RespondAsync("All matches recalculated.");
        }

        private static async Task<Tuple<Tuple<PlayerData, PlayerData>, Tuple<PlayerData, PlayerData>>> GetPlayerMatchups(CommandContext ctx, List<PlayerData> players)
        {
            if (CaptainPick)
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
                embed.AddField(mapNames[i], $":{numbersWritten[i + 1]}:");
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
                if (PreviousMessage == null)
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
            while (waiting)
            {
                if (!SelectingMap)
                {
                    break;
                }

                if (PreviousMessage == null)
                {
                    return ABORT_RESULT;
                }
                var randomizeReacts = await PreviousMessage.GetReactionsAsync(DiscordEmoji.FromName(ctx.Client, randomizeMapEmoji));
                if (randomizeReacts.Count >= NEEDED_FOR_RANDOMIZE + 1)
                {
                    int validReacts = 0;
                    for (int i = 0; i < randomizeReacts.Count; i++)
                    {
                        if (playerIds.Contains(randomizeReacts[i].Id.ToString()))
                        {
                            validReacts++;
                        }
                    }
                    if (validReacts >= NEEDED_FOR_RANDOMIZE)
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
                            if (reacteds[j].Id.ToString() == captain.ID)
                            {
                                waiting = false;
                                chosen = i;
                            }
                        }
                    }
                }
                await Task.Delay(1000);
            }
            if (SelectingMap)
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

        public static async Task<DiscordEmbed> UpdateStatsPostGame(CommandContext ctx, List<PlayerData> team1, List<PlayerData> team2, int matchId, string team1Name, string team2Name)
        {
            //Update stats and shit
            PlayerData t1p1Final = team1[0];
            PlayerData t1p2Final = team1[1];
            PlayerData t2p1Final = team2[0];
            PlayerData t2p2Final = team2[1];

            PlayerGameData team1player1Data = await DatabaseModule.GetPlayerStatsFromMatch(t1p1Final.ID, matchId, team1Name);
            PlayerGameData team1player2Data = await DatabaseModule.GetPlayerStatsFromMatch(t1p2Final.ID, matchId, team1Name);
            PlayerGameData team2player1Data = await DatabaseModule.GetPlayerStatsFromMatch(t2p1Final.ID, matchId, team2Name);
            PlayerGameData team2player2Data = await DatabaseModule.GetPlayerStatsFromMatch(t2p2Final.ID, matchId, team2Name);

            if (team1player1Data.TeamName != team1Name)
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

            var embed = new DiscordEmbedBuilder
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

            return embed;
        }

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
}
