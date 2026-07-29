# Run Arcana.App in development mode
$dotnet = if (Get-Command dotnet -ErrorAction SilentlyContinue) { "dotnet" } else { "C:\Program Files\dotnet\dotnet.exe" }
& $dotnet run --project "$PSScriptRoot/../src/Arcana.App"
