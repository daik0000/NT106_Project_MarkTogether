# 🎨 UI REDESIGN — CHANGELOG

> **Phạm vi**: Thiết kế lại toàn bộ giao diện Client theo yêu cầu Clean UI / Modern, palette nhất quán, typography rõ ràng, bố cục thoáng.  
> **Ngày**: 20/05/2026.  
> **Mục tiêu**: 1 design system duy nhất, mọi form dùng chung. Toàn bộ chức năng cũ giữ nguyên — đây là refactor giao diện thuần.

---

## ✅ TÓM TẮT THAY ĐỔI

| Loại | File | Trạng thái |
|------|------|-----------|
| 🆕 Thêm mới | `MarkTogether.Client/UI/AppTheme.cs` | Created |
| 🆕 Thêm mới | `MarkTogether.Client/UI/UiFactory.cs` | Created |
| ✏️ Refactor toàn bộ designer | `LoginForm.Designer.cs` | Rewritten |
| ✏️ Refactor handler | `LoginForm.cs` | Updated |
| ✏️ Refactor toàn bộ designer | `RegisterForm.Designer.cs` | Rewritten |
| ✏️ Refactor handler | `RegisterForm.cs` | Updated |
| ✏️ Refactor toàn bộ designer | `HomeForm.Designer.cs` | Rewritten |
| ✏️ Refactor handler | `HomeForm.cs` | Updated |
| ✏️ Refactor toàn bộ designer | `TypeRenderForm.Designer.cs` | Rewritten |
| ✏️ Refactor handler | `TypeRenderForm.cs` | Updated (theme + back button + AI mode VN) |
| ✏️ Refactor toàn bộ designer | `CreateDocumentForm.Designer.cs` | Rewritten |
| ✏️ Refactor handler | `CreateDocumentForm.cs` | Updated |
| ✏️ Style update | `NewCommentForm.cs` | Updated |
| ✏️ Style update | `ShareDocumentForm.cs` | Updated |
| ✏️ Style update | `VersionHistoryForm.cs` | Updated |
| ⚙️ Project | `Client.csproj` | Bổ sung 2 file UI mới (`AppTheme.cs`, `UiFactory.cs`) |

Tổng cộng: **2 file mới**, **15 file edited**. Build sạch toàn solution.

---

## 1️⃣ DESIGN SYSTEM MỚI

### `MarkTogether.Client/UI/AppTheme.cs`

Single source of truth cho mọi token UI.

**Color palette** (đúng đặc tả):
```csharp
Primary       #2563EB    PrimaryHover  #1D4ED8    PrimaryPressed #1E40AF
Secondary     #14B8A6    SecondaryHover #0D9A88
Background    #F8FAFC    Surface       #FFFFFF
TextPrimary   #0F172A    TextSecondary #64748B    TextDisabled #94A3B8
Success       #22C55E    Warning       #F59E0B    Error        #EF4444
Border        #E2E8F0    Divider       #F1F5F9    HoverFill    #F1F5F9
SelectedFill  #DBEAFE
```

**Typography**:
- Tự dò font ưu tiên trên máy: **Inter → Be Vietnam Pro → SF Pro Display → Roboto → Segoe UI** (fallback).
- Font scale: H1 24pt, H2 18pt, H3 14pt, Subtitle 11pt, Body 10pt, BodyBold 10pt, Caption 9pt, Button 10pt bold.
- Mono cho code: Consolas 10pt / 12pt bold.

**Spacing scale**:
```
SpaceXs=4   SpaceSm=8   SpaceMd=12  SpaceLg=16
SpaceXl=24  Space2Xl=32 Space3Xl=48
```

**Sizing chuẩn**: Input 38px, Button 40px, Toolbar 56px, Corner radius 8/12.

### `MarkTogether.Client/UI/UiFactory.cs`

Helpers để áp style nhất quán lên control sẵn có.

**Buttons** — 5 variant:
- `StylePrimaryButton(btn)` — nền `#2563EB`, chữ trắng, hover `#1D4ED8`, pressed `#1E40AF`. Dùng cho CTA chính.
- `StyleSecondaryButton(btn)` — nền trắng, viền `#E2E8F0`, chữ Primary. Dùng cho thao tác phụ.
- `StyleGhostButton(btn)` — nền background, chữ secondary. Dùng cho close / refresh.
- `StyleDangerButton(btn)` — nền `#EF4444`. Dùng cho xoá / logout / revoke.
- `StyleSuccessButton(btn)` — nền `#22C55E`. Dùng cho restore / confirm tích cực.

