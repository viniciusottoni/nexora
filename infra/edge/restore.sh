#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_DIR="${EDGE_CONFIG_DIR:-/etc/replay-edge}"
COMPOSE_FILE="${EDGE_COMPOSE_FILE:-$SCRIPT_DIR/docker-compose.yml}"
ENV_FILE="$CONFIG_DIR/edge.env"
MODE="${1:-}"
SOURCE="${2:-}"
CONFIRM="${3:-}"

if [[ "$MODE" != --verify && "$MODE" != --apply ]] || [[ ! -s "$SOURCE" ]]; then
  printf 'Uso: ./restore.sh --verify <backup.dump[.enc]>\n' >&2
  printf '     ./restore.sh --apply <backup.dump[.enc]> --confirm\n' >&2
  exit 2
fi
[[ -r "$ENV_FILE" ]] || { printf 'ERRO: configuração ausente.\n' >&2; exit 1; }
# shellcheck disable=SC1090
source "$ENV_FILE"

work_dir="$(mktemp -d)"
trap 'rm -rf -- "$work_dir"' EXIT
dump="$SOURCE"
if [[ "$SOURCE" == *.enc ]]; then
  dump="$work_dir/restore.dump"
  openssl enc -d -aes-256-cbc -pbkdf2 -iter 200000 \
    -in "$SOURCE" -out "$dump" -pass "file:$CONFIG_DIR/backup-encryption.key"
fi

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T \
  -e "PGPASSWORD=$EDGE_DB_ADMIN_PASSWORD" postgres \
  pg_restore --list < "$dump" >/dev/null

if [[ "$MODE" == --verify ]]; then
  verify_db="restore_verify_$(date +%s)_$$"
  # shellcheck disable=SC2317
  cleanup_verify() {
    docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T \
      -e "PGPASSWORD=$EDGE_DB_ADMIN_PASSWORD" postgres \
      dropdb --if-exists --username "$EDGE_DB_ADMIN_USER" "$verify_db" >/dev/null 2>&1 || true
    rm -rf -- "$work_dir"
  }
  trap cleanup_verify EXIT
  docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T \
    -e "PGPASSWORD=$EDGE_DB_ADMIN_PASSWORD" postgres \
    createdb --username "$EDGE_DB_ADMIN_USER" "$verify_db"
  docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T \
    -e "PGPASSWORD=$EDGE_DB_ADMIN_PASSWORD" postgres \
    pg_restore --exit-on-error --no-owner --no-privileges \
    --username "$EDGE_DB_ADMIN_USER" --dbname "$verify_db" < "$dump"
  docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T \
    -e "PGPASSWORD=$EDGE_DB_ADMIN_PASSWORD" postgres \
    psql --username "$EDGE_DB_ADMIN_USER" --dbname "$verify_db" --tuples-only --command 'SELECT 1' | grep -q 1
  printf 'Backup restaurável: %s\n' "$SOURCE"
  exit 0
fi

[[ "$CONFIRM" == --confirm ]] || { printf 'ERRO: restauração substitui banco atual; informe --confirm.\n' >&2; exit 2; }
"$SCRIPT_DIR/backup.sh" local >/dev/null
# O worker de sincronização deixou de ser um container separado (US-006, gap P0-1) — hoje é um
# BackgroundService embutido no próprio processo do api-edge.
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" stop api-edge web
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T \
  -e "PGPASSWORD=$EDGE_DB_ADMIN_PASSWORD" postgres \
  dropdb --if-exists --force --username "$EDGE_DB_ADMIN_USER" "$POSTGRES_DB"
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T \
  -e "PGPASSWORD=$EDGE_DB_ADMIN_PASSWORD" postgres \
  createdb --username "$EDGE_DB_ADMIN_USER" "$POSTGRES_DB"
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T \
  -e "PGPASSWORD=$EDGE_DB_ADMIN_PASSWORD" postgres \
  pg_restore --exit-on-error --no-owner --no-privileges \
  --username "$EDGE_DB_ADMIN_USER" --dbname "$POSTGRES_DB" < "$dump"
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d api-edge web
printf 'Restauração aplicada. Execute %s/doctor.sh.\n' "$SCRIPT_DIR"
