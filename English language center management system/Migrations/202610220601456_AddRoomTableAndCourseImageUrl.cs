namespace English_language_center_management_system.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRoomTableAndCourseImageUrl : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.rooms",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        room_name = c.String(nullable: false, maxLength: 50),
                        capacity = c.Int(nullable: false),
                        description = c.String(maxLength: 255),
                        is_active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.id);
            
            AddColumn("dbo.classes", "room_id", c => c.Int());
            AddColumn("dbo.class_sessions", "room_id", c => c.Int());
            AddColumn("dbo.courses", "image_url", c => c.String(maxLength: 500));
            CreateIndex("dbo.classes", "room_id");
            CreateIndex("dbo.class_sessions", "room_id");
            AddForeignKey("dbo.class_sessions", "room_id", "dbo.rooms", "id");
            AddForeignKey("dbo.classes", "room_id", "dbo.rooms", "id");
            DropColumn("dbo.classes", "room");
            DropColumn("dbo.class_sessions", "room");
        }
        
        public override void Down()
        {
            AddColumn("dbo.class_sessions", "room", c => c.String(maxLength: 50));
            AddColumn("dbo.classes", "room", c => c.String(nullable: false, maxLength: 50));
            DropForeignKey("dbo.classes", "room_id", "dbo.rooms");
            DropForeignKey("dbo.class_sessions", "room_id", "dbo.rooms");
            DropIndex("dbo.class_sessions", new[] { "room_id" });
            DropIndex("dbo.classes", new[] { "room_id" });
            DropColumn("dbo.courses", "image_url");
            DropColumn("dbo.class_sessions", "room_id");
            DropColumn("dbo.classes", "room_id");
            DropTable("dbo.rooms");
        }
    }
}
