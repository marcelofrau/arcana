# Build Arcana in Debug mode
$dotnet = if (Get-Command dotnet -ErrorAction SilentlyContinue) { "dotnet" } else { "C:\Program Files\dotnet\dotnet.exe" }
& $dotnet build "$PSScriptRoot/../src/Arcana.slnx" -c Debug
