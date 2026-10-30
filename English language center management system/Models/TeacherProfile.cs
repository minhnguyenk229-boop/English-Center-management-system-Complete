using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("teacher_profiles")]
    public class TeacherProfile
    {
        [Key, ForeignKey("User")]
        [Column("user_id")]
        public int UserId { get; set; }

        [StringLength(100)]
        [Column("specialization")]
        [Display(Name = "Chuyên môn / Bằng cấp")]
        public string Specialization { get; set; }

        [Column("bio", TypeName = "text")]
        [Display(Name = "Tiểu sử")]
        public string Bio { get; set; }

        [Column("avatar_url")]
        [Display(Name = "Ảnh đại diện")]
        public string AvatarUrl { get; set; }

        // Thêm vào Models/TeacherProfile.cs
        [Column("certificate_urls", TypeName = "text")]
        public string CertificateUrls { get; set; }

        // Navigation property (1 - 1 với User)
        public virtual User User { get; set; }
    }
}