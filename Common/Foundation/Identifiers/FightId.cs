using System;

namespace Convergence.Common.Foundation.Identifiers;

internal readonly record struct FightId(Guid Value)
{
    public static FightId None => new(Guid.Empty);

    public bool IsNone => Value == Guid.Empty;

    internal static FightId CreateForAuthority()
    {
        return new FightId(Guid.NewGuid());
    }

    internal static FightId FromWire(Guid value)
    {
        return new FightId(value);
    }
}

