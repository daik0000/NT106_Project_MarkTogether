# Kế Hoạch Triển Khai Chi Tiết: Phần AUTH (Đăng Ký / Đăng Nhập)

> **Tham chiếu:** PHẦN 2 trong `KeHoachTrienKhai_ChiTiet.docx`  
> **Dự án:** MarkTogether — Ứng dụng soạn thảo cộng tác real-time  
> **Stack:** .NET Framework 4.7.2 · WinForms · TCP Socket · PostgreSQL · Dapper

---

## Tổng Quan

Hệ thống Auth của MarkTogether hoạt động **qua giao thức TCP Socket** (KHÔNG dùng REST API HTTP). Client gửi `Packet` có `MessageType = AUTH_LOGIN` hoặc `AUTH_REGISTER` đến Server qua kết nối TCP. Server xử lý xong trả về `Packet` có `MessageType = AUTH_RESPONSE`.

```
┌────────────┐    TCP Socket     ┌─────────────┐      Dapper       ┌────────────┐
│  WinForms  │ ──── Packet ────▶ │   Server    │ ──── SQL ───────▶ │ PostgreSQL │
│  Client    │ ◀── Packet ────── │  (Handler)  │ ◀── User row ──── │  (Docker)  │
└────────────┘                   └─────────────┘                   └────────────┘
```

---

## Những Gì Đã Có Sẵn (Đã Code)

| Thành phần | File | Trạng thái |
|---|---|---|
| Bảng `users` trong DB | `Database/Scripts/schema_init.sql` | ✅ Hoàn thành |
| Model POCO `User.cs` | `Database/Models/User.cs` | ✅ Hoàn thành |
| Repository truy vấn DB | `Database/Repositories/UserRepository.cs` | ✅ Hoàn thành |
| Kết nối PostgreSQL | `Database/DbConnectionFactory.cs` | ✅ Hoàn thành |
| Packet & MessageType | `MarkTogether.Shared/Packet.cs` | ✅ Hoàn thành |
| Gửi/Nhận Packet qua TCP | `MarkTogether.Shared/PacketHelper.cs` | ✅ Hoàn thành |
| Package BCrypt.Net-Next | `packages.config` | ✅ Đã cài |
| Package Newtonsoft.Json | `packages.config` | ✅ Đã cài |

---

## Những Gì Cần Triển Khai Thêm

### Cấu trúc file cần tạo/sửa

```
MarkTogether.Server/
├── Services/
│   └── AuthService.cs              ← [MỚI] Logic xác thực (BCrypt hash/verify)
├── Network/
│   ├── SocketServer.cs             ← [MỚI] TcpListener, chấp nhận kết nối
│   ├── ClientHandler.cs            ← [MỚI] Xử lý từng client, dispatch message
│   └── SessionManager.cs           ← [MỚI] Quản lý token ↔ userId (in-memory)
├── Program.cs                      ← [SỬA] Khởi động SocketServer + Dapper config

MarkTogether.Client/
├── Network/
│   └── SocketClient.cs             ← [MỚI] Kết nối TCP, gửi/nhận Packet
├── LoginForm.cs                    ← [MỚI] Giao diện đăng nhập
├── LoginForm.Designer.cs           ← [MỚI] Auto-generated
├── RegisterForm.cs                 ← [MỚI] Giao diện đăng ký
├── RegisterForm.Designer.cs        ← [MỚI] Auto-generated
└── Program.cs                      ← [SỬA] Mở LoginForm thay vì Form1

MarkTogether.Shared/
└── Packet.cs                       ← [SỬA] Thêm payload cho Register (email)
```

---

## Chi Tiết Từng Bước

---

### Bước 1: Tạo `AuthService.cs` — Lớp Logic Xác Thực (Server)

**Đường dẫn:** `MarkTogether.Server/Services/AuthService.cs`

**Vai trò:** Chứa toàn bộ logic nghiệp vụ Auth. Tách riêng khỏi Network để dễ test và bảo trì.

