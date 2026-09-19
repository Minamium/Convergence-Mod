#nullable enable
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Luminance.Common.Easings;
using Luminance.Core.Graphics;
using Convergence.Client.Encounters.FirstSeverance;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Encounters.Runtime;
using Convergence.Common.Networking.Replication;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

// One exact-Fight client rig (the encounter contract allows one active fight).
// Nothing lives on shared GlobalNPC instances, and no gameplay state is written.
[Autoload(Side = ModSide.Client)]
internal sealed class GhostSamuraiPresentation : ModSystem
{
    private static GhostSamuraiBoss? owner;
    private static readonly SamuraiRigHistory history = new();
    private static SamuraiRigPose current, previous, deathPose;
    private static Guid fight, retiredFight;
    private static ulong updated, missingSince, hitAt, deathAt;
    private static bool hitSeen, death;
    private static long stamp;
    private static Vector2 lastCenter, momentum;
    private static float lean, lag, entryLeft, entryRight;
    private static int lastFacing;
    private static SamuraiAttack lastAttack;
    private static bool lastTransition;
    private static ulong entryAt;
    private static ulong systemTick = ulong.MaxValue;
    private static GhostSamuraiMist? mist;
    private static readonly GhostSamuraiSecondaryMotion secondary = new();
    private static readonly List<ScreenShakeSystem.ShakeInfo> shakes = new(4);
    public override void Load() => SamuraiRigMotion.BindEasing(
        x => EasingCurves.Cubic.Evaluate(EasingType.InOut, x), x => EasingCurves.Cubic.Evaluate(EasingType.Out, x));
    internal static bool Talisman(int index, out Vector2 anchor, out float angle) => secondary.Sample(index, Fraction, out anchor, out angle);
    internal static float Fraction => Main.gamePaused || stamp == 0 ? 1 : (float)Math.Clamp((Stopwatch.GetTimestamp() - stamp) * 60d / Stopwatch.Frequency, 0, 1);

