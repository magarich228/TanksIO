$clientFolder = "Bin/WinCleint"
$serverFolder = "Bin/WinDedicated"

if (-not (Test-Path $clientFolder))
{
    New-Item -Type Directory $clientFolder
}

if (-not (Test-Path $serverFolder))
{
    New-Item -Type Directory $serverFolder
}

if (Get-Command "godot" -ErrorAction SilentlyContinue) {
    & "godot" --version
} else {
    Write-Warning "Команда 'godot' не найдена."
}

$clientName = "tanksio.exe"
$serverName = "tanksio_server.exe"

$clientPreset = "WinDesktop"
$serverPreset = "WinServer"

godot --export-debug $clientPreset "$clientFolder/$clientName"
godot --export-debug $serverPreset "$serverFolder/$serverName"