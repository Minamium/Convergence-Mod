# Capture production part transforms without launching a game or graphics device.
param([string]$RepoRoot = (Join-Path $PSScriptRoot '..'), [string]$Output = '.local/samurai-rig-draws.txt')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $RepoRoot).Path
$files = @(
    'Content/Encounters/GhostSamurai/GhostSamuraiRules.cs',
    'Content/Encounters/GhostSamurai/SamuraiArenaBounds.cs',
    'Content/Encounters/GhostSamurai/SamuraiComboRules.cs',
    'Content/Encounters/GhostSamurai/SamuraiWaveRules.cs',
    'Client/Encounters/GhostSamurai/SamuraiSpriteFrames.cs',
    'Client/Encounters/GhostSamurai/SamuraiRigMotion.cs',
    'Client/Encounters/GhostSamurai/GhostSamuraiRigArt.cs'
)
$source = foreach ($file in $files) {
    $text = [IO.File]::ReadAllText((Join-Path $root $file))
    $match = [regex]::Match($text, '(?s)^(.*?)namespace ([^;]+);(.*)$')
    if (!$match.Success) { throw "Expected file-scoped namespace: $file" }
    "#nullable disable`nnamespace " + $match.Groups[2].Value + " {`n" + $match.Groups[1].Value + $match.Groups[3].Value + "`n}"
}
$fixture = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'fixtures/SamuraiRigVisualFixture.cs'))
Add-Type -TypeDefinition (($source -join "`n") + "`n" + $fixture)
[IO.File]::WriteAllText((Join-Path $root $Output), [SamuraiRigVisualFixture]::Capture())
Write-Output "PASS production rig transforms, source bounds, visible body, finite joints and dedicated-server guard; capture: $Output"
