using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Emzi0767.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SquidBot_Sharp.Commands;
using SquidBot_Sharp.Models;
using SquidBot_Sharp.Modules;
using SquidBot_Sharp.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                MinimumLogLevel = LogLevel.Debug
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
            _commands.RegisterCommands(typeof(PlayQueueCMD));

            var RCONinstance = new RconModule();
            RconInstance.RconModuleInstance = RCONinstance;

            _client.Logger.LogInformation("MechaSquidski", "Setting up database connections", DateTime.Now);
            // Database related startup operations
            DatabaseModule.SetUpMySQLConnection(SettingsFile.databaseserver, SettingsFile.databasename, SettingsFile.databaseusername, SettingsFile.databasepassword);

            // Initiate timer for recurring tasks
            _timer = new Timer(Tick, null, TIMER_INTERVAL, Timeout.Infinite);
            _client.Logger.LogInformation("MechaSquidski", "Timer object for recurring tasks initiated", DateTime.Now);

            await _client.ConnectAsync();

            _activities = new CustomActivities();

            await Task.Delay(-1);
        }

        private Task Client_Ready(DiscordClient s, ReadyEventArgs e)
        {
            _client.Logger.LogInformation("MechaSquidski", "Client is ready to process events.", DateTime.Now);

            return Task.CompletedTask;
        }

        private async void Tick(object state)
        {
            // Perform tasks every minute
            // Update status
            _client.Logger.LogInformation("MechaSquidski", "Updating status", DateTime.Now);
            var nextpayload = _activities.GetNextActivity();
            if (nextpayload.Status.Contains("NUMBER_GUILDS")) nextpayload.Status = nextpayload.Status.Replace("NUMBER_GUILDS", _client.Guilds.Count.ToString());
            await _client.UpdateStatusAsync(new DiscordActivity(nextpayload.Status, nextpayload.ActType));

            // Check if time is currently BACKUP_TIME_HOUR, if it is then
            var DateTimeComparison = DateTime.Now;
            if (DateTimeComparison.Hour == BACKUP_TIME_HOUR && DateTimeComparison.Minute == 0)
            {
                // If we've made it here, its time to automatically backup the database
                _client.Logger.LogInformation("MechaSquidski", "Starting automatic database backup...", DateTime.Now);
                await DatabaseModule.BackupDatabase();
                if(DatabaseModule.HitException != null)
                {
                    _client.Logger.LogWarning("MechaSquidski", $"Database could not be backed up due to {DatabaseModule.HitException.Message}", DateTime.Now);
                }
                else
                {
                    _client.Logger.LogInformation("MechaSquidski", "Automatic database backup successful", DateTime.Now);
                }
            }

            // Schedule next timer
            _timer?.Change(TIMER_INTERVAL, Timeout.Infinite);
        }

        private async Task<Task> Client_MessageCreated(DiscordClient s, MessageCreateEventArgs e)
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

        private Task Client_GuildAvailable(DiscordClient s, GuildCreateEventArgs e)
        {
            _client.Logger.LogInformation("MechaSquidski", $"Guild available: {e.Guild.Name}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Client_ClientError(DiscordClient s, ClientErrorEventArgs e)
        {
            _client.Logger.LogError("MechaSquidski", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandsNextExtension c, CommandExecutionEventArgs e)
        {
            e.Context.Client.Logger.LogInformation("MechaSquidski", $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}' in {e.Context.Guild.Name} (#{e.Context.Guild.Id})", DateTime.Now);

            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandsNextExtension c, CommandErrorEventArgs e)
        {
            e.Context.Client.Logger.LogError("MechaSquidski", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' in {e.Context.Guild.Name} (#{e.Context.Guild.Id}) but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

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
