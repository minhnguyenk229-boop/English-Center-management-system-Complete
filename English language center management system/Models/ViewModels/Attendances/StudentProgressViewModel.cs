
using System.Collections.Generic;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Attendances
{
    public class StudentProgressViewModel
    {
        public User StudentInfo { get; set; }

        public List<Enrollment> Enrollments { get; set; }

        public int? SelectedEnrollmentId { get; set; }

        public Enrollment SelectedEnrollment { get; set; }

        public AssessmentScore Score { get; set; }

        public List<Attendance> AttendanceRecords { get; set; }

        public List<TeacherFeedback> TeacherFeedbacks { get; set; }

        public Certificate Certificate { get; set; }

        public int TotalAttendanceRecords { get; set; }

        public int PresentCount { get; set; }

        public int LateCount { get; set; }

        public int AbsentCount { get; set; }

        public int ExcusedCount { get; set; }

        public decimal AttendanceRate { get; set; }
    }
}
