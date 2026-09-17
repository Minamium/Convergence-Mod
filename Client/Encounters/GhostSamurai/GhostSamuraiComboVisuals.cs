using System;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.GhostSamurai;

internal static class GhostSamuraiComboVisuals
{
    internal static void DrawCleave(SpriteBatch batch, SamuraiHazard h, Vector2 center, float age, bool locked, Rectangle view, bool reduced)
    {
        bool live = h.Live(age);
        Color ink = new(6, 13, 30), edge = live ? new(210, 239, 255) : locked ? new(255, 241, 185) : new(255, 180, 58);
        float start = Math.Max(view.Top, center.Y - h.Radius), end = Math.Min(view.Bottom, center.Y + h.Radius);
        int stride = Math.Max(reduced ? 10 : 6, view.Height / 128);
        // The entire dangerous half-disc has restrained continuous coverage.
        // Behind the pivot remains untouched. Work is bounded by the viewport.
        for (float y = start; y < end; y += stride)
        {
            float height = Math.Min(stride, end - y), dy = Math.Max(Math.Abs(y - center.Y), Math.Abs(y + height - center.Y));
            float extent = MathF.Sqrt(Math.Max(0, h.Radius * h.Radius - dy * dy));
            float left = Math.Max(view.Left, h.DX > 0 ? center.X : center.X - extent);
            float right = Math.Min(view.Right, h.DX > 0 ? center.X + extent : center.X);
            if (right <= left) continue;
            GhostSamuraiVisuals.Stroke(batch, new(left, y + height / 2), new(right, y + height / 2), height,
                edge * (live ? .19f : .07f));
        }
        float pivot = center.X + h.DX * 2;
        if (pivot >= view.Left && pivot <= view.Right && end > start)
        {
            GhostSamuraiVisuals.Stroke(batch, new(pivot, start), new(pivot, end), 4, ink * .9f);
            GhostSamuraiVisuals.Stroke(batch, new(pivot, start), new(pivot, end), 2, edge * .8f);
        }
        // One enormous accelerating sword fan, entirely in front of the boss.
        // It does not define a second damage sector or advertise the safe side.
        if (age < h.Fire - SamuraiComboRules.HorizontalSlashSwingTime) return;
        float t = Math.Clamp((age - h.Fire) / (h.End - h.Fire), 0, 1);
        float head = -MathHelper.PiOver2 + MathHelper.Pi * SamuraiComboRules.HorizontalSwingProgress(age - h.Born);
        int arcs = reduced ? 1 : 3;
        for (int arc = 0; arc < arcs; arc++)
        {
            float radius = 260 + arc * 150;
            if (live)
            {
                const float width = 96;
                // A front-only clip and angular inset keep every sprite corner
                // off the safe rear half, including the beginning/end of the fan.
                int split = Math.Clamp((int)(h.DX > 0 ? MathF.Ceiling(center.X) : MathF.Floor(center.X)), view.Left, view.Right);
                Rectangle front = h.DX > 0 ? new(split, view.Top, view.Right - split, view.Height)
                    : new(view.Left, view.Top, split - view.Left, view.Height);
                float margin = MathF.Asin((width * .5f + 3) / radius);
                float from = Math.Max(-MathHelper.PiOver2 + margin, head - 2.9f);
                float to = Math.Min(MathHelper.PiOver2 - margin, head);
                if (to > from) GhostSamuraiSlashArt.Arc(batch, SamuraiSlashArt.Heavy, center, radius,
                    from, to - from, width, GhostSamuraiSlashArt.Energy(age, h.Fire, h.End) * (reduced ? .58f : .9f), front, (int)h.DX);
                continue;
            }
            for (int segment = 0; segment < 24; segment++)
            {
                float a = Math.Max(-MathHelper.PiOver2, head - 1.2f) + segment / 24f * Math.Min(1.2f, head + MathHelper.PiOver2);
                float b = Math.Max(-MathHelper.PiOver2, head - 1.2f) + (segment + 1) / 24f * Math.Min(1.2f, head + MathHelper.PiOver2);
                Vector2 p = center + new Vector2(h.DX * MathF.Cos(a), MathF.Sin(a)) * radius;
                Vector2 q = center + new Vector2(h.DX * MathF.Cos(b), MathF.Sin(b)) * radius;
                GhostSamuraiVisuals.Stroke(batch, p, q, 12, edge * ((live ? .65f : .2f) * (1 - t)));
                GhostSamuraiVisuals.Stroke(batch, p, q, 3, (live ? Color.White : edge) * ((live ? 1 : .45f) * (1 - t)));
            }
        }
    }

    internal static void DrawShock(SpriteBatch batch, SamuraiHazard h, Vector2 center, float age, bool reduced)
    {
        Color ink = new(6, 13, 30), edge = h.Live(age) ? new(203, 232, 255) : new(255, 210, 110);
        if (!h.Live(age))
        {
            // Forecast the maximum swept height, anchored to the same floor as
            // the growing fronts. The bright live rectangle is the current size.
            float radius = h.Radius * SamuraiComboRules.HorizontalSlashWaveMaxScale;
            center.Y += h.Radius * SamuraiComboRules.HorizontalSlashWaveStartScale - radius;
            Vector2 end = center + new Vector2(h.DX * h.Length, 0);
            GhostSamuraiVisuals.Stroke(batch, center, end, radius * 2, ink * .22f);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 offset = new(0, side * (radius - 2));
                GhostSamuraiVisuals.Stroke(batch, center + offset, end + offset, 4, ink * .9f);
                GhostSamuraiVisuals.Stroke(batch, center + offset, end + offset, 2, edge * .8f);
            }
            for (float x = 24; x < h.Length; x += 112)
            {
                Vector2 point = center + new Vector2(x * h.DX, 0);
                GhostSamuraiVisuals.Stroke(batch, point - new Vector2(9 * h.DX, 8), point, 2, edge);
                GhostSamuraiVisuals.Stroke(batch, point - new Vector2(9 * h.DX, -8), point, 2, edge);
            }
            return;
        }
        var geometry = SamuraiComboRules.ShockGeometry(h, age);
        float halfWidth = geometry.Length / 2;
        Vector2 a = center - new Vector2(halfWidth, 0), b = center + new Vector2(halfWidth, 0);
        GhostSamuraiVisuals.Stroke(batch, a, b, geometry.Radius * 2, ink * .7f);
        GhostSamuraiVisuals.Stroke(batch, a, b, geometry.Radius * 2 - 4, edge * .5f);
        GhostSamuraiSlashArt.Strip(batch, SamuraiSlashArt.Wind, h.DX > 0 ? a : b, h.DX > 0 ? b : a,
            geometry.Radius * 2 - 8, reduced ? .35f : .6f);
        // A crest rises only within the actual jump-clearance height.
        for (int i = 0; i < 5; i++)
        {
            float x = -halfWidth + (i + .5f) * geometry.Length / 5;
            GhostSamuraiVisuals.Stroke(batch, center + new Vector2(x, geometry.Radius - 2),
                center + new Vector2(x + h.DX * Math.Min(7, geometry.Length / 12), -geometry.Radius + 2), 3, edge);
        }
    }
}
