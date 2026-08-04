<#
    Builds a release archive laid out the way Nexus and Vortex expect:

        BepInEx/plugins/PlantPeek/PlantPeek.dll

    Deliberately not the dev deploy path (plugins/MoonlightPeaksMods/PlantPeek), which only
    exists to keep hand-built DLLs clear of Vortex during development.

    There is no test project to run: unlike Chest Labels, this mod has no persistence layer or
    parser to test - every code path reads Unity and game types, so a console runner could not
    exercise anything meaningful. Verification is in TESTING.md instead.
#>

$ErrorActionPreference = 'Stop'

$modRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

# The mod lives at mods/PlantPeek in the notes repo and at the root of its own standalone
# repo. Detect which, so one script works in both and the two copies never diverge.
$parent   = Split-Path -Parent $modRoot
$repoRoot = if ((Split-Path -Leaf $parent) -eq 'mods') { Split-Path -Parent $parent } else { $modRoot }

$project = Join-Path $modRoot 'src\PlantPeek\PlantPeek.csproj'

# Single source of truth for the version, so the archive can never disagree with the DLL.
$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Where-Object { $_ }
if (-not $version) { throw "Could not read <Version> from $project" }

# The version is reported to players twice; a mismatch means an archive that lies about what
# is inside it.
$pluginSource = Get-Content (Join-Path $modRoot 'src\PlantPeek\Plugin.cs') -Raw
if ($pluginSource -notmatch 'PluginVersion\s*=\s*"([^"]+)"') { throw 'Could not read PluginVersion from Plugin.cs' }
$pluginVersion = $Matches[1]
if ($pluginVersion -ne $version) {
    throw "Version mismatch: csproj says $version, Plugin.cs says $pluginVersion"
}

Write-Host "Packing Plant Peek $version"

# SkipDeploy keeps a release build from overwriting the copy under test in the game folder.
dotnet build $project -c Release -p:SkipDeploy=true
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

$dll = Join-Path $modRoot 'src\PlantPeek\bin\Release\netstandard2.1\PlantPeek.dll'
if (-not (Test-Path $dll)) { throw "Built DLL not found at $dll" }

$staging = Join-Path $env:TEMP "PlantPeek-pack-$([guid]::NewGuid().ToString('N'))"
$target  = Join-Path $staging 'BepInEx\plugins\PlantPeek'
New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item $dll $target

$dist = Join-Path $repoRoot 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null

$archive = Join-Path $dist "PlantPeek-$version.zip"
if (Test-Path $archive) { Remove-Item $archive }

Compress-Archive -Path (Join-Path $staging 'BepInEx') -DestinationPath $archive
Remove-Item $staging -Recurse -Force

Write-Host "Created $archive"
Write-Host 'Extract it over the game folder to install.'
