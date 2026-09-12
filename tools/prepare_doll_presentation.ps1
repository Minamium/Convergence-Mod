param(
    [Parameter(Mandatory=$true)][string]$BroomSource,
    [Parameter(Mandatory=$true)][string]$BoxSource,
    [Parameter(Mandatory=$true)][string]$RestraintSource,
    [Parameter(Mandatory=$true)][string]$RejectedBroomSource,
    [Parameter(Mandatory=$true)][string]$ArchiveDirectory
)
$ErrorActionPreference='Stop'
$dollRepo=Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing,System.Drawing.Primitives,System.Drawing.Common,System.Collections,System.Console,System.Private.Windows.GdiPlus,System.Private.Windows.Core `
    -Path @((Join-Path $PSScriptRoot 'DollFrameExport.cs'),(Join-Path $PSScriptRoot 'DollPresentationExport.cs'))
$dollOriginals=Join-Path $ArchiveDirectory 'originals'
New-Item -ItemType Directory -Force -Path $dollOriginals | Out-Null
foreach($entry in @(@('broom-green',$BroomSource),@('treasure-box-alpha',$BoxSource),@('newrestraint',$RestraintSource),@('broom-rejected-painted-checkerboard',$RejectedBroomSource))) {
    $archived=Join-Path $dollOriginals ($entry[0]+'.png')
    if(Test-Path -LiteralPath $archived) {
        if((Get-FileHash -LiteralPath $archived).Hash -ne (Get-FileHash -LiteralPath $entry[1]).Hash) {throw 'Different original already archived.'}
    } else {Copy-Item -LiteralPath $entry[1] -Destination $archived}
}
[DollPresentationExport]::ExportBroom($BroomSource,
    (Join-Path $dollRepo 'Assets/Textures/NPCs/DollTheater/DollBroom.png'),$ArchiveDirectory)
[DollPresentationExport]::ExportBox($BoxSource,
    (Join-Path $dollRepo 'Assets/Textures/Items/RitualArmaments/DollTreasureBox.png'),$ArchiveDirectory)
[DollPresentationExport]::ExportRestraint($RestraintSource,
    (Join-Path $dollRepo 'Assets/Textures/NPCs/DollTheater/MechanicalRestraintFrames.png'),$ArchiveDirectory)
Get-FileHash -LiteralPath @(
    (Join-Path $dollRepo 'Assets/Textures/NPCs/DollTheater/DollBroom.png'),
    (Join-Path $dollRepo 'Assets/Textures/Items/RitualArmaments/DollTreasureBox.png'),
    (Join-Path $dollRepo 'Assets/Textures/NPCs/DollTheater/MechanicalRestraintFrames.png'))
