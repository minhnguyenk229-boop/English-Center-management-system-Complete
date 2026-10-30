using System;
using System.Collections.Generic;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Assigments
{
    public class TeacherSubmissionsViewModel
    {
        public List<Class> AvailableClasses { get; set; }

        public List<Assignment> AvailableAssignments { get; set; }

        public int? SelectedClassId { get; set; }

        public int? SelectedAssignmentId { get; set; }

        public Class SelectedClass { get; set; }

        public List<TeacherSubmissionRowViewModel> Submissions { get; set; }
    }

    public class TeacherSubmissionRowViewModel
    {
        public int SubmissionId { get; set; }

        public int AssignmentId { get; set; }

        public string AssignmentTitle { get; set; }

        public string StudentName { get; set; }

        public string StudentUsername { get; set; }

        public string FileUrl { get; set; }

        public DateTime SubmittedAt { get; set; }

        public DateTime DueDate { get; set; }

        public decimal? Grade { get; set; }
    }
}