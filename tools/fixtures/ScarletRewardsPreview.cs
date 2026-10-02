// Offline frames of the Scarlet reward black blood (docs/encounters/crimson-foundry/REWARDS.md#black-blood-material):
// the production ScarletRewardInk (PathLivePass / PathDormantPass / PathResiduePass), ScarletRewardParticles and
// ScarletSpriteBurn drawn through ScarletInk.fxc over the Raid backdrops, at actual size. Not a playtest.
//
//   pwsh tools/preview-scarlet.ps1 -Rewards                 # every scene; -Only <scene name> for one
//
// Each scene is an IRewardsPreviewScene. The shared base ships "shared" (stand-in shapes for every look); a weapon
// slice adds tools/fixtures/ScarletRewardsPreview.<Weapon>.cs with its own scene class, found by reflection, so no
// shared file changes. Scenes may use the Vfx seam and the pure rule files only (no Terraria).
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal interface IRewardsPreviewScene
{
    string Name { get; }
    int[] Ticks { get; }
    // Ink for one frame; `center` is the scene's anchor in world pixels, `tick` the scene's own clock.
    void Emit(ScarletInkCanvas canvas, Vector2 center, int tick);
    // Called once per simulated tick before ScarletRewardParticles.Update.
    void Particles(ScarletRewardParticles particles, Vector2 center, int tick, bool reduced) { }
    // Sprites over the ink (bodies, burns); the device is the preview's render target.
    void Sprites(PreviewRenderer renderer, ScarletView view, Vector2 center, int tick) { }
}

internal static class RewardsPreview
{
    internal static int Run(PreviewRenderer renderer, PreviewOptions options, string output)
    {
        var scenes = typeof(RewardsPreview).Assembly.GetTypes()
            .Where(t => typeof(IRewardsPreviewScene).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .Select(t => (IRewardsPreviewScene)Activator.CreateInstance(t)!)
            .Where(s => options.Only.Length == 0 || s.Name.Contains(options.Only, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.Name, StringComparer.Ordinal).ToArray();
        int written = 0;
        foreach (var scene in scenes) written += Render(renderer, options, output, scene);
        Console.WriteLine($"PASS {written} reward PNGs ({scenes.Length} scenes) under {output}. Production ScarletRewardInk + ScarletInk.fxc path passes; no game launched.");
        return scenes.Length > 0 ? 0 : 2;
    }

    private static int Render(PreviewRenderer renderer, PreviewOptions options, string output, IRewardsPreviewScene scene)
    {
        var device = renderer.Device;
        var assets = renderer.Assets;
        var ink = new ScarletRewardInk();
        using var particles = new ScarletRewardParticles();
        var phrase = PreviewPlanner.Build("rewards", PreviewPlanner.Grid, 0, 1, PreviewPlanner.Players()[0], options.PhraseStart);
        var field = PreviewPlanner.Field;
        Vector2 center = new(field.CenterX - 200, field.Bottom - 260);
        int written = 0;
        var cells = new List<SheetCell>();
        bool reduced = options.ReducedModes[0];
        foreach (var backdrop in options.Backdrops)
            foreach (float zoom in new[] { 1f, 2f })
            {
                particles.Clear();
                int simulated = 0;
                foreach (int tick in scene.Ticks)
                {
                    // Particles are simulated from the scene's start, exactly one Update per tick.
                    for (; simulated < tick; simulated++) { scene.Particles(particles, center, simulated, reduced); particles.Update(); }
                    int w = options.Width, h = options.Height;
                    var view = ScarletView.Create(device, w, h, center - new Vector2(w, h) * .5f, zoom, 6000 + tick, 0, reduced);
                    var target = renderer.Target(w, h);
                    device.SetRenderTarget(target);
                    device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1f, 0);
                    renderer.DrawBackdrop(view, backdrop, phrase);
                    var canvas = ink.Begin(view);
                    scene.Emit(canvas, center, tick);
                    ink.Draw(view, assets);
                    particles.Draw(renderer.Batch, view, assets);
                    scene.Sprites(renderer, view, center, tick);
                    string caption = $"{scene.Name} T{tick} {backdrop} ZOOM {zoom}".ToUpperInvariant();
                    renderer.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
                    PreviewText.Draw(renderer.Batch, renderer.Pixel, caption, new Vector2(12, 10), 3, new Color(240, 240, 245));
                    PreviewText.Draw(renderer.Batch, renderer.Pixel, $"PATHS {ink.DrawnPaths} DROPPED {ink.DroppedPaths} VERTICES {ink.DrawnVertices} PARTICLES {particles.Count}",
                        new Vector2(12, h - 26), 2, new Color(200, 200, 208));
                    renderer.Batch.End();
                    device.SetRenderTarget(null);
                    if (options.Files)
                    {
                        string path = Path.Combine(output, "rewards-" + scene.Name, $"{backdrop}-z{zoom}-t{tick:00}.png".ToLowerInvariant());
                        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                        using var file = File.Create(path);
                        target.SaveAsPng(file, w, h);
                        written++;
                    }
                    cells.Add(PreviewSheet.Cell(target, 480, 270, $"{backdrop} Z{zoom} T{tick}".ToUpperInvariant(), new Color(110, 20, 32)));
                }
            }
        PreviewSheet.Save(device, renderer.Pixel, cells, Math.Max(1, scene.Ticks.Length / 2), 480, 270, $"SCARLET REWARD INK {scene.Name}".ToUpperInvariant(),
            Path.Combine(output, $"contact-rewards-{scene.Name}.png"));
        return written + 1;
    }
}

// Stand-in shapes for every look, from the shared base: dormant strokes (one being written, one warmed, another
// player's dimmed), a live river whose head sweeps and dries behind it, a fire column, burst discs and droplets,
// particles of every kind, and SpriteBurnPass on a repository sprite.
internal sealed class SharedRewardsScene : IRewardsPreviewScene
{
    public string Name => "shared";
    public int[] Ticks { get; } = { 0, 4, 8, 12, 16, 20, 24, 28, 32, 40, 48, 56 };
    private readonly ScarletSpriteBurn burn = new();

