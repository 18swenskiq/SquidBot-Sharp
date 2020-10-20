using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SquidBot_Sharp.Modules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Commands
{
    class PlayQueueCMD : BaseCommandModule
    {
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
                await ctx.RespondAsync("You must have your Steam ID registered to play! Use `>register` to add your Steam ID. (NEEDS to be a SteamID64. Find your Steam ID here: https://steamidfinder.com/)");
                return;
            }

            if (!MatchmakingModule.CanJoinQueue || !MatchmakingModule.Queueing)
            {
                await ctx.RespondAsync("There is no existing queue to join. Use `>startqueue` to start your own queue.");
                return;
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
            if (!MatchmakingModule.CanJoinQueue)
            {
                await ctx.RespondAsync("There is no queue to spectate.");
                return;
            }
            if (!(await MatchmakingModule.DoesPlayerHaveSteamIDRegistered(ctx, ctx.Member)))
            {
                await ctx.RespondAsync("You must have your Steam ID registered to spectate! Use `>register` to add your Steam ID. (NEEDS to be a SteamID64. Find your Steam ID here: https://steamidfinder.com/)");
                return;
            }

            MatchmakingModule.CurrentSpectatorIds.Add(ctx.Member.Id.ToString());

            await ctx.RespondAsync("You have been added to the list of spectators when the game starts");
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

            await ctx.RespondAsync("Player <@" + discordId + ">'s current Elo is " + elo);
        }

        /*[Command("queuedebug"), Description("Join CS:GO play session")]
        public async Task QueueDebug(CommandContext ctx, int amount = 4)
        {
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

        [Command("test"), Description("Join CS:GO play session")]
        public async Task Test(CommandContext ctx)
        {

        }*/

        [Command("register"), Description("Register SteamID for games")]
        public async Task Register(CommandContext ctx, string steamId)
        {
            bool failedToConvert = false;
            try
            {
                System.Convert.ToInt64(steamId);
            }
            catch(Exception e)
            {
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

        [Command("leaderboard"), Description("Display leaderboard")]
        [Aliases("lb")]
        public async Task Leaderboard(CommandContext ctx, string parameters = "")
        {
            List<PlayerData> allPlayers = new List<PlayerData>();

            var playerIds = await DatabaseModule.GetPlayerMatchmakingStatsIds();
            for (int i = 0; i < playerIds.Count; i++)
            {
                var player = await DatabaseModule.GetPlayerMatchmakingStats(playerIds[i]);

                allPlayers.Add(player);
            }

            parameters = parameters.ToLower();
            if(parameters.Contains("kill"))
            {
                allPlayers.Sort((x, y) => { return x.TotalKillCount.CompareTo(y.TotalKillCount); });
            }
            else if (parameters.Contains("assist"))
            {
                allPlayers.Sort((x, y) => { return x.TotalAssistCount.CompareTo(y.TotalAssistCount); });
            }
            else if (parameters.Contains("death"))
            {
                allPlayers.Sort((x, y) => { return x.TotalDeathCount.CompareTo(y.TotalDeathCount); });
            }
            else if (parameters.Contains("headshot"))
            {
                allPlayers.Sort((x, y) => { return x.TotalHeadshotCount.CompareTo(y.TotalHeadshotCount); });
            }
            //else if (parameters.Contains("mvp"))
            //{
            //    allPlayers.Sort((x, y) => { return x.TotalMVPCount.CompareTo(y.TotalMVPCount); });
            //}
            else if (parameters.Contains("round"))
            {
                allPlayers.Sort((x, y) => { return x.TotalRoundsWon.CompareTo(y.TotalRoundsWon); });
            }
            else if (parameters.Contains("game"))
            {
                allPlayers.Sort((x, y) => { return x.TotalGamesWon.CompareTo(y.TotalGamesWon); });
            }
            else
            {
                allPlayers.Sort((x, y) => { return x.CurrentElo.CompareTo(y.CurrentElo); });
            }

            allPlayers.Reverse();

            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0x3277a8),
                Title = "Leaderboard",
                Timestamp = DateTime.UtcNow
            };

            int max = Math.Min(10, allPlayers.Count);
            for (int i = 0; i < max; i++)
            {
                string valueDisplay = "";

                if(parameters.Contains("kill"))
                {
                    valueDisplay = "Kills: " + allPlayers[i].TotalKillCount.ToString();
                }
                else if (parameters.Contains("assist"))
                {
                    valueDisplay = "Assists: " + allPlayers[i].TotalAssistCount.ToString();
                }
                else if (parameters.Contains("death"))
                {
                    valueDisplay = "Deaths: " + allPlayers[i].TotalDeathCount.ToString();
                }
                else if (parameters.Contains("headshot"))
                {
                    valueDisplay = "Headshots: " + allPlayers[i].TotalHeadshotCount.ToString();
                }
                //else if (parameters.Contains("mvp"))
                //{
                //    valueDisplay = "MVPs: " + allPlayers[i].TotalMVPCount.ToString();
                //}
                else if (parameters.Contains("round"))
                {
                    valueDisplay = "Rounds Won: " + allPlayers[i].TotalRoundsWon.ToString();
                }
                else if (parameters.Contains("game"))
                {
                    valueDisplay = "Matches Won: " + allPlayers[i].TotalGamesWon.ToString();
                }
                else
                {
                    valueDisplay = "Elo: " + allPlayers[i].CurrentElo.ToString();
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

                embed.AddField(">startqueue", "Starts a game queue if one doesn't already exist.");
                embed.AddField(">startqueue pick", "Starts a game queue where the users select their teammates.");
                embed.AddField(">stopqueue", "Stops a game queue if you are the host of the queue.");
                embed.AddField(">leavequeue", "Leaves an existing queue if you're part of it. Leaving as a host assigns a new host.");
                embed.AddField(">queue", "Joins an ongoing queue if enough slots are left.");
                embed.AddField(">spectate", "Joins the spectator list for the existing game queue. Cannot join spectators after a game has started.");
                embed.AddField(">register [id]", "Registers your SteamID. This will be required for you to join a game. (NEEDS to be a SteamID64. Find your Steam ID here: https://steamidfinder.com/)");
                embed.AddField(">leaderboard [type]", "Displays a leaderboard of a relevant type. Leaving it empty sorts it based on player elo.");
                embed.AddField(">updatename", "Updates your stored name (used for leaderboards). This automatically updates when you queue for a game.");

                await ctx.RespondAsync(embed: embed);
            }
        }

    }
}
