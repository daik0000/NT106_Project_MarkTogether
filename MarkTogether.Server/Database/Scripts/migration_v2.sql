-- =============================================
-- MarkTogether Migration V2: Document Management Expansion
-- Chạy SAU schema_init.sql
-- File idempotent: an toàn chạy lại nhiều lần.
-- =============================================
-- HƯỚNG DẪN:
-- 1. Đảm bảo schema_init.sql đã chạy thành công.
-- 2. Chạy file này trên database MarkTogether.
-- 3. Script dùng advisory lock + schema_migrations để an toàn khi deploy production.
-- =============================================

BEGIN;

-- Tránh 2 process/deploy chạy migration cùng lúc trên cùng database.
SELECT pg_advisory_xact_lock(hashtext('marktogether:migration:v2'));

-- Bảng tracking migration tối thiểu, không phụ thuộc tooling ngoài.
CREATE TABLE IF NOT EXISTS schema_migrations (
    version         VARCHAR(50) PRIMARY KEY,
    description     TEXT NOT NULL,
    applied_at      TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- 0. Extension cần thiết
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- ═══════════════════════════════════════════════════════════
-- 1. DOCUMENTS: Visibility + soft delete
-- ═══════════════════════════════════════════════════════════

ALTER TABLE documents ADD COLUMN IF NOT EXISTS visibility VARCHAR(20) DEFAULT 'restricted';
ALTER TABLE documents ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE DEFAULT NULL;
ALTER TABLE documents ADD COLUMN IF NOT EXISTS deleted_by INT DEFAULT NULL;

-- Normalize dữ liệu cũ: hệ thống hiện tại chỉ owner/share được truy cập,
-- nên default restricted giữ nguyên behavior trước migration.
UPDATE documents
SET visibility = 'restricted'
WHERE visibility IS NULL OR visibility NOT IN ('public', 'private', 'restricted');

UPDATE documents
SET public_permission = 'viewer'
WHERE public_permission IS NULL OR public_permission NOT IN ('editor', 'commenter', 'viewer');

-- CHECK constraint cho visibility (idempotent)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_documents_visibility') THEN
        ALTER TABLE documents ADD CONSTRAINT chk_documents_visibility
            CHECK (visibility IN ('public', 'private', 'restricted'));
    END IF;
END $$;

-- CHECK constraint cho public_permission, phục vụ visibility public ở phase sau.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_documents_public_permission') THEN
        ALTER TABLE documents ADD CONSTRAINT chk_documents_public_permission
            CHECK (public_permission IN ('editor', 'commenter', 'viewer'));
    END IF;
END $$;

-- FK soft delete actor. SET NULL để audit/khôi phục doc không bị phá khi user bị xóa.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_documents_deleted_by'
          AND table_name = 'documents'
    ) THEN
        ALTER TABLE documents ADD CONSTRAINT fk_documents_deleted_by
            FOREIGN KEY (deleted_by) REFERENCES users(id) ON DELETE SET NULL;
    END IF;
END $$;

-- Index cho visibility/search queries
CREATE INDEX IF NOT EXISTS idx_documents_visibility ON documents(visibility) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_documents_public ON documents(visibility, updated_at DESC)
    WHERE visibility = 'public' AND deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_documents_title_trgm ON documents USING gin(title gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_documents_deleted_at ON documents(deleted_at) WHERE deleted_at IS NOT NULL;

-- ═══════════════════════════════════════════════════════════
-- 2. DOCUMENT_SHARES: Mở rộng permission (thêm 'commenter')
-- ═══════════════════════════════════════════════════════════

UPDATE document_shares
SET permission = 'viewer'
WHERE permission IS NULL OR permission NOT IN ('owner', 'editor', 'commenter', 'viewer');

ALTER TABLE document_shares DROP CONSTRAINT IF EXISTS document_shares_permission_check;
ALTER TABLE document_shares ADD CONSTRAINT document_shares_permission_check
    CHECK (permission IN ('owner', 'editor', 'commenter', 'viewer'));

-- Index lookup permission theo doc + user; unique constraint cũ vẫn giữ nguyên.
CREATE INDEX IF NOT EXISTS idx_doc_shares_doc_user ON document_shares(doc_id, user_id);

-- ═══════════════════════════════════════════════════════════
-- 3. USERS: Profile fields
-- ═══════════════════════════════════════════════════════════

ALTER TABLE users ADD COLUMN IF NOT EXISTS display_name VARCHAR(100);
ALTER TABLE users ADD COLUMN IF NOT EXISTS bio TEXT;
ALTER TABLE users ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT TRUE;
ALTER TABLE users ADD COLUMN IF NOT EXISTS last_login_at TIMESTAMP WITH TIME ZONE;

UPDATE users
SET is_active = TRUE
WHERE is_active IS NULL;

-- Search/profile lookup optimization.
CREATE INDEX IF NOT EXISTS idx_users_username_lower ON users(LOWER(username));
CREATE INDEX IF NOT EXISTS idx_users_active ON users(is_active) WHERE is_active = TRUE;

-- ═══════════════════════════════════════════════════════════
-- 4. IMAGES: Hash + document reference
-- ═══════════════════════════════════════════════════════════

ALTER TABLE images ADD COLUMN IF NOT EXISTS sha256_hash VARCHAR(64);
ALTER TABLE images ADD COLUMN IF NOT EXISTS doc_id VARCHAR(50);

-- FK cho doc_id (idempotent check)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_images_doc_id'
          AND table_name = 'images'
    ) THEN
        ALTER TABLE images ADD CONSTRAINT fk_images_doc_id
            FOREIGN KEY (doc_id) REFERENCES documents(id) ON DELETE SET NULL;
    END IF;