**Các hàm cần có:**

| Hàm | Mô tả | Gọi tới |
|---|---|---|
| `Register(username, email, password)` | Kiểm tra trùng username/email → Hash password bằng BCrypt → Gọi `UserRepository.Create()` → Trả về `AuthResponse` | `UserRepository.GetByUsername()`, `UserRepository.GetByEmail()`, `UserRepository.Create()` |
| `Login(username, password)` | Tìm user theo username → So khớp password với BCrypt.Verify → Tạo token → Trả về `AuthResponse` | `UserRepository.GetByUsername()` |
| `GenerateToken()` | Tạo token ngẫu nhiên (GUID hoặc random bytes) để client gửi kèm các request sau | — |

**Code mẫu chi tiết:**

```csharp
using System;
using BCrypt.Net;
using MarkTogether.Server.Database.Models;
using MarkTogether.Server.Database.Repositories;
using MarkTogether.Shared;

namespace MarkTogether.Server.Services
{
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
            // 1. Tìm user theo username
            var user = UserRepository.GetByUsername(username);
            if (user == null)
                return new AuthResponse { Success = false, Message = "Username không tồn tại" };

            // 2. So khớp password
            bool isMatch = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isMatch)
                return new AuthResponse { Success = false, Message = "Mật khẩu không đúng" };

            // 3. Tạo token
            string token = GenerateToken();

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
            return Guid.NewGuid().ToString("N"); // 32 ký tự hex, ví dụ: "a1b2c3d4e5f6..."
        }
    }
}
```

**Giải thích luồng BCrypt:**
1. **Khi đăng ký:** `BCrypt.HashPassword("abc123")` → sinh ra chuỗi hash dài ~60 ký tự (ví dụ: `$2a$12$LJ3m4...`). Mỗi lần hash cùng một password sẽ cho kết quả khác nhau (do salt ngẫu nhiên).
2. **Khi đăng nhập:** `BCrypt.Verify("abc123", "$2a$12$LJ3m4...")` → trả `true` nếu khớp, `false` nếu sai.
3. **Tại sao dùng BCrypt thay vì SHA256?** BCrypt tự tạo salt, có tham số `workFactor` để làm chậm quá trình hash → chống brute-force attack hiệu quả hơn.

---

### Bước 2: Tạo `SessionManager.cs` — Quản Lý Phiên Đăng Nhập (Server)

**Đường dẫn:** `MarkTogether.Server/Network/SessionManager.cs`

**Vai trò:** Lưu trữ bảng mapping `Token → UserId` trong RAM. Khi client gửi các request sau (tạo document, edit...), server dùng token trong Packet để xác định "đây là user nào".

**Code mẫu:**

```csharp
using System.Collections.Concurrent;

namespace MarkTogether.Server.Network
{
    /// <summary>
    /// Quản lý session token ↔ userId (in-memory).
    /// Thread-safe nhờ ConcurrentDictionary.
    /// </summary>
    public static class SessionManager
    {
        // Key = Token (string), Value = UserId (int)
        private static readonly ConcurrentDictionary<string, int> _sessions
            = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// Thêm session mới sau khi login/register thành công.
        /// </summary>
        public static void AddSession(string token, int userId)
        {
            _sessions[token] = userId;
        }

        /// <summary>
        /// Lấy userId từ token. Trả -1 nếu token không hợp lệ.
        /// </summary>
        public static int GetUserId(string token)
        {
            if (string.IsNullOrEmpty(token)) return -1;
            return _sessions.TryGetValue(token, out int userId) ? userId : -1;
        }

        /// <summary>
        /// Xóa session (logout hoặc disconnect).
        /// </summary>
        public static void RemoveSession(string token)
        {
            if (!string.IsNullOrEmpty(token))
                _sessions.TryRemove(token, out _);
        }

        /// <summary>
        /// Kiểm tra token có hợp lệ không.
        /// </summary>
        public static bool IsValid(string token)
        {
            return !string.IsNullOrEmpty(token) && _sessions.ContainsKey(token);
        }
    }
}
```

