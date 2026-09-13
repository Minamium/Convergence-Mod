using System;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.GhostSamurai;

// Bounded vector presentation of the authority's disk/annulus. No textures,
// gameplay RNG, extra projectiles, screen flashes or client-side targeting.
internal static class GhostSamuraiCircleVisuals
{
    internal static void Draw(SpriteBatch batch, SamuraiHazard h, Vector2 center, float age, Rectangle viewport, bool reduced)
    {
        bool live = h.Live(age);
        float progress = Math.Clamp((age - h.Born) / (h.Fire - h.Born), 0, 1);
        Color ink = new(6, 13, 30);
        Color edge = h.IsOuter ? new(110, 208, 255) : new(255, 116, 107);
        float inner = h.IsOuter ? h.Length : 0;
        // Thin slices fill only the dangerous region, leaving the donut's middle
        // completely clear. Only visible rows are visited, even with a huge outer radius.
        const float slice = 12;
        float firstRow = Math.Max(-h.Radius, MathF.Floor((viewport.Top - center.Y) / slice) * slice);
        float lastRow = Math.Min(h.Radius, viewport.Bottom - center.Y);
        for (float y = firstRow; y < lastRow; y += slice)
        {
            float bottom = Math.Min(y + slice, h.Radius), middle = (y + bottom) / 2;
            float outerX = Extent(h.Radius, Math.Max(Math.Abs(y), Math.Abs(bottom)));
            float innerX = inner > 0 ? Extent(inner, y <= 0 && bottom >= 0 ? 0 : Math.Min(Math.Abs(y), Math.Abs(bottom))) : 0;
            if (outerX <= innerX) continue;
            Span(-outerX, -innerX); Span(innerX, outerX);
            void Span(float left, float right)
            {
                left = Math.Max(left, viewport.Left - center.X);
                right = Math.Min(right, viewport.Right - center.X);
                if (right <= left) return;
                GhostSamuraiVisuals.Stroke(batch, center + new Vector2(left, middle), center + new Vector2(right, middle),
                    bottom - y, ink * .3f);
                GhostSamuraiVisuals.Stroke(batch, center + new Vector2(left, middle), center + new Vector2(right, middle),
                    bottom - y, edge * (live ? (reduced ? .14f : .22f) : .07f + .08f * progress));
            }
        }
        Boundary(h.Radius);
        if (inner > 0) Boundary(inner);

        if (!live)
        {
            // Sparse straight hatch for swords; curved marks announce wind.
            if (h.IsWind) Wind(.25f, age * .007f);
            else Slashes(.3f, 0);
        }
        else if (h.IsWind) Wind(.85f, (age - h.Fire) * .12f);
        else Slashes(1 - (age - h.Fire) / (h.End - h.Fire), (age - h.Fire) * .018f);

        void Boundary(float radius)
        {
            // Even a very large finite boundary stays within a subpixel chord
            // error; off-screen segments are culled before issuing draw calls.
            int segments = Math.Clamp((int)MathF.Ceiling(radius / 24), 128, 1024);
            for (int i = 0; i < segments; i++)
            {
                float a = i * MathF.Tau / segments, b = (i + 1) * MathF.Tau / segments;
                Vector2 first = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
                Vector2 last = center + new Vector2(MathF.Cos(b), MathF.Sin(b)) * radius;
                if (!Clip(ref first, ref last)) continue;
                GhostSamuraiVisuals.Stroke(batch, first, last, 7, ink * .95f);
                GhostSamuraiVisuals.Stroke(batch, first, last, 3, edge);
            }
        }
        void Slashes(float opacity, float rotation)
        {
            float angle = -.6f + rotation;
            Vector2 d = new(MathF.Cos(angle), MathF.Sin(angle)), n = new(-d.Y, d.X);
            Vector2 viewCenter = new(viewport.Center.X - center.X, viewport.Center.Y - center.Y);
            float projected = Vector2.Dot(viewCenter, n), extent = Math.Abs(n.X) * viewport.Width / 2 + Math.Abs(n.Y) * viewport.Height / 2;
            float firstOffset = Math.Max(-h.Radius + 60, MathF.Floor((projected - extent) / 120) * 120);
            float lastOffset = Math.Min(h.Radius, projected + extent);
            for (float offset = firstOffset; offset < lastOffset; offset += 120)
            {
                float outer = Extent(h.Radius - 8, Math.Abs(offset));
                float hole = inner > 0 ? Extent(inner + 8, Math.Abs(offset)) : 0;
                if (outer <= hole) continue;
                Cut(-outer, -hole); Cut(hole, outer);
                void Cut(float a, float b)
                {
                    Vector2 first = center + n * offset + d * a, last = center + n * offset + d * b;
                    if (!Clip(ref first, ref last)) return;
                    GhostSamuraiVisuals.Stroke(batch, first, last, live ? 8 : 2, edge * opacity * .65f);
                    if (live) GhostSamuraiVisuals.Stroke(batch, first, last, 2, Color.White * opacity);
                }
            }
        }
        void Wind(float opacity, float rotation)
        {
            // Three concentric blade wakes occupy exactly this step's region.
            for (int ring = 0; ring < (reduced ? 2 : 3); ring++)
            {
                float radius = inner > 0 ? inner + 80 + ring * 180 : h.Radius * (.2f + ring * .3f);
                for (int blade = 0; blade < 4; blade++)
                for (int segment = 0; segment < 10; segment++)
                {
                    float a = rotation * (ring % 2 == 0 ? 1 : -1) + blade * MathF.PI / 2 + ring * .4f + segment * .07f;
                    Vector2 first = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
                    Vector2 last = center + new Vector2(MathF.Cos(a + .07f), MathF.Sin(a + .07f)) * radius;
                    if (!Clip(ref first, ref last)) continue;
                    GhostSamuraiVisuals.Stroke(batch, first, last, (live ? 10 : 3) * (segment + 1) / 10f,
                        edge * (opacity * (segment + 1) / 10f));
                }
            }
        }
        bool Clip(ref Vector2 a, ref Vector2 b)
        {
            Vector2 d = b - a;
            float enter = 0, exit = 1;
            if (!Axis(a.X, d.X, viewport.Left, viewport.Right) || !Axis(a.Y, d.Y, viewport.Top, viewport.Bottom)) return false;
            b = a + d * exit; a += d * enter;
            return true;
            bool Axis(float origin, float delta, float low, float high)
            {
                if (Math.Abs(delta) < .00001f) return origin >= low && origin <= high;
                float first = (low - origin) / delta, last = (high - origin) / delta;
                if (first > last) (first, last) = (last, first);
                enter = Math.Max(enter, first); exit = Math.Min(exit, last);
                return enter <= exit;
            }
        }

    }

    private static float Extent(float radius, float offset) => MathF.Sqrt(Math.Max(0, radius * radius - offset * offset));
}