Mọi button auto bo góc 8px, font Button bold 10pt, cursor hand.

**Cards / Panels**:
- `StyleAsCard(panel, radius?)` biến panel bất kỳ thành card: nền trắng, viền `#E2E8F0` mảnh, bo góc 12px (mặc định) hoặc 8px (input).
- `DrawBorder(g, bounds, color, radius)` để vẽ viền bo tròn manual khi cần focus highlight.

**ListView**:
- `StyleListView(lv)` — header xám bg + label secondary, row trắng, divider mảnh, selected fill `#DBEAFE`, hover sạch. Tự handle DrawColumnHeader / DrawItem / DrawSubItem.

**Text helpers**:
- `CreateHeading(text, level)` H1/H2/H3.
- `CreateSubtitle`, `CreateFieldLabel`, `CreateCaption`, `CreateBadge`, `CreateErrorBanner`.

---

## 2️⃣ TỪNG MÀN HÌNH

### 🔐 LoginForm

**Trước**: card trắng đơn giản, button DodgerBlue, không có brand label.

**Sau**:
- Centered card 440×500 trên nền `#F8FAFC`, có viền + bo góc 12px.
- Caption "MARKTOGETHER" trên cùng (xám nhỏ).
- Heading H1 "Đăng nhập" (24pt bold), Subtitle xám "Đăng nhập để tiếp tục cộng tác."
- 2 input bo góc 8px với **focus highlight**: viền chuyển sang Primary khi focus, trở về `#E2E8F0` khi blur.
- Một CTA Primary full-width "Đăng nhập" 40px bold.
- Error banner nền `#FEF2F2` (đỏ nhạt), chữ Error, bo góc 8px, chỉ hiện khi có lỗi.
- Footer: text "Chưa có tài khoản?" + LinkLabel Primary "Đăng ký ngay".
- AcceptButton = btnLogin (Enter để submit).

### 🔐 RegisterForm

**Trước**: title màu xanh lá `#4CAF50`, layout cũ.

**Sau**: cùng pattern Login (consistent), 4 input (Username / Email / Mật khẩu / Xác nhận), heading "Tạo tài khoản", validate với error banner thống nhất, CTA Primary "Tạo tài khoản".

### 🏠 HomeForm

**Trước**: layout 1 panel top với mọi nút hỗn loạn (Welcome, Sort, ImportMd, NewNote, JoinCode, Avatar, Logout chen nhau).

**Sau** — bố cục 3 vùng rõ ràng:

**Header bar** (cao 64px, nền trắng + divider):
- Trái: brand "MarkTogether" H2 màu Primary.
- Phải: `Xin chào, {user}` Body secondary + nút Secondary "Đổi avatar" + nút Danger "Đăng xuất".

**Body** (nền `#F8FAFC`, padding 24px):
- **Action bar** (168px):
  - Heading H1 "Tài liệu của bạn" + Subtitle "Tạo, mở, chia sẻ và cộng tác trên các tài liệu Markdown."
  - Bên phải có 2 CTA: Secondary "Import .md" + Primary "+ Tài liệu mới".
  - Card "Tham gia bằng mã" — input mono uppercase + button Primary "Tham gia bằng mã".
- **Filter bar** (60px): "Sắp xếp:" + ComboBox 4 option Tiếng Việt (Mới nhất / Cũ nhất / A → Z / Z → A) + count "N tài liệu" canh phải.
- **List card**: panel trắng bo 12px chứa ListView styled. 3 cột: Tiêu đề / Cập nhật / Quyền. Permission hiển thị tiếng Việt: "Chủ sở hữu" / "Chỉnh sửa" / "Xem".

**Confirm dialog**: bấm Đăng xuất hỏi "Bạn có chắc muốn đăng xuất?" trước khi thực hiện.

### ✍️ TypeRenderForm (Editor)

**Trước**: toolbar đầy emoji rời rạc (💾 Save, 🔗 Share, 🕒 Versions, 📄 PDF, 💬 Comment, 🖼 Insert Image), badge permission tiếng Anh, các CTA cùng kiểu nhau gây rối.

**Sau** — toolbar 56+8px nền trắng có divider mảnh:

