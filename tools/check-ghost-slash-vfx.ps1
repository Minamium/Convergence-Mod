# CPU draw-call capture of the linked production VFX code and shared geometry.
# No game, server, window, graphics device or user saves are opened.
param([string]$RepoRoot = (Join-Path $PSScriptRoot '..'))
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $RepoRoot).Path
$files = @(
    'Content/Encounters/GhostSamurai/GhostSamuraiRules.cs',
    'Content/Encounters/GhostSamurai/SamuraiArenaBounds.cs',
    'Content/Encounters/GhostSamurai/SamuraiComboRules.cs',
    'Content/Encounters/GhostSamurai/SamuraiWaveRules.cs',
    'Client/Encounters/GhostSamurai/GhostSamuraiSlashArt.cs',
    'Client/Encounters/GhostSamurai/GhostSamuraiCircleVisuals.cs',
    'Client/Encounters/GhostSamurai/GhostSamuraiComboVisuals.cs',
    'Client/Encounters/GhostSamurai/GhostSamuraiWaveVisuals.cs'
)
$source = foreach ($file in $files) {
    $text = [IO.File]::ReadAllText((Join-Path $root $file))
    $match = [regex]::Match($text, '(?s)^(.*?)namespace ([^;]+);(.*)$')
    if (!$match.Success) { throw "Expected file-scoped namespace: $file" }
    'namespace ' + $match.Groups[2].Value + ' {' + $match.Groups[1].Value + $match.Groups[3].Value + "`n}"
}
$fixture = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'fixtures/GhostSlashVisualFixture.cs'))
Add-Type -TypeDefinition (($source -join "`n") + "`n" + $fixture)
[GhostSlashVisualFixture]::Run()
