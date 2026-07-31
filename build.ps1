# Build the Arcana solution.
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$dotnet = if (Get-Command dotnet -ErrorAction SilentlyContinue) { "dotnet" } else { "C:\Program Files\dotnet\dotnet.exe" }
$repoRoot = Resolve-Path "$PSScriptRoot"

& $dotnet build "$repoRoot/src/Arcana.slnx" -c $Configuration -v q --nologo
exit $LASTEXITCODE
