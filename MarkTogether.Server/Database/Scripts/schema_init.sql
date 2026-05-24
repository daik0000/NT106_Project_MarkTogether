-- =============================================
-- MarkTogether Database Schema — PostgreSQL
-- Chạy file này trên PostgreSQL (pgAdmin / psql / DBeaver)
-- để tạo toàn bộ bảng cho hệ thống.
-- File idempotent: an toàn chạy lại nhiều lần.
-- =============================================
-- HƯỚNG DẪN KHỞI TẠO:
-- 1. Mở pgAdmin hoặc công cụ quản lý PostgreSQL.
-- 2. Tạo database mới tên là 'myapp_db' (nếu chưa có).
-- 3. Chọn database 'myapp_db' -> Mở Query Tool.
-- 4. Chạy toàn bộ nội dung file này (F5).
-- =============================================

-- 0. Cài đặt Extension (Cần thiết cho gen_random_uuid())
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 0.1. Hàm tự động cập nhật updated_at
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 1. Bảng Users
CREATE TABLE IF NOT EXISTS users (
    id              SERIAL PRIMARY KEY,
    username        VARCHAR(50) NOT NULL UNIQUE,
    email           VARCHAR(100) UNIQUE,
    password_hash   VARCHAR(255) NOT NULL,
    avatar_url      TEXT,
    created_at      TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- 2. Bảng Documents
CREATE TABLE IF NOT EXISTS documents (
    id                VARCHAR(50) PRIMARY KEY DEFAULT 'doc_' || gen_random_uuid()::text,
    owner_id          INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    share_code        VARCHAR(20) UNIQUE,
    is_public         BOOLEAN DEFAULT FALSE,
    public_permission VARCHAR(10) DEFAULT 'viewer',
    title             VARCHAR(500) NOT NULL DEFAULT 'Tài liệu không tiêu đề',
    content           TEXT DEFAULT '',
    file_path_server  TEXT,
    created_at        TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at        TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Bổ sung cột cho DB cũ chưa có (idempotent)
ALTER TABLE documents ADD COLUMN IF NOT EXISTS share_code VARCHAR(20);
ALTER TABLE documents ADD COLUMN IF NOT EXISTS is_public BOOLEAN DEFAULT FALSE;
ALTER TABLE documents ADD COLUMN IF NOT EXISTS public_permission VARCHAR(10) DEFAULT 'viewer';

CREATE UNIQUE INDEX IF NOT EXISTS uq_documents_share_code
    ON documents(share_code) WHERE share_code IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_documents_owner_id ON documents(owner_id);

-- Trigger cập nhật updated_at cho documents
DROP TRIGGER IF EXISTS trg_documents_updated_at ON documents;
CREATE TRIGGER trg_documents_updated_at
BEFORE UPDATE ON documents
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

-- 3. Bảng Document Shares (quan hệ N-N giữa users và documents)
CREATE TABLE IF NOT EXISTS document_shares (
    id              SERIAL PRIMARY KEY,
    doc_id          VARCHAR(50) NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    user_id         INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    permission      VARCHAR(20) NOT NULL DEFAULT 'viewer'
                    CHECK (permission IN ('owner', 'editor', 'viewer')),
    invited_at      TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UNIQUE(doc_id, user_id)
);

CREATE INDEX IF NOT EXISTS idx_doc_shares_doc_id ON document_shares(doc_id);
CREATE INDEX IF NOT EXISTS idx_doc_shares_user_id ON document_shares(user_id);

-- 4. Bảng Document Operations (OT — Operational Transformation)
CREATE TABLE IF NOT EXISTS document_operations (
    id              VARCHAR(50) PRIMARY KEY DEFAULT 'op_' || gen_random_uuid()::text,
    doc_id          VARCHAR(50) NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    user_id         INT NOT NULL REFERENCES users(id),
    op_type         VARCHAR(20) NOT NULL CHECK (op_type IN ('insert', 'delete')),
    pos             INT NOT NULL,
    text            TEXT,
    length          INT,
    revision        INT NOT NULL,
    applied_at      TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Index cho việc query operations theo revision (quan trọng cho OT Engine)
CREATE INDEX IF NOT EXISTS idx_doc_ops_revision
    ON document_operations(doc_id, revision);
CREATE INDEX IF NOT EXISTS idx_doc_ops_doc_revision
    ON document_operations(doc_id, revision);

-- 5. Bảng Document Versions (snapshot lịch sử)
CREATE TABLE IF NOT EXISTS document_versions (
    id              VARCHAR(50) PRIMARY KEY DEFAULT 'ver_' || gen_random_uuid()::text,
    doc_id          VARCHAR(50) NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    content_snapshot TEXT NOT NULL,
    saved_by        INT NOT NULL REFERENCES users(id),
    saved_at        TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    label           VARCHAR(200)
);

CREATE INDEX IF NOT EXISTS idx_doc_versions_doc_time
    ON document_versions(doc_id, saved_at DESC);
CREATE INDEX IF NOT EXISTS idx_doc_versions_doc_id ON document_versions(doc_id);

-- 6. Bảng Images (ảnh user / inline)
CREATE TABLE IF NOT EXISTS images (
    id              VARCHAR(50) PRIMARY KEY DEFAULT 'img_' || gen_random_uuid()::text,
    user_id         INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    file_name       VARCHAR(255) NOT NULL,
    file_data       BYTEA,
    url             TEXT,
    mime_type       VARCHAR(50) DEFAULT 'image/png',
    size            INT,
    uploaded_at     TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_images_user_id ON images(user_id);

-- 7. Bảng Chat Messages (chat realtime trong room)
CREATE TABLE IF NOT EXISTS chat_messages (
    id          BIGSERIAL PRIMARY KEY,
    doc_id      VARCHAR(50) NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    user_id     INT NOT NULL REFERENCES users(id),
    content     TEXT NOT NULL,
    sent_at     TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_chat_doc_time
    ON chat_messages(doc_id, sent_at DESC);

-- 8. Bảng Comments (comment trên đoạn văn bản)
CREATE TABLE IF NOT EXISTS document_comments (
    id              VARCHAR(50) PRIMARY KEY DEFAULT 'cmt_' || gen_random_uuid()::text,
    doc_id          VARCHAR(50) NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    user_id         INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    parent_id       VARCHAR(50) REFERENCES document_comments(id) ON DELETE CASCADE,
    anchor_start    INT NOT NULL,
    anchor_end      INT NOT NULL,
    anchor_text     TEXT,
    content         TEXT NOT NULL,
    resolved        BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_comments_doc
    ON document_comments(doc_id, created_at DESC);
