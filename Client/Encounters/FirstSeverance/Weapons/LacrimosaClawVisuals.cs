#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// One claw controller's presentation on this client: a Doll weapon layer source (LacrimosaClawPresentation.Emit
// fed from the replicated controller and grasp) and the owner's composite-arm pose. It ends with its controller.
internal sealed class LacrimosaClawSource : IDollWeaponSource, IDollArmPose
{
    private static LacrimosaClawMaterial? material;
    private readonly int index, identity, owner;
    internal readonly LacrimosaClawMemory Memory = new();

    internal LacrimosaClawSource(Projectile projectile)
    {
        index = projectile.whoAmI;
        identity = projectile.identity;
        owner = projectile.owner;
    }

    // The claw material through Luminance, resolved per draw (never cached across a Mod reload).
    private static LacrimosaClawMaterial Material => material ??= new LacrimosaClawMaterial(Effect);

    private static Effect? Effect()
        => ShaderManager.TryGetShader(LacrimosaClawMaterial.ShaderName, out ManagedShader? shader) ? shader.WrappedEffect : null;

    internal static void Reset() => material = null;

    private bool TryKata(out Projectile projectile, out LacrimosaClawKata kata)
    {
        projectile = Main.projectile[index];
        kata = null!;
        if (!projectile.active || projectile.identity != identity || projectile.owner != owner
            || projectile.ModProjectile is not LacrimosaClawKata found) return false;
        kata = found;
        return true;
    }

    public bool Emit(DollWeaponCanvas canvas)
    {
        if (Main.gameMenu || !TryKata(out Projectile projectile, out LacrimosaClawKata kata)) return false;
        Player player = Main.player[owner];
        if (!player.active || player.dead) return false;
        var claws = player.GetModPlayer<LacrimosaClawPlayer>();
        bool mine = owner == Main.myPlayer;
        var state = new LacrimosaClawDrawState
        {
            Center = player.MountedCenter + new Vector2(0, player.gfxOffY),
            GravDir = player.gravDir,
            Facing = player.direction,
            Stroke = kata.Stroke,
            Aim = kata.Aim,
            Age = kata.Age,
            Duration = kata.Duration,
            Serial = kata.Serial,
            Beads = kata.Beads,
            Owner = mine,
            ImpactSerial = kata.HasImpact ? kata.ImpactSerial : 0,
            ImpactAt = kata.Impact,
            DryClick = mine && claws.DryClick > 0 ? claws.DryClick : -1,
            Sprites = Sprites(),
            Material = Material,
        };
        int graspIndex = claws.GraspIndex;
        if (graspIndex >= 0 && Main.projectile[graspIndex] is { active: true } held && held.owner == owner
            && held.ModProjectile is LacrimosaClawGrasp grasp)
        {
            state.Grasping = true;
            state.GraspId = held.identity;
            state.GraspAge = grasp.Age;
            state.GraspCenter = held.Center;
            state.GraspHalfExtent = grasp.HasTarget && !grasp.Lost ? grasp.HalfExtent : LacrimosaClawMotion.EmptyHalfExtent;
            state.GraspSide = grasp.Side;
            state.GraspHeld = grasp.HasTarget;
        }
        if (mine && kata.Beads >= LacrimosaHeart.Beads && !state.Grasping && LacrimosaClawPlayer.AimTarget(Main.MouseWorld, player) is { } aim)
        {
            state.HasAimTarget = true;
            state.AimTarget = Main.npc[aim].Hitbox;
        }
        return LacrimosaClawPresentation.Emit(canvas, state, Memory);
    }

    public bool TryGetArmPose(Player player, float fraction, out float front, out float? back)
    {
        front = 0;
        back = null;
        if (!TryKata(out _, out _) || player.whoAmI != owner) return false;
        return LacrimosaClawPresentation.ArmPose(Memory, player.MountedCenter + new Vector2(0, player.gfxOffY), player.gravDir, out front, out back);
    }

