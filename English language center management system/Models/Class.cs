using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace English_language_center_management_system.Models
{
    [Table("classes")]
    public class Class
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khóa học")]
        [Column("course_id")]
        public int CourseId { get; set; }

        [Column("teacher_id")]
        public int? TeacherId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên lớp học")]
        [StringLength(100)]
        [Column("class_name")]
        [Index(IsUnique = true)]
        [Display(Name = "Tên lớp học")]
        public string ClassName { get; set; }

        [Column("room_id")]
        [Display(Name = "Phòng học")]
        public int? RoomId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập sĩ số tối đa")]
        [Column("max_capacity")]
        [Display(Name = "Sĩ số tối đa")]
        public int MaxCapacity { get; set; } = 30;

        // --- CÁC THUỘC TÍNH CHI TIẾT LỊCH HỌC & THỜI KHOÁ BIỂU ---
        [StringLength(50)]
        [Column("schedule_days")]
        [Display(Name = "Thứ học trong tuần")]
        public string ScheduleDays { get; set; } // Ví dụ: "2,4,6" hoặc "Thứ 2, Thứ 4, Thứ 6"

        [Column("daily_start_time")]
        [Display(Name = "Giờ bắt đầu")]
        [Range(typeof(TimeSpan), "08:00", "21:00", ErrorMessage = "Giờ học phải nằm trong khoảng từ 08:00 sáng đến 21:00 tối.")]
        public TimeSpan? DailyStartTime { get; set; }

        [Column("daily_end_time")]
        [Display(Name = "Giờ kết thúc")]
        [Range(typeof(TimeSpan), "08:00", "21:00", ErrorMessage = "Giờ học phải nằm trong khoảng từ 08:00 sáng đến 21:00 tối.")]
        public TimeSpan? DailyEndTime { get; set; }

        [Column("weekly_sessions_count")]
        [Display(Name = "Số lượng buổi/tuần")]
        public int WeeklySessionsCount { get; set; } = 3;

        [StringLength(255)]
        [Column("schedule_description")]
        [Display(Name = "Mô tả lịch học")]
        public string ScheduleDescription { get; set; }

        // --- CÁC THUỘC TÍNH THỜI GIAN LỚP HỌC ---
        [Column("start_date")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày bắt đầu học")]
        public DateTime? StartDate { get; set; }

        [Column("end_date")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày kết thúc học")]
        public DateTime? EndDate { get; set; }

        // --- CÁC THUỘC TÍNH THỜI GIAN ĐĂNG KÝ ---
        [Column("registration_start_date")]
        [DataType(DataType.Date)]
        [Display(Name = "Bắt đầu đăng ký")]
        public DateTime? RegistrationStartDate { get; set; }

        [Column("registration_end_date")]
        [DataType(DataType.Date)]
        [Display(Name = "Kết thúc đăng ký")]
        public DateTime? RegistrationEndDate { get; set; }

        [Required]
        [StringLength(50)]
        [Column("status")]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Mở đăng ký"; // Mở đăng ký, Đã đóng, Đang học, Đã đủ sĩ số, Đã hoàn thành

        // --- NAVIGATION PROPERTIES ---
        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; }

        [ForeignKey("TeacherId")]
        public virtual User Teacher { get; set; }

        [ForeignKey("RoomId")]
        public virtual Room Room { get; set; }

        public virtual ICollection<ClassSession> ClassSessions { get; set; }
        public virtual ICollection<Enrollment> Enrollments { get; set; }
        public virtual ICollection<CourseMaterial> CourseMaterials { get; set; }
        public virtual ICollection<Assignment> Assignments { get; set; }
    }
}