param(
    [string]$AdbPath = 'C:\Program Files\Unity\Hub\Editor\6000.0.62f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $AdbPath)) {
    throw 'adb.exe not found. Pass -AdbPath with the path inside your Unity Android SDK.'
}
& $AdbPath get-state
if ($LASTEXITCODE -ne 0) { throw 'Connect one phone and authorize USB debugging, then retry.' }
$health = Invoke-RestMethod -Uri 'http://127.0.0.1:8080/health' -TimeoutSec 5
if ($health.status -ne 'ok') { throw 'The Go backend health check did not return ok.' }
& $AdbPath reverse tcp:8080 tcp:8080
if ($LASTEXITCODE -ne 0) { throw 'Could not restore USB port forwarding.' }
Write-Host 'PC backend is healthy; USB forwarding restored.'
Write-Host 'Set the mobile Backend URL to http://127.0.0.1:8080 and retry submission.'
