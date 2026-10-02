#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using Score = Convergence.Content.Encounters.FirstSeverance.Rewards.LacunaTestamentScore;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// Lacuna Testament client presentation and audio (docs/encounters/first-severance/WEAPONS.md, "Magic — Lacuna
// Testament"). Every client, the owner included, reads the replicated controller and pellets once per real tick
// (final update): it samples the age, aim and fade for sub-tick drawing, plays each cue once through DollCueClock on
// the accepted age, keeps the beam's loop leased and registers the layer sources. The sources fill LacunaDrawState /
// LacunaPelletState and call the Terraria-free LacunaPresentation. Nothing here decides a hit, spends a resource or
// sends anything; a Dedicated Server never loads it.
[Autoload(Side = ModSide.Client)]
internal sealed class LacunaVisuals : GlobalProjectile
{
    // Cue levels at the call site (Assets/Sounds/Weapons/DollWeapons; docs/AUDIO_CUE_SHEET.md). Single-note cues are
    // recorded at ladder step NoteRoot (C6) and played at the iris's step: F5 Ab5 Bb5 C6 Eb6 F6 Ab6 as the irises join.
    internal const int NoteRoot = 3;
    internal const float IrisWarnVolume = .7f, IrisFireVolume = .8f, PelletWarnVolume = .55f, PelletFireVolume = .7f, PelletHitVolume = .6f;
    internal const float MergeWarnVolume = .8f, MergeFireVolume = .85f, BeamWarnVolume = .85f, BeamFireVolume = .9f, LoopVolume = .7f;
    internal const float WidenVolume = .75f, BeamHitVolume = .6f, EndVolume = .8f, CancelVolume = .55f, MissVolume = .8f;
    // The loop fades in over 4 ticks once the beam has opened and out over 6 after it ends.
    private const float LoopIn = .25f, LoopOut = .17f;
    private const int BeamHitSpacing = 20;

    internal static readonly LacunaEnergy Material = new(Shader, () => MiscTexturesRegistry.WavyBlotchNoise.Value,
        () => MiscTexturesRegistry.TurbulentNoise.Value);

    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        => entity.ModProjectile is LacunaIrisChannel or LacunaPellet;

    // Samples at the end of the last two real ticks (the source interpolates between them).
    internal bool Sampled;
    internal float PreviousAge, CurrentAge, PreviousAim, CurrentAim, PreviousFade, CurrentFade;
    internal Vector2 PreviousHead, CurrentHead;
    // A pellet's recent heads, one per update (newest last), for its wake. Created on a pellet's first update, so
    // the template instance never holds (or shares) one.
    internal Vector2[]? Trail;
    internal int TrailCount;

    private LacunaChannelSource? source;
    private int birthCue = DollCueClock.Armed, openCue = DollCueClock.Armed, tellCue = DollCueClock.Armed, shotCue = DollCueClock.Armed;
    private int mergeWarnCue = DollCueClock.Armed, mergeFireCue = DollCueClock.Armed, beamWarnCue = DollCueClock.Armed;
    private int beamFireCue = DollCueClock.Armed, widenCue = DollCueClock.Armed;
    private SlotId loop = SlotId.Invalid, beamWarn = SlotId.Invalid;
    private float loopGain, warnFade = -1;
    private bool ended;
    private ulong lastBeamHitSound;

    private static Effect? Shader()
        => ShaderManager.TryGetShader(LacunaEnergy.ShaderName, out ManagedShader? shader) ? shader?.WrappedEffect : null;

    public override void PostAI(Projectile projectile)
    {
        if (Main.dedServ || !projectile.active) return;
        if (projectile.ModProjectile is LacunaPellet && projectile.numUpdates >= 0)
        {
            // Every update of a two-update pellet adds a wake point.
            Push(projectile.Center);
            return;
        }
        if (!RitualPresentationStep.IsFinal(projectile.numUpdates)) return;
        switch (projectile.ModProjectile)
        {
            case LacunaIrisChannel channel:
                SampleChannel(projectile, channel);
                Cues(projectile, channel);
                if (source is null)
                {
                    var created = new LacunaChannelSource(projectile);
                    if (DollWeaponLayer.Add(created))
                    {
                        source = created;
                        DollWeaponArmDraw.Set(projectile.owner, created);
                    }
                }
                break;
            case LacunaPellet:
                Push(projectile.Center);
                PreviousHead = Sampled ? CurrentHead : projectile.Center;
                CurrentHead = projectile.Center;
                PreviousAge = Sampled ? CurrentAge : projectile.ai[0];
                CurrentAge = projectile.ai[0];
                Sampled = true;
                LacunaPelletSource.Ensure();
                break;
        }
    }

