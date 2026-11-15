param(
    [Parameter(Mandatory = $true)][string]$Version,
    [ValidateSet('x64', 'arm64')][string]$Arch = 'x64',
    [switch]$Installer
)

$ErrorActionPreference = 'Stop'
$Version = $Version -replace '^v', ''
if ($Version -notmatch '^\d+\.\d+\.\d+([+-][0-9A-Za-z.-]+)?$') { throw "Invalid release version: $Version" }
# Assembly and file versions must be numeric: a prerelease suffix is only legal in
# InformationalVersion, so "1.0.0-beta.1" would otherwise fail to compile.
$numeric = $Version -replace '[-+].*$', ''
$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $PSScriptRoot 'dist'
$rid = "win-$Arch"
$stage = Join-Path $dist ("stage-$rid-" + [Guid]::NewGuid().ToString('N'))
$zipPath = Join-Path $dist "Arcana-v$Version-$rid.zip"
$zipTemporary = "$zipPath.$([Guid]::NewGuid().ToString('N')).tmp"
New-Item -ItemType Directory -Path $stage -Force | Out-Null
try {
    Write-Host "Publishing Arcana $Version for $rid"
    foreach ($name in @('Arcana.App', 'Arcana.Cli')) {
        & dotnet publish "$root/src/$name/$name.csproj" -c Release -r $rid --self-contained true `
            "-p:PublishReadyToRun=$($Arch -eq 'x64')" "-p:Version=$Version" `
            "-p:VersionPrefix=$numeric" "-p:AssemblyVersion=$numeric" "-p:FileVersion=$numeric" `
            "-p:InformationalVersion=$Version" -o (Join-Path $stage $name)
        if ($LASTEXITCODE -ne 0) { throw "Publish failed for $name" }
    }
    Copy-Item "$root/LICENSE" $stage
    Copy-Item "$root/docs/ATTRIBUTIONS.md" (Join-Path $stage 'THIRD_PARTY_NOTICES.md')
    Copy-Item "$root/docs/PRODUCTION_READINESS.md" (Join-Path $stage 'RELEASE_NOTES.md')

    if (($Arch -eq 'x64' -and $env:PROCESSOR_ARCHITECTURE -eq 'AMD64') -or
        ($Arch -eq 'arm64' -and $env:PROCESSOR_ARCHITECTURE -eq 'ARM64')) {
        & "$PSScriptRoot/smoke-test.ps1" -CliPath (Join-Path $stage 'Arcana.Cli/Arcana.Cli.exe')
    }

    # Entry names must use '/' (ZIP spec). Windows PowerShell's ZipFile writes '\', which the
    # release verification and most Unix unzippers do not accept.
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::Open($zipTemporary, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in Get-ChildItem -Path $stage -Recurse -File) {
            $entry = $file.FullName.Substring($stage.Length).TrimStart('\', '/').Replace('\', '/')
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive, $file.FullName, $entry, [System.IO.Compression.CompressionLevel]::Optimal)
        }
    }
    finally { $archive.Dispose() }
    Move-Item $zipTemporary $zipPath -Force
    Write-Host "Portable package: $zipPath"

    if ($Installer) {
        $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
        $compiler = if ($command) { $command.Source } else { "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" }
        if (!(Test-Path $compiler)) { throw 'Install Inno Setup 6 to build the Windows installer.' }
        & $compiler "/DAppVersion=$numeric" "/DAppVersionLabel=$Version" "/DTargetArch=$Arch" "/DSourceRoot=$stage" "/DOutputRoot=$dist" "$PSScriptRoot/installer/Arcana.iss"
        if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
    }
}
finally {
    if (Test-Path $zipTemporary) { Remove-Item $zipTemporary -Force }
    Remove-Item $stage -Recurse -Force
}
