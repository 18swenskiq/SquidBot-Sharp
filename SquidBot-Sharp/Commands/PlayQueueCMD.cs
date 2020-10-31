using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SquidBot_Sharp.Modules;
using SquidBot_Sharp.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Hello, this one comment was written by the Great CommonCrayon of Crayona 

namespace SquidBot_Sharp.Commands
{
    class PlayQueueCMD : BaseCommandModule
    {
        private const string SQUIDCOIN = ":squidcoin:";
        private const ulong SQUID_CUP_ROLE = 767555242161209384;

        [Command("test"), RequireOwner]
        public async Task TestTask(CommandContext ctx)
        {
            var test = await DatabaseModule.HasMatchEnded(60);
        }

        [Command("maptoggle"), Description("Toggles the availability of a map"), RequireOwner]
        [RequireGuild]
        public async Task MapToggle(CommandContext ctx, [RemainingText]string mapname)
        {
            if(ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            var result = await DatabaseModule.MapToggle(mapname);
            if(result == -1)
            {
                await ctx.RespondAsync("Map could not be found in the database");
            }
            if(result == 0)
            {
                await ctx.RespondAsync("Map was enabled!");
            }
            if(result == 1)
            {
                await ctx.RespondAsync("Map was disabled!");
            }
        }

        [Command("squidcup"), Description("Toggle the SquidCup role")]
        [RequireGuild]
        public async Task SquidCoinCheck(CommandContext ctx)
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            bool hasRole = false;
            foreach(var role in ctx.Member.Roles)
            {
                if(role.Id == SQUID_CUP_ROLE)
                {
                    hasRole = true;
                    break;
                }
            }

            if(hasRole)
            {
                await ctx.Member.RevokeRoleAsync(ctx.Guild.Roles[SQUID_CUP_ROLE]);
                await ctx.RespondAsync("The SquidCup role has been removed from you.");
            }
            else
            {
                await ctx.Member.GrantRoleAsync(ctx.Guild.Roles[SQUID_CUP_ROLE]);
                await ctx.RespondAsync("The SquidCup role has been granted to you.");
            }

        }

        [Command("startqueue"), Description("Starting a play session for a CS:GO game")]
        [Aliases("sq", "startq")]
        [RequireGuild]
        public async Task Play(CommandContext ctx, string extra = "")
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            if (!(await MatchmakingModule.DoesPlayerHaveSteamIDRegistered(ctx, ctx.Member)))
            {
                await ctx.RespondAsync("You must have your Steam ID registered to play! Use `>register id` to add your Steam ID. (NEEDS to be a SteamID64. Find your Steam ID here: https://steamidfinder.com/)");
                return;
            }

            if (MatchmakingModule.CurrentGameState != MatchmakingModule.MatchmakingState.Idle)
            {
                if(MatchmakingModule.CurrentGameState == MatchmakingModule.MatchmakingState.GameInProgress || MatchmakingModule.CurrentGameState == MatchmakingModule.MatchmakingState.GameSetup)
                {
                    await ctx.RespondAsync("A match is currently being played, please wait until the match concludes.");
                }
                else if(MatchmakingModule.CurrentGameState == MatchmakingModule.MatchmakingState.Queueing)
                {
                    await ctx.RespondAsync("A queue already exists. Use `>queue` to join the game.");
                }
                else if (MatchmakingModule.CurrentGameState == MatchmakingModule.MatchmakingState.DisplayingResults)
                {
                    await ctx.RespondAsync("The results for the previous game are being calculated. Please wait for it to finish before starting a new queue.");
                }
                return;
            }

            MatchmakingModule.CurrentGameState = MatchmakingModule.MatchmakingState.Queueing;
            MatchmakingModule.PlayersInQueue.Clear();

            MatchmakingModule.CaptainPick = extra.ToLower() == "pick";

            await MatchmakingModule.ChangeNameIfRelevant(ctx.Member);

            await MatchmakingModule.JoinQueue(ctx, ctx.Member);

            new Task(async () => { await MatchmakingModule.TimeOut(ctx); }).Start();
        }

