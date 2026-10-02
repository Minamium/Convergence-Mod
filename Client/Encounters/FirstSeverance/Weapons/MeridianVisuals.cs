#nullable enable
using System;
using Convergence.Client.Weapons;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// Client presentation and sound of the Pale Meridian (docs/encounters/first-severance/WEAPONS.md "Pale Meridian").
// Per projectile it keeps the previous/current samples for WeaponDrawClock.Fraction, poses the owner's arms on the
// grip and plays the cues once per projectile identity from the accepted age (DollCueClock), on every client alike.
// One shared source walks the live Meridian projectiles each frame and records them into DollWeaponLayer through
// the Terraria-free MeridianPresentation. Nothing here decides a hit, spends ammo or sends a packet; a dedicated
// server never loads it.
[Autoload(Side = ModSide.Client)]
internal sealed class MeridianVisuals : GlobalProjectile, IDollArmPose
{
    // One file per ladder step (F5 .. C7), so a note is never transposed at runtime.
    internal static readonly string[] NoteCues =
    {
        "MeridianNote0", "MeridianNote1", "MeridianNote2", "MeridianNote3", "MeridianNote4",
        "MeridianNote5", "MeridianNote6", "MeridianNote7", "MeridianNote8",
    };
    internal const float LoopVolume = .5f;
    private const int RoundHitGap = 4, HeavyHitGap = 6;
    private static readonly uint[] lastRoundHit = new uint[Main.maxPlayers + 1];

    private bool initialized;
    private Vector2 previousCenter, currentCenter;
    private float previousAngle, angle;
    private float lastAge = float.NaN, lastStowed = float.NaN;
    private int assembleCue = DollCueClock.Armed, noteCue = DollCueClock.Armed, heavyCue = DollCueClock.Armed;
    private int partWarnCue = DollCueClock.Armed, partFireCue = DollCueClock.Armed, igniteWarnCue = DollCueClock.Armed;
    private int igniteFireCue = DollCueClock.Armed, missCue = DollCueClock.Armed, warnCue = DollCueClock.Armed, fireCue = DollCueClock.Armed;
    private SlotId loop = SlotId.Invalid;
    private float loopGain;
    private uint lastHeavyHit;
    private int whoAmI = -1, identity = -1, type = -1;

    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        => entity.type == ModContent.ProjectileType<MeridianHoldout>() || entity.type == ModContent.ProjectileType<MeridianRound>()
            || entity.type == ModContent.ProjectileType<MeridianLine>();

    internal bool Initialized => initialized;

    // The same projectile this instance last sampled (a reused slot gets fresh globals, but be strict).
    private bool Owns(Projectile p) => p.active && p.whoAmI == whoAmI && p.identity == identity && p.type == type;

    public override void PostAI(Projectile p)
    {
        if (!RitualPresentationStep.IsFinal(p.numUpdates) || Main.dedServ) return;
        whoAmI = p.whoAmI;
        identity = p.identity;
        type = p.type;
        Sample(p);
        MeridianLayerSource.Ensure();
        if (p.ModProjectile is MeridianHoldout gun) Holdout(p, gun);
        else if (p.ModProjectile is MeridianLine line) Line(p, line);
    }

    private void Sample(Projectile p)
    {
        float target = p.velocity.LengthSquared() > 1e-6f ? p.velocity.ToRotation() : p.rotation;
        if (!initialized || Vector2.DistanceSquared(currentCenter, p.Center) > 1800 * 1800)
        {
            previousCenter = currentCenter = p.Center;
            previousAngle = angle = target;
            initialized = true;
            return;
        }
        previousCenter = currentCenter;
        currentCenter = p.Center;
        previousAngle = angle;
        // The owner's aim is already turn-capped by the holdout; a peer's corrections and the release swing ease in.
        bool released = p.ModProjectile is MeridianHoldout && p.ai[2] < 0;
        float follow = released ? .45f : p.owner == Main.myPlayer ? 1 : .46f;
        angle += MathF.IEEERemainder(target - angle, MathF.Tau) * follow;
    }

    internal float DrawAngle(float fraction) => previousAngle + MathF.IEEERemainder(angle - previousAngle, MathF.Tau) * fraction;
    internal Vector2 DrawCenter(float fraction) => Vector2.Lerp(previousCenter, currentCenter, fraction);

    // ---- The held gun ------------------------------------------------------------------------------------------