    public override void PostUpdateEverything()
    {
        if (Main.dedServ || Main.gameMenu || Main.gamePaused) return;
        ulong now = Main.GameUpdateCount;
        if (systemTick == now) return;
        systemTick = now;
        stamp = Stopwatch.GetTimestamp();
        mist ??= ModContent.GetInstance<GhostSamuraiMist>();
        mist.Tick((float)(now % 36000));
        if (GhostSamuraiRigArt.Reduced || !ModContent.GetInstance<FirstSeveranceVisualConfig>().ScreenShake)
            foreach (var shake in shakes) shake.ShakeStrength = 0;
        shakes.RemoveAll(s => s.ShakeStrength <= .01f);
        var terminal = Main.netMode == NetmodeID.MultiplayerClient
            ? ModContent.GetInstance<EncounterReplicaSystem>().LastTerminalSnapshot
            : ModContent.GetInstance<EncounterCoordinatorSystem>().LastTerminalSnapshot;
        // A lethal HitEffect can precede a phase interception. Only an accepted
        // victory starts the harmless ending; timeout/wipe/disconnect do not.
        if (owner is not null && terminal is { } ended && ended.IsTerminal && ended.FightId.Value == fight)
        {
            if (ended.EndReason == EncounterEndReason.Victory && retiredFight != fight)
            { deathPose = current; deathAt = now; death = true; retiredFight = fight; }
            DropOwner();
            if (death)
                for (int i = 0; i < 24; i++)
                {
                    Vector2 d = (i * 2.39996f).ToRotationVector2();
                    mist.Emit(retiredFight, new Vector2(deathPose.X, deathPose.Y) + d * (18 + i % 5 * 8), d * 1.4f, 28 + i % 4 * 4, 60);
                }
        }
        if (death && now - deathAt >= SamuraiRigMotion.DeathDuration) { death = false; mist.ClearOwned(); }
        GhostSamuraiBoss? active = null;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is GhostSamuraiBoss boss && GhostSamuraiContainmentPlayer.FightActive(boss)) { active = boss; break; }
        if (active is null)
        {
            // Retain only the last pose briefly to match a terminal snapshot that
            // arrives after native NPC removal. It is not drawn as a live actor.
            if (owner is not null)
            {
                if (missingSince == 0) missingSince = now;
                if (now - missingSince > 45) DropOwner();
            }
            return;
        }
        if (!ReferenceEquals(owner, active) || fight != active.Fight)
        {
            DropOwner(); owner = active; fight = active.Fight; death = false;
            lastCenter = active.NPC.Center; current = previous = Neutral(active);
            lastAttack = active.Attack; lastFacing = active.NPC.direction; lastTransition = active.TransitionRemaining > 0;
        }
        missingSince = 0;
        if (updated == now && history.Count > 0) return;
        Vector2 center = active.NPC.Center, velocity = center - lastCenter;
        bool teleported = velocity.LengthSquared() > 320 * 320;
        if (teleported) { history.Clear(); secondary.Clear(); mist.ClearOwned(); velocity = Vector2.Zero; momentum = Vector2.Zero; }
        lastCenter = center;
        momentum = Vector2.Lerp(momentum, velocity, .2f);
        lean = MathHelper.Lerp(lean, MathHelper.Clamp(velocity.X * .004f, -.22f, .22f), .2f);
        lag = MathHelper.Lerp(lag, lean, .12f);
        bool transition = active.TransitionRemaining > 0;
        float timer = transition ? GhostSamuraiRules.TransitionTime - active.TransitionRemaining : active.VisualAttackTimer;
        float age = active.VisualAge;
        var left = SamuraiRigMotion.Blade(active.Attack, active.Phase, timer, age, -1, active.NPC.direction, active.Combo, transition);
        var right = SamuraiRigMotion.Blade(active.Attack, active.Phase, timer, age, 1, active.NPC.direction, active.Combo, transition);
        if (lastAttack != active.Attack || lastFacing != active.NPC.direction || lastTransition != transition)
        {
            entryLeft = SamuraiRigMotion.Wrap(current.Left.Angle - left.Angle);
            entryRight = SamuraiRigMotion.Wrap(current.Right.Angle - right.Angle);
            entryAt = now; lastAttack = active.Attack; lastFacing = active.NPC.direction; lastTransition = transition;
        }
        float blend = 1 - SamuraiRigMotion.Smooth((now - entryAt) / 10f);
        // Finish reorientation during the harmless start of a new state. A late
        // client already inside a strike must show that strike immediately.
        if (active.Beat != SamuraiBeat.Strike)
        {
            left = left with { Angle = left.Angle + entryLeft * blend };
            right = right with { Angle = right.Angle + entryRight * blend };
        }
        float hit = hitSeen ? GhostSamuraiSecondaryMotion.HitRelease.Evaluate(Math.Clamp((now - hitAt) / 10f, 0, 1)) : 0;
        // A bounded cosmetic overrun continues a few pixels when movement stops;
        // it never changes NPC.Center, contact damage or an attack's origin.
        Vector2 overrun = momentum * .12f;
        if (overrun.LengthSquared() > 12 * 12) overrun = Vector2.Normalize(overrun) * 12;
        previous = current;
        current = new(center.X + overrun.X - active.NPC.direction * hit * 3,
            center.Y + overrun.Y + MathF.Sin(age * .035f) * 4, age,
            lean + MathF.Sin(age * .023f) * .025f, SamuraiRigMotion.DashCompression(active.Attack, timer),
            left, right, hit, velocity.Length(), lag);
        if (teleported || history.Count == 0) previous = current;
        secondary.Update(current, velocity);
        if (now % 3 == 0)
        {
            Vector2 tail = center + new Vector2(MathF.Sin(age * .17f) * 38, 128);
            mist.Emit(fight, tail, new Vector2(-momentum.X * .025f, -.5f), 32);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            var blade = side < 0 ? current.Left : current.Right;
            var oldBlade = side < 0 ? previous.Left : previous.Right;
            Vector2 hand = GhostSamuraiRigArt.Hand(current, side), tip = GhostSamuraiRigArt.Tip(current, side);
            if (blade.Charge > .3f && now % 5 == 0)
            {
                Vector2 at = Vector2.Lerp(hand, tip, .65f) + (age * .12f + side).ToRotationVector2() * 32;
                mist.Emit(fight, at, (tip - at).SafeNormalize(Vector2.UnitY) * 1.3f, 18, 26);
            }
            if (blade.Trail > 0 && oldBlade.Trail == 0 && history.Count > 0)
            {
                mist.Emit(fight, tip, (tip - hand).SafeNormalize(Vector2.UnitY) * 2, 38);
                if (shakes.Count < 4 && blade.Size > 1.2f && !GhostSamuraiRigArt.Reduced
                    && Main.LocalPlayer.GetModPlayer<GhostSamuraiContainmentPlayer>().BoundTo(active)
                    && ModContent.GetInstance<FirstSeveranceVisualConfig>().ScreenShake)
                    shakes.Add(ScreenShakeSystem.StartShakeAtPoint(center, 1.8f, angularVariance: .3f,
                        shakeDirection: Vector2.UnitY, shakeStrengthDissipationIncrement: .3f));
            }
        }
        history.Add(fight, active.NPC.whoAmI, current, now);
        updated = now;
    }

    internal static void Hit(GhostSamuraiBoss boss)
    {
        if (Main.dedServ || !ReferenceEquals(owner, boss) || fight != boss.Fight) return;
        if (!hitSeen || Main.GameUpdateCount - hitAt >= 6) { hitAt = Main.GameUpdateCount; hitSeen = true; }
    }
    private static SamuraiRigPose Neutral(GhostSamuraiBoss boss) => new(boss.NPC.Center.X, boss.NPC.Center.Y,
        boss.VisualAge, 0, 1, new(2.16f, 1, 0, 0), new(.98f, 1, 0, 0), 0, 0, 0);
    internal static void Draw(SpriteBatch batch, GhostSamuraiBoss boss, Vector2 screen)
    {
        bool owns = ReferenceEquals(owner, boss) && fight == boss.Fight;
        SamuraiRigPose pose = owns ? SamuraiRigMotion.Interpolate(previous, current, Fraction) : Neutral(boss);
        // Fallback remains visible even before the first accepted replica/tick.
        if (owns) mist?.Draw(batch);
        GhostSamuraiRigArt.Draw(batch, pose, screen, owns ? history : null, Main.GameUpdateCount + (double)Fraction - 1);
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu || !death) return;
        float age = Math.Max(0, Main.GameUpdateCount - deathAt - 1f + Fraction);
        var batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try { mist?.Draw(batch); GhostSamuraiRigArt.DrawDeath(batch, deathPose, Main.screenPosition, age); }
        finally { batch.End(); }
    }
    private static void DropOwner()
    {
        owner = null; fight = Guid.Empty; history.Clear(); updated = missingSince = hitAt = entryAt = 0;
        secondary.Clear(); mist?.ClearOwned();
        foreach (var shake in shakes) shake.ShakeStrength = 0;
        shakes.Clear();
        hitSeen = false; momentum = Vector2.Zero; lean = lag = entryLeft = entryRight = 0;
    }
    private static void Clear()
    { DropOwner(); retiredFight = Guid.Empty; death = false; deathAt = 0; stamp = 0; systemTick = ulong.MaxValue; current = previous = deathPose = default; }
    public override void ClearWorld() => Clear();
    public override void OnWorldUnload() => Clear();
    public override void Unload() { Clear(); mist = null; SamuraiRigMotion.ClearEasing(); GhostSamuraiMaterials.Reset(); }
}
