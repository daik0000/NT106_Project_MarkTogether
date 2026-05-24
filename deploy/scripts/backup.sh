#!/usr/bin/env bash
set -euo pipefail

BACKUP_DIR="${MARKTOGETHER_BACKUP_DIR:-/opt/marktogether/backups}"
STORAGE_DIR="${MARKTOGETHER_STORAGE_PATH:-/opt/marktogether/storage}"
CONTAINER_NAME="${MARKTOGETHER_POSTGRES_CONTAINER:-marktogether-postgres}"
DB_NAME="${MARKTOGETHER_DB_NAME:-marktogether_db}"
DB_USER="${MARKTOGETHER_DB_USER:-marktogether_user}"
RETENTION_DAYS="${MARKTOGETHER_BACKUP_RETENTION_DAYS:-7}"

DATE="$(date +%Y%m%d_%H%M%S)"
DB_BACKUP_FILE="${BACKUP_DIR}/db_${DATE}.sql.gz"
STORAGE_BACKUP_DIR="${BACKUP_DIR}/storage_latest"

mkdir -p "${BACKUP_DIR}" "${STORAGE_BACKUP_DIR}"

if ! docker ps --format '{{.Names}}' | grep -Fxq "${CONTAINER_NAME}"; then
  echo "[backup] PostgreSQL container '${CONTAINER_NAME}' is not running." >&2
  exit 1
fi

echo "[backup] Dumping PostgreSQL database '${DB_NAME}' from container '${CONTAINER_NAME}'..."
docker exec "${CONTAINER_NAME}" pg_dump -U "${DB_USER}" "${DB_NAME}" | gzip > "${DB_BACKUP_FILE}"

if [ ! -s "${DB_BACKUP_FILE}" ]; then
  echo "[backup] Database backup file is empty: ${DB_BACKUP_FILE}" >&2
  exit 1
fi

echo "[backup] Syncing storage '${STORAGE_DIR}' to '${STORAGE_BACKUP_DIR}'..."
if [ -d "${STORAGE_DIR}" ]; then
  rsync -a --delete "${STORAGE_DIR}/" "${STORAGE_BACKUP_DIR}/"
else
  echo "[backup] Storage directory '${STORAGE_DIR}' does not exist yet; skipping storage sync."
fi

echo "[backup] Removing DB backups older than ${RETENTION_DAYS} days..."
find "${BACKUP_DIR}" -type f -name "db_*.sql.gz" -mtime "+${RETENTION_DAYS}" -delete

echo "[backup] Completed: ${DB_BACKUP_FILE}"