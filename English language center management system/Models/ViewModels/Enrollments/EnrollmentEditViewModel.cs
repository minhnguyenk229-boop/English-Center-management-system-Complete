using System;
using System.ComponentModel.DataAnnotations;

namespace English_language_center_management_system.Models.ViewModels.Enrollments
{
    public class EnrollmentEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn học viên")]
        [Display(Name = "Học viên")]
        public int StudentId { get; set; }

        [Display(Name = "Tên Học viên")]
        public string StudentName { get; set; } // Hỗ trợ hiển thị trên view chỉ đọc (readonly)

        [Required(ErrorMessage = "Vui lòng chọn lớp học")]
        [Display(Name = "Lớp học")]
        public int ClassId { get; set; }

        [Display(Name = "Tên Lớp học")]
        public string ClassName { get; set; } // Hỗ trợ hiển thị trên view chỉ đọc (readonly)

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        [Display(Name = "Trạng thái ghi danh")]
        public string Status { get; set; }

        [Display(Name = "Ngày ghi danh")]
        public DateTime EnrollmentDate { get; set; }
    }
}