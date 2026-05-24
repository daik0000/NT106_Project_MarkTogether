using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using MarkTogether.Server.Database.Models;

namespace MarkTogether.Server.Database.Repositories
{
    /// <summary>
    /// Repository quản lý bảng "chat_messages" — tin nhắn chat trong room tài liệu.
    /// </summary>
    public static class ChatRepository
    {
        public static long Create(ChatMessage msg)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<long>(
                    @"INSERT INTO chat_messages (doc_id, user_id, content)
                      VALUES (@DocId, @UserId, @Content)
                      RETURNING id",
                    new { msg.DocId, msg.UserId, msg.Content });
            }
        }

        /// <summary>
        /// Lấy n tin gần nhất, đã JOIN với users để lấy username.
        /// Trả về theo thứ tự cũ → mới.
        /// </summary>
        public static List<ChatMessageWithUser> GetRecent(string docId, int limit = 50)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                var rows = db.Query<ChatMessageWithUser>(
                    @"SELECT cm.id          AS Id,
                             cm.doc_id      AS DocId,
                             cm.user_id     AS UserId,
                             cm.content     AS Content,
                             cm.sent_at     AS SentAt,
                             u.username     AS Username
                      FROM chat_messages cm
                      INNER JOIN users u ON cm.user_id = u.id
                      WHERE cm.doc_id = @DocId
                      ORDER BY cm.sent_at DESC
                      LIMIT @Limit",
                    new { DocId = docId, Limit = limit }).ToList();

                rows.Reverse();
                return rows;
            }
        }
    }

    public class ChatMessageWithUser : ChatMessage
    {
        public string Username { get; set; }
    }
}