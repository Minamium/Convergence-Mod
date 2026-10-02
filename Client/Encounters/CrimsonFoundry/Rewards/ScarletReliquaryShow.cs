#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// The Scarlet Score Reliquary's opening show (REWARDS.md, "Scarlet Score Reliquary", Opening show): small, personal and
// client-only, about 70 px above the opener's head, 60 ticks, at most four at once.
//   0-4    the closed reliquary appears and settles
//   4-10   windup: the wax seal heats, three fine crimson cracks run across it, the casket trembles by one dot
//   10     release: the seal splits and its halves tumble away; the lid swings back on its rear hinge to ~110 degrees
//          in 8 ticks with a small overshoot and no spin
//   10-40  a crimson glow wells up out of the velvet (painted, moved by noise; no rays, rings or glyphs); five short
//          dormant ink lines rise ~120 px and curl apart like a staff, ignite as live black blood at 20, then dry and
//          break into embers; a few black-blood droplets leap and fall back
//   44-60  the reliquary burns away from its edges (ScarletInk SpriteBurnPass), not an alpha fade
// The casket follows the opener's head until the release, then stays where it opened (Free): the body, lid, glow, its
// light, droplets, rising staff and burn all share that one place, so a moving opener never leaves the glow behind an
// open casket. The ink goes through ScarletRewardInk (this system is an emitter); the parts draw after it in
// PostDrawTiles. Time is the per-tick sample count plus WeaponDrawClock.Fraction. No hit, packet or world state is
// involved.
[Autoload(Side = ModSide.Client)]
internal sealed class ScarletReliquaryShow : ModSystem, IScarletInkEmitter
{
    private const int Life = CrimsonRewardRules.ShowLife, Settle = CrimsonRewardRules.ShowSettle, Release = CrimsonRewardRules.ShowRelease;
    private const int Ignite = CrimsonRewardRules.ShowIgnite, BurnStart = CrimsonRewardRules.ShowBurnStart, MaxShows = CrimsonRewardRules.ShowMax;
    private const int LiveEnd = 30, ResidueEnd = 50, Droplets = 6;
    private const float Hover = CrimsonRewardRules.ShowHover, Rise = CrimsonRewardRules.ShowInkRise;

    private static readonly List<Show> shows = new(MaxShows);
    private static readonly ScarletSpriteBurn burner = new();
    private static BoxArt? art;
    private static bool failed;
    private static int opened;

    // Delivered parts (SR01P_c, one lattice; tools/export_scarlet_reward_art.py), in texels from the body's top-left:
    // the lid's top-left where it closes over the velvet, its rear hinge (bottom-left corner) in lid texels, the seal's
    // top-left on the ring recess, and the mouth (the velvet's centre row).
    private static readonly Vector2 FinalLidAt = new(-1, -7), FinalHinge = new(0, 13), FinalSealAt = new(8, 5);
    private const float FinalMouth = 3.14f;

    // Resolved parts. LidAt, SealAt and MouthAt are in texels from the body's top-left, HingeAt in lid texels. Local
    // layout (px, y down, origin at the closed box's centre): the lid closes on the body; it turns about its rear (left)
    // bottom corner; the mouth is where the velvet lies; the seal sits on the body front.
    private sealed record BoxArt(Texture2D Body, Rectangle BodySrc, Texture2D Lid, Rectangle LidSrc, Texture2D? Seal, Rectangle SealSrc,
        float Scale, bool Pixel, Vector2 LidAt, Vector2 HingeAt, Vector2 SealAt, float MouthAt)
    {
        private Vector2 BodySize => new(BodySrc.Width, BodySrc.Height);
        private Vector2 Min => Vector2.Min(Vector2.Zero, LidAt);
        private Vector2 Max => Vector2.Max(BodySize, LidAt + new Vector2(LidSrc.Width, LidSrc.Height));
        private Vector2 Centre => (Min + Max) * .5f;
        internal float Width => (Max.X - Min.X) * Scale;
        internal float Height => (Max.Y - Min.Y) * Scale;
        internal float Mouth => (MouthAt - Centre.Y) * Scale;
        internal Vector2 BodyCenter => (BodySize * .5f - Centre) * Scale;
        internal Vector2 Hinge => (LidAt + HingeAt - Centre) * Scale;
        internal Vector2 SealCenter => (SealAt + new Vector2(SealSrc.Width, SealSrc.Height) * .5f - Centre) * Scale;
        internal float Dot => Pixel ? CrimsonRewardSprites.PixelScale : 2;
    }

