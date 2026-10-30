using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("class_sessions")]
    public class ClassSession
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("class_id")]
        public int ClassId { get; set; }

        [Required]
        [Column("session_date", TypeName = "date")]
        public DateTime SessionDate { get; set; }

        [Required]
        [Column("start_time")]
        public TimeSpan StartTime { get; set; } // Kiểu TimeSpan tương ứng với TIME trong SQL

        [Required]
        [Column("end_time")]
        public TimeSpan EndTime { get; set; }

        [Column("room_id")]
        [Display(Name = "Phòng học buổi này")]
        public int? RoomId { get; set; }

        // Navigation property
        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; }
        [ForeignKey("RoomId")]
        public virtual Room Room { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; }
    }
}