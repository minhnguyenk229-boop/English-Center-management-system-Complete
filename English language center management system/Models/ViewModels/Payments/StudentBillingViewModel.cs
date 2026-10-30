using System;
using System.Collections.Generic;

namespace English_language_center_management_system.Models.ViewModels.Payments
{
    public class StudentBillingViewModel
    {
        public User StudentInfo { get; set; }
        public List<Enrollment> Enrollments { get; set; }
        public List<Payment> PaymentHistory { get; set; }
        public decimal TotalTuitionFee { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal OutstandingBalance { get; set; }
        public int PendingEnrollmentCount { get; set; }
    }
}