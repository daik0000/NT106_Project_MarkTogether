# KẾ HOẠCH MỞ RỘNG KIẾN TRÚC MARKTOGETHER
## Nền tảng tài liệu cộng tác (Notion/Docs Mini)

> Ngày tạo: 2026-05-21  
> Phiên bản: 1.0  
> Tác giả: Architecture Review

---

## MỤC LỤC

1. [Phân tích kiến trúc hiện tại](#1-phân-tích-kiến-trúc-hiện-tại)
2. [Vấn đề hiện tại](#2-vấn-đề-hiện-tại)
3. [Thiết kế mới](#3-thiết-kế-mới)
4. [Database Schema](#4-database-schema)
5. [API / Protocol Structure](#5-api--protocol-structure)
6. [Security Architecture](#6-security-architecture)
7. [Upload Architecture](#7-upload-architecture)
8. [Deployment Architecture](#8-deployment-architecture)
9. [Realtime Collaboration](#9-realtime-collaboration)
10. [Flow hoạt động](#10-flow-hoạt-động)
11. [Scaling & Future-proof](#11-scaling--future-proof)
12. [Checklist Implementation](#12-checklist-implementation)
13. [Lỗi phổ biến cần tránh](#13-lỗi-phổ-biến-cần-tránh)

---

## 1. PHÂN TÍCH KIẾN TRÚC HIỆN TẠI

### 1.1 Tổng quan

```
┌─────────────────┐         TCP (port 5000)         ┌─────────────────────┐
│  WinForms Client │ ◄──── 4-byte len + JSON ────► │   .NET 4.7.2 Server  │
│  (SocketClient)  │                                │   (ClientHandler)    │
└─────────────────┘                                └──────────┬──────────┘
                                                              │
                                                    Npgsql + Dapper
                                                              │
                                                   ┌──────────▼──────────┐
                                                   │   PostgreSQL 16     │
                                                   │   (Docker local)    │
                                                   └─────────────────────┘
```

### 1.2 Stack hiện tại

| Layer | Technology |
|-------|-----------|
| Client | C# WinForms (.NET Framework 4.7.2) |
| Protocol | Custom TCP binary: 4-byte length prefix + JSON (Newtonsoft.Json) |
| Server | C# Console App (.NET Framework 4.7.2), multi-threaded (1 thread/client) |
| ORM | Dapper (micro-ORM) |
| Database | PostgreSQL 16 (Docker) |
| Auth | BCrypt hash + GUID session token (in-memory ConcurrentDictionary) |
| Realtime | OT (Operational Transformation) qua TCP broadcast |
| Image Storage | Local disk (`Storage/images/{userId}/{guid}.ext`) |
| AI | Gemini API (async HTTP call) |

### 1.3 Các module đã có

- **Auth**: Register, Login, Logout (BCrypt + GUID token)
- **Document CRUD**: Create, Open, Save, List, Delete
- **Sharing**: Share by username, Share code (join link), Update/Revoke permission
- **Real-time editing**: OT Engine (insert/delete), broadcast to room
- **Chat**: Real-time chat per document room
- **Comments**: Anchor-based comments with resolve/delete
- **Image**: Upload/Get (disk storage, 5MB limit)
- **Version History**: Save/List/Detail/Restore/Delete snapshots
- **AI**: Gemini chatbot integration

### 1.4 Permission model hiện tại

```
GetPermission(docId, userId):
  1. Nếu user là owner_id của document → return "owner"
  2. Nếu có record trong document_shares → return permission ("editor"/"viewer")
  3. Không có gì → return null (không có quyền)
```

**Roles hiện tại**: `owner`, `editor`, `viewer`

### 1.5 Database schema hiện tại (8 bảng)

- `users` — thông tin user
- `documents` — tài liệu (có `is_public`, `public_permission` nhưng CHƯA dùng trong code)
- `document_shares` — N-N permission (doc ↔ user)
- `document_operations` — OT operations log
- `document_versions` — version snapshots
- `images` — metadata ảnh
- `chat_messages` — chat per room
- `document_comments` — anchor comments

---

## 2. VẤN ĐỀ HIỆN TẠI

### 2.1 Vấn đề kiến trúc

| # | Vấn đề | Mức độ | Giải thích |
|---|--------|--------|-----------|
| 1 | **Token là GUID thuần, không có expiry** | 🔴 Critical | Token sống mãi cho đến khi server restart. Không có refresh mechanism. Nếu token bị leak → truy cập vĩnh viễn. |
| 2 | **Session in-memory, mất khi restart** | 🔴 Critical | Server restart = tất cả user bị logout. Không scale được nhiều server instance. |
| 3 | **Không có document visibility (public/private/restricted)** | 🟡 Medium | Cột `is_public` tồn tại trong DB nhưng code KHÔNG kiểm tra. Mọi doc chỉ accessible qua owner hoặc share. |
| 4 | **Thiếu role "commenter"** | 🟡 Medium | Hiện chỉ có viewer/editor. Viewer vẫn comment được (code cho phép bất kỳ ai có permission != null). |
| 5 | **Image upload không validate content** | 🟡 Medium | Chỉ check extension/mime từ client gửi lên. Không verify magic bytes → có thể upload file độc hại. |
| 6 | **Không có duplicate image detection** | 🟢 Low | Mỗi upload tạo file mới dù nội dung giống nhau. |
| 7 | **Không có rate limiting** | 🟡 Medium | Chỉ AI có throttle (3s/request). Các endpoint khác không giới hạn. |
| 8 | **Password trong connection string** | 🟡 Medium | Hardcode trong App.config, commit vào git. |
| 9 | **Packet size limit 64MB** | 🟢 Low | PacketHelper cho phép packet tới 64MB. Image chỉ 5MB nhưng document content không giới hạn. |
| 10 | **Không có audit log** | 🟡 Medium | Không track ai làm gì, khi nào. Khó debug và compliance. |

### 2.2 Vấn đề Image Upload cụ thể

| Tiêu chí | Đánh giá | Chi tiết |
|-----------|----------|----------|
| **Bảo mật** | ⚠️ Yếu | Không validate magic bytes, không scan malware, extension từ client có thể giả mạo |
| **Dung lượng** | ✅ OK | Giới hạn 5MB hợp lý |
| **Tốc độ** | ⚠️ Chậm | Toàn bộ binary đi qua TCP JSON (base64 encode → +33% size). 5MB file → ~6.7MB trên wire |
| **Lưu trữ** | ⚠️ Đơn giản | Local disk, không backup, không CDN, mất server = mất ảnh |
| **Duplicate** | ❌ Không có | Không hash check, upload cùng ảnh 10 lần = 10 file |
| **Validate** | ⚠️ Yếu | Chỉ check null/empty và size. Không check mime thực tế |
| **Giới hạn** | ⚠️ Thiếu | Không giới hạn số lượng ảnh per user, không quota |

---

## 3. THIẾT KẾ MỚI

### 3.1 Nguyên tắc thiết kế

1. **Backward compatible**: Giữ nguyên TCP protocol, thêm MessageType mới
2. **Incremental**: Thêm từng feature, không rewrite
3. **Additive schema**: Chỉ ADD column/table, không DROP/RENAME cột cũ
4. **Same patterns**: Tiếp tục dùng static Repository + Dapper + PacketHelper

### 3.2 Kiến trúc mục tiêu

```
┌─────────────────┐         TCP (port 5000)         ┌─────────────────────────────┐
│  WinForms Client │ ◄──── 4-byte len + JSON ────► │   .NET 4.7.2 Server          │
│                  │                                │                              │
│  + ProfileForm   │                                │  + VisibilityMiddleware      │
│  + SearchForm    │                                │  + CommentPermissionCheck    │
│  + PublicDocView │                                │  + TokenExpiry + Refresh     │
│                  │                                │  + RateLimiter               │
└─────────────────┘                                │  + ImageValidator            │
                                                   │  + AuditLogger               │
                                                   └──────────┬──────────────────┘
                                                              │
                                                    Npgsql + Dapper
                                                              │
                                                   ┌──────────▼──────────┐
                                                   │   PostgreSQL 16     │
                                                   │   (VPS Production)  │
                                                   └─────────────────────┘
                                                              │
                                                   ┌──────────▼──────────┐
                                                   │   File Storage      │
                                                   │   (VPS disk / MinIO)│
                                                   └─────────────────────┘
```

### 3.3 Visibility Model mới

```
Document.visibility = "public" | "private" | "restricted"

Quy tắc truy cập:
┌─────────────┬──────────────────────────────────────────────────┐
│ Visibility  │ Ai được truy cập                                  │
├─────────────┼──────────────────────────────────────────────────┤
│ public      │ Mọi user đã login (read theo public_permission)  │
│ private     │ Chỉ owner                                         │
│ restricted  │ Owner + users trong document_shares               │
├─────────────┼──────────────────────────────────────────────────┤
│ (mặc định)  │ restricted (giữ behavior hiện tại)               │
└─────────────┴──────────────────────────────────────────────────┘
```

### 3.4 Permission Model mới (thêm "commenter")

```
Roles: owner > editor > commenter > viewer

┌───────────┬──────┬──────┬─────────┬────────┐
│ Action    │owner │editor│commenter│ viewer │
├───────────┼──────┼──────┼─────────┼────────┤
│ Read      │  ✅  │  ✅  │   ✅    │   ✅   │
│ Edit      │  ✅  │  ✅  │   ❌    │   ❌   │
│ Comment   │  ✅  │  ✅  │   ✅    │   ❌   │
│ Share     │  ✅  │  ❌  │   ❌    │   ❌   │
│ Delete doc│  ✅  │  ❌  │   ❌    │   ❌   │
│ Manage    │  ✅  │  ❌  │   ❌    │   ❌   │
└───────────┴──────┴──────┴─────────┴────────┘
```

---

## 4. DATABASE SCHEMA

### 4.1 Migration script (additive, không phá schema cũ)

```sql
-- =============================================
-- Migration V2: Document Management Expansion
-- Chạy SAU schema_init.sql
-- =============================================

-- 0. Extension cần thiết
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- 1. Thêm cột visibility cho documents
ALTER TABLE documents ADD COLUMN IF NOT EXISTS visibility VARCHAR(20) DEFAULT 'restricted';
-- Note: CHECK constraint thêm riêng vì ADD COLUMN IF NOT EXISTS không hỗ trợ inline CHECK
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_documents_visibility') THEN
        ALTER TABLE documents ADD CONSTRAINT chk_documents_visibility
            CHECK (visibility IN ('public', 'private', 'restricted'));
    END IF;
END $$;

-- 2. Thêm soft delete cho documents
ALTER TABLE documents ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE DEFAULT NULL;
ALTER TABLE documents ADD COLUMN IF NOT EXISTS deleted_by INT DEFAULT NULL;

-- 3. Mở rộng permission trong document_shares (thêm 'commenter')
ALTER TABLE document_shares DROP CONSTRAINT IF EXISTS document_shares_permission_check;
ALTER TABLE document_shares ADD CONSTRAINT document_shares_permission_check
    CHECK (permission IN ('owner', 'editor', 'commenter', 'viewer'));

-- 4. Bảng Audit Logs
CREATE TABLE IF NOT EXISTS audit_logs (
    id              BIGSERIAL PRIMARY KEY,
    user_id         INT REFERENCES users(id) ON DELETE SET NULL,
    action          VARCHAR(50) NOT NULL,
    target_type     VARCHAR(30),
    target_id       VARCHAR(50),
    metadata        JSONB,
    ip_address      VARCHAR(45),
    created_at      TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_audit_logs_user ON audit_logs(user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_logs_target ON audit_logs(target_type, target_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_action ON audit_logs(action, created_at DESC);

-- 5. Bảng Sharing Links
CREATE TABLE IF NOT EXISTS sharing_links (
    id              VARCHAR(50) PRIMARY KEY DEFAULT 'link_' || gen_random_uuid()::text,
    doc_id          VARCHAR(50) NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    created_by      INT NOT NULL REFERENCES users(id),
    token           VARCHAR(64) NOT NULL UNIQUE,
    permission      VARCHAR(20) NOT NULL DEFAULT 'viewer'
                    CHECK (permission IN ('editor', 'commenter', 'viewer')),
    expires_at      TIMESTAMP WITH TIME ZONE,
    max_uses        INT,
    use_count       INT DEFAULT 0,
    is_active       BOOLEAN DEFAULT TRUE,
    created_at      TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_sharing_links_doc ON sharing_links(doc_id);
CREATE INDEX IF NOT EXISTS idx_sharing_links_token ON sharing_links(token);

-- 6. Thêm cột cho images
ALTER TABLE images ADD COLUMN IF NOT EXISTS sha256_hash VARCHAR(64);
ALTER TABLE images ADD COLUMN IF NOT EXISTS doc_id VARCHAR(50) REFERENCES documents(id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS idx_images_hash ON images(sha256_hash) WHERE sha256_hash IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_images_doc ON images(doc_id) WHERE doc_id IS NOT NULL;

-- 7. Bảng User Sessions (persistent, thay thế in-memory)
CREATE TABLE IF NOT EXISTS user_sessions (
    id              VARCHAR(50) PRIMARY KEY DEFAULT 'sess_' || gen_random_uuid()::text,
    user_id         INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token           VARCHAR(64) NOT NULL UNIQUE,
    refresh_token   VARCHAR(64) UNIQUE,
    expires_at      TIMESTAMP WITH TIME ZONE NOT NULL,
    refresh_expires_at TIMESTAMP WITH TIME ZONE,
    ip_address      VARCHAR(45),
    user_agent      TEXT,
    created_at      TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_active_at  TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_sessions_token ON user_sessions(token);
CREATE INDEX IF NOT EXISTS idx_sessions_refresh ON user_sessions(refresh_token) WHERE refresh_token IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_sessions_user ON user_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_sessions_expiry ON user_sessions(expires_at);

-- 8. Thêm profile fields cho users
ALTER TABLE users ADD COLUMN IF NOT EXISTS display_name VARCHAR(100);
ALTER TABLE users ADD COLUMN IF NOT EXISTS bio TEXT;
ALTER TABLE users ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT TRUE;
ALTER TABLE users ADD COLUMN IF NOT EXISTS last_login_at TIMESTAMP WITH TIME ZONE;

-- 9. Index cho document search
CREATE INDEX IF NOT EXISTS idx_documents_visibility ON documents(visibility) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_documents_public ON documents(visibility, updated_at DESC)
    WHERE visibility = 'public' AND deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_documents_title_trgm ON documents USING gin(title gin_trgm_ops);
```

### 4.2 Schema tổng quan (sau migration)

```
┌─────────────────────────────────────────────────────────────────┐
│                         DATABASE SCHEMA                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  users ──────────┬──── documents ──────┬──── document_shares    │
│  (id, username,  │    (id, owner_id,   │    (doc_id, user_id,   │
│   email, hash,   │     visibility,     │     permission)        │
│   avatar, bio,   │     title, content, │                        │
│   display_name)  │     deleted_at)     └──── sharing_links      │
│                  │         │                 (doc_id, token,     │
│                  │         │                  permission, expiry)│
│                  │         │                                     │
│                  │         ├──── document_operations (OT)        │
│                  │         ├──── document_versions (snapshots)   │
│                  │         ├──── document_comments               │
│                  │         ├──── chat_messages                   │
│                  │         └──── images (doc_id FK, sha256)      │
│                  │                                               │
│                  ├──── user_sessions (token, refresh, expiry)    │
│                  └──── audit_logs (action, target, metadata)     │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 4.3 Cascade & Soft Delete Rules

| Relation | Rule | Lý do |
|----------|------|-------|
| documents.owner_id → users.id | CASCADE | User bị xóa → xóa hết doc |
| document_shares → documents | CASCADE | Doc bị xóa → xóa shares |
| document_shares → users | CASCADE | User bị xóa → xóa shares |
| document_comments → documents | CASCADE | Doc bị xóa → xóa comments |
| images.doc_id → documents | SET NULL | Doc bị xóa → ảnh vẫn còn (orphan cleanup job) |
| audit_logs.user_id → users | SET NULL | User bị xóa → log vẫn còn |
| documents (soft delete) | `deleted_at` timestamp | Cho phép restore, 30 ngày sau mới hard delete |

---

## 5. API / PROTOCOL STRUCTURE

### 5.1 MessageType mới cần thêm vào Packet.cs

```csharp
// Thêm vào enum MessageType
DOC_SET_VISIBILITY,         // Owner thay đổi visibility
DOC_GET_PUBLIC_LIST,        // Lấy danh sách doc public
DOC_SEARCH,                 // Tìm kiếm doc theo ID hoặc title
DOC_DELETE,                 // Soft delete document

PROFILE_GET,                // Lấy profile user
PROFILE_UPDATE,             // Cập nhật profile

DOC_CREATE_LINK,            // Tạo sharing link
DOC_REVOKE_LINK,            // Vô hiệu hóa link
DOC_JOIN_LINK,              // Join doc qua sharing link

AUTH_REFRESH,               // Refresh token
AUTH_VALIDATE,              // Validate token còn sống không
```

### 5.2 Chi tiết từng API

#### DOC_SET_VISIBILITY

| Field | Value |
|-------|-------|
| Request | `{ docID, visibility: "public"\|"private"\|"restricted", publicPermission?: "viewer"\|"commenter"\|"editor" }` |
| Response | `{ success, message }` |
| Auth | owner only |
| Error | 403 nếu không phải owner |

#### DOC_GET_PUBLIC_LIST

| Field | Value |
|-------|-------|
| Request | `{ page?: 1, limit?: 20, sortBy?: "updated_at" }` |
| Response | `{ documents: [{ docID, title, ownerUsername, updatedAt, publicPermission }], total }` |
| Auth | Bất kỳ user đã login |

#### DOC_SEARCH

| Field | Value |
|-------|-------|
| Request | `{ query: string, searchBy: "id"\|"title"\|"all" }` |
| Response | `{ results: [{ docID, title, ownerUsername, visibility, permission }] }` |
| Auth | User đã login |
| Logic | searchBy="id" → exact match; searchBy="title" → ILIKE trên docs user có quyền + public |

#### PROFILE_GET

| Field | Value |
|-------|-------|
| Request | `{ targetUserId?: int, targetUsername?: string }` |
| Response | `{ userId, username, displayName, bio, avatarUrl, createdAt, publicDocuments: [...], sharedWithMe: [...] }` |
| Auth | User đã login |
| Logic | Xem người khác: chỉ public docs. Xem mình: tất cả docs |

#### PROFILE_UPDATE

| Field | Value |
|-------|-------|
| Request | `{ displayName?, bio? }` |
| Response | `{ success, message }` |
| Auth | Chỉ user đó |

#### DOC_CREATE_LINK

| Field | Value |
|-------|-------|
| Request | `{ docID, permission: "viewer"\|"commenter"\|"editor", expiresInHours?: int, maxUses?: int }` |
| Response | `{ success, linkToken }` |
| Auth | owner only |

#### DOC_JOIN_LINK

| Field | Value |
|-------|-------|
| Request | `{ linkToken: string }` |
| Response | `{ success, docID, title, content, revision, permission, message }` |
| Auth | User đã login |
| Logic | Validate link → tạo share → increment use_count |

#### AUTH_REFRESH

| Field | Value |
|-------|-------|
| Request | `{ refreshToken: string }` |
| Response | `{ success, token, refreshToken, expiresAt, message }` |
| Auth | None (unauthenticated) |

#### COMMENT_CREATE (cập nhật logic)

| Field | Value |
|-------|-------|
| Permission check MỚI | owner/editor/commenter: ✅ comment. viewer: ❌ trả error |

---

## 6. SECURITY ARCHITECTURE

### 6.1 Authentication: Token + Refresh Token

**Quyết định**: Giữ custom token (không dùng JWT) vì:
- Protocol là TCP custom, không phải HTTP → JWT overhead không cần thiết
- Server-side validation qua DB lookup đủ nhanh với index
- Dễ revoke (xóa record trong DB)
- Phù hợp với kiến trúc stateful TCP connection hiện tại

**Token specs**:
- Access token: 32 chars hex (GUID), TTL = 2 giờ
- Refresh token: 64 chars (crypto random), TTL = 30 ngày
- Mỗi user tối đa 5 sessions đồng thời (FIFO eviction)

**Flow**:

```
Login → Server tạo (token + refreshToken) → lưu user_sessions → trả client
         ↓
Client gửi token trong mỗi Packet.Token
         ↓
Server validate: SELECT FROM user_sessions WHERE token=@t AND expires_at > NOW()
         ↓
Token hết hạn → Client gửi AUTH_REFRESH(refreshToken)
         ↓
Server: validate refresh → tạo token pair mới → invalidate cũ → trả về
         ↓
Refresh cũng hết hạn → Client phải login lại
```

### 6.2 Authorization Middleware (cập nhật GetPermission)

```csharp
// Thay thế GetPermission hiện tại trong ClientHandler
private string ResolvePermission(string docId, int userId)
{
    var doc = DocumentRepository.GetById(docId);
    if (doc == null || doc.DeletedAt != null) return null;

    // 1. Owner check
    if (doc.OwnerId == userId) return "owner";

    // 2. Direct share check
    string sharePerm = DocumentShareRepository.GetSharePermission(docId, userId);
    if (sharePerm != null) return sharePerm;

    // 3. Public document check
    if (doc.Visibility == "public") return doc.PublicPermission ?? "viewer";

    // 4. Private hoặc restricted mà không có share → null
    return null;
}

// Helper methods cho permission check
private bool CanEdit(string permission) => permission == "owner" || permission == "editor";
private bool CanComment(string permission) => permission == "owner" || permission == "editor" || permission == "commenter";
private bool CanRead(string permission) => permission != null;
private bool IsOwner(string permission) => permission == "owner";
```

### 6.3 Rate Limiting

```csharp
public static class RateLimiter
{
    // Sliding window per user per category
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> _windows = new();

    private static readonly Dictionary<string, (int maxRequests, int windowSeconds)> _limits = new()
    {
        ["auth"]    = (5, 60),      // 5 login attempts / phút
        ["doc"]     = (60, 60),     // 60 doc operations / phút
        ["upload"]  = (10, 60),     // 10 uploads / phút
        ["chat"]    = (30, 60),     // 30 messages / phút
        ["ai"]      = (5, 60),      // 5 AI requests / phút
        ["search"]  = (20, 60),     // 20 searches / phút
    };

    public static bool IsAllowed(int userId, string category)
    {
        string key = $"{userId}:{category}";
        var (max, window) = _limits.GetValueOrDefault(category, (100, 60));

        var queue = _windows.GetOrAdd(key, _ => new Queue<DateTime>());
        var cutoff = DateTime.UtcNow.AddSeconds(-window);

        lock (queue)
        {
            while (queue.Count > 0 && queue.Peek() < cutoff)
                queue.Dequeue();

            if (queue.Count >= max) return false;

            queue.Enqueue(DateTime.UtcNow);
            return true;
        }
    }
}
```

### 6.4 Security Checklist

| Threat | Mitigation | Implementation |
|--------|-----------|----------------|
| **SQL Injection** | ✅ Đã an toàn | Dapper parameterized queries (đã dùng @param) |
| **XSS** | N/A | WinForms client, không render HTML |
| **CSRF** | N/A | TCP protocol, không phải HTTP |
| **Token theft** | Token expiry + refresh | user_sessions table với TTL |
| **Brute force login** | Rate limit auth | 5 attempts/phút |
| **File upload attack** | Magic bytes validation | ImageValidator service |
| **DoS** | Rate limit + packet size | Per-user limits + 10MB max packet |
| **Data leak** | Visibility + permission | ResolvePermission middleware |
| **Privilege escalation** | Server-side permission check | Mọi handler đều check permission |
| **Connection string leak** | Environment variables | Đọc từ env thay vì hardcode |

### 6.5 Secure Connection String

```csharp
// Thay đổi DbConnectionFactory.cs
public static class DbConnectionFactory
{
    private static readonly string _connectionString =
        Environment.GetEnvironmentVariable("MARKTOGETHER_DB_CONNECTION")
        ?? ConfigurationManager.ConnectionStrings["MarkTogetherDb"]?.ConnectionString
        ?? "Host=localhost;Port=5432;Database=myapp_db;Username=postgres;Password=123";

    public static IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
```

---

## 7. UPLOAD ARCHITECTURE

### 7.1 Vấn đề hiện tại và giải pháp

**Hiện tại**: Client gửi byte[] trong JSON packet qua TCP → Newtonsoft tự base64 encode → +33% overhead.

**Đề xuất**: Giữ nguyên flow TCP (vì WinForms client, không có HTTP endpoint riêng) nhưng cải thiện:

### 7.2 Kiến trúc Upload mới

> Cập nhật 2026-05-21: Client không lưu đường dẫn `file:///...` vào nội dung Markdown nữa. Markdown chỉ lưu `marktogether-image://{imageId}`. Khi preview, client resolve `imageId` sang file cache local; nếu cache mất thì tải lại ảnh từ server bằng `IMAGE_GET`.

```
Client                          Server                              Storage
  │                               │                                    │
  │  IMAGE_UPLOAD                 │                                    │
  │  { fileName, mimeType,       │                                    │
  │    data: byte[], docID }     │                                    │
  │──────────────────────────────►│                                    │
  │                               │  1. Validate size (≤5MB)           │
  │                               │  2. Validate magic bytes           │
  │                               │  3. Compute SHA256 hash            │
  │                               │  4. Check duplicate (hash lookup)  │
  │                               │     ├── Duplicate found → return existing URL
  │                               │     └── New file:                  │
  │                               │         5. Save to disk            │
  │                               │────────────────────────────────────►│
  │                               │         6. Save metadata to DB     │
  │                               │         7. Log audit               │
  │                               │                                    │
  │  IMAGE_UPLOAD_Response        │                                    │
  │  { success, imageId, url }   │                                    │
  │◄──────────────────────────────│                                    │
```

### 7.2.1 Markdown Image Reference Architecture ✅ ĐÃ CẬP NHẬT

#### Vấn đề cũ

Trước đây client chèn ảnh bằng cách cache file vào `%TEMP%` rồi lưu trực tiếp đường dẫn local vào Markdown:

```markdown
![Screenshot](file:///C:/Users/admin/AppData/Local/Temp/MarkTogether/doc_xxx/img_xxx.png)
```

Cách này gây lỗi khi:

- File trong `%TEMP%` bị xóa.
- Mở lại document sau khi cache mất.
- User khác mở document trên máy khác.
- Đóng gói client gửi cho bạn bè nhưng Markdown vẫn trỏ tới path của máy người upload.

#### Thiết kế mới

Markdown chỉ lưu reference ổn định theo `imageId` server:

```markdown
![Screenshot](marktogether-image://img_xxx)
```

Khi render preview:

```text
Markdown content
    │
    ├─ chứa: marktogether-image://img_xxx
    │
    ▼
PrepareMarkdownImagesForPreview()
    │
    ├─ EnsureImageCached(imageId)
    │     ├─ Nếu có cache local:
    │     │    dùng %TEMP%/MarkTogether/{docId}/{imageId}.ext
    │     │
    │     └─ Nếu chưa có cache:
    │          gọi SocketClient.GetImage(imageId)
    │          nhận bytes từ server
    │          ghi xuống cache local
    │
    ▼
Markdown preview tạm thời
    │
    ├─ đổi marktogether-image://img_xxx
    │  thành file:///C:/Users/.../Temp/MarkTogether/{docId}/img_xxx.png
    │
    ▼
Markdown.ToHtml()
    │
    ▼
WebView2 render ảnh từ file cache local
```

#### Quy tắc quan trọng

Nội dung document lưu trong database **không được chứa** đường dẫn tuyệt đối dạng:

```text
file:///C:/Users/...
```

Document chỉ được lưu dạng:

```text
marktogether-image://{imageId}
```

`file:///...` chỉ là đường dẫn tạm trong runtime preview, không lưu vào DB.

#### Code client liên quan

File:

```text
MarkTogether.Client/TypeRenderForm.cs
```

Các hàm chính:

```csharp
PrepareMarkdownImagesForPreview(string markdown)
EnsureImageCached(string imageId)
CacheImageLocallyAsync(string imageId, string mime, byte[] data)
```

Flow chèn ảnh hiện tại:

```csharp
var resp = SocketClient.Instance.UploadImage(fileName, mime, bytes, false, _docId);
await CacheImageLocallyAsync(resp.imageId, mime, bytes);
string snippet = $"![{Path.GetFileNameWithoutExtension(fileName)}](marktogether-image://{resp.imageId})";
```

Flow tải lại ảnh khi cache mất:

```csharp
var resp = SocketClient.Instance.GetImage(imageId);
File.WriteAllBytes(localPath, resp.data);
```

#### Backward compatibility

Các document cũ đã lưu ảnh dạng:

```markdown
![...](file:///C:/Users/admin/AppData/Local/Temp/...)
```

không thể tự chuyển chắc chắn sang `marktogether-image://...` vì nội dung cũ không lưu `imageId` server theo format chuẩn.

Cách xử lý thủ công:

```text
Xóa dòng ảnh cũ.
Bấm Chèn ảnh lại.
Lưu document.
```

Sau khi chèn lại, ảnh sẽ dùng `marktogether-image://...` và có thể mở trên máy khác.

### 7.3 ImageValidator Service (mới)

```csharp
public static class ImageValidator
{
    // Magic bytes cho các format phổ biến
    private static readonly Dictionary<string, byte[][]> _signatures = new()
    {
        ["image/png"]  = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
        ["image/jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        ["image/gif"]  = new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } },
        ["image/webp"] = new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } }, // RIFF
        ["image/bmp"]  = new[] { new byte[] { 0x42, 0x4D } },
    };

    private static readonly HashSet<string> _allowedMimes = new()
    {
        "image/png", "image/jpeg", "image/jpg", "image/gif", "image/webp", "image/bmp"
    };

    public static (bool valid, string error) Validate(byte[] data, string claimedMime, string fileName)
    {
        if (data == null || data.Length == 0)
            return (false, "Dữ liệu ảnh rỗng");

        if (data.Length > ImageStorageService.MaxImageBytes)
            return (false, $"Ảnh vượt quá {ImageStorageService.MaxImageBytes / (1024*1024)} MB");

        // Normalize mime
        string mime = (claimedMime ?? "").ToLowerInvariant();
        if (!_allowedMimes.Contains(mime))
            return (false, $"Định dạng '{mime}' không được hỗ trợ");

        // Validate magic bytes
        if (!MatchesMagicBytes(data, mime))
            return (false, "Nội dung file không khớp với định dạng khai báo");

        // Validate extension
        string ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        if (!string.IsNullOrEmpty(ext) && !IsExtensionValid(ext, mime))
            return (false, "Extension không khớp với mime type");

        return (true, null);
    }

    private static bool MatchesMagicBytes(byte[] data, string mime)
    {
        if (!_signatures.TryGetValue(mime, out var sigs)) return true; // unknown = pass
        foreach (var sig in sigs)
        {
            if (data.Length >= sig.Length && data.Take(sig.Length).SequenceEqual(sig))
                return true;
        }
        return false;
    }

    private static bool IsExtensionValid(string ext, string mime)
    {
        var validExts = new Dictionary<string, string[]>
        {
            ["image/png"]  = new[] { ".png" },
            ["image/jpeg"] = new[] { ".jpg", ".jpeg" },
            ["image/gif"]  = new[] { ".gif" },
            ["image/webp"] = new[] { ".webp" },
            ["image/bmp"]  = new[] { ".bmp" },
        };
        if (!validExts.TryGetValue(mime, out var exts)) return true;
        return exts.Contains(ext);
    }

    public static string ComputeSha256(byte[] data)
    {
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
```

### 7.4 Cải tiến ImageStorageService

```csharp
// Thêm vào ImageStorageService.Save()
public static string SaveWithDedup(int userId, string fileName, byte[] data, string mimeType, string docId)
{
    // 1. Validate
    var (valid, error) = ImageValidator.Validate(data, mimeType, fileName);
    if (!valid) throw new InvalidOperationException(error);

    // 2. Compute hash
    string hash = ImageValidator.ComputeSha256(data);

    // 3. Check duplicate
    var existing = ImageRepository.GetByHash(hash);
    if (existing != null)
    {
        // Ảnh đã tồn tại → trả URL cũ, tạo reference mới nếu cần
        return existing.Url;
    }

    // 4. Save file (logic cũ)
    string url = Save(userId, fileName, data, mimeType);

    // 5. Save metadata với hash
    ImageRepository.Create(new Image
    {
        UserId = userId,
        FileName = fileName,
        MimeType = mimeType,
        Url = url,
        Size = data.Length,
        Sha256Hash = hash,
        DocId = docId
    });

    return url;
}
```

### 7.5 So sánh: Gửi file trực tiếp vs Object Storage

| Tiêu chí | Gửi qua TCP (hiện tại) | Object Storage (MinIO/S3) |
|-----------|------------------------|---------------------------|
| **Phù hợp với kiến trúc** | ✅ Không cần thay đổi protocol | ❌ Cần thêm HTTP endpoint hoặc presigned URL |
| **Performance** | ⚠️ Base64 overhead +33% | ✅ Binary upload trực tiếp |
| **Complexity** | ✅ Đơn giản | ⚠️ Thêm service, config |
| **Scalability** | ⚠️ Giới hạn bởi server RAM | ✅ Scale riêng |
| **CDN** | ❌ Không hỗ trợ | ✅ Dễ tích hợp |
| **Backup** | ⚠️ Phải tự backup disk | ✅ Built-in replication |

**Quyết định cho giai đoạn hiện tại**: Giữ TCP upload vì:
1. WinForms client không có HTTP client riêng cho upload
2. 5MB limit đủ nhỏ, base64 overhead chấp nhận được
3. Không muốn phá vỡ kiến trúc hiện tại
4. Khi cần scale → migrate sang MinIO (Phase 3)

**Quyết định cho production VPS**: Lưu file trên disk VPS tại `/opt/marktogether/storage/images/`. Backup bằng cron job rsync sang backup server.

---

## 8. DEPLOYMENT ARCHITECTURE

### 8.1 Kiến trúc Production VPS

```
┌─────────────────────────────────────────────────────────────────────┐
│                          VPS (Ubuntu 22.04)                          │
│                                                                      │
│  ┌──────────────────────┐    ┌──────────────────────────────────┐   │
│  │  MarkTogether Server │    │  PostgreSQL 16 (Docker)           │   │
│  │  (.NET 4.7.2 / Mono) │    │  Port: 5432 (internal only)      │   │
│  │  Port: 5000 (public) │───►│  Volume: /var/lib/pg_data        │   │
│  │                       │    │  Backup: daily pg_dump            │   │
│  └──────────────────────┘    └──────────────────────────────────┘   │
│           │                                                          │
│           │                  ┌──────────────────────────────────┐   │
│           └─────────────────►│  File Storage                     │   │
│                              │  /opt/marktogether/storage/       │   │
│                              │  Backup: rsync daily              │   │
│                              └──────────────────────────────────┘   │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  Systemd Service: marktogether.service                        │   │
│  │  Auto-restart on failure                                      │   │
│  │  Environment file: /etc/marktogether/env                      │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  UFW Firewall                                                 │   │
│  │  - Allow TCP 5000 (app)                                       │   │
│  │  - Allow TCP 22 (SSH)                                         │   │
│  │  - Deny all other inbound                                     │   │
│  │  - PostgreSQL 5432: localhost only                             │   │
│  └──────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

### 8.2 Docker Compose Production ✅ ĐÃ TRIỂN KHAI

File thực tế đã tạo: `docker-compose.prod.yml`

```yaml
services:
  postgres:
    image: postgres:16
    container_name: marktogether-postgres
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${MARKTOGETHER_DB_NAME:-marktogether_db}
      POSTGRES_USER: ${MARKTOGETHER_DB_USER:-marktogether_user}
      POSTGRES_PASSWORD: ${MARKTOGETHER_DB_PASSWORD:?MARKTOGETHER_DB_PASSWORD is required}
    ports:
      - "127.0.0.1:${MARKTOGETHER_DB_PORT:-5432}:5432"
    volumes:
      - /var/lib/marktogether/pg_data:/var/lib/postgresql/data
      - ./MarkTogether.Server/Database/Scripts/schema_init.sql:/docker-entrypoint-initdb.d/01_schema_init.sql:ro
      - ./MarkTogether.Server/Database/Scripts/migration_v2.sql:/docker-entrypoint-initdb.d/02_migration_v2.sql:ro
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${MARKTOGETHER_DB_USER:-marktogether_user} -d ${MARKTOGETHER_DB_NAME:-marktogether_db}"]
      interval: 10s
      timeout: 5s
      retries: 5
    mem_limit: 512m
    security_opt:
      - no-new-privileges:true
    logging:
      driver: json-file
      options:
        max-size: "10m"
        max-file: "3"
```

**Triển khai thực tế:**
- Giữ `docker-compose.yml` hiện tại cho local/dev để backward compatible.
- Thêm `docker-compose.prod.yml` riêng cho production.
- PostgreSQL chỉ bind `127.0.0.1`, không expose internet.
- Password lấy từ `/opt/marktogether/app/.env`, không commit secret vào repo.
- `schema_init.sql` và `migration_v2.sql` được mount cho DB mới.
- DB đã tồn tại dùng script migration riêng: `deploy/scripts/run-migrations.sh`.

**Tradeoff:**
- Chọn PostgreSQL trên cùng App VPS cho giai đoạn hiện tại để đơn giản và tránh expose DB public.
- Nếu tách DB VPS riêng, cần private network/VPN hoặc firewall allowlist chặt chẽ.

### 8.3 Environment File ✅ ĐÃ TRIỂN KHAI

Template đã tạo: `deploy/env/marktogether.env.example`

```bash
# /etc/marktogether/env
# chmod 640, owned by root:marktogether
MARKTOGETHER_PORT=5000
MARKTOGETHER_STORAGE_PATH=/opt/marktogether/storage
MARKTOGETHER_DB_CONNECTION=Host=127.0.0.1;Port=5432;Database=marktogether_db;Username=marktogether_user;Password=CHANGE_ME_STRONG_PASSWORD
MARKTOGETHER_GEMINI_KEY=
```

**Code runtime đã hỗ trợ:**
- `DbConnectionFactory.cs` ưu tiên `MARKTOGETHER_DB_CONNECTION`.
- `Program.cs` đã được cập nhật để đọc `MARKTOGETHER_PORT`, fallback `5000`.
- `ImageStorageService.cs` đã được cập nhật để đọc `MARKTOGETHER_STORAGE_PATH`, fallback storage local cũ.

**Security note:**
- Không ghi mật khẩu VPS/DB thật vào repository.
- Secret chỉ nhập trực tiếp trên VPS trong `/etc/marktogether/env` và `/opt/marktogether/app/.env`.

### 8.4 Systemd Service ✅ ĐÃ TRIỂN KHAI

File thực tế đã tạo: `deploy/systemd/marktogether.service`

```ini
[Unit]
Description=MarkTogether Collaboration Server
Documentation=file:/opt/marktogether/DEPLOYMENT.md
After=network-online.target docker.service
Wants=network-online.target
Requires=docker.service

[Service]
Type=simple
User=marktogether
Group=marktogether
WorkingDirectory=/opt/marktogether/server
EnvironmentFile=/etc/marktogether/env
ExecStart=/usr/bin/mono /opt/marktogether/server/MarkTogether.Server.exe --service
Restart=always
RestartSec=5
KillSignal=SIGINT
TimeoutStopSec=30
StandardOutput=journal
StandardError=journal
SyslogIdentifier=marktogether
LimitNOFILE=65535

# Security hardening
NoNewPrivileges=true
PrivateTmp=true
ProtectHome=true
ProtectSystem=strict
ReadWritePaths=/opt/marktogether/storage /opt/marktogether/backups /opt/marktogether/logs

[Install]
WantedBy=multi-user.target
```

**Lý do thêm `--service`:**
- Tránh server chờ `Console.ReadLine()` khi chạy dưới systemd.
- Cho phép process sống ổn định đến khi nhận signal dừng.

**Backward compatibility:**
- Chạy interactive trên Windows/local vẫn giữ flow cũ nếu không truyền `--service`.

### 8.5 Backup / Migration / Deploy Scripts ✅ ĐÃ TRIỂN KHAI

#### Backup

File thực tế: `deploy/scripts/backup.sh`

Chức năng:
- Validate container PostgreSQL đang chạy.
- Dump DB bằng `pg_dump`.
- Nén thành `db_YYYYMMDD_HHMMSS.sql.gz`.
- Sync storage bằng `rsync --delete`.
- Xóa backup DB cũ sau `MARKTOGETHER_BACKUP_RETENTION_DAYS`, mặc định 7 ngày.

Cron đề xuất:

```cron
0 3 * * * /opt/marktogether/app/deploy/scripts/backup.sh >> /opt/marktogether/logs/backup.log 2>&1
```

#### Migration

File thực tế: `deploy/scripts/run-migrations.sh`

Lý do cần script riêng:
- Docker entrypoint chỉ chạy SQL init khi volume DB mới được tạo.
- Với DB production đã tồn tại, cần apply `migration_v2.sql` thủ công/idempotent.

Cách chạy:

```bash
cd /opt/marktogether/app
./deploy/scripts/run-migrations.sh
```

#### Release deploy

File thực tế: `deploy/scripts/deploy-release.sh`

Chức năng:
- Nhận release folder, ví dụ `/tmp/marktogether-release`.
- Backup release hiện tại vào `/opt/marktogether/release_backups/server_YYYYMMDD_HHMMSS`.
- Sync release mới vào `/opt/marktogether/server`.
- Set ownership `marktogether:marktogether`.
- Restart service.

Cách chạy:

```bash
./deploy/scripts/deploy-release.sh /tmp/marktogether-release
```

### 8.6 Client → Server Connection

```
Client (WinForms)
    │
    │  TCP connect to VPS_PUBLIC_IP:5000
    │
    ▼
VPS Firewall (UFW allow 5000)
    │
    ▼
MarkTogether Server Process (Mono/.NET)
    │
    │  Npgsql connect to 127.0.0.1:5432
    │
    ▼
PostgreSQL (Docker, localhost only)
```

**Client config** (`server.config`):
```xml
<appSettings>
    <add key="ServerHost" value="159.203.184.87" />
    <add key="ServerPort" value="5000" />
</appSettings>
```

### 8.7 Quy trình update server thực tế ✅ ĐÃ TRIỂN KHAI

Tài liệu chi tiết đã tạo: `Plan/PHASE6_PRODUCTION_DEPLOYMENT.md`

#### Build Release trên Windows

```cmd
cd /d D:\hoc_tren_truong\CN\lap_trinh_mang\Đồ An\NT106_Project_MarkTogether

"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" MarkTogether.Server\Server.csproj /p:Configuration=Release /t:Rebuild
```

#### Upload release lên App VPS

```cmd
scp -r MarkTogether.Server\bin\Release\* root@159.203.184.87:/tmp/marktogether-release/
```

#### Deploy trên VPS

```bash
cd /opt/marktogether/app

./deploy/scripts/backup.sh
./deploy/scripts/run-migrations.sh
./deploy/scripts/deploy-release.sh /tmp/marktogether-release

systemctl status marktogether --no-pager
journalctl -u marktogether -f
```

### 8.8 Phase 6 security checklist ✅ ĐÃ KIỂM TRA

- [x] PostgreSQL production bind `127.0.0.1`, không public internet.
- [x] Không commit mật khẩu VPS/DB thật vào repo.
- [x] `/etc/marktogether/env` dùng permission khuyến nghị `640`.
- [x] Server chạy bằng user riêng `marktogether`.
- [x] `systemd` có hardening cơ bản.
- [x] Storage tách khỏi release folder.
- [x] Có backup DB + storage trước migration/deploy.
- [x] Có rollback release bằng thư mục backup.
- [x] Build Release server thành công.
- [x] Không đổi TCP protocol/API cũ.

---

## 9. REALTIME COLLABORATION

### 9.1 Đánh giá hệ thống hiện tại

| Aspect | Hiện tại | Đánh giá |
|--------|----------|----------|
| **Protocol** | TCP persistent connection | ✅ Tốt cho realtime |
| **Algorithm** | OT (Operational Transformation) | ✅ Phù hợp, đã implement |
| **Broadcast** | SessionManager.BroadcastToRoom | ✅ Hoạt động tốt |
| **Conflict resolution** | Server-authoritative OT | ✅ Đúng pattern |
| **Autosave** | Mỗi lần DOC_SAVE tạo version | ⚠️ Chỉ khi user chủ động save |
| **Typing indicator** | Chưa có | 🟡 Nice-to-have |

### 9.2 Đề xuất cải tiến

#### Autosave (Priority: High)

```csharp
// Client-side: Timer autosave mỗi 30 giây nếu có thay đổi
private Timer _autosaveTimer;
private bool _hasUnsavedChanges = false;

private void InitAutosave()
{
    _autosaveTimer = new Timer(30000); // 30s
    _autosaveTimer.Elapsed += (s, e) =>
    {
        if (_hasUnsavedChanges && !string.IsNullOrEmpty(_currentDocId))
        {
            try
            {
                SocketClient.Instance.SaveDocument(_currentDocId, GetCurrentContent());
                _hasUnsavedChanges = false;
            }
            catch { /* retry next cycle */ }
        }
    };
    _autosaveTimer.Start();
}
```

#### Typing Indicator (Priority: Low)

```csharp
// Thêm MessageType
TYPING_INDICATOR,  // { docID, userId, username, isTyping }

// Server: broadcast to room, không persist
// Client: hiển thị "User X đang gõ..." với debounce 2s
```

### 9.3 OT vs CRDT — Có nên chuyển?

| Tiêu chí | OT (hiện tại) | CRDT |
|-----------|---------------|------|
| **Complexity** | Medium (đã implement) | High (phải rewrite) |
| **Server dependency** | Cần server transform | Có thể offline |
| **Correctness** | ✅ Proven | ✅ Proven |
| **Performance** | ✅ Tốt cho text | ⚠️ Metadata overhead |
| **Migration effort** | 0 (đã có) | 🔴 Rất lớn |

**Quyết định**: Giữ OT. Không có lý do chuyển sang CRDT vì:
1. Hệ thống đã hoạt động ổn định
2. Server luôn online (không cần offline-first)
3. Chi phí migration quá lớn so với lợi ích

---

## 10. FLOW HOẠT ĐỘNG

### 10.1 Flow: User mở document

```
1. Client gửi DOC_OPEN { docID }
2. Server:
   a. ResolvePermission(docId, userId)
   b. Nếu null:
      - Doc visibility = "public"? → return public_permission
      - Else → return ERROR "Không có quyền" hoặc "Tài liệu không tồn tại"
   c. Nếu có permission:
      - JoinRoom(docId, handler)
      - Return DOC_OPEN_Response { content, revision, permission, shareCode? }
3. Client render document theo permission:
   - viewer: readonly, no comment button
   - commenter: readonly + comment panel
   - editor/owner: full edit
```

### 10.2 Flow: Tìm kiếm document bằng ID

```
1. Client gửi DOC_SEARCH { query: "doc_abc123", searchBy: "id" }
2. Server:
   a. Tìm document theo ID exact match
   b. Nếu không tìm thấy → return "Không tìm thấy"
   c. Nếu tìm thấy:
      - ResolvePermission(docId, userId)
      - Nếu có quyền → return doc info + permission
      - Nếu doc.visibility = "private" và không có quyền → "Tài liệu không tồn tại"
      - Nếu doc.visibility = "restricted" và không có quyền → "Bạn không có quyền truy cập"
3. Client hiển thị kết quả, cho phép Open nếu có quyền
```

### 10.3 Flow: Chia sẻ document

```
1. Owner mở ShareManagementForm
2. Client gửi DOC_SHARE { docID, targetUsername, permission: "commenter" }
3. Server:
   a. Verify owner
   b. Tìm target user
   c. Upsert document_shares
   d. Log audit: { action: "doc.share", target_id: docId, metadata: { targetUser, permission } }
   e. Return success
4. Owner có thể:
   - Thay đổi permission (DOC_SHARE_UPDATE)
   - Thu hồi quyền (DOC_SHARE_REVOKE)
   - Tạo sharing link (DOC_CREATE_LINK)
```

### 10.4 Flow: Comment với permission check

```
1. User (commenter role) chọn text và gửi COMMENT_CREATE
2. Server:
   a. ResolvePermission → "commenter"
   b. CanComment("commenter") → true
   c. Tạo comment, broadcast
3. User (viewer role) thử comment:
   a. ResolvePermission → "viewer"
   b. CanComment("viewer") → false
   c. Return ERROR "Bạn chỉ có quyền xem, không được comment"
```

### 10.5 Flow: Profile page

```
1. User A xem profile User B:
   a. Client gửi PROFILE_GET { targetUsername: "userB" }
   b. Server:
      - Lấy user info (username, displayName, bio, avatar)
      - Query documents WHERE owner_id = B AND visibility = 'public' AND deleted_at IS NULL
      - Return profile + public docs only
2. User A xem profile mình:
   a. Client gửi PROFILE_GET { } (không có target → mặc định là mình)
   b. Server:
      - Lấy user info
      - Query tất cả docs (owned + shared)
      - Return profile + all accessible docs
```

---

## 11. SCALING & FUTURE-PROOF

### 11.1 Giai đoạn hiện tại (1-100 users)

| Component | Solution | Lý do |
|-----------|----------|-------|
| Cache | In-memory ConcurrentDictionary | Đủ cho scale nhỏ |
| Search | PostgreSQL pg_trgm + ILIKE | Đủ nhanh cho <10K docs |
| Queue | Không cần | Single server |
| CDN | Không cần | File serve qua TCP |
| Monitoring | Console.WriteLine + journalctl | Đơn giản |

### 11.2 Giai đoạn mở rộng (100-1000 users)

| Component | Solution | Khi nào cần |
|-----------|----------|-------------|
| **Redis** | Session cache + rate limit | Khi cần multiple server instances |
| **MinIO** | Object storage thay disk | Khi storage > 50GB hoặc cần CDN |
| **Elasticsearch** | Full-text search | Khi > 10K documents |
| **Message Queue** | RabbitMQ cho async tasks | Khi cần email notification, async processing |
| **Nginx** | TCP load balancer | Khi cần multiple server instances |

### 11.3 Monitoring & Logging

```csharp
// AuditLogger service
public static class AuditLogger
{
    public static void Log(int? userId, string action, string targetType, string targetId,
        object metadata = null)
    {
        try
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                db.Execute(
                    @"INSERT INTO audit_logs (user_id, action, target_type, target_id, metadata)
                      VALUES (@UserId, @Action, @TargetType, @TargetId, @Metadata::jsonb)",
                    new
                    {
                        UserId = userId,
                        Action = action,
                        TargetType = targetType,
                        TargetId = targetId,
                        Metadata = metadata != null
                            ? Newtonsoft.Json.JsonConvert.SerializeObject(metadata)
                            : null
                    });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Audit] Log failed: {ex.Message}");
        }
    }
}

// Usage trong ClientHandler:
AuditLogger.Log(_userId, "doc.create", "document", docId, new { title });
AuditLogger.Log(_userId, "doc.share", "document", docId, new { targetUser, permission });
AuditLogger.Log(_userId, "image.upload", "image", imageId, new { fileName, size });
```

### 11.4 Docker Deployment (Future)

```yaml
# docker-compose.full.yml (khi cần containerize server)
services:
  postgres:
    image: postgres:16
    # ... (như trên)

  server:
    build:
      context: .
      dockerfile: Dockerfile.server
    ports:
      - "5000:5000"
    environment:
      - MARKTOGETHER_DB_CONNECTION=Host=postgres;Port=5432;...
    volumes:
      - storage_data:/app/Storage
    depends_on:
      postgres:
        condition: service_healthy

  # Future: Redis for sessions
  # redis:
  #   image: redis:7-alpine
  #   ports:
  #     - "127.0.0.1:6379:6379"

volumes:
  pg_data:
  storage_data:
```

### 11.5 CI/CD Pipeline (GitHub Actions)

```yaml
# .github/workflows/deploy.yml
name: Deploy to VPS
on:
  push:
    branches: [main]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - name: Build Server
        run: |
          nuget restore MarkTogether.sln
          msbuild MarkTogether.Server/Server.csproj /p:Configuration=Release
      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: server-release
          path: MarkTogether.Server/bin/Release/

  deploy:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - name: Download artifact
        uses: actions/download-artifact@v4
      - name: Deploy to VPS
        uses: appleboy/scp-action@v0.1.7
        with:
          host: ${{ secrets.VPS_HOST }}
          username: ${{ secrets.VPS_USER }}
          key: ${{ secrets.VPS_SSH_KEY }}
          source: "server-release/*"
          target: "/opt/marktogether/server/"
      - name: Restart service
        uses: appleboy/ssh-action@v1.0.3
        with:
          host: ${{ secrets.VPS_HOST }}
          username: ${{ secrets.VPS_USER }}
          key: ${{ secrets.VPS_SSH_KEY }}
          script: sudo systemctl restart marktogether
```

---

## 12. CHECKLIST IMPLEMENTATION

> Cập nhật 2026-05-21: 4 phase đầu đã được triển khai theo hướng incremental/backward-compatible.  
> Lưu ý: thứ tự phase triển khai thực tế đã được điều chỉnh so với checklist ban đầu để giảm rủi ro breaking system:
> 1. Database schema & migrations  
> 2. RBAC & permission system  
> 3. Document visibility system  
> 4. Share system with secure links  

---

### Phase 1: Database Schema & Migrations ✅ HOÀN THÀNH 2026-05-21

#### Mục tiêu

Chuẩn hóa nền tảng DB cho các phase sau, đảm bảo:

- Additive migration, không drop/rename schema cũ.
- Có migration tracking.
- Có nền tảng cho visibility, RBAC/commenter, sharing links, sessions, audit logs, profile fields, image metadata.
- Production config không phụ thuộc hardcoded connection string.

#### Checklist đã hoàn thành

- [x] **1.1** Tạo/hoàn thiện `migration_v2.sql`
  - Thêm advisory lock bằng `pg_advisory_xact_lock`.
  - Thêm bảng `schema_migrations`.
  - Script idempotent, có thể chạy lại an toàn.

- [x] **1.2** Thêm schema nền cho document visibility
  - `documents.visibility`
  - `documents.deleted_at`
  - `documents.deleted_by`
  - constraint `chk_documents_visibility`
  - constraint `chk_documents_public_permission`
  - index visibility/public/search.

- [x] **1.3** Mở rộng permission schema
  - `document_shares.permission` hỗ trợ:
    - `owner`
    - `editor`
    - `commenter`
    - `viewer`

- [x] **1.4** Thêm bảng production/security foundation
  - `user_sessions`
  - `sharing_links`
  - `audit_logs`

- [x] **1.5** Thêm profile/image metadata fields
  - `users.display_name`
  - `users.bio`
  - `users.is_active`
  - `users.last_login_at`
  - `images.sha256_hash`
  - `images.doc_id`

- [x] **1.6** Cập nhật `DbConnectionFactory`
  - Ưu tiên `MARKTOGETHER_DB_CONNECTION`.
  - Fallback `App.config` để giữ backward compatibility.
  - Fallback local dev connection string cũ.

- [x] **1.7** Include migration vào project
  - `Server.csproj` đã include:
    - `Database\Scripts\schema_init.sql`
    - `Database\Scripts\migration_v2.sql`

#### Files đã sửa/tạo

- `MarkTogether.Server/Database/Scripts/migration_v2.sql`
- `MarkTogether.Server/Database/DbConnectionFactory.cs`
- `MarkTogether.Server/Server.csproj`

#### Verification

- `dotnet build MarkTogether.Server/Server.csproj --configuration Debug --no-restore`
  - Server build succeeded.
  - Còn warning cũ `SocketServer.cs CS4014`.

#### Backward compatibility

- Không đổi protocol.
- Không đổi handler.
- Không đổi payload.
- Không xóa schema cũ.
- Existing docs mặc định `restricted`, giữ behavior cũ.

---

### Phase 2: RBAC & Permission System ✅ HOÀN THÀNH 2026-05-21

#### Mục tiêu

Centralize permission logic, thêm role `commenter`, giảm string-literal permission check trong `ClientHandler`.

#### Checklist đã hoàn thành

- [x] **2.1** Thêm service RBAC trung tâm
  - `DocumentPermissionService`
  - Constants:
    - `owner`
    - `editor`
    - `commenter`
    - `viewer`

- [x] **2.2** Thêm action-based permission helpers
  - `CanRead(permission)`
  - `CanEdit(permission)`
  - `CanComment(permission)`
  - `CanManage(permission)`
  - `IsOwner(permission)`
  - `NormalizePermission(permission)`
  - `NormalizeSharePermission(permission)`

- [x] **2.3** Refactor `ClientHandler.GetPermission`
  - Từ gọi trực tiếp `DocumentShareRepository.GetPermission`
  - Sang gọi `DocumentPermissionService.ResolvePermission`

- [x] **2.4** Refactor document edit/save/OT permission
  - `DOC_SAVE`
  - `OP_INSERT`
  - `OP_DELETE`
  - `DOC_VERSION_RESTORE`
  - Chỉ `owner/editor` được edit.

- [x] **2.5** Refactor comment permission
  - `COMMENT_CREATE`
  - `COMMENT_RESOLVE`
  - `COMMENT_DELETE`
  - Viewer không còn được comment/resolve.
  - `owner/editor/commenter` được comment.

- [x] **2.6** Refactor share management permission
  - `DOC_SHARE`
  - `DOC_SHARE_LIST`
  - `DOC_SHARE_UPDATE`
  - `DOC_SHARE_REVOKE`
  - `DOC_SHARE_REGEN_CODE`
  - Chỉ `owner` được manage.

- [x] **2.7** Cho phép share role `commenter`
  - Server accept permission `commenter`.
  - Không cho set `owner` qua share payload.

- [x] **2.8** Thêm logging denied actions
  - Log dạng:
    - `[RBAC] <ACTION> denied user=<id> doc=<docId> permission=<perm>`

#### Files đã sửa/tạo

- `MarkTogether.Server/Services/DocumentPermissionService.cs`
- `MarkTogether.Server/Network/ClientHandler.cs`
- `MarkTogether.Server/Server.csproj`

#### Security impact

- Fix lỗi viewer có thể comment.
- Giảm nguy cơ privilege escalation do permission check phân tán.
- Share payload sai permission fallback về `viewer`.

#### Verification

- `dotnet build MarkTogether.Server/Server.csproj --configuration Debug --no-restore`
  - Server build succeeded.
  - Còn warning cũ `SocketServer.cs CS4014`.

#### Backward compatibility

- Không đổi MessageType cũ.
- Không đổi payload cũ.
- Owner/editor/viewer vẫn hoạt động.
- Thay đổi behavior có chủ đích:
  - Viewer không còn được comment/resolve comment.

---

### Phase 3: Document Visibility System ✅ HOÀN THÀNH 2026-05-21

#### Mục tiêu

Triển khai visibility model:

```text
public     → user đã login có thể đọc theo public_permission
private    → giữ an toàn, owner/direct share vẫn được ưu tiên để backward compatible
restricted → owner + users trong document_shares
```

#### Checklist đã hoàn thành

- [x] **3.1** Cập nhật shared protocol
  - Thêm `MessageType.DOC_SET_VISIBILITY`
  - Thêm `MessageType.DOC_GET_PUBLIC_LIST`

- [x] **3.2** Thêm payload DTO
  - `Payload_DOC_SET_VISIBILITY_Request`
  - `Payload_DOC_SET_VISIBILITY_Response`
  - `Payload_DOC_GET_PUBLIC_LIST_Request`
  - `Payload_DOC_GET_PUBLIC_LIST_Response`
  - `PublicDocumentDto`

- [x] **3.3** Cập nhật `Document` model
  - `Visibility`
  - `DeletedAt`
  - `DeletedBy`

- [x] **3.4** Cập nhật `DocumentRepository`
  - `UpdateVisibility(docId, visibility, publicPermission)`
  - `CountPublicDocuments()`
  - `GetPublicDocuments(page, limit)`
  - Filter `deleted_at IS NULL` cho các query list chính.
  - `Delete()` chuyển sang soft-delete cơ bản.

- [x] **3.5** Mở rộng `DocumentPermissionService.ResolvePermission`
  - Owner luôn là `owner`.
  - Direct share được ưu tiên để giữ backward compatibility.
  - Public document trả `public_permission`.
  - Deleted document không resolve permission.

- [x] **3.6** Implement server handlers
  - `HandleDocSetVisibility`
  - `HandleDocGetPublicList`

- [x] **3.7** Cập nhật response metadata
  - `DOC_OPEN` trả thêm:
    - `visibility`
    - `publicPermission`
  - `DOC_LIST`/`DocInfo` trả thêm:
    - `visibility`
    - `publicPermission`

#### Files đã sửa

- `MarkTogether.Shared/Packet.cs`
- `MarkTogether.Server/Database/Models/Document.cs`
- `MarkTogether.Server/Database/Repositories/DocumentRepository.cs`
- `MarkTogether.Server/Services/DocumentPermissionService.cs`
- `MarkTogether.Server/Network/ClientHandler.cs`

#### Security impact

- Public list chỉ trả metadata, không trả content.
- Public open vẫn yêu cầu login.
- `DOC_SET_VISIBILITY` owner-only.
- `publicPermission` không cho `owner`.
- Page limit public list giới hạn tối đa 100.

#### Verification

- `dotnet build MarkTogether.Server/Server.csproj --configuration Debug --no-restore`
  - Server build succeeded.
  - Còn warning cũ `SocketServer.cs CS4014`.

#### Backward compatibility

- Existing docs vẫn `restricted`.
- Client cũ bỏ qua field mới.
- Direct share vẫn được ưu tiên kể cả khi visibility thay đổi.

---

### Phase 4: Share System With Secure Links ✅ HOÀN THÀNH PHẦN SERVER CORE 2026-05-21

#### Mục tiêu

Bổ sung sharing link có token bảo mật, expiry, max uses và join bằng link token.

#### Checklist đã hoàn thành

- [x] **4.1** Cập nhật shared protocol
  - Thêm `MessageType.DOC_CREATE_LINK`
  - Thêm `MessageType.DOC_REVOKE_LINK`
  - Thêm `MessageType.DOC_JOIN_LINK`

- [x] **4.2** Thêm payload DTO
  - `Payload_DOC_CREATE_LINK_Request`
  - `Payload_DOC_CREATE_LINK_Response`
  - `Payload_DOC_REVOKE_LINK_Request`
  - `Payload_DOC_REVOKE_LINK_Response`
  - `Payload_DOC_JOIN_LINK_Request`
  - `Payload_DOC_JOIN_LINK_Response`

- [x] **4.3** Thêm model/repository
  - `SharingLink`
  - `SharingLinkRepository`
  - Methods:
    - `Create(...)`
    - `GetByToken(token)`
    - `RevokeByToken(token, requesterUserId)`
    - `IncrementUseCount(token)`

- [x] **4.4** Thêm secure token generator
  - `SecureTokenGenerator.GenerateUrlSafeToken(length = 48)`
  - Dùng `RandomNumberGenerator.Create()`
  - Token URL-safe alphabet.

- [x] **4.5** Implement server handlers
  - `HandleDocCreateLink`
  - `HandleDocRevokeLink`
  - `HandleDocJoinLink`

- [x] **4.6** Permission/security checks
  - Create link: owner-only.
  - Revoke link: owner-only qua repository join `documents`.
  - Join link:
    - token phải active.
    - check expiry.
    - check max uses.
    - check document tồn tại/chưa deleted.
    - permission normalize về `viewer/commenter/editor`.
    - không cấp `owner`.
  - Max expiry input được clamp tối đa 30 ngày.
  - Max uses input được clamp tối đa 1000.

- [x] **4.7** Include compile project
  - `Server.csproj` include:
    - `Database\Models\SharingLink.cs`
    - `Database\Repositories\SharingLinkRepository.cs`
    - `Services\SecureTokenGenerator.cs`

#### Files đã sửa/tạo

- `MarkTogether.Shared/Packet.cs`
- `MarkTogether.Server/Database/Models/SharingLink.cs`
- `MarkTogether.Server/Database/Repositories/SharingLinkRepository.cs`
- `MarkTogether.Server/Services/SecureTokenGenerator.cs`
- `MarkTogether.Server/Network/ClientHandler.cs`
- `MarkTogether.Server/Server.csproj`

#### Security impact

- Token sinh bằng CSPRNG.
- Không expose direct DB id làm secret.
- Link có thể expire.
- Link có thể giới hạn số lần dùng.
- Link revoke được.
- Permission từ link không thể là owner.
- Join link tạo/cập nhật direct share cho user hiện tại.

#### Verification

- `dotnet build MarkTogether.Server/Server.csproj --configuration Debug --no-restore`
  - Server build succeeded.
  - Còn warning cũ `SocketServer.cs CS4014`.

#### Backward compatibility

- Share by username cũ giữ nguyên.
- Share code cũ `DOC_JOIN_CODE` giữ nguyên.
- Sharing links là API mới, không phá client cũ.

#### Còn lại sau Phase 4

- [ ] Client UI cho tạo/revoke/join link.
- [ ] API list sharing links nếu muốn quản lý nhiều link trên UI.
- [ ] Audit log DB insert cho create/revoke/join link.
- [ ] Rate limit cho join link để chống brute force token.

### Phase 5: Image Improvements & Autosave (Tuần 9-10) ✅ HOÀN THÀNH 2026-05-21

- [x] **5.1** Thêm SHA256 hash vào image upload flow
  - Đã dùng `ImageValidator.ComputeSha256()`
  - Đã lưu `sha256_hash` vào bảng `images`
  - Đã cập nhật `ImageRepository.Create()` nhận `Sha256Hash` và `DocId`

- [x] **5.2** Implement duplicate detection (check hash trước khi save)
  - Đã thêm `ImageRepository.GetByHash()`
  - Đã thêm `ImageStorageService.SaveWithDedup()`
  - Nếu ảnh trùng SHA256, server trả lại metadata ảnh đã có thay vì ghi file mới

- [x] **5.3** Thêm image quota per user (100 images hoặc 500MB)
  - Đã thêm quota constants:
    - `MaxImagesPerUser = 100`
    - `MaxBytesPerUser = 500MB`
  - Đã thêm `CountByUserId()`, `SumSizeByUserId()`, `WouldExceedQuota()`
  - Upload mới bị từ chối nếu vượt quota

- [x] **5.4** Client: Implement autosave timer (30s)
  - Đã thêm `_autosaveTimer` trong `TypeRenderForm`
  - Chỉ autosave khi:
    - document có `_docId`
    - nội dung thay đổi so với `_lastSavedContent`
    - user là `owner` hoặc `editor`
    - client còn đăng nhập
  - Autosave dùng API `DOC_SAVE` hiện có để giữ backward compatibility

- [ ] **5.5** Client: Typing indicator (optional)
  - Chưa triển khai vì đây là optional item và cần protocol/broadcast UI riêng
  - Đề xuất chuyển sang Phase realtime riêng nếu cần

- [x] **5.6** Soft delete cho documents (DOC_DELETE → set deleted_at)
  - Đã thêm `DOC_DELETE` vào `MessageType`
  - Đã thêm payload request/response cho delete
  - Đã thêm `DocumentRepository.SoftDelete(docId, deletedBy)`
  - Đã thêm `ClientHandler.HandleDocDelete()`
  - Owner-only permission check
  - Broadcast `DOC_RELOAD_BROADCAST` với reason `deleted`

#### Phase 5 implementation notes

**Files đã sửa:**
- `MarkTogether.Shared/Packet.cs`
  - Thêm `DOC_SEARCH`, `DOC_DELETE`
  - Thêm payload DTO cho search/delete
- `MarkTogether.Server/Database/Repositories/ImageRepository.cs`
  - Thêm SHA256 lookup, quota, access-control helper
- `MarkTogether.Server/Services/ImageStorageService.cs`
  - Thêm `SaveWithDedup()`
  - Thêm secure filename handling
  - Dùng MIME normalized từ `ImageValidator`
- `MarkTogether.Server/Server.csproj`
  - Thêm `Services\ImageValidator.cs` vào compile list
- `MarkTogether.Server/Database/Repositories/DocumentRepository.cs`
  - Thêm soft delete có `deleted_by`
  - Thêm search accessible documents theo ID/title/all
  - Filter `deleted_at IS NULL` cho query quan trọng
- `MarkTogether.Server/Network/ClientHandler.cs`
  - Thêm route `DOC_SEARCH`, `DOC_DELETE`
  - Nâng cấp `IMAGE_UPLOAD` dùng validation/dedup/quota/permission
  - Nâng cấp `IMAGE_GET` có access control
- `MarkTogether.Client/TypeRenderForm.cs`
  - Thêm autosave timer 30 giây
  - Track unsaved changes
- `MarkTogether.Client/Client.csproj`
  - Gỡ reference `Form1.cs`, `Form1.Designer.cs` không tồn tại để solution build được

**Security checks đã áp dụng:**
- MIME whitelist
- Magic bytes validation
- Extension/MIME consistency
- Size limit 5MB
- Secure filename handling
- Upload document permission check
- Image retrieval access control
- Owner-only document delete
- Search không trả private/restricted docs nếu user không có quyền

**Backward compatibility:**
- Không đổi payload cũ của `IMAGE_UPLOAD`, `IMAGE_GET`, `DOC_SAVE`
- API upload cũ vẫn hoạt động, chỉ được harden thêm
- Autosave dùng `DOC_SAVE` hiện có
- DB changes dựa trên `migration_v2.sql` đã có sẵn (`sha256_hash`, `doc_id`, `deleted_at`, `deleted_by`)

**Verification:**
- `dotnet build MarkTogether.sln --no-restore -v:minimal`
  - Build succeeded
  - Còn 1 warning cũ: `TypeRenderForm.Designer.cs` biến `rightStart` unused
- `dotnet build MarkTogether.Server/Server.csproj --no-restore -v:minimal`
  - Build succeeded

### Phase 6: Production Deployment ✅ HOÀN THÀNH PHẦN KIẾN TRÚC/TOOLING 2026-05-21

- [x] **6.1** Tách production Docker Compose
  - Đã tạo `docker-compose.prod.yml`.
  - PostgreSQL bind `127.0.0.1`.
  - Password lấy từ env, không hardcode.

- [x] **6.2** Runtime config qua environment variables
  - `Program.cs` đọc `MARKTOGETHER_PORT`.
  - `DbConnectionFactory.cs` đọc `MARKTOGETHER_DB_CONNECTION`.
  - `ImageStorageService.cs` đọc `MARKTOGETHER_STORAGE_PATH`.

- [x] **6.3** Systemd service production
  - Đã tạo `deploy/systemd/marktogether.service`.
  - Chạy Mono với `--service`.
  - Có auto restart và hardening cơ bản.

- [x] **6.4** Environment template
  - Đã tạo `deploy/env/marktogether.env.example`.
  - Không chứa secret thật.

- [x] **6.5** Backup strategy
  - Đã tạo `deploy/scripts/backup.sh`.
  - Backup DB bằng `pg_dump`.
  - Backup storage bằng `rsync`.
  - Có retention cleanup.

- [x] **6.6** Migration runner cho DB đã tồn tại
  - Đã tạo `deploy/scripts/run-migrations.sh`.
  - Apply `migration_v2.sql` bằng `psql -v ON_ERROR_STOP=1`.

- [x] **6.7** Release deploy script
  - Đã tạo `deploy/scripts/deploy-release.sh`.
  - Backup release cũ trước khi sync release mới.
  - Restart service sau deploy.

- [x] **6.8** Tài liệu vận hành VPS
  - Đã tạo `Plan/PHASE6_PRODUCTION_DEPLOYMENT.md`.
  - Bao gồm setup VPS, build Windows, upload, migration, backup, rollback, smoke test.

#### Files đã sửa/tạo

- `MarkTogether.Server/Program.cs`
- `MarkTogether.Server/Services/ImageStorageService.cs`
- `docker-compose.prod.yml`
- `deploy/systemd/marktogether.service`
- `deploy/env/marktogether.env.example`
- `deploy/scripts/backup.sh`
- `deploy/scripts/run-migrations.sh`
- `deploy/scripts/deploy-release.sh`
- `Plan/PHASE6_PRODUCTION_DEPLOYMENT.md`
- `Plan/ARCHITECTURE_EXPANSION_PLAN.md`

#### Verification

- Release build:
  - `MSBuild.exe MarkTogether.Server\Server.csproj /p:Configuration=Release /t:Rebuild`
  - Build succeeded.
  - Còn 1 warning cũ `SocketServer.cs CS4014`, không phát sinh từ Phase 6.

- Security check:
  - Đã kiểm tra không ghi mật khẩu VPS được cung cấp vào các file deployment.
  - Secret chỉ được hướng dẫn nhập trực tiếp trên VPS.

#### Backward compatibility

- Không đổi protocol TCP.
- Không đổi MessageType/payload cũ.
- Không sửa `docker-compose.yml` dev hiện tại.
- Không yêu cầu env mới khi chạy local; tất cả đều có fallback.
- Storage DB URL vẫn là relative path `images/...`.

#### Còn lại khi deploy thật trên VPS

- [ ] Chạy lệnh setup trực tiếp trên VPS.
- [ ] Điền secret thật vào `/opt/marktogether/app/.env` và `/etc/marktogether/env` trên VPS.
- [ ] Upload release folder.
- [ ] Chạy backup/migration/deploy scripts.
- [ ] Smoke test từ WinForms client đến `159.203.184.87:5000`.
- [ ] Cấu hình cron backup daily.
- [ ] Thiết lập disk usage alert nếu cần.

---

## 13. LỖI PHỔ BIẾN CẦN TRÁNH

### 13.1 Lỗi kiến trúc

| # | Lỗi | Hậu quả | Cách tránh |
|---|------|---------|-----------|
| 1 | **Thay đổi schema breaking** | Client cũ crash | Chỉ ADD column, không DROP/RENAME |
| 2 | **Quên check permission** | Data leak | Mọi handler PHẢI gọi ResolvePermission |
| 3 | **Race condition trong OT** | Document corruption | Giữ lock trong DocumentState |
| 4 | **N+1 query** | DB overload | Dùng JOIN hoặc batch query |
| 5 | **Không handle disconnect** | Memory leak, ghost sessions | LeaveAllRooms + cleanup |
| 6 | **Blocking I/O trên main thread** | Server freeze | Async cho AI, heavy DB ops |
| 7 | **Hardcode config** | Khó deploy | Environment variables |

### 13.2 Lỗi implementation

| # | Lỗi | Cách tránh |
|---|------|-----------|
| 1 | Thêm MessageType nhưng quên thêm case trong switch | Luôn thêm handler + test |
| 2 | Client gửi permission "commenter" nhưng server chưa accept | Cập nhật CHECK constraint trước |
| 3 | Quên cập nhật SocketClient khi thêm API mới | Thêm method wrapper cho mỗi MessageType mới |
| 4 | Migration chạy trên DB production nhưng quên test | Luôn test migration trên DB copy trước |
| 5 | Soft delete nhưng quên filter `WHERE deleted_at IS NULL` | Thêm vào MỌI query SELECT documents |
| 6 | Token refresh race condition (2 request cùng refresh) | Dùng DB transaction + unique constraint |
| 7 | Image hash collision (cực hiếm nhưng có thể) | SHA256 đủ an toàn, không cần lo |
| 8 | Rate limiter memory leak | Cleanup expired entries định kỳ (timer 5 phút) |

### 13.3 Lỗi deployment

| # | Lỗi | Cách tránh |
|---|------|-----------|
| 1 | Expose PostgreSQL port ra internet | Bind 127.0.0.1 only |
| 2 | Chạy server bằng root | Tạo user riêng `marktogether` |
| 3 | Không backup trước khi migration | Script backup tự động trước mỗi deploy |
| 4 | Quên mở port 5000 trên firewall | Checklist deploy |
| 5 | Connection string sai sau deploy | Test connection trước khi start service |
| 6 | Disk full vì images | Monitor disk usage, alert ở 80% |
| 7 | Server crash loop không có log | journalctl -u marktogether -f |

### 13.4 Thứ tự ưu tiên tuyệt đối

```
1. 🔴 Security (Phase 1) — Không deploy production khi chưa có
2. 🟡 Visibility + Permission (Phase 1-2) — Core feature mới
3. 🟡 Search + Profile (Phase 2-3) — UX quan trọng
4. 🟢 Session persistence (Phase 4) — Cần cho production
5. 🟢 Image improvements (Phase 5) — Nice-to-have
6. 🔵 Deployment (Phase 6) — Khi code đã stable
```

---

## TÓM TẮT QUYẾT ĐỊNH KIẾN TRÚC

| Quyết định | Lý do |
|-----------|-------|
| Giữ TCP protocol | Không phá client hiện tại, WinForms không cần HTTP |
| Custom token thay JWT | TCP stateful, dễ revoke, không cần self-contained token |
| PostgreSQL giữ nguyên | Đã hoạt động tốt, pg_trgm đủ cho search |
| OT giữ nguyên (không CRDT) | Đã implement, server always-online, migration cost quá lớn |
| Disk storage (không MinIO) | Đơn giản, đủ cho giai đoạn hiện tại, migrate sau |
| Additive migration | Không break client cũ, rollback dễ |
| Mono trên Linux VPS | .NET 4.7.2 chạy được trên Mono, không cần rewrite |
| Single server (không microservice) | Scale nhỏ, complexity thấp, đủ cho 100-500 users |

---

*Document này là living document. Cập nhật khi có thay đổi kiến trúc.*
