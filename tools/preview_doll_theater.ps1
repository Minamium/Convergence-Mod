param([Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$dollRepo=Split-Path -Parent $PSScriptRoot
Add-Type -ReferencedAssemblies System.Drawing,System.Drawing.Primitives,System.Drawing.Common,System.Numerics.Vectors,System.Collections,System.Private.Windows.GdiPlus,System.Private.Windows.Core `
    -Path @((Join-Path $dollRepo 'Client/Encounters/FirstSeverance/FirstSeveranceDollPose.cs'),(Join-Path $dollRepo 'Client/Encounters/FirstSeverance/FirstSeveranceDollCapture.cs'),(Join-Path $dollRepo 'Client/Encounters/FirstSeverance/FirstSeveranceShellSurface.cs'),(Join-Path $PSScriptRoot 'DollTheaterPreview.cs'))
[DollTheaterPreview]::Create((Join-Path $dollRepo 'Assets/Textures/NPCs/DollTheater'),$OutputPath)
Get-Item -LiteralPath $OutputPath | Select-Object FullName,Length
