using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using laba4_POP3_server;
using laba4_POP3_server_.DatabaseContext.Tables;
using laba4_POP3_server_.Sessions;

namespace laba4_POP3_server_
{
    class Session : ISession
    {
        private static List<int> userIds = new List<int>();

        private Socket client;
        private string userName;
        private int userId;
        private List<int> messagesForDelete;

        public Session(Socket client)
        {
            this.client = client;
        }

        private void Send(Status status, string message)
        {
            string str = "";
            switch (status)
            {
                case Status.OK:
                    str = "+OK";
                    break;
                case Status.ERR:
                    str = "-ERR";
                    break;
            }
            str += " " + message + "\r\n";
            var data = Encoding.UTF8.GetBytes(str);
            client.Send(data);
            Console.WriteLine("S: " + str);
        }

        private string ReceiveStr()
        {
            var data = new byte[client.ReceiveBufferSize];
            client.Receive(data);
            var str = Encoding.UTF8.GetString(data);
            return str.Substring(0, str.IndexOf('\0'));
        }

        private Commands Recieve(out string info)
        {
            var str = ReceiveStr();
            while (str.Length < 2 || !(str[str.Length - 2] == '\r' && str[str.Length - 1] == '\n'))
                str += ReceiveStr();
            Console.Write("C: " + str);
            info = null;
            try
            {
                var spaceIndex = str.IndexOfAny(new char[] {' ', '\r'});
                var strCommand = (spaceIndex < 0) ? str : str.Substring(0, spaceIndex);
                var command = CommandsInfo.GetCommands(strCommand);
                if (str.Length > strCommand.Length + 3)
                    info = str.Substring(strCommand.Length + 1, str.IndexOf('\r') - strCommand.Length - 1);
                return command;
            }
            catch (Exception)
            {
                return Commands.ERROR;
            }
        }

        private bool WaitCorrectCommand(params Commands[] correctCommands)
        {
            string info;
            Commands command = Recieve(out info);
            while (!correctCommands.Contains(command) && command != Commands.QUIT)
            {
                Send(Status.ERR, "invalid command");
                command = Recieve(out info);
            }
            return RunCommand(command, info);
        }

        private bool RunCommand(Commands command, string info)
        {
            switch (command)
            {
                case Commands.USER:
                    return EnterUser(info);
                case Commands.PASS:
                    return EnterPass(info);
                case Commands.QUIT:
                    return EnterQuit();
                case Commands.STAT:
                    return EnterStat();
                case Commands.RETR:
                    return EnterRetr(info);
                case Commands.DELE:
                    return EnterDele(info);
                case Commands.NOOP:
                    return EnterNoop();
                case Commands.LIST:
                    return EnterList(info);
                case Commands.RSET:
                    return EnterRset();
                default:
                    return false;
            }
        }

        private void Authorization()
        {
            while (!WaitCorrectCommand(Commands.USER)) { }
            if (userName == null)
                return;
            WaitPass();
        }

        private void WaitPass()
        {
            while (!WaitCorrectCommand(Commands.USER, Commands.PASS)) { }
            if (userName != null && userId == 0)
                WaitPass();
        }

        private bool EnterUser(string user)
        {
            if (CheckUser(user))
            { 
                Send(Status.OK, user + " is a real hoopy frood");
                userName = user;
                return true;
            }
            else
            {
                Send(Status.ERR, "sorry, no mailbox for " + user + " here");
                return false;
            }
        }

        private bool CheckUser(string user)
        {
            using (var db = new UserContext())
            {
                return (db.Users.FirstOrDefault(x => x.Name == user) != null);
            }
        }

        private bool EnterPass(string pass)
        {
            if (CheckPass(userName, pass))
            {
                return Login(userName, pass);
            }
            else
            {
                Send(Status.ERR, pass + "is invalid password for " + userName);
                return false;
            }
        }

        private bool CheckPass(string user, string pass)
        {
            using (var db = new UserContext())
            {
                return db.Users.FirstOrDefault(x => x.Name == user && x.Password == pass) != null;
            }
        }

        private bool Login(string user, string pass)
        {
            using (var db = new UserContext())
            {
                userId = db.Users.FirstOrDefault(x => x.Name == user && x.Password == pass).Id;
                if (userIds.Contains(userId))
                {
                    userId = 0;
                    Send(Status.ERR, "maildrop already locked");
                }
                else
                {
                    lock (client)
                    {
                        userIds.Add(userId);
                    }
                    messagesForDelete = new List<int>();
                    long size;
                    Send(Status.OK,
                        userName + "'s maildrop has " + RunStat(out size) + " messages (" + size + " octets)");
                }
                return userId != 0;
            }
        }

