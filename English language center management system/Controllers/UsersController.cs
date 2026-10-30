using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Users;

namespace English_language_center_management_system.Controllers
{
    public class UsersController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        #region 1. DANH SÁCH NGƯỜI DÙNG & THỐNG KÊ (INDEX & SORTING)

        // GET: Users
        public ActionResult Index(string searchTerm, int? roleId, string sortBy = "name", string sortOrder = "asc")
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            // 1. Thống kê số lượng người dùng theo vai trò
            int totalStudents = db.Users.Count(u => u.Role.Name.Contains("Student") || u.Role.Name.Contains("Học viên"));
            int totalTeachers = db.Users.Count(u => u.Role.Name.Contains("Teacher") || u.Role.Name.Contains("Giáo viên"));
            int totalAdmins = db.Users.Count(u => u.Role.Name.Contains("Admin") || u.Role.Name.Contains("Administrator") || u.Role.Name.Contains("Quản trị"));

            // 2. Query danh sách người dùng
            var usersQuery = db.Users
                .Include(u => u.Role)
                .Include(u => u.StudentProfile)
                .Include(u => u.TeacherProfile)
                .AsQueryable();

            // Tìm kiếm theo Username, FullName, Email hoặc Phone
            if (!string.IsNullOrEmpty(searchTerm))
            {
                usersQuery = usersQuery.Where(u =>
                    u.Username.Contains(searchTerm) ||
                    u.FullName.Contains(searchTerm) ||
                    u.Email.Contains(searchTerm) ||
                    u.Phone.Contains(searchTerm));
            }

            // Lọc theo Vai trò (RoleId)
            if (roleId.HasValue && roleId.Value > 0)
            {
                usersQuery = usersQuery.Where(u => u.RoleId == roleId.Value);
            }

            // 3. Sắp xếp theo ID, Tên hoặc Ngày tạo
            switch (sortBy.ToLower())
            {
                case "id":
                    usersQuery = sortOrder == "desc" ? usersQuery.OrderByDescending(u => u.Id) : usersQuery.OrderBy(u => u.Id);
                    break;
                case "created_at":
                case "date":
                    usersQuery = sortOrder == "desc" ? usersQuery.OrderByDescending(u => u.CreatedAt) : usersQuery.OrderBy(u => u.CreatedAt);
                    break;
                case "name":
                default:
                    usersQuery = sortOrder == "desc" ? usersQuery.OrderByDescending(u => u.FullName) : usersQuery.OrderBy(u => u.FullName);
                    break;
            }

            var viewModel = new UserIndexViewModel
            {
                Users = usersQuery.ToList(),
                SearchTerm = searchTerm,
                SelectedRoleId = roleId,
                Roles = db.Roles.ToList(),
                SortBy = sortBy,
                SortOrder = sortOrder,
                TotalStudents = totalStudents,
                TotalTeachers = totalTeachers,
                TotalAdmins = totalAdmins
            };

