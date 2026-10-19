param([Parameter(Mandatory = $true)][string]$CliPath)
$ErrorActionPreference = 'Stop'
$CliPath = (Resolve-Path $CliPath).Path
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('arcana-smoke-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $temporary | Out-Null
function Invoke-Arcana([string[]]$Arguments) {
    & $CliPath @Arguments --no-color --log-level error
    if ($LASTEXITCODE -ne 0) { throw "Arcana command failed with exit code $LASTEXITCODE" }
}
try {
    $source = Join-Path $temporary 'sample [one].txt'
    [IO.File]::WriteAllText($source, 'Arcana package verification')
    $zip = Join-Path $temporary 'sample.zip'
    $output = Join-Path $temporary 'out'
    Invoke-Arcana -Arguments @('compress', $source, '-o', $zip, '-p', 'smoke-test-only')
    Invoke-Arcana -Arguments @('extract', $zip, $output, '-p', 'smoke-test-only')
    if ([IO.File]::ReadAllText((Join-Path $output 'sample [one].txt')) -ne [IO.File]::ReadAllText($source)) {
        throw 'Extracted bytes differ from the source.'
    }
    $json = & $CliPath list $zip --json -p smoke-test-only --log-level error
    if ($LASTEXITCODE -ne 0) { throw 'JSON listing failed.' }
    $entries = $json | ConvertFrom-Json
    if ($entries.Count -ne 1 -or $entries[0].Path -ne 'sample [one].txt') { throw 'Unexpected JSON listing.' }
    Write-Host 'Package smoke test passed: encrypted ZIP, extraction and JSON.'
}
finally { Remove-Item $temporary -Recurse -Force }
