#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.EbonManor;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.EbonManor;

internal enum EbonAftermathKind : byte { PropCrash, ChandelierWreck, LoomSnap, ShearsHeal, SpokeRelease }
internal readonly record struct EbonAftermath(bool Valid, EbonAftermathKind Kind, Vector2 At, Vector2 To, float Value, int Start, int Seed, int Variant);

// The world pass. Every danger is drawn from the accepted plan geometry at the
// render age: quiet silk veils and hairlines before Fire, razor silk on the
// honest band while live, and art (props, chandeliers, shears) where the
// collision actually is. Only damaging strands are razor silk; every harmless
// strand (hanging, flung, recoiling, frayed) stays cool classic silk.
internal static class EbonScene
{
    internal const float PropScale = .75f;
    private static readonly Vector2 PropCentroid = new(88, 90), PropHookLocal = new(88, 10);
    private static readonly (Vector2 Ring, Vector2 Centroid, float Scale, string Name)[] Chandeliers =
    {
        (new(149.5f, 9.9f), new(149.1f, 198.1f), .75f, "ChandelierWide"),
        (new(119.6f, 13f), new(119.3f, 236f), .80f, "ChandelierTall"),
    };
    // Measured from Shears.png: two 360x140 halves, pivot holes and blades along +X.
    private static readonly Vector2 UpperPivot = new(159f, 21.5f), LowerPivot = new(137.7f, 121.8f);
    private const float ShearsScale = .92f;
    private static readonly List<Vector2> path = new(40);
    private static readonly EbonAftermath[] aftermath = new EbonAftermath[64];
    private static int next;
    private static Guid owner;

    internal static void Reset() { Array.Clear(aftermath); next = 0; owner = Guid.Empty; }
    internal static void Add(Guid fight, EbonAftermath item)
    {
        if (owner != fight) { Array.Clear(aftermath); next = 0; owner = fight; }
        aftermath[next] = item; next = (next + 1) % aftermath.Length;
    }

    // ---------- placement shared by threads, sound and art ----------
    internal static float Progress(in EbonAttackPlan p, float age) => Math.Clamp((age - p.Born) / Math.Max(1f, p.Fire - p.Born), 0, 1);
    internal static float PropRotation(in EbonAttackPlan p, float age)
    {
        float t = age - p.Fire;
        if (t < 0)
        {
            float lift = EbonVisualsMath.Ease((age - p.Born) / Math.Max(1f, p.Fire - p.Born)), progress = Progress(p, age);
            return MathF.Sin(age * .21f + p.Born) * .05f * lift + MathF.Sin(age * 1.3f + p.Born) * .025f * progress * progress;
        }
        return EbonRules.PropTravel(t) * .0045f * (MathF.Cos(p.Angle) >= 0 ? 1 : -1);
    }
    internal static Vector2 PropHook(in EbonAttackPlan p, float age)
        => EbonGeometry.Prop(p, age) + ((PropHookLocal - PropCentroid) * PropScale).RotatedBy(PropRotation(p, age));
    internal static float ClipLength(in EbonAttackPlan p) => Math.Max(0, p.Length - 160);
    internal static int CrashTick(in EbonAttackPlan p) => p.Fire + EbonRules.PropFlightTicks(ClipLength(p));
    internal static Vector2 CrashPoint(in EbonAttackPlan p) => new Vector2(p.X, p.Y) + EbonGeometry.Direction(p) * ClipLength(p);

    internal static Vector2 ChandelierBody(in EbonAttackPlan p, float age)
        => EbonGeometry.Chandelier(p, age) + new Vector2(0, age < p.Fire ? -70 * (1 - EbonVisualsMath.Ease((age - p.Born) / 24)) : 0);
    internal static float ChandelierSway(in EbonAttackPlan p, float age)
    {
        if (age < p.Fire)
        {
            float progress = Progress(p, age);
            return MathF.Sin(age * .07f + p.Born) * .035f * (1 - progress) + MathF.Sin(age * .9f) * .012f * progress;
        }
        return MathF.Sin(age * .3f) * .03f * MathF.Exp(-(age - p.Fire) / 20);
    }
    internal static Vector2 ChandelierRing(in EbonAttackPlan p, float age)
    {
        var c = Chandeliers[Math.Clamp((int)p.Variant, 0, 1)];
        return ChandelierBody(p, age) + ((c.Ring - c.Centroid) * c.Scale).RotatedBy(ChandelierSway(p, age));
    }
    internal static bool Segment(in EbonState s, in EbonAttackPlan p, out Vector2 a, out Vector2 b)
        => EbonGeometry.Line(s.Field, new(p.X, p.Y), EbonGeometry.Direction(p), out a, out b);

