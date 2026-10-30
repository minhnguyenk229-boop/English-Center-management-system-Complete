using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Home
{
    public class HomeViewModel
    {
        // Danh sách các khóa học nổi bật hiển thị ở Trang chủ
        public List<Course> FeaturedCourses { get; set; }

        // Các con số thống kê ấn tượng ở Banner/Thống kê trung tâm
        public int TotalCoursesCount { get; set; }
        public int TotalStudentsCount { get; set; }
        public int TotalTeachersCount { get; set; }
        public int TotalActiveClassesCount { get; set; }
    }
}