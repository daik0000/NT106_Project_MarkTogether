# MarkTogether.Shared

Thư viện `MarkTogether.Shared` chứa model dùng chung giữa client/server:

- `Packet`: khung gói tin chung.
- `MessageType`: loại message.
- Các `Payload_*`: dữ liệu theo từng loại message.
- `PacketHelper`: hàm gửi/nhận qua `NetworkStream`.

---

## 1) Cấu trúc `Packet`

`Packet` có 3 field chính:

- `Type`: kiểu `MessageType`.
- `Token`: token phiên đăng nhập (nếu cần auth).
- `Payload`: chuỗi JSON của payload thực tế.

Tạo packet nhanh bằng:

```csharp
var packet = Packet.Create(MessageType.AUTH_LOGIN, new Payload_AUTH_LOGIN
{
    Username = "alice",
    Password = "123456"
});
```

Đọc payload ở bên nhận bằng:

```csharp
var login = packet.GetPayload<Payload_AUTH_LOGIN>();
```

---

## 2) Cách gửi/nhận với `PacketHelper`

`PacketHelper` đang dùng protocol:

1. Serialize `Packet` -> JSON
2. Prefix 4 bytes độ dài
3. Gửi lên stream

Bên nhận đọc đúng thứ tự:

1. Đọc 4 bytes length
2. Đọc đúng `length` bytes data
3. Deserialize về `Packet`

### Gửi

```csharp
PacketHelper.Send(stream, packet);
```

### Nhận

```csharp
Packet packet = PacketHelper.Receive(stream);
```

---

## 3) Ví dụ Client -> Server (AUTH_LOGIN)

### Client gửi login

```csharp
var req = new Payload_AUTH_LOGIN
{
    Username = "alice",
    Password = "123456"
};

var packet = Packet.Create(MessageType.AUTH_LOGIN, req);
PacketHelper.Send(stream, packet);
```

### Server nhận và xử lý

```csharp
Packet incoming = PacketHelper.Receive(stream);

if (incoming.Type == MessageType.AUTH_LOGIN)
{
    var payload = incoming.GetPayload<Payload_AUTH_LOGIN>();

    // validate payload.Username / payload.Password...

    var res = new Payload_AUTH_RESPONSE
    {
        Success = true,
        Token = "jwt_or_session_token",
        Message = "Login success",
        UserId = 1,
        Username = payload.Username
    };

    PacketHelper.Send(stream, Packet.Create(MessageType.AUTH_RESPONSE, res));
}
```

---

## 4) Ví dụ OP_INSERT / OP_DELETE batch

Theo plan hiện tại, `OP_INSERT` và `OP_DELETE` gửi dạng batch cùng loại:

- `docID`
- `clientResivion`
- `ops` (tối đa 5 phần tử)
- mỗi item `ops`: `pos`, `text`, `timestamp`

### Client gửi `OP_INSERT`

```csharp
var insertPayload = new Payload_OP_INSERT
{
    docID = 10,
    clientResivion = 15,
    ops = new List<EditOpItem>
    {
        new EditOpItem { pos = 5, text = "H", timestamp = DateTime.UtcNow },
        new EditOpItem { pos = 6, text = "i", timestamp = DateTime.UtcNow }
    }
};

PacketHelper.Send(stream, Packet.Create(MessageType.OP_INSERT, insertPayload));
```

### Server broadcast lại `OP_BROADCAST`

```csharp
var broadcast = new Payload_OP_BROADCAST
{
    docID = 10,
    clientResivion = 16,
    userID = 1,
    username = "alice",
    opType = "insert",
    ops = insertPayload.ops
};

PacketHelper.Send(targetClientStream, Packet.Create(MessageType.OP_BROADCAST, broadcast));
```

---

## 5) Các helper tiện ích hiện có

### `Packet`

- `Packet.Create(MessageType type, object payload = null)`
  - Tạo packet và tự serialize payload.
- `GetPayload<T>()`
  - Deserialize `Payload` về class mong muốn.

### `PacketHelper`

- `Send(NetworkStream stream, Packet packet)`
  - Gửi packet qua stream.
- `Receive(NetworkStream stream)`
  - Nhận packet từ stream.

