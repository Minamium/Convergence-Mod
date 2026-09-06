#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Protocol;
using Convergence.Common.Raids.Revive;

namespace Convergence.Content.Encounters.FirstSeverance;

internal readonly record struct FirstSeveranceValidationMessage(
    uint RequestNonce,
    bool IsAccepted,
    string FailureCode);

internal static class FirstSeverancePacketCodec
{
    private const int MaximumFailureCodeBytes = 120;

    internal static void WriteActivateRequest(
        BinaryWriter writer,
        in EncounterPacketHeader header,
        in TilePoint requestedAnchor,
        uint requestNonce)
    {
        EncounterPacketCodec.WriteHeader(writer, header);
        writer.Write(requestedAnchor.X);
        writer.Write(requestedAnchor.Y);
        writer.Write(requestNonce);
    }

    internal static bool TryReadActivateRequest(
        BinaryReader reader,
        out TilePoint requestedAnchor,
        out uint requestNonce,
        out string failureCode)
    {
        requestedAnchor = new TilePoint(reader.ReadInt32(), reader.ReadInt32());
        requestNonce = reader.ReadUInt32();
        if (requestNonce == 0)
        {
            failureCode = "first_severance.activate_nonce_invalid";
            return false;
        }

        failureCode = string.Empty;
        return true;
    }

    internal static void WriteReadyRequest(
        BinaryWriter writer,
        in EncounterPacketHeader header,
        bool isReady,
        uint requestNonce)
    {
        EncounterPacketCodec.WriteHeader(writer, header);
        WriteBoolean(writer, isReady);
        writer.Write(requestNonce);
    }

    internal static bool TryReadReadyRequest(
        BinaryReader reader,
        out bool isReady,
        out uint requestNonce,
        out string failureCode)
    {
        bool validReady = TryReadBoolean(reader, out isReady);
        requestNonce = reader.ReadUInt32();
        if (!validReady)
        {
            failureCode = "first_severance.ready_value_invalid";
            return false;
        }

        if (requestNonce == 0)
        {
            failureCode = "first_severance.ready_nonce_invalid";
            return false;
        }

        failureCode = string.Empty;
        return true;
    }

    internal static void WriteCancelRequest(
        BinaryWriter writer,
        in EncounterPacketHeader header,
        uint requestNonce)
    {
        EncounterPacketCodec.WriteHeader(writer, header);
        writer.Write(requestNonce);
    }

    internal static bool TryReadCancelRequest(
        BinaryReader reader,
        out uint requestNonce,
        out string failureCode)
    {
        requestNonce = reader.ReadUInt32();
        if (requestNonce == 0)
        {
            failureCode = "first_severance.cancel_nonce_invalid";
            return false;
        }

        failureCode = string.Empty;
        return true;
    }

    internal static void WritePrototypeDownRequest(
        BinaryWriter writer,
        in EncounterPacketHeader header,
        uint requestNonce)
    {
        EncounterPacketCodec.WriteHeader(writer, header);
        writer.Write(requestNonce);
    }

    internal static bool TryReadPrototypeDownRequest(
        BinaryReader reader,
        out uint requestNonce,
        out string failureCode)
    {
        // tML supplies a shared receive stream, not a packet-sized stream.
        // Read only our fixed field; the router handles truncated reads.
        requestNonce = reader.ReadUInt32();
        if (requestNonce == 0)
        {
            failureCode = "first_severance.prototype_down_nonce_invalid";
            return false;
        }

        failureCode = string.Empty;
        return true;
    }

    internal static void WriteReviveNearestRequest(
        BinaryWriter writer,
        in EncounterPacketHeader header,
        uint requestNonce)
    {
        EncounterPacketCodec.WriteHeader(writer, header);
        writer.Write(requestNonce);
    }

    internal static bool TryReadReviveNearestRequest(
        BinaryReader reader,
        out uint requestNonce,
        out string failureCode)
    {
        // Do not use BaseStream.Length as the ModPacket boundary.
        requestNonce = reader.ReadUInt32();
        if (requestNonce == 0)
        {
            failureCode = "first_severance.revive_nonce_invalid";
            return false;
        }

        failureCode = string.Empty;
        return true;
    }

