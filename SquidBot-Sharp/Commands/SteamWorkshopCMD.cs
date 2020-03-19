using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SquidBot_Sharp.Models;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Commands
{
    class SteamWorkshopCMD
    {
        [Command("searchworkshopcsgo"), Description("Search the CSGO workshop")]
        [Aliases("swscsgo")]
        public async Task SearchWorkshopCSGO(CommandContext ctx, [RemainingText] string SearchQuery)
        {
            await SearchWorkshop(ctx, SearchQuery, 730);
        }

        [Command("searchworkshoptf2"), Description("Search the TF2 workshop")]
        [Aliases("swstf2")]
        public async Task SearchWorkshopTF2(CommandContext ctx, [RemainingText] string SearchQuery)
        {
            await SearchWorkshop(ctx, SearchQuery, 440);
        }

        [Command("searchworkshopl4d2"), Description("Search the L4D2 workshop")]
        [Aliases("swsl4d2")]
        public async Task SearchWorkshopL4D2(CommandContext ctx, [RemainingText] string SearchQuery)
        {
            await SearchWorkshop(ctx, SearchQuery, 550);
        }

        [Command("searchworkshopportal2"), Description("Search the Portal 2 workshop")]
        [Aliases("swsp2")]
        public async Task SearchWorkshopPortal2(CommandContext ctx, [RemainingText] string SearchQuery)
        {
            await SearchWorkshop(ctx, SearchQuery, 620);
        }

        [Command("searchworkshopgmod"), Description("Search the Garry's Mod workshop")]
        [Aliases("swsgmod")]
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

                    for(int i = 0; i < totalSearchResults; i++)
                    {
                        foreach (var child in kvResults.Children)
                        {
                            if (child.Name == "publishedfiledetails")
                            {
                                foreach (var child2 in child.Children)
                                {
                                    foreach (var child3 in child2.Children)
                                    {
                                        if (child3.Name == "publishedfileid") searchResultIDList.Add(child3.Value);
                                    }
                                }
                            }
                            if (child.Name == "next_cursor") next_cursor = child.Value;
                        }
                        if(i + 1 < totalSearchResults)
                        {
                            kvResults = await SteamWorkshopQuery.QueryFiles(
                                query_type: 0,
                                appid: gameid,
                                cursor: next_cursor,
                                search_text: SearchQuery,
                                method: HttpMethod.Get
                            );
                        }
                    }

                    //Console.WriteLine(searchResultIDList);
                    var listresults = await GetPublishedFileDetails(ctx, searchResultIDList);

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


        private async Task<List<WorkshopReturnInformation>> GetPublishedFileDetails(CommandContext ctx, List<string> itemids)
        {
            var kvlist = new List<KeyValue>();
            using (dynamic SteamPublishedFileDetails = WebAPI.GetAsyncInterface("ISteamRemoteStorage"))
            {
                var basedict = new Dictionary<string, object>
                {
                    ["itemcount"] = 1,
                    ["method"] = HttpMethod.Post,
                    ["publishedfileids[0]"] = ""
                };
                foreach(var itemid in itemids)
                {
                    basedict["publishedfileids[0]"] = itemid;
                    try
                    {
                        KeyValue kvResults = await SteamPublishedFileDetails.GetPublishedFileDetails(basedict);
                        kvlist.Add(kvResults);
                    }
                    catch (Exception ex)
                    {
                        await ctx.RespondAsync($"Could not get the details of a published file due to: {ex.Message}. You might want to contact Squidski");
                        return null;
                    }
                }
            }
            // Parse out the information we want
            var returninfo = new List<WorkshopReturnInformation>();
            foreach(var kvitem in kvlist)
            {
                foreach(var child in kvitem.Children)
                {
                    foreach(var child2 in child.Children)
                    {
                        if(child2.Name == "0")
                        {
                            var thisreturninfo = new WorkshopReturnInformation();
                            // TODO: If we get duplicate resutls from the API, call the API again for more results

                            foreach(var child3 in child2.Children)
                            {
                                switch(child3.Name)
                                {
                                    case "publishedfileid":
                                        thisreturninfo.PublishedFileID = child3.Value;
                                        break;
                                    case "creator":
                                        thisreturninfo.CreatorID = child3.Value;
                                        break;
                                    case "file_url":
                                        thisreturninfo.FileURL = child3.Value;
                                        break;
                                    case "preview_url":
                                        thisreturninfo.PreviewURL = child3.Value;
                                        break;
                                    case "title":
                                        thisreturninfo.Title = child3.Value;
                                        break;
                                    case "description":
                                        thisreturninfo.Description = child3.Value;
                                        break;
                                    case "time_created":
                                        thisreturninfo.TimeCreated = Int64.Parse(child3.Value);
                                        break;
                                    case "time_updated":
                                        thisreturninfo.TimeCreated = Int64.Parse(child3.Value);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            returninfo.Add(thisreturninfo);
                        }
                    }
                }
            }

            // Remove duplicate entries
            var tempreturninfo = new List<WorkshopReturnInformation>();
            var usedids = new List<string>();
            foreach(var item in returninfo)
            {
                if(!usedids.Contains(item.PublishedFileID))
                {
                    tempreturninfo.Add(item);
                    usedids.Add(item.PublishedFileID);
                }
            }

            returninfo = tempreturninfo;


            return returninfo;

        }
    }

    class WorkshopReturnInformation
    {
        public string Description { get; set; }
        public string Title { get; set; }
        public string FileURL { get; set; }
        public string PreviewURL { get; set; }
        public string PublishedFileID { get; set; }
        public string CreatorID { get; set; }
        public Int64 TimeCreated { get; set; }
        public Int64 TimeUpdated { get; set; }
    }
}
