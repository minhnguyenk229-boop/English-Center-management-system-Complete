using System.Collections.Generic;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.AssessmentScores
{
    public class TeacherGradesViewModel
    {
        public List<Class> AvailableClasses { get; set; }

        public int? SelectedClassId { get; set; }

        public Class SelectedClass { get; set; }

        public List<TeacherGradeRowViewModel> Students { get; set; }
    }

    public class TeacherGradeRowViewModel
    {
        public int EnrollmentId { get; set; }

        public int StudentId { get; set; }

        public string FullName { get; set; }

        public string Username { get; set; }

        public decimal? MidtermScore { get; set; }

        public decimal? FinalScore { get; set; }

        public decimal? TotalScore { get; set; }

        public string LearningOutcome { get; set; }

        public string FeedbackComments { get; set; }
    }
}