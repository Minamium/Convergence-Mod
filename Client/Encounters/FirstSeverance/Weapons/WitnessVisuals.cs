#nullable enable
using System;
using Convergence.Client.Weapons;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// Client presentation and sound of Last Witness v2 (WEAPONS.md "Rogue — Last Witness"). Each WitnessHang,
// WitnessThrownBlade, WitnessShard and WitnessJudgement gets one retained source in the shared DollWeaponLayer
// (front stratum). Once per real tick (PostAI at numUpdates == -1) the source samples the replicated state and plays
// its cues through DollCueClock on the accepted projectile age; each drawn frame it interpolates with
// WeaponDrawClock.Fraction and hands a WitnessDrawState to the Terraria-free WitnessPresentation. Nothing here decides
// a hit, spends a resource or sends a packet; a source outlives its projectile only for its residue.
[Autoload(Side = ModSide.Client)]
internal sealed class WitnessVisuals : GlobalProjectile
{
    private WitnessSource? source;

    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        => entity.ModProjectile is WitnessHang or WitnessThrownBlade or WitnessShard or WitnessJudgement;

    public override void PostAI(Projectile projectile)
    {
        if (Main.dedServ || Main.gameMenu || !RitualPresentationStep.IsFinal(projectile.numUpdates)) return;
        source ??= WitnessSource.For(projectile);
        source?.Tick(projectile);
    }

    public override void OnKill(Projectile projectile, int timeLeft)
    {
        if (Main.dedServ) return;
        source?.End(projectile);
    }

    // Owner-local hit feedback: a porcelain tick where a testimony struck.
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.dedServ || damageDone <= 0 || projectile.ModProjectile is not WitnessShard) return;
        DollWeaponAudio.Play("WitnessShardHit", projectile.Center, .7f);
    }
}

// Per-owner facts the sources share: when this owner's thrown blade was last alive, when it was last caught and
// when a hang last showed its blade (so a held trigger carries the hanging blade into the next score).
internal static class WitnessPresence
{
    private static readonly ulong[] bladeSeen = new ulong[Main.maxPlayers + 1];
    private static readonly ulong[] catchAt = new ulong[Main.maxPlayers + 1];
    private static readonly ulong[] bodySeen = new ulong[Main.maxPlayers + 1];
    private static readonly ulong[] hangSeen = new ulong[Main.maxPlayers + 1];

    private static bool Valid(int owner) => (uint)owner < (uint)bladeSeen.Length;
    internal static void Blade(int owner, ulong tick) { if (Valid(owner)) bladeSeen[owner] = tick; }
    internal static void Catch(int owner, ulong tick) { if (Valid(owner)) catchAt[owner] = tick; }
    internal static void Body(int owner, ulong tick) { if (Valid(owner)) bodySeen[owner] = tick; }
    internal static void Hang(int owner, ulong tick) { if (Valid(owner)) hangSeen[owner] = tick; }
    internal static ulong BladeSeen(int owner) => Valid(owner) ? bladeSeen[owner] : 0;
    internal static ulong CaughtAt(int owner) => Valid(owner) ? catchAt[owner] : 0;
    internal static ulong BodySeen(int owner) => Valid(owner) ? bodySeen[owner] : 0;
    internal static ulong HangSeen(int owner) => Valid(owner) ? hangSeen[owner] : 0;

    internal static void Clear()
    {
        Array.Clear(bladeSeen);
        Array.Clear(catchAt);
        Array.Clear(bodySeen);
        Array.Clear(hangSeen);
    }
}

// World and Mod unload: the shared facts reset (the layer drops the sources and DollWeaponAudio stops the voices).
[Autoload(Side = ModSide.Client)]
internal sealed class WitnessVisualsSystem : ModSystem
{
    public override void OnWorldUnload() => WitnessPresence.Clear();
    public override void Unload() => WitnessSource.Reset();
}

internal abstract class WitnessSource : IDollWeaponSource
{
    private static WitnessEnergyMaterial? energy;
    protected readonly int Owner, Identity, Seed;
    protected bool Registered;
    // GameUpdateCount of the last sample and of the kill (0 while alive).
    protected ulong Sampled, Killed;

    protected WitnessSource(Projectile p)
    {
        Owner = p.owner;
        Identity = p.identity;
        Seed = p.identity * 31 + p.owner * 7 + p.type;
    }

