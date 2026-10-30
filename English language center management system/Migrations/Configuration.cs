namespace English_language_center_management_system.Migrations
{
    using English_language_center_management_system.Models;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<English_language_center_management_system.Models.EnglishCenterDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(English_language_center_management_system.Models.EnglishCenterDbContext context)
        {
            // 1. Khởi tạo danh sách Roles cố định
            context.Roles.AddOrUpdate(
                r => r.Name,
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "Teacher" },
                new Role { Id = 3, Name = "Student" }
            );

            // 2. Khởi tạo tài khoản Admin mặc định để đăng nhập test
            context.Users.AddOrUpdate(
                u => u.Username,
                new User
                {
                    RoleId = 1,
                    Username = "admin",
                    PasswordHash = "123456", // Nên hash nếu có hàm mã hóa
                    FullName = "Quản trị hệ thống",
                    Email = "admin@englishcenter.com",
                    CreatedAt = DateTime.Now
                }
            );

            context.SaveChanges();
        }
    }
}
