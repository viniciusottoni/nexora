#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_DIR="${EDGE_CONFIG_DIR:-/etc/replay-edge}"
STATE_DIR="${EDGE_STATE_DIR:-/var/lib/replay-edge/install}"
KEYS_DIR="${EDGE_KEYS_DIR:-$CONFIG_DIR/keys}"
TLS_DIR="${EDGE_TLS_DIR:-$CONFIG_DIR/tls}"
BACKUP_DIR="${EDGE_BACKUP_DIR:-/var/backups/replay-edge}"
COMPOSE_FILE="${EDGE_COMPOSE_FILE:-$SCRIPT_DIR/docker-compose.yml}"
CLOUD_URL="${EDGE_CLOUD_URL:-}"
CONTAINER_GID="${EDGE_CONTAINER_GID:-1000}"
TENANT_ID=""
INSTALL_TOKEN=""
STEP=0

say() { printf '[%d/8] %s\n' "$1" "$2"; }
die() { printf 'ERRO: %s\n' "$*" >&2; exit 1; }
next_step() { STEP=$((STEP + 1)); say "$STEP" "$1"; }

usage() {
  printf 'Uso: sudo ./install.sh --tenant=<uuid> --token=<token> [--cloud-url=<https://...>]\n'
}

parse_args() {
  for arg in "$@"; do
    case "$arg" in
      --tenant=*) TENANT_ID="${arg#*=}" ;;
      --token=*) INSTALL_TOKEN="${arg#*=}" ;;
      --cloud-url=*) CLOUD_URL="${arg#*=}" ;;
      --help|-h) usage; exit 0 ;;
      *) die "Argumento desconhecido: $arg" ;;
    esac
  done
  [[ "$TENANT_ID" =~ ^[0-9a-fA-F-]{36}$ ]] || die 'Tenant inválido. Use o UUID entregue pela plataforma.'
  [[ ${#INSTALL_TOKEN} -ge 16 ]] || die 'Token de instalação ausente ou inválido.'
  [[ -n "$CLOUD_URL" ]] || die 'Defina EDGE_CLOUD_URL ou informe --cloud-url.'
  [[ "$CLOUD_URL" == https://* || "${EDGE_ALLOW_HTTP:-0}" == 1 ]] || die 'A nuvem deve usar HTTPS.'
  [[ "$CONTAINER_GID" =~ ^[0-9]+$ ]] || die 'EDGE_CONTAINER_GID deve ser numérico.'
  CLOUD_URL="${CLOUD_URL%/}"
}

sign_installation_request() {
  local method="$1" request_url="$2" message
  INSTALL_TIMESTAMP="$(date -u +%s)"
  INSTALL_NONCE="$(openssl rand -hex 16)"
  message="$(printf '%s\n%s\n%s\n%s' "$method" "$request_url" "$INSTALL_TIMESTAMP" "$INSTALL_NONCE")"
  INSTALL_SIGNATURE="$(printf '%s' "$message" | openssl pkeyutl -sign -rawin \
    -inkey "$KEYS_DIR/edge-private.pem" | openssl base64 -A)"
}

require_host() {
  if [[ "${EDGE_SKIP_ROOT_CHECK:-0}" != 1 && "$(id -u)" != 0 ]]; then
    die 'Execute como root: sudo ./install.sh --tenant=... --token=...'
  fi
  for command in docker curl jq openssl flock; do
    command -v "$command" >/dev/null || die "Dependência ausente: $command"
  done
  docker compose version >/dev/null 2>&1 || die 'Docker Compose v2 não está disponível.'
  docker info >/dev/null 2>&1 || die 'Docker não está em execução.'
}

generate_uuid_v7() {
  local timestamp_ms timestamp_hex random_hex
  timestamp_ms="$(date -u +%s%3N)"
  [[ "$timestamp_ms" =~ ^[0-9]{13}$ ]] || die 'O host precisa de GNU date com suporte a milissegundos.'
  printf -v timestamp_hex '%012x' "$timestamp_ms"
  random_hex="$(openssl rand -hex 9)"
  printf '%s-%s-7%s-8%s-%s\n' \
    "${timestamp_hex:0:8}" "${timestamp_hex:8:4}" \
    "${random_hex:0:3}" "${random_hex:3:3}" "${random_hex:6:12}"
}

prepare_identity() {
  install -d -m 700 "$CONFIG_DIR" "$STATE_DIR" "$KEYS_DIR" "$TLS_DIR" "$BACKUP_DIR/hourly" "$BACKUP_DIR/remote"
  if [[ ! -s "$STATE_DIR/installation-id" ]]; then
    generate_uuid_v7 > "$STATE_DIR/installation-id"
  fi
  INSTALLATION_ID="$(tr -d '\r\n' < "$STATE_DIR/installation-id")"
  if [[ ! -s "$KEYS_DIR/edge-private.pem" ]]; then
    openssl genpkey -algorithm ED25519 -out "$KEYS_DIR/edge-private.pem"
    openssl pkey -in "$KEYS_DIR/edge-private.pem" -pubout -out "$KEYS_DIR/edge-public.pem"
  fi
  chmod 600 "$KEYS_DIR/edge-private.pem"
  chmod 644 "$KEYS_DIR/edge-public.pem"
}

generate_tls() {
  [[ -s "$TLS_DIR/edge.local.crt" && -s "$TLS_DIR/edge.local.key" ]] && return
  local hostname
  hostname="$(hostname -f 2>/dev/null || hostname)"
  openssl genpkey -algorithm ED25519 -out "$TLS_DIR/local-ca.key"
  openssl req -x509 -new -key "$TLS_DIR/local-ca.key" -out "$TLS_DIR/local-ca.crt" \
    -days 3650 -subj '/CN=Replay Edge Local CA'
  openssl genpkey -algorithm ED25519 -out "$TLS_DIR/edge.local.key"
  openssl req -new -key "$TLS_DIR/edge.local.key" -out "$TLS_DIR/edge.local.csr" -subj '/CN=edge.local'
  printf 'subjectAltName=DNS:edge.local,DNS:%s\nextendedKeyUsage=serverAuth\n' "$hostname" > "$TLS_DIR/tls.ext"
  openssl x509 -req -in "$TLS_DIR/edge.local.csr" -CA "$TLS_DIR/local-ca.crt" \
    -CAkey "$TLS_DIR/local-ca.key" -CAcreateserial -out "$TLS_DIR/edge.local.crt" \
    -days 825 -extfile "$TLS_DIR/tls.ext"
  rm -f "$TLS_DIR/edge.local.csr" "$TLS_DIR/tls.ext" "$TLS_DIR/local-ca.srl"
  chmod 600 "$TLS_DIR/local-ca.key" "$TLS_DIR/edge.local.key"
  chmod 644 "$TLS_DIR/local-ca.crt" "$TLS_DIR/edge.local.crt"
}

register_installation() {
  local registration="$STATE_DIR/registration.json"
  if [[ -s "$registration" ]]; then
    [[ "$(jq -r '.tenant.id' "$registration")" == "$TENANT_ID" ]] || die 'Este servidor já pertence a outro tenant.'
    return
  fi

  local payload response status public_key
  payload="$(mktemp "$STATE_DIR/register-payload.XXXXXX")"
  response="$(mktemp "$STATE_DIR/register-response.XXXXXX")"
  public_key="$(openssl pkey -in "$KEYS_DIR/edge-private.pem" -pubout -outform DER | openssl base64 -A)"
  jq -n \
    --arg installationId "$INSTALLATION_ID" \
    --arg hostname "$(hostname -f 2>/dev/null || hostname)" \
    --arg version "${APP_VERSION:-0.1.0}" \
    --arg publicKey "$public_key" \
    '{installationId:$installationId,hostname:$hostname,version:$version,publicKey:$publicKey}' > "$payload"

  if ! status="$(curl --silent --show-error --output "$response" --write-out '%{http_code}' \
      --connect-timeout 10 --max-time 30 --request POST \
      --header 'Content-Type: application/json' --header "X-Install-Token: $INSTALL_TOKEN" \
      --header "Idempotency-Key: $INSTALLATION_ID" \
      --data-binary "@$payload" "$CLOUD_URL/v1/platform/installations/register")"; then
    rm -f "$payload" "$response"
    die 'Sem conexão com a nuvem no registro. Reexecute o mesmo comando quando a conexão voltar.'
  fi
  rm -f "$payload"
  if [[ "$status" != 201 ]]; then
    local detail
    detail="$(jq -r '.detail // .title // "Token inválido, expirado ou já consumido."' "$response" 2>/dev/null || true)"
    rm -f "$response"
    die "Registro recusado pela nuvem (HTTP $status): $detail"
  fi
  jq -e --arg tenant "$TENANT_ID" \
    '.tenant.id == $tenant and (.store.id | type == "string") and (.syncEndpoint | startswith("https://"))' \
    "$response" >/dev/null || { rm -f "$response"; die 'Resposta de registro inválida.'; }
  mv -f "$response" "$registration"
  chmod 600 "$registration"
}

download_initial_load() {
  local target="$STATE_DIR/initial-load.json"
  [[ -s "$target" ]] && return
  local endpoint response status cursor next_cursor has_more page_dir page_key
  endpoint="$(jq -r '.syncEndpoint' "$STATE_DIR/registration.json")"
  page_dir="$STATE_DIR/initial-pages"
  install -d -m 700 "$page_dir"
  cursor="$(cat "$STATE_DIR/initial-cursor" 2>/dev/null || printf 0)"
  [[ "$cursor" =~ ^[0-9]+$ ]] || die 'Checkpoint da carga inicial está corrompido.'

  while true; do
    local request_url="/v1/sync/pull?cursor=$cursor&limit=500"
    sign_installation_request GET "$request_url"
    response="$(mktemp "$STATE_DIR/initial-load.XXXXXX")"
    if ! status="$(curl --silent --show-error --output "$response" --write-out '%{http_code}' \
        --connect-timeout 10 --max-time 120 \
        --header "X-Installation-Id: $INSTALLATION_ID" \
        --header "X-Installation-Timestamp: $INSTALL_TIMESTAMP" \
        --header "X-Installation-Nonce: $INSTALL_NONCE" \
        --header "X-Installation-Signature: $INSTALL_SIGNATURE" \
        "$endpoint/pull?cursor=$cursor&limit=500")"; then
      rm -f "$response"
      die 'Conexão caiu durante a carga inicial. Registro e cursor preservados; reexecute para retomar.'
    fi
    [[ "$status" == 200 ]] || { rm -f "$response"; die "Carga inicial falhou (HTTP $status). Reexecute para retomar."; }
    jq -e '.events | type == "array"' "$response" >/dev/null \
      || { rm -f "$response"; die 'Carga inicial retornou JSON inválido.'; }
    next_cursor="$(jq -r '.nextCursor | tostring' "$response")"
    has_more="$(jq -r '.hasMore // false' "$response")"
    [[ "$next_cursor" =~ ^[0-9]+$ ]] || { rm -f "$response"; die 'Cursor inválido na carga inicial.'; }
    printf -v page_key '%020d' "$cursor"
    mv -f "$response" "$page_dir/page-$page_key.json"
    printf '%s' "$next_cursor" > "$STATE_DIR/initial-cursor.tmp"
    mv -f "$STATE_DIR/initial-cursor.tmp" "$STATE_DIR/initial-cursor"
    [[ "$has_more" == true ]] || break
    [[ "$next_cursor" != "$cursor" ]] || die 'Nuvem não avançou o cursor da carga inicial.'
    cursor="$next_cursor"
  done

  jq -s '{events: (map(.events) | add), nextCursor: (last | .nextCursor), hasMore: false}' \
    "$page_dir"/page-*.json > "$target.tmp"
  mv -f "$target.tmp" "$target"
  chmod 600 "$target"
}

write_environment() {
  local env_file="$CONFIG_DIR/edge.env"
  local admin_password_file="$CONFIG_DIR/db-admin-password" runtime_password_file="$CONFIG_DIR/db-runtime-password"
  [[ -s "$admin_password_file" ]] || openssl rand -hex 32 > "$admin_password_file"
  [[ -s "$runtime_password_file" ]] || openssl rand -hex 32 > "$runtime_password_file"
  local jwt_file="$CONFIG_DIR/jwt-secret" pepper_file="$CONFIG_DIR/device-hash-pepper"
  local pin_pepper_file="$CONFIG_DIR/pin-lookup-pepper"
  [[ -s "$jwt_file" ]] || openssl rand -hex 32 > "$jwt_file"
  [[ -s "$pepper_file" ]] || openssl rand -hex 32 > "$pepper_file"
  if [[ ! -s "$pin_pepper_file" ]]; then
    jq -er '.pinLookupPepper | select(type == "string" and length >= 32)' \
      "$STATE_DIR/registration.json" > "$pin_pepper_file" \
      || die 'Resposta de registro sem segredo de busca de PIN válido.'
  fi
  local sync_endpoint timezone
  sync_endpoint="$(jq -r '.syncEndpoint' "$STATE_DIR/registration.json")"
  timezone="$(jq -r '.store.timezone // "America/Fortaleza"' "$STATE_DIR/registration.json")"
  [[ "${EDGE_API_IMAGE:-}" =~ @sha256:[0-9a-f]{64}$ ]] || die 'EDGE_API_IMAGE deve ser referência imutável @sha256.'
  [[ "${EDGE_WEB_IMAGE:-}" =~ @sha256:[0-9a-f]{64}$ ]] || die 'EDGE_WEB_IMAGE deve ser referência imutável @sha256.'
  {
    printf 'EDGE_TENANT_ID=%s\n' "$TENANT_ID"
    printf 'EDGE_STORE_ID=%s\n' "$(jq -r '.store.id' "$STATE_DIR/registration.json")"
    printf 'INSTALLATION_ID=%s\n' "$INSTALLATION_ID"
    printf 'SYNC_ENDPOINT=%s\n' "$sync_endpoint"
    printf 'APP_VERSION=%s\n' "${APP_VERSION:-0.1.0}"
    printf 'POSTGRES_DB=replay_edge\n'
    printf 'EDGE_DB_ADMIN_USER=edge_admin\n'
    printf 'EDGE_DB_ADMIN_PASSWORD=%s\n' "$(tr -d '\r\n' < "$admin_password_file")"
    printf 'EDGE_DB_RUNTIME_USER=edge_runtime\n'
    printf 'EDGE_DB_RUNTIME_PASSWORD=%s\n' "$(tr -d '\r\n' < "$runtime_password_file")"
    printf 'EDGE_API_IMAGE=%s\n' "$EDGE_API_IMAGE"
    printf 'EDGE_WEB_IMAGE=%s\n' "$EDGE_WEB_IMAGE"
    printf 'EDGE_CONTAINER_GID=%s\n' "$CONTAINER_GID"
    printf 'JWT_SECRET=%s\n' "$(tr -d '\r\n' < "$jwt_file")"
    printf 'DEVICE_HASH_PEPPER=%s\n' "$(tr -d '\r\n' < "$pepper_file")"
    printf 'PIN_LOOKUP_PEPPER=%s\n' "$(tr -d '\r\n' < "$pin_pepper_file")"
    printf 'TZ=%s\n' "$timezone"
    printf 'EDGE_KEYS_DIR=%s\n' "$KEYS_DIR"
    printf 'EDGE_TLS_DIR=%s\n' "$TLS_DIR"
    printf 'EDGE_STATE_DIR=%s\n' "$STATE_DIR"
    printf 'CLOUD_URL=%s\n' "$CLOUD_URL"
  } > "$env_file.tmp"
  mv -f "$env_file.tmp" "$env_file"
  chmod 600 "$env_file" "$admin_password_file" "$runtime_password_file" "$jwt_file" "$pepper_file" "$pin_pepper_file"
  chown root:"$CONTAINER_GID" "$STATE_DIR" "$KEYS_DIR" "$STATE_DIR/registration.json" \
    "$STATE_DIR/initial-load.json" "$KEYS_DIR/edge-private.pem" "$KEYS_DIR/edge-public.pem"
  chmod 750 "$STATE_DIR" "$KEYS_DIR"
  chmod 640 "$STATE_DIR/registration.json" "$STATE_DIR/initial-load.json" \
    "$KEYS_DIR/edge-private.pem" "$KEYS_DIR/edge-public.pem"
  ENV_FILE="$env_file"
}

configure_firewall() {
  [[ "${EDGE_CONFIGURE_FIREWALL:-1}" == 1 ]] || return
  command -v ufw >/dev/null || return
  ufw allow from 10.0.0.0/8 to any port 80 proto tcp >/dev/null
  ufw allow from 10.0.0.0/8 to any port 443 proto tcp >/dev/null
  ufw allow from 172.16.0.0/12 to any port 80 proto tcp >/dev/null
  ufw allow from 172.16.0.0/12 to any port 443 proto tcp >/dev/null
  ufw allow from 192.168.0.0/16 to any port 80 proto tcp >/dev/null
  ufw allow from 192.168.0.0/16 to any port 443 proto tcp >/dev/null
}

start_containers() {
  docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" pull
  docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d --remove-orphans
}

install_backup_schedule() {
  local cron_file="${EDGE_CRON_FILE:-/etc/cron.d/replay-edge}"
  printf '12 * * * * root EDGE_CONFIG_DIR=%s EDGE_STATE_DIR=%s EDGE_BACKUP_DIR=%s %s/backup.sh auto\n' \
    "$CONFIG_DIR" "$STATE_DIR" "$BACKUP_DIR" "$SCRIPT_DIR" > "$cron_file"
  printf '42 2 * * * root EDGE_CONFIG_DIR=%s EDGE_STATE_DIR=%s EDGE_BACKUP_DIR=%s %s/backup.sh daily\n' \
    "$CONFIG_DIR" "$STATE_DIR" "$BACKUP_DIR" "$SCRIPT_DIR" >> "$cron_file"
  chmod 644 "$cron_file"
}

wait_for_health() {
  local _attempt
  for _attempt in $(seq 1 120); do
    if curl --silent --show-error --fail --cacert "$TLS_DIR/local-ca.crt" \
      --resolve edge.local:443:127.0.0.1 https://edge.local/v1/health >/dev/null; then
      return
    fi
    sleep 2
  done
  die "Containers subiram, mas health check não ficou verde. Execute $SCRIPT_DIR/doctor.sh."
}

main() {
  parse_args "$@"
  next_step 'Validando host e Docker'; require_host
  exec 9>"${EDGE_INSTALL_LOCK:-/var/lock/replay-edge-install.lock}"
  flock -n 9 || die 'Outra instalação está em andamento.'
  next_step 'Preparando identidade Ed25519'; prepare_identity
  next_step 'Gerando TLS local'; generate_tls
  next_step 'Validando token e registrando na nuvem'; register_installation
  INSTALL_TOKEN=''
  next_step 'Baixando configuração e cardápio inicial'; download_initial_load
  next_step 'Gravando configuração local'; write_environment; configure_firewall
  next_step 'Subindo containers'; start_containers
  next_step 'Configurando backup e verificando saúde'; install_backup_schedule; wait_for_health
  touch "$STATE_DIR/completed"
  printf '\nInstalação concluída. URL: https://edge.local\nDiagnóstico: %s/doctor.sh\nCA local: %s/local-ca.crt\n' "$SCRIPT_DIR" "$TLS_DIR"
}

main "$@"
