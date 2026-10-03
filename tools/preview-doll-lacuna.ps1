# Hidden FNA device rendering the Lacuna Testament through the shared Doll weapon layer: the production presentation
# (LacunaPresentation, LacunaEnergy), canvas, placement, score, art fit and exported PNGs with the compiled DollPixel.fxc
# and DollLacunaEnergy.fxc and Luminance's noise read from its package. Offline only: no Terraria, no game launch.
# Writes frames and contact sheets to -OutputDirectory (relative paths resolve from the repository root) and exits
# non-zero on a failed check.
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
$files=@('tools/fixtures/DollLacunaPreview.cs','Client/Encounters/FirstSeverance/Weapons/DollWeaponLayer.cs',
    'Client/Encounters/FirstSeverance/Weapons/DollWeaponCanvas.cs','Client/Encounters/FirstSeverance/Weapons/DollPixelArt.cs',
    'Client/Encounters/FirstSeverance/Weapons/DollSpritePlacement.cs','Client/Encounters/FirstSeverance/Weapons/DollArtAnchors.g.cs',
    'Client/Encounters/FirstSeverance/Weapons/LacunaArtFit.cs','Client/Encounters/FirstSeverance/Weapons/LacunaPresentation.cs',
    'Content/Encounters/FirstSeverance/Rewards/LacunaTestamentScore.cs','Content/Encounters/FirstSeverance/Rewards/RitualArmamentRules.cs')
$includes=($files | ForEach-Object { '<Compile Include="'+[Security.SecurityElement]::Escape((Join-Path $root $_))+'" />' }) -join ''
$fna=[Security.SecurityElement]::Escape((Join-Path $tml 'Libraries/FNA/1.0.0/FNA.dll'))
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost></PropertyGroup><ItemGroup><Reference Include="FNA"><HintPath>'+$fna+'</HintPath></Reference>'+$includes+'</ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $work 'DollLacunaPreview.csproj'),$project)
dotnet build (Join-Path $work 'DollLacunaPreview.csproj') -c Release --nologo
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$env:FNA3D_FORCE_DRIVER='D3D11'
dotnet (Join-Path $work 'bin/Release/net8.0/DollLacunaPreview.dll') $root $lumi (Join-Path $tml 'Libraries/Native/Windows') $out
exit $LASTEXITCODE
