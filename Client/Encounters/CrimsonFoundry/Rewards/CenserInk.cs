#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// The Ember Censer's black blood (REWARDS.md, "Summon - Ember Censer", Presentation): every live pour is one live path
// with the fire flag (flame tongues on its lips), drawn exactly over the capsules it collides with; when its window
// ends the column stays where it fell and dries into a 24-tick embered scar (residue; harmless), the top first. At most
// two paths per censer: the live column and the previous pour's scar. Everything goes through ScarletRewardInk,
// beneath the Raid's forecasts. Client only.
[Autoload(Side = ModSide.Client)]
internal sealed class CenserInk : ModSystem, IScarletInkEmitter
{
    private const int MaxScars = 48, ScarPoints = CenserRules.MaxColumnPoints;

    private sealed class Scar
    {
        internal readonly Vector2[] At = new Vector2[ScarPoints];
        internal int Count, Owner;
        internal float Seed, Radius;
        internal ulong Born;
        internal bool Used;
    }

    private static readonly Scar[] scars = CreateScars();
    private static CenserInk? instance;

    private static Scar[] CreateScars()
    {
        var all = new Scar[MaxScars];
        for (int i = 0; i < all.Length; i++) all[i] = new Scar();
        return all;
    }

    public override void Load()
    {
        instance = this;
        ScarletRewardInk.Register(this);
    }

    public override void Unload()
    {
        if (instance is not null) ScarletRewardInk.Unregister(instance);
        instance = null;
        Clear();
        CenserVisuals.Reset();
    }

    public override void ClearWorld() => Clear();
    public override void OnWorldUnload() => Clear();

    private static void Clear()
    {
        foreach (var scar in scars) scar.Used = false;
    }

    // A pour's window has ended (or its censer is gone): keep its column where it fell as a drying scar. `points` are
    // world positions, top (mouth) first.
    internal static void Keep(int owner, float seed, in CenserPour pour, ReadOnlySpan<Vector2> points)
    {
        if (points.Length < 1) return;
        Scar? slot = null;
        foreach (var scar in scars)
            if (!scar.Used) { slot = scar; break; }
        if (slot is null)
        {
            // Full: the oldest scar gives way.
            slot = scars[0];
            foreach (var scar in scars) if (scar.Born < slot.Born) slot = scar;
        }
        int n = Math.Min(points.Length, ScarPoints);
        points[..n].CopyTo(slot.At);
        slot.Count = n; slot.Owner = owner; slot.Seed = seed; slot.Radius = pour.Radius;
        slot.Born = Main.GameUpdateCount; slot.Used = true;
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ || Main.gamePaused) return;
        ulong now = Main.GameUpdateCount;
        foreach (var scar in scars)
        {
            if (!scar.Used) continue;
            float age = now - scar.Born - 1;
            if (age >= CrimsonRewardRules.PourScar) { scar.Used = false; continue; }
            if (age < 0) continue;
            // An embered scar: sparks lift off it now and then, more while it is fresh.
            float h = ScarletRewardParticles.Hash(scar.Seed, (int)now);
            if (h > .35f + .4f * age / CrimsonRewardRules.PourScar) continue;
            int k = (int)(ScarletRewardParticles.Hash(scar.Seed, (int)now + 7) * scar.Count);
            Vector2 at = scar.At[Math.Clamp(k, 0, scar.Count - 1)];
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, scar.Owner, at + new Vector2((h - .5f) * scar.Radius, 0),
                new Vector2((ScarletRewardParticles.Hash(scar.Seed, (int)now + 11) - .5f) * .9f, -.7f - h), 20 + 10 * h, 4, scar.Seed + now);
        }
    }

    public void Emit(ScarletInkCanvas canvas, in ScarletView view)
    {
        if (Main.gameMenu) return;
        float fraction = view.Fraction;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is EmberCenserMinion minion && p.TryGetGlobalProjectile(out CenserVisuals visuals) && visuals.Look is { } look)
                look.EmitColumn(canvas, p, minion, fraction);
        ulong now = Main.GameUpdateCount;
        foreach (var scar in scars)
        {
            if (!scar.Used) continue;
            // Drawn from the update after the pour ended, when the live column's interpolation has reached its end.
            float age = now - scar.Born - 1 + fraction;
            if (age < 0 || age >= CrimsonRewardRules.PourScar || scar.Count < 1) continue;
            if (!canvas.Begin(ScarletRewardFx.Ink(scar.Owner, ScarletInkLook.Residue, scar.Seed, 1, fire: true))) return;
            Vector2 previous = default;
            for (int k = 0; k < scar.Count; k++)
            {
                // The top (k = 0, the mouth) dries first; the floor end 4 ticks later (a 20-24 tick scar).
                float u = scar.Count > 1 ? k / (scar.Count - 1f) : 1;
                float fade = Math.Clamp(1 - (age + 4 * (1 - u)) / CrimsonRewardRules.PourScar, 0, 1);
                Vector2 at = scar.At[k];
                if (k > 0) Subdivide(canvas, previous, at, scar.Radius, fade);
                canvas.Point(at, scar.Radius, fade);
                previous = at;
            }
            canvas.End();
        }
    }

    // Samples between two points every ~10 px (the canvas wants 8-12 px), excluding both ends.
    internal static void Subdivide(ScarletInkCanvas canvas, Vector2 from, Vector2 to, float radius, float time, float fromTime = float.NaN)
    {
        float length = Vector2.Distance(from, to);
        int parts = (int)MathF.Ceiling(length / 10f);
        for (int s = 1; s < parts; s++)
        {
            float u = s / (float)parts;
            canvas.Point(Vector2.Lerp(from, to, u), radius, float.IsNaN(fromTime) ? time : MathHelper.Lerp(fromTime, time, u));
        }
    }
}