**Tại sao dùng in-memory (RAM) thay vì lưu DB?**
- Nhanh hơn nhiều so với query DB mỗi lần xác thực (mỗi thao tác gõ phím đều cần xác thực).
- Phù hợp cho project demo/đồ án. Khi server restart thì token hết hạn, user phải login lại → chấp nhận được.

---

### Bước 3: Tạo `SocketServer.cs` — Lắng Nghe Kết Nối TCP (Server)

**Đường dẫn:** `MarkTogether.Server/Network/SocketServer.cs`

**Vai trò:** Mở TCP port (mặc định 5000), lắng nghe và chấp nhận các kết nối client đến, rồi bàn giao cho `ClientHandler` xử lý riêng.

**Code mẫu:**

```csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MarkTogether.Server.Network
{
    public class SocketServer
    {
        private TcpListener _listener;
        private readonly int _port;
        private bool _running;

        public SocketServer(int port = 5000)
        {
            _port = port;
        }

        /// <summary>
        /// Khởi động server, vòng lặp chấp nhận client liên tục.
        /// </summary>
        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _running = true;

            Console.WriteLine($"[Server] Đang lắng nghe trên port {_port}...");

            while (_running)
            {
                try
                {
                    // Chờ client kết nối (không block thread nhờ async)
                    TcpClient tcpClient = await _listener.AcceptTcpClientAsync();
                    string clientIp = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
                    Console.WriteLine($"[Server] Client mới kết nối: {clientIp}");

                    // Tạo handler riêng cho client này, xử lý song song
                    var handler = new ClientHandler(tcpClient);
                    Task.Run(() => handler.ProcessAsync());
                }
                catch (Exception ex)
                {
                    if (_running)
                        Console.WriteLine($"[Server] Lỗi accept: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Dừng server.
        /// </summary>
        public void Stop()
        {
            _running = false;
            _listener?.Stop();
            Console.WriteLine("[Server] Đã dừng.");
        }
    }
}
```

---

### Bước 4: Tạo `ClientHandler.cs` — Xử Lý Từng Client (Server)

**Đường dẫn:** `MarkTogether.Server/Network/ClientHandler.cs`

**Vai trò:** Mỗi client kết nối sẽ có 1 instance `ClientHandler` riêng chạy trên 1 thread riêng. Nó đọc Packet từ client, kiểm tra `MessageType`, rồi gọi hàm xử lý tương ứng.

**Code mẫu (phần Auth):**

