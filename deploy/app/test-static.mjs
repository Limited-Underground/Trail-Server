import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";

const root = process.cwd();
const read = (relativePath) => fs.readFileSync(path.join(root, relativePath), "utf8");

const requiredFiles = [
  "server/TrailServer.Api/TrailServer.Api.csproj",
  "server/TrailServer.Api/Program.cs",
  "server/TrailServer.Api/Configuration/TrailServerOptions.cs",
  "server/TrailServer.Api/Radio/IServerRadioStatus.cs",
  "server/TrailServer.Api/Radio/UnavailableServerRadioStatus.cs",
  "server/TrailServer.Api/Dockerfile",
  "deploy/app/compose.yaml",
  "deploy/app/docker-nftables.conf",
  "deploy/app/install-app.sh",
  "deploy/app/verify-app.sh",
];

for (const file of requiredFiles) {
  assert.ok(fs.existsSync(path.join(root, file)), `missing ${file}`);
}

const project = read("server/TrailServer.Api/TrailServer.Api.csproj");
const program = read("server/TrailServer.Api/Program.cs");
const radio = read("server/TrailServer.Api/Radio/UnavailableServerRadioStatus.cs");
const compose = read("deploy/app/compose.yaml");
const dockerNftables = read("deploy/app/docker-nftables.conf");
const caddy = read("deploy/host/files/Caddyfile");
const verifier = read("deploy/app/verify-app.sh");

assert.ok(project.includes("<TargetFramework>net8.0</TargetFramework>"), "service must target net8.0");
assert.ok(program.includes('app.MapGet("/api/health"'), "service health endpoint is missing");
assert.ok(program.includes("operational = false"), "service must reject an operational claim");
assert.ok(radio.includes('Availability: "unavailable"'), "radio must default unavailable");
assert.ok(radio.includes('Reason: "not-configured"'), "radio must explain unavailable state");
assert.ok(!program.match(/serial|packet|transmit|receive/i), "TS-004 scaffold must not invent the TS-003 radio contract");
assert.ok(compose.includes('"127.0.0.1:5080:8080"'), "container port must bind only to host loopback");
assert.ok(!compose.includes("network_mode: host"), "service must retain its network namespace");
assert.ok(compose.includes("no-new-privileges:true"), "container must reject privilege escalation");
assert.ok(compose.includes("cap_drop:\n      - ALL"), "container must drop Linux capabilities");
assert.ok(dockerNftables.includes("After=nftables.service"), "Docker must start after nftables");
assert.ok(dockerNftables.includes("PartOf=nftables.service"), "Docker restart must follow nftables restart");
assert.match(caddy, /handle \/api\/\* \{\s+reverse_proxy 127\.0\.0\.1:5080\s+\}/, "Caddy API proxy is missing");
assert.ok(verifier.startsWith("#!/usr/bin/env bash\nset -Eeuo pipefail"), "app verifier is not fail-closed");
assert.ok(verifier.includes('"operational":false'), "app verifier must reject an operational claim");
assert.ok(verifier.includes('"status":"unavailable"'), "app verifier must require unavailable radio");
assert.ok(verifier.includes("127.0.0.1:5080->8080/tcp"), "app verifier must require loopback publication");

console.log(`Trail Server service scaffold checks passed (${requiredFiles.length} files).`);
