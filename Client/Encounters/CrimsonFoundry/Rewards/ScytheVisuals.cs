#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Client.Weapons;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Convergence.Content.Items.Oboro;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using NVector = System.Numerics.Vector2;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// The Sable Scythe's client presentation (REWARDS.md, "Melee - Sable Scythe", Presentation and Audio). Everything here
// reads accepted projectile state only: the held scythe drawn from the same SablePose that collides, the swing wake,
// edge embers and the Whip's lash arc, the crescents (body, bead, cinder wake, drips, droplets, light, break spatter and scar),
// the hanging staff, Staff Reap's lines and barline, droplets, glints, cues, the arm pose and the barline's shake.
// Nothing here writes positions, input, hits or packets; a dedicated server never loads it. Ink goes through
// ScarletRewardInk (ScytheInk is an emitter), beneath the Raid's forecasts; nothing moves on a beat.
internal static class ScytheLook
{
    internal const float WakeRadius = 14, StaffInkRadius = 2.5f, DropletRadius = 2.6f;
    internal const int WakeTicks = 9, WakeSamplesPerTick = 4, WakeLiveTicks = 3, Droplets = 6, DropletLife = 20;
    internal const int WriteTicks = 6, DrainFade = 20, GlintLife = 7;
    // Edge embers: one a tick off the hook tip through a live window, drifting back along the swing.
    internal const int EdgeEmberLife = 10; internal const float EdgeEmberSize = 6, EdgeEmberDrift = 1.6f;
    // Crescents: the throw's flare (three embers for an Over or Under, one per volley crescent), a droplet every 4 ticks
    // from alternating tips and a cinder every 3 from the apex lip in flight, 3 droplets and 2 embers per hit (owner),
    // the apex's crimson light (fading over the 6-tick close after a break).
    internal const int FlareLife = 8, DripEvery = 4, EmberEvery = 3, EmberLife = 14, HitDroplets = 3, HitEmbers = 2;
    internal static readonly float[] FlareSizes = { 16, 9, 6 };
    internal const float EmberSize = 5, HitSpread = .6f, HitSpeedMin = 4, HitSpeedMax = 7;
    internal static readonly Vector3 CrescentLight = new(.7f, .1f, .06f);
    // Cue timing (ScarletRewardCues): a stroke's age runs one ahead of its time (the first update ages it to 1; DrawAge is
    // age - 1), so a cue due at stroke time t fires when the age reaches t + 1. The swing breath fires at time 0 and peaks
    // on the first live tick; the Whip's brace starts with the draw-back and its lash fires as the blade goes live.
    internal const int SwingCue = ScarletRewardCues.ScytheSwingAt + 1, WhipBraceCue = ScarletRewardCues.ScytheWhipBraceAt + 1,
        WhipCue = ScarletRewardCues.ScytheWhipAt + 1, VolleyCue = ScarletRewardCues.ScytheVolleyAt + 1;

    internal static float Seed(int owner, int salt) => (owner * 977 + salt * 131) % 997 * .01f;

    // The world grip/shoulder of a held pose, for both the stroke and the release.
    internal static Vector2 Shoulder(Player player, int facing) => SableScytheFrame.Shoulder(player, facing);
    internal static Vector2 World(Vector2 shoulder, NVector local, float aim, int facing) => SableScytheFrame.World(shoulder, local, aim, facing);

    // The arm follows the grip: the native front arm pointed at the grip with the closest stretch.
    internal static void PoseArm(Player player, Vector2 shoulder, Vector2 grip, int facing)
    {
        player.ChangeDir(facing);
        float arm = (grip - shoulder).ToRotation();
        player.SetCompositeArmFront(true, OboroHandAnchor.Stretch(player, grip, arm), arm - MathF.PI / 2);
    }

    internal static Vector2 X(NVector v) => new(v.X, v.Y);
    internal static NVector N(Vector2 v) => new(v.X, v.Y);
    internal static Vector2 Rotate(Vector2 v, float angle)
    {
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }
}

// One stroke as drawn and heard: the hook-tip history for the wake (world points, as this client accepted them),
// droplets thrown by hits, the aim-crossing glint and the swing cues.
[Autoload(Side = ModSide.Client)]
internal sealed class ScytheStrokeVisuals : GlobalProjectile
{
    private const int WakeCapacity = ScytheLook.WakeTicks * ScytheLook.WakeSamplesPerTick + 1;
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is SableStroke;

    internal readonly Vector2[] Wake = new Vector2[WakeCapacity];
    internal readonly float[] WakeAge = new float[WakeCapacity];
    internal int WakeCount;
    internal readonly Vector2[] DropFrom = new Vector2[ScytheLook.Droplets * 2], DropVelocity = new Vector2[ScytheLook.Droplets * 2];
    internal readonly ulong[] DropBorn = new ulong[ScytheLook.Droplets * 2];
    internal int DropCount;
    private int lastAge = -1, heard = -1;
    private float lastSide;

