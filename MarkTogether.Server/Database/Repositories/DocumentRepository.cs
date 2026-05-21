using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using MarkTogether.Server.Database.Models;

namespace MarkTogether.Server.Database.Repositories
{
    /// <summary>
    /// Repository quản lý bảng "documents" — tạo, đọc, cập nhật, xóa tài liệu.
    /// </summary>
    public static class DocumentRepository
    {
        public static string Create(Document doc)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<string>(
                    @"INSERT INTO documents (owner_id, title, content, file_path_server, share_code)
                      VALUES (@OwnerId, @Title, @Content, @FilePathServer, @ShareCode)
                      RETURNING id",
                    new
                    {
                        doc.OwnerId,
                        doc.Title,
                        doc.Content,
                        doc.FilePathServer,
                        doc.ShareCode
                    });
            }
        }

        public static Document GetById(string docId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.QueryFirstOrDefault<Document>(
                    "SELECT * FROM documents WHERE id = @Id",
                    new { Id = docId });
            }
        }

        public static Document GetByShareCode(string shareCode)
        {
            if (string.IsNullOrWhiteSpace(shareCode)) return null;

            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.QueryFirstOrDefault<Document>(
                    "SELECT * FROM documents WHERE share_code = @Code",
                    new { Code = shareCode });
            }
        }

        public static bool ShareCodeExists(string shareCode)
        {
            if (string.IsNullOrWhiteSpace(shareCode)) return false;

            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int count = db.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM documents WHERE share_code = @Code",
                    new { Code = shareCode });
                return count > 0;
            }
        }

        public static bool UpdateShareCode(string docId, string shareCode)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    "UPDATE documents SET share_code = @Code WHERE id = @Id",
                    new { Code = shareCode, Id = docId });
                return affected > 0;
            }
        }

        /// <summary>
        /// Danh sách doc user sở hữu hoặc được share — kèm permission đã resolve.
        /// </summary>
        public static List<DocumentWithPermission> GetByUserIdWithPermission(int userId, int page = 1, int limit = 100)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int offset = (page - 1) * limit;
                return db.Query<DocumentWithPermission>(
                    @"SELECT d.id              AS Id,
                             d.owner_id        AS OwnerId,
                             d.title           AS Title,
                             d.updated_at      AS UpdatedAt,
                             CASE
                                 WHEN d.owner_id = @UserId THEN 'owner'
                                 ELSE COALESCE(ds.permission, 'viewer')
                             END               AS Permission
                      FROM documents d
                      LEFT JOIN document_shares ds
                             ON d.id = ds.doc_id AND ds.user_id = @UserId
                      WHERE d.owner_id = @UserId OR ds.user_id = @UserId
                      ORDER BY d.updated_at DESC
                      LIMIT @Limit OFFSET @Offset",
                    new { UserId = userId, Limit = limit, Offset = offset }).ToList();
            }
        }

        public static List<Document> GetByUserId(int userId, int page = 1, int limit = 20)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int offset = (page - 1) * limit;
                return db.Query<Document>(
                    @"SELECT DISTINCT d.*
                      FROM documents d
                      LEFT JOIN document_shares ds ON d.id = ds.doc_id
                      WHERE d.owner_id = @UserId OR ds.user_id = @UserId
                      ORDER BY d.updated_at DESC
                      LIMIT @Limit OFFSET @Offset",
                    new { UserId = userId, Limit = limit, Offset = offset }).ToList();
            }
        }

        public static int CountByUserId(int userId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<int>(
                    @"SELECT COUNT(DISTINCT d.id)
                      FROM documents d
                      LEFT JOIN document_shares ds ON d.id = ds.doc_id
                      WHERE d.owner_id = @UserId OR ds.user_id = @UserId",
                    new { UserId = userId });
            }
        }

        public static bool UpdateTitle(string docId, string title)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    @"UPDATE documents
                      SET title = @Title, updated_at = NOW()
                      WHERE id = @Id",
                    new { Title = title, Id = docId });
                return affected > 0;
            }
        }

        public static bool UpdateContent(string docId, string content)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    @"UPDATE documents
                      SET content = @Content, updated_at = NOW()
                      WHERE id = @Id",
                    new { Content = content, Id = docId });
                return affected > 0;
            }
        }

        public static bool Delete(string docId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    "DELETE FROM documents WHERE id = @Id",
                    new { Id = docId });
                return affected > 0;
            }
        }

        public static int GetCurrentRevision(string docId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<int>(
                    @"SELECT COALESCE(MAX(revision), 0)
                      FROM document_operations
                      WHERE doc_id = @DocId",
                    new { DocId = docId });
            }
        }
    }

    /// <summary>
    /// DTO trả về từ join documents + document_shares cho danh sách hiển thị HomeForm.
    /// </summary>
    public class DocumentWithPermission
    {
        public string Id { get; set; }
        public int OwnerId { get; set; }
        public string Title { get; set; }
        public System.DateTime UpdatedAt { get; set; }
        public string Permission { get; set; }
    }
}