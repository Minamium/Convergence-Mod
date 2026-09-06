using System;
using System.IO;
using System.Text;

namespace Convergence.Common.Networking.Protocol;

// A bounded definition selector, not authority or a client-supplied session identity.
internal static class EncounterRouteCodec
{
    internal const int MaximumKeyBytes = 64;

    internal static bool IsValidKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length > MaximumKeyBytes) return false;
        foreach (char value in key)
            if (!(value is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')) return false;
        return true;
    }

    internal static void Write(BinaryWriter writer, string key)
    {
        // Empty is reserved for the common Idle snapshot, never a registered feature.
        if (key.Length != 0 && !IsValidKey(key)) throw new ArgumentException("Invalid encounter route.", nameof(key));
        writer.Write((byte)key.Length);
        writer.Write(Encoding.ASCII.GetBytes(key));
    }

    internal static bool TryRead(BinaryReader reader, out string key)
    {
        key = string.Empty;
        try
        {
            int length = reader.ReadByte();
            if (length > MaximumKeyBytes) return false;
            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length != length) return false;
            foreach (byte value in bytes)
                if (value > 127) return false;
            key = Encoding.ASCII.GetString(bytes);
            return length == 0 || IsValidKey(key);
        }
        catch (IOException) { return false; }
    }

    internal static void WriteHeader(BinaryWriter writer, in EncounterPacketHeader header, string key)
    {
        EncounterPacketCodec.WriteHeader(writer, header);
        Write(writer, key);
    }
}
