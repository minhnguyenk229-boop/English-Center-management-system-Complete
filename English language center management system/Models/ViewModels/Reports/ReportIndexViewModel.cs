using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models.ViewModels.Reports
{
    // Dữ liệu tổng hợp doanh thu theo từng Khóa học
    public class CourseRevenueSummaryItem
    {
        public string CourseName { get; set; }
        public int TotalClasses { get; set; }
        public int TotalEnrollments { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // Dữ liệu tổng hợp theo Phương thức thanh toán
    public class PaymentMethodSummaryItem
    {
        public string PaymentMethod { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ReportIndexViewModel
    {
        // Bộ lọc khoảng thời gian
        [Display(Name = "Từ ngày")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "Đến ngày")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        public int? SelectedCourseId { get; set; }
        public List<Course> Courses { get; set; }

        // Các chỉ số KPI tổng quan
        public decimal TotalRevenue { get; set; }          // Tổng doanh thu thực thu
        public int TotalEnrollments { get; set; }          // Tổng số lượt ghi danh
        public int TotalActiveClasses { get; set; }        // Tổng số lớp đang mở/đang học
        public int TotalStudents { get; set; }             // Tổng số học viên

        // Bảng dữ liệu phân tích
        public List<CourseRevenueSummaryItem> CourseSummaries { get; set; }
        public List<PaymentMethodSummaryItem> PaymentMethodSummaries { get; set; }
        public List<Payment> PaymentDetails { get; set; }   // Danh sách chi tiết các giao dịch trong kỳ
    }
}