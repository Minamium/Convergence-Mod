# Read the compiled policy directly from a TMOD; no game, Mod load or file writes.
param([Parameter(Mandatory=$true)][string]$PackagePath, [switch]$InspectOnly)
$ErrorActionPreference = 'Stop'
$path = (Resolve-Path -LiteralPath $PackagePath).Path
$stream = [IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
$reader = [IO.BinaryReader]::new($stream)
try {
    if ([string]::new($reader.ReadChars(4)) -ne 'TMOD') { throw 'Invalid package header' }
    $null = $reader.ReadString(); $null = $reader.ReadBytes(280)
    $name = $reader.ReadString(); $version = $reader.ReadString(); $count = $reader.ReadInt32()
    if ($name -ne 'Convergence' -or $count -lt 1 -or $count -gt 10000) { throw 'Invalid package identity/count' }
    $offset = [long]0; $selected = [long]-1; $size = 0; $packed = 0
    for ($i=0; $i -lt $count; $i++) {
        $entry = $reader.ReadString(); $plainSize = $reader.ReadInt32(); $compressedSize = $reader.ReadInt32()
        if ($plainSize -lt 0 -or $compressedSize -lt 0 -or $compressedSize -gt $stream.Length) { throw 'Invalid entry size' }
        if ($entry -eq 'Convergence.dll') { $selected=$offset; $size=$plainSize; $packed=$compressedSize }
        $offset += $compressedSize
    }
    if ($selected -lt 0 -or $size -gt 64MB -or $stream.Position+$offset -gt $stream.Length) { throw 'Invalid assembly bounds' }
    $stream.Position += $selected; $raw = $reader.ReadBytes($packed)
    if ($raw.Length -ne $packed) { throw 'Truncated package' }
    if ($size -ne $packed) {
        $input = [IO.MemoryStream]::new($raw)
        $deflate = [IO.Compression.DeflateStream]::new($input,[IO.Compression.CompressionMode]::Decompress)
        $output = [IO.MemoryStream]::new()
        try { $deflate.CopyTo($output); $raw = $output.ToArray() }
        finally { $deflate.Dispose(); $output.Dispose(); $input.Dispose() }
    }
    if ($raw.Length -ne $size) { throw 'Invalid decompressed length' }
    $assembly = [Reflection.Assembly]::Load($raw)
    $flags = [Reflection.BindingFlags]'Static,Public,NonPublic'
    $policy = $assembly.GetType('Convergence.Content.Encounters.FirstSeverance.Development.FirstSeveranceDevelopmentPolicy',$true)
    $field = $policy.GetField('AllowSoloStart',$flags)
    if ($null -eq $field) { $field = $policy.GetField('AllowSoloDebugStart',$flags) }
    $solo = $field.GetRawConstantValue()
    $minimum = $policy.GetProperty('MinimumParticipants',$flags).GetValue($null)
    $maximum = $assembly.GetType('Convergence.Content.Encounters.FirstSeverance.FirstSeveranceRoster',$true).GetField('MaximumCount',$flags).GetRawConstantValue()
    [ordered]@{version=$version; solo_enabled=$solo; minimum_participants=$minimum; maximum_participants=$maximum; sha256=(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant()} | ConvertTo-Json -Compress
    if (-not $InspectOnly -and (-not $solo -or $minimum -ne 1 -or $maximum -ne 4)) {
        throw "Package $version violates the 1-4 player admission policy; do not install/publish it."
    }
} finally { $reader.Dispose(); $stream.Dispose() }