    private static LacrimosaClawSprites Sprites() => new(
        DollWeaponTextures.Get(LacrimosaClawArt.OpenSmall.Name), DollWeaponTextures.Get(LacrimosaClawArt.OpenLarge.Name),
        DollWeaponTextures.Get(LacrimosaClawArt.RakeSmall.Name), DollWeaponTextures.Get(LacrimosaClawArt.RakeLarge.Name),
        DollWeaponTextures.Get(LacrimosaClawArt.ClenchSmall.Name), DollWeaponTextures.Get(LacrimosaClawArt.ClenchLarge.Name),
        DollWeaponTextures.Get(LacrimosaClawArt.ThrustSmall.Name), DollWeaponTextures.Get(LacrimosaClawArt.ThrustLarge.Name));
}

// Client glue for the claw controller and grasp: registers the layer source and arm pose, and plays every claw cue
// through DollWeaponAudio on the accepted projectile age (DollCueClock: once per cue tick, never more than 3 ticks
// late). Warning and firing pairs: RakeDown / RakeUp / Clap Warn -> Fire on the strokes, GraspWarn -> GraspFire (or
// GraspMiss on air), CrushWarn (with the four squeezes) -> CrushFire. Bead, full-meter and dry-click cues are the
// owner's only. Screen shake only through RitualWeaponFeedback.Kick (owner, config-aware).
[Autoload(Side = ModSide.Client)]
internal sealed class LacrimosaClawVisuals : GlobalProjectile
{
    // Real ticks from a cue's start to its main transient (the cue starts that much before its event).
    internal const int RakeFireLead = 3, ClapFireLead = 1, GraspFireLead = 1, CrushFireLead = 1, FullDelay = 8;

    private LacrimosaClawSource? source;
    private ulong retryAt;
    private int serial = -1, warnSlot = DollCueClock.Armed, fireSlot = DollCueClock.Armed, impactSeen, lastBeads = -1;
    private int graspWarn = DollCueClock.Armed, graspFire = DollCueClock.Armed, crushWarn = DollCueClock.Armed, crushFire = DollCueClock.Armed;
    private float previousAge = float.NaN, previousGraspAge = float.NaN;
    private ulong dryHeard, fullAt;

    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        => entity.ModProjectile is LacrimosaClawKata or LacrimosaClawGrasp;

    public override void PostAI(Projectile projectile)
    {
        if (Main.dedServ || !RitualPresentationStep.IsFinal(projectile.numUpdates)) return;
        if (projectile.ModProjectile is LacrimosaClawKata kata) Kata(projectile, kata);
        else if (projectile.ModProjectile is LacrimosaClawGrasp grasp) Grasp(projectile, grasp);
    }