```csharp
using System;
using System.IO;
using System.Net.Sockets;
using MarkTogether.Server.Services;
using MarkTogether.Shared;

namespace MarkTogether.Server.Network
{
    public class ClientHandler
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private string _token;     // Token của client này sau khi login
        private int _userId = -1;  // UserId sau khi xác thực

        public ClientHandler(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
        }

        /// <summary>
        /// Vòng lặp chính: đọc packet → xử lý → trả kết quả.
        /// Chạy cho đến khi client ngắt kết nối.
        /// </summary>
        public void ProcessAsync()
        {
            try
            {
                while (_tcpClient.Connected)
                {
                    // 1. Đọc packet từ client
                    Packet packet = PacketHelper.Receive(_stream);

                    // 2. Dispatch theo MessageType
                    switch (packet.Type)
                    {
                        case MessageType.AUTH_REGISTER:
                            HandleRegister(packet);
                            break;

                        case MessageType.AUTH_LOGIN:
                            HandleLogin(packet);
                            break;

                        // ... các case khác (DOC_CREATE, OP_INSERT...) sẽ thêm sau

                        default:
                            // Nếu chưa login → từ chối
                            if (_userId < 0)
                            {
                                SendError("Bạn chưa đăng nhập. Vui lòng login trước.");
                                break;
                            }
                            // TODO: xử lý các message type khác
                            break;
                    }
                }
            }
            catch (IOException)
            {
                // Client ngắt kết nối
                Console.WriteLine($"[Handler] Client {_userId} đã ngắt kết nối.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Handler] Lỗi: {ex.Message}");
            }
            finally
            {
                // Dọn dẹp
                if (!string.IsNullOrEmpty(_token))
                    SessionManager.RemoveSession(_token);

                _stream?.Close();
                _tcpClient?.Close();
            }
        }

        // ═══════════════════════════════════════════
        //  XỬ LÝ ĐĂNG KÝ
        // ═══════════════════════════════════════════
        private void HandleRegister(Packet packet)
        {
            // 1. Parse payload từ client
            var payload = packet.GetPayload<RegisterPayload>();

            // 2. Gọi AuthService xử lý logic
            AuthResponse response = AuthService.Register(
                payload.Username, payload.Email, payload.Password
            );

            // 3. Nếu thành công → lưu session
            if (response.Success)
            {
                _token = response.Token;
                _userId = response.UserId;
                SessionManager.AddSession(_token, _userId);
            }

            // 4. Gửi kết quả về cho client
            var responsePacket = Packet.Create(MessageType.AUTH_RESPONSE, response);
            PacketHelper.Send(_stream, responsePacket);
        }

        // ═══════════════════════════════════════════
        //  XỬ LÝ ĐĂNG NHẬP
        // ═══════════════════════════════════════════
        private void HandleLogin(Packet packet)
        {
            // 1. Parse payload
            var payload = packet.GetPayload<AuthPayload>();

            // 2. Gọi AuthService
            AuthResponse response = AuthService.Login(
                payload.Username, payload.Password
            );

            // 3. Nếu thành công → lưu session
            if (response.Success)
            {
                _token = response.Token;
                _userId = response.UserId;
                SessionManager.AddSession(_token, _userId);
            }

            // 4. Trả kết quả
            var responsePacket = Packet.Create(MessageType.AUTH_RESPONSE, response);
            PacketHelper.Send(_stream, responsePacket);
        }

        // ═══════════════════════════════════════════
        //  GỬI LỖI
        // ═══════════════════════════════════════════
        private void SendError(string message)
        {
            var errorPacket = Packet.Create(MessageType.ERROR, new ErrorPayload { Message = message });
            PacketHelper.Send(_stream, errorPacket);
        }
    }
}
```

---

### Bước 5: Cập Nhật `Packet.cs` — Thêm Payload Đăng Ký (Shared)

**Đường dẫn:** `MarkTogether.Shared/Packet.cs`

Hiện tại `AuthPayload` chỉ có `Username` và `Password`. Cần thêm class `RegisterPayload` có thêm trường `Email` cho đăng ký.

**Cần thêm vào cuối file (trước dấu `}` đóng namespace):**

```csharp
// Payload riêng cho đăng ký (có thêm Email)
public class RegisterPayload
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}
```

---

### Bước 6: Sửa `Program.cs` Server — Khởi Động Hệ Thống

**Đường dẫn:** `MarkTogether.Server/Program.cs`

Hiện tại file `Program.cs` đang rỗng. Cần thêm code khởi tạo Dapper config và chạy SocketServer.

**Code mới:**

```csharp
using System;
using System.Threading.Tasks;
using MarkTogether.Server.Network;

namespace MarkTogether.Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 1. Bật Dapper mapping snake_case (PostgreSQL) ↔ PascalCase (C#)
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║     MarkTogether Server v1.0          ║");
            Console.WriteLine("╠════════════════════════════════════════╣");
            Console.WriteLine("║  PostgreSQL: localhost:5432/myapp_db   ║");
            Console.WriteLine("║  TCP Socket: port 5000                ║");
            Console.WriteLine("╚════════════════════════════════════════╝");

            // 2. Khởi động TCP Socket Server
            var server = new SocketServer(5000);
            Task.Run(() => server.StartAsync());

            Console.WriteLine("\nNhấn Enter để dừng server...");
            Console.ReadLine();
            server.Stop();
        }
    }
}
```

