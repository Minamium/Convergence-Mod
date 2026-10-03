# Hidden FNA device rendering the Pale Meridian through the shared Doll weapon layer: the production
# MeridianPresentation, canvas, art and placement code with the compiled DollPixel.fxc and DollMeridianEnergy.fxc,
# the exported DollWeapons PNGs and the noise read from the installed Luminance package. Offline only: no Terraria,
# no game launch. Writes contact sheets (meridian-contact.png and one per section) to -OutputDirectory (relative
# paths resolve from the repository root) and exits non-zero on a failed check.
param([Parameter(Mandatory=$true)][string]$TModLoaderPath,
      [Parameter(Mandatory=$true)][string]$LuminancePackage,
      [Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$tml=(Resolve-Path -LiteralPath $TModLoaderPath).Path
$lumi=(Resolve-Path -LiteralPath $LuminancePackage).Path
$out=if([IO.Path]::IsPathRooted($OutputDirectory)){$OutputDirectory}else{Join-Path $root $OutputDirectory}
$work=Join-Path $out 'build'
New-Item -ItemType Directory -Force -Path $out,$work | Out-Null
$files=@('tools/fixtures/MeridianPreview.cs','Client/Encounters/FirstSeverance/Weapons/MeridianPresentation.cs',
    'Client/Encounters/FirstSeverance/Weapons/DollWeaponLayer.cs','Client/Encounters/FirstSeverance/Weapons/DollWeaponCanvas.cs',
    'Client/Encounters/FirstSeverance/Weapons/DollPixelArt.cs','Client/Encounters/FirstSeverance/Weapons/DollSpritePlacement.cs',
    'Client/Encounters/FirstSeverance/Weapons/DollArtAnchors.g.cs','Content/Encounters/FirstSeverance/Rewards/PaleMeridianScore.cs',
    'Content/Encounters/FirstSeverance/Rewards/PaleMeridianLattice.cs','Content/Encounters/FirstSeverance/Rewards/PaleMeridianRig.cs',
    'Content/Encounters/FirstSeverance/Rewards/RitualArmamentRules.cs')
$includes=($files | ForEach-Object { '<Compile Include="'+[Security.SecurityElement]::Escape((Join-Path $root $_))+'" />' }) -join ''
$fna=[Security.SecurityElement]::Escape((Join-Path $tml 'Libraries/FNA/1.0.0/FNA.dll'))
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><Nullable>enable</Nullable><LangVersion>12.0</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost></PropertyGroup><ItemGroup><Reference Include="FNA"><HintPath>'+$fna+'</HintPath></Reference>'+$includes+'</ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $work 'MeridianPreview.csproj'),$project)
dotnet build (Join-Path $work 'MeridianPreview.csproj') -c Release --nologo
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$env:FNA3D_FORCE_DRIVER='D3D11'
dotnet (Join-Path $work 'bin/Release/net8.0/MeridianPreview.dll') $root (Join-Path $tml 'Libraries/Native/Windows') $lumi $out
exit $LASTEXITCODE