    private void Push(Vector2 head)
    {
        Vector2[] trail = Trail ??= new Vector2[LacunaPresentation.MaxWakePoints];
        if (TrailCount == trail.Length)
        {
            Array.Copy(trail, 1, trail, 0, trail.Length - 1);
            TrailCount--;
        }
        trail[TrailCount++] = head;
    }

    private void SampleChannel(Projectile projectile, LacunaIrisChannel channel)
    {
        float age = projectile.ai[0], aim = channel.Axis.ToRotation(), fade = Math.Max(0, -projectile.ai[2]);
        if (!Sampled)
        {
            PreviousAge = Math.Max(0, age - 1);
            PreviousAim = aim;
            PreviousFade = Math.Max(0, fade - 1);
        }
        else
        {
            PreviousAge = CurrentAge;
            PreviousAim = CurrentAim;
            PreviousFade = CurrentFade;
        }
        CurrentAge = age;
        CurrentAim = aim;
        CurrentFade = fade;
        Sampled = true;
    }

    // Cues on the accepted age, once per projectile identity (DollCueClock; more than 3 ticks late is skipped).
    private void Cues(Projectile projectile, LacunaIrisChannel channel)
    {
        float previous = PreviousAge, age = CurrentAge;
        Player owner = Main.player[projectile.owner];
        Vector2 muzzle = channel.Muzzle;
        if (!channel.Fading)
        {
            if (age <= Score.Merge + DollCueClock.DefaultLate + Score.ShotTell)
            {
                int tick = Score.LatestBirth(age, out int iris);
                if (tick >= 0 && DollCueClock.Take(ref birthCue, previous, age, tick))
                    DollWeaponAudio.Play("LacunaIrisWarn", owner.MountedCenter, IrisWarnVolume);
                tick = Score.LatestOpening(age, out iris);
                if (tick >= 0 && DollCueClock.Take(ref openCue, previous, age, tick))
                    DollWeaponAudio.Note("LacunaIrisFire", NoteRoot, iris, Seat(projectile, channel, iris), IrisFireVolume);
                tick = Score.LatestShot(age, true, out iris);
                if (tick >= 0 && DollCueClock.Take(ref tellCue, previous, age, tick))
                    DollWeaponAudio.Play("LacunaPelletWarn", Seat(projectile, channel, iris), PelletWarnVolume);
                tick = Score.LatestShot(age, false, out iris);
                if (tick >= 0 && DollCueClock.Take(ref shotCue, previous, age, tick))
                    DollWeaponAudio.Note("LacunaPelletFire", NoteRoot, iris, Seat(projectile, channel, iris), PelletFireVolume);
            }
            if (DollCueClock.Take(ref mergeWarnCue, previous, age, Score.Merge))
                DollWeaponAudio.Play("LacunaMergeWarn", muzzle, MergeWarnVolume);
            if (DollCueClock.Take(ref mergeFireCue, previous, age, Score.Formed))
                DollWeaponAudio.Play("LacunaMergeFire", muzzle, MergeFireVolume);
            if (DollCueClock.Take(ref beamWarnCue, previous, age, Score.Formed))
                beamWarn = DollWeaponAudio.Play("LacunaBeamWarn", muzzle, BeamWarnVolume);
            if (DollCueClock.Take(ref beamFireCue, previous, age, Score.Fire))
            {
                DollWeaponAudio.Play("LacunaBeamFire", muzzle, BeamFireVolume);
                ModContent.GetInstance<RitualWeaponFeedback>().Kick(projectile.owner, 5f);
            }
            int beat = Score.LatestWiden(age, out int widen);
            if (beat >= 0 && DollCueClock.Take(ref widenCue, previous, age, beat))
            {
                if (widen == 0) DollWeaponAudio.Play("LacunaWiden1", muzzle, WidenVolume);
                else if (widen == 1) DollWeaponAudio.Play("LacunaWiden2", muzzle, WidenVolume);
                else DollWeaponAudio.Play("LacunaWiden3", muzzle, WidenVolume);
            }
        }
        else if (!ended)
        {
            ended = true;
            // The cause arrives with the same sync as the fade, so every peer hears the same close. A collapse for
            // lack of mana is the failed ritual's own cue; a release closes the irises (quieter before the beam).
            if (channel.End == LacunaEnd.Starved) DollWeaponAudio.Play("LacunaBeamMiss", muzzle, MissVolume);
            else if (age >= Score.Formed) DollWeaponAudio.Play("LacunaBeamEnd", muzzle, EndVolume);
            else if (Score.OpenCount(age) > 0) DollWeaponAudio.Play("LacunaBeamEnd", owner.MountedCenter, CancelVolume);
            if (age < Score.Fire && beamWarn.IsValid) warnFade = 1;
        }
        // A charge cut short by a release ramps its swell out over four ticks instead of cutting it.
        if (warnFade >= 0)
        {
            warnFade -= .25f;
            if (SoundEngine.TryGetActiveSound(beamWarn, out ActiveSound? sound) && sound is not null && warnFade > 0) sound.Volume = warnFade;
            else
            {
                DollWeaponAudio.Stop(ref beamWarn);
                warnFade = -1;
            }
        }
        bool sounding = !channel.Fading && age >= Score.Fire + 4;
        loopGain = Math.Clamp(loopGain + (sounding ? LoopIn : -LoopOut), 0f, 1f);
        if (loopGain > 0) DollWeaponAudio.Sustain(ref loop, "LacunaBeamLoop", projectile.owner, projectile.identity, muzzle, LoopVolume * loopGain);
        else DollWeaponAudio.Stop(ref loop);
    }

