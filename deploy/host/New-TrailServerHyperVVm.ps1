[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string] $IsoPath,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $SwitchName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $VmRoot,

    [ValidateNotNullOrEmpty()]
    [string] $VmName = 'Limited Underground Trail Server - TS-002'
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command New-VM -ErrorAction SilentlyContinue)) {
    throw 'The Hyper-V PowerShell module is unavailable. Enable Hyper-V and its management tools first.'
}

if (Get-VM -Name $VmName -ErrorAction SilentlyContinue) {
    throw "A Hyper-V VM already exists with the name '$VmName'."
}

if (-not (Get-VMSwitch -Name $SwitchName -ErrorAction SilentlyContinue)) {
    throw "The existing Hyper-V switch '$SwitchName' was not found."
}

$resolvedIso = (Resolve-Path -LiteralPath $IsoPath).Path
$resolvedRoot = [System.IO.Path]::GetFullPath($VmRoot)
$vmDirectory = Join-Path $resolvedRoot $VmName
$vhdPath = Join-Path $vmDirectory "$VmName.vhdx"

if (-not $PSCmdlet.ShouldProcess($VmName, "Create Generation 2 VM at '$vmDirectory'")) {
    return
}

New-Item -ItemType Directory -Force -Path $vmDirectory | Out-Null

$vm = New-VM `
    -Name $VmName `
    -Generation 2 `
    -MemoryStartupBytes 4GB `
    -NewVHDPath $vhdPath `
    -NewVHDSizeBytes 40GB `
    -Path $resolvedRoot `
    -SwitchName $SwitchName

Set-VMProcessor -VM $vm -Count 2
Set-VMMemory -VM $vm -DynamicMemoryEnabled $true -MinimumBytes 2GB -StartupBytes 4GB -MaximumBytes 8GB
Set-VMFirmware -VM $vm -EnableSecureBoot On -SecureBootTemplate MicrosoftUEFICertificateAuthority
Set-VM -VM $vm -AutomaticCheckpointsEnabled $false -AutomaticStartAction Nothing -AutomaticStopAction ShutDown

$dvd = Add-VMDvdDrive -VM $vm -Path $resolvedIso -Passthru
Set-VMFirmware -VM $vm -FirstBootDevice $dvd

Get-VM -Name $VmName | Select-Object Name, State, Generation, ProcessorCount, MemoryStartup
