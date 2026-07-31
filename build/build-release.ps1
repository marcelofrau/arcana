param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Arch = "x64",
    [switch]$Installer
)

# Strip leading v prefix if present
$Version = $Version -replace '^v', ''

$dotnet = (Get-Command "dotnet" -ErrorAction SilentlyContinue).Source
if ([string]::IsNullOrEmpty($dotnet)) { $dotnet = "C:\Program Files\dotnet\dotnet.exe" }

$root = Split-Path -Parent $PSScriptRoot
$dist = "$PSScriptRoot/dist"
$rid = "win-$Arch"
$zipName = "Arcana-v$Version-$rid.zip"
$zipPath = Join-Path $dist $zipName

Write-Host "Building Arcana v$Version for $rid..." -ForegroundColor Green

# Skip PublishReadyToRun for arm64 cross-compile
$r2r = if ($Arch -eq "arm64") { "false" } else { "true" }

foreach ($proj in @("$root/src/Arcana.App/Arcana.App.csproj", "$root/src/Arcana.Cli/Arcana.Cli.csproj")) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($proj)
    & $dotnet publish $proj `
        -c Release `
        -r $rid `
        --self-contained true `
        -p:PublishReadyToRun=$r2r `
        -p:Version=$Version `
        -o "$dist/$name"

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed: $proj"
        exit 1
    }
}

# Package (whole tree: Arcana.App/ + Arcana.Cli/)
# Explicit per-file entries: ZipFile.CreateFromDirectory with the zip inside
# the source dir is a known .NET conflict ("file in use by another process").
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($name in @("Arcana.App", "Arcana.Cli")) {
        $src = Join-Path $dist $name
        Get-ChildItem $src -Recurse -File | ForEach-Object {
            $entryName = ($_.FullName.Substring($dist.Length + 1) -replace '\\', '/')
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $entryName, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
}
finally {
    $zip.Dispose()
}

# Cleanup publish dirs
foreach ($name in @("Arcana.App", "Arcana.Cli")) {
    Remove-Item (Join-Path $dist $name) -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Release created: $zipPath" -ForegroundColor Green

# Installer (Inno Setup) — requires installer/Arcana.iss
if ($Installer) {
    Write-Error "Installer not yet implemented (no installer/Arcana.iss). Zip published only."
    exit 1
}
