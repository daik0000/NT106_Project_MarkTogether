# Thuật toán tần suất gửi `OP_INSERT` / `OP_DELETE`

## Mục tiêu

Chuẩn hóa cách client gửi gói chỉnh sửa realtime để:

- Không gửi quá dày (spam packet mỗi ký tự).
- Không gửi quá trễ (đặc biệt khi user đổi kiểu thao tác insert/delete).
- Giữ đúng ràng buộc:
  - **Tối đa 5 ký tự/gói** `OP_INSERT` hoặc `OP_DELETE`.
  - Nếu đang gom Insert mà user chuyển sang Delete thì **flush Insert ngay lập tức**.
  - Nếu đang gom Delete mà user chuyển sang Insert thì **flush Delete ngay lập tức**.

---

## Contract đầu vào/đầu ra

### Input (mỗi lần text thay đổi)

- `oldText`: nội dung trước khi sửa.
- `newText`: nội dung sau khi sửa.
- `docId`, `clientRevision`, `token`.

### Output

- Một hoặc nhiều packet:
  - `MessageType.OP_INSERT` + `Payload_OP_INSERT`
  - `MessageType.OP_DELETE` + `Payload_OP_DELETE`
- Mỗi packet chứa tổng số ký tự trong `ops` **<= 5**.

> Ghi chú: giữ nguyên cấu trúc payload `OP_INSERT` / `OP_DELETE` hiện tại.

---

## Trạng thái nội bộ (client)

Client giữ một “buffer thao tác đang chờ gửi” (pending op):

- `pendingType`: `Insert` hoặc `Delete`.
- `pendingStartPos`: vị trí bắt đầu.
- `pendingText`: chuỗi ký tự đang gom.
- `flushTimer`: timer ngắn (ví dụ ~250ms) để gửi phần còn lại khi user tạm dừng.

---

## Luồng thuật toán

### Bước 1 — Tách delta từ lần sửa

Khi `TextChanged`, tính delta giữa `oldText` và `newText`:

- `deletedText` (nếu có)
- `insertedText` (nếu có)
- `position`

Có 3 trường hợp:

1. **Chỉ Insert**
2. **Chỉ Delete**
3. **Replace** (vừa delete vừa insert)

Với Replace:

- Flush pending hiện tại
- Queue Delete trước
- Flush
- Queue Insert sau
- Flush (hoặc tiếp tục gom theo policy)

---

### Bước 2 — Queue thao tác theo loại

Khi queue một thao tác mới (`Insert` hoặc `Delete`):

1. Nếu pending đang là loại khác → **flush ngay**.
2. Nếu chưa có pending → tạo pending mới.
3. Nếu có pending cùng loại:
   - Cố merge vào pending hiện tại (liền mạch theo vị trí).
   - Nếu không merge được → flush pending cũ rồi tạo pending mới.

---

### Bước 3 — Ngưỡng kích thước packet (max 5 ký tự)

Sau mỗi lần merge vào pending:

- Nếu `pendingText.Length >= 5`:
  - Cắt chunk 5 ký tự đầu.
  - Gửi thành 1 packet OP cùng loại.
  - Trừ phần đã gửi khỏi pending.
  - Lặp lại đến khi `pendingText.Length < 5`.

Điều này đảm bảo **mỗi packet không vượt 5 ký tự**.

---

### Bước 4 — Flush theo thời gian tạm dừng

Nếu pending chưa đủ 5 ký tự, restart `flushTimer`.

Khi timer nổ:

- Flush toàn bộ phần pending còn lại (dù < 5 ký tự).

=> User gõ chậm vẫn được gửi đều, không bị giữ packet quá lâu.

---

### Bước 5 — Flush tại sự kiện quan trọng

Phải flush pending ngay ở các tình huống:

- User đổi loại thao tác Insert ↔ Delete.
- Form đóng.
- Chuẩn bị save/leave tài liệu.

---

## Tần suất gửi thực tế

Tần suất phụ thuộc hành vi gõ:

- **Gõ liên tục cùng loại**: gửi mỗi 5 ký tự (batch-based).
- **Gõ ngắt quãng/chậm**: gửi theo timeout (`flushTimer`).
- **Đổi Insert/Delete liên tục**: gửi ngay tại điểm chuyển loại.

Có thể xem tần suất gần đúng:

$$
	ext{sendFrequency} \approx \max\left(\frac{\text{chars/sec}}{5},\; \frac{1}{\text{flushInterval}}\right)
$$

(trong đó `flushInterval` tính theo giây).

---

## Ví dụ minh họa

### Ví dụ 1: Insert liên tục 12 ký tự

- User gõ `abcdefgh1234`
- Packet gửi:
  - `OP_INSERT("abcde")`
  - `OP_INSERT("fgh12")`
  - `OP_INSERT("34")` (sau timer hoặc flush sự kiện)

### Ví dụ 2: Insert 3 ký tự rồi backspace

- User gõ `abc` (pending Insert = `abc`, chưa đủ 5)
- User nhấn xóa → loại thao tác đổi sang Delete
- Hệ thống:
  1. Flush ngay `OP_INSERT("abc")`
  2. Bắt đầu pending Delete

### Ví dụ 3: Delete liên tục 7 ký tự

- Packet gửi:
  - `OP_DELETE(5 ký tự)`
  - `OP_DELETE(2 ký tự)`

---

## Điều kiện đúng/sai (checklist)

### Correctness

- [x] Không packet nào > 5 ký tự.
- [x] Đổi loại thao tác thì flush ngay packet cũ.
- [x] Không mất ký tự khi tách chunk.
- [x] Flush pending khi đóng form.

### Stability

- [x] Không throw exception làm gián đoạn trải nghiệm gõ.
- [x] Lỗi mạng khi gửi OP không làm crash editor.

---

## Phạm vi hiện tại

- Server hiện tại **chỉ nhận + ACK** `OP_INSERT`/`OP_DELETE`, chưa áp dụng OT/reconcile.
- Thuật toán này tập trung vào **batching + tần suất gửi client-side**.

---

## Mở rộng đề xuất (phase sau)

1. Gửi ACK chứa `serverRevision` để đồng bộ chính xác hơn.
2. Bổ sung retry queue khi mất mạng tạm thời.
3. Gộp nhiều `EditOpItem` trong một packet (hiện có thể đang gửi 1 item/chunk).
4. Thêm debounce adapt theo tốc độ gõ thực tế.
