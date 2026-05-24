using System;
using System.Data;
using Dapper;
using MarkTogether.Server.Database.Models;

namespace MarkTogether.Server.Database.Repositories
{
    public static class SharingLinkRepository
    {
        public static SharingLink Create(string docId, int createdBy, string token, string permission, DateTime? expiresAt, int? maxUses)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.QueryFirstOrDefault<SharingLink>(
                    @"INSERT INTO sharing_links (doc_id, created_by, token, permission, expires_at, max_uses)
                      VALUES (@DocId, @CreatedBy, @Token, @Permission, @ExpiresAt, @MaxUses)
                      RETURNING *",
                    new
                    {
                        DocId = docId,
                        CreatedBy = createdBy,
                        Token = token,
                        Permission = permission,
                        ExpiresAt = expiresAt,
                        MaxUses = maxUses
                    });
            }
        }

        public static SharingLink GetByToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                return db.QueryFirstOrDefault<SharingLink>(
                    "SELECT * FROM sharing_links WHERE token = @Token",
                    new { Token = token.Trim() });
            }
        }

        public static bool RevokeByToken(string token, int requesterUserId)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    @"UPDATE sharing_links sl
                      SET is_active = FALSE
                      FROM documents d
                      WHERE sl.doc_id = d.id
                        AND sl.token = @Token
                        AND d.owner_id = @RequesterUserId
                        AND sl.is_active = TRUE",
                    new { Token = token, RequesterUserId = requesterUserId });
                return affected > 0;
            }
        }

        public static bool IncrementUseCount(string token)
        {
            using (IDbConnection db = DbConnectionFactory.CreateConnection())
            {
                db.Open();
                int affected = db.Execute(
                    @"UPDATE sharing_links
                      SET use_count = use_count + 1
                      WHERE token = @Token
                        AND is_active = TRUE
                        AND (expires_at IS NULL OR expires_at > NOW())
                        AND (max_uses IS NULL OR use_count < max_uses)",
                    new { Token = token });
                return affected > 0;
            }
        }
    }
}