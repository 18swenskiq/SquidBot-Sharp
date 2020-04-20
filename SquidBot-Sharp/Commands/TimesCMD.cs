using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using SquidBot_Sharp.Models;
using SquidBot_Sharp.Modules;
using SquidBot_Sharp.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Commands
{
    public class TimesCMD : BaseCommandModule
    {
        [Command("times"), Description("Get current times across the world")]
        public async Task Times(CommandContext ctx)
        {

            // Check which user profiles we have in the current server
            var upm = new UserProfileModule();
            var memberswithinfo = new List<DiscordMember>();
            foreach(var member in ctx.Guild.Members)
            {
                var testingprofile = upm.CheckIfUserProfileExists(member.Value);
                if (testingprofile == null) continue;
                if (testingprofile.TimeZone == null) continue;

                memberswithinfo.Add(member.Value);
            }

            if(memberswithinfo.Count == 0)
            {
                await ctx.RespondAsync("No users with timezone data found on this server");
                return;
            }

            var ouruserprofiles = new List<UserProfile>();
            // Now we have to get the user profiles
            foreach(var user in memberswithinfo)
            {
                var thisprofile = upm.DeserializeProfile(user.Id);
                ouruserprofiles.Add(thisprofile);
            }

            var userprofileorder = new List<UserProfile>();

            foreach(var utctime in new List<string> { "UTC-12:00", "UTC-11:00", "UTC-10:00", "UTC-09:30", "UTC-09:00", "UTC-08:00", "UTC-07:00", "UTC-06:00", "UTC-05:00", "UTC-04:00", "UTC-03:30","UTC-03:00", "UTC-02:00", "UTC-01:00", "UTC±00:00", "UTC+01:00", "UTC+02:00", "UTC+03:00", "UTC+03:30", "UTC+04:00", "UTC+04:30", "UTC+05:00", "UTC+05:30", "UTC+05:45", "UTC+06:00", "UTC+07:00", "UTC+08:00", "UTC+08:45", "UTC+09:00", "UTC+09:30", "UTC+10:00", "UTC+10:30", "UTC+11:00", "UTC+12:00", "UTC+12:45", "UTC+13:00", "UTC+14:00"  })
            {
                foreach(var user in ouruserprofiles)
                {
                    //build our UTC string from scratch
                    var thistimehours = user.TimeZone.BaseUtcOffset.Hours;
                    var thistimeminutes = user.TimeZone.BaseUtcOffset.Minutes;

                    string utcstring = "UTC";
                    if (thistimehours < 0) utcstring += "-";
                    else if (thistimehours == 0) utcstring += "±";
                    else utcstring += "+";

                    if (Math.Abs(thistimehours) < 10) utcstring += $"0{Math.Abs(thistimehours)}";
                    else utcstring += Math.Abs(thistimehours);

                    utcstring += ":";

                    if (Math.Abs(thistimeminutes) < 10) utcstring += $"0{thistimeminutes}";
                    else utcstring += thistimeminutes;

                    if (utcstring == utctime) userprofileorder.Add(user);
                }
            }

            // Reverse userprofileorder so we get the times in the order we want
            //userprofileorder.Reverse();

            var timesstring = "```py\n";
            var profileidsusedsofar = new List<ulong>();

            foreach(var item in userprofileorder)
            {
                // we can skip this user if we've already used them
                if (profileidsusedsofar.Contains(item.UserID)) continue;

                profileidsusedsofar.Add(item.UserID);

                var userswiththistime = new List<UserProfile>();
                foreach(var otheruserwithtime in userprofileorder)
                {
                    if (otheruserwithtime == item) continue;
                    DateTime thistime = DateTime.Now;
                    DateTime person1dt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(thistime, item.TimeZone.Id);
                    DateTime person2dt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(thistime, otheruserwithtime.TimeZone.Id);

                    if(person1dt == person2dt)
                    {
                        userswiththistime.Add(otheruserwithtime);
                        profileidsusedsofar.Add(otheruserwithtime.UserID);
                    }
                }

                DateTime thisdatetime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, item.TimeZone.Id);

                // Now that we've got all the users that belong in this time, let's make it
                string timestringline = $"{thisdatetime.ToString("MMM dd").PadRight(7)} {thisdatetime.ToString("HH:mm")} ";
                if(userswiththistime.Count == 0)
                {
                    timestringline += $"({item.ProfileName})\n";
                    timesstring += timestringline;
                }

                else
                {
                    timestringline += $"({item.ProfileName}, ";
                    if (userswiththistime.Count == 1)
                    {
                        timestringline += $"{userswiththistime.Single().ProfileName})\n";
                        timesstring += timestringline;
                    }
                    else
                    {
                        var lastitem = userswiththistime[userswiththistime.Count - 1];
                        foreach (var user in userswiththistime)
                        {
                            if (user == lastitem) break;
                            timestringline += $"{user.ProfileName}, ";
                        }
                        timestringline += $"{lastitem.ProfileName})\n";
                        timesstring += timestringline;
                    }
                }

            }

            timesstring += "```";


            await ctx.RespondAsync("Use `>settimezone` to add your own time zone!\n" + timesstring);
            return;
        }

        [Command("gettimezone"), Hidden, RequireOwner]
        public async Task GetTimeZone(CommandContext ctx)
        {
            var upm = new UserProfileModule();
            var test = upm.DeserializeProfile(ctx.User.Id);
            await ctx.RespondAsync($"Timezone is {test.TimeZone.Id}");
        }


        [Command("sudosettimezone"), Hidden, RequireOwner]
        [Aliases("sudoaddtimezone")]
        public async Task SudoSetTimeZone(CommandContext ctx, DiscordMember Mentioned)
        {

            var interactivity = ctx.Client.GetInteractivity();
            await ctx.RespondAsync($"Please enter either the largest city near you or in your timezone");
            var userinput = await interactivity.WaitForMessageAsync(us => us.Author == ctx.User, TimeSpan.FromSeconds(120));
            string argument = userinput.Result.Content;

            TimeZoneInfo usertimezone = null;

            var cities = new CitiesTimezones();
            foreach (var city in cities.entries)
            {
                if (city.CityName.ToLower() == userinput.Result.Content.ToLower())
                {
                    usertimezone = city.TimeZone;
                    break;
                }
            }
            if (usertimezone == null)
            {
                await ctx.RespondAsync("City name could not be found. Try again with a different city in your timezone, or contact Squidski#9545 to add your city");
                return;
            }

            var upm = new UserProfileModule();
            UserProfile thisuserprofile = null;
            thisuserprofile = upm.CheckIfUserProfileExists(Mentioned);
            if (thisuserprofile == null)
            {
                thisuserprofile = upm.BuildUserProfile(usertimezone, Mentioned);
                if (thisuserprofile == null)
                {
                    await ctx.RespondAsync("Something unexpected happened. Contact Squidski");
                    return;
                }
            }
            else
            {
                var isModificationSucessful = upm.ModifyUserProfile(new UserProfile { TimeZone = usertimezone, UserID = thisuserprofile.UserID, Version = thisuserprofile.Version, ProfileName = Mentioned.Username });
                if (!isModificationSucessful)
                {
                    await ctx.RespondAsync("Something unexpected happened. Contact Squidski");
                    return;
                }
                await ctx.RespondAsync("Timezone successfully updated");
                return;
            }
            await ctx.RespondAsync("Timezone successfully updated");
            return;
        }


        [Command("settimezone"), Description("Set your time zone through a local city name")]
        [Aliases("addtimezone")]
        public async Task SetTimeZone(CommandContext ctx)
        {

            var interactivity = ctx.Client.GetInteractivity();
            await ctx.RespondAsync($"Please enter either the largest city near you or in your timezone");
            var userinput = await interactivity.WaitForMessageAsync(us => us.Author == ctx.User, TimeSpan.FromSeconds(120));
            string argument = userinput.Result.Content;

            TimeZoneInfo usertimezone = null;

            var cities = new CitiesTimezones();
            foreach(var city in cities.entries)
            {
                if(city.CityName.ToLower() == userinput.Result.Content.ToLower())
                {
                    usertimezone = city.TimeZone;
                    break;
                }
            }
            if(usertimezone == null)
            {
                await ctx.RespondAsync("City name could not be found. Try again with a different city in your timezone, or contact Squidski#9545 to add your city");
                return;
            }

            var upm = new UserProfileModule();
            UserProfile thisuserprofile = null;
            thisuserprofile = upm.CheckIfUserProfileExists(ctx.User);
            if(thisuserprofile == null)
            {
                thisuserprofile = upm.BuildUserProfile(usertimezone, ctx.User);
                if(thisuserprofile == null)
                {
                    await ctx.RespondAsync("Something unexpected happened. Contact Squidski");
                    return;
                }
            }
            else
            {
                var isModificationSucessful = upm.ModifyUserProfile(new UserProfile { TimeZone = usertimezone, UserID = thisuserprofile.UserID, Version = thisuserprofile.Version, ProfileName = ctx.User.Username });
                if(!isModificationSucessful)
                {
                    await ctx.RespondAsync("Something unexpected happened. Contact Squidski");
                    return;
                }
                await ctx.RespondAsync("Timezone successfully updated");
                return;
            }
            await ctx.RespondAsync("Timezone successfully updated");
            return;
        }

    }

}
