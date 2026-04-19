# Hướng dẫn triển khai **Main Page sau Login** (MVP)

> Phạm vi theo yêu cầu:
> - ✅ Có **trang chủ** hiển thị danh sách tài liệu hiện có + nút tạo tài liệu mới.
> - ✅ Nút tạo tài liệu mới mở popup nhập `title`, tạo xong chuyển sang form tài liệu đó.
> - ✅ Chọn tài liệu thì lấy `content` từ DB và render lên UI.
> - ❌ **Không đổi schema DB**.
> - ❌ **Chưa làm multi-user** (`document_shares`), OT, đồng soạn thảo realtime.

---

## 1) Mục tiêu chức năng (contract rõ ràng)

### Input
- Người dùng đã login thành công (đã có `Token`, `UserId`, `Username` trong `SocketClient.Instance`).

### Output
1. Sau login vào `HomeForm`.
2. `HomeForm` load danh sách tài liệu của user từ server/DB.
3. Bấm **Tạo tài liệu mới**:
   - mở popup nhập `title`
   - tạo document trong DB
   - mở `TypeRenderForm` của document vừa tạo.
4. Chọn 1 document:
   - lấy `content` từ DB qua server
   - render vào `TypeRenderForm`.

### Error modes cần xử lý
- Mất kết nối server.
- Token không hợp lệ/hết session.
- Tài liệu không tồn tại hoặc không thuộc user.
- Dữ liệu trả về null/empty.

---

## 2) Lưu ý quan trọng trước khi code

## ⚠️ Đồng bộ kiểu `docID`

Hiện tại:
- Schema DB `documents.id` là `VARCHAR(50)` (vd: `doc_xxx`).
- Một số payload trong `MarkTogether.Shared/Packet.cs` đang dùng `int docID`.

Để không đổi schema, bạn nên **chuẩn hóa `docID` sang `string` ở Shared + Client + Server** cho các message liên quan document:
- `Payload_DOC_CREATE_Response.docID`
- `Payload_DOC_OPEN_Request.docID`
- `Payload_DOC_OPEN_Response.docID`
- `DocInfo.docID`
- (các payload OT để sau, tạm chưa dùng)

> Nếu không chuẩn hóa ngay, bạn sẽ gặp lỗi mapping/deserialize khi trả id kiểu `doc_xxx` từ DB.

---

## 3) Thiết kế luồng tổng quát

```text
LoginForm (login OK)
  -> HomeForm
      -> DOC_LIST (load list)
      -> [Create New]
           -> Popup nhập title
           -> DOC_CREATE
           -> mở TypeRenderForm(doc mới)
      -> [Click tài liệu]
           -> DOC_OPEN(docID)
           -> mở TypeRenderForm(doc đã có content)
```

---

## 4) Kế hoạch file cần tạo/sửa

## `MarkTogether.Client`
- **Tạo mới**
  - `HomeForm.cs`
  - `HomeForm.Designer.cs`
  - `CreateDocumentForm.cs`
  - `CreateDocumentForm.Designer.cs`
- **Sửa**
  - `LoginForm.cs` (login xong mở `HomeForm` thay vì mở thẳng `TypeRenderForm`)
  - `Network/SocketClient.cs` (thêm hàm gọi DOC_LIST, DOC_CREATE, DOC_OPEN)
  - `TypeRenderForm.cs` (thêm constructor nhận dữ liệu doc mở từ Home)
  - `Client.csproj` (include form mới)

## `MarkTogether.Server`
- **Sửa**
  - `Network/ClientHandler.cs` (thêm case: `DOC_LIST`, `DOC_CREATE`, `DOC_OPEN`)
  - `Database/Repositories/DocumentRepository.cs` (đã có nhiều hàm; kiểm tra/mở rộng nhỏ nếu cần)

## `MarkTogether.Shared`
- **Sửa**
  - `Packet.cs` (chuẩn hóa `docID` kiểu `string`, payload cho list/open/create)

---

## 5) Step-by-step triển khai chi tiết

## Bước 1 — Chuẩn hóa contract packet (Shared)

**File:** `MarkTogether.Shared/Packet.cs`

