using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.AssessmentScores;
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
    
    public class AssessmentScoresController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();


        #region 1. ADMIN - Quản lý toàn bộ điểm số
        // GET: AssessmentScores
        public ActionResult Index()
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên")) return RedirectToAction("Login", "Auth");
            var assessmentScores = db.AssessmentScores.Include(a => a.Enrollment);
            return View(assessmentScores.ToList());
        }

        // GET: AssessmentScores/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên")) return RedirectToAction("Login", "Auth");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            AssessmentScore assessmentScore = db.AssessmentScores.Find(id);
            if (assessmentScore == null) return HttpNotFound();

            return View(assessmentScore);
        }

        // GET: AssessmentScores/Create
        public ActionResult Create()
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên")) return RedirectToAction("Login", "Auth");
            ViewBag.EnrollmentId = new SelectList(db.Enrollments, "Id", "Status");
            return View();
        }

        // POST: AssessmentScores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "EnrollmentId,MidtermScore,FinalScore,TotalScore,LearningOutcome")] AssessmentScore assessmentScore)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên")) return RedirectToAction("Login", "Auth");
            if (ModelState.IsValid)
            {
                db.AssessmentScores.Add(assessmentScore);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.EnrollmentId = new SelectList(db.Enrollments, "Id", "Status", assessmentScore.EnrollmentId);
            return View(assessmentScore);
        }

        // GET: AssessmentScores/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên")) return RedirectToAction("Login", "Auth");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            AssessmentScore assessmentScore = db.AssessmentScores.Find(id);
            if (assessmentScore == null) return HttpNotFound();

            ViewBag.EnrollmentId = new SelectList(db.Enrollments, "Id", "Status", assessmentScore.EnrollmentId);
            return View(assessmentScore);
        }

        // POST: AssessmentScores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "EnrollmentId,MidtermScore,FinalScore,TotalScore,LearningOutcome")] AssessmentScore assessmentScore)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên")) return RedirectToAction("Login", "Auth");
            if (ModelState.IsValid)
            {
                db.Entry(assessmentScore).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.EnrollmentId = new SelectList(db.Enrollments, "Id", "Status", assessmentScore.EnrollmentId);
            return View(assessmentScore);
        }

        // GET: AssessmentScores/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên")) return RedirectToAction("Login", "Auth");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            AssessmentScore assessmentScore = db.AssessmentScores.Find(id);
            if (assessmentScore == null) return HttpNotFound();

            return View(assessmentScore);
        }

        // POST: AssessmentScores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên")) 
                return RedirectToAction("Login", "Auth");
            
            AssessmentScore assessmentScore = db.AssessmentScores.Find(id);
            db.AssessmentScores.Remove(assessmentScore);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        #endregion

        #region 2. TEACHER - Cập nhật điểm và nhận xét cho lớp phụ trách
        // Chuyển hướng đường dẫn cũ sang trang nhập điểm chính thức
        [HttpGet]
        public ActionResult TeacherGrading(int? classId)
        {
            return RedirectToAction(
                "TeacherGrades",
                "AssessmentScores",
                new { classId = classId }
            );
        }
        [HttpGet]
        public ActionResult TeacherGrades(int? classId)
        {
            if (!IsAuthorizedRole("Teacher"))
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

            // NGHIỆP VỤ KHÓA ĐIỂM: Khóa sau 5 ngày kể từ ngày kết thúc lớp
            bool isLocked = false;
            if (selectedClass != null && selectedClass.EndDate.HasValue)
            {
                isLocked = DateTime.Now > selectedClass.EndDate.Value.AddDays(5);
            }
            ViewBag.IsLocked = isLocked;

            var studentRows = new List<TeacherGradeRowViewModel>();
            if (selectedClass != null)
            {
                var enrollments = db.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.AssessmentScore)
                    .Where(e => e.ClassId == selectedClass.Id &&
                        (e.Status == "Confirmed" || e.Status == "Studying" || e.Status == "Completed" || e.Status == "Active" || e.Status == "Approved"))
                    .OrderBy(e => e.Student.FullName)
                    .ToList();

                var enrollmentIds = enrollments.Select(e => e.Id).ToList();

                var feedbackList = db.TeacherFeedbacks
                    .Where(f => f.TeacherId == userId && enrollmentIds.Contains(f.EnrollmentId))
                    .OrderByDescending(f => f.CreatedAt)
                    .ToList();

                var feedbackByEnrollment = feedbackList
                    .GroupBy(f => f.EnrollmentId)
                    .ToDictionary(g => g.Key, g => g.First().Comments);

                // Chuẩn bị dữ liệu tính Chuyên cần
                int totalSessions = db.ClassSessions.Count(s => s.ClassId == selectedClass.Id);
                var classAttendances = db.Attendances.Where(a => a.ClassSession.ClassId == selectedClass.Id).ToList();

                foreach (var enrollment in enrollments)
                {
                    var score = enrollment.AssessmentScore;
                    string feedback = feedbackByEnrollment.ContainsKey(enrollment.Id) ? feedbackByEnrollment[enrollment.Id] : "";

                    // TÍNH ĐIỂM CHUYÊN CẦN TỰ ĐỘNG
                    var studentAttendances = classAttendances.Where(a => a.StudentId == enrollment.StudentId).ToList();
                    int attendedSessions = studentAttendances.Count(a => a.Status == "Present" || a.Status == "Late");
                    decimal attendanceScore = totalSessions > 0 ? Math.Round((decimal)attendedSessions / totalSessions * 10, 1) : 0m;

                    // TÍNH TỔNG ĐIỂM ĐỂ HIỂN THỊ (Nếu giáo viên đã nhập đủ giữa kỳ và cuối kỳ)
                    decimal? totalScore = score?.TotalScore;
                    if (score != null && score.MidtermScore.HasValue && score.FinalScore.HasValue)
                    {
                        totalScore = Math.Round((attendanceScore * 0.1m) + (score.MidtermScore.Value * 0.3m) + (score.FinalScore.Value * 0.6m), 1);
                    }

                    studentRows.Add(new TeacherGradeRowViewModel
                    {
                        EnrollmentId = enrollment.Id,
                        StudentId = enrollment.StudentId,
                        FullName = enrollment.Student != null ? enrollment.Student.FullName : "Chưa có tên",
                        Username = enrollment.Student != null ? enrollment.Student.Username : "",
                        MidtermScore = score != null ? score.MidtermScore : null,
                        FinalScore = score != null ? score.FinalScore : null,
                        TotalScore = totalScore,
                        LearningOutcome = attendanceScore.ToString("0.0"), // Lưu Điểm chuyên cần vào LearningOutcome
                        FeedbackComments = feedback
                    });
                }
            }

            var model = new TeacherGradesViewModel
            {
                AvailableClasses = availableClasses,
                SelectedClassId = selectedClassId,
                SelectedClass = selectedClass,
                Students = studentRows
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTeacherGrades(TeacherGradesViewModel model)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            if (model == null || !model.SelectedClassId.HasValue) return new HttpStatusCodeResult(400);

            int userId = Convert.ToInt32(Session["UserId"]);
            var selectedClass = db.Classes
                .Include(c => c.Course)
                .FirstOrDefault(c => c.Id == model.SelectedClassId.Value && c.TeacherId == userId);

            if (selectedClass == null) return HttpNotFound();

            // NGHIỆP VỤ KHÓA ĐIỂM: Chặn không cho lưu nếu quá 5 ngày
            if (selectedClass.EndDate.HasValue && DateTime.Now > selectedClass.EndDate.Value.AddDays(5))
            {
                TempData["ErrorMessage"] = "Đã quá thời hạn 5 ngày kể từ ngày kết thúc khóa học. Bạn không thể nhập hoặc sửa điểm nữa.";
                return RedirectToAction("TeacherGrades", new { classId = model.SelectedClassId });
            }

            var availableClasses = db.Classes
                .Include(c => c.Course)
                .Where(c => c.TeacherId == userId)
                .OrderBy(c => c.ClassName)
                .ToList();

            var rows = model.Students ?? new List<TeacherGradeRowViewModel>();
            var validEnrollmentIds = db.Enrollments
                .Where(e => e.ClassId == selectedClass.Id &&
                    (e.Status == "Confirmed" || e.Status == "Studying" || e.Status == "Completed" || e.Status == "Active" || e.Status == "Approved"))
                .Select(e => e.Id)
                .ToList();

            if (rows.Any(r => r == null)) return new HttpStatusCodeResult(400);

            var postedEnrollmentIds = rows.Select(r => r.EnrollmentId).ToList();
            if (postedEnrollmentIds.Count != validEnrollmentIds.Count ||
                postedEnrollmentIds.Distinct().Count() != postedEnrollmentIds.Count ||
                !postedEnrollmentIds.OrderBy(id => id).SequenceEqual(validEnrollmentIds.OrderBy(id => id)))
            {
                return new HttpStatusCodeResult(400);
            }

            // Validate Input từ Giáo viên
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.MidtermScore.HasValue && (row.MidtermScore.Value < 0 || row.MidtermScore.Value > 10))
                    ModelState.AddModelError($"Students[{i}].MidtermScore", "Điểm giữa kỳ phải từ 0 đến 10.");
                if (row.FinalScore.HasValue && (row.FinalScore.Value < 0 || row.FinalScore.Value > 10))
                    ModelState.AddModelError($"Students[{i}].FinalScore", "Điểm cuối kỳ phải từ 0 đến 10.");
                if (!string.IsNullOrEmpty(row.FeedbackComments) && row.FeedbackComments.Length > 4000)
                    ModelState.AddModelError($"Students[{i}].FeedbackComments", "Nhận xét không vượt quá 4000 ký tự.");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Điểm số không hợp lệ. Nếu máy tính không nhận dấu chấm (.), hãy thử nhập bằng dấu phẩy (,) hoặc ngược lại.";

                model.AvailableClasses = availableClasses;
                model.SelectedClass = selectedClass;

                // BỔ SUNG: Nạp lại thông tin Tên, Username và Điểm chuyên cần để không bị trắng màn hình
                int totalSessionsDisplay = db.ClassSessions.Count(s => s.ClassId == selectedClass.Id);
                var classAttendancesDisplay = db.Attendances.Where(a => a.ClassSession.ClassId == selectedClass.Id).ToList();

                foreach (var row in rows)
                {
                    // Lấy lại tên học viên
                    var student = db.Users.Find(row.StudentId);
                    if (student != null)
                    {
                        row.FullName = student.FullName;
                        row.Username = student.Username;
                    }

                    // Tính lại điểm chuyên cần
                    var studentAttendances = classAttendancesDisplay.Where(a => a.StudentId == row.StudentId).ToList();
                    int attendedSessions = studentAttendances.Count(a => a.Status == "Present" || a.Status == "Late");
                    decimal attendanceScore = totalSessionsDisplay > 0 ? Math.Round((decimal)attendedSessions / totalSessionsDisplay * 10, 1) : 0m;

                    row.LearningOutcome = attendanceScore.ToString("0.0");

                    // Tính lại tổng điểm hiển thị nếu có nhập điểm
                    if (row.MidtermScore.HasValue && row.FinalScore.HasValue)
                    {
                        row.TotalScore = Math.Round((attendanceScore * 0.1m) + (row.MidtermScore.Value * 0.3m) + (row.FinalScore.Value * 0.6m), 1);
                    }
                }

                return View("TeacherGrades", model);
            }

            // Chuẩn bị dữ liệu tính điểm chuyên cần
            int totalSessions = db.ClassSessions.Count(s => s.ClassId == selectedClass.Id);
            var classAttendances = db.Attendances.Where(a => a.ClassSession.ClassId == selectedClass.Id).ToList();

            foreach (var row in rows)
            {
                // 1. TÍNH ĐIỂM CHUYÊN CẦN VÀ TỔNG ĐIỂM
                var studentAttendances = classAttendances.Where(a => a.StudentId == row.StudentId).ToList();
                int attendedSessions = studentAttendances.Count(a => a.Status == "Present" || a.Status == "Late");
                decimal attendanceScore = totalSessions > 0 ? Math.Round((decimal)attendedSessions / totalSessions * 10, 1) : 0m;

                decimal? finalTotalScore = null;
                if (row.MidtermScore.HasValue && row.FinalScore.HasValue)
                {
                    finalTotalScore = Math.Round((attendanceScore * 0.1m) + (row.MidtermScore.Value * 0.3m) + (row.FinalScore.Value * 0.6m), 1);
                }

                // 2. CẬP NHẬT ĐIỂM VÀO DATABASE
                var score = db.AssessmentScores.Find(row.EnrollmentId);

                if (score == null)
                {
                    score = new AssessmentScore { EnrollmentId = row.EnrollmentId };
                    db.AssessmentScores.Add(score);
                }

                score.MidtermScore = row.MidtermScore;
                score.FinalScore = row.FinalScore;
                score.TotalScore = finalTotalScore;
                score.LearningOutcome = attendanceScore.ToString("0.0");

                // 3. CẬP NHẬT NHẬN XÉT (FEEDBACK)
                var comment = (row.FeedbackComments ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    var feedback = db.TeacherFeedbacks
                        .Where(f => f.EnrollmentId == row.EnrollmentId && f.TeacherId == userId)
                        .OrderByDescending(f => f.CreatedAt)
                        .FirstOrDefault();

                    if (feedback == null)
                    {
                        db.TeacherFeedbacks.Add(new TeacherFeedback
                        {
                            EnrollmentId = row.EnrollmentId,
                            TeacherId = userId,
                            Comments = comment,
                            CreatedAt = DateTime.Now
                        });
                    }
                    else
                    {
                        feedback.Comments = comment;
                        feedback.CreatedAt = DateTime.Now;
                    }
                }
            }

            db.SaveChanges();
            TempData["SuccessMessage"] = "Đã lưu điểm và nhận xét học viên thành công!";
            return RedirectToAction("TeacherGrades", new { classId = model.SelectedClassId });
        }
        #endregion

        #region 3. STUDENT - Theo dõi kết quả học tập
        [HttpGet]
        public ActionResult MyScores()
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Học viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            // Lấy toàn bộ lượt đăng ký lớp học của học viên kèm điểm số và lớp học
            var myEnrollments = db.Enrollments
                .Include(e => e.Class)
                .Include(e => e.Class.Course)
                .Include(e => e.AssessmentScore)
                .Where(e => e.StudentId == userId && (e.Status == "Confirmed" || e.Status == "Studying" || e.Status == "Completed" || e.Status == "Approved"))
                .OrderByDescending(e => e.EnrollmentDate)
                .ToList();

            return View(myEnrollments);
        }
        #endregion

        protected bool IsAuthorizedRole(params string[] roles)
        {
            var userRole = Session["Role"] as string ?? Session["RoleName"] as string;
            if (string.IsNullOrEmpty(userRole)) return false;
            return roles.Any(r => r.Equals(userRole, StringComparison.OrdinalIgnoreCase));
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