using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using MarkTogether.Server.Database.Models;

namespace MarkTogether.Server.Database.Repositories
{
    /// <summary>
    /// Repository quản lý bảng "document_shares" — chia sẻ, cấp quyền, thu hồi quyền.
    /// </summary>
    public static class DocumentShareRepository
    {
        /// <summary>
        /// Chia sẻ document với một user. Trả về ID của share vừa tạo.
        /// </summary>
        public static int ShareDocument(string docId, int userId, string permission = "viewer")
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<int>(
                    @"INSERT INTO document_shares (doc_id, user_id, permission)
                      VALUES (@DocId, @UserId, @Permission)
                      RETURNING id",
                    new { DocId = docId, UserId = userId, Permission = permission });
            }
        }

        /// <summary>
        /// Cập nhật quyền truy cập của user trên document.
        /// </summary>
        public static bool UpdatePermission(string docId, int userId, string permission)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    @"UPDATE document_shares
                      SET permission = @Permission
                      WHERE doc_id = @DocId AND user_id = @UserId",
                    new { DocId = docId, UserId = userId, Permission = permission });
                return affected > 0;
            }
        }

        /// <summary>
        /// Thu hồi quyền truy cập của user trên document.
        /// </summary>
        public static bool RevokeAccess(string docId, int userId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    "DELETE FROM document_shares WHERE doc_id = @DocId AND user_id = @UserId",
                    new { DocId = docId, UserId = userId });
                return affected > 0;
            }
        }

        /// <summary>
        /// Lấy danh sách người cộng tác trên document (JOIN với bảng users).
        /// Trả về User + Permission thông qua dynamic hoặc custom DTO.
        /// </summary>
        public static List<dynamic> GetCollaborators(string docId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.Query(
                    @"SELECT u.id, u.username, u.email, u.avatar_url,
                             ds.permission, ds.invited_at
                      FROM document_shares ds
                      INNER JOIN users u ON ds.user_id = u.id
                      WHERE ds.doc_id = @DocId
                      ORDER BY ds.invited_at ASC",
                    new { DocId = docId }).ToList();
            }
        }

        /// <summary>
        /// Kiểm tra quyền của một user trên document.
        /// Trả về "owner"/"editor"/"viewer" hoặc null nếu không có quyền.
        /// </summary>
        public static string GetPermission(string docId, int userId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();

                // Kiểm tra xem user có phải owner không
                int ownerCheck = db.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM documents WHERE id = @DocId AND owner_id = @UserId",
                    new { DocId = docId, UserId = userId });

                if (ownerCheck > 0)
                    return "owner";

                // Kiểm tra trong bảng shares
                return db.ExecuteScalar<string>(
                    @"SELECT permission FROM document_shares
                      WHERE doc_id = @DocId AND user_id = @UserId",
                    new { DocId = docId, UserId = userId });
            }
        }
    }
}
