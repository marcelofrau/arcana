# Copy color icons from the local icons8 personal set
# (C:\Users\fraumar\workspace\_non_work_\icons8-personal-set, MIT) and commit
# them under src/Arcana.App/Assets/Arcana under the built-in slot names used by
# PngIconThemeProvider. The 50x50 render is closest to the other themes' 48px.
#
# Usage:  pwsh build/update-arcana-icons.ps1
# Source: C:\Users\fraumar\workspace\_non_work_\icons8-personal-set (50x50)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$icons8Root = "C:\Users\fraumar\workspace\_non_work_\icons8-personal-set"
$srcDir = Join-Path $icons8Root "50x50"

if (-not (Test-Path (Join-Path $srcDir "icons8-opened-folder-50.png"))) {
    throw "icons8 personal set not found at $srcDir"
}

# slot -> icons8 source file (50x50)
$icons = [ordered]@{
    "open"          = "icons8-opened-folder-50.png"
    "add"           = "icons8-add-50.png"
    "extract"       = "icons8-archive-50.png"
    "test"          = "icons8-test-passed-50.png"
    "view"          = "icons8-view-50.png"
    "delete"        = "icons8-trash-50.png"
    "find"          = "icons8-search-50.png"
    "info"          = "icons8-info-50.png"
    "save"          = "icons8-save-50.png"
    "settings"      = "icons8-gear-50.png"
    "help"          = "icons8-help-50.png"
    "sort-up"       = "icons8-arrow-up-50.png"
    "sort-down"     = "icons8-arrow-down-50.png"
    "close"         = "icons8-close-50.png"
    "split"         = "icons8-split-2d-50.png"
    "convert"       = "icons8-synchronize-50.png"
    "password"      = "icons8-password-key-50.png"
    "rename"        = "icons8-rename-2d-50.png"
    "select-all"    = "icons8-select-all-2d-50.png"
    "favorite"      = "icons8-favorite-50.png"
    "compare"       = "icons8-data-transfer-50.png"
    "join"          = "fluentui-puzzle-piece-50.png"
    "hash"          = "fluentui-keycap-hashtag-50.png"
    "optimize"      = "fluentui-magic-wand-50.png"
    "check"         = "icons8-check-mark-50.png"
    "arrow-back"    = "icons8-arrow-left-50.png"
    "arrow-forward" = "icons8-arrow-right-50.png"
    "copy"          = "icons8-copy-2d-50.png"
    "refresh"       = "icons8-refresh-50.png"
    "play"          = "icons8-play-50.png"
}

$outDir = Join-Path $repoRoot "src/Arcana.App/Assets/Arcana"
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

foreach ($slot in $icons.Keys) {
    $source = Join-Path $srcDir $icons[$slot]
    if (-not (Test-Path $source)) {
        throw "Missing source for $slot : $($icons[$slot])"
    }
    Copy-Item -LiteralPath $source -Destination (Join-Path $outDir "$slot.png") -Force
    Write-Host "copied $slot <- $($icons[$slot])"
}

Write-Host ""
Write-Host "Arcana PNG icons written to $outDir"
