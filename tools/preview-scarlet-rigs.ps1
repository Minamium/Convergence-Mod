# Offline harness for the Scarlet Invocation attack expression: the REAL Vespera and apparition rigs, the real
# forecasts, seals and ScarletInk strikes over the real backdrop shader, composited in the in-game layer order
# (background -> NPCs -> PostDrawTiles -> players -> participant mask) and filmed through the in-game cameras at
# real size. Writes silent webm (or PNG frames), stills, per-tick state.json, the arranged BGM window and gates.json.
# tools/encode_scarlet_preview.py adds sound set A / the BGM and the review page. Hidden FNA D3D11 device; no game,
# Terraria or server is started. Not a playtest: in-game acceptance stays not_run.
#
#   pwsh tools/preview-scarlet-rigs.ps1                                         # Act I-III signature, camera A2, gates
#   pwsh tools/preview-scarlet-rigs.ps1 -Acts 1,2,3 -Phrases signature,basic -Cameras A,A2,C,V -Reduced both
#   pwsh tools/preview-scarlet-rigs.ps1 -Scenes act1-signature -Cameras C -Frames png -Limit 30 -Gates off
#   pwsh tools/preview-scarlet-rigs.ps1 -Gates only                             # gates.json only
#
# Production files are LINKED unchanged (see $client below). Vespera's DrawPerformer cannot be linked while it sits
# in the Terraria-bound CrimsonRig.cs, so it is extracted verbatim at build time (with its sha256 and line range) from
# CrimsonRig.Performer.cs when that file exists, else from CrimsonRig.cs. TModLoaderPath / LuminancePackage / Ffmpeg
# are discovered when omitted (like tools/preview-scarlet.ps1; ffmpeg from imageio_ffmpeg under py -3.12).
param(
    [string]$TModLoaderPath,
    [string]$LuminancePackage,
    [string]$Ffmpeg,
    [Alias('Out')][string]$OutputDirectory = '.local/scarlet-rigs',
    [string[]]$Scenes = @(),                          # act1-signature, act2-basic, act1-signature-trio, ...
    [string[]]$Acts = @('1', '2', '3'),                # also '1,2,3' as one string (pwsh -File)
    [string[]]$Phrases = @('signature'),               # signature, basic
    [switch]$Trio,                                     # add act1-signature-trio (columns 1, 5, 8)
    [string[]]$Cameras = @('A2'),                      # A (ground), A2 (air), C (apparition x2), V (Vespera x2), B (review x0.65)
    [ValidateSet('off', 'on', 'both')][string]$Reduced = 'off',
    [ValidateSet('off', 'on', 'both')][string]$Material = 'off',
    [ValidateSet('current', 'proposed')][string]$Motion = 'current',
    [ValidateSet('on', 'off', 'both')][string]$Yield = 'on',
    [ValidateSet('stream', 'png', 'none')][string]$Frames = 'stream',
    [ValidateSet('auto', 'all', 'off')][string]$Stills = 'auto',
    [ValidateSet('on', 'off', 'only')][string]$Gates = 'on',
    [ValidateSet('black', 'dim', 'none')][string]$Mask = 'black',
    [ValidateSet('auto', 'on', 'off')][string]$Flip = 'auto',
    [string]$Baseline = '.local/scarlet-rigs-baseline', # G8 / G11 reference frames: compared when present, written when absent
    [string]$Size = '1920x1080',
    [int]$Limit = 0,                                   # frames per scene (0 = the whole phrase window)
    [string]$From = '',                                # first frame relative to the phrase's first warning (default -50)
    [switch]$NoLabels,
    [switch]$NoContext,                                # leave out the neighbouring phrases
    [switch]$NoAudio
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

function Find-Ffmpeg {
    if ($Ffmpeg) { return $Ffmpeg }
    if ($env:FFMPEG -and (Test-Path -LiteralPath $env:FFMPEG)) { return $env:FFMPEG }
    try {
        $path = & py -3.12 -c 'import imageio_ffmpeg; print(imageio_ffmpeg.get_ffmpeg_exe())' 2>$null
        if ($LASTEXITCODE -eq 0 -and $path -and (Test-Path -LiteralPath $path.Trim())) { return $path.Trim() }
    } catch { }
    $command = Get-Command ffmpeg -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    return ''
}

function Get-Sha256([string]$text) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text))) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

