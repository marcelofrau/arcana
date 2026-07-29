param(
    [Parameter(Mandatory)]
    [string]$Version,
    [Parameter(Mandatory)]
    [string]$Arch,
    [switch]$Installer
)

$dotnet = if (Get-Command dotnet -ErrorAction SilentlyContinue) { "dotnet" } else { "C:\Program Files\dotnet\dotnet.exe" }
$outputDir = "$PSScriptRoot/dist/Arcana-v$Version-$Arch"

Write-Host "Building Arcana v$Version for $Arch..."

# Publish self-contained
& $dotnet publish "$PSScriptRoot/../src/Arcana.App/Arcana.App.csproj" `
    -c Release `
    -r $Arch `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:Version=$Version `
    -o "$outputDir/Arcana.App"

& $dotnet publish "$PSScriptRoot/../src/Arcana.Cli/Arcana.Cli.csproj" `
    -c Release `
    -r $Arch `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:Version=$Version `
    -o "$outputDir/Arcana.Cli"

# Package
Compress-Archive -Path "$outputDir/*" -DestinationPath "$outputDir.zip" -Force
Write-Host "Release package: $outputDir.zip"
