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
internal static class EbonCeremony
{
    // The drawn root before an act change moved her; captured by EbonVisuals.
    internal static Vector2? Departure;
    internal static int DepartureEpoch = -1;
    private static readonly List<Vector2> curve = new(24);

    internal static float IntroBars(in EbonState s, float age) => s.MusicStart < 0 ? -1 : (age - s.MusicStart) / EbonRules.BarTicks;

    // Behind the attacks: the waiting invitation, the drop's radiating silk.
    internal static void Behind(ManagedShader shader, EbonBoss boss, in NoirettePose pose, float age)
    {
        var s = boss.State;
        float b = IntroBars(s, age);
        bool waiting = s.Stage is EbonStage.Deployment or EbonStage.Ready || s.Stage == EbonStage.Countdown && b < 6;
        if (waiting)
        {
            var at = Invitation(boss, age);
            float unravel = s.Stage == EbonStage.Countdown ? EbonVisualsMath.Ease(b - 5) : 0;
            EbonMaterials.Glow(shader, at, 190, EbonMaterials.Moon, .30f * (1 - unravel) + .25f * MathF.Pow(.5f + .5f * MathF.Sin(age * .05f), 3));
            var art = EbonMaterials.Texture("BlackInvitation");
            shader.TrySetParameter("signal", new Vector4(1, 1 - unravel, .3f * unravel, 7));
            EbonMaterials.Sprite(shader, "AutoloadPass", art, new Rectangle(0, 0, art.Width, art.Height), at, art.Size() * .5f, 2,
                -.16f + MathF.Sin(age * .03f) * .08f, false, SamplerState.PointClamp, 0, 1, 10);
            if (unravel > 0 && unravel < 1) EbonMaterials.Burst(shader, at, 91, 18, (b - 5) * EbonRules.BarTicks, EbonRules.BarTicks * 1.2f, 2, 3, -.02f, 16);
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

    internal static Vector2 Invitation(EbonBoss boss, float age) => EbonNoirette.Root(boss, age) + new Vector2(0, MathF.Sin(age * .04f) * 6);

    // Silk layer (primitives).
    internal static void Silk(EbonBoss boss, in NoirettePose pose, float age)
    {
        var s = boss.State;
        var field = s.Field;
        float b = IntroBars(s, age);
        if (s.Stage is EbonStage.Deployment or EbonStage.Ready || s.Stage == EbonStage.Countdown && b < 6)
        {
            // The invitation hangs on one thread; at bar 4 six more descend to it.
            var at = Invitation(boss, age);
            float unravel = s.Stage == EbonStage.Countdown ? EbonVisualsMath.Ease(b - 5) : 0;
            EbonMaterials.Straight(new(at.X + 4, field.Top - 520), at + new Vector2(4, -40), 2, EbonMaterials.Silk, .6f * (1 - unravel), .4f, 0, 1, age);
            if (s.Stage == EbonStage.Countdown && b >= 4)
                for (int i = 0; i < 6; i++)
                {
                    float x = at.X + (i - 2.5f) * 210, reach = EbonVisualsMath.Ease((b - 4) / 1.1f - i * .06f);
                    var top = new Vector2(x, field.Top - 520);
                    var tip = Vector2.Lerp(top, at, reach);
                    EbonMaterials.Straight(top, tip, 2.4f, EbonMaterials.Silk, .7f * (1 - EbonVisualsMath.Ease(b - 5.6f)), reach, 0, i, age, 3 * (1 - reach), .2f);
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

    private static void Radiate(in NoirettePose pose, Convergence.Common.Raids.Arena.RaidFieldGeometry field, float age, float t, int count, Vector3 tint, float seed)
    {
        float reach = EbonVisualsMath.OutExpo(t / 10), fade = 1 - EbonVisualsMath.Ease((t - 14) / 22);
        for (int k = 0; k < count; k++)
        {
            var d = (k / (float)count * MathHelper.TwoPi + seed).ToRotationVector2();
            var from = pose.Root;
            if (!field.ClipAxis(from.X, from.Y, d.X, d.Y, out _, out float last)) continue;
            EbonMaterials.Straight(from + d * 30, from + d * (30 + (last - 30) * reach), 3, tint, fade, 1, .6f, k + seed, age);
        }
    }

    // Above the attacks: nothing that could hide a footprint; reserved for glints.
    internal static void Front(ManagedShader shader, EbonBoss boss, in NoirettePose pose, float age)
    {
        var s = boss.State;
        if (s.Stage != EbonStage.Countdown) return;
        float b = IntroBars(s, age);
        if (b >= 7.5f && b < 8.2f)
            EbonMaterials.Glow(shader, EbonNoirette.Hand(pose), 90, EbonMaterials.Hot, .9f * EbonVisualsMath.Pulse((b - 7.5f) * EbonRules.BarTicks, 12));
    }
}
