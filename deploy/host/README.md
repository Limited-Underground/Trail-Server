# First functional Linux host profile

This directory is the reproducible starting point for TS-002. It configures a
minimal Debian host, a local browser kiosk, a deliberately small LAN surface,
and a bounded recovery path. It does not install a Trail radio service,
database schema, or operational server.

The authoritative selection and its limits are recorded in
[Decision 0002](../../docs/decisions/0002-select-first-linux-host-profile.md).

## Frozen installation seed

| Item | Value |
| --- | --- |
| Distribution | Debian GNU/Linux 13.6.0 (`trixie`) |
| Architecture | `amd64` |
| Image | `debian-13.6.0-amd64-netinst.iso` |
| SHA-256 | `65273beed27b2df543b68b65630ba525cfbad8df2b12035732b2dff87d6664e7` |
| Download | <https://cdimage.debian.org/debian-cd/13.6.0/amd64/iso-cd/> |

Download `SHA256SUMS` and `SHA256SUMS.sign` from the same fixed directory.
Verify Debian's signature and then verify the ISO hash. On Windows, the final
hash comparison can be made with:

```powershell
(Get-FileHash .\debian-13.6.0-amd64-netinst.iso -Algorithm SHA256).Hash
```

Do not continue if the result differs from the recorded SHA-256.

## Clean Hyper-V reproduction target

Use Windows 11 Pro with Hyper-V and an already-created external virtual switch.
Creating an external switch can briefly interrupt the laptop's network, so it
is intentionally not automated here.

The VM profile is:

- Generation 2;
- UEFI Secure Boot template `MicrosoftUEFICertificateAuthority`;
- 2 virtual CPUs;
- dynamic memory: 2 GiB minimum, 4 GiB startup, 8 GiB maximum;
- dynamically expanding 40 GiB VHDX;
- one LAN-connected virtual network adapter;
- automatic checkpoints disabled.

Preview the included helper before allowing it to create anything:

```powershell
.\deploy\host\New-TrailServerHyperVVm.ps1 `
  -IsoPath C:\Path\debian-13.6.0-amd64-netinst.iso `
  -SwitchName "Your external switch" `
  -VmRoot C:\Path\TrailServerVM `
  -WhatIf
```

Run the same command without `-WhatIf` only after reviewing the resolved VM,
switch, ISO, and storage paths.

## Debian installer selections

Use the normal, non-graphical installer and record the following selections in
the private reproduction log:

1. hostname `trail-server`; leave the domain blank;
2. leave the root password blank and create administrator `trailadmin`;
3. guided partitioning, entire disk, all files in one partition;
4. use the default Debian mirror and enable normal security updates;
5. in task selection, deselect every desktop environment;
6. select only SSH server and standard system utilities;
7. install GRUB to the VM's primary disk and reboot without the ISO.

Passwords, MAC addresses, private IP addresses, and network-specific names do
not belong in this repository or in public evidence.

## Provision

Copy the example profile to the intentionally ignored local file and replace
the LAN prefix with the VM's actual trusted IPv4 LAN:

```bash
cp deploy/host/profile.env.example deploy/host/profile.env
editor deploy/host/profile.env
sudo ./deploy/host/install-host.sh deploy/host/profile.env
sudo reboot
```

Run provisioning from the Hyper-V console until the firewall has been verified.
The host continues to use DHCP; configure a router-side reservation privately
after installation and confirm the same address is returned after reboot.

The provisioner installs only maintained Debian stable/security packages. The
base ISO stays exact while the package set remains updateable. Exact package
versions are captured by the verifier for each reproduction.

## Verify and recover

After reboot, use the local console:

```bash
sudo ./deploy/host/verify-host.sh
```

Then repeat the verifier after each recovery level:

1. restart `caddy`, `lightdm`, `docker`, `nftables`, and `ssh` individually;
2. reboot the VM;
3. reinstall the VM from the frozen image and re-run provisioning.

From a second LAN machine, verify `http://<reserved-ip>/`, SSH access for the
administrator, and denial of other probed TCP ports. Do not publish the actual
address or scan output if it includes private identifiers.

The clean-VM reinstall, reprovision, reboot, verifier, and local kiosk evidence
passed on 2026-08-28. TS-002 remains in progress until the router-reserved
external-LAN and second-machine access and blocked-port results are recorded and
reviewed.
