#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.EbonManor;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;

namespace Convergence.Client.Encounters.EbonManor;

// Binding Stitch (Stack): one gold hoop at the accepted centre with inward
// needles; silk loops around each called member tighten with the clock.
// Torn Stitch (Spread): a rose hoop on every called member with outward
// needles. Verdict art uses the server-sampled positions and failed mask only.
internal static class EbonStitchVisuals
{
    private static readonly List<Vector2> loop = new(26);

    private static Vector2 MemberCenter(EbonBoss boss, EbonStitch marker, int index, out bool present)
    {
        present = false;
        var member = boss.State.Members[index];
        if (marker.Resolved && index < marker.Positions.Length) { present = true; return new(marker.Positions[index].X, marker.Positions[index].Y); }
        var player = Main.player[member.Slot];
        if (!player.active || member.Out) return Vector2.Zero;
        present = true; return player.Center;
    }

    internal static void Marker(ManagedShader shader, EbonBoss boss, EbonStitch marker, float age)
    {
        var p = marker.Plan;
        if (age < p.Born || age >= p.End) return;
        float progress = Math.Clamp((age - p.Born) / (float)(p.Fire - p.Born), 0, 1), after = marker.ImpactAge(age);
        float appear = EbonVisualsMath.Ease((age - p.Born) / 24), remain = 1 - EbonVisualsMath.Ease((after - 30) / 40);
        bool spread = p.Kind == EbonStitchKind.Spread;
        var center = new Vector2(p.Center.X, p.Center.Y);
        if (!spread)
        {
            if (after < 0)
            {
                EbonMaterials.Ring(shader, center, EbonStitchRules.StackRadius, false, progress, EbonMaterials.Gold, appear, 0, false, p.Born * .01f);
                Needles(shader, center, EbonStitchRules.StackRadius, false, age, appear);
            }
            else if (marker.Resolved)
            {
                bool failed = marker.FailedMask != 0;
                EbonMaterials.Ring(shader, center, EbonStitchRules.StackRadius, false, 1, failed ? EbonMaterials.Rose : EbonMaterials.Gold,
                    remain * (1 - EbonVisualsMath.Ease(after / 30)), EbonVisualsMath.Pulse(after, 8), failed, p.Born * .01f);
                EbonMaterials.Glow(shader, center, 260, failed ? EbonMaterials.Rose : EbonMaterials.Gold, .8f * EbonVisualsMath.Pulse(after, 7));
                if (!failed) EbonMaterials.Burst(shader, center, p.Born, 12, after, 40, 2, 4, -.05f, 20);
            }
        }
        for (int i = 0; i < boss.State.Members.Length; i++)
        {
            if ((p.Members & (1 << i)) == 0) continue;
            var at = MemberCenter(boss, marker, i, out bool present);
            if (!present) continue;
            bool failed = marker.Resolved && (marker.FailedMask & (1 << i)) != 0;
            if (spread)
            {
                if (after < 0)
                {
                    EbonMaterials.Ring(shader, at, EbonStitchRules.SpreadRadius, true, progress, EbonMaterials.Rose, appear * .9f, 0, false, i * .37f);
                    Needles(shader, at, EbonStitchRules.SpreadRadius, true, age, appear);
                }
                else if (marker.Resolved)
                {
                    if (failed)
                    {
                        EbonMaterials.Glow(shader, at, 240, EbonMaterials.Rose, .9f * EbonVisualsMath.Pulse(after, 8));
                        EbonMaterials.Burst(shader, at, p.Born + i * 31, 14, after, 44, 2, 8, .12f, 30);
                    }
                    else EbonMaterials.Burst(shader, at, p.Born + i * 31, 8, after, 36, 2, 5, .05f, 18);
                }
            }
            else if (after >= 0 && failed)
            {
                EbonMaterials.Glow(shader, at, 200, EbonMaterials.Rose, .9f * EbonVisualsMath.Pulse(after, 8));
                EbonMaterials.Burst(shader, at, p.Born + i * 17, 12, after, 40, 2, 7, .1f, 26);
            }
        }
        // Submission telemetry: the verdict reached this layer, not proof that every peer saw each pixel.
        if (after >= 0 && marker.Resolved && !marker.DrawLogged)
        {
            marker.DrawLogged = true;
            EbonPackets.Log(FormattableString.Invariant($"event=StitchDrawn fight={p.Fight} born={p.Born} kind={p.Kind} failed_mask={marker.FailedMask} verdict_delay_ticks={age - p.Fire:F1} observer={Main.myPlayer} reduced={EbonVisuals.Reduced}"));
        }
    }

    internal static void Silk(EbonBoss boss, EbonStitch marker, float age)
    {
        var p = marker.Plan;
        if (age < p.Born || age >= p.End || p.Kind != EbonStitchKind.Stack) return;
        float progress = Math.Clamp((age - p.Born) / (float)(p.Fire - p.Born), 0, 1), after = marker.ImpactAge(age);
        float appear = EbonVisualsMath.Ease((age - p.Born) / 24);
        var center = new Vector2(p.Center.X, p.Center.Y);
        for (int i = 0; i < boss.State.Members.Length; i++)
        {
            if ((p.Members & (1 << i)) == 0) continue;
            var at = MemberCenter(boss, marker, i, out bool present);
            if (!present) continue;
            bool failed = marker.Resolved && (marker.FailedMask & (1 << i)) != 0;
            float opacity = after < 0 ? appear : 1 - EbonVisualsMath.Ease(after / 26);
            if (opacity <= .01f) continue;
            // A guide thread toward the hoop, faint once the member stands inside it.
            float inside = Vector2.Distance(at, center) <= EbonStitchRules.StackRadius ? .3f : 1;
            if (after < 0) EbonMaterials.Straight(at, center, 1.8f, EbonMaterials.Gold, .38f * opacity * inside, progress, 0, i * .3f, age, 2, .2f);
            // Two loops that tighten with the clock; on failure they cinch, on success they lift away.
            float cinch = after >= 0 ? (failed ? EbonVisualsMath.OutExpo(after / 6) : 0) : 0, lift = after >= 0 && !failed ? after * 1.6f : 0;
            for (int ring = 0; ring < 2; ring++)
            {
                float radius = (30 + ring * 10) * (1 - .35f * progress) * (1 - .6f * cinch), spin = age * (.05f + ring * .03f) * (ring == 0 ? 1 : -1);
                loop.Clear();
                for (int k = 0; k <= 24; k++)
                {
                    float a = k / 24f * MathHelper.TwoPi + spin;
                    loop.Add(at + new Vector2(MathF.Cos(a) * radius, MathF.Sin(a) * radius * .42f + (ring - .5f) * 16 - lift));
                }
                EbonMaterials.Thread(loop, 2.2f, failed ? EbonMaterials.Rose : EbonMaterials.Gold, .75f * opacity, progress, failed ? cinch : 0, i + ring * .5f, age);
            }
        }
    }

    private static void Needles(ManagedShader shader, Vector2 center, float radius, bool outward, float age, float alpha)
    {
        float pulse = 6 * MathF.Sin(age * .08f);
        for (int k = 0; k < 4; k++)
        {
            var d = (MathHelper.PiOver4 + k * MathHelper.PiOver2).ToRotationVector2();
            var at = center + d * (radius + 26 + (outward ? pulse : -pulse));
            EbonMaterials.Shard(shader, at, 34, 7, (outward ? d : -d).ToRotation(), 2, alpha * .9f, age * .05f + k);
        }
    }
}
