using DSharpPlus.Entities;
using System;

namespace SquidBot_Sharp.Modules
{
    public class UserInformation
    {
        public DiscordUser Info { get; set; }
        public TimeZoneInfo UserTimeZone { get; set; }
    }
}