    public override void PostAI(Projectile projectile)
    {
        if (Main.dedServ || projectile.ModProjectile is not SableStroke stroke || !projectile.active) return;
        Player player = Main.player[projectile.owner];
        if (!player.active || player.HeldItem.ModItem is not CrimsonSableScythe) return;
        int age = stroke.Age, previous = lastAge;
        if (age == previous) return;
        lastAge = age;
        // Cues cross from the furthest age this client has seen, not the previous one: a late netUpdate can set a peer's
        // age back a few ticks (the Whip's lash at 15 after the sync at 12), and the cue must not ring a second time.
        int reached = heard;
        heard = Math.Max(heard, age);
        Vector2 shoulder = ScytheLook.Shoulder(player, stroke.Facing);
        SablePose pose = stroke.PoseAt(age);
        ScytheLook.PoseArm(player, shoulder, ScytheLook.World(shoulder, pose.Hand, stroke.Aim, stroke.Facing), stroke.Facing);

        // The wake: four samples a tick of the real hook tip, the newest last.
        for (int i = previous < 0 ? ScytheLook.WakeSamplesPerTick : 1; i <= ScytheLook.WakeSamplesPerTick; i++)
        {
            float t = age - 1 + i / (float)ScytheLook.WakeSamplesPerTick;
            Push(ScytheLook.World(shoulder, SableScytheMotion.Tip(stroke.PoseAt(Math.Max(0, t))), stroke.Aim, stroke.Facing), t);
        }

        // A short white-red glint at each crossing of the aim during the live window.
        NVector tipLocal = SableScytheMotion.Tip(pose);
        float side = MathF.Sign(tipLocal.Y);
        if (SableScytheMotion.Live(stroke.Stroke, age) && side != 0 && lastSide != 0 && side != lastSide && tipLocal.X > 0)
        {
            Vector2 at = ScytheLook.World(shoulder, tipLocal, stroke.Aim, stroke.Facing);
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, projectile.owner, at, Vector2.Zero, ScytheLook.GlintLife, 18, age * 7 + projectile.identity);
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, projectile.owner, at, Vector2.Zero, ScytheLook.GlintLife - 2, 9, age * 11 + projectile.identity);
        }
        lastSide = side;

        // Edge embers: through the live window one ember a tick lifts off the hook tip and drifts back along the swing.
        int kind = SableScytheMotion.Kind(stroke.Stroke);
        Vector2 tip = ScytheLook.World(shoulder, tipLocal, stroke.Aim, stroke.Facing);
        if (SableScytheMotion.Live(stroke.Stroke, age) && age > previous)
        {
            Vector2 before = ScytheLook.World(shoulder, SableScytheMotion.Tip(stroke.PoseAt(age - .25f)), stroke.Aim, stroke.Facing);
            Vector2 along = tip - before;
            along = along.LengthSquared() > 1e-3f ? Vector2.Normalize(along) : Vector2.UnitX * stroke.Facing;
            float seed = age * 13 + projectile.identity * 7;
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, projectile.owner, tip,
                -along * ScytheLook.EdgeEmberDrift + new Vector2((ScarletRewardParticles.Hash(seed, 1) - .5f) * .6f, -.5f), ScytheLook.EdgeEmberLife,
                ScytheLook.EdgeEmberSize, seed);
        }

        // Cues: a drawn breath into each Over (high) and Under (low); the Whip's brace, then the lash, then the volley's
        // tear as the lash arc sheds its crescents. All are per-swing cues of the left-click measure: the owner at its
        // role's level, other players 8 dB lower with one voice. A crescent's throw falls inside its swing's breath.
        if (kind != SableScytheMotion.Whip && Crossed(reached, age, ScytheLook.SwingCue))
            ScarletRewardAudio.Shot(kind == SableScytheMotion.Over ? ScarletRewardCues.ScytheSwingHigh : ScarletRewardCues.ScytheSwingLow, projectile.owner, tip);
        if (kind == SableScytheMotion.Whip && Crossed(reached, age, ScytheLook.WhipBraceCue))
            ScarletRewardAudio.Shot(ScarletRewardCues.ScytheWhipBrace, projectile.owner, player.Center);
        if (kind == SableScytheMotion.Whip && Crossed(reached, age, ScytheLook.WhipCue))
            ScarletRewardAudio.Shot(ScarletRewardCues.ScytheWhip, projectile.owner, tip);
        if (kind == SableScytheMotion.Whip && Crossed(reached, age, ScytheLook.VolleyCue))
        {
            var arc = stroke.Lash;
            Vector2 middle = arc.Length > 0 ? ScytheLook.X(SableCrescentFlight.ArcPoint(arc, SableCrescentFlight.VolleyWritten(2), SableStroke.LashPerTick)) : tip;
            ScarletRewardAudio.Shot(ScarletRewardCues.ScytheVolley, projectile.owner, middle);
        }
    }

    private static bool Crossed(int previous, int age, int at) => previous < at && age >= at && age <= at + 2;

    private void Push(Vector2 at, float age)
    {
        if (WakeCount == Wake.Length)
        {
            Array.Copy(Wake, 1, Wake, 0, Wake.Length - 1);
            Array.Copy(WakeAge, 1, WakeAge, 0, WakeAge.Length - 1);
            WakeCount--;
        }
        Wake[WakeCount] = at; WakeAge[WakeCount] = age; WakeCount++;
    }

    // Native hit callback (owner only): up to six droplets spray along the swing, at most two bursts per stroke.
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.dedServ || projectile.ModProjectile is not SableStroke stroke || DropCount >= DropBorn.Length || WakeCount < 2) return;
        Vector2 head = Wake[WakeCount - 1], along = head - Wake[WakeCount - 2];
        if (along.LengthSquared() < 1e-3f) along = Vector2.UnitX * stroke.Facing;
        along.Normalize();
        Vector2 at = new(Math.Clamp(head.X, target.Hitbox.Left, target.Hitbox.Right), Math.Clamp(head.Y, target.Hitbox.Top, target.Hitbox.Bottom));
        int count = CrimsonRewardRules.ReducedCount(ScytheLook.Droplets, ScarletRewardFx.Reduced);
        for (int i = 0; i < count && DropCount < DropBorn.Length; i++)
        {
            float spread = (ScarletRewardParticles.Hash(projectile.identity + DropCount, 3) - .5f) * .9f;
            float speed = 4.5f + 3.5f * ScarletRewardParticles.Hash(projectile.identity + DropCount, 7);
            DropFrom[DropCount] = at;
            DropVelocity[DropCount] = along.RotatedBy(spread) * speed + new Vector2(0, -1.5f);
            DropBorn[DropCount] = Main.GameUpdateCount;
            DropCount++;
        }
        ScarletRewardFx.Particle(ScarletParticleKind.BoneChip, projectile.owner, at, along * 2.5f + new Vector2(0, -2), 26, 2, projectile.identity + DropCount);
    }

    internal static Vector2 Droplet(Vector2 from, Vector2 velocity, float t) => from + velocity * t + new Vector2(0, .28f * t * t);

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (projectile.ModProjectile is not SableStroke stroke) return false;
        Player player = Main.player[projectile.owner];
        if (!player.active || player.HeldItem.ModItem is not CrimsonSableScythe) return false;
        float age = SableScytheMotion.DrawAge(stroke.Stroke, stroke.Age, WeaponDrawClock.Fraction);
        ScytheArt.Draw(Main.spriteBatch, ScytheLook.Shoulder(player, stroke.Facing), stroke.PoseAt(age), stroke.Aim, stroke.Facing, Color.White);
        return false;
    }

    // The lash arc's remaining scar outlives the projectile.
    public override void OnKill(Projectile projectile, int timeLeft)
    {
        if (Main.dedServ || projectile.ModProjectile is not SableStroke stroke) return;
        ScytheInk.KeepLash(projectile.owner, stroke, projectile.identity);
    }
}

