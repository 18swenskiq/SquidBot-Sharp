using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using FaceitLib;
using FaceitLib.Models.Shared;
using SquidBot_Sharp.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Commands
{
    public class FaceitCMD : BaseCommandModule
    {
        public FaceitClient faceitClient { get; set; }

        [Command("faceitplayerdetails")]
        [Aliases("faceitpd")]
        [Cooldown(1, 10, CooldownBucketType.User)]
        public async Task FaceitDetails(CommandContext ctx, [RemainingText, Description("The username of the Faceit account to get details for")] string username)
        {

            faceitClient = new FaceitClient(SettingsFile.faceitapikey);
            PlayerDetails stats = null;
            int attemptnumber = 1;
            RetrySearch:
            stats = await faceitClient.GetPlayerDetailsFromNickname(username);
            if(faceitClient.GetStatusCode() == HttpStatusCode.ServiceUnavailable)
            {
                if(attemptnumber == 6)
                {
                    await ctx.RespondAsync($"Attempt #5 failed. Retry command later");
                    return;
                }
                await ctx.RespondAsync($"Attempt #{attemptnumber} failed");
                await Task.Delay(100);
                goto RetrySearch;
            }
            if(faceitClient.GetStatusCode() != HttpStatusCode.OK)
            {
                await ctx.RespondAsync("Player could not be found");
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(0xFF5500),
                Description = $"Faceit ID: `{stats.PlayerID}`",
                Title = $"{stats.Nickname}",
                Timestamp = DateTime.UtcNow,
                ThumbnailUrl = stats.Avatar,
                Url = stats.FaceitURL,
            };
            embed.AddField("Membership Type", stats.MembershipType);
            embed.AddField("Steam ID", stats.SteamID64);
            embed.AddField("Country", $":flag_{stats.Country}:");
            embed.AddField("CSGO Skill Level", $"{stats.Games.CSGO.SkillLevel}");


            await ctx.RespondAsync(embed: embed);

        }

    }
}
