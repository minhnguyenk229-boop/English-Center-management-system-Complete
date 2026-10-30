USE [EnglishCenterDB];
GO

-- 1. Xóa dữ liệu các bảng nghiệp vụ phụ (Child Tables)
DELETE FROM [dbo].[attendances];
DELETE FROM [dbo].[assignments];
DELETE FROM [dbo].[assessment_scores];
DELETE FROM [dbo].[course_materials];
DELETE FROM [dbo].[class_sessions];
DELETE FROM [dbo].[enrollments];
DELETE FROM [dbo].[payments];
DELETE FROM [dbo].[certificates];

-- 2. Xóa dữ liệu các bảng đối tượng trung gian
DELETE FROM [dbo].[classes];
DELETE FROM [dbo].[rooms];
DELETE FROM [dbo].[courses];

-- 3. Xóa Hồ sơ chi tiết
DELETE FROM [dbo].[student_profiles];
DELETE FROM [dbo].[teacher_profiles];

-- 4. Xóa Tài khoản User (Chỉ giữ lại User có role_id = 1 là Admin)
DELETE FROM [dbo].[users] WHERE [role_id] <> 1;

-- Reset lại các cột ID tự tăng (IDENTITY) về lại 1
DBCC CHECKIDENT ('[courses]', RESEED, 0);
DBCC CHECKIDENT ('[classes]', RESEED, 0);
DBCC CHECKIDENT ('[rooms]', RESEED, 0);
GO