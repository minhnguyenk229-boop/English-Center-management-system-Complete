using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace English_language_center_management_system.Models
{
    [Table("courses")]
    public class Course
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khóa học")]
        [StringLength(150)]
        [Column("course_name")]
        [Display(Name = "Tên khóa học")]
        public string CourseName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trình độ")]
        [StringLength(50)]
        [Column("level")]
        [Display(Name = "Trình độ")]
        public string Level { get; set; } // Beginner, Elementary, Intermediate, Upper Intermediate, Advanced, IELTS/TOEIC preparation

        [Column("description")]
        [Display(Name = "Mô tả khóa học")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tổng thời lượng học")]
        [Column("duration")]
        [Display(Name = "Thời lượng học (giờ)")]
        public int Duration { get; set; } // Tổng số giờ học

        // Các thuộc tính hỗ trợ tính Thời lượng học
        [Column("sessions_per_week")]
        [Display(Name = "Số buổi/tuần")]
        public int SessionsPerWeek { get; set; } = 3;

        [Column("hours_per_session")]
        [Display(Name = "Thời gian học/ngày (giờ)")]
        public double HoursPerSession { get; set; } = 1.5;

        [Column("total_weeks")]
        [Display(Name = "Số tuần học")]
        public int TotalWeeks { get; set; } = 8;

        [Required(ErrorMessage = "Vui lòng nhập học phí")]
        [Column("tuition_fee")]
        [Display(Name = "Học phí (VNĐ)")]
        [Range(0, 1000000000, ErrorMessage = "Học phí phải là số dương và không vượt quá 1 tỷ VNĐ")]
        public decimal TuitionFee { get; set; }

        [Column("syllabus")]
        [Display(Name = "Giáo trình / Khung chương trình")]
        public string Syllabus { get; set; }

        [StringLength(500)]
        [Column("image_url")]
        [Display(Name = "Hình ảnh khóa học")]
        public string ImageUrl { get; set; }

        [Column("is_hidden")]
        [Display(Name = "Ẩn khóa học")]
        public bool IsHidden { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Class> Classes { get; set; }
    }
}