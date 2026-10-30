using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models.ViewModels.Courses
{
    public class CourseAdminClassItemViewModel
    {
        public Class Class { get; set; }
        public int EnrolledCount { get; set; }       // Số học viên hiện tại
        public decimal ClassRevenue { get; set; }   // Doanh thu thực thu của lớp
    }

    public class CourseAdminDetailViewModel
    {
        public Course Course { get; set; }

        // Thống kê chỉ số KPI của khóa học
        public int TotalClassesCount { get; set; }
        public int ActiveClassesCount { get; set; }
        public int TotalStudentsCount { get; set; }
        public decimal TotalRevenueGenerated { get; set; }

        // Danh sách tất cả các lớp thuộc khóa học này
        public List<CourseAdminClassItemViewModel> Classes { get; set; }
    }
}