    internal static MeridianGunView GunView(Projectile p, MeridianVisuals visuals, float fraction)
    {
        Player owner = Main.player[p.owner];
        Vector2 pivot = owner.RotatedRelativePoint(owner.MountedCenter, true) + new Vector2(0, owner.gfxOffY);
        bool released = p.ai[2] < 0;
        float age = released ? p.ai[0] : Math.Max(0, p.ai[0] - 1 + fraction);
        float stowed = released ? Math.Max(0, -p.ai[2] - 1 + fraction) : 0;
        return new MeridianGunView(pivot, visuals.DrawAngle(fraction), age, released, stowed, Math.Max(0, (int)p.ai[1]),
            owner.gravDir, p.owner != Main.myPlayer, p.identity);
    }

    public bool TryGetArmPose(Player player, float fraction, out float front, out float? back)
    {
        front = 0;
        back = null;
        if (whoAmI < 0 || whoAmI >= Main.maxProjectiles) return false;
        Projectile p = Main.projectile[whoAmI];
        if (!Owns(p) || p.owner != player.whoAmI || p.ModProjectile is not MeridianHoldout) return false;
        if (p.ai[2] < 0 && -p.ai[2] >= PaleMeridianScore.StowTicks / 2) return false;
        MeridianGunView view = GunView(p, this, fraction);
        MeridianPresentation.GunPose pose = MeridianPresentation.Pose(view);
        Vector2 reach = pose.Grip - (player.MountedCenter + new Vector2(0, player.gfxOffY));
        front = (reach.LengthSquared() > 1 ? reach.ToRotation() : view.Aim) - MathHelper.PiOver2;
        back = view.Aim - MathHelper.PiOver2 + .12f * player.direction;
        return true;
    }

    private void Holdout(Projectile p, MeridianHoldout gun)
    {
        Player owner = Main.player[p.owner];
        bool released = p.ai[2] < 0;
        if (!released || -p.ai[2] < PaleMeridianScore.StowTicks / 2) DollWeaponArmDraw.Set(p.owner, this);
        float age = p.ai[0], prev = lastAge;
        MeridianGunView view = GunView(p, this, 1);
        MeridianPresentation.GunPose pose = MeridianPresentation.Pose(view);
        Vector2 muzzle = pose.Muzzle;
        if (!released)
        {
            if (DollCueClock.Take(ref assembleCue, prev, age, 1)) DollWeaponAudio.Play("MeridianAssemble", p.Center, .55f);
            int from = float.IsFinite(prev) ? Math.Max((int)prev + 1, (int)age - 6) : (int)age;
            for (int a = Math.Max(1, from); a <= (int)age; a++)
            {
                int note = PaleMeridianScore.Note(a);
                if (note >= 0 && DollCueClock.Take(ref noteCue, prev, age, a))
                    DollWeaponAudio.Play(NoteCues[note], muzzle, a >= PaleMeridianScore.Ignite ? .42f : .5f + .03f * PaleMeridianScore.Parts(a));
                if (a > PaleMeridianScore.Ignite && PaleMeridianScore.Shot(a) == MeridianShot.Heavy && DollCueClock.Take(ref heavyCue, prev, age, a))
                {
                    DollWeaponAudio.Play("MeridianHeavy", muzzle, .5f);
                    ModContent.GetInstance<RitualWeaponFeedback>().Kick(p.owner, 1.5f);
                }
                for (int part = 0; part < PaleMeridianScore.PartCount; part++)
                {
                    if (a == PaleMeridianScore.Launch(part) && DollCueClock.Take(ref partWarnCue, prev, age, a))
                        DollWeaponAudio.Play("MeridianPartWarn", p.Center, .45f);
                    if (a == PaleMeridianScore.Seats[part] && DollCueClock.Take(ref partFireCue, prev, age, a))
                        DollWeaponAudio.Play("MeridianPartFire", p.Center, .5f + .06f * part);
                }
            }
            if (DollCueClock.Take(ref igniteWarnCue, prev, age, PaleMeridianScore.KeyRise)) DollWeaponAudio.Play("MeridianIgniteWarn", p.Center, .6f);
            if (DollCueClock.Take(ref igniteFireCue, prev, age, PaleMeridianScore.Ignite))
            {
                DollWeaponAudio.Play("MeridianIgniteFire", muzzle, .75f);
                ModContent.GetInstance<RitualWeaponFeedback>().Kick(p.owner, 4);
            }
            loopGain = age >= PaleMeridianScore.Ignite + 2 ? Math.Min(1, loopGain + .25f) : 0;
        }
        else
        {
            float stowed = -p.ai[2];
            if (gun.Fired == 0 && PaleMeridianScore.Tier((int)age) >= 1 && DollCueClock.Take(ref missCue, lastStowed, stowed, 1))
                DollWeaponAudio.Play("MeridianStrikeMiss", p.Center, .55f);
            lastStowed = stowed;
            loopGain = Math.Max(0, loopGain - 1f / 6);
        }
        if (loopGain > 0) DollWeaponAudio.Sustain(ref loop, "MeridianLoop", p.owner, p.identity, muzzle, LoopVolume * loopGain);
        else if (loop.IsValid) DollWeaponAudio.Stop(ref loop);
        lastAge = age;
    }

