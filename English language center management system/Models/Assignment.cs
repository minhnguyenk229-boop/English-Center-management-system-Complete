using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("assignments")]
    public class Assignment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("class_id")]
        public int ClassId { get; set; }

        [Required]
        [StringLength(255)]
        [Column("title")]
        public string Title { get; set; }

        [Required]
        [Column("due_date")]
        public DateTime DueDate { get; set; }

        // Navigation properties
        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; }

        public virtual ICollection<Submission> Submissions { get; set; }
    }
}