    // ---------- the world pass ----------
    internal static void Draw(SpriteBatch batch, EbonBoss boss, in NoirettePose pose, float age)
    {
        if (Main.dedServ) return;
        using var scope = new WorldGraphicsScope(batch);
        var s = boss.State;
        var shader = EbonMaterials.Manor(age);
        EbonCeremony.Behind(shader, boss, pose, age);
        shader = EbonMaterials.Manor(age);
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is EbonStitch marker && marker.Plan.Fight == s.Fight && EbonStitch.TryBoss(marker.Plan, out _))
                EbonStitchVisuals.Marker(shader, boss, marker, age);
            if (p.ModProjectile is EbonAttack a && a.Plan.Fight == s.Fight && a.TryBoss(out _)) Forecast(shader, s, a.Plan, age);
        }
        // Silk (primitive ribbons) above the chalk, below the furniture.
        EbonThreads.Draw(boss, pose, age);
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is EbonAttack a && a.Plan.Fight == s.Fight && a.TryBoss(out _)) Silk(s, a.Plan, pose, age);
            if (p.ModProjectile is EbonStitch marker && marker.Plan.Fight == s.Fight && EbonStitch.TryBoss(marker.Plan, out _))
                EbonStitchVisuals.Silk(boss, marker, age);
        }
        EbonCeremony.Silk(boss, pose, age);
        AftermathSilk(s, age);
        shader = EbonMaterials.Manor(age);
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is EbonAttack a && a.Plan.Fight == s.Fight && a.TryBoss(out _)) Body(shader, s, a.Plan, age);
        WebKnots(shader, s, age);
        Aftermath(shader, s, age);
        EbonCeremony.Front(shader, boss, pose, age);
    }

    // Warnings are quiet silk: one element per hazard, exactly on its accepted
    // footprint. Broad bands get a feathered veil; thread-width hazards get one
    // hairline strung anchor to anchor over a faint film of the honest band.
    private static void Forecast(ManagedShader shader, in EbonState s, in EbonAttackPlan p, float age)
    {
        if (age < p.Born) return;
        float progress = Progress(p, age), appear = EbonVisualsMath.Ease((age - p.Born) / 8), seed = p.Born * .013f + p.X * .0007f;
        switch (p.Kind)
        {
            case EbonAttackKind.Thread:
            {
                if (age >= CrashTick(p)) return;
                var d = EbonGeometry.Direction(p);
                bool live = age >= p.Fire;
                Vector2 to = CrashPoint(p), from = live ? EbonGeometry.Prop(p, age) : new Vector2(p.X, p.Y);
                // Only the route still ahead of the furniture is drawn: that is the danger.
                if (Vector2.Dot(to - from, d) > 8)
                    EbonMaterials.Veil(shader, from, to, p.Width, live ? 1 : progress, appear * (live ? .85f : 1), seed, live ? 1 : 0);
                break;
            }
            case EbonAttackKind.Chandelier:
            {
                int impact = EbonGeometry.ImpactTick(p);
                var burst = EbonGeometry.Burst(p);
                if (age < impact)
                {
                    var body = age < p.Fire ? EbonGeometry.Chandelier(p, p.Fire) : EbonGeometry.Chandelier(p, age);
                    float hold = age < p.Fire ? progress : 1;
                    // The fall corridor runs to the burst centre but fades out across the
                    // disc's edge, so the two veils meet without stacking a denser band.
                    if (burst.Y - body.Y > 8)
                        EbonMaterials.Veil(shader, body, burst, EbonRules.ChandelierBodyRadius, hold, appear, seed, 0, EbonRules.BurstRadius);
                    EbonMaterials.VeilDisc(shader, burst, EbonRules.BurstRadius, hold, appear, seed + .5f);
                }
                else if (age < p.End) Dome(shader, burst, EbonRules.BurstRadius, 1 - (age - impact) / EbonRules.BurstTicks);
                break;
            }
            case EbonAttackKind.Loom:
            {
                if (age >= p.Fire || !Segment(s, p, out var a, out var b)) return;
                float strung = EbonVisualsMath.OutExpo((age - p.Born) / 14);
                EbonMaterials.Hairline(shader, a, Vector2.Lerp(a, b, strung), p.Width, progress, appear, seed, strung, 3.5f * (1 - progress) + .3f);
                break;
            }
            case EbonAttackKind.Shears:
            {
                if (age >= p.Fire || !Segment(s, p, out var a, out var b)) return;
                // The veil is the band; one fine thread marks the line the blades will run.
                EbonMaterials.Veil(shader, a, b, p.Width, progress, appear, seed);
                EbonMaterials.Hairline(shader, a, b, 3, progress, .7f * appear, seed + .3f);
                break;
            }
            case EbonAttackKind.Web:
            {
                if (age >= p.Fire || !Segment(s, p, out var a, out var b)) return;
                float extend = EbonVisualsMath.OutExpo((age - p.Born) / 8);
                EbonMaterials.Hairline(shader, a, Vector2.Lerp(a, b, extend), p.Width, progress, appear, seed, extend, 2.5f * (1 - progress) + .3f);
                break;
            }
            case EbonAttackKind.Waltz:
            {
                if (age >= p.End) return;
                var c = EbonGeometry.Center(s, age);
                bool live = age >= p.Fire;
                float fade = 1 - EbonVisualsMath.Ease((age - p.End + 6) / 6);
                float extend = EbonVisualsMath.OutExpo((age - p.Born) / 24), sign = p.Spin >= 0 ? 1 : -1;
                float within = (age - p.Born) / EbonRules.BeatTicks % 1, liveT = age - p.Fire;
                for (int k = 0; k < p.Variant; k++)
                {
                    float angle = EbonGeometry.SpokeAngle(p, k, age);
                    var d = angle.ToRotationVector2();
                    if (!s.Field.ClipAxis(c.X, c.Y, d.X, d.Y, out _, out float last) || last <= 40) continue;
                    float reach = Math.Min(last, p.Length);
                    if (!live)
                    {
                        // Each beat a faint fan near the walls leans the way the spokes will turn.
                        EbonMaterials.Sweep(shader, s.Field, c, angle, angle + sign * .24f * EbonVisualsMath.OutExpo(within / .6f), 40, reach,
                            EbonMaterials.Silk, .18f * (1 - within) * (1 - within) * extend, seed + k, .6f);
                        EbonMaterials.Hairline(shader, c + d * 40, c + d * (40 + (reach - 40) * extend), p.Width, progress, appear, seed + k,
                            extend, 1.5f * (1 - progress));
                    }
                    else
                    {
                        // A soft afterglow behind each spoke, as long as the arc it just swept.
                        float trail = Math.Abs(p.Spin) * EbonRules.WaltzRate(liveT) * 16;
                        EbonMaterials.Sweep(shader, s.Field, c, angle, angle - sign * trail, 40, reach, EbonMaterials.Dusty, .12f * fade, seed + k);
                    }
                }
                break;
            }
        }
    }

    // The chandelier burst while live: a white flash in moon-silver with a soft
    // dusty-rose front at the true radius.
    private static void Dome(ManagedShader shader, Vector2 center, float radius, float live)
    {
        float diameter = radius * 2 / .96f;
        EbonMaterials.Glow(shader, center, diameter, EbonMaterials.Pale, live * .8f, 0, .1f, .35f);
        EbonMaterials.Glow(shader, center, diameter, EbonMaterials.Dusty, live, .96f, .025f, .25f);
    }

    // A standing wave sampled between two points (warning silk trembling as it tightens).
    private static void Wave(List<Vector2> into, Vector2 a, Vector2 b, float amplitude, float rate, float age, int steps)
    {
        var n = (b - a).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
        for (int i = 1; i <= steps; i++)
        {
            float u = i / (float)steps;
            into.Add(Vector2.Lerp(a, b, u) + n * MathF.Sin(u * MathF.PI) * MathF.Sin(u * MathF.PI * 3 + age * rate) * amplitude);
        }
    }

    // Silk ribbons through Luminance's primitive renderer. Warnings keep only the
    // silk that is physically there (hanging, pulling); every live, damaging
    // strand is razor silk on its honest band, and harmless silk stays classic.
    private static void Silk(in EbonState s, in EbonAttackPlan p, in NoirettePose pose, float age)
    {
        if (age < p.Born) return;
        float progress = Progress(p, age), appear = EbonVisualsMath.Ease((age - p.Born) / 10), seed = p.Born * .07f + p.Y * .001f;
        switch (p.Kind)
        {
            case EbonAttackKind.Thread:
            {
                int crash = CrashTick(p);
                if (age >= crash) return;
                var hook = PropHook(p, age);
                var to = CrashPoint(p);
                // The pull silk is tied to the top ring and runs down the lane's spine to the far anchor.
                var spine = EbonGeometry.Prop(p, age) + EbonGeometry.Direction(p) * 90;
                path.Clear(); path.Add(hook); path.Add(Vector2.Lerp(hook, spine, .5f)); path.Add(spine);
                if (age < p.Fire)
                {
                    Wave(path, spine, to, (3 * (1 - progress) + .3f) * (EbonVisuals.Reduced ? .4f : 1), .12f + .4f * progress, age, 12);
                    var tone = Vector3.Lerp(EbonMaterials.Silk, EbonMaterials.Dusty, EbonVisualsMath.Ease((progress - .66f) / .34f));
                    EbonMaterials.Thread(path, 2.2f, tone, (.42f + .2f * progress) * appear, progress, 0, seed, age);
                }
                else
                {
                    float t = age - p.Fire;
                    // The hook's silk is harmless classic silk; the route still ahead of
                    // the furniture is yanked taut into razor silk, the snap racing in.
                    EbonMaterials.Thread(path, 2.2f, EbonMaterials.Silk, .55f, 1, .3f, seed, age);
                    if (Vector2.Dot(to - spine, EbonGeometry.Direction(p)) > 8)
                        EbonMaterials.Razor(spine, to, 2.5f, 1, t, seed, age, 2.2f * MathF.Exp(-t / 5), 1.4f, 1, openStart: true);
                    // The released control silk whips back into her hand.
                    if (t < 12)
                    {
                        var hand = EbonNoirette.Hand(pose, p.Born / 29 % 2);
                        var start = PropHook(p, p.Fire);
                        EbonMaterials.Straight(hand, Vector2.Lerp(start, hand, EbonVisualsMath.OutExpo(t / 12)), 2.2f, EbonMaterials.Silk, 1 - t / 12, 1, .3f, seed + 3, age);
                    }
                    // Motion ribbon sampled from the same trajectory as the collision.
                    path.Clear();
                    for (int k = 8; k >= 0; k--) path.Add(EbonGeometry.Prop(p, Math.Max(p.Fire, age - k * 1.4f)));
                    EbonMaterials.Thread(path, 26, EbonMaterials.Silk, .26f, 1, .5f, seed + 5, age, true);
                }
                break;
            }
            case EbonAttackKind.Chandelier:
            {
                int impact = EbonGeometry.ImpactTick(p);
                if (age >= impact) return;
                float top = s.Field.Top - 520;
                var ring = ChandelierRing(p, age);
                if (age < p.Fire)
                    EbonMaterials.Straight(new(ring.X, top), ring, 2.6f, EbonMaterials.Silk, .75f * appear, progress, 0, seed, age, progress > .7f ? 1.2f : 0, 1.1f);
                else
                {
                    // Cut at the hook: the harmless upper silk recoils into the ceiling.
                    float t = age - p.Fire, recoil = EbonVisualsMath.OutExpo(t / 10);
                    var cut = ChandelierRing(p, p.Fire);
                    if (t < 14)
                        EbonMaterials.Straight(new(cut.X, top), Vector2.Lerp(cut, new(cut.X, top), recoil), 2.2f, EbonMaterials.Silk, .6f * (1 - t / 14), 1, .3f, seed, age);
                    if (t < 18) EbonMaterials.Straight(ring - new Vector2(0, 50 * (1 - t / 18)), ring, 2.2f, EbonMaterials.Silk, .8f * (1 - t / 18), .5f, 0, seed + 1, age);
                }
                break;
            }
            case EbonAttackKind.Loom:
            {
                if (age >= p.End || age < p.Fire || !Segment(s, p, out var a, out var b)) return;
                float t = age - p.Fire;
                EbonMaterials.Razor(a, b, p.Width, 1, t, seed, age, 4.5f * MathF.Exp(-t / 3.5f), 1.9f);
                break;
            }
            case EbonAttackKind.Web:
            {
                if (age >= p.End || !Segment(s, p, out var a, out var b)) return;
                float t0 = age - p.Born;
                // Each strand is flung from her hand to its first anchor (harmless silk), then drawn across.
                if (t0 < 10)
                {
                    var hand = EbonNoirette.Hand(pose, p.Variant % 2);
                    EbonMaterials.Straight(hand, Vector2.Lerp(hand, a, EbonVisualsMath.OutExpo(t0 / 6)), 2.2f, EbonMaterials.Silk, .55f * (1 - t0 / 10), 1, .2f, seed, age);
                }
                if (age >= p.Fire)
                {
                    float t = age - p.Fire;
                    EbonMaterials.Razor(a, b, p.Width, 1, t, seed, age, 3.5f * EbonVisualsMath.Pulse(t, 3), 2.2f);
                }
                break;
            }
            case EbonAttackKind.Shears:
            {
                if (age >= p.Fire || !Segment(s, p, out var a, out var b)) return;
                // The shears hang on silk above their pivot while they wait.
                var pivot = a + (b - a).SafeNormalize(Vector2.UnitX) * 190;
                EbonMaterials.Straight(new(pivot.X, s.Field.Top - 520), pivot, 2.2f, EbonMaterials.Silk, .55f * appear, progress, 0, seed, age);
                break;
            }
            case EbonAttackKind.Waltz:
            {
                if (age >= p.End || age < p.Fire) return;
                var c = EbonGeometry.Center(s, age);
                float fade = 1 - EbonVisualsMath.Ease((age - p.End + 6) / 6);
                // Every beat the taut spokes snap brighter and twang with the step; the
                // snap is full at fire and softer on the eleven beats that follow.
                float liveT = age - p.Fire, beat = liveT % EbonRules.BeatTicks, step = EbonVisualsMath.Pulse(beat, 7);
                float flash = liveT < EbonRules.BeatTicks ? 1 : EbonVisuals.Reduced ? .25f : .5f;
                for (int k = 0; k < p.Variant; k++)
                {
                    float angle = EbonGeometry.SpokeAngle(p, k, age);
                    var d = angle.ToRotationVector2();
                    if (!s.Field.ClipAxis(c.X, c.Y, d.X, d.Y, out _, out float last) || last <= 40) continue;
                    EbonMaterials.Razor(c + d * 40, c + d * Math.Min(last, p.Length), p.Width, fade, beat, seed + k, age, 1.6f * step, 2.6f, flash, openStart: true);
                }
                break;
            }
        }
    }

    private static void Body(ManagedShader shader, in EbonState s, in EbonAttackPlan p, float age)
    {
        if (age < p.Born) return;
        float progress = Progress(p, age);
        switch (p.Kind)
        {
            case EbonAttackKind.Thread:
            {
                if (age >= CrashTick(p)) return;
                var art = EbonMaterials.Texture("ManorProps");
                float weave = EbonVisualsMath.Ease((age - p.Born) / 14), t = age - p.Fire;
                shader.TrySetParameter("signal", new Vector4(1, weave, t >= 0 ? .6f * EbonVisualsMath.Pulse(t, 5) : .25f * progress * progress, p.Variant * .31f));
                bool flip = MathF.Cos(p.Angle) < 0;
                EbonMaterials.Sprite(shader, "AutoloadPass", art, new Rectangle(p.Variant * 176, 0, 176, 176), EbonGeometry.Prop(p, age),
                    PropCentroid, PropScale, PropRotation(p, age), flip, SamplerState.LinearClamp);
                // The pull silk's far anchor: a small pin of light where the furniture will land.
                float pin = t >= 0 ? 1 : (.45f + .3f * progress) * EbonVisualsMath.Ease((age - p.Born) / 8);
                EbonMaterials.Glint(shader, CrashPoint(p), 20 + 8 * progress, EbonMaterials.Pale, pin * (.8f + .2f * MathF.Sin(age * .3f)), .8f, p.Angle);
                break;
            }
            case EbonAttackKind.Chandelier:
            {
                int impact = EbonGeometry.ImpactTick(p);
                if (age >= impact) return;
                var c = Chandeliers[Math.Clamp((int)p.Variant, 0, 1)];
                var art = EbonMaterials.Texture(c.Name);
                var body = ChandelierBody(p, age);
                float weave = EbonVisualsMath.Ease((age - p.Born) / 20), falling = age >= p.Fire ? 1 : 0;
                // Candle light belongs to the chandelier; it streams upward while falling.
                float flicker = .8f + .2f * MathF.Sin(age * .9f + p.Born);
                EbonMaterials.Glow(shader, body - new Vector2(0, 30 + 26 * falling), 260 * c.Scale + 60 * falling, EbonMaterials.Candle, .22f * weave * flicker);
                shader.TrySetParameter("signal", new Vector4(1, weave, falling * .35f * EbonVisualsMath.Pulse(age - p.Fire, 6), p.Born * .11f));
                EbonMaterials.Sprite(shader, "AutoloadPass", art, new Rectangle(0, 0, art.Width, art.Height), body, c.Centroid, c.Scale,
                    ChandelierSway(p, age), false, SamplerState.LinearClamp);
                break;
            }
            case EbonAttackKind.Web:
            {
                if (age >= p.End || !Segment(s, p, out var a, out var b)) return;
                // Anchor pins where the strand is tied to the walls.
                bool live = age >= p.Fire;
                float glint = live ? 1 - (age - p.Fire) / EbonRules.WebLive : .3f + .4f * progress, rotation = (b - a).ToRotation();
                EbonMaterials.Glint(shader, a, live ? 30 : 18, live ? EbonMaterials.Bloom : EbonMaterials.Silk, glint, .8f, rotation);
                if (age >= p.Born + 8) EbonMaterials.Glint(shader, b, live ? 30 : 18, live ? EbonMaterials.Bloom : EbonMaterials.Silk, glint, .8f, rotation);
                break;
            }
            case EbonAttackKind.Loom:
            {
                if (age >= p.End || !Segment(s, p, out var a, out var b)) return;
                bool live = age >= p.Fire;
                float glint = live ? 1 - (age - p.Fire) / EbonRules.LoomLive : (.3f + .4f * progress) * EbonVisualsMath.Ease((age - p.Born) / 8),
                    rotation = (b - a).ToRotation();
                EbonMaterials.Glint(shader, a, live ? 34 : 20, live ? EbonMaterials.Bloom : EbonMaterials.Silk, glint, .8f, rotation);
                if (live || age >= p.Born + 14) EbonMaterials.Glint(shader, b, live ? 34 : 20, live ? EbonMaterials.Bloom : EbonMaterials.Silk, glint, .8f, rotation);
                break;
            }
            case EbonAttackKind.Shears:
            {
                if (age >= p.End || !Segment(s, p, out var a, out var b)) return;
                var d = (b - a).SafeNormalize(Vector2.UnitX);
                float rotation = d.ToRotation(), t = age - p.Fire;
                Vector2 pivot; float open, weave = EbonVisualsMath.Ease((age - p.Born) / 16), opacity = 1;
                if (t < 0)
                {
                    pivot = a + d * 190;
                    float beat = (age - p.Born) / EbonRules.BeatTicks % 1;
                    open = .42f + .10f * MathF.Exp(-beat * 5) + .22f * EbonVisualsMath.Ease((age - (p.Fire - 14)) / 14);
                }
                else
                {
                    // The live band is the whole line; the blades race along it snipping.
                    float run = EbonVisualsMath.OutExpo(t / EbonRules.ShearsLive);
                    pivot = Vector2.Lerp(a + d * 190, b - d * 60, run);
                    open = .55f * MathF.Abs(MathF.Cos(t * .75f));
                    EbonMaterials.Tear(shader, a, b, p.Width, t, EbonRules.ShearsLive, 1, p.Born * .017f, Vector2.Dot(pivot - a, d));
                    opacity = 1 - EbonVisualsMath.Ease((t - EbonRules.ShearsLive + 4) / 4);
                }
                var art = EbonMaterials.Texture("Shears");
                shader.TrySetParameter("signal", new Vector4(opacity, weave, t >= 0 ? .4f : .2f * progress, p.Born * .09f));
                EbonMaterials.Sprite(shader, "AutoloadPass", art, new Rectangle(0, 0, 360, 140), pivot, UpperPivot, ShearsScale, rotation - open, false, SamplerState.LinearClamp);
                EbonMaterials.Sprite(shader, "AutoloadPass", art, new Rectangle(0, 140, 360, 140), pivot, LowerPivot, ShearsScale, rotation + open, false, SamplerState.LinearClamp);
                break;
            }
            case EbonAttackKind.Waltz:
            {
                if (age >= p.End) return;
                var c = EbonGeometry.Center(s, age);
                bool live = age >= p.Fire;
                float fade = 1 - EbonVisualsMath.Ease((age - p.End + 6) / 6);
                EbonMaterials.Glow(shader, c, 150, EbonMaterials.Moon, (.22f + .18f * progress) * fade);
                if (!live)
                {
                    // Each beat a small glint slides along the wall the way the spokes will turn.
                    float within = (age - p.Born) / EbonRules.BeatTicks % 1;
                    float lean = (p.Spin >= 0 ? 1 : -1) * .24f * EbonVisualsMath.OutExpo(within / .6f);
                    float show = (1 - within) * (1 - within) * EbonVisualsMath.OutExpo((age - p.Born) / 24);
                    for (int k = 0; k < p.Variant; k++)
                    {
                        float angle = EbonGeometry.SpokeAngle(p, k, age) + lean;
                        var d = angle.ToRotationVector2();
                        if (!s.Field.ClipAxis(c.X, c.Y, d.X, d.Y, out _, out float last) || last <= 40) continue;
                        EbonMaterials.Glint(shader, c + d * Math.Min(last, p.Length), 44, EbonMaterials.Pale, show, 1, angle);
                    }
                    return;
                }
                float t = age - p.Fire;
                for (int k = 0; k < p.Variant; k++)
                {
                    float angle = EbonGeometry.SpokeAngle(p, k, age);
                    var d = angle.ToRotationVector2();
                    if (!s.Field.ClipAxis(c.X, c.Y, d.X, d.Y, out _, out float last) || last <= 40) continue;
                    var end = c + d * Math.Min(last, p.Length);
                    EbonMaterials.Glint(shader, end, 34, EbonMaterials.Bloom, .85f * fade, .8f, angle);
                    // Silk sparks where each spoke scrapes along the wall.
                    if (!EbonVisuals.Reduced)
                        EbonMaterials.Burst(shader, end, p.Born + k * 31 + (int)(t / 18) * 7, 4, t % 18, 18, 2, 3.5f, .03f, 12, 1.4f, angle + MathHelper.Pi);
                }
                break;
            }
        }
    }

    private static void Aftermath(ManagedShader shader, in EbonState s, float age)
    {
        if (owner != s.Fight) return;
        for (int i = 0; i < aftermath.Length; i++)
        {
            var e = aftermath[i];
            if (!e.Valid) continue;
            float t = age - e.Start;
            if (t < 0) continue;
            switch (e.Kind)
            {
                case EbonAftermathKind.PropCrash:
                    if (t >= 56) break;
                    EbonMaterials.Glow(shader, e.At, 220, EbonMaterials.Hot, .8f * EbonVisualsMath.Pulse(t, 5));
                    EbonMaterials.Glow(shader, e.At, 60 + t * 9, EbonMaterials.Silk, .4f * (1 - t / 36), .9f, .05f, .6f);
                    EbonMaterials.Burst(shader, e.At, e.Seed, 16, t, 56, 1, 7, .32f, 26, MathHelper.Pi * 1.3f, e.Value + MathHelper.Pi);
                    EbonMaterials.Burst(shader, e.At, e.Seed + 7, 9, t, 44, 0, 8.5f, .30f, 16, MathHelper.Pi * 1.5f, e.Value + MathHelper.Pi);
                    break;
                case EbonAftermathKind.ChandelierWreck:
                {
                    if (t >= 96) break;
                    var c = Chandeliers[Math.Clamp(e.Variant, 0, 1)];
                    var art = EbonMaterials.Texture(c.Name);
                    float settle = EbonVisualsMath.Ease(t / 8);
                    EbonMaterials.Glow(shader, e.At, 620, EbonMaterials.Pale, (EbonVisuals.Reduced ? .35f : .65f) * EbonVisualsMath.Pulse(t, 6));
                    // A soft gold shock ring (no second hard ring beside the rose front).
                    EbonMaterials.Glow(shader, e.At, 200 + t * 14, EbonMaterials.Gold, .3f * (1 - t / 46), .93f, .06f, .6f);
                    shader.TrySetParameter("signal", new Vector4(1 - EbonVisualsMath.Ease((t - 70) / 26), 1 - EbonVisualsMath.Ease((t - 30) / 60), .5f * EbonVisualsMath.Pulse(t, 4), e.Seed * .01f));
                    EbonMaterials.Sprite(shader, "AutoloadPass", art, new Rectangle(0, 0, art.Width, art.Height),
                        e.At + new Vector2(0, -40 * c.Scale + 10 * settle), c.Centroid, c.Scale * (1 - .12f * settle), e.Value * settle, false, SamplerState.LinearClamp, .45f);
                    EbonMaterials.Burst(shader, e.At, e.Seed, 30, t, 80, 0, 9, .34f, 20, MathHelper.Pi * 1.25f, -MathHelper.PiOver2);
                    if (!EbonVisuals.Reduced)
                        for (int k = 0; k < 7; k++)
                        {
                            float h = EbonMaterials.Hash(e.Seed, k, 9), rise = t * (1.1f + h);
                            var ember = e.At + new Vector2((h - .5f) * 300 + MathF.Sin(t * .1f + k) * 12, -rise);
                            EbonMaterials.Glow(shader, ember, 26, EbonMaterials.Candle, .55f * (1 - t / 96));
                        }
                    break;
                }
                case EbonAftermathKind.ShearsHeal:
                {
                    if (t >= 16) break;
                    // The cut closes into one faint dusty seam that dissolves; harmless, so no razor.
                    float close = EbonVisualsMath.OutExpo(t / 8);
                    EbonMaterials.Veil(shader, e.At, e.To, MathHelper.Lerp(e.Value * .3f, 5, close), 1, 1.1f * (1 - EbonVisualsMath.Ease(t / 16)),
                        e.Seed * .013f, 1);
                    break;
                }
            }
        }
    }

    // Where web strands cross, the silk catches the light: small crisp glints
    // along the bisectors that make the web read as one woven trap. Purely
    // decorative; each strand owns its footprint.
    private static readonly List<(Vector2 A, Vector2 B, float Show, int Fire)> strands = new(16);
    private static void WebKnots(ManagedShader shader, in EbonState s, float age)
    {
        strands.Clear();
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is not EbonAttack a || a.Plan.Fight != s.Fight || a.Plan.Kind != EbonAttackKind.Web || age < a.Plan.Born + 8
                || age >= a.Plan.End || strands.Count >= 16 || !a.TryBoss(out _) || !Segment(s, a.Plan, out var from, out var to)) continue;
            strands.Add((from, to, EbonVisualsMath.Ease((age - a.Plan.Born - 8) / 10), a.Plan.Fire));
        }
        for (int i = 0; i < strands.Count; i++)
            for (int j = i + 1; j < strands.Count; j++)
            {
                if (strands[i].Fire != strands[j].Fire || !Cross(strands[i].A, strands[i].B, strands[j].A, strands[j].B, out var knot)) continue;
                int fire = strands[i].Fire;
                float live = age >= fire ? 1 - (age - fire) / EbonRules.WebLive : 0;
                float show = Math.Min(strands[i].Show, strands[j].Show);
                float bisector = ((strands[i].B - strands[i].A).ToRotation() + (strands[j].B - strands[j].A).ToRotation()) * .5f;
                if (live > 0) EbonMaterials.Glint(shader, knot, 26 + 12 * live, EbonMaterials.Bloom, show * live, 1, bisector);
                else EbonMaterials.Glint(shader, knot, 14, EbonMaterials.Silk, show * .3f, .5f, bisector);
            }
    }
    private static bool Cross(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 at)
    {
        at = default;
        Vector2 r = b - a, q = d - c;
        float denominator = r.X * q.Y - r.Y * q.X;
        if (MathF.Abs(denominator) < 1e-3f) return false;
        float t = ((c.X - a.X) * q.Y - (c.Y - a.Y) * q.X) / denominator, u = ((c.X - a.X) * r.Y - (c.Y - a.Y) * r.X) / denominator;
        if (t is < 0 or > 1 || u is < 0 or > 1) return false;
        at = a + r * t;
        return true;
    }

    // Primitive-only residue drawn with the silk layer. Once a strand stops
    // dealing damage it loses its band and white heat at once: still tied to its
    // wall, it slackens back, droops and frays into fading motes.
    internal static void AftermathSilk(in EbonState s, float age)
    {
        if (owner != s.Fight) return;
        for (int i = 0; i < aftermath.Length; i++)
        {
            var e = aftermath[i];
            if (!e.Valid) continue;
            float t = age - e.Start;
            if (t < 0 || t >= 18) continue;
            float fray = EbonVisualsMath.Ease(t / 18), recoil = .35f * EbonVisualsMath.OutExpo(t / 10), sag = 24 * EbonVisualsMath.Ease(t / 18);
            switch (e.Kind)
            {
                case EbonAftermathKind.LoomSnap:
                {
                    // Parted mid-strand: each half hangs from its own wall.
                    var mid = Vector2.Lerp(e.At, e.To, .5f + (EbonMaterials.Hash(e.Seed, 0, 1) - .5f) * .3f);
                    Droop(path, e.At, Vector2.Lerp(mid, e.At, recoil), sag);
                    EbonMaterials.Frayed(path, 3, fray, e.Seed, age, false, true);
                    Droop(path, e.To, Vector2.Lerp(mid, e.To, recoil), sag);
                    EbonMaterials.Frayed(path, 3, fray, e.Seed + 1, age, false, true);
                    break;
                }
                case EbonAftermathKind.SpokeRelease:
                    // Let go at the hub: the spoke hangs from its wall end.
                    Droop(path, e.To, Vector2.Lerp(e.At, e.To, recoil), sag);
                    EbonMaterials.Frayed(path, 3, fray, e.Seed, age, false, true);
                    break;
            }
        }
    }

    // A strand tied at anchor whose other end has let go, sampled anchor to free
    // end and drooping more toward the free end.
    private static void Droop(List<Vector2> into, Vector2 anchor, Vector2 free, float sag)
    {
        into.Clear();
        for (int i = 0; i <= 13; i++)
        {
            float u = i / 13f;
            into.Add(Vector2.Lerp(anchor, free, u) + new Vector2(0, sag * u * u));
        }
    }
}
