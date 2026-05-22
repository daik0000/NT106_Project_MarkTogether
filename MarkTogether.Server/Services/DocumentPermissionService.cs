using System;
using MarkTogether.Server.Database.Models;
using MarkTogether.Server.Database.Repositories;

namespace MarkTogether.Server.Services
{
    /// <summary>
    /// Centralized RBAC service for document-level permissions.
    /// Phase 2 keeps the current owner/share behavior for backward compatibility,
    /// while adding the "commenter" role and action-based checks.
    /// Visibility/public access is intentionally left for the next phase.
    /// </summary>
    public static class DocumentPermissionService
    {
        public const string Owner = "owner";
        public const string Editor = "editor";
        public const string Commenter = "commenter";
        public const string Viewer = "viewer";

        public const string VisibilityPublic = "public";
        public const string VisibilityPrivate = "private";
        public const string VisibilityRestricted = "restricted";

        public static string ResolvePermission(string docId, int userId)
        {
            if (string.IsNullOrWhiteSpace(docId) || userId <= 0)
                return null;

            docId = docId.Trim();
            Document document = DocumentRepository.GetById(docId);
            if (document == null || document.DeletedAt.HasValue)
                return null;

            if (document.OwnerId == userId)
                return Owner;

            string directPermission = DocumentShareRepository.GetPermission(docId, userId);
            directPermission = NormalizePermission(directPermission);
            if (directPermission != null)
                return directPermission;

            string visibility = NormalizeVisibility(document.Visibility);
            if (visibility == VisibilityPublic)
                return NormalizePublicPermission(document.PublicPermission);

            return null;
        }

        public static string NormalizePermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
                return null;

            string normalized = permission.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case Owner:
                case Editor:
                case Commenter:
                case Viewer:
                    return normalized;
                default:
                    return null;
            }
        }

        public static string NormalizeSharePermission(string permission, string fallback = Viewer)
        {
            string normalized = NormalizePermission(permission);
            if (normalized == Owner)
                return fallback;

            if (normalized == Editor || normalized == Commenter || normalized == Viewer)
                return normalized;

            return fallback;
        }

        public static string NormalizePublicPermission(string permission)
        {
            string normalized = NormalizeSharePermission(permission, Viewer);
            return normalized == Owner ? Viewer : normalized;
        }

        public static string NormalizeVisibility(string visibility)
        {
            if (string.IsNullOrWhiteSpace(visibility))
                return VisibilityRestricted;

            string normalized = visibility.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case VisibilityPublic:
                case VisibilityPrivate:
                case VisibilityRestricted:
                    return normalized;
                default:
                    return VisibilityRestricted;
            }
        }

        public static bool CanRead(string permission)
        {
            return NormalizePermission(permission) != null;
        }

        public static bool CanEdit(string permission)
        {
            permission = NormalizePermission(permission);
            return permission == Owner || permission == Editor;
        }

        public static bool CanComment(string permission)
        {
            permission = NormalizePermission(permission);
            return permission == Owner || permission == Editor || permission == Commenter;
        }

        public static bool CanManage(string permission)
        {
            return NormalizePermission(permission) == Owner;
        }

        public static bool IsOwner(string permission)
        {
            return NormalizePermission(permission) == Owner;
        }

        public static bool IsOwner(Document document, int userId)
        {
            return document != null && userId > 0 && document.OwnerId == userId;
        }
    }
}