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

foreach ($name in $packageVersions.Keys | Where-Object { $_ -match '^Aspire(?:\.|$)' }) {
  Add-Pin -Path "Directory.Packages.props" -Name $name -Version $packageVersions[$name]
}

foreach ($workflow in @("_run-tests", "pr-validation", "publish")) {
  Add-RegexPin -Path ".github/workflows/$workflow.yml" -Name "Aspire.Cli" -Pattern 'dotnet tool install -g Aspire\.Cli --version (?<version>\S+)'
}

foreach ($directory in @("samples/TypeScriptAppHost", "tests/Aspire.Hosting.Bunny.Storage.Tests/Fixtures/TypeScriptAppHost")) {
  $path = "$directory/aspire.config.json"
  $config = Get-Content -Raw -Path (Join-Path $repoRoot $path) | ConvertFrom-Json -AsHashtable
  Add-Pin -Path $path -Name "Aspire CLI" -Version $config.sdk.version
  foreach ($name in $config.packages.Keys | Where-Object { $_ -match '^Aspire(?:\.|$)' }) {
    Add-Pin -Path $path -Name $name -Version $config.packages[$name]
  }
}

$driftedPins = $pins | Where-Object { $_.Version -ne $baseline }

if ($driftedPins) {
  $details = $driftedPins |
    ForEach-Object { " - $($_.Path) [$($_.Name)] is $($_.Version)" }
  $detailsText = $details -join "`n"

  throw "Aspire version pin drift detected. Expected every pin to match Directory.Packages.props Aspire.Hosting version '$baseline'.`n$detailsText"
}

Write-Host "All Aspire version pins match $baseline."
