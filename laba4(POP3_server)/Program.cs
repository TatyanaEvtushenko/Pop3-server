using System;

namespace laba4_POP3_server_
{
    class Program
    {
        static void Main(string[] args)
        {
            new POP3Server().Listen();
            Console.ReadKey();
        }
    }
}

