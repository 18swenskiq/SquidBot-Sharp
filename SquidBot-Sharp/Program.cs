using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SquidBot_Sharp.Commands;
using SquidBot_Sharp.Models;
using SquidBot_Sharp.Modules;
using SquidBot_Sharp.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SquidBot_Sharp
{
    public class Program
    {

        public DiscordClient _client { get; set; }
        public InteractivityExtension _interactivity { get; set; }
        public CommandsNextExtension _commands { get; set; }
        public CustomActivities _activities { get; set; }


        public bool IsActivityServiceRunning { get; set; }

        public static void Main(string[] args)
        {
            var prog = new Program();
            prog.RunBotAsync().GetAwaiter().GetResult();
        }

        public async Task RunBotAsync()
        {
            IsActivityServiceRunning = false;
            using (StreamReader r = new StreamReader("settings.json"))
            {
                string json = await r.ReadToEndAsync();
                var sfd = JsonConvert.DeserializeObject<SettingsFileDeserialize>(json);

                SettingsFile.botkey = sfd.botkey;
                SettingsFile.databasepassword = sfd.databasepassword;
                SettingsFile.databaseurl = sfd.databaseurl;
                SettingsFile.databaseusername = sfd.databaseusername;
                SettingsFile.faceitapikey = sfd.faceitapikey;
                SettingsFile.steamwebapikey = sfd.steamwebapikey;
            }
            var cfg = new DiscordConfiguration
            {
                Token = SettingsFile.botkey,
                TokenType = TokenType.Bot,

                AutoReconnect = true,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            };

            _client = new DiscordClient(cfg);
            _client.Ready += Client_Ready;
            _client.GuildAvailable += Client_GuildAvailable;
            _client.ClientErrored += Client_ClientError;

            _client.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.WrapAround,

                Timeout = TimeSpan.FromMinutes(2)
            });


            var commandconfig = new CommandsNextConfiguration
            {
                StringPrefixes = new List<string> { ">" },

                CaseSensitive = false,          

                EnableDms = true,
                EnableMentionPrefix = true
            };


            _commands = _client.UseCommandsNext(commandconfig);
            _commands.CommandExecuted += Commands_CommandExecuted;
            _commands.CommandErrored += Commands_CommandErrored;
            _commands.RegisterCommands(typeof(KetalQuoteCMD));
            _commands.RegisterCommands(typeof(CurrencyCMD));
            _commands.RegisterCommands(typeof(ConvertCMD));
            _commands.RegisterCommands(typeof(TimesCMD));
            _commands.RegisterCommands(typeof(FaceitCMD));
            _commands.RegisterCommands(typeof(InviteCMD));
            _commands.RegisterCommands(typeof(SteamWorkshopCMD));
            _commands.RegisterCommands(typeof(OwnerUtilCMD));
            _commands.RegisterCommands(typeof(TranslateCMD));

            _client.DebugLogger.LogMessage(LogLevel.Info, "MechaSquidski", "Setting up database connections", DateTime.Now);
            // Database related startup operations
            var _database = new DatabaseModule(SettingsFile.databaseurl, SettingsFile.databaseusername, SettingsFile.databasepassword);

            // Startup KetalQuoteModule
            DatabaseModule.RetrieveFile(@"datafiles\data.ketalquotes");
            KetalQuoteModule.DeserializeQuotes();
            _client.DebugLogger.LogMessage(LogLevel.Info, "MechaSquidski", "KetalQuotes are set up", DateTime.Now);

            await _client.ConnectAsync();

            await Task.Delay(-1);
        }

        private Task Client_Ready(ReadyEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "MechaSquidski", "Client is ready to process events.", DateTime.Now);
        

           // var startTimePeriod = TimeSpan.Zero;
            //var periodTimeSpan = TimeSpan.FromMinutes(1);

            _activities = new CustomActivities();

            Thread statusupdate = new Thread(new ThreadStart(UpdateStatus));
            statusupdate.Start();

            return Task.CompletedTask;
        }

        private void UpdateStatus()
        {
            if (IsActivityServiceRunning)
            {
                _client.DebugLogger.LogMessage(LogLevel.Info, "MechaSquidski", "Another status update task was attempting to launch. Supressing...", DateTime.Now);
                return;
            }
            while (true)
            {
                IsActivityServiceRunning = true;
                _client.DebugLogger.LogMessage(LogLevel.Info, "MechaSquidski", "Attempting to update status...", DateTime.Now);
                var nextpayload = _activities.GetNextActivity();
                if (nextpayload.Status.Contains("NUMBER_GUILDS")) nextpayload.Status = nextpayload.Status.Replace("NUMBER_GUILDS", _client.Guilds.Count.ToString());
                _client.UpdateStatusAsync(new DiscordActivity(nextpayload.Status, nextpayload.ActType));
                Thread.Sleep(1000 * 60);
            }
        }


        private Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "MechaSquidski", $"Guild available: {e.Guild.Name}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Client_ClientError(ClientErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, "MechaSquidski", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "MechaSquidski", $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}' in {e.Context.Guild.Name} (#{e.Context.Guild.Id})", DateTime.Now);

            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandErrorEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "MechaSquidski", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' in {e.Context.Guild.Name} (#{e.Context.Guild.Id}) but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

            if (e.Exception is ChecksFailedException ex)
            {
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Access denied",
                    Description = $"{emoji} You do not have permissions required to execute this command",
                    Color = new DiscordColor(0xFF0000)
                };
                await e.Context.RespondAsync("", embed: embed);
            }
        }

    }
}
