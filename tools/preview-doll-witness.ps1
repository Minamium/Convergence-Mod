# Hidden FNA device rendering Last Witness v2 through the shared Doll weapon layer: the production canvas, art,
# placement and WitnessPresentation/WitnessRules sources, the real exported PNGs and the compiled DollPixel.fxc and
# DollWitnessEnergy.fxc. Offline only: no Terraria, no game launch. Writes frames, layer captures and a contact sheet
# to -OutputDirectory (relative paths resolve from the repository root) and exits non-zero on a failed check.
param([Parameter(Mandatory=$true)][string]$TModLoaderPath,
      [Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$tml=(Resolve-Path -LiteralPath $TModLoaderPath).Path
$out=if([IO.Path]::IsPathRooted($OutputDirectory)){$OutputDirectory}else{Join-Path $root $OutputDirectory}
$work=Join-Path $out 'build'
New-Item -ItemType Directory -Force -Path $out,$work | Out-Null
$files=@('tools/fixtures/DollWitnessPreview.cs',
    'Client/Encounters/FirstSeverance/Weapons/DollWeaponLayer.cs','Client/Encounters/FirstSeverance/Weapons/DollWeaponCanvas.cs',
    'Client/Encounters/FirstSeverance/Weapons/DollPixelArt.cs','Client/Encounters/FirstSeverance/Weapons/DollSpritePlacement.cs',
    'Client/Encounters/FirstSeverance/Weapons/DollArtAnchors.g.cs','Client/Encounters/FirstSeverance/Weapons/WitnessPresentation.cs',
    'Content/Encounters/FirstSeverance/Rewards/WitnessRules.cs','Content/Encounters/FirstSeverance/Rewards/RitualGrandScore.cs',
    'Content/Encounters/FirstSeverance/Rewards/RitualKineticMotion.cs','Content/Encounters/FirstSeverance/Rewards/NullCantorClawMotion.cs',
    'Content/Encounters/FirstSeverance/Rewards/RitualArmamentChoreography.cs','Content/Encounters/FirstSeverance/Rewards/RitualArmamentRules.cs')
$includes=($files | ForEach-Object { '<Compile Include="'+[Security.SecurityElement]::Escape((Join-Path $root $_))+'" />' }) -join ''
$fna=[Security.SecurityElement]::Escape((Join-Path $tml 'Libraries/FNA/1.0.0/FNA.dll'))
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><AssemblyName>DollWitnessPreview</AssemblyName></PropertyGroup><ItemGroup><Reference Include="FNA"><HintPath>'+$fna+'</HintPath></Reference>'+$includes+'</ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $work 'DollWitnessPreview.csproj'),$project)
dotnet build (Join-Path $work 'DollWitnessPreview.csproj') -c Release --nologo
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$env:FNA3D_FORCE_DRIVER='D3D11'
dotnet (Join-Path $work 'bin/Release/net8.0/DollWitnessPreview.dll') $root (Join-Path $tml 'Libraries/Native/Windows') $out
exit $LASTEXITCODE
