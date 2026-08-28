#!/usr/bin/env bash
set -Eeuo pipefail

readonly SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
readonly PROFILE_PATH="${1:-${SCRIPT_DIR}/profile.env}"
readonly BASE_IMAGE="debian-13.6.0-amd64-netinst.iso"
readonly BASE_IMAGE_SHA256="65273beed27b2df543b68b65630ba525cfbad8df2b12035732b2dff87d6664e7"
readonly KIOSK_USER="trailkiosk"
readonly KIOSK_URL="http://127.0.0.1/"

fail() {
  printf 'ERROR: %s\n' "$*" >&2
  exit 1
}

[[ ${EUID} -eq 0 ]] || fail "run this provisioner as root"
[[ -r "${PROFILE_PATH}" ]] || fail "profile file is not readable: ${PROFILE_PATH}"

# The local profile is trusted root input and must contain no credentials.
# shellcheck disable=SC1090
source "${PROFILE_PATH}"

: "${TRAIL_ADMIN_USER:?TRAIL_ADMIN_USER is required}"
: "${TRAIL_LAN_CIDR:?TRAIL_LAN_CIDR is required}"

[[ "${TRAIL_ADMIN_USER}" =~ ^[a-z_][a-z0-9_-]*$ ]] || fail "invalid administrator user"
[[ "${TRAIL_LAN_CIDR}" != REPLACE_* ]] || fail "replace the LAN CIDR placeholder"
[[ "${TRAIL_LAN_CIDR}" =~ ^([0-9]{1,3}\.){3}[0-9]{1,3}/([0-9]|[12][0-9]|3[0-2])$ ]] || fail "LAN CIDR must be IPv4 CIDR notation"

# shellcheck disable=SC1091
source /etc/os-release
[[ "${ID:-}" == "debian" ]] || fail "this profile requires Debian"
[[ "${VERSION_ID:-}" == "13" ]] || fail "this profile requires Debian major version 13"
[[ "$(dpkg --print-architecture)" == "amd64" ]] || fail "this profile requires amd64"
id "${TRAIL_ADMIN_USER}" >/dev/null 2>&1 || fail "installer-created administrator does not exist"

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get -y full-upgrade
apt-get install -y \
  caddy \
  chromium \
  curl \
  docker-compose \
  docker.io \
  git \
  lightdm \
  nftables \
  openbox \
  openssh-server \
  sudo \
  unclutter-xfixes \
  x11-xserver-utils \
  xserver-xorg

hostnamectl set-hostname trail-server
usermod -aG sudo "${TRAIL_ADMIN_USER}"

if ! id "${KIOSK_USER}" >/dev/null 2>&1; then
  useradd --create-home --shell /bin/bash "${KIOSK_USER}"
fi
passwd --lock "${KIOSK_USER}" >/dev/null
gpasswd --delete "${KIOSK_USER}" sudo >/dev/null 2>&1 || true
gpasswd --delete "${KIOSK_USER}" docker >/dev/null 2>&1 || true

install -d -m 0755 /etc/trail-server /etc/lightdm/lightdm.conf.d /usr/local/libexec
install -d -m 0755 /var/www/trail-server
install -d -m 0755 -o "${KIOSK_USER}" -g "${KIOSK_USER}" "/home/${KIOSK_USER}/.config/openbox"

install -m 0644 "${SCRIPT_DIR}/files/Caddyfile" /etc/caddy/Caddyfile
install -m 0644 "${SCRIPT_DIR}/files/index.html" /var/www/trail-server/index.html
printf 'host-profile-ready\n' > /var/www/trail-server/healthz
chmod 0644 /var/www/trail-server/healthz

install -m 0755 "${SCRIPT_DIR}/files/trail-server-kiosk" /usr/local/libexec/trail-server-kiosk
install -m 0644 "${SCRIPT_DIR}/files/openbox-autostart" "/home/${KIOSK_USER}/.config/openbox/autostart"
chown -R "${KIOSK_USER}:${KIOSK_USER}" "/home/${KIOSK_USER}/.config"

sed "s|__TRAIL_KIOSK_USER__|${KIOSK_USER}|g" \
  "${SCRIPT_DIR}/files/50-trail-server.conf.template" \
  > /etc/lightdm/lightdm.conf.d/50-trail-server.conf
chmod 0644 /etc/lightdm/lightdm.conf.d/50-trail-server.conf

sed "s|__TRAIL_LAN_CIDR__|${TRAIL_LAN_CIDR}|g" \
  "${SCRIPT_DIR}/files/nftables.conf.template" \
  > /etc/nftables.conf
chmod 0644 /etc/nftables.conf
nft --check --file /etc/nftables.conf

cat > /etc/trail-server/host-profile.env <<EOF
TRAIL_HOST_PROFILE=TS-002-v0
TRAIL_BASE_IMAGE=${BASE_IMAGE}
TRAIL_BASE_IMAGE_SHA256=${BASE_IMAGE_SHA256}
TRAIL_ARCHITECTURE=amd64
TRAIL_LAN_ADDRESSING=dhcp-with-router-reservation
TRAIL_LAN_CIDR=${TRAIL_LAN_CIDR}
TRAIL_KIOSK_USER=${KIOSK_USER}
TRAIL_KIOSK_URL=${KIOSK_URL}
EOF
chmod 0644 /etc/trail-server/host-profile.env

caddy validate --config /etc/caddy/Caddyfile
systemctl enable caddy docker lightdm nftables ssh
systemctl restart nftables
systemctl restart docker
systemctl restart caddy
systemctl restart ssh
systemctl set-default graphical.target

printf '\nProvisioning complete. Reboot, configure the private router reservation, then run:\n'
printf '  sudo %s/verify-host.sh\n' "${SCRIPT_DIR}"
