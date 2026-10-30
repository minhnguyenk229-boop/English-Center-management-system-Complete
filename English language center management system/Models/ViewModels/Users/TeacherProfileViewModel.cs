using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace English_language_center_management_system.Models.ViewModels.Users
{
    public class TeacherProfileViewModel
    {
        public string Username { get; set; }
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        public string Phone { get; set; }
        public string Specialization { get; set; }
        public string Bio { get; set; }

        // Quản lý ảnh đại diện và file chứng chỉ
        public string ExistingAvatarUrl { get; set; }
        public HttpPostedFileBase AvatarFile { get; set; }

        public string ExistingCertificateUrls { get; set; }
        public IEnumerable<HttpPostedFileBase> CertificateFiles { get; set; }

        // Thống kê nhanh (Tùy chọn hiển thị lên giao diện)
        public int TotalAssignedClasses { get; set; }
        public int TotalTeachingHours { get; set; }
    }
}