    public void Emit(ScarletInkCanvas canvas, Vector2 center, int tick) => Scene(canvas, center, tick);
    public void Particles(ScarletRewardParticles particles, Vector2 center, int tick, bool reduced) => Spawn(particles, center, tick, reduced);
    public void Sprites(PreviewRenderer renderer, ScarletView view, Vector2 center, int tick) => Sprites(burn, view, renderer.Assets, center, tick);

    // Stand-in shapes for every look: dormant strokes (one being written, one warmed), a live river whose head sweeps
    // and dries behind it, a fire column, burst discs, droplets, and other players' dimmed copies.
    private static void Scene(ScarletInkCanvas canvas, Vector2 c, int tick)
    {
        // Dormant score: four baton-like strokes; the newest is still being written; the row below is a full (warm) build.
        for (int i = 0; i < 4; i++)
        {
            Vector2 a = c + new Vector2(-420 + i * 70, -150), b = a + new Vector2(60, -40), k = (a + b) * .5f + new Vector2(-10, -26);
            bool writing = i == 3 && tick < 12;
            Vector2 end = writing ? Vector2.Lerp(a, b, Math.Clamp(tick / 12f, .1f, 1)) : b;
            canvas.Quadratic(new ScarletInkStyle(ScarletInkLook.Dormant, true, i * 1.3f), a, writing ? Vector2.Lerp(a, k, tick / 12f) : k, end, 10, 10, 0, 0, writing);
            canvas.Quadratic(new ScarletInkStyle(ScarletInkLook.Dormant, true, i * 1.9f, 1, false, 1), a + new Vector2(0, 90), k + new Vector2(0, 90), b + new Vector2(0, 90), 10, 10, 0, 0);
            canvas.Quadratic(new ScarletInkStyle(ScarletInkLook.Dormant, false, i * 2.3f), a + new Vector2(0, 180), k + new Vector2(0, 180), b + new Vector2(0, 180), 10, 10, 0, 0);
        }
        // A live river (radius 28): its head covers the path in 24 ticks; each point is live for 14 ticks, then a scar.
        Vector2 River(float s) => c + new Vector2(-80 + 420 * s, 40 * MathF.Sin(s * 6.2f) - 120 + 60 * s);
        const int samples = 64;
        float head = Math.Clamp(tick / 24f, 0, 1);
        bool live = false;
        canvas.Begin(new ScarletInkStyle(ScarletInkLook.Live, true, 3.7f));
        for (int i = 0; i <= samples; i++)
        {
            float s = i / (float)samples, ignited = tick - 24 * s;
            if (s > head || ignited >= 14) continue;
            canvas.Point(River(s), 28, ignited); live = true;
        }
        canvas.End(bead: head < 1 && live);
        canvas.Begin(new ScarletInkStyle(ScarletInkLook.Residue, true, 3.7f));
        for (int i = 0; i <= samples; i++)
        {
            float s = i / (float)samples, ignited = tick - 24 * s;
            if (ignited < 14 || ignited >= 14 + 24) continue;
            canvas.Point(River(s), 28, 1 - (ignited - 14) / 24f);
        }
        canvas.End();
        // A censer pour: a falling column with flame tongues (radius 24), its head falling 40 px/tick.
        float fall = Math.Min(260, tick * 40f);
        if (tick < 20)
            canvas.Line(new ScarletInkStyle(ScarletInkLook.Live, true, 5.1f, 1, true), c + new Vector2(420, -220), c + new Vector2(430, -220 + fall), 24, 24, tick, Math.Max(0, tick - fall / 40));
        else if (tick < 44)
            canvas.Line(new ScarletInkStyle(ScarletInkLook.Residue, true, 5.1f, 1, true), c + new Vector2(420, -220), c + new Vector2(430, 40), 24, 24, 1 - (tick - 20) / 24f, 1 - (tick - 20) / 24f);
        // Bursts: a hand slam (48) and another player's (0.85), live 3 ticks then scars.
        float slam = tick % 28;
        var burstLook = slam < 6 ? ScarletInkLook.Live : ScarletInkLook.Residue;
        float burstTime = burstLook == ScarletInkLook.Live ? slam : 1 - (slam - 6) / 22f;
        canvas.Disc(new ScarletInkStyle(burstLook, true, 7.3f), c + new Vector2(140, 120), 48, burstTime);
        canvas.Disc(new ScarletInkStyle(burstLook, false, 8.9f), c + new Vector2(280, 120), 48, burstTime);
        // Droplets from the slam.
        for (int i = 0; i < 8; i++)
        {
            float t = slam, angle = -MathF.PI / 2 + (i - 3.5f) * .32f, speed = 3 + (i % 3);
            Vector2 at = c + new Vector2(140, 100) + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed * t + new Vector2(0, .3f * t * t);
            Vector2 before = c + new Vector2(140, 100) + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed * (t - 1.5f) + new Vector2(0, .3f * (t - 1.5f) * (t - 1.5f));
            if (t > 1.5f && t < 22) canvas.Droplet(new ScarletInkStyle(ScarletInkLook.Live, true, i, 1, false, 0, 0), before, at, 2.6f, t);
        }
    }

