using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models.ViewModels.Enrollments
{
    public class EnrollmentIndexItemViewModel
    {
        public Enrollment Enrollment { get; set; }
        public decimal AmountPaid { get; set; }        // Số tiền học viên đã đóng
        public string PaymentStatus { get; set; }     // Trạng thái thanh toán (Paid/Pending/Partial)
    }

    public class EnrollmentIndexViewModel
    {
        public List<EnrollmentIndexItemViewModel> Enrollments { get; set; }
        public string SearchTerm { get; set; }
        public int? SelectedClassId { get; set; }
        public string SelectedStatus { get; set; }
        public List<Class> Classes { get; set; }
    }
}