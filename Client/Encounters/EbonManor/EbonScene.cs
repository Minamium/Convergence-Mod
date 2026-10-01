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
// render age: chalk lanes before Fire, lit bands while live, and art (props,
// chandeliers, shears) where the collision actually is.
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
                Vector2 to = CrashPoint(p), from = age < p.Fire ? new Vector2(p.X, p.Y) : EbonGeometry.Prop(p, age);
                // Only the route still ahead of the furniture is drawn: that is the danger.
                if (Vector2.Dot(to - from, d) > 8)
                    EbonMaterials.Lane(shader, from, to, p.Width, progress, 0, EbonMaterials.Chalk, appear * (age < p.Fire ? .95f : .55f), 1, seed);
                if (age < p.Fire)
                    EbonMaterials.Glow(shader, to, 70 + 30 * progress, EbonMaterials.Silk, .35f * appear * (.6f + .4f * MathF.Sin(age * .3f)));
                break;
            }
            case EbonAttackKind.Chandelier:
            {
                int impact = EbonGeometry.ImpactTick(p);
                var burst = EbonGeometry.Burst(p);
                if (age < impact)
                {
                    var body = age < p.Fire ? EbonGeometry.Chandelier(p, p.Fire) : EbonGeometry.Chandelier(p, age);
                    EbonMaterials.Lane(shader, body, burst, EbonRules.ChandelierBodyRadius, progress, 0, EbonMaterials.Rose, appear * .75f, 1, seed);
                    Dome(shader, burst, EbonRules.BurstRadius, appear * (age < p.Fire ? .55f + .35f * progress : .95f), progress, 0, age);
                }
                else if (age < p.End) Dome(shader, burst, EbonRules.BurstRadius, 1, 1, 1 - (age - impact) / EbonRules.BurstTicks, age);
                break;
            }
            case EbonAttackKind.Loom:
            case EbonAttackKind.Shears:
            {
                if (age >= p.End || !Segment(s, p, out var a, out var b)) return;
                bool live = age >= p.Fire;
                var tint = p.Kind == EbonAttackKind.Loom ? EbonMaterials.Silk : EbonMaterials.Chalk;
                if (!live) EbonMaterials.Lane(shader, a, b, p.Width, progress, 0, tint, appear * (p.Kind == EbonAttackKind.Loom ? .8f : .95f), 1, seed);
                else if (p.Kind == EbonAttackKind.Loom)
                    EbonMaterials.Lane(shader, a, b, p.Width, 1, 1, tint, 1 - EbonVisualsMath.Ease((age - p.End + 3) / 3), 1, seed);
                break;
            }
            case EbonAttackKind.Web:
            {
                if (age >= p.End || !Segment(s, p, out var a, out var b)) return;
                float extend = EbonVisualsMath.OutExpo((age - p.Born) / 8);
                if (age < p.Fire) EbonMaterials.Lane(shader, a, Vector2.Lerp(a, b, extend), p.Width, progress, 0, EbonMaterials.Silk, appear * .85f, 1, seed);
                else EbonMaterials.Lane(shader, a, b, p.Width, 1, 1, EbonMaterials.Silk, 1 - EbonVisualsMath.Ease((age - p.End + 3) / 3), 1, seed);
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
                // The lace hub opens with the parasol and turns with the spokes.
                EbonMaterials.Lace(shader, c, 58, p.Variant, EbonGeometry.SpokeAngle(p, 0, age),
                    EbonVisualsMath.Ease((age - p.Born) / 30), .8f * fade, p.Born * .01f);
                for (int k = 0; k < p.Variant; k++)
                {
                    float angle = EbonGeometry.SpokeAngle(p, k, age);
                    var d = angle.ToRotationVector2();
                    if (!s.Field.ClipAxis(c.X, c.Y, d.X, d.Y, out _, out float last) || last <= 40) continue;
                    float reach = Math.Min(last, p.Length);
                    if (!live)
                    {
                        // Threads are drawn out to the walls, then each beat swings a faint
                        // preview the way the spokes will turn.
                        EbonMaterials.Sweep(shader, s.Field, c, angle, angle + sign * .30f * EbonVisualsMath.OutExpo(within / .7f), 40, reach,
                            EbonMaterials.Silk, .22f * (1 - within) * (1 - within) * extend, seed + k);
                        EbonMaterials.Lane(shader, c + d * 40, c + d * (40 + (reach - 40) * extend), p.Width, progress, 0, EbonMaterials.Silk, appear * .85f, 1, seed + k);
                    }
                    else
                    {
                        // Afterglow behind each spoke, as long as the arc it just swept.
                        float trail = Math.Abs(p.Spin) * EbonRules.WaltzRate(liveT) * 16;
                        EbonMaterials.Sweep(shader, s.Field, c, angle, angle - sign * trail, 40, reach, EbonMaterials.Rose, .17f * fade, seed + k);
                        EbonMaterials.Lane(shader, c + d * 40, c + d * reach, p.Width, 1, 1, EbonMaterials.Silk, fade, 1, seed + k);
                    }
                }
                break;
            }
        }
    }

    // The chandelier burst: a floor dome with a thin rim at the true radius.
    private static void Dome(ManagedShader shader, Vector2 center, float radius, float opacity, float progress, float live, float age)
    {
        float diameter = radius * 2 / .96f;
        EbonMaterials.Glow(shader, center, diameter, EbonMaterials.Rose, opacity * (.10f + .10f * progress) * (1 - live) , 0, .1f, .2f);
        EbonMaterials.Glow(shader, center, diameter, EbonMaterials.Rose, opacity * (.55f + .35f * progress), .96f, .012f, .4f);
        if (live > 0)
        {
            EbonMaterials.Glow(shader, center, diameter, EbonMaterials.Hot, live * .85f, 0, .1f, .35f);
            EbonMaterials.Glow(shader, center, diameter, EbonMaterials.Hot, live, .96f, .02f, .2f);
        }
        else if (!EbonVisuals.Reduced)
        {
            float pulse = (age * .025f) % 1;
            EbonMaterials.Glow(shader, center, diameter * (.2f + .76f * pulse), EbonMaterials.Rose, opacity * .25f * (1 - pulse) * progress, .96f, .02f, .5f);
        }
    }

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
                path.Clear(); path.Add(hook); path.Add(Vector2.Lerp(hook, spine, .5f)); path.Add(spine); path.Add(to);
                if (age < p.Fire)
                {
                    EbonMaterials.Thread(path, 2.2f, EbonMaterials.Silk, .35f * appear, progress, 0, seed, age);
                    EbonMaterials.Straight(spine, to, 2.4f, EbonMaterials.Silk, .45f * appear, progress, 0, seed, age, 5 * (1 - progress) + .6f, .12f + .4f * progress);
                }
                else
                {
                    float t = age - p.Fire;
                    EbonMaterials.Thread(path, 3f, EbonMaterials.Hot, .6f, 1, .5f, seed, age);
                    EbonMaterials.Straight(spine, to, 3.4f, EbonMaterials.Hot, .8f, 1, .7f, seed, age, 2.5f * MathF.Exp(-t / 5), 1.4f);
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
                    EbonMaterials.Thread(path, 26, EbonMaterials.Silk, .32f, 1, .5f, seed + 5, age, true);
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
                    // Cut at the hook: the upper silk recoils into the ceiling.
                    float t = age - p.Fire, recoil = EbonVisualsMath.OutExpo(t / 10);
                    var cut = ChandelierRing(p, p.Fire);
                    if (t < 14) EbonMaterials.Straight(new(cut.X, top), Vector2.Lerp(cut, new(cut.X, top), recoil), 2.8f, EbonMaterials.Hot, 1 - t / 14, 1, .8f, seed, age);
                    if (t < 18) EbonMaterials.Straight(ring - new Vector2(0, 50 * (1 - t / 18)), ring, 2.2f, EbonMaterials.Silk, .8f * (1 - t / 18), .5f, 0, seed + 1, age);
                }
                break;
            }
            case EbonAttackKind.Loom:
            {
                if (age >= p.End || !Segment(s, p, out var a, out var b)) return;
                if (age < p.Fire)
                    EbonMaterials.Straight(a, b, 3f, EbonMaterials.Silk, .65f * appear, progress, 0, seed, age, 7 * (1 - progress) + .8f, .08f + .45f * progress);
                else
                {
                    float t = age - p.Fire;
                    EbonMaterials.Straight(a, b, 6f, EbonMaterials.Hot, 1, 1, 1, seed, age, 4.5f * MathF.Exp(-t / 3.5f), 1.9f);
                }
                break;
            }
            case EbonAttackKind.Web:
            {
                if (age >= p.End || !Segment(s, p, out var a, out var b)) return;
                float t0 = age - p.Born, extend = EbonVisualsMath.OutExpo(t0 / 8);
                // Each strand is flung from her hand to its first anchor, then drawn across.
                if (t0 < 10)
                {
                    var hand = EbonNoirette.Hand(pose, p.Variant % 2);
                    EbonMaterials.Straight(hand, Vector2.Lerp(hand, a, EbonVisualsMath.OutExpo(t0 / 6)), 2.4f, EbonMaterials.Hot, 1 - t0 / 10, 1, .5f, seed, age);
                }
                if (age < p.Fire)
                    EbonMaterials.Straight(a, Vector2.Lerp(a, b, extend), 2.6f, EbonMaterials.Silk, .7f * appear, progress, 0, seed, age, 4 * (1 - progress) + .5f, .15f + .5f * progress);
                else
                    EbonMaterials.Straight(a, b, 4.5f, EbonMaterials.Hot, 1, 1, 1, seed, age, 3.5f * EbonVisualsMath.Pulse(age - p.Fire, 3), 2.2f);
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
                if (age >= p.End) return;
                var c = EbonGeometry.Center(s, age);
                bool live = age >= p.Fire;
                float fade = 1 - EbonVisualsMath.Ease((age - p.End + 6) / 6), extend = EbonVisualsMath.OutExpo((age - p.Born) / 24);
                // Every beat the taut threads twang and brighten with the step.
                float step = live ? EbonVisualsMath.Pulse((age - p.Fire) % EbonRules.BeatTicks, 7) : 0;
                for (int k = 0; k < p.Variant; k++)
                {
                    float angle = EbonGeometry.SpokeAngle(p, k, age);
                    var d = angle.ToRotationVector2();
                    if (!s.Field.ClipAxis(c.X, c.Y, d.X, d.Y, out _, out float last) || last <= 40) continue;
                    float reach = Math.Min(last, p.Length);
                    if (live)
                        EbonMaterials.Straight(c + d * 40, c + d * reach, 3.4f + 1.6f * step, EbonMaterials.Hot, fade * (.82f + .18f * step), 1,
                            .75f + .25f * step, seed + k, age, 1.6f * step, 2.6f);
                    else
                        EbonMaterials.Straight(c + d * 40, c + d * (40 + (reach - 40) * extend), 2.2f, EbonMaterials.Silk, .5f * appear, progress, 0,
                            seed + k, age, 1.5f * (1 - progress), .3f);
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
                float glint = age >= p.Fire ? 1 - (age - p.Fire) / EbonRules.WebLive : .3f + .45f * progress;
                EbonMaterials.Glow(shader, a, 40, EbonMaterials.Silk, glint);
                if (age >= p.Born + 8) EbonMaterials.Glow(shader, b, 40, EbonMaterials.Silk, glint);
                break;
            }
            case EbonAttackKind.Loom:
            {
                if (age >= p.End || !Segment(s, p, out var a, out var b)) return;
                float glint = age >= p.Fire ? 1 - (age - p.Fire) / EbonRules.LoomLive : .35f + .4f * progress;
                EbonMaterials.Glow(shader, a, 46, EbonMaterials.Silk, glint);
                EbonMaterials.Glow(shader, b, 46, EbonMaterials.Silk, glint);
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
                    EbonMaterials.Tear(shader, a, b, p.Width, t, EbonRules.ShearsLive, 1, p.Born * .017f);
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
                if (!live) return;
                float t = age - p.Fire;
                for (int k = 0; k < p.Variant; k++)
                {
                    float angle = EbonGeometry.SpokeAngle(p, k, age);
                    var d = angle.ToRotationVector2();
                    if (!s.Field.ClipAxis(c.X, c.Y, d.X, d.Y, out _, out float last) || last <= 40) continue;
                    var end = c + d * Math.Min(last, p.Length);
                    EbonMaterials.Glow(shader, end, 64, EbonMaterials.Hot, .75f * fade);
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
                    EbonMaterials.Glow(shader, e.At, 620, EbonMaterials.Hot, .9f * EbonVisualsMath.Pulse(t, 6));
                    EbonMaterials.Glow(shader, e.At, 200 + t * 14, EbonMaterials.Candle, .5f * (1 - t / 46), .93f, .03f, .6f);
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
                    if (t >= 24) break;
                    EbonMaterials.Lane(shader, e.At, e.To, e.Value * (.25f + .2f * (1 - t / 24)), 1, 0, EbonMaterials.Hot, .6f * (1 - t / 24), 1, e.Seed * .01f);
                    break;
            }
        }
    }

    // Where web strands cross, the silk knots: small lights that make the web
    // read as one woven trap. Purely decorative; each strand owns its footprint.
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
                EbonMaterials.Glow(shader, knot, 34 + 26 * live, live > 0 ? EbonMaterials.Hot : EbonMaterials.Silk, show * (.45f + .55f * live));
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

    // Primitive-only residue (snapping silk) drawn with the silk layer.
    internal static void AftermathSilk(in EbonState s, float age)
    {
        if (owner != s.Fight) return;
        for (int i = 0; i < aftermath.Length; i++)
        {
            var e = aftermath[i];
            if (!e.Valid) continue;
            float t = age - e.Start;
            if (t < 0 || t >= 14) continue;
            float recoil = EbonVisualsMath.OutExpo(t / 12), fade = 1 - t / 14;
            if (e.Kind == EbonAftermathKind.LoomSnap)
            {
                var mid = Vector2.Lerp(e.At, e.To, .5f + (EbonMaterials.Hash(e.Seed, 0, 1) - .5f) * .3f);
                EbonMaterials.Straight(e.At, Vector2.Lerp(mid, e.At, recoil), 3, EbonMaterials.Silk, fade, 1, .4f, e.Seed, age);
                EbonMaterials.Straight(Vector2.Lerp(mid, e.To, recoil), e.To, 3, EbonMaterials.Silk, fade, 1, .4f, e.Seed + 1, age);
            }
            else if (e.Kind == EbonAftermathKind.SpokeRelease)
                EbonMaterials.Straight(Vector2.Lerp(e.At, e.To, recoil), e.To, 3.5f, EbonMaterials.Hot, fade, 1, .6f, e.Seed, age);
        }
    }
}
