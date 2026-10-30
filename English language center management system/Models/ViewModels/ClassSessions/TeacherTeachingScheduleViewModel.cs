
using System;
using System.Collections.Generic;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.ClassSessions
{
    public class TeacherTeachingScheduleViewModel
    {
        public User TeacherInfo { get; set; }

        public List<Class> AssignedClasses { get; set; }

        public List<ClassSession> Sessions { get; set; }

        public Dictionary<int, int> EnrolledCounts { get; set; }

        public DateTime WeekStart { get; set; }

        public DateTime WeekEnd { get; set; }

        public int WeekOffset { get; set; }

        public int? SelectedClassId { get; set; }
    }
}