**Bên trái toolbar**:
- Nút **"← Trở về"** (Secondary) — cho phép quay lại HomeForm như yêu cầu.
- Tiêu đề tài liệu H3.
- Badge permission: nền + chữ đổi theo quyền (xanh lá owner / xanh dương editor / xám viewer), text tiếng Việt.

**Bên phải toolbar** (right-aligned chain với gap 8px):
- "Chèn ảnh", "Comment", "Xuất PDF", "Lịch sử", "Chia sẻ" (Success — nổi bật), "Lưu" (Primary — CTA chính), nút ghost "≡" để gập side panel.

**Body** chia 2 card có bo góc:
- **Editor card**: label caption "MARKDOWN" + textbox raw không viền nền trắng, font Consolas 10pt.
- **Preview card**: label caption "XEM TRƯỚC" + WebView2.

**Side panel** (TabControl 3 tab tiếng Việt):
- **Trò chuyện**: list message + input + nút Primary "Gửi". Enter để gửi.
- **Bình luận**: list comment + nút Secondary "Đã xử lý" + Danger "Xoá" + Ghost "Tải lại". Double-click để jump tới anchor.
- **Trợ lý AI**: history textbox xám + ComboBox mode tiếng Việt (Hỏi đáp / Tóm tắt / Viết tiếp / Dịch — map sang `chat / summarize / continue / translate` ở client) + textbox prompt + nút Primary "Gửi".

**Permission UI**:
- Owner thấy đầy đủ nút.
- Editor: ẩn nút Share.
- Viewer: textbox readonly, ẩn Save / Insert Image / Versions / Share.

**Confirm dialog** bắt buộc: trước khi đóng tab có thay đổi → tự động save. Trước khi xoá comment, restore version → MessageBox confirm.

### 📤 ShareDocumentForm

**Trước**: 3 group rời rạc, nút màu DodgerBlue / Đỏ nhạt.

**Sau**: 3 nhóm rõ ràng:
- **Mã chia sẻ** — textbox mono Consolas 14pt bold + nút Secondary "Copy" + Secondary "Tạo lại".
- **Mời theo username** — input + ComboBox quyền + Primary "Mời".
- **Người cộng tác** — ListView styled (Username / Email / Quyền / Mời lúc) + Secondary "Đổi quyền..." + Danger "Thu hồi" + Ghost "Đóng".

Mọi thao tác Revoke / Update permission đều confirm trước.

### 📜 VersionHistoryForm

**Trước**: button thường, list không styled.

**Sau**: ListView styled (Thời gian / Người lưu / Ghi chú) + preview readonly. Footer: Secondary "Refresh" + Success "Khôi phục" (CTA tích cực) + Danger "Xoá" + Ghost "Đóng". Bắt buộc confirm dialog trước khi restore / delete.

### 💬 NewCommentForm

**Trước**: layout cứng, nút DodgerBlue.

**Sau**: heading "Đoạn văn bản:" + preview snippet trong panel viền nhẹ + input multiline + Primary "Gửi" + Secondary "Huỷ".

### 📄 CreateDocumentForm

**Trước**: dialog cũ với nút "Create / Cancel".

**Sau**: heading H2 "Tạo tài liệu mới" + Subtitle "Đặt tên cho tài liệu để bắt đầu cộng tác." + ô Tiêu đề bo góc focus highlight + error banner caption đỏ + Secondary "Huỷ" + Primary "Tạo".

---

## 3️⃣ NHẤT QUÁN VÀ NGUYÊN TẮC

### Button hierarchy (nhất quán xuyên suốt)

| Variant | Khi nào dùng |
|---------|-------------|
| **Primary** (`#2563EB`) | CTA chính của trang: Đăng nhập, Tạo, Lưu, Tham gia, Mời, Gửi |
| **Success** (`#22C55E`) | Hành động tích cực có hậu quả lớn: Khôi phục version, Chia sẻ |
| **Secondary** (white + border) | Action phụ: Import, Tải lại, Đóng dialog, Đổi avatar |
| **Danger** (`#EF4444`) | Xoá, Đăng xuất, Thu hồi |
| **Ghost** (no fill) | Refresh, Toggle, Close không quan trọng |

### Input pattern thống nhất

