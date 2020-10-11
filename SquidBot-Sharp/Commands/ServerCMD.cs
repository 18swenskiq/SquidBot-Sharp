using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Org.BouncyCastle.Asn1.Cms;
using SquidBot_Sharp.Models;
using SquidBot_Sharp.Modules;
using SquidBot_Sharp.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Commands
{
    public class ServerCMD : BaseCommandModule
    {
        [Command("addserver"), Description("add a server to the server list")]
        [RequireOwner, Hidden]
        public async Task AddServer(CommandContext ctx, string Id, string ServerId, string Description, string Address, string RconPassword, string FtpUser, string FtpPath, string FtpType, string Game)
        {
            var newserver = new Server { Id = Int32.Parse(Id), ServerId = ServerId, Description = Description, Address = Address, RconPassword = RconPassword, FtpUser = FtpUser, FtpPath = FtpPath, FtpType = FtpType, Game = Game };
            await DatabaseModule.AddTestServer(newserver);
            await ctx.RespondAsync($"Added {newserver.Id} to server list!");
        }

        [Command("servers"), Description("get the list of servers")]
        [RequireOwner]
        public async Task GetServerList(CommandContext ctx)
        {
            var serverlist = await DatabaseModule.GetServerList();
            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = "Server List",
                Timestamp = DateTime.UtcNow,
            };
            foreach (var server in serverlist)
            {
                embed.AddField(server.Address, $"`{server.Game}`\n{server.Description}");
            }

            await ctx.RespondAsync(embed: embed);
        }

        [Command("r"), Description("Send an rcon command to a server")]
        [RequireOwner, Hidden]
        public async Task SendRcon(CommandContext ctx, [RemainingText]string command)
        {
            var localrcon = RconInstance.RconModuleInstance;
            string responsestring = await localrcon.RconCommand("can", command);
            await ctx.RespondAsync($"```\n{responsestring}\n```");
        }
    }
}
