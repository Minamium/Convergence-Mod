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
        float breath = reduced ? 0 : MathF.Sin(age * .034f + species * 1.7f);
        kick *= strength;
        if (part == 0)
            return new(new(0, -load * 3 + kick * 4), breath * .009f,
                new(1 + breath * .008f - kick * .018f, 1 - breath * .006f + load * .01f));
        float side = part is 1 or 3 ? -1 : 1, lower = part >= 3 ? 1 : 0;
        float flutter = reduced ? 0 : MathF.Sin(age * .043f - part * 1.7f) + .3f * MathF.Sin(age * .079f + part);
        return species switch
        {
            0 => new(new(side * load * (7 + lower * 5), lower * (flutter * 4 + kick * 9)),
                side * (load * .052f - kick * .11f + lower * flutter * .022f), new(1 + load * .03f, 1 - kick * .025f)),
            1 => new(new(side * load * (11 + lower * 7), -load * 8 + flutter * (2 + lower * 3)),
                side * (-load * .15f + kick * .21f + flutter * .045f), new(1 + load * .07f, 1)),
            _ => new(new(side * (flutter * 3 + load * 7), lower * (load * 9 - kick * 12)),
                side * (load * .11f + flutter * .036f - kick * .14f), new(1, 1 + load * .04f))
        };
    }
}
