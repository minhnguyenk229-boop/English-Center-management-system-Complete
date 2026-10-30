using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Reports;

namespace English_language_center_management_system.Controllers
{
    public class ReportsController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        #region 1. BÁO CÁO TỔNG QUAN & LỌC DỮ LIỆU (INDEX)

        // GET: Reports
        public ActionResult Index(DateTime? fromDate, DateTime? toDate, int? courseId)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào trang Báo cáo!";
                return RedirectToAction("Login", "Auth");
            }

            // Mặc định lấy khoảng thời gian từ đầu tháng hiện tại nếu không truyền vào
            DateTime start = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime end = toDate.HasValue ? toDate.Value.Date.AddDays(1).AddTicks(-1) : DateTime.Now;

            // Lọc danh sách thanh toán trong khoảng thời gian
            var paymentsQuery = db.Payments
                .Include(p => p.Enrollment)
                .Include(p => p.Enrollment.Student)
                .Include(p => p.Enrollment.Class)
                .Include(p => p.Enrollment.Class.Course)
                .Where(p => p.PaymentDate >= start && p.PaymentDate <= end);

            // Lọc theo Khóa học nếu chọn
            if (courseId.HasValue && courseId.Value > 0)
            {
                paymentsQuery = paymentsQuery.Where(p => p.Enrollment.Class.CourseId == courseId.Value);
            }

            var paymentList = paymentsQuery.OrderByDescending(p => p.PaymentDate).ToList();

            // Tính toán chỉ số KPI
            decimal totalRevenue = paymentList.Sum(p => p.AmountPaid);
            int totalEnrollments = db.Enrollments.Count(e => e.EnrollmentDate >= start && e.EnrollmentDate <= end);
            int totalActiveClasses = db.Classes.Count(c => c.Status == "Open" || c.Status == "Studying");
            int totalStudents = db.StudentProfiles.Count();

            // Tổng hợp doanh thu theo từng Khóa học
            var courses = db.Courses.Include(c => c.Classes).ToList();
            var courseSummaries = courses.Select(c =>
            {
                var classIds = c.Classes.Select(cls => cls.Id).ToList();
                var coursePayments = paymentList.Where(p => p.Enrollment != null && p.Enrollment.Class != null && p.Enrollment.Class.CourseId == c.Id).ToList();
                var courseEnrollmentCount = db.Enrollments.Count(e => classIds.Contains(e.ClassId) && e.EnrollmentDate >= start && e.EnrollmentDate <= end);

                return new CourseRevenueSummaryItem
                {
                    CourseName = c.CourseName,
                    TotalClasses = c.Classes.Count,
                    TotalEnrollments = courseEnrollmentCount,
                    TotalRevenue = coursePayments.Sum(p => p.AmountPaid)
                };
            }).Where(cs => cs.TotalRevenue > 0 || cs.TotalEnrollments > 0).ToList();

            // Tổng hợp theo Phương thức thanh toán
            var methodSummaries = paymentList
                .GroupBy(p => p.PaymentMethod ?? "Khác")
                .Select(g => new PaymentMethodSummaryItem
                {
                    PaymentMethod = g.Key,
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(p => p.AmountPaid)
                }).ToList();

            var viewModel = new ReportIndexViewModel
            {
                FromDate = start,
                ToDate = toDate ?? DateTime.Now,
                SelectedCourseId = courseId,
                Courses = db.Courses.OrderBy(c => c.CourseName).ToList(),
                TotalRevenue = totalRevenue,
                TotalEnrollments = totalEnrollments,
                TotalActiveClasses = totalActiveClasses,
                TotalStudents = totalStudents,
                CourseSummaries = courseSummaries,
                PaymentMethodSummaries = methodSummaries,
                PaymentDetails = paymentList
            };

            return View(viewModel);
        }

        #endregion

        #region 2. XUẤT / IN BÁO CÁO (PRINT)

        // GET: Reports/Print

        public ActionResult Print(
            DateTime? fromDate,
            DateTime? toDate,
            int? courseId)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            DateTime start = fromDate ??
                new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            DateTime end = toDate.HasValue
                ? toDate.Value.Date.AddDays(1).AddTicks(-1)
                : DateTime.Now;

            var paymentsQuery = db.Payments
                .Include(p => p.Enrollment)
                .Include(p => p.Enrollment.Student)
                .Include(p => p.Enrollment.Class)
                .Include(p => p.Enrollment.Class.Course)
                .Where(p => p.PaymentDate >= start &&
                            p.PaymentDate <= end);

            if (courseId.HasValue && courseId.Value > 0)
            {
                paymentsQuery = paymentsQuery.Where(
                    p => p.Enrollment.Class.CourseId == courseId.Value);
            }

            var paymentList = paymentsQuery
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            var coursesQuery = db.Courses
                .Include(c => c.Classes)
                .AsQueryable();

            if (courseId.HasValue && courseId.Value > 0)
            {
                coursesQuery = coursesQuery.Where(c => c.Id == courseId.Value);
            }

            var courses = coursesQuery.ToList();

            var courseSummaries = courses.Select(c =>
            {
                var classIds = c.Classes.Select(cls => cls.Id).ToList();

                var coursePayments = paymentList
                    .Where(p =>
                        p.Enrollment != null &&
                        p.Enrollment.Class != null &&
                        p.Enrollment.Class.CourseId == c.Id)
                    .ToList();

                var courseEnrollmentCount = db.Enrollments.Count(e =>
                    classIds.Contains(e.ClassId) &&
                    e.EnrollmentDate >= start &&
                    e.EnrollmentDate <= end);

                return new CourseRevenueSummaryItem
                {
                    CourseName = c.CourseName,
                    TotalClasses = c.Classes.Count,
                    TotalEnrollments = courseEnrollmentCount,
                    TotalRevenue = coursePayments.Sum(p => p.AmountPaid)
                };
            })
            .Where(c => c.TotalRevenue > 0 || c.TotalEnrollments > 0)
            .ToList();

            var courseFilterName = "Tất cả khóa học";

            if (courseId.HasValue && courseId.Value > 0)
            {
                courseFilterName = db.Courses
                    .Where(c => c.Id == courseId.Value)
                    .Select(c => c.CourseName)
                    .FirstOrDefault() ?? "Khóa học đã chọn";
            }

            var viewModel = new PrintableReportViewModel
            {
                ReportTitle = "BÁO CÁO DOANH THU & HOẠT ĐỘNG TRUNG TÂM",
                CourseFilterName = courseFilterName,
                GeneratedBy = Session["FullName"]?.ToString()
                              ?? "Quản trị viên",
                GeneratedDate = DateTime.Now,
                FromDate = start,
                ToDate = toDate ?? DateTime.Now,
                TotalRevenue = paymentList.Sum(p => p.AmountPaid),
                TotalTransactions = paymentList.Count,
                CourseSummaries = courseSummaries,
                PaymentRecords = paymentList
            };

            return View(viewModel);
        }

        #endregion

        #region 3. HELPER METHODS

        private bool IsAdmin()
        {
            if (Session["UserId"] == null) return false;

            string roleName = Session["RoleName"]?.ToString();
            if (string.IsNullOrEmpty(roleName)) return false;

            return roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                   roleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
                   roleName.Equals("Quản trị viên", StringComparison.OrdinalIgnoreCase);
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