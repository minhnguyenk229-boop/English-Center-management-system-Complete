using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Courses
{
    public class CourseAdminItemViewModel
    {
        public Course Course { get; set; }

        [Display(Name = "Tổng số lớp")]
        public int TotalClassesCount { get; set; }    

        [Display(Name = "Số lớp đang hoạt động")]
        public int ActiveClassesCount { get; set; }

        [Display(Name = "Số lượng học viên")]
        public int TotalStudentsCount { get; set; }     
    }

    public class CourseAdminIndexViewModel
    {
        public List<CourseAdminItemViewModel> Courses { get; set; }
        public string SearchTerm { get; set; }
        public string SelectedLevel { get; set; }
        public string SelectedStatus { get; set; }      // Tất cả / Active / Hidden

        // Thêm tham số phục vụ Sắp xếp
        public string SortBy { get; set; } = "name";    // "students", "classes", "tuition", "duration", "name"
        public string SortOrder { get; set; } = "asc";  // "asc", "desc"
    }
}