    internal static WitnessSource? For(Projectile p) => p.ModProjectile switch
    {
        WitnessHang => new HangSource(p),
        WitnessThrownBlade => new BladeSource(p),
        WitnessShard => new ShardSource(p),
        WitnessJudgement => new JudgementSource(p),
        _ => null,
    };

    internal static void Reset() => energy = null;

    protected static IDollEnergyMaterial Energy
        => energy ??= new WitnessEnergyMaterial(static () => ShaderManager.GetShader(WitnessEnergyMaterial.ShaderName).WrappedEffect);

    protected bool Peer => Owner != Main.myPlayer;
    protected bool Ended => Killed != 0;

    internal void Tick(Projectile p)
    {
        Sampled = Main.GameUpdateCount;
        Sample(p);
        if (!Registered) Registered = DollWeaponLayer.Add(this);
    }

    internal void End(Projectile p)
    {
        if (Ended) return;
        Killed = Math.Max(1, Main.GameUpdateCount);
        Finish(p);
    }

    protected abstract void Sample(Projectile p);
    protected virtual void Finish(Projectile p) { }
    protected abstract bool Draw(DollWeaponCanvas canvas, ref WitnessDrawState state);

    public bool Emit(DollWeaponCanvas canvas)
    {
        var state = new WitnessDrawState
        {
            Blade = DollWeaponTextures.Get(WitnessBladeArt.Name),
            Sword = DollWeaponTextures.Get("WitnessSword"),
            Shards = DollWeaponTextures.Get("WitnessShards"),
            Energy = Energy,
            Peer = Peer,
            Seed = Seed,
            Clock = (float)(canvas.Clock % WitnessEnergyMaterial.ClockPeriod),
            Gone = Ended ? (float)(canvas.Clock - 1 - Killed) : -1,
            Ghost = -1,
        };
        // A source that was never sampled (or whose projectile vanished without a kill) ends itself.
        if (!Ended && canvas.Clock - Sampled > 3) Killed = Sampled == 0 ? Main.GameUpdateCount : Sampled;
        return Draw(canvas, ref state) && WitnessPresentation.Emit(canvas, in state);
    }

    protected static float Lerp(float a, float b, float t) => a + (b - a) * t;
    protected static float LerpAngle(float a, float b, float t) => a + MathF.IEEERemainder(b - a, MathF.Tau) * t;
    protected static void Kick(int owner, float force) => ModContent.GetInstance<RitualWeaponFeedback>().Kick(owner, force);
}

// The held score: the hanging blade, its testimonies and the throw; also poses the owner's arm per drawn frame.
internal sealed class HangSource : WitnessSource, IDollArmPose
{
    private static readonly string[] testimonyWarn =
    {
        "WitnessTestimonyWarn1", "WitnessTestimonyWarn2", "WitnessTestimonyWarn3",
        "WitnessTestimonyWarn4", "WitnessTestimonyWarn5", "WitnessTestimonyWarn6",
    };
    private float previousAge, age, previousAim, aim;
    private int facing = 1, spoken;
    private bool stealth, body, caught, started;
    private ulong bodySince, thrownAt;
    private int settleCue = DollCueClock.Armed, warnCue = DollCueClock.Armed, fireCue = DollCueClock.Armed;
    private int sealCue = DollCueClock.Armed, throwWarnCue = DollCueClock.Armed, throwCue = DollCueClock.Armed;
    private SlotId throwWarn = SlotId.Invalid;
    private Vector2 lastRoot;

    internal HangSource(Projectile p) : base(p) { }

