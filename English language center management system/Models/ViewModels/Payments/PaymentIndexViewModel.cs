using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models.ViewModels.Payments
{
    public class PaymentIndexItemViewModel
    {
        public Payment Payment { get; set; }
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public string CourseName { get; set; }
        public decimal TotalTuitionFee { get; set; }  // Học phí gốc của khóa học
        public decimal RemainingBalance { get; set; } // Dư nợ còn lại (Học phí - Số tiền đã đóng)
    }

    public class PaymentIndexViewModel
    {
        public List<PaymentIndexItemViewModel> Payments { get; set; }
        public string SearchTerm { get; set; }
        public string SelectedStatus { get; set; }
        public string SelectedMethod { get; set; }

        // Con số tổng doanh thu thực thu trên danh sách lọc
        public decimal TotalFilteredRevenue { get; set; }
    }
}