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
    public class KetalQuoteCMD : BaseCommandModule
    {
        [Command("ketalquote"), Description("Get a quote from the great Russian Legend, Ketal")]
        [Aliases("kq", "ketalq")]
        [Cooldown(1, 2, CooldownBucketType.User)]
        public async Task KetalQuote(CommandContext ctx, [Description("Which quote number you would like to get")] int quotenumber = 9999)
        {
            var thisketalquote = new KetalQuote();
            var KetalQuotesObjectList = await DatabaseModule.GetKetalQuotes();

            if (quotenumber == 9999)
            {
                var rnd = new Random();
                int r = rnd.Next(KetalQuotesObjectList.Count);
                thisketalquote = KetalQuotesObjectList[r];
            }
            else
            {
                thisketalquote = (from quotes in KetalQuotesObjectList where quotes.QuoteNumber == quotenumber select quotes).Single();
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
            // Todo: replace this with a generic "get largest number" function in the database once its done so we don't have to grab all the quotes
            var KetalQuotesObjectList = await DatabaseModule.GetKetalQuotes();

            var interactivity = ctx.Client.GetInteractivity();

            await ctx.RespondAsync("Please enter the quote (type 'exit' to exit the interactive session)");
            var quote = await interactivity.WaitForMessageAsync(kq => kq.Author == ctx.User, TimeSpan.FromSeconds(60));

            if(quote.Result.Content.ToLower() == "exit")
            {
                await ctx.RespondAsync("Exiting...");
                return;
            }

            await ctx.RespondAsync("Please enter the footer (type 'exit' to exit the interactive session)");
            var footer = await interactivity.WaitForMessageAsync(kq => kq.Author == ctx.User, TimeSpan.FromSeconds(60));

            if (quote.Result.Content.ToLower() == "exit")
            {
                await ctx.RespondAsync("Exiting...");
                return;
            }

            await ctx.RespondAsync("Building Quote...");
            var test = (from quotes in KetalQuotesObjectList select quotes.QuoteNumber).ToArray();

            var num = test.Max();

            var newQuote = new KetalQuote { Quote = quote.Result.Content, Footer = footer.Result.Content, QuoteNumber = num + 1 };
            await DatabaseModule.AddKetalQuote(newQuote.QuoteNumber, newQuote.Quote, newQuote.Footer);
            //KetalQuoteModule.Quotes.Add(newQuote);

            //KetalQuoteModule.SerializeQuotes();
            //DatabaseModule.UploadFile("datafiles/data.ketalquotes");

            await ctx.RespondAsync($"Quote successfully added as #{num+1}");
        }
    }
    public class KetalQuote
    {
        public int QuoteNumber { get; set; }
        public string Quote { get; set; }
        public string Footer { get; set; }
    }
}
