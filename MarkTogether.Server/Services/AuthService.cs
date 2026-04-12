using System;
using MarkTogether.Server.Database.Models;
using MarkTogether.Server.Database.Repositories;
using MarkTogether.Shared;

namespace MarkTogether.Server.Services
{
    /// <summary>
    /// Logic nghiệp vụ xác thực: Đăng ký, Đăng nhập.
    /// Password được hash bằng BCrypt (workFactor = 12).
    /// </summary>
    public static class AuthService
    {
        /// <summary>
        /// Xử lý đăng ký tài khoản mới.
        /// </summary>
        public static AuthResponse Register(string username, string email, string password)
        {
            // 1. Validate input
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return new AuthResponse { Success = false, Message = "Username và password không được để trống" };

            if (password.Length < 6)
                return new AuthResponse { Success = false, Message = "Mật khẩu phải có ít nhất 6 ký tự" };

            // 2. Kiểm tra username đã tồn tại chưa
            var existingUser = UserRepository.GetByUsername(username);
            if (existingUser != null)
                return new AuthResponse { Success = false, Message = "Username đã được sử dụng" };

            // 3. Kiểm tra email đã tồn tại chưa (nếu có nhập email)
            if (!string.IsNullOrEmpty(email))
            {
                var existingEmail = UserRepository.GetByEmail(email);
                if (existingEmail != null)
                    return new AuthResponse { Success = false, Message = "Email đã được sử dụng" };
            }

            // 4. Hash password bằng BCrypt (cost factor = 12)
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

            // 5. Tạo user mới trong database
            var newUser = new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash
            };
            int userId = UserRepository.Create(newUser);

            // 6. Tạo session token
            string token = GenerateToken();

            Console.WriteLine($"[Auth] Đăng ký thành công: {username} (ID={userId})");

            return new AuthResponse
            {
                Success = true,
                Message = "Đăng ký thành công",
                Token = token,
                UserId = userId,
                Username = username
            };
        }

        /// <summary>
        /// Xử lý đăng nhập.
        /// </summary>
        public static AuthResponse Login(string username, string password)
        {
            // 1. Validate input
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return new AuthResponse { Success = false, Message = "Username và password không được để trống" };

            // 2. Tìm user theo username
            var user = UserRepository.GetByUsername(username);
            if (user == null)
                return new AuthResponse { Success = false, Message = "Username không tồn tại" };

            // 3. So khớp password
            bool isMatch = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isMatch)
                return new AuthResponse { Success = false, Message = "Mật khẩu không đúng" };

            // 4. Tạo token
            string token = GenerateToken();

            Console.WriteLine($"[Auth] Đăng nhập thành công: {username} (ID={user.Id})");

            return new AuthResponse
            {
                Success = true,
                Message = "Đăng nhập thành công",
                Token = token,
                UserId = user.Id,
                Username = user.Username
            };
        }

        /// <summary>
        /// Tạo token ngẫu nhiên (dùng GUID cho đơn giản).
        /// </summary>
        private static string GenerateToken()
        {
            return Guid.NewGuid().ToString("N"); // 32 ký tự hex
        }
    }
}
