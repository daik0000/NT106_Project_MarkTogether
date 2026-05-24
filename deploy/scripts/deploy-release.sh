#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="${MARKTOGETHER_ROOT:-/opt/marktogether}"
RELEASE_SOURCE="${1:-}"

SERVER_DIR="${APP_ROOT}/server"
BACKUP_ROOT="${APP_ROOT}/release_backups"
DATE="$(date +%Y%m%d_%H%M%S)"
BACKUP_DIR="${BACKUP_ROOT}/server_${DATE}"

if [ -z "${RELEASE_SOURCE}" ]; then
  echo "Usage: $0 /path/to/release-folder" >&2
  echo "Example: $0 /tmp/marktogether-release" >&2
  exit 1
fi

if [ ! -d "${RELEASE_SOURCE}" ]; then
  echo "[deploy] Release source directory does not exist: ${RELEASE_SOURCE}" >&2
  exit 1
fi

if [ ! -f "${RELEASE_SOURCE}/MarkTogether.Server.exe" ]; then
  echo "[deploy] MarkTogether.Server.exe not found in release source: ${RELEASE_SOURCE}" >&2
  exit 1
fi

sudo mkdir -p "${SERVER_DIR}" "${BACKUP_ROOT}" "${APP_ROOT}/storage" "${APP_ROOT}/logs" "${APP_ROOT}/backups"

if [ -d "${SERVER_DIR}" ] && [ "$(find "${SERVER_DIR}" -mindepth 1 -maxdepth 1 2>/dev/null | wc -l)" -gt 0 ]; then
  echo "[deploy] Backing up current server directory to ${BACKUP_DIR}..."
  sudo mkdir -p "${BACKUP_DIR}"
  sudo rsync -a --delete "${SERVER_DIR}/" "${BACKUP_DIR}/"
fi

echo "[deploy] Stopping MarkTogether service if active..."
sudo systemctl stop marktogether || true

echo "[deploy] Syncing new release..."
sudo rsync -a --delete "${RELEASE_SOURCE}/" "${SERVER_DIR}/"

echo "[deploy] Applying ownership..."
sudo chown -R marktogether:marktogether "${APP_ROOT}/server" "${APP_ROOT}/storage" "${APP_ROOT}/logs" "${APP_ROOT}/backups"

echo "[deploy] Reloading systemd and starting service..."
sudo systemctl daemon-reload
sudo systemctl enable marktogether
sudo systemctl restart marktogether

echo "[deploy] Service status:"
sudo systemctl --no-pager --full status marktogether

echo "[deploy] Completed."