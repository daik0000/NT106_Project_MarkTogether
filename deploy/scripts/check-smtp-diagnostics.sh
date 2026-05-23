#!/usr/bin/env bash
set -u

APP_DIR="${1:-/opt/marktogether/app}"
SERVICE_NAME="${2:-marktogether}"
SMTP_HOST="${SMTP_HOST:-smtp-relay.brevo.com}"
SMTP_PORT="${SMTP_PORT:-2525}"
LOG_LINES="${LOG_LINES:-150}"

GREEN="\033[0;32m"
YELLOW="\033[1;33m"
RED="\033[0;31m"
BLUE="\033[0;34m"
NC="\033[0m"

section() {
  echo
  echo -e "${BLUE}========== $1 ==========${NC}"
}

ok() {
  echo -e "${GREEN}[OK]${NC} $1"
}

warn() {
  echo -e "${YELLOW}[WARN]${NC} $1"
}

fail() {
  echo -e "${RED}[FAIL]${NC} $1"
}

have_cmd() {
  command -v "$1" >/dev/null 2>&1
}

mask_config_line() {
  sed -E 's/(SmtpPassword\" value=\")[^\"]+/\1***MASKED***/Ig; s/(SmtpUser\" value=\")[^\"]+/\1***MASKED***/Ig'
}

section "MarkTogether SMTP diagnostics"
echo "App dir      : $APP_DIR"
echo "Service name : $SERVICE_NAME"
echo "SMTP target  : $SMTP_HOST:$SMTP_PORT"
echo "Log lines    : $LOG_LINES"
echo "Run time     : $(date -Is 2>/dev/null || date)"

section "1. Basic system info"
echo "User         : $(id 2>/dev/null || true)"
echo "Hostname     : $(hostname 2>/dev/null || true)"
echo "OS           : $(uname -a 2>/dev/null || true)"
echo "Working dir  : $(pwd)"

section "2. App directory and config"
if [ -d "$APP_DIR" ]; then
  ok "App directory exists: $APP_DIR"
  echo
  echo "Files in app dir:"
  ls -la "$APP_DIR" 2>/dev/null || true
else
  fail "App directory does not exist: $APP_DIR"
fi

CONFIG_FILE=""
if [ -f "$APP_DIR/MarkTogether.Server.exe.config" ]; then
  CONFIG_FILE="$APP_DIR/MarkTogether.Server.exe.config"
elif [ -f "$APP_DIR/App.config" ]; then
  CONFIG_FILE="$APP_DIR/App.config"
else
  found_config="$(find "$APP_DIR" -maxdepth 2 -iname "*.config" 2>/dev/null | head -n 1 || true)"
  if [ -n "$found_config" ]; then
    CONFIG_FILE="$found_config"
  fi
fi

if [ -n "$CONFIG_FILE" ] && [ -f "$CONFIG_FILE" ]; then
  ok "Found config file: $CONFIG_FILE"
  echo
  echo "SMTP config lines (password/user masked):"
  grep -i "Smtp" "$CONFIG_FILE" 2>/dev/null | mask_config_line || warn "No SMTP config lines found"
else
  fail "No .config file found under: $APP_DIR"
fi

section "3. Parse SMTP values from config"
CONFIG_SMTP_HOST=""
CONFIG_SMTP_PORT=""
CONFIG_SMTP_SSL=""
CONFIG_SMTP_USER_SET="no"
CONFIG_SMTP_PASS_SET="no"