            return View(viewModel);
        }

        #endregion

        #region 2. XEM CHI TIẾT (DETAILS)

        // GET: Users/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            User user = db.Users
                .Include(u => u.Role)
                .Include(u => u.StudentProfile)
                .Include(u => u.TeacherProfile)
                .FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
        }

        #endregion

        #region 3. THÊM MỚI NGƯỜI DÙNG (CREATE)

        // GET: Users/Create
        public ActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            ViewBag.RoleId = new SelectList(db.Roles, "Id", "Name");
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UserCreateViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                // Kiểm tra trùng Username / Email
                if (db.Users.Any(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
                }
                if (db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                }
                if (db.Users.Any(u => u.Phone == model.Phone))
                {
                    ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng.");
                }

                if (ModelState.IsValid)
                {
                    // 1. Tạo đối tượng User
                    var user = new User
                    {
                        Username = model.Username,
                        PasswordHash = HashPassword(model.Password),
                        FullName = model.FullName,
                        Email = model.Email,
                        Phone = model.Phone,
                        RoleId = model.RoleId,
                        CreatedAt = DateTime.Now
                    };

                    db.Users.Add(user);
                    db.SaveChanges(); // Lưu để sinh user.Id

                    // 2. Upload Avatar nếu có
                    string avatarUrl = SaveUploadedAvatar(model.AvatarFile);

                    // 3. Khởi tạo Hồ sơ theo Vai trò
                    var role = db.Roles.Find(model.RoleId);
                    if (role != null)
                    {
                        string roleName = role.Name.ToLower();
                        if (roleName.Contains("teacher") || roleName.Contains("giáo viên"))
                        {
                            var teacherProfile = new TeacherProfile
                            {
                                UserId = user.Id,
                                Specialization = model.Specialization,
                                AvatarUrl = avatarUrl
                            };
                            db.TeacherProfiles.Add(teacherProfile);
                        }
                        else if (roleName.Contains("student") || roleName.Contains("học viên"))
                        {
                            var studentProfile = new StudentProfile
                            {
                                UserId = user.Id,
                                Address = model.Address,
                                DateOfBirth = model.DateOfBirth,
                                AvatarUrl = avatarUrl
                            };
                            db.StudentProfiles.Add(studentProfile);
                        }
                        db.SaveChanges();
                    }

                    TempData["SuccessMessage"] = "Tạo tài khoản người dùng thành công!";
                    return RedirectToAction("Index");
                }
            }

            ViewBag.RoleId = new SelectList(db.Roles, "Id", "Name", model.RoleId);
            return View(model);
        }

        #endregion

        #region 4. CẬP NHẬT NGƯỜI DÙNG (EDIT)

        // GET: Users/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            User user = db.Users
                .Include(u => u.TeacherProfile)
                .Include(u => u.StudentProfile)
                .FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return HttpNotFound();
            }

            var viewModel = new UserEditViewModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleId = user.RoleId
            };

            if (user.TeacherProfile != null)
            {
                viewModel.Specialization = user.TeacherProfile.Specialization;
                viewModel.ExistingAvatarUrl = user.TeacherProfile.AvatarUrl;
            }

            if (user.StudentProfile != null)
            {
                viewModel.Address = user.StudentProfile.Address;
                viewModel.DateOfBirth = user.StudentProfile.DateOfBirth;
                if (string.IsNullOrEmpty(viewModel.ExistingAvatarUrl))
                {
                    viewModel.ExistingAvatarUrl = user.StudentProfile.AvatarUrl;
                }
            }

            ViewBag.RoleId = new SelectList(db.Roles, "Id", "Name", user.RoleId);
            return View(viewModel);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UserEditViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                var user = db.Users
                    .Include(u => u.TeacherProfile)
                    .Include(u => u.StudentProfile)
                    .FirstOrDefault(u => u.Id == model.Id);

                if (user == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra Email trùng lập
                if (db.Users.Any(u => u.Email == model.Email && u.Id != model.Id))
                {
                    ModelState.AddModelError("Email", "Email này đã được tài khoản khác sử dụng.");
                }
                // Thêm đoạn này trước khi gọi ModelState.IsValid
                if (db.Users.Any(u => u.Phone == model.Phone && u.Id != model.Id))
                {
                    ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng bởi tài khoản khác.");
                }

                if (ModelState.IsValid)
                {
                    user.FullName = model.FullName;
                    user.Email = model.Email;
                    user.Phone = model.Phone;
                    user.RoleId = model.RoleId;

                    // Đổi mật khẩu nếu nhập
                    if (!string.IsNullOrEmpty(model.NewPassword))
                    {
                        user.PasswordHash = HashPassword(model.NewPassword);
                    }

                    // Upload Avatar mới nếu có chọn file
                    string newAvatarUrl = SaveUploadedAvatar(model.AvatarFile);

                    var role = db.Roles.Find(model.RoleId);
                    if (role != null)
                    {
                        string roleName = role.Name.ToLower();

                        if (roleName.Contains("teacher") || roleName.Contains("giáo viên"))
                        {
                            if (user.TeacherProfile == null)
                            {
                                user.TeacherProfile = new TeacherProfile { UserId = user.Id };
                                db.TeacherProfiles.Add(user.TeacherProfile);
                            }
                            user.TeacherProfile.Specialization = model.Specialization;
                            if (!string.IsNullOrEmpty(newAvatarUrl))
                            {
                                user.TeacherProfile.AvatarUrl = newAvatarUrl;
                            }
                        }
                        else if (roleName.Contains("student") || roleName.Contains("học viên"))
                        {
                            if (user.StudentProfile == null)
                            {
                                user.StudentProfile = new StudentProfile { UserId = user.Id };
                                db.StudentProfiles.Add(user.StudentProfile);
                            }
                            user.StudentProfile.Address = model.Address;
                            user.StudentProfile.DateOfBirth = model.DateOfBirth;
                            if (!string.IsNullOrEmpty(newAvatarUrl))
                            {
                                user.StudentProfile.AvatarUrl = newAvatarUrl;
                            }
                        }
                    }

                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Cập nhật thông tin người dùng thành công!";
                    return RedirectToAction("Index");
                }
            }

            ViewBag.RoleId = new SelectList(db.Roles, "Id", "Name", model.RoleId);
            return View(model);
        }

        #endregion

        #region 5. XÓA NGƯỜI DÙNG - YÊU CẦU XÁC NHẬN MẬT KHẨU ADMIN (DELETE)

        // GET: Users/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            User user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return HttpNotFound();
            }

            var confirmViewModel = new UserConfirmDeleteViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Username = user.Username,
                RoleName = user.Role?.Name
            };

            return View(confirmViewModel);
        }

        // POST: Users/DeleteConfirm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(UserConfirmDeleteViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            // 1. Kiểm tra Mật khẩu Admin đang đăng nhập
            if (Session["UserId"] == null) return RedirectToAction("Login", "Auth");
            int currentAdminId = Convert.ToInt32(Session["UserId"]);

            var adminUser = db.Users.Find(currentAdminId);
            string hashedInputPassword = HashPassword(model.AdminPassword);

            if (adminUser == null || adminUser.PasswordHash != hashedInputPassword)
            {
                ModelState.AddModelError("AdminPassword", "Mật khẩu Admin không chính xác. Không thể xóa người dùng!");
                return View("Delete", model);
            }

            // 2. Mật khẩu đúng -> Thực hiện xóa tài khoản và Hồ sơ liên quan
            try
            {
                User userToDelete = db.Users.Find(model.UserId);
                if (userToDelete != null)
                {
                    var studentProfile = db.StudentProfiles.FirstOrDefault(sp => sp.UserId == model.UserId);
                    if (studentProfile != null) db.StudentProfiles.Remove(studentProfile);

                    var teacherProfile = db.TeacherProfiles.FirstOrDefault(tp => tp.UserId == model.UserId);
                    if (teacherProfile != null) db.TeacherProfiles.Remove(teacherProfile);

                    db.Users.Remove(userToDelete);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã xóa tài khoản người dùng thành công!";
                }

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Không thể xoá người dùng, có thể do ràng buộc dữ liệu hoặc lỗi hệ thống. Chi tiết: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region 6. HELPER METHODS

        private string SaveUploadedAvatar(HttpPostedFileBase avatarFile)
        {
            if (avatarFile != null && avatarFile.ContentLength > 0)
            {
                string extension = Path.GetExtension(avatarFile.FileName);
                string fileName = "avatar_" + Guid.NewGuid().ToString("N") + extension;
                string folderPath = Server.MapPath("~/Uploads/Avatars/");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string filePath = Path.Combine(folderPath, fileName);
                avatarFile.SaveAs(filePath);
                return "/Uploads/Avatars/" + fileName;
            }
            return null;
        }

        private bool IsAdmin()
        {
            if (Session["UserId"] == null) return false;

            string roleName = Session["RoleName"]?.ToString() ?? Session["Role"]?.ToString();
            if (string.IsNullOrEmpty(roleName)) return false;

            return roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                   roleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
                   roleName.Equals("Quản trị viên", StringComparison.OrdinalIgnoreCase);
        }

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
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