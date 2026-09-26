# Offline hidden-device preview of the compiled DollCoreEnergy material.
param(
    [Parameter(Mandatory=$true)][string]$TModLoaderPath,
    [Parameter(Mandatory=$true)][string]$LuminancePackage,
    [string]$RepoRoot = (Join-Path $PSScriptRoot '..')
)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $RepoRoot).Path
$tml=(Resolve-Path -LiteralPath $TModLoaderPath).Path
$lumi=(Resolve-Path -LiteralPath $LuminancePackage).Path
$work=Join-Path $root '.local/doll-core-rupture'
New-Item -ItemType Directory -Force -Path $work | Out-Null
$fna=Join-Path $tml 'Libraries/FNA/1.0.0/FNA.dll'
$native=Join-Path $tml 'Libraries/Native/Windows'
$fixture=Join-Path $PSScriptRoot 'fixtures/DollCorePreview.cs'
$sources = @($fixture, (Join-Path $PSScriptRoot 'fixtures/DollCorePreviewStubs.cs'))
foreach ($name in @('FirstSeveranceMechanicalCore', 'FirstSeveranceCoreRupture', 'FirstSeveranceCoreCrater')) {
    $sources += Join-Path $root "Client/Encounters/FirstSeverance/$name.cs"
}
$includes = ($sources | ForEach-Object { '<Compile Include="'+[Security.SecurityElement]::Escape($_)+'" />' }) -join ''
$project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost></PropertyGroup><ItemGroup><Reference Include="FNA"><HintPath>'+[Security.SecurityElement]::Escape($fna)+'</HintPath></Reference>'+$includes+'</ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $work 'DollCoreGpu.csproj'),$project)
dotnet build (Join-Path $work 'DollCoreGpu.csproj') -c Release --nologo
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
$env:FNA3D_FORCE_DRIVER='D3D11'
dotnet (Join-Path $work 'bin/Release/net8.0/DollCoreGpu.dll') $root $lumi $native $work
exit $LASTEXITCODE
