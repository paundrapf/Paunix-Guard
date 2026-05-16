param(
  [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
dotnet build ".\PaunixGuard.sln" -c $Configuration -p:Platform=x64