1. Đổi các trường `docID` từ `int` -> `string` cho DOC flow.
2. Đảm bảo các payload MVP có đủ field:

- `Payload_DOC_CREATE_Request`
  - `string title`
- `Payload_DOC_CREATE_Response`
  - `string docID`
  - `string title`
  - `string content`
  - `int revision`
- `Payload_DOC_LIST_Response`
  - `List<DocInfo> documents`
- `DocInfo`
  - `string docID`
  - `string title`
  - `string permission`
  - `DateTime updateAt`
- `Payload_DOC_OPEN_Request`
  - `string docID`
- `Payload_DOC_OPEN_Response`
  - `string docID`
  - `string title`
  - `string content`
  - `int revision`
  - `string permission`

> Chưa đụng tới OT/share payload trong phase này (chỉ sửa nếu cần compile).

---

## Bước 2 — Thêm API-level methods ở `SocketClient`

**File:** `MarkTogether.Client/Network/SocketClient.cs`

Thêm 3 method đồng bộ cho MVP:

1. `Payload_DOC_LIST_Response GetDocuments()`
2. `Payload_DOC_CREATE_Response CreateDocument(string title)`
3. `Payload_DOC_OPEN_Response OpenDocument(string docId)`

### Rule gọi packet
- Luôn set `packet.Token = Token` cho các request sau login.
- Kiểm tra `response.Type`:
  - Nếu `ERROR` -> lấy `Payload_ERROR.Message` và ném exception/return lỗi.
  - Nếu đúng type mong muốn -> deserialize payload tương ứng.

---

## Bước 3 — Server xử lý DOC_LIST / DOC_CREATE / DOC_OPEN

**File:** `MarkTogether.Server/Network/ClientHandler.cs`

### 3.1 Add `switch` cases
- `MessageType.DOC_LIST`
- `MessageType.DOC_CREATE`
- `MessageType.DOC_OPEN`

### 3.2 Add handlers

#### `HandleDocList(Packet packet)`
- Xác thực user từ token/session:
  - ưu tiên `packet.Token` hoặc dùng `_userId` của connection.
- Gọi `DocumentRepository.GetByUserId(userId)`.
- Map sang `Payload_DOC_LIST_Response`.
  - `docID = d.Id`
  - `title = d.Title`
  - `permission = "owner"` (phase MVP)
  - `updateAt = d.UpdatedAt`
- Trả packet `DOC_LIST`.

#### `HandleDocCreate(Packet packet)`
- Parse `Payload_DOC_CREATE_Request`.
- Validate `title` (nếu rỗng -> gán mặc định `"Tài liệu không tiêu đề"`).
- Tạo `Document` mới:
  - `OwnerId = userId`
  - `Title = title`
  - `Content = ""`
- Gọi `DocumentRepository.Create(doc)` -> nhận `docId` kiểu string.
- Trả `Payload_DOC_CREATE_Response`.

#### `HandleDocOpen(Packet packet)`
- Parse `Payload_DOC_OPEN_Request` lấy `docID`.
- `DocumentRepository.GetById(docId)`.
- Check quyền tối thiểu MVP:
  - `doc.OwnerId == currentUserId` (chưa xử lý shares theo yêu cầu).
- Trả `Payload_DOC_OPEN_Response` gồm `content`.

---

## Bước 4 — Tạo `HomeForm` (main page sau login)

**File mới:** `MarkTogether.Client/HomeForm.cs`, `HomeForm.Designer.cs`

### UI đề xuất
- Header: `Xin chào <username>`.
- Button: `btnCreateDocument` ("+ Tạo tài liệu mới").
- List/Grid hiển thị docs:
  - Có thể dùng `ListView` (columns: Title, UpdatedAt)
  - Hoặc `DataGridView`.
- Optional: `btnRefresh`.

### Logic
- Event `HomeForm_Shown`:
  - gọi `LoadDocumentsAsync()`.
- `LoadDocumentsAsync()`:
  - gọi `SocketClient.Instance.GetDocuments()`
  - bind data vào list.
- Double click item:
  - lấy `docID`
  - gọi `OpenDocument(docID)`
  - mở `TypeRenderForm` với title/content.

---