    protected override void Sample(Projectile p)
    {
        if (p.ModProjectile is not WitnessHang hang) return;
        ulong now = Main.GameUpdateCount;
        float sampledAge = hang.Age, sampledAim = hang.Axis.ToRotation();
        if (!started)
        {
            started = true;
            previousAge = sampledAge - 1;
            previousAim = sampledAim;
            // A held trigger: the blade still hanging from the last score carries on without settling again.
            bool continuous = WitnessPresence.BodySeen(Owner) != 0 && now - WitnessPresence.BodySeen(Owner) <= 4;
            bodySince = continuous ? now - 60 : now;
            caught = continuous;
            // Already showing: the appearance below is not replayed.
            body = continuous;
        }
        else
        {
            previousAge = age;
            previousAim = aim;
        }
        age = sampledAge;
        aim = previousAim + MathF.IEEERemainder(sampledAim - previousAim, MathF.Tau);
        facing = hang.Facing;
        spoken = hang.Spoken;
        stealth = hang.Stealth;
        lastRoot = p.Center;
        WitnessPresence.Hang(Owner, now);

        if (age >= WitnessRules.Throw && thrownAt == 0) thrownAt = now;
        bool bladeOut = now - WitnessPresence.BladeSeen(Owner) <= 1 && WitnessPresence.BladeSeen(Owner) != 0;
        ulong caughtAt = WitnessPresence.CaughtAt(Owner);
        bool visible = !bladeOut && (age < WitnessRules.Throw || thrownAt != 0 && caughtAt >= thrownAt);
        if (visible && !body)
        {
            bodySince = now;
            caught = caughtAt != 0 && now - caughtAt <= 2;
        }
        body = visible;
        if (body) WitnessPresence.Body(Owner, now);
        DollWeaponArmDraw.Set(Owner, this);
        Cues(p);
    }

    private void Cues(Projectile p)
    {
        Vector2 center = Balance(p.Center, aim, age);
        if (DollCueClock.Take(ref settleCue, previousAge, age, 1) && body && Main.GameUpdateCount - bodySince < 4 && !caught)
            DollWeaponAudio.Play("WitnessSettle", center, .7f);
        for (int birth = 0; birth < WitnessRules.Testimonies; birth++)
        {
            if (DollCueClock.Take(ref warnCue, previousAge, age, WitnessRules.TestimonyWarn(birth)))
                DollWeaponAudio.Play(testimonyWarn[birth], center, .75f);
            if (DollCueClock.Take(ref fireCue, previousAge, age, WitnessRules.TestimonyFire(birth)))
                DollWeaponAudio.Play("WitnessTestimonyFire", center, .8f);
        }
        if (DollCueClock.Take(ref sealCue, previousAge, age, WitnessRules.Seal))
            DollWeaponAudio.Play("WitnessSeal", center, .8f);
        if (DollCueClock.Take(ref throwWarnCue, previousAge, age, WitnessRules.ThrowWarn))
            throwWarn = DollWeaponAudio.Play("WitnessThrowWarn", center, .85f);
        if (DollCueClock.Take(ref throwCue, previousAge, age, WitnessRules.Throw))
        {
            DollWeaponAudio.Play("WitnessThrowFire", p.Center + Vector2.Normalize(new Vector2(MathF.Cos(aim), MathF.Sin(aim))) * WitnessRules.ReleaseRadius, .95f);
            Kick(Owner, 4.5f);
        }
    }

    private Vector2 Balance(Vector2 root, float aimAngle, float atAge)
    {
        System.Numerics.Vector2 point = WitnessRules.BalancePoint(new(root.X, root.Y), aimAngle, facing, WitnessRules.Pose(atAge));
        return new Vector2(point.X, point.Y);
    }

    protected override void Finish(Projectile p)
    {
        // A cancel before the throw silences the throw's warning; everything else has already finished.
        if (age < WitnessRules.Throw) DollWeaponAudio.Stop(ref throwWarn);
        DollWeaponArmDraw.Release(Owner, this);
    }

    protected override bool Draw(DollWeaponCanvas canvas, ref WitnessDrawState s)
    {
        float fraction = canvas.Fraction;
        // A newer score of the same owner took over the hanging blade.
        if (Ended && WitnessPresence.HangSeen(Owner) > Killed) return false;
        s.Part = WitnessPart.Hang;
        s.Root = Root();
        s.Aim = Ended ? aim : LerpAngle(previousAim, aim, fraction);
        s.Age = Ended ? age : Lerp(previousAge, age, fraction);
        s.Facing = facing;
        s.Stealth = stealth;
        s.Spoken = spoken;
        s.Body = body;
        s.Caught = caught;
        s.BodyAge = (float)Math.Max(0, canvas.Clock - 1 - bodySince);
        // A short hold before a cancelled or finished blade crumbles, in case the next score carries it on.
        s.Gone = Ended ? MathF.Max(-1, s.Gone - 3) : -1;
        if (s.Gone > -1 && s.Gone < 0) s.Gone = -1;
        if (thrownAt != 0 && !body && Peer && WitnessPresence.BladeSeen(Owner) < thrownAt)
            s.Ghost = (float)(canvas.Clock - 1 - thrownAt);
        return true;
    }

