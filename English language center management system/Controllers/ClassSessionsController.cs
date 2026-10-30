using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Classes;
using English_language_center_management_system.Models.ViewModels.ClassSessions;
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
    public class ClassSessionsController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();


        #region 1. ADMIN - Quản lý Buổi học / Lịch học (CRUD)

        // GET: ClassSessions/AdminTimetable
        [HttpGet]
        public ActionResult AdminTimetable(int? courseId, int? classId, int weekOffset = 0, DateTime? selectedDate = null)
        {
            if (!IsAuthorizedRole("Admin", "Quản trị viên", "Administrator"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            // 1. Tính toán Ngày đầu tuần (Thứ 2) và Ngày cuối tuần (Chủ Nhật)
            DateTime baseDate = selectedDate ?? DateTime.Today;
            int daysSinceMonday = ((int)baseDate.DayOfWeek + 6) % 7; // Đưa về Thứ 2 gần nhất
            DateTime currentMonday = baseDate.Date.AddDays(-daysSinceMonday);

            // Nếu người dùng chọn ngày cụ thể từ DatePicker thì tính lại weekOffset tương ứng, ngược lại cộng theo weekOffset
            DateTime weekStart = selectedDate.HasValue ? currentMonday : currentMonday.AddDays(7 * weekOffset);
            DateTime weekEnd = weekStart.AddDays(6); // Chủ nhật
            DateTime weekEndExclusive = weekStart.AddDays(7);

            // Đồng bộ lại weekOffset nếu người dùng nhảy tuần bằng lịch (selectedDate)
            int todayDaysSinceMonday = ((int)DateTime.Today.DayOfWeek + 6) % 7;
            DateTime thisWeekMonday = DateTime.Today.AddDays(-todayDaysSinceMonday);
            weekOffset = (int)((weekStart - thisWeekMonday).TotalDays / 7);

            // 2. Truy vấn toàn bộ buổi học trong tuần (để kiểm tra xung đột chính xác trên toàn trung tâm trước khi lọc)
            var allWeekSessions = db.ClassSessions
                .Include(s => s.Class)
                .Include(s => s.Class.Course)
                .Include(s => s.Class.Teacher)
                .Include(s => s.Room)
                .Include(s => s.Attendances)
                .Where(s => s.SessionDate >= weekStart && s.SessionDate < weekEndExclusive)
                .ToList();

            // 3. NGHIỆP VỤ: Phát hiện xung đột (Trùng Phòng hoặc Trùng Giáo viên trong cùng Ngày & Khung giờ giao nhau)
            var roomConflicts = new HashSet<int>();
            var teacherConflicts = new HashSet<int>();

            for (int i = 0; i < allWeekSessions.Count; i++)
            {
                for (int j = i + 1; j < allWeekSessions.Count; j++)
                {
                    var s1 = allWeekSessions[i];
                    var s2 = allWeekSessions[j];

                    // Cùng ngày và khung giờ giao nhau (StartA < EndB && StartB < EndA)
                    if (s1.SessionDate.Date == s2.SessionDate.Date &&
                        s1.StartTime < s2.EndTime && s2.StartTime < s1.EndTime)
                    {
                        // Kiểm tra trùng phòng học
                        if (s1.RoomId.HasValue && s2.RoomId.HasValue && s1.RoomId.Value == s2.RoomId.Value)
                        {
                            roomConflicts.Add(s1.Id);
                            roomConflicts.Add(s2.Id);
                        }

                        // Kiểm tra trùng giáo viên
                        if (s1.Class?.TeacherId != null && s2.Class?.TeacherId != null &&
                            s1.Class.TeacherId == s2.Class.TeacherId)
                        {
                            teacherConflicts.Add(s1.Id);
                            teacherConflicts.Add(s2.Id);
                        }
                    }
                }
            }

            // 4. Áp dụng Bộ lọc hiển thị theo Khóa học (CourseId) và Lớp học (ClassId)
            var filteredSessions = allWeekSessions.AsEnumerable();

            if (courseId.HasValue && courseId.Value > 0)
            {
                filteredSessions = filteredSessions.Where(s => s.Class != null && s.Class.CourseId == courseId.Value);
            }

            if (classId.HasValue && classId.Value > 0)
            {
                filteredSessions = filteredSessions.Where(s => s.ClassId == classId.Value);
            }

            var finalSessionList = filteredSessions
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.StartTime)
                .ToList();

            // 5. Lấy danh sách Lớp học cho Dropdown (nếu đã chọn Khóa học thì chỉ hiện Lớp thuộc Khóa học đó)
            var classesQuery = db.Classes.AsQueryable();
            if (courseId.HasValue && courseId.Value > 0)
            {
                classesQuery = classesQuery.Where(c => c.CourseId == courseId.Value);
            }

            var allConflictIds = roomConflicts.Union(teacherConflicts).ToList();

            // 6. Đóng gói vào AdminTimetableViewModel
            var viewModel = new AdminTimetableViewModel
            {
                Sessions = finalSessionList,
                SelectedCourseId = courseId,
                SelectedClassId = classId,
                Courses = db.Courses.OrderBy(c => c.CourseName).ToList(),
                Classes = classesQuery.OrderBy(c => c.ClassName).ToList(),
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                WeekOffset = weekOffset,
                RoomConflictSessionIds = roomConflicts.ToList(),
                TeacherConflictSessionIds = teacherConflicts.ToList(),
                TotalSessionsInWeek = finalSessionList.Count,
                AttendedSessionsCount = finalSessionList.Count(s => s.Attendances != null && s.Attendances.Any()),
                ConflictSessionsCount = finalSessionList.Count(s => allConflictIds.Contains(s.Id))
            };

            return View(viewModel);
        }

        // GET: ClassSessions
        public ActionResult Index()
        {
            if (!IsAuthorizedRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            var classSessions = db.ClassSessions
                .Include(c => c.Class)
                .Include(c => c.Class.Course)
                .Include(c => c.Room)
                .OrderByDescending(c => c.SessionDate)
                .ThenBy(c => c.StartTime);

            return View(classSessions.ToList());
        }

        // GET: ClassSessions/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAuthorizedRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ClassSession classSession = db.ClassSessions
                .Include(c => c.Class)
                .Include(c => c.Class.Course)
                .Include(c => c.Room)
                .FirstOrDefault(c => c.Id == id);

            if (classSession == null)
            {
                return HttpNotFound();
            }

            return View(classSession);
        }

        // GET: ClassSessions/Create
        public ActionResult Create()
        {
            if (!IsAuthorizedRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.ClassId = new SelectList(db.Classes.OrderBy(c => c.ClassName), "Id", "ClassName");
            ViewBag.RoomId = new SelectList(db.Rooms.Where(r => r.IsActive).OrderBy(r => r.RoomName), "Id", "RoomName");
            return View();
        }

        // POST: ClassSessions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,ClassId,SessionDate,StartTime,EndTime,RoomId")] ClassSession classSession)
        {
            if (!IsAuthorizedRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                db.ClassSessions.Add(classSession);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Tạo buổi học mới thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.ClassId = new SelectList(db.Classes.OrderBy(c => c.ClassName), "Id", "ClassName", classSession.ClassId);
            return View(classSession);
        }

        // GET: ClassSessions/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAuthorizedRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ClassSession classSession = db.ClassSessions.Find(id);
            if (classSession == null)
            {
                return HttpNotFound();
            }

            ViewBag.ClassId = new SelectList(db.Classes.OrderBy(c => c.ClassName), "Id", "ClassName", classSession.ClassId);
            ViewBag.RoomId = new SelectList(db.Rooms.Where(r => r.IsActive).OrderBy(r => r.RoomName), "Id", "RoomName", classSession.RoomId);
            return View(classSession);
        }

        // POST: ClassSessions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,ClassId,SessionDate,StartTime,EndTime,RoomId")] ClassSession classSession)
        {
            if (!IsAuthorizedRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                db.Entry(classSession).State = EntityState.Modified;
                db.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật buổi học thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.ClassId = new SelectList(db.Classes.OrderBy(c => c.ClassName), "Id", "ClassName", classSession.ClassId);
            return View(classSession);
        }

        // GET: ClassSessions/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAuthorizedRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ClassSession classSession = db.ClassSessions
                .Include(c => c.Class)
                .Include(c => c.Class.Course)
                .Include(c => c.Room)
                .FirstOrDefault(c => c.Id == id);

            if (classSession == null)
            {
                return HttpNotFound();
            }

            return View(classSession);
        }

        // POST: ClassSessions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAuthorizedRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            ClassSession classSession = db.ClassSessions.Find(id);
            if (classSession != null)
            {
                bool hasAttendances = db.Attendances.Any(a => a.SessionId == id);
                if (hasAttendances)
                {
                    TempData["ErrorMessage"] = "Không thể xóa buổi học đã có dữ liệu điểm danh!";
                    return RedirectToAction("Index");
                }

                db.ClassSessions.Remove(classSession);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa buổi học thành công!";
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region 2. TEACHER - Lịch dạy theo tuần

        [HttpGet]
        public ActionResult TeacherTeachingSchedule(int weekOffset = 0)
        {
            if (!IsAuthorizedRole("Teacher"))
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

            weekOffset = Math.Max(-52, Math.Min(52, weekOffset));

            string[] activeStatuses = { "Open", "Studying", "Full", "Mở đăng ký", "Đang học", "Đã đủ sĩ số" };

            var assignedClasses = db.Classes
                .Include(c => c.Course)
                .Where(c => c.TeacherId == userId && activeStatuses.Contains(c.Status))
                .OrderBy(c => c.ClassName)
                .ToList();

            var classIds = assignedClasses.Select(c => c.Id).ToList();

            var enrollmentRows = db.Enrollments
                .Where(e => classIds.Contains(e.ClassId) &&
                    (e.Status == "Active" || e.Status == "Approved" || e.Status == "Studying" || e.Status == "Confirmed"))
                .Select(e => new { e.ClassId, e.StudentId })
                .ToList();

            var enrolledCounts = enrollmentRows
                .GroupBy(e => e.ClassId)
                .ToDictionary(g => g.Key, g => g.Select(e => e.StudentId).Distinct().Count());

            DateTime today = DateTime.Today;
            int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            DateTime currentMonday = today.AddDays(-daysSinceMonday);

            DateTime weekStart = currentMonday.AddDays(7 * weekOffset);
            DateTime weekEndExclusive = weekStart.AddDays(7);

            var sessions = db.ClassSessions
                .Include(s => s.Class)
                .Include(s => s.Class.Course)
                .Include(s => s.Room)
                .Include(s => s.Attendances)
                .Where(s => classIds.Contains(s.ClassId) &&
                    s.SessionDate >= weekStart &&
                    s.SessionDate < weekEndExclusive)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.StartTime)
                .ToList();

            var viewModel = new TeacherTeachingScheduleViewModel
            {
                TeacherInfo = teacher,
                AssignedClasses = assignedClasses,
                Sessions = sessions,
                EnrolledCounts = enrolledCounts,
                WeekStart = weekStart,
                WeekEnd = weekEndExclusive.AddDays(-1),
                WeekOffset = weekOffset
            };

            return View(viewModel);
        }

        #endregion


        #region 3. STUDENT - Thời khóa biểu theo tuần
        [HttpGet]
        public ActionResult StudentTimetable(int? courseId, int weekOffset = 0)
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Học viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Find(userId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            weekOffset = Math.Max(-52, Math.Min(52, weekOffset));

            // 1. Lấy các lượt ghi danh của học viên đang ở trạng thái Đã đăng ký (Đã duyệt) hoặc Đang học
            string[] validEnrollmentStatuses = {"Active", "Approved", "Studying", "Confirmed", "Đã duyệt", "Đang học", "Đã xác nhận" };

            var studentEnrollments = db.Enrollments
                .Include(e => e.Class)
                .Include(e => e.Class.Course)
                .Where(e => e.StudentId == userId && validEnrollmentStatuses.Contains(e.Status))
                .ToList();

            // 2. Rút trích danh sách các Môn học / Khóa học (Course) mà học viên đã đăng ký hoặc đang học để đưa vào Dropdown
            var enrolledCourses = studentEnrollments
                .Where(e => e.Class != null && e.Class.Course != null)
                .Select(e => e.Class.Course)
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .OrderBy(c => c.CourseName)
                .ToList();

            // 3. Lọc danh sách ClassId theo Môn học (CourseId) nếu học viên có chọn trên bộ lọc
            var filteredEnrollments = studentEnrollments.AsEnumerable();
            if (courseId.HasValue && courseId.Value > 0)
            {
                filteredEnrollments = filteredEnrollments.Where(e => e.Class != null && e.Class.CourseId == courseId.Value);
            }

            var classIds = filteredEnrollments
                .Select(e => e.ClassId)
                .Distinct()
                .ToList();

            // 4. Tính ngày bắt đầu (Thứ 2) và kết thúc (Chủ Nhật) của tuần được chọn
            DateTime today = DateTime.Today;
            int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            DateTime currentMonday = today.AddDays(-daysSinceMonday);
            DateTime weekStart = currentMonday.AddDays(7 * weekOffset);
            DateTime weekEndExclusive = weekStart.AddDays(7);

            // 5. Truy vấn các buổi học trong tuần (Include thêm Room, Teacher và Attendances để hiển thị trên lưới)
            var sessions = db.ClassSessions
                .Include(s => s.Class)
                .Include(s => s.Class.Course)
                .Include(s => s.Class.Teacher)
                .Include(s => s.Room)
                .Include(s => s.Attendances)
                .Where(s => classIds.Contains(s.ClassId) &&
                            s.SessionDate >= weekStart &&
                            s.SessionDate < weekEndExclusive)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.StartTime)
                .ToList();

            var viewModel = new StudentTimetableViewModel
            {
                StudentInfo = user,
                WeekStart = weekStart,
                WeekEnd = weekEndExclusive.AddDays(-1),
                WeekOffset = weekOffset,
                Sessions = sessions,
                EnrolledCourses = enrolledCourses,
                SelectedCourseId = courseId
            };

            return View(viewModel);
        }

        #endregion

        #region Helper Authorization Method
        protected bool IsAuthorizedRole(params string[] roles)
        {
            var userRole = Session["Role"] as string ?? Session["RoleName"] as string;
            if (string.IsNullOrEmpty(userRole)) return false;

            return roles.Any(r => r.Equals(userRole, StringComparison.OrdinalIgnoreCase));
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