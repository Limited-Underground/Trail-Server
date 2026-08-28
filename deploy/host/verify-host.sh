#!/usr/bin/env bash
set -Eeuo pipefail

readonly EXPECTED_IMAGE="debian-13.6.0-amd64-netinst.iso"
readonly EXPECTED_SHA256="65273beed27b2df543b68b65630ba525cfbad8df2b12035732b2dff87d6664e7"
readonly PROFILE_FILE="/etc/trail-server/host-profile.env"

fail() {
  printf 'FAIL: %s\n' "$*" >&2
  exit 1
}

pass() {
  printf 'PASS: %s\n' "$*"
}

[[ ${EUID} -eq 0 ]] || fail "run this verifier as root"
[[ -r "${PROFILE_FILE}" ]] || fail "host profile marker is missing"

# shellcheck disable=SC1091
source /etc/os-release
# shellcheck disable=SC1090
source "${PROFILE_FILE}"

[[ "${ID:-}" == "debian" && "${VERSION_ID:-}" == "13" ]] || fail "expected Debian 13"
[[ "$(dpkg --print-architecture)" == "amd64" ]] || fail "expected amd64"
[[ "${TRAIL_BASE_IMAGE:-}" == "${EXPECTED_IMAGE}" ]] || fail "unexpected base image marker"
[[ "${TRAIL_BASE_IMAGE_SHA256:-}" == "${EXPECTED_SHA256}" ]] || fail "unexpected base image hash marker"
[[ "${TRAIL_LAN_ADDRESSING:-}" == "dhcp-with-router-reservation" ]] || fail "unexpected LAN addressing policy"
[[ "${TRAIL_KIOSK_USER:-}" == "trailkiosk" ]] || fail "unexpected kiosk user"
[[ "${TRAIL_KIOSK_URL:-}" == "http://127.0.0.1/" ]] || fail "unexpected kiosk URL"
pass "frozen host identity"

for package in caddy chromium docker-compose docker.io lightdm nftables openbox openssh-server unclutter-xfixes xserver-xorg; do
  dpkg-query -W -f='${db:Status-Status}\n' "${package}" 2>/dev/null | grep -qx installed \
    || fail "package is not installed: ${package}"
done
pass "required packages installed"

for service in caddy docker lightdm nftables ssh; do
  systemctl is-enabled --quiet "${service}" || fail "service is not enabled: ${service}"
  systemctl is-active --quiet "${service}" || fail "service is not active: ${service}"
done
[[ "$(systemctl get-default)" == "graphical.target" ]] || fail "default target is not graphical"
pass "required services active after boot"

[[ "$(passwd --status trailkiosk | awk '{print $2}')" == "L" ]] || fail "kiosk password is not locked"
groups="$(id -nG trailkiosk)"
[[ " ${groups} " != *" sudo "* ]] || fail "kiosk user belongs to sudo"
[[ " ${groups} " != *" docker "* ]] || fail "kiosk user belongs to docker"
grep -q '^autologin-user=trailkiosk$' /etc/lightdm/lightdm.conf.d/50-trail-server.conf \
  || fail "LightDM kiosk autologin is not configured"
grep -q 'http://127.0.0.1/' /usr/local/libexec/trail-server-kiosk \
  || fail "kiosk launcher does not use loopback"
pass "locked non-administrative kiosk"

caddy validate --config /etc/caddy/Caddyfile >/dev/null
[[ "$(curl --fail --silent --show-error http://127.0.0.1/healthz)" == "host-profile-ready" ]] \
  || fail "Caddy host-profile health response is unavailable"
pass "loopback host-ready page"

rules="$(nft list table inet trail_server)"
grep -Fq "${TRAIL_LAN_CIDR}" <<<"${rules}" || fail "firewall does not contain the configured LAN CIDR"
grep -Eq 'tcp dport \{ 22, 80 \}.*accept' <<<"${rules}" || fail "firewall does not contain the bounded LAN ports"
pass "default-deny firewall profile"

published="$(docker ps --format '{{.Ports}}')"
if grep -Eq '0\.0\.0\.0:|\[::\]:' <<<"${published}"; then
  fail "a container publishes a port to all host interfaces"
fi

while IFS= read -r listener; do
  local_address="$(awk '{print $4}' <<<"${listener}")"
  case "${local_address}" in
    127.*:*|\[::1\]:*) continue ;;
  esac
  port="${local_address##*:}"
  [[ "${port}" == "22" || "${port}" == "80" ]] \
    || fail "unexpected non-loopback TCP listener: ${local_address}"
done < <(ss --listening --numeric --tcp --no-header)
pass "no unexpected non-loopback TCP listeners"

printf '\nSanitized package manifest:\n'
dpkg-query -W -f='${binary:Package}\t${Version}\n' \
  caddy chromium docker-compose docker.io lightdm nftables openbox openssh-server unclutter-xfixes xserver-xorg \
  | sort
printf '\nHOST_PROFILE_RESULT=PASS\n'
