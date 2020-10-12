using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static List<DiscordMember> PlayersInQueue = new List<DiscordMember>();
        public static DiscordMessage PreviousMessage = null;
        public static bool MatchSelectionOngoing = false;
        public static bool MatchFull = false;
        public static bool CaptainPick = false;
        public static bool SelectingMap = false;
        private static Dictionary<DiscordMember, PlayerData> discordPlayerToGamePlayer = new Dictionary<DiscordMember, PlayerData>();
        private static Dictionary<PlayerData, DiscordMember> gamePlayerToDiscordPlayer = new Dictionary<PlayerData, DiscordMember>();

        public static void Reset()
        {
            SelectingMap = false;
            MatchFull = false;
            MatchSelectionOngoing = false;
            PlayersInQueue.Clear();
            discordPlayerToGamePlayer.Clear();
            gamePlayerToDiscordPlayer.Clear();
            PreviousMessage = null;
        }

        public static async Task JoinQueue(CommandContext ctx, DiscordMember member)
        {
            if(MatchFull)
            {
                return;
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
                MatchFull = true;
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
                    embed.AddField(PlayersInQueue[i].DisplayName, "Elo: " + discordPlayerToGamePlayer[PlayersInQueue[i]].CurrentElo);
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
                List<string> mapNames = new List<string>()
                {
                    "Eternity",
                    "Bell",
                    "Pitstop",
                    "Turnpike",
                    "Station",
                    "Rio",
                    "The Spooky Manor",
                    "Akihabara",
                    "Terraza",
                    "Malice",
                    "Austria",
                    "Chalice",
                    "CleanUp",
                    "Breach",
                    "Boyard",
                    "Gongji",
                    "Beerhouse",
                    "Chlore"
                };

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
                        await StartMap(mapSelectionResult, team1: new List<PlayerData>() { Team1Player1, Team1Player2 }, team2: new List<PlayerData>() { Team2Player1, Team2Player2 }, team1Name, team2Name);
                    }
                } while (true);

            }
        }

        private static async Task StartMap(string mapName, List<PlayerData> team1, List<PlayerData> team2, string team1Name, string team2Name)
        {
            MatchConfigData configData = new MatchConfigData()
            {
                matchid = "example_match",
                num_maps = 1,
                players_per_team = 2,
                min_players_to_ready = 2,
                min_spectators_to_ready = 0,
                skip_veto = true,
                veto_first = "team1",
                side_type = "standard",
                spectators = new PlayerJsonData()
                {
                    players = new List<string>()
                    {

                    }
                },
                maplist = new List<string>()
                {
                    mapName
                },
                favored_percentage_team1 = 65,
                favored_percentage_text = "HLTV Bets",
                team1 = new TeamJsonData()
                {
                    name = team1Name,
                    tag = team1Name,
                    flag = "FR",
                    logo = "nv",
                    players = new List<string>()
                    {
                        await DatabaseModule.GetPlayerSteamIDFromDiscordID(team1[0].ID),
                        await DatabaseModule.GetPlayerSteamIDFromDiscordID(team1[1].ID),
                    }
                },
                team2 = new TeamJsonData()
                {
                    name = team2Name,
                    tag = team2Name,
                    flag = "SE",
                    logo = "fntc",
                    players = new List<string>()
                    {
                        await DatabaseModule.GetPlayerSteamIDFromDiscordID(team2[0].ID),
                        await DatabaseModule.GetPlayerSteamIDFromDiscordID(team2[1].ID),
                    }
                },
                cvars = new CvarsJsonData()
                {
                    hostname = "Match server #1"
                }
            };

            string json = JsonConvert.SerializeObject(configData, Formatting.Indented);
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
            //for (int i = 0; i < mapNames.Count; i += 2)
            //{
            //    embed.AddField(":" + numbersWritten[i + 1] + ":" + mapNames[i] + "\t\t\t" + ":" + numbersWritten[i + 2] + ":" + mapNames[i + 1], "Vote to Remove");
            //}
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
                await PreviousMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":" + numbersWritten[i + 1] + ":"));
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
                    var reacteds = await PreviousMessage.GetReactionsAsync(DiscordEmoji.FromName(ctx.Client, ":" + numbersWritten[i + 1] + ":"));
                    if (reacteds.Count > 1)
                    {
                        for (int j = 0; j < reacteds.Count; j++)
                        {
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
                new Tuple<PlayerData, PlayerData>(dataEntries[0], dataEntries[2]),
                new Tuple<PlayerData, PlayerData>(dataEntries[1], dataEntries[3])
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
            float MVP_SCALING_FACTOR = 2;

            const float WIN_REDUCE = 1.5f;
            const float ROUND_REDUCE = 1.2f;
            const float KILL_REDUCE = 0.8f;
            const float ASSIST_REDUCE = 0.2f;
            const float DEATH_REDUCE = 0.6f;
            const float HEADSHOT_REDUCE = 0.4f;
            const float MVP_REDUCE = 0.25f;

            float currentPlayerElo = player.CurrentElo;
            float friendlyElo = friendlyTeam.CombinedElo / 2f;
            float enemyElo = enemyTeam.CombinedElo / 2f;
            float roundsWon = friendlyTeam.RoundsWon;
            float roundsLost = enemyTeam.RoundsWon;
            int killCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.KillCount : friendlyTeam.Player2MatchStats.KillCount;
            int assistCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.AssistCount : friendlyTeam.Player2MatchStats.AssistCount;
            int deathCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.DeathCount : friendlyTeam.Player2MatchStats.DeathCount;
            int headshotCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.Headshots : friendlyTeam.Player2MatchStats.Headshots;
            int mvpCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.MVPs : friendlyTeam.Player2MatchStats.MVPs;

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

            float mvpElo = 0;
            for (int i = 0; i < mvpCount; i++)
            {
                mvpElo += MVP_SCALING_FACTOR * (1 - expected);
            }
            mvpElo *= MVP_REDUCE;


            float finalResult = currentPlayerElo
                + roundElo
                + winElo
                + killElo
                + assistElo
                + deathElo
                + headshotElo
                + mvpElo;
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
        public int TotalMVPCount;

        public int TotalGames
        {
            get
            {
                return TotalGamesWon + TotalGamesLost;
            }
        }
    }

    public class PlayerGameData
    {
        public PlayerData Self;
        public PlayerData Teammate;
        public int TeamNumber;

        public int RoundsWon;
        public int RoundsLost;

        public int KillCount;
        public int AssistCount;
        public int DeathCount;
        public int Headshots;
        public int MVPs;

        public bool WonGame;

        public PlayerGameData() { }
        public PlayerGameData(PlayerGameData one, PlayerGameData two)
        {
            KillCount = one.KillCount + two.KillCount;
            AssistCount = one.AssistCount + two.AssistCount;
            DeathCount = one.DeathCount + two.DeathCount;
            Headshots = one.Headshots + two.Headshots;
            MVPs = one.MVPs + two.MVPs;

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
