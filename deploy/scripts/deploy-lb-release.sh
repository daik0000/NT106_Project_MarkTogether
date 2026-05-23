#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="${MARKTOGETHER_ROOT:-/opt/marktogether}"
RELEASE_SOURCE="${1:-}"

LB_DIR="${APP_ROOT}/lb"
BACKUP_ROOT="${APP_ROOT}/release_backups"
DATE="$(date +%Y%m%d_%H%M%S)"
BACKUP_DIR="${BACKUP_ROOT}/lb_${DATE}"

if [ -z "${RELEASE_SOURCE}" ]; then
  echo "Usage: $0 /path/to/lb-release-folder" >&2
  exit 1
fi

if [ ! -d "${RELEASE_SOURCE}" ]; then
  echo "[deploy-lb] Release source directory does not exist: ${RELEASE_SOURCE}" >&2
  exit 1
fi

if [ ! -f "${RELEASE_SOURCE}/MarkTogether.Gateway.exe" ]; then
  echo "[deploy-lb] MarkTogether.Gateway.exe not found in release source: ${RELEASE_SOURCE}" >&2
  exit 1
fi

sudo mkdir -p "${LB_DIR}" "${BACKUP_ROOT}" "${APP_ROOT}/logs"

if [ -d "${LB_DIR}" ] && [ "$(find "${LB_DIR}" -mindepth 1 -maxdepth 1 2>/dev/null | wc -l)" -gt 0 ]; then
  echo "[deploy-lb] Backing up current LB directory to ${BACKUP_DIR}..."
  sudo mkdir -p "${BACKUP_DIR}"
  sudo rsync -a --delete "${LB_DIR}/" "${BACKUP_DIR}/"
fi

echo "[deploy-lb] Stopping LB service if active..."
sudo systemctl stop marktogether-lb || true

echo "[deploy-lb] Syncing new release..."
sudo rsync -a --delete "${RELEASE_SOURCE}/" "${LB_DIR}/"

echo "[deploy-lb] Applying ownership..."
sudo chown -R marktogether:marktogether "${LB_DIR}" "${APP_ROOT}/logs"

echo "[deploy-lb] Reloading systemd and starting service..."
sudo systemctl daemon-reload
sudo systemctl enable marktogether-lb
sudo systemctl restart marktogether-lb

echo "[deploy-lb] Service status:"
sudo systemctl --no-pager --full status marktogether-lb

echo "[deploy-lb] Completed."
