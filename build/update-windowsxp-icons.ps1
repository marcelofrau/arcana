# Copy selected icons from the "Windows XP High Resolution Icon Pack" (by
# marchmountain, CC0-1.0) and downscale them to the 48px PNGs used by the
# built-in "Windows XP" icon theme. The PNGs are committed to
# src/Arcana.App/Assets/WindowsXP so the app needs no rasterizer at runtime.
#
# Usage:  pwsh build/update-windowsxp-icons.ps1
# Source: C:\Users\fraumar\Downloads\Windows.XP.High.Resolution.Icon.Pack.2.zip
#         (DeviantArt: https://www.deviantart.com/marchmountain/art/Windows-XP-High-Resolution-Icon-Pack-916042853)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$zip = "C:\Users\fraumar\Downloads\Windows.XP.High.Resolution.Icon.Pack.2.zip"
$outDir = Join-Path $repoRoot "src/Arcana.App/Assets/WindowsXP"
$size = 48

if (-not (Test-Path $zip)) {
    throw "Icon pack zip not found at $zip"
}

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO.Compression.FileSystem

# slot -> icon name inside the pack's "Windows XP Icons" folder.
$map = [ordered]@{
    "open"        = "Folder Opened"
    "add"         = "Add"
    "extract"     = "Copy To"
    "test"        = "Success"
    "view"        = "Explorer"
    "delete"      = "Delete"
    "find"        = "Search"
    "info"        = "Information"
    "save"        = "Save"
    "settings"    = "Control Panel"
    "help"        = "Help and Support"
    "close"       = "Exit"
    "split"       = "Cut"
    "convert"     = "WME Convert a File"
    "password"    = "Lock"
    "rename"      = "Rename"
    "select-all"  = "Checklist"
    "favorite"    = "Favorites"
    "hash"        = "Key"
    "optimize"    = "Disk Defragmenter"
    "check"       = "Security - Ok"
    "arrow-back"  = "Back"
    "arrow-forward" = "Forward"
    "copy"        = "Copy"
    "refresh"     = "IE Refresh"
    "play"        = "Play"
}

New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$arc = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    foreach ($slot in $map.Keys) {
        $name = $map[$slot]
        $entry = $arc.Entries |
            Where-Object { $_.FullName -eq "Windows XP High Resolution Icon Pack/Windows XP Icons/$name.png" } |
            Select-Object -First 1
        if (-not $entry) {
            Write-Warning "missing source icon: $name (slot $slot)"
            continue
        }

        $src = $null
        try {
            $stream = $entry.Open()
            $src = [System.Drawing.Bitmap]::FromStream($stream)
        }
        finally {
            if ($stream) { $stream.Dispose() }
        }

        $dst = New-Object System.Drawing.Bitmap $size, $size
        $g = [System.Drawing.Graphics]::FromImage($dst)
        try {
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $g.Clear([System.Drawing.Color]::Transparent)
            $g.DrawImage($src, 0, 0, $size, $size)
        }
        finally {
            $g.Dispose()
        }
        $dst.Save((Join-Path $outDir "$slot.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $dst.Dispose()
        $src.Dispose()
        Write-Host "ok  $slot.png <- $name"
    }
}
finally {
    $arc.Dispose()
}

Write-Host ""
Write-Host "Windows XP PNG icons written to $outDir"