Mọi text field theo pattern: `Panel` wrap `TextBox` không viền. Panel có:
- Background trắng, padding ngang 12px.
- Border 1px `#E2E8F0`, bo góc 8px.
- Focus highlight: chuyển border sang Primary khi textbox bên trong focus, về lại khi blur. Implement bằng `Tag = "focus"` + `Invalidate` + Paint handler.

### Spacing pattern

- Card padding 24-32px.
- Khoảng cách label → input: 8px.
- Giữa các button: 8px.
- Giữa các block khác nhau: 16-24px.
- Heading → subtitle: 12-16px.

### Tiếng Việt nhất quán

Mọi user-facing text đã chuyển sang tiếng Việt: "Đăng nhập / Đăng xuất / Tài liệu của bạn / Chia sẻ / Lưu / Khôi phục / Bình luận / Trò chuyện / Trợ lý AI / Chủ sở hữu / Chỉnh sửa / Chỉ xem / Mới nhất / Cũ nhất".

### Confirm dialog cho action quan trọng

Theo yêu cầu *"Cho phép người dùng quay lại, chỉnh sửa thông tin và xác nhận trước các hành động quan trọng"*:
- Đăng xuất → confirm.
- Xoá comment, xoá version → confirm.
- Khôi phục version (ghi đè doc) → confirm.
- Thu hồi quyền chia sẻ → confirm.
- Editor có nút "← Trở về" để quay lại Home.

### Responsive

Mọi form dùng `AutoScaleMode.Dpi` + `Anchor` cho các nút phải-canh-phải. `MinimumSize` cho HomeForm 960×600, TypeRenderForm 1280×720. Layout chính dùng `Dock.Fill / Top / Bottom` để tự co giãn theo cửa sổ.

---

## 4️⃣ BUILD STATE

```
Shared.dll  → bin/Debug/MarkTogether.Shared.dll  ✅
Client.exe  → bin/Debug/MarkTogether.Client.exe  ✅
Server.exe  → bin/Debug/MarkTogether.Server.exe  ✅
```

Chỉ 1 warning vô hại trong designer (biến local không dùng). Không có lỗi compile.

---

## 5️⃣ FILE TREE PHẦN UI

```
MarkTogether.Client/
├── UI/
│   ├── AppTheme.cs            (mới) — palette, typography, spacing, sizing
│   └── UiFactory.cs           (mới) — Style*Button, StyleAsCard, StyleListView, DrawBorder
├── LoginForm.cs               (refactor)
├── LoginForm.Designer.cs      (rewritten)
├── RegisterForm.cs            (refactor)
├── RegisterForm.Designer.cs   (rewritten)
├── HomeForm.cs                (refactor)
├── HomeForm.Designer.cs       (rewritten)
├── TypeRenderForm.cs          (theme + back button + AI VN)
├── TypeRenderForm.Designer.cs (rewritten)
├── CreateDocumentForm.cs      (refactor)
├── CreateDocumentForm.Designer.cs (rewritten)
├── NewCommentForm.cs          (style update)
├── NewCommentForm.Designer.cs (no change)
├── ShareDocumentForm.cs       (style update)
├── ShareDocumentForm.Designer.cs (no change)
├── VersionHistoryForm.cs      (style update)
└── VersionHistoryForm.Designer.cs (no change)
```

---

## 6️⃣ HOW TO REUSE

Nếu thêm form mới sau này, chỉ cần:

1. Set `BackColor = AppTheme.Background` (cho page) hoặc `AppTheme.Surface` (cho dialog).
2. Tạo `Button` rồi gọi `UiFactory.StylePrimaryButton(btn)` (hoặc Secondary/Ghost/Danger/Success).
3. Tạo `Panel` chứa `TextBox` không viền, gọi `UiFactory.StyleAsCard(panel, AppTheme.CornerRadius)` rồi wire focus highlight 5 dòng.
4. Tạo `ListView` rồi gọi `UiFactory.StyleListView(lv)`.
5. Dùng `AppTheme.H1/H2/H3/Body/Subtitle/Caption` cho font, `AppTheme.SpaceXs..3Xl` cho khoảng cách.

Không cần biết hex color hay font name — chỉ tham chiếu token.

---

> Bản UI mới đảm bảo: **clean**, **modern**, **nhất quán**, **chỉ hiện điều cần thiết**, **CTA nổi bật**, **confirm trước hành động quan trọng**, **responsive theo kích cỡ cửa sổ**.