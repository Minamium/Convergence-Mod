#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.EbonManor;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.EbonManor;

// Entrance, act changes and endings, all on AutoMatador's bar grid from the
// accepted clocks. Decoration only: no camera/input flags, no hitboxes.
// The entrance follows the intro's own structure (see EbonIntro).
internal static class EbonCeremony
{
    // The drawn root before an act change moved her; captured by EbonVisuals.
    internal static Vector2? Departure;
    internal static int DepartureEpoch = -1;
    private static readonly List<Vector2> curve = new(28);

    internal static Vector2 Invitation(EbonBoss boss, float age) => EbonNoirette.Root(boss, age) + new Vector2(0, MathF.Sin(age * .04f) * 6);

    // Behind the attacks: the waiting invitation and its burst, the break's
    // spotlight, the gathering motes and the drop's ring of light.
    internal static void Behind(ManagedShader shader, EbonBoss boss, in NoirettePose pose, float age)
    {
        var s = boss.State;
        float b = EbonIntro.Bars(s, age);
        bool countdown = s.Stage == EbonStage.Countdown;
        var heart = Invitation(boss, age);
        if (s.Stage is EbonStage.Deployment or EbonStage.Ready || countdown && b < EbonIntro.Burst + .3f)
        {
            // The card trembles as the threads arrive and the first build climbs,
            // then unravels into silk on the build's peak.
            float tremble = countdown ? EbonVisualsMath.Ease((b - EbonIntro.Threads) / 4) : 0;
            float unravel = countdown ? EbonVisualsMath.Ease((b - (EbonIntro.Burst - .5f)) / .6f) : 0;
            float glow = .30f * (1 - unravel) + .25f * MathF.Pow(.5f + .5f * MathF.Sin(age * .05f), 3) + .3f * tremble;
            EbonMaterials.Glow(shader, heart, 190 + 80 * tremble, EbonMaterials.Moon, glow);
            var art = EbonMaterials.Texture("BlackInvitation");
            shader.TrySetParameter("signal", new Vector4(1, 1 - unravel, .3f * unravel + .25f * tremble * tremble, 7));
            float rotation = -.16f + MathF.Sin(age * .03f) * .08f + MathF.Sin(age * .9f) * .05f * tremble;
            EbonMaterials.Sprite(shader, "AutoloadPass", art, new Rectangle(0, 0, art.Width, art.Height), heart, art.Size() * .5f, 2,
                rotation, false, SamplerState.PointClamp, 0, 1, 10);
        }
        if (countdown)
        {
            float burst = (b - EbonIntro.Burst) * EbonRules.BarTicks;
            if (burst >= 0 && burst < 70)
            {
                EbonMaterials.Glow(shader, heart, 260 + burst * 9, EbonMaterials.Moon, .7f * (1 - burst / 70), .9f, .04f, .6f);
                EbonMaterials.Burst(shader, heart, 91, 26, burst, 70, 2, 5, -.02f, 18);
            }
            // The near-silent break: a moonlit spotlight while she is woven in.
            float spot = EbonVisualsMath.Ease((b - EbonIntro.Break) * 2) * (1 - EbonVisualsMath.Ease((b - EbonIntro.Bloom - .4f) * 2));
            EbonMaterials.Glow(shader, pose.Root, 440, EbonMaterials.Moon, .38f * spot);
            // The bass returns: motes of silk spiral into her hands.
            if (b >= EbonIntro.Gather && b < EbonIntro.Drop && !EbonVisuals.Reduced)
            {
                var hand = EbonNoirette.Hand(pose);
                float gather = EbonVisualsMath.Ease((b - EbonIntro.Gather) * 4) * (1 - EbonVisualsMath.Ease((b - EbonIntro.Drop + .1f) * 10));
                for (int k = 0; k < 12; k++)
                {
                    float u = ((b - EbonIntro.Gather) * 2 + k / 12f) % 1;
                    var d = (k * 2.39996f + age * .02f).ToRotationVector2();
                    EbonMaterials.Glow(shader, hand + d * 300 * MathF.Pow(1 - u, 1.5f), 24, EbonMaterials.Hot, .6f * u * gather);
                }
            }
        }
        // The drop: a ring of light and the first silk flung to the walls.
        if (s.UnlockAt >= 0)
        {
            float t = age - s.UnlockAt;
            if (t >= 0 && t < 40) EbonMaterials.Glow(shader, pose.Root, 200 + t * 30, EbonMaterials.Moon, .6f * (1 - t / 40), .9f, .04f, .7f);
        }
        if (s.Phase != EbonPhase.ActOne && s.Stage == EbonStage.Performance)
        {
            float t = age - s.PhaseAt;
            if (Departure is { } from && DepartureEpoch == s.PhaseAt && t < 40)
            {
                EbonMaterials.Glow(shader, from, 180, EbonMaterials.Silk, .7f * (1 - t / 40));
                EbonMaterials.Burst(shader, from, s.PhaseAt, 20, t, 40, 2, 5, -.03f, 22);
            }
            float epoch = age - s.Epoch;
            if (epoch >= 0 && epoch < 44) EbonMaterials.Glow(shader, pose.Root, 240 + epoch * 36, s.Phase == EbonPhase.Finale ? EbonMaterials.Hot : EbonMaterials.Moon, .7f * (1 - epoch / 44), .9f, .04f, .7f);
        }
        if (s.EndAt >= 0 && s.Stage == EbonStage.Victory)
        {
            float t = age - s.EndAt;
            if (t < 50) EbonMaterials.Glow(shader, pose.Root, 460, EbonMaterials.Hot, .8f * EbonVisualsMath.Pulse(t, 10));
            if (t >= 150 && t < 330) EbonMaterials.Burst(shader, pose.Root, 404, 26, t - 150, 180, 2, 2.2f, -.012f, 20);
        }
    }