// One crescent as drawn, lit and heard on this client: the throw's flare, its ember wake, drips and embers in flight,
// hit droplets (owner), the break (its spatter, light and cue) and the scar it leaves.
[Autoload(Side = ModSide.Client)]
internal sealed class ScytheCrescentVisuals : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is SableCrescent;

    // The break as this client first saw it (0: not broken): where the apex was, which way, and which side spatters.
    internal int BreakSeen;
    internal Vector2 BreakApex, BreakHeading;
    internal float BreakSide;
    internal ulong BrokeTick;
    private bool seen;
    private int lastWhole = -1;

    public override void PostAI(Projectile projectile)
    {
        if (Main.dedServ || projectile.ModProjectile is not SableCrescent crescent || !projectile.active) return;
        int owner = projectile.owner, kind = crescent.Kind;
        float age = crescent.Age;
        Vector2 heading = ScytheLook.X(crescent.Heading);
        Vector2 apex = ScytheLook.X(SableCrescentFlight.Apex(ScytheLook.N(projectile.Center), crescent.Heading, kind));
        float seed = projectile.identity * 31 + kind;

        // The throw: the body ignites with the material's blaze where it leaves the blade, and embers flare there.
        if (!seen)
        {
            seen = true;
            if (age <= 2)
            {
                int flares = SableCrescentFlight.IsVolley(kind) ? 1 : ScytheLook.FlareSizes.Length;
                for (int i = 0; i < flares; i++)
                    ScarletRewardFx.Particle(ScarletParticleKind.Ember, owner, apex, heading * (1.2f + i * .4f), ScytheLook.FlareLife,
                        SableCrescentFlight.IsVolley(kind) ? ScytheLook.FlareSizes[1] : ScytheLook.FlareSizes[i], seed + i);
            }
        }

        // In flight: its wake (a cinder every 2 ticks left across the arc's back), a droplet every 4 ticks from
        // alternating tips (they fall) and a cinder every 3 from the apex lip.
        int whole = (int)age;
        if (whole != lastWhole && !crescent.Broken)
        {
            lastWhole = whole;
            Vector2 normal = new(-heading.Y, heading.X), velocity = projectile.velocity * projectile.MaxUpdates;
            if (whole % ScarletCrescentInk.WakeEvery == 0)
                ScarletRewardFx.Particle(ScarletParticleKind.Cinder, owner,
                    ScarletCrescentInk.WakeFrom(projectile.Center, heading, kind, ScarletRewardParticles.Hash(seed, whole + 101)),
                    ScarletCrescentInk.WakeVelocity(heading), ScarletCrescentInk.WakeLife, ScarletCrescentInk.WakeSize(ScarletRewardParticles.Hash(seed, whole + 211)),
                    seed + whole * .53f);
            // Reduced Effects halves droplets: a drip every 8 ticks instead of 4, still from alternating tips.
            int every = ScytheLook.DripEvery * (ScarletRewardFx.Reduced ? 2 : 1);
            if (whole % every == 0)
            {
                float tipSide = (whole / every & 1) == 0 ? 1 : -1;
                ScytheInk.Drip(owner, ScarletCrescentInk.DripFrom(projectile.Center, heading, kind, tipSide),
                    ScarletCrescentInk.DripVelocity(velocity, heading, tipSide), seed + whole, ScarletCrescentInk.DripRadius, ScarletCrescentInk.DripLife);
            }
            if (whole % ScytheLook.EmberEvery == 0)
                ScarletRewardFx.Particle(ScarletParticleKind.Cinder, owner, apex,
                    -heading * 1.4f + normal * ((ScarletRewardParticles.Hash(seed, whole) - .5f) * 1.2f) + new Vector2(0, -.4f),
                    ScytheLook.EmberLife, ScytheLook.EmberSize, seed + whole * .37f);
        }

        // The break (replicated, so every client sees it): the spatter's place, a fading light and the cue.
        if (crescent.Broken && BreakSeen == 0)
        {
            BreakSeen = crescent.BreakAge;
            BreakApex = apex; BreakHeading = heading; BrokeTick = Main.GameUpdateCount;
            BreakSide = (projectile.identity & 1) == 0 ? 1 : -1;
            ScytheInk.BreakCue(owner, apex);
        }
        float light = crescent.Broken ? 1 - MathHelper.Clamp((Main.GameUpdateCount - BrokeTick) / (float)CrimsonRewardRules.CrescentClose, 0, 1) : 1;
        if (light > 0) Lighting.AddLight(apex, ScytheLook.CrescentLight * light);
    }

    // Native hit callback (owner only): 3 droplets along the travel (+-0.6 rad, 4-7 px/tick) and 2 embers.
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.dedServ || projectile.ModProjectile is not SableCrescent crescent) return;
        Vector2 heading = ScytheLook.X(crescent.Heading);
        Vector2 apex = ScytheLook.X(SableCrescentFlight.Apex(ScytheLook.N(projectile.Center), crescent.Heading, crescent.Kind));
        Vector2 at = new(Math.Clamp(apex.X, target.Hitbox.Left, target.Hitbox.Right), Math.Clamp(apex.Y, target.Hitbox.Top, target.Hitbox.Bottom));
        float seed = projectile.identity * 17 + target.whoAmI;
        for (int i = 0; i < CrimsonRewardRules.ReducedCount(ScytheLook.HitDroplets, ScarletRewardFx.Reduced); i++)
        {
            float turn = (ScarletRewardParticles.Hash(seed, i) * 2 - 1) * ScytheLook.HitSpread;
            float speed = MathHelper.Lerp(ScytheLook.HitSpeedMin, ScytheLook.HitSpeedMax, ScarletRewardParticles.Hash(seed, i + 7));
            ScytheInk.Drip(projectile.owner, at, ScytheLook.Rotate(heading, turn) * speed + new Vector2(0, -1.2f), seed + i,
                ScarletCrescentInk.HitDropRadius, ScarletCrescentInk.HitDropLife);
        }
        for (int i = 0; i < ScytheLook.HitEmbers; i++)
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, projectile.owner, at,
                ScytheLook.Rotate(heading, (i - .5f) * 1.1f) * 1.8f + new Vector2(0, -.6f), 12, 7, seed + 11 + i);
    }

    // The body's scar (and a break's spatter) outlive the projectile: 20 ticks, drawn by ScytheInk.
    public override void OnKill(Projectile projectile, int timeLeft)
    {
        if (Main.dedServ || projectile.ModProjectile is not SableCrescent crescent) return;
        ScytheInk.KeepCrescent(projectile.owner, projectile.identity, projectile.Center, ScytheLook.X(crescent.Heading), crescent.Kind, this);
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class ScytheReleaseVisuals : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is SableRelease;
    private int lastAge = -1;

    public override void PostAI(Projectile projectile)
    {
        if (Main.dedServ || projectile.ModProjectile is not SableRelease release || !projectile.active) return;
        Player player = Main.player[projectile.owner];
        if (!player.active || player.HeldItem.ModItem is not CrimsonSableScythe) return;
        int previous = lastAge;
        lastAge = release.Age;
        Vector2 shoulder = ScytheLook.Shoulder(player, release.Facing);
        ScytheLook.PoseArm(player, shoulder, ScytheLook.World(shoulder, release.PoseAt(release.Age).Hand, release.Aim, release.Facing), release.Facing);
        // The windup cue on the first tick any client sees: positional for everyone.
        if (previous < 0 && release.Age <= 3) ScarletRewardAudio.Play(ScarletRewardCues.StaffWindup, projectile.owner, player.Center);
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (projectile.ModProjectile is not SableRelease release) return false;
        Player player = Main.player[projectile.owner];
        if (!player.active || player.HeldItem.ModItem is not CrimsonSableScythe) return false;
        float age = Math.Clamp(release.Age - 1 + WeaponDrawClock.Fraction, 0, release.Duration);
        ScytheArt.Draw(Main.spriteBatch, ScytheLook.Shoulder(player, release.Facing), release.PoseAt(age), release.Aim, release.Facing, Color.White);
        return false;
    }
}

