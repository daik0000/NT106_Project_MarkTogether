# 📋 PHÂN CHIA CÔNG VIỆC — DỰ ÁN MARKTOGETHER (NT106)

> **Hệ thống:** Collaborative Markdown Editor qua TCP Socket  
> **Nhóm:** 3 thành viên  
> **Stack:** C# WinForms (Client) + C# TCP Server + PostgreSQL  
> **Ngày cập nhật:** 20/04/2026  

---

## 🗺️ TỔNG QUAN TIẾN ĐỘ (7 GIAI ĐOẠN)

```
GĐ 1: Nền tảng & Giao tiếp         ██████████ HOÀN THÀNH
GĐ 2: Xác thực người dùng           ██████████ HOÀN THÀNH
GĐ 3: Quản lý tài liệu cơ bản      ████████░░ ~85%
GĐ 4: Real-time Editing (OP)        ████░░░░░░ ~40%
GĐ 5: Chia sẻ & Phân quyền         ██░░░░░░░░ ~20%
GĐ 6: OT Engine (xung đột)         ░░░░░░░░░░ CHƯA BẮT ĐẦU
GĐ 7: Tính năng nâng cao / AI      ░░░░░░░░░░ CHƯA BẮT ĐẦU
```

---

## 📌 GIAI ĐOẠN 1 — NỀN TẢNG & GIAO TIẾP MẠNG ✅

> *Mục tiêu: thiết lập xương sống kỹ thuật — không có bước này thì không có gì cả.*

### Những gì cần làm:

| # | Công việc | File liên quan | Trạng thái |
|---|-----------|----------------|------------|
| 1.1 | Thiết kế giao thức `Packet` (JSON + 4-byte length prefix) | `Shared/Packet.cs`, `Shared/PacketHelper.cs` | ✅ Xong |
| 1.2 | Định nghĩa `MessageType` enum cho tất cả loại tin nhắn | `Shared/Packet.cs` | ✅ Xong |
| 1.3 | Định nghĩa tất cả `Payload_*` class cho từng packet | `Shared/Packet.cs` | ✅ Xong |
| 1.4 | Cài đặt `PacketHelper.Send()` / `Receive()` | `Shared/PacketHelper.cs` | ✅ Xong |
| 1.5 | Tạo `SocketServer` lắng nghe TCP trên cổng cố định | `Server/Network/SocketServer.cs` | ✅ Xong |
| 1.6 | Tạo `ClientHandler` — mỗi client một thread riêng | `Server/Network/ClientHandler.cs` | ✅ Xong |
| 1.7 | Tạo `SocketClient` kết nối server từ phía client | `Client/Network/SocketClient.cs` | ✅ Xong |
| 1.8 | Thiết lập project solution `.sln` với 3 project con | `MarkTogether.sln` | ✅ Xong |
| 1.9 | Kết nối PostgreSQL — `DbConnectionFactory` | `Server/Database/DbConnectionFactory.cs` | ✅ Xong |
| 1.10 | Chạy SQL script tạo tables | `Server/Database/Scripts/` | ✅ Xong |

---

## 📌 GIAI ĐOẠN 2 — XÁC THỰC NGƯỜI DÙNG (AUTH) ✅

> *Mục tiêu: người dùng có thể đăng ký, đăng nhập, có token phiên.*

### Những gì cần làm:

| # | Công việc | File liên quan | Trạng thái |
|---|-----------|----------------|------------|
| 2.1 | `UserRepository` — CRUD user trong DB | `Server/Database/Repositories/UserRepository.cs` | ✅ Xong |
| 2.2 | `AuthService.Register()` — hash password, tạo user | `Server/Services/AuthService.cs` | ✅ Xong |
| 2.3 | `AuthService.Login()` — kiểm tra password, cấp token | `Server/Services/AuthService.cs` | ✅ Xong |
| 2.4 | `SessionManager` — lưu token → userId in-memory | `Server/Network/SessionManager.cs` | ✅ Xong |
| 2.5 | `HandleRegister()` trong `ClientHandler` | `Server/Network/ClientHandler.cs` | ✅ Xong |
| 2.6 | `HandleLogin()` trong `ClientHandler` | `Server/Network/ClientHandler.cs` | ✅ Xong |
| 2.7 | `LoginForm` — UI đăng nhập ở Client | `Client/LoginForm.cs` | ✅ Xong |
| 2.8 | `RegisterForm` — UI đăng ký ở Client | `Client/RegisterForm.cs` | ✅ Xong |
| 2.9 | Client gửi `AUTH_LOGIN` / nhận `AUTH_RESPONSE` | `Client/Network/SocketClient.cs` | ✅ Xong |
| 2.10 | Validate token ở mọi request cần auth | `Server/Network/ClientHandler.cs` | ✅ Xong |

