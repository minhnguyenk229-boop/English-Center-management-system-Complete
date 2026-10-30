using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("submissions")]
    public class Submission
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("assignment_id")]
        public int AssignmentId { get; set; }

        [Required]
        [Column("student_id")]
        public int StudentId { get; set; }

        [Required]
        [StringLength(500)]
        [Column("file_url")]
        public string FileUrl { get; set; }

        [Column("submitted_at")]
        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        [Column("grade")]
        public decimal? Grade { get; set; }

        // Navigation properties
        [ForeignKey("AssignmentId")]
        public virtual Assignment Assignment { get; set; }

        [ForeignKey("StudentId")]
        public virtual User Student { get; set; }
    }
}