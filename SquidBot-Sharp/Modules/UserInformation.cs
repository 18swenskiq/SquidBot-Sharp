using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SquidBot_Sharp.Modules
{
    public class UserInformation
    {
        public DiscordUser Info { get; set; }
        public TimeZoneInfo UserTimeZone { get; set; }
    }
}
