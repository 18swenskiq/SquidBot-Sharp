using SquidBot_Sharp.Utilities;
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
}