    private sealed class Show
    {
        internal readonly int Owner, Seed;
        internal int Ticks;
        internal Vector2 Previous, Anchor, Free;

        internal Show(Player player, int seed)
        {
            Owner = player.whoAmI; Seed = seed;
            Previous = Anchor = Free = Target(player);
        }

        internal float Age(float fraction) => Math.Max(0, Ticks - 1 + fraction);
        internal Vector2 Live(float fraction) => Vector2.Lerp(Previous, Anchor, fraction);
        // Where the casket is drawn: on the opener until the release, then where it opened.
        internal Vector2 Casket(float fraction) => Age(fraction) < Release ? Live(fraction) : Free;
    }

    public override void Load()
    {
        CrimsonScoreReliquary.Opened = Open;
        ScarletRewardInk.Register(this);
    }

    public override void Unload()
    {
        CrimsonScoreReliquary.Opened = null;
        ScarletRewardInk.Unregister(this);
        Clear(); art = null; failed = false;
        ScarletRewardArt.Reset(); ScarletRewardAudio.Reset();
    }

    public override void ClearWorld() => Clear();
    public override void OnWorldUnload() => Clear();

    private static void Clear()
    {
        shows.Clear();
        opened = 0;
    }

    private void Open(Player player)
    {
        if (Main.dedServ || Main.gameMenu || !player.active) return;
        ScarletRewardAudio.Play(ScarletRewardCues.ReliquaryOpen, player.Center, .8f, 0, .02f, 2);
        if (ResolveArt() is null) return;
        uint hash = unchecked((Main.GameUpdateCount + (uint)(++opened) * 977u + (uint)player.whoAmI * 131u) * 2654435761u);
        if (shows.Count >= MaxShows) shows.RemoveAt(0);
        shows.Add(new Show(player, (int)(hash >> 8)));
    }

    private static Vector2 Target(Player player)
        => (player.gravDir >= 0 ? player.Top : player.Bottom) + new Vector2(0, -Hover * (player.gravDir >= 0 ? 1 : -1));

    public override void PostUpdateEverything()
    {
        if (shows.Count == 0 || Main.gamePaused) return;
        for (int i = shows.Count - 1; i >= 0; i--)
        {
            var show = shows[i];
            if (show.Ticks > Life + 1) { shows.RemoveAt(i); continue; }
            Step(show);
        }
    }

