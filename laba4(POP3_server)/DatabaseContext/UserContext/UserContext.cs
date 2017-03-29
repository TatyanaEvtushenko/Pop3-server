using System.Data.Entity;
using laba4_POP3_server_.DatabaseContext.Tables;

namespace laba4_POP3_server_
{
    class UserContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }

        public UserContext() : base("DbConnection") { }
    }
}
