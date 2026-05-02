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
        /// <summary>
        /// Tạo document mới. Trả về ID (doc_xxx) của document vừa tạo.
        /// </summary>
        public static string Create(Document doc)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<string>(
                    @"INSERT INTO documents (owner_id, share_code, title, content, file_path_server)
                      VALUES (@OwnerId, @ShareCode, @Title, @Content, @FilePathServer)
                      RETURNING id",
                    new
                    {
                        doc.OwnerId,
                        doc.ShareCode,
                        doc.Title,
                        doc.Content,
                        doc.FilePathServer
                    });
            }
        }

        /// <summary>
        /// Lấy chi tiết document theo ID.
        /// </summary>
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

        // [ADDED] Lấy document theo ShareCode
        public static Document GetByShareCode(string shareCode)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.QueryFirstOrDefault<Document>(
                    "SELECT * FROM documents WHERE share_code = @Code",
                    new { Code = shareCode });
            }
        }

        // [ADDED] Cập nhật ShareCode cho document
        public static bool UpdateShareCode(string docId, string shareCode)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    @"UPDATE documents
                      SET share_code = @Code, updated_at = NOW()
                      WHERE id = @Id",
                    new { Code = shareCode, Id = docId });
                return affected > 0;
            }
        }

        /// <summary>
        /// Lấy danh sách documents mà user sở hữu HOẶC được share.
        /// Hỗ trợ phân trang.
        /// </summary>
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

        /// <summary>
        /// Đếm tổng số documents mà user có quyền truy cập (dùng cho phân trang).
        /// </summary>
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

        /// <summary>
        /// Cập nhật tiêu đề document.
        /// </summary>
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

        /// <summary>
        /// Cập nhật nội dung document (content).
        /// </summary>
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

        /// <summary>
        /// Xóa document (cascade sẽ tự động xóa shares, versions, operations).
        /// </summary>
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

        /// <summary>
        /// Lấy revision hiện tại = MAX(revision) từ document_operations.
        /// Trả về 0 nếu chưa có operation nào.
        /// </summary>
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

        // [OT] Save operation history
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

        // [OT] Get operations since a revision
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
}
