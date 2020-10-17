using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
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
    }
}
