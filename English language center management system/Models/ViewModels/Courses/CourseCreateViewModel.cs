using System.ComponentModel.DataAnnotations;
using System.Web;

namespace English_language_center_management_system.Models.ViewModels.Courses
{
    public class CourseCreateEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khóa học")]
        [Display(Name = "Tên khóa học")]
        public string CourseName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trình độ")]
        [Display(Name = "Trình độ")]
        public string Level { get; set; }

        [Display(Name = "Mô tả khóa học")]
        public string Description { get; set; }

        // Công thức tính: SessionsPerWeek * HoursPerSession * TotalWeeks
        [Display(Name = "Số buổi học / tuần")]
        public int SessionsPerWeek { get; set; } = 3;

        [Display(Name = "Thời gian học 1 buổi (giờ)")]
        public double HoursPerSession { get; set; } = 1.5;

        [Display(Name = "Số tuần cần học")]
        public int TotalWeeks { get; set; } = 8;

        [Display(Name = "Thời lượng học (Tổng số giờ)")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập học phí")]
        [Display(Name = "Học phí (VNĐ)")]
        public decimal TuitionFee { get; set; }

        [Display(Name = "Giáo trình / Khung chương trình")]
        public string Syllabus { get; set; }

        [Display(Name = "Ẩn khóa học")]
        public bool IsHidden { get; set; }

        public string ExistingImageUrl { get; set; }

        [Display(Name = "Hình ảnh khóa học")]
        public HttpPostedFileBase ImageFile { get; set; }
    }
}