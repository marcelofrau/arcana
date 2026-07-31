# Publish a self-contained win-x64 build and pack it into a .zip under build/dist/.
param(
    [switch]$SkipVersion
)

$ErrorActionPreference = "Stop"

$dotnet = if (Get-Command dotnet -ErrorAction SilentlyContinue) { "dotnet" } else { "C:\Program Files\dotnet\dotnet.exe" }
$repoRoot = Resolve-Path "$PSScriptRoot"
$distDir = Join-Path $repoRoot "build/dist"
$publishDir = Join-Path $distDir "publish"
$rid = "win-x64"

if (-not $SkipVersion) {
    & "$PSScriptRoot/build/increment-version.ps1"
}
$counterRaw = (Get-Content (Join-Path $repoRoot "build/build-counter.txt") -Raw).Trim()
$versionPrefix = $counterRaw.Split('|')[0]
$buildNumber = $counterRaw.Split('|')[1]
$version = "$versionPrefix-build.$buildNumber"

Write-Host "Publishing Arcana $version ($rid, self-contained)..."
& $dotnet publish "$repoRoot/src/Arcana.App/Arcana.App.csproj" `
    -c Release `
    -r $rid `
    --self-contained true `
    -o $publishDir `
    -v q --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$zipName = "Arcana-$version-$rid.zip"
$zipPath = Join-Path $distDir $zipName
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

$tmp = Join-Path $distDir "pack_$version"
if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
Copy-Item $publishDir $tmp -Recurse
Compress-Archive -Path (Join-Path $tmp "*") -DestinationPath $zipPath -CompressionLevel Optimal
Remove-Item $tmp -Recurse -Force

Write-Host ""
Write-Host "Package ready: $zipPath"
Write-Host "  run:  .\run.ps1   (from source)"
Write-Host "  dist: $publishDir (unpacked)"