    internal static void WriteSnapshot(
        BinaryWriter writer,
        in EncounterSnapshot snapshot,
        FirstSeverancePreparationProjection? preparation,
        FirstSeveranceCombatProjection? combat)
    {
        var header = new EncounterPacketHeader(
            EncounterProtocol.CurrentVersion,
            EncounterPacketType.Snapshot,
            snapshot.EncounterSequence,
            snapshot.FightId,
            snapshot.Revision);
        EncounterPacketCodec.WriteHeader(writer, header);
        writer.Write((byte)snapshot.Lifecycle);
        writer.Write(snapshot.AuthorityTick);
        writer.Write(snapshot.LifecycleEnteredTick);
        writer.Write(snapshot.ActiveFightTick);
        WriteTermination(writer, snapshot.Termination);

        bool hasPreparation = preparation is not null
            && preparation.EncounterSequence == snapshot.EncounterSequence
            && preparation.FightId == snapshot.FightId
            && snapshot.Lifecycle == EncounterLifecycle.Preparing;
        WriteBoolean(writer, hasPreparation);
        if (hasPreparation)
        {
            WritePreparation(writer, preparation!);
        }

        bool hasCombat = combat is not null
            && combat.EncounterSequence == snapshot.EncounterSequence
            && combat.FightId == snapshot.FightId
            && snapshot.Lifecycle == EncounterLifecycle.Active;
        WriteBoolean(writer, hasCombat);
        if (hasCombat)
        {
            WriteCombat(writer, combat!);
        }
    }

    internal static bool TryReadSnapshot(
        BinaryReader reader,
        in EncounterPacketHeader header,
        out EncounterSnapshot snapshot,
        out FirstSeverancePreparationProjection? preparation,
        out FirstSeveranceCombatProjection? combat,
        out string failureCode)
    {
        snapshot = default;
        preparation = null;
        combat = null;
        var lifecycle = (EncounterLifecycle)reader.ReadByte();
        ulong authorityTick = reader.ReadUInt64();
        ulong lifecycleEnteredTick = reader.ReadUInt64();
        ulong activeFightTick = reader.ReadUInt64();
        if (!Enum.IsDefined(lifecycle)
            || !TryReadTermination(reader, out EncounterTerminationDescriptor termination)
            || !TryReadBoolean(reader, out bool hasPreparation))
        {
            failureCode = "first_severance.snapshot_header_invalid";
            return false;
        }

        string definitionKey = lifecycle == EncounterLifecycle.Idle
            ? string.Empty
            : FirstSeveranceDefinition.EncounterKey;
        snapshot = new EncounterSnapshot(
            header.EncounterSequence,
            header.FightId,
            definitionKey,
            lifecycle,
            header.Revision,
            authorityTick,
            lifecycleEnteredTick,
            activeFightTick,
            termination);

        if (hasPreparation
            && (lifecycle != EncounterLifecycle.Preparing
                || !TryReadPreparation(
                    reader,
                    header.EncounterSequence,
                    header.FightId,
                    out preparation)))
        {
            snapshot = default;
            preparation = null;
            failureCode = "first_severance.snapshot_preparation_invalid";
            return false;
        }

        if (!TryReadBoolean(reader, out bool hasCombat))
        {
            snapshot = default;
            preparation = null;
            failureCode = "first_severance.snapshot_combat_marker_invalid";
            return false;
        }

        if (hasCombat
            && (lifecycle != EncounterLifecycle.Active
                || !TryReadCombat(
                reader,
                header.EncounterSequence,
                header.FightId,
                out combat)))
        {
            snapshot = default;
            preparation = null;
            combat = null;
            failureCode = "first_severance.snapshot_combat_invalid";
            return false;
        }

        failureCode = string.Empty;
        return true;
    }

