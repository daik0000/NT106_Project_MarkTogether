using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MarkTogether.Shared
{
    // Enum tất cả loại message trong hệ thống
    public enum MessageType
    {
        // Auth
        AUTH_REGISTER,
        AUTH_LOGIN,
        AUTH_RESPONSE,

        // Document
        DOC_CREATE,
        DOC_LIST,
        DOC_OPEN,
        DOC_JOIN_CODE,
        DOC_LEAVE,
        DOC_SHARE,
        // DOC_UPLOAD_PIC,

        // Real-time edit 
        OP_INSERT,
        OP_DELETE,
        OP_BROADCAST,

        // System
        ERROR,
        OK
    }

    // Packet chính — mọi message đều dùng class này
    public class Packet
    {
        public MessageType Type { get; set; }
        public string Token { get; set; }     // session token sau khi login
        public string Payload { get; set; }   // JSON string của data cụ thể

        // Helper: deserialize Payload thành object cụ thể
        public T GetPayload<T>() =>
            JsonConvert.DeserializeObject<T>(Payload);

        // Helper: tạo packet nhanh
        public static Packet Create(MessageType type, object payload = null)
            => new Packet
            {
                Type = type,
                Payload = payload != null
                    ? JsonConvert.SerializeObject(payload)
                    : null
            };
    }

    // AUTH_REGISTER
    public class Payload_AUTH_REGISTER
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    // AUTH_LOGIN
    public class Payload_AUTH_LOGIN
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    // AUTH_RESPONSE
    public class Payload_AUTH_RESPONSE
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
    }

    // DOC_CREATE
    public class Payload_DOC_CREATE_Request
    {
        public string title { get; set; }
    }

    public class Payload_DOC_CREATE_Response
    {
        public int docID { get; set; }
        public string shareCode { get; set; }
        public string title { get; set; }
        public string content { get; set; } = "";
        public int revision { get; set; } = 0;
    }

    // DOC_LIST
    public class Payload_DOC_LIST_Request
    {
    }

    public class Payload_DOC_LIST_Response
    {
        public List<DocInfo> documents { get; set; }
    }

    // DOC_OPEN
    public class Payload_DOC_OPEN_Request
    {
        public int docID { get; set; }
    }

    public class Payload_DOC_OPEN_Response
    {
        public int docID { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public int revision { get; set; }
        public string permission { get; set; }
    }

    // DOC_JOIN_CODE
    public class Payload_DOC_JOIN_CODE_Request
    {
        public string shareCode { get; set; }
    }

    public class Payload_DOC_JOIN_CODE_Response
    {
        public int docID { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public int revision { get; set; }
        public string permission { get; set; }
        ErrorPayload error { get; set; } // Nếu có lỗi (code sai, code hết hạn), trả về lỗi thay vì doc info
    }

    // DOC_LEAVE
    public class Payload_DOC_LEAVE_Request
    {
        public int docID { get; set; }
    }

    public class Payload_DOC_LEAVE_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
    }

    // DOC_SHARE
    public class Payload_DOC_SHARE_Request
    {
        public int docID { get; set; }
        public string targetUsername { get; set; }
    }

    public class Payload_DOC_SHARE_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
    }


    // 
    // OP_INSERT (batch cùng loại, tối đa 5 ops)
    public class Payload_OP_INSERT
    {
        public int docID { get; set; }
        public int clientResivion { get; set; }
        public List<EditOpItem> ops { get; set; } = new List<EditOpItem>();
    }

    // OP_DELETE (batch cùng loại, tối đa 5 ops)
    public class Payload_OP_DELETE
    {
        public int docID { get; set; }
        public int clientResivion { get; set; }
        public List<EditOpItem> ops { get; set; } = new List<EditOpItem>();
    }

    // OP_BROADCAST
    public class Payload_OP_BROADCAST
    {
        public int docID { get; set; }
        public int clientResivion { get; set; }
        public int userID { get; set; }
        public string username { get; set; }
        public string opType { get; set; }
        public List<EditOpItem> ops { get; set; } = new List<EditOpItem>();
    }

    // ERROR
    public class Payload_ERROR
    {
        public string Message { get; set; }
    }
    // OK
    public class Payload_OK
    {
        public string Message { get; set; }
    }

    // Backward-compatible aliases
    public class Payload_AuthLogin_Request : Payload_AUTH_LOGIN{ }
    public class AuthResponse : Payload_AUTH_RESPONSE { }
    public class ErrorPayload : Payload_ERROR { }
    public class DocCreateRequestPayload : Payload_DOC_CREATE_Request { }
    public class DocCreateResponse : Payload_DOC_CREATE_Response { }
    public class DocListResponse : Payload_DOC_LIST_Response { }

    public class DocInfo
    {
        public int docID { get; set; }

        public string title { get; set; }

        public string permission { get; set; }

        public DateTime updateAt { get; set; }
    }

    public class EditOpItem
    {
        public int pos { get; set; }
        public string text { get; set; }
        public DateTime timestamp { get; set; }
    }
}