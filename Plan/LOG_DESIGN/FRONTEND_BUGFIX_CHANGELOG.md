# Frontend Bugfix Changelog

**Ngày:** 21/05/2026  
**Phạm vi:** MarkTogether.Client  
**Build:** ✅ Thành công (0 errors, 1 warning không ảnh hưởng)

---

## Tổng quan

Sau khi review toàn bộ code frontend, phát hiện và fix 4 vấn đề (1 nghiêm trọng, 3 trung bình). Ngoài ra ghi nhận 2 vấn đề nhỏ chưa cần fix ngay và 1 vấn đề thiết kế (double OpenDocument) được giữ nguyên vì server xử lý idempotent.

---

## Các vấn đề đã fix

### 1. 🔴 Font GDI Resource Leak

**File:** `MarkTogether.Client/UI/AppTheme.cs`  
**Mức độ:** Nghiêm trọng  

**Vấn đề:**  
Tất cả Font properties (`H1`, `H2`, `Body`, `Caption`, v.v.) sử dụng expression-body (`=>`), tạo một `Font` object mới mỗi lần truy cập mà không bao giờ được Dispose. Font là GDI resource — leak nhiều sẽ gây lỗi "Out of GDI handles" sau khi app chạy lâu.

**Trước:**
```csharp
public static Font H1 => new Font(ResolvedFontFamily, 22f, FontStyle.Bold, GraphicsUnit.Point);
public static Font Body => new Font(ResolvedFontFamily, 10f, FontStyle.Regular, GraphicsUnit.Point);
// ... 13 properties tương tự
```

**Sau:**
```csharp
public static readonly Font H1 = new Font(ResolvedFontFamily, 22f, FontStyle.Bold, GraphicsUnit.Point);
public static readonly Font Body = new Font(ResolvedFontFamily, 10f, FontStyle.Regular, GraphicsUnit.Point);
// ... 13 fields tương tự
```

**Giải thích:** Đổi sang `static readonly` field — Font chỉ được tạo 1 lần duy nhất khi class được load, tồn tại suốt vòng đời app (không cần Dispose vì app-level singleton).

---

### 2. 🟡 Delete Operation Position Bug (Real-time Collaboration)

**File:** `MarkTogether.Client/TypeRenderForm.cs`  
**Mức độ:** Trung bình — gây sai lệch nội dung khi cộng tác real-time  

**Vấn đề:**  
Trong `SendPendingChunksIfNeeded()` và `FlushPendingEditOperation()`, sau khi gửi mỗi chunk, `_pendingOpStartPos` luôn được tăng lên. Điều này đúng cho Insert (vì text mới được chèn, position tiếp theo phải dịch sang phải), nhưng **sai cho Delete** — khi xóa text tại position X, text tiếp theo cần xóa vẫn nằm tại position X (vì text trước đó đã bị xóa).

**Trước:**
```csharp
_pendingOpText = _pendingOpText.Substring(MaxCharsPerPacket);
_pendingOpStartPos += MaxCharsPerPacket; // ← SAI cho delete
```

**Sau:**
```csharp
_pendingOpText = _pendingOpText.Substring(MaxCharsPerPacket);
// Chỉ tăng position cho insert; delete luôn xóa tại cùng vị trí
if (_pendingOpType.Value == PendingOpType.Insert)
    _pendingOpStartPos += MaxCharsPerPacket;
```

**Ảnh hưởng:** Fix này áp dụng cho cả 2 method: `SendPendingChunksIfNeeded()` và `FlushPendingEditOperation()`.

---

### 3. 🟡 Stale Connection Detection

**File:** `MarkTogether.Client/Network/SocketClient.cs`  
**Mức độ:** Trung bình  

**Vấn đề:**  
Nếu server restart hoặc mạng bị ngắt, `_connected` flag vẫn là `true` (chỉ được set `false` khi ReceiveLoop catch exception). Khi user thử login lại, `Connect()` return ngay vì `_connected == true`, nhưng socket thực sự đã chết → mọi request fail.

**Fix:**  
Thêm `IsSocketAlive()` check vào đầu `Connect()`. Nếu socket đã chết → tự `Disconnect()` trước rồi mới connect lại.

```csharp
public void Connect(string host = "localhost", int port = 5000)
{
    // Nếu đã đánh dấu connected nhưng socket thực sự đã chết → dọn dẹp trước
    if (_connected && !IsSocketAlive())
    {
        try { Disconnect(); } catch { }
    }
    if (_connected) return;
    // ... connect bình thường
}

private bool IsSocketAlive()
{
    try
    {
        if (_tcp == null || _tcp.Client == null || !_tcp.Client.Connected) return false;
        bool readable = _tcp.Client.Poll(0, SelectMode.SelectRead);
        if (readable && _tcp.Client.Available == 0) return false; // peer đã đóng
        return true;
    }
    catch { return false; }
}
```