// The hanging staff: the next toll as a line is engraved (owner only), the written-in head, drained lines drying away.
[Autoload(Side = ModSide.Client)]
internal sealed class ScytheStaffVisuals : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is SableStaff;
    internal int Shown = -1;
    internal ulong WrittenAt, DrainedAt;
    internal int Drained = -1;

    public override void PostAI(Projectile projectile)
    {
        if (Main.dedServ || projectile.ModProjectile is not SableStaff staff || !projectile.active) return;
        int now = staff.Current;
        if (Shown < 0 && staff.Since > 2) { Shown = now; return; } // first seen long after its engraving: no toll
        if (now > Math.Max(0, Shown))
        {
            WrittenAt = Main.GameUpdateCount;
            ScarletRewardAudio.BuildToll(now - 1, projectile.owner, projectile.Center);
        }
        else if (Shown > 0 && now < Shown) { Drained = now; DrainedAt = Main.GameUpdateCount; }
        Shown = now;
    }
}

// Staff Reap's cues: each line cuts, the Final Barline strikes on the cadence voicing (with local shake 3), a partial
// staff ends on the cadence with its last line; the whole staff flares as the barline lands.
[Autoload(Side = ModSide.Client)]
internal sealed class StaffCutVisuals : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is StaffCut;
    private float lastT = float.NegativeInfinity;

    public override void PostAI(Projectile projectile)
    {
        if (Main.dedServ || projectile.ModProjectile is not StaffCut cut || !projectile.active) return;
        float previous = lastT, t = cut.T;
        lastT = t;
        if (!(previous < 0 && t >= 0 && t <= 2)) { Flare(projectile, cut, previous, t); return; }
        SableCutPlan plan = cut.Plan;
        if (cut.Barline)
        {
            Vector2 middle = new(plan.Start.X, plan.Start.Y + plan.Length * .5f);
            ScarletRewardAudio.Play(ScarletRewardCues.StaffBarline, projectile.owner, middle);
            ScarletRewardFx.Shake(projectile.owner, middle, CrimsonRewardRules.BarlineShake, Vector2.UnitY);
            return;
        }
        Vector2 start = new(plan.Start.X, plan.Start.Y);
        ScarletRewardAudio.Play(ScarletRewardCues.StaffCut, projectile.owner, start);
        if (!cut.FullStaff && cut.Index == cut.Lines - 1) ScarletRewardAudio.Play(ScarletRewardCues.Cadence, projectile.owner, start);
    }

    // The whole staff ignites together as the barline lands: embers lift off every scar of a full staff.
    private static void Flare(Projectile projectile, StaffCut cut, float previous, float t)
    {
        if (cut.Barline || !cut.FullStaff) return;
        float land = CrimsonRewardRules.BarlineTick + CrimsonRewardRules.BarlineFall - CrimsonRewardRules.StaffLineTick(cut.Index);
        if (!(previous < land && t >= land)) return;
        SableCutPlan plan = cut.Plan;
        const int count = 10; // Reduced Effects halves particles where they are spawned (ScarletRewardParticles)
        for (int i = 0; i < count; i++)
        {
            float f = (i + .5f) / count, seed = projectile.identity * 13 + i;
            Vector2 at = new(plan.Start.X + plan.Length * f, plan.Start.Y);
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, projectile.owner, at,
                new Vector2((ScarletRewardParticles.Hash(seed, 1) - .5f) * 1.2f, -.8f - ScarletRewardParticles.Hash(seed, 2)), 22, 5, seed);
        }
    }
}

