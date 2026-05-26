# Báo cáo thực thi OT_FIX_PLAN_V2

Ngày thực hiện: 2026-05-26

## Phạm vi đã hoàn thành

Đã triển khai đợt 2 theo `OT_FIX_PLAN_V2.md`:

1. Phase A log/verify instrumentation.
   - File: `MarkTogether.Client/TypeRenderForm.cs`
   - Thêm log metadata cho `TextChanged`, delta, queue/flush/enqueue, sender loop và broadcast apply.
   - Không log nội dung text.

2. Server accept bulk packet trước client bulk.
   - File: `MarkTogether.Server/Network/ClientHandler.cs`
   - `BulkMaxCharsPerPacket = 8192`.
   - `MaxOpsPerPacket = 64`.
   - Batch broadcast `transformedOps` trong một `OP_BROADCAST`.

3. Client tách pipeline theo edit case.
   - File: `MarkTogether.Client/TypeRenderForm.cs`
   - Thêm `EditCase`, `ClassifyEditCase`, `EnqueueBulkOp`.
   - `TypingMaxCharsPerPacket = 32`.
   - `BulkMaxCharsPerPacket = 8192`.
   - `TypingDebounceMs = 120`.
   - `BulkClassifyThreshold = 64`.

4. Typing debounce cho IME tiếng Việt.
   - `TextChanged` tạo baseline đầu burst và flush sau 120 ms.
   - Bulk delta lớn được flush ngay, không đợi debounce.
   - `OnFormClosing` flush debounce trước khi flush pending queue.

5. Bulk insert/delete/replace block.
   - Paste dài qua `PerformFastPaste` dùng `EnqueueBulkOp`.
   - Xóa block và paste/replace qua `TextChanged` dùng bulk path.
   - Replace block enqueue delete trước, insert sau.

6. Remote apply optimization.
   - `HandleOpBroadcast` dùng `currentLen` thay vì đọc `txtRawMarkdown.Text` mỗi op.
   - Vẫn dùng `SelectedText` để tránh reset scroll.
   - `_lastMarkdownText` chỉ sync một lần cuối.

7. OT timeout client.
   - File: `MarkTogether.Client/Network/SocketClient.cs`
   - `SendInsertOps` / `SendDeleteOps` timeout tăng từ 5000 ms lên 15000 ms.

8. Hotfix revision/index lệch khi gõ realtime.
   - File: `MarkTogether.Client/TypeRenderForm.cs`, `MarkTogether.Client/HomeForm.cs`, `MarkTogether.Client/Network/SocketClient.cs`
   - `TypeRenderForm` nhận revision ban đầu từ `DOC_OPEN` / create / join-code.
   - Sender dùng server revision trả về trong `OK.Message` thay vì tự `_clientRevision++`.
   - `HandleOpBroadcast` flush và đợi local sender drain ngắn trước khi apply remote op, tránh queued local op dùng revision mới nhưng position cũ.

## File đã sửa

- `MarkTogether.Client/TypeRenderForm.cs`
- `MarkTogether.Client/HomeForm.cs`
- `MarkTogether.Client/Network/SocketClient.cs`
- `MarkTogether.Server/Network/ClientHandler.cs`
- `DOCUMENTATION.md`
- `OT_FIX_PLAN.md`
- `OT_FIX_IMPLEMENTATION_REPORT_V2.md`

Không sửa `MarkTogether.Shared/Packet.cs`; protocol hiện có `ops = List<EditOpItem>` đủ dùng.

## Build

Đã chạy:

```powershell
dotnet msbuild MarkTogether.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /v:minimal
dotnet msbuild MarkTogether.sln /t:Rebuild /p:Configuration=Debug /p:Platform="Any CPU" /v:minimal
```

Kết quả: build thành công cho `Shared`, `Gateway`, `Server`, `Client`.

Warning còn lại:

- `TypeRenderForm.cs`: field `_isPasting` assigned but never used.

## Kiểm thử

Đã kiểm thực tế trong phiên này:

- Build full solution sau server change.
- Build full solution sau client change.
- Build full solution sau chỉnh tài liệu/report.

Chưa chạy được trong phiên này vì cần 2 client + server + tài khoản/document thực:

- 2 client cùng mở 1 document.
- Gõ tiếng Anh thường.
- Gõ tiếng Việt Telex/VNI.
- Paste 200 ký tự.
- Paste 1000 ký tự.
- Xóa select 500 ký tự.
- Replace block bằng paste.
- 2 user cùng gõ tiếng Việt 30 giây.
- Đóng form khi còn pending op.
- Kiểm log không có `SendChunk FAIL` liên tục và editor không freeze.

## Ghi chú triển khai

Deploy server trước client:

1. Server mới nhận packet bulk tối đa 8192 ký tự và vẫn nhận client cũ gửi ≤ 32 ký tự.
2. Client mới chỉ nên deploy sau khi server mới đã chạy.
3. Nếu rollback, rollback client trước rồi server sau.

Không cần migration DB vì `document_operations.text` là PostgreSQL `TEXT`.

## Addendum hotfix viewport broadcast

Ngay sau report V2, client được bổ sung fix cho lỗi user đang ở vùng trên bị kéo xuống vùng edit của user khác khi nhận `OP_BROADCAST`.

- File: `MarkTogether.Client/TypeRenderForm.cs`
- `HandleOpBroadcast` lưu first visible line bằng `EM_GETFIRSTVISIBLELINE` trước khi apply remote op bằng `SelectedText`.
- Sau khi restore caret, client gọi `EM_LINESCROLL` để đưa viewport về line đang nhìn.
- Mục tiêu: giữ viewport của người nhận ổn định khi remote edit xảy ra ở vùng khác trong tài liệu.

Build kiểm tra:

```powershell
dotnet msbuild MarkTogether.Client/Client.csproj /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /p:OutputPath=bin\DebugScrollCheck\ /v:minimal
```

Kết quả: build client sang output tạm thành công. Full solution build bị chặn ở bước copy `bin\Debug\MarkTogether.Client.exe` vì các process `MarkTogether.Client` đang chạy lock file.
