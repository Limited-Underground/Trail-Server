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
  "server/TrailServer.Api/Radio/BridgeServerRadioStatus.cs",
  "server/TrailServer.RadioBridge/ServerRadioBridge.cs",
  "server/TrailServer.RadioBridge/IRadioByteTransport.cs",
  "server/TrailServer.RadioBridge/ConfiguredRadioByteTransport.cs",
  "server/TrailServer.RadioBridge/RadioBridgeOptionsValidator.cs",
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
const bridge = read("server/TrailServer.RadioBridge/ServerRadioBridge.cs");
const bridgeOptions = read("server/TrailServer.RadioBridge/RadioBridgeOptions.cs");
const configuredTransport = read("server/TrailServer.RadioBridge/ConfiguredRadioByteTransport.cs");
const optionsValidator = read("server/TrailServer.RadioBridge/RadioBridgeOptionsValidator.cs");
const appsettings = read("server/TrailServer.Api/appsettings.json");
const dockerfile = read("server/TrailServer.Api/Dockerfile");
const compose = read("deploy/app/compose.yaml");
const dockerNftables = read("deploy/app/docker-nftables.conf");
const caddy = read("deploy/host/files/Caddyfile");
const verifier = read("deploy/app/verify-app.sh");

assert.ok(project.includes("<TargetFramework>net8.0</TargetFramework>"), "service must target net8.0");
assert.ok(program.includes('app.MapGet("/api/health"'), "service health endpoint is missing");
assert.ok(program.includes("operational = false"), "service must reject an operational claim");
assert.ok(program.includes("AddHostedService<ServerRadioBridge>"), "radio bridge worker is not registered");
assert.ok(program.includes("RadioBridgeOptionsValidator"), "radio bridge options validator is not registered");
assert.ok(program.includes("ConfiguredRadioByteTransport"), "configured serial transport is not registered");
assert.ok(radio.includes('Availability: "unavailable"'), "radio must default unavailable");
assert.ok(radio.includes('Reason: "not-configured"'), "radio must explain unavailable state");
assert.ok(appsettings.includes('"Enabled": false'), "deployed radio bridge must default disabled");
assert.ok(appsettings.includes('"Transport": "disabled"'), "deployed transport must default disabled");
assert.ok(bridge.includes("BackgroundService"), "radio bridge must use supervised background lifecycle");
assert.ok(bridge.includes("RadioMessageType.RxPacket"), "bridge must explicitly handle receive-without-persistence");
assert.ok(!bridge.match(/TxSubmit|TxAccepted|RxAck/), "Phase C bridge must not transmit or acknowledge RX");
assert.ok(bridgeOptions.includes("ReconnectDelaySeconds"), "bridge reconnect delay must be bounded configuration");
assert.ok(optionsValidator.includes('StablePrefix = "/dev/serial/by-id/"'), "serial path must use stable Linux identity");
assert.ok(configuredTransport.includes("new(devicePath, baudRate, Parity.None, 8, StopBits.One)"), "serial framing changed");
assert.ok(configuredTransport.includes("Handshake = Handshake.None"), "serial flow control must remain disabled");
assert.ok(configuredTransport.includes("DtrEnable = false") && configuredTransport.includes("RtsEnable = false"), "serial control lines must remain disabled");
assert.ok(!configuredTransport.includes("GetPortNames"), "transport must not enumerate host serial devices");
assert.ok(dockerfile.includes("TrailServer.RadioBridge.csproj"), "Docker build omits bridge dependency");
assert.ok(compose.includes('"127.0.0.1:5080:8080"'), "container port must bind only to host loopback");
assert.ok(!compose.includes("network_mode: host"), "service must retain its network namespace");
assert.ok(compose.includes("TrailServer__RadioBridge__Enabled: \"false\""), "Compose must explicitly disable the bridge");
assert.ok(compose.includes("TrailServer__RadioBridge__Transport: disabled"), "Compose must explicitly disable serial transport");
assert.ok(!compose.match(/\/dev\/|devices:|privileged:/), "Phase C must not add hardware authority");
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
