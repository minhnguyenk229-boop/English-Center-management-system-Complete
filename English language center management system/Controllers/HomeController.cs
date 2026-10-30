using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Home;

namespace English_language_center_management_system.Controllers
{
    public class HomeController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        #region 1. TRANG CHỦ PUBLIC & ĐIỀU HƯỚNG CHUNG

        // GET: /Home/Index
        [HttpGet]
        public ActionResult Index()
        {
            var viewModel = new HomeViewModel
            {
                FeaturedCourses = db.Courses.Take(6).ToList(),
                TotalCoursesCount = db.Courses.Count(),
                TotalStudentsCount = db.StudentProfiles.Count(),
                TotalTeachersCount = db.TeacherProfiles.Count(),
                TotalActiveClassesCount = db.Classes.Count(c => c.Status == "Open" || c.Status == "Studying")
            };

            return View(viewModel);
        }

        #endregion

        #region 2. ADMIN EXECUTIVE DASHBOARD (PHẦN D)

        // GET: /Home/AdminDashboard
        [HttpGet]
        public ActionResult AdminDashboard()
        {
            if (!IsAuthorizedRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang quản trị!";

                return RedirectToAction("Login", "Auth");
            }

            // 1. Tổng doanh thu các khoản đã thanh toán.
            decimal totalRevenue = db.Payments
                .Where(p =>
                    p.PaymentStatus == "Paid" ||
                    p.PaymentStatus == "Completed")
                .Select(p => (decimal?)p.AmountPaid)
                .Sum() ?? 0m;

            // 2. Các chỉ số tổng quan.
            int totalStudents = db.StudentProfiles.Count();

            int totalTeachers = db.TeacherProfiles.Count();

            int totalUsers = db.Users.Count();

            int totalCourses = db.Courses.Count();

            int activeClasses = db.Classes.Count(c =>
                c.Status == "Open" ||
                c.Status == "Studying"|| c.Status == "Full");

            int pendingEnrollments = db.Enrollments.Count(e =>
                e.Status == "Pending");

            string[] activeEnrollmentStatuses ={"Active","Approved","Confirmed", "Studying"};

            int activeStudents = db.Enrollments
                .Where(e => activeEnrollmentStatuses.Contains(e.Status))
                .Select(e => e.StudentId)
                .Distinct()
                .Count();

            // 3. Dữ liệu doanh thu 6 tháng gần nhất.
            DateTime currentMonth = new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1);

            DateTime chartStart = currentMonth.AddMonths(-5);

            var paymentRows = db.Payments
                .Where(p =>
                    (p.PaymentStatus == "Paid" ||
                     p.PaymentStatus == "Completed") &&
                    p.PaymentDate >= chartStart)
                .Select(p => new
                {
                    p.PaymentDate,
                    p.AmountPaid
                })
                .ToList();

            var monthlyRevenue = new List<MonthlyRevenuePointViewModel>();

            for (int i = 0; i < 6; i++)
            {
                DateTime monthStart = chartStart.AddMonths(i);
                DateTime monthEnd = monthStart.AddMonths(1);

                decimal revenue = paymentRows
                    .Where(p =>
                        p.PaymentDate >= monthStart &&
                        p.PaymentDate < monthEnd)
                    .Sum(p => p.AmountPaid);

                monthlyRevenue.Add(new MonthlyRevenuePointViewModel
                {
                    MonthLabel = "T" + monthStart.ToString("MM"),
                    Revenue = revenue
                });
            }

            // 4. Tìm các lớp đã sử dụng ít nhất 80% sức chứa.
            var classesToCheck = db.Classes
                .Include(c => c.Course)
                .Include(c => c.Enrollments)
                .Where(c =>
                    c.Status == "Open" ||
                    c.Status == "Studying" ||
                    c.Status == "Full")
                .ToList();

            var classesNearCapacity = classesToCheck
                .Select(c =>
                {
                    int enrolledCount = c.Enrollments == null
                        ? 0
                        : c.Enrollments.Count(e =>
                            activeEnrollmentStatuses.Contains(e.Status));

                    decimal occupancyPercent = c.MaxCapacity > 0
                        ? Math.Round(
                            enrolledCount * 100m / c.MaxCapacity,
                            1)
                        : 0m;

                    return new AdminClassCapacityViewModel
                    {
                        ClassId = c.Id,
                        ClassName = c.ClassName,

                        CourseName = c.Course != null
                            ? c.Course.CourseName
                            : "Chưa cập nhật",

                        //Room = c.Room,
                        EnrolledCount = enrolledCount,
                        MaxCapacity = c.MaxCapacity,
                        OccupancyPercent = occupancyPercent,
                        Status = c.Status
                    };
                })
                .Where(c =>
                    c.MaxCapacity > 0 &&
                    c.OccupancyPercent >= 80)
                .OrderByDescending(c => c.OccupancyPercent)
                .Take(6)
                .ToList();