    // Silk layer (primitives).
    internal static void Silk(EbonBoss boss, in NoirettePose pose, float age)
    {
        var s = boss.State;
        var field = s.Field;
        float b = EbonIntro.Bars(s, age);
        bool countdown = s.Stage == EbonStage.Countdown;
        var heart = Invitation(boss, age);
        if (s.Stage is EbonStage.Deployment or EbonStage.Ready || countdown && b < EbonIntro.Burst + .3f)
        {
            float unravel = countdown ? EbonVisualsMath.Ease((b - (EbonIntro.Burst - .5f)) / .6f) : 0;
            EbonMaterials.Straight(new(heart.X + 4, field.Top - 520), heart + new Vector2(4, -40), 2, EbonMaterials.Silk, .6f * (1 - unravel), .4f, 0, 1, age);
        }
        if (countdown && b >= EbonIntro.Threads && b < EbonIntro.Burst + .6f)
        {
            // From bar 4 one thread falls to the card every half bar; the first
            // build tightens them, and on its peak they whip back into the dark.
            float tension = EbonVisualsMath.Ease((b - 6) / 2), burst = EbonVisualsMath.Ease((b - EbonIntro.Burst) / .4f);
            for (int i = 0; i < 6; i++)
            {
                float start = EbonIntro.Threads + i * .5f;
                if (b < start) continue;
                var top = new Vector2(heart.X + (i - 2.5f) * 230, field.Top - 520);
                var tip = Vector2.Lerp(top, heart, EbonVisualsMath.OutExpo((b - start) / .9f));
                if (burst > 0) tip = Vector2.Lerp(tip, top, EbonVisualsMath.OutExpo(burst));
                EbonMaterials.Straight(top, tip, 2.4f, EbonMaterials.Silk, .75f * (1 - burst), .3f + .7f * tension, .3f * tension, i, age,
                    3 * (1 - tension) + .4f, .2f + .8f * tension);
            }
        }
        if (countdown && b >= EbonIntro.Break - .1f && b < EbonIntro.Bloom + .3f)
        {
            // The break: a silk cocoon unwinds around her as she is woven in.
            float u = Math.Clamp(b - EbonIntro.Break, 0, 1), fade = 1 - EbonVisualsMath.Ease((b - EbonIntro.Bloom) / .3f);
            for (int ring = 0; ring < 3; ring++)
            {
                float radius = (74 - ring * 15) * (1 - .5f * u), spin = age * (.04f + ring * .02f) * (ring % 2 == 0 ? 1 : -1);
                curve.Clear();
                for (int j = 0; j <= 24; j++)
                {
                    float a = j / 24f * MathHelper.TwoPi + spin;
                    curve.Add(pose.Root + new Vector2(MathF.Cos(a) * radius, MathF.Sin(a) * radius * 1.35f + (ring - 1) * 18));
                }
                EbonMaterials.Thread(curve, 2.2f, EbonMaterials.Silk, .7f * fade * (1 - u * .4f), u, .2f, ring, age);
            }
        }
        if (countdown)
        {
            // The parasol opens on the second build with a flick of short threads.
            float bloom = (b - EbonIntro.Bloom) * EbonRules.BarTicks;
            if (bloom >= 0 && bloom < 36) Radiate(pose, field, age, bloom, 6, EbonMaterials.Silk, .5f, 280);
            if (b >= EbonIntro.Gather && b < EbonIntro.Drop)
            {
                // The bass returns: threads run from the walls into her hands and the
                // two held beats before the drop draw them taut.
                float hush = EbonVisualsMath.Ease((b - EbonIntro.Hush) * 4);
                for (int k = 0; k < 8; k++)
                {
                    var hand = EbonNoirette.Hand(pose, k % 2);
                    var d = (k / 8f * MathHelper.TwoPi + .3f).ToRotationVector2();
                    if (!field.ClipAxis(hand.X, hand.Y, d.X, d.Y, out _, out float last)) continue;
                    var wall = hand + d * last;
                    float reach = EbonVisualsMath.OutExpo((b - EbonIntro.Gather - k * .06f) / .5f);
                    EbonMaterials.Straight(wall, Vector2.Lerp(wall, hand, reach), 2.2f + hush, hush > .5f ? EbonMaterials.Hot : EbonMaterials.Silk, .7f,
                        .5f + .5f * hush, .6f * hush, k, age, 2.5f * (1 - hush), .3f);
                }
            }
        }
        if (s.UnlockAt >= 0)
        {
            float t = age - s.UnlockAt;
            if (t >= 0 && t < 36) Radiate(pose, field, age, t, 8, EbonMaterials.Hot, 0);
        }
        if (s.Phase != EbonPhase.ActOne && s.Stage == EbonStage.Performance)
        {
            float t = age - s.PhaseAt;
            // The re-weave: silk streaks carry her from the old station to the centre.
            if (Departure is { } from && DepartureEpoch == s.PhaseAt && t >= 4 && t < 48)
            {
                var to = pose.Root;
                var normal = (to - from).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                float head = EbonVisualsMath.Ease((t - 4) / 22), tail = EbonVisualsMath.Ease((t - 14) / 30);
                for (int k = 0; k < 5; k++)
                {
                    var control = (from + to) * .5f + normal * (k - 2) * 70;
                    curve.Clear();
                    for (int j = 0; j <= 16; j++)
                    {
                        float u = MathHelper.Lerp(tail, head, j / 16f);
                        curve.Add((1 - u) * (1 - u) * from + 2 * (1 - u) * u * control + u * u * to);
                    }
                    EbonMaterials.Thread(curve, 3, k % 2 == 0 ? EbonMaterials.Silk : EbonMaterials.Hot, .8f * (1 - tail * tail), 1, .4f, k, age, true);
                }
            }
            float epoch = age - s.Epoch;
            if (epoch >= 0 && epoch < 36) Radiate(pose, field, age, epoch, s.Phase == EbonPhase.Finale ? 10 : 6, EbonMaterials.Hot, s.PhaseAt * .1f);
        }
        if (s.EndAt >= 0)
        {
            float t = age - s.EndAt;
            if (s.Stage == EbonStage.Victory && t < 200)
            {
                // Her last threads to the manor walls snap one per beat.
                for (int k = 0; k < 6; k++)
                {
                    var hand = EbonNoirette.Hand(pose, k % 2);
                    var d = (MathHelper.Pi * (1.15f + k * .14f)).ToRotationVector2() * new Vector2(k % 2 == 0 ? 1 : -1, 1);
                    if (!field.ClipAxis(hand.X, hand.Y, d.X, d.Y, out _, out float last)) continue;
                    var wall = hand + d * last;
                    float snap = 24 + k * EbonRules.BeatTicks, since = t - snap;
                    if (since < 0) EbonMaterials.Straight(hand, wall, 3, EbonMaterials.Silk, EbonVisualsMath.Ease(t / 10), 1, .2f, k, age, 1.2f, 1.5f);
                    else if (since < 14) EbonMaterials.Straight(Vector2.Lerp(hand, wall, EbonVisualsMath.OutExpo(since / 12)), wall, 3, EbonMaterials.Hot, 1 - since / 14, 1, .8f, k, age);
                }
            }
            else if (s.Stage == EbonStage.Defeat)
            {
                // The manor closes: silk creeps in from every wall.
                for (int k = 0; k < 12; k++)
                {
                    float a = k / 12f * MathHelper.TwoPi + .2f;
                    var d = a.ToRotationVector2();
                    var c = new Vector2(field.CenterX, field.CenterY);
                    if (!field.ClipAxis(c.X, c.Y, d.X, d.Y, out _, out float last)) continue;
                    var wall = c + d * last;
                    float reach = EbonVisualsMath.Ease((t - k * 4) / 150) * .8f;
                    EbonMaterials.Straight(wall, Vector2.Lerp(wall, c, reach), 2.4f, EbonMaterials.Silk, .6f * (1 - EbonVisualsMath.Ease((t - 230) / 60)), reach, 0, k, age, 2, .2f);
                }
            }
        }
    }

