using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace English_language_center_management_system.Models
{
    [Table("assessment_scores")]
    public class AssessmentScore
    {
        // enrollment_id vừa là Primary Key vừa là Foreign Key trỏ về Enrollment
        [Key, ForeignKey("Enrollment")]
        [Column("enrollment_id")]
        public int EnrollmentId { get; set; }

        [Column("midterm_score")]
        public decimal? MidtermScore { get; set; }

        [Column("final_score")]
        public decimal? FinalScore { get; set; }

        [Column("total_score")]
        public decimal? TotalScore { get; set; }

        [StringLength(50)]
        [Column("learning_outcome")]
        public string LearningOutcome { get; set; } //Xuất sắc/Đạt/ Không đạt 

        // Navigation property (1-1 với Enrollment)
        public virtual Enrollment Enrollment { get; set; }
    }
}