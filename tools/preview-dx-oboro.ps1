# Hidden FNA device exercising DXOboro's production motion, sword art and managed material.
param([Parameter(Mandatory=$true)][string]$TModLoaderPath,
      [string]$OutputDirectory='.local/weapon-motion-20260927/soboro', [switch]$IconsOnly,
      [switch]$OneSequence)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$tml=(Resolve-Path -LiteralPath $TModLoaderPath).Path
$work=Join-Path $root $OutputDirectory
New-Item -ItemType Directory -Force -Path $work | Out-Null
$files=@('tools/fixtures/DXOboroPreview.cs','Content/Items/DXOboro/DXOboroMotion.cs',
    'Client/Weapons/DXOboroArt.cs','Client/Weapons/DXOboroItemVisuals.cs','Client/Weapons/DXOboroMaterial.cs',
    'Client/Graphics/WorldGraphicsScope.cs','Client/Graphics/ReadableItemIcon.cs','Client/Graphics/SwordLightning.cs')
$includes=($files | ForEach-Object { '<Compile Include="'+[Security.SecurityElement]::Escape((Join-Path $root $_))+'" />' }) -join ''
$fna=[Security.SecurityElement]::Escape((Join-Path $tml 'Libraries/FNA/1.0.0/FNA.dll'))
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost></PropertyGroup><ItemGroup><Reference Include="FNA"><HintPath>'+$fna+'</HintPath></Reference>'+$includes+'</ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $work 'DXOboroPreview.csproj'),$project)
dotnet build (Join-Path $work 'DXOboroPreview.csproj') -c Release --nologo
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$env:FNA3D_FORCE_DRIVER='D3D11'
dotnet (Join-Path $work 'bin/Release/net8.0/DXOboroPreview.dll') $root (Join-Path $tml 'Libraries/Native/Windows') $work $IconsOnly.IsPresent $OneSequence.IsPresent
exit $LASTEXITCODE