    private static void Radiate(in NoirettePose pose, Convergence.Common.Raids.Arena.RaidFieldGeometry field, float age, float t, int count, Vector3 tint,
        float seed, float maxReach = float.MaxValue)
    {
        float reach = EbonVisualsMath.OutExpo(t / 10), fade = 1 - EbonVisualsMath.Ease((t - 14) / 22);
        for (int k = 0; k < count; k++)
        {
            var d = (k / (float)count * MathHelper.TwoPi + seed).ToRotationVector2();
            var from = pose.Root;
            if (!field.ClipAxis(from.X, from.Y, d.X, d.Y, out _, out float last)) continue;
            last = Math.Min(last, maxReach);
            EbonMaterials.Straight(from + d * 30, from + d * (30 + (last - 30) * reach), 3, tint, fade, 1, .6f, k + seed, age);
        }
    }

    // Above the attacks: nothing that could hide a footprint; reserved for glints.
    internal static void Front(ManagedShader shader, EbonBoss boss, in NoirettePose pose, float age)
    {
        var s = boss.State;
        if (s.Stage != EbonStage.Countdown) return;
        float b = EbonIntro.Bars(s, age);
        if (b >= EbonIntro.Hush && b < EbonIntro.Drop + .2f)
            EbonMaterials.Glow(shader, EbonNoirette.Hand(pose), 90, EbonMaterials.Hot, .9f * EbonVisualsMath.Pulse((b - EbonIntro.Hush) * EbonRules.BarTicks, 12));
    }
}
