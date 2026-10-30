using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("notifications")]
    public class Notification
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        [Column("title")]
        public string Title { get; set; }

        [Required]
        [Column("message", TypeName = "text")]
        public string Message { get; set; }

        [StringLength(50)]
        [Column("type")]
        public string Type { get; set; } // Class_Schedule, Payment_Deadline, Score_Release, General

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}