namespace English_language_center_management_system.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updatecomments : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.teacher_feedbacks", "comments", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.teacher_feedbacks", "comments", c => c.String(nullable: false, unicode: false, storeType: "text"));
        }
    }
}
