param(
    [Parameter(Mandatory=$true)][string]$NpcSource,
    [Parameter(Mandatory=$true)][string]$RigSource,
    [Parameter(Mandatory=$true)][string]$ShellSource,
    [Parameter(Mandatory=$true)][string]$ArchiveDirectory
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
# Mechanical game export, not an illustration generator: strip the generated
# green matte, nearest-sample, and quantize to one restrained 32-colour palette.
# Original generated images are always retained, outside the packaged source.
Add-Type -ReferencedAssemblies System.Drawing,System.Drawing.Primitives,System.Drawing.Common -TypeDefinition @'
using System;
using System.Drawing;
public static class DollAssetExport {
    static readonly int[] Rgb = {
        0x121017,0x1d1a22,0x29252f,0x38323d,0x49404a,0x5b505a,0x6b626b,0x817881,
        0x99929a,0xb1aab0,0xc9c4c9,0xe1dce0,0xf4edef,0x302a29,0x493a36,0x635048,
        0x80675b,0x9c8070,0xb69a85,0xd0b69e,0xe5cfb7,0xf4e3ce,0x756b6a,0x938481,
        0xb0a19b,0xc8b9af,0xdbcec2,0xede2d4,0xfcf4e6,0x444855,0x747b89,0xa9aeb6
    };
    static bool Empty(Color c) => c.A < 100 || (c.G > 75 && c.G > c.R * 1.22 + 18 && c.G > c.B * 1.22 + 18);
    static Color Palette(Color c) {
        if (Empty(c)) return Color.FromArgb(0,0,0,0);
        int winner = 0, best = int.MaxValue;
        for (int i = 0; i < Rgb.Length; ++i) {
            int rgb = Rgb[i], r = c.R - ((rgb >> 16) & 255), g = c.G - ((rgb >> 8) & 255), b = c.B - (rgb & 255);
            int distance = r*r + g*g*2 + b*b;
            if (distance < best) { best = distance; winner = rgb; }
        }
        return Color.FromArgb(255, (winner >> 16) & 255, (winner >> 8) & 255, winner & 255);
    }
    public static Rectangle Bounds(Bitmap source, Rectangle region) {
        int left = region.Right, right = region.Left, top = region.Bottom, bottom = region.Top;
        for (int y = region.Top; y < region.Bottom; ++y) for (int x = region.Left; x < region.Right; ++x) {
            if (Empty(source.GetPixel(x,y))) continue;
            left = Math.Min(left,x); right = Math.Max(right,x); top = Math.Min(top,y); bottom = Math.Max(bottom,y);
        }
        if (left > right || top > bottom) throw new InvalidOperationException("Empty sprite.");
        return new Rectangle(left,top,right-left+1,bottom-top+1);
    }
    public static void Copy(Bitmap source, Rectangle from, Bitmap target, Rectangle to) {
        for (int y = 0; y < to.Height; ++y) for (int x = 0; x < to.Width; ++x) {
            int sx = from.Left + Math.Min(from.Width-1,(int)((x+.5)*from.Width/to.Width));
            int sy = from.Top + Math.Min(from.Height-1,(int)((y+.5)*from.Height/to.Height));
            target.SetPixel(to.Left+x,to.Top+y,Palette(source.GetPixel(sx,sy)));
        }
    }
}
'@
$dollRepo = Split-Path -Parent $PSScriptRoot
$dollOut = Join-Path $dollRepo 'Assets/Textures/NPCs/DollTheater'
New-Item -ItemType Directory -Force -Path $dollOut,$ArchiveDirectory | Out-Null
$dollInputs = @{npc=$NpcSource;rig=$RigSource;shell=$ShellSource}
foreach ($kind in $dollInputs.Keys) {
    $destination = Join-Path $ArchiveDirectory ($kind + '-source.png')
    if (Test-Path -LiteralPath $destination) {
        if ((Get-FileHash -LiteralPath $destination).Hash -ne (Get-FileHash -LiteralPath $dollInputs[$kind]).Hash) {
            throw "Archive already contains different $kind source; choose a new directory."
        }
    } else { Copy-Item -LiteralPath $dollInputs[$kind] -Destination $destination }
}
$npc = [System.Drawing.Bitmap]::FromFile($NpcSource)
$npcOut = [System.Drawing.Bitmap]::new(32,104)
try {
    for ($frame=0;$frame -lt 2;$frame++) {
        $region = [System.Drawing.Rectangle]::new([int]($frame*$npc.Width/2),0,[int]($npc.Width/2),$npc.Height)
        $bounds = [DollAssetExport]::Bounds($npc,$region)
        $width = [Math]::Max(1,[int][Math]::Round($bounds.Width*48.0/$bounds.Height))
        [DollAssetExport]::Copy($npc,$bounds,$npcOut,[System.Drawing.Rectangle]::new([int]((32-$width)/2),$frame*52+2,$width,48))
    }
    $npcOut.Save((Join-Path $dollOut 'DollAttendant.png'))
} finally { $npc.Dispose(); $npcOut.Dispose() }
$rig = [System.Drawing.Bitmap]::FromFile($RigSource)
$rigOut = [System.Drawing.Bitmap]::new(384,384)
try {
    # Fixed 3x3 cells retain authored joint pivots, including transparent padding.
    for ($cell=0;$cell -lt 9;$cell++) {
        $cx=$cell%3; $cy=[Math]::Floor($cell/3)
        [DollAssetExport]::Copy($rig,[System.Drawing.Rectangle]::new([int]($cx*$rig.Width/3),[int]($cy*$rig.Height/3),[int]($rig.Width/3),[int]($rig.Height/3)),
            $rigOut,[System.Drawing.Rectangle]::new($cx*128,$cy*128,128,128))
    }
    $rigOut.Save((Join-Path $dollOut 'DollRigAtlas.png'))
    $headOut=[System.Drawing.Bitmap]::new(34,34)
    try {
        [DollAssetExport]::Copy($rigOut,[System.Drawing.Rectangle]::new(24,1,84,82),$headOut,[System.Drawing.Rectangle]::new(0,0,34,34))
        $headOut.Save((Join-Path $dollOut 'DollHead.png'))
    } finally { $headOut.Dispose() }
} finally { $rig.Dispose(); $rigOut.Dispose() }
$shell = [System.Drawing.Bitmap]::FromFile($ShellSource)
$shellOut = [System.Drawing.Bitmap]::new(256,256)
try {
    $bounds = [DollAssetExport]::Bounds($shell,[System.Drawing.Rectangle]::new(0,0,$shell.Width,$shell.Height))
    $width = [int][Math]::Round($bounds.Width*244.0/$bounds.Height)
    [DollAssetExport]::Copy($shell,$bounds,$shellOut,[System.Drawing.Rectangle]::new([int]((256-$width)/2),6,$width,244))
    $shellOut.Save((Join-Path $dollOut 'DollCoffin.png'))
} finally { $shell.Dispose(); $shellOut.Dispose() }
Get-ChildItem -LiteralPath $dollOut -File | Select-Object Name,Length
