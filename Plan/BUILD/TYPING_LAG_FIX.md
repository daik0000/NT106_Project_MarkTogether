# Fix: Typing lag trên editor (gõ văn bản bị giật)

> Phiên bản: 1.0 — 2026-05-22
> Đối tượng đọc: Dev / AI maintain sau này.

---

## 1. Triệu chứng

Khi gõ văn bản liên tục trong `TypeRenderForm` (editor), UI bị giật hoặc treo nhẹ sau mỗi 5 ký tự. Trên kết nối **localhost** gần như không cảm nhận được (RTT ~1 ms). Trên **VPS** hoặc mạng LAN có latency cao (RTT 50–200 ms), giật rất rõ: mỗi 5 ký tự gõ ra UI freeze ~50–200 ms.

---

## 2. Nguyên nhân gốc rễ

### Path typing trước khi fix:

```
User gõ ký tự
  → txtRawMarkdown_TextChanged          (UI thread)
  → TrackRealtimeEditOps                 (UI thread)
  → QueueOperation → MergeIntoPending    (UI thread)
  → SendPendingChunksIfNeeded            (UI thread)
  → [khi _pendingOpText.Length >= 5]:
      SendCurrentPendingChunk(chunk)
      → SocketClient.SendInsertOps(...)  ← ĐỒNG BỘ, BLOCK UI THREAD
      → Request(..., timeoutMs: 5000)    ← đợi ACK từ server
```

**File:dòng:** `TypeRenderForm.cs:700–708` — `SocketClient.SendInsertOps/SendDeleteOps` được gọi trực tiếp trên UI thread, block đến khi server reply.

**Hệ quả trên VPS (RTT 50 ms):**
- Gõ 5 ký tự → block UI 50 ms
- Gõ 10 ký tự → 2 lần block = 100 ms
- Gõ liên tục → UI gần như luôn pending → cảm giác "giật lag" liên tục

### Tại sao chỉ paste được fix trước mà không fix typing?

Paste optimization (plan trước) đã đẩy **paste** qua background queue (`_pasteChunkQueue` + `PasteSenderLoop`). Khi `_isPasting=true`, code ở `SendCurrentPendingChunk` redirect typing op vào queue. Nhưng khi **không paste**, typing vẫn rơi vào nhánh đồng bộ cũ.

---

## 3. Giải pháp

**Ý tưởng:** Dùng cùng infrastructure background sender đã có cho paste — **mọi op (typing + paste) đều enqueue, không có gì gửi đồng bộ trên UI thread nữa.**

Chỉ sửa 3 chỗ trong `MarkTogether.Client/TypeRenderForm.cs`, không sửa `SocketClient.cs` hay `ClientHandler.cs`.

---

## 4. Thay đổi chi tiết

### 4.1. `_isPasting` thêm `volatile` (dòng 49)

**Trước:**
```csharp
private bool _isPasting;
```

**Sau:**
```csharp
private volatile bool _isPasting;
```

**Lý do:** `_isPasting` được ghi trên background thread (`PasteSenderLoop`) và đọc trên UI thread (`SendCurrentPendingChunk`). Không có `volatile`, CPU có thể cache giá trị cũ → UI thread thấy trạng thái sai.

---

### 4.2. `SendCurrentPendingChunk` — luôn enqueue thay vì gửi đồng bộ (dòng 675)

**Trước (~35 dòng):**
```csharp
private void SendCurrentPendingChunk(string chunk)
{
    if (!_pendingOpType.HasValue || ...) return;

    if (_isPasting)
    {
        lock (_pasteLock)
        {
            char op = ...;
            _pasteChunkQueue.Enqueue($"{op}|{_pendingOpStartPos}|{chunk}");
        }
        EnsurePasteSenderRunning();
        return;
    }

    // ← ĐÂY LÀ NGUỒN LAG: block UI thread đồng bộ
    SocketClient.Instance.SendInsertOps(_docId, _clientRevision, ops);
    Interlocked.Increment(ref _clientRevision);
}
```

**Sau (~10 dòng):**
```csharp
private void SendCurrentPendingChunk(string chunk)
{
    if (!_pendingOpType.HasValue || string.IsNullOrEmpty(chunk) || string.IsNullOrWhiteSpace(_docId)) return;
    if (!_trackRealtimeOps || !SocketClient.Instance.IsLoggedIn) return;
    if (_permission != "owner" && _permission != "editor") return;

    lock (_pasteLock)
    {
        char op = _pendingOpType.Value == PendingOpType.Insert ? 'I' : 'D';
        _pasteChunkQueue.Enqueue($"{op}|{_pendingOpStartPos}|{chunk}");
    }
    EnsurePasteSenderRunning();
}
```

