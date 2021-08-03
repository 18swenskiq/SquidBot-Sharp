using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Modules
{
    enum BetType
    {
        MISC = 0,
        CSGOMATCH = 1
    }

    public class BettingModule
    {  
        public static DiscordEmbed GetBetEventEmbed(BetEvent inputevent)
        {
            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = inputevent.Title,
                Description = $"{inputevent.Choice1} vs {inputevent.Choice2} at {inputevent.EventName}",
                Timestamp = DateTime.UtcNow,
                Footer = new DiscordEmbedBuilder.EmbedFooter() { Text = "squidcoin squidcoin squidcoin squidcoin" }
            };
            embed.AddField("1:", inputevent.Choice1, false);
            embed.AddField("2:", inputevent.Choice2, false);
            return embed;
        }

        public static List<BetEvent> RemoveBetEventDuplicates(List<BetEvent> inputlist)
        {
            List<BetEvent> returnlist = new List<BetEvent>();
            List<int> indicesfound = new List<int>();

            foreach(var item in inputlist)
            {
                if(!indicesfound.Contains(item.Index))
                {
                    returnlist.Add(item);
                    indicesfound.Add(item.Index);
                }
                else
                {
                    continue;
                }
            }

            return returnlist;
        }

        public static List<int> GetBetIndices(List<BetEvent> inputlist)
        {
            List<int> retlist = new List<int>();

            foreach (var bet in inputlist)
            {
                retlist.Add(bet.Index);
            }

            return retlist;
        }

        public async Task AddSquidBetEvent(string title, int type, int active, string eventname, string choice1, string choice2, int result)
        {
            var sbevent = new BetEvent
            {
                Title = title,
                Type = type,
                Active = active,
                EventName = eventname,
                Choice1 = choice1,
                Choice2 = choice2,
                Result = result
            };

            await DatabaseModule.AddSquidBetEvent(sbevent);
        }
    }

    public class BetEvent
    {
        public string Title { get; set; }
        public int Type { get; set; }
        public int Active { get; set; }
        public string EventName { get; set; }
        public string Choice1 { get; set; }
        public string Choice2 { get; set; }
        public int Result { get; set; }
        public int Index { get; set; }
    }
}