    private static void Step(Show s)
    {
        s.Ticks++;
        s.Previous = s.Anchor;
        Player player = Main.player[s.Owner];
        if (player.active)
        {
            Vector2 want = Target(player);
            if (Vector2.DistanceSquared(s.Anchor, want) > 400f * 400f) s.Previous = s.Anchor = want;
            else s.Anchor = Vector2.Lerp(s.Anchor, want, .35f);
        }
        // Free is the casket's place at the release (age 10, where Live arrives); from then on the casket and everything
        // that leaves it stay there.
        if (s.Ticks <= Release) s.Free = s.Anchor;
        var box = art;
        if (box is null) return;
        float t = s.Ticks;
        Vector2 mouth = s.Free + new Vector2(0, box.Mouth);
        float glow = Glow(s, t);
        if (glow > .01f) Lighting.AddLight(mouth, .9f * glow, .1f * glow, .08f * glow);
        // The dried staff breaks into embers.
        if (t >= LiveEnd && t < BurnStart) // Reduced Effects halves particles where they are spawned
        {
            int line = (int)(Hash(s.Seed, s.Ticks) * CrimsonRewardRules.ShowInkLines);
            float along = .2f + .8f * Hash(s.Seed, s.Ticks + 101);
            Vector2 at = InkPoint(s, line, along);
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, s.Owner, at, new Vector2((Hash(s.Seed, s.Ticks + 7) - .5f) * .8f, -.4f - .6f * Hash(s.Seed, s.Ticks + 9)),
                18 + 10 * Hash(s.Seed, s.Ticks + 3), 5, s.Seed + s.Ticks);
        }
        // Embers lift off the burning edges.
        if (t >= BurnStart && t < Life - 2)
        {
            float edge = Hash(s.Seed, s.Ticks + 211) - .5f;
            Vector2 at = s.Free + new Vector2(edge * box.Width, (Hash(s.Seed, s.Ticks + 223) - .5f) * box.Height);
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, s.Owner, at, new Vector2(edge * .6f, -.6f), 16, 4, s.Seed * 3 + s.Ticks);
            if (Hash(s.Seed, s.Ticks + 227) > .6f)
                ScarletRewardFx.Particle(ScarletParticleKind.Smoke, s.Owner, at, new Vector2(0, -.5f), 26, 9, s.Seed * 5 + s.Ticks);
        }
        // Wax flakes from the split seal.
        if (s.Ticks == Release && box.Seal is not null)
        {
            Vector2 seal = s.Free + box.SealCenter;
            for (int i = 0; i < 6; i++)
                ScarletRewardFx.Particle(ScarletParticleKind.WaxFlake, s.Owner, seal, new Vector2((Hash(s.Seed, i + 31) - .5f) * 3, -1.5f - 2 * Hash(s.Seed, i + 37)),
                    24, box.Dot, s.Seed + i);
        }
    }

    // ---- Ink ----------------------------------------------------------------------------------------------------

    // Line j of the staff at fraction `along` of the full rise: rising from the mouth and curling apart.
    private static Vector2 InkPoint(Show s, int line, float along)
    {
        var box = art!;
        float k = line - (CrimsonRewardRules.ShowInkLines - 1) * .5f;
        float curl = (Hash(s.Seed, line + 51) - .5f) * 16;
        float x = k * (box.Width * .16f + 30 * MathF.Pow(along, 1.5f)) + curl * along * along;
        return s.Free + new Vector2(x, box.Mouth - Rise * along);
    }

    public void Emit(ScarletInkCanvas canvas, in ScarletView view)
    {
        if (art is null) return;
        float fraction = view.Fraction;
        foreach (var s in shows)
        {
            float a = s.Age(fraction);
            if (a < Release || a >= ResidueEnd) continue;
            float written = Math.Clamp((a - Release) / (Ignite - Release), 0, 1);
            written = 1 - (1 - written) * (1 - written);
            for (int line = 0; line < CrimsonRewardRules.ShowInkLines; line++)
            {
                float seed = (s.Seed % 997) * .01f + line * 1.7f;
                ScarletInkLook look = a < Ignite ? ScarletInkLook.Dormant : a < LiveEnd ? ScarletInkLook.Live : ScarletInkLook.Residue;
                float fade = 1 - Math.Clamp((a - LiveEnd) / (ResidueEnd - LiveEnd), 0, 1);
                // The whole staff stops burning together at LiveEnd, so it closes into its scar as a whole.
                var style = ScarletRewardFx.Ink(s.Owner, look, seed) with { Remaining = LiveEnd - a };
                if (!canvas.Begin(style)) return;
                const int samples = 14;
                for (int i = 0; i <= samples; i++)
                {
                    float along = written * i / samples;
                    float radius = look == ScarletInkLook.Dormant ? 4 : look == ScarletInkLook.Live ? 6 : 6;
                    radius *= 1 - .45f * (i / (float)samples); // tapering toward the tips
                    // The whole staff ignites together; the blaze runs a little up each line.
                    float time = look == ScarletInkLook.Live ? a - Ignite - along * 2 : look == ScarletInkLook.Residue ? fade : 0;
                    canvas.Point(InkPoint(s, line, along), radius, Math.Max(0, time));
                }
                canvas.End(bead: look == ScarletInkLook.Dormant && written < 1);
            }
            // A few droplets leap from the mouth at the ignition and fall back.
            if (a >= Ignite && a < Ignite + 22)
            {
                int count = CrimsonRewardRules.ReducedCount(Droplets, canvas.Reduced);
                for (int i = 0; i < count; i++)
                {
                    Vector2 now = Droplet(s, i, a - Ignite), before = Droplet(s, i, Math.Max(0, a - Ignite - 1.5f));
                    if (now.Y > s.Free.Y + art.Mouth + 2) continue; // fallen back into the velvet
                    canvas.Droplet(ScarletRewardFx.Ink(s.Owner, ScarletInkLook.Live, s.Seed % 101 + i), before, now, 2.6f, a - Ignite);
                }
            }
        }
    }

    private static Vector2 Droplet(Show s, int i, float t)
    {
        float angle = -MathF.PI / 2 + (Hash(s.Seed, i + 61) - .5f) * 1.6f, speed = 3.2f + 2.2f * Hash(s.Seed, i + 67);
        Vector2 v = new(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
        return s.Free + new Vector2(0, art!.Mouth) + v * t + new Vector2(0, .32f * t * t);
    }

    // ---- Sprites ------------------------------------------------------------------------------------------------

    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu || shows.Count == 0) return;
        ScarletRewardInk.DrawWorld(); // the ink lies under the parts
        var box = art;
        if (box is null || failed) return;
        var device = Main.instance.GraphicsDevice;
        var saved = new ScarletRewardInk.SavedDevice(device);
        try
        {
            var view = ScarletRewardInk.View();
            foreach (var s in shows) DrawParts(view, box, s);
            DrawGlowAndCracks(view, box);
        }
        catch (Exception e)
        {
            failed = true; Clear();
            Mod.Logger.Warn("Scarlet reliquary show disabled for this session after a drawing error; opening still works.", e);
        }
        finally { saved.Restore(device); }
    }

    private static float OutBack(float x, float overshoot)
    {
        x = Math.Clamp(x, 0, 1) - 1;
        return 1 + (overshoot + 1) * x * x * x + overshoot * x * x;
    }
    private static float Ease(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }

    // Appear and settle (0-4), tremble by one dot (4-10).
    private static Vector2 BoxOffset(Show s, BoxArt box, float a)
    {
        float settle = (1 - Ease(a / Settle)) * -10;
        float tremble = a >= Settle && a < Release ? (Hash(s.Seed, (int)a + 300) > .5f ? box.Dot : -box.Dot) * (Hash(s.Seed, (int)a + 400) > .35f ? 1 : 0) : 0;
        return new Vector2(tremble, settle);
    }

    private static float Burn(float a) => Ease((a - BurnStart) / (Life - BurnStart));

    private static void DrawParts(in ScarletView view, BoxArt box, Show s)
    {
        float a = s.Age(view.Fraction);
        if (a > Life) return;
        float pop = a < Settle ? .6f + .4f * OutBack(a / Settle, 1.4f) : 1;
        Vector2 at = s.Casket(view.Fraction) + BoxOffset(s, box, a);
        float scale = box.Scale * pop, burn = Burn(a), seed = (s.Seed % 89) * .113f;
        Color color = Color.White * Ease(a / 2);
        // Body
        burner.Draw(view, ScarletVfxHost.Assets, box.Body, box.BodySrc, at + box.BodyCenter * pop, new Vector2(box.BodySrc.Width, box.BodySrc.Height) * .5f,
            0, new Vector2(scale), color, burn, seed, box.Pixel);
        // Lid: swings back on its rear hinge to about 110 degrees in 8 ticks, a small overshoot, no spin.
        float swing = a < Release ? 0 : -MathHelper.ToRadians(CrimsonRewardRules.ShowLidDegrees) * OutBack((a - Release) / CrimsonRewardRules.ShowLidTicks, .9f);
        burner.Draw(view, ScarletVfxHost.Assets, box.Lid, box.LidSrc, at + box.Hinge * pop, box.HingeAt,
            swing, new Vector2(scale), color, burn, seed + .37f, box.Pixel);
        if (box.Seal is null) return;
        Rectangle src = box.SealSrc;
        if (a < Release)
        {
            // The seal heats before it cracks.
            float heat = Ease((a - Settle) / (Release - Settle));
            burner.Draw(view, ScarletVfxHost.Assets, box.Seal, src, at + box.SealCenter * pop, new Vector2(src.Width, src.Height) * .5f, 0, new Vector2(scale),
                Color.Lerp(Color.White, new Color(255, 196, 160), heat) * Ease(a / 2), 0, seed + .71f, box.Pixel);
            return;
        }
        // Two halves tumble away under gravity, burning out as they fall.
        float t = a - Release;
        for (int half = 0; half < 2; half++)
        {
            int side = half == 0 ? -1 : 1;
            var part = new Rectangle(src.X + (half == 0 ? 0 : src.Width / 2), src.Y, src.Width / 2, src.Height);
            Vector2 from = s.Free + box.SealCenter + new Vector2(side * src.Width * scale * .25f, 0);
            Vector2 pos = from + new Vector2(side * 1.4f * t, -2.2f * t + .3f * t * t);
            burner.Draw(view, ScarletVfxHost.Assets, box.Seal, part, pos, new Vector2(part.Width, part.Height) * .5f, side * .12f * t, new Vector2(scale),
                Color.White, Ease((t - 8) / 14), seed + half, box.Pixel);
        }
    }

    // Additive: the glow welling out of the velvet (10-40) and the seal's cracks (4-10).
    private static void DrawGlowAndCracks(in ScarletView view, BoxArt box)
    {
        var batch = Main.spriteBatch;
        Texture2D soft = ScarletVfxHost.Assets.GetTexture("Luminance/BloomCircleSmall");
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        Matrix world = Matrix.CreateTranslation(-view.ScreenPosition.X, -view.ScreenPosition.Y, 0) * view.GameView;
        batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, world);
        try
        {
            foreach (var s in shows)
            {
                float a = s.Age(view.Fraction);
                float glow = Glow(s, a) * (1 - Burn(a));
                if (glow > .01f)
                {
                    Vector2 mouth = s.Free + new Vector2((Noise(s.Seed, a * .09f) - .5f) * box.Width * .2f, box.Mouth - 4 - 8 * Noise(s.Seed + 5, a * .07f));
                    float w = box.Width * (1.1f + .35f * Noise(s.Seed + 9, a * .11f)) / soft.Width * 2, h = w * (.55f + .25f * Noise(s.Seed + 13, a * .08f));
                    batch.Draw(soft, mouth, null, new Color(1f, .1f, .07f) * (.75f * glow), 0, new Vector2(soft.Width, soft.Height) * .5f,
                        new Vector2(w, h), SpriteEffects.None, 0);
                    batch.Draw(soft, mouth + new Vector2(0, 3), null, new Color(1f, .45f, .25f) * (.35f * glow), 0, new Vector2(soft.Width, soft.Height) * .5f,
                        new Vector2(w * .45f, h * .4f), SpriteEffects.None, 0);
                }
                if (box.Seal is null || a < Settle || a >= Release) continue;
                // Three fine cracks run across the seal from inside.
                float run = Ease((a - Settle - 1) / (Release - Settle - 1));
                Vector2 c = s.Casket(view.Fraction) + BoxOffset(s, box, a) + box.SealCenter;
                float reach = box.SealSrc.Width * box.Scale * .5f;
                for (int k = 0; k < CrimsonRewardRules.ShowSealCracks; k++)
                {
                    float angle = k * MathF.Tau / 3 + Hash(s.Seed, k + 71) * 1.2f;
                    Vector2 p = c;
                    for (int seg = 0; seg < 3; seg++)
                    {
                        float bend = angle + (Hash(s.Seed, k * 7 + seg + 81) - .5f) * .9f;
                        Vector2 q = p + new Vector2(MathF.Cos(bend), MathF.Sin(bend)) * reach * .34f * run;
                        Vector2 d = q - p;
                        batch.Draw(pixel, p, new Rectangle(0, 0, 1, 1), new Color(1f, .22f, .1f) * .9f, MathF.Atan2(d.Y, d.X), new Vector2(0, .5f),
                            new Vector2(d.Length(), 1.2f), SpriteEffects.None, 0);
                        p = q;
                    }
                }
            }
        }
        finally { batch.End(); }
    }

    // Wells up after the release, fades as the staff dries; scaled by aperiodic noise, never a pulse.
    private static float Glow(Show s, float a)
        => Ease((a - Release) / 6) * (1 - Ease((a - 30) / 12)) * (.75f + .25f * Noise(s.Seed + 17, a * .13f));

    // ---- Art ----------------------------------------------------------------------------------------------------

    private BoxArt? ResolveArt()
    {
        if (failed) return null;
        if (art is not null) return art;
        try
        {
            var body = ScarletRewardArt.Get(CrimsonRewardSprites.ReliquaryBody);
            if (body.Pixel)
            {
                var lid = ScarletRewardArt.Get(CrimsonRewardSprites.ReliquaryLid);
                var seal = ScarletRewardArt.Get(CrimsonRewardSprites.ReliquarySeal);
                return art = new BoxArt(body.Texture, body.Source, lid.Texture, lid.Source, seal.Pixel ? seal.Texture : null, seal.Source,
                    CrimsonRewardSprites.PixelScale, true, FinalLidAt, FinalHinge, FinalSealAt, FinalMouth);
            }
            // Placeholder: the vanilla crate, its top 35% standing in for the lid (hinged at its bottom-left corner);
            // no separate seal, so the cracks and flakes sit 42% down the body front.
            Rectangle all = OpaqueBounds(body.Texture);
            int cut = Math.Clamp((int)MathF.Round(all.Height * .35f), 1, Math.Max(1, all.Height - 1));
            return art = new BoxArt(body.Texture, new Rectangle(all.X, all.Y + cut, all.Width, all.Height - cut), body.Texture,
                new Rectangle(all.X, all.Y, all.Width, cut), null, default, body.Scale, false,
                new Vector2(0, -cut), new Vector2(0, cut), new Vector2(all.Width * .5f, (all.Height - cut) * .42f), 0);
        }
        catch (Exception e)
        {
            failed = true;
            Mod.Logger.Warn("Scarlet reliquary show art unavailable; the reliquary still opens.", e);
            return null;
        }
    }

    private static Rectangle OpaqueBounds(Texture2D texture)
    {
        var pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);
        int x0 = texture.Width, y0 = texture.Height, x1 = -1, y1 = -1;
        for (int y = 0; y < texture.Height; y++)
            for (int x = 0; x < texture.Width; x++)
                if (pixels[y * texture.Width + x].A > 8)
                { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
        return x1 >= x0 ? new Rectangle(x0, y0, x1 - x0 + 1, y1 - y0 + 1) : texture.Bounds;
    }

    private static float Hash(int seed, int salt) => ScarletRewardParticles.Hash(seed, salt);

    // Smooth value noise in time: aperiodic, so nothing pulses.
    private static float Noise(int seed, float x)
    {
        int i = (int)MathF.Floor(x);
        float f = x - i, u = f * f * (3 - 2 * f);
        return MathHelper.Lerp(Hash(seed, i), Hash(seed, i + 1), u);
    }
}
