#!/usr/bin/env bash
set -Eeuo pipefail

escaped_password=${EDGE_DB_RUNTIME_PASSWORD//\'/\'\'}
escaped_user=${EDGE_DB_RUNTIME_USER//\"/\"\"}
psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --set ON_ERROR_STOP=1 <<SQL
DO \$\$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_user_role') THEN
    CREATE ROLE app_user_role NOLOGIN;
  END IF;
END \$\$;
CREATE ROLE "$escaped_user" LOGIN PASSWORD '$escaped_password' IN ROLE app_user_role NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;
SQL
