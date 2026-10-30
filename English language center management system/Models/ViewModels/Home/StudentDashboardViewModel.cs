using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Home
{
    public class StudentDashboardViewModel
    {
        // Thông tin cá nhân học viên
        public User StudentInfo { get; set; }
        public StudentProfile Profile { get; set; }

        // Các con số thống kê nhanh
        public int TotalEnrolledClasses { get; set; }       // Số lớp đã/đang học
        public int TotalCertificates { get; set; }          // Số chứng chỉ đã nhận

        // Danh sách lớp học đang tham gia
        public List<Enrollment> ActiveEnrollments { get; set; }

        // Danh sách các buổi học sắp tới (Lịch học)
        public List<ClassSession> UpcomingSessions { get; set; }
        // Thông tin học phí của các lớp đã duyệt hoặc đang học
        public decimal TotalTuitionFee { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal OutstandingBalance { get; set; }

        // Thông báo gần đây của học viên
        public List<Notification> RecentNotifications { get; set; }

        // Kết quả đánh giá gần đây
        public List<AssessmentScore> RecentScores { get; set; }
    }
}