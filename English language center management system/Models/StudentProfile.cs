using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("student_profiles")]
    public class StudentProfile
    {
        [Key, ForeignKey("User")]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("date_of_birth")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(255)]
        [Column("address")]
        [Display(Name = "Địa chỉ thường trú")]
        public string Address { get; set; }

        [Column("avatar_url")]
        [Display(Name = "Ảnh đại diện")]
        public string AvatarUrl { get; set; }

        // Navigation property (1 - 1 với User)
        public virtual User User { get; set; }
    }
}