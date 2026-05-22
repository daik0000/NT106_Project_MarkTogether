using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MarkTogether.Shared
{
    // Enum tất cả loại message trong hệ thống
    public enum MessageType
    {
        // ─── Auth ─────────────────────────────
        AUTH_REGISTER,
        AUTH_LOGIN,
        AUTH_RESPONSE,
        AUTH_LOGOUT,

        // ─── Document ─────────────────────────
        DOC_CREATE,
        DOC_LIST,
        DOC_OPEN,
        DOC_SAVE,
        DOC_JOIN_CODE,
        DOC_LEAVE,
        DOC_SHARE,
        DOC_SHARE_LIST,
        DOC_SHARE_UPDATE,
        DOC_SHARE_REVOKE,
        DOC_SHARE_REGEN_CODE,
        DOC_CREATE_LINK,
        DOC_REVOKE_LINK,
        DOC_JOIN_LINK,
        DOC_SET_VISIBILITY,
        DOC_GET_PUBLIC_LIST,
        DOC_RELOAD_BROADCAST,
        DOC_SEARCH,
        DOC_DELETE,

        // ─── Real-time edit ──────────────────
        OP_INSERT,
        OP_DELETE,
        OP_BROADCAST,

        // ─── Chat ────────────────────────────
        CHAT_SEND,
        CHAT_BROADCAST,
        CHAT_HISTORY,

        // ─── Comment ─────────────────────────
        COMMENT_CREATE,
        COMMENT_LIST,
        COMMENT_RESOLVE,
        COMMENT_DELETE,
        COMMENT_BROADCAST,

        // ─── Image ───────────────────────────
        IMAGE_UPLOAD,
        IMAGE_GET,

        // ─── Version ─────────────────────────
        DOC_VERSION_LIST,
        DOC_VERSION_DETAIL,
        DOC_VERSION_RESTORE,
        DOC_VERSION_DELETE,

        // ─── AI ──────────────────────────────
        AI_REQUEST,
        AI_RESPONSE,

        // ─── System ──────────────────────────
        ERROR,
        OK
    }

    /// <summary>
    /// Packet chính — mọi message đều dùng class này.
    /// RequestId dùng để correlation: client gắn Guid khi gửi, server echo lại
    /// trong response để client match kết quả với request đang đợi.
    /// Khi RequestId rỗng → packet là server-push (broadcast), không phải reply.
    /// </summary>
    public class Packet
    {
        public MessageType Type { get; set; }
        public string Token { get; set; }
        public string RequestId { get; set; }
        public string Payload { get; set; }

        public T GetPayload<T>() =>
            string.IsNullOrEmpty(Payload)
                ? default(T)
                : JsonConvert.DeserializeObject<T>(Payload);

        public static Packet Create(MessageType type, object payload = null)
            => new Packet
            {
                Type = type,
                Payload = payload != null
                    ? JsonConvert.SerializeObject(payload)
                    : null
            };
    }

    // ═══════════════════════════════════════════════════════════
    //  AUTH
    // ═══════════════════════════════════════════════════════════
    public class Payload_AUTH_REGISTER
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class Payload_AUTH_LOGIN
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class Payload_AUTH_RESPONSE
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
    }

    public class Payload_AUTH_LOGOUT_Request { }
    public class Payload_AUTH_LOGOUT_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    //  DOCUMENT
    // ═══════════════════════════════════════════════════════════
    public class Payload_DOC_CREATE_Request
    {
        public string title { get; set; }
        public string content { get; set; }
    }

    public class Payload_DOC_CREATE_Response
    {
        public string docID { get; set; }
        public string shareCode { get; set; }
        public string title { get; set; }
        public string content { get; set; } = "";
        public int revision { get; set; } = 0;
    }

    public class Payload_DOC_LIST_Request { }

    public class Payload_DOC_LIST_Response
    {
        public List<DocInfo> documents { get; set; }
    }

    public class Payload_DOC_OPEN_Request
    {
        public string docID { get; set; }
    }

    public class Payload_DOC_OPEN_Response
    {
        public string docID { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public int revision { get; set; }
        public string permission { get; set; }
        public string shareCode { get; set; }
        public string visibility { get; set; }
        public string publicPermission { get; set; }
    }

    public class Payload_DOC_SAVE_Request
    {
        public string docID { get; set; }
        public string content { get; set; }
    }

    public class Payload_DOC_JOIN_CODE_Request
    {
        public string shareCode { get; set; }
    }

    public class Payload_DOC_JOIN_CODE_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string docID { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public int revision { get; set; }
        public string permission { get; set; }
    }

    public class Payload_DOC_LEAVE_Request
    {
        public string docID { get; set; }
    }

    public class Payload_DOC_LEAVE_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
    }

    // ─── Sharing ───
    public class Payload_DOC_SHARE_Request
    {
        public string docID { get; set; }
        public string targetUsername { get; set; }
        public string permission { get; set; } = "viewer";
    }

    public class Payload_DOC_SHARE_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
    }

    public class Payload_DOC_SHARE_LIST_Request
    {
        public string docID { get; set; }
    }

    public class Payload_DOC_SHARE_LIST_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string docID { get; set; }
        public string shareCode { get; set; }
        public List<CollaboratorDto> collaborators { get; set; } = new List<CollaboratorDto>();
    }

    public class CollaboratorDto
    {
        public int userId { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string avatarUrl { get; set; }
        public string permission { get; set; }
        public DateTime invitedAt { get; set; }
    }

    public class Payload_DOC_SHARE_UPDATE_Request
    {
        public string docID { get; set; }
        public int targetUserId { get; set; }
        public string newPermission { get; set; }
    }

    public class Payload_DOC_SHARE_UPDATE_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
    }

    public class Payload_DOC_SHARE_REVOKE_Request
    {
        public string docID { get; set; }
        public int targetUserId { get; set; }
    }

    public class Payload_DOC_SHARE_REVOKE_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
    }

    public class Payload_DOC_SHARE_REGEN_CODE_Request
    {
        public string docID { get; set; }
    }

    public class Payload_DOC_SHARE_REGEN_CODE_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string shareCode { get; set; }
    }

    public class Payload_DOC_CREATE_LINK_Request
    {
        public string docID { get; set; }
        public string permission { get; set; } = "viewer";
        public int expiresInHours { get; set; }
        public int maxUses { get; set; }
    }

    public class Payload_DOC_CREATE_LINK_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string linkId { get; set; }
        public string linkToken { get; set; }
        public string permission { get; set; }
        public DateTime? expiresAt { get; set; }
        public int? maxUses { get; set; }
    }

    public class Payload_DOC_REVOKE_LINK_Request
    {
        public string linkToken { get; set; }
    }

    public class Payload_DOC_REVOKE_LINK_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
    }

    public class Payload_DOC_JOIN_LINK_Request
    {
        public string linkToken { get; set; }
    }

    public class Payload_DOC_JOIN_LINK_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string docID { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public int revision { get; set; }
        public string permission { get; set; }
    }

    public class Payload_DOC_SET_VISIBILITY_Request
    {
        public string docID { get; set; }
        public string visibility { get; set; }
        public string publicPermission { get; set; }
    }

    public class Payload_DOC_SET_VISIBILITY_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string docID { get; set; }
        public string visibility { get; set; }
        public string publicPermission { get; set; }
    }

    public class Payload_DOC_GET_PUBLIC_LIST_Request
    {
        public int page { get; set; } = 1;
        public int limit { get; set; } = 20;
    }

    public class PublicDocumentDto
    {
        public string docID { get; set; }
        public string title { get; set; }
        public int ownerId { get; set; }
        public string ownerUsername { get; set; }
        public DateTime updatedAt { get; set; }
        public string publicPermission { get; set; }
    }

    public class Payload_DOC_GET_PUBLIC_LIST_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public int page { get; set; }
        public int limit { get; set; }
        public int total { get; set; }
        public List<PublicDocumentDto> documents { get; set; } = new List<PublicDocumentDto>();
    }

    public class Payload_DOC_SEARCH_Request
    {
        public string query { get; set; }
        public string searchBy { get; set; } = "all";
    }

    public class DocumentSearchResultDto
    {
        public string docID { get; set; }
        public string title { get; set; }
        public string ownerUsername { get; set; }
        public string visibility { get; set; }
        public string permission { get; set; }
        public DateTime updatedAt { get; set; }
    }

    public class Payload_DOC_SEARCH_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public List<DocumentSearchResultDto> results { get; set; } = new List<DocumentSearchResultDto>();
    }

    public class Payload_DOC_DELETE_Request
    {
        public string docID { get; set; }
    }

    public class Payload_DOC_DELETE_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string docID { get; set; }
    }

    public class Payload_DOC_RELOAD_BROADCAST
    {
        public string docID { get; set; }
        public string reason { get; set; }
        public int revision { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    //  REAL-TIME OPS
    // ═══════════════════════════════════════════════════════════
    public class Payload_OP_INSERT
    {
        public string docID { get; set; }
        public int clientResivion { get; set; }
        public List<EditOpItem> ops { get; set; } = new List<EditOpItem>();
    }

    public class Payload_OP_DELETE
    {
        public string docID { get; set; }
        public int clientResivion { get; set; }
        public List<EditOpItem> ops { get; set; } = new List<EditOpItem>();
    }

    public class Payload_OP_BROADCAST
    {
        public string docID { get; set; }
        public int clientResivion { get; set; }
        public int userID { get; set; }
        public string username { get; set; }
        public string opType { get; set; }
        public List<EditOpItem> ops { get; set; } = new List<EditOpItem>();
    }

    // ═══════════════════════════════════════════════════════════
    //  CHAT
    // ═══════════════════════════════════════════════════════════
    public class Payload_CHAT_SEND_Request
    {
        public string docID { get; set; }
        public string content { get; set; }
    }

    public class Payload_CHAT_SEND_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
    }

    public class Payload_CHAT_BROADCAST
    {
        public string docID { get; set; }
        public long messageId { get; set; }
        public int userId { get; set; }
        public string username { get; set; }
        public string content { get; set; }
        public DateTime sentAt { get; set; }
    }

    public class Payload_CHAT_HISTORY_Request
    {
        public string docID { get; set; }
        public int limit { get; set; } = 50;
    }

    public class Payload_CHAT_HISTORY_Response
    {
        public string docID { get; set; }
        public List<Payload_CHAT_BROADCAST> messages { get; set; } = new List<Payload_CHAT_BROADCAST>();
    }

    // ═══════════════════════════════════════════════════════════
    //  COMMENT
    // ═══════════════════════════════════════════════════════════
    public class Payload_COMMENT_CREATE_Request
    {
        public string docID { get; set; }
        public string parentId { get; set; }
        public int anchorStart { get; set; }
        public int anchorEnd { get; set; }
        public string anchorText { get; set; }
        public string content { get; set; }
    }

    public class Payload_COMMENT_CREATE_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public CommentDto comment { get; set; }
    }

    public class CommentDto
    {
        public string id { get; set; }
        public string docID { get; set; }
        public string parentId { get; set; }
        public int userId { get; set; }
        public string username { get; set; }
        public int anchorStart { get; set; }
        public int anchorEnd { get; set; }
        public string anchorText { get; set; }
        public string content { get; set; }
        public bool resolved { get; set; }
        public DateTime createdAt { get; set; }
    }

    public class Payload_COMMENT_LIST_Request
    {
        public string docID { get; set; }
    }

    public class Payload_COMMENT_LIST_Response
    {
        public string docID { get; set; }
        public List<CommentDto> comments { get; set; } = new List<CommentDto>();
    }

    public class Payload_COMMENT_RESOLVE_Request
    {
        public string commentId { get; set; }
        public bool resolved { get; set; } = true;
    }

    public class Payload_COMMENT_DELETE_Request
    {
        public string commentId { get; set; }
    }

    public class Payload_COMMENT_BROADCAST
    {
        public string action { get; set; }   // "created" | "resolved" | "deleted"
        public CommentDto comment { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    //  IMAGE
    // ═══════════════════════════════════════════════════════════
    public class Payload_IMAGE_UPLOAD_Request
    {
        public string fileName { get; set; }
        public string mimeType { get; set; }
        public byte[] data { get; set; }       // Newtonsoft tự encode base64
        public bool isAvatar { get; set; }
        public string docID { get; set; }
    }

    public class Payload_IMAGE_UPLOAD_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string imageId { get; set; }
        public string url { get; set; }
        public string avatarUrl { get; set; }
    }

    public class Payload_IMAGE_GET_Request
    {
        public string imageId { get; set; }
    }

    public class Payload_IMAGE_GET_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string imageId { get; set; }
        public string mimeType { get; set; }
        public byte[] data { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    //  VERSION
    // ═══════════════════════════════════════════════════════════
    public class Payload_DOC_VERSION_LIST_Request
    {
        public string docID { get; set; }
        public int page { get; set; } = 1;
        public int limit { get; set; } = 20;
    }

    public class VersionInfoDto
    {
        public string id { get; set; }
        public DateTime savedAt { get; set; }
        public int savedByUserId { get; set; }
        public string savedByUsername { get; set; }
        public string label { get; set; }
    }

    public class Payload_DOC_VERSION_LIST_Response
    {
        public string docID { get; set; }
        public int total { get; set; }
        public List<VersionInfoDto> versions { get; set; } = new List<VersionInfoDto>();
    }

    public class Payload_DOC_VERSION_DETAIL_Request
    {
        public string versionId { get; set; }
    }

    public class Payload_DOC_VERSION_DETAIL_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string id { get; set; }
        public string docID { get; set; }
        public string contentSnapshot { get; set; }
        public DateTime savedAt { get; set; }
        public string label { get; set; }
    }

    public class Payload_DOC_VERSION_RESTORE_Request
    {
        public string docID { get; set; }
        public string versionId { get; set; }
    }

    public class Payload_DOC_VERSION_RESTORE_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string newContent { get; set; }
        public int revision { get; set; }
    }

    public class Payload_DOC_VERSION_DELETE_Request
    {
        public string versionId { get; set; }
    }

    public class Payload_DOC_VERSION_DELETE_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    //  AI
    // ═══════════════════════════════════════════════════════════
    public class Payload_AI_Request
    {
        public string docID { get; set; }
        public string mode { get; set; }        // "chat" | "summarize" | "continue" | "translate"
        public string userPrompt { get; set; }
        public string contextText { get; set; }

        // Cấu hình AI động phía client
        public string provider { get; set; }    // "gemini" mặc định, reserved cho future
        public string model { get; set; }       // ví dụ "gemini-2.5-flash"
        public string apiKey { get; set; }      // null/rỗng → server fallback App.config

        // Editor context cho Action Mode
        public string actionMode { get; set; }  // "text" (mặc định) | "edit"
        public string documentText { get; set; }
        public int selectionStart { get; set; }
        public int selectionEnd { get; set; }
        public int cursorPosition { get; set; }
    }

    public class Payload_AI_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string text { get; set; }

        // "text" | "edit_plan"
        public string kind { get; set; }
        public string editPlanJson { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    //  SYSTEM
    // ═══════════════════════════════════════════════════════════
    public class Payload_ERROR
    {
        public string Message { get; set; }
    }

    public class Payload_OK
    {
        public string Message { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    //  Backward-compatible aliases
    // ═══════════════════════════════════════════════════════════
    public class Payload_AuthLogin_Request : Payload_AUTH_LOGIN { }
    public class AuthResponse : Payload_AUTH_RESPONSE { }
    public class ErrorPayload : Payload_ERROR { }
    public class DocCreateRequestPayload : Payload_DOC_CREATE_Request { }
    public class DocCreateResponse : Payload_DOC_CREATE_Response { }
    public class DocListResponse : Payload_DOC_LIST_Response { }

    public class DocInfo
    {
        public string docID { get; set; }
        public string title { get; set; }
        public string permission { get; set; }
        public string visibility { get; set; }
        public string publicPermission { get; set; }
        public DateTime updateAt { get; set; }
    }

    public class EditOpItem
    {
        public int pos { get; set; }
        public string text { get; set; }
        public DateTime timestamp { get; set; }
    }
}