---

## 📌 GIAI ĐOẠN 3 — QUẢN LÝ TÀI LIỆU CƠ BẢN 🔶

> *Mục tiêu: tạo, mở, lưu, xem danh sách tài liệu.*

### Những gì cần làm:

| # | Công việc | File liên quan | Trạng thái |
|---|-----------|----------------|------------|
| 3.1 | `Document` model (Id, Title, Content, OwnerId, UpdatedAt) | `Server/Database/Models/Document.cs` | ✅ Xong |
| 3.2 | `DocumentRepository.Create()` — tạo doc mới | `Server/Database/Repositories/DocumentRepository.cs` | ✅ Xong |
| 3.3 | `DocumentRepository.GetById()` — lấy doc theo ID | `Server/Database/Repositories/DocumentRepository.cs` | ✅ Xong |
| 3.4 | `DocumentRepository.GetByUserId()` — danh sách doc của user | `Server/Database/Repositories/DocumentRepository.cs` | ✅ Xong |
| 3.5 | `DocumentRepository.UpdateContent()` — lưu nội dung | `Server/Database/Repositories/DocumentRepository.cs` | ✅ Xong |
| 3.6 | `HandleDocCreate()` — server xử lý tạo doc | `Server/Network/ClientHandler.cs` | ✅ Xong |
| 3.7 | `HandleDocList()` — server trả danh sách doc | `Server/Network/ClientHandler.cs` | ✅ Xong |
| 3.8 | `HandleDocOpen()` — server trả nội dung doc | `Server/Network/ClientHandler.cs` | ✅ Xong |
| 3.9 | `HandleDocSave()` — server lưu nội dung doc | `Server/Network/ClientHandler.cs` | ✅ Xong |
| 3.10 | `HomeForm` — hiển thị danh sách tài liệu | `Client/HomeForm.cs` | ✅ Xong |
| 3.11 | `CreateDocumentForm` — UI tạo tài liệu mới | `Client/CreateDocumentForm.cs` | ✅ Xong |
| 3.12 | `TypeRenderForm` — editor Markdown + preview live | `Client/TypeRenderForm.cs` | ✅ Xong |
| 3.13 | `DocumentVersionRepository` — lịch sử phiên bản | `Server/Database/Repositories/DocumentVersionRepository.cs` | ⚠️ Có model, chưa tích hợp |
| 3.14 | Revision tracking — tăng revision mỗi lần save | `Server/Network/ClientHandler.cs` | ❌ Chưa xong |

---

## 📌 GIAI ĐOẠN 4 — REAL-TIME EDITING (OP_INSERT / OP_DELETE) 🔶

> *Mục tiêu: nhiều người cùng chỉnh sửa tài liệu realtime, ops phát tán tới các client khác.*

### Những gì cần làm:

| # | Công việc | File liên quan | Trạng thái |
|---|-----------|----------------|------------|
| 4.1 | Client phát hiện thay đổi text → tính delta | `Client/TypeRenderForm.cs` → `ComputeTextDelta()` | ✅ Xong |
| 4.2 | Client batch ops ≤5 ký tự, flush theo timer (250ms) | `Client/TypeRenderForm.cs` → `QueueOperation()` | ✅ Xong |
| 4.3 | Client gửi `OP_INSERT` / `OP_DELETE` kèm clientRevision | `Client/Network/SocketClient.cs` | ✅ Xong |
| 4.4 | Server nhận `OP_INSERT` / `OP_DELETE`, validate | `Server/Network/ClientHandler.cs` | ✅ Xong (chỉ log, chưa broadcast) |
| 4.5 | **Server broadcast `OP_BROADCAST` tới các client cùng doc** | `Server/Network/ClientHandler.cs` | ❌ **CHƯA LÀM** |
| 4.6 | `SessionManager` theo dõi client nào đang mở doc nào | `Server/Network/SessionManager.cs` | ❌ **CHƯA LÀM** |
| 4.7 | Client nhận `OP_BROADCAST` → cập nhật textbox | `Client/Network/SocketClient.cs` + `TypeRenderForm.cs` | ❌ **CHƯA LÀM** |
| 4.8 | Revision đồng bộ giữa server và client | `ClientHandler.cs` / `TypeRenderForm.cs` | ❌ Chưa thực sự đúng |
| 4.9 | `DocumentOperation` model — lưu ops vào DB | `Server/Database/Models/DocumentOperation.cs` | ⚠️ Có model, chưa dùng |

