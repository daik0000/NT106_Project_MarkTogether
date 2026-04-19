using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using MarkTogether.Server.Database.Models;

namespace MarkTogether.Server.Database.Repositories
{
    /// <summary>
    /// Repository quản lý bảng "document_versions" — lưu, xem, khôi phục version.
    /// </summary>
    public static class DocumentVersionRepository
    {
        /// <summary>
        /// Lưu snapshot nội dung hiện tại thành một version mới.
        /// Trả về ID (ver_xxx) của version vừa tạo.
        /// </summary>
        public static string SaveVersion(DocumentVersion ver)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<string>(
                    @"INSERT INTO document_versions (doc_id, content_snapshot, saved_by, label)
                      VALUES (@DocId, @ContentSnapshot, @SavedBy, @Label)
                      RETURNING id",
                    new
                    {
                        ver.DocId,
                        ver.ContentSnapshot,
                        ver.SavedBy,
                        ver.Label
                    });
            }
        }

        /// <summary>
        /// Lấy danh sách versions của document (JOIN users để lấy tên người lưu).
        /// Hỗ trợ phân trang.
        /// </summary>
        public static List<dynamic> GetVersions(string docId, int page = 1, int limit = 20)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int offset = (page - 1) * limit;
                return db.Query(
                    @"SELECT dv.id, dv.doc_id, dv.saved_by, dv.saved_at, dv.label,
                             u.username AS saved_by_username, u.avatar_url AS saved_by_avatar
                      FROM document_versions dv
                      INNER JOIN users u ON dv.saved_by = u.id
                      WHERE dv.doc_id = @DocId
                      ORDER BY dv.saved_at DESC
                      LIMIT @Limit OFFSET @Offset",
                    new { DocId = docId, Limit = limit, Offset = offset }).ToList();
            }
        }

        /// <summary>
        /// Đếm tổng số versions của document (dùng cho phân trang).
        /// </summary>
        public static int CountVersions(string docId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM document_versions WHERE doc_id = @DocId",
                    new { DocId = docId });
            }
        }

        /// <summary>
        /// Lấy chi tiết version (bao gồm content_snapshot).
        /// </summary>
        public static DocumentVersion GetVersionDetail(string versionId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.QueryFirstOrDefault<DocumentVersion>(
                    "SELECT * FROM document_versions WHERE id = @Id",
                    new { Id = versionId });
            }
        }

        /// <summary>
        /// Xóa một version.
        /// </summary>
        public static bool DeleteVersion(string versionId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    "DELETE FROM document_versions WHERE id = @Id",
                    new { Id = versionId });
                return affected > 0;
            }
        }

        /// <summary>
        /// Khôi phục document về nội dung của version cũ.
        /// Copy content_snapshot của version vào documents.content.
        /// </summary>
        public static bool RestoreVersion(string docId, string versionId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();

                // Lấy content_snapshot của version cần khôi phục
                string snapshot = db.ExecuteScalar<string>(
                    "SELECT content_snapshot FROM document_versions WHERE id = @Id AND doc_id = @DocId",
                    new { Id = versionId, DocId = docId });

                if (snapshot == null)
                    return false;

                // Ghi đè nội dung document
                int affected = db.Execute(
                    @"UPDATE documents
                      SET content = @Content, updated_at = NOW()
                      WHERE id = @DocId",
                    new { Content = snapshot, DocId = docId });

                return affected > 0;
            }
        }
    }
}
