using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Courses
{
    public class CourseDetailViewModel
    {
        public Course Course { get; set; }

        // Danh sách các lớp thuộc khóa học này đang mở đăng ký
        public List<Class> AvailableClasses { get; set; }
    }
}