    private static void WriteCombat(
        BinaryWriter writer,
        FirstSeveranceCombatProjection combat)
    {
        writer.Write((byte)combat.Substate);
        writer.Write(combat.ResolveTick);
        writer.Write(combat.ZeroBasedLoopIndex);
        writer.Write(combat.RemainingPylons);
        writer.Write(combat.BossLife);
        writer.Write(combat.BossMaximumLife);
        writer.Write(combat.RemainingReviveTokens);
        writer.Write(combat.ReviveRevision);
        writer.Write(combat.StackX);
        writer.Write(combat.StackY);
        writer.Write(combat.CoreX);
        writer.Write(combat.CoreY);
        writer.Write((byte)combat.LastMechanicResult);
        writer.Write(combat.MechanicRevision);
        writer.Write(checked((byte)combat.Participants.Count));
        for (int index = 0; index < combat.Participants.Count; index++)
        {
            FirstSeveranceCombatParticipantProjection participant = combat.Participants[index];
            writer.Write(participant.ParticipantId.Value);
            writer.Write(checked((byte)participant.ServerWhoAmI));
            WriteBoolean(writer, participant.IsConnected);
            writer.Write((byte)participant.CombatState);
            WriteBoolean(writer, participant.IsReviving);
            writer.Write(participant.DownedDeadlineTick);
            writer.Write(participant.ReviveCompletesTick);
            writer.Write(participant.HealthRevision);
            writer.Write(participant.Life);
            writer.Write(participant.AnchorX);
            writer.Write(participant.AnchorY);
            writer.Write(participant.InvulnerabilityUntilTick);
            writer.Write(participant.WeaknessUntilTick);
            writer.Write(participant.ReviveLockoutUntilTick);
            WriteBoolean(writer, participant.DebugAssistProtected);
        }
        WriteBoolean(writer, combat.LanceVolley is not null);
        if (combat.LanceVolley is { } volley)
        {
            writer.Write(volley.Serial);
            writer.Write(volley.StartTick);
            writer.Write((byte)volley.Kind);
            writer.Write(volley.Step);
            writer.Write((short)volley.TargetSlot);
            writer.Write(volley.MotionTick);
            writer.Write(checked((byte)volley.Rays.Count));
            foreach (FirstSeveranceLanceRay ray in volley.Rays)
            {
                writer.Write(ray.X);
                writer.Write(ray.Y);
                writer.Write(ray.DirectionX);
                writer.Write(ray.DirectionY);
                writer.Write(ray.Length);
                writer.Write(ray.HalfWidth);
            }
        }
        writer.Write((byte)combat.BossPhase);
        writer.Write(combat.BossPhaseStartedTick);
        writer.Write(combat.ActionStartedTick);
        writer.Write(checked((sbyte)combat.ActionIndex));
        writer.Write(checked((byte)combat.CompletedPhaseCycles));
        WriteBoolean(writer, combat.GridVolley is not null);
        if (combat.GridVolley is { } grid)
        {
            writer.Write(grid.Serial);
            writer.Write(grid.StartTick);
            writer.Write(grid.Pattern);
            writer.Write(checked((byte)grid.CoreBeams.Count));
            foreach (var ray in grid.CoreBeams)
            {
                writer.Write(ray.DirectionX);
                writer.Write(ray.DirectionY);
            }
        }
    }