        [Command("stopqueue"), Description("Ends CS:GO play queue")]
        [RequireGuild]
        public async Task StopQueue(CommandContext ctx)
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            bool isHost = MatchmakingModule.IsPlayerHost(ctx.Member);
            if(isHost)
            {
                await MatchmakingModule.Reset();

                await ctx.RespondAsync("CS:GO session queue cancelled");
            }
            else
            {
                await ctx.RespondAsync("You are not the host of this session. Only the host can stop their queue.");
            }
        }

        [Command("queue"), Description("Join CS:GO play session")]
        [Aliases("q")]
        [RequireGuild]
        public async Task Queue(CommandContext ctx)
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            if (!(await MatchmakingModule.DoesPlayerHaveSteamIDRegistered(ctx, ctx.Member)))
            {
                await ctx.RespondAsync("You must have your Steam ID registered to play! Use `>register [id]` to add your Steam ID. (NEEDS to be a SteamID64. Find your Steam ID here: https://steamidfinder.com/)");
                return;
            }

            if (MatchmakingModule.CurrentGameState == MatchmakingModule.MatchmakingState.Idle)
            {
                await ctx.RespondAsync("There is no existing queue to join. Use `>startqueue` to start your own queue.");
                return;
            }
            else if (MatchmakingModule.CurrentGameState != MatchmakingModule.MatchmakingState.Queueing)
            {
                await ctx.RespondAsync("A game is already being played, you cannot join this queue.");
                return;
            }

            if (MatchmakingModule.Bets.ContainsKey(ctx.Member.Id.ToString()))
            {
                await ctx.RespondAsync("Your bet of " + MatchmakingModule.Bets[ctx.Member.Id.ToString()] + " SquidCoin has been removed.");
                MatchmakingModule.Bets.Remove(ctx.Member.Id.ToString());
            }

            await MatchmakingModule.ChangeNameIfRelevant(ctx.Member);

            await MatchmakingModule.JoinQueue(ctx, ctx.Member);
        }

        [Command("leavequeue"), Description("Leave CS:GO play session")]
        [Aliases("lq")]
        [RequireGuild]
        public async Task LeaveQueue(CommandContext ctx)
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            if (MatchmakingModule.CurrentGameState != MatchmakingModule.MatchmakingState.Queueing)
            {
                if(MatchmakingModule.CurrentGameState == MatchmakingModule.MatchmakingState.Idle)
                {
                    await ctx.RespondAsync("There is no existing queue to leave.");
                    return;
                }
                else
                {
                    await ctx.RespondAsync("A match is already ongoing, it cannot be left or joined.");
                    return;
                }
            }

            await MatchmakingModule.LeaveQueue(ctx, ctx.Member);
        }

        [Command("updatename"), Description("Updates matchmaking name to current nickname")]
        [RequireGuild]
        public async Task UpdateName(CommandContext ctx)
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            bool success = await MatchmakingModule.ChangeNameIfRelevant(ctx.Member);

            if(success)
            {
                await ctx.RespondAsync("Your name was updated to " + ctx.Member.DisplayName);
            }
            else
            {
                await ctx.RespondAsync("Your name was unable to be updated (You may not be in the system yet. Try playing at least one game.)");
            }
        }

        [Command("spectate"), Description("Join spectators for current game")]
        [Aliases("spec")]
        [RequireGuild]
        public async Task Spectate(CommandContext ctx)
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            if (MatchmakingModule.CurrentGameState != MatchmakingModule.MatchmakingState.Queueing && MatchmakingModule.CurrentGameState != MatchmakingModule.MatchmakingState.GameSetup)
            {
                await ctx.RespondAsync("You cannot join spectators when a game has already started");
                return;
            }
            if (MatchmakingModule.CurrentGameState == MatchmakingModule.MatchmakingState.Idle)
            {
                await ctx.RespondAsync("There is no queue to spectate.");
                return;
            }
            if (!(await MatchmakingModule.DoesPlayerHaveSteamIDRegistered(ctx, ctx.Member)))
            {
                await ctx.RespondAsync("You must have your Steam ID registered to spectate! Use `>register [id]` to add your Steam ID. (NEEDS to be a SteamID64. Find your Steam ID here: https://steamidfinder.com/)");
                return;
            }

            MatchmakingModule.CurrentSpectatorDiscordIds.Add(ctx.Member.Id.ToString());
            MatchmakingModule.CurrentSpectatorIds.Add(ctx.Member.Id.ToString());
            MatchmakingModule.CurrentSpectatorNames.Add(ctx.Member.DisplayName);

            await ctx.RespondAsync("You have been added to the list of spectators when the game starts");
        }

        [Command("squidcoins"), Description("Check player SquidCoin")]
        [Aliases("squidcoin")]
        [RequireGuild]
        public async Task SquidCoinCheck(CommandContext ctx, string discordId = "")
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            if (discordId == string.Empty)
            {
                discordId = ctx.Member.Id.ToString();
            }

            long coin = await DatabaseModule.GetPlayerSquidCoin(discordId);

            DiscordUser user = await ctx.Client.GetUserAsync(System.Convert.ToUInt64(discordId));

            string response = user.Username + " has " + coin + DiscordEmoji.FromName(ctx.Client, SQUIDCOIN);
            await ctx.RespondAsync(response);
        }

        [Command("bet"), Description("Bet SquidCoin on a match")]
        [RequireGuild]
        public async Task SquidCoinBet(CommandContext ctx, long amount, string userToBetOn = "")
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            if (MatchmakingModule.CurrentGameState == MatchmakingModule.MatchmakingState.Idle)
            {
                await ctx.RespondAsync("There is no game to bet on.");
                return;
            }
            if (!MatchmakingModule.BettingAllowed)
            {
                await ctx.RespondAsync("Betting has been closed for the current match.");
                return;
            }
            if(MatchmakingModule.PlayersInQueue.Contains(ctx.Member))
            {
                await ctx.RespondAsync("You cannot bet in a game you are playing in.");
                return;
            }
            if(userToBetOn == string.Empty)
            {
                await ctx.RespondAsync("Please select a user to bet on. You may enter their Discord ID or mention them. Example: >bet " + amount + " 107967155928088576");
                return;
            }

            if(userToBetOn.Contains("<"))
            {
                userToBetOn = userToBetOn.Substring(3, userToBetOn.Length - 4);
            }
            PlayerData betUser = await DatabaseModule.GetPlayerMatchmakingStats(userToBetOn);

            string discordId = ctx.Member.Id.ToString();

            long coin = await DatabaseModule.GetPlayerSquidCoin(discordId);

            if(amount > coin)
            {
                await ctx.RespondAsync("You cannot bet " + amount + " SquidCoin - you only have " + coin + " SquidCoin.");
                return;
            }

            string extraDialogue = "";
            if(MatchmakingModule.Bets.ContainsKey(discordId))
            {
                PlayerData previousBet = await DatabaseModule.GetPlayerMatchmakingStats(MatchmakingModule.Bets[discordId].UserToBetOn);
                extraDialogue = " This replaces your previous bet of " + MatchmakingModule.Bets[discordId].BetAmount + " SquidCoin on " + previousBet.Name + ".";
                MatchmakingModule.Bets[discordId] = new MatchmakingModule.BetData()
                {
                    Name = ctx.Member.DisplayName,
                    UserToBetOn = userToBetOn,
                    BetAmount = amount
                };
            }
            else
            {
                MatchmakingModule.Bets.Add(discordId, new MatchmakingModule.BetData()
                {
                    Name = ctx.Member.DisplayName,
                    UserToBetOn = userToBetOn,
                    BetAmount = amount
                });
            }

            await ctx.RespondAsync(amount + " SquidCoin has been bet on " + betUser.Name + ". You will see changes in your balance reflected after the match has ended." + extraDialogue);
        }


        [Command("elo"), Description("Check player elo")]
        [RequireGuild]
        public async Task Elo(CommandContext ctx, string discordId = "")
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            if (discordId == string.Empty)
            {
                discordId = ctx.Member.Id.ToString();
            }

            float elo = 1000;
            PlayerData player = await DatabaseModule.GetPlayerMatchmakingStats(discordId);
            if(player.ID != null)
            {
                elo = player.CurrentElo;
            }

            await ctx.RespondAsync("Player " + player.Name + "'s current Elo is " + elo);
        }

        [Command("queuedebug"), Description("Join CS:GO play session")]
        [RequireGuild]
        public async Task QueueDebug(CommandContext ctx, int amount = 4)
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            if (!ctx.Member.Id.ToString().Contains("107967155928088576") && !ctx.Member.Id.ToString().Contains("66318815247466496"))
            {
                await ctx.RespondAsync("You are not authorized to use this");
                return;
            }
            if (MatchmakingModule.CurrentGameState == MatchmakingModule.MatchmakingState.Idle)
            {
                await ctx.RespondAsync("There is no existing queue to join. Use `>startqueue` to start your own queue.");
                return;
            }

            ulong[] ids =
            {
                277360174371438592,
                66318815247466496,
                337684398294040577,
                107967155928088576,
                219353394115117056
            };

            for (int i = 0; i < MathF.Min(ids.Length, amount); i++)
            {
                await MatchmakingModule.JoinQueue(ctx, ctx.Guild.Members[ids[i]]);
            }
        }

        [Command("forcemapchange"), Description("Force a map change on the server")]
        [RequireGuild]
        public async Task ForceMapChange(CommandContext ctx)
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            if (MatchmakingModule.PlayersInQueue[0].Id == ctx.User.Id || MatchmakingModule.PlayersInQueue[0].Id == 66318815247466496)
            {
                if(MatchmakingModule.CurrentGameState == MatchmakingModule.MatchmakingState.GameInProgress)
                {
                    var localrcon = RconInstance.RconModuleInstance;
                    await localrcon.RconCommand("sc1", $"host_workshop_map {MatchmakingModule.CurrentMapID}");
                    await ctx.RespondAsync("Sent command to force the server to change maps");
                    return;
                }
                else
                {
                    await ctx.RespondAsync("A map is not currently running on the server");
                    return;
                }
            }
            await ctx.RespondAsync("You do not have the proper permissions to call this command currently");
            return;
        }

        [Command("getmaplist"), Description("Get the map list")]
        [Aliases("getmapnames")]
        [RequireGuild]
        public async Task GetMapList(CommandContext ctx)
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            var result = await DatabaseModule.GetAllMapNames();
            string responsestring = "List of maps available:\n```\n";
            foreach(var item in result)
            {
                responsestring += $"{item}, ";
            }
            responsestring += "\n```";
            await ctx.RespondAsync(responsestring);
        }

        [Command("Recalculate"), Description("Join CS:GO play session")]
        public async Task Recalculate(CommandContext ctx, int startFrom = 1)
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            if (!ctx.Member.Id.ToString().Contains("107967155928088576") && !ctx.Member.Id.ToString().Contains("66318815247466496"))
            {
                await ctx.RespondAsync("You are not authorized to use this");
                return;
            }

            Dictionary<string, string> steamIdToPlayerId = new Dictionary<string, string>();
            //Reset all player ELO first
            List<string> ids = await DatabaseModule.GetPlayerMatchmakingStatsIds();
            for (int i = 0; i < ids.Count; i++)
            {
                PlayerData player = await DatabaseModule.GetPlayerMatchmakingStats(ids[i]);
                player = new PlayerData()
                {
                    ID = player.ID,
                    Name = player.Name,
                    CurrentElo = 1000
                };

                steamIdToPlayerId.Add(await DatabaseModule.GetPlayerSteamIDFromDiscordID(player.ID), player.ID);

                await DatabaseModule.DeletePlayerStats(ids[i]);
                await DatabaseModule.AddPlayerMatchmakingStat(player);
            }

            List<string> squidCoinIds = await DatabaseModule.GetPlayerSquidIds();
            for (int i = 0; i < squidCoinIds.Count; i++)
            {
                await DatabaseModule.DeleteSquidCoinPlayer(squidCoinIds[i]);
            }

            //Go through all the matches and recalculate their ELO
            await MatchmakingModule.RecalculateAllElo(ctx, steamIdToPlayerId, startFrom);
        }

        [Command("register"), Description("Register SteamID for games")]
        [RequireGuild]
        public async Task Register(CommandContext ctx, string steamId = "")
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            if (steamId == string.Empty)
            {
                await ctx.RespondAsync("You need to enter the id. Example: >register 76561198065593279 (Find your Steam ID here: https://steamidfinder.com/)");
                return;
            }

            bool failedToConvert = false;
            try
            {
                System.Convert.ToInt64(steamId);
            }
            catch(Exception e)
            {
                Console.WriteLine($"Failed to convert because {e.Message}.");
                failedToConvert = true;
            }
            if(steamId.Contains("STEAM") || steamId.Contains("[U") || failedToConvert)
            {
                await ctx.RespondAsync("Steam ID needs to be in the SteamID64 format! Example: >register 76561198065593279 (Find your Steam ID here: https://steamidfinder.com/)");
                return;
            }
            string existingId = await DatabaseModule.GetPlayerSteamIDFromDiscordID(ctx.Member.Id.ToString());
            if(existingId != string.Empty)
            {
                await DatabaseModule.DeletePlayerSteamID(ctx.Member.Id.ToString());
            }
            await DatabaseModule.AddPlayerSteamID(ctx.Member.Id.ToString(), steamId);
            await ctx.RespondAsync("Steam ID added");
        }



        private struct PlayerLeaderboardStats
        {
            public string Name;
            public PlayerData playerData;
            public long squidCoin;
        }

        [Command("leaderboard"), Description("Display leaderboard")]
        [Aliases("lb")]
        [RequireGuild]
        public async Task Leaderboard(CommandContext ctx, string parameters = "", string shouldReverse = "")
        {
            await ctx.RespondAsync("Go to this page to view the stats: http://squidcup.quintonswenski.com/leaderboard.php");
        }

        [Command("gamehelp"), Description("Get help for commands")]
        [RequireGuild]
        public async Task GameHelp(CommandContext ctx, string specific = "")
        {
            if (ctx.Guild.Id != 572662006692315136)
            {
                await ctx.RespondAsync("Command is not usable within this guild");
                return;
            }
            //if(specific == string.Empty)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Color = new DiscordColor(0x3277a8),
                    Title = "Matchmaking Game Commands",
                    Timestamp = DateTime.UtcNow
                };

                embed.AddField(">squidcup", "Toggles the squidcup role on yourself");
                embed.AddField(">startqueue", "Starts a game queue if one doesn't already exist.");
                embed.AddField(">startqueue pick", "Starts a game queue where the users select their teammates.");
                embed.AddField(">stopqueue", "Stops a game queue if you are the host of the queue.");
                embed.AddField(">leavequeue", "Leaves an existing queue if you're part of it. Leaving as a host assigns a new host.");
                embed.AddField(">queue", "Joins an ongoing queue if enough slots are left.");
                embed.AddField(">spectate", "Joins the spectator list for the existing game queue. Cannot join spectators after a game has started.");
                embed.AddField(">register [id]", "Registers your SteamID. This will be required for you to join a game. (NEEDS to be a SteamID64. Find your Steam ID here: https://steamidfinder.com/)");
                embed.AddField(">leaderboard [type]", "Displays a leaderboard of a relevant type. Leaving it empty sorts it based on player elo.");
                embed.AddField(">updatename", "Updates your stored name (used for leaderboards). This automatically updates when you queue for a game.");
                embed.AddField(">elo", "See your current elo");
                embed.AddField(">elo [discord id]", "See the current elo of the given discord id");
                embed.AddField(">squidcoin", "See how much squidcoin you currently have");
                embed.AddField(">squidcoin [discord id]", "See how much squidcoin the given discord id has");
                embed.AddField(">bet [amount] [discord id]", "When a game is queueing, you can bet an amount of SquidCoins you have on a specific user (using their discord id)");

                await ctx.RespondAsync(embed: embed);
            }
        }

    }
}