---

## 6) Khuyến nghị khi dùng

- Luôn parse payload đúng theo `MessageType`.
- Với `OP_INSERT` / `OP_DELETE`, giữ `ops.Count <= 5` trước khi gửi.
- Dùng `Token` cho các request cần xác thực.
- Bắt exception ở tầng network (`IOException`, mất kết nối, packet lỗi JSON).

---

## 7) Bảng tóm tắt các loại message và payload

| `MessageType` | Payload gửi (class + fields) | Payload nhận/trả về (class + fields) |
|---|---|---|
| `AUTH_REGISTER` | `Payload_AUTH_REGISTER` (`Username`, `Password`) | `Payload_AUTH_RESPONSE` (`Success`, `Token`, `Message`, `UserId`, `Username`) hoặc `Payload_ERROR` (`Message`) |
| `AUTH_LOGIN` | `Payload_AUTH_LOGIN` (`Username`, `Password`) | `Payload_AUTH_RESPONSE` (`Success`, `Token`, `Message`, `UserId`, `Username`) hoặc `Payload_ERROR` (`Message`) |
| `AUTH_RESPONSE` | `Payload_AUTH_RESPONSE` (`Success`, `Token`, `Message`, `UserId`, `Username`) | `Payload_AUTH_RESPONSE` (`Success`, `Token`, `Message`, `UserId`, `Username`) |
| `DOC_CREATE` | `Payload_DOC_CREATE_Request` (`title`) | `Payload_DOC_CREATE_Response` (`docID`, `shareCode`, `title`, `content`, `revision`) hoặc `Payload_ERROR` (`Message`) |
| `DOC_LIST` | `Payload_DOC_LIST_Request` (không có field) | `Payload_DOC_LIST_Response` (`documents`: `List<DocInfo>`) hoặc `Payload_ERROR` (`Message`) |
| `DOC_OPEN` | `Payload_DOC_OPEN_Request` (`docID`) | `Payload_DOC_OPEN_Response` (`docID`, `title`, `content`, `revision`, `permission`) hoặc `Payload_ERROR` (`Message`) |
| `DOC_JOIN_CODE` | `Payload_DOC_JOIN_CODE_Request` (`shareCode`) | `Payload_DOC_JOIN_CODE_Response` (`docID`, `title`, `content`, `revision`, `permission`, `error`) hoặc `Payload_ERROR` (`Message`) |
| `DOC_LEAVE` | `Payload_DOC_LEAVE_Request` (`docID`) | `Payload_DOC_LEAVE_Response` (`success`, `message`) hoặc `Payload_ERROR` (`Message`) |
| `DOC_SHARE` | `Payload_DOC_SHARE_Request` (`docID`, `targetUsername`) | `Payload_DOC_SHARE_Response` (`success`, `message`) hoặc `Payload_ERROR` (`Message`) |
| `OP_INSERT` | `Payload_OP_INSERT` (`docID`, `clientResivion`, `ops`) với `ops` là `List<EditOpItem>` gồm (`pos`, `text`, `timestamp`) | `Payload_OK` (`Message`) hoặc `Payload_ERROR` (`Message`) |
| `OP_DELETE` | `Payload_OP_DELETE` (`docID`, `clientResivion`, `ops`) với `ops` là `List<EditOpItem>` gồm (`pos`, `text`, `timestamp`) | `Payload_OK` (`Message`) hoặc `Payload_ERROR` (`Message`) |
| `OP_BROADCAST` | `Payload_OP_BROADCAST` (`docID`, `clientResivion`, `userID`, `username`, `opType`, `ops`) với `ops` là `List<EditOpItem>` gồm (`pos`, `text`, `timestamp`) | `Payload_OP_BROADCAST` (cùng cấu trúc) |
| `ERROR` | `Payload_ERROR` (`Message`) | `Payload_ERROR` (`Message`) |
| `OK` | `Payload_OK` (`Message`) | `Payload_OK` (`Message`) |

> Ghi chú: với `OP_INSERT`/`OP_DELETE` theo plan mới, payload chứa `docID`, `clientResivion`, `ops` (tối đa 5 item), mỗi item gồm `pos`, `text`, `timestamp`.
