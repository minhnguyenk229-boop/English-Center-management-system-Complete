using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Users;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace English_language_center_management_system.Controllers
{
    public class ProfilesController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        protected bool IsAuthorizedRole(params string[] roles)
        {
            var userRole = Session["Role"] as string ?? Session["RoleName"] as string;
            if (string.IsNullOrEmpty(userRole)) return false;
            return roles.Any(r => r.Equals(userRole, StringComparison.OrdinalIgnoreCase));
        }

        #region 1. TEACHER PROFILE
        [HttpGet]
        public ActionResult TeacherProfile()
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Include(u => u.TeacherProfile).FirstOrDefault(u => u.Id == userId);
            if (user == null) return HttpNotFound();

            var model = new TeacherProfileViewModel
            {
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Specialization = user.TeacherProfile?.Specialization,
                Bio = user.TeacherProfile?.Bio,
                ExistingAvatarUrl = user.TeacherProfile?.AvatarUrl,
                ExistingCertificateUrls = user.TeacherProfile?.CertificateUrls
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TeacherProfile(TeacherProfileViewModel model)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên")) return RedirectToAction("Login", "Auth");

            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Include(u => u.TeacherProfile).FirstOrDefault(u => u.Id == userId);
            if (user == null) return HttpNotFound();

            string email = (model.Email ?? "").Trim();
            if (db.Users.Any(u => u.Id != userId && u.Email == email))
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
           
            if (!string.IsNullOrEmpty(model.Phone) && db.Users.Any(u => u.Phone == model.Phone && u.Id != userId))
            {
                ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng bởi tài khoản khác.");
            }

            if (!ModelState.IsValid)
            {
                model.Username = user.Username;
                model.ExistingAvatarUrl = user.TeacherProfile?.AvatarUrl;
                model.ExistingCertificateUrls = user.TeacherProfile?.CertificateUrls;
                return View(model);
            }

            user.FullName = (model.FullName ?? "").Trim();
            user.Email = email;
            user.Phone = (model.Phone ?? "").Trim();

            if (user.TeacherProfile == null)
            {
                user.TeacherProfile = new TeacherProfile { UserId = userId };
                db.TeacherProfiles.Add(user.TeacherProfile);
            }

            user.TeacherProfile.Specialization = (model.Specialization ?? "").Trim();
            user.TeacherProfile.Bio = (model.Bio ?? "").Trim();

            if (model.AvatarFile != null && model.AvatarFile.ContentLength > 0)
                user.TeacherProfile.AvatarUrl = SaveUploadedFile(model.AvatarFile, "Avatars", "avatar");

            if (model.CertificateFiles != null && model.CertificateFiles.Any(f => f != null && f.ContentLength > 0))
            {
                var uploadedUrls = new List<string>();
                if (!string.IsNullOrEmpty(user.TeacherProfile.CertificateUrls))
                    uploadedUrls.AddRange(user.TeacherProfile.CertificateUrls.Split(';'));

                foreach (var file in model.CertificateFiles)
                {
                    string fileUrl = SaveUploadedFile(file, "Certificates", "cert");
                    if (fileUrl != null) uploadedUrls.Add(fileUrl);
                }
                user.TeacherProfile.CertificateUrls = string.Join(";", uploadedUrls);
            }

            db.SaveChanges();
            Session["FullName"] = user.FullName;
            TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("TeacherProfile");
        }
        #endregion

        #region 2. STUDENT PROFILE
        [HttpGet]
        public ActionResult StudentProfile()
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Học viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Include(u => u.StudentProfile).FirstOrDefault(u => u.Id == userId);
            if (user == null) return HttpNotFound();

            var viewModel = new StudentProfileViewModel
            {
                Username = user.Username,
                CreatedAt = user.CreatedAt,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                DateOfBirth = user.StudentProfile?.DateOfBirth,
                Address = user.StudentProfile?.Address,
                ExistingAvatarUrl = user.StudentProfile?.AvatarUrl
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StudentProfile(StudentProfileViewModel model)
        {
            if (!IsAuthorizedRole("Student", "Học viên")) return RedirectToAction("Login", "Auth");

            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Include(u => u.StudentProfile).FirstOrDefault(u => u.Id == userId);
            if (user == null) return HttpNotFound();

            if (model.DateOfBirth.HasValue && model.DateOfBirth.Value.Date > DateTime.Today)
                ModelState.AddModelError("DateOfBirth", "Ngày sinh không được ở trong tương lai.");

            if (db.Users.Any(u => u.Id != userId && u.Email == model.Email))
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
            
            if (!string.IsNullOrEmpty(model.Phone) && db.Users.Any(u => u.Phone == model.Phone && u.Id != userId))
            {
                ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng bởi tài khoản khác.");
            }

            if (!ModelState.IsValid)
            {
                model.Username = user.Username;
                model.CreatedAt = user.CreatedAt;
                model.ExistingAvatarUrl = user.StudentProfile?.AvatarUrl;
                return View(model);
            }

            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim();
            user.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

            if (user.StudentProfile == null)
            {
                user.StudentProfile = new StudentProfile { UserId = userId };
                db.StudentProfiles.Add(user.StudentProfile);
            }

            user.StudentProfile.DateOfBirth = model.DateOfBirth;
            user.StudentProfile.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();

            if (model.AvatarFile != null && model.AvatarFile.ContentLength > 0)
                user.StudentProfile.AvatarUrl = SaveUploadedFile(model.AvatarFile, "Avatars", "avatar");

            db.SaveChanges();
            Session["FullName"] = user.FullName;
            TempData["SuccessMessage"] = "Cập nhật hồ sơ cá nhân thành công.";
            return RedirectToAction("StudentProfile");
        }
        #endregion

        #region 3. HELPER METHOD
        private string SaveUploadedFile(HttpPostedFileBase file, string subFolder, string prefix)
        {
            if (file != null && file.ContentLength > 0)
            {
                string extension = Path.GetExtension(file.FileName);
                string fileName = $"{prefix}_{Guid.NewGuid():N}{extension}";
                string folderPath = Server.MapPath($"~/Uploads/{subFolder}/");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                file.SaveAs(Path.Combine(folderPath, fileName));
                return $"/Uploads/{subFolder}/{fileName}";
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