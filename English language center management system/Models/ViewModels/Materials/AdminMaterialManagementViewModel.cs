using System;
using System.Collections.Generic;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Materials
{
    // Đại diện cho 1 dòng dữ liệu (Gộp chung cả CourseMaterial và Submission)
    public class AdminDocumentItemViewModel
    {
        public int Id { get; set; }

        // Phân loại: "Material" (Giáo trình/BTVN của GV) hoặc "Submission" (Bài nộp của HV)
        public string DocumentType { get; set; }
        public string DocumentTypeLabel { get; set; }

        public string Title { get; set; }           // Tên tài liệu hoặc Tên bài tập
        public string FileUrl { get; set; }         // Đường dẫn file hoặc Link

        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public string CourseName { get; set; }

        public int? UploaderId { get; set; }
        public string UploaderName { get; set; }
        public string UploaderUsername { get; set; }
        public string UploaderRole { get; set; }    // "Teacher" hoặc "Student"

        public DateTime UploadedAt { get; set; }
        public decimal? Grade { get; set; }         // Điểm số (nếu là bài nộp của Học viên)
    }

    // ViewModel chính truyền ra View Index của Admin
    public class AdminMaterialManagementViewModel
    {
        public List<AdminDocumentItemViewModel> Documents { get; set; } = new List<AdminDocumentItemViewModel>();

        // Các biến giữ trạng thái Bộ lọc (Filter)
        public int? SelectedClassId { get; set; }
        public string SelectedUploaderRole { get; set; } // "" (Tất cả), "Teacher" (Giảng viên), "Student" (Học viên)
        public string SearchTerm { get; set; }           // Lọc theo tên người nộp hoặc tên tài liệu

        // Danh sách Lớp học để đổ vào Dropdown lọc
        public List<Class> Classes { get; set; } = new List<Class>();

        // Thống kê nhanh trên đầu trang
        public int TotalCount { get; set; }
        public int TeacherMaterialsCount { get; set; }
        public int StudentSubmissionsCount { get; set; }
    }
}