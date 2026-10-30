using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Courses;

namespace English_language_center_management_system.Controllers
{
    public class CoursesController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        private bool IsAdmin()
        {
            if (Session["UserId"] == null) return false;
            string roleName = Session["RoleName"]?.ToString() ?? Session["Role"]?.ToString();
            if (string.IsNullOrEmpty(roleName)) return false;

            return roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }

        #region 1. DANH SÁCH KHÓA HỌC ADMIN (ADMIN INDEX & SORTING)

        // GET: Courses/AdminIndex
        public ActionResult AdminIndex(string searchTerm, string level, string status, string sortBy = "name", string sortOrder = "asc")
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập quản lý khóa học!";
                return RedirectToAction("Login", "Auth");
            }

            var coursesQuery = db.Courses.AsQueryable();

            // 1. Bộ lọc Tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                coursesQuery = coursesQuery.Where(c => c.CourseName.Contains(searchTerm) || c.Description.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(level))
            {
                coursesQuery = coursesQuery.Where(c => c.Level == level);
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Active") coursesQuery = coursesQuery.Where(c => !c.IsHidden);
                else if (status == "Hidden") coursesQuery = coursesQuery.Where(c => c.IsHidden);
            }

            var courseList = coursesQuery.ToList();

            // 2. Mapping và đếm Tổng số lớp & Lượt học viên
            var adminItems = courseList.Select(c => new CourseAdminItemViewModel
            {
                Course = c,
                TotalClassesCount = db.Classes.Count(cls => cls.CourseId == c.Id), // Tổng số lớp
                ActiveClassesCount = db.Classes.Count(cls => cls.CourseId == c.Id && (cls.Status == "Open" || cls.Status == "Studying" || cls.Status == "Mở đăng ký" || cls.Status == "Đang học")),
                TotalStudentsCount = db.Enrollments.Count(e => e.Class.CourseId == c.Id && (e.Status == "Active" || e.Status == "Approved" || e.Status == "Đã duyệt" || e.Status == "Đang học"))
            }).AsQueryable();

            // 3. Nghiệp vụ Sắp xếp theo Lượt học viên, Tổng số lớp, Học phí, Thời lượng
            switch (sortBy.ToLower())
            {
                case "students": // Lượt học viên
                    adminItems = sortOrder == "desc" ? adminItems.OrderByDescending(x => x.TotalStudentsCount) : adminItems.OrderBy(x => x.TotalStudentsCount);
                    break;
                case "classes": // Tổng số lớp
                    adminItems = sortOrder == "desc" ? adminItems.OrderByDescending(x => x.TotalClassesCount) : adminItems.OrderBy(x => x.TotalClassesCount);
                    break;
                case "tuition": // Học phí
                    adminItems = sortOrder == "desc" ? adminItems.OrderByDescending(x => x.Course.TuitionFee) : adminItems.OrderBy(x => x.Course.TuitionFee);
                    break;
                case "duration": // Thời lượng
                    adminItems = sortOrder == "desc" ? adminItems.OrderByDescending(x => x.Course.Duration) : adminItems.OrderBy(x => x.Course.Duration);
                    break;
                case "name":
                default:
                    adminItems = sortOrder == "desc" ? adminItems.OrderByDescending(x => x.Course.CourseName) : adminItems.OrderBy(x => x.Course.CourseName);
                    break;
            }

            var viewModel = new CourseAdminIndexViewModel
            {
                Courses = adminItems.ToList(),
                SearchTerm = searchTerm,
                SelectedLevel = level,
                SelectedStatus = status,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            return View(viewModel);
        }

        #endregion

        #region 2. XEM CHI TIẾT KHÓA HỌC (ADMIN DETAIL)

        // GET: Courses/AdminDetails/5
        public ActionResult AdminDetails(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Course course = db.Courses.Find(id);
            if (course == null) return HttpNotFound();

            // Lấy danh sách các lớp thuộc khóa học kèm Trạng thái thực tế
            var classes = db.Classes
                .Where(c => c.CourseId == id)
                .OrderByDescending(c => c.Id)
                .ToList();

            var classItems = classes.Select(c => new CourseAdminClassItemViewModel
            {
                Class = c,
                EnrolledCount = db.Enrollments.Count(e => e.ClassId == c.Id && (e.Status == "Active" || e.Status == "Approved")),
                ClassRevenue = db.Payments.Where(p => p.Enrollment.ClassId == c.Id && (p.PaymentStatus == "Paid" || p.PaymentStatus == "Completed")).Sum(p => (decimal?)p.AmountPaid) ?? 0
            }).ToList();

            var viewModel = new CourseAdminDetailViewModel
            {
                Course = course,
                TotalClassesCount = classes.Count, // Tổng số lớp
                ActiveClassesCount = classes.Count(c => c.Status == "Open" || c.Status == "Studying"),
                TotalStudentsCount = db.Enrollments.Count(e => e.Class.CourseId == id && (e.Status == "Active" || e.Status == "Approved")),
                TotalRevenueGenerated = classItems.Sum(ci => ci.ClassRevenue),
                Classes = classItems
            };

            return View(viewModel);
        }

        #endregion

        #region 3. THÊM MỚI KHÓA HỌC (CREATE & IMPORT IMAGE & DURATION CALCULATION)

        // GET: Courses/Create
        public ActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            return View(new CourseCreateEditViewModel());
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CourseCreateEditViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            try
            {
                if (ModelState.IsValid)
                {
                    // 1. Tính toán nghiệp vụ Thời lượng học = Số ngày học/tuần * Số giờ/ngày * Số tuần
                    int calculatedDuration = (int)Math.Ceiling(model.SessionsPerWeek * model.HoursPerSession * model.TotalWeeks);

                    // 2. Upload hình ảnh Khóa học
                    string imageUrl = SaveCourseImage(model.ImageFile);

                    var course = new Course
                    {
                        CourseName = model.CourseName,
                        Level = model.Level,
                        Description = model.Description,
                        SessionsPerWeek = model.SessionsPerWeek,
                        HoursPerSession = model.HoursPerSession,
                        TotalWeeks = model.TotalWeeks,
                        Duration = calculatedDuration, // Lưu kết quả thời lượng
                        TuitionFee = model.TuitionFee,
                        Syllabus = model.Syllabus,
                        ImageUrl = imageUrl,
                        IsHidden = model.IsHidden,
                        CreatedAt = DateTime.Now
                    };

                    db.Courses.Add(course);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Thêm mới khóa học thành công!";
                    return RedirectToAction("AdminIndex");
                }

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi thêm mới khóa học: " + ex.Message);
            }

            return View(model);
        }

        #endregion

        #region 4. CẬP NHẬT KHÓA HỌC (EDIT & IMPORT IMAGE)

        // GET: Courses/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Course course = db.Courses.Find(id);
            if (course == null) return HttpNotFound();

            var viewModel = new CourseCreateEditViewModel
            {
                Id = course.Id,
                CourseName = course.CourseName,
                Level = course.Level,
                Description = course.Description,
                SessionsPerWeek = course.SessionsPerWeek,
                HoursPerSession = course.HoursPerSession,
                TotalWeeks = course.TotalWeeks,
                Duration = course.Duration,
                TuitionFee = course.TuitionFee,
                Syllabus = course.Syllabus,
                IsHidden = course.IsHidden,
                ExistingImageUrl = course.ImageUrl
            };

            return View(viewModel);
        }

        // POST: Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CourseCreateEditViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            try
            {
                if (ModelState.IsValid)
                {
                    Course course = db.Courses.Find(model.Id);
                    if (course == null) return HttpNotFound();

                    // 1. Tính lại Thời lượng học
                    int calculatedDuration = (int)Math.Ceiling(model.SessionsPerWeek * model.HoursPerSession * model.TotalWeeks);

                    // 2. Upload ảnh mới nếu được cung cấp
                    string newImageUrl = SaveCourseImage(model.ImageFile);

                    course.CourseName = model.CourseName;
                    course.Level = model.Level;
                    course.Description = model.Description;
                    course.SessionsPerWeek = model.SessionsPerWeek;
                    course.HoursPerSession = model.HoursPerSession;
                    course.TotalWeeks = model.TotalWeeks;
                    course.Duration = calculatedDuration;
                    course.TuitionFee = model.TuitionFee;
                    course.Syllabus = model.Syllabus;
                    course.IsHidden = model.IsHidden;

                    if (!string.IsNullOrEmpty(newImageUrl))
                    {
                        course.ImageUrl = newImageUrl;
                    }

                    db.Entry(course).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Cập nhật thông tin khóa học thành công!";
                    return RedirectToAction("AdminIndex");
                }

            }catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật khóa học: " + ex.Message);
            }

            return View(model);
        }

        #endregion
        // GET: Courses/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Course course = db.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }

            ViewBag.RelatedClassesCount = db.Classes.Count(c => c.CourseId == id);
            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            Course course = db.Courses.Find(id);
            if (course != null)
            {
                // Ràng buộc an toàn: Không cho xóa nếu đã có lớp học thuộc khóa này
                bool hasClasses = db.Classes.Any(c => c.CourseId == id);
                if (hasClasses)
                {
                    TempData["ErrorMessage"] = "Không thể xóa khóa học đã có danh sách lớp học liên kết!";
                    return RedirectToAction("AdminIndex");
                }

                db.Courses.Remove(course);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa khóa học thành công!";
            }

            return RedirectToAction("AdminIndex");
        }


        #region 6 Course Public Catalog and Details for Students
        public ActionResult Index(string searchTerm, string level)
        {
            var coursesQuery = db.Courses.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                coursesQuery = coursesQuery.Where(c => c.CourseName.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(level))
            {
                coursesQuery = coursesQuery.Where(c => c.Level == level);
            }

            var viewModel = new CourseCatalogViewModel
            {
                Courses = coursesQuery.OrderBy(c => c.CourseName).ToList(),
                SearchTerm = searchTerm,
                SelectedLevel = level
            };

            return View(viewModel);
        }

        // GET: Courses/Details/5 (Trang chi tiết khóa học)
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Course course = db.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }

            var availableClasses = db.Classes
                .Where(c => c.CourseId == id && c.Status == "Open")
                .Include(c => c.Teacher)
                .OrderBy(c => c.ClassName)
                .ToList();

            var viewModel = new CourseDetailViewModel
            {
                Course = course,
                AvailableClasses = availableClasses
            };

            return View(viewModel);
        }

        #endregion

        #region 5. HELPER METHODS

        private string SaveCourseImage(HttpPostedFileBase imageFile)
        {
            if (imageFile != null && imageFile.ContentLength > 0)
            {
                string extension = Path.GetExtension(imageFile.FileName);
                string fileName = "course_" + Guid.NewGuid().ToString("N") + extension;
                string folderPath = Server.MapPath("~/Uploads/Courses/");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string filePath = Path.Combine(folderPath, fileName);
                imageFile.SaveAs(filePath);
                return "/Uploads/Courses/" + fileName;
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        #endregion
    }
}