---

## 📌 GIAI ĐOẠN 5 — CHIA SẺ & PHÂN QUYỀN 🔴

> *Mục tiêu: chia sẻ doc bằng share code hoặc username, phân quyền viewer/editor/owner.*

### Những gì cần làm:

| # | Công việc | File liên quan | Trạng thái |
|---|-----------|----------------|------------|
| 5.1 | `DocumentShare` model (docId, userId, permission) | `Server/Database/Models/DocumentShare.cs` | ✅ Model xong |
| 5.2 | `DocumentShareRepository` — CRUD phân quyền | `Server/Database/Repositories/DocumentShareRepository.cs` | ✅ Xong |
| 5.3 | `HandleDocShare()` — chia sẻ bằng username | `Server/Network/ClientHandler.cs` | ❌ **CHƯA LÀM** |
| 5.4 | `HandleDocJoinCode()` — tham gia bằng share code | `Server/Network/ClientHandler.cs` | ❌ **CHƯA LÀM** |
| 5.5 | Sinh `shareCode` ngẫu nhiên khi tạo tài liệu | `Server/Network/ClientHandler.cs` | ❌ Chưa tích hợp |
| 5.6 | `HandleDocOpen()` kiểm tra quyền (không chỉ owner) | `Server/Network/ClientHandler.cs` | ❌ Hiện chỉ cho owner |
| 5.7 | UI: nhập share code / tên user để tham gia | `Client/HomeForm.cs` | ❌ Chưa có UI |
| 5.8 | Hiển thị quyền đang có trên editor | `Client/TypeRenderForm.cs` | ❌ Chưa có |

---

## 📌 GIAI ĐOẠN 6 — OT ENGINE (GIẢI QUYẾT XUNG ĐỘT) 🔴

> *Mục tiêu: Operational Transformation — khi hai người cùng sửa cùng vị trí, hệ thống tự hoà giải đúng.*

### Những gì cần làm:

| # | Công việc | File liên quan | Trạng thái |
|---|-----------|----------------|------------|
| 6.1 | Thiết kế thuật toán OT (transform insert/delete) | File mới: `Server/Services/OTEngine.cs` | ❌ Chưa làm |
| 6.2 | Server nhận op → transform theo server revision | `OTEngine.cs` + `ClientHandler.cs` | ❌ Chưa làm |
| 6.3 | Server broadcast op đã transform | `ClientHandler.cs` | ❌ Chưa làm |
| 6.4 | Client nhận op broadcast → apply vào text buffer đúng vị trí | `Client/TypeRenderForm.cs` | ❌ Chưa làm |
| 6.5 | Lưu op history vào DB (để replay/debug) | `DocumentOperation` + `DocumentRepository` | ❌ Chưa làm |
| 6.6 | Test xung đột: 2 client sửa cùng vị trí → kết quả nhất quán | Unit test / manual test | ❌ Chưa làm |

---

## 📌 GIAI ĐOẠN 7 — TÍNH NĂNG NÂNG CAO / AI 🔴

> *Mục tiêu: hoàn thiện UX, thêm tính năng giá trị gia tăng.*

### Những gì cần làm:

| # | Công việc | File liên quan | Trạng thái |
|---|-----------|----------------|------------|
| 7.1 | AI gợi ý nội dung (tích hợp API GPT/Gemini) | File mới: `Server/Services/AISuggestionService.cs` | ❌ Chưa làm |
| 7.2 | Hiển thị con trỏ của các thành viên khác (cursor presence) | `Client/TypeRenderForm.cs` + `Packet.cs` | ❌ Chưa làm |
| 7.3 | Export tài liệu ra PDF / HTML | `Client/TypeRenderForm.cs` | ❌ Chưa làm |
| 7.4 | Lịch sử phiên bản — xem & khôi phục | `DocumentVersionRepository` + UI | ❌ Chưa làm |
| 7.5 | Load Balancer / Scale Out Server | Docker / Nginx config | ❌ Chưa làm |
| 7.6 | Upload ảnh vào tài liệu | `Server/Database/Models/Image.cs` + API | ❌ Chưa làm |

