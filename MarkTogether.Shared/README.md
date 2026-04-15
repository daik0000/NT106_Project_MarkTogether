# MarkTogether.Shared

Th? vi?n `MarkTogether.Shared` ch?a model dùng chung gi?a client/server:

- `Packet`: khung gói tin chung.
- `MessageType`: lo?i message.
- Các `Payload_*`: d? li?u theo t?ng lo?i message.
- `PacketHelper`: hàm g?i/nh?n qua `NetworkStream`.

---

## 1) C?u trúc `Packet`

`Packet` có 3 field chính:

- `Type`: ki?u `MessageType`.
- `Token`: token phiên ??ng nh?p (n?u c?n auth).
- `Payload`: chu?i JSON c?a payload th?c t?.

T?o packet nhanh b?ng:

```csharp
var packet = Packet.Create(MessageType.AUTH_LOGIN, new Payload_AUTH_LOGIN
{
    Username = "alice",
    Password = "123456"
});
```

??c payload ? bên nh?n b?ng:

```csharp
var login = packet.GetPayload<Payload_AUTH_LOGIN>();
```

---

## 2) Cách g?i/nh?n v?i `PacketHelper`

`PacketHelper` ?ang dùng protocol:

1. Serialize `Packet` -> JSON
2. Prefix 4 bytes ?? dài
3. G?i lên stream

Bên nh?n ??c ?úng th? t?:

1. ??c 4 bytes length
2. ??c ?úng `length` bytes data
3. Deserialize v? `Packet`

### G?i

```csharp
PacketHelper.Send(stream, packet);
```

### Nh?n

```csharp
Packet packet = PacketHelper.Receive(stream);
```

---

## 3) Ví d? Client -> Server (AUTH_LOGIN)

### Client g?i login

```csharp
var req = new Payload_AUTH_LOGIN
{
    Username = "alice",
    Password = "123456"
};

var packet = Packet.Create(MessageType.AUTH_LOGIN, req);
PacketHelper.Send(stream, packet);
```

### Server nh?n và x? lý

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

## 4) Ví d? OP_INSERT / OP_DELETE batch

Theo plan hi?n t?i, `OP_INSERT` và `OP_DELETE` g?i d?ng batch cùng lo?i:

- `docID`
- `clientResivion`
- `ops` (t?i ?a 5 ph?n t?)
- m?i item `ops`: `pos`, `text`, `timestamp`

### Client g?i `OP_INSERT`

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

### Server broadcast l?i `OP_BROADCAST`

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

## 5) Các helper ti?n ích hi?n có

### `Packet`

- `Packet.Create(MessageType type, object payload = null)`
  - T?o packet và t? serialize payload.
- `GetPayload<T>()`
  - Deserialize `Payload` v? class mong mu?n.

### `PacketHelper`

- `Send(NetworkStream stream, Packet packet)`
  - G?i packet qua stream.
- `Receive(NetworkStream stream)`
  - Nh?n packet t? stream.

---

## 6) Khuy?n ngh? khi dùng

- Luôn parse payload ?úng theo `MessageType`.
- V?i `OP_INSERT` / `OP_DELETE`, gi? `ops.Count <= 5` tr??c khi g?i.
- Dùng `Token` cho các request c?n xác th?c.
- B?t exception ? t?ng network (`IOException`, m?t k?t n?i, packet l?i JSON).

---

## 7) B?ng tóm t?t các lo?i message và payload

| `MessageType` | Payload g?i (class + fields) | Payload nh?n/tr? v? (class + fields) |
|---|---|---|
| `AUTH_REGISTER` | `Payload_AUTH_REGISTER` (`Username`, `Password`) | `Payload_AUTH_RESPONSE` (`Success`, `Token`, `Message`, `UserId`, `Username`) ho?c `Payload_ERROR` (`Message`) |
| `AUTH_LOGIN` | `Payload_AUTH_LOGIN` (`Username`, `Password`) | `Payload_AUTH_RESPONSE` (`Success`, `Token`, `Message`, `UserId`, `Username`) ho?c `Payload_ERROR` (`Message`) |
| `AUTH_RESPONSE` | `Payload_AUTH_RESPONSE` (`Success`, `Token`, `Message`, `UserId`, `Username`) | `Payload_AUTH_RESPONSE` (`Success`, `Token`, `Message`, `UserId`, `Username`) |
| `DOC_CREATE` | `Payload_DOC_CREATE_Request` (`title`) | `Payload_DOC_CREATE_Response` (`docID`, `shareCode`, `title`, `content`, `revision`) ho?c `Payload_ERROR` (`Message`) |
| `DOC_LIST` | `Payload_DOC_LIST_Request` (không có field) | `Payload_DOC_LIST_Response` (`documents`: `List<DocInfo>`) ho?c `Payload_ERROR` (`Message`) |
| `DOC_OPEN` | `Payload_DOC_OPEN_Request` (`docID`) | `Payload_DOC_OPEN_Response` (`docID`, `title`, `content`, `revision`, `permission`) ho?c `Payload_ERROR` (`Message`) |
| `DOC_JOIN_CODE` | `Payload_DOC_JOIN_CODE_Request` (`shareCode`) | `Payload_DOC_JOIN_CODE_Response` (`docID`, `title`, `content`, `revision`, `permission`, `error`) ho?c `Payload_ERROR` (`Message`) |
| `DOC_LEAVE` | `Payload_DOC_LEAVE_Request` (`docID`) | `Payload_DOC_LEAVE_Response` (`success`, `message`) ho?c `Payload_ERROR` (`Message`) |
| `DOC_SHARE` | `Payload_DOC_SHARE_Request` (`docID`, `targetUsername`) | `Payload_DOC_SHARE_Response` (`success`, `message`) ho?c `Payload_ERROR` (`Message`) |
| `OP_INSERT` | `Payload_OP_INSERT` (`docID`, `clientResivion`, `ops`) v?i `ops` là `List<EditOpItem>` g?m (`pos`, `text`, `timestamp`) | `Payload_OK` (`Message`) ho?c `Payload_ERROR` (`Message`) |
| `OP_DELETE` | `Payload_OP_DELETE` (`docID`, `clientResivion`, `ops`) v?i `ops` là `List<EditOpItem>` g?m (`pos`, `text`, `timestamp`) | `Payload_OK` (`Message`) ho?c `Payload_ERROR` (`Message`) |
| `OP_BROADCAST` | `Payload_OP_BROADCAST` (`docID`, `clientResivion`, `userID`, `username`, `opType`, `ops`) v?i `ops` là `List<EditOpItem>` g?m (`pos`, `text`, `timestamp`) | `Payload_OP_BROADCAST` (cùng c?u trúc) |
| `ERROR` | `Payload_ERROR` (`Message`) | `Payload_ERROR` (`Message`) |
| `OK` | `Payload_OK` (`Message`) | `Payload_OK` (`Message`) |

> Ghi chú: v?i `OP_INSERT`/`OP_DELETE` theo plan m?i, payload ch?a `docID`, `clientResivion`, `ops` (t?i ?a 5 item), m?i item g?m `pos`, `text`, `timestamp`.
