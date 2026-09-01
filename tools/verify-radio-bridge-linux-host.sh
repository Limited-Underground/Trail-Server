#!/usr/bin/env bash
set -Eeuo pipefail

if (( $# != 1 )) || [[ ! "$1" =~ ^[0-9a-f]{40}$ ]]; then
  echo 'Usage: verify-radio-bridge-linux-host.sh <expected-commit>' >&2
  exit 2
fi

readonly expected_commit="$1"
readonly repository_root="$(git rev-parse --show-toplevel)"
cd "$repository_root"

[[ -z "${DOCKER_HOST:-}" ]] || {
  echo 'FAIL: DOCKER_HOST must be unset for local-daemon evidence' >&2
  exit 1
}

readonly actual_commit="$(git rev-parse --verify HEAD)"
[[ "$actual_commit" == "$expected_commit" ]] || {
  echo 'FAIL: source commit does not match the expected public commit' >&2
  exit 1
}
git diff --quiet
git diff --cached --quiet
[[ -z "$(git ls-files --others --exclude-standard)" ]] || {
  echo 'FAIL: source tree contains untracked files' >&2
  exit 1
}
sudo -n true

printf 'source_commit=%s\nsource_tree=clean\n' "$actual_commit"

# shellcheck disable=SC1091
. /etc/os-release
[[ "$ID" == 'debian' && "$VERSION_ID" == '13' ]] || {
  echo 'FAIL: exact Debian 13 host gate was not selected' >&2
  exit 1
}
printf 'host_os_id=%s\nhost_os_version=%s\nhost_debian_version=%s\nhost_arch=%s\nhost_kernel=%s\n' \
  "$ID" "$VERSION_ID" "$(cat /etc/debian_version)" "$(dpkg --print-architecture)" "$(uname -r)"
sudo -n docker version --format 'docker_server={{.Server.Version}}'
sudo -n docker compose version --short | sed 's/^/docker_compose=/'

sudo -n ./deploy/host/verify-host.sh 2>&1 | sed -n -E \
  '/^(PASS:|Sanitized package manifest:|caddy[[:space:]]|chromium[[:space:]]|docker-compose[[:space:]]|docker\.io[[:space:]]|lightdm[[:space:]]|nftables[[:space:]]|openbox[[:space:]]|openssh-server[[:space:]]|unclutter-xfixes[[:space:]]|xserver-xorg[[:space:]]|HOST_PROFILE_RESULT=PASS$)/p'
sudo -n ./deploy/app/verify-app.sh 2>&1 | sed -n -E \
  '/^(PASS:|TRAIL_SERVER_APP_RESULT=PASS$)/p'

sudo -n ./tools/test-radio-bridge-linux.sh 2>&1 | grep -Fx \
  'RADIO_BRIDGE_LINUX_PTY_RESULT=PASS'

sudo -n docker image inspect \
  limited-underground/trail-server-radio-bridge-linux-test:local \
  --format 'pty_test_image_id={{.Id}}'
sudo -n docker run --rm \
  --network none \
  --read-only \
  --user 1654:1654 \
  --pids-limit 32 \
  --memory 128m \
  --cpus 1 \
  --cap-drop ALL \
  --security-opt no-new-privileges \
  --entrypoint dotnet \
  limited-underground/trail-server-radio-bridge-linux-test:local \
  --list-runtimes | sed -E 's/ \[[^]]+\]$//; s/^/pty_test_runtime=/'

sudo -n ./deploy/app/verify-app.sh 2>&1 | sed -n -E \
  '/^(PASS:|TRAIL_SERVER_APP_RESULT=PASS$)/p'
sudo -n ./deploy/host/verify-host.sh 2>&1 | sed -n -E \
  '/^(PASS:|HOST_PROFILE_RESULT=PASS$)/p'

echo 'TARGET_HOST_LINUX_PTY_RESULT=PASS'
