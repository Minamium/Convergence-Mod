# Offline hidden-device renderer only. Does not instantiate Terraria or start a game.
# Renders the distributable Ebon Manor effects on the real textures for material QA.
param(
    [Parameter(Mandatory=$true)][string]$TModLoaderPath,
    # Folder holding WavyBlotchNoise.png and TurbulentNoise.png from the installed Luminance 1.0.14.
    [Parameter(Mandatory=$true)][string]$NoisePath,
    [string]$RepoRoot = (Join-Path $PSScriptRoot '..')
)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $RepoRoot).Path
$tml=(Resolve-Path -LiteralPath $TModLoaderPath).Path
$noise=(Resolve-Path -LiteralPath $NoisePath).Path
$work=Join-Path $root '.local/ebon-gpu'
$out=Join-Path $root '.local/ebon-preview'
New-Item -ItemType Directory -Force -Path $work,$out | Out-Null
$fna=Join-Path $tml 'Libraries/FNA/1.0.0/FNA.dll'
$native=Join-Path $tml 'Libraries/Native/Windows'
$fixture=Join-Path $PSScriptRoot 'fixtures/EbonManorPreview.cs'
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost></PropertyGroup><ItemGroup><Reference Include="FNA"><HintPath>'+[Security.SecurityElement]::Escape($fna)+'</HintPath></Reference><Compile Include="'+[Security.SecurityElement]::Escape($fixture)+'" /></ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $work 'EbonGpu.csproj'),$project)
dotnet build (Join-Path $work 'EbonGpu.csproj') -c Release --nologo
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$env:FNA3D_FORCE_DRIVER='D3D11'
dotnet (Join-Path $work 'bin/Release/net8.0/EbonGpu.dll') $root $noise $native $out
exit $LASTEXITCODE