// The crown censer body (SR05 middle cell) or its placeholder, with the anchors the pose needs, in source pixels:
// the ring (where it hangs), the bowl mouth (the sprite turns about it, and it sits exactly on the column's top) and
// the two drape tails. Base turns the source so its ring-to-mouth axis hangs straight down.
internal sealed record CenserBody(Texture2D Texture, Rectangle Source, float Scale, bool Pixel, Vector2 Ring, Vector2 Mouth,
    Vector2 DrapeLeft, Vector2 DrapeRight, float Base)
{
    internal SamplerState Sampler => Pixel ? SamplerState.PointClamp : SamplerState.LinearClamp;

    // Anchors of EmberCenser.png (SR05_d, 30 x 36 logical) in texels, measured by tools/export_scarlet_reward_art.py:
    // the hole of the top ring, the bowl mouth on top of the gold rim straight under it (CrimsonRewardRules.BowlDrop is
    // their distance at the 2 px dot, so the drawn ring hangs on the pendulum's pivot) and the lowest texels of the two
    // outer drapes, where the Verlet tails continue them.
    private static readonly Vector2 FinalRing = new(14.5f, 3f), FinalMouth = new(14.5f, 20f), FinalDrapeLeft = new(3.5f, 30f), FinalDrapeRight = new(26f, 31f);

    private static CenserBody? body;
    private static bool failed;

    internal static CenserBody? Get()
    {
        if (body is not null && !body.Texture.IsDisposed) return body;
        if (failed) return null;
        try
        {
            var sprite = ScarletRewardArt.Get(CrimsonRewardSprites.CenserMinion);
            Rectangle src = sprite.Source;
            if (sprite.Pixel)
                return body = new CenserBody(sprite.Texture, src, sprite.Scale, true, FinalRing, FinalMouth, FinalDrapeLeft, FinalDrapeRight, 0);
            // Placeholder (the Imp Staff item, a staff from lower left to a head at upper right): hang it head down,
            // ring at the handle, the bowl mouth BowlDrop along it so the ring sits on the pendulum's pivot.
            Rectangle opaque = OpaqueBounds(sprite.Texture, src);
            Vector2 ring = new(opaque.Left + 2 - src.X, opaque.Bottom - 2 - src.Y), head = new(opaque.Right - 2 - src.X, opaque.Top + 2 - src.Y);
            Vector2 axis = head - ring;
            float length = Math.Max(1, axis.Length());
            Vector2 along = axis / length, across = new(-along.Y, along.X);
            float reach = Math.Min(length * .85f, CrimsonRewardRules.BowlDrop / Math.Max(.01f, sprite.Scale));
            Vector2 mouth = ring + along * reach;
            float turn = MathHelper.PiOver2 - MathF.Atan2(axis.Y, axis.X);
            return body = new CenserBody(sprite.Texture, src, sprite.Scale, false, ring, mouth, mouth - across * 5, mouth + across * 5, turn);
        }
        catch (Exception e)
        {
            failed = true;
            ConvergenceMod.Instance.Logger.Warn("Ember Censer body art unavailable; the censers still work.", e);
            return null;
        }
    }

    internal static void Reset() { body = null; failed = false; }

    private static Rectangle OpaqueBounds(Texture2D texture, Rectangle src)
    {
        var pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);
        int x0 = src.Right, y0 = src.Bottom, x1 = -1, y1 = -1;
        for (int y = src.Top; y < src.Bottom; y++)
            for (int x = src.Left; x < src.Right; x++)
                if (pixels[y * texture.Width + x].A > 8)
                { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
        return x1 >= x0 ? new Rectangle(x0, y0, x1 - x0 + 1, y1 - y0 + 1) : src;
    }
}
