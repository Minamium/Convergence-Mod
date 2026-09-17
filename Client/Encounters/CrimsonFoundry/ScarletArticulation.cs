#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Common.Easings;
using Luminance.Common.StateMachines;
using Luminance.Common.VerletIntergration;
using Luminance.Core.Cutscenes;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

internal enum ScarletPose { Follow, Stage, Windup, Strike, Recover, Manifest, Hidden }
internal readonly record struct ScarletPartPose(Vector2 Offset, float Rotation, Vector2 Scale);

// Disposable, client-only secondary motion. It never supplies collision geometry.
internal sealed class ScarletArticulatedBody
{
    private readonly PushdownAutomata<EntityAIState<ScarletPose>, ScarletPose> machine = new(new(ScarletPose.Follow));
    private ScarletPose requested;
    private readonly List<VerletSegment>[] chains = new List<VerletSegment>[4];
    private readonly Vector2[][] old = new Vector2[4][];
    private readonly List<Vector2> interpolated = new(16);
    private Vector2 lastCenter;
    private bool initialized;
    internal Vector2 Motion { get; private set; }
    internal ScarletPose Pose => machine.CurrentState.Identifier;
    internal ScarletArticulatedBody()
    {
        foreach (var state in Enum.GetValues<ScarletPose>())
        {
            if (state != ScarletPose.Follow) machine.RegisterState(new(state));
            machine.RegisterStateBehavior(state, () => machine.CurrentState.Time++);
            foreach (var next in Enum.GetValues<ScarletPose>())
                if (next != state) machine.RegisterTransition(state, next, false, () => requested == next);
        }
        for (int i = 0; i < 4; i++) { chains[i] = new(12); old[i] = new Vector2[12]; }
    }
    internal void Update(Vector2 center, int species, ScarletPose desired, float age, float charge)
    {
        requested = desired; machine.PerformStateTransitionCheck(); machine.PerformBehaviors();
        Motion = initialized ? Vector2.Clamp(center - lastCenter, new(-40), new(40)) : Vector2.Zero;
        bool reset = !initialized || Vector2.DistanceSquared(center, lastCenter) > 200 * 200;
        initialized = true; lastCenter = center;
        for (int limb = 0; limb < 4; limb++)
        {
            float side = limb % 2 == 0 ? -1 : 1;
            float length = species == 1 ? 160 + limb / 2 * 55 : species == 2 ? 135 + limb / 2 * 80 : 85;
            Vector2 root = center + new Vector2(side * (species == 1 ? 65 : 32), species == 1 ? 12 : 65);
            var chain = chains[limb];
            if (reset)
            {
                chain.Clear();
                for (int j = 0; j < 12; j++)
                {
                    float t = j / 11f;
                    Vector2 p = root + new Vector2(side * length * .6f * t, length * .6f * t);
                    chain.Add(new(p, Vector2.Zero, j == 0)); old[limb][j] = p;
                }
            }
            else for (int j = 0; j < 12; j++) old[limb][j] = chain[j].Position;
            chain[0].Position = chain[0].OldPosition = root;
            for (int j = 1; j < 12; j++)
            {
                float t = j / 11f;
                Vector2 breeze = new(side * (.09f + charge * .10f), .015f * MathF.Sin(age * .04f + j + limb));
                chain[j].Velocity = Vector2.Clamp(chain[j].Velocity * .88f + breeze - Motion * (.035f * t), new(-6), new(6));
            }
            VerletSimulations.VerletSimulation(chain, length / 11,
                new VerletSettings(TileCollision: false, SlowInWater: false, Gravity: species == 1 ? .06f : .13f, MaxFallSpeed: 5), 6);
            for (int j = 1; j < 12; j++)
            {
                Vector2 offset = chain[j].Position - root;
                if (!float.IsFinite(offset.X) || !float.IsFinite(offset.Y)) chain[j].Position = root;
                else if (offset.LengthSquared() > length * length * 1.44f)
                    chain[j].Position = root + offset.SafeNormalize(Vector2.UnitY) * length * 1.2f;
            }
        }
    }
    internal void DrawSecondary(SpriteBatch batch, int species, float age, float alpha)
    {
        if (!initialized || species == 3 || Pose == ScarletPose.Hidden || alpha <= .001f) return;
        using var scope = new ScarletGraphicsScope(batch);
        float fraction = Math.Clamp(age - MathF.Floor(age), 0, 1);
        for (int limb = 0; limb < (CrimsonVisuals.Reduced ? 2 : 4); limb++)
        {
            interpolated.Clear();
            for (int j = 0; j < 12; j++) interpolated.Add(Vector2.Lerp(old[limb][j], chains[limb][j].Position, fraction));
            ScarletMaterials.Path(interpolated, species == 1 ? 8 : 4, species, age,
                alpha * (Pose is ScarletPose.Strike or ScarletPose.Windup ? .12f : .27f));
        }
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class ScarletArticulation : ModSystem
{
    private Guid fight;
    private int epoch = -1;
    private ulong lastUpdate;
    private ScarletPhaseCutscene? cutscene;
    private readonly ScarletArticulatedBody?[] rigs = new ScarletArticulatedBody?[4];
    private readonly List<ScreenShakeSystem.ShakeInfo> shakes = new(8);
    private static readonly PiecewiseCurve anticipation = new PiecewiseCurve()
        .Add(EasingCurves.Sine, EasingType.InOut, .28f, .55f).Add(EasingCurves.Cubic, EasingType.Out, 1, 1);
    private static readonly PiecewiseCurve recoil = new PiecewiseCurve()
        .Add(EasingCurves.Exp, EasingType.Out, 1, .12f).Add(EasingCurves.Sine, EasingType.InOut, -.10f, .55f)
        .Add(EasingCurves.Sine, EasingType.Out, 0, 1);
    internal static bool Participant(CrimsonBoss? boss) => !Main.dedServ && !Main.gameMenu && boss is not null
        && boss.Fresh && boss.State.Contains(Main.myPlayer) && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost;
    public override void PostUpdateEverything()
    {
        var boss = CrimsonPackets.Boss;
        if (!Participant(boss)) { Reset(); return; }
        if (fight != boss!.State.Fight || epoch != boss.State.PhaseStart)
        {
            Reset(); fight = boss.State.Fight; epoch = boss.State.PhaseStart;
            if (epoch > 0 && boss.VisualAge - epoch < 4 && !CrimsonVisuals.Reduced
                && !CutsceneManager.AnyActive && ModContent.GetInstance<CrimsonVisualConfig>().CinematicCamera)
            {
                cutscene = ModContent.GetInstance<ScarletPhaseCutscene>(); cutscene.Bind(fight, epoch);
                CutsceneManager.QueueCutscene(cutscene);
            }
        }
        if (lastUpdate == Main.GameUpdateCount || Main.gamePaused) return;
        lastUpdate = Main.GameUpdateCount;
        float age = boss.VisualAge;
        for (int source = 0; source < 4; source++)
        {
            NPC? npc = source == 3 ? boss.NPC : null;
            if (source < 3)
                foreach (NPC n in Main.ActiveNPCs)
                    if (n.ModNPC is CrimsonEffigy e && e.State.Fight == fight && e.State.Index == source) { npc = n; break; }
            if (npc is null) continue;
            var signal = CrimsonRig.Signal(boss, source == 3 ? -1 : source, age);
            ScarletPose pose = source < 3 && boss.State.Presence(source, age) < .01f ? ScarletPose.Hidden
                : age < boss.State.UnlockAt ? ScarletPose.Manifest : ScarletPose.Follow;
            Vector2 at = npc.Center;
            if (pose is not (ScarletPose.Hidden or ScarletPose.Manifest) && CrimsonGesture.TryPose(boss, source, age, out var plan))
            {
                at = CrimsonGestureVisuals.V(plan.Body(age));
                pose = age < plan.Born ? ScarletPose.Stage : age < plan.Fire ? ScarletPose.Windup
                    : age < plan.End ? ScarletPose.Strike : ScarletPose.Recover;
            }
            (rigs[source] ??= new()).Update(at, source, pose, age, signal.Charge);
        }
        if (CrimsonVisuals.Reduced || !ModContent.GetInstance<CrimsonVisualConfig>().ScreenShake)
            foreach (var info in shakes) info.ShakeStrength = 0;
        shakes.RemoveAll(info => info.ShakeStrength <= .01f);
    }
    internal static ScarletPartPose Part(int species, int part, float age, float charge, float kick)
    {
        if (part == 0 || species == 3) return new(Vector2.Zero, 0, Vector2.One);
        float strength = CrimsonVisuals.Reduced ? .15f : 1;
        float side = part is 1 or 3 ? -1 : 1, lower = part >= 3 ? 1 : 0;
        float load = anticipation.Evaluate(charge) * strength;
        float flutter = CrimsonVisuals.Reduced ? 0 : MathF.Sin(age * .037f - part * 1.7f);
        kick *= strength;
        return species switch
        {
            0 => new(new(side * load * (9 + lower * 5), lower * (flutter * 3 + kick * 8)),
                side * (load * .045f - kick * .08f), new(1 + load * .04f, 1 - kick * .035f)),
            1 => new(new(side * load * (16 + lower * 8), -load * 11 + flutter * (3 + lower * 3)),
                side * (-load * .11f + kick * .16f + flutter * .018f), new(1 + load * .08f, 1)),
            _ => new(new(side * (flutter * 4 + load * 7), lower * (load * 10 - kick * 15)),
                side * (load * .08f + flutter * .025f - kick * .08f), new(1, 1 + load * .06f))
        };
    }
    internal static float Release(float ticks) => recoil.Evaluate(Math.Clamp(ticks / 20, 0, 1));
    internal static void DrawSecondary(SpriteBatch batch, int source, float age, float alpha)
        => ModContent.GetInstance<ScarletArticulation>().rigs[source]?.DrawSecondary(batch, source, age, alpha);
    internal static void Impact(int source, int accent, Vector2 center)
    {
        if (Main.dedServ || CrimsonVisuals.Reduced || !ModContent.GetInstance<CrimsonVisualConfig>().ScreenShake) return;
        var self = ModContent.GetInstance<ScarletArticulation>();
        if (self.shakes.Count >= 6) return;
        self.shakes.Add(ScreenShakeSystem.StartShakeAtPoint(center, source == 0 ? 1.3f + .55f * accent : .65f + .30f * accent,
            shakeDirection: source == 0 ? Vector2.UnitY : Vector2.UnitX, angularVariance: .55f, shakeStrengthDissipationIncrement: .35f));
    }
    private void Reset()
    {
        foreach (var info in shakes) info.ShakeStrength = 0;
        shakes.Clear(); Array.Clear(rigs); fight = Guid.Empty; epoch = -1; lastUpdate = 0; cutscene?.Cancel();
    }
    public override void OnWorldUnload() => Reset();
    public override void ClearWorld() => Reset();
    public override void Unload() { Reset(); cutscene = null; ScarletMaterials.Reset(); }
}

[Autoload(Side = ModSide.Client)]
public sealed class ScarletPhaseCutscene : Cutscene
{
    private Guid fight;
    private int epoch;
    public override int CutsceneLength => CrimsonPhaseRules.TransitionTicks;
    internal void Bind(Guid id, int start) { fight = id; epoch = start; EndAbruptly = false; }
    internal void Cancel() { fight = Guid.Empty; EndAbruptly = true; }
    private bool Valid(CrimsonBoss? boss) => ScarletArticulation.Participant(boss) && boss!.State.Fight == fight
        && boss.State.PhaseStart == epoch && boss.VisualAge >= epoch && boss.VisualAge < boss.State.UnlockAt
        && !CrimsonVisuals.Reduced && ModContent.GetInstance<CrimsonVisualConfig>().CinematicCamera;
    public override void OnBegin() { if (!Valid(CrimsonPackets.Boss)) EndAbruptly = true; }
    public override void Update() { if (!Valid(CrimsonPackets.Boss)) EndAbruptly = true; }
    public override void ModifyScreenPosition()
    {
        var boss = CrimsonPackets.Boss;
        if (!Valid(boss)) return;
        float t = Math.Clamp((boss!.VisualAge - epoch) / CrimsonPhaseRules.TransitionTicks, 0, 1);
        CameraPanSystem.PanTowards(new(boss.State.Field.CenterX, boss.State.Field.CenterY - 70), MathF.Sin(t * MathF.PI) * .10f);
    }
}
