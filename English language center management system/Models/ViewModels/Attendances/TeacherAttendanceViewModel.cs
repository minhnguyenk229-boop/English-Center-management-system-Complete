using English_language_center_management_system.Models;
using System.Collections.Generic;

namespace English_language_center_management_system.Models.ViewModels.Attendances
{
    public class TeacherAttendanceViewModel
    {
        public User TeacherInfo { get; set; }

        // Danh sách các buổi học của các lớp giáo viên phụ trách để hiển thị lên Dropdown
        public List<ClassSession> AvailableSessions { get; set; }

        public int? SelectedSessionId { get; set; }
        public ClassSession SelectedSession { get; set; }

        // Danh sách học viên trong buổi học để điểm danh
        public List<TeacherAttendanceRowViewModel> Students { get; set; }
    }

    public class TeacherAttendanceRowViewModel
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }

        // Trạng thái: Present, Late, Absent, Excused
        public string Status { get; set; }

        // Lý do đi trễ / vắng phép
        public string Notes { get; set; }
    }
}