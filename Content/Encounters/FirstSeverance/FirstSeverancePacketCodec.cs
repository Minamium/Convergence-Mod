#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Geometry;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Common.Networking.Protocol;

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
        if (!TryReadBoolean(reader, out isReady))
        {
            requestNonce = 0;
            failureCode = "first_severance.ready_value_invalid";
            return false;
        }

        requestNonce = reader.ReadUInt32();
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

    internal static void WriteSnapshot(
        BinaryWriter writer,
        in EncounterSnapshot snapshot,
        FirstSeverancePreparationProjection? preparation)
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
        if (!hasPreparation)
        {
            return;
        }

        WritePreparation(writer, preparation!);
    }

    internal static bool TryReadSnapshot(
        BinaryReader reader,
        in EncounterPacketHeader header,
        out EncounterSnapshot snapshot,
        out FirstSeverancePreparationProjection? preparation,
        out string failureCode)
    {
        snapshot = default;
        preparation = null;
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

        if (!hasPreparation)
        {
            failureCode = string.Empty;
            return true;
        }

        if (lifecycle != EncounterLifecycle.Preparing
            || !TryReadPreparation(
                reader,
                header.EncounterSequence,
                header.FightId,
                out preparation))
        {
            snapshot = default;
            preparation = null;
            failureCode = "first_severance.snapshot_preparation_invalid";
            return false;
        }

        failureCode = string.Empty;
        return true;
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
