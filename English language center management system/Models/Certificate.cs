using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("certificates")]
    public class Certificate
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("enrollment_id")]
        public int EnrollmentId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("certificate_code")]
        [Index(IsUnique = true)] // Mã chứng chỉ là duy nhất
        public string CertificateCode { get; set; }

        [Required]
        [Column("issue_date", TypeName = "date")]
        public DateTime IssueDate { get; set; }

        // Navigation property
        [ForeignKey("EnrollmentId")]
        public virtual Enrollment Enrollment { get; set; }
    }
}