    private static Vector2 Seat(Projectile projectile, LacunaIrisChannel channel, int iris)
    {
        System.Numerics.Vector2 seat = Score.Seat(Math.Max(0, iris), channel.Facing, channel.GravDir);
        return projectile.Center + new Vector2(seat.X, seat.Y);
    }

    // Owner side: a beam contact bites the target (one sound per 20 ticks).
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.dedServ || projectile.ModProjectile is not LacunaIrisChannel) return;
        LacunaPelletSource.Bite(target.Center, projectile.owner != Main.myPlayer, true);
        if (Main.GameUpdateCount - lastBeamHitSound < BeamHitSpacing) return;
        lastBeamHitSound = Main.GameUpdateCount;
        DollWeaponAudio.Play("LacunaBeamHit", target.Center, BeamHitVolume);
    }

    public override void OnKill(Projectile projectile, int timeLeft)
    {
        if (Main.dedServ) return;
        if (projectile.ModProjectile is LacunaIrisChannel)
        {
            DollWeaponAudio.Stop(ref loop);
            if (beamWarn.IsValid && CurrentAge < Score.Fire) DollWeaponAudio.Stop(ref beamWarn);
            return;
        }
        if (projectile.ModProjectile is not LacunaPellet pellet) return;
        // A pellet bites only when it really hit: the owner knows; a peer sees the owner's kill arrive early, near an
        // NPC, while its own AI did not end it. A timeout or an unusable owner only puffs out.
        bool mine = projectile.owner == Main.myPlayer;
        bool hit = pellet.Struck || !mine && !pellet.Quiet && timeLeft > 4 && NearNpc(projectile.Center);
        bool peer = !mine;
        if (hit)
        {
            LacunaPelletSource.Bite(projectile.Center, peer, false);
            DollWeaponAudio.Play("LacunaPelletHit", projectile.Center, PelletHitVolume);
        }
        else LacunaPelletSource.Puff(projectile.Center, peer);
    }

    private static bool NearNpc(Vector2 at)
    {
        foreach (NPC npc in Main.ActiveNPCs)
        {
            Rectangle box = npc.Hitbox;
            box.Inflate(32, 32);
            if (box.Contains((int)at.X, (int)at.Y)) return true;
        }
        return false;
    }
}