## Bước 5 — Tạo popup `CreateDocumentForm`

**File mới:** `MarkTogether.Client/CreateDocumentForm.cs`, `CreateDocumentForm.Designer.cs`

### UI tối thiểu
- `TextBox txtTitle`
- `Button btnCreate`
- `Button btnCancel`

### Contract
- Khi user bấm Create và title hợp lệ:
  - set property `DocumentTitle`.
  - `DialogResult = OK`.

### Gọi từ HomeForm

Pseudo flow:
1. `using var dlg = new CreateDocumentForm();`
2. `if (dlg.ShowDialog(this) == DialogResult.OK)`:
   - `var created = SocketClient.Instance.CreateDocument(dlg.DocumentTitle)`
   - refresh list
   - mở `TypeRenderForm(created.docID, created.title, created.content)`

---

## Bước 6 — Chỉnh `TypeRenderForm` để mở được doc từ Home

**File:** `MarkTogether.Client/TypeRenderForm.cs`

Thêm constructor overload:

- `TypeRenderForm()` (giữ nguyên demo/default)
- `TypeRenderForm(string docId, string title, string initialContent)`
  - set private fields `_docId`, `_docTitle`
  - set `Text = $"MarkTogether - {title}"`
  - set `txtRawMarkdown.Text = initialContent ?? string.Empty`
  - gọi `RenderMarkdown()`

> Giai đoạn này chỉ cần **mở + render nội dung** theo yêu cầu.
> Chức năng save edit có thể thêm ở phase sau.

---

## Bước 7 — Đổi điều hướng sau login

**File:** `MarkTogether.Client/LoginForm.cs`

Thay vì:
- mở `TypeRenderForm` ngay

Đổi thành:
- mở `HomeForm`.

Và xử lý `FormClosed`:
- đóng socket khi app thoát hẳn (`SocketClient.Instance.Disconnect()`).

---

## Bước 8 — Cập nhật `.csproj`

**File:** `MarkTogether.Client/Client.csproj`

Add include cho form mới:
- `HomeForm.cs`, `HomeForm.Designer.cs`
- `CreateDocumentForm.cs`, `CreateDocumentForm.Designer.cs`

Kiểm tra `DependentUpon` đúng để Designer hoạt động.

---

## Bước 9 — Test checklist (manual)

## Functional test
1. Login thành công -> vào `HomeForm`.
2. `HomeForm` load được danh sách docs.
3. Bấm tạo mới -> popup hiện.
4. Nhập title, Create -> mở đúng doc mới, preview render.
5. Quay lại home, chọn 1 doc cũ -> mở đúng content DB.

## Error test
1. Tắt server -> thao tác trên HomeForm báo lỗi thân thiện.
2. Token null/hết session -> server trả lỗi, client hiện message.
3. Mở doc không thuộc user -> báo `Không có quyền truy cập`.

## Data test (PostgreSQL)
- Sau create, có row mới trong `documents`.
- `owner_id` khớp user login.
- `title/content` khớp dữ liệu gửi.

---

## 6) Gợi ý thứ tự commit (để dễ rollback)

1. `feat(shared): normalize document payload contract (docID as string)`
2. `feat(server): implement DOC_LIST DOC_CREATE DOC_OPEN handlers`
3. `feat(client): add socket methods for document APIs`
4. `feat(client): add HomeForm + CreateDocumentForm`
5. `feat(client): route Login -> HomeForm and open TypeRenderForm from home`

---

## 7) Mốc mở rộng sau khi MVP chạy ổn

- Save document content (manual save / auto save debounce).
- Search + sort + paging ở HomeForm.
- Delete/Rename document.
- Share doc + permission (`document_shares`).
- OT/realtime collaboration.

---

## 8) Tóm tắt ngắn

Bạn có thể hoàn thành yêu cầu hiện tại bằng 3 luồng packet chính:
- `DOC_LIST` để đổ danh sách trang chủ
- `DOC_CREATE` để tạo từ popup title
- `DOC_OPEN` để lấy content và render

Toàn bộ làm được mà **không cần đổi schema DB**. Trọng tâm kỹ thuật là chuẩn hóa `docID` sang `string` để khớp với `documents.id` hiện tại.