---

## 👥 PHÂN CÔNG THEO TỪNG THÀNH VIÊN — TOÀN BỘ DỰ ÁN (GĐ1 → GĐ7)

> Bảng phân công đầy đủ từ **ngày đầu tiên** của dự án đến khi hoàn thành.  
> Các task **✅ Đã xong** ghi nhận để báo cáo. Các task ❌ / ⚠️ là việc cần làm tiếp theo.

---

### 🔵 THÀNH VIÊN A — Backend / Server Core

**Vai trò:** Phụ trách toàn bộ tầng Server — Network, Database, Auth, Session, Business Logic.

| # | Giai đoạn | Công việc | Thời gian | Trạng thái |
|---|-----------|-----------|-----------|------------|
| A-01 | GĐ 1 | Tạo project solution `.sln` với 3 project con (Server, Client, Shared) | Đầu dự án (T0) | ✅ Xong |
| A-02 | GĐ 1 | Tạo `SocketServer` — lắng nghe TCP trên cổng cố định (1.5) | Đầu dự án (T0) | ✅ Xong |
| A-03 | GĐ 1 | Tạo `ClientHandler` — mỗi kết nối một thread riêng (1.6) | Đầu dự án (T0) | ✅ Xong |
| A-04 | GĐ 1 | Kết nối PostgreSQL — `DbConnectionFactory` (1.9) | Đầu dự án (T0) | ✅ Xong |
| A-05 | GĐ 1 | Chạy SQL script tạo toàn bộ tables (1.10) | Đầu dự án (T0) | ✅ Xong |
| A-06 | GĐ 2 | `UserRepository` — CRUD user trong DB (2.1) | Đầu dự án (T0) | ✅ Xong |
| A-07 | GĐ 2 | `AuthService.Register()` — hash password, insert user (2.2) | Đầu dự án (T0) | ✅ Xong |
| A-08 | GĐ 2 | `AuthService.Login()` — verify password, cấp token (2.3) | Đầu dự án (T0) | ✅ Xong |
| A-09 | GĐ 2 | `SessionManager` — lưu token → userId in-memory (2.4) | Đầu dự án (T0) | ✅ Xong |
| A-10 | GĐ 2 | `HandleRegister()` + `HandleLogin()` trong `ClientHandler` (2.5, 2.6) | Đầu dự án (T0) | ✅ Xong |
| A-11 | GĐ 2 | Validate token ở mọi request cần auth (2.10) | Đầu dự án (T0) | ✅ Xong |
| A-12 | GĐ 3 | `HandleDocCreate()`, `HandleDocList()`, `HandleDocOpen()`, `HandleDocSave()` (3.6–3.9) | Đầu dự án (T0) | ✅ Xong |
| A-13 | GĐ 4 | Server nhận `OP_INSERT` / `OP_DELETE`, validate (4.4) | Đầu dự án (T0) | ✅ Xong (chỉ log) |
| **A-14** | **GĐ 4** | **Cập nhật `SessionManager` — map `docId → List<ClientHandler>`** (4.6) | **Tuần 1** (21–27/04) | ❌ Cần làm |
| **A-15** | **GĐ 4** | **Broadcast `OP_BROADCAST` tới tất cả client cùng doc** (4.5) | **Tuần 1** (21–27/04) | ❌ Cần làm |
| A-16 | GĐ 3 | Tích hợp revision tracking vào `HandleDocSave` (3.14) | **Tuần 2** (28/04–04/05) | ❌ Cần làm |
| A-17 | GĐ 5 | Sinh `shareCode` ngẫu nhiên khi `HandleDocCreate()` (5.5) | **Tuần 2** (28/04–04/05) | ❌ Cần làm |
| A-18 | GĐ 5 | `HandleDocShare()` — chia sẻ doc bằng username (5.3) | **Tuần 2** (28/04–04/05) | ❌ Cần làm |
| A-19 | GĐ 5 | `HandleDocJoinCode()` — tham gia doc bằng share code (5.4) | **Tuần 2** (28/04–04/05) | ❌ Cần làm |
| A-20 | GĐ 5 | Fix `HandleDocOpen()` — kiểm tra quyền (không chỉ owner) (5.6) | **Tuần 2** (28/04–04/05) | ❌ Cần làm |
| A-21 | GĐ 6 | Cài đặt `OTEngine.cs` — thuật toán transform insert/delete (6.1, 6.2) | **Tuần 3** (05–11/05) | ❌ Cần làm |
| A-22 | GĐ 6 | Server broadcast op **đã được transform** tới các client (6.3) | **Tuần 3** (05–11/05) | ❌ Cần làm |
| A-23 | GĐ 7 | `AISuggestionService` — tích hợp API GPT/Gemini (7.1) | **Tuần 4+** (12/05–) | ❌ Cần làm |
| A-24 | Kết thúc | Kiểm thử end-to-end toàn bộ server flow | ~19–25/05/2026 | ⏳ Chưa tới |

