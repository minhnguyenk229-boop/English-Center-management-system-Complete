using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("attendances")]
    public class Attendance
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("session_id")]
        [Index("IX_Session_Student", 1, IsUnique = true)] // Đảm bảo mỗi học viên chỉ điểm danh 1 lần / buổi học
        public int SessionId { get; set; }

        [Required]
        [Column("student_id")]
        [Index("IX_Session_Student", 2, IsUnique = true)]
        public int StudentId { get; set; }

        [Required]
        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } // Present, Absent, Late, Excused

        [StringLength(255)]
        [Column("notes")]
        public string Notes { get; set; }

        // Navigation properties
        [ForeignKey("SessionId")]
        public virtual ClassSession ClassSession { get; set; }

        [ForeignKey("StudentId")]
        public virtual User Student { get; set; }
    }
}