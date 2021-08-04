using SquidBot_Sharp.Utilities;
using System;
using System.Collections.Generic;

namespace SquidBot_Sharp.Modules
{


    public class MatchConfigData
    {
        public string matchid;
        public int num_maps;
        public int players_per_team;
        public int min_players_to_ready;
        public int min_spectators_to_ready;
        public bool skip_veto;
        public string veto_first;
        public string side_type;
        public PlayerJsonData spectators;
        public List<string> maplist;
        public int favored_percentage_team1;
        public string favored_percentage_text;
        public TeamJsonData team1;
        public TeamJsonData team2;
        public CvarsJsonData cvars;

        public MatchConfigData(string matchid, List<string> spectatorids, string mapstring, string team1name, string team2name,
            string team1player1steamid, string team1player2steamid, string team2player1steamid, string team2player2steamid)
        {
            this.matchid = matchid;
            num_maps = 1;
            players_per_team = 2;
            min_players_to_ready = 2;
            min_spectators_to_ready = 0;
            skip_veto = true;
            veto_first = "team1";
            side_type = "standard";
            spectators = new PlayerJsonData()
            {
                players = spectatorids
            };
            maplist = new List<string>()
            {
                mapstring
            };
            favored_percentage_team1 = 65;
            favored_percentage_text = "HLTV Bets";
            this.team1 = new TeamJsonData()
            {
                name = team1name,
                tag = team1name,
                players = new List<string>()
                {
                    GeneralUtil.SteamIDFrom64ToLegacy(team1player1steamid),
                    GeneralUtil.SteamIDFrom64ToLegacy(team1player2steamid),
                }
            };
            this.team2 = new TeamJsonData()
            {
                name = team2name,
                tag = team2name,
                players = new List<string>()
                {
                    GeneralUtil.SteamIDFrom64ToLegacy(team2player1steamid),
                    GeneralUtil.SteamIDFrom64ToLegacy(team2player2steamid),
                }
            };
            cvars = new CvarsJsonData()
            {
                hostname = "Match server #1"
            };
        }

        public MatchConfigData(string matchid, List<string> spectatorids, string mapstring, string team1name, string team2name, 
            string team1player1steamid, string team1player2steamid, string team1player3steamid, string team2player1steamid, 
            string team2player2steamid, string team2player3steamid)
        {
            this.matchid = matchid;
            num_maps = 1;
            players_per_team = 3;
            min_players_to_ready = 3;
            min_spectators_to_ready = 0;
            skip_veto = true;
            veto_first = "team1";
            side_type = "standard";
            spectators = new PlayerJsonData()
            {
                players = spectatorids
            };
            maplist = new List<string>()
            {
                mapstring
            };
            favored_percentage_team1 = 65;
            favored_percentage_text = "HLTV Bets";
            this.team1 = new TeamJsonData()
            {
                name = team1name,
                tag = team1name,
                players = new List<string>()
                {
                    GeneralUtil.SteamIDFrom64ToLegacy(team1player1steamid),
                    GeneralUtil.SteamIDFrom64ToLegacy(team1player2steamid),
                    GeneralUtil.SteamIDFrom64ToLegacy(team1player3steamid),
                }
            };
            this.team2 = new TeamJsonData()
            {
                name = team2name,
                tag = team2name,
                players = new List<string>()
                {
                    GeneralUtil.SteamIDFrom64ToLegacy(team2player1steamid),
                    GeneralUtil.SteamIDFrom64ToLegacy(team2player2steamid),
                    GeneralUtil.SteamIDFrom64ToLegacy(team2player3steamid),
                }
            };
            cvars = new CvarsJsonData()
            {
                hostname = "Match server #1"
            };
        }
    }

    public class PlayerJsonData
    {
        public List<string> players;
    }

    public class TeamJsonData
    {
        public string name;
        public string tag;
        public string flag;
        public string logo;
        public List<string> players;
    }

    public class CvarsJsonData
    {
        public string hostname;
    }

    public class PlayerData
    {
        //Cosmetic
        public string Name;

        //Data
        public string ID;
        public float CurrentElo;

        public uint TotalGamesWon;
        public uint TotalGamesLost;
        public ulong TotalRoundsWon;
        public ulong TotalRoundsLost;
        public ulong TotalKillCount;
        public ulong TotalAssistCount;
        public ulong TotalDeathCount;
        public ulong TotalHeadshotCount;

        public uint TotalGames
        {
            get
            {
                return TotalGamesWon + TotalGamesLost;
            }
        }

