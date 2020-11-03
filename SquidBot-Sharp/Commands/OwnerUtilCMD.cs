using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using SquidBot_Sharp.Models;
using SquidBot_Sharp.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Commands
{
    class OwnerUtilCMD : BaseCommandModule
    {
        [Command("todo"), RequireOwner]
        public async Task Todo(CommandContext ctx, [RemainingText] string dothis)
        {
            string todofile = "todo.txt";
            if (!File.Exists(todofile)) File.Create(todofile);

            using (StreamWriter sw = File.AppendText(todofile))
            {
                try
                {
                    await sw.WriteLineAsync(dothis);
                }
                catch(Exception e)
                {
                    await ctx.RespondAsync($"Error: {e.Message} | Try again?");
                    return;
                }
            }

            await ctx.RespondAsync("Successfully added to the todo list");
            return;
        }

        [Command("restart"), RequireOwner]
        public async Task Restart(CommandContext ctx)
        {
            Environment.Exit(0);
        }

/*        [Command("uploaduserprofiles"), RequireOwner, Hidden]
        public async Task UploadUserProfiles(CommandContext ctx)
        {
            string path = @"J:\Users\Quinton\source\repos\SquidBot-Sharp\SquidBot-Sharp\bin\Debug\netcoreapp3.0\datafiles\UserProfiles";
            string[] files = Directory.GetFiles(path);
            var profilemodule = new UserProfileModule();
            var profilelist = new List<UserProfile> { };
            await ctx.RespondAsync($"Reading files...");
            foreach (var file in files)
            {
                string profilestring = Path.GetFileNameWithoutExtension(file);
                var profilenum = Int64.Parse(profilestring);
                var profile = profilemodule.DeserializeProfile((ulong)profilenum);
                profilelist.Add(profile);
            }
            await ctx.RespondAsync($"Parsed {profilelist.Count} profiles!");
            foreach(var item in profilelist)
            {
                string json = JsonConvert.SerializeObject(item.TimeZone, Formatting.Indented);
                await DatabaseModule.AddTimeZoneData(item.UserID.ToString(), json);
            }

        }*/
    }
}
