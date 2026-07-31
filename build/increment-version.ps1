# Increment the build counter in build-counter.txt.
# Format: <prefix>|<counter> (e.g. 0.1.0|2). Counter resets to 1 when the prefix changes.
$ErrorActionPreference = "Stop"

$counterFile = Resolve-Path "$PSScriptRoot/build-counter.txt"
$propsFile = Resolve-Path "$PSScriptRoot/../src/Directory.Build.props"

$raw = (Get-Content $counterFile -Raw).Trim()
$storedPrefix = $raw.Split('|')[0]
$counter = [int]$raw.Split('|')[1]

$propsContent = Get-Content $propsFile -Raw
$major = [regex]::Match($propsContent, '<VersionMajor>([^<]+)</VersionMajor>')
$minor = [regex]::Match($propsContent, '<VersionMinor>([^<]+)</VersionMinor>')
$patch = [regex]::Match($propsContent, '<VersionPatch>([^<]+)</VersionPatch>')
if (-not ($major.Success -and $minor.Success -and $patch.Success)) {
    throw "Could not parse version components from $propsFile"
}
$prefix = "$($major.Groups[1].Value).$($minor.Groups[1].Value).$($patch.Groups[1].Value)"

if ($prefix -ne $storedPrefix) {
    $counter = 0
    Write-Host "Version prefix changed: $storedPrefix -> $prefix (counter reset)"
}
$counter++

Set-Content -Path $counterFile -Value "$prefix|$counter" -NoNewline
Write-Host "Build counter incremented to $counter ($prefix-build.$counter)"
