using System;
using Microsoft.Xna.Framework;

namespace Convergence.Client.Encounters.CrimsonFoundry;

internal readonly record struct ScarletPartPose(Vector2 Offset, float Rotation, Vector2 Scale);

// Fractional presentation only. The root/collision path stays with the accepted
// gesture; cloth and bony extremities load, hesitate, snap and settle around it.
internal static class ScarletRigMotion
{
    internal static ScarletPartPose Part(int species, int part, float age, float charge, float kick, bool reduced)
    {
        if (species == 3) return new(Vector2.Zero, 0, Vector2.One);
        float strength = reduced ? .15f : 1;
        float load = (.28f * CrimsonRigMotion.Ease(charge / .55f)
            + .72f * MathF.Pow(CrimsonRigMotion.Ease((charge - .55f) / .45f), 2)) * strength;
        float breath = reduced ? 0 : MathF.Sin(age * (species == 0 ? .019f : species == 1 ? .041f : .027f) + species * 1.7f);
        kick *= strength;
        if (part == 0)
            return new(new(0, -load * 3 + kick * 4), breath * .009f,
                new(1 + breath * .008f - kick * .018f, 1 - breath * .006f + load * .01f));
        float side = part is 1 or 3 ? -1 : 1, lower = part >= 3 ? 1 : 0;
        float flutter = reduced ? 0 : MathF.Sin(age * .043f - part * 1.7f) + .3f * MathF.Sin(age * .079f + part);
        return species switch
        {
            0 => new(new(side * load * (10 + lower * 5), lower * (breath * 6 + kick * 9)),
                side * (load * .07f - kick * .12f + lower * breath * .025f), new(1 + load * .045f, 1 - kick * .025f)),
            1 => new(new(side * (load * (15 + lower * 8) + breath * 8), -load * 10 + flutter * (3 + lower * 4)),
                side * (-load * .22f + kick * .28f + flutter * .082f), new(1 + load * .08f, 1)),
            _ => new(new(side * (flutter * 6 + load * 10), lower * (load * 13 - kick * 16) + flutter * 3),
                side * (load * .17f + flutter * .062f - kick * .21f), new(1, 1 + load * .055f))
        };
    }
}
