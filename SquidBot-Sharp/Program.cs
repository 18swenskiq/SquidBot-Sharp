﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
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
        public ImpersonateModule _impersonate { get; set; }     
        public Timer _timer { get; set; }


        public readonly static int TIMER_INTERVAL = 60000;
        public static readonly int BACKUP_TIME_HOUR = 3; 

        public static void Main(string[] args)
        {
            var prog = new Program();
            prog.RunBotAsync().GetAwaiter().GetResult();
        }

        public async Task RunBotAsync()
        {
            _impersonate = new ImpersonateModule();
            using (StreamReader r = new StreamReader("settings.json"))
            {
                string json = await r.ReadToEndAsync();
                var sfd = JsonConvert.DeserializeObject<SettingsFileDeserialize>(json);

                SettingsFile.botkey = sfd.botkey;
                SettingsFile.databasepassword = sfd.databasepassword;
                SettingsFile.databaseserver = sfd.databaseserver;
                SettingsFile.databasename = sfd.databasename;
                SettingsFile.databaseusername = sfd.databaseusername;
                SettingsFile.faceitapikey = sfd.faceitapikey;
                SettingsFile.steamwebapikey = sfd.steamwebapikey;
                SettingsFile.databasebackuplocation = sfd.databasebackuplocation;
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
            _client.MessageCreated += Client_MessageCreated;

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
            _commands.RegisterCommands(typeof(ImpersonateCMD));
            _commands.RegisterCommands(typeof(ServerCMD));

            var RCONinstance = new RconModule();
            RconInstance.RconModuleInstance = RCONinstance;

            //_commands.RegisterCommands(typeof(DatabaseCMD));

            _client.DebugLogger.LogMessage(LogLevel.Info, "MechaSquidski", "Setting up database connections", DateTime.Now);
            // Database related startup operations
            DatabaseModule.SetUpMySQLConnection(SettingsFile.databaseserver, SettingsFile.databasename, SettingsFile.databaseusername, SettingsFile.databasepassword);

            // Startup KetalQuoteModule
            //DatabaseModule.RetrieveFile(@"datafiles\data.ketalquotes");
            KetalQuoteModule.DeserializeQuotes();
            _client.DebugLogger.LogMessage(LogLevel.Info, "MechaSquidski", "KetalQuotes are set up", DateTime.Now);

            // Initiate timer for recurring tasks
            _timer = new Timer(Tick, null, TIMER_INTERVAL, Timeout.Infinite);
            _client.DebugLogger.LogMessage(LogLevel.Info, "MechaSquidski", "Timer object for recurring tasks initiated", DateTime.Now);

            await _client.ConnectAsync();

            _activities = new CustomActivities();

            await Task.Delay(-1);
        }

        private Task Client_Ready(ReadyEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "MechaSquidski", "Client is ready to process events.", DateTime.Now);

            return Task.CompletedTask;
        }

        private async void Tick(object state)
        {
            // Perform tasks every minute
            // Update status
            _client.DebugLogger.LogMessage(LogLevel.Info, "MechaSquidski", "Updating status", DateTime.Now);
            var nextpayload = _activities.GetNextActivity();
            if (nextpayload.Status.Contains("NUMBER_GUILDS")) nextpayload.Status = nextpayload.Status.Replace("NUMBER_GUILDS", _client.Guilds.Count.ToString());
            await _client.UpdateStatusAsync(new DiscordActivity(nextpayload.Status, nextpayload.ActType));

            // Check if time is currently BACKUP_TIME_HOUR, if it is then
            var DateTimeComparison = DateTime.Now;
            if (DateTimeComparison.Hour == BACKUP_TIME_HOUR && DateTimeComparison.Minute == 0)
            {
                // If we've made it here, its time to automatically backup the database
                _client.DebugLogger.LogMessage(LogLevel.Info, "MechaSquidski", "Starting automatic database backup...", DateTime.Now);
                await DatabaseModule.BackupDatabase();
                if(DatabaseModule.HitException != null)
                {
                    _client.DebugLogger.LogMessage(LogLevel.Warning, "MechaSquidski", $"Database could not be backed up due to {DatabaseModule.HitException.Message}", DateTime.Now);
                }
                else
                {
                    _client.DebugLogger.LogMessage(LogLevel.Info, "MechaSquidski", "Automatic database backup successful", DateTime.Now);
                }
            }

            // Schedule next timer
            _timer?.Change(TIMER_INTERVAL, Timeout.Infinite);
        }

        private async Task<Task> Client_MessageCreated(MessageCreateEventArgs e)
        {
            if(e.Message.Content.StartsWith(">") || string.IsNullOrWhiteSpace(e.Message.Content) || e.Message.Content.StartsWith("+"))
            {
                return Task.CompletedTask;
            }

            await DatabaseModule.AddNewUserMessage(e.Author.Id, e.Message.Content);
            if(DatabaseModule.HitException != null)
            {
                Console.WriteLine("FAILED");
            }

            return Task.CompletedTask;
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
                var checks = ex.FailedChecks;
                if(checks[0].GetType() == typeof(CooldownAttribute))
                {
                    var ratelimitemoji = DiscordEmoji.FromGuildEmote(e.Context.Client, 628767100709765158);
                    var ratelimitembed = new DiscordEmbedBuilder
                    {
                        Title = "You have been rate limited",
                        Description = $"{ratelimitemoji} Please slow down your command usage",
                        Color = new DiscordColor(0x0f00ff)
                    };
                    await e.Context.RespondAsync("", embed: ratelimitembed);
                    return;
                }
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
