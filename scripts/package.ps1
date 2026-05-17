param(
  [string]$Version = "0.1.2",
  [ValidateSet("stable", "beta")]
  [string]$Channel = "stable",
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$publishDir = ".\publish\PaunixGuard"
$releaseDir = ".\Releases"
$velopackChannel = if ($Channel -eq "beta") { "win-beta" } else { "win" }
$iconPath = ".\apps\desktop\PaunixGuard.App\Assets\app.ico"
$splashImagePath = ".\assets\logo-512x512.png"

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
  --packAuthors "Paunix Guard contributors" `
  --packTitle "Paunix Guard" `
  --icon $iconPath `
  --splashImage $splashImagePath `
  --mainExe "PaunixGuard.exe" `
  --channel $velopackChannel `
  --outputDir $releaseDir