if [ -n "$CONFIG_FILE" ] && [ -f "$CONFIG_FILE" ]; then
  CONFIG_SMTP_HOST="$(grep -i 'key="SmtpHost"' "$CONFIG_FILE" 2>/dev/null | sed -nE 's/.*value="([^"]*)".*/\1/p' | head -n 1)"
  CONFIG_SMTP_PORT="$(grep -i 'key="SmtpPort"' "$CONFIG_FILE" 2>/dev/null | sed -nE 's/.*value="([^"]*)".*/\1/p' | head -n 1)"
  CONFIG_SMTP_SSL="$(grep -i 'key="SmtpEnableSsl"' "$CONFIG_FILE" 2>/dev/null | sed -nE 's/.*value="([^"]*)".*/\1/p' | head -n 1)"
  CONFIG_SMTP_USER="$(grep -i 'key="SmtpUser"' "$CONFIG_FILE" 2>/dev/null | sed -nE 's/.*value="([^"]*)".*/\1/p' | head -n 1)"
  CONFIG_SMTP_PASS="$(grep -i 'key="SmtpPassword"' "$CONFIG_FILE" 2>/dev/null | sed -nE 's/.*value="([^"]*)".*/\1/p' | head -n 1)"

  [ -n "$CONFIG_SMTP_USER" ] && CONFIG_SMTP_USER_SET="yes"
  [ -n "$CONFIG_SMTP_PASS" ] && CONFIG_SMTP_PASS_SET="yes"

  echo "SmtpHost      : ${CONFIG_SMTP_HOST:-<missing>}"
  echo "SmtpPort      : ${CONFIG_SMTP_PORT:-<missing>}"
  echo "SmtpEnableSsl : ${CONFIG_SMTP_SSL:-<missing>}"
  echo "SmtpUser set  : $CONFIG_SMTP_USER_SET"
  echo "SmtpPass set  : $CONFIG_SMTP_PASS_SET"

  if [ -n "$CONFIG_SMTP_HOST" ]; then SMTP_HOST="$CONFIG_SMTP_HOST"; fi
  if [ -n "$CONFIG_SMTP_PORT" ]; then SMTP_PORT="$CONFIG_SMTP_PORT"; fi

  if [ -z "$CONFIG_SMTP_HOST" ]; then fail "SmtpHost missing in config"; fi
  if [ -z "$CONFIG_SMTP_PORT" ]; then fail "SmtpPort missing in config"; fi
  if [ "$CONFIG_SMTP_USER_SET" != "yes" ]; then fail "SmtpUser missing/empty in config"; fi
  if [ "$CONFIG_SMTP_PASS_SET" != "yes" ]; then fail "SmtpPassword missing/empty in config"; fi
else
  warn "Skipping config parse because config file was not found"
fi

section "4. DNS resolution"
if have_cmd getent; then
  if getent hosts "$SMTP_HOST"; then
    ok "DNS resolved by getent: $SMTP_HOST"
  else
    fail "DNS failed by getent: $SMTP_HOST"
  fi
elif have_cmd nslookup; then
  if nslookup "$SMTP_HOST"; then
    ok "DNS resolved by nslookup: $SMTP_HOST"
  else
    fail "DNS failed by nslookup: $SMTP_HOST"
  fi
else
  warn "Neither getent nor nslookup found; cannot test DNS directly"
fi

section "5. TCP connectivity to SMTP"
if timeout 10 bash -c "</dev/tcp/${SMTP_HOST}/${SMTP_PORT}" >/dev/null 2>&1; then
  ok "TCP connect succeeded: $SMTP_HOST:$SMTP_PORT"
else
  fail "TCP connect failed/timeout: $SMTP_HOST:$SMTP_PORT"
  warn "If this fails on VPS, provider/firewall may block outbound SMTP port $SMTP_PORT"
fi

if have_cmd nc; then
  echo
  echo "nc test:"
  if nc -vz -w 10 "$SMTP_HOST" "$SMTP_PORT"; then
    ok "nc succeeded: $SMTP_HOST:$SMTP_PORT"
  else
    fail "nc failed: $SMTP_HOST:$SMTP_PORT"
  fi
else
  warn "nc not installed; skipped nc test"
fi

section "6. STARTTLS SMTP handshake"
if have_cmd openssl; then
  OPENSSL_OUT="$(timeout 20 openssl s_client -starttls smtp -connect "${SMTP_HOST}:${SMTP_PORT}" -crlf </dev/null 2>&1 || true)"
  echo "$OPENSSL_OUT" | sed -n '1,80p'

  if echo "$OPENSSL_OUT" | grep -qi "Verify return code: 0"; then
    ok "OpenSSL STARTTLS certificate verification returned 0"
  elif echo "$OPENSSL_OUT" | grep -qi "CONNECTED"; then
    warn "OpenSSL connected, but certificate verify may not be 0. Check output above."
  else
    fail "OpenSSL STARTTLS did not connect successfully"
  fi
else
  warn "openssl not installed; skipped STARTTLS test"
fi

section "7. MarkTogether service status"
if have_cmd systemctl; then
  if systemctl list-units --type=service --all | grep -qi "^${SERVICE_NAME}.service"; then
    systemctl status "$SERVICE_NAME" --no-pager || true
  else
    warn "Service ${SERVICE_NAME}.service not found. Similar services:"
    systemctl list-units --type=service --all | grep -i "mark\|together" || true
  fi
else
  warn "systemctl not available; skipped service status"
fi

section "8. Recent service logs"
if have_cmd journalctl; then
  if systemctl list-units --type=service --all 2>/dev/null | grep -qi "^${SERVICE_NAME}.service"; then
    journalctl -u "$SERVICE_NAME" -n "$LOG_LINES" --no-pager || true
  else
    warn "Cannot read logs for ${SERVICE_NAME}: service not found"
  fi
else
  warn "journalctl not available; skipped logs"
fi

section "9. Process check"
if have_cmd pgrep; then
  pgrep -a -f "MarkTogether.Server|marktogether" || warn "No MarkTogether process found by pgrep"
else
  ps aux | grep -i "MarkTogether.Server\|marktogether" | grep -v grep || warn "No MarkTogether process found by ps"
fi

section "10. Quick interpretation"
echo "A) If TCP connect failed: VPS/provider/firewall blocks outbound SMTP, or DNS/network issue."
echo "B) If TCP/STARTTLS OK but app logs show authentication failed: Brevo SMTP login/API key is wrong, revoked, or sender/domain is not verified."
echo "C) If config lines are missing here: deployed MarkTogether.Server.exe.config is stale/wrong."
echo "D) If service logs show timeout at AUTH_FORGOT_PASSWORD: server is stuck in SendMailAsync."
echo "E) If no logs appear when pressing Forgot Password: client is not reaching this service or handler does not log that path."

echo
ok "Diagnostics completed"