using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Materials;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace English_language_center_management_system.Controllers
{
    public class CourseMaterialsController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        #region 1. ADMIN - Quản lý tổng hợp Tài liệu & Bài nộp (CourseMaterials & Submissions)

        // GET: CourseMaterials/Index
        [HttpGet]
        public ActionResult Index(int? classId, string uploaderRole, string searchTerm)
        {
            if (!IsAuthorizedRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập quản lý tài liệu!";
                return RedirectToAction("Login", "Auth");
            }

            var unifiedList = new List<AdminDocumentItemViewModel>();

            // 1. Lấy danh sách Giáo trình / Tài liệu BTVN từ Giảng viên (CourseMaterials)
            if (string.IsNullOrEmpty(uploaderRole) || uploaderRole == "Teacher")
            {
                var materialsQuery = db.CourseMaterials
                    .Include(m => m.Class)
                    .Include(m => m.Class.Course)
                    .Include(m => m.Class.Teacher)
                    .AsQueryable();

                if (classId.HasValue && classId.Value > 0)
                {
                    materialsQuery = materialsQuery.Where(m => m.ClassId == classId.Value);
                }

                var materials = materialsQuery.ToList();
                foreach (var m in materials)
                {
                    bool isHomeworkFile = m.FileUrl != null &&
                        m.FileUrl.IndexOf("/Uploads/Assignments/", StringComparison.OrdinalIgnoreCase) >= 0;

                    unifiedList.Add(new AdminDocumentItemViewModel
                    {
                        Id = m.Id,
                        DocumentType = "Material",
                        DocumentTypeLabel = isHomeworkFile ? "BTVN (Giảng viên giao)" : "Giáo trình (Giảng viên)",
                        Title = m.Title,
                        FileUrl = m.FileUrl,
                        ClassId = m.ClassId,
                        ClassName = m.Class != null ? m.Class.ClassName : "N/A",
                        CourseName = (m.Class != null && m.Class.Course != null) ? m.Class.Course.CourseName : "",
                        UploaderId = m.Class != null ? m.Class.TeacherId : null,
                        UploaderName = (m.Class != null && m.Class.Teacher != null) ? m.Class.Teacher.FullName : "Giảng viên (Chưa phân công)",
                        UploaderUsername = (m.Class != null && m.Class.Teacher != null) ? m.Class.Teacher.Username : "",
                        UploaderRole = "Teacher",
                        UploadedAt = m.UploadedAt,
                        Grade = null
                    });
                }
            }

            // 2. Lấy danh sách Bài tập đã nộp từ Học viên (Submissions)
            if (string.IsNullOrEmpty(uploaderRole) || uploaderRole == "Student")
            {
                var submissionsQuery = db.Submissions
                    .Include(s => s.Assignment)
                    .Include(s => s.Assignment.Class)
                    .Include(s => s.Assignment.Class.Course)
                    .Include(s => s.Student)
                    .AsQueryable();

                if (classId.HasValue && classId.Value > 0)
                {
                    submissionsQuery = submissionsQuery.Where(s => s.Assignment.ClassId == classId.Value);
                }

                var submissions = submissionsQuery.ToList();
                foreach (var s in submissions)
                {
                    unifiedList.Add(new AdminDocumentItemViewModel
                    {
                        Id = s.Id,
                        DocumentType = "Submission",
                        DocumentTypeLabel = "Bài tập học viên nộp",
                        Title = s.Assignment != null ? "Nộp bài: " + s.Assignment.Title : "Bài nộp học viên",
                        FileUrl = s.FileUrl,
                        ClassId = s.Assignment != null ? s.Assignment.ClassId : 0,
                        ClassName = (s.Assignment != null && s.Assignment.Class != null) ? s.Assignment.Class.ClassName : "N/A",
                        CourseName = (s.Assignment != null && s.Assignment.Class != null && s.Assignment.Class.Course != null) ? s.Assignment.Class.Course.CourseName : "",
                        UploaderId = s.StudentId,
                        UploaderName = s.Student != null ? s.Student.FullName : "Học viên",
                        UploaderUsername = s.Student != null ? s.Student.Username : "",
                        UploaderRole = "Student",
                        UploadedAt = s.SubmittedAt,
                        Grade = s.Grade
                    });
                }
            }

            // 3. Lọc theo từ khóa (Tên tài liệu, tên người đăng, username, tên lớp)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string keyword = searchTerm.Trim().ToLower();
                unifiedList = unifiedList.Where(d =>
                    (d.Title != null && d.Title.ToLower().Contains(keyword)) ||
                    (d.UploaderName != null && d.UploaderName.ToLower().Contains(keyword)) ||
                    (d.UploaderUsername != null && d.UploaderUsername.ToLower().Contains(keyword)) ||
                    (d.ClassName != null && d.ClassName.ToLower().Contains(keyword))
                ).ToList();
            }

            // 4. Sắp xếp mới nhất lên đầu
            unifiedList = unifiedList.OrderByDescending(d => d.UploadedAt).ToList();

            var viewModel = new AdminMaterialManagementViewModel
            {
                Documents = unifiedList,
                SelectedClassId = classId,
                SelectedUploaderRole = uploaderRole,
                SearchTerm = searchTerm,
                Classes = db.Classes.Include(c => c.Course).OrderBy(c => c.ClassName).ToList(),
                TotalCount = unifiedList.Count,
                TeacherMaterialsCount = unifiedList.Count(d => d.UploaderRole == "Teacher"),
                StudentSubmissionsCount = unifiedList.Count(d => d.UploaderRole == "Student")
            };

            return View(viewModel);
        }

        // POST: CourseMaterials/DeleteDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteDocument(int id, string documentType, int? returnClassId, string returnRole, string returnSearch)
        {
            if (!IsAuthorizedRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện thao tác xóa!";
                return RedirectToAction("Login", "Auth");
            }

            if (documentType == "Material")
            {
                var material = db.CourseMaterials.Find(id);
                if (material != null)
                {
                    // Xóa file vật lý trên server trước khi xóa CSDL
                    DeletePhysicalFileIfExists(material.FileUrl);

                    db.CourseMaterials.Remove(material);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = $"Đã xóa tài liệu '{material.Title}' thành công!";
                }
            }
            else if (documentType == "Submission")
            {
                var submission = db.Submissions.Find(id);
                if (submission != null)
                {
                    // Xóa file vật lý trên server trước khi xóa CSDL
                    DeletePhysicalFileIfExists(submission.FileUrl);

                    db.Submissions.Remove(submission);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Đã xóa bài nộp của học viên thành công!";
                }
            }

            return RedirectToAction("Index", new
            {
                classId = returnClassId,
                uploaderRole = returnRole,
                searchTerm = returnSearch
            });
        }

        #endregion

        #region 2. TEACHER - Quản lý Giáo trình & Bài tập về nhà theo Lớp phụ trách

        // GET: CourseMaterials/TeacherClassResources
        [HttpGet]
        public ActionResult TeacherClassResources(int? classId)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            // Chỉ lấy lớp do giáo viên hiện tại phụ trách
            var availableClasses = db.Classes
                .Include(c => c.Course)
                .Where(c => c.TeacherId == userId)
                .OrderBy(c => c.ClassName)
                .ToList();

            if (classId.HasValue && !availableClasses.Any(c => c.Id == classId.Value))
            {
                return HttpNotFound();
            }

            int? selectedClassId = classId;
            if (!selectedClassId.HasValue && availableClasses.Any())
            {
                selectedClassId = availableClasses[0].Id;
            }

            var selectedClass = selectedClassId.HasValue
                ? availableClasses.FirstOrDefault(c => c.Id == selectedClassId.Value)
                : null;

            var assignments = new List<Assignment>();
            var materials = new List<CourseMaterial>();

            if (selectedClass != null)
            {
                assignments = db.Assignments
                    .Where(a => a.ClassId == selectedClass.Id)
                    .OrderByDescending(a => a.DueDate)
                    .ToList();

                materials = db.CourseMaterials
                    .Where(m => m.ClassId == selectedClass.Id)
                    .OrderByDescending(m => m.UploadedAt)
                    .ToList();
            }

            var model = new TeacherClassResourcesViewModel
            {
                AvailableClasses = availableClasses,
                SelectedClassId = selectedClassId,
                SelectedClass = selectedClass,
                Assignments = assignments,
                Materials = materials
            };

            return View(model);
        }

        // POST: Giáo viên Đăng mới Giáo trình hoặc File BTVN (Lưu vào ~/Uploads/Materials hoặc ~/Uploads/Assignments)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTeacherMaterial(TeacherClassResourcesViewModel model, HttpPostedFileBase materialFile, string materialCategory = "Material")
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            if (model == null || !model.SelectedClassId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            int classId = model.SelectedClassId.Value;

            var selectedClass = db.Classes.FirstOrDefault(c => c.Id == classId && c.TeacherId == userId);
            if (selectedClass == null)
            {
                return HttpNotFound();
            }

            string title = (model.NewMaterialTitle ?? "").Trim();
            if (string.IsNullOrWhiteSpace(title) || title.Length > 255)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập tên tài liệu hợp lệ (tối đa 255 ký tự).";
                return RedirectToAction("TeacherClassResources", new { classId = classId });
            }

            string savedFileUrl = null;

            // Ưu tiên 1: Upload file trực tiếp từ máy tính vào ~/Uploads/Materials hoặc ~/Uploads/Assignments
            if (materialFile != null && materialFile.ContentLength > 0)
            {
                string folder = (materialCategory == "Assignment") ? "Assignments" : "Materials";
                string prefix = (materialCategory == "Assignment") ? "homework" : "material";
                savedFileUrl = SaveUploadedDocument(materialFile, folder, prefix);
            }
            // Ưu tiên 2: Nếu không chọn file thì lấy từ ô nhập đường dẫn (link ngoài)
            else if (!string.IsNullOrWhiteSpace(model.NewMaterialFileUrl))
            {
                savedFileUrl = model.NewMaterialFileUrl.Trim();
            }

            if (string.IsNullOrWhiteSpace(savedFileUrl))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn tệp tải lên hoặc nhập đường dẫn tài liệu!";
                return RedirectToAction("TeacherClassResources", new { classId = classId });
            }

            var material = new CourseMaterial
            {
                ClassId = classId,
                Title = title,
                FileUrl = savedFileUrl,
                UploadedAt = DateTime.Now
            };

            db.CourseMaterials.Add(material);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã đăng tài liệu lên lớp học thành công!";
            return RedirectToAction("TeacherClassResources", new { classId = classId });
        }

        // POST: Giáo viên Sửa Giáo trình / File BTVN (Đổi tên hoặc thay thế bằng file mới)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTeacherMaterial(int id, int classId, string title, string fileUrl, HttpPostedFileBase materialFile, string materialCategory = "Material")
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            // Tìm tài liệu và xác minh tài liệu thuộc lớp do chính giáo viên này phụ trách
            var material = db.CourseMaterials
                .Include(m => m.Class)
                .FirstOrDefault(m => m.Id == id && m.ClassId == classId && m.Class.TeacherId == userId);

            if (material == null)
            {
                return HttpNotFound();
            }

            title = (title ?? "").Trim();
            if (string.IsNullOrWhiteSpace(title) || title.Length > 255)
            {
                TempData["ErrorMessage"] = "Tên tài liệu không được để trống và tối đa 255 ký tự.";
                return RedirectToAction("TeacherClassResources", new { classId = classId });
            }

            material.Title = title;

            // Trường hợp 1: Giáo viên chọn tải lên 1 file mới thay thế file cũ
            if (materialFile != null && materialFile.ContentLength > 0)
            {
                // Xóa file vật lý cũ trên ổ cứng trước khi lưu file mới
                DeletePhysicalFileIfExists(material.FileUrl);

                string folder = (materialCategory == "Assignment") ? "Assignments" : "Materials";
                string prefix = (materialCategory == "Assignment") ? "homework" : "material";
                material.FileUrl = SaveUploadedDocument(materialFile, folder, prefix);
                material.UploadedAt = DateTime.Now;
            }
            // Trường hợp 2: Giáo viên không tải file mới nhưng sửa bằng đường dẫn link khác
            else if (!string.IsNullOrWhiteSpace(fileUrl) && !string.Equals(fileUrl.Trim(), material.FileUrl, StringComparison.OrdinalIgnoreCase))
            {
                DeletePhysicalFileIfExists(material.FileUrl);
                material.FileUrl = fileUrl.Trim();
                material.UploadedAt = DateTime.Now;
            }

            db.Entry(material).State = EntityState.Modified;
            db.SaveChanges();

            TempData["SuccessMessage"] = $"Đã cập nhật tài liệu '{material.Title}' thành công!";
            return RedirectToAction("TeacherClassResources", new { classId = classId });
        }

        // POST: Giáo viên Xóa Giáo trình / File BTVN (Xóa cả file vật lý trong ~/Uploads)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteTeacherMaterial(int id, int classId)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var material = db.CourseMaterials
                .Include(m => m.Class)
                .FirstOrDefault(m => m.Id == id && m.ClassId == classId && m.Class.TeacherId == userId);

            if (material == null)
            {
                return HttpNotFound();
            }

            // Xóa file vật lý trên server trước khi xóa CSDL
            DeletePhysicalFileIfExists(material.FileUrl);

            string deletedTitle = material.Title;
            db.CourseMaterials.Remove(material);
            db.SaveChanges();

            TempData["SuccessMessage"] = $"Đã xóa tài liệu '{deletedTitle}' thành công!";
            return RedirectToAction("TeacherClassResources", new { classId = classId });
        }

        // POST: Giáo viên Tạo mới Đợt giao Bài tập về nhà (Assignment)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTeacherAssignment(TeacherClassResourcesViewModel model)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            if (model == null || !model.SelectedClassId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            int classId = model.SelectedClassId.Value;

            // Xác minh lớp thuộc giáo viên đang đăng nhập
            var selectedClass = db.Classes.FirstOrDefault(c => c.Id == classId && c.TeacherId == userId);
            if (selectedClass == null)
            {
                return HttpNotFound();
            }

            string title = (model.NewAssignmentTitle ?? "").Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập tiêu đề bài tập.";
            }
            else if (title.Length > 255)
            {
                TempData["ErrorMessage"] = "Tiêu đề bài tập không được vượt quá 255 ký tự.";
            }
            else if (!model.NewAssignmentDueDate.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn hạn nộp bài tập.";
            }
            else
            {
                var assignment = new Assignment
                {
                    ClassId = classId,
                    Title = title,
                    DueDate = model.NewAssignmentDueDate.Value
                };
                db.Assignments.Add(assignment);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Đã tạo bài tập về nhà thành công!";
                return RedirectToAction("TeacherClassResources", new { classId = classId });
            }

            return RedirectToAction("TeacherClassResources", new { classId = classId });
        }

        // POST: Giáo viên Sửa Đợt giao Bài tập về nhà (Tiêu đề & Hạn nộp)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTeacherAssignment(int id, int classId, string title, DateTime? dueDate)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var assignment = db.Assignments
                .Include(a => a.Class)
                .FirstOrDefault(a => a.Id == id && a.ClassId == classId && a.Class.TeacherId == userId);

            if (assignment == null)
            {
                return HttpNotFound();
            }

            title = (title ?? "").Trim();
            if (string.IsNullOrWhiteSpace(title) || title.Length > 255)
            {
                TempData["ErrorMessage"] = "Tiêu đề bài tập không hợp lệ (tối đa 255 ký tự).";
                return RedirectToAction("TeacherClassResources", new { classId = classId });
            }

            if (!dueDate.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn hạn nộp bài tập.";
                return RedirectToAction("TeacherClassResources", new { classId = classId });
            }

            assignment.Title = title;
            assignment.DueDate = dueDate.Value;

            db.Entry(assignment).State = EntityState.Modified;
            db.SaveChanges();

            TempData["SuccessMessage"] = $"Đã cập nhật bài tập '{assignment.Title}' thành công!";
            return RedirectToAction("TeacherClassResources", new { classId = classId });
        }

        // POST: Giáo viên Xóa Đợt giao Bài tập về nhà (Chỉ cho xóa khi chưa có học viên nộp bài)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteTeacherAssignment(int id, int classId)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var assignment = db.Assignments
                .Include(a => a.Class)
                .FirstOrDefault(a => a.Id == id && a.ClassId == classId && a.Class.TeacherId == userId);

            if (assignment == null)
            {
                return HttpNotFound();
            }

            bool hasSubmissions = db.Submissions.Any(s => s.AssignmentId == id);
            if (hasSubmissions)
            {
                TempData["ErrorMessage"] = "Không thể xóa bài tập đã có học viên nộp bài làm!";
                return RedirectToAction("TeacherClassResources", new { classId = classId });
            }

            string deletedTitle = assignment.Title;
            db.Assignments.Remove(assignment);
            db.SaveChanges();

            TempData["SuccessMessage"] = $"Đã xóa bài tập '{deletedTitle}' thành công!";
            return RedirectToAction("TeacherClassResources", new { classId = classId });
        }

        #endregion

        #region 3. HELPER METHODS

        protected bool IsAuthorizedRole(params string[] roles)
        {
            var userRole = Session["Role"] as string ?? Session["RoleName"] as string;
            if (string.IsNullOrEmpty(userRole)) return false;
            return roles.Any(r => r.Equals(userRole, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Lưu file upload vào đúng 1 trong 3 thư mục: Materials, Assignments, hoặc Submissions
        /// </summary>
        private string SaveUploadedDocument(HttpPostedFileBase uploadedFile, string subFolder, string prefix)
        {
            if (uploadedFile != null && uploadedFile.ContentLength > 0)
            {
                string extension = Path.GetExtension(uploadedFile.FileName);
                string fileName = prefix + "_" + Guid.NewGuid().ToString("N") + extension;
                string virtualFolder = "~/Uploads/" + subFolder + "/";
                string folderPath = Server.MapPath(virtualFolder);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string filePath = Path.Combine(folderPath, fileName);
                uploadedFile.SaveAs(filePath);

                return "/Uploads/" + subFolder + "/" + fileName;
            }
            return null;
        }

        /// <summary>
        /// Xóa file vật lý an toàn trên ổ cứng Server (Xử lý được cả đường dẫn nội bộ lẫn link HTTP/HTTPS)
        /// </summary>
        private void DeletePhysicalFileIfExists(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return;

            try
            {
                // Nếu là link online (Google Drive, OneDrive...) thì bỏ qua xóa ổ cứng, chỉ xóa DB
                Uri parsedUri;
                if (Uri.TryCreate(fileUrl, UriKind.Absolute, out parsedUri) &&
                    (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps))
                {
                    return;
                }

                // Nếu là đường dẫn tương đối trên Server (VD: "/Uploads/Materials/abc.pdf" hoặc "~/Uploads/...")
                string virtualPath = fileUrl.StartsWith("~") ? fileUrl : "~/" + fileUrl.TrimStart('/');
                string physicalPath = Server.MapPath(virtualPath);

                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }
            catch (Exception)
            {
                // Bỏ qua lỗi IO để không làm gián đoạn việc xóa bản ghi trong Database
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}