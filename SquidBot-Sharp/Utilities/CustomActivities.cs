using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SquidBot_Sharp.Utilities
{
    public class CustomActivities
    {
        private List<ActivityPayload> Activities { get; set; }
        private int Index { get; set; }

        public CustomActivities()
        {
            // Here we will put all of our activity payloads
            Index = 0;
            Activities = new List<ActivityPayload>();
            Activities.Add(new ActivityPayload { ActType = ActivityType.Watching, Status = "NUMBER_GUILDS servers" });
            Activities.Add(new ActivityPayload { ActType = ActivityType.ListeningTo, Status = "Ketal's quotes" });
            Activities.Add(new ActivityPayload { ActType = ActivityType.ListeningTo, Status = "plans on how to take over the world" });
            Activities.Add(new ActivityPayload { ActType = ActivityType.Watching, Status = "deez nuts" });
            Activities.Add(new ActivityPayload { ActType = ActivityType.Watching, Status = "speef money" });
            Activities.Add(new ActivityPayload { ActType = ActivityType.Watching, Status = "fuck like sex or fuck like fuck off" });
            Activities.Add(new ActivityPayload { ActType = ActivityType.Playing, Status = "invite me with >invite!" });
        }

        public ActivityPayload GetNextActivity()
        {
            var returnact = Activities[Index];
            if(Index == Activities.Count - 1)
            {
                Index = 0;
            }
            else
            {
                Index += 1;
            }
            return returnact;

        }

        
    }

    public class ActivityPayload
    {
        public string Status { get; set; }
        public ActivityType ActType { get; set; }
    }
}
