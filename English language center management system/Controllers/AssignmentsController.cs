using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Assigments;
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
    public class AssignmentsController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        #region Helper Authorization Method
        protected bool IsAuthorizedRole(params string[] roles)
        {
            var userRole = Session["Role"] as string ?? Session["RoleName"] as string;
            if (string.IsNullOrEmpty(userRole)) return false;

            return roles.Any(r => r.Equals(userRole, StringComparison.OrdinalIgnoreCase));
        }
        #endregion

        #region 1. ADMIN - Quản lý Bài tập (CRUD)

        // GET: Assignments
        public ActionResult Index()
        {
            if (!IsAuthorizedRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            var assignments = db.Assignments
                .Include(a => a.Class)
                .Include(a => a.Class.Course)
                .OrderByDescending(a => a.DueDate);

            return View(assignments.ToList());
        }

        // GET: Assignments/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAuthorizedRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Assignment assignment = db.Assignments
                .Include(a => a.Class)
                .Include(a => a.Class.Course)
                .FirstOrDefault(a => a.Id == id);

            if (assignment == null) return HttpNotFound();

            return View(assignment);
        }

        // GET: Assignments/Create
        public ActionResult Create()
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.ClassId = new SelectList(db.Classes.OrderBy(c => c.ClassName), "Id", "ClassName");
            return View();
        }

        // POST: Assignments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,ClassId,Title,DueDate")] Assignment assignment)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                db.Assignments.Add(assignment);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Thêm bài tập mới thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.ClassId = new SelectList(db.Classes.OrderBy(c => c.ClassName), "Id", "ClassName", assignment.ClassId);
            return View(assignment);
        }

        // GET: Assignments/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Assignment assignment = db.Assignments.Find(id);
            if (assignment == null) return HttpNotFound();

            ViewBag.ClassId = new SelectList(db.Classes.OrderBy(c => c.ClassName), "Id", "ClassName", assignment.ClassId);
            return View(assignment);
        }

        // POST: Assignments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,ClassId,Title,DueDate")] Assignment assignment)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                db.Entry(assignment).State = EntityState.Modified;
                db.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật bài tập thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.ClassId = new SelectList(db.Classes.OrderBy(c => c.ClassName), "Id", "ClassName", assignment.ClassId);
            return View(assignment);
        }

        // GET: Assignments/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Assignment assignment = db.Assignments
                .Include(a => a.Class)
                .FirstOrDefault(a => a.Id == id);

            if (assignment == null) return HttpNotFound();

            return View(assignment);
        }

        // POST: Assignments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            Assignment assignment = db.Assignments.Find(id);
            if (assignment != null)
            {
                bool hasSubmissions = db.Submissions.Any(s => s.AssignmentId == id);
                if (hasSubmissions)
                {
                    TempData["ErrorMessage"] = "Không thể xóa bài tập đã có học viên nộp bài!";
                    return RedirectToAction("Index");
                }

                db.Assignments.Remove(assignment);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa bài tập thành công!";
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region 2. TEACHER - Quản lý Bài nộp & Chấm điểm cho Lớp phụ trách

        [HttpGet]
        public ActionResult TeacherSubmissions(int? classId, int? assignmentId)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

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

            var availableAssignments = new List<Assignment>();
            var submissionRows = new List<TeacherSubmissionRowViewModel>();

            if (selectedClass != null)
            {
                availableAssignments = db.Assignments
                    .Where(a => a.ClassId == selectedClass.Id)
                    .OrderByDescending(a => a.DueDate)
                    .ToList();

                if (assignmentId.HasValue && !availableAssignments.Any(a => a.Id == assignmentId.Value))
                {
                    return HttpNotFound();
                }

                var assignmentIds = availableAssignments.Select(a => a.Id).ToList();

                var submissionQuery = db.Submissions
                    .Include(s => s.Assignment)
                    .Include(s => s.Student)
                    .Where(s => assignmentIds.Contains(s.AssignmentId));

                if (assignmentId.HasValue)
                {
                    submissionQuery = submissionQuery.Where(s => s.AssignmentId == assignmentId.Value);
                }

                var submissions = submissionQuery.OrderByDescending(s => s.SubmittedAt).ToList();

                foreach (var submission in submissions)
                {
                    string safeFileUrl = null;
                    Uri parsedUri;

                    if (!string.IsNullOrWhiteSpace(submission.FileUrl) &&
                        Uri.TryCreate(submission.FileUrl, UriKind.Absolute, out parsedUri) &&
                        (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps) || submission.FileUrl.StartsWith("/Uploads/"))
                    {
                        safeFileUrl = submission.FileUrl;
                    }

                    submissionRows.Add(new TeacherSubmissionRowViewModel
                    {
                        SubmissionId = submission.Id,
                        AssignmentId = submission.AssignmentId,
                        AssignmentTitle = submission.Assignment != null ? submission.Assignment.Title : "Bài tập",
                        StudentName = submission.Student != null ? submission.Student.FullName : "Học viên",
                        StudentUsername = submission.Student != null ? submission.Student.Username : "",
                        FileUrl = safeFileUrl,
                        SubmittedAt = submission.SubmittedAt,
                        DueDate = submission.Assignment != null ? submission.Assignment.DueDate : DateTime.MinValue,
                        Grade = submission.Grade
                    });
                }
            }

            var model = new TeacherSubmissionsViewModel
            {
                AvailableClasses = availableClasses,
                AvailableAssignments = availableAssignments,
                SelectedClassId = selectedClassId,
                SelectedAssignmentId = assignmentId,
                SelectedClass = selectedClass,
                Submissions = submissionRows
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveSubmissionGrades(TeacherSubmissionsViewModel model)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            if (model == null || !model.SelectedClassId.HasValue)
            {
                return new HttpStatusCodeResult(400);
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            int selectedClassId = model.SelectedClassId.Value;

            var selectedClass = db.Classes.FirstOrDefault(c => c.Id == selectedClassId && c.TeacherId == userId);
            if (selectedClass == null) return HttpNotFound();

            var assignmentIds = db.Assignments
                .Where(a => a.ClassId == selectedClassId)
                .Select(a => a.Id)
                .ToList();

            if (model.SelectedAssignmentId.HasValue && !assignmentIds.Contains(model.SelectedAssignmentId.Value))
            {
                return HttpNotFound();
            }

            var validSubmissionQuery = db.Submissions.Where(s => assignmentIds.Contains(s.AssignmentId));
            if (model.SelectedAssignmentId.HasValue)
            {
                validSubmissionQuery = validSubmissionQuery.Where(s => s.AssignmentId == model.SelectedAssignmentId.Value);
            }

            var validSubmissionIds = validSubmissionQuery.Select(s => s.Id).ToList();
            var rows = model.Submissions ?? new List<TeacherSubmissionRowViewModel>();

            if (rows.Any(r => r == null)) return new HttpStatusCodeResult(400);

            var postedIds = rows.Select(r => r.SubmissionId).ToList();

            if (postedIds.Distinct().Count() != postedIds.Count || postedIds.Any(id => !validSubmissionIds.Contains(id)))
            {
                return new HttpStatusCodeResult(400);
            }

            foreach (var row in rows)
            {
                if (row.Grade.HasValue && (row.Grade.Value < 0 || row.Grade.Value > 10))
                {
                    TempData["ErrorMessage"] = "Điểm bài tập phải nằm trong khoảng từ 0 đến 10.";
                    return RedirectToAction("TeacherSubmissions", new { classId = selectedClassId, assignmentId = model.SelectedAssignmentId });
                }
            }

            foreach (var row in rows)
            {
                var submission = db.Submissions.FirstOrDefault(s => s.Id == row.SubmissionId && validSubmissionIds.Contains(s.Id));
                if (submission == null) return HttpNotFound();

                submission.Grade = row.Grade;
            }

            db.SaveChanges();
            TempData["SuccessMessage"] = "Đã lưu điểm bài tập thành công!";

            return RedirectToAction("TeacherSubmissions", new { classId = selectedClassId, assignmentId = model.SelectedAssignmentId });
        }

        #endregion

        #region 3. STUDENT - Xem Bài tập, Tài liệu & Nộp bài làm

        [HttpGet]
        public ActionResult StudentLearningResources()
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Học viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Find(userId);

            var classIds = db.Enrollments
                .Where(e => e.StudentId == userId &&
                    (e.Status == "Confirmed" || e.Status == "Studying" || e.Status == "Completed" || e.Status == "Active" || e.Status == "Approved"))
                .Select(e => e.ClassId)
                .Distinct()
                .ToList();

            var assignments = db.Assignments
                .Include(a => a.Class)
                .Include(a => a.Class.Course)
                .Where(a => classIds.Contains(a.ClassId))
                .OrderBy(a => a.DueDate)
                .ToList();

            var materials = db.CourseMaterials
                .Include(m => m.Class)
                .Include(m => m.Class.Course)
                .Where(m => classIds.Contains(m.ClassId))
                .OrderByDescending(m => m.UploadedAt)
                .ToList();

            var assignmentIds = assignments.Select(a => a.Id).ToList();

            var submissions = db.Submissions
                .Where(s => s.StudentId == userId && assignmentIds.Contains(s.AssignmentId))
                .OrderByDescending(s => s.SubmittedAt)
                .ToList();

            var viewModel = new StudentLearningViewModel
            {
                StudentInfo = user,
                Assignments = assignments,
                Materials = materials,
                Submissions = submissions
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitLearningAssignment(int assignmentId, string fileUrl, HttpPostedFileBase submissionFile)
        {
            if (!IsAuthorizedRole("Student"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Học viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            var assignment = db.Assignments.FirstOrDefault(a => a.Id == assignmentId);
            if (assignment == null) return HttpNotFound();

            string finalFileUrl = null;

            // 1. Nếu học viên chọn file nộp từ máy tính -> Lưu vào ~/Uploads/Submissions/
            if (submissionFile != null && submissionFile.ContentLength > 0)
            {
                string extension = System.IO.Path.GetExtension(submissionFile.FileName);
                string fileName = "submission_" + Guid.NewGuid().ToString("N") + extension;
                string folderPath = Server.MapPath("~/Uploads/Submissions/");
                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }
                submissionFile.SaveAs(System.IO.Path.Combine(folderPath, fileName));
                finalFileUrl = "/Uploads/Submissions/" + fileName;
            }
            // 2. Nếu học viên nhập link
            else if (!string.IsNullOrWhiteSpace(fileUrl))
            {
                finalFileUrl = fileUrl.Trim();
            }

            if (string.IsNullOrWhiteSpace(finalFileUrl))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn tệp bài làm để tải lên!";
                return RedirectToAction("StudentLearningResources");
            }

            var existingSubmission = db.Submissions
                .Where(s => s.AssignmentId == assignmentId && s.StudentId == userId)
                .OrderByDescending(s => s.SubmittedAt)
                .FirstOrDefault();

            if (existingSubmission != null && existingSubmission.Grade.HasValue)
            {
                TempData["ErrorMessage"] = "Bài tập đã được chấm điểm, không thể thay đổi bài nộp.";
                return RedirectToAction("StudentLearningResources");
            }

            if (existingSubmission != null)
            {
                // Xóa file nộp cũ trên ổ cứng nếu có trước khi ghi đè file mới
                if (!string.IsNullOrWhiteSpace(existingSubmission.FileUrl) && existingSubmission.FileUrl.StartsWith("/Uploads/"))
                {
                    string oldPath = Server.MapPath("~" + existingSubmission.FileUrl);
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                existingSubmission.FileUrl = finalFileUrl;
                existingSubmission.SubmittedAt = DateTime.Now;
            }
            else
            {
                db.Submissions.Add(new Submission
                {
                    AssignmentId = assignmentId,
                    StudentId = userId,
                    FileUrl = finalFileUrl,
                    SubmittedAt = DateTime.Now
                });
            }

            db.SaveChanges();
            TempData["SuccessMessage"] = "Bài tập của bạn đã được nộp thành công!";
            return RedirectToAction("StudentLearningResources");
        }


        // POST: Assignments/DeleteStudentSubmission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteStudentSubmission(int submissionId)
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Học viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            // Tìm bài nộp thuộc về học viên hiện tại
            var submission = db.Submissions.FirstOrDefault(s => s.Id == submissionId && s.StudentId == userId);

            if (submission == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra: Chỉ cho phép xóa khi giáo viên chưa chấm điểm
            if (submission.Grade.HasValue)
            {
                TempData["ErrorMessage"] = "Bài tập đã được giáo viên chấm điểm, không thể xóa hoặc rút lại!";
                return RedirectToAction("StudentLearningResources");
            }

            // Xóa file vật lý trên server (nếu file được lưu trong ~/Uploads/Submissions/)
            if (!string.IsNullOrWhiteSpace(submission.FileUrl) && submission.FileUrl.StartsWith("/Uploads/"))
            {
                try
                {
                    string filePath = Server.MapPath("~" + submission.FileUrl);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                catch (Exception)
                {
                    // Bỏ qua lỗi IO để không cản trở việc xóa bản ghi trong CSDL
                }
            }

            db.Submissions.Remove(submission);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã xóa (rút lại) bài nộp thành công!";
            return RedirectToAction("StudentLearningResources");
        }
        #endregion

        private string SaveStudentSubmissionFile(HttpPostedFileBase submissionFile)
        {
            if (submissionFile != null && submissionFile.ContentLength > 0)
            {
                string extension = Path.GetExtension(submissionFile.FileName);
                string fileName = "submission_" + Guid.NewGuid().ToString("N") + extension;
                string folderPath = Server.MapPath("~/Uploads/Submissions/");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string filePath = Path.Combine(folderPath, fileName);
                submissionFile.SaveAs(filePath);

                return "/Uploads/Submissions/" + fileName;
            }
            return null;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}