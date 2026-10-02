# Hidden FNA device rendering Lacrimosa's Claws through the shared Doll weapon layer: the production canvas,
# pixel art, placement, generated anchors, the claw's pure motion and LacrimosaClawPresentation.Emit, with the
# compiled DollPixel.fxc and DollClawEnergy.fxc and the exported DW01 PNGs. Offline only: no Terraria, no game
# launch. Writes frames and claws-contact.png to -OutputDirectory (relative paths resolve from the repository root)
# and exits non-zero on a failed check.
param([Parameter(Mandatory=$true)][string]$TModLoaderPath,
      [Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$tml=(Resolve-Path -LiteralPath $TModLoaderPath).Path
$out=if([IO.Path]::IsPathRooted($OutputDirectory)){$OutputDirectory}else{Join-Path $root $OutputDirectory}
$work=Join-Path $out 'build'
New-Item -ItemType Directory -Force -Path $out,$work | Out-Null
$files=@('tools/fixtures/DollClawsPreview.cs','Client/Encounters/FirstSeverance/Weapons/DollWeaponLayer.cs',
    'Client/Encounters/FirstSeverance/Weapons/DollWeaponCanvas.cs','Client/Encounters/FirstSeverance/Weapons/DollPixelArt.cs',
    'Client/Encounters/FirstSeverance/Weapons/DollSpritePlacement.cs','Client/Encounters/FirstSeverance/Weapons/DollArtAnchors.g.cs',
    'Client/Encounters/FirstSeverance/Weapons/LacrimosaClawArt.cs','Client/Encounters/FirstSeverance/Weapons/LacrimosaClawPresentation.cs',
    'Content/Encounters/FirstSeverance/Rewards/LacrimosaClawMotion.cs','Content/Encounters/FirstSeverance/Rewards/LacrimosaClawHeart.cs',
    'Content/Encounters/FirstSeverance/Rewards/RitualArmamentRules.cs')
$includes=($files | ForEach-Object { '<Compile Include="'+[Security.SecurityElement]::Escape((Join-Path $root $_))+'" />' }) -join ''
$fna=[Security.SecurityElement]::Escape((Join-Path $tml 'Libraries/FNA/1.0.0/FNA.dll'))
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost></PropertyGroup><ItemGroup><Reference Include="FNA"><HintPath>'+$fna+'</HintPath></Reference>'+$includes+'</ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $work 'DollClawsPreview.csproj'),$project)
dotnet build (Join-Path $work 'DollClawsPreview.csproj') -c Release --nologo
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$env:FNA3D_FORCE_DRIVER='D3D11'
dotnet (Join-Path $work 'bin/Release/net8.0/DollClawsPreview.dll') $root (Join-Path $tml 'Libraries/Native/Windows') $out
exit $LASTEXITCODE