        public void UpdateWithGameData(PlayerGameData gameData)
        {
            if (gameData.WonGame)
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
        public uint TeamNumber;

        public ulong RoundsWon;
        public ulong RoundsLost;

        public ulong KillCount;
        public ulong AssistCount;
        public ulong DeathCount;
        public ulong Headshots;

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
        public string TeamName;
        public ulong RoundsWon;

        public PlayerData Player1;
        public PlayerData Player2;
        public PlayerData Player3;

        public PlayerGameData Player1MatchStats;
        public PlayerGameData Player2MatchStats;
        public PlayerGameData Player3MatchStats;

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

    public static class Match
    {
        private const int STARTING_ELO = 1000;

        public static List<PlayerData[]> GetMatchup(PlayerData[] dataEntries)
        {
            if (dataEntries.Length != 4 && dataEntries.Length != 6)
            {
                Console.WriteLine("ERROR: Incorrect number of player entries sent");
                return null;
            }

            Array.Sort(dataEntries, (x, y) => { return x.CurrentElo.CompareTo(y.CurrentElo); });

            if (dataEntries.Length == 4)
            {
                PlayerData[] team1 = { dataEntries[0], dataEntries[3] };
                PlayerData[] team2 = { dataEntries[1], dataEntries[2] };
                return new List<PlayerData[]> { team1, team2 };
            }
            else // Number is 6
            {
                PlayerData[] team1 = { dataEntries[0], dataEntries[4], dataEntries[5] };
                PlayerData[] team2 = { dataEntries[1], dataEntries[2], dataEntries[3] };
                return new List<PlayerData[]> { team1, team2 };
            }           
        }

        public static float GetUpdatedPlayerEloWithMatchData(PlayerData player, PlayerTeamMatch friendlyTeam, PlayerTeamMatch enemyTeam)
        {
            const float ELO_SCALING_FACTOR = 400;

            const float BEGINNER_GAME_COUNT = 15;

            float allGamesPlayed = player.TotalGames - 1;
            float scalingRatio = 1 - MathF.Min(0.5f, allGamesPlayed / BEGINNER_GAME_COUNT);

            float GAME_WIN_SCALING_FACTOR = 20 * scalingRatio;
            float ROUND_SCALING_FACTOR = 10 * scalingRatio;
            float KILL_SCALING_FACTOR = 5 * scalingRatio;
            float ASSIST_SCALING_FACTOR = 1 * scalingRatio;
            float DEATH_SCALING_FACTOR = 4 * scalingRatio;
            float HEADSHOT_SCALING_FACTOR = 2 * scalingRatio;

            const float WIN_REDUCE = 1.5f;
            const float ROUND_REDUCE_WIN = 0.5f;
            const float ROUND_REDUCE_LOSE = 0.48f;
            const float KILL_REDUCE = 0.6f;
            const float ASSIST_REDUCE = 0.2f;
            const float DEATH_REDUCE = 0.65f;
            const float HEADSHOT_REDUCE = 0.2f;

            float currentPlayerElo = player.CurrentElo;
            float friendlyElo = friendlyTeam.CombinedElo / 2f;
            float enemyElo = enemyTeam.CombinedElo / 2f;
            float roundsWon = friendlyTeam.RoundsWon;
            float roundsLost = enemyTeam.RoundsWon;
            ulong killCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.KillCount : friendlyTeam.Player2MatchStats.KillCount;
            ulong assistCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.AssistCount : friendlyTeam.Player2MatchStats.AssistCount;
            ulong deathCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.DeathCount : friendlyTeam.Player2MatchStats.DeathCount;
            ulong headshotCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.Headshots : friendlyTeam.Player2MatchStats.Headshots;

            float expected = 1 / (1 + MathF.Pow(10, ((enemyElo - player.CurrentElo) / ELO_SCALING_FACTOR)));

            bool tie = roundsWon == roundsLost;
            bool won = roundsWon > roundsLost || tie;
            float winElo = 0;
            winElo += GAME_WIN_SCALING_FACTOR * ((won ? 1 : 0) - expected);
            winElo *= WIN_REDUCE;

            float roundEloWin = 0;
            for (int i = 0; i < roundsWon; i++)
            {
                roundEloWin += ROUND_SCALING_FACTOR * (1 - expected);
            }
            roundEloWin *= ROUND_REDUCE_WIN;

            float roundEloLose = 0;
            for (int i = 0; i < roundsLost; i++)
            {
                roundEloLose += ROUND_SCALING_FACTOR * (0 - expected);
            }
            roundEloLose *= ROUND_REDUCE_LOSE;

            float roundElo = roundEloWin + roundEloLose;

            float killElo = 0;
            for (ulong i = 0; i < killCount; i++)
            {
                killElo += KILL_SCALING_FACTOR * (1 - expected);
            }
            killElo *= KILL_REDUCE;

            float assistElo = 0;
            for (ulong i = 0; i < assistCount; i++)
            {
                assistElo += ASSIST_SCALING_FACTOR * (1 - expected);
            }
            assistElo *= ASSIST_REDUCE;

            float deathElo = 0;
            for (ulong i = 0; i < deathCount; i++)
            {
                deathElo += DEATH_SCALING_FACTOR * (0 - expected);
            }
            deathElo *= DEATH_REDUCE;

            float headshotElo = 0;
            for (ulong i = 0; i < headshotCount; i++)
            {
                headshotElo += HEADSHOT_SCALING_FACTOR * (1 - expected);
            }
            headshotElo *= HEADSHOT_REDUCE;

            float changeAmount = roundElo
                + winElo
                + killElo
                + assistElo
                + deathElo
                + headshotElo;

            if (won)
            {
                changeAmount = MathF.Max(1, changeAmount);
            }
            if (tie)
            {
                changeAmount = MathF.Max(0, changeAmount);
            }

            float finalResult = currentPlayerElo + changeAmount;


            return finalResult;
        }
    }
}
