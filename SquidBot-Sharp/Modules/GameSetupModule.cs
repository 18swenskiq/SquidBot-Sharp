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
