using System;
using System.ComponentModel.DataAnnotations;

namespace English_language_center_management_system.Models.ViewModels.Enrollments
{
    public class EnrollmentCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn học viên")]
        [Display(Name = "Học viên")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lớp học")]
        [Display(Name = "Lớp học")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái ghi danh")]
        [Display(Name = "Trạng thái ghi danh")]
        public string Status { get; set; } = "Pending"; // Mặc định là Pending chờ duyệt hoặc thanh toán

        [Display(Name = "Ngày ghi danh")]
        public DateTime EnrollmentDate { get; set; } = DateTime.Now;
    }
}