﻿using DSharpPlus.CommandsNext;
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

        [Command("maptoggle"), Description("Toggles the availability of a map")]
        public async Task MapToggle(CommandContext ctx, [RemainingText]string mapname)
        {
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
        public async Task SquidCoinCheck(CommandContext ctx)
        {
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
        [Aliases("sq")]
        public async Task Play(CommandContext ctx, string extra = "")
        {
            if(!(await MatchmakingModule.DoesPlayerHaveSteamIDRegistered(ctx, ctx.Member)))
            {
                await ctx.RespondAsync("You must have your Steam ID registered to play! Use `>register id` to add your Steam ID. (NEEDS to be a SteamID64. Find your Steam ID here: https://steamidfinder.com/)");
                return;
            }

            if (!MatchmakingModule.CanJoinQueue || MatchmakingModule.Queueing)
            {
                if(MatchmakingModule.MatchPlaying)
                {
                    await ctx.RespondAsync("A match is currently being played, please wait until the match concludes.");
                }
                else if(MatchmakingModule.Queueing)
                {
                    await ctx.RespondAsync("A queue already exists. Use `>queue` to join the game.");
                }
                return;
            }

            MatchmakingModule.Queueing = true;
            MatchmakingModule.WasReset = false;
            MatchmakingModule.CanJoinQueue = true;
            MatchmakingModule.PlayersInQueue.Clear();

            MatchmakingModule.CaptainPick = extra.ToLower() == "pick";

            await MatchmakingModule.ChangeNameIfRelevant(ctx.Member);

            await MatchmakingModule.JoinQueue(ctx, ctx.Member);

            new Task(async () => { await MatchmakingModule.TimeOut(ctx); }).Start();
        }

        [Command("stopqueue"), Description("Ends CS:GO play queue")]
        public async Task StopQueue(CommandContext ctx)
        {
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
        public async Task Queue(CommandContext ctx)
        {
            if (!(await MatchmakingModule.DoesPlayerHaveSteamIDRegistered(ctx, ctx.Member)))
            {
                await ctx.RespondAsync("You must have your Steam ID registered to play! Use `>register [id]` to add your Steam ID. (NEEDS to be a SteamID64. Find your Steam ID here: https://steamidfinder.com/)");
                return;
            }

            if (!MatchmakingModule.CanJoinQueue || !MatchmakingModule.Queueing)
            {
                await ctx.RespondAsync("There is no existing queue to join. Use `>startqueue` to start your own queue.");
                return;
            }

            if(MatchmakingModule.Bets.ContainsKey(ctx.Member.Id.ToString()))
            {
                await ctx.RespondAsync("Your bet of " + MatchmakingModule.Bets[ctx.Member.Id.ToString()] + " SquidCoin has been removed.");
                MatchmakingModule.Bets.Remove(ctx.Member.Id.ToString());
            }

            await MatchmakingModule.ChangeNameIfRelevant(ctx.Member);

            bool isFull = await MatchmakingModule.JoinQueue(ctx, ctx.Member);

            if(isFull)
            {
                MatchmakingModule.CanJoinQueue = false;
            }
        }

        [Command("leavequeue"), Description("Leave CS:GO play session")]
        [Aliases("lq")]
        public async Task LeaveQueue(CommandContext ctx)
        {
            if (!MatchmakingModule.CanJoinQueue)
            {
                await ctx.RespondAsync("There is no existing queue to leave.");
                return;
            }

            await MatchmakingModule.LeaveQueue(ctx, ctx.Member);
        }

        [Command("updatename"), Description("Updates matchmaking name to current nickname")]
        public async Task UpdateName(CommandContext ctx)
        {
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
        public async Task Spectate(CommandContext ctx)
        {
            if(MatchmakingModule.MatchPlaying && !MatchmakingModule.SelectingMap)
            {
                await ctx.RespondAsync("You cannot join spectators when a game has already started");
                return;
            }
            if (!MatchmakingModule.CanJoinQueue || !MatchmakingModule.Queueing)
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
        public async Task SquidCoinCheck(CommandContext ctx, string discordId = "")
        {
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
        public async Task SquidCoinBet(CommandContext ctx, long amount, string userToBetOn = "")
        {
            if (!MatchmakingModule.CanJoinQueue || !MatchmakingModule.Queueing)
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
        public async Task Elo(CommandContext ctx, string discordId = "")
        {
            if(discordId == string.Empty)
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
        public async Task QueueDebug(CommandContext ctx, int amount = 4)
        {
            if (!ctx.Member.Id.ToString().Contains("107967155928088576") && !ctx.Member.Id.ToString().Contains("66318815247466496"))
            {
                await ctx.RespondAsync("You are not authorized to use this");
                return;
            }
            if (!MatchmakingModule.CanJoinQueue)
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

            bool isFull = false;
            for (int i = 0; i < amount; i++)
            {
                isFull = await MatchmakingModule.JoinQueue(ctx, ctx.Guild.Members[ids[i]]);
            }
        }

        [Command("forcemapchange"), Description("Force a map change on the server")]
        public async Task ForceMapChange(CommandContext ctx)
        {
            if(MatchmakingModule.PlayersInQueue[0].Id == ctx.User.Id || MatchmakingModule.PlayersInQueue[0].Id == 66318815247466496)
            {
                if(MatchmakingModule.MatchPlaying)
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
        public async Task GetMapList(CommandContext ctx)
        {
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
        public async Task Recalculate(CommandContext ctx)
        {
            if(!ctx.Member.Id.ToString().Contains("107967155928088576") && !ctx.Member.Id.ToString().Contains("66318815247466496"))
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

            //Go through all the matches and recalculate their ELO
            await MatchmakingModule.RecalculateAllElo(ctx, steamIdToPlayerId);
        }

        [Command("register"), Description("Register SteamID for games")]
        public async Task Register(CommandContext ctx, string steamId = "")
        {
            if(steamId == string.Empty)
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
        public async Task Leaderboard(CommandContext ctx, string parameters = "", string shouldReverse = "")
        {
            List<PlayerLeaderboardStats> allPlayers = new List<PlayerLeaderboardStats>();
            parameters = parameters.ToLower();

            if (parameters.Contains("squidcoin"))
            {
                var playerIds = await DatabaseModule.GetPlayerSquidIds();
                for (int i = 0; i < playerIds.Count; i++)
                {
                    DiscordUser user = await ctx.Client.GetUserAsync(System.Convert.ToUInt64(playerIds[i]));
                    long coins = await DatabaseModule.GetPlayerSquidCoin(playerIds[i]);

                    allPlayers.Add(new PlayerLeaderboardStats()
                    {
                        Name = user.Username,
                        squidCoin = coins
                    });
                }
            }
            else
            {
                var playerIds = await DatabaseModule.GetPlayerMatchmakingStatsIds();
                for (int i = 0; i < playerIds.Count; i++)
                {
                    var player = await DatabaseModule.GetPlayerMatchmakingStats(playerIds[i]);

                    allPlayers.Add(new PlayerLeaderboardStats()
                    {
                        Name = player.Name,
                        playerData = player
                    });
                }
            }


            if(parameters.Contains("kill"))
            {
                allPlayers.Sort((x, y) => { return y.playerData.TotalKillCount.CompareTo(x.playerData.TotalKillCount); });
            }
            else if (parameters.Contains("assist"))
            {
                allPlayers.Sort((x, y) => { return y.playerData.TotalAssistCount.CompareTo(x.playerData.TotalAssistCount); });
            }
            else if (parameters.Contains("death"))
            {
                allPlayers.Sort((x, y) => { return y.playerData.TotalDeathCount.CompareTo(x.playerData.TotalDeathCount); });
            }
            else if (parameters.Contains("headshot"))
            {
                allPlayers.Sort((x, y) => { return y.playerData.TotalHeadshotCount.CompareTo(x.playerData.TotalHeadshotCount); });
            }
            else if (parameters.Contains("round"))
            {
                allPlayers.Sort((x, y) => { return y.playerData.TotalRoundsWon.CompareTo(x.playerData.TotalRoundsWon); });
            }
            else if (parameters.Contains("game"))
            {
                allPlayers.Sort((x, y) => { return y.playerData.TotalGamesWon.CompareTo(x.playerData.TotalGamesWon); });
            }
            else if (parameters.Contains("squidcoin"))
            {
                allPlayers.Sort((x, y) => { return y.squidCoin.CompareTo(x.squidCoin); });
            }
            else
            {
                allPlayers.Sort((x, y) => { return y.playerData.CurrentElo.CompareTo(x.playerData.CurrentElo); });
            }

            bool reverse = shouldReverse.ToLower() == "reverse" || shouldReverse.ToLower() == "r";
            if (reverse)
            {
                allPlayers.Reverse();
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = "Leaderboard " + (reverse ? "(Reversed)" : string.Empty),
                Timestamp = DateTime.UtcNow
            };

            int max = Math.Min(10, allPlayers.Count);
            for (int i = 0; i < max; i++)
            {
                string valueDisplay = "";

                if(parameters.Contains("kill"))
                {
                    valueDisplay = "Kills: " + allPlayers[i].playerData.TotalKillCount.ToString();
                }
                else if (parameters.Contains("assist"))
                {
                    valueDisplay = "Assists: " + allPlayers[i].playerData.TotalAssistCount.ToString();
                }
                else if (parameters.Contains("death"))
                {
                    valueDisplay = "Deaths: " + allPlayers[i].playerData.TotalDeathCount.ToString();
                }
                else if (parameters.Contains("headshot"))
                {
                    valueDisplay = "Headshots: " + allPlayers[i].playerData.TotalHeadshotCount.ToString();
                }
                else if (parameters.Contains("round"))
                {
                    valueDisplay = "Rounds Won: " + allPlayers[i].playerData.TotalRoundsWon.ToString();
                }
                else if (parameters.Contains("game"))
                {
                    valueDisplay = "Matches Won: " + allPlayers[i].playerData.TotalGamesWon.ToString();
                }
                else if (parameters.Contains("squidcoin"))
                {
                    valueDisplay = "SquidCoin: " + allPlayers[i].squidCoin.ToString();
                }
                else
                {
                    valueDisplay = "Elo: " + allPlayers[i].playerData.CurrentElo.ToString();
                }

                embed.AddField((i + 1) + ". " + allPlayers[i].Name, valueDisplay);
            }

            await ctx.RespondAsync(embed: embed);
        }

        [Command("gamehelp"), Description("Get help for commands")]
        public async Task GameHelp(CommandContext ctx, string specific = "")
        {
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