// Sub-tick arm pose for the held stroke or release (the WeaponArmDraw technique).
[Autoload(Side = ModSide.Client)]
internal sealed class ScytheClientPlayer : ModPlayer
{
    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.headOnlyRender || Player.dead || !Player.active || Player.HeldItem.ModItem is not CrimsonSableScythe) return;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.owner != Player.whoAmI) continue;
            SablePose pose; float aim; int facing;
            if (p.ModProjectile is SableStroke stroke && !(stroke.Age > SableScytheMotion.Duration(stroke.Stroke)))
            {
                pose = stroke.PoseAt(SableScytheMotion.DrawAge(stroke.Stroke, stroke.Age, WeaponDrawClock.Fraction));
                aim = stroke.Aim; facing = stroke.Facing;
            }
            else if (p.ModProjectile is SableRelease release)
            {
                pose = release.PoseAt(Math.Clamp(release.Age - 1 + WeaponDrawClock.Fraction, 0, release.Duration));
                aim = release.Aim; facing = release.Facing;
            }
            else continue;
            NVector hand = SableScytheMotion.ToWorld(pose.Hand, aim, facing);
            float rotation = MathF.Atan2(hand.Y, hand.X) - MathF.PI / 2f;
            drawInfo.compositeFrontArmRotation = Player.gravDir == -1f ? -rotation : rotation;
            return;
        }
    }
}

// The Sable Scythe's black blood, emitted into ScarletRewardInk once per frame: swing wakes, the Whip's lash arc (live,
// then its 20-tick scar, which outlives the stroke), the crescents (live bodies with the bead on the apex, drips and hit
// droplets, a break's spatter blots, and the 10-tick scar a body leaves), the hanging staff (dormant, half opacity for other
// players, warm lips when full), and Staff Reap's lines and barline (live with their ignition blaze, then scars; a full
// staff holds its scars until the barline has dried). Fixed arrays only; Emit allocates nothing and changes no state.
// Reduced Effects drops the crescents' spatters (ScarletCrescentInk) and halves droplets and particles.
[Autoload(Side = ModSide.Client)]
internal sealed class ScytheInk : ModSystem, IScarletInkEmitter
{
    private const int MaxScars = 8, ScarPoints = 32, MaxCrescentScars = 32, MaxDrops = 96;
    private struct Scar
    {
        internal int Owner, Count, Seed;
        internal ulong From;   // the tick its residue started
        internal float Radius;
    }
    // A crescent's body where it ended, drying for 10 ticks from `From`; a break also leaves its spatter blots.
    private struct CrescentScar
    {
        internal int Owner, Kind, Seed;
        internal ulong From;
        internal Vector2 Center, Heading, SpatterApex, SpatterHeading;
        internal float SpatterSide;
        internal bool Live, Spatter;
    }
    private struct Drop
    {
        internal int Owner, Life;
        internal ulong Born;
        internal Vector2 From, Velocity;
        internal float Seed, Radius;
    }
    private static readonly Scar[] scars = new Scar[MaxScars];
    private static readonly Vector2[] scarPoints = new Vector2[MaxScars * ScarPoints];
    private static readonly CrescentScar[] crescentScars = new CrescentScar[MaxCrescentScars];
    private static readonly Drop[] drops = new Drop[MaxDrops];
    private static readonly ulong[] lastBreakCue = new ulong[256];
    private static int nextScar, nextCrescentScar, nextDrop;
    private static ScytheInk? instance;

    public override void Load() { instance = this; ScarletRewardInk.Register(this); }
    public override void Unload() { if (instance is not null) ScarletRewardInk.Unregister(instance); instance = null; Clear(); }
    public override void ClearWorld() => Clear();
    public override void OnWorldUnload() => Clear();
    private static void Clear()
    {
        Array.Clear(scars); Array.Clear(crescentScars); Array.Clear(drops); Array.Clear(lastBreakCue);
        nextScar = nextCrescentScar = nextDrop = 0;
    }

