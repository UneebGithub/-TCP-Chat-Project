using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TCPChatServer
{
    class Program
    {
        static TcpListener listener;
        static Dictionary<TcpClient, string> clientUsernames = new Dictionary<TcpClient, string>();
        static List<TcpClient> connectedClients = new List<TcpClient>();
        static List<string> moderators = new List<string>();

        static void Main()
        {
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            Console.WriteLine(" Server started on port 5000...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                connectedClients.Add(client);
                Console.WriteLine(" Client connected.");
                Thread t = new Thread(() => HandleClient(client));
                t.Start();
            }
        }

        static void HandleClient(TcpClient client)
        {
            string username = null;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    if (bytes == 0) break;

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();
                    Console.WriteLine($" Received: {msg}");

                    if (msg.StartsWith("!username "))
                    {
                        username = msg.Substring(10).Trim();

                        if (clientUsernames.ContainsValue(username))
                        {
                            Send(client, " Username already taken.");
                            continue;
                        }

                        clientUsernames[client] = username;

                        if (username == "admin" && !moderators.Contains("admin"))
                            moderators.Add("admin");

                        Send(client, $" Welcome {username}!");
                        Broadcast($"{username} has joined the chat.", client);
                    }
                    else if (msg == "!who")
                    {
                        string list = "Online Users:\n" + string.Join("\n", clientUsernames.Values);
                        Send(client, list);
                    }
                    else if (msg == "!time")
                    {
                        Send(client, $" Server time: {DateTime.Now}");
                    }
                    else if (msg == "!mods")
                    {
                        Send(client, " Moderators:\n" + string.Join("\n", moderators));
                    }
                    else if (msg == "!about")
                    {
                        Send(client, " TCP Chat Server by YourName, 2024 – Console-based real-time chat system.");
                    }
                    else if (msg == "!commands" || msg == "!help")
                    {
                        string list = @"
📋 Available Commands:
!username [name]   - Set your username
!who               - Show online users
!time              - Show server time
!about             - Show info about this app
!commands / !help  - Show all commands
!mods              - Show list of moderators
!mod [user]        - Promote/demote moderator (mod only)
!kick [user]       - Kick a user from chat (mod only)
!me [msg]          - Send an emote message
!rename [newname]  - Change your username";
                        Send(client, list);
                    }
                    else if (msg.StartsWith("!me "))
                    {
                        if (username == null)
                        {
                            Send(client, " Set your username first using !username [name]");
                            continue;
                        }

                        string action = msg.Substring(4).Trim();
                        Broadcast($"* {username} {action}", client);
                    }
                    else if (msg.StartsWith("!rename "))
                    {
                        if (username == null)
                        {
                            Send(client, " Set your username first using !username [name]");
                            continue;
                        }

                        string newname = msg.Substring(8).Trim();
                        if (clientUsernames.ContainsValue(newname))
                        {
                            Send(client, " Username already taken.");
                            continue;
                        }

                        string old = username;
                        clientUsernames[client] = newname;
                        username = newname;
                        Send(client, $"Your name has been changed to {newname}");
                        Broadcast($" {old} is now known as {newname}", client);
                    }
                    else if (msg.StartsWith("!mod "))
                    {
                        if (username == null)
                        {
                            Send(client, "Set your username first using !username [name]");
                            continue;
                        }

                        if (!moderators.Contains(username))
                        {
                            Send(client, "You are not a moderator.");
                            continue;
                        }

                        string[] parts = msg.Split(' ');
                        if (parts.Length != 2)
                        {
                            Send(client, " Usage: !mod [username]");
                            continue;
                        }

                        string target = parts[1];

                        if (!clientUsernames.ContainsValue(target))
                        {
                            Send(client, " User not found.");
                            continue;
                        }

                        if (!moderators.Contains(target))
                        {
                            moderators.Add(target);
                            Broadcast($" {target} is now a moderator.", null);
                        }
                        else
                        {
                            moderators.Remove(target);
                            Broadcast($" {target} is no longer a moderator.", null);
                        }
                    }
                    else if (msg.StartsWith("!kick "))
                    {
                        if (username == null)
                        {
                            Send(client, "Set your username first using !username [name]");
                            continue;
                        }

                        if (!moderators.Contains(username))
                        {
                            Send(client, " You are not a moderator.");
                            continue;
                        }

                        string[] parts = msg.Split(' ');
                        if (parts.Length != 2)
                        {
                            Send(client, " Usage: !kick [username]");
                            continue;
                        }

                        string target = parts[1];
                        TcpClient targetClient = null;

                        foreach (var kvp in clientUsernames)
                        {
                            if (kvp.Value == target)
                            {
                                targetClient = kvp.Key;
                                break;
                            }
                        }

                        if (targetClient != null)
                        {
                            Send(targetClient, "You have been kicked by a moderator.");
                            Thread.Sleep(100);
                            targetClient.Close();
                            connectedClients.Remove(targetClient);
                            clientUsernames.Remove(targetClient);
                            Broadcast($" {target} was kicked by {username}", null);
                        }
                        else
                        {
                            Send(client, " User not found.");
                        }
                    }
                    else
                    {
                        if (username == null)
                        {
                            Send(client, "Please set your username first using !username [name]");
                            continue;
                        }

                        Broadcast($"{username}: {msg}", client);
                    }
                }
            }
            catch { }

            Console.WriteLine("Client disconnected");
            connectedClients.Remove(client);
            if (username != null)
            {
                Broadcast($"{username} has left the chat.", client);
                clientUsernames.Remove(client);
            }
        }

        static void Send(TcpClient client, string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                client.GetStream().Write(data, 0, data.Length);
            }
            catch { }
        }

        static void Broadcast(string message, TcpClient sender)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            foreach (var client in connectedClients)
            {
                if (client != sender)
                {
                    try
                    {
                        client.GetStream().Write(data, 0, data.Length);
                    }
                    catch { }
                }
            }

            Console.WriteLine($" Broadcast: {message}");
        }
    }
}
