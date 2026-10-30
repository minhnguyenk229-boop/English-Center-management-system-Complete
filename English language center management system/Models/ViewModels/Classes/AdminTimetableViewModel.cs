using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Classes
{
    public class AdminTimetableViewModel
    {
        // 1. Danh sách các buổi học trong tuần được chọn để render lên lưới (8:00 - 21:00)
        public List<ClassSession> Sessions { get; set; } = new List<ClassSession>();

        // 2. Các tham số Bộ lọc (Filter) đang được chọn
        [Display(Name = "Khóa học / Môn học")]
        public int? SelectedCourseId { get; set; }

        [Display(Name = "Lớp học")]
        public int? SelectedClassId { get; set; }

        // 3. Danh sách dữ liệu nạp vào DropdownList trên thanh công cụ lọc
        public List<Course> Courses { get; set; } = new List<Course>();
        public List<Class> Classes { get; set; } = new List<Class>();

        // 4. Các biến lưu trữ ngày bắt đầu/kết thúc của tuần được chọn & điều hướng tuần
        [Display(Name = "Ngày đầu tuần (Thứ 2)")]
        [DataType(DataType.Date)]
        public DateTime WeekStart { get; set; }

        [Display(Name = "Ngày cuối tuần (Chủ Nhật)")]
        [DataType(DataType.Date)]
        public DateTime WeekEnd { get; set; }

        // Độ lệch tuần so với tuần hiện tại (0: tuần này, -1: tuần trước, +1: tuần sau)
        public int WeekOffset { get; set; } = 0;

        // 5. Phục vụ Nghiệp vụ: Cảnh báo xung đột trực quan (Conflict Visualization)
        // Lưu danh sách ID các buổi học (ClassSession.Id) bị trùng Phòng học hoặc Giáo viên
        public List<int> RoomConflictSessionIds { get; set; } = new List<int>();
        public List<int> TeacherConflictSessionIds { get; set; } = new List<int>();

        // 6. Các chỉ số thống kê nhanh trên đầu bảng Thời khóa biểu
        public int TotalSessionsInWeek { get; set; }
        public int AttendedSessionsCount { get; set; } // Số buổi đã điểm danh
        public int ConflictSessionsCount { get; set; } // Số buổi đang bị trùng lịch
    }
}