// One Lacuna channel on the Doll weapon layer, and its owner's arm pose. Lives on after the controller ends to finish
// the fade, the residue or the collapse from the last state it saw.
internal sealed class LacunaChannelSource : IDollWeaponSource, IDollArmPose
{
    private readonly int index, identity, owner, type;
    private LacunaDrawState last;
    private bool seen, gone;
    private double goneClock;

    internal LacunaChannelSource(Projectile projectile)
    {
        index = projectile.whoAmI;
        identity = projectile.identity;
        owner = projectile.owner;
        type = projectile.type;
    }

    // The exact controller this source was made for (same slot, identity, type and owner), once it has a sample.
    private bool TryGet(out Projectile projectile, out LacunaIrisChannel channel, out LacunaVisuals visuals)
    {
        projectile = Main.projectile[index];
        channel = null!;
        visuals = null!;
        if (!projectile.active || projectile.identity != identity || projectile.type != type || projectile.owner != owner) return false;
        if (projectile.ModProjectile is not LacunaIrisChannel found || !projectile.TryGetGlobalProjectile(out LacunaVisuals? sampled)
            || sampled is null || !sampled.Sampled) return false;
        channel = found;
        visuals = sampled;
        return true;
    }

    public bool Emit(DollWeaponCanvas canvas)
    {
        if (!gone && TryGet(out Projectile projectile, out LacunaIrisChannel channel, out LacunaVisuals visuals))
        {
            last = Build(projectile, channel, visuals, canvas.Fraction);
            seen = true;
            LacunaPresentation.EmitChannel(canvas, last, Sprites(), LacunaVisuals.Material);
            return true;
        }
        if (!seen) return !gone && Main.projectile[index].active && Main.projectile[index].identity == identity;
        if (!gone)
        {
            gone = true;
            goneClock = canvas.Clock;
        }
        float since = (float)Math.Max(0, canvas.Clock - goneClock);
        LacunaDrawState state = last.Phase == LacunaPhase.Live
            ? last with { Phase = LacunaPhase.Collapse, Fade = since }
            : last with { Fade = last.Fade + since };
        float end = state.Phase == LacunaPhase.Collapse ? Score.CollapseTicks + 8 : Score.FadeTicks;
        if (state.Age > Score.Fire) end = Math.Max(end, Score.ResidueTicks);
        if (state.Fade >= end) return false;
        LacunaPresentation.EmitChannel(canvas, state, Sprites(), LacunaVisuals.Material);
        return true;
    }

    public bool TryGetArmPose(Player player, float fraction, out float front, out float? back)
    {
        back = null;
        front = 0;
        if (player.whoAmI != owner || !TryGet(out _, out _, out LacunaVisuals visuals)) return false;
        front = Lerp(visuals.PreviousAim, visuals.CurrentAim, fraction) - MathHelper.PiOver2;
        return true;
    }

    private static LacunaDrawState Build(Projectile projectile, LacunaIrisChannel channel, LacunaVisuals v, float fraction)
    {
        Player player = Main.player[projectile.owner];
        float aim = Lerp(v.PreviousAim, v.CurrentAim, fraction);
        bool fading = projectile.ai[2] < 0;
        float age = fading ? v.CurrentAge : v.PreviousAge + (v.CurrentAge - v.PreviousAge) * fraction;
        float fade = fading ? v.PreviousFade + (v.CurrentFade - v.PreviousFade) * fraction : 0;
        Vector2 centre = player.MountedCenter;
        return new LacunaDrawState
        {
            Center = centre,
            Hand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, aim - MathHelper.PiOver2),
            Aim = aim,
            Age = age,
            Facing = channel.Facing,
            Direction = player.direction < 0 ? -1 : 1,
            GravDir = player.gravDir < 0 ? -1 : 1,
            Fade = fade,
            Phase = fading ? LacunaPhase.Fading : LacunaPhase.Live,
            End = channel.End,
            Peer = projectile.owner != Main.myPlayer,
            Seed = projectile.identity,
        };
    }

    internal static LacunaSprites Sprites()
        => new(DollWeaponTextures.Get(LacunaArtFit.Book), DollWeaponTextures.Get(LacunaArtFit.Iris), DollWeaponTextures.Get(LacunaArtFit.Great));

    private static float Lerp(float from, float to, float t) => from + MathHelper.WrapAngle(to - from) * Math.Clamp(t, 0f, 1f);
}

