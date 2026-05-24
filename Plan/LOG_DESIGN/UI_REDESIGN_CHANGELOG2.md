# UI Redesign Changelog 2

**Ngày thực hiện:** 2026-05-21  
**File sửa:** `MarkTogether.Client/TypeRenderForm.cs`  
**Build:** Thành công (0 errors, 1 warning cũ không liên quan)

---

## 1. PreviewHtmlTemplate — Viết lại toàn bộ CSS

**Vị trí:** `TypeRenderForm.cs`, hằng số `PreviewHtmlTemplate`

### highlight.js theme
| Trước | Sau |
|-------|-----|
| `github.min.css` (light theme) | `github-dark.min.css` (dark theme) |

### Font stack
| Trước | Sau |
|-------|-----|
| `Segoe UI, Arial, sans-serif` | `-apple-system, 'Segoe UI Variable Text', 'Segoe UI', system-ui, Arial, sans-serif` |

### Body layout
| Trước | Sau |
|-------|-----|
| `padding: 16px` | `padding: 32px 40px` |
| Không giới hạn chiều rộng | `max-width: 820px; margin: 0 auto` (căn giữa) |
| `line-height: 1.6` | `line-height: 1.75` |
| `font-size` mặc định | `font-size: 15px` |

### Headings
- `h1`: border-bottom 2px, font-weight 700, letter-spacing -0.02em, màu `#0f172a`
- `h2`: border-bottom 1px, font-weight 700, màu `#1e293b`
- `h3`: font-weight 600, màu `#1e293b`
- `h4–h6`: font-weight 600, màu `#475569`

### Inline code
| Trước | Sau |
|-------|-----|
| `background: #f6f8fa; color: #cf222e` | `background: #f1f5f9; color: #be123c` |
| `border-radius: 4px` | `border-radius: 5px; border: 1px solid #e2e8f0` |
| Font `Consolas` | Font `'Cascadia Code', 'Fira Code', Consolas` |

### Code block (`pre`)
| Trước | Sau |
|-------|-----|
| Nền xám sáng `#f6f8fa` | Nền đen `#0f172a` |
| `border-radius: 6px` | `border-radius: 12px` |
| Không có shadow | `box-shadow: 0 4px 16px rgba(0,0,0,.25)` |
| `padding: 10px` | `padding: 18px 22px` |

### Blockquote
| Trước | Sau |
|-------|-----|
| Viền trái xám `#ddd`, không nền | Viền trái `#2563eb` (xanh primary), nền `#eff6ff` |
| `color: #666` | `color: #1e40af` |
| Không bo góc | `border-radius: 0 10px 10px 0` |

### Table
| Trước | Sau |
|-------|-----|
| `border: 1px solid #ddd` trên từng ô | `border: 1px solid #e2e8f0` toàn bảng, `border-radius: 10px` |
| Header không phân biệt | `thead { background: #f8fafc }`, header text uppercase, letter-spacing |
| Không có hover | `tbody tr:hover td { background: #f8fafc }` |
| `padding: 6px 10px` | `padding: 10px 16px` |

### Images
| Trước | Sau |
|-------|-----|
| `max-width: 100%` | `border-radius: 10px; box-shadow: 0 2px 12px rgba(0,0,0,.1)` |

### Scrollbar tùy chỉnh (mới hoàn toàn)
```css
::-webkit-scrollbar { width: 6px; height: 6px }
::-webkit-scrollbar-track { background: transparent }
::-webkit-scrollbar-thumb { background: #cbd5e1; border-radius: 3px }
::-webkit-scrollbar-thumb:hover { background: #94a3b8 }
```

### Horizontal rule
- `border: none; border-top: 1px solid #e2e8f0; margin: 2em 0`

---

## 2. Tab headers (panel side) — Custom OwnerDraw

**Vị trí:** `ApplyTheme()` + method `DrawSideTabItem(DrawItemEventArgs e)`

**Trước:** Tab headers mặc định của WinForms (raised, 3D border cũ).

**Sau:**
- `tabSide.DrawMode = TabDrawMode.OwnerDrawFixed`
- Tab inactive: nền `AppTheme.Background`, text màu `TextSecondary`
- Tab active: nền `AppTheme.Surface`, text màu `Primary` (bold), **underline 3px màu primary** ở đáy
- `tabSide.Padding = new Point(16, 6)` — tab header rộng hơn

---

## 3. Chat list (`lstChat`) — Custom OwnerDraw 2 dòng

**Vị trí:** `ApplyTheme()` + method `DrawChatListItem(DrawItemEventArgs e)`

**Trước:** ListBox mặc định, mỗi item 1 dòng text "[HH:mm:ss] username: message".

**Sau:**
- `lstChat.DrawMode = DrawMode.OwnerDrawFixed`
- `lstChat.ItemHeight = 50`
- **Dòng 1 (top):** Username in đậm màu primary + thời gian màu muted (gray)
- **Dòng 2 (bottom):** Nội dung tin nhắn, text primary
- Màu nền xen kẽ: chẵn = `Surface` (#fff), lẻ = `Background` (#f8fafc)
- Item được chọn: nền `SelectedFill` (#dbeafe)
- Divider line `AppTheme.Divider` ở đáy mỗi item

---

## 4. Comment list (`lstComments`) — Custom OwnerDraw với resolved indicator

**Vị trí:** `ApplyTheme()` + method `DrawCommentListItem(DrawItemEventArgs e)`

**Trước:** ListBox mặc định, tất cả item cùng màu text.

**Sau:**
- `lstComments.DrawMode = DrawMode.OwnerDrawFixed`
- `lstComments.ItemHeight = 48`
- **Accent bar 3px bên trái:**
  - Màu `AppTheme.Primary` (xanh) nếu chưa xử lý
  - Màu `AppTheme.Success` (xanh lá) nếu đã resolved (`✓`)
- Text: `TextPrimary` nếu chưa xử lý, `TextSecondary` (mờ) nếu đã resolved
- Divider line `AppTheme.Divider` ở đáy mỗi item

---

## 5. Section headers (`lblRaw` / `lblPreview`) — Border-bottom

**Vị trí:** `ApplyTheme()`

**Trước:** Label text thuần, không phân cách rõ ràng với nội dung bên dưới.

**Sau:**
- `lblRaw.Paint` và `lblPreview.Paint` vẽ 1 đường `AppTheme.Border` ở đáy label
- Trông như IDE tab header, phân tách rõ phần tiêu đề và vùng soạn thảo/preview

---

## Tóm tắt kỹ thuật

| Thành phần | Phương pháp |
|-----------|-------------|
| Markdown preview CSS | Rewrite `PreviewHtmlTemplate` string constant |
| Tab headers | `TabDrawMode.OwnerDrawFixed` + `DrawItem` event |
| Chat list items | `DrawMode.OwnerDrawFixed` + `DrawItem` event |
| Comment list items | `DrawMode.OwnerDrawFixed` + `DrawItem` event |
| Section header dividers | `Paint` event trên `lblRaw` / `lblPreview` |

Tất cả thay đổi nằm trong `TypeRenderForm.cs`, không sửa Designer, không thêm dependencies mới.