---

### Bước 7: Tạo `SocketClient.cs` — Kết Nối TCP (Client)

**Đường dẫn:** `MarkTogether.Client/Network/SocketClient.cs`

**Vai trò:** Singleton class phía Client, giữ kết nối TCP xuyên suốt phiên làm việc.

**Code mẫu:**

```csharp
using System;
using System.Net.Sockets;
using MarkTogether.Shared;

namespace MarkTogether.Client.Network
{
    public class SocketClient
    {
        private TcpClient _tcp;
        private NetworkStream _stream;

        // Singleton
        public static SocketClient Instance { get; } = new SocketClient();

        // Thông tin user sau khi login thành công
        public string Token { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }

        /// <summary>
        /// Kết nối đến server.
        /// </summary>
        public void Connect(string host = "localhost", int port = 5000)
        {
            _tcp = new TcpClient();
            _tcp.Connect(host, port);
            _stream = _tcp.GetStream();
        }

        /// <summary>
        /// Gửi packet đến server.
        /// </summary>
        public void Send(Packet packet)
        {
            PacketHelper.Send(_stream, packet);
        }

        /// <summary>
        /// Nhận packet từ server (blocking).
        /// </summary>
        public Packet Receive()
        {
            return PacketHelper.Receive(_stream);
        }

        /// <summary>
        /// Đăng nhập. Trả AuthResponse.
        /// </summary>
        public AuthResponse Login(string username, string password)
        {
            var packet = Packet.Create(MessageType.AUTH_LOGIN,
                new AuthPayload { Username = username, Password = password });
            Send(packet);

            Packet response = Receive();
            var authResp = response.GetPayload<AuthResponse>();

            if (authResp.Success)
            {
                Token = authResp.Token;
                UserId = authResp.UserId;
                Username = authResp.Username;
            }

            return authResp;
        }

        /// <summary>
        /// Đăng ký. Trả AuthResponse.
        /// </summary>
        public AuthResponse Register(string username, string email, string password)
        {
            var packet = Packet.Create(MessageType.AUTH_REGISTER,
                new RegisterPayload
                {
                    Username = username,
                    Email = email,
                    Password = password
                });
            Send(packet);

            Packet response = Receive();
            var authResp = response.GetPayload<AuthResponse>();

            if (authResp.Success)
            {
                Token = authResp.Token;
                UserId = authResp.UserId;
                Username = authResp.Username;
            }

            return authResp;
        }

        /// <summary>
        /// Ngắt kết nối.
        /// </summary>
        public void Disconnect()
        {
            _stream?.Close();
            _tcp?.Close();
        }
    }
}
```

---

### Bước 8: Tạo `LoginForm.cs` — Giao Diện Đăng Nhập (Client)

**Đường dẫn:** `MarkTogether.Client/LoginForm.cs`

**Vai trò:** Form WinForms có 2 ô nhập (Username, Password), nút "Đăng nhập", link "Chưa có tài khoản? Đăng ký".

**Các control cần có:**
| Control | Name | Mô tả |
|---|---|---|
| TextBox | `txtUsername` | Nhập username |
| TextBox | `txtPassword` | Nhập password (`PasswordChar = '*'`) |
| Button | `btnLogin` | Nút đăng nhập |
| LinkLabel | `lnkRegister` | Link chuyển sang form đăng ký |
| Label | `lblError` | Hiển thị lỗi (ẩn mặc định) |

**Code xử lý sự kiện:**