    // A stroke ended: if its lash arc was written, its scar keeps drying for the rest of its 20 ticks.
    internal static void KeepLash(int owner, SableStroke stroke, int identity)
    {
        var points = stroke.Lash;
        if (points.Length < 2) return;
        int slot = nextScar++ % MaxScars;
        int count = Math.Min(points.Length, ScarPoints);
        for (int i = 0; i < count; i++)
        {
            var p = points[i * (points.Length - 1) / Math.Max(1, count - 1)];
            scarPoints[slot * ScarPoints + i] = new Vector2(p.X, p.Y);
        }
        int residueStart = Math.Min(stroke.Age, CrimsonRewardRules.LashLiveEnd);
        scars[slot] = new Scar
        {
            Owner = owner, Count = count, Seed = identity, Radius = CrimsonRewardRules.LashRadius,
            From = Main.GameUpdateCount - (ulong)Math.Max(0, stroke.Age - residueStart),
        };
    }

    // A crescent ended: its body dries where it stopped for 10 ticks, with its break's spatter blots beside it.
    internal static void KeepCrescent(int owner, int identity, Vector2 center, Vector2 heading, int kind, ScytheCrescentVisuals visuals)
    {
        int slot = nextCrescentScar++ % MaxCrescentScars;
        crescentScars[slot] = new CrescentScar
        {
            Owner = owner, Kind = kind, Seed = identity, From = Main.GameUpdateCount, Center = center, Heading = heading, Live = true,
            Spatter = visuals.BreakSeen > 0, SpatterApex = visuals.BreakApex, SpatterHeading = visuals.BreakHeading, SpatterSide = visuals.BreakSide,
        };
    }

    // A droplet of black blood (falls under gravity for its life), from a crescent's tip in flight or a hit.
    internal static void Drip(int owner, Vector2 from, Vector2 velocity, float seed, float radius, int life)
    {
        drops[nextDrop++ % MaxDrops] = new Drop
        {
            Owner = owner, Born = Main.GameUpdateCount, From = from, Velocity = velocity, Seed = seed, Radius = radius, Life = life,
        };
    }

    // A crescent broke: CrescentBreak rings at most once per 6 ticks per owner, so a volley breaking together rings once
    // or twice.
    internal static void BreakCue(int owner, Vector2 at)
    {
        int slot = owner is >= 0 and < 255 ? owner : 255;
        ulong now = Main.GameUpdateCount;
        if (lastBreakCue[slot] != 0 && now - lastBreakCue[slot] < ScarletRewardCues.CrescentBreakSpacing) return;
        lastBreakCue[slot] = now;
        ScarletRewardAudio.Shot(ScarletRewardCues.CrescentBreak, owner, at);
    }