    private static void Spawn(ScarletRewardParticles particles, Vector2 c, int tick, bool reduced)
    {
        float h = ScarletRewardParticles.Hash(tick, 1);
        particles.Spawn(ScarletParticleKind.Ember, 0, true, c + new Vector2(420 + (h - .5f) * 40, 40), new Vector2((h - .5f) * 1.5f, -1.2f), 26, 5, reduced, tick);
        if (tick % 3 == 0) particles.Spawn(ScarletParticleKind.Smoke, 0, true, c + new Vector2(430, 30), new Vector2(0, -.6f), 34, 14, reduced, tick + .5f);
        if (tick % 28 == 0)
            for (int i = 0; i < 6; i++)
            {
                particles.Spawn(ScarletParticleKind.BoneChip, 0, true, c + new Vector2(140, 110), new Vector2((i - 2.5f) * .9f, -3.5f), 30, 2, reduced, tick + i);
                particles.Spawn(ScarletParticleKind.WaxFlake, 1, false, c + new Vector2(280, 110), new Vector2((i - 2.5f) * .9f, -3f), 30, 2, reduced, tick + i * 3);
            }
    }

    // SpriteBurnPass on a repository sprite (the Covenant icon): intact, then eroding from its edges.
    private static void Sprites(ScarletSpriteBurn burn, in ScarletView view, PreviewAssets assets, Vector2 c, int tick)
    {
        var texture = assets.GetTexture("CrimsonFoundry/CrimsonPact");
        float progress = Math.Clamp((tick - 20) / 32f, 0, 1);
        for (int i = 0; i < 3; i++)
            burn.Draw(view, assets, texture, texture.Bounds, c + new Vector2(-420 + i * 90, 160), new Vector2(texture.Width, texture.Height) * .5f, 0,
                new Vector2(72f / texture.Width), Color.White, i == 0 ? 0 : progress * (i == 1 ? .6f : 1), i * .37f, false);
    }
}
