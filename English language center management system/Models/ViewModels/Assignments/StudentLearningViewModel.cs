
using System.Collections.Generic;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Assigments
{
    public class StudentLearningViewModel
    {
        public User StudentInfo { get; set; }

        public List<Assignment> Assignments { get; set; }

        public List<CourseMaterial> Materials { get; set; }

        public List<Submission> Submissions { get; set; }
    }
}