    private static bool TryReadCombat(
        BinaryReader reader,
        ulong encounterSequence,
        FightId fightId,
        out FirstSeveranceCombatProjection? combat)
    {
        combat = null;
        var substate = (FirstSeveranceSubstate)reader.ReadByte();
        ulong resolveTick = reader.ReadUInt64();
        int zeroBasedLoopIndex = reader.ReadInt32();
        int remainingPylons = reader.ReadInt32();
        int bossLife = reader.ReadInt32();
        int bossMaximumLife = reader.ReadInt32();
        int remainingReviveTokens = reader.ReadInt32();
        uint reviveRevision = reader.ReadUInt32();
        float stackX = reader.ReadSingle();
        float stackY = reader.ReadSingle();
        float coreX = reader.ReadSingle();
        float coreY = reader.ReadSingle();
        var mechanicResult = (FirstSeveranceMechanicResult)reader.ReadByte();
        uint mechanicRevision = reader.ReadUInt32();
        int participantCount = reader.ReadByte();
        if (participantCount is < FirstSeveranceRoster.MinimumCount
            or > FirstSeveranceRoster.MaximumCount)
        {
            return false;
        }

        var participants = new FirstSeveranceCombatParticipantProjection[participantCount];
        var seenSlots = new HashSet<int>();
        var seenParticipants = new HashSet<ParticipantId>();
        for (int index = 0; index < participantCount; index++)
        {
            var participantId = new ParticipantId(reader.ReadByte());
            int serverWhoAmI = reader.ReadByte();
            if (!TryReadBoolean(reader, out bool isConnected))
                return false;
            var combatState = (RaidParticipantCombatState)reader.ReadByte();
            if (!TryReadBoolean(reader, out bool isReviving))
            {
                return false;
            }

            ulong downedDeadlineTick = reader.ReadUInt64();
            ulong reviveCompletesTick = reader.ReadUInt64();
            uint healthRevision = reader.ReadUInt32();
            int life = reader.ReadInt32();
            float anchorX = reader.ReadSingle();
            float anchorY = reader.ReadSingle();
            ulong invulnerabilityUntilTick = reader.ReadUInt64();
            ulong weaknessUntilTick = reader.ReadUInt64();
            ulong reviveLockoutUntilTick = reader.ReadUInt64();
            if (!TryReadBoolean(reader, out bool debugAssistProtected)
                || (debugAssistProtected && (!isConnected || combatState != RaidParticipantCombatState.Alive)))
                return false;
            if (!participantId.IsValid
                || participantId.Value >= participantCount || serverWhoAmI >= 255
                || !Enum.IsDefined(combatState)
                || life < 1 || !float.IsFinite(anchorX) || !float.IsFinite(anchorY)
                || !seenSlots.Add(serverWhoAmI)
                || !seenParticipants.Add(participantId))
            {
                return false;
            }

            participants[index] = new FirstSeveranceCombatParticipantProjection(
                participantId,
                serverWhoAmI,
                isConnected,
                combatState,
                isReviving,
                downedDeadlineTick,
                reviveCompletesTick,
                healthRevision,
                life,
                anchorX,
                anchorY,
                invulnerabilityUntilTick,
                weaknessUntilTick,
                reviveLockoutUntilTick,
                debugAssistProtected);
        }

        if (!TryReadBoolean(reader, out bool hasLance))
            return false;
        FirstSeveranceLanceVolley? lance = null;
        if (hasLance)
        {
            uint serial = reader.ReadUInt32();
            ulong startTick = reader.ReadUInt64();
            var kind = (FirstSeveranceAttackKind)reader.ReadByte();
            byte step = reader.ReadByte();
            int targetSlot = reader.ReadInt16();
            ulong motionTick = reader.ReadUInt64();
            int rayCount = reader.ReadByte();
            if (rayCount is < 1 or > FirstSeveranceLanceTuning.MaximumRays)
                return false;
            var rays = new FirstSeveranceLanceRay[rayCount];
            for (int index = 0; index < rayCount; index++)
                rays[index] = new FirstSeveranceLanceRay(reader.ReadSingle(), reader.ReadSingle(),
                    reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            try
            {
                lance = new FirstSeveranceLanceVolley(serial, startTick, Array.AsReadOnly(rays), kind, step, targetSlot, motionTick);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        var bossPhase = (FirstSeveranceBossPhase)reader.ReadByte();
        ulong bossPhaseStartedTick = reader.ReadUInt64();
        ulong actionStartedTick = reader.ReadUInt64();
        int actionIndex = reader.ReadSByte();
        int completedPhaseCycles = reader.ReadByte();
        if (!TryReadBoolean(reader, out bool hasGrid)) return false;
        FirstSeveranceGridVolley? grid = null;
        if (hasGrid)
        {
            uint serial = reader.ReadUInt32();
            ulong startTick = reader.ReadUInt64();
            byte pattern = reader.ReadByte();
            byte count = reader.ReadByte();
            if (count > FirstSeveranceGridVolley.MaximumCoreBeams) return false;
            var beams = new FirstSeveranceLanceRay[count];
            for (int index = 0; index < count; index++)
                beams[index] = new(coreX, coreY - FirstSeveranceLanceTuning.BossHeightAboveCore,
                    reader.ReadSingle(), reader.ReadSingle(), FirstSeveranceGridVolley.CoreBeamLength,
                    FirstSeveranceGridVolley.CoreBeamHalfWidth);
            try { grid = new(serial, startTick, pattern, coreX, coreY, beams); }
            catch (ArgumentException) { return false; }
        }
        try
        {
            combat = new FirstSeveranceCombatProjection(
                encounterSequence,
                fightId,
                substate,
                resolveTick,
                zeroBasedLoopIndex,
                remainingPylons,
                bossLife,
                bossMaximumLife,
                remainingReviveTokens,
                reviveRevision,
                stackX,
                stackY,
                coreX,
                coreY,
                mechanicResult,
                mechanicRevision,
                Array.AsReadOnly(participants),
                lance, bossPhase, bossPhaseStartedTick, grid, actionStartedTick, actionIndex, completedPhaseCycles);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    internal static void WriteValidation(
        BinaryWriter writer,
        in EncounterSnapshot authoritySnapshot,
        in FirstSeveranceValidationMessage validation)
    {
        var header = new EncounterPacketHeader(
            EncounterProtocol.CurrentVersion,
            EncounterPacketType.ValidationResult,
            authoritySnapshot.EncounterSequence,
            authoritySnapshot.FightId,
            authoritySnapshot.Revision);
        EncounterPacketCodec.WriteHeader(writer, header);
        writer.Write(validation.RequestNonce);
        WriteBoolean(writer, validation.IsAccepted);
        WriteBoundedString(writer, validation.FailureCode);
    }

    internal static bool TryReadValidation(
        BinaryReader reader,
        out FirstSeveranceValidationMessage validation,
        out string failureCode)
    {
        uint requestNonce = reader.ReadUInt32();
        if (requestNonce == 0
            || !TryReadBoolean(reader, out bool isAccepted)
            || !TryReadBoundedString(reader, out string code)
            || (isAccepted && code.Length != 0)
            || (!isAccepted && string.IsNullOrWhiteSpace(code)))
        {
            validation = default;
            failureCode = "first_severance.validation_payload_invalid";
            return false;
        }

        validation = new FirstSeveranceValidationMessage(requestNonce, isAccepted, code);
        failureCode = string.Empty;
        return true;
    }

    private static void WritePreparation(
        BinaryWriter writer,
        FirstSeverancePreparationProjection preparation)
    {
        writer.Write(preparation.ServerTileEntityId);
        writer.Write(preparation.CoreTopLeft.X);
        writer.Write(preparation.CoreTopLeft.Y);
        writer.Write(preparation.ArenaBounds.Left);
        writer.Write(preparation.ArenaBounds.Top);
        writer.Write(preparation.ArenaBounds.Width);
        writer.Write(preparation.ArenaBounds.Height);
        writer.Write(preparation.EnteredTick);
        writer.Write(preparation.DeadlineTick);
        WriteBoolean(writer, preparation.CombatGateClosed);
        writer.Write(checked((byte)preparation.Members.Count));
        for (int index = 0; index < preparation.Members.Count; index++)
        {
            FirstSeverancePreparationMemberSnapshot member = preparation.Members[index];
            writer.Write(member.ParticipantId.Value);
            writer.Write(checked((byte)member.ServerWhoAmI));
            writer.Write(member.ConnectionEpoch);
            WriteBoolean(writer, member.IsReady);
        }
    }

    private static bool TryReadPreparation(
        BinaryReader reader,
        ulong encounterSequence,
        FightId fightId,
        out FirstSeverancePreparationProjection? preparation)
    {
        preparation = null;
        int serverTileEntityId = reader.ReadInt32();
        var topLeft = new TilePoint(reader.ReadInt32(), reader.ReadInt32());
        var arenaBounds = new TileRectangle(
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32());
        ulong enteredTick = reader.ReadUInt64();
        ulong deadlineTick = reader.ReadUInt64();
        if (!TryReadBoolean(reader, out bool combatGateClosed))
        {
            return false;
        }

        int memberCount = reader.ReadByte();
        if (memberCount is < FirstSeveranceRoster.MinimumCount
            or > FirstSeveranceRoster.MaximumCount)
        {
            return false;
        }

        var members = new FirstSeverancePreparationMemberSnapshot[memberCount];
        for (int index = 0; index < memberCount; index++)
        {
            var participantId = new ParticipantId(reader.ReadByte());
            int serverWhoAmI = reader.ReadByte();
            ulong connectionEpoch = reader.ReadUInt64();
            if (!TryReadBoolean(reader, out bool isReady))
            {
                return false;
            }

            members[index] = new FirstSeverancePreparationMemberSnapshot(
                participantId,
                serverWhoAmI,
                connectionEpoch,
                isReady);
        }

        try
        {
            preparation = new FirstSeverancePreparationProjection(
                encounterSequence,
                fightId,
                serverTileEntityId,
                topLeft,
                arenaBounds,
                enteredTick,
                deadlineTick,
                combatGateClosed,
                Array.AsReadOnly(members));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static void WriteTermination(
        BinaryWriter writer,
        in EncounterTerminationDescriptor termination)
    {
        writer.Write((byte)termination.EndReason);
        writer.Write(termination.Feature.SchemaId);
        writer.Write(termination.Feature.SchemaVersion);
        writer.Write(termination.Feature.Cause);
    }

    private static bool TryReadTermination(
        BinaryReader reader,
        out EncounterTerminationDescriptor termination)
    {
        var endReason = (EncounterEndReason)reader.ReadByte();
        ushort schemaId = reader.ReadUInt16();
        byte schemaVersion = reader.ReadByte();
        byte cause = reader.ReadByte();
        if (endReason == EncounterEndReason.None
            && schemaId == 0
            && schemaVersion == 0
            && cause == 0)
        {
            termination = EncounterTerminationDescriptor.None;
            return true;
        }

        if (!Enum.IsDefined(endReason))
        {
            termination = default;
            return false;
        }

        try
        {
            EncounterFeatureTermination feature = EncounterFeatureTermination.Create(
                schemaId,
                schemaVersion,
                cause);
            termination = EncounterTerminationDescriptor.Create(endReason, feature);
            return FirstSeveranceTerminationContract.Instance.IsValid(termination);
        }
        catch (ArgumentException)
        {
            termination = default;
            return false;
        }
    }

    private static void WriteBoolean(BinaryWriter writer, bool value)
    {
        writer.Write(value ? (byte)1 : (byte)0);
    }

    private static bool TryReadBoolean(BinaryReader reader, out bool value)
    {
        byte raw = reader.ReadByte();
        value = raw == 1;
        return raw <= 1;
    }

    private static void WriteBoundedString(BinaryWriter writer, string value)
    {
        byte[] encoded = Encoding.UTF8.GetBytes(value ?? string.Empty);
        if (encoded.Length > MaximumFailureCodeBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        writer.Write(checked((byte)encoded.Length));
        writer.Write(encoded);
    }

    private static bool TryReadBoundedString(BinaryReader reader, out string value)
    {
        int length = reader.ReadByte();
        if (length > MaximumFailureCodeBytes)
        {
            value = string.Empty;
            return false;
        }

        byte[] bytes = reader.ReadBytes(length);
        if (bytes.Length != length)
        {
            value = string.Empty;
            return false;
        }

        value = Encoding.UTF8.GetString(bytes);
        return Encoding.UTF8.GetByteCount(value) == length;
    }
}
