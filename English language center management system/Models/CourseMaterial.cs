using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("course_materials")]
    public class CourseMaterial
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
        [StringLength(500)]
        [Column("file_url")]
        public string FileUrl { get; set; }

        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; }
    }
}