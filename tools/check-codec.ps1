# Requires PowerShell 7+; tests the supplied compiled assembly, never starts Terraria.
param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [ValidateSet('enabled','disabled','any')][string]$ExpectedSoloDebug = 'enabled'
)
$ErrorActionPreference = 'Stop'
$assemblyFile = (Resolve-Path -LiteralPath $AssemblyPath).Path
$assembly = [Reflection.Assembly]::LoadFrom($assemblyFile)
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$protocolSource = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'Common/Networking/Protocol/EncounterProtocol.cs')
$expectedProtocol = [int][regex]::Match($protocolSource, 'CurrentVersion\s*=\s*(\d+)').Groups[1].Value
if ($expectedProtocol -lt 1) { throw 'Cannot read the repository protocol declaration' }
$instanceFlags = [Reflection.BindingFlags]'Instance,Public,NonPublic'
$staticFlags = [Reflection.BindingFlags]'Static,Public,NonPublic'
$feature = 'Convergence.Content.Encounters.FirstSeverance.'
$ids = 'Convergence.Common.Foundation.Identifiers.'
function New-Record([string]$name, [object[]]$values) {
    if ($name.EndsWith('FirstSeveranceCombatProjection') -and $values.Count -eq 21) {
        $step = if ($values[18].ToString() -eq 'Sealed' -or $values[2].ToString() -eq 'PhaseTransition') { -1 } else { 0 }
        $values = $values + @([ulong]100, [int]$step, [int]0)
    }
    if ($name.EndsWith('FirstSeveranceCombatProjection') -and $values.Count -eq 24) { $values = $values + @([ulong]0, $null) }
    if ($name.EndsWith('FirstSeveranceCombatProjection') -and $values.Count -eq 26) { $values = $values + @($null) }
    $ctor = $assembly.GetType($name, $true).GetConstructors($instanceFlags) | Where-Object { $_.GetParameters().Count -eq $values.Count } | Select-Object -First 1
    if ($null -eq $ctor) { throw "Constructor missing: $name" }
    return $ctor.Invoke($values)
}
function Get-Field($value, [string]$name) {
    return $value.GetType().GetProperty($name, $instanceFlags).GetValue($value)
}
function Enum-Value([string]$name, [string]$value) { return [Enum]::Parse($assembly.GetType($name), $value) }
$codec = $assembly.GetType($feature + 'FirstSeverancePacketCodec')
$write = $codec.GetMethod('WriteCombat', $staticFlags)
$read = $codec.GetMethod('TryReadCombat', $staticFlags)
$createPattern = $assembly.GetType($feature + 'FirstSeveranceAttackPatterns').GetMethod('Create', $staticFlags)
$version = $assembly.GetType('Convergence.Common.Networking.Protocol.EncounterProtocol').GetField('CurrentVersion', $staticFlags).GetRawConstantValue()
if ($version -ne $expectedProtocol) { throw "Compiled protocol $version does not match source $expectedProtocol" }
$createPrism = $assembly.GetType($feature + 'FirstSeveranceAttackPatterns').GetMethod('CreatePrism', $staticFlags)
$downed = Enum-Value 'Convergence.Common.Raids.Revive.RaidParticipantCombatState' 'Downed'
$soloFlag = $assembly.GetType($feature + 'Development.FirstSeveranceDevelopmentPolicy').GetField('AllowSoloDebugStart', $staticFlags).GetRawConstantValue()
if (($ExpectedSoloDebug -eq 'enabled' -and -not $soloFlag) -or ($ExpectedSoloDebug -eq 'disabled' -and $soloFlag)) {
    throw "Unexpected compiled solo flag: $soloFlag"
}
$aimCore = $assembly.GetType($feature + 'FirstSeveranceGridVolley').GetMethod('AimCoreBeam', $staticFlags)
$partyScaling = $assembly.GetType($feature + 'FirstSeverancePartyScaling').GetMethod('ForCount', $staticFlags)
$sealed = Enum-Value ($feature + 'FirstSeveranceBossPhase') 'Sealed'
$unbound = Enum-Value ($feature + 'FirstSeveranceBossPhase') 'Unbound'
$fight = New-Record ($ids + 'FightId') @([Guid]::NewGuid())
$alive = Enum-Value 'Convergence.Common.Raids.Revive.RaidParticipantCombatState' 'Alive'
$result = Enum-Value ($feature + 'FirstSeveranceMechanicResult') 'None'
$passed = 0
$invalidChecks = 0
foreach ($rosterCount in 1, 2, 3, 4) {
    $participants = [Array]::CreateInstance($assembly.GetType($feature + 'FirstSeveranceCombatParticipantProjection'), $rosterCount)
    for ($i = 0; $i -lt $rosterCount; $i++) {
        $id = New-Record ($ids + 'ParticipantId') @([byte]$i)
        $combatState = if ($i -eq 0) { $downed } else { $alive }
        $member = New-Record ($feature + 'FirstSeveranceCombatParticipantProjection') @($id, [int]$i, $true, $combatState, $false, [ulong]0, [ulong]0, [uint]1, [int]500, [float]100, [float]200, [ulong]0, [ulong]0, [ulong](3600 + $i), ($i -eq 1))
        $participants.SetValue($member, $i)
    }
    foreach ($phaseName in 'PylonCheck', 'CoreExposure') {
        $phase = Enum-Value ($feature + 'FirstSeveranceSubstate') $phaseName
        $stepCount = if ($phaseName -eq 'PylonCheck') { 8 } else { 4 }
        for ($step = -1; $step -lt $stepCount; $step++) {
            $volley = $null
            if ($step -ge 0) {
                $volley = $createPattern.Invoke($null, @([uint]17, [ulong]100, $phase, [byte]$step, [int]($rosterCount - 1), [float]1000, [float]2000, [float]12, [float]-5))
                if ($phaseName -eq 'PylonCheck') {
                    $targets = [Array]::CreateInstance($assembly.GetType($feature + 'FirstSeverancePrismTarget'), $rosterCount)
                    for ($i=0; $i -lt $rosterCount; $i++) {
                        $targets.SetValue((New-Record ($feature + 'FirstSeverancePrismTarget') @([int]$i,[float](1000+$i*350),[float](2000+$i*100),[float]12,[float]-5)), $i)
                    }
                    $volley = $createPrism.Invoke($null, @([uint]17,[ulong]100,[byte]$step,$targets))
                }
                if ((Get-Field $volley 'IsCharge')) {
                    $advance = $volley.GetType().GetMethod('AdvanceCharge', $instanceFlags)
                    $sampleEnd = if ($step -eq 0) { [int](Get-Field $volley 'LockTick') - 1 } else { [int](Get-Field $volley 'FireTick') + 2 }
                    for ($sampleTick = 101; $sampleTick -le $sampleEnd; $sampleTick++) {
                        $volley = $advance.Invoke($volley, @([ulong]$sampleTick,[float]1000,[float]2000,[float]12,[float]-5))
                    }
                }
            }
            $projection = New-Record ($feature + 'FirstSeveranceCombatProjection') @([ulong]1, $fight, $phase, [ulong]1000, [int]0, [int]0, [int]4000000, [int]4000000, [int]0, [uint]1, [float]100, [float]-320, [float]100, [float]560, $result, [uint]0, $participants, $volley, $sealed, [ulong]0, $null)
            $stream = [IO.MemoryStream]::new()
            $writer = [IO.BinaryWriter]::new($stream)
            $writer.Write([byte[]]@(11,12,13,14,15))
            $null = $write.Invoke($null, @($writer, $projection))
            $payloadEnd = $stream.Position
            $writer.Write([byte[]]@(222,173,190,239))
            $stream.Position = 5
            $reader = [IO.BinaryReader]::new($stream)
            $arguments = [object[]]@($reader, [ulong]1, $fight, $null)
            if (-not $read.Invoke($null, $arguments)) { throw "Decode failed: $phaseName/$step/$rosterCount" }
            if ($stream.Position -ne $payloadEnd) { throw 'Wrong shared-buffer consumption' }
            $decoded = $arguments[3]
            if ($decoded.Participants[0].CombatState -ne $downed -or $decoded.Participants[0].DownedDeadlineTick -ne 0) { throw 'Untimed Down changed in transit' }
            if ($decoded.StackX -ne 100 -or $decoded.StackY -ne -320 -or $decoded.BossPhase -ne $sealed) { throw 'Changed fixed stack/stage' }
            for ($i = 0; $i -lt $rosterCount; $i++) {
                if ($decoded.Participants[$i].ReviveLockoutUntilTick -ne (3600 + $i)) { throw 'Recovery deadline changed' }
                if ($decoded.Participants[$i].DebugAssistProtected -ne ($i -eq 1)) { throw 'Auxiliary protection changed target' }
            }
            if ($step -ge 0) {
                $actual = $decoded.LanceVolley
                foreach ($name in 'Kind','Step','TargetSlot','Serial','StartTick','FireTick','EndTick','MotionTick') {
                    if ((Get-Field $actual $name) -ne (Get-Field $volley $name)) { throw "Changed $name" }
                }
                $expectedRays = Get-Field $volley 'Rays'
                $actualRays = Get-Field $actual 'Rays'
                if ($expectedRays.Count -ne $actualRays.Count) { throw 'Ray count changed' }
                for ($i = 0; $i -lt $actualRays.Count; $i++) {
                    if (-not $actualRays[$i].Equals($expectedRays[$i])) { throw 'Ray geometry changed' }
                }
            } elseif ($null -ne $decoded.LanceVolley) { throw 'Unexpected volley' }
            $passed++
            if ($step -eq 0 -and $phaseName -eq 'PylonCheck' -and $rosterCount -eq 2) {
                $original = $stream.ToArray()
                # Feature-body layout: 55-byte fixed prefix and 62 bytes per participant. Outer route is tested in PacketTests.
                $attackOffset = 5 + 55 + 62 * $rosterCount
                if ($original[$attackOffset + 25] -ne $rosterCount) { throw 'Attack fixture layout changed; update malformed offsets intentionally' }
                foreach ($case in 'kind','step','slot','motion','count','length','width','truncated','assist_bool','assist_disconnected','assist_downed','eliminated','prism_roster') {
                    [byte[]]$bad = $original.Clone()
                    switch ($case) {
                        'kind' { $bad[$attackOffset + 13] = 255 }
                        'step' { $bad[$attackOffset + 14] = 8 }
                        'slot' { $bad[$attackOffset + 15] = 255; $bad[$attackOffset + 16] = 0 }
                        'motion' { [BitConverter]::GetBytes([ulong]100).CopyTo($bad, $attackOffset + 17) }
                        'count' { $bad[$attackOffset + 25] = 5 }
                        'length' { [BitConverter]::GetBytes([float]::NaN).CopyTo($bad, $attackOffset + 42) }
                        'width' { [BitConverter]::GetBytes([float]1024).CopyTo($bad, $attackOffset + 46) }
                        'truncated' { $bad = $bad[0..($payloadEnd - 2)] }
                        'assist_bool' { $bad[5 + 55 + 61] = 2 }
                        'assist_disconnected' { $bad[5 + 55 + 62 + 2] = 0 }
                        'assist_downed' { $bad[5 + 55 + 62 + 3] = 1 }
                        'eliminated' { $bad[5 + 55 + 3] = 2 }
                        'prism_roster' {
                            $insert = $attackOffset + 26 + 24 * $rosterCount
                            $extended = [byte[]]::new($bad.Length + 24)
                            [Array]::Copy($bad,0,$extended,0,$insert)
                            [Array]::Copy($bad,$attackOffset+26,$extended,$insert,24)
                            [Array]::Copy($bad,$insert,$extended,$insert+24,$bad.Length-$insert)
                            $bad = $extended
                            $bad[$attackOffset+25] = 3
                        }
                    }
                    $badStream = [IO.MemoryStream]::new($bad)
                    $badStream.Position = 5
                    $badReader = [IO.BinaryReader]::new($badStream)
                    $badArgs = [object[]]@($badReader, [ulong]1, $fight, $null)
                    $rejected = $false
                    try { $rejected = -not $read.Invoke($null, $badArgs) }
                    catch {
                        if ($case -ne 'truncated' -or $_.Exception.InnerException -isnot [IO.EndOfStreamException]) { throw }
                        $rejected = $true # The packet router owns the EOF rejection boundary.
                    }
                    if (-not $rejected -or $null -ne $badArgs[3]) { throw "Malformed $case accepted" }
                    $badReader.Dispose()
                    $badStream.Dispose()
                    $invalidChecks++
                }
            }
            $reader.Dispose()
            $writer.Dispose()
            $stream.Dispose()
        }
    }
}
foreach ($rosterCount in 1, 2, 3, 4) {
    $members = [Array]::CreateInstance($assembly.GetType($feature + 'FirstSeveranceCombatParticipantProjection'), $rosterCount)
    for ($i = 0; $i -lt $rosterCount; $i++) {
        $id = New-Record ($ids + 'ParticipantId') @([byte]$i)
        $members.SetValue((New-Record ($feature + 'FirstSeveranceCombatParticipantProjection') @($id, [int]$i, $true, $alive, $false, [ulong]0, [ulong]0, [uint]1, [int]500, [float]4000, [float]3120, [ulong]0, [ulong]0, [ulong]3600, $false)), $i)
    }
    foreach ($pattern in -2, -1, 0, 1, 2, 3, 8, 9, 10, 11) {
        $counts = if ($pattern -ge 0 -and $pattern -le 3) { @(0, 1, $rosterCount) } else { @(0) }
        foreach ($coreCount in $counts) {
        $phaseName = if ($pattern -eq -2) { 'PhaseTransition' } else { 'Lattice' }
        $phase = Enum-Value ($feature + 'FirstSeveranceSubstate') $phaseName
        $beams = [Array]::CreateInstance($assembly.GetType($feature + 'FirstSeveranceLanceRay'), $coreCount)
        for ($i = 0; $i -lt $coreCount; $i++) {
            $beams.SetValue($aimCore.Invoke($null, @([float]4000, [float]4000, [float](4200 - $i * 230), [float](3300 + $i * 210))), $i)
        }
        $serial = if ($coreCount -eq 0) { [uint]2 } elseif ($coreCount -eq 1) { [uint]3 } else { [uint]17 }
        $grid = if ($pattern -ge 0) { New-Record ($feature + 'FirstSeveranceGridVolley') @($serial, [ulong]500, [byte]$pattern, [float]4000, [float]4000, $beams) } else { $null }
        $hp = Get-Field ($partyScaling.Invoke($null, @([int]$rosterCount))) 'BossLife'
        $projection = New-Record ($feature + 'FirstSeveranceCombatProjection') @([ulong]1, $fight, $phase, [ulong]1000, [int]0, [int]0, [int]($hp / 2), [int]$hp, [int]0, [uint]1, [float]4000, [float]3120, [float]4000, [float]4000, $result, [uint]0, $members, $null, $unbound, [ulong]100, $grid)
        $stream = [IO.MemoryStream]::new()
        $writer = [IO.BinaryWriter]::new($stream)
        $writer.Write([byte[]]@(11,12,13,14,15))
        $null = $write.Invoke($null, @($writer, $projection))
        $end = $stream.Position
        $writer.Write([byte[]]@(222,173,190,239))
        $stream.Position = 5
        $reader = [IO.BinaryReader]::new($stream)
        $argsRead = [object[]]@($reader, [ulong]1, $fight, $null)
        if (-not $read.Invoke($null, $argsRead) -or $stream.Position -ne $end) { throw 'Stage shared-buffer decode failed' }
        $decoded = $argsRead[3]
        if ($decoded.BossPhase -ne $unbound -or $decoded.BossPhaseStartedTick -ne 100 -or $decoded.StackY -ne 3120) { throw 'Stage epoch or anchor changed' }
        if ($decoded.BossMaximumLife -ne $hp -or $decoded.BossLife -ne $hp / 2) { throw 'Scaled health changed' }
        if ($null -ne $grid) {
            $actual = $decoded.GridVolley
            foreach ($name in 'Serial','StartTick','Pattern','FireTick','EndTick') {
                if ((Get-Field $actual $name) -ne (Get-Field $grid $name)) { throw "Changed grid $name" }
            }
            $rays = Get-Field $grid 'Rays'
            $actualRays = Get-Field $actual 'Rays'
            if ($rays.Count -ne $actualRays.Count) { throw 'Grid count changed' }
            for ($i = 0; $i -lt $rays.Count; $i++) {
                if (-not $rays[$i].Equals($actualRays[$i])) { throw 'Grid geometry drift' }
            }
            $actualBeams = Get-Field $actual 'CoreBeams'
            if ($actualBeams.Count -ne $coreCount) { throw 'Core salvo count drift' }
            for ($i = 0; $i -lt $coreCount; $i++) {
                if (-not $beams[$i].Equals($actualBeams[$i])) { throw 'Core salvo geometry drift' }
            }
        } elseif ($null -ne $decoded.GridVolley) { throw 'Unexpected cinematic grid' }
        $passed++
        if ($pattern -eq 0 -and $rosterCount -eq 2 -and $coreCount -eq $rosterCount) {
            $original = $stream.ToArray()
            $phaseOffset = 5 + 55 + 62 * $rosterCount + 1 # null Lance byte
            foreach ($case in 'phase', 'epoch', 'grid_bool', 'pattern', 'retired_stack', 'sanctuary_core', 'serial', 'grid_start', 'stack_nan', 'grid_truncated', 'core_count', 'core_early', 'core_nan', 'core_nonunit', 'core_roster') {
                [byte[]]$bad = $original.Clone()
                switch ($case) {
                    'phase' { $bad[$phaseOffset] = 255 }
                    'epoch' { [BitConverter]::GetBytes([ulong]0).CopyTo($bad, $phaseOffset + 1) }
                    'grid_bool' { $bad[$phaseOffset + 19] = 2 }
                    'pattern' { $bad[$phaseOffset + 32] = 12 }
                    'retired_stack' { $bad[$phaseOffset + 32] = 4 }
                    'sanctuary_core' { $bad[$phaseOffset + 32] = 8 }
                    'serial' { [BitConverter]::GetBytes([uint]0).CopyTo($bad, $phaseOffset + 20) }
                    'grid_start' { [BitConverter]::GetBytes([ulong]999).CopyTo($bad, $phaseOffset + 24) }
                    'stack_nan' { [BitConverter]::GetBytes([float]::NaN).CopyTo($bad, 5 + 33) }
                    'grid_truncated' { $bad = $bad[0..($end - 2)] }
                    'core_count' { $bad[$phaseOffset + 33] = 5 }
                    'core_early' { [BitConverter]::GetBytes([uint]2).CopyTo($bad, $phaseOffset + 20) }
                    'core_nan' { [BitConverter]::GetBytes([float]::NaN).CopyTo($bad, $phaseOffset + 34) }
                    'core_nonunit' { [BitConverter]::GetBytes([float]2).CopyTo($bad, $phaseOffset + 34) }
                    'core_roster' {
                        $extended = [byte[]]::new($bad.Length + 16)
                        [Array]::Copy($bad, 0, $extended, 0, $end)
                        [Array]::Copy($bad, $end, $extended, $end + 16, $bad.Length - $end)
                        [BitConverter]::GetBytes([float]1).CopyTo($extended, $end)
                        [BitConverter]::GetBytes([float]1).CopyTo($extended, $end + 8)
                        $bad = $extended
                        $bad[$phaseOffset + 33] = 4
                    }
                }
                $badStream = [IO.MemoryStream]::new($bad)
                $badStream.Position = 5
                $badReader = [IO.BinaryReader]::new($badStream)
                $badArgs = [object[]]@($badReader, [ulong]1, $fight, $null)
                $rejected = $false
                try { $rejected = -not $read.Invoke($null, $badArgs) }
                catch {
                    if ($case -ne 'grid_truncated' -or $_.Exception.InnerException -isnot [IO.EndOfStreamException]) { throw }
                    $rejected = $true
                }
                if (-not $rejected -or $null -ne $badArgs[3]) { throw "Malformed $case accepted" }
                $badReader.Dispose()
                $badStream.Dispose()
                $invalidChecks++
            }
        }
        $reader.Dispose(); $writer.Dispose(); $stream.Dispose()
        }
    }
}
# Fixed-score descriptors: every action of every phase, 1/2/3/4 members.
$scoreType = $assembly.GetType($feature + 'FirstSeveranceChoreography')
foreach ($count in 1,2,3,4) {
    $members = [Array]::CreateInstance($assembly.GetType($feature + 'FirstSeveranceCombatParticipantProjection'),$count)
    for ($i=0; $i -lt $count; $i++) {
        $id=New-Record ($ids+'ParticipantId') @([byte]$i)
        $members.SetValue((New-Record ($feature+'FirstSeveranceCombatParticipantProjection') @($id,[int]$i,$true,$alive,$false,[ulong]0,[ulong]0,[uint]1,[int]500,[float]4000,[float]3440,[ulong]0,[ulong]0,[ulong]0,$false)),$i)
    }
    foreach ($phaseName in 'Sealed','Unbound','Distant','Final') {
        $bossPhase=Enum-Value ($feature+'FirstSeveranceBossPhase') $phaseName
        $score=$scoreType.GetField($phaseName,$staticFlags).GetValue($null)
        for ($step=0; $step -lt $score.Count; $step++) {
            $action=$score[$step]
            $state=Get-Field $action 'State'
            $life=if ($phaseName -eq 'Final') {0} else {1250000}
            $projection=New-Record ($feature+'FirstSeveranceCombatProjection') @([ulong]1,$fight,$state,[ulong]1000,[int]0,[int]0,[int]$life,[int]5000000,[int]0,[uint]1,[float]4000,[float]3140,[float]4000,[float]4000,$result,[uint]0,$members,$null,$bossPhase,[ulong]100,$null,[ulong]400,[int]$step,[int]0)
            $stream=[IO.MemoryStream]::new(); $writer=[IO.BinaryWriter]::new($stream)
            $null=$write.Invoke($null,@($writer,$projection)); $end=$stream.Position
            $writer.Write([byte]234); $stream.Position=0; $reader=[IO.BinaryReader]::new($stream)
            $argsRead=[object[]]@($reader,[ulong]1,$fight,$null)
            if (-not $read.Invoke($null,$argsRead) -or $stream.Position -ne $end) {throw 'Score round-trip failed'}
            $decoded=$argsRead[3]
            if ($decoded.ActionIndex -ne $step -or $decoded.ActionStartedTick -ne 400 -or $decoded.BossPhase -ne $bossPhase -or $decoded.BossLife -ne $life) {throw 'Score identity/timing changed'}
            $passed++
            if ($count -eq 2 -and $step -eq 0) {
                $bytes=$stream.ToArray(); $offset=55+62*$count+1
                foreach($case in 'unknown_step','negative_step','future_start','state_mismatch') {
                    $bad=[byte[]]$bytes.Clone()
                    switch($case) {
                        'unknown_step' {$bad[$offset+17]=120}
                        'negative_step' {$bad[$offset+17]=254}
                        'future_start' {[BitConverter]::GetBytes([ulong]1000).CopyTo($bad,$offset+9)}
                        'state_mismatch' {$bad[0]=13}
                    }
                    $badStream=[IO.MemoryStream]::new($bad); $badReader=[IO.BinaryReader]::new($badStream)
                    $badArgs=[object[]]@($badReader,[ulong]1,$fight,$null)
                    if ($read.Invoke($null,$badArgs) -or $null -ne $badArgs[3]) {throw "Score accepted $case"}
                    $invalidChecks++; $badReader.Dispose(); $badStream.Dispose()
                }
            }
            $reader.Dispose(); $writer.Dispose(); $stream.Dispose()
        }
    }
}
# Preparation must round-trip one actual member as well, including manual Ready.
$writePrep = $codec.GetMethod('WritePreparation', $staticFlags)
$readPrep = $codec.GetMethod('TryReadPreparation', $staticFlags)
$geometry = 'Convergence.Common.Foundation.Geometry.'
$blueprint = $assembly.GetType($feature + 'FirstSeveranceArenaBlueprint')
$arenaWidth = $blueprint.GetField('WidthInTiles', $staticFlags).GetRawConstantValue()
$arenaHeight = $blueprint.GetField('HeightInTiles', $staticFlags).GetRawConstantValue()
foreach ($count in 1,2,3,4) {
    foreach ($ready in $false,$true) {
        $members = [Array]::CreateInstance($assembly.GetType($feature + 'FirstSeverancePreparationMemberSnapshot'), $count)
        for ($i=0; $i -lt $count; $i++) {
            $id=New-Record ($ids + 'ParticipantId') @([byte]$i)
            $members.SetValue((New-Record ($feature + 'FirstSeverancePreparationMemberSnapshot') @($id,[int]$i,[ulong](100+$i),[bool]$ready)),$i)
        }
        $point=New-Record ($geometry+'TilePoint') @([int]4000,[int]4000)
        $rectangle=New-Record ($geometry+'TileRectangle') @([int]3900,[int]3930,[int]$arenaWidth,[int]$arenaHeight)
        $projection=New-Record ($feature+'FirstSeverancePreparationProjection') @([ulong]1,$fight,[int]7,$point,$rectangle,[ulong]100,[ulong]1000,$true,$members)
        $stream=[IO.MemoryStream]::new(); $writer=[IO.BinaryWriter]::new($stream)
        $writer.Write([byte[]]@(11,12,13,14,15)); $null=$writePrep.Invoke($null,@($writer,$projection)); $end=$stream.Position
        $writer.Write([byte]234); $stream.Position=5; $reader=[IO.BinaryReader]::new($stream)
        $argsRead=[object[]]@($reader,[ulong]1,$fight,$null)
        if (-not $readPrep.Invoke($null,$argsRead) -or $stream.Position -ne $end) {throw 'Preparation shared-buffer round-trip failed'}
        if ($argsRead[3].Members.Count -ne $count -or $argsRead[3].AreAllReady -ne $ready) {throw 'Preparation roster or Ready changed'}
        $passed++
        if ($count -eq 1 -and -not $ready) {
            $bytes=$stream.ToArray()
            foreach ($case in 'zero_members','too_many_members','stale_epoch','bad_slot','bad_ready','truncated') {
                $bad=[byte[]]$bytes.Clone()
                switch ($case) {
                    'zero_members' {$bad[5+45]=0}
                    'too_many_members' {$bad[5+45]=5}
                    'stale_epoch' {[BitConverter]::GetBytes([ulong]0).CopyTo($bad,5+48)}
                    'bad_slot' {$bad[5+47]=255}
                    'bad_ready' {$bad[5+56]=2}
                    'truncated' {$bad=$bad[0..($end-2)]}
                }
                $badStream=[IO.MemoryStream]::new($bad); $badStream.Position=5
                $badReader=[IO.BinaryReader]::new($badStream); $badArgs=[object[]]@($badReader,[ulong]1,$fight,$null)
                $rejected=$false
                try {$rejected=-not $readPrep.Invoke($null,$badArgs)}
                catch {
                    if ($case -ne 'truncated' -or $_.Exception.InnerException -isnot [IO.EndOfStreamException]) {throw}
                    $rejected=$true
                }
                if (-not $rejected -or $null -ne $badArgs[3]) {throw "Preparation accepted $case"}
                $invalidChecks++; $badReader.Dispose(); $badStream.Dispose()
            }
        }
        $reader.Dispose(); $writer.Dispose(); $stream.Dispose()
    }
}
Write-Output "Assembly: $assemblyFile; SHA256=$((Get-FileHash -LiteralPath $assemblyFile -Algorithm SHA256).Hash)"
Write-Output "PASS compiled protocol ${version}: $passed round-trips; $invalidChecks malformed/truncated cases rejected. Simultaneous 1-4-ray Prisms, untimed Down, eliminated/oversized roster rejection, every phase/action, HP-zero Final and shared-buffer boundaries. Compiled solo flag=$soloFlag."
