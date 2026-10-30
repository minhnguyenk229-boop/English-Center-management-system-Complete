namespace English_language_center_management_system.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Notnull : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.payments", "payment_date", c => c.DateTime());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.payments", "payment_date", c => c.DateTime(nullable: false));
        }
    }
}
