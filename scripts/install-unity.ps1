param(
    [string]$EditorRoot = 'D:\Unity\Hub\Editor',
    [string]$Version = '6000.0.62f1',
    [string]$Changeset = 'f99f05b3e950',
    [string]$LocalHubInstaller = ''
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$downloadRoot = Join-Path $projectRoot '.tools'
New-Item -ItemType Directory -Path $downloadRoot -Force | Out-Null
$hubPath = 'C:\Program Files\Unity Hub\Unity Hub.exe'

if (-not (Test-Path -LiteralPath $hubPath)) {
    $installerPath = Join-Path $downloadRoot 'UnityHubSetup-x64.exe'
    $installerUrl = 'https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup-x64.exe'
    if ($LocalHubInstaller) {
        $installerPath = (Resolve-Path -LiteralPath $LocalHubInstaller).Path
    } else {
        Write-Host 'Downloading Unity Hub from the official Unity CDN...'
        & curl.exe --location --fail --show-error --connect-timeout 20 --max-time 1800 --output $installerPath $installerUrl
        if ($LASTEXITCODE -ne 0) { throw 'Unity Hub download failed. Check network access to the official CDN, then run again.' }
    }
    $signature = Get-AuthenticodeSignature -LiteralPath $installerPath
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Unity') {
        throw 'Unity Hub installer signature is not valid or not issued to Unity. Installation stopped.'
    }
    $installerProcess = Start-Process -FilePath $installerPath -ArgumentList '/S' -WindowStyle Hidden -PassThru -Wait
    if ($installerProcess.ExitCode -ne 0) { throw "Hub installer failed: $($installerProcess.ExitCode)" }
    if (-not (Test-Path -LiteralPath $hubPath)) {
        throw 'Hub was not found at its standard installation path. Locate the installed Hub before continuing.'
    }
}

New-Item -ItemType Directory -Path $EditorRoot -Force | Out-Null
# These are documented Hub headless commands. New Hub versions may direct users
# to Unity CLI instead; inspect the output rather than assuming installation worked.
function Invoke-HubAndWait([string[]]$HubArguments, [string]$LogName) {
    $stdoutPath = Join-Path $downloadRoot ($LogName + '.stdout.log')
    $stderrPath = Join-Path $downloadRoot ($LogName + '.stderr.log')
    $hubProcess = Start-Process -FilePath $hubPath -ArgumentList $HubArguments -WindowStyle Hidden -PassThru -Wait -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
    if ($hubProcess.ExitCode -ne 0) {
        Get-Content -LiteralPath $stdoutPath,$stderrPath -Tail 30
        throw "Hub command failed ($($hubProcess.ExitCode)); see $stdoutPath and $stderrPath"
    }
}
Invoke-HubAndWait -HubArguments @('--','--headless','install-path','--set',('"' + $EditorRoot + '"'),'--errors') -LogName 'unity-path'
Invoke-HubAndWait -HubArguments @('--','--headless','install','--version',$Version,'--changeset',$Changeset,'--module','android','--childModules','--errors') -LogName 'unity-install'

$editorPath = Join-Path $EditorRoot "$Version\Editor\Unity.exe"
$androidPath = Join-Path $EditorRoot "$Version\Editor\Data\PlaybackEngines\AndroidPlayer"
$required = @(
    $editorPath,
    (Join-Path $androidPath 'SDK\platform-tools\adb.exe'),
    (Join-Path $androidPath 'NDK\source.properties'),
    (Join-Path $androidPath 'OpenJDK\bin\java.exe')
)
$missing = @($required | Where-Object { -not (Test-Path -LiteralPath $_) })
if ($missing.Count -gt 0) { throw "Installation is incomplete. Missing: $($missing -join ', ')" }
Write-Host "Unity Editor and Android module files verified at $editorPath"
Write-Host 'Sign in and activate an eligible Unity license in Hub manually before opening/building the project.'
