using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("payments")]
    public class Payment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("enrollment_id")]
        public int EnrollmentId { get; set; }

        [Required]
        [Column("amount_paid")]
        public decimal AmountPaid { get; set; }

        [Column("payment_date")]
        public DateTime? PaymentDate { get; set; }

        [Required]
        [StringLength(50)]
        [Column("receipt_number")]
        [Index(IsUnique = true)] // Mã biên lai là duy nhất
        public string ReceiptNumber { get; set; }

        [StringLength(50)]
        [Column("payment_method")]
        public string PaymentMethod { get; set; } // Cash, Bank Transfer, Credit Card

        [Required]
        [StringLength(20)]
        [Column("payment_status")]
        public string PaymentStatus { get; set; } // Paid, Partial, Pending

        // Navigation property
        [ForeignKey("EnrollmentId")]
        public virtual Enrollment Enrollment { get; set; }
    }
}