using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using SquidBot_Sharp.Models;
using SquidBot_Sharp.Modules;

namespace SquidBot_Sharp.Commands
{
    public class BettingCMD : BaseCommandModule
    {
        [Command("squidbet"), Description("Bet on things")]
        [Hidden, RequireOwner]
        [Cooldown(1, 5, CooldownBucketType.User)]
        public async Task SquidBet(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivity();
            var options = await DatabaseModule.GetSquidBetEvents();
            options = BettingModule.RemoveBetEventDuplicates(options);
            string bettable = "";
            bettable += "**Betting Options:**\n";
            bettable += "Misc:\n";
            bettable += "```\n";
            if (options.FindIndex(b => b.Type == 0) >= 0)    // If we have misc options, put them here
            {
                foreach(var bet in options)
                {
                    if(bet.Type == 0)
                    {
                        bettable += $"{bet.Index}: {bet.Title}\n";
                    }
                }
            }
            else
            {
                bettable += "Nothing to display!\n";
            }
            bettable += "```\n";

            bettable += "CSGO Matches:\n";
            bettable += "```\n";
            if (options.FindIndex(b => b.Type == 1) >= 0) // If we have CSGO matches, put them here
            {
                foreach(var bet in options)
                {
                    if(bet.Type == 1)
                    {
                        bettable += $"{bet.Index}: {bet.Title}\n";
                    }
                }
            }
            else
            {
                bettable += "Nothing to display!\n";
            }
            bettable += "```\n";
            bettable += "**Type the number of the bet you would like to bet on**\n";
            bettable += "**Type 'exit' to exit**\n";

            SendBetTable:
            var sentmessage = await ctx.RespondAsync(bettable);

            WaitUserMessage:
            var usermessage = await interactivity.WaitForMessageAsync(us => us.Author == ctx.User, TimeSpan.FromSeconds(120));

            int userindexinput;
            if (usermessage.Result.Content.ToLower() == "exit")
            {
                await sentmessage.ModifyAsync("Bet store closed, goodbye");
            }           
            else if(int.TryParse(usermessage.Result.Content, out userindexinput))
            {
                if(BettingModule.GetBetIndices(options).Contains(userindexinput))
                {
                    var embed = BettingModule.GetBetEventEmbed(options.Find(b => b.Index == userindexinput));
                    var embedmessage = await sentmessage.ModifyAsync(embed: embed);
                    await embedmessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":one:"));
                    await embedmessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":two:"));
                    await embedmessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":x:"));
                    await ctx.RespondAsync("React with the number of the option you would like to bet on, or react with x to go back");
                    bool waiting = true;
                    int option = 0;
                    while (waiting)
                    {
                        var reactone = await embedmessage.GetReactionsAsync(DiscordEmoji.FromName(ctx.Client, ":one:"));
                        var reacttwo = await embedmessage.GetReactionsAsync(DiscordEmoji.FromName(ctx.Client, ":two:"));
                        var reactthree = await embedmessage.GetReactionsAsync(DiscordEmoji.FromName(ctx.Client, ":two:"));

                        if(reactone.Count > 1)
                        {
                            for (int j = 0; j < reactone.Count; j++)
                            {
                                if (reactone[j].Id.ToString() == ctx.User.Id.ToString())
                                {
                                    waiting = false;
                                    option = 1;
                                    break;
                                }
                            }
                        }
                        if(reacttwo.Count > 1)
                        {
                            for(int j = 0; j < reacttwo.Count; j++)
                            {
                                if(reacttwo[j].Id.ToString() == ctx.User.Id.ToString())
                                {
                                    waiting = false;
                                    option = 2;
                                    break;
                                }
                            }
                        }
                        if(reactthree.Count > 1)
                        {
                            for(int j = 0; j < reactthree.Count; j++)
                            {
                                if(reactthree[j].Id.ToString() == ctx.User.Id.ToString())
                                {
                                    waiting = false;
                                    option = 0;
                                    break;
                                }
                            }
                        }
                    }

                    if(option == 0)
                    {
                        await embedmessage.DeleteAsync();
                        goto SendBetTable;
                    }
                    else
                    {
                        // TODO: get bet of squidcoin
                        // check how much squidcoin
                        // create bet entry in database
                        // close bet
                        // end bet and distribute squidcoin
                    }
                }
                else
                {
                    await sentmessage.ModifyAsync("Your number was not recognized. Try again");
                }
            }
            else
            {
                await usermessage.Result.DeleteAsync();
                goto WaitUserMessage;
            }
        }

        [Command("addsquidbet"), Description("Add an option to the betting table")]
        [Hidden, RequireOwner]
        public async Task AddSquidBet(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivity();

            // Get title of bet
            await ctx.RespondAsync("Please enter the title for this betting table entry");
            var titlenameraw = await interactivity.WaitForMessageAsync(us => us.Author == ctx.User, TimeSpan.FromSeconds(120));
            string titlename = titlenameraw.Result.Content;


            // Get type of bet
            Dictionary<int, string> BetTypeDict = new Dictionary<int, string>();
            foreach(int i in Enum.GetValues(typeof(BetType)))
            {
                string s = Enum.GetName(typeof(BetType), i);
                BetTypeDict.Add(i, s);
            }
            string responsestring = "Please enter the number for the type of bet this is. Types:\n";
            foreach(var item in BetTypeDict)
            {
                responsestring += $"{item.Key}: {item.Value}\n";
            }
            await ctx.RespondAsync(responsestring);
            var typeraw = await interactivity.WaitForMessageAsync(us => us.Author == ctx.User, TimeSpan.FromSeconds(120));
            int type = int.Parse(typeraw.Result.Content);

            // Start active?
            await ctx.RespondAsync("Should this bet be available for betting immediately? 0 for no, 1 for yes");
            var activeraw = await interactivity.WaitForMessageAsync(us => us.Author == ctx.User, TimeSpan.FromSeconds(120));
            int active = int.Parse(activeraw.Result.Content);

            // Get the name of the event
            await ctx.RespondAsync("Please enter the event this bet takes place at");
            var eventnameraw = await interactivity.WaitForMessageAsync(us => us.Author == ctx.User, TimeSpan.FromSeconds(120));
            string eventname = eventnameraw.Result.Content;

            // Get choice 1
            await ctx.RespondAsync("Please enter the first choice for this bet event");
            var choice1raw = await interactivity.WaitForMessageAsync(us => us.Author == ctx.User, TimeSpan.FromSeconds(120));
            string choice1 = choice1raw.Result.Content;

            // Get choice 2
            await ctx.RespondAsync("Please enter the second choice for this bet event");
            var choice2raw = await interactivity.WaitForMessageAsync(us => us.Author == ctx.User, TimeSpan.FromSeconds(120));
            var choice2 = choice2raw.Result.Content;

            var result = 0;

            var squidbm = new BettingModule();

            await squidbm.AddSquidBetEvent(titlename, type, active, eventname, choice1, choice2, result);

            await ctx.RespondAsync("Sucessfully created bet event!");
        }    
    }
}