// Every Lacuna pellet on screen, plus the bites and puffs they leave, as one layer source. Registered while any pellet
// or mark exists; the marks are a fixed ring (oldest overwritten) cleared with the layer at world unload.
internal sealed class LacunaPelletSource : IDollWeaponSource
{
    private const int MaxMarks = 48;

    private struct Mark
    {
        internal Vector2 At;
        internal double Start;
        internal int Seed;
        internal bool Peer, Beam, Bite;
    }

    private static readonly LacunaPelletSource instance = new();
    private static readonly Mark[] marks = new Mark[MaxMarks];
    private static int next, live;
    private readonly Vector2[] spine = new Vector2[LacunaPresentation.MaxWakePoints];

    // Registers the one pellet source (the layer accepts a registered source again without duplicating it).
    internal static void Ensure() => DollWeaponLayer.Add(instance);

    internal static void Bite(Vector2 at, bool peer, bool beam) => Add(at, peer, beam, true);
    internal static void Puff(Vector2 at, bool peer) => Add(at, peer, false, false);

    private static void Add(Vector2 at, bool peer, bool beam, bool bite)
    {
        if (Main.dedServ) return;
        marks[next] = new Mark { At = at, Start = Main.GameUpdateCount, Seed = (int)(Main.GameUpdateCount * 31 + next * 7), Peer = peer, Beam = beam, Bite = bite };
        next = (next + 1) % MaxMarks;
        live = Math.Min(MaxMarks, live + 1);
        Ensure();
    }

    public bool Emit(DollWeaponCanvas canvas)
    {
        bool any = false;
        int pelletType = ModContent.ProjectileType<LacunaPellet>();
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.type != pelletType || projectile.ModProjectile is not LacunaPellet pellet
                || !projectile.TryGetGlobalProjectile(out LacunaVisuals? v) || v is null || !v.Sampled) continue;
            any = true;
            float fraction = canvas.Fraction;
            Vector2 head = Vector2.Lerp(v.PreviousHead, v.CurrentHead, fraction);
            // The wake: recorded heads older than the interpolated head, then the head.
            int count = 0;
            int keep = v.Trail is null ? 0 : Math.Max(0, v.TrailCount - 2);
            for (int k = 0; k < keep && count < spine.Length - 1; k++) spine[count++] = v.Trail![k];
            Vector2 direction = projectile.velocity.LengthSquared() > .01f ? Vector2.Normalize(projectile.velocity) : Vector2.UnitX;
            var state = new LacunaPelletState(head, direction, v.PreviousAge + (v.CurrentAge - v.PreviousAge) * fraction, pellet.Iris,
                projectile.owner != Main.myPlayer, projectile.identity);
            LacunaPresentation.EmitPellet(canvas, state, spine.AsSpan(0, count), LacunaVisuals.Material);
        }
        int alive = 0;
        for (int k = 0; k < live; k++)
        {
            ref Mark mark = ref marks[k];
            float t = (float)(canvas.Clock - mark.Start);
            float life = mark.Bite ? LacunaPresentation.BiteLife : LacunaPresentation.PuffLife(canvas.Reduced);
            if (t < 0 || t >= life) continue;
            alive++;
            if (mark.Bite) LacunaPresentation.EmitBite(canvas, mark.At, t, mark.Seed, mark.Peer, mark.Beam);
            else LacunaPresentation.EmitPuff(canvas, mark.At, t, mark.Peer);
        }
        return any || alive > 0;
    }

    // World unload: the layer has dropped every source; forget the marks too.
    internal static void Clear()
    {
        Array.Clear(marks);
        next = live = 0;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class LacunaVisualsSystem : ModSystem
{
    public override void OnWorldUnload() => LacunaPelletSource.Clear();
    public override void Unload() => LacunaPelletSource.Clear();
}
