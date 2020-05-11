using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using SquidBot_Sharp.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Commands
{
    class DatabaseCMD : BaseCommandModule
    {
        /*
        [Command("senddbcommand")]
        [Hidden, RequireOwner]
        public async Task SendCommand(CommandContext ctx, [RemainingText] string input)
        {
            var dbresponse = await DatabaseModule.ExecuteCommand(input);
            if(dbresponse.ResponseList[0] == "Good")
            {
                await ctx.RespondAsync("Connection successful");
                return;
            }
            await ctx.RespondAsync("Connection failed");
            return;
        }
        [Command("senddbquery")]
        [Hidden, RequireOwner]
        public async Task SendQuery(CommandContext ctx, [RemainingText] string input)
        {
            var dbresponse = await DatabaseModule.SendQuery(input);
            await ctx.RespondAsync(dbresponse.ToString());
            return;
        }
        */
    }
}
