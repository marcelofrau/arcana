# Run the Arcana test suite (all projects).
$dotnet = if (Get-Command dotnet -ErrorAction SilentlyContinue) { "dotnet" } else { "C:\Program Files\dotnet\dotnet.exe" }
$repoRoot = Resolve-Path "$PSScriptRoot"

& $dotnet test "$repoRoot/src/Arcana.slnx" -v q --nologo
exit $LASTEXITCODE
