using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Commands
{
    class InviteCMD : BaseCommandModule
    {
        [Command("invite"), Description("Invite MechaSquidski to your server!")]
        public async Task Invite(CommandContext ctx)
        {
            await ctx.RespondAsync("Invite Link: <https://discordapp.com/api/oauth2/authorize?client_id=565566309257969668&permissions=103931008&scope=bot>");
            return;
        }
    }
}
