# Download La Capitaine icon SVGs (GPL-3.0, https://github.com/keeferrourke/
# la-capitaine-icon-theme) and render them to PNG icons used by the built-in
# "La Capitaine" icon theme. The PNGs are committed to
# src/Arcana.App/Assets/LaCapitaine so the app needs no SVG rasterizer at runtime.
#
# Usage:  pwsh build/update-lacapitaine-icons.ps1
# Source: https://github.com/keeferrourke/la-capitaine-icon-theme (branch `master`)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$tmp = Join-Path $env:TEMP "arcana-lacapitaine"
$tar = Join-Path $tmp "la-capitaine.tar.gz"
$extract = Join-Path $tmp "ex"

New-Item -ItemType Directory -Path $tmp -Force | Out-Null
Invoke-WebRequest -Uri "https://codeload.github.com/keeferrourke/la-capitaine-icon-theme/tar.gz/refs/heads/master" -OutFile $tar -UseBasicParsing
if (Test-Path $extract) { Remove-Item $extract -Recurse -Force }
New-Item -ItemType Directory -Path $extract -Force | Out-Null
tar -xzf $tar -C $extract
$root = (Get-ChildItem $extract -Directory | Select-Object -First 1).FullName

# slot name -> path inside the repo root
$icons = [ordered]@{
    "open"     = "places/scalable/folder-open.svg"
    "add"      = "actions/22x22/list-add.svg"
    "extract"  = "actions/22x22/archive-extract.svg"
    "test"     = "actions/22x22/dialog-ok.svg"
    "view"     = "actions/22x22/view-preview.svg"
    "delete"   = "actions/22x22/edit-delete.svg"
    "find"     = "actions/22x22/system-search.svg"
    "info"     = "status/scalable/dialog-information.svg"
    "save"     = "actions/22x22/document-save.svg"
    "settings" = "actions/22x22/configure.svg"
    "sort-up"  = "actions/22x22/view-sort-ascending.svg"
    "sort-down"= "actions/22x22/view-sort-descending.svg"
}

$srcDir = Join-Path $repoRoot "tools/IconTool/src"
$outDir = Join-Path $repoRoot "src/Arcana.App/Assets/LaCapitaine"
if (Test-Path $srcDir) { Remove-Item $srcDir -Recurse -Force }
New-Item -ItemType Directory -Path $srcDir -Force | Out-Null
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

# Some entries are git symlinks; on Windows they extract as text files with the
# target path. Resolve chains and return the real SVG content.
function Get-LaCapitaineSvg([string]$path) {
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
        [IO.File]::WriteAllText($dest, (Get-LaCapitaineSvg $repoPath))
        Write-Host "copied $name ($repoPath)"
    }
    catch {
        Write-Error "Failed to copy $repoPath : $_"
    }
}

& dotnet run --project (Join-Path $repoRoot "tools/IconTool") -- $srcDir $outDir 48
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "La Capitaine PNG icons written to src/Arcana.App/Assets/LaCapitaine"
