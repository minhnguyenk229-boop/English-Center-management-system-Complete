using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace English_language_center_management_system.Controllers
{
    public class AuthController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        #region 1. ĐĂNG NHẬP (LOGIN)

        // GET: /Auth/Login
        [HttpGet]
        public ActionResult Login()
        {
            if (Session["UserId"] != null)
            {
                return RedirectByUserRole(Session["RoleName"]?.ToString());
            }
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Mã hóa mật khẩu
            string hashedPassword = HashPassword(model.Password);

            // Sửa tên thuộc tính theo đúng Model User (Username, Email, PasswordHash)
            var user = db.Users.FirstOrDefault(u =>
                (u.Username == model.Username || u.Email == model.Username) &&
                u.PasswordHash == hashedPassword);

            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Lấy RoleName từ Role Model (Id, Name)
            var role = db.Roles.Find(user.RoleId);
            string roleName = role != null ? role.Name : "Student";

            // Lưu thông tin vào Session (Sửa sang Id, Username, FullName, Email, RoleId)
            Session["UserId"] = user.Id;
            Session["Username"] = user.Username;
            Session["FullName"] = user.FullName;
            Session["Email"] = user.Email;
            Session["RoleId"] = user.RoleId;
            Session["RoleName"] = roleName;

            return RedirectByUserRole(roleName);
        }

        #endregion

        #region 2. ĐĂNG KÝ (REGISTER)

        // GET: /Auth/Register
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (db.Users.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng.");
                return View(model);
            }

            if (db.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký.");
                return View(model);
            }

            // Tìm Role "Student" (dùng Name và Id)
            var studentRole = db.Roles.FirstOrDefault(r => r.Name.ToLower() == "student");
            int defaultRoleId = studentRole != null ? studentRole.Id : 3;

            // Tạo đối tượng User mới với thuộc tính PascalCase
            var newUser = new User
            {
                Username = model.Username,
                PasswordHash = HashPassword(model.Password),
                FullName = model.FullName,
                Email = model.Email,
                RoleId = defaultRoleId,
                CreatedAt = DateTime.Now
            };

            db.Users.Add(newUser);
            db.SaveChanges(); // Lưu để sinh ra newUser.Id

            // Tạo StudentProfile tương ứng (gắn UserId = newUser.Id)
            var studentProfile = new StudentProfile
            {
                UserId = newUser.Id
            };
            db.StudentProfiles.Add(studentProfile);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        #endregion

        #region 3. ĐĂNG XUẤT (LOGOUT)

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Login");
        }

        #endregion

        #region 4. HELPER METHODS

        private ActionResult RedirectByUserRole(string roleName)
        {
            if (string.IsNullOrEmpty(roleName)) return RedirectToAction("Index", "Home");

            switch (roleName.ToLower())
            {
                case "admin":
                    return RedirectToAction("AdminDashboard", "Home");
                case "teacher":
                    return RedirectToAction("TeacherDashboard", "Home");
                case "student":
                default:
                    return RedirectToAction("StudentDashboard", "Home");
            }
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