    // The live hand at draw time, so the blade never trails a running player.
    private Vector2 Root()
    {
        Player owner = Main.player[Owner];
        if (Ended || !owner.active) return lastRoot;
        return owner.RotatedRelativePoint(owner.MountedCenter, true);
    }

    public bool TryGetArmPose(Player player, float fraction, out float front, out float? back)
    {
        back = null;
        front = 0;
        if (Ended || player.whoAmI != Owner) return false;
        float frameAge = Lerp(previousAge, age, fraction), frameAim = LerpAngle(previousAim, aim, fraction);
        front = frameAim + facing * WitnessRules.ArmBeta(frameAge) - MathHelper.PiOver2;
        return true;
    }
}

// The thrown blade: flight, Axiom turns, return; the spin loops crossfade per stage.
internal sealed class BladeSource : WitnessSource
{
    private const int TrailLength = 12;
    // Sampled centres (newest first) and the drawn frame's copy, whose head is the interpolated centre.
    private readonly Vector2[] trail = new Vector2[TrailLength + 1], frame = new Vector2[TrailLength + 1];
    private int trailCount;
    private Vector2 previous, current;
    private float previousRotation, rotation, previousPhaseAge, phaseAge;
    private WitnessPhase phase;
    private int phaseStart = -1, spinSign = 1;
    private bool stealth, anchored, started;
    private int turnCue = DollCueClock.Armed, biteCue = DollCueClock.Armed, returnCue = DollCueClock.Armed;
    private SlotId cruise = SlotId.Invalid, axiom = SlotId.Invalid;
    private float age, previousAge;

    internal BladeSource(Projectile p) : base(p) { }

    protected override void Sample(Projectile p)
    {
        if (p.ModProjectile is not WitnessThrownBlade blade) return;
        ulong now = Main.GameUpdateCount;
        if (!started)
        {
            started = true;
            previous = current = p.Center;
            previousRotation = rotation = p.rotation;
            previousAge = blade.Age - 1;
        }
        else
        {
            previous = current;
            previousRotation = rotation;
            previousAge = age;
        }
        current = p.Center;
        rotation = previousRotation + MathF.IEEERemainder(p.rotation - previousRotation, MathF.Tau);
        age = blade.Age;
        WitnessPhase sampledPhase = blade.Phase;
        if (sampledPhase != phase || blade.PhaseStart != phaseStart)
        {
            phase = sampledPhase;
            phaseStart = blade.PhaseStart;
            previousPhaseAge = phaseAge = blade.PhaseAge;
        }
        else
        {
            previousPhaseAge = phaseAge;
            phaseAge = blade.PhaseAge;
        }
        spinSign = blade.SpinSign;
        stealth = blade.Stealth;
        if (phase == WitnessPhase.Turn) anchored = p.ai[1] >= 0;
        else if (phase == WitnessPhase.Outbound) anchored = false;
        Array.Copy(trail, 0, trail, 1, TrailLength);
        trail[0] = p.Center;
        trailCount = Math.Min(trailCount + 1, TrailLength + 1);
        WitnessPresence.Blade(Owner, now);
        Cues(p);
    }