    // ---- Release lines -----------------------------------------------------------------------------------------

    private void Line(Projectile p, MeridianLine line)
    {
        float age = p.ai[0], prev = lastAge;
        int tier = line.Tier;
        if (!line.Lattice)
        {
            if (DollCueClock.Take(ref warnCue, prev, age, 1)) DollWeaponAudio.Play("MeridianStrikeWarn", p.Center, .6f);
            if (DollCueClock.Take(ref fireCue, prev, age, PaleMeridianLattice.MeridianFire))
            {
                DollWeaponAudio.Play("MeridianStrikeFire", p.Center, tier >= 3 ? .85f : tier == 2 ? .72f : .6f);
                ModContent.GetInstance<RitualWeaponFeedback>().Kick(p.owner, tier >= 3 ? 4 : tier == 2 ? 3 : 2);
            }
        }
        else
        {
            Vector2 node = p.Center + p.velocity * line.Node;
            if (DollCueClock.Take(ref warnCue, prev, age, -PaleMeridianLattice.SplitLead)) DollWeaponAudio.Play("MeridianLatticeWarn", node, .6f);
            if (DollCueClock.Take(ref fireCue, prev, age, 0))
            {
                DollWeaponAudio.Play("MeridianLatticeFire", node, tier >= 3 ? .9f : .75f);
                ModContent.GetInstance<RitualWeaponFeedback>().Kick(p.owner, tier >= 3 ? 5 : 3.5f);
            }
        }
        lastAge = age;
    }

    // Owner-side hit ticks (the owner computes its projectiles' hits), throttled.
    public override void OnHitNPC(Projectile p, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.dedServ) return;
        bool heavy = p.ModProjectile is MeridianLine || p.ModProjectile is MeridianRound { Kind: MeridianShot.Heavy };
        uint now = Main.GameUpdateCount;
        if (heavy)
        {
            if (now - lastHeavyHit < HeavyHitGap && lastHeavyHit != 0) return;
            lastHeavyHit = now;
            DollWeaponAudio.Play("MeridianHitHeavy", target.Center, .5f);
            return;
        }
        if ((uint)p.owner >= (uint)lastRoundHit.Length || now - lastRoundHit[p.owner] < RoundHitGap) return;
        lastRoundHit[p.owner] = now;
        DollWeaponAudio.Play("MeridianHit", target.Center, .35f);
    }

    public override void OnKill(Projectile p, int timeLeft)
    {
        if (Main.dedServ || p.ModProjectile is not MeridianHoldout) return;
        DollWeaponArmDraw.Release(p.owner, this);
        // A cancelled overcharge fades its loop out instead of cutting it.
        if (loop.IsValid && loopGain > 0) MeridianLayerSource.FadeLoop(loop, p.owner, p.identity, currentCenter, LoopVolume * loopGain);
        loop = SlotId.Invalid;
        loopGain = 0;
    }

    internal static void ClearHits() => Array.Clear(lastRoundHit);
}

// The one layer source for every Pale Meridian projectile. Rounds' wakes first (one material batch), then their
// heads, the release lines and the held guns. Removed by the layer when no Meridian projectile is left; registered
// again by the next one.
internal sealed class MeridianLayerSource : IDollWeaponSource
{
    private static readonly MeridianLayerSource instance = new();
    private static readonly MeridianArt art = new();
    private static MeridianEnergyMaterial? material;
    private static bool shaderMissing;
    private static readonly Vector2[] trail = new Vector2[8];

    // Fading loops of holdouts that are gone (cancelled overcharge): six ticks down to silence.
    private struct Fade { internal SlotId Voice; internal int Owner, Identity, Ticks; internal Vector2 At; internal float Gain; }
    private static readonly Fade[] fades = new Fade[8];

    private static uint ensured = uint.MaxValue;

    // Once per tick, however many Meridian projectiles are alive (the layer drops the source when none are left).
    internal static void Ensure()
    {
        if (Main.dedServ || ensured == Main.GameUpdateCount) return;
        ensured = Main.GameUpdateCount;
        DollWeaponLayer.Add(instance);
    }

    private static Effect? Shader()
    {
        if (shaderMissing) return null;
        if (ShaderManager.TryGetShader(MeridianEnergyMaterial.ShaderName, out ManagedShader? shader) && shader is not null) return shader.WrappedEffect;
        if (ShaderManager.HasFinishedLoading) shaderMissing = true;
        return null;
    }

