using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Home
{
    public class TeacherDashboardViewModel
    {
        // Thông tin cá nhân giáo viên
        public User TeacherInfo { get; set; }
        public TeacherProfile Profile { get; set; }

        // Các con số thống kê nhanh
        public int TotalAssignedClasses { get; set; }       // Số lớp phụ trách
        public int TotalStudentsTaught { get; set; }        // Tổng số học viên giảng dạy

        // Danh sách lớp học đang phụ trách
        public List<Class> AssignedClasses { get; set; }

        // Danh sách các buổi dạy sắp tới (Lịch dạy)
        public List<ClassSession> UpcomingTeachingSessions { get; set; }
        // Bài tập và bài nộp cần giáo viên theo dõi
        public int TotalAssignments { get; set; }

        public int PendingSubmissionsCount { get; set; }

        public List<Submission> PendingSubmissions { get; set; }

        public List<Assignment> UpcomingAssignments { get; set; }
    }
}