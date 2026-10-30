namespace English_language_center_management_system.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class fixcode : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.users", "phone", c => c.String(maxLength: 10));
            CreateIndex("dbo.classes", "class_name", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.classes", new[] { "class_name" });
            AlterColumn("dbo.users", "phone", c => c.String(maxLength: 20));
        }
    }
}
