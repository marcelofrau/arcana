# Download Papirus 24x24 SVGs (Papirus icon theme, GPL-3.0) and render them to
# PNG icons used by the built-in "Papirus" icon theme. The PNGs are committed to
# src/Arcana.App/Assets/Papirus so the app needs no SVG rasterizer at runtime.
#
# Usage:  pwsh build/update-papirus-icons.ps1
# Source: https://github.com/PapirusDevelopmentTeam/papirus-icon-theme (24x24)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$apiBase = "https://api.github.com/repos/PapirusDevelopmentTeam/papirus-icon-theme"

$icons = [ordered]@{
    "open"         = "places/folder-open.svg"
    "add"          = "actions/document-new.svg"
    "extract"      = "actions/archive-extract.svg"
    "test"         = "actions/dialog-ok.svg"
    "view"         = "actions/view-preview.svg"
    "delete"       = "actions/edit-delete.svg"
    "find"         = "actions/system-search.svg"
    "info"         = "status/dialog-information.svg"
    "folder"       = "places/folder.svg"
    "file-generic" = "mimetypes/text-x-generic.svg"
    "file-archive" = "mimetypes/application-x-archive.svg"
    "file-image"   = "mimetypes/image-x-generic.svg"
    "file-code"    = "mimetypes/text-x-script.svg"
    "file-media"   = "mimetypes/video-x-generic.svg"
    "file-doc"     = "mimetypes/x-office-document.svg"
    "file-rar"     = "mimetypes/application-x-rar.svg"
    "sort-up"      = "actions/view-sort-ascending.svg"
    "sort-down"    = "actions/view-sort-descending.svg"
}

$srcDir = Join-Path $repoRoot "tools/IconTool/src"
$outDir = Join-Path $repoRoot "src/Arcana.App/Assets/Papirus"
New-Item -ItemType Directory -Path $srcDir -Force | Out-Null
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

# Papirus stores some icons as git symlinks (file -> target in same dir).
# The contents API follows symlinks for regular files but raw.githubusercontent
# serves the literal link text, so we download through the API (base64 content).
function Get-PapirusFile([string]$path) {
    $seen = @{}
    while ($true) {
        if ($seen.ContainsKey($path)) { throw "Symlink loop at $path" }
        $seen[$path] = $true
        $entry = Invoke-RestMethod -Uri "$apiBase/contents/Papirus/24x24/$path" -Headers @{ "User-Agent" = "arcana" }
        if ($entry.type -eq "symlink") {
            $target = [System.Text.Encoding]::UTF8.GetString(
                [System.Convert]::FromBase64String($entry.content)).Trim()
            $dir = Split-Path $path -Parent
            $path = if ($target -like "*/*") { $target } else { if ($dir) { "$dir/$target" } else { $target } }
            continue
        }
        return [System.Convert]::FromBase64String($entry.content)
    }
}

foreach ($name in $icons.Keys) {
    $repoPath = $icons[$name]
    $dest = Join-Path $srcDir "$name.svg"
    try {
        [IO.File]::WriteAllBytes($dest, (Get-PapirusFile $repoPath))
        Write-Host "downloaded $name ($repoPath)"
    }
    catch {
        Write-Error "Failed to download $repoPath : $_"
    }
}

& dotnet run --project (Join-Path $repoRoot "tools/IconTool") -- $srcDir $outDir 48
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Papirus PNG icons written to src/Arcana.App/Assets/Papirus"
