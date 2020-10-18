using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Newtonsoft.Json;
using SquidBot_Sharp.Models;
using SquidBot_Sharp.Modules;
using SquidBot_Sharp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Commands
{
    public class TimesCMD : BaseCommandModule
    {
        private async Task<Dictionary<string, string>> CheckWhichUserTimeInfoExists(IReadOnlyDictionary<ulong, DiscordMember> memberdict)
        {
            return await DatabaseModule.CheckGuildMembersHaveTimeZoneData(memberdict);
        }

        private string BuildUTCString(int hours, int minutes)
        {
            string utcstring = "UTC";
            if (hours < 0)
            {
                utcstring += "-";
            }
            else if (hours == 0)
            {
                utcstring += "±";
            }
            else
            {
                utcstring += "+";
            }

            if (Math.Abs(hours) < 10)
            {
                utcstring += $"0{Math.Abs(hours)}";
            }
            else
            {
                utcstring += Math.Abs(hours);
            }

            utcstring += ":";

            if (Math.Abs(minutes) < 10)
            {
                utcstring += $"0{minutes}";
            }
            else
            {
                utcstring += minutes;
            }

            return utcstring;
        }

        [Command("times"), Description("Get current times across the world")]
        [Cooldown(1, 5, CooldownBucketType.User)]
        public async Task Times(CommandContext ctx)
        {
            var waitingmessage = await ctx.RespondAsync("Please wait while I retrieve time information...");
            // Check which user profiles we have in the current server
            var memberswithinfo = new List<DiscordMember>();
            var existingusers = await CheckWhichUserTimeInfoExists(ctx.Guild.Members);

            // Check which of those are in our guild
            foreach(var member in ctx.Guild.Members)
            {
                if (existingusers.Keys.Contains(member.Key.ToString()))
                {
                    memberswithinfo.Add(member.Value);
                }
            }

            if (memberswithinfo.Count == 0)
            {
                await ctx.RespondAsync("No users with timezone data found on this server");
                return;
            }

            // Build dictionary only containing people that are on the current guild
            var memberdict = new Dictionary<DiscordMember, string> { };
            foreach(var item in memberswithinfo)
            {
                string timezonejsonstring = existingusers[item.Id.ToString()];
                memberdict.Add(item, timezonejsonstring);
            }

            var userprofileorder = new List<UserProfile>();
            var orderedmemberdictwithvalueobj = new Dictionary<DiscordMember, TimeZoneInfo> { };

            foreach(var utctime in new List<string> { "UTC-12:00", "UTC-11:00", "UTC-10:00", "UTC-09:30", "UTC-09:00", "UTC-08:00", "UTC-07:00", "UTC-06:00", "UTC-05:00", "UTC-04:00", "UTC-03:30","UTC-03:00", "UTC-02:00", "UTC-01:00", "UTC±00:00", "UTC+01:00", "UTC+02:00", "UTC+03:00", "UTC+03:30", "UTC+04:00", "UTC+04:30", "UTC+05:00", "UTC+05:30", "UTC+05:45", "UTC+06:00", "UTC+07:00", "UTC+08:00", "UTC+08:45", "UTC+09:00", "UTC+09:30", "UTC+10:00", "UTC+10:30", "UTC+11:00", "UTC+12:00", "UTC+12:45", "UTC+13:00", "UTC+14:00"  })
            {
                foreach (var user in memberdict)
                {
                    //build our UTC string from scratch
                    TimeZoneInfo desobj = JsonConvert.DeserializeObject<TimeZoneInfo>(user.Value);

                    string utcstring = BuildUTCString(desobj.BaseUtcOffset.Hours, desobj.BaseUtcOffset.Minutes);

                    if (utcstring == utctime)
                    {
                        orderedmemberdictwithvalueobj.Add(user.Key, desobj);
                    }
                }
            }

            var timesstring = "```py\n";
            var profileidsusedsofar = new List<ulong>();

            foreach(var item in orderedmemberdictwithvalueobj)
            {
                // Is this check actually needed? Test removing it and see if anything breaks
                // we can skip this user if we've already used them
                if (profileidsusedsofar.Contains(item.Key.Id)) continue;

                profileidsusedsofar.Add(item.Key.Id);

                var userswiththistime = new List<DiscordMember>();
                foreach(var otheruserwithtime in orderedmemberdictwithvalueobj)
                {
                    if (otheruserwithtime.Key == item.Key) continue;
                    DateTime thistime = DateTime.Now;
                    DateTime person1dt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(thistime, item.Value.Id);
                    DateTime person2dt = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(thistime, otheruserwithtime.Value.Id);

                    if(person1dt == person2dt)
                    {
                        userswiththistime.Add(otheruserwithtime.Key);
                        profileidsusedsofar.Add(otheruserwithtime.Key.Id);
                    }
                }

                DateTime thisdatetime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, item.Value.Id);

                // Now that we've got all the users that belong in this time, let's make it
                string timestringline = $"{thisdatetime.ToString("MMM dd").PadRight(7)} {thisdatetime.ToString("HH:mm")} ";
                if(userswiththistime.Count == 0)
                {
                    timestringline += $"({item.Key.Username})\n";
                    timesstring += timestringline;
                }

                else
                {
                    timestringline += $"({item.Key.Username}, ";
                    if (userswiththistime.Count == 1)
                    {
                        timestringline += $"{userswiththistime.Single().Username})\n";
                        timesstring += timestringline;
                    }
                    else
                    {
                        var lastitem = userswiththistime[userswiththistime.Count - 1];
                        foreach (var user in userswiththistime)
                        {
                            if (user == lastitem) break;
                            timestringline += $"{user.Username}, ";
                        }
                        timestringline += $"{lastitem.Username})\n";
                        timesstring += timestringline;
                    }
                }

            }

            timesstring += "```";

            await waitingmessage.DeleteAsync();
            await ctx.RespondAsync("Use `>settimezone` to add your own time zone!\n" + timesstring);
            return;
        }

/*        [Command("gettimezone"), Hidden, RequireOwner]
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
            //UserProfile thisuserprofile = null;
            //var thisuserprofile = CheckIfUserTimeInfoExists(Mentioned);
            //if (thisuserprofile == null)
            //{
                //thisuserprofile = upm.BuildUserProfile(usertimezone, Mentioned);
                //if (thisuserprofile == null)
                //{
                    //await ctx.RespondAsync("Something unexpected happened. Contact Squidski");
                    //return;
                //}
            //}
            //else
            //{
                //var isModificationSucessful = upm.ModifyUserProfile(new UserProfile { TimeZone = usertimezone, UserID = thisuserprofile.UserID, Version = thisuserprofile.Version, ProfileName = Mentioned.Username });
                //if (!isModificationSucessful)
                //{
                    //await ctx.RespondAsync("Something unexpected happened. Contact Squidski");
                    //return;
                //}
                //await ctx.RespondAsync("Timezone successfully updated");
                //return;
            //}
            //await ctx.RespondAsync("Timezone successfully updated");
            //return;
        }

        [Command("settimezone"), Description("Set your time zone through a local city name")]
        [Aliases("addtimezone")]
        [Priority(1)]
        [Cooldown(1, 5, CooldownBucketType.User)]
        public async Task SetTimeZone(CommandContext ctx, string CityName)
        {
            TimeZoneInfo usertimezone = null;

            var cities = new CitiesTimezones();
            foreach(var city in cities.entries)
            {
                if(city.CityName.ToLower() == CityName.ToLower())
                {
                    usertimezone = city.TimeZone;
                }
            }
            if (usertimezone == null)
            {
                await ctx.RespondAsync("City name could not be found. Try again with a different city in your timezone, or contact Squidski#9545 to add your city");
                return;
            }
            var upm = new UserProfileModule();
            UserProfile thisuserprofile = null;
            //thisuserprofile = CheckIfUserTimeInfoExists(ctx.User);
            if (thisuserprofile == null)
            {
                thisuserprofile = upm.BuildUserProfile(usertimezone, ctx.User);
                if (thisuserprofile == null)
                {
                    await ctx.RespondAsync("Something unexpected happened. Contact Squidski");
                    return;
                }
            }
            else
            {
                var isModificationSucessful = upm.ModifyUserProfile(new UserProfile { TimeZone = usertimezone, UserID = thisuserprofile.UserID, Version = thisuserprofile.Version, ProfileName = ctx.User.Username });
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
        [Priority(0)]
        [Cooldown(1, 5, CooldownBucketType.User)]
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
            //thisuserprofile = await CheckIfUserTimeInfoExists(ctx.User);
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
        }*/

    }

}
