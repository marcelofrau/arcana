# Run the Arcana GUI app (Debug build).
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$dotnet = if (Get-Command dotnet -ErrorAction SilentlyContinue) { "dotnet" } else { "C:\Program Files\dotnet\dotnet.exe" }
$repoRoot = Resolve-Path "$PSScriptRoot"

& $dotnet run --project "$repoRoot/src/Arcana.App" -c $Configuration
exit $LASTEXITCODE
