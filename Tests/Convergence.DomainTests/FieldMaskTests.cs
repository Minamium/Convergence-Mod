using System;
using System.Numerics;
using Convergence.Client.Encounters.FirstSeverance;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Field mask retains world edges and physical coverage at fractional UI scale and zoom")]
    private static void FieldMaskScaleContract()
    {
        foreach (var size in new[] { (1280, 720), (1920, 1080), (2560, 1440), (3840, 2160) })
        foreach (float zoom in new[] { 1f, 1.17f, 1.5f })
        foreach (float ui in new[] { .75f, 1f, 1.07f, 1.25f, 1.5f, 2f })
        {
            var camera = new Vector2(70000.25f, 6000.5f);
            var view = Matrix3x2.CreateTranslation(-camera)
                * Matrix3x2.CreateScale(zoom, new Vector2(size.Item1, size.Item2) / 2);
            float l = camera.X + size.Item1 * .32f, r = camera.X + size.Item1 * .76f;
            float t = camera.Y + size.Item2 * .27f, b = camera.Y + size.Item2 * .81f;
            var layout = FirstSeveranceFieldMaskLayout.Capture(l, t, r, b, view, size.Item1, size.Item2);
            Vector2 expected = Vector2.Transform(new(l, t), view);
            MaskNear(expected.X, layout.Left.Width, "same subpixel left as the outline");
            MaskNear(expected.Y, layout.Top.Height, "same subpixel top as the outline");
            MaskNear(size.Item1, layout.Right.X + layout.Right.Width, $"right viewport edge, UI {ui}");
            MaskNear(size.Item2, layout.Bottom.Y + layout.Bottom.Height, $"bottom viewport edge, UI {ui}");
            // Simulate the engine changing Main.screenWidth for the UI callback.
            // The captured layout must survive untouched and draw with identity.
            int callbackWidth = (int)(size.Item1 / ui);
            if (ui == 1.07f)
                AssertEqual(true, Math.Abs(callbackWidth - (layout.Right.X + layout.Right.Width)) > 50,
                    "107% regression: UI callback width must NOT become the physical mask width");
            CheckMaskPartition(layout, size.Item1, size.Item2);
        }
    }

    [DomainTest("Field mask clips offscreen arenas and covers the viewport without gaps or overlap")]
    private static void FieldMaskClippingContract()
    {
        foreach (float x in new[] { -5000f, -300f, 0f, 500.25f, 1700f, 5000f })
        foreach (float y in new[] { -5000f, -100f, 0f, 400.75f, 900f, 5000f })
            CheckMaskPartition(FirstSeveranceFieldMaskLayout.Capture(x, y, x + 1500, y + 800,
                Matrix3x2.Identity, 1920, 1080), 1920, 1080);
        var fullArena = FirstSeveranceFieldMaskLayout.Capture(-500, -500, 3000, 2000, Matrix3x2.Identity, 1920, 1080);
        MaskNear(0, MaskArea(fullArena), "arena covers entire viewport; no black inside");
        var missingArena = FirstSeveranceFieldMaskLayout.Capture(5000, 5000, 7000, 7000, Matrix3x2.Identity, 1920, 1080);
        MaskNear(1920 * 1080, MaskArea(missingArena), "fully offscreen arena; entire viewport black");
    }

    [DomainTest("Field mask follows gravity flip and fractional cinematic camera motion")]
    private static void FieldMaskCameraContract()
    {
        var view = Matrix3x2.CreateScale(1, -1) * Matrix3x2.CreateTranslation(.25f, 1080.75f);
        var mask = FirstSeveranceFieldMaskLayout.Capture(100, 200, 1400, 900, view, 1920, 1080);
        MaskNear(180.75, mask.Top.Height, "flipped bottom becomes top");
        MaskNear(880.75, mask.Bottom.Y, "flipped top becomes bottom");
        MaskNear(100.25, mask.Left.Width, "subpixel camera offset retained");
        CheckMaskPartition(mask, 1920, 1080);
    }

    private static double MaskArea(FirstSeveranceFieldMaskLayout mask)
        => (double)mask.Top.Width * mask.Top.Height + (double)mask.Bottom.Width * mask.Bottom.Height
            + (double)mask.Left.Width * mask.Left.Height + (double)mask.Right.Width * mask.Right.Height;

    private static void CheckMaskPartition(FirstSeveranceFieldMaskLayout mask, int width, int height)
    {
        foreach (var rect in new[] { mask.Top, mask.Bottom, mask.Left, mask.Right })
            AssertEqual(true, rect.X >= 0 && rect.Y >= 0 && rect.Width >= 0 && rect.Height >= 0
                && rect.X + rect.Width <= width + .01f && rect.Y + rect.Height <= height + .01f,
                "bounded nonnegative rectangles");
        MaskNear(mask.Top.Height, mask.Left.Y, "top joins left");
        MaskNear(mask.Bottom.Y, mask.Left.Y + mask.Left.Height, "bottom joins left");
        MaskNear(mask.Left.Y, mask.Right.Y, "side strips aligned");
        MaskNear(mask.Left.Height, mask.Right.Height, "side strips have identical height");
        double interior = (mask.Right.X - mask.Left.Width) * (double)mask.Left.Height;
        MaskNear(width * (double)height, MaskArea(mask) + interior, "four strips plus arena partition viewport", 1);
    }

    private static void MaskNear(double expected, double actual, string why, double tolerance = .02)
    {
        if (Math.Abs(expected - actual) > tolerance) throw new InvalidOperationException($"{why}: {actual} != {expected}");
    }
}