        private bool EnterQuit()
        {
            Send(Status.OK, "dewey POP3 server singing off");
            userName = null;
            if (userId != 0)
            {
                Update();
                lock (client)
                {
                    var user = userIds.FirstOrDefault(x => x == userId);
                    userIds.Remove(user);
                }
                userId = 0;
}
            return true;
        }
        
        private void Update()
        {
            using (var db = new UserContext())
            {
                foreach (var messageId in messagesForDelete)
                {
                    var message = db.Messages.FirstOrDefault(x => x.Id == messageId);
                    if (message != null)
                        db.Messages.Remove(message);
                }
                db.SaveChanges();
            }
        }

        private void Transaction()
        {
            while (userId != 0)
            {
                while (!WaitCorrectCommand(Commands.STAT, Commands.RETR, Commands.DELE, Commands.NOOP, Commands.LIST, Commands.RSET)) { }
            }
        }

        private void RunWithMessageNum(string strNum, Action<int, List<Message>> someCommand)
        {
            var num = GetNum(strNum);
            if (num != 0)
            {
                using (var db = new UserContext())
                {
                    var messages = GetExistedMessages(db);
                    if (num > messages.Count)
                        Send(Status.ERR, "no such message, only " + messages.Count + " message(s) in maildrop");
                    else
                        someCommand(num, messages);
                }
            }
        }

        private int GetNum(string strNum)
        {
            int num;
            if (Int32.TryParse(strNum, out num))
            {
                return num;
            }
            else
            {
                Send(Status.ERR, "invalid arguments");
                return 0;
            }
        }

        private bool EnterStat()
        {
            long size;
            Send(Status.OK, RunStat(out size) + " " + size);
            return true;
        }

        private bool EnterRetr(string strNum)
        {
            RunWithMessageNum(strNum, (num, messages) =>
            {
                Send(Status.OK, num + " " + messages[num - 1].Text.Length + "\r\n" + messages[num - 1].Text + "\r\n.");
            });
            return true;
        }

        private bool EnterDele(string strNum)
        {
            RunWithMessageNum(strNum, (num, messages) =>
            {
                messagesForDelete.Add(messages[num - 1].Id);
                Send(Status.OK, "message " + num + " deleted");
            });
            return true;
        }

        private bool EnterNoop()
        {
            Send(Status.OK, "");
            return true;
        }

        private bool EnterRset()
        {
            messagesForDelete = new List<int>();
            long size;
            Send(Status.OK, "maildrop has " + RunStat(out size) + " messages (" + size + " octets)");
            return true;
        }

        private bool EnterList(string strNum)
        {
            if (strNum == null)
            {
                long size = 0;
                var str = "";
                using (var db = new UserContext())
                {
                    var messages = GetExistedMessages(db);
                    for (int i = 0; i < messages.Count; i++)
                    {
                        size += messages[i].Text.Length;
                        str += "\r\n" + (i + 1) + " " + messages[i].Text.Length;
                    }
                    var header = userName + "'s maildrop has " + messages.Count + " messages (" + size + " octets)";
                    Send(Status.OK, header + str + "\r\n.");
                }
            }
            else
            {
                RunWithMessageNum(strNum, (num, messages) =>
                {
                    Send(Status.OK, num + " " + messages[num - 1].Text.Length);
                });
            }
            return true;
        }

        private int RunStat(out long size)
        {
            size = 0;
            using (var db = new UserContext())
            {
                var messages = GetExistedMessages(db);
                foreach (var message in messages)
                    size += message.Text.Length;
                return messages.Count();
            }
        }

        private List<Message> GetExistedMessages(UserContext db)
        {
            var messages = db.Messages.Where(x => x.GetterId == userId).ToList();
            foreach (var messageId in messagesForDelete)
            {
                var messageForDelete = messages.FirstOrDefault(x => x.Id == messageId);
                if (messageForDelete != null)
                    messages.Remove(messageForDelete);
            }
            return messages;
        }

        public void Work()
        {
            try
            {
                Send(Status.OK, "POP3 server ready");
                Authorization();
                Transaction();
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            finally
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
        }
    }
}
