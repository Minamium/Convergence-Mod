# Hidden FNA device rendering the Scarlet Invocation field: real phrases built by the
# production rhythm/choreography, the authoritative hit shapes drawn through the Vfx/
# foundation (ScarletGeometryOverlay), stand-in characters and three backdrops.
# Offline review only; no game, Terraria or server is started. Not a playtest.
#
#   pwsh tools/preview-scarlet.ps1                       # everything, defaults
#   pwsh tools/preview-scarlet.ps1 -Only act1 -Step 4    # one scene family, denser frames
#   pwsh tools/preview-scarlet.ps1 -Beats 128            # constant 128 BPM (28.125 ticks per beat)
#
# TModLoaderPath / LuminancePackage are discovered when omitted: parameter, then
# $env:TML_PATH / $env:LUMINANCE_PACKAGE, Convergence.local.props, then the usual Steam,
# workshop (1281930, 1105840) and Documents/My Games/Terraria/tModLoader/Mods locations.
param(
    [string]$TModLoaderPath,
    [string]$LuminancePackage,
    [string]$OutputDirectory = '.local/scarlet-preview',
    [string]$Only = '',                         # substring of "<scene>-<player>", e.g. act1, final-rift, -edge
    [int]$Step = 8,                             # ticks between sequence frames
    [string]$Beats = 'score',                   # score = Assets/Music/CrimsonFoundry/Score.json, or a BPM such as 128
    [int]$PhraseStart = 1000,                   # score tick the phrase is scheduled from
    [string]$Backgrounds = 'sanctum,night,day', # variants sheet rows
    [string]$Zooms = '0.65,1,2',                # variants sheet columns
    [string]$SequenceBackground = 'night',     # sanctum is ~2 MB per frame; the variants sheets always cover all backgrounds
    [double]$SequenceZoom = 0.65,
    [string]$Size = '1920x1080',
    [string]$Players = 'center,edge',
    [ValidateSet('off', 'on', 'both')][string]$Reduced = 'off',
    [ValidateSet('black', 'dim', 'none')][string]$Mask = 'dim',
    [switch]$NoSequences,
    [switch]$NoMatrix,
    [switch]$NoSmoke,
    [switch]$NoContract,                        # skip the pixel check of the overlay against the authoritative capsules
    [switch]$SheetsOnly                         # write contact sheets only, no per-frame PNGs
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Find-TModLoader {
    if ($TModLoaderPath) { return $TModLoaderPath }
    if ($env:TML_PATH) { return $env:TML_PATH }
    $props = Join-Path $root 'Convergence.local.props'
    if (Test-Path -LiteralPath $props) {
        $match = [regex]::Match((Get-Content -LiteralPath $props -Raw), '<TModLoaderPath>([^<]+)</TModLoaderPath>')
        if ($match.Success -and (Test-Path -LiteralPath $match.Groups[1].Value)) { return $match.Groups[1].Value }
    }
    foreach ($candidate in 'D:/SteamLibrary/steamapps/common/tModLoader', 'C:/Program Files (x86)/Steam/steamapps/common/tModLoader') {
        if (Test-Path -LiteralPath (Join-Path $candidate 'Libraries/FNA/1.0.0/FNA.dll')) { return $candidate }
    }
    throw 'tModLoader not found; pass -TModLoaderPath.'
}

function Find-Luminance([string]$tml) {
    if ($LuminancePackage) { return $LuminancePackage }
    if ($env:LUMINANCE_PACKAGE) { return $env:LUMINANCE_PACKAGE }
    $found = @()
    $documents = [Environment]::GetFolderPath('MyDocuments')
    $loose = Join-Path $documents 'My Games/Terraria/tModLoader/Mods/Luminance.tmod'
    if (Test-Path -LiteralPath $loose) { $found += Get-Item -LiteralPath $loose }
    $steamapps = Split-Path (Split-Path $tml -Parent) -Parent
    foreach ($id in '1281930', '1105840') {
        $workshop = Join-Path $steamapps "workshop/content/$id"
        if (Test-Path -LiteralPath $workshop) { $found += Get-ChildItem -LiteralPath $workshop -Recurse -Filter Luminance.tmod -File -ErrorAction SilentlyContinue }
    }
    if (-not $found) { throw 'Luminance.tmod not found; pass -LuminancePackage.' }
    return ($found | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
}

$tml = (Resolve-Path -LiteralPath (Find-TModLoader)).Path
$lumi = (Resolve-Path -LiteralPath (Find-Luminance $tml)).Path
$work = Join-Path $root '.local/scarlet-preview-gpu'
$out = Join-Path $root $OutputDirectory
New-Item -ItemType Directory -Force -Path $work, $out | Out-Null

# The preview links the production Vfx foundation and the Terraria-independent authority.
$files = @(
    'tools/fixtures/ScarletPreview.cs', 'tools/fixtures/ScarletPreviewAssets.cs',
    'tools/fixtures/ScarletPreviewPlanner.cs', 'tools/fixtures/ScarletPreviewSheet.cs', 'tools/fixtures/ScarletPreviewContract.cs',
    'Common/Raids/Arena/RaidFieldGeometry.cs')
$authority = 'CrimsonTechnique', 'CrimsonTrackingBeam', 'CrimsonChoirRakes', 'CrimsonClusters', 'CrimsonSpatialCuts',
    'CrimsonChoreography', 'CrimsonEnsemble', 'CrimsonInvocation', 'CrimsonRhythm', 'CrimsonPhaseRules',
    'CrimsonCovenantRules', 'CrimsonChorusRules', 'CrimsonChorusImpactPositions', 'CrimsonState',
    'CrimsonRecoveryState', 'CrimsonPlaytestTuning', 'CrimsonScore'
$files += $authority | ForEach-Object { "Content/Encounters/CrimsonFoundry/$_.cs" }
$includes = ($files | ForEach-Object { '<Compile Include="' + [Security.SecurityElement]::Escape((Join-Path $root $_)) + '" />' }) -join ''
$includes += '<Compile Include="' + [Security.SecurityElement]::Escape((Join-Path $root 'Client/Encounters/CrimsonFoundry/Vfx')) + '/*.cs" />'
$fna = [Security.SecurityElement]::Escape((Join-Path $tml 'Libraries/FNA/1.0.0/FNA.dll'))
$project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><Nullable>enable</Nullable><LangVersion>12.0</LangVersion><NoWarn>CS8632</NoWarn></PropertyGroup><ItemGroup><Reference Include="FNA"><HintPath>' + $fna + '</HintPath></Reference>' + $includes + '</ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $work 'ScarletPreview.csproj'), $project)
dotnet build (Join-Path $work 'ScarletPreview.csproj') -c Release --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$options = @('--step', $Step, '--phrase-start', $PhraseStart, '--bg', $Backgrounds, '--zoom', $Zooms,
    '--seq-bg', $SequenceBackground, '--seq-zoom', $SequenceZoom, '--size', $Size, '--players', $Players,
    '--reduced', $Reduced, '--mask', $Mask, '--beats', $(if ($Beats -eq 'score') { 'score' } else { $Beats }))
if ($Only) { $options += @('--only', $Only) }
if ($NoSequences) { $options += '--no-sequences' }
if ($NoMatrix) { $options += '--no-matrix' }
if ($NoSmoke) { $options += '--no-smoke' }
if ($NoContract) { $options += '--no-contract' }
if ($SheetsOnly) { $options += '--sheets-only' }
$env:FNA3D_FORCE_DRIVER = 'D3D11'
dotnet (Join-Path $work 'bin/Release/net8.0/ScarletPreview.dll') $root $lumi (Join-Path $tml 'Libraries/Native/Windows') $out @options
exit $LASTEXITCODE