$tml = (Resolve-Path -LiteralPath (Find-TModLoader)).Path
$lumi = (Resolve-Path -LiteralPath (Find-Luminance $tml)).Path
$work = Join-Path $root '.local/scarlet-rigs-gpu'
$out = if ([IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $root $OutputDirectory }
New-Item -ItemType Directory -Force -Path $work, $out | Out-Null

# ---- Vespera's performer, extracted verbatim -----------------------------------------------------------------------
$candidates = 'Client/Encounters/CrimsonFoundry/CrimsonRig.Performer.cs', 'Client/Encounters/CrimsonFoundry/CrimsonRig.cs'
$source = $null; $text = $null
foreach ($candidate in $candidates) {
    $path = Join-Path $root $candidate
    if (-not (Test-Path -LiteralPath $path)) { continue }
    $content = [IO.File]::ReadAllText($path) -replace "`r`n", "`n"
    if ($content.Contains('internal static void DrawPerformer(')) { $source = $candidate; $text = $content; break }
}
if (-not $source) { throw 'DrawPerformer not found in ' + ($candidates -join ' or ') }
$signature = $text.IndexOf('internal static void DrawPerformer(')
$lineStart = $text.LastIndexOf("`n", $signature) + 1
$open = $text.IndexOf('{', $signature)
$depth = 0; $close = -1
for ($i = $open; $i -lt $text.Length; $i++) {
    $c = $text[$i]
    if ($c -eq '{') { $depth++ }
    elseif ($c -eq '}') { $depth--; if ($depth -eq 0) { $close = $i; break } }
}
if ($close -lt 0) { throw 'DrawPerformer has unbalanced braces in ' + $source }
$method = $text.Substring($lineStart, $close - $lineStart + 1)
$firstLine = ($text.Substring(0, $lineStart) -split "`n").Count
$lastLine = $firstLine + ($method -split "`n").Count - 1
$lines = $text -split "`n"
$poses = $lines | Where-Object { $_ -match 'static readonly Rectangle\[\] poses\b' } | Select-Object -First 1
$pivots = $lines | Where-Object { $_ -match 'static readonly Vector2\[\] pivots\b' } | Select-Object -First 1
if (-not $poses -or -not $pivots) { throw 'poses / pivots not found in ' + $source }
$header = $text.Substring(0, $text.IndexOf("`nnamespace ") + 1)
$hash = Get-Sha256 ($poses + "`n" + $pivots + "`n" + $method)
$generated = "// GENERATED by tools/preview-scarlet-rigs.ps1 from $source lines ${firstLine}-${lastLine} (sha256 $hash). Verbatim; do not edit.`n" +
    $header + "namespace Convergence.Client.Encounters.CrimsonFoundry;`n`n" +
    "internal static class CrimsonRigPerformerExtract`n{`n    internal static Texture2D? performer;`n" + $poses + "`n" + $pivots + "`n" + $method + "`n}`n`n" +
    "internal static class RigProvenance`n{`n    internal static readonly object Performer = new { source = `"$source`", lines = `"${firstLine}-${lastLine}`", sha256 = `"$hash`" };`n}`n"
[IO.File]::WriteAllText((Join-Path $work 'CrimsonRigPerformerExtract.g.cs'), $generated, (New-Object Text.UTF8Encoding $false))

# ---- project ----------------------------------------------------------------------------------------------------
$files = @(
    'tools/fixtures/ScarletRigScene.cs', 'tools/fixtures/ScarletRigGates.cs', 'tools/fixtures/ScarletPreviewHost.cs',
    'tools/fixtures/ScarletPreviewAssets.cs', 'tools/fixtures/ScarletPreviewPlanner.cs', 'tools/fixtures/ScarletPreviewContract.cs',
    'tools/fixtures/ScarletPreviewSheet.cs', 'Common/Raids/Arena/RaidFieldGeometry.cs', 'Client/Graphics/WorldGraphicsScope.cs')
# The production presentation linked unchanged: rigs, forecast/orb energy, seals, the shared motion clock, the music mixer.
$client = 'ScarletApparitionRig', 'CrimsonChoirRig', 'CrimsonChoirMotion', 'CrimsonEnergy', 'ScarletSorcery', 'CrimsonRigMotion', 'CrimsonMusicMixer'
$files += $client | ForEach-Object { "Client/Encounters/CrimsonFoundry/$_.cs" }
$authority = 'CrimsonTechnique', 'CrimsonTrackingBeam', 'CrimsonChoirRakes', 'CrimsonClusters', 'CrimsonSpatialCuts',
    'CrimsonChoreography', 'CrimsonEnsemble', 'CrimsonInvocation', 'CrimsonRhythm', 'CrimsonPhaseRules',
    'CrimsonCovenantRules', 'CrimsonChorusRules', 'CrimsonChorusImpactPositions', 'CrimsonState',
    'CrimsonRecoveryState', 'CrimsonPlaytestTuning', 'CrimsonMeter', 'CrimsonSignatureMoves'
$files += $authority | ForEach-Object { "Content/Encounters/CrimsonFoundry/$_.cs" }
foreach ($file in $files) { if (-not (Test-Path -LiteralPath (Join-Path $root $file))) { throw "missing $file" } }
$includes = ($files | ForEach-Object { '<Compile Include="' + [Security.SecurityElement]::Escape((Join-Path $root $_)) + '" />' }) -join ''
$includes += '<Compile Include="' + [Security.SecurityElement]::Escape((Join-Path $root 'Client/Encounters/CrimsonFoundry/Vfx')) + '/*.cs" />'
$includes += '<Compile Include="CrimsonRigPerformerExtract.g.cs" />'
$fna = [Security.SecurityElement]::Escape((Join-Path $tml 'Libraries/FNA/1.0.0/FNA.dll'))
$vorbis = Get-ChildItem -LiteralPath (Join-Path $tml 'Libraries') -Recurse -Filter NVorbis.dll -File | Select-Object -First 1
if (-not $vorbis) { throw 'NVorbis.dll not found under the tModLoader Libraries' }
$references = '<Reference Include="FNA"><HintPath>' + $fna + '</HintPath></Reference>' +
    '<Reference Include="NVorbis"><HintPath>' + [Security.SecurityElement]::Escape($vorbis.FullName) + '</HintPath></Reference>'
$project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType>' +
    '<AssemblyName>ScarletRigs</AssemblyName><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost>' +
    '<Nullable>enable</Nullable><LangVersion>12.0</LangVersion><AllowUnsafeBlocks>true</AllowUnsafeBlocks>' +
    '<NoWarn>CS8632;CS8618;CS8625;CS8600;CS8602;CS8603;CS8604;CS0168;CS0219</NoWarn></PropertyGroup><ItemGroup>' +
    $references + $includes + '</ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $work 'ScarletRigs.csproj'), $project)
dotnet build (Join-Path $work 'ScarletRigs.csproj') -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# ---- run --------------------------------------------------------------------------------------------------------
if ($Scenes.Count -eq 0) {
    $actList = ($Acts -join ',') -split ',' | Where-Object { $_ }
    $phraseList = ($Phrases -join ',') -split ',' | Where-Object { $_ }
    $Scenes = foreach ($act in $actList) { foreach ($phrase in $phraseList) { "act$act-$phrase" } }
    if ($Trio) { $Scenes += 'act1-signature-trio' }
}
$ff = ''
if ($Frames -eq 'stream' -and $Gates -ne 'only') {
    $ff = Find-Ffmpeg
    if (-not $ff) { throw 'ffmpeg not found (py -3.12 -m pip install imageio-ffmpeg, or pass -Ffmpeg); or use -Frames png.' }
}
$baselinePath = if ([IO.Path]::IsPathRooted($Baseline)) { $Baseline } else { Join-Path $root $Baseline }
$options = @('--scenes', ($Scenes -join ','), '--cameras', ($Cameras -join ','), '--reduced', $Reduced, '--material', $Material,
    '--motion', $Motion, '--yield', $Yield, '--frames', $Frames, '--stills', $Stills, '--gates', $Gates, '--mask', $Mask,
    '--flip', $Flip, '--baseline', $baselinePath, '--size', $Size, '--limit', $Limit)
if ($ff) { $options += @('--ffmpeg', $ff) }
if ($From) { $options += @('--from', $From) }
if ($NoLabels) { $options += @('--labels', 'off') }
if ($NoContext) { $options += @('--context', 'off') }
if ($NoAudio) { $options += @('--audio', 'off') }
$env:FNA3D_FORCE_DRIVER = 'D3D11'
dotnet (Join-Path $work 'bin/Release/net8.0/ScarletRigs.dll') $root $lumi (Join-Path $tml 'Libraries/Native/Windows') $out @options
exit $LASTEXITCODE