END $$;

-- sha256_hash chỉ nhận lowercase hex 64 ký tự khi có giá trị.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_images_sha256_hash') THEN
        ALTER TABLE images ADD CONSTRAINT chk_images_sha256_hash
            CHECK (sha256_hash IS NULL OR sha256_hash ~ '^[a-f0-9]{64}$');
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_images_hash ON images(sha256_hash) WHERE sha256_hash IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_images_doc ON images(doc_id) WHERE doc_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_images_user_uploaded ON images(user_id, uploaded_at DESC);

-- ═══════════════════════════════════════════════════════════
-- 5. USER_SESSIONS: Persistent sessions
-- ═══════════════════════════════════════════════════════════

CREATE TABLE IF NOT EXISTS user_sessions (
    id                  VARCHAR(50) PRIMARY KEY DEFAULT 'sess_' || gen_random_uuid()::text,
    user_id             INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token               VARCHAR(64) NOT NULL UNIQUE,
    refresh_token       VARCHAR(64) UNIQUE,
    expires_at          TIMESTAMP WITH TIME ZONE NOT NULL,
    refresh_expires_at  TIMESTAMP WITH TIME ZONE,
    ip_address          VARCHAR(45),
    user_agent          TEXT,
    created_at          TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_active_at      TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Constraints bổ sung idempotent cho DB đã từng tạo bảng user_sessions trước đó.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_user_sessions_token_not_blank') THEN
        ALTER TABLE user_sessions ADD CONSTRAINT chk_user_sessions_token_not_blank
            CHECK (length(trim(token)) > 0);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_user_sessions_refresh_not_blank') THEN
        ALTER TABLE user_sessions ADD CONSTRAINT chk_user_sessions_refresh_not_blank
            CHECK (refresh_token IS NULL OR length(trim(refresh_token)) > 0);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_user_sessions_expiry_order') THEN
        ALTER TABLE user_sessions ADD CONSTRAINT chk_user_sessions_expiry_order
            CHECK (refresh_expires_at IS NULL OR refresh_expires_at >= expires_at);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_sessions_token ON user_sessions(token);
CREATE INDEX IF NOT EXISTS idx_sessions_refresh ON user_sessions(refresh_token) WHERE refresh_token IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_sessions_user ON user_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_sessions_expiry ON user_sessions(expires_at);
CREATE INDEX IF NOT EXISTS idx_sessions_user_active ON user_sessions(user_id, last_active_at DESC);

-- ═══════════════════════════════════════════════════════════
-- 6. SHARING_LINKS: Public sharing links với expiry/usage limits
-- ═══════════════════════════════════════════════════════════

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

-- Constraints bổ sung idempotent cho DB đã từng tạo bảng sharing_links trước đó.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_sharing_links_token_not_blank') THEN
        ALTER TABLE sharing_links ADD CONSTRAINT chk_sharing_links_token_not_blank
            CHECK (length(trim(token)) > 0);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_sharing_links_max_uses_positive') THEN
        ALTER TABLE sharing_links ADD CONSTRAINT chk_sharing_links_max_uses_positive
            CHECK (max_uses IS NULL OR max_uses > 0);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_sharing_links_use_count_non_negative') THEN
        ALTER TABLE sharing_links ADD CONSTRAINT chk_sharing_links_use_count_non_negative
            CHECK (use_count >= 0);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_sharing_links_use_count_within_limit') THEN
        ALTER TABLE sharing_links ADD CONSTRAINT chk_sharing_links_use_count_within_limit
            CHECK (max_uses IS NULL OR use_count <= max_uses);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_sharing_links_doc ON sharing_links(doc_id);
CREATE INDEX IF NOT EXISTS idx_sharing_links_token ON sharing_links(token);
CREATE INDEX IF NOT EXISTS idx_sharing_links_active_token ON sharing_links(token)
    WHERE is_active = TRUE;
CREATE INDEX IF NOT EXISTS idx_sharing_links_expiry ON sharing_links(expires_at)
    WHERE expires_at IS NOT NULL;

-- ═══════════════════════════════════════════════════════════
-- 7. AUDIT_LOGS: Tracking các hành động quan trọng
-- ═══════════════════════════════════════════════════════════

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

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_audit_logs_action_not_blank') THEN
        ALTER TABLE audit_logs ADD CONSTRAINT chk_audit_logs_action_not_blank
            CHECK (length(trim(action)) > 0);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_audit_logs_user ON audit_logs(user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_logs_target ON audit_logs(target_type, target_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_action ON audit_logs(action, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at ON audit_logs(created_at DESC);

-- ═══════════════════════════════════════════════════════════
-- 8. Mark migration as applied
-- ═══════════════════════════════════════════════════════════

INSERT INTO schema_migrations(version, description)
VALUES ('v2', 'Document management expansion: visibility, sessions, links, audit, image metadata, profile fields')
ON CONFLICT (version) DO UPDATE
SET description = EXCLUDED.description,
    applied_at = NOW();

COMMIT;

-- ═══════════════════════════════════════════════════════════
-- DONE
-- ═══════════════════════════════════════════════════════════
-- Kiểm tra nhanh:
-- SELECT version, description, applied_at FROM schema_migrations ORDER BY applied_at;
-- SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';
-- Expected includes:
-- users, documents, document_shares, document_operations, document_versions,
-- images, chat_messages, document_comments, user_sessions, sharing_links,
-- audit_logs, schema_migrations