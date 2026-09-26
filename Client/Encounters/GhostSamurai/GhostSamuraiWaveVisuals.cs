using System;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.GhostSamurai;

internal static class GhostSamuraiWaveVisuals
{
    internal static void Draw(SpriteBatch batch, SamuraiHazard h, Vector2 center, float age, bool locked, bool reduced)
    {
        Vector2 d = new(h.DX, h.DY), n = new(-h.DY, h.DX);
        if (!h.Live(age))
        {
            float reach = (h.End - h.Fire - 1) * SamuraiWaveRules.ChargedSlashWaveSpeed + h.Length / 2;
            GhostSamuraiEnergy.Line(batch, center, center + d * reach, h.Radius, age,
                Math.Clamp((age - h.Born) / Math.Max(1, h.Fire - h.Born), 0, 1), false, 0, 0, reduced, route: true);
            return;
        }
        GhostSamuraiEnergy.Line(batch, center - d * h.Length / 2, center + d * h.Length / 2, h.Radius,
            age, 1, true, age - h.Fire, h.End - age, reduced);
        float width = Math.Min(72, h.Length * .42f), inset = width * .5f + 5;
        float energy = GhostSamuraiSlashArt.Energy(age, h.Fire, h.End);
        for (int segment = 0; segment < (reduced ? 16 : 32); segment++)
        {
            float u0 = segment / (reduced ? 16f : 32f), u1 = (segment + 1) / (reduced ? 16f : 32f);
            GhostSamuraiSlashArt.Strip(batch, SamuraiSlashArt.Heavy, Curve(u0), Curve(u1), width,
                energy * (reduced ? .48f : .88f), null, u0, u1);
        }
        Vector2 Curve(float u)
        {
            float t = u * 2 - 1;
            return center + n * (t * (h.Radius - inset)) + d * ((1 - t * t) * (h.Length * .5f - inset));
        }
    }
}
