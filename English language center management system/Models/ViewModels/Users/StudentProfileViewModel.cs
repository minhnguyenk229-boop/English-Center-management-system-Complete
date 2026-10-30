using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace English_language_center_management_system.Models.ViewModels.Users
{
    public class StudentProfileViewModel
    {
        public string Username { get; set; }
        public DateTime CreatedAt { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        public string Phone { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }
        public string Address { get; set; }

        // Thuộc tính xử lý Avatar
        public string ExistingAvatarUrl { get; set; }
        public HttpPostedFileBase AvatarFile { get; set; }
    }
}