#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
backup="$("$SCRIPT_DIR/backup.sh" local | tail -1)"
"$SCRIPT_DIR/restore.sh" --verify "$backup"
