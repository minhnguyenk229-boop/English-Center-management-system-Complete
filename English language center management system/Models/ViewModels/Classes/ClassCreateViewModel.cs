using System;
using System.ComponentModel.DataAnnotations;

namespace English_language_center_management_system.Models.ViewModels.Classes
{
    public class ClassCreateViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khóa học")]
        [Display(Name = "Khóa học")]
        public int CourseId { get; set; }

        [Display(Name = "Giáo viên phụ trách")]
        public int? TeacherId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên lớp học")]
        [Display(Name = "Tên lớp học")]
        public string ClassName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng học")]
        [Display(Name = "Phòng học")]
        public int? RoomId { get; set; }

        [Display(Name = "Sĩ số tối đa (Tự động theo phòng)")]
        public int MaxCapacity { get; set; }

        // --- Lịch học ---
        [Required(ErrorMessage = "Vui lòng chọn thứ học trong tuần")]
        [Display(Name = "Thứ học trong tuần (VD: 2,4,6)")]
        public string ScheduleDays { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu")]
        [Display(Name = "Giờ bắt đầu")]
        public string DailyStartTimeStr { get; set; }

        [Display(Name = "Giờ kết thúc (Tự động tính)")]
        public string DailyEndTimeStr { get; set; }

        [Display(Name = "Số lượng buổi/tuần")]
        public int WeeklySessionsCount { get; set; }

        [Display(Name = "Mô tả lịch học")]
        public string ScheduleDescription { get; set; }

        // --- Khung thời gian học ---
        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu học")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày bắt đầu học")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày kết thúc học (Tự động tính)")]
        public DateTime? EndDate { get; set; }

        // --- Thời gian mở đăng ký ---
        [DataType(DataType.Date)]
        [Display(Name = "Thời gian bắt đầu đăng ký")]
        public DateTime? RegistrationStartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Thời gian kết thúc đăng ký")]
        public DateTime? RegistrationEndDate { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Mở đăng ký";
    }
}