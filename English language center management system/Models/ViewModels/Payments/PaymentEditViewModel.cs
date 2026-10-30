using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models.ViewModels.Payments
{
    public class PaymentEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn đơn ghi danh học viên")]
        [Display(Name = "Học viên & Lớp học")]
        public int EnrollmentId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền thanh toán")]
        [Range(1000, 500000000, ErrorMessage = "Số tiền thanh toán không hợp lệ")]
        [Display(Name = "Số tiền thu (VNĐ)")]
        public decimal AmountPaid { get; set; }

        [Display(Name = "Ngày thanh toán")]
        public DateTime? PaymentDate { get; set; }

        [Display(Name = "Mã biên lai / Số hóa đơn")]
        public string ReceiptNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hình thức thanh toán")]
        [Display(Name = "Hình thức thanh toán")]
        public string PaymentMethod { get; set; }

        [Display(Name = "Trạng thái thanh toán")]
        public string PaymentStatus { get; set; }
    }
}