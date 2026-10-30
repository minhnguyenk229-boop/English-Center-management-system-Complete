using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace English_language_center_management_system.Models
{
    [Table("rooms")]
    public class Room
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên phòng không được để trống")]
        [StringLength(50)]
        [Column("room_name")]
        [Display(Name = "Tên phòng học")]
        public string RoomName { get; set; } // Ví dụ: Phòng A101, Lab 2, Hội trường B

        [Required(ErrorMessage = "Sức chứa không được để trống")]
        [Column("capacity")]
        [Display(Name = "Sức chứa tối đa (Học viên)")]
        public int Capacity { get; set; }

        [StringLength(255)]
        [Column("description")]
        [Display(Name = "Ghi chú / Trang thiết bị")]
        public string Description { get; set; } // Ví dụ: Có máy chiếu, 30 máy tính...

        [Column("is_active")]
        [Display(Name = "Đang sử dụng")]
        public bool IsActive { get; set; } = true;

        // Navigation Properties (Thuộc tính điều hướng)
        public virtual ICollection<Class> Classes { get; set; }
        public virtual ICollection<ClassSession> ClassSessions { get; set; }
    }
}