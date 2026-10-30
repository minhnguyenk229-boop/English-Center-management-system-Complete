using System;

namespace English_language_center_management_system.Models.ViewModels.Enrollments
{
    public class StudentEnrollViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public string CourseName { get; set; }
        public decimal TuitionFee { get; set; }
        public string ScheduleDays { get; set; }
        public TimeSpan? DailyStartTime { get; set; }
        public TimeSpan? DailyEndTime { get; set; }
        public DateTime? StartDate { get; set; }
    }
}