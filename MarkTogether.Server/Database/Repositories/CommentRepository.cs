using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using MarkTogether.Server.Database.Models;

namespace MarkTogether.Server.Database.Repositories
{
    /// <summary>
    /// Repository quản lý bảng "document_comments".
    /// </summary>
    public static class CommentRepository
    {
        public static string Create(Comment c)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.ExecuteScalar<string>(
                    @"INSERT INTO document_comments
                          (doc_id, user_id, parent_id, anchor_start, anchor_end, anchor_text, content)
                      VALUES (@DocId, @UserId, @ParentId, @AnchorStart, @AnchorEnd, @AnchorText, @Content)
                      RETURNING id",
                    new
                    {
                        c.DocId,
                        c.UserId,
                        c.ParentId,
                        c.AnchorStart,
                        c.AnchorEnd,
                        c.AnchorText,
                        c.Content
                    });
            }
        }

        public static CommentWithUser GetById(string id)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.QueryFirstOrDefault<CommentWithUser>(
                    @"SELECT c.id            AS Id,
                             c.doc_id        AS DocId,
                             c.user_id       AS UserId,
                             c.parent_id     AS ParentId,
                             c.anchor_start  AS AnchorStart,
                             c.anchor_end    AS AnchorEnd,
                             c.anchor_text   AS AnchorText,
                             c.content       AS Content,
                             c.resolved      AS Resolved,
                             c.created_at    AS CreatedAt,
                             u.username      AS Username
                      FROM document_comments c
                      INNER JOIN users u ON c.user_id = u.id
                      WHERE c.id = @Id",
                    new { Id = id });
            }
        }

        public static List<CommentWithUser> GetByDoc(string docId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.Query<CommentWithUser>(
                    @"SELECT c.id            AS Id,
                             c.doc_id        AS DocId,
                             c.user_id       AS UserId,
                             c.parent_id     AS ParentId,
                             c.anchor_start  AS AnchorStart,
                             c.anchor_end    AS AnchorEnd,
                             c.anchor_text   AS AnchorText,
                             c.content       AS Content,
                             c.resolved      AS Resolved,
                             c.created_at    AS CreatedAt,
                             u.username      AS Username
                      FROM document_comments c
                      INNER JOIN users u ON c.user_id = u.id
                      WHERE c.doc_id = @DocId
                      ORDER BY c.created_at ASC",
                    new { DocId = docId }).ToList();
            }
        }

        public static bool SetResolved(string id, bool resolved)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    "UPDATE document_comments SET resolved = @R WHERE id = @Id",
                    new { R = resolved, Id = id });
                return affected > 0;
            }
        }

        public static bool Delete(string id)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    "DELETE FROM document_comments WHERE id = @Id",
                    new { Id = id });
                return affected > 0;
            }
        }
    }

    public class CommentWithUser : Comment
    {
        public string Username { get; set; }
    }
}