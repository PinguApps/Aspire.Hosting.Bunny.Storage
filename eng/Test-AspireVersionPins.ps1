$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$pins = @()

function Add-Pin {
  param(
    [string] $Path,
    [string] $Name,
    [string] $Version
  )

  if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Missing Aspire version pin '$Name' in '$Path'."
  }

  $script:pins += [pscustomobject]@{
    Path = $Path
    Name = $Name
    Version = $Version
  }
}

function Add-RegexPin {
  param(
    [string] $Path,
    [string] $Name,
    [string] $Pattern
  )

  $fullPath = Join-Path $repoRoot $Path
  $content = Get-Content -Raw -Path $fullPath
  $matches = [regex]::Matches($content, $Pattern)

  if ($matches.Count -eq 0) {
    throw "Missing Aspire version pin '$Name' in '$Path'."
  }

  foreach ($match in $matches) {
    Add-Pin -Path $Path -Name $Name -Version $match.Groups["version"].Value
  }
}

$propsPath = Join-Path $repoRoot "Directory.Packages.props"
[xml] $props = Get-Content -Raw -Path $propsPath

$packageVersions = @{}
foreach ($packageVersion in $props.Project.ItemGroup.PackageVersion) {
  $packageVersions[$packageVersion.Include] = $packageVersion.Version
}

$baseline = $packageVersions["Aspire.Hosting"]
if ([string]::IsNullOrWhiteSpace($baseline)) {
  throw "Missing Aspire.Hosting baseline in Directory.Packages.props."
}

Add-Pin -Path "Directory.Packages.props" -Name "Aspire.Hosting.Azure.Storage" -Version $packageVersions["Aspire.Hosting.Azure.Storage"]
Add-RegexPin -Path "samples/BunnyStorageManual/AppHost/BunnyStorageManual.AppHost.csproj" -Name "Aspire.AppHost.Sdk" -Pattern '<Project Sdk="Aspire\.AppHost\.Sdk/(?<version>[^"]+)"'

$driftedPins = $pins | Where-Object { $_.Version -ne $baseline }

if ($driftedPins) {
  $details = $driftedPins |
    ForEach-Object { " - $($_.Path) [$($_.Name)] is $($_.Version)" }
  $detailsText = $details -join "`n"

  throw "Aspire version pin drift detected. Expected every pin to match Directory.Packages.props Aspire.Hosting version '$baseline'.`n$detailsText"
}

Write-Host "All Aspire version pins match $baseline."
