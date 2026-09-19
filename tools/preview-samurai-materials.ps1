# Offline hidden-device renderer only. Does not instantiate Terraria or start a game.
param(
    [Parameter(Mandatory=$true)][string]$TModLoaderPath,
    [Parameter(Mandatory=$true)][string]$LuminanceSource,
    [string]$RepoRoot = (Join-Path $PSScriptRoot '..')
)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $RepoRoot).Path
$tml=(Resolve-Path -LiteralPath $TModLoaderPath).Path
$lumi=(Resolve-Path -LiteralPath $LuminanceSource).Path
$work=Join-Path $root '.local/rig-gpu'
New-Item -ItemType Directory -Force -Path $work | Out-Null
$fna=Join-Path $tml 'Libraries/FNA/1.0.0/FNA.dll'
$native=Join-Path $tml 'Libraries/Native/Windows'
$fixture=Join-Path $PSScriptRoot 'fixtures/SamuraiMaterialPreview.cs'
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost></PropertyGroup><ItemGroup><Reference Include="FNA"><HintPath>'+[Security.SecurityElement]::Escape($fna)+'</HintPath></Reference><Compile Include="'+[Security.SecurityElement]::Escape($fixture)+'" /></ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $work 'RigGpu.csproj'),$project)
dotnet build (Join-Path $work 'RigGpu.csproj') -c Release --nologo
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$env:FNA3D_FORCE_DRIVER='D3D11'
dotnet (Join-Path $work 'bin/Release/net8.0/RigGpu.dll') $root $lumi $native $work
exit $LASTEXITCODE
