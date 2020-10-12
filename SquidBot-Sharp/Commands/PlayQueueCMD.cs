using DSharpPlus;
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
using System.Threading.Tasks;

namespace SquidBot_Sharp.Commands
{
    class PlayQueueCMD : BaseCommandModule
    {
        [Command("startqueue"), Description("Starting a play session for a CS:GO game")]
        public async Task Play(CommandContext ctx, string extra = "")
        {
            if (MatchmakingModule.MatchSelectionOngoing)
            {
                return;
            }

            MatchmakingModule.MatchSelectionOngoing = true;
            MatchmakingModule.PlayersInQueue.Clear();

            MatchmakingModule.CaptainPick = extra.ToLower() == "pick";

            await MatchmakingModule.JoinQueue(ctx, ctx.Member);
        }

        [Command("stopqueue"), Description("Ends CS:GO play queue")]
        public async Task StopQueue(CommandContext ctx)
        {
            MatchmakingModule.Reset();

            if(MatchmakingModule.PreviousMessage != null)
            {
                await MatchmakingModule.PreviousMessage.DeleteAsync();
            }
            await ctx.RespondAsync("CS:GO session queue cancelled");
        }

        [Command("queue"), Description("Join CS:GO play session")]
        public async Task Queue(CommandContext ctx)
        {
            if(!MatchmakingModule.MatchSelectionOngoing)
            {
                return;
            }

            await MatchmakingModule.JoinQueue(ctx, ctx.Member);
        }

        [Command("queuedebug"), Description("Join CS:GO play session")]
        public async Task QueueDebug(CommandContext ctx)
        {
            if (!MatchmakingModule.MatchSelectionOngoing)
            {
                return;
            }
            

            await MatchmakingModule.JoinQueue(ctx, ctx.Guild.Members[277360174371438592]);
            await MatchmakingModule.JoinQueue(ctx, ctx.Guild.Members[66318815247466496]);
            await MatchmakingModule.JoinQueue(ctx, ctx.Guild.Members[337684398294040577]);
        }

        [Command("test"), Description("Join CS:GO play session")]
        public async Task Test(CommandContext ctx)
        {
            string steamId = await DatabaseModule.GetPlayerSteamIDFromDiscordID(ctx.Member.Id.ToString());

            await ctx.RespondAsync("Your SteamID is " + steamId);
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

    }
}
