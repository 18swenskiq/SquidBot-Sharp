using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreRCON;
using SquidBot_Sharp.Utilities;

namespace SquidBot_Sharp.Modules
{
    public class RconModule
    {

        private static Dictionary<string, RCON> _rconClients;
        private static bool _running;

        public RconModule()
        {
            _rconClients = new Dictionary<string, RCON>();
        }

        private async Task<RCON> GetOrCreateRconClient(string serverID)
        {
            if (!_rconClients.ContainsKey(serverID))
            {
                var server = await DatabaseModule.GetTestServerInfo(serverID);

                if (server == null) throw new NullReferenceException(nameof(serverID));

                var ipEndPoint = GeneralUtil.GetIpEndPointFromString(server.Address);

                var rconClient = new RCON(ipEndPoint.Address, (ushort)ipEndPoint.Port, server.RconPassword);

                _rconClients.Add(serverID, rconClient);

                rconClient.OnDisconnected += () =>
                {
                    Console.WriteLine($"RCON client for `{serverID}` has been disposed.");
                    if (_rconClients.ContainsKey(serverID)) _rconClients.Remove(serverID);
                };

                rconClient.OnLog += logMessage =>
                {
                    Console.WriteLine($"RCON: {logMessage}");
                };
            }
            return _rconClients[serverID];
        }

        public async Task<string> RconCommand(string serverID, string command, int retryCount = 0)
        {
            var recursiveRetryCount = retryCount;

            if (recursiveRetryCount >= 3)
            {
                return $"Failed to communicate with RCON server {serverID} after retrying {recursiveRetryCount} times. The" +
                        " server may be running, but I was unable to properly communicate with it." +
                       $" \n\n{command} WAS NOT sent.";
            }

            if(recursiveRetryCount == 0)
            {
                while(_running)
                {
                    Console.WriteLine("Waiting for another instance of RCON to finish before sending\n" +
                                     $"{command}\nTo: {serverID}");
                    await Task.Delay(500);
                }
            }
            _running = true;

            serverID = GeneralUtil.GetServerCode(serverID);
            var reply = "";
            RCON client = null;
            var reconnectCount = 0;

            if (command.StartsWith("say ", StringComparison.OrdinalIgnoreCase))
            {
                command = "say " + command.Substring(3).Trim();
            }

            var t = Task.Run(async () =>
            {
                while (true)
                {
                    client = await GetOrCreateRconClient(serverID);
                    if (client.GetConnected()) break;
                    await Task.Delay(250);
                    client.Dispose();
                    if (_rconClients.ContainsKey(serverID))
                    {
                        _rconClients.Remove(serverID);
                    }

                    if (reconnectCount > 2)
                    {
                        reply = "Failed to establish connection to rcon server. Is SRCDS running?" +
                               $"\nIPEndPoint: {client.GetIpEndPoint()}\nConnected: {client.GetConnected()}";
                        break;
                    }
                    reconnectCount++;
                }

                if (client.GetConnected())
                {
                    try
                    {
                        reply = await client.SendCommandAsync(command);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"RCON: Failed to communicate with RCON server {serverID}. Will retry...\n{e.Message}");
                        client.Dispose();

                        reply = await RconCommand(serverID, command, recursiveRetryCount + 1);
                    }
                }
            });

            // TODO: Why the fuck does this break it?
            //if (await Task.WhenAny(t, Task.Delay(5 * 1000)) != t)
            //{
                //try
                //{
                   // client.Dispose();
               // }
                //catch
                //{
                    //Console.WriteLine("Failed disposing");
               // }

               // Console.WriteLine($"RCON: Failed to communicate with RCON server {serverID} within the timeout period." +
                                  //$"\nRetry count is `{recursiveRetryCount}` of `3`" +
                                  //$"\n`{serverID}`\n`{command}`");

                //reply = await RconCommand(serverID, command, recursiveRetryCount + 1);
           // }

            _running = false;
            Console.WriteLine(reply);
            reply = FormatRconServerReply(reply);

            if(string.IsNullOrWhiteSpace(reply))
            {
                reply = $"{command} was sent, but provided no reply.";

                if (recursiveRetryCount == 0)
                {
                    Console.WriteLine($"RCON: Sending {command}\nTo: {serverID}\nResponse Was: {reply}");
                }
            }
            return reply;
        }

        private string FormatRconServerReply(string input)
        {
            var replyArray = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return string.Join("\n", replyArray.Where(x => !x.Trim().StartsWith("L ")));
        }

        public async Task WakeRconServer(string serverID)
        {
            await RconCommand(serverID, "//WakeServer_" + Guid.NewGuid().ToString().Substring(0, 6));
        }

        public async Task<string[]> GetRunningLevelAsync(string server)
        {
            var reply = await RconCommand(server, "host_map");
            try
            {
                reply = reply.Substring(14, reply.IndexOf(".bsp", StringComparison.Ordinal) - 14);
            }
            catch
            {
                return null;
            }
            return reply.Split('/');
        }
    }
}
