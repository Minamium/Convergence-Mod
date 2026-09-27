using System;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.GhostSamurai;

internal static class GhostSamuraiComboVisuals
{
    internal static void DrawCleave(SpriteBatch batch, SamuraiHazard h, Vector2 center, float age, bool locked, Rectangle view, bool reduced)
    {
        GhostSamuraiCuts.Field(batch, h, center, age, view, reduced);
    }
    internal static void DrawShock(SpriteBatch batch, SamuraiHazard h, Vector2 center, float age, bool reduced)
    {
        if (!h.Live(age))
        {
            float radius = h.Radius * SamuraiComboRules.HorizontalSlashWaveMaxScale;
            center.Y += h.Radius * SamuraiComboRules.HorizontalSlashWaveStartScale - radius;
            GhostSamuraiCuts.Stroke(batch, center, center + new Vector2(h.DX * h.Length, 0), radius, age,
                h.Born, h.Fire, h.End, reduced, GhostSamuraiCuts.Seed(h), route: true);
            return;
        }
        var geometry = SamuraiComboRules.ShockGeometry(h, age);
        Vector2 d = new(h.DX * geometry.Length / 2, 0);
        GhostSamuraiCuts.Stroke(batch, center - d, center + d, geometry.Radius, age,
            h.Born, h.Fire, h.End, reduced, GhostSamuraiCuts.Seed(h));
    }
}
