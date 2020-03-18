using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using SquidBot_Sharp.Modules;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus.Interactivity;

namespace SquidBot_Sharp.Commands
{
    public class KetalQuoteCMD
    {
        [Command("ketalquote"), Description("Get a quote from the great Russian Legend, Ketal")]
        [Aliases("kq", "ketalq")]
        public async Task KetalQuote(CommandContext ctx, [Description("Which quote number you would like to get")] int quotenumber = 9999)
        {
            var thisketalquote = new KetalQuote();

            if (quotenumber == 9999)
            {
                var rnd = new Random();
                int r = rnd.Next(KetalQuoteModule.Quotes.Count);
                thisketalquote = KetalQuoteModule.Quotes[r];
            }
            else
            {
                thisketalquote = (from quotes in KetalQuoteModule.Quotes where quotes.QuoteNumber == quotenumber select quotes).Single();
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0xFF00FF),
                Description = thisketalquote.Quote,
                Title = $"Ketal Quote #{thisketalquote.QuoteNumber}",
                Timestamp = DateTime.UtcNow,
                
            };
            embed.WithFooter($"Ketal on {thisketalquote.Footer}");
            await ctx.RespondAsync(embed: embed.Build());
        }

        [Command("addketalquote"), Description("Add a ketal quote (Squidski Only)"), Hidden, RequireOwner]
        public async Task AddKetalQuote(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivityModule();

            await ctx.RespondAsync("Please enter the quote (type 'exit' to exit the interactive session)");
            var quote = await interactivity.WaitForMessageAsync(kq => kq.Author == ctx.User, TimeSpan.FromSeconds(60));

            if(quote.Message.Content.ToLower() == "exit")
            {
                await ctx.RespondAsync("Exiting...");
                return;
            }

            await ctx.RespondAsync("Please enter the footer (type 'exit' to exit the interactive session)");
            var footer = await interactivity.WaitForMessageAsync(kq => kq.Author == ctx.User, TimeSpan.FromSeconds(60));

            if (quote.Message.Content.ToLower() == "exit")
            {
                await ctx.RespondAsync("Exiting...");
                return;
            }

            await ctx.RespondAsync("Building Quote...");
            var test = (from quotes in KetalQuoteModule.Quotes select quotes.QuoteNumber).ToArray();

            var num = test.Max();

            var newQuote = new KetalQuote { Quote = quote.Message.Content, Footer = footer.Message.Content, QuoteNumber = num + 1 };
            KetalQuoteModule.Quotes.Add(newQuote);

            KetalQuoteModule.SerializeQuotes();
            DatabaseModule.UploadFile("datafiles/data.ketalquotes");

            await ctx.RespondAsync($"Quote successfully added as #{num+1}");
        }
    }
}
