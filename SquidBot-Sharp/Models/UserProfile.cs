using DSharpPlus.Entities;
using System;

namespace SquidBot_Sharp.Models
{
    [Serializable] public class UserProfile
    {
        public int Version { get; set; }
        public TimeZoneInfo TimeZone { get; set; }
        public ulong UserID { get; set; }
        public string ProfileName { get; set; }
    }
}
