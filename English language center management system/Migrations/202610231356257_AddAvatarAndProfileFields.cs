namespace English_language_center_management_system.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAvatarAndProfileFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.student_profiles", "avatar_url", c => c.String());
            AddColumn("dbo.teacher_profiles", "avatar_url", c => c.String());
            AlterColumn("dbo.student_profiles", "date_of_birth", c => c.DateTime());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.student_profiles", "date_of_birth", c => c.DateTime(storeType: "date"));
            DropColumn("dbo.teacher_profiles", "avatar_url");
            DropColumn("dbo.student_profiles", "avatar_url");
        }
    }
}
