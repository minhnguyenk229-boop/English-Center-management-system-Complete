using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("teacher_feedbacks")]
    public class TeacherFeedback
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("enrollment_id")]
        public int EnrollmentId { get; set; }

        [Required]
        [Column("teacher_id")]
        public int TeacherId { get; set; }

        [Required]
        [Column("comments")]
        public string Comments { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("EnrollmentId")]
        public virtual Enrollment Enrollment { get; set; }

        [ForeignKey("TeacherId")]
        public virtual User Teacher { get; set; }
    }
}