**Tại sao an toàn:**
- `_pasteChunkQueue` là queue tuần tự → thứ tự enqueue = thứ tự gửi → revision đơn điệu.
- `Interlocked.Increment(ref _clientRevision)` vẫn nằm trong `PasteSenderLoop` sau mỗi ACK.
- `EnsurePasteSenderRunning` là idempotent (không tạo task mới nếu task cũ còn chạy).
- Guard `_trackRealtimeOps` + `IsLoggedIn` + `_permission`: các điều kiện này đã được check ở `TrackRealtimeEditOps` trước khi `QueueOperation` được gọi. Thêm vào đây là defensive để an toàn khi `FlushPendingEditOperation` được gọi từ các nơi khác (e.g. `OnFormClosing`).

---

### 4.3. `OnFormClosing` — flush trước, wait sau (dòng 1537)

**Trước:**
```csharp
protected override void OnFormClosing(FormClosingEventArgs e)
{
    // Wait paste sender (nhưng typing flush chưa xảy ra!)
    try { var task = _pasteSendTask; if (task != null && !task.IsCompleted) task.Wait(3s); }
    catch { }

    FlushPendingEditOperation();  // ← đẩy vào queue SAU KHI đã wait xong → không được gửi
    ...
}
```

**Sau:**
```csharp
protected override void OnFormClosing(FormClosingEventArgs e)
{
    // Flush trước để đẩy chunk typing cuối vào queue
    FlushPendingEditOperation();

    // Wait để background sender gửi nốt (cả typing + paste)
    try { var task = _pasteSendTask; if (task != null && !task.IsCompleted) task.Wait(3s); }
    catch { }
    ...
}
```

**Tại sao quan trọng:** Sau fix 4.2, `FlushPendingEditOperation` không còn gửi đồng bộ mà enqueue. Nếu wait trước flush thì chunk cuối bị bỏ (task đã finish trước khi chunk được enqueue). Đảo thứ tự: flush → enqueue → start task → wait → task drain → form đóng.

---

## 5. Tính chất sau fix

| Scenario | Trước fix | Sau fix |
|----------|-----------|---------|
| Gõ 5 ký tự (VPS 100 ms RTT) | Block UI ~100 ms | UI responsive ngay (<1 ms) |
| Gõ 100 ký tự liên tục | Block UI ~2 s tổng cộng | UI responsive, sender drain ~2 s nền |
| Paste 5000 ký tự | UI responsive (đã fix trước) | Vẫn responsive |
| Đóng form đang gõ dở | Flush đồng bộ rồi đóng | Flush async, wait ≤ 3 s, đóng |
| Multi-user OT | Đúng | Vẫn đúng: sender tuần tự, revision tăng sau ACK |

---

## 6. Những gì KHÔNG thay đổi

- `SocketClient.cs` — không sửa
- `ClientHandler.cs` — không sửa
- Giới hạn 5 ký tự/packet của server — không tăng
- OT correctness — revision vẫn tăng đơn điệu trong `PasteSenderLoop`
- Paste path — hoàn toàn giữ nguyên (`PerformFastPaste` → `EnqueuePasteOperations` → sender)

---

## 7. Optional follow-up (ngoài scope fix này)

- **Nâng `MaxCharsPerPacket` từ 5 → 100–256:** sửa `ClientHandler.cs:1063` và `TypeRenderForm.cs:54`. Giảm số round-trip ~20–50× khi gõ nhanh / paste. Cần test OT engine với text op dài hơn.
- **Rename `_pasteChunkQueue` → `_opSendQueue`:** phản ánh đúng hơn rằng queue này phục vụ cả typing + paste.

---

## 8. File tham chiếu

| Khái niệm | File:dòng |
|-----------|-----------|
| `SendCurrentPendingChunk` (đã fix) | `TypeRenderForm.cs:675` |
| `PasteSenderLoop` (xử lý queue) | `TypeRenderForm.cs:1724` |
| `EnsurePasteSenderRunning` | `TypeRenderForm.cs:1713` |
| `FlushPendingEditOperation` | `TypeRenderForm.cs:658` |
| `OnFormClosing` (thứ tự flush/wait) | `TypeRenderForm.cs:1537` |
| `_isPasting volatile` | `TypeRenderForm.cs:49` |
