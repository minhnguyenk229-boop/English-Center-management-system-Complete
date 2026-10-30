using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("role_id")]
        public int RoleId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("username")]
        public string Username { get; set; }

        [Required]
        [StringLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(100)]
        [Column("full_name")]
        public string FullName { get; set; }

        [Required]
        [StringLength(100)]
        [Column("email")]
        public string Email { get; set; }

        
        [StringLength(10, ErrorMessage = "Số điện thoại không được vượt quá 10 ký tự.")]
        [Column("phone")]
        public string Phone { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }

        public virtual TeacherProfile TeacherProfile { get; set; }
        public virtual StudentProfile StudentProfile { get; set; }
        public virtual ICollection<Class> TaughtClasses { get; set; }
        public virtual ICollection<Enrollment> Enrollments { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<Submission> Submissions { get; set; }
        public virtual ICollection<TeacherFeedback> GivenFeedbacks { get; set; }
    }
}

