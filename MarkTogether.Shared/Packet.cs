using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarkTogether.Shared
{
    public enum MessageType
    {
        AUTH_REGISTER = 0,
        AUTH_LOGIN = 1,
        AUTH_RESPONSE = 2,
        DOC_CREATE = 3,
        DOC_LIST = 4,
        DOC_OPEN = 5,
        DOC_SAVE = 6,
        DOC_JOIN_CODE = 7,
        DOC_LEAVE = 8,
        DOC_SHARE = 9,
        OP_INSERT = 10,
        OP_DELETE = 11,
        OP_BROADCAST = 12,
        ERROR = 13,
        OK = 14
    }

    public class Packet
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MessageType Type { get; set; }

        public string Token { get; set; }
        public string Payload { get; set; }

        public T GetPayload<T>() =>
            JsonConvert.DeserializeObject<T>(Payload);

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
        public string docID { get; set; }
    }

    public class Payload_DOC_OPEN_Response
    {
        public string docID { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public int revision { get; set; }
        public string permission { get; set; }
    }

    // DOC_SAVE
    public class Payload_DOC_SAVE_Request
    {
        public string docID { get; set; }
        public string content { get; set; }
    }

    // DOC_JOIN_CODE
    public class Payload_DOC_JOIN_CODE_Request
    {
        public string shareCode { get; set; }
    }

    public class Payload_DOC_JOIN_CODE_Response
    {
        public string docID { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public int revision { get; set; }
        public string permission { get; set; }
        ErrorPayload error { get; set; } // Nếu có lỗi (code sai, code hết hạn), trả về lỗi thay vì doc info
    }

    // DOC_LEAVE
    public class Payload_DOC_LEAVE_Request
    {
        public string docID { get; set; }
    }

    public class Payload_DOC_LEAVE_Response
    {
        public bool success { get; set; }
        public string message { get; set; }
    }

    // DOC_SHARE
    public class Payload_DOC_SHARE_Request
    {
        public string docID { get; set; }
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
        public string docID { get; set; }
        public int clientResivion { get; set; }
        public List<EditOpItem> ops { get; set; } = new List<EditOpItem>();
    }

    // OP_DELETE (batch cùng loại, tối đa 5 ops)
    public class Payload_OP_DELETE
    {
        public string docID { get; set; }
        public int clientResivion { get; set; }
        public List<EditOpItem> ops { get; set; } = new List<EditOpItem>();
    }

    // OP_BROADCAST
    public class Payload_OP_BROADCAST
    {
        public string docID { get; set; }
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
        public string docID { get; set; }

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