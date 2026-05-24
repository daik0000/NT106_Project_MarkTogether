using System;
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
                    "SELECT * FROM documents WHERE id = @Id AND deleted_at IS NULL",
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
                    "SELECT * FROM documents WHERE share_code = @Code AND deleted_at IS NULL",
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
                    "SELECT COUNT(*) FROM documents WHERE share_code = @Code AND deleted_at IS NULL",
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
                    "UPDATE documents SET share_code = @Code WHERE id = @Id AND deleted_at IS NULL",
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
                              d.visibility      AS Visibility,
                              d.public_permission AS PublicPermission,
                              d.updated_at      AS UpdatedAt,
                              CASE
                                 WHEN d.owner_id = @UserId THEN 'owner'
                                 ELSE COALESCE(ds.permission, 'viewer')
                             END               AS Permission
                      FROM documents d
                       LEFT JOIN document_shares ds
                              ON d.id = ds.doc_id AND ds.user_id = @UserId
                       WHERE (d.owner_id = @UserId OR ds.user_id = @UserId)
                         AND d.deleted_at IS NULL
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
                      WHERE (d.owner_id = @UserId OR ds.user_id = @UserId)
                        AND d.deleted_at IS NULL
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
                       WHERE (d.owner_id = @UserId OR ds.user_id = @UserId)
                         AND d.deleted_at IS NULL",
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
                      WHERE id = @Id AND deleted_at IS NULL",
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
                      WHERE id = @Id AND deleted_at IS NULL",
                    new { Content = content, Id = docId });
                return affected > 0;
            }
        }

        public static bool UpdateVisibility(string docId, string visibility, string publicPermission)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    @"UPDATE documents
                      SET visibility = @Visibility,
                          public_permission = @PublicPermission,
                          is_public = CASE WHEN @Visibility = 'public' THEN TRUE ELSE FALSE END,
                          updated_at = NOW()
                      WHERE id = @Id AND deleted_at IS NULL",
                    new
                    {
                        Id = docId,
                        Visibility = visibility,
                        PublicPermission = publicPermission
                    });
                return affected > 0;
            }
        }

        public static int CountPublicDocuments()
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<int>(
                    @"SELECT COUNT(*)
                      FROM documents
                      WHERE visibility = 'public'
                        AND deleted_at IS NULL");
            }
        }

        public static List<PublicDocumentRow> GetPublicDocuments(int page = 1, int limit = 20)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int safePage = page < 1 ? 1 : page;
                int safeLimit = limit < 1 ? 20 : (limit > 100 ? 100 : limit);
                int offset = (safePage - 1) * safeLimit;

                return db.Query<PublicDocumentRow>(
                    @"SELECT d.id AS Id,
                             d.title AS Title,
                             d.owner_id AS OwnerId,
                             u.username AS OwnerUsername,
                             d.public_permission AS PublicPermission,
                             d.updated_at AS UpdatedAt
                      FROM documents d
                      INNER JOIN users u ON u.id = d.owner_id
                      WHERE d.visibility = 'public'
                        AND d.deleted_at IS NULL
                      ORDER BY d.updated_at DESC
                      LIMIT @Limit OFFSET @Offset",
                    new { Limit = safeLimit, Offset = offset }).ToList();
            }
        }

        public static bool Delete(string docId)
        {
            return SoftDelete(docId, null);
        }

        public static bool SoftDelete(string docId, int? deletedBy)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    @"UPDATE documents
                      SET deleted_at = NOW(),
                          deleted_by = @DeletedBy,
                          updated_at = NOW()
                      WHERE id = @Id AND deleted_at IS NULL",
                    new { Id = docId, DeletedBy = deletedBy });
                return affected > 0;
            }
        }

        public static List<DocumentSearchRow> SearchAccessibleDocuments(int userId, string query, string searchBy, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<DocumentSearchRow>();

            string normalizedSearchBy = (searchBy ?? "all").Trim().ToLowerInvariant();
            bool searchId = normalizedSearchBy == "id" || normalizedSearchBy == "all";
            bool searchTitle = normalizedSearchBy == "title" || normalizedSearchBy == "all";
            string trimmedQuery = query.Trim();
            string titlePattern = "%" + trimmedQuery + "%";

            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.Query<DocumentSearchRow>(
                    @"SELECT d.id AS Id,
                             d.title AS Title,
                             u.username AS OwnerUsername,
                             d.visibility AS Visibility,
                             d.updated_at AS UpdatedAt,
                             CASE
                                WHEN d.owner_id = @UserId THEN 'owner'
                                WHEN ds.permission IS NOT NULL THEN ds.permission
                                WHEN d.visibility = 'public' THEN COALESCE(d.public_permission, 'viewer')
                                ELSE NULL
                             END AS Permission
                      FROM documents d
                      INNER JOIN users u ON u.id = d.owner_id
                      LEFT JOIN document_shares ds ON ds.doc_id = d.id AND ds.user_id = @UserId
                      WHERE d.deleted_at IS NULL
                        AND (
                            d.owner_id = @UserId
                            OR ds.user_id = @UserId
                            OR d.visibility = 'public'
                        )
                        AND (
                            (@SearchId = TRUE AND d.id = @ExactQuery)
                            OR (@SearchTitle = TRUE AND d.title ILIKE @TitlePattern)
                        )
                      ORDER BY
                        CASE WHEN d.id = @ExactQuery THEN 0 ELSE 1 END,
                        d.updated_at DESC
                      LIMIT @Limit",
                    new
                    {
                        UserId = userId,
                        ExactQuery = trimmedQuery,
                        TitlePattern = titlePattern,
                        SearchId = searchId,
                        SearchTitle = searchTitle,
                        Limit = limit < 1 ? 20 : (limit > 50 ? 50 : limit)
                    }).ToList();
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

        /// <summary>
        /// [OT] Lưu một operation đã transform vào bảng document_operations.
        /// Được gọi bởi DocumentState.TransformAndApply() sau mỗi op được xử lý.
        /// </summary>
        public static void SaveOperation(DocumentOperation op)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                db.Execute(
                    @"INSERT INTO document_operations (doc_id, user_id, op_type, pos, text, length, revision, applied_at)
                      VALUES (@DocId, @UserId, @OpType, @Pos, @Text, @Length, @Revision, @AppliedAt)",
                    new
                    {
                        op.DocId,
                        op.UserId,
                        op.OpType,
                        op.Pos,
                        op.Text,
                        op.Length,
                        op.Revision,
                        AppliedAt = DateTime.UtcNow
                    });
            }
        }

        /// <summary>
        /// [OT] Lấy các operations có revision > fromRevision theo thứ tự tăng dần.
        /// Dùng để tìm các concurrent ops mà client chưa biết, phục vụ transform.
        /// </summary>
        public static List<DocumentOperation> GetOpsSince(string docId, int fromRevision)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.Query<DocumentOperation>(
                    @"SELECT * FROM document_operations
                      WHERE doc_id = @DocId AND revision > @FromRevision
                      ORDER BY revision ASC",
                    new { DocId = docId, FromRevision = fromRevision }).ToList();
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
        public string Visibility { get; set; }
        public string PublicPermission { get; set; }
        public System.DateTime UpdatedAt { get; set; }
        public string Permission { get; set; }
    }

    public class PublicDocumentRow
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int OwnerId { get; set; }
        public string OwnerUsername { get; set; }
        public string PublicPermission { get; set; }
        public System.DateTime UpdatedAt { get; set; }
    }

    public class DocumentSearchRow
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string OwnerUsername { get; set; }
        public string Visibility { get; set; }
        public System.DateTime UpdatedAt { get; set; }
        public string Permission { get; set; }
    }
}