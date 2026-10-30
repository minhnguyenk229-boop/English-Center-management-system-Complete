using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models.ViewModels.Courses
{
    public class CourseEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khóa học")]
        [StringLength(200, ErrorMessage = "Tên khóa học không vượt quá 200 ký tự")]
        [Display(Name = "Tên khóa học")]
        public string CourseName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập hoặc chọn trình độ")]
        [Display(Name = "Trình độ / Cấp độ (Level)")]
        public string Level { get; set; }

        [Display(Name = "Mô tả khóa học")]
        public string Description { get; set; }

        [Display(Name = "Thời lượng học")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập học phí")]
        [Range(0, 1000000000, ErrorMessage = "Học phí phải là số dương hợp lệ")]
        [Display(Name = "Học phí (VNĐ)")]
        public decimal TuitionFee { get; set; }

        [Display(Name = "Khung chương trình (Syllabus)")]
        public string Syllabus { get; set; }

        [Display(Name = "Trạng thái ẩn khóa học")]
        public bool IsHidden { get; set; }
    }
}