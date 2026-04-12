# Giải Thích Chi Tiết Cấu Trúc Thư Mục Database

Tài liệu này giải thích ý nghĩa và vai trò của từng file trong thư mục `Database` của dự án **MarkTogether.Server** dựa trên kịch bản `implementation_plan2.md`. 

Cấu trúc Database của dự án được thiết kế theo mô hình **Repository Pattern**, dùng **Dapper** làm công cụ mapping dữ liệu (ORM siêu nhẹ) và kết nối với **PostgreSQL**.

---

## 1. Cốt Lõi Cơ Sở Dữ Liệu

### `Scripts/schema_init.sql`
- **Vai trò:** Là bản thiết kế nền móng (foundation).
- **Ý nghĩa:** Chứa các chuỗi lệnh SQL để tạo các bảng (tables) trong PostgreSQL. Bất cứ khi nào bạn bật một Database mới (như Docker bạn vừa làm), bạn cần chạy file này để tạo ra 6 bảng thực thể: `users`, `documents`, `document_shares`, `document_operations`, `document_versions`, `images`.

---

## 2. Cầu Nối Kết Nối (Connection)

### `DbConnectionFactory.cs`
- **Vai trò:** Chuyên gia tạo đường truyền (Bridge).
- **Ý nghĩa:** File này có nhiệm vụ duy nhất là đọc chuỗi cấu hình (chứa username, password, tên database...) từ file `App.config` và tạo ra đối tượng `NpgsqlConnection`. Mỗi khi các Repository muốn "nói chuyện" với database, chúng sẽ gọi hàm trong file này để xin một đường kết nối.

---

## 3. Các Lớp Mô Hình Dữ Liệu (Models)
Các file trong thư mục `Models/` là các **POCO Classes** (Plain Old CLR Objects). Chúng đóng vai trò giống như những "cái khuôn" (mold) trong C#, phản chiếu chính xác cấu trúc cột của các bảng trong SQL. Khi Dapper lấy dữ liệu từ DB, nó sẽ đúc dữ liệu vào các khuôn này.

- **`User.cs`**: Tương ứng với bảng `users`. Chứa thông tin người dùng như Id, Tên, Email và chuỗi bảo mật PasswordHash. Dùng để xử lý Đăng nhập / Đăng ký.
- **`Document.cs`**: Tương ứng với bảng `documents`. Chứa thông tin chung của tài liệu (Tiêu đề, Tác giả, Thời gian tạo, Đường dẫn file gốc nếu cần).
- **`DocumentShare.cs`**: Tương ứng với bảng `document_shares`. Đây là bảng trung gian quy định "Ai được xem/sửa tài liệu nào". Chứa thông tin phân quyền (`viewer`, `editor`, `owner`).
- **`DocumentOperation.cs`**: Tương ứng với bảng `document_operations`. Lưu từng thao tác gõ (thêm/xóa ký tự, ở vị trí nào) của người dùng để hỗ trợ đồng bộ theo thời gian thực (OT/CRDT).
- **`DocumentVersion.cs`**: Tương ứng với bảng `document_versions`. Lưu lại toàn bộ nội dung của văn bản tại một thời điểm nhất định, giúp người dùng có thể *Khôi phục lại lịch sử* (Undo/Restore version cũ).
- **`Image.cs`**: Phản chiếu bảng `images`. Chứa thông tin ảnh được người dùng tải lên và nhúng vào văn bản.
- **`ActiveSession.cs`**: *Đặc biệt:* File này không lưu xuống database PostgreSQL mà chỉ nằm trong bộ nhớ RAM của Server C#. Nó dùng để theo dõi xem "Ai đang mở tài liệu, và con trỏ chuột của họ đang ở đâu" theo thời gian thực (Real-time).

---

## 4. Lớp Tương Tác Dữ Liệu (Repositories)
Thư mục `Repositories/` chứa các hàm và lệnh SQL (Data Access Layer). Mọi thao tác Thêm, Sửa, Xóa hay Lấy dữ liệu đều phải đi qua các class này. Chúng gọi đến `DbConnectionFactory` để mở kết nối, thực thi câu truy vấn SQL thông qua **Dapper**, rồi lấy dữ liệu ném vào các **Models**.

- **`UserRepository.cs`**: 
  - Chứa lệnh truy vấn `SELECT * FROM users WHERE username = ...`. 
  - Nhiệm vụ: Xác thực đăng nhập (so khớp password dạng Hash), tạo mới tài khoản.
- **`DocumentRepository.cs`**:
  - Chứa các hàm `Create`, `UpdateContent`, `Delete` tài liệu.
  - Nhiệm vụ: Quản lý vòng đời của văn bản (tạo mới, lưu, đổi tên).
- **`DocumentShareRepository.cs`**:
  - Nhiệm vụ: Kiểm tra phân quyền (ví dụ: User này có được quyền `editor` trên Document này không?), xử lý hợp tác văn bản.
- **`DocumentVersionRepository.cs`**:
  - Nhiệm vụ: Lưu lại Snapshot của toàn bộ văn bản vào một thời điểm, và truy xuất nó lên khi User yêu cầu khôi phục phiên bản lịch sử.

---

> **Tóm lại Luồng đi của dữ liệu:** 
> Server C# nhận lệnh từ Client (Winform/Browser) -> Gọi hàm tại thư mục `Repository` (Ví dụ: `Thêm user`) -> `Repository` gọi `DbConnectionFactory` để mở kết nối ra -> Bắn câu lệnh SQL vào DB PostgreSQL -> `Dapper` nhận dòng dữ liệu thô từ DB trả về và tự động nhét vào các khuôn trong thư mục `Models` (Objects) -> Trả object kết quả về cho C# chạy tiếp logic.
