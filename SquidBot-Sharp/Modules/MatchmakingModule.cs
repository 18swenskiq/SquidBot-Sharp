using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.VisualBasic.CompilerServices;
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
        public struct BetData { public string Name; public long BetAmount; public string UserToBetOn; }
        private enum MapSelectionType { RandomPoolVeto, AllPick, LeaderPick, CompletelyRandomPick };

        public enum MatchmakingState { Idle, Queueing, GameSetup, GameInProgress, DisplayingResults }

        private static string[] numbersWritten = new string[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
        private const string randomizeMapEmoji = ":game_die:";
        private const string RANDOMIZE_RESULT = "RANDOMIZE";
        private const string ABORT_RESULT = "ABORT";
        private const string SQUIDCOIN = ":squidcoin:";
        private const int MAP_COUNT = 7;
        private const int NEEDED_FOR_RANDOMIZE = 3;
        private const int SECONDS_IN_MINUTE = 60;
        private const int SECONDS_UNTIL_TIMEOUT = SECONDS_IN_MINUTE * 5;
        private const int FREQUENCY_TO_CHECK_FOR_POSTGAME = 5;
        private const bool PRE_SETUP_ONLY = false;

        private const long SQUID_COIN_REWARD_SPECTATE = 30;
        private const long SQUID_COIN_REWARD_PLAY = 100;
        private const float SQUID_COIN_BET_WIN = 2f;
        private const int SECONDS_IN_ALLOW_BETTING = 45;

        public static Dictionary<string, BetData> Bets = new Dictionary<string, BetData>();
        public static List<DiscordMember> PlayersInQueue = new List<DiscordMember>();
        public static List<string> CurrentSpectatorDiscordIds = new List<string>();
        public static List<string> CurrentSpectatorIds = new List<string>();
        public static List<string> CurrentSpectatorNames = new List<string>();
        public static DiscordMessage PreviousMessage = null;
        public static MatchmakingState CurrentGameState = MatchmakingState.Idle;
        public static ulong CurrentMapID;
        public static int PlayersPerTeam;
        public static bool BettingAllowed { get; private set; } = false;
        private static Dictionary<DiscordMember, PlayerData> discordPlayerToGamePlayer = new Dictionary<DiscordMember, PlayerData>();
        private static Dictionary<PlayerData, DiscordMember> gamePlayerToDiscordPlayer = new Dictionary<PlayerData, DiscordMember>();
        private static PlayerTeamMatch? currentWinner = null;
        private static Random rng = new Random();



        public static async Task TimeOut(CommandContext ctx)
        {
            await Task.Delay(1000 * SECONDS_UNTIL_TIMEOUT);

            if (CurrentGameState == MatchmakingState.Queueing)
            {
                await Reset();
                await ctx.RespondAsync("CS:GO session queue timed out after " + SECONDS_UNTIL_TIMEOUT + " seconds with no joins");
            }
        }

        public static async Task Reset()
        {
            CurrentGameState = MatchmakingState.Idle;
            BettingAllowed = false;
            PlayersInQueue.Clear();
            discordPlayerToGamePlayer.Clear();
            gamePlayerToDiscordPlayer.Clear();
            CurrentSpectatorIds.Clear();
            CurrentSpectatorNames.Clear();
            CurrentSpectatorDiscordIds.Clear();
            Bets.Clear();
            currentWinner = null;


            if (PreviousMessage != null)
            {
                await PreviousMessage.DeleteAsync();
            }

            PreviousMessage = null;
        }

        private static async Task AwardSquidCoin(string discordId, long amount)
        {
            long currentCoin = await DatabaseModule.GetPlayerSquidCoin(discordId);

            await DatabaseModule.DeleteSquidCoinPlayer(discordId);
            await DatabaseModule.AddSquidCoinPlayer(discordId, Math.Max(0, currentCoin + amount));
        }

        public static async Task<bool> DoesPlayerHaveSteamIDRegistered(CommandContext ctx, DiscordMember member)
        {
            string id = await DatabaseModule.GetPlayerSteamIDFromDiscordID(member.Id.ToString());

            return id != string.Empty;
        }

        public static async Task JoinQueue(CommandContext ctx, DiscordMember member)
        {
            if (CurrentGameState != MatchmakingState.Queueing)
            {
                return;
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
        }
        public static async Task LeaveQueue(CommandContext ctx, DiscordMember member)
        {
            if (CurrentGameState == MatchmakingState.GameInProgress)
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
            int PlayersToStart = PlayersPerTeam * 2;
            BettingAllowed = true;
            bool readyToStart = PlayersInQueue.Count >= PlayersToStart;
            string playersNeededText = "(" + (PlayersToStart - PlayersInQueue.Count) + " Players Required)";
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

            PlayerData Team1Captain = null;          
            PlayerData Team1Player1 = null;
            PlayerData Team1Player2 = null;
            PlayerData Team1Player3 = null;

            PlayerData Team2Captain = null;
            PlayerData Team2Player1 = null;
            PlayerData Team2Player2 = null;
            PlayerData Team2Player3 = null;

            string team1Name = string.Empty;
            string team2Name = string.Empty;

            if (readyToStart)
            {
                CurrentGameState = MatchmakingState.GameSetup;
                List<PlayerData> players = new List<PlayerData>();
                for (int i = 0; i < PlayersInQueue.Count; i++)
                {
                    players.Add(discordPlayerToGamePlayer[PlayersInQueue[i]]);
                }

                var teams = Match.GetMatchup(players.ToArray());
                Team1Player1 = teams[0][0];
                Team1Player2 = teams[0][1];
                Team2Player1 = teams[1][0];
                Team2Player2 = teams[1][1];
                Team1Captain = Team1Player1;
                Team2Captain = Team2Player1;

                if (teams[0].Length > 2)
                {
                    Team1Player3 = teams[0][2];
                    Team2Player3 = teams[1][2];
                    team1Name = GeneralUtil.GetThirdString(Team1Player1.Name, 1) + GeneralUtil.GetThirdString(Team1Player2.Name, 2) + GeneralUtil.GetThirdString(Team1Player3.Name, 3);
                    team2Name = GeneralUtil.GetThirdString(Team2Player1.Name, 1) + GeneralUtil.GetThirdString(Team2Player2.Name, 2) + GeneralUtil.GetThirdString(Team2Player3.Name, 3);

                    embed.AddField("Team " + team1Name, Team1Player1.Name + " (" + Team1Player1.CurrentElo + ") & " + Team1Player2.Name + " (" + Team1Player2.CurrentElo + ") & " + Team1Player3.Name + " (" + Team1Player3.CurrentElo + ")");
                    embed.AddField("Team " + team2Name, Team2Player1.Name + " (" + Team2Player1.CurrentElo + ") & " + Team2Player2.Name + " (" + Team2Player2.CurrentElo + ") & " + Team2Player3.Name + " (" + Team2Player3.CurrentElo + ")");
                }
                else
                {
                    team1Name = GeneralUtil.GetHalfString(Team1Player1.Name, true) + GeneralUtil.GetHalfString(Team1Player2.Name, false);
                    team2Name = GeneralUtil.GetHalfString(Team2Player1.Name, true) + GeneralUtil.GetHalfString(Team2Player2.Name, false);

                    embed.AddField("Team " + team1Name, Team1Player1.Name + " (" + Team1Player1.CurrentElo + ") & " + Team1Player2.Name + " (" + Team1Player2.CurrentElo + ")");
                    embed.AddField("Team " + team2Name, Team2Player1.Name + " (" + Team2Player1.CurrentElo + ") & " + Team2Player2.Name + " (" + Team2Player2.CurrentElo + ")");
                }                                        
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

                do
                {
                    string mapSelectionResult = await DetermineMapSelectionType(ctx, Team1Captain, Team2Captain, playerIds);

                    if (mapSelectionResult == ABORT_RESULT)
                    {
                        break;
                    }
                    else if (mapSelectionResult != RANDOMIZE_RESULT)
                    {
                        //Start map
                        CurrentGameState = MatchmakingState.GameInProgress;
                        var mapid = await DatabaseModule.GetMapIDFromName(mapSelectionResult);
                        CurrentMapID = ulong.Parse(mapid);
                        await StartMap(ctx, mapid, mapSelectionResult, team1: new List<PlayerData>() { Team1Player1, Team1Player2 }, team2: new List<PlayerData>() { Team2Player1, Team2Player2 }, team1Name, team2Name, PlayersToStart / 2);
                        break;
                    }
                } while (true);

            }
        }

        #region Map Selection

        private static async Task<string> DetermineMapSelectionType(CommandContext ctx, PlayerData team1Captain, PlayerData team2Captain, List<string> playerIds)
        {
            int teamSize = playerIds.Count / 2;

            // Add new methods for map selection
            var mapselectmodeembed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = "Map Veto Mode Selector",
                Timestamp = DateTime.UtcNow,
            };

            DiscordMember userCaptain = PlayersInQueue[0];

            int mapSelectionTypesLength = System.Enum.GetValues(typeof(MapSelectionType)).Length;
            for (int i = 0; i < mapSelectionTypesLength; i++)
            {
                MapSelectionType type = (MapSelectionType)i;
                mapselectmodeembed.AddField(type.GetDialogueDisplay(), type.GetEmoteRepresentation());
            }

            mapselectmodeembed.AddField("Queue Leader: " + userCaptain.DisplayName, "Select your preferred option for map selection!");


            Task<DiscordMessage> taskMapMsg = ctx.RespondAsync(embed: mapselectmodeembed);
            PreviousMessage = taskMapMsg.Result;

            await taskMapMsg;
            for (int i = 0; i < mapSelectionTypesLength; i++)
            {
                MapSelectionType type = (MapSelectionType)i;
                await PreviousMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, type.GetEmoteRepresentation()));
            }

            List<string> allMapNames;
            if((teamSize * 2) == 4)
            {
                allMapNames = await DatabaseModule.GetAllMapNames(false, 2);
            }
            else // 6 players
            {
                allMapNames = await DatabaseModule.GetAllMapNames(false, 3);
            }

            MapSelectionType? confirmedType = null;

            while (true)
            {
                var listoflists = new List<IReadOnlyList<DiscordUser>>();
                for (int i = 0; i < mapSelectionTypesLength; i++)
                {
                    MapSelectionType type = (MapSelectionType)i;
                    listoflists.Add(await PreviousMessage.GetReactionsAsync(DiscordEmoji.FromName(ctx.Client, type.GetEmoteRepresentation())));
                }

                for (int i = 0; i < mapSelectionTypesLength; i++)
                {
                    MapSelectionType type = (MapSelectionType)i;
                    if (listoflists[i].Contains(userCaptain))
                    {
                        confirmedType = type;
                    }
                }

                if (confirmedType != null)
                {
                    break;
                }
            }

            switch (confirmedType)
            {
                case MapSelectionType.RandomPoolVeto:
                    List<string> mapOptions = new List<string>(allMapNames);
                    Shuffle(mapOptions);
                    List<string> sendMapOptions = new List<string>();
                    for (int i = 0; i < MAP_COUNT; i++)
                    {
                        sendMapOptions.Add(mapOptions[i]);
                    }

                    return await StartRandomVetoMapSelection(ctx, sendMapOptions, team1Captain, team2Captain, playerIds);
                case MapSelectionType.AllPick:
                    return await StartAllPickMapSelection(ctx, allMapNames, teamSize);
                case MapSelectionType.LeaderPick:
                    return await StartLeaderPickMapSelection(ctx);
                case MapSelectionType.CompletelyRandomPick:
                    return StartRandomMapSelection(ctx, allMapNames);
            }

            await PreviousMessage.DeleteAsync();
            PreviousMessage = null;

            return ABORT_RESULT;
        }

        public static async Task<string> StartRandomVetoMapSelection(CommandContext ctx, List<string> mapNames, PlayerData captain, PlayerData enemyCaptain, List<string> playerIds)
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
                if (CurrentGameState != MatchmakingState.GameSetup)
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
            if (CurrentGameState == MatchmakingState.GameSetup)
            {
                mapNames.RemoveAt(chosen);

                if (mapNames.Count == 1)
                {
                    await PreviousMessage.DeleteAsync();
                    return mapNames[0];
                }
                else
                {
                    return await StartRandomVetoMapSelection(ctx, mapNames, enemyCaptain, captain, playerIds);
                }
            }
            else
            {
                return ABORT_RESULT;
            }
        }

        private static async Task<string> StartAllPickMapSelection(CommandContext ctx, List<string> enabledMapNames, int teamSize)
        {
            var allMapNamesIncludingDisabled = await DatabaseModule.GetAllMapNames(true);
            if (PreviousMessage != null)
            {
                await PreviousMessage.DeleteAsync();
            }
            PreviousMessage = null;

            var interactivity = ctx.Client.GetInteractivity();
            var playerSelections = new Dictionary<DiscordUser, string>();
            bool updateEmbed = true;
            while (true)
            {
                if(CurrentGameState != MatchmakingState.GameSetup)
                {
                    return ABORT_RESULT;
                }

                if (playerSelections.Count == (teamSize * 2))
                {
                    break;
                }

                var embed = new DiscordEmbedBuilder
                {
                    Color = new DiscordColor(0x3277a8),
                    Title = "Map Selector",
                    Timestamp = DateTime.UtcNow,
                    Description = "Please type your map choice! "
                };

                if (updateEmbed)
                {
                    updateEmbed = false;

                    //Purely visual for who has/hasn't selected a map yet.
                    embed.Description = "Please type your map choice! Waiting for: ";
                    for (int i = 0; i < PlayersInQueue.Count; i++)
                    {
                        bool contained = false;
                        foreach (DiscordUser user in playerSelections.Keys)
                        {
                            if (user.Id == PlayersInQueue[i].Id)
                            {
                                contained = true;
                                break;
                            }
                        }
                        if (!contained)
                        {
                            embed.Description += PlayersInQueue[i].DisplayName + ", ";
                        }
                    }

                    if (embed.Description.Contains(","))
                    {
                        embed.Description = embed.Description.Substring(0, embed.Description.Length - 2);
                    }

                    if(playerSelections.Count > 0)
                    {
                        embed.Description += "\n\nCurrent Maps:";
                    }
                    foreach (var user in playerSelections.Keys)
                    {
                        embed.AddField(playerSelections[user], "Chosen by " + user.Username);
                    }

                    Task<DiscordMessage> taskMsg = ctx.RespondAsync(embed: embed);
                    if (PreviousMessage != null)
                    {
                        await PreviousMessage.DeleteAsync();
                    }
                    PreviousMessage = taskMsg.Result;

                    await taskMsg;
                }

                var userinput = await interactivity.WaitForMessageAsync(us => us.Author == us.Author, TimeSpan.FromSeconds(5000));
                if (PlayersInQueue.Contains(userinput.Result.Author) && !playerSelections.Keys.Contains(userinput.Result.Author) && userinput.Result.Content != ">getmaplist" && userinput.Result.Content != ">getmapnames")
                {
                    if(userinput.Result.Content.ToLower() == "random")
                    {
                        var randomresult = StartRandomMapSelection(ctx, enabledMapNames);
                        playerSelections.Add(userinput.Result.Author, randomresult);
                        updateEmbed = true;
                        continue;
                    }

                    var mapselectresult = GeneralUtil.ListContainsCaseInsensitive(allMapNamesIncludingDisabled, userinput.Result.Content);
                    if (mapselectresult)
                    {
                        playerSelections.Add(userinput.Result.Author, userinput.Result.Content);
                        updateEmbed = true;
                    }
                    else
                    {
                        await ctx.RespondAsync($"{userinput.Result.Author.Username}, your map selection was not found. Please try another name.");
                    }
                }
            }

            var mapselectindex = rng.Next(0, (teamSize * 2));
            return playerSelections.ElementAt(mapselectindex).Value;
        }

        private static async Task<string> StartLeaderPickMapSelection(CommandContext ctx)
        {
            var allMapNamesIncludingDisabled = await DatabaseModule.GetAllMapNames(true);
            await ctx.RespondAsync($"{PlayersInQueue[0].Nickname}, please type the name of a map to play!");
            var interactivity = ctx.Client.GetInteractivity();
            while (true)
            {
                var userinput = await interactivity.WaitForMessageAsync(us => us.Author == us.Author, TimeSpan.FromSeconds(5000));
                if (userinput.Result.Author == PlayersInQueue[0])
                {
                    var boolresult = GeneralUtil.ListContainsCaseInsensitive(allMapNamesIncludingDisabled, userinput.Result.Content);
                    if (boolresult)
                    {
                        return userinput.Result.Content;
                    }
                    else
                    {
                        await ctx.RespondAsync($"{userinput.Result.Author.Username}, your map selection was not found. Please try another name.");
                    }

                }
            }
        }

        private static string StartRandomMapSelection(CommandContext ctx, List<string> allMapNames)
        {
            var mapselectindex = rng.Next(0, allMapNames.Count);
            return allMapNames.ElementAt(mapselectindex);
        }

        #endregion

        private static async Task StartMap(CommandContext ctx, string mapID, string mapName, List<PlayerData> team1, List<PlayerData> team2, string team1Name, string team2Name, int teamSize)
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

            MatchConfigData configData;

            if (teamSize == 2)
            {
                configData = new MatchConfigData($"{lastmatchid + 1}", CurrentSpectatorIds, @$"workshop\{mapID}\{bspname}", team1Name, team2Name,
                    await DatabaseModule.GetPlayerSteamIDFromDiscordID(team1[0].ID), await DatabaseModule.GetPlayerSteamIDFromDiscordID(team1[1].ID),
                    await DatabaseModule.GetPlayerSteamIDFromDiscordID(team2[0].ID), await DatabaseModule.GetPlayerSteamIDFromDiscordID(team2[1].ID));
            }
            else // teamSize is 3
            {
                configData = new MatchConfigData($"{lastmatchid + 1}", CurrentSpectatorIds, @$"workshop\{mapID}\{bspname}", team1Name, team2Name,
                    await DatabaseModule.GetPlayerSteamIDFromDiscordID(team1[0].ID), await DatabaseModule.GetPlayerSteamIDFromDiscordID(team1[1].ID),
                    await DatabaseModule.GetPlayerSteamIDFromDiscordID(team1[2].ID), await DatabaseModule.GetPlayerSteamIDFromDiscordID(team2[0].ID), 
                    await DatabaseModule.GetPlayerSteamIDFromDiscordID(team2[1].ID), await DatabaseModule.GetPlayerSteamIDFromDiscordID(team2[2].ID));
            }

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
                await ctx.RespondAsync("The FTP server didn't let me write the match config. This program will now stop and you'll have to restart the queue. If you don't want to have to restart the queue then bug Squidski.");
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
            //await localrcon.RconCommand("sc1", $"host_workshop_map {mapID}");
            //await Task.Delay(2000);
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
                Title = $"Server Ready! Match ID: {lastmatchid + 1}",
                Description = $"Connect Information: `connect sc1.quintonswenski.com`",
                Timestamp = DateTime.UtcNow,
                ImageUrl = iteminfo[0].PreviewURL,
                Footer = new DiscordEmbedBuilder.EmbedFooter() { Text = "Map: " + mapName }
            };

            if(teamSize == 2)
            {
                matchembed.AddField($"Team {team1Name}", $"{team1[0].Name}\n{team1[1].Name}", true);
                matchembed.AddField($"Team {team2Name}", $"{team2[0].Name}\n{team2[1].Name}", true);
            }
            else // Team size is 3
            {
                matchembed.AddField($"Team {team1Name}", $"{team1[0].Name}\n{team1[1].Name}\n{team1[2].Name}", true);
                matchembed.AddField($"Team {team2Name}", $"{team2[0].Name}\n{team2[1].Name}\n{team2[2].Name}", true);
            }
            
            matchembed.AddField(iteminfo[0].Title, "---------------------------------", false);

            //matchembed.WithFooter(iteminfo[0].Title);

            await embedmessage.ModifyAsync(string.Empty, (DiscordEmbed)matchembed);

            await MatchPostGame(ctx, lastmatchid + 1, mapName, team1, team2, team1Name, team2Name, false, teamSize);
        }


        public static async Task MatchPostGame(CommandContext ctx, int matchId, string mapName, List<PlayerData> team1, List<PlayerData> team2, string team1Name, string team2Name, bool recalculate = false, int teamSize = 2)
        {
            int updatedmatchid = matchId;
            int currentSeconds = 0;
            while (true)
            {
                bool currentstatus;
                currentstatus = await DatabaseModule.HasMatchEnded(matchId);
                Console.WriteLine($"CHECK END LOOP STATUS: {currentstatus}");
                if (currentstatus)
                {
                    break;
                }
                currentstatus = await DatabaseModule.HasMatchEnded(matchId + 1);
                if (currentstatus)
                {
                    updatedmatchid = matchId + 1;
                    break;
                }

                await Task.Delay(FREQUENCY_TO_CHECK_FOR_POSTGAME * 1000);
                currentSeconds += FREQUENCY_TO_CHECK_FOR_POSTGAME;
                if(currentSeconds >= SECONDS_IN_ALLOW_BETTING)
                {
                    BettingAllowed = false;
                }
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

            CurrentGameState = MatchmakingState.DisplayingResults;

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

            var statsembed = await UpdateStatsPostGame(ctx, team1, team2, updatedmatchid, team1Name, team2Name, teamSize);

            //Award SquidCoin for playing or spectating.
            await AwardSquidCoin(team1[0].ID, SQUID_COIN_REWARD_PLAY);
            await AwardSquidCoin(team1[1].ID, SQUID_COIN_REWARD_PLAY);
            await AwardSquidCoin(team2[0].ID, SQUID_COIN_REWARD_PLAY);
            await AwardSquidCoin(team2[1].ID, SQUID_COIN_REWARD_PLAY);

            if(teamSize == 3)
            {
                await AwardSquidCoin(team1[2].ID, SQUID_COIN_REWARD_PLAY);
                await AwardSquidCoin(team2[2].ID, SQUID_COIN_REWARD_PLAY);
            }


            statsembed.Description = "SquidCoin Awards\n\n";
            statsembed.Description += team1[0].Name + ": +" + SQUID_COIN_REWARD_PLAY + DiscordEmoji.FromName(ctx.Client, SQUIDCOIN) + "(" + await DatabaseModule.GetPlayerSquidCoin(team1[0].ID) + ")\n";
            statsembed.Description += team1[1].Name + ": +" + SQUID_COIN_REWARD_PLAY + DiscordEmoji.FromName(ctx.Client, SQUIDCOIN) + "(" + await DatabaseModule.GetPlayerSquidCoin(team1[1].ID) + ")\n";
            if(teamSize == 3)
            {
                statsembed.Description += team1[2].Name + ": +" + SQUID_COIN_REWARD_PLAY + DiscordEmoji.FromName(ctx.Client, SQUIDCOIN) + "(" + await DatabaseModule.GetPlayerSquidCoin(team1[2].ID) + ")\n";
            }
            statsembed.Description += team2[0].Name + ": +" + SQUID_COIN_REWARD_PLAY + DiscordEmoji.FromName(ctx.Client, SQUIDCOIN) + "(" + await DatabaseModule.GetPlayerSquidCoin(team2[0].ID) + ")\n";
            statsembed.Description += team2[1].Name + ": +" + SQUID_COIN_REWARD_PLAY + DiscordEmoji.FromName(ctx.Client, SQUIDCOIN) + "(" + await DatabaseModule.GetPlayerSquidCoin(team2[1].ID) + ")\n";
            if (teamSize == 3)
            {
                statsembed.Description += team2[2].Name + ": +" + SQUID_COIN_REWARD_PLAY + DiscordEmoji.FromName(ctx.Client, SQUIDCOIN) + "(" + await DatabaseModule.GetPlayerSquidCoin(team2[2].ID) + ")\n";
            }

            //Add squidcoin for spectators (Should we verify they joined somehow?)
            for (int i = 0; i < CurrentSpectatorDiscordIds.Count; i++)
            {
                await AwardSquidCoin(CurrentSpectatorDiscordIds[i], SQUID_COIN_REWARD_SPECTATE);
                if(!recalculate)
                {
                    await DatabaseModule.AddSpectator(CurrentSpectatorDiscordIds[i], matchId);
                }

                statsembed.Description += CurrentSpectatorNames[i] + ": +" + SQUID_COIN_REWARD_SPECTATE + DiscordEmoji.FromName(ctx.Client, SQUIDCOIN) + "(" + await DatabaseModule.GetPlayerSquidCoin(CurrentSpectatorDiscordIds[i]) + ")\n";
            }

            taskMsg = ctx.RespondAsync(embed: statsembed);
            PreviousMessage = null;

            await taskMsg;

            //Award SquidCoin for bets
            if(Bets.Count > 0)
            {
                await HandleBetResults(ctx, mapName, matchId, recalculate);
            }

            await Reset();
            await localrcon.WakeRconServer("sc1");
        }

        public static async Task HandleBetResults(CommandContext ctx, string mapName, long matchId, bool recalculate)
        {
            string winnerText = currentWinner == null ? "TIE" : currentWinner.Value.TeamName;
            var betEmbed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = "Match Bet Results (Winner: " + winnerText + ")",
                Timestamp = DateTime.UtcNow,
                Footer = new DiscordEmbedBuilder.EmbedFooter() { Text = "Map: " + mapName }
            };
            if (currentWinner == null)
            {
                betEmbed.Description = "No bets are evaluated on a tie.";
            }
            else
            {
                foreach (string betUser in Bets.Keys)
                {
                    string idOfBet = Bets[betUser].UserToBetOn;
                    bool wonBet = currentWinner.Value.Player1.ID == idOfBet || currentWinner.Value.Player2.ID == idOfBet;
                    long change = wonBet ? ((long)(Bets[betUser].BetAmount * SQUID_COIN_BET_WIN)) - Bets[betUser].BetAmount : Bets[betUser].BetAmount;

                    if(!recalculate)
                    {
                        await DatabaseModule.AddBet(betUser, Bets[betUser].Name, idOfBet, Bets[betUser].BetAmount, matchId, wonBet);
                    }

                    await AwardSquidCoin(betUser, wonBet ? change : -change);

                    betEmbed.AddField(
                        Bets[betUser].Name +
                        (wonBet ? " won " : " lost ") + "bet.", 
                        (wonBet ? " Gained: " : " Lost: ") + change + "SC " + 
                        "Current: " + await DatabaseModule.GetPlayerSquidCoin(betUser));
                }
            }

            await ctx.RespondAsync(embed: betEmbed);
        }

        public static async Task RecalculateAllElo(CommandContext ctx, Dictionary<string, string> steamIdToPlayerId, int startFrom)
        {
            int lastMatchId = await DatabaseModule.GetLastMatchID();

            int currentMatchId = startFrom;
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

                    CurrentSpectatorDiscordIds = await DatabaseModule.GetSpectatorsFromMatch(currentMatchId);
                    CurrentSpectatorNames = new List<string>();
                    for (int i = 0; i < CurrentSpectatorDiscordIds.Count; i++)
                    {
                        CurrentSpectatorNames.Add("Some loser with the ID of: " + CurrentSpectatorDiscordIds[i]);
                    }
                    Bets = await DatabaseModule.GetBetsFromMatch(currentMatchId);

                    await MatchPostGame(ctx, currentMatchId, "WhoCares", playersTeam1, playersTeam2, teamNames[0], teamNames[1], true);

                }
                currentMatchId++;
            }

            await Reset();
            await ctx.RespondAsync("All matches recalculated.");
        }
        public static async Task<DiscordEmbedBuilder> UpdateStatsPostGame(CommandContext ctx, List<PlayerData> team1, List<PlayerData> team2, int matchId, string team1Name, string team2Name, int teamSize)
        {
            //Update stats and shit
            PlayerData t1p1Final = team1[0];
            PlayerData t1p2Final = team1[1];
            PlayerData t1p3Final = null;
            PlayerData t2p1Final = team2[0];
            PlayerData t2p2Final = team2[1];
            PlayerData t2p3Final = null;

            if(teamSize == 3)
            {
                t1p3Final = team1[2];
                t2p3Final = team2[2];
            }

            PlayerGameData team1player1Data = await DatabaseModule.GetPlayerStatsFromMatch(t1p1Final.ID, matchId, team1Name);
            PlayerGameData team1player2Data = await DatabaseModule.GetPlayerStatsFromMatch(t1p2Final.ID, matchId, team1Name);
            PlayerGameData team1Player3Data = null;
            PlayerGameData team2player1Data = await DatabaseModule.GetPlayerStatsFromMatch(t2p1Final.ID, matchId, team2Name);
            PlayerGameData team2player2Data = await DatabaseModule.GetPlayerStatsFromMatch(t2p2Final.ID, matchId, team2Name);
            PlayerGameData team2Player3Data = null;

            if(teamSize == 3)
            {
                team1Player3Data = await DatabaseModule.GetPlayerStatsFromMatch(t1p3Final.ID, matchId, team1Name);
                team2Player3Data = await DatabaseModule.GetPlayerStatsFromMatch(t2p3Final.ID, matchId, team2Name);
            }


            if (team1player1Data.TeamName != team1Name)
            {
                string temp = team1Name;
                team1Name = team2Name;
                team2Name = temp;
            }

            PlayerTeamMatch team1Data = new PlayerTeamMatch()
            {
                TeamName = team1Name,
                Player1 = t1p1Final,
                Player1MatchStats = team1player1Data,
                Player2 = t1p2Final,
                Player2MatchStats = team1player2Data,
                Player3 = t1p3Final,
                Player3MatchStats = team1Player3Data,
                RoundsWon = team1player1Data.RoundsWon
            };

            PlayerTeamMatch team2Data = new PlayerTeamMatch()
            {
                TeamName = team2Name,
                Player1 = t2p1Final,
                Player1MatchStats = team2player1Data,
                Player2 = t2p2Final,
                Player2MatchStats = team2player2Data,
                Player3 = t2p3Final,
                Player3MatchStats = team2Player3Data,
                RoundsWon = team2player1Data.RoundsWon
            };

            if(team1Data.RoundsWon == 0 && team2Data.RoundsWon == 0)
            {
                currentWinner = null;
                var badGameEmbed = new DiscordEmbedBuilder
                {
                    Color = new DiscordColor(0x3277a8),
                    Title = "CS:GO Match Finished: Game Cancelled",
                    Timestamp = DateTime.UtcNow,
                };

                return badGameEmbed;
            }

            if(team1player1Data.WonGame && team2player1Data.WonGame)
            {
                currentWinner = null;
            }
            else if(team1player1Data.WonGame)
            {
                currentWinner = team1Data;
            }
            else
            {
                currentWinner = team2Data;
            }

            int t1p1EloDiff;
            int t1p2EloDiff;
            int t1p3EloDiff = -1;
            int t2p1EloDiff;
            int t2p2EloDiff;
            int t2p3EloDiff = -1;

            // TODO: finish this for players, and add optional player 3

            (t1p1Final, t1p1EloDiff) = await UpdatePlayerStatsAndElo(t1p1Final, team1player1Data, team1Data, team2Data);
            (t1p2Final, t1p2EloDiff) = await UpdatePlayerStatsAndElo(t1p2Final, team1player2Data, team1Data, team2Data);
            (t2p1Final, t2p1EloDiff) = await UpdatePlayerStatsAndElo(t2p1Final, team2player1Data, team2Data, team1Data);
            (t2p2Final, t2p2EloDiff) = await UpdatePlayerStatsAndElo(t2p2Final, team2player2Data, team2Data, team1Data);
            
            if(teamSize == 3)
            {
                (t1p3Final, t1p3EloDiff) = await UpdatePlayerStatsAndElo(t1p3Final, team1Player3Data, team1Data, team2Data);
                (t2p3Final, t2p3EloDiff) = await UpdatePlayerStatsAndElo(t2p3Final, team2Player3Data, team2Data, team1Data);
            }

            string winner = team1player1Data.WonGame && team2player1Data.WonGame ? "TIE" : (team1player1Data.WonGame ? team1Name : team2Name);
            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = "CS:GO Match Finished: " + winner + " Won! (" + (team1player1Data.WonGame ? team1player1Data.RoundsWon : team2player1Data.RoundsWon) + " / " + (team1player1Data.WonGame ? team1player1Data.RoundsLost : team2player1Data.RoundsLost) + ")",
                Timestamp = DateTime.UtcNow,
            };

            string diffVal = "";


            diffVal = t1p1EloDiff > 0 ? "+" : "";
            embed.AddField(t1p1Final.Name + " Elo: " + (int)(t1p1Final.CurrentElo) + " (" + diffVal + t1p1EloDiff + ")", "Kills: " + team1player1Data.KillCount + " | Assists: " + team1player1Data.AssistCount + " | Deaths: " + team1player1Data.DeathCount);

            diffVal = t1p2EloDiff > 0 ? "+" : "";
            embed.AddField(t1p2Final.Name + " Elo: " + (int)(t1p2Final.CurrentElo) + " (" + diffVal + t1p2EloDiff + ")", "Kills: " + team1player2Data.KillCount + " | Assists: " + team1player2Data.AssistCount + " | Deaths: " + team1player2Data.DeathCount);
            
            if(teamSize == 3)
            {
                diffVal = t1p3EloDiff > 0 ? "+" : "";
                embed.AddField(t1p3Final.Name + " Elo: " + (int)(t1p3Final.CurrentElo) + " (" + diffVal + t1p3EloDiff + ")", "Kills: " + team1Player3Data.KillCount + " | Assists: " + team1Player3Data.AssistCount + " | Deaths: " + team1Player3Data.DeathCount);
            }

            diffVal = t2p1EloDiff > 0 ? "+" : "";
            embed.AddField(t2p1Final.Name + " Elo: " + (int)(t2p1Final.CurrentElo) + " (" + diffVal + t2p1EloDiff + ")", "Kills: " + team2player1Data.KillCount + " | Assists: " + team2player1Data.AssistCount + " | Deaths: " + team2player1Data.DeathCount);

            diffVal = t2p2EloDiff > 0 ? "+" : "";
            embed.AddField(t2p2Final.Name + " Elo: " + (int)(t2p2Final.CurrentElo) + " (" + diffVal + t2p2EloDiff + ")", "Kills: " + team2player2Data.KillCount + " | Assists: " + team2player2Data.AssistCount + " | Deaths: " + team2player2Data.DeathCount);

            if (teamSize == 3)
            {
                diffVal = t2p3EloDiff > 0 ? "+" : "";
                embed.AddField(t2p3Final.Name + " Elo: " + (int)(t2p3Final.CurrentElo) + " (" + diffVal + t2p3EloDiff + ")", "Kills: " + team2Player3Data.KillCount + " | Assists: " + team2Player3Data.AssistCount + " | Deaths: " + team2Player3Data.DeathCount);
            }

            return embed;
        }

        public static async Task<(PlayerData, int)> UpdatePlayerStatsAndElo(PlayerData finalStats, PlayerGameData pGameData, PlayerTeamMatch playerTeam, PlayerTeamMatch enemyTeam)
        {

            float playerElo = Match.GetUpdatedPlayerEloWithMatchData(finalStats, playerTeam, enemyTeam);
            int playerEloDiff = (int)(playerElo - finalStats.CurrentElo);
            finalStats.CurrentElo = playerElo;
            finalStats.UpdateWithGameData(pGameData);
            await DatabaseModule.DeletePlayerStats(finalStats.ID);
            await DatabaseModule.AddPlayerMatchmakingStat(finalStats);
            return (finalStats, playerEloDiff);
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

        private static string GetEmoteRepresentation(this MapSelectionType type)
        {
            return ":" + numbersWritten[((int)type) + 1] + ":"; 
        }

        private static string GetDialogueDisplay(this MapSelectionType type)
        {
            switch (type)
            {
                case MapSelectionType.RandomPoolVeto:
                    return "Random Pool Veto";
                case MapSelectionType.AllPick:
                    return "All Pick/Random Selection";
                case MapSelectionType.LeaderPick:
                    return "Queue Leader Pick";
                case MapSelectionType.CompletelyRandomPick:
                    return "MechaSquidski's Pick";
                default:
                    return string.Empty;
            }
        }
    }
}
