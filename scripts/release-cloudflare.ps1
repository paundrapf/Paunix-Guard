param(
  [string]$Version = "0.1.1",
  [ValidateSet("stable", "beta")]
  [string]$Channel = "stable",
  [string]$Configuration = "Release",
  [string]$BucketName = "paunix-guard-releases",
  [string]$PagesProject = "paunix-guard",
  [string]$PublicSiteUrl = "https://paunix-guard.pages.dev",
  [switch]$SkipDesktopBuild,
  [switch]$SkipPackage,
  [switch]$SkipSmokeLaunch,
  [switch]$SkipUpload,
  [switch]$SkipWebsiteDeploy,
  [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$websiteDir = Join-Path $root "websites\paunix-guard"
$releaseDir = Join-Path $root "Releases"
$publishExe = Join-Path $root "publish\PaunixGuard\PaunixGuard.exe"

function Invoke-LoggedCommand {
  param([string]$FilePath, [string[]]$Arguments, [string]$WorkingDirectory = $root)

  $rendered = "$FilePath $($Arguments -join ' ')"
  Write-Host ">> $rendered"
  if ($DryRun) {
    return
  }

  Push-Location $WorkingDirectory
  try {
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
      throw "Command failed with exit code ${LASTEXITCODE}: $rendered"
    }
  }
  finally {
    Pop-Location
  }
}

function Put-R2Object {
  param([string]$Key, [string]$Path)

  if (!$DryRun -and !(Test-Path $Path)) {
    throw "Missing release file: $Path"
  }

  Invoke-LoggedCommand "wrangler" @("r2", "object", "put", "$BucketName/$Key", "--file", $Path)
}

if (!$SkipDesktopBuild) {
  Invoke-LoggedCommand "dotnet" @("build", ".\PaunixGuard.sln", "-c", $Configuration, "-p:Platform=x64")
  Invoke-LoggedCommand "dotnet" @("test", ".\PaunixGuard.sln", "-c", $Configuration, "-p:Platform=x64", "--no-build")
}

if (!$SkipPackage) {
  Invoke-LoggedCommand "powershell" @("-ExecutionPolicy", "Bypass", "-File", ".\scripts\package.ps1", "-Version", $Version, "-Channel", $Channel, "-Configuration", $Configuration)
}

if (!$SkipSmokeLaunch -and !$DryRun) {
  if (!(Test-Path $publishExe)) {
    throw "Published smoke-test executable not found: $publishExe"
  }

  $process = Start-Process -FilePath $publishExe -WindowStyle Hidden -PassThru
  Start-Sleep -Seconds 5
  if (!$process.HasExited) {
    Stop-Process -Id $process.Id -Force
  }
}

$setup = Join-Path $releaseDir "PaunixGuard-win-Setup.exe"
$package = Join-Path $releaseDir "PaunixGuard-$Version-full.nupkg"
$releaseIndex = Join-Path $releaseDir "releases.win.json"
$legacyReleaseIndex = Join-Path $releaseDir "RELEASES"
$sha256 = if (Test-Path $setup) { (Get-FileHash -Algorithm SHA256 $setup).Hash.ToLowerInvariant() } else { "" }
$metadataPath = Join-Path ([System.IO.Path]::GetTempPath()) "paunix-guard-latest-$Version.json"
$changelogPath = Join-Path ([System.IO.Path]::GetTempPath()) "paunix-guard-changelog-$Version.json"
$publishedAt = (Get-Date).ToUniversalTime().ToString("o")
$metadata = [ordered]@{
  version = $Version
  channel = $Channel
  installerUrl = "$PublicSiteUrl/download/windows"
  releaseNotesUrl = "$PublicSiteUrl/changelog/$Version"
  publishedAt = $publishedAt
  sha256 = $sha256
}
$changelog = @(
  [ordered]@{
    version = $Version
    channel = $Channel
    title = "Paunix Guard $Version"
    releaseNotesUrl = "$PublicSiteUrl/changelog/$Version"
    publishedAt = $publishedAt
  }
)

if (!$DryRun) {
  $metadata | ConvertTo-Json | Set-Content -Path $metadataPath -Encoding utf8
  $changelog | ConvertTo-Json | Set-Content -Path $changelogPath -Encoding utf8
}

if (!$SkipUpload) {
  Put-R2Object "installers/windows/latest/PaunixGuard-win-Setup.exe" $setup
  Put-R2Object "installers/windows/$Version/PaunixGuard-win-Setup.exe" $setup
  Put-R2Object "updates/windows/PaunixGuard-$Version-full.nupkg" $package
  Put-R2Object "updates/windows/releases.win.json" $releaseIndex
  Put-R2Object "updates/windows/RELEASES" $legacyReleaseIndex
  Put-R2Object "metadata/latest.json" $metadataPath
  Put-R2Object "metadata/changelog.json" $changelogPath
}

Push-Location $websiteDir
try {
  if (Test-Path "package-lock.json") {
    Invoke-LoggedCommand "npm" @("ci") $websiteDir
  }
  else {
    Invoke-LoggedCommand "npm" @("install") $websiteDir
  }

  Invoke-LoggedCommand "npm" @("run", "build") $websiteDir

  if (!$SkipWebsiteDeploy) {
    Invoke-LoggedCommand "wrangler" @("pages", "deploy", ".\dist", "--project-name", $PagesProject) $websiteDir
  }
}
finally {
  Pop-Location
}

if (!$SkipUpload -or !$SkipWebsiteDeploy) {
  Write-Host "Verify:"
  Write-Host "  $PublicSiteUrl/download/windows"
  Write-Host "  $PublicSiteUrl/api/latest"
  Write-Host "  $PublicSiteUrl/updates/windows/RELEASES"
}