---

### 4. 🟢 RegisterForm Connect Conflict

**File:** `MarkTogether.Client/RegisterForm.cs`  
**Mức độ:** Thấp  

**Vấn đề:**  
`RegisterForm` gọi `Connect("localhost", 5000)` riêng. Nếu `LoginForm` đã connect trước đó (user click login fail), singleton `SocketClient.Instance` có thể ở trạng thái conflict.

**Fix:**  
Thêm disconnect trước khi connect:

```csharp
if (SocketClient.Instance.IsLoggedIn)
    SocketClient.Instance.Disconnect();
SocketClient.Instance.Connect("localhost", 5000);
```

---

## Vấn đề ghi nhận nhưng chưa fix

### Double OpenDocument Call

**File:** `TypeRenderForm.cs` dòng 173-196  
**Lý do giữ nguyên:** Server xử lý `DOC_OPEN` idempotent (join room lần 2 không tạo duplicate). Lần gọi thứ 2 trong `TypeRenderForm_Shown` cần thiết để lấy `permission` và `shareCode` — thông tin này không được truyền qua constructor.

### Không xử lý OnConnectionLost event

Event `OnConnectionLost` được raise nhưng không có form nào subscribe để thông báo user. Đây là feature cần thêm sau (reconnect UI, toast notification).

### Potential Deadlock Pattern

`SocketClient.Request()` dùng `.GetAwaiter().GetResult()` — nếu gọi trực tiếp từ UI thread sẽ deadlock. Hiện tại tất cả form đều wrap bằng `Task.Run()` nên chưa bị. Cần document rõ hoặc đổi sang full async pattern trong tương lai.

---

---

## Lần 2 — 21/05/2026 (chiều)

### 5. 🟡 Xóa chức năng Avatar

**Files:**
- `MarkTogether.Client/HomeForm.Designer.cs` — Xóa `btnAvatar` khỏi controls và field declarations
- `MarkTogether.Client/HomeForm.cs` — Xóa `btnAvatar_Click`, `GuessMimeType`, `StyleSecondaryButton(btnAvatar)`, `btnAvatar.Enabled`, layout code cho btnAvatar

**Lý do:** Chức năng avatar chưa cần thiết, gây rối UI.

---

### 6. 🟡 Fix viền input bị mất nét (LoginForm, RegisterForm, CreateDocumentForm)

**Files:**
- `MarkTogether.Client/UI/UiFactory.cs` — Thêm method `StyleInputPanel(Panel)`
- `MarkTogether.Client/LoginForm.cs` — Thay `StyleAsCard(pnlUsername/pnlPassword)` bằng `StyleInputPanel`
- `MarkTogether.Client/RegisterForm.cs` — Thay `StyleAsCard(pnlUsername/pnlEmail/pnlPassword/pnlConfirmPassword)` bằng `StyleInputPanel`
- `MarkTogether.Client/CreateDocumentForm.cs` — Thay `StyleAsCard(pnlTitle)` bằng `StyleInputPanel`

**Vấn đề:**
Trước đó, input panels (Panel wrapper cho TextBox) dùng `StyleAsCard` → gọi `ApplyRoundedRegion` → set `Region` = rounded rectangle. Khi `Region` clip control, border 1px được vẽ trong Paint event bị cắt ở các góc bo, gây ra hiện tượng viền mất nét, không rõ ràng.

**Fix:**
Thêm method mới `UiFactory.StyleInputPanel(Panel)` chỉ set `BackColor` và vẽ border trong `Paint` event mà **KHÔNG clip Region**. Border vẫn được vẽ bo góc (rounded) nhưng control không bị cắt pixel → viền hiển thị đầy đủ, sắc nét.

```csharp
public static void StyleInputPanel(Panel panel)
{
    panel.BackColor = AppTheme.Surface;
    panel.Paint += (s, e) =>
    {
        DrawBorder(e.Graphics, panel.ClientRectangle, AppTheme.Border, AppTheme.CornerRadius);
    };
}
```

**Lưu ý:** `WireInputFocus` trong các form vẫn thêm Paint handler để đổi màu border khi focus (xanh) / blur (xám). Hai Paint handler cùng chạy — cái sau (WireInputFocus) sẽ vẽ đè lên cái trước, nên focus highlight vẫn hoạt động bình thường.

---

## Warning còn lại

```
TypeRenderForm.Designer.cs(125,17): warning CS0219: The variable 'rightStart' is assigned but its value is never used
```

Biến `rightStart` trong Designer file không ảnh hưởng runtime, có thể xóa khi refactor UI.
