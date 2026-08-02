#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_DIR="${EDGE_CONFIG_DIR:-/etc/replay-edge}"
KEYS_DIR="${EDGE_KEYS_DIR:-$CONFIG_DIR/keys}"
BACKUP_DIR="${EDGE_BACKUP_DIR:-/var/backups/replay-edge}"
COMPOSE_FILE="${EDGE_COMPOSE_FILE:-$SCRIPT_DIR/docker-compose.yml}"
ENV_FILE="$CONFIG_DIR/edge.env"
MODE="${1:-auto}"

[[ -r "$ENV_FILE" ]] || { printf 'ERRO: configuração não encontrada: %s\n' "$ENV_FILE" >&2; exit 1; }
# shellcheck disable=SC1090
source "$ENV_FILE"
mkdir -p "$BACKUP_DIR/hourly" "$BACKUP_DIR/remote"

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
dump="$BACKUP_DIR/hourly/edge-$timestamp.dump"
tmp="$dump.tmp"

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T \
  -e "PGPASSWORD=$EDGE_DB_ADMIN_PASSWORD" postgres \
  pg_dump --format=custom --compress=9 --no-owner --no-privileges \
  --username "$EDGE_DB_ADMIN_USER" --dbname "$POSTGRES_DB" > "$tmp"
[[ -s "$tmp" ]] || { rm -f "$tmp"; printf 'ERRO: dump vazio.\n' >&2; exit 1; }
mv -f "$tmp" "$dump"
chmod 600 "$dump"

shopt -s nullglob
backup_files=("$BACKUP_DIR/hourly"/edge-*.dump)
mapfile -t old_backups < <(printf '%s\n' "${backup_files[@]}" | sort -r)
for old_backup in "${old_backups[@]:24}"; do rm -f -- "$old_backup"; done

hour="$(date +%H)"
if [[ "$MODE" == remote || "$MODE" == daily || ( "$MODE" == auto && $((10#$hour % 6)) -eq 0 ) ]]; then
  key_file="$CONFIG_DIR/backup-encryption.key"
  if [[ ! -s "$key_file" ]]; then
    openssl rand -base64 48 > "$key_file"
    chmod 600 "$key_file"
  fi
  encrypted="$BACKUP_DIR/remote/$(basename "$dump").enc"
  openssl enc -aes-256-cbc -salt -pbkdf2 -iter 200000 \
    -in "$dump" -out "$encrypted.tmp" -pass "file:$key_file"
  mv -f "$encrypted.tmp" "$encrypted"
  chmod 600 "$encrypted"

  upload_url="${BACKUP_UPLOAD_URL:-${CLOUD_URL%/}/v1/platform/installations/$INSTALLATION_ID/backups}"
  digest="$(openssl dgst -sha256 -r "$encrypted" | cut -d' ' -f1)"
  request_path="/v1/platform/installations/$INSTALLATION_ID/backups"
  request_timestamp="$(date -u +%s)"
  request_nonce="$(openssl rand -hex 16)"
  request_idempotency_key="$(tr -d '\r\n' < /proc/sys/kernel/random/uuid)"
  message="$(printf '%s\n%s\n%s\n%s' PUT "$request_path" "$request_timestamp" "$request_nonce")"
  signature="$(printf '%s' "$message" | openssl pkeyutl -sign -rawin -inkey "$KEYS_DIR/edge-private.pem" | openssl base64 -A)"
  if ! curl --silent --show-error --fail --connect-timeout 10 --max-time 300 \
      --request PUT --data-binary "@$encrypted" \
      --header 'Content-Type: application/octet-stream' \
      --header "X-Installation-Id: $INSTALLATION_ID" \
      --header "X-Content-SHA256: $digest" \
      --header "X-Installation-Timestamp: $request_timestamp" \
      --header "X-Installation-Nonce: $request_nonce" \
      --header "X-Installation-Signature: $signature" \
      --header "Idempotency-Key: $request_idempotency_key" \
      --header "X-Backup-Class: $([[ "$MODE" == daily ]] && printf daily || printf six-hour)" \
      "$upload_url" >/dev/null; then
    alert_url="${BACKUP_ALERT_URL:-${CLOUD_URL%/}/v1/platform/installations/$INSTALLATION_ID/backup-alerts}"
    alert_path="/v1/platform/installations/$INSTALLATION_ID/backup-alerts"
    alert_timestamp="$(date -u +%s)"
    alert_nonce="$(openssl rand -hex 16)"
    alert_idempotency_key="$(tr -d '\r\n' < /proc/sys/kernel/random/uuid)"
    alert_message="$(printf '%s\n%s\n%s\n%s' POST "$alert_path" "$alert_timestamp" "$alert_nonce")"
    alert_signature="$(printf '%s' "$alert_message" | openssl pkeyutl -sign -rawin -inkey "$KEYS_DIR/edge-private.pem" | openssl base64 -A)"
    curl --silent --show-error --fail --max-time 20 --request POST \
      --header 'Content-Type: application/json' \
      --header "X-Installation-Id: $INSTALLATION_ID" \
      --header "X-Installation-Timestamp: $alert_timestamp" \
      --header "X-Installation-Nonce: $alert_nonce" \
      --header "X-Installation-Signature: $alert_signature" \
      --header "Idempotency-Key: $alert_idempotency_key" \
      --data "{\"occurredAt\":\"$(date -u +%FT%TZ)\",\"reason\":\"UPLOAD_FAILED\"}" \
      "$alert_url" >/dev/null || true
    logger -t replay-edge 'Falha no envio do backup remoto; alerta enviado à plataforma.' || true
    printf 'ERRO: backup local criado, mas upload remoto falhou.\n' >&2
    exit 1
  fi
fi

printf '%s\n' "$dump"
