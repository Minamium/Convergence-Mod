// Pixel contract for ScarletGeometryOverlay: what it rasterises must equal the authoritative
// capsules. The overlay is drawn 1:1 over the field on black; sampled pixels are compared with the
// exact point-to-segment distance to every stroke CrimsonTechniqueGeometry.Write returned.
#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal static class PreviewContract
{
    internal static bool Diagnose = Environment.GetEnvironmentVariable("SCARLET_CONTRACT_DIAG") == "1";
    private const int Stride = 3;      // sample every third pixel in both directions
    private const float Margin = 2f;   // ignore pixels this close to a boundary (anti-alias / tessellation)

    internal readonly record struct Result(int Checked, int Wrong, int FillSamples, int RingSamples)
    {
        public static Result operator +(Result a, Result b)
            => new(a.Checked + b.Checked, a.Wrong + b.Wrong, a.FillSamples + b.FillSamples, a.RingSamples + b.RingSamples);
    }

    internal static Result Run(GraphicsDevice device, ScarletGeometryOverlay overlay, PreviewPhrase phrase, int tick)
    {
        var field = PreviewPlanner.Field;
        int width = (int)(field.Right - field.Left), height = (int)(field.Bottom - field.Top);
        var style = ScarletOverlayStyle.Default;
        var warning = new List<CrimsonStroke>(); var active = new List<CrimsonStroke>(); var residue = new List<CrimsonStroke>();
        foreach (var plan in phrase.Plans)
        {
            var scratch = new List<CrimsonStroke>();
            var kind = ScarletGeometryOverlay.Collect(plan, tick, scratch);
            if (kind == ScarletOverlayPhase.Warning) warning.AddRange(scratch);
            else if (kind == ScarletOverlayPhase.Active) active.AddRange(scratch);
            else if (kind == ScarletOverlayPhase.Residue) residue.AddRange(scratch);
        }
        using var target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
        device.SetRenderTarget(target);
        device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1f, 0);
        overlay.Draw(ScarletView.Create(device, width, height, new Vector2(field.Left, field.Top), 1f, tick), phrase.Plans, style);
        device.SetRenderTarget(null);
        var pixels = new Color[width * height];
        target.GetData(pixels);

        Color solid(Vector4 c) => new(new Vector4(c.X * c.W, c.Y * c.W, c.Z * c.W, c.W));
        Color expectActive = solid(style.Active), expectResidue = solid(style.Residue);
        float line = style.WarningLinePixels;
        int checkedCount = 0, wrong = 0, fills = 0, rings = 0;
        for (int y = 1; y < height - 1; y += Stride)
        {
            for (int x = 1; x < width - 1; x += Stride)
            {
                var p = new Vector2(field.Left + x + .5f, field.Top + y + .5f);
                float da = Distance(p, active), dr = Distance(p, residue), dw = Distance(p, warning);
                bool inA = da < -Margin, outA = da > Margin, inR = dr < -Margin, outR = dr > Margin;
                Color got = pixels[y * width + x];
                bool ok;
                // The outline is drawn last, so it wins wherever it lies (also over another note's fill).
                if (dw > .35f && dw < line - .35f) { ok = got.R > 200 && got.G > 200 && got.B > 200; rings++; }
                else if (dw >= -Margin && dw <= line + Margin) continue;
                else if (inA && outR) { ok = Near(got, expectActive, 4); fills++; }
                else if (outA && inR) { ok = Near(got, expectResidue, 4); fills++; }
                else if (outA && outR) ok = Near(got, Color.Black, 4);
                else continue;
                checkedCount++;
                if (!ok)
                {
                    wrong++;
                    if (Diagnose && wrong <= 6)
                    {
                        Console.WriteLine($"    wrong px({x},{y}) got=({got.R},{got.G},{got.B}) dA={da:0.0} dR={dr:0.0} dW={dw:0.0} tick={tick}");
                        foreach (var stroke in warning)
                        {
                            Vector2 a = new(stroke.A.X, stroke.A.Y), b = new(stroke.B.X, stroke.B.Y), v = b - a;
                            float t = Math.Clamp(Vector2.Dot(p - a, v) / v.LengthSquared(), 0, 1);
                            float d = Vector2.Distance(p, a + v * t) - stroke.Radius;
                            if (d < 3) Console.WriteLine($"      stroke A=({a.X - field.Left:0.0},{a.Y - field.Top:0.0}) B=({b.X - field.Left:0.0},{b.Y - field.Top:0.0}) r={stroke.Radius} d={d:0.0} t={t:0.00}");
                        }
                    }
                }
            }
        }
        return new Result(checkedCount, wrong, fills, rings);
    }

    internal readonly record struct YieldResult(int Strokes, int Holding, int LitPixels, int Wrong)
    {
        public static YieldResult operator +(YieldResult a, YieldResult b)
            => new(a.Strokes + b.Strokes, a.Holding + b.Holding, a.LitPixels + b.LitPixels, a.Wrong + b.Wrong);
    }

    // Residue yield of the signature moves, drawn by the production ScarletInkStroke alone, 1:1 over transparent black.
    // Yield on: one tick after End every residue stroke still shows (it dries, it does not vanish); YieldTicks after End
    // every lit pixel lies inside a stroke the move's next note covers (radius + the ink's quad margin), and each such
    // holding stroke still shows. Yield off: every stroke still shows YieldTicks after End (the full residue).
    internal static YieldResult ResidueYield(GraphicsDevice device, ScarletInkStroke ink, IScarletAssets assets, PreviewPhrase phrase)
    {
        var field = PreviewPlanner.Field;
        int width = (int)(field.Right - field.Left), height = (int)(field.Bottom - field.Top);
        using var target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
        var pixels = new Color[width * height];
        var result = new YieldResult();
        var residue = new CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        var next = new CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (var plan in phrase.Plans)
        {
            if (!plan.IsSignature) continue;
            int count = CrimsonTechniqueGeometry.Write(plan, plan.End - 1, residue, false);
            int follower = ScarletResidueYield.Successor(plan, phrase.Plans);
            int forecast = follower >= 0 ? ScarletResidueYield.Forecast(phrase.Plans[follower], next) : 0;
            var holds = new bool[count];
            for (int i = 0; i < count; i++) holds[i] = !ScarletResidueYield.Enabled || ScarletResidueYield.Holds(residue[i], next.AsSpan(0, forecast));
            Render(plan, plan.End + 1);
            for (int i = 0; i < count; i++)
                if (!Shows(residue[i])) { result += new YieldResult(0, 0, 0, 1); Report(plan, i, "missing one tick after End"); }
            Render(plan, plan.End + ScarletResidueYield.YieldTicks);
            int lit = 0, wrong = 0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    var c = pixels[y * width + x];
                    if (c.A <= 3 && c.R <= 3 && c.G <= 3 && c.B <= 3) continue;
                    lit++;
                    var p = new Vector2(field.Left + x + .5f, field.Top + y + .5f);
                    bool inside = false;
                    for (int i = 0; i < count && !inside; i++)
                        inside = holds[i] && Distance(p, residue[i]) <= ScarletInkStroke.Margin + 1;
                    if (!inside) wrong++;
                }
            if (wrong > 0) Report(plan, -1, $"{wrong} lit pixels outside the held strokes at End+{ScarletResidueYield.YieldTicks}");
            int held = 0;
            for (int i = 0; i < count; i++)
            {
                if (!holds[i]) continue;
                held++;
                if (!Shows(residue[i])) { wrong++; Report(plan, i, "held stroke missing"); }
            }
            result += new YieldResult(count, held, lit, wrong);
        }
        return result;

        void Render(in CrimsonGesturePlan plan, int tick)
        {
            device.SetRenderTarget(target);
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1f, 0);
            ink.Draw(ScarletView.Create(device, width, height, new Vector2(field.Left, field.Top), 1f, tick), assets, plan, phrase.Plans);
            device.SetRenderTarget(null);
            target.GetData(pixels);
        }
        // Some lit pixel along the stroke's axis band (the dried scar is narrower than the capsule).
        bool Shows(CrimsonStroke s)
        {
            Vector2 a = new(s.A.X - field.Left, s.A.Y - field.Top), b = new(s.B.X - field.Left, s.B.Y - field.Top);
            int samples = Math.Max(2, (int)(Vector2.Distance(a, b) / 4));
            for (int k = 0; k <= samples; k++)
            {
                var q = Vector2.Lerp(a, b, k / (float)samples);
                int x = (int)q.X, y = (int)q.Y;
                if (x < 0 || y < 0 || x >= width || y >= height) continue;
                if (pixels[y * width + x].A > 3) return true;
            }
            return false;
        }
        static void Report(in CrimsonGesturePlan plan, int stroke, string what)
        {
            if (Diagnose) Console.WriteLine($"    yield {plan.Technique} note {plan.Pulse} stroke {stroke}: {what}");
        }
    }

    private static float Distance(Vector2 p, CrimsonStroke s)
    {
        Vector2 a = new(s.A.X, s.A.Y), b = new(s.B.X, s.B.Y), v = b - a;
        float t = v.LengthSquared() < 1e-6f ? 0 : Math.Clamp(Vector2.Dot(p - a, v) / v.LengthSquared(), 0, 1);
        return Vector2.Distance(p, a + v * t) - s.Radius;
    }

    private static bool Near(Color a, Color b, int tolerance)
        => Math.Abs(a.R - b.R) <= tolerance && Math.Abs(a.G - b.G) <= tolerance && Math.Abs(a.B - b.B) <= tolerance;

    // Signed distance to the union of capsules: negative inside.
    private static float Distance(Vector2 p, List<CrimsonStroke> strokes)
    {
        float best = float.PositiveInfinity;
        for (int i = 0; i < strokes.Count; i++)
        {
            var s = strokes[i];
            Vector2 a = new(s.A.X, s.A.Y), b = new(s.B.X, s.B.Y), v = b - a;
            float t = v.LengthSquared() < 1e-6f ? 0 : Math.Clamp(Vector2.Dot(p - a, v) / v.LengthSquared(), 0, 1);
            best = Math.Min(best, Vector2.Distance(p, a + v * t) - s.Radius);
        }
        return best;
    }
}
