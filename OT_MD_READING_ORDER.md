# Thứ tự đọc tài liệu OT

Đọc theo thứ tự này khi cần hiểu hoặc tiếp tục sửa pipeline OT / realtime editor của MarkTogether:

1. `OT_FIX_PLAN.md`
   - Kế hoạch đợt 1.
   - Nắm các fix nền: `_clientRevision`, transform delete/insert, prewarm `DocumentState`, flush pending trước broadcast, chunk typing 32 ký tự.

2. `OT_FIX_IMPLEMENTATION_REPORT.md`
   - Báo cáo đợt 1 đã implement.
   - Dùng để biết thay đổi nào đã làm thật và build/test đã chạy.

3. `DOCUMENTATION.md`
   - Đọc các mục OT chính:
     - `7.9. Realtime collaboration — Operational Transformation`
     - `10.3. Đồng bộ revision & conflict resolution`
     - `17.2. OT engine — 4 quy tắc transform`
     - `17.3. Tần suất gửi OP từ client`
   - Dùng làm tài liệu tổng quan đang đồng bộ với code sau đợt 2.

4. `OT_FIX_PLAN_V2.md`
   - Kế hoạch đợt 2.
   - Nắm pipeline mới: `TextChanged -> debounce -> ClassifyEditCase -> {Typing|BulkInsert|BulkDelete|ReplaceBlock}`.
   - Nắm giới hạn mới: `TypingMaxCharsPerPacket = 32`, `BulkMaxCharsPerPacket = 8192`, `TypingDebounceMs = 120`.

5. `OT_FIX_IMPLEMENTATION_REPORT_V2.md`
   - Báo cáo đợt 2 đã implement.
   - Kiểm file đã sửa, build command, warning còn lại, ghi chú deploy server trước client.

6. Code liên quan sau khi đọc tài liệu:
   - `MarkTogether.Client/TypeRenderForm.cs`
   - `MarkTogether.Client/Network/SocketClient.cs`
   - `MarkTogether.Server/Network/ClientHandler.cs`
   - `MarkTogether.Server/OT/OTEngine.cs`
   - `MarkTogether.Server/OT/DocumentState.cs`

Lưu ý: không sửa `MarkTogether.Shared/Packet.cs` nếu protocol `OP_INSERT` / `OP_DELETE` với `ops = List<EditOpItem>` vẫn đủ dùng.
