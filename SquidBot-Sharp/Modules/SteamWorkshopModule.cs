using SquidBot_Sharp.Models;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Modules
{
    static class SteamWorkshopModule
    {
        static public async Task<List<WorkshopReturnInformation>> GetPublishedFileDetails(List<string> itemids)
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
                foreach (var itemid in itemids)
                {
                    basedict["publishedfileids[0]"] = itemid;
                    try
                    {
                        KeyValue kvResults = await SteamPublishedFileDetails.GetPublishedFileDetails(basedict);
                        kvlist.Add(kvResults);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return null;
                    }
                }
            }
            // Parse out the information we want
            var returninfo = new List<WorkshopReturnInformation>();
            foreach (var kvitem in kvlist)
            {
                foreach (var child in kvitem.Children)
                {
                    foreach (var child2 in child.Children)
                    {
                        if (child2.Name == "0")
                        {
                            var thisreturninfo = new WorkshopReturnInformation();

                            foreach (var child3 in child2.Children)
                            {
                                switch (child3.Name)
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
                                    case "filename":
                                        thisreturninfo.Filename = child3.Value;
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


            return returninfo;

        }
    }
}