---

### 🟢 THÀNH VIÊN B — Client / UI / UX

**Vai trò:** Phụ trách toàn bộ tầng Client — WinForms, giao diện, trải nghiệm người dùng, kết nối socket từ phía client.

| # | Giai đoạn | Công việc | Thời gian | Trạng thái |
|---|-----------|-----------|-----------|------------|
| B-01 | GĐ 1 | Tạo `SocketClient` — kết nối TCP tới server (1.7) | Đầu dự án (T0) | ✅ Xong |
| B-02 | GĐ 2 | `LoginForm` — UI đăng nhập (2.7) | Đầu dự án (T0) | ✅ Xong |
| B-03 | GĐ 2 | `RegisterForm` — UI đăng ký (2.8) | Đầu dự án (T0) | ✅ Xong |
| B-04 | GĐ 2 | Client gửi `AUTH_LOGIN` / `AUTH_REGISTER`, nhận response (2.9) | Đầu dự án (T0) | ✅ Xong |
| B-05 | GĐ 3 | `HomeForm` — danh sách tài liệu (3.10) | Đầu dự án (T0) | ✅ Xong |
| B-06 | GĐ 3 | `CreateDocumentForm` — dialog tạo tài liệu mới (3.11) | Đầu dự án (T0) | ✅ Xong |
| B-07 | GĐ 3 | `TypeRenderForm` — editor Markdown + preview live (3.12) | Đầu dự án (T0) | ✅ Xong |
| B-08 | GĐ 4 | Client phát hiện thay đổi text → tính delta `ComputeTextDelta()` (4.1) | Đầu dự án (T0) | ✅ Xong |
| B-09 | GĐ 4 | Batch ops ≤5 ký tự, flush theo timer 250ms `QueueOperation()` (4.2) | Đầu dự án (T0) | ✅ Xong |
| B-10 | GĐ 4 | Client gửi `OP_INSERT` / `OP_DELETE` kèm `clientRevision` (4.3) | Đầu dự án (T0) | ✅ Xong |
| **B-11** | **GĐ 4** | **Handler nhận `OP_BROADCAST` ở `SocketClient`** — điều phối sự kiện (4.7) | **Tuần 1** (21–27/04) | ❌ Cần làm |
| **B-12** | **GĐ 4** | **Apply broadcast op vào textbox** trong `TypeRenderForm` (4.7) | **Tuần 1** (21–27/04) | ❌ Cần làm |
| B-13 | GĐ 4 | Đồng bộ revision hiển thị đúng trên editor (4.8) | **Tuần 1–2** | ❌ Cần làm |
| B-14 | GĐ 5 | UI nhập share code / tên user để join hoặc share doc (5.7) | **Tuần 2** (28/04–04/05) | ❌ Cần làm |
| B-15 | GĐ 5 | Hiển thị badge quyền (owner / editor / viewer) trên editor (5.8) | **Tuần 2** (28/04–04/05) | ❌ Cần làm |
| B-16 | GĐ 6 | Client apply OT op nhận từ broadcast vào text buffer đúng vị trí (6.4) | **Tuần 3** (05–11/05) | ❌ Cần làm |
| B-17 | GĐ 7 | Hiển thị con trỏ các thành viên khác — cursor presence (7.2) | **Tuần 3** (05–11/05) | ❌ Cần làm |
| B-18 | GĐ 7 | Nút Export PDF / HTML (7.3) | **Tuần 4+** (12/05–) | ❌ Cần làm |
| B-19 | GĐ 7 | Giao diện xem & khôi phục lịch sử phiên bản (7.4) | **Tuần 4+** (12/05–) | ❌ Cần làm |
| B-20 | Kết thúc | Tổng hợp UI, fix UX bug, kiểm thử giao diện toàn bộ | ~19–25/05/2026 | ⏳ Chưa tới |

