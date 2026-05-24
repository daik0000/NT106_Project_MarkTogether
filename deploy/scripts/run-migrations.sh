#!/usr/bin/env bash
set -euo pipefail

APP_DIR="${MARKTOGETHER_APP_DIR:-/opt/marktogether/app}"
CONTAINER_NAME="${MARKTOGETHER_POSTGRES_CONTAINER:-marktogether-postgres}"
DB_NAME="${MARKTOGETHER_DB_NAME:-marktogether_db}"
DB_USER="${MARKTOGETHER_DB_USER:-marktogether_user}"
MIGRATION_FILE="${APP_DIR}/MarkTogether.Server/Database/Scripts/migration_v2.sql"

if [ ! -f "${MIGRATION_FILE}" ]; then
  echo "[migration] Migration file not found: ${MIGRATION_FILE}" >&2
  exit 1
fi

if ! docker ps --format '{{.Names}}' | grep -Fxq "${CONTAINER_NAME}"; then
  echo "[migration] PostgreSQL container '${CONTAINER_NAME}' is not running." >&2
  exit 1
fi

echo "[migration] Waiting for PostgreSQL readiness..."
for i in $(seq 1 30); do
  if docker exec "${CONTAINER_NAME}" pg_isready -U "${DB_USER}" -d "${DB_NAME}" >/dev/null 2>&1; then
    break
  fi

  if [ "${i}" -eq 30 ]; then
    echo "[migration] PostgreSQL is not ready after 30 attempts." >&2
    exit 1
  fi

  sleep 2
done

echo "[migration] Applying migration_v2.sql to '${DB_NAME}'..."
docker exec -i "${CONTAINER_NAME}" psql -v ON_ERROR_STOP=1 -U "${DB_USER}" -d "${DB_NAME}" < "${MIGRATION_FILE}"

echo "[migration] Completed."