    private void Cues(Projectile p)
    {
        // Two fixed-pitch loops, one per spin stage, crossfaded over a few ticks (no live pitch bend).
        float axiomGain = phase switch
        {
            WitnessPhase.Turn => RitualKineticMotion.Settle(phaseAge / 4f),
            WitnessPhase.Return => 1 - RitualKineticMotion.Settle(phaseAge / 6f),
            _ => 0,
        };
        float cruiseGain = (1 - axiomGain) * RitualKineticMotion.Settle(age / 3f);
        if (cruiseGain > .01f) DollWeaponAudio.Sustain(ref cruise, "WitnessSpinLoop", Owner, Identity, p.Center, .5f * cruiseGain);
        if (axiomGain > .01f) DollWeaponAudio.Sustain(ref axiom, "WitnessAxiomLoop", Owner, Identity, p.Center, .55f * axiomGain);
        if (phase == WitnessPhase.Turn && phaseStart >= 0)
        {
            if (DollCueClock.Take(ref turnCue, previousAge, age, phaseStart))
            {
                if (anchored)
                {
                    DollWeaponAudio.Play("WitnessAxiomWarn", p.Center, .9f);
                    Kick(Owner, 2.5f);
                }
                else DollWeaponAudio.Play("WitnessAxiomMiss", p.Center, .8f);
            }
            for (int bite = 0; bite < WitnessRules.TurnBites; bite++)
                if (DollCueClock.Take(ref biteCue, previousAge, age, phaseStart + WitnessRules.TurnBiteTick(bite)))
                    DollWeaponAudio.Play(bite switch { 0 => "WitnessAxiomFire1", 1 => "WitnessAxiomFire2", 2 => "WitnessAxiomFire3", _ => "WitnessAxiomFire4" },
                        p.Center, anchored ? .8f : .45f);
        }
        if (phase == WitnessPhase.Return && phaseStart >= 0 && DollCueClock.Take(ref returnCue, previousAge, age, phaseStart))
            DollWeaponAudio.Play("WitnessReturnWarn", p.Center, .8f);
    }

    protected override void Finish(Projectile p)
    {
        DollWeaponAudio.Stop(ref cruise);
        DollWeaponAudio.Stop(ref axiom);
        if (Caught(p))
        {
            WitnessPresence.Catch(Owner, Main.GameUpdateCount);
            DollWeaponAudio.Play("WitnessReturnFire", WitnessHang.CatchPoint(Owner), .8f);
            caughtHome = true;
        }
    }

    private bool caughtHome;

    // The owner knows; a peer's copy sits on the catch point when the kill arrives.
    private bool Caught(Projectile p)
    {
        if (p.ModProjectile is WitnessThrownBlade { Caught: true }) return true;
        return phase == WitnessPhase.Return && Owner != Main.myPlayer
            && Vector2.Distance(p.Center, WitnessHang.CatchPoint(Owner)) <= WitnessRules.CatchRadius + WitnessRules.ReturnSpeed;
    }

    protected override bool Draw(DollWeaponCanvas canvas, ref WitnessDrawState s)
    {
        // Caught: the hang draws the blade from here.
        if (caughtHome) return false;
        float fraction = Ended ? 1 : canvas.Fraction;
        s.Part = WitnessPart.Blade;
        s.Center = Vector2.Lerp(previous, current, fraction);
        s.Rotation = Lerp(previousRotation, rotation, fraction);
        s.SpinSign = spinSign;
        s.Phase = phase;
        s.PhaseAge = Lerp(previousPhaseAge, phaseAge, fraction);
        s.Age = Lerp(previousAge, age, fraction);
        s.Stealth = stealth;
        s.Anchored = anchored;
        // The drawn blade sits between the last two samples: the path behind it starts at the previous one.
        frame[0] = s.Center;
        Array.Copy(trail, 1, frame, 1, TrailLength);
        s.Trail = frame;
        s.TrailCount = trailCount;
        return true;
    }
}

// A testimony shard in flight.
internal sealed class ShardSource : WitnessSource
{
    private const int TrailLength = 4;
    private readonly Vector2[] trail = new Vector2[TrailLength + 1], frame = new Vector2[TrailLength + 1];
    private int trailCount, shape;
    private Vector2 previous, current;
    private float previousHeading, heading;
    private bool started;

    internal ShardSource(Projectile p) : base(p) { }

    protected override void Sample(Projectile p)
    {
        if (p.ModProjectile is not WitnessShard shard) return;
        float sampled = p.velocity.LengthSquared() > 1e-4f ? p.velocity.ToRotation() : p.rotation;
        if (!started)
        {
            started = true;
            previous = current = p.Center;
            previousHeading = heading = sampled;
        }
        else
        {
            previous = current;
            previousHeading = heading;
        }
        current = p.Center;
        heading = previousHeading + MathF.IEEERemainder(sampled - previousHeading, MathF.Tau);
        shape = shard.Shape;
        Array.Copy(trail, 0, trail, 1, TrailLength);
        trail[0] = p.Center;
        trailCount = Math.Min(trailCount + 1, TrailLength + 1);
    }