            // 5. Năm đơn ghi danh mới nhất.
            var recentEnrollments = db.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .Include(e => e.Class.Course)
                .OrderByDescending(e => e.EnrollmentDate)
                .Take(5)
                .ToList();

            // 6. Năm giao dịch gần nhất.
            var recentPayments = db.Payments
                .Include(p => p.Enrollment)
                .Include(p => p.Enrollment.Student)
                .Include(p => p.Enrollment.Class)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToList();

            var model = new AdminDashboardViewModel
            {
                TotalRevenue = totalRevenue,
                TotalStudents = totalStudents,
                ActiveStudents = activeStudents,
                TotalTeachers = totalTeachers,
                TotalUsers = totalUsers,
                TotalCourses = totalCourses,
                ActiveClasses = activeClasses,
                PendingEnrollments = pendingEnrollments,
                MonthlyRevenue = monthlyRevenue,
                ClassesNearCapacity = classesNearCapacity,
                RecentEnrollments = recentEnrollments,
                RecentPayments = recentPayments
            };

            return View(model);
        }


        #endregion

        #region 3. TEACHER DASHBOARD (PHẦN B)

        // GET: /Home/TeacherDashboard

        [HttpGet]
        public ActionResult TeacherDashboard()
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Giáo viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var user = db.Users.Find(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var teacherProfile = db.TeacherProfiles
                .FirstOrDefault(tp => tp.UserId == userId);

            // 1. Các lớp được phân công cho giáo viên đang đăng nhập.
            var assignedClasses = db.Classes
                .Include(c => c.Course)
                .Where(c => c.TeacherId == userId)
                .OrderBy(c => c.ClassName)
                .ToList();

            var assignedClassIds = assignedClasses
                .Select(c => c.Id)
                .ToList();

            // Các trạng thái ghi danh có thể xuất hiện trong dự án.
            // Không tính học viên đã rút khỏi lớp hoặc đã hoàn thành.
            int totalStudents = db.Enrollments
                .Where(e =>
                    assignedClassIds.Contains(e.ClassId) &&
                    (
                        e.Status == "Confirmed" ||
                        e.Status == "Studying" ||
                        e.Status == "Active" ||
                        e.Status == "Approved"
                    ))
                .Select(e => e.StudentId)
                .Distinct()
                .Count();

            // 2. Các buổi dạy sắp diễn ra.
            DateTime today = DateTime.Today;

            var upcomingSessions = db.ClassSessions
                .Include(cs => cs.Class)
                .Include(cs => cs.Class.Course)
                .Where(cs =>
                    assignedClassIds.Contains(cs.ClassId) &&
                    cs.SessionDate >= today)
                .OrderBy(cs => cs.SessionDate)
                .ThenBy(cs => cs.StartTime)
                .Take(5)
                .ToList();

            // 3. Tổng số bài tập thuộc các lớp của giáo viên.
            int totalAssignments = db.Assignments
                .Count(a => assignedClassIds.Contains(a.ClassId));

            // 4. Số bài nộp chưa được chấm điểm.
            int pendingSubmissionsCount = db.Submissions
                .Count(s =>
                    assignedClassIds.Contains(s.Assignment.ClassId) &&
                    !s.Grade.HasValue);

            // 5. Danh sách tối đa 5 bài nộp chưa chấm gần nhất.
            var pendingSubmissions = db.Submissions
                .Include(s => s.Assignment)
                .Include(s => s.Assignment.Class)
                .Include(s => s.Student)
                .Where(s =>
                    assignedClassIds.Contains(s.Assignment.ClassId) &&
                    !s.Grade.HasValue)
                .OrderBy(s => s.SubmittedAt)
                .Take(5)
                .ToList();

            // 6. Các bài tập sắp đến hạn thuộc lớp của giáo viên.
            var upcomingAssignments = db.Assignments
                .Include(a => a.Class)
                .Include(a => a.Class.Course)
                .Where(a =>
                    assignedClassIds.Contains(a.ClassId) &&
                    a.DueDate >= today)
                .OrderBy(a => a.DueDate)
                .Take(5)
                .ToList();

            var dashboardData = new TeacherDashboardViewModel
            {
                TeacherInfo = user,
                Profile = teacherProfile,

                TotalAssignedClasses = assignedClasses.Count,
                TotalStudentsTaught = totalStudents,

                AssignedClasses = assignedClasses,
                UpcomingTeachingSessions = upcomingSessions,

                TotalAssignments = totalAssignments,
                PendingSubmissionsCount = pendingSubmissionsCount,
                PendingSubmissions = pendingSubmissions,
                UpcomingAssignments = upcomingAssignments
            };

            return View(dashboardData);
        }
        #endregion

        #region 4. STUDENT DASHBOARD (PHẦN C)

        // GET: /Home/StudentDashboard

        [HttpGet]
        public ActionResult StudentDashboard()
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Học viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var user = db.Users.Find(userId);

            var studentProfile = db.StudentProfiles
                .FirstOrDefault(sp => sp.UserId == userId);

            // Lấy các lớp đã duyệt hoặc đang học
            var activeEnrollments = db.Enrollments
                .Include(e => e.Class)
                .Include(e => e.Class.Course)
                .Where(e => e.StudentId == userId && (e.Status == "Active" || e.Status == "Approved"))
                .OrderByDescending(e => e.EnrollmentDate)
                .ToList();

            var enrolledClassIds = activeEnrollments
                .Select(e => e.ClassId)
                .Distinct()
                .ToList();

            var enrollmentIds = activeEnrollments
                .Select(e => e.Id)
                .ToList();

            // Lịch học sắp tới
            DateTime today = DateTime.Today;

            var upcomingSessions = db.ClassSessions
                .Include(s => s.Class)
                .Include(s => s.Class.Course)
                .Where(s =>
                    enrolledClassIds.Contains(s.ClassId) &&
                    s.SessionDate >= today)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.StartTime)
                .Take(5)
                .ToList();

            // Tổng số chứng chỉ
            int totalCertificates = db.Certificates
                .Count(c => c.Enrollment.StudentId == userId);

            // Tổng học phí các lớp đã duyệt/đang học
            decimal totalTuitionFee = activeEnrollments
                .Where(e => e.Class != null && e.Class.Course != null)
                .Sum(e => e.Class.Course.TuitionFee);

            // Các khoản thanh toán đã ghi nhận
            decimal totalPaid = 0m;

            if (enrollmentIds.Any())
            {
                totalPaid = db.Payments
                    .Where(p =>
                        enrollmentIds.Contains(p.EnrollmentId) &&
                        (p.PaymentStatus == "Paid" ||
                         p.PaymentStatus == "Partial" ||
                         p.PaymentStatus == "Completed"))
                    .Select(p => (decimal?)p.AmountPaid)
                    .Sum() ?? 0m;
            }

            decimal outstandingBalance =
                Math.Max(totalTuitionFee - totalPaid, 0m);

            // Thông báo gần đây
            var recentNotifications = db.Notifications
                .Where(n => n.UserId == userId)
                .OrderBy(n => n.IsRead)
                .ThenByDescending(n => n.CreatedAt)
                .Take(4)
                .ToList();

            // Kết quả đánh giá gần đây
            var recentScores = db.AssessmentScores
                .Include(a => a.Enrollment)
                .Include(a => a.Enrollment.Class)
                .Include(a => a.Enrollment.Class.Course)
                .Where(a => a.Enrollment.StudentId == userId)
                .OrderByDescending(a => a.Enrollment.EnrollmentDate)
                .Take(4)
                .ToList();

            var dashboardData = new StudentDashboardViewModel
            {
                StudentInfo = user,
                Profile = studentProfile,

                TotalEnrolledClasses = activeEnrollments.Count,
                TotalCertificates = totalCertificates,

                ActiveEnrollments = activeEnrollments,
                UpcomingSessions = upcomingSessions,

                TotalTuitionFee = totalTuitionFee,
                TotalPaid = totalPaid,
                OutstandingBalance = outstandingBalance,

                RecentNotifications = recentNotifications,
                RecentScores = recentScores
            };

            return View(dashboardData);
        }
        #endregion

        #region 5. TRANG THÔNG TIN BỔ TRỢ & HELPER

        public ActionResult About()
        {
            ViewBag.Message = "Giới thiệu về Trung tâm Anh ngữ";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Thông tin liên hệ Trung tâm";
            return View();
        }

        private bool IsAuthorizedRole(params string[] allowedRoles)
        {
            if (Session["UserId"] == null) return false;
            string currentRole = Session["RoleName"]?.ToString();
            if (string.IsNullOrEmpty(currentRole)) return false;

            return allowedRoles.Any(r => r.Equals(currentRole, StringComparison.OrdinalIgnoreCase));
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