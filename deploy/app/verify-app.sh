#!/usr/bin/env bash
set -Eeuo pipefail

readonly COMPOSE_FILE="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/compose.yaml"

fail() {
  printf 'FAIL: %s\n' "$*" >&2
  exit 1
}

pass() {
  printf 'PASS: %s\n' "$*"
}

[[ ${EUID} -eq 0 ]] || fail "run this verifier as root"

docker compose -f "${COMPOSE_FILE}" ps --status running --services \
  | grep -qx 'trail-server-api' \
  || fail "Trail Server API container is not running"
pass "Trail Server API container running"

body="$(curl --fail --silent --show-error http://127.0.0.1/api/health)" \
  || fail "Caddy API health route is unavailable"
grep -Fq '"service":"limited-underground-trail-server"' <<<"${body}" \
  || fail "unexpected service identity"
grep -Fq '"operational":false' <<<"${body}" \
  || fail "service must not claim operational readiness"
grep -Fq '"status":"unavailable"' <<<"${body}" \
  || fail "radio must report unavailable"
grep -Fq '"reason":"not-configured"' <<<"${body}" \
  || fail "radio unavailability reason is missing"
pass "bounded non-operational API health response"

published="$(docker ps --filter name=trail-server-api --format '{{.Ports}}')"
grep -Fq '127.0.0.1:5080->8080/tcp' <<<"${published}" \
  || fail "API is not published only on host loopback"
if grep -Eq '0\.0\.0\.0:|\[::\]:' <<<"${published}"; then
  fail "API container publishes to all host interfaces"
fi
pass "loopback-only container publication"

systemctl show docker.service --property=After --value \
  | grep -qw 'nftables.service' \
  || fail "Docker is not ordered after nftables"
systemctl show docker.service --property=PartOf --value \
  | grep -qw 'nftables.service' \
  || fail "Docker restart is not coupled to nftables"
pass "Docker nftables recovery coupling"

printf '\nTRAIL_SERVER_APP_RESULT=PASS\n'
