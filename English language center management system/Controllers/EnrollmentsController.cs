using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Enrollments;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace English_language_center_management_system.Controllers
{
    public class EnrollmentsController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        #region 1. DANH SÁCH GHI DANH (INDEX)

        // GET: Enrollments
        public ActionResult Index(string searchTerm, int? classId, string status)
        {
            if (!IsAuthorizedRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào quản lý ghi danh!";
                return RedirectToAction("Login", "Auth");
            }

            var enrollmentsQuery = db.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .Include(e => e.Class.Course)
                .AsQueryable();

            // Tìm kiếm theo Tên học viên, Username, Email hoặc Tên lớp
            if (!string.IsNullOrEmpty(searchTerm))
            {
                enrollmentsQuery = enrollmentsQuery.Where(e =>
                    e.Student.FullName.Contains(searchTerm) ||
                    e.Student.Username.Contains(searchTerm) ||
                    e.Student.Email.Contains(searchTerm) ||
                    e.Class.ClassName.Contains(searchTerm));
            }

            // Lọc theo Lớp học
            if (classId.HasValue && classId.Value > 0)
            {
                enrollmentsQuery = enrollmentsQuery.Where(e => e.ClassId == classId.Value);
            }

            // Lọc theo Trạng thái (Pending, Approved, Active, Rejected, Cancelled)
            if (!string.IsNullOrEmpty(status))
            {
                enrollmentsQuery = enrollmentsQuery.Where(e => e.Status == status);
            }

            var enrollmentList = enrollmentsQuery.OrderByDescending(e => e.EnrollmentDate).ToList();

            // Lấy thông tin thanh toán cho từng lượt ghi danh
            var enrollmentItems = enrollmentList.Select(e =>
            {
                var payment = db.Payments.FirstOrDefault(p => p.EnrollmentId == e.Id);
                return new EnrollmentIndexItemViewModel
                {
                    Enrollment = e,
                    AmountPaid = payment != null ? payment.AmountPaid : 0,
                    PaymentStatus = payment != null ? payment.PaymentStatus : "Unpaid"
                };
            }).ToList();

            var viewModel = new EnrollmentIndexViewModel
            {
                Enrollments = enrollmentItems,
                SearchTerm = searchTerm,
                SelectedClassId = classId,
                SelectedStatus = status,
                Classes = db.Classes.OrderBy(c => c.ClassName).ToList()
            };

            return View(viewModel);
        }

        #endregion

        #region 2. XEM CHI TIẾT GHI DANH (DETAILS)

        // GET: Enrollments/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAuthorizedRole("Admin")) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Enrollment enrollment = db.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .Include(e => e.Class.Course)
                .Include(e => e.Class.Teacher)
                .FirstOrDefault(e => e.Id == id);

            if (enrollment == null)
            {
                return HttpNotFound();
            }

            ViewBag.Payments = db.Payments.Where(p => p.EnrollmentId == id).ToList();

            return View(enrollment);
        }

        #endregion

        #region 3. THÊM GHI DANH MỚI (CREATE)

        // GET: Enrollments/Create
        public ActionResult Create()
        {
            if (!IsAuthorizedRole("Admin")) return RedirectToAction("Login", "Auth");

            PopulateDropdowns();
            return View(new EnrollmentCreateViewModel());
        }

        // POST: Enrollments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EnrollmentCreateViewModel model)
        {
            if (!IsAuthorizedRole("Admin")) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                bool alreadyEnrolled = db.Enrollments.Any(e => e.StudentId == model.StudentId && e.ClassId == model.ClassId && e.Status != "Cancelled" && e.Status != "Rejected");
                if (alreadyEnrolled) ModelState.AddModelError("", "Học viên này đã ghi danh vào lớp học này rồi!");

                var selectedClass = db.Classes.Include(c => c.Course).FirstOrDefault(c => c.Id == model.ClassId);
                if (selectedClass != null)
                {
                    int currentEnrolled = db.Enrollments.Count(e => e.ClassId == model.ClassId && (e.Status == "Active" || e.Status == "Approved"));
                    if (currentEnrolled >= selectedClass.MaxCapacity)
                        ModelState.AddModelError("ClassId", $"Lớp học đã đủ sĩ số tối đa ({selectedClass.MaxCapacity})!");

                    // 2. NGHIỆP VỤ MỚI: Kiểm tra trùng lịch học của Học viên
                    string conflictError = CheckStudentScheduleConflict(model.StudentId, selectedClass);
                    if (!string.IsNullOrEmpty(conflictError))
                    {
                        ModelState.AddModelError("ClassId", conflictError); // Cảnh báo lỗi ngay tại Dropdown Lớp học
                    }


                }

                if (ModelState.IsValid)
                {
                    var enrollment = new Enrollment
                    {
                        StudentId = model.StudentId,
                        ClassId = model.ClassId,
                        Status = "Pending", // NGHIỆP VỤ MỚI: Ép mặc định về chờ duyệt
                        EnrollmentDate = model.EnrollmentDate
                    };
                    db.Enrollments.Add(enrollment);
                    db.SaveChanges(); // Cần lưu trước để sinh ra enrollment.Id

                    // NGHIỆP VỤ MỚI: Lấy học phí và tự động tạo công nợ (Payment)
                    if (selectedClass != null && selectedClass.Course != null)
                    {
                        // Sinh mã biên lai tạm (Hoặc bạn có thể chuyển hàm GenerateReceiptNumber ra Helper)
                        string receipt = $"REC-{DateTime.Now:yyyyMMdd}-{enrollment.Id}";

                        var newPayment = new Payment
                        {
                            EnrollmentId = enrollment.Id,
                            AmountPaid = 0, // Mới ghi danh chưa đóng tiền
                            PaymentDate = DateTime.Now,
                            ReceiptNumber = receipt,
                            PaymentMethod = "Cash",
                            PaymentStatus = "Pending" // Đang nợ 100% học phí
                        };
                        db.Payments.Add(newPayment);
                        db.SaveChanges();
                    }

                    TempData["SuccessMessage"] = "Ghi danh thành công! Đã tạo công nợ chờ thanh toán.";
                    return RedirectToAction("Index");
                }
            }
            PopulateDropdowns(model.StudentId, model.ClassId);
            return View(model);
        }

        #endregion

        #region 4. CẬP NHẬT GHI DANH (EDIT)

        // GET: Enrollments/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAuthorizedRole("Admin")) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Enrollment enrollment = db.Enrollments.Find(id);
            if (enrollment == null)
            {
                return HttpNotFound();
            }

            var viewModel = new EnrollmentEditViewModel
            {
                Id = enrollment.Id,
                StudentId = enrollment.StudentId,
                ClassId = enrollment.ClassId,
                Status = enrollment.Status,
                EnrollmentDate = enrollment.EnrollmentDate
            };

            PopulateDropdowns(enrollment.StudentId, enrollment.ClassId);
            return View(viewModel);
        }

        // POST: Enrollments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EnrollmentEditViewModel model)
        {
            if (!IsAuthorizedRole("Admin")) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                Enrollment enrollment = db.Enrollments.Find(model.Id);
                if (enrollment == null)
                {
                    return HttpNotFound();
                }

                // Nếu đổi sang trạng thái Approved/Active, kiểm tra xem lớp có bị vượt quá sĩ số không
                if ((model.Status == "Approved" || model.Status == "Active") &&
                    (enrollment.Status != "Approved" && enrollment.Status != "Active"))
                {
                    var selectedClass = db.Classes.Find(model.ClassId);
                    if (selectedClass != null)
                    {
                        int currentEnrolled = db.Enrollments.Count(e => e.ClassId == model.ClassId && (e.Status == "Active" || e.Status == "Approved"));
                        if (currentEnrolled >= selectedClass.MaxCapacity)
                        {
                            ModelState.AddModelError("Status", $"Không thể duyệt! Lớp học đã đạt sĩ số tối đa ({selectedClass.MaxCapacity}).");
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    enrollment.StudentId = model.StudentId;
                    enrollment.ClassId = model.ClassId;
                    enrollment.Status = model.Status;
                    enrollment.EnrollmentDate = model.EnrollmentDate;

                    db.Entry(enrollment).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Cập nhật thông tin ghi danh thành công!";
                    return RedirectToAction("Index");
                }
            }

            PopulateDropdowns(model.StudentId, model.ClassId);
            return View(model);
        }

        #endregion

        #region 5. QUICK ACTIONS: DUYỆT / TỪ CHỐI ĐƠN

        // POST: Enrollments/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            if (!IsAuthorizedRole("Admin")) return RedirectToAction("Login", "Auth");

            Enrollment enrollment = db.Enrollments.Find(id);
            if (enrollment != null)
            {
                var payment = db.Payments.FirstOrDefault(p => p.EnrollmentId == id);
                if (payment == null || (payment.PaymentStatus != "Paid" && payment.PaymentStatus != "Partial"))
                {
                    TempData["ErrorMessage"] = "Không thể duyệt đơn! Học viên chưa nộp học phí (Yêu cầu thanh toán đủ hoặc 1 phần).";
                    return RedirectToAction("Index");
                }

                var selectedClass = db.Classes.Find(enrollment.ClassId);
                int currentEnrolled = db.Enrollments.Count(e => e.ClassId == enrollment.ClassId && (e.Status == "Active" || e.Status == "Approved"));

                if (selectedClass != null && currentEnrolled >= selectedClass.MaxCapacity)
                {
                    TempData["ErrorMessage"] = "Không thể duyệt đơn! Lớp học đã đầy sĩ số.";
                }
                else
                {
                    enrollment.Status = "Approved";
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Đã phê duyệt đơn ghi danh thành công!";
                }
            }
            return RedirectToAction("Index");
        }

        // POST: Enrollments/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id)
        {
            if (!IsAuthorizedRole("Admin")) return RedirectToAction("Login", "Auth");

            Enrollment enrollment = db.Enrollments.Find(id);
            if (enrollment != null)
            {
                enrollment.Status = "Rejected";
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã từ chối đơn ghi danh!";
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region 6. XÓA GHI DANH (DELETE)

        // GET: Enrollments/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAuthorizedRole("Admin")) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Enrollment enrollment = db.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .FirstOrDefault(e => e.Id == id);

            if (enrollment == null)
            {
                return HttpNotFound();
            }

            return View(enrollment);
        }

        // POST: Enrollments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAuthorizedRole("Admin")) return RedirectToAction("Login", "Auth");

            Enrollment enrollment = db.Enrollments.Find(id);
            if (enrollment != null)
            {
                // Kiểm tra xem đã phát sinh giao dịch thanh toán chưa
                bool hasPayments = db.Payments.Any(p => p.EnrollmentId == id);
                if (hasPayments)
                {
                    TempData["ErrorMessage"] = "Không thể xóa đơn ghi danh đã phát sinh lịch sử thanh toán học phí!";
                    return RedirectToAction("Index");
                }

                db.Enrollments.Remove(enrollment);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa bản ghi ghi danh!";
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region 7. HỌC VIÊN TỰ GHI DANH (STUDENT PORTAL)

        // GET: Enrollments/StudentEnroll
        public ActionResult StudentEnroll(int? courseId, int? classId)
        {
            if (!IsAuthorizedRole("Student"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập với tài khoản Học viên để ghi danh!";
                return RedirectToAction("Login", "Auth");
            }

            // 1. NẾU CHƯA CHỌN LỚP (classId rỗng) -> HIỂN THỊ DANH SÁCH LỚP ĐANG MỞ
            if (classId == null)
            {
                var courses = db.Courses.Where(c => !c.IsHidden).ToList();

                // Lấy các lớp đang ở trạng thái Mở đăng ký
                var classesQuery = db.Classes.Include(c => c.Course)
                    .Where(c => c.Status == "Open" || c.Status == "Mở đăng ký" || c.Status == "RegistrationOpen");

                if (courseId.HasValue && courseId.Value > 0)
                {
                    classesQuery = classesQuery.Where(c => c.CourseId == courseId.Value);
                }

                ViewBag.Courses = new SelectList(courses, "Id", "CourseName", courseId);
                ViewBag.SelectedCourseId = courseId;

                // Trả về view danh sách để học viên chọn (View mới)
                return View("StudentEnrollList", classesQuery.ToList());
            }

            // 2. NẾU ĐÃ CHỌN LỚP -> HIỂN THỊ TRANG XÁC NHẬN (Giữ nguyên logic cũ của bạn)
            var selectedClass = db.Classes.Include(c => c.Course).FirstOrDefault(c => c.Id == classId);
            if (selectedClass == null) return HttpNotFound();

            var viewModel = new StudentEnrollViewModel
            {
                ClassId = selectedClass.Id,
                ClassName = selectedClass.ClassName,
                CourseName = selectedClass.Course?.CourseName,
                TuitionFee = selectedClass.Course?.TuitionFee ?? 0,
                ScheduleDays = selectedClass.ScheduleDays,
                DailyStartTime = selectedClass.DailyStartTime,
                DailyEndTime = selectedClass.DailyEndTime,
                StartDate = selectedClass.StartDate
            };

            // Trả về giao diện xác nhận (chính là file StudentEnroll.cshtml bạn đã làm trước đó)
            return View(viewModel);
        }

        // POST: Enrollments/StudentEnroll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StudentEnroll(int classId)
        {
            if (Session["UserId"] == null || Session["RoleName"]?.ToString() != "Student")
            {
                return RedirectToAction("Login", "Auth");
            }

            int studentId = (int)Session["UserId"];
            var selectedClass = db.Classes.Include(c => c.Course).FirstOrDefault(c => c.Id == classId);

            if (selectedClass == null) return HttpNotFound();

            // 1. Kiểm tra chống Spam / Trùng lặp
            bool alreadyEnrolled = db.Enrollments.Any(e => e.StudentId == studentId && e.ClassId == classId && e.Status != "Cancelled" && e.Status != "Rejected");
            if (alreadyEnrolled)
            {
                TempData["ErrorMessage"] = "Bạn đã ghi danh vào lớp học này rồi!";
                return RedirectToAction("StudentDashboard", "Home");
            }

            // 2. Kiểm tra Rào cản sĩ số
            int currentEnrolled = db.Enrollments.Count(e => e.ClassId == classId && (e.Status == "Active" || e.Status == "Approved" || e.Status == "Đang học"));
            if (currentEnrolled >= selectedClass.MaxCapacity)
            {
                TempData["ErrorMessage"] = "Rất tiếc, lớp học đã đủ sĩ số tối đa!";
                return RedirectToAction("StudentDashboard", "Home");
            }

            // 3. Kiểm tra Trùng lịch học (Gọi hàm Helper đã viết)
            string conflictError = CheckStudentScheduleConflict(studentId, selectedClass);
            if (!string.IsNullOrEmpty(conflictError))
            {
                TempData["ErrorMessage"] = conflictError;
                return RedirectToAction("StudentDashboard", "Home");
            }

            // 4. Lưu thông tin Ghi danh (Ép trạng thái Pending)
            var enrollment = new Enrollment
            {
                StudentId = studentId,
                ClassId = classId,
                Status = "Pending",
                EnrollmentDate = DateTime.Now
            };
            db.Enrollments.Add(enrollment);
            db.SaveChanges(); // Lưu trước để có Id cho bảng Payment

            // 5. Tự động sinh Hóa đơn / Công nợ (Nợ 100%)
            if (selectedClass.Course != null)
            {
                string receipt = $"REC-{DateTime.Now:yyyyMMdd}-{enrollment.Id}";
                var newPayment = new Payment
                {
                    EnrollmentId = enrollment.Id,
                    AmountPaid = 0, // Chưa đóng tiền
                    PaymentDate = DateTime.Now,
                    ReceiptNumber = receipt,
                    PaymentMethod = "Bank Transfer", // Giả định học viên sẽ chuyển khoản
                    PaymentStatus = "Pending"
                };
                db.Payments.Add(newPayment);
                db.SaveChanges();
            }

            TempData["SuccessMessage"] = "Ghi danh thành công! Hóa đơn đã được tạo, vui lòng thanh toán để hệ thống duyệt bạn vào lớp.";
            // Điều hướng sang trang xem hóa đơn của học viên
            return RedirectToAction("StudentPayments", "Payments");
        }

        #endregion

        #region 8. HELPER METHODS


        private void PopulateDropdowns(int? selectedStudentId = null, int? selectedClassId = null)
        {
            // Lấy danh sách Học viên
            var students = db.Users
                .Where(u => u.Role.Name.ToLower().Contains("student") || db.StudentProfiles.Any(sp => sp.UserId == u.Id))
                .Select(u => new
                {
                    Id = u.Id,
                    FullNameWithUsername = u.FullName + " (" + u.Username + ")"
                })
                .OrderBy(s => s.FullNameWithUsername)
                .ToList();

            ViewBag.StudentId = new SelectList(students, "Id", "FullNameWithUsername", selectedStudentId);

            // Lấy danh sách Lớp học
            var classes = db.Classes
                .Include(c => c.Course)
                .Select(c => new
                {
                    Id = c.Id,
                    ClassNameWithCourse = c.ClassName + " - " + (c.Course != null ? c.Course.CourseName : "") + " [" + c.Status + "]"
                })
                .OrderBy(c => c.ClassNameWithCourse)
                .ToList();

            ViewBag.ClassId = new SelectList(classes, "Id", "ClassNameWithCourse", selectedClassId);
        }
        // Kiểm tra Trùng lịch học của Học viên
        private string CheckStudentScheduleConflict(int studentId, Class newClass)
        {
            // Nếu lớp mới chưa được thiết lập lịch học đầy đủ, bỏ qua kiểm tra
            if (!newClass.StartDate.HasValue || !newClass.EndDate.HasValue ||
                !newClass.DailyStartTime.HasValue || !newClass.DailyEndTime.HasValue ||
                string.IsNullOrEmpty(newClass.ScheduleDays))
            {
                return null;
            }

            // 1. Lấy danh sách các lớp học viên ĐANG HỌC hoặc ĐÃ DUYỆT
            string[] activeStatuses = { "Active", "Approved", "Studying", "Đang học", "Đã duyệt" };
            var studentActiveClasses = db.Enrollments
                .Include(e => e.Class)
                .Where(e => e.StudentId == studentId && activeStatuses.Contains(e.Status))
                .Select(e => e.Class)
                .ToList();

            var newClassDays = ParseScheduleDaysToDayOfWeeks(newClass.ScheduleDays);

            // 2. Duyệt qua từng lớp để so sánh
            foreach (var existingClass in studentActiveClasses)
            {
                if (!existingClass.StartDate.HasValue || !existingClass.EndDate.HasValue ||
                    !existingClass.DailyStartTime.HasValue || !existingClass.DailyEndTime.HasValue ||
                    string.IsNullOrEmpty(existingClass.ScheduleDays))
                {
                    continue;
                }

                // 2.1. Kiểm tra giao thoa về khoảng thời gian (Ngày bắt đầu - Ngày kết thúc)
                bool datesOverlap = (newClass.StartDate.Value <= existingClass.EndDate.Value) &&
                                    (newClass.EndDate.Value >= existingClass.StartDate.Value);

                if (!datesOverlap) continue; // Không học cùng giai đoạn -> An toàn

                // 2.2. Kiểm tra giao thoa về Thứ trong tuần
                var existingClassDays = ParseScheduleDaysToDayOfWeeks(existingClass.ScheduleDays);
                if (newClassDays.Intersect(existingClassDays).Any())
                {
                    // 2.3. Kiểm tra giao thoa về Khung giờ học
                    bool timesOverlap = (newClass.DailyStartTime.Value < existingClass.DailyEndTime.Value) &&
                                        (newClass.DailyEndTime.Value > existingClass.DailyStartTime.Value);

                    if (timesOverlap)
                    {
                        return $"Học viên đang học lớp '{existingClass.ClassName}' bị trùng lịch vào các thứ [{existingClass.ScheduleDays}] từ {existingClass.DailyStartTime.Value:hh\\:mm} đến {existingClass.DailyEndTime.Value:hh\\:mm}!";
                    }
                }
            }

            return null; // Không trùng lịch
        }
        private List<DayOfWeek> ParseScheduleDaysToDayOfWeeks(string scheduleDays)
        {
            var result = new List<DayOfWeek>();
            if (string.IsNullOrEmpty(scheduleDays)) return result;

            // Không tách bằng khoảng trắng ' ' để tránh lỗi chuỗi "Thứ 2" bị cắt làm đôi
            var tokens = scheduleDays.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                string t = token.ToLower().Replace(" ", "").Trim();
                if (t == "2" || t == "thu2" || t == "thứ2" || t == "t2" || t == "mon" || t == "monday") result.Add(DayOfWeek.Monday);
                else if (t == "3" || t == "thu3" || t == "thứ3" || t == "t3" || t == "tue" || t == "tuesday") result.Add(DayOfWeek.Tuesday);
                else if (t == "4" || t == "thu4" || t == "thứ4" || t == "t4" || t == "wed" || t == "wednesday") result.Add(DayOfWeek.Wednesday);
                else if (t == "5" || t == "thu5" || t == "thứ5" || t == "t5" || t == "thu" || t == "thursday") result.Add(DayOfWeek.Thursday);
                else if (t == "6" || t == "thu6" || t == "thứ6" || t == "t6" || t == "fri" || t == "friday") result.Add(DayOfWeek.Friday);
                else if (t == "7" || t == "thu7" || t == "thứ7" || t == "t7" || t == "sat" || t == "saturday") result.Add(DayOfWeek.Saturday);
                else if (t == "cn" || t == "chunhat" || t == "chủnhật" || t == "sun" || t == "sunday" || t == "8") result.Add(DayOfWeek.Sunday);
            }
            return result.Distinct().ToList();
        }
        //private bool IsAdmin()
        //{
        //    if (Session["UserId"] == null) return false;

        //    string roleName = Session["RoleName"]?.ToString();
        //    if (string.IsNullOrEmpty(roleName)) return false;

        //    return roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        //}
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
