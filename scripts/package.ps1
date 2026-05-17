param(
  [string]$Version = "0.1.1",
  [ValidateSet("stable", "beta")]
  [string]$Channel = "stable",
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$publishDir = ".\publish\PaunixGuard"
$releaseDir = ".\Releases"
$velopackChannel = if ($Channel -eq "beta") { "win-beta" } else { "win" }

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
  --channel $velopackChannel `
  --outputDir $releaseDir
