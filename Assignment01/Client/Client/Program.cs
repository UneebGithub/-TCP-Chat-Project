using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TCPChatClient
{
    class Program
    {
        static TcpClient client;
        static NetworkStream stream;

        static void Main(string[] args)
        {
            Console.Title = " TCP Chat Client-Assignment";
            Console.WriteLine("Connecting to server...");

            try
            {
                client = new TcpClient("127.0.0.1", 5000); 
                stream = client.GetStream();

                Console.WriteLine("Connected to server.");
                Console.Write("Enter your username (e.g. !username YourName): ");
                string username = Console.ReadLine();
                Send(username);

                Thread receiveThread = new Thread(ReceiveMessages);
                receiveThread.Start();

                while (true)
                {
                    string input = Console.ReadLine();
                    if (input.ToLower() == "!exit")
                    {
                        Console.WriteLine(" Exiting...");
                        break;
                    }
                    Send(input);
                }

                client.Close();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(" Could not connect to server: " + ex.Message);
            }
        }

        static void Send(string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                stream.Write(data, 0, data.Length);
            }
            catch
            {
                Console.WriteLine(" Error sending message.");
            }
        }

        static void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    if (bytes == 0) break;

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();
                    Console.WriteLine(msg);

                    if (msg.Contains("Username already taken") || msg.Contains("Connection closed."))
                    {
                        Console.WriteLine("Disconnected by server.");
                        client.Close();
                        Environment.Exit(0);
                    }
                }
            }
            catch
            {
                Console.WriteLine("Lost connection to server.");
                Environment.Exit(0);
            }
        }
    }
}