    protected override bool Draw(DollWeaponCanvas canvas, ref WitnessDrawState s)
    {
        float fraction = Ended ? 1 : canvas.Fraction;
        s.Part = WitnessPart.Shard;
        s.Center = Vector2.Lerp(previous, current, fraction);
        s.Heading = Lerp(previousHeading, heading, fraction);
        s.Shape = shape;
        frame[0] = s.Center;
        Array.Copy(trail, 1, frame, 1, TrailLength);
        s.Trail = frame;
        s.TrailCount = trailCount;
        return true;
    }
}

// The stealth Triangle Judgement: forecast, swords, edges, execution.
internal sealed class JudgementSource : WitnessSource
{
    private Vector2 previous, current;
    private float previousAge, age;
    private bool started, struck;
    private int stakeWarnCue = DollCueClock.Armed, stakeCue = DollCueClock.Armed, executeWarnCue = DollCueClock.Armed;
    private int executeCue = DollCueClock.Armed, withdrawCue = DollCueClock.Armed;
    private SlotId stakeWarn = SlotId.Invalid, executeWarn = SlotId.Invalid;

    internal JudgementSource(Projectile p) : base(p) { }

    protected override void Sample(Projectile p)
    {
        if (p.ModProjectile is not WitnessJudgement judgement) return;
        if (!started)
        {
            started = true;
            previous = current = p.Center;
            previousAge = judgement.Age - 1;
        }
        else
        {
            previous = current;
            previousAge = age;
        }
        current = p.Center;
        age = judgement.Age;
        if (DollCueClock.Take(ref stakeWarnCue, previousAge, age, 1)) stakeWarn = DollWeaponAudio.Play("VerdictStakeWarn", p.Center, .85f);
        if (DollCueClock.Take(ref stakeCue, previousAge, age, RitualArmamentChoreography.VerdictLock))
        {
            DollWeaponAudio.Play("VerdictStakeFire", p.Center, .95f);
            Kick(Owner, 2f);
        }
        if (DollCueClock.Take(ref executeWarnCue, previousAge, age, WitnessRules.EdgeWriteStart))
            executeWarn = DollWeaponAudio.Play("VerdictExecuteWarn", p.Center, .85f);
        if (DollCueClock.Take(ref executeCue, previousAge, age, RitualArmamentChoreography.VerdictHit))
        {
            struck = AnyoneInside(p.Center);
            if (struck)
            {
                DollWeaponAudio.Play("VerdictExecuteFire", p.Center, .95f);
                Kick(Owner, 5.5f);
            }
            else DollWeaponAudio.Play("VerdictExecuteMiss", p.Center, .85f);
        }
        if (DollCueClock.Take(ref withdrawCue, previousAge, age, WitnessRules.WithdrawStart))
            DollWeaponAudio.Play("VerdictWithdraw", p.Center, .7f);
    }

    // Presentation only: whether any hittable enemy stands inside the live footprint (the hit itself is owner-side).
    private static bool AnyoneInside(Vector2 center)
    {
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5 || npc.immortal) continue;
            Rectangle box = npc.Hitbox;
            if (RitualArmamentChoreography.TriangleHits(new(box.Center.X - center.X, box.Center.Y - center.Y),
                new(box.Width * .5f, box.Height * .5f), RitualArmamentChoreography.VerdictRadius)) return true;
        }
        return false;
    }

    protected override void Finish(Projectile p)
    {
        if (age < RitualArmamentChoreography.VerdictLock) DollWeaponAudio.Stop(ref stakeWarn);
        if (age < RitualArmamentChoreography.VerdictHit) DollWeaponAudio.Stop(ref executeWarn);
    }

    protected override bool Draw(DollWeaponCanvas canvas, ref WitnessDrawState s)
    {
        float fraction = Ended ? 1 : canvas.Fraction;
        s.Part = WitnessPart.Judgement;
        s.Center = Vector2.Lerp(previous, current, fraction);
        s.Age = Lerp(previousAge, age, fraction);
        s.Struck = struck || age < RitualArmamentChoreography.VerdictHit;
        // A judgement that ran its course is already faded; only an early end leaves a residue.
        if (Ended && age >= RitualArmamentChoreography.VerdictDuration - 1) return false;
        return true;
    }
}
