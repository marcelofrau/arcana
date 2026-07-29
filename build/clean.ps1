# Clean all build artifacts
$dotnet = if (Get-Command dotnet -ErrorAction SilentlyContinue) { "dotnet" } else { "C:\Program Files\dotnet\dotnet.exe" }

$repoRoot = Resolve-Path "$PSScriptRoot/.."

& $dotnet clean "$repoRoot/src/Arcana.slnx" -c Debug -v q --nologo

@('bin', 'obj', 'dist') | ForEach-Object {
    $dirs = Get-ChildItem -Path $repoRoot -Recurse -Directory -Filter $_ -Force -ErrorAction SilentlyContinue
    if ($dirs) {
        $dirs | Remove-Item -Recurse -Force
        Write-Host "Deleted $($dirs.Count) '$($_)' directories"
    }
}

Write-Host "Clean complete"
