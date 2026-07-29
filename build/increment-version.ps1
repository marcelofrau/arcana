# Increment the build counter in build-counter.txt
$counterFile = Resolve-Path "$PSScriptRoot/build-counter.txt"
$counter = [int](Get-Content $counterFile -Raw).Trim()
$counter++
Set-Content -Path $counterFile -Value "$counter" -NoNewline
Write-Host "Build counter incremented to $counter"