    private static MeridianEnergyMaterial Material()
        => material ??= new MeridianEnergyMaterial(Shader, () => MiscTexturesRegistry.TurbulentNoise.Value, () => MiscTexturesRegistry.WavyBlotchNoise.Value);

    public bool Emit(DollWeaponCanvas canvas)
    {
        // Read through Asset.Value on the frame it draws (DollWeaponTextures keeps the ImmediateLoad handles).
        art.Gun = DollWeaponTextures.Get("MeridianGun");
        art.Bare = DollWeaponTextures.Get("MeridianBare");
        art.Parts = DollWeaponTextures.Get("MeridianParts");
        art.Key = DollWeaponTextures.Get("MeridianKey");
        art.Energy = Material();
        float fraction = canvas.Fraction;
        int holdout = ModContent.ProjectileType<MeridianHoldout>(), round = ModContent.ProjectileType<MeridianRound>(),
            line = ModContent.ProjectileType<MeridianLine>();
        bool any = false;
        for (int pass = 0; pass < 4; pass++)
        {
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.type != holdout && p.type != round && p.type != line) continue;
                any = true;
                if (!p.TryGetGlobalProjectile(out MeridianVisuals visuals) || !visuals.Initialized) continue;
                if (p.owner < 0 || p.owner >= Main.maxPlayers) continue;
                if (p.type == round && pass <= 1)
                {
                    var view = new MeridianRoundView(visuals.DrawCenter(fraction), p.velocity * p.MaxUpdates,
                        (MeridianShot)(int)p.ai[2], p.ai[0], p.owner != Main.myPlayer, p.identity);
                    if (pass == 0) MeridianPresentation.EmitRoundWake(canvas, art, view, Trail(p, view));
                    else MeridianPresentation.EmitRoundHead(canvas, art, view);
                }
                else if (p.type == line && pass == 2 && p.ModProjectile is MeridianLine meridian)
                    MeridianPresentation.EmitLine(canvas, art, new MeridianLineView(p.Center, p.velocity, meridian.Node, meridian.Tier,
                        meridian.Lattice, p.ai[0] - 1 + fraction, p.owner != Main.myPlayer, p.identity));
                else if (p.type == holdout && pass == 3)
                    MeridianPresentation.EmitGun(canvas, art, MeridianVisuals.GunView(p, visuals, fraction));
            }
        }
        return any;
    }

    // The round's recent path, oldest first, only points behind the drawn head.
    private static ReadOnlySpan<Vector2> Trail(Projectile p, in MeridianRoundView view)
    {
        int points = Math.Min(MeridianPresentation.TrailPoints(view.Kind), Math.Min(p.oldPos.Length, trail.Length));
        int n = 0;
        for (int i = points; i >= 1; i--)
        {
            Vector2 at = p.oldPos[i] + p.Size * .5f;
            if (p.oldPos[i] == Vector2.Zero || Vector2.Dot(at - view.Head, view.Velocity) >= 0) continue;
            trail[n++] = at;
        }
        return new ReadOnlySpan<Vector2>(trail, 0, n);
    }

    internal static void FadeLoop(SlotId voice, int owner, int identity, Vector2 at, float gain)
    {
        for (int i = 0; i < fades.Length; i++)
        {
            if (fades[i].Ticks > 0) continue;
            fades[i] = new Fade { Voice = voice, Owner = owner, Identity = identity, At = at, Gain = gain, Ticks = 6 };
            return;
        }
    }

    internal static void TickFades()
    {
        for (int i = 0; i < fades.Length; i++)
        {
            ref Fade fade = ref fades[i];
            if (fade.Ticks <= 0) continue;
            fade.Ticks--;
            DollWeaponAudio.Sustain(ref fade.Voice, "MeridianLoop", fade.Owner, fade.Identity, fade.At, fade.Gain * fade.Ticks / 6f);
        }
    }

    internal static void Clear()
    {
        ensured = uint.MaxValue;
        Array.Clear(fades);
        art.Gun = art.Bare = art.Parts = art.Key = null;
        art.Energy = null;
    }

    internal static void Unload()
    {
        Clear();
        material = null;
        shaderMissing = false;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class MeridianPresentationSystem : ModSystem
{
    public override void PostUpdateEverything() => MeridianLayerSource.TickFades();

    public override void OnWorldUnload()
    {
        MeridianLayerSource.Clear();
        MeridianVisuals.ClearHits();
    }

    public override void Unload()
    {
        MeridianLayerSource.Unload();
        MeridianVisuals.ClearHits();
    }
}
