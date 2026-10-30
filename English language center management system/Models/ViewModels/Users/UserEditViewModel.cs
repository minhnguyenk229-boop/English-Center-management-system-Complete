using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace English_language_center_management_system.Models.ViewModels.Users
{
    public class UserEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        [Display(Name = "Vai trò")]
        public int RoleId { get; set; }

        [Display(Name = "Mật khẩu mới (bỏ trống nếu không muốn đổi)")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        // --- Dành cho Giáo viên ---
        [Display(Name = "Chuyên môn")]
        public string Specialization { get; set; }

        // --- Dành cho Học sinh ---
        [Display(Name = "Địa chỉ thường trú")]
        public string Address { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        // --- Ảnh đại diện ---
        public string ExistingAvatarUrl { get; set; }

        [Display(Name = "Ảnh đại diện mới")]
        public HttpPostedFileBase AvatarFile { get; set; }
    }
}