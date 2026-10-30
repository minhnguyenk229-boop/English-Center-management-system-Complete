using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("enrollments")]
    public class Enrollment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("student_id")]
        [Index("IX_Student_Class", 1, IsUnique = true)] // Đảm bảo 1 học viên không đăng ký 2 lần cho 1 lớp
        public int StudentId { get; set; }

        [Required]
        [Column("class_id")]
        [Index("IX_Student_Class", 2, IsUnique = true)]
        public int ClassId { get; set; }

        [Column("enrollment_date")]
        public DateTime EnrollmentDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Studying, Completed, Withdrawn

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual User Student { get; set; }

        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; }

        public virtual ICollection<Payment> Payments { get; set; }

        public virtual AssessmentScore AssessmentScore { get; set; }

        // Thuộc tính bổ sung cho Module 5
        public virtual ICollection<Certificate> Certificates { get; set; }
        public virtual ICollection<TeacherFeedback> TeacherFeedbacks { get; set; }
    }
}