```csharp
using System;
using System.Windows.Forms;
using MarkTogether.Client.Network;
using MarkTogether.Shared;

namespace MarkTogether.Client
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            // Validate
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Vui lòng nhập đầy đủ thông tin!";
                lblError.Visible = true;
                return;
            }

            try
            {
                // Kết nối server (nếu chưa)
                SocketClient.Instance.Connect("localhost", 5000);

                // Gửi yêu cầu đăng nhập
                AuthResponse result = SocketClient.Instance.Login(username, password);

                if (result.Success)
                {
                    // Thành công → mở MainForm
                    MessageBox.Show($"Chào mừng {result.Username}!", "Đăng nhập thành công");
                    this.Hide();
                    var mainForm = new Form1(); // Sau này đổi thành MainForm
                    mainForm.FormClosed += (s, args) => this.Close();
                    mainForm.Show();
                }
                else
                {
                    // Thất bại → hiện lỗi
                    lblError.Text = result.Message;
                    lblError.Visible = true;
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "Không thể kết nối đến server!";
                lblError.Visible = true;
                MessageBox.Show(ex.Message, "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lnkRegister_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Mở form đăng ký
            var registerForm = new RegisterForm();
            registerForm.ShowDialog();
        }
    }
}
```

---

### Bước 9: Tạo `RegisterForm.cs` — Giao Diện Đăng Ký (Client)

**Đường dẫn:** `MarkTogether.Client/RegisterForm.cs`

**Các control cần có:**
| Control | Name |
|---|---|
| TextBox | `txtUsername` |
| TextBox | `txtEmail` |
| TextBox | `txtPassword` |
| TextBox | `txtConfirmPassword` |
| Button | `btnRegister` |
| Label | `lblError` |

**Code xử lý sự kiện:**

```csharp
private void btnRegister_Click(object sender, EventArgs e)
{
    string username = txtUsername.Text.Trim();
    string email = txtEmail.Text.Trim();
    string password = txtPassword.Text;
    string confirmPassword = txtConfirmPassword.Text;

    // Validate
    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
    {
        lblError.Text = "Username và Password không được để trống!";
        lblError.Visible = true;
        return;
    }

    if (password != confirmPassword)
    {
        lblError.Text = "Mật khẩu xác nhận không khớp!";
        lblError.Visible = true;
        return;
    }

    if (password.Length < 6)
    {
        lblError.Text = "Mật khẩu phải có ít nhất 6 ký tự!";
        lblError.Visible = true;
        return;
    }

    try
    {
        // Kết nối nếu chưa kết nối
        SocketClient.Instance.Connect("localhost", 5000);

        // Gửi yêu cầu đăng ký
        AuthResponse result = SocketClient.Instance.Register(username, email, password);

        if (result.Success)
        {
            MessageBox.Show("Đăng ký thành công! Bạn có thể đăng nhập ngay.", "Thành công");
            this.Close();
        }
        else
        {
            lblError.Text = result.Message;
            lblError.Visible = true;
        }
    }
    catch (Exception ex)
    {
        lblError.Text = "Không thể kết nối server!";
        lblError.Visible = true;
    }
}
```

---

### Bước 10: Sửa `Program.cs` Client — Mở LoginForm Đầu Tiên

**Đường dẫn:** `MarkTogether.Client/Program.cs`

```csharp
static void Main()
{
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new LoginForm()); // Thay Form1 → LoginForm
}
```

---

## Sơ Đồ Luồng Hoạt Động Chi Tiết

### Luồng Đăng Ký

```
Client (WinForms)                    Server (TCP)                     PostgreSQL
     │                                    │                               │
     │  1. User nhập thông tin            │                               │
     │     vào RegisterForm               │                               │
     │                                    │                               │
     ├──── Packet ──────────────────────▶ │                               │
     │  Type: AUTH_REGISTER               │                               │
     │  Payload: {username,               │                               │
     │            email, password}        │                               │
     │                                    │                               │
     │                            2. ClientHandler nhận packet            │
     │                               gọi AuthService.Register()          │
     │                                    │                               │
     │                                    ├─── GetByUsername() ──────────▶│
     │                                    │◀── null (chưa tồn tại) ──────│
     │                                    │                               │
     │                                    │  3. BCrypt.HashPassword()     │
     │                                    │     (hash password)           │
     │                                    │                               │
     │                                    ├─── Create(user) ────────────▶│
     │                                    │◀── userId = 1 ───────────────│
     │                                    │                               │
     │                            4. Tạo token (GUID)                    │
     │                               Lưu vào SessionManager              │
     │                                    │                               │
     │ ◀── Packet ────────────────────────┤                               │
     │  Type: AUTH_RESPONSE               │                               │
     │  Payload: {success: true,          │                               │
     │            token: "a1b2c3...",      │                               │
     │            userId: 1,              │                               │
     │            username: "test_user"}  │                               │
     │                                    │                               │
     │  5. Client lưu token              │                               │
     │     hiện thông báo thành công      │                               │
```

