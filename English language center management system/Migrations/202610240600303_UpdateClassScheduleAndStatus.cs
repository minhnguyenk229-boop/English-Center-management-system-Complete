namespace English_language_center_management_system.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateClassScheduleAndStatus : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.classes", "schedule_days", c => c.String(maxLength: 50));
            AddColumn("dbo.classes", "daily_start_time", c => c.Time(precision: 7));
            AddColumn("dbo.classes", "daily_end_time", c => c.Time(precision: 7));
            AddColumn("dbo.classes", "weekly_sessions_count", c => c.Int(nullable: false));
            AddColumn("dbo.classes", "start_date", c => c.DateTime());
            AddColumn("dbo.classes", "end_date", c => c.DateTime());
            AddColumn("dbo.classes", "registration_start_date", c => c.DateTime());
            AddColumn("dbo.classes", "registration_end_date", c => c.DateTime());
            AlterColumn("dbo.classes", "status", c => c.String(nullable: false, maxLength: 50));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.classes", "status", c => c.String(nullable: false, maxLength: 20));
            DropColumn("dbo.classes", "registration_end_date");
            DropColumn("dbo.classes", "registration_start_date");
            DropColumn("dbo.classes", "end_date");
            DropColumn("dbo.classes", "start_date");
            DropColumn("dbo.classes", "weekly_sessions_count");
            DropColumn("dbo.classes", "daily_end_time");
            DropColumn("dbo.classes", "daily_start_time");
            DropColumn("dbo.classes", "schedule_days");
        }
    }
}
