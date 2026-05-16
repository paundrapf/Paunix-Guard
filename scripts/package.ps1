param(
  [string]$Version = "0.1.0",
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$publishDir = ".\publish\PaunixGuard"
$releaseDir = ".\Releases"

dotnet publish ".\apps\desktop\PaunixGuard.App\PaunixGuard.App.csproj" `
  -c $Configuration `
  -r win-x64 `
  --self-contained true `
  -o $publishDir `
  -p:Platform=x64

vpk pack `
  --packId "PaunixGuard" `
  --packVersion $Version `
  --packDir $publishDir `
  --mainExe "PaunixGuard.exe" `
  --outputDir $releaseDir

