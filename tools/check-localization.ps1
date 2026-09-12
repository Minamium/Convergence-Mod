param(
    [Parameter(Mandatory=$true)][string]$TModLoaderPath,
    [string]$LocalizationRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Localization')
)
$ErrorActionPreference = 'Stop'
# Parse with the exact library installed alongside tML, not a JSON parser or a
# regex that misses Hjson's unquoted-string/newline rules. No game is launched.
$libraries = Join-Path $TModLoaderPath 'Libraries/hjson'
$assemblies = @(Get-ChildItem -LiteralPath $libraries -Filter Hjson.dll -File -Recurse)
if ($assemblies.Count -ne 1) { throw "Expected one installed Hjson library under $libraries; found $($assemblies.Count)." }
[Reflection.Assembly]::LoadFrom($assemblies[0].FullName) | Out-Null
$files = @(Get-ChildItem -LiteralPath $LocalizationRoot -Filter *.hjson -File -Recurse | Sort-Object FullName)
if ($files.Count -eq 0) { throw "No localization files under $LocalizationRoot." }
$failures = 0
foreach ($file in $files) {
    try {
        [Hjson.HjsonValue]::Load($file.FullName) | Out-Null
    } catch {
        $failures++
        $reason = $_.Exception.GetBaseException().Message
        [Console]::Error.WriteLine("Invalid Hjson: $($file.FullName): $reason")
    }
}
if ($failures -gt 0) {
    [Console]::Error.WriteLine("Localization validation failed: $failures of $($files.Count) files.")
    exit 1
}
Write-Output "Localization validation passed ($($files.Count) files; installed $($assemblies[0].FullName))."
