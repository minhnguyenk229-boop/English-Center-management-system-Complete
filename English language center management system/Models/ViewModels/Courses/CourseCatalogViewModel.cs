using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Courses
{
    public class CourseCatalogViewModel
    {
        public List<Course> Courses { get; set; }

        // Các biến dùng để lưu trạng thái tìm kiếm/lọc trên giao diện
        public string SearchTerm { get; set; }
        public string SelectedLevel { get; set; }
    }
}