# Hidden FNA device rendering Choir of the Unmade through the production Doll weapon layer: the real
# ChoirPresentation.Emit, ChoirConcertRules, DollArtAnchors, the exported DollWeapons PNGs and the compiled
# DollPixel.fxc / DollChoirEnergy.fxc. Offline only: no Terraria, no game launch. Writes frames, a contact sheet
# (choir-contact.png) and check frames to -OutputDirectory (relative paths resolve from the repository root) and
# exits non-zero on a failed check.
param([Parameter(Mandatory=$true)][string]$TModLoaderPath,
      [Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$tml=(Resolve-Path -LiteralPath $TModLoaderPath).Path
$out=if([IO.Path]::IsPathRooted($OutputDirectory)){$OutputDirectory}else{Join-Path $root $OutputDirectory}
$work=Join-Path $out 'build'
New-Item -ItemType Directory -Force -Path $out,$work | Out-Null
$files=@('tools/fixtures/DollChoirPreview.cs','Client/Encounters/FirstSeverance/Weapons/DollWeaponLayer.cs',
    'Client/Encounters/FirstSeverance/Weapons/DollWeaponCanvas.cs','Client/Encounters/FirstSeverance/Weapons/DollPixelArt.cs',
    'Client/Encounters/FirstSeverance/Weapons/DollSpritePlacement.cs','Client/Encounters/FirstSeverance/Weapons/ChoirPresentation.cs',
    'Client/Encounters/FirstSeverance/Weapons/DollArtAnchors.g.cs','Content/Encounters/FirstSeverance/Rewards/ChoirConcertRules.cs')
$includes=($files | ForEach-Object { '<Compile Include="'+[Security.SecurityElement]::Escape((Join-Path $root $_))+'" />' }) -join ''
$fna=[Security.SecurityElement]::Escape((Join-Path $tml 'Libraries/FNA/1.0.0/FNA.dll'))
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost></PropertyGroup><ItemGroup><Reference Include="FNA"><HintPath>'+$fna+'</HintPath></Reference>'+$includes+'</ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $work 'DollChoirPreview.csproj'),$project)
dotnet build (Join-Path $work 'DollChoirPreview.csproj') -c Release --nologo
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$env:FNA3D_FORCE_DRIVER='D3D11'
dotnet (Join-Path $work 'bin/Release/net8.0/DollChoirPreview.dll') $root (Join-Path $tml 'Libraries/Native/Windows') $out
exit $LASTEXITCODE
