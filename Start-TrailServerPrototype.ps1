$projectPath = 'D:\ESP32\Limited-Underground-Trail-Server-Prototype'
$nodePath = 'D:\Program Files\nodejs\node.exe'
$serverCli = Join-Path $projectPath 'node_modules\vinext\dist\cli.js'
$homeUrl = 'http://localhost:4173/'

function Test-TrailPrototype {
    try {
        $response = Invoke-WebRequest -Uri $homeUrl -UseBasicParsing -TimeoutSec 2
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

if (-not (Test-TrailPrototype)) {
    if (-not (Test-Path -LiteralPath $nodePath) -or -not (Test-Path -LiteralPath $serverCli)) {
        Add-Type -AssemblyName PresentationFramework
        [System.Windows.MessageBox]::Show(
            'The local Trail Server prototype runtime is missing. The project may need npm install run again.',
            'Trail Server Prototype'
        ) | Out-Null
        exit 1
    }

    $arguments = @(
        ('"' + $serverCli + '"'),
        'dev',
        '--host',
        'localhost',
        '--port',
        '4173'
    )
    Start-Process -FilePath $nodePath -ArgumentList $arguments -WorkingDirectory $projectPath -WindowStyle Hidden

    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        Start-Sleep -Milliseconds 500
        if (Test-TrailPrototype) {
            $ready = $true
            break
        }
    }

    if (-not $ready) {
        Add-Type -AssemblyName PresentationFramework
        [System.Windows.MessageBox]::Show(
            'The local Trail Server prototype did not start within 30 seconds.',
            'Trail Server Prototype'
        ) | Out-Null
        exit 1
    }
}

Start-Process $homeUrl