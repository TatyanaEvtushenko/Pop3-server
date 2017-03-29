namespace laba4_POP3_server_.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addmessages : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Messages",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GetterId = c.Int(nullable: false),
                        SenderId = c.Int(nullable: false),
                        Text = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Messages");
        }
    }
}
