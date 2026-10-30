using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Classes;

namespace English_language_center_management_system.Controllers
{
    public class ClassesController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        #region 1. DANH SÁCH LỚP HỌC (INDEX)

        // GET: Classes
        public ActionResult Index(string searchTerm, int? courseId, string status)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào quản lý lớp học!";
                return RedirectToAction("Login", "Auth");
            }

            var classesQuery = db.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .Include(c => c.Room)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                classesQuery = classesQuery.Where(c =>
                    c.ClassName.Contains(searchTerm) ||
                    (c.Room != null && c.Room.RoomName.Contains(searchTerm)));
            }

            if (courseId.HasValue && courseId.Value > 0)
            {
                classesQuery = classesQuery.Where(c => c.CourseId == courseId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                classesQuery = classesQuery.Where(c => c.Status == status);
            }

            var classList = classesQuery.OrderByDescending(c => c.Id).ToList();

            var classIndexItems = classList.Select(c => {
                if (!db.ClassSessions.Any(s => s.ClassId == c.Id) && c.StartDate.HasValue && !string.IsNullOrWhiteSpace(c.ScheduleDays))
                {
                    GenerateSessionsForClass(c, c.Course);
                }

                int enrolledCount = db.Enrollments.Count(e => e.ClassId == c.Id && (e.Status == "Active" || e.Status == "Approved"));
              

                string calculatedStatus = CalculateDynamicClassStatus(c, enrolledCount);
                if (c.Status != calculatedStatus)
                {
                    c.Status = calculatedStatus;
                    db.Entry(c).State = EntityState.Modified;
                }

                return new ClassIndexItemViewModel
                {
                    Class = c,
                    EnrolledCount = enrolledCount
                };
            }).ToList();

            db.SaveChanges();

            var viewModel = new ClassIndexViewModel
            {
                Classes = classIndexItems,
                SearchTerm = searchTerm,
                SelectedCourseId = courseId,
                SelectedStatus = status,
                Courses = db.Courses.OrderBy(co => co.CourseName).ToList()
            };

            return View(viewModel);
        }

        #endregion

        #region 2. XEM CHI TIẾT LỚP HỌC (DETAILS)

        // GET: Classes/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Class @class = db.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .Include(c => c.Room)
                .FirstOrDefault(c => c.Id == id);

            if (@class == null) return HttpNotFound();

            ViewBag.Enrollments = db.Enrollments
                .Include(e => e.Student)
                .Where(e => e.ClassId == id)
                .ToList();

            return View(@class);
        }

        #endregion

        #region 3. MỞ LỚP HỌC MỚI (CREATE)

        // GET: Classes/Create
        public ActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            PopulateDropdowns();
            return View(new ClassCreateViewModel());
        }

        // POST: Classes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ClassCreateViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var course = db.Courses.Find(model.CourseId);
            if (course == null)
            {
                ModelState.AddModelError("CourseId", "Khóa học được chọn không tồn tại.");
            }
            else
            {
                // 1. NGHIỆP VỤ 1: Kiểm tra Số ngày học trong 1 tuần = SessionsPerWeek của Khóa học
                var selectedDaysList = ParseScheduleDaysToDayOfWeeks(model.ScheduleDays);
                if (selectedDaysList.Count != course.SessionsPerWeek)
                {
                    ModelState.AddModelError("ScheduleDays", $"Khóa học '{course.CourseName}' quy định học đúng {course.SessionsPerWeek} buổi/tuần. Bạn đã chọn {selectedDaysList.Count} ngày!");
                }

                // 2. NGHIỆP VỤ 2: Tự động tính Giờ kết thúc theo DailyStartTime + HoursPerSession của Khóa học
                TimeSpan? startTime = ParseTimeSpan(model.DailyStartTimeStr);
                TimeSpan? endTime = CalculateClassEndTime(startTime, course.HoursPerSession);
                if (startTime.HasValue && (startTime.Value < new TimeSpan(8, 0, 0) || startTime.Value > new TimeSpan(21, 0, 0)))
                {
                    ModelState.AddModelError("DailyStartTimeStr", "Giờ bắt đầu phải nằm trong khoảng từ 08:00 đến 21:00.");
                }

                if (endTime.HasValue && endTime.Value > new TimeSpan(21, 0, 0))
                {
                    ModelState.AddModelError("DailyStartTimeStr", $"Ca học kéo dài {course.HoursPerSession} giờ sẽ kết thúc lúc {endTime.Value:hh\\:mm} (vượt quá giới hạn 21:00). Vui lòng chọn giờ bắt đầu sớm hơn.");
                }

                // 3. NGHIỆP VỤ 3: Tự động tính Ngày kết thúc học theo StartDate + ScheduleDays + TotalWeeks của Khóa học
                DateTime? calculatedEndDate = CalculateClassEndDate(model.StartDate, model.ScheduleDays, course.TotalWeeks, course.SessionsPerWeek);

                // 4. Lấy MaxCapacity từ Sức chứa của Phòng học
                int roomCapacity = 30;
                if (model.RoomId.HasValue)
                {
                    var room = db.Rooms.Find(model.RoomId.Value);
                    if (room != null) roomCapacity = room.Capacity;
                }
                //Kiểm tra thời gian bắt đầu đăng ký phải nhỏ hơn thời gian kết thúc đăng ký
                if (model.RegistrationStartDate.HasValue && model.RegistrationEndDate.HasValue)
                {
                    if (model.RegistrationStartDate.Value >= model.RegistrationEndDate.Value)
                    {
                        ModelState.AddModelError("RegistrationEndDate", "Thời gian bắt đầu đăng ký phải nhỏ hơn thời gian kết thúc đăng ký.");
                    }
                }

                // 5. Kiểm tra Trùng phòng học
                if (model.RoomId.HasValue && startTime.HasValue && endTime.HasValue && model.StartDate.HasValue && calculatedEndDate.HasValue)
                {
                    string conflictError = CheckRoomConflict(
                        classId: 0,
                        roomId: model.RoomId.Value,
                        scheduleDays: model.ScheduleDays,
                        startTime: startTime,
                        endTime: endTime,
                        startDate: model.StartDate,
                        endDate: calculatedEndDate
                    );

                    if (!string.IsNullOrEmpty(conflictError))
                    {
                        ModelState.AddModelError("RoomId", conflictError);
                    }
                }
                // 5. THÊM MỚI: Kiểm tra Trùng lịch Giảng viên
                if (model.TeacherId.HasValue && startTime.HasValue && endTime.HasValue && model.StartDate.HasValue && calculatedEndDate.HasValue)
                {
                    string teacherConflictError = CheckTeacherConflict(
                        classId: model.Id,
                        teacherId: model.TeacherId.Value,
                        scheduleDays: model.ScheduleDays,
                        startTime: startTime,
                        endTime: endTime,
                        startDate: model.StartDate,
                        endDate: calculatedEndDate
                    );

                    if (!string.IsNullOrEmpty(teacherConflictError))
                    {
                        ModelState.AddModelError("TeacherId", teacherConflictError);
                    }
                }

                if (ModelState.IsValid)
                {
                    var @class = new Class
                    {
                        CourseId = model.CourseId,
                        TeacherId = model.TeacherId,
                        ClassName = model.ClassName,
                        RoomId = model.RoomId,
                        MaxCapacity = roomCapacity,
                        ScheduleDays = model.ScheduleDays,
                        DailyStartTime = startTime,
                        DailyEndTime = endTime,
                        WeeklySessionsCount = course.SessionsPerWeek,
                        ScheduleDescription = model.ScheduleDescription ?? $"{model.ScheduleDays} ({startTime:hh\\:mm} - {endTime:hh\\:mm})",
                        StartDate = model.StartDate,
                        EndDate = calculatedEndDate, // Tự động lưu Ngày kết thúc vừa tính
                        RegistrationStartDate = model.RegistrationStartDate,
                        RegistrationEndDate = model.RegistrationEndDate,
                        Status = model.Status ?? "Mở đăng ký"
                    };

                    db.Classes.Add(@class);
                    db.SaveChanges(); // Lưu Class trước để lấy @class.Id

                  
                    int generatedCount = GenerateSessionsForClass(@class, course);

                    TempData["SuccessMessage"] = "Mở lớp học và tự động tạo thời khóa biểu thành công!";
                    return RedirectToAction("Index");
                }
            }

            PopulateDropdowns(model.CourseId, model.TeacherId, model.RoomId);
            return View(model);
        }

        #endregion

        #region 4. CẬP NHẬT LỚP HỌC (EDIT)

        // GET: Classes/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Class @class = db.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .FirstOrDefault(c => c.Id == id);

            if (@class == null) return HttpNotFound();

            var viewModel = new ClassEditViewModel
            {
                Id = @class.Id,
                CourseId = @class.CourseId,
                TeacherId = @class.TeacherId,
                ClassName = @class.ClassName,
                RoomId = @class.RoomId,
                MaxCapacity = @class.MaxCapacity,
                ScheduleDays = @class.ScheduleDays,
                DailyStartTimeStr = @class.DailyStartTime?.ToString(@"hh\:mm"),
                DailyEndTimeStr = @class.DailyEndTime?.ToString(@"hh\:mm"),
                WeeklySessionsCount = @class.WeeklySessionsCount,
                ScheduleDescription = @class.ScheduleDescription,
                StartDate = @class.StartDate,
                EndDate = @class.EndDate,
                RegistrationStartDate = @class.RegistrationStartDate,
                RegistrationEndDate = @class.RegistrationEndDate,
                Status = @class.Status
            };

            PopulateDropdowns(@class.CourseId, @class.TeacherId, @class.RoomId);
            return View(viewModel);
        }

        // POST: Classes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ClassEditViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            var course = db.Courses.Find(model.CourseId);
            if (course == null)
            {
                ModelState.AddModelError("CourseId", "Khóa học được chọn không tồn tại.");
            }
            else
            {
                // 1. Kiểm tra Số ngày học
                var selectedDaysList = ParseScheduleDaysToDayOfWeeks(model.ScheduleDays);
                if (selectedDaysList.Count != course.SessionsPerWeek)
                {
                    ModelState.AddModelError("ScheduleDays", $"Khóa học '{course.CourseName}' quy định học đúng {course.SessionsPerWeek} buổi/tuần. Bạn đã chọn {selectedDaysList.Count} ngày!");
                }

                // 2. Tính lại Giờ kết thúc & Ngày kết thúc
                TimeSpan? startTime = ParseTimeSpan(model.DailyStartTimeStr);
                TimeSpan? endTime = CalculateClassEndTime(startTime, course.HoursPerSession);
                DateTime? calculatedEndDate = CalculateClassEndDate(model.StartDate, model.ScheduleDays, course.TotalWeeks, course.SessionsPerWeek);
                if (startTime.HasValue && (startTime.Value < new TimeSpan(8, 0, 0) || startTime.Value > new TimeSpan(21, 0, 0)))
                {
                    ModelState.AddModelError("DailyStartTimeStr", "Giờ bắt đầu phải nằm trong khoảng từ 08:00 đến 21:00.");
                }

                if (endTime.HasValue && endTime.Value > new TimeSpan(21, 0, 0))
                {
                    ModelState.AddModelError("DailyStartTimeStr", $"Ca học kéo dài {course.HoursPerSession} giờ sẽ kết thúc lúc {endTime.Value:hh\\:mm} (vượt quá giới hạn 21:00). Vui lòng chọn giờ bắt đầu sớm hơn.");
                }

                // 3. Lấy Capacity mới nếu thay đổi Phòng học
                int roomCapacity = model.MaxCapacity;
                if (model.RoomId.HasValue)
                {
                    var room = db.Rooms.Find(model.RoomId.Value);
                    if (room != null) roomCapacity = room.Capacity;
                }

                // Thêm đoạn này trước khi gọi ModelState.IsValid
                if (!string.IsNullOrEmpty(model.ClassName) && db.Classes.Any(c => c.ClassName == model.ClassName && c.Id != model.Id))
                {
                    ModelState.AddModelError("ClassName", "Tên lớp học này đã được sử dụng.");
                }

                //Kiểm tra thời gian bắt đầu đăng ký phải nhỏ hơn thời gian kết thúc đăng ký
                if (model.RegistrationStartDate.HasValue && model.RegistrationEndDate.HasValue)
                {
                    if (model.RegistrationStartDate.Value >= model.RegistrationEndDate.Value)
                    {
                        ModelState.AddModelError("RegistrationEndDate", "Thời gian bắt đầu đăng ký phải nhỏ hơn thời gian kết thúc đăng ký.");
                    }
                }
                // 4. Kiểm tra Trùng phòng
                if (model.RoomId.HasValue && startTime.HasValue && endTime.HasValue && model.StartDate.HasValue && calculatedEndDate.HasValue)
                {
                    string conflictError = CheckRoomConflict(
                        classId: model.Id,
                        roomId: model.RoomId.Value,
                        scheduleDays: model.ScheduleDays,
                        startTime: startTime,
                        endTime: endTime,
                        startDate: model.StartDate,
                        endDate: calculatedEndDate
                    );

                    if (!string.IsNullOrEmpty(conflictError))
                    {
                        ModelState.AddModelError("RoomId", conflictError);
                    }
                }

                // 5. THÊM MỚI: Kiểm tra Trùng lịch Giảng viên
                if (model.TeacherId.HasValue && startTime.HasValue && endTime.HasValue && model.StartDate.HasValue && calculatedEndDate.HasValue)
                {
                    string teacherConflictError = CheckTeacherConflict(
                        classId: model.Id,
                        teacherId: model.TeacherId.Value,
                        scheduleDays: model.ScheduleDays,
                        startTime: startTime,
                        endTime: endTime,
                        startDate: model.StartDate,
                        endDate: calculatedEndDate
                    );

                    if (!string.IsNullOrEmpty(teacherConflictError))
                    {
                        ModelState.AddModelError("TeacherId", teacherConflictError);
                    }
                }

                if (ModelState.IsValid)
                {
                    Class @class = db.Classes.Find(model.Id);
                    if (@class == null) return HttpNotFound();

                    @class.CourseId = model.CourseId;
                    @class.TeacherId = model.TeacherId;
                    @class.ClassName = model.ClassName;
                    @class.RoomId = model.RoomId;
                    @class.MaxCapacity = roomCapacity;
                    @class.ScheduleDays = model.ScheduleDays;
                    @class.DailyStartTime = startTime;
                    @class.DailyEndTime = endTime;
                    @class.WeeklySessionsCount = course.SessionsPerWeek;
                    @class.ScheduleDescription = model.ScheduleDescription ?? $"{model.ScheduleDays} ({startTime:hh\\:mm} - {endTime:hh\\:mm})";
                    @class.StartDate = model.StartDate;
                    @class.EndDate = calculatedEndDate;
                    @class.RegistrationStartDate = model.RegistrationStartDate;
                    @class.RegistrationEndDate = model.RegistrationEndDate;
                    @class.Status = model.Status;

                    db.Entry(@class).State = EntityState.Modified;
                   
                    var existingSessions = db.ClassSessions.Where(s => s.ClassId == @class.Id).ToList();
                    foreach (var session in existingSessions)
                    {
                        if (startTime.HasValue) session.StartTime = startTime.Value;
                        if (endTime.HasValue) session.EndTime = endTime.Value;

                        // Nếu Admin có đổi phòng học, cập nhật luôn phòng cho từng buổi
                        if (model.RoomId.HasValue) session.RoomId = model.RoomId;

                        db.Entry(session).State = EntityState.Modified;
                    }
                    db.SaveChanges();

                    int generatedCount = GenerateSessionsForClass(@class, course);

                    TempData["SuccessMessage"] = generatedCount > 0
                        ? $"Cập nhật lớp học và đã bổ sung {generatedCount} buổi học vào Thời khóa biểu!"
                        : "Cập nhật thông tin lớp học thành công!";

                    return RedirectToAction("Index");
                }
            }

            PopulateDropdowns(model.CourseId, model.TeacherId, model.RoomId);
            return View(model);
        }

        #endregion

        #region 5. XÓA LỚP HỌC (DELETE)

        // GET: Classes/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Class @class = db.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .Include(c => c.Room)
                .FirstOrDefault(c => c.Id == id);

            if (@class == null) return HttpNotFound();

            return View(@class);
        }

        // POST: Classes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            Class @class = db.Classes.Find(id);
            if (@class != null)
            {
                bool hasEnrollments = db.Enrollments.Any(e => e.ClassId == id);
                if (hasEnrollments)
                {
                    TempData["ErrorMessage"] = "Không thể xóa lớp học đã có học viên ghi danh!";
                    return RedirectToAction("Index");
                }

                db.Classes.Remove(@class);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Xóa lớp học thành công!";
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region 6. ENDPOINT API AJAX CHO FRONTEND (LẤY THÔNG TIN KHÓA HỌC & PHÒNG HỌC)

        // API trả về thông tin Khóa học để JS tự động tính ở View
        [HttpGet]
        public ActionResult GetCourseInfo(int courseId)
        {
            var course = db.Courses.Find(courseId);
            if (course == null) return HttpNotFound();

            return Json(new
            {
                sessionsPerWeek = course.SessionsPerWeek,
                hoursPerSession = course.HoursPerSession,
                totalWeeks = course.TotalWeeks,
                tuitionFee = course.TuitionFee
            }, JsonRequestBehavior.AllowGet);
        }

        // API trả về Capacity của Phòng học
        [HttpGet]
        public ActionResult GetRoomInfo(int roomId)
        {
            var room = db.Rooms.Find(roomId);
            if (room == null) return HttpNotFound();

            return Json(new
            {
                capacity = room.Capacity,
                roomName = room.RoomName
            }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region 7. HELPER COMPUTATION LOGIC (TÍNH GIỜ/NGÀY & CHỐNG TRÙNG)

        // Tự động tính Giờ kết thúc theo Giờ bắt đầu + HoursPerSession
        private TimeSpan? CalculateClassEndTime(TimeSpan? startTime, double hoursPerSession)
        {
            if (!startTime.HasValue || hoursPerSession <= 0) return null;
            return startTime.Value.Add(TimeSpan.FromHours(hoursPerSession));
        }

        // Tự động tính Ngày kết thúc theo StartDate, Thứ học và TotalWeeks
        private DateTime? CalculateClassEndDate(DateTime? startDate, string scheduleDays, int totalWeeks, int sessionsPerWeek)
        {
            if (!startDate.HasValue || string.IsNullOrWhiteSpace(scheduleDays) || totalWeeks <= 0 || sessionsPerWeek <= 0)
                return null;

            var targetDayOfWeeks = ParseScheduleDaysToDayOfWeeks(scheduleDays);
            if (!targetDayOfWeeks.Any()) return null;

            int totalSessionsNeeded = totalWeeks * sessionsPerWeek;
            int currentSessionsCount = 0;
            DateTime currentDate = startDate.Value;

            int safetyCounter = 0;
            while (currentSessionsCount < totalSessionsNeeded && safetyCounter < 500)
            {
                if (targetDayOfWeeks.Contains(currentDate.DayOfWeek))
                {
                    currentSessionsCount++;
                    if (currentSessionsCount == totalSessionsNeeded)
                    {
                        return currentDate;
                    }
                }
                currentDate = currentDate.AddDays(1);
                safetyCounter++;
            }

            return currentDate;
        }

        // Chuyển đổi chuỗi thứ học (2,4,6) thành danh sách DayOfWeek C#
        // SỬA LẠI: Không cắt bằng dấu cách ' ' trước để giữ nguyên cụm "thứ 2", "chủ nhật"
        private List<DayOfWeek> ParseScheduleDaysToDayOfWeeks(string scheduleDays)
        {
            var result = new List<DayOfWeek>();
            if (string.IsNullOrWhiteSpace(scheduleDays)) return result;

            // Chuẩn hóa chuỗi về chữ thường và thay thế các cụm tiếng Việt thành số trước khi cắt
            string normalized = scheduleDays.ToLower()
                .Replace("chủ nhật", "cn")
                .Replace("chu nhat", "cn")
                .Replace("thứ ", "t")
                .Replace("thu ", "t");

            var tokens = normalized.Split(new[] { ',', ';', ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                string t = token.Trim();
                if (t == "2" || t == "thu2" || t == "t2" || t == "mon" || t == "monday")
                    result.Add(DayOfWeek.Monday);
                else if (t == "3" || t == "thu3" || t == "t3" || t == "tue" || t == "tuesday")
                    result.Add(DayOfWeek.Tuesday);
                else if (t == "4" || t == "thu4" || t == "t4" || t == "wed" || t == "wednesday")
                    result.Add(DayOfWeek.Wednesday);
                else if (t == "5" || t == "thu5" || t == "t5" || t == "thu" || t == "thursday")
                    result.Add(DayOfWeek.Thursday);
                else if (t == "6" || t == "thu6" || t == "t6" || t == "fri" || t == "friday")
                    result.Add(DayOfWeek.Friday);
                else if (t == "7" || t == "thu7" || t == "t7" || t == "sat" || t == "saturday")
                    result.Add(DayOfWeek.Saturday);
                else if (t == "cn" || t == "chunhat" || t == "sun" || t == "sunday" || t == "8")
                    result.Add(DayOfWeek.Sunday);
            }
            return result.Distinct().ToList();
        }

        // THÊM HÀM MỚI NÀY: Tự động sinh danh sách buổi học (ClassSession) cho 1 lớp học
        private int GenerateSessionsForClass(Class cls, Course course)
        {
            if (cls == null || !cls.StartDate.HasValue || string.IsNullOrWhiteSpace(cls.ScheduleDays))
                return 0;

            var daysOfWeek = ParseScheduleDaysToDayOfWeeks(cls.ScheduleDays);
            if (!daysOfWeek.Any()) return 0;

            // Đảm bảo luôn có Giờ bắt đầu và Giờ kết thúc (mặc định 08:00 - 09:30 nếu bị trống)
            TimeSpan startTime = cls.DailyStartTime ?? new TimeSpan(8, 0, 0);
            double hoursPerSession = (course != null && course.HoursPerSession > 0) ? course.HoursPerSession : 1.5;
            TimeSpan endTime = cls.DailyEndTime ?? startTime.Add(TimeSpan.FromHours(hoursPerSession));

            // Đảm bảo luôn có Ngày kết thúc (nếu bị trống thì tự tính theo TotalWeeks hoặc mặc định 8 tuần)
            int totalWeeks = (course != null && course.TotalWeeks > 0) ? course.TotalWeeks : 8;
            int sessionsPerWeek = (course != null && course.SessionsPerWeek > 0) ? course.SessionsPerWeek : daysOfWeek.Count;
            DateTime endDate = cls.EndDate ?? CalculateClassEndDate(cls.StartDate, cls.ScheduleDays, totalWeeks, sessionsPerWeek) ?? cls.StartDate.Value.AddDays(totalWeeks * 7);

            // Cập nhật ngược lại vào Class nếu lúc trước bị null
            cls.DailyStartTime = startTime;
            cls.DailyEndTime = endTime;
            cls.EndDate = endDate;

            int createdCount = 0;
            for (DateTime date = cls.StartDate.Value.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (daysOfWeek.Contains(date.DayOfWeek))
                {
                    bool exists = db.ClassSessions.Any(s => s.ClassId == cls.Id && DbFunctions.TruncateTime(s.SessionDate) == date);
                    if (!exists)
                    {
                        db.ClassSessions.Add(new ClassSession
                        {
                            ClassId = cls.Id,
                            SessionDate = date,
                            StartTime = startTime,
                            EndTime = endTime,
                            RoomId = cls.RoomId
                        });
                        createdCount++;
                    }
                }
            }

            if (createdCount > 0)
            {
                db.SaveChanges();
            }
            return createdCount;
        }

        // Kiểm tra Trùng phòng học
        private string CheckRoomConflict(int classId, int roomId, string scheduleDays, TimeSpan? startTime, TimeSpan? endTime, DateTime? startDate, DateTime? endDate)
        {
            if (!startTime.HasValue || !endTime.HasValue || !startDate.HasValue || !endDate.HasValue || string.IsNullOrEmpty(scheduleDays))
            {
                return null;
            }

            var otherClasses = db.Classes
                .Include(c => c.Room)
                .Where(c => c.Id != classId && c.RoomId == roomId)
                .ToList();

            foreach (var existingClass in otherClasses)
            {
                if (existingClass.StartDate.HasValue && existingClass.EndDate.HasValue)
                {
                    bool datesOverlap = (startDate.Value <= existingClass.EndDate.Value) && (endDate.Value >= existingClass.StartDate.Value);
                    if (!datesOverlap) continue;
                }

                if (!string.IsNullOrEmpty(existingClass.ScheduleDays) && AreDaysOverlapping(scheduleDays, existingClass.ScheduleDays))
                {
                    if (existingClass.DailyStartTime.HasValue && existingClass.DailyEndTime.HasValue)
                    {
                        bool timesOverlap = (startTime.Value < existingClass.DailyEndTime.Value) && (endTime.Value > existingClass.DailyStartTime.Value);
                        if (timesOverlap)
                        {
                            string roomNameDisplay = existingClass.Room != null ? existingClass.Room.RoomName : "Phòng học này";
                            return $"{roomNameDisplay} đã bị trùng lịch với lớp '{existingClass.ClassName}' vào các thứ [{existingClass.ScheduleDays}] từ {existingClass.DailyStartTime.Value:hh\\:mm} đến {existingClass.DailyEndTime.Value:hh\\:mm}!";
                        }
                    }
                }
            }

            return null;
        }
        // Kiểm tra Trùng lịch Giảng viên
        private string CheckTeacherConflict(int classId, int teacherId, string scheduleDays, TimeSpan? startTime, TimeSpan? endTime, DateTime? startDate, DateTime? endDate)
        {
            if (!startTime.HasValue || !endTime.HasValue || !startDate.HasValue || !endDate.HasValue || string.IsNullOrEmpty(scheduleDays))
            {
                return null;
            }

            var otherClasses = db.Classes
                .Include(c => c.Teacher)
                .Where(c => c.Id != classId && c.TeacherId == teacherId)
                .ToList();

            foreach (var existingClass in otherClasses)
            {
                // 1. Kiểm tra có giao nhau về giai đoạn ngày học (StartDate - EndDate) không
                if (existingClass.StartDate.HasValue && existingClass.EndDate.HasValue)
                {
                    bool datesOverlap = (startDate.Value <= existingClass.EndDate.Value) && (endDate.Value >= existingClass.StartDate.Value);
                    if (!datesOverlap) continue;
                }

                // 2. Kiểm tra có giao nhau về Thứ học không
                if (!string.IsNullOrEmpty(existingClass.ScheduleDays) && AreDaysOverlapping(scheduleDays, existingClass.ScheduleDays))
                {
                    // 3. Kiểm tra có giao nhau về Khung giờ học không
                    if (existingClass.DailyStartTime.HasValue && existingClass.DailyEndTime.HasValue)
                    {
                        bool timesOverlap = (startTime.Value < existingClass.DailyEndTime.Value) && (endTime.Value > existingClass.DailyStartTime.Value);
                        if (timesOverlap)
                        {
                            string teacherName = existingClass.Teacher != null ? existingClass.Teacher.FullName : "Giảng viên này";
                            return $"Giảng viên đã bị trùng lịch dạy với lớp '{existingClass.ClassName}' vào các thứ [{existingClass.ScheduleDays}] từ {existingClass.DailyStartTime.Value:hh\\:mm} đến {existingClass.DailyEndTime.Value:hh\\:mm}!";
                        }
                    }
                }
            }

            return null;
        }

        private bool AreDaysOverlapping(string days1, string days2)
        {
            var daysList1 = ParseScheduleDaysToDayOfWeeks(days1);
            var daysList2 = ParseScheduleDaysToDayOfWeeks(days2);
            return daysList1.Intersect(daysList2).Any();
        }

        // Tự động cập nhật Trạng thái lớp học
        private string CalculateDynamicClassStatus(Class c, int enrolledCount)
        {
            DateTime now = DateTime.Now;
            int maxCap = c.Room != null ? c.Room.Capacity : c.MaxCapacity;

            if (enrolledCount >= maxCap && now <= c.StartDate.Value)
            {
                return "Full";
            }

            if (c.EndDate.HasValue && now > c.EndDate.Value)
            {
                return "Completed";
            }

            double capacityRate = maxCap > 0 ? (double)enrolledCount / maxCap : 0;
            if (c.StartDate.HasValue && now >= c.StartDate.Value && (capacityRate >= 0.20 || enrolledCount >= maxCap))
            {
                return "Studying";
            }

            if (c.RegistrationStartDate.HasValue && c.RegistrationEndDate.HasValue)
            {
                if (now >= c.RegistrationStartDate.Value && now <= c.RegistrationEndDate.Value)
                {
                    return "Open";
                }
                else if (now > c.RegistrationEndDate.Value && capacityRate < 0.20)
                {
                    return "Closed";
                }
            }

            return c.Status ?? "Mở đăng ký";
        }

        private TimeSpan? ParseTimeSpan(string timeStr)
        {
            if (TimeSpan.TryParse(timeStr, out TimeSpan ts))
            {
                return ts;
            }
            return null;
        }

        private void PopulateDropdowns(int? selectedCourseId = null, int? selectedTeacherId = null, int? selectedRoomId = null)
        {
            ViewBag.CourseId = new SelectList(db.Courses.OrderBy(c => c.CourseName), "Id", "CourseName", selectedCourseId);

            var teachers = db.Users
                .Where(u => u.Role.Name.ToLower().Contains("teacher") || db.TeacherProfiles.Any(tp => tp.UserId == u.Id))
                .Select(u => new
                {
                    Id = u.Id,
                    FullNameWithUsername = u.FullName + " (" + u.Username + ")"
                })
                .OrderBy(t => t.FullNameWithUsername)
                .ToList();

            ViewBag.TeacherId = new SelectList(teachers, "Id", "FullNameWithUsername", selectedTeacherId);

            var rooms = db.Rooms.Where(r => r.IsActive).OrderBy(r => r.RoomName).ToList();
            ViewBag.RoomId = new SelectList(rooms, "Id", "RoomName", selectedRoomId);
        }

        private bool IsAdmin()
        {
            if (Session["UserId"] == null) return false;
            string roleName = Session["RoleName"]?.ToString() ?? Session["Role"]?.ToString();
            if (string.IsNullOrEmpty(roleName)) return false;

            return roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose(); base.Dispose(disposing);
        }

        #endregion
    }
}