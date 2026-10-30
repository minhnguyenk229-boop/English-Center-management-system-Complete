
using System;
using System.Collections.Generic;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.ClassSessions
{
    public class StudentTimetableViewModel
    {
        public User StudentInfo { get; set; }

        public DateTime WeekStart { get; set; }

        public DateTime WeekEnd { get; set; }

        public int WeekOffset { get; set; }

        public List<ClassSession> Sessions { get; set; } = new List<ClassSession>();

        // THÊM 2 THUỘC TÍNH NÀY ĐỂ LỌC THEO MÔN HỌC ĐÃ ĐĂNG KÝ / ĐANG HỌC:
        public List<Course> EnrolledCourses { get; set; } = new List<Course>();
        public int? SelectedCourseId { get; set; }
    }
}
