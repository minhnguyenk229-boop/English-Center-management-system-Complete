using System.Data.Entity;

namespace English_language_center_management_system.Models
{
    public class EnglishCenterDbContext : DbContext
    {
        public EnglishCenterDbContext() : base("name=EnglishCenterDBContext")
        {
        }

        // Module 1: User & Role Management
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<TeacherProfile> TeacherProfiles { get; set; }
        public DbSet<StudentProfile> StudentProfiles { get; set; }

        // Module 2: Course & Class Management
        public DbSet<Course> Courses { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<ClassSession> ClassSessions { get; set; }
        public DbSet<Room> Rooms { get; set; }

        // Module 3: Enrollment & Payment Management
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Payment> Payments { get; set; }

        // Module 4: Attendance & Score Management
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<AssessmentScore> AssessmentScores { get; set; }

        // Module 5: Extended Features
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<CourseMaterial> CourseMaterials { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<TeacherFeedback> TeacherFeedbacks { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ----------------------------------------------------
            // MODULE 1 CONFIGURATIONS
            // ----------------------------------------------------
            modelBuilder.Entity<User>()
                .HasOptional(u => u.TeacherProfile)
                .WithRequired(tp => tp.User);

            modelBuilder.Entity<User>()
                .HasOptional(u => u.StudentProfile)
                .WithRequired(sp => sp.User);

            // ----------------------------------------------------
            // MODULE 2 CONFIGURATIONS
            // ----------------------------------------------------
            modelBuilder.Entity<Course>()
                .Property(c => c.TuitionFee)
                .HasPrecision(12, 2);

            modelBuilder.Entity<Class>()
                .HasOptional(c => c.Teacher)
                .WithMany(u => u.TaughtClasses)
                .HasForeignKey(c => c.TeacherId)
                .WillCascadeOnDelete(false);

            // ----------------------------------------------------
            // MODULE 3 CONFIGURATIONS
            // ----------------------------------------------------
            modelBuilder.Entity<Payment>()
                .Property(p => p.AmountPaid)
                .HasPrecision(12, 2);

            // ----------------------------------------------------
            // MODULE 4 CONFIGURATIONS
            // ----------------------------------------------------
            modelBuilder.Entity<AssessmentScore>()
                .Property(a => a.MidtermScore)
                .HasPrecision(4, 2);

            modelBuilder.Entity<AssessmentScore>()
                .Property(a => a.FinalScore)
                .HasPrecision(4, 2);

            modelBuilder.Entity<AssessmentScore>()
                .Property(a => a.TotalScore)
                .HasPrecision(4, 2);

            modelBuilder.Entity<Attendance>()
                .HasRequired(a => a.Student)
                .WithMany(u => u.Attendances)
                .HasForeignKey(a => a.StudentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Enrollment>()
                .HasOptional(e => e.AssessmentScore)
                .WithRequired(a => a.Enrollment);

            // ----------------------------------------------------
            // MODULE 5 CONFIGURATIONS
            // ----------------------------------------------------
            modelBuilder.Entity<Submission>()
                .Property(s => s.Grade)
                .HasPrecision(4, 2);

            // Ngắt vòng lặp Cascade Delete giữa User và Submissions
            modelBuilder.Entity<Submission>()
                .HasRequired(s => s.Student)
                .WithMany(u => u.Submissions)
                .HasForeignKey(s => s.StudentId)
                .WillCascadeOnDelete(false);

            // Ngắt vòng lặp Cascade Delete giữa Teacher (User) và TeacherFeedback
            modelBuilder.Entity<TeacherFeedback>()
                .HasRequired(tf => tf.Teacher)
                .WithMany(u => u.GivenFeedbacks)
                .HasForeignKey(tf => tf.TeacherId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Class>()
                .HasOptional(c => c.Room)
                .WithMany(r => r.Classes)
                .HasForeignKey(c => c.RoomId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ClassSession>()
                .HasOptional(cs => cs.Room)
                .WithMany(r => r.ClassSessions)
                .HasForeignKey(cs => cs.RoomId)
                .WillCascadeOnDelete(false);


        }
    }
}