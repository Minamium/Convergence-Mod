using System;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.GhostSamurai;

internal static class GhostSamuraiComboVisuals
{
    internal static void DrawCleave(SpriteBatch batch, SamuraiHazard h, Vector2 center, float age, bool locked, Rectangle view, bool reduced)
    {
        GhostSamuraiEnergy.Field(batch, h, center, age, view, reduced);
        if (!h.Live(age)) return;
        float head = -MathHelper.PiOver2 + MathHelper.Pi * SamuraiComboRules.HorizontalSwingProgress(age - h.Born);
        int split = Math.Clamp((int)(h.DX > 0 ? MathF.Ceiling(center.X) : MathF.Floor(center.X)), view.Left, view.Right);
        Rectangle front = h.DX > 0 ? new(split, view.Top, view.Right - split, view.Height) : new(view.Left, view.Top, split - view.Left, view.Height);
        for (int arc = 0; arc < (reduced ? 2 : 4); arc++)
        {
            float radius = Math.Min(h.Radius - 58, 210 + arc * 130), width = 96;
            if (radius <= width) continue;
            float margin = MathF.Asin((width * .5f + 3) / radius);
            float from = Math.Max(-MathHelper.PiOver2 + margin, head - 2.9f), to = Math.Min(MathHelper.PiOver2 - margin, head);
            if (to > from) GhostSamuraiSlashArt.Arc(batch, SamuraiSlashArt.Heavy, center, radius,
                from, to - from, width, GhostSamuraiSlashArt.Energy(age, h.Fire, h.End) * (reduced ? .50f : .90f), front, (int)h.DX);
        }
    }
    internal static void DrawShock(SpriteBatch batch, SamuraiHazard h, Vector2 center, float age, bool reduced)
    {
        if (!h.Live(age))
        {
            float radius = h.Radius * SamuraiComboRules.HorizontalSlashWaveMaxScale;
            center.Y += h.Radius * SamuraiComboRules.HorizontalSlashWaveStartScale - radius;
            GhostSamuraiEnergy.Line(batch, center, center + new Vector2(h.DX * h.Length, 0), radius, age,
                Math.Clamp((age - h.Born) / Math.Max(1, h.Fire - h.Born), 0, 1), false, 0, 0, reduced, route: true);
            return;
        }
        var geometry = SamuraiComboRules.ShockGeometry(h, age);
        Vector2 d = new(h.DX * geometry.Length / 2, 0);
        GhostSamuraiEnergy.Line(batch, center - d, center + d, geometry.Radius, age, 1, true, age - h.Fire, h.End - age, reduced);
    }
}