    public void Emit(ScarletInkCanvas canvas, in ScarletView view)
    {
        float fraction = view.Fraction;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            switch (p.ModProjectile)
            {
                case SableStroke stroke: EmitStroke(canvas, p, stroke, fraction); break;
                case SableCrescent crescent: EmitCrescent(canvas, p, crescent, fraction); break;
                case SableStaff staff: EmitStaff(canvas, p, staff, fraction); break;
                case StaffCut cut: EmitCut(canvas, p, cut, fraction); break;
            }
        }
        EmitScars(canvas, fraction);
        EmitCrescentScars(canvas, fraction);
        EmitDrops(canvas, fraction);
    }

    // A crescent at the span it just flew: the live body with its bead on the apex while it flies, and after a break its
    // spatter.
    private static void EmitCrescent(ScarletInkCanvas canvas, Projectile p, SableCrescent crescent, float fraction)
    {
        var visuals = p.GetGlobalProjectile<ScytheCrescentVisuals>();
        int kind = crescent.Kind;
        float seed = ScytheLook.Seed(p.owner, 40 + p.identity);
        Vector2 heading = ScytheLook.X(crescent.Heading);
        Vector2 center = ScarletCrescentInk.DrawCenter(p.Center, p.velocity * p.MaxUpdates, fraction);
        float age = ScarletCrescentInk.DrawAge(crescent.Age, fraction);
        var style = ScarletRewardFx.Ink(p.owner, ScarletInkLook.Live, seed);

        ScarletCrescentInk.Body(canvas, style, center, heading, kind, age, crescent.Remaining + (1 - fraction), !crescent.Broken);
        if (visuals.BreakSeen > 0)
            ScarletCrescentInk.Spatter(canvas, style, visuals.BreakApex, visuals.BreakHeading, kind, visuals.BreakSide, 1);
    }

    private static void EmitCrescentScars(ScarletInkCanvas canvas, float fraction)
    {
        for (int s = 0; s < MaxCrescentScars; s++)
        {
            ref readonly CrescentScar scar = ref crescentScars[s];
            if (!scar.Live) continue;
            float t = Main.GameUpdateCount - scar.From - 1 + fraction;
            if (t < 0 || t > CrimsonRewardRules.CrescentScar || !Main.player[scar.Owner].active) continue;
            float fade = 1 - t / CrimsonRewardRules.CrescentScar;
            var style = ScarletRewardFx.Ink(scar.Owner, ScarletInkLook.Residue, ScytheLook.Seed(scar.Owner, 40 + scar.Seed));
            ScarletCrescentInk.Scar(canvas, style, scar.Center, scar.Heading, scar.Kind, fade);
            if (scar.Spatter) ScarletCrescentInk.Spatter(canvas, style, scar.SpatterApex, scar.SpatterHeading, scar.Kind, scar.SpatterSide, fade);
        }
    }

    private static void EmitDrops(ScarletInkCanvas canvas, float fraction)
    {
        for (int i = 0; i < MaxDrops; i++)
        {
            ref readonly Drop drop = ref drops[i];
            if (drop.Born == 0) continue;
            float t = MathHelper.Max(0, Main.GameUpdateCount - drop.Born - 1 + fraction);
            ScarletCrescentInk.Drop(canvas, ScarletRewardFx.Ink(drop.Owner, ScarletInkLook.Live, drop.Seed), drop.From, drop.Velocity, t, drop.Radius, drop.Life);
        }
    }

    private static void EmitStroke(ScarletInkCanvas canvas, Projectile p, SableStroke stroke, float fraction)
    {
        Player player = Main.player[p.owner];
        if (!player.active || player.HeldItem.ModItem is not CrimsonSableScythe) return;
        var visuals = p.GetGlobalProjectile<ScytheStrokeVisuals>();
        float age = SableScytheMotion.DrawAge(stroke.Stroke, stroke.Age, fraction);
        int start = SableScytheMotion.LiveStart(stroke.Stroke), end = SableScytheMotion.LiveEnd(stroke.Stroke);
        float seed = ScytheLook.Seed(p.owner, p.identity);

        // Wake: hot (live) for the newest three ticks of the live window, cooling to residue as it trails. It is drawn
        // only around the cut, inside the area the blade swept, tapering from 14 px at the head to nothing.
        int n = visuals.WakeCount, split = n;
        while (split > 0 && Hot(visuals.WakeAge[split - 1], age, start, end)) split--;
        if (split > 0)
        {
            var residue = ScarletRewardFx.Ink(p.owner, ScarletInkLook.Residue, seed, .8f);
            if (canvas.Begin(residue))
            {
                for (int i = 0; i < Math.Min(n, split + 1); i++)
                {
                    float back = age - visuals.WakeAge[i], s = visuals.WakeAge[i];
                    if (back < 0 || back > ScytheLook.WakeTicks || s < start - 2 || s > end + ScytheLook.WakeTicks) continue;
                    float taper = 1 - back / ScytheLook.WakeTicks;
                    canvas.Point(visuals.Wake[i], ScytheLook.WakeRadius * taper, taper);
                }
                canvas.End();
            }
        }
        if (split < n && canvas.Begin(ScarletRewardFx.Ink(p.owner, ScarletInkLook.Live, seed)))
        {
            for (int i = split; i < n; i++)
            {
                float back = Math.Max(0, age - visuals.WakeAge[i]);
                canvas.Point(visuals.Wake[i], ScytheLook.WakeRadius * (1 - back / ScytheLook.WakeTicks), back);
            }
            canvas.End();
        }

        // Droplets from hits.
        for (int i = 0; i < visuals.DropCount; i++)
        {
            float t = MathHelper.Max(0, Main.GameUpdateCount - visuals.DropBorn[i] - 1 + fraction);
            if (t < 1.5f || t > ScytheLook.DropletLife) continue;
            canvas.Droplet(ScarletRewardFx.Ink(p.owner, ScarletInkLook.Live, seed + i),
                ScytheStrokeVisuals.Droplet(visuals.DropFrom[i], visuals.DropVelocity[i], t - 1.5f),
                ScytheStrokeVisuals.Droplet(visuals.DropFrom[i], visuals.DropVelocity[i], t), ScytheLook.DropletRadius, t);
        }

        // The Whip's lash arc: live as the tip writes it (with its ember-gold head) and until 26, then a 20-tick scar. It
        // gives up its fire over 20-26 (its close) while the volley's five crescents fan out of it.
        var lash = stroke.Lash;
        if (lash.Length < 2) return;
        bool live = age <= CrimsonRewardRules.LashLiveEnd;
        // Every point of the lash arc stops at 26 together, so it closes as a whole.
        var style = ScarletRewardFx.Ink(p.owner, live ? ScarletInkLook.Live : ScarletInkLook.Residue, seed + .5f)
            with { Remaining = CrimsonRewardRules.LashLiveEnd - age };
        if (!canvas.Begin(style)) return;
        float fade = 1 - (age - CrimsonRewardRules.LashLiveEnd) / CrimsonRewardRules.LashScar;
        for (int i = 0; i < lash.Length; i++)
        {
            float written = CrimsonRewardRules.WhipLiveStart + i / (float)SableStroke.LashPerTick;
            if (written > age + .01f) break;
            canvas.Point(new Vector2(lash[i].X, lash[i].Y), CrimsonRewardRules.LashRadius, live ? age - written : fade);
        }
        canvas.End(bead: live && age < CrimsonRewardRules.WhipLiveEnd);
    }

    private static bool Hot(float sampleAge, float age, int start, int end)
        => sampleAge >= start && sampleAge <= end && age - sampleAge <= ScytheLook.WakeLiveTicks;

    private static void EmitStaff(ScarletInkCanvas canvas, Projectile p, SableStaff staff, float fraction)
    {
        Player owner = Main.player[p.owner];
        if (!owner.active || owner.dead) return;
        var visuals = p.GetGlobalProjectile<ScytheStaffVisuals>();
        int lines = staff.Current, facing = owner.direction < 0 ? -1 : 1;
        Vector2 shoulder = ScytheLook.Shoulder(owner, facing);
        float half = CrimsonRewardRules.StaffLineLength * .5f;
        float written = (Main.GameUpdateCount - visuals.WrittenAt - 1 + fraction) / ScytheLook.WriteTicks;
        for (int i = 0; i < lines; i++)
        {
            NVector c = SableScytheMotion.HangingLine(i, facing);
            Vector2 centre = shoulder + new Vector2(c.X, c.Y);
            // The newest line is written in from the far end toward the reaper, led by its ember-gold head.
            float reach = i == lines - 1 && written < 1 ? Math.Clamp(written, .08f, 1) : 1;
            Vector2 from = centre + new Vector2(-facing * half, 0), to = Vector2.Lerp(from, centre + new Vector2(facing * half, 0), reach);
            canvas.Line(ScarletRewardFx.Ink(p.owner, ScarletInkLook.Dormant, ScytheLook.Seed(p.owner, i), 1, false,
                lines >= CrimsonRewardRules.StaffLines ? 1 : 0), from, to, ScytheLook.StaffInkRadius, ScytheLook.StaffInkRadius, 0, 0, reach < 1);
        }
        // A drained line dries away as a scar.
        float drained = Main.GameUpdateCount - visuals.DrainedAt - 1 + fraction;
        if (visuals.Drained >= 0 && visuals.Drained < CrimsonRewardRules.StaffLines && drained < ScytheLook.DrainFade)
        {
            NVector c = SableScytheMotion.HangingLine(visuals.Drained, facing);
            Vector2 centre = shoulder + new Vector2(c.X, c.Y);
            canvas.Line(ScarletRewardFx.Ink(p.owner, ScarletInkLook.Residue, ScytheLook.Seed(p.owner, 9)), centre - new Vector2(half, 0),
                centre + new Vector2(half, 0), ScytheLook.StaffInkRadius + 1, ScytheLook.StaffInkRadius + 1,
                1 - drained / ScytheLook.DrainFade, 1 - drained / ScytheLook.DrainFade);
        }
    }

    private static void EmitCut(ScarletInkCanvas canvas, Projectile p, StaffCut cut, float fraction)
    {
        float t = cut.T - 1 + fraction;
        if (t < 0) return;
        SableCutPlan plan = cut.Plan;
        float seed = ScytheLook.Seed(p.owner, 20 + cut.Index);
        Vector2 start = new(plan.Start.X, plan.Start.Y);
        if (cut.Barline)
        {
            bool live = SableScytheMotion.BarlineLive(t);
            float head = SableScytheMotion.BarlineHead(t), fade = 1 - (t - CrimsonRewardRules.BarlineFall - CrimsonRewardRules.BarlineLive) / CrimsonRewardRules.StaffScar;
            if (!live && fade <= 0) return;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 top = start + new Vector2(side * CrimsonRewardRules.BarlineGap * .5f, 0);
                if (!canvas.Begin(ScarletRewardFx.Ink(p.owner, live ? ScarletInkLook.Live : ScarletInkLook.Residue, seed + side)
                        with { Remaining = CrimsonRewardRules.BarlineFall + CrimsonRewardRules.BarlineLive - t })) return;
                int samples = Math.Clamp((int)(plan.Length * head / 10) + 2, 2, 40);
                for (int i = 0; i < samples; i++)
                {
                    float f = head * i / (samples - 1);
                    canvas.Point(top + new Vector2(0, plan.Length * f), CrimsonRewardRules.BarlineRadius,
                        live ? Math.Max(0, SableScytheMotion.BarlineIgnited(t, f)) : fade);
                }
                canvas.End(bead: live && head < 1);
            }
            return;
        }
        // A line: the residue behind (fractions 0..tail), the live river from tail to the head.
        float lineHead = SableScytheMotion.LineHead(t), tail = SableScytheMotion.LineTail(t);
        if (tail > 0 && canvas.Begin(ScarletRewardFx.Ink(p.owner, ScarletInkLook.Residue, seed)))
        {
            int samples = Math.Clamp((int)(MathF.Abs(plan.Length) * tail / 10) + 2, 2, 90);
            for (int i = 0; i < samples; i++)
            {
                float f = tail * i / (samples - 1);
                canvas.Point(start + new Vector2(plan.Length * f, 0), CrimsonRewardRules.StaffRadius,
                    SableScytheMotion.LineScarFade(t, f, cut.Index, cut.Lines));
            }
            canvas.End();
        }
        if (lineHead > tail && SableScytheMotion.LineLive(t)
            && canvas.Begin(ScarletRewardFx.Ink(p.owner, ScarletInkLook.Live, seed) with { Window = CrimsonRewardRules.StaffLineLive }))
        {
            int samples = Math.Clamp((int)(MathF.Abs(plan.Length) * (lineHead - tail) / 10) + 2, 2, 90);
            for (int i = 0; i < samples; i++)
            {
                float f = tail + (lineHead - tail) * i / (samples - 1);
                canvas.Point(start + new Vector2(plan.Length * f, 0), CrimsonRewardRules.StaffRadius, Math.Max(0, SableScytheMotion.LineIgnited(t, f)));
            }
            canvas.End(bead: t < CrimsonRewardRules.StaffHeadTicks);
        }
    }

    private static void EmitScars(ScarletInkCanvas canvas, float fraction)
    {
        for (int s = 0; s < MaxScars; s++)
        {
            ref readonly Scar scar = ref scars[s];
            if (scar.Count < 2) continue;
            float t = Main.GameUpdateCount - scar.From - 1 + fraction;
            if (t < 0 || t > CrimsonRewardRules.LashScar || !Main.player[scar.Owner].active) continue;
            if (!canvas.Begin(ScarletRewardFx.Ink(scar.Owner, ScarletInkLook.Residue, ScytheLook.Seed(scar.Owner, scar.Seed) + .5f))) return;
            for (int i = 0; i < scar.Count; i++) canvas.Point(scarPoints[s * ScarPoints + i], scar.Radius, 1 - t / CrimsonRewardRules.LashScar);
            canvas.End();
        }
    }
}
