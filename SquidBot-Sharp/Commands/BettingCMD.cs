using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SquidBot_Sharp.Models;
using SquidBot_Sharp.Modules;

namespace SquidBot_Sharp.Commands
{
    public class BettingCMD : BaseCommandModule
    {
        [Command("squidbet"), Description("Bet on something")]
        [Cooldown(1, 5, CooldownBucketType.User)]
        public async Task SquidBet(CommandContext ctx)
        {

        }
    }
}
