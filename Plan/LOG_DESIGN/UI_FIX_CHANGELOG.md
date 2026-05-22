# UI FIX — CHANGELOG

> **Ngày**: 21/05/2026  
> **Phạm vi**: Sửa lỗi bể giao diện + nút bấm vô hình trên HomeForm (Dashboard).  
> **Build trước fix**: có thể build nhưng giao diện sai.  
> **Build sau fix**: ✅ 0 error, 1 warning không liên quan (biến không dùng trong Designer).

---

## Vấn đề được báo cáo

| # | Triệu chứng | Form |
|---|-------------|------|
| 1 | **Không thấy nút bấm nào cả** — btnNew, btnImport, btnAvatar, btnLogout, lblCount đều vô hình | HomeForm (Dashboard) |
| 2 | **pnlJoin bị kéo giãn** ra ngoài màn hình khi resize | HomeForm |
| 3 | **Focus highlight** trên ô nhập mã chia sẻ không hoạt động đúng | HomeForm |

---

## Nguyên nhân gốc

### Bug 1 & 2 — WinForms Anchor-bug với nested Panel

**Cơ chế**: Khi `InitializeComponent()` chạy, các panel con (`pnlHeader`, `pnlActionBar`, `pnlFilter`) chưa được add vào form nên `Width` của chúng vẫn là **200px** (giá trị mặc định của Panel).

Khi button có `Anchor = Top | Right` và `Location.X > 200` được add vào panel này, WinForms tính khoảng cách đến cạnh phải:

```
anchorRightDist = parentWidth - control.Right = 200 - (880 + 110) = -790
```

Khi form hiển thị ở 1200px, WinForms đặt lại vị trí:

```
btnLogout.X = 1200 - 110 - (-790) = 1880px  ← ra ngoài màn hình!
```

| Control | Location.X ban đầu | Vị trí thực khi form 1200px |
|---------|-------------------|----------------------------|
| `btnLogout` | 880 | ~1880 (off-screen) |
| `btnAvatar` | 720 | ~1920 (off-screen) |
| `btnNew` | 820 | ~1972 (off-screen) |
| `btnImport` | 660 | ~1812 (off-screen) |
| `lblCount` | 870 | ~1822 (off-screen) |
| `pnlJoin` | Width=640, Anchor=Right | ~1592px (tràn sang phải) |

### Bug 3 — Double Paint handler trên pnlJoin

`StyleAsCard(pnlJoin)` thêm một Paint handler vẽ **border cứng** màu `AppTheme.Border`. Sau đó code thủ công thêm **thêm một Paint handler** vẽ border focus-aware. Kết quả: border focus không thấy vì bị vẽ đè bởi handler đầu tiên.

---

## Những gì đã sửa

### `HomeForm.Designer.cs`

| Thay đổi | Lý do |
|----------|-------|
| Xóa `Anchor = Top | Right` và `Location` cứng của **btnAvatar, btnLogout** | Vị trí sẽ được tính động trong `LayoutHeaderButtons()` |
| Xóa `Anchor = Top | Right` và `Location` cứng của **btnNew, btnImport** | Vị trí sẽ được tính động trong `LayoutActionBarButtons()` |
| Xóa `Anchor = Top | Right` và `Location` cứng của **lblWelcome** | Vị trí sẽ được tính động trong `LayoutHeaderButtons()` |
| Xóa `Anchor = Top | Right` và `Location` cứng của **lblCount** | Vị trí sẽ được tính động trong `LayoutFilterBar()` |
| Đổi `pnlJoin.Anchor` từ `Top|Left|Right` → `Top|Left` | pnlJoin cần Width cố định 640px, không stretch |

### `HomeForm.cs`

**Trong `HomeForm_Load`:**

| Thay đổi | Lý do |
|----------|-------|
| Thay `UiFactory.StyleAsCard(pnlJoin)` bằng `ApplyRoundedRegion + Resize handler` thủ công | Tránh double Paint handler |
| Thêm `UiFactory.StyleAsCard(pnlListContainer)` tách riêng | Rõ ràng hơn |
| Thêm `LayoutHeaderButtons()`, `LayoutActionBarButtons()`, `LayoutFilterBar()` và wire Resize | Căn phải dynamic |

**Thêm 3 method layout mới:**

```csharp
private void LayoutHeaderButtons()
// Căn btnLogout, btnAvatar, lblWelcome từ phải sang trái trong pnlHeader

private void LayoutActionBarButtons()
// Căn btnNew, btnImport từ phải sang trái trong pnlActionBar

private void LayoutFilterBar()
// Căn lblCount cạnh phải pnlFilter
```

**Logic**: Giống `TypeRenderForm.LayoutToolbarButtons()` đã có sẵn — tính `right = panelWidth - padding`, sau đó trừ dần width của từng control và gap.

**Gọi thêm:**
- `LayoutHeaderButtons()` sau khi cập nhật `lblWelcome.Text` trong `HomeForm_Shown`
- `LayoutFilterBar()` sau khi cập nhật `lblCount.Text` trong `BindDocuments`

---

## File thay đổi

| File | Loại thay đổi |
|------|--------------|
| `MarkTogether.Client/HomeForm.Designer.cs` | Xóa Anchor/Location sai của 5 control |
| `MarkTogether.Client/HomeForm.cs` | Fix double Paint, thêm 3 layout method + gọi chúng |

---

## Kết quả

- ✅ Dashboard hiển thị đầy đủ: `btnNew`, `btnImport`, `btnAvatar`, `btnLogout`, `lblCount`, `pnlJoin`
- ✅ pnlJoin cố định 640px, không tràn màn hình
- ✅ Focus highlight trên ô mã chia sẻ hoạt động đúng
- ✅ Resize form → buttons tự căn lại đúng vị trí
- ✅ Build sạch: **0 error, 1 warning cũ**

---

## Ghi chú kỹ thuật

> **Tại sao `TypeRenderForm` không bị bug này?**  
> `TypeRenderForm.Designer.cs` **không set Location** cho toolbar buttons. Sau `InitializeComponent`, constructor gọi `ApplyTheme()` → `LayoutToolbarButtons()` để đặt vị trí dựa trên `pnlHeader.ClientSize.Width` thực tế. HomeForm cũ đặt `Location` tĩnh + dùng Anchor → sai.  
> Fix này áp dụng đúng cùng pattern.