---

### 🟡 THÀNH VIÊN C — Shared / Protocol / OT / Testing

**Vai trò:** Phụ trách tầng Shared (giao thức dùng chung), thiết kế OT data model, tích hợp DB nâng cao, viết test.

| # | Giai đoạn | Công việc | Thời gian | Trạng thái |
|---|-----------|-----------|-----------|------------|
| C-01 | GĐ 1 | Thiết kế giao thức `Packet` (JSON + 4-byte length prefix) (1.1) | Đầu dự án (T0) | ✅ Xong |
| C-02 | GĐ 1 | Định nghĩa `MessageType` enum toàn bộ loại message (1.2) | Đầu dự án (T0) | ✅ Xong |
| C-03 | GĐ 1 | Định nghĩa tất cả `Payload_*` class cho từng packet type (1.3) | Đầu dự án (T0) | ✅ Xong |
| C-04 | GĐ 1 | Cài đặt `PacketHelper.Send()` / `Receive()` (1.4) | Đầu dự án (T0) | ✅ Xong |
| C-05 | GĐ 3 | `Document` model + `DocumentRepository` CRUD (3.1–3.5) | Đầu dự án (T0) | ✅ Xong |
| C-06 | GĐ 5 | `DocumentShare` model + `DocumentShareRepository` (5.1, 5.2) | Đầu dự án (T0) | ✅ Xong |
| C-07 | GĐ 4 | `DocumentOperation` model (đã có, chưa dùng) (4.9) | Đầu dự án (T0) | ⚠️ Model xong |
| C-08 | GĐ 3 | `DocumentVersionRepository` model (đã có, chưa tích hợp) (3.13) | Đầu dự án (T0) | ⚠️ Model xong |
| **C-09** | **GĐ 6** | **Thiết kế dữ liệu OT** — `EditOpItem`, transform spec vào `Packet.cs`| **Tuần 1** (21–27/04) | ❌ Cần làm |
| **C-10** | **GĐ 4** | **Bổ sung `MessageType`** cho cursor presence packet (phục vụ 7.2) | **Tuần 1** (21–27/04) | ❌ Cần làm |
| C-11 | GĐ 4 | Lưu `DocumentOperation` vào DB khi server nhận op (4.9) | **Tuần 2** (28/04–04/05) | ❌ Cần làm |
| C-12 | GĐ 3 | Tích hợp `DocumentVersionRepository` vào flow `HandleDocSave` (3.13) | **Tuần 2** (28/04–04/05) | ❌ Cần làm |
| C-13 | GĐ 6 | Lưu op history vào DB để replay / debug (6.5) | **Tuần 3** (05–11/05) | ❌ Cần làm |
| C-14 | GĐ 6 | Viết unit test OT: 2 ops xung đột → kết quả nhất quán (6.6) | **Tuần 3** (05–11/05) | ❌ Cần làm |
| C-15 | GĐ 7 | Client-side: gọi AI suggestion qua `SocketClient` (7.1 phần client) | **Tuần 4+** (12/05–) | ❌ Cần làm |
| C-16 | GĐ 7 | Thiết kế Load Balancer / Docker Compose Scale-out (7.5) | **Tuần 4+** (12/05–) | ❌ Cần làm |
| C-17 | Kết thúc | Viết báo cáo đồ án, hoàn thiện tài liệu kỹ thuật | ~19–25/05/2026 | ⏳ Chưa tới |

---

## 📅 LỘ TRÌNH TỔNG QUAN — TỪ ĐẦU ĐẾN CUỐI DỰ ÁN

