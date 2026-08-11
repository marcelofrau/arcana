# Copy Numix SVG icons from the local clone (D:\workspace\numix-icon-theme, GPL-3.0)
# and render them to PNGs used by the built-in "Numix" (white strokes for dark UIs)
# and "Numix-Light" (dark strokes for light UIs) icon variants. The PNGs are
# committed to src/Arcana.App/Assets/Numix and Assets/Numix-Light so the app needs
# no SVG rasterizer at runtime.
#
# Usage:  pwsh build/update-numix-icons.ps1
# Source: D:\workspace\numix-icon-theme (branch `master`)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$numixRoot = "D:\workspace\numix-icon-theme"

if (-not (Test-Path (Join-Path $numixRoot "Numix/24/actions/document-new.svg"))) {
    throw "Numix clone not found at $numixRoot (expected Numix/24/actions). Clone https://github.com/numixproject/numix-icon-theme"
}

# variant -> (source subdir, output asset dir, slot -> svg path inside source)
$variants = @(
    @{
        Name   = "Numix"
        Source = "Numix/24"
        Out    = "src/Arcana.App/Assets/Numix"
        Icons  = [ordered]@{
            "open"      = "places/folder-open.svg"
            "add"       = "actions/document-new.svg"
            "extract"   = "actions/archive-extract.svg"
            "test"      = "actions/dialog-ok.svg"
            "view"      = "actions/view-preview.svg"
            "delete"    = "actions/edit-delete.svg"
            "find"      = "actions/system-search.svg"
            "info"      = "status/dialog-information.svg"
            "save"      = "actions/document-save.svg"
            "settings"  = "actions/configure.svg"
            "help"      = "actions/help-about.svg"
            "sort-up"   = "actions/view-sort-ascending.svg"
            "sort-down" = "actions/view-sort-descending.svg"
        }
    }
    @{
        Name   = "Numix-Light"
        Source = "Numix-Light/scalable"
        Out    = "src/Arcana.App/Assets/Numix-Light"
        Icons  = [ordered]@{
            "open"      = "actions/document-open-symbolic.svg"
            "add"       = "actions/document-new-symbolic.svg"
            "delete"    = "actions/edit-delete-symbolic.svg"
            "find"      = "actions/system-search-symbolic.svg"
            "info"      = "status/dialog-information-symbolic.svg"
            "save"      = "actions/document-save-symbolic.svg"
            "help"      = "actions/help-about-symbolic.svg"
            "sort-up"   = "actions/view-sort-ascending-symbolic.svg"
            "sort-down" = "actions/view-sort-descending-symbolic.svg"
        }
    }
)

# Numix stores many icons as git symlinks (file -> sibling in same dir).
# On Windows checkout they are plain text files holding the target path.
function Get-NumixSvg([string]$baseDir, [string]$path) {
    $seen = @{}
    while ($true) {
        if ($seen.ContainsKey($path)) { throw "Symlink loop at $path" }
        $seen[$path] = $true
        $full = Join-Path $baseDir ($path -replace "/", [IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path $full)) { throw "Missing $path" }
        $text = Get-Content $full -Raw
        if ($text -match "<svg") { return $text }
        $target = $text.Trim()
        $dir = Split-Path $path -Parent
        $path = if ($target -like "*/*") { $target } else { if ($dir) { "$dir/$target" } else { $target } }
    }
}

foreach ($v in $variants) {
    $srcDir = Join-Path $repoRoot "tools/IconTool/src"
    $outDir = Join-Path $repoRoot $v.Out
    if (Test-Path $srcDir) { Remove-Item $srcDir -Recurse -Force }
    New-Item -ItemType Directory -Path $srcDir -Force | Out-Null
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null

    $baseDir = Join-Path $numixRoot $v.Source
    foreach ($name in $v.Icons.Keys) {
        $dest = Join-Path $srcDir "$name.svg"
        try {
            [IO.File]::WriteAllText($dest, (Get-NumixSvg $baseDir $v.Icons[$name]))
            Write-Host "copied $name ($($v.Name): $($v.Icons[$name]))"
        }
        catch {
            Write-Error "Failed to copy $($v.Icons[$name]) : $_"
        }
    }

    & dotnet run --project (Join-Path $repoRoot "tools/IconTool") -- $srcDir $outDir 48
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host ""
    Write-Host "$($v.Name) PNG icons written to $outDir"
}