    private void Kata(Projectile projectile, LacrimosaClawKata kata)
    {
        Player player = Main.player[projectile.owner];
        if (source is null && Main.GameUpdateCount >= retryAt)
        {
            var created = new LacrimosaClawSource(projectile);
            if (DollWeaponLayer.Add(created)) source = created;
            else retryAt = Main.GameUpdateCount + 30;
        }
        var claws = player.GetModPlayer<LacrimosaClawPlayer>();
        if (source is not null && (kata.Striking || claws.GraspActive)) DollWeaponArmDraw.Set(projectile.owner, source);

        // Stroke cues on the stroke's accepted age; a new stroke re-arms them.
        if (kata.Serial != serial)
        {
            serial = kata.Serial;
            DollCueClock.Reset(ref warnSlot);
            DollCueClock.Reset(ref fireSlot);
            previousAge = float.NaN;
        }
        int stroke = kata.Stroke;
        if (kata.Striking)
        {
            int duration = kata.Duration;
            float age = kata.Age;
            Vector2 at = player.MountedCenter;
            if (DollCueClock.Take(ref warnSlot, previousAge, age, 1))
            {
                if (stroke == LacrimosaClawMotion.Clap) DollWeaponAudio.PlayFor(projectile.owner, "ClawClapWarn", at, .8f);
                else if (stroke == LacrimosaClawMotion.RakeUp) DollWeaponAudio.PlayFor(projectile.owner, "ClawRakeUpWarn", at, .6f);
                else DollWeaponAudio.PlayFor(projectile.owner, "ClawRakeDownWarn", at, .6f);
            }
            int fire = stroke == LacrimosaClawMotion.Clap
                ? Math.Max(1, ContactAge(duration) - ClapFireLead)
                : Math.Max(1, LacrimosaClawMotion.FirstLiveAge(stroke, duration) - RakeFireLead);
            if (DollCueClock.Take(ref fireSlot, previousAge, age, fire))
            {
                if (stroke == LacrimosaClawMotion.Clap)
                {
                    DollWeaponAudio.PlayFor(projectile.owner, "ClawClapFire", at, .95f);
                    ModContent.GetInstance<RitualWeaponFeedback>().Kick(projectile.owner, 2.5f);
                }
                else if (stroke == LacrimosaClawMotion.RakeUp) DollWeaponAudio.PlayFor(projectile.owner, "ClawRakeUpFire", at, .7f);
                else DollWeaponAudio.PlayFor(projectile.owner, "ClawRakeDownFire", at, .7f);
            }
            previousAge = age;
        }
        if (kata.HasImpact && kata.ImpactSerial != impactSeen)
        {
            impactSeen = kata.ImpactSerial;
            DollWeaponAudio.PlayFor(projectile.owner, "ClawHit", kata.Impact, .9f);
        }

        if (projectile.owner != Main.myPlayer) return;
        // The owner's meter: each bead its own music-box note up the ladder, then the full-meter cadence.
        int beads = kata.Beads;
        if (lastBeads >= 0 && beads > lastBeads)
        {
            DollWeaponAudio.Note("ClawBead", 0, beads - 1, player.MountedCenter, .85f);
            if (beads >= LacrimosaHeart.Beads) fullAt = Main.GameUpdateCount + FullDelay;
        }
        lastBeads = beads;
        if (fullAt != 0 && Main.GameUpdateCount >= fullAt)
        {
            fullAt = 0;
            if (beads >= LacrimosaHeart.Beads) DollWeaponAudio.Play("ClawBeadsFull", player.MountedCenter, .8f);
        }
        if (claws.DryClick != 0 && claws.DryClick != dryHeard)
        {
            dryHeard = claws.DryClick;
            if (Main.GameUpdateCount - claws.DryClick <= 2) DollWeaponAudio.Play("ClawBeadDry", player.MountedCenter, .8f);
        }
    }

    // The clap's contact (palms meet) as a real stroke age.
    private static int ContactAge(int duration)
    {
        int base_ = LacrimosaClawMotion.BaseTicks(LacrimosaClawMotion.Clap);
        return Math.Max(1, (LacrimosaClawMotion.ClapContact * duration + base_ - 1) / base_);
    }

    private void Grasp(Projectile projectile, LacrimosaClawGrasp grasp)
    {
        float age = grasp.Age;
        Vector2 at = projectile.Center;
        if (DollCueClock.Take(ref graspWarn, previousGraspAge, age, 1))
            DollWeaponAudio.PlayFor(projectile.owner, "ClawGraspWarn", Main.player[projectile.owner].MountedCenter, .9f);
        if (DollCueClock.Take(ref graspFire, previousGraspAge, age, LacrimosaClawMotion.GraspContact - GraspFireLead))
        {
            if (grasp.HasTarget && !grasp.Lost)
            {
                DollWeaponAudio.PlayFor(projectile.owner, "ClawGraspFire", at, .95f);
                ModContent.GetInstance<RitualWeaponFeedback>().Kick(projectile.owner, 2f);
            }
            else DollWeaponAudio.PlayFor(projectile.owner, "ClawGraspMiss", at, .9f);
        }
        if (DollCueClock.Take(ref crushWarn, previousGraspAge, age, LacrimosaClawMotion.GraspContactEnd))
            DollWeaponAudio.PlayFor(projectile.owner, "ClawCrushWarn", at, .9f);
        if (DollCueClock.Take(ref crushFire, previousGraspAge, age, LacrimosaClawMotion.GraspCrush - CrushFireLead))
        {
            DollWeaponAudio.PlayFor(projectile.owner, "ClawCrushFire", at, 1f);
            ModContent.GetInstance<RitualWeaponFeedback>().Kick(projectile.owner, 7f);
        }
        previousGraspAge = age;
    }

    // The arm pose goes with its controller.
    public override void OnKill(Projectile projectile, int timeLeft)
    {
        if (source is not null) DollWeaponArmDraw.Release(projectile.owner, source);
        source = null;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class LacrimosaClawVisualSystem : ModSystem
{
    public override void Unload() => LacrimosaClawSource.Reset();
}
