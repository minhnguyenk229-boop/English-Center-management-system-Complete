using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Attendances;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace English_language_center_management_system.Controllers
{
    public class AttendancesController : Controller
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

        #region 1. ADMIN - Quản lý Điểm danh (CRUD)

        // GET: Attendances
        public ActionResult Index()
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            var attendances = db.Attendances
                .Include(a => a.ClassSession)
                .Include(a => a.ClassSession.Class)
                .Include(a => a.Student)
                .OrderByDescending(a => a.ClassSession.SessionDate);

            return View(attendances.ToList());
        }

        // GET: Attendances/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Attendance attendance = db.Attendances
                .Include(a => a.ClassSession)
                .Include(a => a.ClassSession.Class)
                .Include(a => a.Student)
                .FirstOrDefault(a => a.Id == id);

            if (attendance == null) return HttpNotFound();

            return View(attendance);
        }

        // GET: Attendances/Create
        public ActionResult Create()
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.SessionId = new SelectList(db.ClassSessions, "Id", "Room");
            ViewBag.StudentId = new SelectList(db.Users, "Id", "Username");
            return View();
        }

        // POST: Attendances/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,SessionId,StudentId,Status,Notes")] Attendance attendance)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                db.Attendances.Add(attendance);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Thêm bản ghi điểm danh thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.SessionId = new SelectList(db.ClassSessions, "Id", "Room", attendance.SessionId);
            ViewBag.StudentId = new SelectList(db.Users, "Id", "Username", attendance.StudentId);
            return View(attendance);
        }

        // GET: Attendances/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Attendance attendance = db.Attendances.Find(id);
            if (attendance == null) return HttpNotFound();

            ViewBag.SessionId = new SelectList(db.ClassSessions, "Id", "Room", attendance.SessionId);
            ViewBag.StudentId = new SelectList(db.Users, "Id", "Username", attendance.StudentId);
            return View(attendance);
        }

        // POST: Attendances/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,SessionId,StudentId,Status,Notes")] Attendance attendance)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                db.Entry(attendance).State = EntityState.Modified;
                db.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật điểm danh thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.SessionId = new SelectList(db.ClassSessions, "Id", "Room", attendance.SessionId);
            ViewBag.StudentId = new SelectList(db.Users, "Id", "Username", attendance.StudentId);
            return View(attendance);
        }

        // GET: Attendances/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Attendance attendance = db.Attendances
                .Include(a => a.ClassSession)
                .Include(a => a.Student)
                .FirstOrDefault(a => a.Id == id);

            if (attendance == null) return HttpNotFound();

            return View(attendance);
        }

        // POST: Attendances/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            Attendance attendance = db.Attendances.Find(id);
            if (attendance != null)
            {
                db.Attendances.Remove(attendance);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa bản ghi điểm danh thành công!";
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region 2. TEACHER - Điểm danh Buổi học cho Lớp phụ trách

        [HttpGet]
        public ActionResult TeacherAttendance(int? sessionId)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            var teacher = db.Users.Find(userId);

            if (teacher == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Chỉ lấy các buổi học thuộc lớp của giáo viên hiện tại.
            var availableSessions = db.ClassSessions
                .Include(s => s.Class)
                .Include(s => s.Class.Course)
                .Where(s => s.Class.TeacherId == userId)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.StartTime)
                .ToList();

            ClassSession selectedSession;

            if (sessionId.HasValue)
            {
                selectedSession = availableSessions.FirstOrDefault(s => s.Id == sessionId.Value);

                if (selectedSession == null)
                {
                    return HttpNotFound();
                }
            }
            else
            {
                selectedSession = availableSessions.FirstOrDefault(s => s.SessionDate.Date >= DateTime.Today)
                    ?? availableSessions.LastOrDefault();
            }

            var viewModel = new TeacherAttendanceViewModel
            {
                TeacherInfo = teacher,
                AvailableSessions = availableSessions,
                SelectedSessionId = selectedSession != null ? (int?)selectedSession.Id : null,
                SelectedSession = selectedSession,
                Students = new List<TeacherAttendanceRowViewModel>()
            };

            if (selectedSession != null)
            {
                var eligibleStatuses = new[] { "Confirmed", "Studying", "Completed", "Active", "Approved" };

                var enrollments = db.Enrollments
                    .Include(e => e.Student)
                    .Where(e => e.ClassId == selectedSession.ClassId && eligibleStatuses.Contains(e.Status))
                    .OrderBy(e => e.Student.FullName)
                    .ToList();

                var savedAttendance = db.Attendances
                    .Where(a => a.SessionId == selectedSession.Id)
                    .ToList();

                viewModel.Students = enrollments.Select(e =>
                {
                    var attendance = savedAttendance.FirstOrDefault(a => a.StudentId == e.StudentId);

                    return new TeacherAttendanceRowViewModel
                    {
                        StudentId = e.StudentId,
                        FullName = e.Student != null ? e.Student.FullName : "Học viên",
                        Username = e.Student != null ? e.Student.Username : "",
                        Status = attendance != null ? attendance.Status : "",
                        Notes = attendance != null ? attendance.Notes : ""
                    };
                }).ToList();
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTeacherAttendance(TeacherAttendanceViewModel model)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            if (model == null || !model.SelectedSessionId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            int selectedSessionId = model.SelectedSessionId.Value;

            var session = db.ClassSessions
                .Include(s => s.Class)
                .FirstOrDefault(s => s.Id == selectedSessionId && s.Class.TeacherId == userId);

            if (session == null)
            {
                return HttpNotFound();
            }

            var eligibleStatuses = new[] { "Confirmed", "Studying", "Completed", "Active", "Approved" };

            var eligibleStudentIds = db.Enrollments
                .Where(e => e.ClassId == session.ClassId && eligibleStatuses.Contains(e.Status))
                .Select(e => e.StudentId)
                .Distinct()
                .ToList();

            var rows = model.Students ?? new List<TeacherAttendanceRowViewModel>();

            // Kiểm tra tính hợp lệ của danh sách học viên
            bool invalidRoster = rows.Count != eligibleStudentIds.Count ||
                rows.Select(r => r.StudentId).Distinct().Count() != rows.Count ||
                rows.Any(r => !eligibleStudentIds.Contains(r.StudentId)) ||
                eligibleStudentIds.Any(id => !rows.Any(r => r.StudentId == id));

            if (invalidRoster)
            {
                TempData["ErrorMessage"] = "Danh sách học viên đã thay đổi. Vui lòng tải lại trang.";
                return RedirectToAction("TeacherAttendance", new { sessionId = selectedSessionId });
            }

            var allowedAttendanceStatuses = new[] { "Present", "Absent", "Late", "Excused" };

            // Kiểm tra input hợp lệ cơ bản
            bool invalidInput = rows.Any(r =>
                string.IsNullOrWhiteSpace(r.Status) ||
                !allowedAttendanceStatuses.Contains(r.Status) ||
                (r.Notes != null && r.Notes.Length > 255));

            if (invalidInput)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn trạng thái hợp lệ cho từng học viên. Ghi chú không được vượt quá 255 ký tự.";
                return RedirectToAction("TeacherAttendance", new { sessionId = selectedSessionId });
            }

            // BỔ SUNG: Ràng buộc nghiệp vụ bắt buộc nhập lý do cho Trễ (Late) và Có phép (Excused)
            bool missingReason = rows.Any(r =>
                (r.Status == "Late" || r.Status == "Excused") &&
                string.IsNullOrWhiteSpace(r.Notes));

            if (missingReason)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do (Ghi chú) cho những học viên đi Trễ hoặc vắng Có phép.";
                return RedirectToAction("TeacherAttendance", new { sessionId = selectedSessionId });
            }

            // Lưu dữ liệu điểm danh
            foreach (var row in rows)
            {
                var attendance = db.Attendances.FirstOrDefault(a => a.SessionId == selectedSessionId && a.StudentId == row.StudentId);

                if (attendance == null)
                {
                    attendance = new Attendance
                    {
                        SessionId = selectedSessionId,
                        StudentId = row.StudentId
                    };
                    db.Attendances.Add(attendance);
                }

                attendance.Status = row.Status;
                attendance.Notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim();
            }

            db.SaveChanges();

            TempData["SuccessMessage"] = $"Đã lưu điểm danh cho {rows.Count} học viên thành công.";

            return RedirectToAction("TeacherAttendance", new { sessionId = selectedSessionId });
        }

        #endregion

        #region 3. STUDENT - Xem Tiến độ Học tập & Điểm danh

        [HttpGet]
        public ActionResult StudentProgress(int? enrollmentId)
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Học viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Find(userId);

            var enrollments = db.Enrollments
                .Include(e => e.Class)
                .Include(e => e.Class.Course)
                .Where(e => e.StudentId == userId &&
                    e.Status != "Pending" &&
                    e.Status != "Withdrawn" &&
                    e.Status != "Cancelled" &&
                    e.Status != "Rejected")
                .OrderByDescending(e => e.EnrollmentDate)
                .ToList();

            var selectedEnrollment = enrollmentId.HasValue
                ? enrollments.FirstOrDefault(e => e.Id == enrollmentId.Value)
                : enrollments.FirstOrDefault();

            var viewModel = new StudentProgressViewModel
            {
                StudentInfo = user,
                Enrollments = enrollments,
                SelectedEnrollmentId = selectedEnrollment?.Id,
                SelectedEnrollment = selectedEnrollment,
                AttendanceRecords = new List<Attendance>(),
                TeacherFeedbacks = new List<TeacherFeedback>()
            };

            if (selectedEnrollment == null)
            {
                return View(viewModel);
            }

            int selectedClassId = selectedEnrollment.ClassId;
            int selectedId = selectedEnrollment.Id;

            viewModel.Score = db.AssessmentScores.FirstOrDefault(s => s.EnrollmentId == selectedId);

            var attendanceRecords = db.Attendances
                .Include(a => a.ClassSession)
                .Where(a => a.StudentId == userId && a.ClassSession.ClassId == selectedClassId)
                .OrderByDescending(a => a.ClassSession.SessionDate)
                .ThenByDescending(a => a.ClassSession.StartTime)
                .ToList();

            viewModel.AttendanceRecords = attendanceRecords;
            viewModel.TotalAttendanceRecords = attendanceRecords.Count;
            viewModel.PresentCount = attendanceRecords.Count(a => a.Status == "Present");
            viewModel.LateCount = attendanceRecords.Count(a => a.Status == "Late");
            viewModel.AbsentCount = attendanceRecords.Count(a => a.Status == "Absent");
            viewModel.ExcusedCount = attendanceRecords.Count(a => a.Status == "Excused");

            viewModel.AttendanceRate = attendanceRecords.Count == 0
                ? 0m
                : (decimal)(viewModel.PresentCount + viewModel.LateCount) * 100m / attendanceRecords.Count;

            viewModel.TeacherFeedbacks = db.TeacherFeedbacks
                .Include(f => f.Teacher)
                .Where(f => f.EnrollmentId == selectedId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            viewModel.Certificate = db.Certificates.FirstOrDefault(c => c.EnrollmentId == selectedId);

            return View(viewModel);
        }

        #endregion

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