# Download Tango base icon SVGs (public domain / CC-BY-SA) and render them to
# PNG icons used by the built-in "Tango" icon theme. The PNGs are committed to
# src/Arcana.App/Assets/Tango so the app needs no SVG rasterizer at runtime.
#
# Usage:  pwsh build/update-tango-icons.ps1
# Source: https://github.com/stephenc/tango-icon-theme (mirror of the
#         freedesktop Tango base theme, branch `master`, `scalable/`)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$tmp = Join-Path $env:TEMP "arcana-tango"
$tar = Join-Path $tmp "tango.tar.gz"
$extract = Join-Path $tmp "ex"

New-Item -ItemType Directory -Path $tmp -Force | Out-Null
Invoke-WebRequest -Uri "https://codeload.github.com/stephenc/tango-icon-theme/tar.gz/refs/heads/master" -OutFile $tar -UseBasicParsing
if (Test-Path $extract) { Remove-Item $extract -Recurse -Force }
New-Item -ItemType Directory -Path $extract -Force | Out-Null
tar -xzf $tar -C $extract
$root = (Get-ChildItem $extract -Directory | Select-Object -First 1).FullName

# slot name -> path inside the repo root
$icons = [ordered]@{
    "add"    = "scalable/actions/list-add.svg"
    "delete" = "scalable/actions/edit-delete.svg"
    "find"   = "scalable/actions/system-search.svg"
    "info"   = "scalable/status/dialog-information.svg"
    "save"   = "scalable/actions/document-save.svg"
}

$srcDir = Join-Path $repoRoot "tools/IconTool/src"
$outDir = Join-Path $repoRoot "src/Arcana.App/Assets/Tango"
if (Test-Path $srcDir) { Remove-Item $srcDir -Recurse -Force }
New-Item -ItemType Directory -Path $srcDir -Force | Out-Null
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

# Some entries are git symlinks; on Windows they extract as text files with the
# target path. Resolve chains and return the real SVG content.
function Get-TangoSvg([string]$path) {
    $seen = @{}
    while ($true) {
        if ($seen.ContainsKey($path)) { throw "Symlink loop at $path" }
        $seen[$path] = $true
        $full = Join-Path $root ($path -replace "/", [IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path $full)) { throw "Missing $path" }
        $text = Get-Content $full -Raw
        if ($text -match "<svg") { return $text }
        $target = $text.Trim()
        $dir = Split-Path $path -Parent
        $path = if ($target -like "*/*") { $target } else { if ($dir) { "$dir/$target" } else { $target } }
    }
}

foreach ($name in $icons.Keys) {
    $repoPath = $icons[$name]
    $dest = Join-Path $srcDir "$name.svg"
    try {
        [IO.File]::WriteAllText($dest, (Get-TangoSvg $repoPath))
        Write-Host "copied $name ($repoPath)"
    }
    catch {
        Write-Error "Failed to copy $repoPath : $_"
    }
}

& dotnet run --project (Join-Path $repoRoot "tools/IconTool") -- $srcDir $outDir 48
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Tango PNG icons written to src/Arcana.App/Assets/Tango"
