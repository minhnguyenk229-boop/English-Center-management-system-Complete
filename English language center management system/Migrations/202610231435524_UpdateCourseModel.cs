namespace English_language_center_management_system.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateCourseModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.courses", "sessions_per_week", c => c.Int(nullable: false));
            AddColumn("dbo.courses", "hours_per_session", c => c.Double(nullable: false));
            AddColumn("dbo.courses", "total_weeks", c => c.Int(nullable: false));
            AddColumn("dbo.courses", "is_hidden", c => c.Boolean(nullable: false));
            AddColumn("dbo.courses", "created_at", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.courses", "created_at");
            DropColumn("dbo.courses", "is_hidden");
            DropColumn("dbo.courses", "total_weeks");
            DropColumn("dbo.courses", "hours_per_session");
            DropColumn("dbo.courses", "sessions_per_week");
        }
    }
}
