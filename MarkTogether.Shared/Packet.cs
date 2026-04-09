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
        DOC_OPEN,
        DOC_JOIN,
        DOC_LEAVE,
        DOC_SHARE,

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

    // Payload models — B và C sẽ dùng để build UI
    public class AuthPayload
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
    }

    public class ErrorPayload
    {
        public string Message { get; set; }
    }
}