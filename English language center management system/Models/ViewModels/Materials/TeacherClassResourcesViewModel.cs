using System;
using System.Collections.Generic;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Materials
{
    public class TeacherClassResourcesViewModel
    {
        public List<Class> AvailableClasses { get; set; }

        public int? SelectedClassId { get; set; }

        public Class SelectedClass { get; set; }

        public List<Assignment> Assignments { get; set; }

        public List<CourseMaterial> Materials { get; set; }

        // Dữ liệu tạo bài tập
        public string NewAssignmentTitle { get; set; }

        public DateTime? NewAssignmentDueDate { get; set; }

        // Dữ liệu đăng tài liệu
        public string NewMaterialTitle { get; set; }

        public string NewMaterialFileUrl { get; set; }
    }
}