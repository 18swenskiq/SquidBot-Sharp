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

        public MatchConfigData(string matchid, List<string> spectatorids, string mapstring, string team1name, string team2name, string team1player1steamid, string team1player2steamid, string team2player1steamid, string teams2player2steamid)
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
                    GeneralUtil.SteamIDFrom64ToLegacy(teams2player2steamid),
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

        public int TotalGamesWon;
        public int TotalGamesLost;
        public int TotalRoundsWon;
        public int TotalRoundsLost;
        public int TotalKillCount;
        public int TotalAssistCount;
        public int TotalDeathCount;
        public int TotalHeadshotCount;

        public int TotalGames
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
        public string TeamName;
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
            const float KILL_REDUCE = 0.4f;
            const float ASSIST_REDUCE = 0.2f;
            const float DEATH_REDUCE = 0.45f;
            const float HEADSHOT_REDUCE = 0.2f;

            float currentPlayerElo = player.CurrentElo;
            float friendlyElo = friendlyTeam.CombinedElo / 2f;
            float enemyElo = enemyTeam.CombinedElo / 2f;
            float roundsWon = friendlyTeam.RoundsWon;
            float roundsLost = enemyTeam.RoundsWon;
            int killCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.KillCount : friendlyTeam.Player2MatchStats.KillCount;
            int assistCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.AssistCount : friendlyTeam.Player2MatchStats.AssistCount;
            int deathCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.DeathCount : friendlyTeam.Player2MatchStats.DeathCount;
            int headshotCount = friendlyTeam.Player1.ID == player.ID ? friendlyTeam.Player1MatchStats.Headshots : friendlyTeam.Player2MatchStats.Headshots;

            float expected = 1 / (1 + MathF.Pow(10, ((enemyElo - player.CurrentElo) / ELO_SCALING_FACTOR)));

            float winElo = 0;
            winElo += GAME_WIN_SCALING_FACTOR * ((roundsWon > roundsLost ? 1 : 0) - expected);
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

            float finalResult = currentPlayerElo
                + roundElo
                + winElo
                + killElo
                + assistElo
                + deathElo
                + headshotElo;
            return finalResult;
        }
    }
}
