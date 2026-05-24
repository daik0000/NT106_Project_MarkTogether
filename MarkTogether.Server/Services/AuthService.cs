using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
        private static readonly ConcurrentDictionary<string, LoginFailEntry> _loginFails
            = new ConcurrentDictionary<string, LoginFailEntry>();

        private const int MaxFailBeforeLock = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private static readonly ConcurrentDictionary<string, OtpEntry> _resetOtps
            = new ConcurrentDictionary<string, OtpEntry>();

        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan OtpResendCooldown = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Xử lý đăng ký tài khoản mới.
        /// </summary>
    public static Payload_AUTH_RESPONSE Register(string username, string email, string password)
        {
            // 1. Validate input
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return new Payload_AUTH_RESPONSE { Success = false, Message = "Username và password không được để trống" };

            if (password.Length < 6)
                return new Payload_AUTH_RESPONSE { Success = false, Message = "Mật khẩu phải có ít nhất 6 ký tự" };

            // 2. Kiểm tra username đã tồn tại chưa
            var existingUser = UserRepository.GetByUsername(username);
            if (existingUser != null)
                return new Payload_AUTH_RESPONSE { Success = false, Message = "Username đã được sử dụng" };

            // 3. Kiểm tra email đã tồn tại chưa (nếu có nhập email)
            if (!string.IsNullOrEmpty(email))
            {
                var existingEmail = UserRepository.GetByEmail(email);
                if (existingEmail != null)
                    return new Payload_AUTH_RESPONSE { Success = false, Message = "Email đã được sử dụng" };
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

            return new Payload_AUTH_RESPONSE
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
        public static Payload_AUTH_RESPONSE Login(string username, string password)
        {
            // 1. Validate input
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return new Payload_AUTH_RESPONSE { Success = false, Message = "Username và password không được để trống" };

            string key = username.Trim().ToLowerInvariant();
            LoginFailEntry entry;
            if (_loginFails.TryGetValue(key, out entry) && entry.LockUntilUtc > DateTime.UtcNow)
            {
                var remain = (int)Math.Ceiling((entry.LockUntilUtc - DateTime.UtcNow).TotalSeconds);
                Console.WriteLine($"[Auth] Locked login attempt: user={key} remaining={remain}s");
                return new Payload_AUTH_RESPONSE
                {
                    Success = false,
                    Message = $"Tài khoản tạm bị khoá. Thử lại sau {remain} giây."
                };
            }

            // 2. Tìm user theo username
            var user = UserRepository.GetByUsername(username);
            if (user == null)
                return new Payload_AUTH_RESPONSE { Success = false, Message = "Username không tồn tại" };

            // 3. So khớp password
            bool isMatch = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isMatch)
            {
                var updated = _loginFails.AddOrUpdate(key,
                    _ => new LoginFailEntry(1, DateTime.MinValue),
                    (_, prev) =>
                    {
                        int count = prev.FailCount + 1;
                        DateTime until = count >= MaxFailBeforeLock ? DateTime.UtcNow.Add(LockoutDuration) : DateTime.MinValue;
                        return new LoginFailEntry(count, until);
                    });

                Console.WriteLine($"[Auth] Login failed user={key} count={updated.FailCount}");
                return new Payload_AUTH_RESPONSE { Success = false, Message = "Mật khẩu không đúng" };
            }

            _loginFails.TryRemove(key, out _);

            // 4. Tạo token
            string token = GenerateToken();

            Console.WriteLine($"[Auth] Đăng nhập thành công: {username} (ID={user.Id})");

            return new Payload_AUTH_RESPONSE
            {
                Success = true,
                Message = "Đăng nhập thành công",
                Token = token,
                UserId = user.Id,
                Username = user.Username
            };
        }

        public static async Task<Payload_AUTH_FORGOT_PASSWORD_Response> RequestPasswordResetAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                return new Payload_AUTH_FORGOT_PASSWORD_Response
                {
                    Success = false,
                    Message = "Email không hợp lệ."
                };
            }

            string key = email.Trim().ToLowerInvariant();
            var generic = new Payload_AUTH_FORGOT_PASSWORD_Response
            {
                Success = true,
                Message = "Nếu email tồn tại, mã OTP đã được gửi."
            };

            var user = UserRepository.GetByEmail(key);
            if (user == null) return generic;

            OtpEntry prev;
            if (_resetOtps.TryGetValue(key, out prev)
                && (DateTime.UtcNow - prev.CreatedAtUtc) < OtpResendCooldown)
            {
                return generic;
            }

            string otp = GenerateOtp6();
            DateTime now = DateTime.UtcNow;
            _resetOtps[key] = new OtpEntry
            {
                OtpHash = Sha256(otp),
                ExpiresAtUtc = now.Add(OtpLifetime),
                CreatedAtUtc = now,
                UserId = user.Id
            };

            try
            {
                await EmailService.SendOtpAsync(user.Email, user.Username, otp).ConfigureAwait(false);
                Console.WriteLine($"[Auth] Reset OTP sent user={user.Id} email={Mask(user.Email)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Auth] SMTP send failed user={user.Id}: {ex.Message}");
            }

            return generic;
        }

        public static Payload_AUTH_RESET_PASSWORD_Response ResetPassword(string email, string otp, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(newPassword))
            {
                return new Payload_AUTH_RESET_PASSWORD_Response
                {
                    Success = false,
                    Message = "Thiếu thông tin."
                };
            }

            if (newPassword.Length < 6)
            {
                return new Payload_AUTH_RESET_PASSWORD_Response
                {
                    Success = false,
                    Message = "Mật khẩu phải có ít nhất 6 ký tự."
                };
            }

            string key = email.Trim().ToLowerInvariant();
            OtpEntry entry;
            if (!_resetOtps.TryGetValue(key, out entry))
            {
                return new Payload_AUTH_RESET_PASSWORD_Response
                {
                    Success = false,
                    Message = "OTP không hợp lệ hoặc đã hết hạn."
                };
            }

            if (DateTime.UtcNow > entry.ExpiresAtUtc)
            {
                _resetOtps.TryRemove(key, out _);
                return new Payload_AUTH_RESET_PASSWORD_Response
                {
                    Success = false,
                    Message = "OTP đã hết hạn."
                };
            }

            if (!string.Equals(Sha256(otp.Trim()), entry.OtpHash, StringComparison.OrdinalIgnoreCase))
            {
                return new Payload_AUTH_RESET_PASSWORD_Response
                {
                    Success = false,
                    Message = "OTP không đúng."
                };
            }

            string hash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
            if (!UserRepository.UpdatePassword(entry.UserId, hash))
            {
                return new Payload_AUTH_RESET_PASSWORD_Response
                {
                    Success = false,
                    Message = "Không cập nhật được mật khẩu."
                };
            }

            _resetOtps.TryRemove(key, out _);

            var user = UserRepository.GetById(entry.UserId);
            if (user != null)
                _loginFails.TryRemove(user.Username.Trim().ToLowerInvariant(), out _);
            _loginFails.TryRemove(key, out _);

            Console.WriteLine($"[Auth] Password reset success user={entry.UserId}");
            return new Payload_AUTH_RESET_PASSWORD_Response
            {
                Success = true,
                Message = "Đặt lại mật khẩu thành công."
            };
        }

        /// <summary>
        /// Tạo token ngẫu nhiên bằng CSPRNG.
        /// </summary>
        private static string GenerateToken()
        {
            return SecureTokenGenerator.GenerateUrlSafeToken(48);
        }

        private static string GenerateOtp6()
        {
            var bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return ((BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF) % 1000000).ToString("D6");
        }

        private static string Sha256(string value)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static string Mask(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@")) return "***";
            string[] parts = email.Split('@');
            string name = parts[0];
            return name.Substring(0, Math.Min(2, name.Length)) + "***@" + parts[1];
        }

        private struct LoginFailEntry
        {
            public LoginFailEntry(int failCount, DateTime lockUntilUtc)
            {
                FailCount = failCount;
                LockUntilUtc = lockUntilUtc;
            }

            public int FailCount { get; }
            public DateTime LockUntilUtc { get; }
        }

        private struct OtpEntry
        {
            public string OtpHash { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
            public DateTime CreatedAtUtc { get; set; }
            public int UserId { get; set; }
        }
    }
}
