namespace laba4_POP3_server
{
    public enum Commands { USER, PASS, QUIT, STAT, RETR, DELE, NOOP, LIST, RSET, ERROR }

    static class CommandsInfo
    {
        private static string[] names = { "USER", "PASS", "QUIT", "STAT", "RETR", "DELE", "NOOP", "LIST", "RSET" };

        public static Commands GetCommands(string str)
        {
            for (var i = 0; i < names.Length; i++)
                if (str.ToUpper() == names[i])
                    return (Commands)i;
            return Commands.ERROR;
        }

        public static string GetString(Commands command)
        {
            return (int)command <= names.Length ? names[(int)command] : null;
        }
    }
}
