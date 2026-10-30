using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Users
{
    public class UserIndexViewModel
    {
        public List<User> Users { get; set; }
        public string SearchTerm { get; set; }
        public int? SelectedRoleId { get; set; }
        public List<Role> Roles { get; set; }

        //Sắp xếp
        public string SortBy { get; set; } = "name"; // "name", "id", "created_at"
        public string SortOrder { get; set; } = "asc"; // "asc", "desc"

        // Thống kê
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalAdmins { get; set; }
    }
}