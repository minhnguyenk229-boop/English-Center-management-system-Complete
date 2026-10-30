using System.ComponentModel.DataAnnotations;

namespace English_language_center_management_system.Models.ViewModels.Users
{
    public class UserConfirmDeleteViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string RoleName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu Admin để xác nhận xóa")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu Admin xác nhận")]
        public string AdminPassword { get; set; }
    }
}