param(
    [Parameter(Mandatory=$true)][string]$WalkSource,
    [Parameter(Mandatory=$true)][string]$CastSource,
    [Parameter(Mandatory=$true)][string]$IconSource,
    [Parameter(Mandatory=$true)][string]$ArchiveDirectory
)
$ErrorActionPreference='Stop'
$dollRepo=Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing,System.Drawing.Primitives,System.Drawing.Common,System.Collections,System.Console,System.Private.Windows.GdiPlus,System.Private.Windows.Core `
    -Path @((Join-Path $PSScriptRoot 'DollFrameExport.cs'),(Join-Path $PSScriptRoot 'DollCompanionExport.cs'))
New-Item -ItemType Directory -Force -Path $ArchiveDirectory | Out-Null
foreach($entry in @(@('walk',$WalkSource),@('cast',$CastSource),@('item',$IconSource))) {
    $archived=Join-Path $ArchiveDirectory ($entry[0]+'.png')
    if(Test-Path -LiteralPath $archived) {
        if((Get-FileHash $archived).Hash -ne (Get-FileHash $entry[1]).Hash) {throw 'Different original already archived.'}
    } else {Copy-Item -LiteralPath $entry[1] -Destination $archived}
}
[DollCompanionExport]::Export((Join-Path $dollRepo 'Assets/Textures/NPCs/DollTheater/DollAttendant.png'),
    $WalkSource,$CastSource,$IconSource,(Join-Path $dollRepo 'Assets/Textures/NPCs/DollTheater/DollCompanion.png'),
    (Join-Path $ArchiveDirectory 'companion-poses-4x.png'))
