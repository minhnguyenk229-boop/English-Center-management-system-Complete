using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Classes
{
    public class ClassIndexItemViewModel
    {
        public Class Class { get; set; }
        public int EnrolledCount { get; set; } // Số học viên đã ghi danh thành công
    }

    public class ClassIndexViewModel
    {
        public List<ClassIndexItemViewModel> Classes { get; set; }
        public string SearchTerm { get; set; }
        public int? SelectedCourseId { get; set; }
        public string SelectedStatus { get; set; }
        public List<Course> Courses { get; set; }
    }
}