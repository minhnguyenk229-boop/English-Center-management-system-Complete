namespace English_language_center_management_system.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.assessment_scores",
                c => new
                    {
                        enrollment_id = c.Int(nullable: false),
                        midterm_score = c.Decimal(precision: 4, scale: 2),
                        final_score = c.Decimal(precision: 4, scale: 2),
                        total_score = c.Decimal(precision: 4, scale: 2),
                        learning_outcome = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.enrollment_id)
                .ForeignKey("dbo.enrollments", t => t.enrollment_id)
                .Index(t => t.enrollment_id);
            
            CreateTable(
                "dbo.enrollments",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        student_id = c.Int(nullable: false),
                        class_id = c.Int(nullable: false),
                        enrollment_date = c.DateTime(nullable: false),
                        status = c.String(nullable: false, maxLength: 20),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.users", t => t.student_id, cascadeDelete: true)
                .ForeignKey("dbo.classes", t => t.class_id, cascadeDelete: true)
                .Index(t => new { t.student_id, t.class_id }, unique: true, name: "IX_Student_Class");
            
            CreateTable(
                "dbo.certificates",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        enrollment_id = c.Int(nullable: false),
                        certificate_code = c.String(nullable: false, maxLength: 100),
                        issue_date = c.DateTime(nullable: false, storeType: "date"),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.enrollments", t => t.enrollment_id, cascadeDelete: true)
                .Index(t => t.enrollment_id)
                .Index(t => t.certificate_code, unique: true);
            
            CreateTable(
                "dbo.classes",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        course_id = c.Int(nullable: false),
                        teacher_id = c.Int(),
                        class_name = c.String(nullable: false, maxLength: 100),
                        room = c.String(nullable: false, maxLength: 50),
                        max_capacity = c.Int(nullable: false),
                        schedule_description = c.String(maxLength: 255),
                        status = c.String(nullable: false, maxLength: 20),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.courses", t => t.course_id, cascadeDelete: true)
                .ForeignKey("dbo.users", t => t.teacher_id)
                .Index(t => t.course_id)
                .Index(t => t.teacher_id);
            
            CreateTable(
                "dbo.assignments",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        class_id = c.Int(nullable: false),
                        title = c.String(nullable: false, maxLength: 255),
                        due_date = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.classes", t => t.class_id, cascadeDelete: true)
                .Index(t => t.class_id);
            
            CreateTable(
                "dbo.submissions",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        assignment_id = c.Int(nullable: false),
                        student_id = c.Int(nullable: false),
                        file_url = c.String(nullable: false, maxLength: 500),
                        submitted_at = c.DateTime(nullable: false),
                        grade = c.Decimal(precision: 4, scale: 2),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.assignments", t => t.assignment_id, cascadeDelete: true)
                .ForeignKey("dbo.users", t => t.student_id)
                .Index(t => t.assignment_id)
                .Index(t => t.student_id);
            
            CreateTable(
                "dbo.users",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        role_id = c.Int(nullable: false),
                        username = c.String(nullable: false, maxLength: 50),
                        password_hash = c.String(nullable: false, maxLength: 255),
                        full_name = c.String(nullable: false, maxLength: 100),
                        email = c.String(nullable: false, maxLength: 100),
                        phone = c.String(maxLength: 20),
                        created_at = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.roles", t => t.role_id, cascadeDelete: true)
                .Index(t => t.role_id);
            
            CreateTable(
                "dbo.attendances",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        session_id = c.Int(nullable: false),
                        student_id = c.Int(nullable: false),
                        status = c.String(nullable: false, maxLength: 20),
                        notes = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.class_sessions", t => t.session_id, cascadeDelete: true)
                .ForeignKey("dbo.users", t => t.student_id)
                .Index(t => new { t.session_id, t.student_id }, unique: true, name: "IX_Session_Student");
            
            CreateTable(
                "dbo.class_sessions",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        class_id = c.Int(nullable: false),
                        session_date = c.DateTime(nullable: false, storeType: "date"),
                        start_time = c.Time(nullable: false, precision: 7),
                        end_time = c.Time(nullable: false, precision: 7),
                        room = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.classes", t => t.class_id, cascadeDelete: true)
                .Index(t => t.class_id);
            
            CreateTable(
                "dbo.teacher_feedbacks",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        enrollment_id = c.Int(nullable: false),
                        teacher_id = c.Int(nullable: false),
                        comments = c.String(nullable: false, unicode: false, storeType: "text"),
                        created_at = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.enrollments", t => t.enrollment_id, cascadeDelete: true)
                .ForeignKey("dbo.users", t => t.teacher_id)
                .Index(t => t.enrollment_id)
                .Index(t => t.teacher_id);
            
            CreateTable(
                "dbo.notifications",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        user_id = c.Int(nullable: false),
                        title = c.String(nullable: false, maxLength: 200),
                        message = c.String(nullable: false, unicode: false, storeType: "text"),
                        type = c.String(maxLength: 50),
                        is_read = c.Boolean(nullable: false),
                        created_at = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.users", t => t.user_id, cascadeDelete: true)
                .Index(t => t.user_id);
            
            CreateTable(
                "dbo.roles",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        name = c.String(nullable: false, maxLength: 50),
                    })
                .PrimaryKey(t => t.id);
            
            CreateTable(
                "dbo.student_profiles",
                c => new
                    {
                        user_id = c.Int(nullable: false),
                        date_of_birth = c.DateTime(storeType: "date"),
                        address = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.user_id)
                .ForeignKey("dbo.users", t => t.user_id)
                .Index(t => t.user_id);
            
            CreateTable(
                "dbo.teacher_profiles",
                c => new
                    {
                        user_id = c.Int(nullable: false),
                        specialization = c.String(maxLength: 100),
                        bio = c.String(unicode: false, storeType: "text"),
                    })
                .PrimaryKey(t => t.user_id)
                .ForeignKey("dbo.users", t => t.user_id)
                .Index(t => t.user_id);
            
            CreateTable(
                "dbo.courses",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        course_name = c.String(nullable: false, maxLength: 150),
                        level = c.String(nullable: false, maxLength: 50),
                        description = c.String(unicode: false, storeType: "text"),
                        duration = c.Int(nullable: false),
                        tuition_fee = c.Decimal(nullable: false, precision: 12, scale: 2),
                        syllabus = c.String(unicode: false, storeType: "text"),
                    })
                .PrimaryKey(t => t.id);
            
            CreateTable(
                "dbo.course_materials",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        class_id = c.Int(nullable: false),
                        title = c.String(nullable: false, maxLength: 255),
                        file_url = c.String(nullable: false, maxLength: 500),
                        uploaded_at = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.classes", t => t.class_id, cascadeDelete: true)
                .Index(t => t.class_id);
            
            CreateTable(
                "dbo.payments",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        enrollment_id = c.Int(nullable: false),
                        amount_paid = c.Decimal(nullable: false, precision: 12, scale: 2),
                        payment_date = c.DateTime(nullable: false),
                        receipt_number = c.String(nullable: false, maxLength: 50),
                        payment_method = c.String(maxLength: 50),
                        payment_status = c.String(nullable: false, maxLength: 20),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.enrollments", t => t.enrollment_id, cascadeDelete: true)
                .Index(t => t.enrollment_id)
                .Index(t => t.receipt_number, unique: true);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.payments", "enrollment_id", "dbo.enrollments");
            DropForeignKey("dbo.classes", "teacher_id", "dbo.users");
            DropForeignKey("dbo.enrollments", "class_id", "dbo.classes");
            DropForeignKey("dbo.course_materials", "class_id", "dbo.classes");
            DropForeignKey("dbo.classes", "course_id", "dbo.courses");
            DropForeignKey("dbo.submissions", "student_id", "dbo.users");
            DropForeignKey("dbo.teacher_profiles", "user_id", "dbo.users");
            DropForeignKey("dbo.student_profiles", "user_id", "dbo.users");
            DropForeignKey("dbo.users", "role_id", "dbo.roles");
            DropForeignKey("dbo.notifications", "user_id", "dbo.users");
            DropForeignKey("dbo.teacher_feedbacks", "teacher_id", "dbo.users");
            DropForeignKey("dbo.teacher_feedbacks", "enrollment_id", "dbo.enrollments");
            DropForeignKey("dbo.enrollments", "student_id", "dbo.users");
            DropForeignKey("dbo.attendances", "student_id", "dbo.users");
            DropForeignKey("dbo.class_sessions", "class_id", "dbo.classes");
            DropForeignKey("dbo.attendances", "session_id", "dbo.class_sessions");
            DropForeignKey("dbo.submissions", "assignment_id", "dbo.assignments");
            DropForeignKey("dbo.assignments", "class_id", "dbo.classes");
            DropForeignKey("dbo.certificates", "enrollment_id", "dbo.enrollments");
            DropForeignKey("dbo.assessment_scores", "enrollment_id", "dbo.enrollments");
            DropIndex("dbo.payments", new[] { "receipt_number" });
            DropIndex("dbo.payments", new[] { "enrollment_id" });
            DropIndex("dbo.course_materials", new[] { "class_id" });
            DropIndex("dbo.teacher_profiles", new[] { "user_id" });
            DropIndex("dbo.student_profiles", new[] { "user_id" });
            DropIndex("dbo.notifications", new[] { "user_id" });
            DropIndex("dbo.teacher_feedbacks", new[] { "teacher_id" });
            DropIndex("dbo.teacher_feedbacks", new[] { "enrollment_id" });
            DropIndex("dbo.class_sessions", new[] { "class_id" });
            DropIndex("dbo.attendances", "IX_Session_Student");
            DropIndex("dbo.users", new[] { "role_id" });
            DropIndex("dbo.submissions", new[] { "student_id" });
            DropIndex("dbo.submissions", new[] { "assignment_id" });
            DropIndex("dbo.assignments", new[] { "class_id" });
            DropIndex("dbo.classes", new[] { "teacher_id" });
            DropIndex("dbo.classes", new[] { "course_id" });
            DropIndex("dbo.certificates", new[] { "certificate_code" });
            DropIndex("dbo.certificates", new[] { "enrollment_id" });
            DropIndex("dbo.enrollments", "IX_Student_Class");
            DropIndex("dbo.assessment_scores", new[] { "enrollment_id" });
            DropTable("dbo.payments");
            DropTable("dbo.course_materials");
            DropTable("dbo.courses");
            DropTable("dbo.teacher_profiles");
            DropTable("dbo.student_profiles");
            DropTable("dbo.roles");
            DropTable("dbo.notifications");
            DropTable("dbo.teacher_feedbacks");
            DropTable("dbo.class_sessions");
            DropTable("dbo.attendances");
            DropTable("dbo.users");
            DropTable("dbo.submissions");
            DropTable("dbo.assignments");
            DropTable("dbo.classes");
            DropTable("dbo.certificates");
            DropTable("dbo.enrollments");
            DropTable("dbo.assessment_scores");
        }
    }
}