| Giai đoạn | Thời gian | Thành viên A | Thành viên B | Thành viên C |
|-----------|-----------|-------------|-------------|-------------|
| **GĐ 1+2** (✅ Hoàn thành) | T0 (trước 21/04) | SocketServer, ClientHandler, DB, Auth backend | SocketClient, LoginForm, RegisterForm | Packet protocol, MessageType, Payload classes, Models |
| **GĐ 3** (✅ ~85%) | T0 (trước 21/04) | HandleDocCreate/List/Open/Save | HomeForm, CreateDocForm, TypeRenderForm | DocumentRepository, DocumentShare model |
| **GĐ 4** (🔶 ~40%) | **Tuần 1** (21–27/04) | SessionManager docRoom + broadcast OP_BROADCAST | Handler + apply OP_BROADCAST, revision sync | OT data model design, bổ sung MessageType |
| **GĐ 5** (🔴 ~20%) | **Tuần 2** (28/04–04/05) | shareCode, HandleDocShare/JoinCode, fix quyền | UI share code, badge quyền | Lưu DocumentOperation, tích hợp VersionRepository |
| **GĐ 6** (❌ Chưa bắt đầu) | **Tuần 3** (05–11/05) | OTEngine.cs (transform + broadcast transformed) | Apply OT op phía client, cursor presence UI | Lưu op history DB, unit test OT |
| **GĐ 7** (❌ Chưa bắt đầu) | **Tuần 4+** (12/05–) | AI Suggestion Service | Export PDF/HTML, lịch sử phiên bản | Load Balancer, Docker Scale-out, AI client-side |
| **Kiểm thử & Báo cáo** | ~19–25/05/2026 | Kiểm thử end-to-end, bug fix server | Fix UI/UX, kiểm thử giao diện | Viết báo cáo, hoàn thiện tài liệu kỹ thuật |

---

## 🔑 CHI TIẾT KỸ THUẬT CẦN NẮM

### Protocol giao tiếp:
- Mọi message đều dùng class `Packet { Type, Token, Payload }`
- Payload là JSON string, parse bằng `GetPayload<T>()`
- Frame: `[4 bytes length][JSON bytes]` (xem `PacketHelper.cs`)

### Luồng Broadcast (cần hoàn thiện GĐ4):
```
Client A gõ phím
  → TrackRealtimeEditOps() tính delta
  → QueueOperation() gom batch ≤5 ký tự
  → OpFlushTimer (250ms) → FlushPendingEditOperation()
  → SocketClient.SendInsertOps() gửi OP_INSERT
  → Server nhận HandleOpInsert()
  → [CHỖ CẦN LÀM] Broadcast OP_BROADCAST tới tất cả ClientHandler có cùng docId
  → Client B nhận OP_BROADCAST → apply vào textbox
```

### Database tables hiện có:
| Table | Mô tả |
|-------|-------|
| `users` | Thông tin tài khoản |
| `documents` | Meta tài liệu |
| `document_shares` | Phân quyền |
| `document_versions` | Lịch sử phiên bản |
| `document_operations` | Log các ops |
| `active_sessions` | Session (hiện dùng in-memory SessionManager) |

---

*Tài liệu này phản ánh trạng thái code thực tế tính đến 20/04/2026.*

---

## 🔑 CHI TIẾT KỸ THUẬT CẦN NẮM

### Protocol giao tiếp:
- Mọi message đều dùng class `Packet { Type, Token, Payload }`
- Payload là JSON string, parse bằng `GetPayload<T>()`
- Frame: `[4 bytes length][JSON bytes]` (xem `PacketHelper.cs`)

### Luồng Broadcast (cần hoàn thiện GĐ4):
```
Client A gõ phím
  → TrackRealtimeEditOps() tính delta
  → QueueOperation() gom batch ≤5 ký tự
  → OpFlushTimer (250ms) → FlushPendingEditOperation()
  → SocketClient.SendInsertOps() gửi OP_INSERT
  → Server nhận HandleOpInsert()
  → [CHỖ CẦN LÀM] Broadcast OP_BROADCAST tới tất cả ClientHandler có cùng docId
  → Client B nhận OP_BROADCAST → apply vào textbox
```

### Database tables hiện có:
| Table | Mô tả |
|-------|-------|
| `users` | Thông tin tài khoản |
| `documents` | Meta tài liệu |
| `document_shares` | Phân quyền |
| `document_versions` | Lịch sử phiên bản |
| `document_operations` | Log các ops |
| `active_sessions` | Session (hiện dùng in-memory SessionManager) |

---

*Tài liệu này phản ánh trạng thái code thực tế tính đến 20/04/2026.*
