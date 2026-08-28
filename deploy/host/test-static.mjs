import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";

const root = process.cwd();
const read = (relativePath) => fs.readFileSync(path.join(root, relativePath), "utf8");
const checksum = "65273beed27b2df543b68b65630ba525cfbad8df2b12035732b2dff87d6664e7";

const requiredFiles = [
  "deploy/host/README.md",
  "deploy/host/profile.env.example",
  "deploy/host/install-host.sh",
  "deploy/host/verify-host.sh",
  "deploy/host/New-TrailServerHyperVVm.ps1",
  "deploy/host/files/Caddyfile",
  "deploy/host/files/index.html",
  "deploy/host/files/trail-server-kiosk",
  "deploy/host/files/openbox-autostart",
  "deploy/host/files/50-trail-server.conf.template",
  "deploy/host/files/nftables.conf.template",
  "docs/decisions/0002-select-first-linux-host-profile.md",
];

for (const file of requiredFiles) {
  assert.ok(fs.existsSync(path.join(root, file)), `missing ${file}`);
}

const decision = read("docs/decisions/0002-select-first-linux-host-profile.md");
const guide = read("deploy/host/README.md");
const install = read("deploy/host/install-host.sh");
const verify = read("deploy/host/verify-host.sh");
const firewall = read("deploy/host/files/nftables.conf.template");
const placeholder = read("deploy/host/files/index.html");
const hyperV = read("deploy/host/New-TrailServerHyperVVm.ps1");

for (const content of [decision, guide, install, verify]) {
  assert.ok(content.includes("debian-13.6.0-amd64-netinst.iso"), "missing frozen ISO identity");
  assert.ok(content.includes(checksum), "missing frozen ISO checksum");
}

for (const script of [install, verify]) {
  assert.ok(script.startsWith("#!/usr/bin/env bash\nset -Eeuo pipefail"), "shell script is not fail-closed");
}

for (const packageName of ["caddy", "chromium", "docker-compose", "docker.io", "lightdm", "nftables", "openbox", "openssh-server", "xserver-xorg"]) {
  assert.ok(install.includes(packageName), `provisioner missing ${packageName}`);
  assert.ok(verify.includes(packageName), `verifier missing ${packageName}`);
}

assert.ok(firewall.includes("policy drop"), "firewall must default deny inbound traffic");
assert.ok(firewall.includes("__TRAIL_LAN_CIDR__"), "firewall must require the site LAN CIDR");
assert.match(firewall, /tcp dport \{ 22, 80 \}/, "firewall must limit the LAN TCP surface");
assert.ok(!firewall.match(/tcp dport.*\b(443|5432|3000|5000|8080)\b/), "firewall exposes an unaccepted TCP port");

assert.ok(placeholder.includes("This is not an operational Trail Server"), "placeholder must reject an operational claim");
assert.ok(hyperV.includes("SupportsShouldProcess"), "Hyper-V helper must support WhatIf and confirmation");
assert.ok(hyperV.includes("-Generation 2"), "Hyper-V helper must use Generation 2");
assert.ok(hyperV.includes("MicrosoftUEFICertificateAuthority"), "Hyper-V helper must use the Linux Secure Boot template");

console.log(`TS-002 static profile checks passed (${requiredFiles.length} files).`);
