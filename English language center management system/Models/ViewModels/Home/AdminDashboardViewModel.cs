using System.Collections.Generic;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Home
{
    public class AdminDashboardViewModel
    {
        // Các chỉ số tổng quan
        public decimal TotalRevenue { get; set; }

        public int TotalStudents { get; set; }

        public int ActiveStudents { get; set; }

        public int TotalTeachers { get; set; }

        public int TotalUsers { get; set; }

        public int TotalCourses { get; set; }

        public int ActiveClasses { get; set; }

        public int PendingEnrollments { get; set; }

        // Doanh thu 6 tháng gần nhất
        public List<MonthlyRevenuePointViewModel> MonthlyRevenue { get; set; }

        // Danh sách lớp gần đầy
        public List<AdminClassCapacityViewModel> ClassesNearCapacity { get; set; }

        // Các danh sách gần đây
        public List<Enrollment> RecentEnrollments { get; set; }

        public List<Payment> RecentPayments { get; set; }
    }

    public class MonthlyRevenuePointViewModel
    {
        public string MonthLabel { get; set; }

        public decimal Revenue { get; set; }
    }

    public class AdminClassCapacityViewModel
    {
        public int ClassId { get; set; }

        public string ClassName { get; set; }

        public string CourseName { get; set; }

        public string Room { get; set; }

        public int EnrolledCount { get; set; }

        public int MaxCapacity { get; set; }

        public decimal OccupancyPercent { get; set; }

        public string Status { get; set; }
    }
}