# Hidden FNA device rendering the production Ghost Samurai field (backdrop, seal,
# abyss) from the exported SamuraiBattlefield.fxc over a stand-in world.
# Offline material review only; no game, Terraria or server is started.
param([Parameter(Mandatory=$true)][string]$TModLoaderPath,
      [Parameter(Mandatory=$true)][string]$LuminancePackage,
      [string]$OutputDirectory='.local/samurai-field-preview')
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$tml=(Resolve-Path -LiteralPath $TModLoaderPath).Path
$lumi=(Resolve-Path -LiteralPath $LuminancePackage).Path
$work=Join-Path $root '.local/samurai-field-gpu'
$out=Join-Path $root $OutputDirectory
New-Item -ItemType Directory -Force -Path $work,$out | Out-Null
$files=@('tools/fixtures/SamuraiFieldPreview.cs',
    'Client/Encounters/GhostSamurai/SamuraiFieldRenderer.cs',
    'Content/Encounters/GhostSamurai/SamuraiArenaBounds.cs')
$includes=($files | ForEach-Object { '<Compile Include="'+[Security.SecurityElement]::Escape((Join-Path $root $_))+'" />' }) -join ''
$fna=[Security.SecurityElement]::Escape((Join-Path $tml 'Libraries/FNA/1.0.0/FNA.dll'))
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><Nullable>enable</Nullable><LangVersion>12.0</LangVersion></PropertyGroup><ItemGroup><Reference Include="FNA"><HintPath>'+$fna+'</HintPath></Reference>'+$includes+'</ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $work 'SamuraiFieldPreview.csproj'),$project)
dotnet build (Join-Path $work 'SamuraiFieldPreview.csproj') -c Release --nologo
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$env:FNA3D_FORCE_DRIVER='D3D11'
dotnet (Join-Path $work 'bin/Release/net8.0/SamuraiFieldPreview.dll') $root $lumi (Join-Path $tml 'Libraries/Native/Windows') $out
exit $LASTEXITCODE
