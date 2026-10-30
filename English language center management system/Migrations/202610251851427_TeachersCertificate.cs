namespace English_language_center_management_system.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TeachersCertificate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.teacher_profiles", "certificate_urls", c => c.String(unicode: false, storeType: "text"));
        }
        
        public override void Down()
        {
            DropColumn("dbo.teacher_profiles", "certificate_urls");
        }
    }
}
