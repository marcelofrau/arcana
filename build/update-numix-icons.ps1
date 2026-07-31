# Copy Numix 24x24 SVGs from the local clone (F:\workspace\numix-icon-theme,
# GPL-3.0) and render them to PNG icons used by the built-in "Numix" icon
# theme. The PNGs are committed to src/Arcana.App/Assets/Numix so the app
# needs no SVG rasterizer at runtime.
#
# Usage:  pwsh build/update-numix-icons.ps1
# Source: F:\workspace\numix-icon-theme (branch `master`, `Numix/24`)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$numix = "F:\workspace\numix-icon-theme"

if (-not (Test-Path (Join-Path $numix "Numix/24/actions/document-new.svg"))) {
    throw "Numix clone not found at $numix (expected Numix/24/actions). Clone https://github.com/numixproject/numix-icon-theme"
}

# slot name -> path inside Numix/24
$icons = [ordered]@{
    "open"         = "places/folder-open.svg"
    "add"          = "actions/document-new.svg"
    "extract"      = "actions/archive-extract.svg"
    "test"         = "actions/dialog-ok.svg"
    "view"         = "actions/view-preview.svg"
    "delete"       = "actions/edit-delete.svg"
    "find"         = "actions/system-search.svg"
    "info"         = "status/dialog-information.svg"
    "save"         = "actions/document-save.svg"
    "settings"     = "actions/configure.svg"
    "help"         = "actions/help-about.svg"
    "sort-up"      = "actions/view-sort-ascending.svg"
    "sort-down"    = "actions/view-sort-descending.svg"
}

$srcDir = Join-Path $repoRoot "tools/IconTool/src"
$outDir = Join-Path $repoRoot "src/Arcana.App/Assets/Numix"
if (Test-Path $srcDir) { Remove-Item $srcDir -Recurse -Force }
New-Item -ItemType Directory -Path $srcDir -Force | Out-Null
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

# Numix stores many icons as git symlinks (file -> sibling in same dir).
# On Windows checkout they are plain text files holding the target path.
function Get-NumixSvg([string]$path) {
    $base = Join-Path $numix "Numix/24"
    $seen = @{}
    while ($true) {
        if ($seen.ContainsKey($path)) { throw "Symlink loop at $path" }
        $seen[$path] = $true
        $full = Join-Path $base ($path -replace "/", [IO.Path]::DirectorySeparatorChar)
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
        [IO.File]::WriteAllText($dest, (Get-NumixSvg $repoPath))
        Write-Host "copied $name ($repoPath)"
    }
    catch {
        Write-Error "Failed to copy $repoPath : $_"
    }
}

& dotnet run --project (Join-Path $repoRoot "tools/IconTool") -- $srcDir $outDir 48
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Numix PNG icons written to src/Arcana.App/Assets/Numix"
