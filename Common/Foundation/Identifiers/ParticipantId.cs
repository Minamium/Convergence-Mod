namespace Convergence.Common.Foundation.Identifiers;

internal readonly record struct ParticipantId(byte Value)
{
    public const byte InvalidValue = byte.MaxValue;

    public static ParticipantId Invalid => new(InvalidValue);

    public bool IsValid => Value != InvalidValue;
}

