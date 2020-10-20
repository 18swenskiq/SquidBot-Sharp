using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SquidBot_Sharp.Models;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using SquidBot_Sharp.Modules;

namespace SquidBot_Sharp.Commands
{
    class SteamWorkshopCMD : BaseCommandModule
    {
        [Command("searchworkshopcsgo"), Description("Search the CSGO workshop")]
        [Aliases("swscsgo", "swscs")]
        [Cooldown(1, 10, CooldownBucketType.User)]
        public async Task SearchWorkshopCSGO(CommandContext ctx, [RemainingText] string SearchQuery)
        {
            await SearchWorkshop(ctx, SearchQuery, 730);
        }

        [Command("searchworkshoptf2"), Description("Search the TF2 workshop")]
        [Aliases("swstf2", "swstf")]
        [Cooldown(1, 10, CooldownBucketType.User)]
        public async Task SearchWorkshopTF2(CommandContext ctx, [RemainingText] string SearchQuery)
        {
            await SearchWorkshop(ctx, SearchQuery, 440);
        }

        [Command("searchworkshopl4d2"), Description("Search the L4D2 workshop")]
        [Aliases("swsl4d2", "swslfd2")]
        [Cooldown(1, 10, CooldownBucketType.User)]
        public async Task SearchWorkshopL4D2(CommandContext ctx, [RemainingText] string SearchQuery)
        {
            await SearchWorkshop(ctx, SearchQuery, 550);
        }

        [Command("searchworkshopportal2"), Description("Search the Portal 2 workshop")]
        [Aliases("swsp2", "swsportal2")]
        [Cooldown(1, 10, CooldownBucketType.User)]
        public async Task SearchWorkshopPortal2(CommandContext ctx, [RemainingText] string SearchQuery)
        {
            await SearchWorkshop(ctx, SearchQuery, 620);
        }

        [Command("searchworkshopgmod"), Description("Search the Garry's Mod workshop")]
        [Aliases("swsgmod", "swsgm")]
        [Cooldown(1, 10, CooldownBucketType.User)]
        public async Task SearchWorkshopGMOD(CommandContext ctx, [RemainingText] string SearchQuery)
        {
            await SearchWorkshop(ctx, SearchQuery, 4000);
        }

        private async Task SearchWorkshop(CommandContext ctx, string SearchQuery, int gameid)
        {

            await ctx.RespondAsync("Querying workshop... (This might take a minute)");
            using (dynamic SteamWorkshopQuery = WebAPI.GetAsyncInterface( "IPublishedFileService", SettingsFile.steamwebapikey))
            {
                SteamWorkshopQuery.Timeout = TimeSpan.FromSeconds(5);

                var searchResultIDList = new List<string>();
                int totalSearchResults = 0;
                string next_cursor = "";

                KeyValue kvResults = await SteamWorkshopQuery.QueryFiles(
                    query_type: 0,
                    cursor: "*",
                    appid: gameid,
                    search_text: SearchQuery,
                    method: HttpMethod.Get
                 );

                    // Get the total search results
                foreach(var child in kvResults.Children)
                {
                    if (child.Name == "total")
                    {
                        totalSearchResults = Int32.Parse(child.Value);
                        break;
                    }
                }

                // Clamp results
                if (totalSearchResults > 5) totalSearchResults = 5;

                while(searchResultIDList.Count < totalSearchResults)
                {
                    foreach (var child in kvResults.Children)
                    {
                        if (child.Name == "publishedfiledetails")
                        {
                            foreach (var child2 in child.Children)
                            {
                                foreach (var child3 in child2.Children)
                                {
                                    if (child3.Name == "publishedfileid")
                                    {
                                        if (!searchResultIDList.Contains(child3.Value))
                                        {
                                            searchResultIDList.Add(child3.Value);
                                        }
                                    }
                                }
                            }
                        }
                        if (child.Name == "next_cursor") next_cursor = child.Value;
                    }
                    kvResults = await SteamWorkshopQuery.QueryFiles(
                        query_type: 0,
                        appid: gameid,
                        cursor: next_cursor,
                        search_text: SearchQuery,
                        method: HttpMethod.Get
                    );
                }


                var listresults = await SteamWorkshopModule.GetPublishedFileDetails(searchResultIDList);

                var embed = new DiscordEmbedBuilder
                {
                    Title = $"`{SearchQuery}` search results",
                    Color = new DiscordColor(0x171A21)
                };

                foreach(var workshopitem in listresults)
                {
                    if (gameid == 730 && workshopitem.FileURL == null) continue;
                    embed.AddField($"{workshopitem.Title}", $"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopitem.PublishedFileID}");
                }

                await ctx.RespondAsync(embed: embed);
            }
        }
    }
}
