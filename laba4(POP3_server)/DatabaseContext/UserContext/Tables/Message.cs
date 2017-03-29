namespace laba4_POP3_server_.DatabaseContext.Tables
{
    class Message
    {
        public int Id { get; set; }
        public int GetterId { get; set; }
        public int SenderId { get; set; }
        public string Text { get; set; }
    }
}
