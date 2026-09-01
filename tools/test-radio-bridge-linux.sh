#!/usr/bin/env bash
set -Eeuo pipefail

readonly image='limited-underground/trail-server-radio-bridge-linux-test:local'

docker build \
  --file server/TrailServer.RadioBridge.LinuxIntegrationTests/Dockerfile \
  --tag "$image" \
  .

timeout --signal=KILL 30s docker run --rm \
  --network none \
  --read-only \
  --user 1654:1654 \
  --pids-limit 128 \
  --memory 512m \
  --cpus 1 \
  --cap-drop ALL \
  --security-opt no-new-privileges \
  --tmpfs /tmp:rw,noexec,nosuid,nodev,size=64m,mode=1777 \
  --tmpfs /dev/serial:rw,noexec,nosuid,nodev,size=64k,mode=1777 \
  "$image"
