#!/usr/bin/env bash
set -uo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_DIR="${EDGE_CONFIG_DIR:-/etc/replay-edge}"
STATE_DIR="${EDGE_STATE_DIR:-/var/lib/replay-edge/install}"
TLS_DIR="${EDGE_TLS_DIR:-$CONFIG_DIR/tls}"
BACKUP_DIR="${EDGE_BACKUP_DIR:-/var/backups/replay-edge}"
COMPOSE_FILE="${EDGE_COMPOSE_FILE:-$SCRIPT_DIR/docker-compose.yml}"
ENV_FILE="$CONFIG_DIR/edge.env"
failures=0

ok() { printf 'OK    %s\n' "$1"; }
fail() { printf 'FALHA %s\n' "$1"; failures=$((failures + 1)); }

printf 'Diagnóstico Replay Edge — %s\n' "$(date -u +%FT%TZ)"
if [[ ! -r "$ENV_FILE" ]]; then fail 'config: edge.env ausente'; exit 1; fi
# shellcheck disable=SC1090
source "$ENV_FILE"

if docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" ps --status running >/dev/null 2>&1; then
  containers="$(docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" ps --status running --services 2>/dev/null)"
  # O worker de sincronização periódica deixou de ser um container separado (US-006, gap P0-1):
  # hoje é um BackgroundService embutido no processo do api-edge.
  for service in postgres redis api-edge web watchtower; do
    if grep -qx "$service" <<< "$containers"; then ok "containers: $service"; else fail "containers: $service parado"; fi
  done
else fail 'containers: Docker Compose indisponível'; fi

if docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T postgres \
  pg_isready --username "$EDGE_DB_RUNTIME_USER" --dbname "$POSTGRES_DB" >/dev/null 2>&1; then
  ok 'postgres: aceita conexões'
else fail 'postgres: indisponível'; fi
if docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T redis redis-cli ping 2>/dev/null | grep -q PONG; then
  ok 'redis: PONG'
else fail 'redis: indisponível'; fi

if openssl verify -CAfile "$TLS_DIR/local-ca.crt" "$TLS_DIR/edge.local.crt" >/dev/null 2>&1; then
  ok 'tls: certificado válido'
else fail 'tls: certificado inválido'; fi

health="$(curl --silent --show-error --fail --max-time 5 --cacert "$TLS_DIR/local-ca.crt" \
  --resolve edge.local:443:127.0.0.1 https://edge.local/v1/health 2>/dev/null || true)"
if jq -e '.postgres == "OK" and .redis == "OK"' <<< "$health" >/dev/null 2>&1; then
  ok "sync: $(jq -r '.sync' <<< "$health") · pendentes: $(jq -r '.pendingEvents' <<< "$health")"
else fail 'sync: health local indisponível'; fi

disk_percent="$(df -P "$STATE_DIR" | awk 'NR==2 {gsub(/%/,"",$5); print $5}')"
if [[ "$disk_percent" =~ ^[0-9]+$ && "$disk_percent" -lt 80 ]]; then ok "disk: ${disk_percent}% usado"; else fail "disk: ${disk_percent:-?}% usado"; fi

shopt -s nullglob
backup_files=("$BACKUP_DIR/hourly"/edge-*.dump)
latest_backup="$(printf '%s\n' "${backup_files[@]}" | sort -r | head -1)"
if [[ -n "$latest_backup" && -s "$latest_backup" ]]; then ok "backup: $(basename "$latest_backup")"; else fail 'backup: nenhum dump local'; fi

exit "$failures"
