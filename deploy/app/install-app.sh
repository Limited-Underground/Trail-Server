#!/usr/bin/env bash
set -Eeuo pipefail

readonly SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
readonly REPOSITORY_ROOT="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"
readonly COMPOSE_FILE="${SCRIPT_DIR}/compose.yaml"
readonly CADDY_SOURCE="${REPOSITORY_ROOT}/deploy/host/files/Caddyfile"
readonly DOCKER_DROP_IN_SOURCE="${SCRIPT_DIR}/docker-nftables.conf"

fail() {
  printf 'ERROR: %s\n' "$*" >&2
  exit 1
}

[[ ${EUID} -eq 0 ]] || fail "run this installer as root"
[[ -r "${COMPOSE_FILE}" ]] || fail "Compose file is missing"
[[ -r "${CADDY_SOURCE}" ]] || fail "Caddy source is missing"
[[ -r "${DOCKER_DROP_IN_SOURCE}" ]] || fail "Docker nftables drop-in is missing"

caddy validate --config "${CADDY_SOURCE}"
install -d -m 0755 /etc/systemd/system/docker.service.d
install -m 0644 "${DOCKER_DROP_IN_SOURCE}" \
  /etc/systemd/system/docker.service.d/10-trail-server-nftables.conf
systemctl daemon-reload
systemctl restart docker
docker compose -f "${COMPOSE_FILE}" up --detach --build
install -m 0644 "${CADDY_SOURCE}" /etc/caddy/Caddyfile

# The hardened Caddy configuration disables its admin endpoint, so reload is
# intentionally unavailable. A bounded service restart applies the validated
# configuration.
systemctl restart caddy
"${SCRIPT_DIR}/verify-app.sh"
