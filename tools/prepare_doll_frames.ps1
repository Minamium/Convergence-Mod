param(
    [Parameter(Mandatory=$true)][string]$HandSource,
    [Parameter(Mandatory=$true)][string]$BodySource,
    [Parameter(Mandatory=$true)][string]$ArchiveDirectory
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$frameRepo=Split-Path -Parent $PSScriptRoot
Add-Type -ReferencedAssemblies System.Drawing,System.Drawing.Primitives,System.Drawing.Common,System.Collections,System.Console,System.Private.Windows.GdiPlus,System.Private.Windows.Core `
    -Path (Join-Path $PSScriptRoot 'DollFrameExport.cs')
New-Item -ItemType Directory -Force -Path $ArchiveDirectory | Out-Null
foreach($entry in @(@('hand',$HandSource),@('body',$BodySource))) {
    $archived=Join-Path $ArchiveDirectory ($entry[0]+'-matte.png')
    if(Test-Path -LiteralPath $archived) {
        if((Get-FileHash -LiteralPath $archived).Hash -ne (Get-FileHash -LiteralPath $entry[1]).Hash) { throw 'Different original already archived.' }
    } else { Copy-Item -LiteralPath $entry[1] -Destination $archived }
    $name=if($entry[0] -eq 'hand') {'RemoteClawFrames.png'} else {'RestraintFrames.png'}
    [DollFrameExport]::Export($entry[1],(Join-Path $frameRepo ('Assets/Textures/NPCs/DollTheater/'+$name)),$entry[0] -eq 'hand')
}
