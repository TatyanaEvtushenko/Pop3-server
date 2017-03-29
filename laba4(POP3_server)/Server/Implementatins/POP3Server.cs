using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using laba4_POP3_server_.Server;

namespace laba4_POP3_server_
{
    class POP3Server : IServer
    {
        private readonly IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("192.168.56.1"), 110);
        private Task[] sessions = new Task[0];

        public static List<int> UserIds { get; set; } = new List<int>();

        public void Listen()
        {
            var server = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                server.Bind(endPoint);
                Console.WriteLine("Address of server is {0}", endPoint);
                server.Listen(10);
                Console.WriteLine("Server was started.");

                while (true)
                {
                    var client = server.Accept();
                    Console.WriteLine("{0} was connected", client.RemoteEndPoint);
                    var newTask = Task.Factory.StartNew(() =>
                    {
                        new Session(client).Work();
                    });
                    Array.Resize(ref sessions, sessions.Length + 1);
                    sessions[sessions.Length - 1] = newTask;
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
            }
            finally
            {
                server.Close();
                Console.WriteLine("Server was stoped.");
                Task.WaitAll(sessions);
            }
        }

        public static void LoginUser(Session client, int userId)
        {
            lock (client)
            {
                UserIds.Add(userId);
            }
        }

        public static void LogoutUser(Session client, int userId)
        {
            lock (client)
            {
                var user = UserIds.FirstOrDefault(x => x == userId);
                UserIds.Remove(user);
            }
        }
    }
}
