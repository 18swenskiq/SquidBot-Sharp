﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using FaceitLib.Models.Shared;
using MySql.Data.MySqlClient.Memcached;
using Newtonsoft.Json;
using SquidBot_Sharp.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
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
                await ctx.RespondAsync("You must have your Steam ID registered to play! Use `>register id` to add your Steam ID.");
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
                await ctx.RespondAsync("You must have your Steam ID registered to play! Use `>register` to add your Steam ID.");
                return;
            }

            if (!MatchmakingModule.CanJoinQueue)
            {
                await ctx.RespondAsync("There is no existing queue to join. Use `>startqueue` to start your own queue.");
                return;
            }

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

        [Command("elo"), Description("Check player elo")]
        public async Task Elo(CommandContext ctx, string discordId = "")
        {
            if(discordId == string.Empty)
            {
                discordId = ctx.Member.Id.ToString();
            }

            PlayerData player = await DatabaseModule.GetPlayerMatchmakingStats(discordId);

            await ctx.RespondAsync("Player <@" + discordId + ">'s current Elo is " + player.CurrentElo);
        }

        [Command("queuedebug"), Description("Join CS:GO play session")]
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

        [Command("register"), Description("Register SteamID for games")]
        public async Task Register(CommandContext ctx, string steamId)
        {
            string existingId = await DatabaseModule.GetPlayerSteamIDFromDiscordID(ctx.Member.Id.ToString());
            if(existingId != string.Empty)
            {
                await DatabaseModule.DeletePlayerSteamID(ctx.Member.Id.ToString());
            }
            await DatabaseModule.AddPlayerSteamID(ctx.Member.Id.ToString(), steamId);
            await ctx.RespondAsync("Steam ID added");
        }

        [Command("leaderboard"), Description("Display leaderboard")]
        [Aliases("l")]
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
            else if (parameters.Contains("mvp"))
            {
                allPlayers.Sort((x, y) => { return x.TotalMVPCount.CompareTo(y.TotalMVPCount); });
            }
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
                else if (parameters.Contains("mvp"))
                {
                    valueDisplay = "MVPs: " + allPlayers[i].TotalMVPCount.ToString();
                }
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
                embed.AddField(">register [id]", "Registers your SteamID. This will be required for you to join a game.");
                embed.AddField(">leaderboard [type]", "Displays a leaderboard of a relevant type. Leaving it empty sorts it based on player elo.");

                await ctx.RespondAsync(embed: embed);
            }
        }

    }
}
