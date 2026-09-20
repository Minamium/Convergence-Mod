# Offline production Oboro renderer on a hidden FNA device. No Terraria/game session.
param([Parameter(Mandatory=$true)][string]$TModLoaderPath,
      [string]$OutputDirectory='.local/oboro-motion-preview')
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$tml=(Resolve-Path -LiteralPath $TModLoaderPath).Path
$work=Join-Path $root $OutputDirectory
New-Item -ItemType Directory -Force -Path $work | Out-Null
$files=@('tools/fixtures/OboroMotionPreview.cs','Client/Weapons/OboroArt.cs','Client/Weapons/OboroFinisherArt.cs','Client/Weapons/SpectralSpriteCutouts.cs','Client/Weapons/OboroSwingPresentation.cs',
    'Content/Items/Oboro/OboroRules.cs','Content/Items/Oboro/OboroComboSettings.cs','Content/Items/Oboro/OboroFirstSwingMotion.cs',
    'Content/Items/Oboro/OboroThirdSwingMotion.cs','Content/Items/Oboro/OboroSecondSwingMotion.cs','Content/Items/Oboro/OboroWire.cs')
$includes=($files | ForEach-Object { '<Compile Include="'+[Security.SecurityElement]::Escape((Join-Path $root $_))+'" />' }) -join ''
$fna=[Security.SecurityElement]::Escape((Join-Path $tml 'Libraries/FNA/1.0.0/FNA.dll'))
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost></PropertyGroup><ItemGroup><Reference Include="FNA"><HintPath>'+$fna+'</HintPath></Reference>'+$includes+'</ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $work 'OboroPreview.csproj'),$project)
dotnet build (Join-Path $work 'OboroPreview.csproj') -c Release --nologo
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$env:FNA3D_FORCE_DRIVER='D3D11'
dotnet (Join-Path $work 'bin/Release/net8.0/OboroPreview.dll') $root (Join-Path $tml 'Libraries/Native/Windows') $work
exit $LASTEXITCODE
