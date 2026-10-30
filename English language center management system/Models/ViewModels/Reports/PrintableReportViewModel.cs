using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models.ViewModels.Reports
{
    public class PrintableReportViewModel
    {
        public string ReportTitle { get; set; }
        public string CourseFilterName { get; set; }
        public string GeneratedBy { get; set; } // Tên Admin xuất báo cáo
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }

        public List<CourseRevenueSummaryItem> CourseSummaries { get; set; }
        public List<Payment> PaymentRecords { get; set; }
    }
}