### Luồng Đăng Nhập

```
Client (WinForms)                    Server (TCP)                     PostgreSQL
     │                                    │                               │
     │  1. User nhập username             │                               │
     │     + password vào LoginForm       │                               │
     │                                    │                               │
     ├──── Packet ──────────────────────▶ │                               │
     │  Type: AUTH_LOGIN                  │                               │
     │  Payload: {username, password}     │                               │
     │                                    │                               │
     │                            2. ClientHandler nhận packet            │
     │                               gọi AuthService.Login()             │
     │                                    │                               │
     │                                    ├─── GetByUsername() ──────────▶│
     │                                    │◀── User{id:1, hash:$2a...} ──│
     │                                    │                               │
     │                            3. BCrypt.Verify(password, hash)        │
     │                               → true (khớp!)                      │
     │                                    │                               │
     │                            4. Tạo token mới                       │
     │                               Lưu SessionManager                  │
     │                                    │                               │
     │ ◀── Packet ────────────────────────┤                               │
     │  Type: AUTH_RESPONSE               │                               │
     │  Payload: {success: true,          │                               │
     │            token: "x9y8z7...",      │                               │
     │            userId: 1}              │                               │
     │                                    │                               │
     │  5. Client lưu token              │                               │
     │     → Hide LoginForm              │                               │
     │     → Show MainForm               │                               │
```

---

## Checklist Bảo Mật Auth

| Hạng mục | Cách xử lý | Trạng thái |
|---|---|---|
| Không lưu password dạng plaintext | BCrypt hash với workFactor=12 | ✅ Đã thiết kế |
| Chống SQL Injection | Dapper parameterized query (`@Username`) | ✅ Đã có trong Repository |
| Token ngẫu nhiên | GUID 32 ký tự hex | ✅ Đã thiết kế |
| Validate input trống | Kiểm tra null/empty trước khi xử lý | ✅ Đã thiết kế |
| Kiểm tra trùng username/email | Query DB trước khi INSERT | ✅ Đã thiết kế |
| Password tối thiểu 6 ký tự | Validate phía client | ✅ Đã thiết kế |

---

## Thứ Tự Triển Khai (Đề Xuất)

| # | Việc cần làm | Ước tính |
|---|---|---|
| 1 | Tạo `RegisterPayload` trong `Packet.cs` (Shared) | 5 phút |
| 2 | Tạo `Services/AuthService.cs` (Server) | 15 phút |
| 3 | Tạo `Network/SessionManager.cs` (Server) | 10 phút |
| 4 | Tạo `Network/SocketServer.cs` (Server) | 15 phút |
| 5 | Tạo `Network/ClientHandler.cs` (Server) | 20 phút |
| 6 | Sửa `Program.cs` Server | 5 phút |
| 7 | Tạo `Network/SocketClient.cs` (Client) | 15 phút |
| 8 | Tạo `LoginForm.cs` + Designer (Client) | 20 phút |
| 9 | Tạo `RegisterForm.cs` + Designer (Client) | 15 phút |
| 10 | Sửa `Program.cs` Client → mở LoginForm | 5 phút |
| 11 | Test end-to-end: chạy Server → chạy Client → đăng ký → đăng nhập | 15 phút |
| **Tổng** | | **~2.5 giờ** |

---

*Tài liệu cập nhật: 12/04/2026*
