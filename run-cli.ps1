# Run the Arcana CLI (dotnet run --project src/Arcana.Cli -- <args>).
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Args
)

$dotnet = if (Get-Command dotnet -ErrorAction SilentlyContinue) { "dotnet" } else { "C:\Program Files\dotnet\dotnet.exe" }
$repoRoot = Resolve-Path "$PSScriptRoot"

& $dotnet run --project "$repoRoot/src/Arcana.Cli" -c Debug -- $Args
exit $LASTEXITCODE
