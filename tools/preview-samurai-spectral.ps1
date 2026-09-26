# Hidden FNA device exercising the linked production Samurai body and hazard renderers.
param([Parameter(Mandatory=$true)][string]$TModLoaderPath,
      [Parameter(Mandatory=$true)][string]$LuminancePackage,
      [string]$OutputDirectory='.local/samurai-spectral-preview')
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$tml=(Resolve-Path -LiteralPath $TModLoaderPath).Path
$lumi=(Resolve-Path -LiteralPath $LuminancePackage).Path
$work=Join-Path $root $OutputDirectory
New-Item -ItemType Directory -Force -Path $work | Out-Null
$files=@('tools/fixtures/SamuraiSpectralPreview.cs',
    'Client/Encounters/GhostSamurai/GhostSamuraiRigArt.cs',
    'Client/Encounters/GhostSamurai/GhostSamuraiEnergy.cs',
    'Client/Encounters/GhostSamurai/GhostSamuraiComposite.cs',
    'Client/Encounters/GhostSamurai/GhostSamuraiMaterials.cs',
    'Client/Encounters/GhostSamurai/SamuraiRigMotion.cs',
    'Client/Encounters/GhostSamurai/SamuraiSpriteFrames.cs',
    'Content/Encounters/GhostSamurai/GhostSamuraiRules.cs',
    'Content/Encounters/GhostSamurai/SamuraiArenaBounds.cs',
    'Content/Encounters/GhostSamurai/SamuraiComboRules.cs',
    'Content/Encounters/GhostSamurai/SamuraiWaveRules.cs',
    'Client/Graphics/WorldGraphicsScope.cs')
$includes=($files | ForEach-Object { '<Compile Include="'+[Security.SecurityElement]::Escape((Join-Path $root $_))+'" />' }) -join ''
$fna=[Security.SecurityElement]::Escape((Join-Path $tml 'Libraries/FNA/1.0.0/FNA.dll'))
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><Nullable>enable</Nullable></PropertyGroup><ItemGroup><Reference Include="FNA"><HintPath>'+$fna+'</HintPath></Reference>'+$includes+'</ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $work 'SamuraiSpectralPreview.csproj'),$project)
dotnet build (Join-Path $work 'SamuraiSpectralPreview.csproj') -c Release --nologo
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$env:FNA3D_FORCE_DRIVER='D3D11'
dotnet (Join-Path $work 'bin/Release/net8.0/SamuraiSpectralPreview.dll') $root $lumi (Join-Path $tml 'Libraries/Native/Windows') $work
exit $LASTEXITCODE
