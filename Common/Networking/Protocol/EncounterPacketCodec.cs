using System;
using System.IO;
using Convergence.Common.Foundation.Identifiers;

namespace Convergence.Common.Networking.Protocol;

internal static class EncounterPacketCodec
{
    private const int FightIdByteCount = 16;

    public static bool TryReadHeader(
        BinaryReader reader,
        out EncounterPacketHeader header,
        out string failureCode)
    {
        header = default;
        failureCode = string.Empty;

        try
        {
            ushort protocolVersion = reader.ReadUInt16();
            byte rawPacketType = reader.ReadByte();
            ulong encounterSequence = reader.ReadUInt64();
            byte[] fightIdBytes = reader.ReadBytes(FightIdByteCount);
            uint revision = reader.ReadUInt32();

            if (fightIdBytes.Length != FightIdByteCount)
            {
                failureCode = "packet.truncated_fight_id";
                return false;
            }

            header = new EncounterPacketHeader(
                protocolVersion,
                (EncounterPacketType)rawPacketType,
                encounterSequence,
                FightId.FromWire(new Guid(fightIdBytes)),
                revision);
            return true;
        }
        catch (EndOfStreamException)
        {
            failureCode = "packet.truncated_header";
            return false;
        }
        catch (IOException)
        {
            failureCode = "packet.io_failure";
            return false;
        }
    }

    public static void WriteHeader(
        BinaryWriter writer,
        in EncounterPacketHeader header)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(header.ProtocolVersion);
        writer.Write((byte)header.PacketType);
        writer.Write(header.EncounterSequence);
        writer.Write(header.FightId.Value.ToByteArray());
        writer.Write(header.Revision);
    }
}
