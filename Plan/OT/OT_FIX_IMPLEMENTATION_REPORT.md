# Báo cáo thực thi OT_FIX_PLAN

Ngày thực hiện: 2026-05-25

## Phạm vi đã hoàn thành

Đã thực thi 6 mục trong `OT_FIX_PLAN.md` cho realtime sync / Operational Transformation:

1. Client cập nhật `_clientRevision` khi nhận `OP_BROADCAST`.
   - File: `MarkTogether.Client/TypeRenderForm.cs`
   - Cập nhật revision theo `broadcast.clientResivion` bằng thao tác atomic, chỉ tăng monotonically.

2. Server xử lý `TransformDeleteAgainstInsert` khi insert nằm giữa range delete.
   - File: `MarkTogether.Server/OT/OTEngine.cs`
   - Nếu insert nằm trong vùng delete, delete op được rút ngắn để không xóa text vừa được user khác chèn.

3. Server dùng revision cục bộ cho nhiều op trong cùng packet.
   - File: `MarkTogether.Server/Network/ClientHandler.cs`
   - Thêm `currentClientRev`, cập nhật bằng `state.ServerRevision` sau mỗi `TransformAndApply`.

4. Prewarm `DocumentState` khi mở document.
   - File: `MarkTogether.Server/Network/ClientHandler.cs`
   - Gọi `DocumentStateManager.GetOrCreate(docId)` ngay sau `SessionManager.JoinRoom`.

5. Tăng giới hạn ký tự mỗi packet từ 5 lên 32.
   - Client: `MaxCharsPerPacket = 32`.
   - Server: reject khi `charCount > 32` và cập nhật message lỗi tương ứng.

6. Flush pending ops trước khi áp `OP_BROADCAST`.
   - File: `MarkTogether.Client/TypeRenderForm.cs`
   - Gọi `FlushPendingEditOperation()` ở đầu `BeginInvoke` trước `_suppressOpTracking = true`.

7. Bổ sung fix client về permission khi mở editor.
   - File: `MarkTogether.Client/HomeForm.cs`, `MarkTogether.Client/TypeRenderForm.cs`
   - `HomeForm` truyền permission vào `TypeRenderForm` ngay khi mở tài liệu để editor không bị mặc định `viewer` trong khoảng ngắn trước khi `Shown -> OpenDocument()` chạy lại.
   - Thêm log `[OT] Pending chunk skipped: ...` khi client không enqueue typing op vì thiếu state, chưa login/tracking tắt, hoặc permission không phải `owner/editor`.

## Tài liệu đã cập nhật

Đã cập nhật `DOCUMENTATION.md`:

- Mục `7.9`: giới hạn chunk 32 ký tự, flush trước broadcast, đồng bộ `_clientRevision`.
- Mục `7.9`: bổ sung luồng truyền permission sớm vào editor và log skip khi client không gửi typing op.
- Mục `10.3`: mô tả client cập nhật revision theo `broadcast.clientResivion`.
- Phụ lục OT: cập nhật `Delete vs Insert` đã xử lý case insert nằm trong delete range.
- Mục sender/chunking: cập nhật công thức và mô tả từ 5 lên 32 ký tự.

## Kiểm chứng

Đã chạy build:

```powershell
dotnet msbuild MarkTogether.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /v:minimal
```

Kết quả: build thành công cho `Shared`, `Gateway`, `Server`, `Client`.

Warning còn lại:

- `TypeRenderForm.Designer.cs`: biến `rightStart` assigned but unused.
- `TypeRenderForm.cs`: field `_isPasting` assigned but unused.

Hai warning này đã tồn tại ngoài phạm vi sửa OT hiện tại và không chặn build.

## Ghi chú triển khai

Nên deploy server trước client để server mới chấp nhận packet tối đa 32 ký tự. Client cũ vẫn tương thích với server mới vì chỉ gửi tối đa 5 ký tự mỗi packet.
