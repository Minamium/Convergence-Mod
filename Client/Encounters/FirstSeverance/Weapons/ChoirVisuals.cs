#nullable enable
using System;
using Convergence.Client.Weapons;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// Choir of the Unmade's client half (2026-10 refresh). Reads the replicated concert (the lead voice's clock and
// target, every voice, note and beam) and only fills ChoirDrawState for ChoirPresentation.Emit through the shared
// Doll weapon layer; plays the cues through DollWeaponAudio on the accepted clock (DollCueClock, once per owner and
// concert tick); poses the conductor's arm. Presentation only: nothing here touches hits, input, packets or saved
// state, and a Dedicated Server never loads it.
internal static class ChoirClient
{
    internal const float PeerVolume = .7f;
    // Call-site volumes; tools/generate_doll_weapon_sfx.py records the same values for the audition page.
    internal const float SummonVolume = .7f, VerseWarnVolume = .8f, VerseVolume = .55f, OrganRiseVolume = .75f, PipeVolume = .6f;
    internal const float ChorusWarnVolume = .85f, ChorusFireVolume = .9f, ChorusEndVolume = .7f, MissVolume = .8f;
    internal const float NoteHitVolume = .6f, ChorusHitVolume = .55f;
    internal static readonly string[] VerseFire =
    {
        "ChoirVerseFire0", "ChoirVerseFire1", "ChoirVerseFire2", "ChoirVerseFire3", "ChoirVerseFire4",
        "ChoirVerseFire5", "ChoirVerseFire6", "ChoirVerseFire7", "ChoirVerseFire8",
    };
    internal static readonly string[] Pipe = { "ChoirPipe0", "ChoirPipe1", "ChoirPipe2", "ChoirPipe3", "ChoirPipe4", "ChoirPipe5" };
    internal static readonly string[] ChorusFire =
        { "ChoirChorusFire1", "ChoirChorusFire2", "ChoirChorusFire3", "ChoirChorusFire4", "ChoirChorusFire5", "ChoirChorusFire6" };

    // Cue slots: count-in, 24 verse slots (beat x part), organ, 6 pipe ranks, inhale, chorus, release.
    private const int SlotWarn = 0, SlotVerse = 1, SlotOrgan = 25, SlotPipe = 26, SlotInhale = 32, SlotFire = 33, SlotEnd = 34, Slots = 35;

    internal sealed class OwnerState
    {
        internal readonly ChoirDrawState Draw = new();
        internal readonly int[] Cues = new int[Slots];
        internal readonly Vector2[] Hits = new Vector2[ChoirDrawState.MaxSparks];
        internal readonly ulong[] HitTicks = new ulong[ChoirDrawState.MaxSparks];
        internal int HitHead, Voices = 1, ChordVoices = 1, CancelClock;
        internal float PreviousClock;
        internal ulong LeadTick, CancelTick, ResidueTick, ChorusFadeTick, LastNoteHit, LastChorusHit;
        internal bool Cancelled, Residue;
        internal Vector2 Stage, ResidueOrigin;
        internal float ResidueAngle;
        internal SlotId Chorus = SlotId.Invalid;

        internal OwnerState() => Array.Fill(Cues, DollCueClock.Armed);
    }

    private static OwnerState?[] owners = new OwnerState?[Main.maxPlayers + 1];
    private static readonly int[] touched = new int[Main.maxPlayers + 1];
    private static int touchedCount;
    internal static readonly ChoirSource Source = new();
    internal static readonly ChoirHymnMaterial Hymn = new();

    internal static OwnerState? Peek(int owner) => (uint)owner < (uint)owners.Length ? owners[owner] : null;

    internal static OwnerState Owner(int owner) => owners[Math.Clamp(owner, 0, owners.Length - 1)] ??= new OwnerState();

    // The running concert clock of a player's lead voice this tick, or 0.
    internal static float ClockOf(int owner)
        => Peek(owner) is { } o && o.LeadTick + 1 >= Main.GameUpdateCount ? o.PreviousClock : 0;

    internal static void Clear()
    {
        foreach (OwnerState? o in owners)
            if (o is not null) DollWeaponAudio.Stop(ref o.Chorus);
        owners = new OwnerState?[Main.maxPlayers + 1];
        touchedCount = 0;
        Hymn.Effect = null;
        Hymn.NoiseA = Hymn.NoiseB = null;
    }

    private static float Volume(int owner, float volume) => owner == Main.myPlayer ? volume : volume * PeerVolume;

    // The lead voice's accepted clock, once per real tick: concert cues, a cancel's miss cue and organ crumble.
    internal static void Concert(Projectile p, ChoirChorister lead)
    {
        OwnerState o = Owner(p.owner);
        float clock = lead.Clock, previous = o.PreviousClock;
        o.LeadTick = Main.GameUpdateCount;
        o.Voices = lead.Voices;
        o.Stage = ChoirConcert.X(lead.Stage);
        // A concert ends by returning to idle (0); a jump back of more than a few ticks is a fresh score. A peer's
        // one- or two-tick correction is neither.
        bool wrapped = previous >= ChoirConcertRules.Cycle - 2 && clock >= 1 && clock <= 3;
        bool stopped = clock == 0 && previous > 0;
        if (stopped && previous >= ChoirConcertRules.Gather && previous < ChoirConcertRules.Release) Cancel(o, p.owner, (int)previous);
        if (stopped || wrapped || clock < previous - DollCueClock.FreshScoreDrop) Array.Fill(o.Cues, DollCueClock.Armed);
        if (clock > 0) Cues(o, p, previous, clock);
        o.PreviousClock = clock;
    }

    // A concert that stopped with its organ open (a lost target, an unusable owner, every voice gone): the failure
    // cue, the chorus fading out over CloseTicks and the organ crumbling.
    private static void Cancel(OwnerState o, int owner, int clock)
    {
        o.Cancelled = true;
        o.CancelClock = clock;
        o.CancelTick = Main.GameUpdateCount;
        DollWeaponAudio.Play("ChoirChorusMiss", o.Stage, Volume(owner, MissVolume));
        if (o.Chorus.IsValid) o.ChorusFadeTick = Main.GameUpdateCount;
    }

    private static void Cues(OwnerState o, Projectile lead, float previous, float clock)
    {
        int owner = lead.owner;
        Vector2 stage = o.Stage;
        if (DollCueClock.Take(ref o.Cues[SlotWarn], previous, clock, ChoirConcertRules.TapA))
            DollWeaponAudio.Play("ChoirVerseWarn", lead.Center, Volume(owner, VerseWarnVolume));
        for (int beat = 0; beat < ChoirConcertRules.VerseBeats; beat++)
        for (int part = 0; part < ChoirConcertRules.Parts; part++)
        {
            if (!DollCueClock.Take(ref o.Cues[SlotVerse + beat * ChoirConcertRules.Parts + part], previous, clock,
                    ChoirConcertRules.NoteTick(beat, part))) continue;
            // Voices sharing this slot sing one cue, a little louder (at most 1.5x).
            int voices = 0;
            Vector2 at = Vector2.Zero;
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.owner != owner || p.ModProjectile is not ChoirChorister voice || ChoirConcertRules.Part(voice.Ordinal) != part) continue;
                voices++;
                at += p.Center;
            }
            if (voices == 0) continue;
            float merge = MathF.Min(1.5f, 1 + .25f * MathF.Log2(voices));
            DollWeaponAudio.Play(VerseFire[ChoirConcertRules.NotePitch(beat, part)], at / voices, Volume(owner, VerseVolume * merge));
        }
        if (DollCueClock.Take(ref o.Cues[SlotOrgan], previous, clock, ChoirConcertRules.Gather))
            DollWeaponAudio.Play("ChoirOrganRise", stage, Volume(owner, OrganRiseVolume));
        int raised = ChoirConcertRules.RaisedRanks(o.Voices);
        for (int rank = 0; rank < ChoirConcertRules.Ranks; rank++)
            if (DollCueClock.Take(ref o.Cues[SlotPipe + rank], previous, clock, ChoirConcertRules.PipeSeat(rank)) && rank < raised)
                DollWeaponAudio.Play(Pipe[rank], stage + new Vector2(0, -110), Volume(owner, PipeVolume));
        if (DollCueClock.Take(ref o.Cues[SlotInhale], previous, clock, ChoirConcertRules.Inhale))
            DollWeaponAudio.Play("ChoirChorusWarn", stage, Volume(owner, ChorusWarnVolume));
        if (DollCueClock.Take(ref o.Cues[SlotFire], previous, clock, ChoirConcertRules.Fire))
        {
            o.ChordVoices = ChoirConcertRules.ChordVoices(o.Voices);
            DollWeaponAudio.Stop(ref o.Chorus);
            o.ChorusFadeTick = 0;
            o.Chorus = DollWeaponAudio.Play(ChorusFire[o.ChordVoices - 1], stage, Volume(owner, ChorusFireVolume));
            ModContent.GetInstance<RitualWeaponFeedback>().Kick(owner, 3);
        }
        if (DollCueClock.Take(ref o.Cues[SlotEnd], previous, clock, ChoirConcertRules.Release))
            DollWeaponAudio.Play("ChoirChorusEnd", stage, Volume(owner, ChorusEndVolume));
    }

    // Once per tick after every projectile updated: a concert whose lead vanished (death, dismissal, every voice
    // sacrificed) is cancelled; a cancelled chorus cue fades out over CloseTicks.
    internal static void PostUpdate()
    {
        ulong now = Main.GameUpdateCount;
        for (int i = 0; i < owners.Length; i++)
        {
            if (owners[i] is not { } o) continue;
            if (o.LeadTick < now && o.PreviousClock > 0)
            {
                if (o.PreviousClock >= ChoirConcertRules.Gather && o.PreviousClock < ChoirConcertRules.Release) Cancel(o, i, (int)o.PreviousClock);
                o.PreviousClock = 0;
                Array.Fill(o.Cues, DollCueClock.Armed);
            }
            if (o.ChorusFadeTick != 0)
            {
                float t = (now - o.ChorusFadeTick) / (float)ChoirConcertRules.CloseTicks;
                if (t >= 1 || !SoundEngine.TryGetActiveSound(o.Chorus, out var sound)) { DollWeaponAudio.Stop(ref o.Chorus); o.ChorusFadeTick = 0; }
                else sound.Volume = 1 - t;
            }
        }
    }

    internal static void NoteHit(Projectile note)
    {
        OwnerState o = Owner(note.owner);
        o.Hits[o.HitHead] = note.Center;
        o.HitTicks[o.HitHead] = Main.GameUpdateCount;
        o.HitHead = (o.HitHead + 1) % o.Hits.Length;
        if (Main.GameUpdateCount - o.LastNoteHit < 6) return;
        o.LastNoteHit = Main.GameUpdateCount;
        DollWeaponAudio.Play("ChoirNoteHit", note.Center, Volume(note.owner, NoteHitVolume));
    }

    // Beam contact is known only to its owner (native hits run there): an owner-local accent, one per 24 ticks.
    internal static void ChorusHit(Projectile beam)
    {
        OwnerState o = Owner(beam.owner);
        if (Main.GameUpdateCount - o.LastChorusHit < 24) return;
        o.LastChorusHit = Main.GameUpdateCount;
        DollWeaponAudio.Play("ChoirChorusHit", beam.Center + beam.velocity.SafeNormalize(Vector2.UnitX) * 160, ChorusHitVolume);
    }

    internal static void BeginResidue(Projectile beam, float angle)
    {
        OwnerState o = Owner(beam.owner);
        o.Residue = true;
        o.ResidueTick = Main.GameUpdateCount;
        o.ResidueOrigin = beam.Center;
        o.ResidueAngle = angle;
    }

    // ---- Drawing -------------------------------------------------------------------------------------------

    // Fills every owner's draw state from the replicated projectiles and the conductors, then emits them.
    internal static bool Emit(DollWeaponCanvas canvas)
    {
        if (Main.gameMenu) return false;
        BindMaterial();
        float fraction = canvas.Fraction;
        double now = Main.GameUpdateCount + (double)fraction;
        touchedCount = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is not (ChoirChorister or ChoirSungNote or ChoirChorus)) continue;
            OwnerState o = Touch(p.owner);
            ChoirDrawState s = o.Draw;
            ChoirVisuals visuals = p.GetGlobalProjectile<ChoirVisuals>();
            switch (p.ModProjectile)
            {
                case ChoirChorister voice:
                    s.VoiceTotal++;
                    if (voice.IsLead)
                    {
                        s.Stage = visuals.Stage(fraction);
                        s.Clock = ChoirConcertRules.Running(voice.Clock) ? voice.Clock + fraction : 0;
                    }
                    if (s.VoiceCount >= ChoirDrawState.MaxVoices) break;
                    s.Voices[s.VoiceCount++] = new ChoirVoiceDraw
                    {
                        Center = visuals.Center(p, fraction), Velocity = visuals.Velocity, Facing = p.spriteDirection,
                        Variant = voice.Variant, Ordinal = voice.Ordinal, Life = voice.Life + fraction,
                    };
                    break;
                case ChoirSungNote note:
                    if (s.NoteCount >= ChoirDrawState.MaxNotes) break;
                    s.Notes[s.NoteCount++] = new ChoirNoteDraw
                    {
                        Center = visuals.Center(p, fraction), Pitch = note.Pitch, Age = note.Age + fraction,
                        Fade = note.Fading ? note.Fade : 0, Trail = visuals.Trail, TrailCount = visuals.TrailCount, TrailHead = visuals.TrailHead,
                    };
                    break;
                case ChoirChorus beam:
                    s.Beam = true;
                    s.BeamAngle = visuals.Angle(fraction);
                    s.BeamAge = beam.Closing ? beam.Age : beam.Age + fraction;
                    s.BeamClose = beam.Closing ? Math.Clamp(beam.CloseProgress + fraction / ChoirConcertRules.CloseTicks, 0, 1) : 0;
                    s.ChordVoices = o.ChordVoices;
                    s.BeamOrigin = visuals.Center(p, fraction);
                    break;
            }
        }
        // Owners whose organ is still crumbling, whose beam residue is cooling or whose notes just struck, even
        // with no projectile left this frame.
        for (int i = 0; i < Main.maxPlayers; i++)
            if (owners[i] is { } o && (o.Cancelled || o.Residue || Main.GameUpdateCount - o.HitTicks[(o.HitHead + o.Hits.Length - 1) % o.Hits.Length] <= 16))
                Touch(i);
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (!player.active || player.GetModPlayer<ChoirConductor>() is not { Posing: true } conductor) continue;
            ChoirDrawState s = Touch(i).Draw;
            conductor.Fill(s, fraction);
        }
        bool any = false;
        for (int n = 0; n < touchedCount; n++)
        {
            int owner = touched[n];
            OwnerState o = owners[owner]!;
            ChoirDrawState s = o.Draw;
            s.Peer = owner != Main.myPlayer;
            s.Seed = owner;
            if (s.VoiceTotal > 0 && s.Clock <= 0 && o.LeadTick + 1 < Main.GameUpdateCount) s.Clock = 0;
            if (o.Cancelled)
            {
                float age = (float)(now - o.CancelTick);
                if (age >= ChoirPresentation.CrumbleTicks + 30) o.Cancelled = false;
                else { s.CancelAge = age; s.CancelClock = o.CancelClock; s.Stage = s.VoiceTotal > 0 ? s.Stage : o.Stage; }
            }
            if (o.Residue)
            {
                float age = (float)(now - o.ResidueTick);
                if (age >= ChoirPresentation.ResidueTicks) o.Residue = false;
                else
                {
                    s.ResidueAge = age;
                    s.ResidueOrigin = o.ResidueOrigin;
                    s.ResidueAngle = o.ResidueAngle;
                    s.ResidueLength = ChoirConcertRules.BeamLength;
                }
            }
            for (int h = 0; h < o.Hits.Length; h++)
            {
                float age = (float)(now - o.HitTicks[h]);
                if (o.HitTicks[h] == 0 || age > 16 || s.SparkCount >= ChoirDrawState.MaxSparks) continue;
                s.Sparks[s.SparkCount++] = new ChoirSpark { At = o.Hits[h], Age = age, Seed = (int)(o.HitTicks[h] % 9973) + h * 31 };
            }
            if (s.Empty) continue;
            any = true;
            var sprites = new ChoirSprites(DollWeaponTextures.Get("Chorister0"), DollWeaponTextures.Get("Chorister1"),
                DollWeaponTextures.Get("Chorister2"), DollWeaponTextures.Get("ChoirOrgan"), DollWeaponTextures.Get("ChoirBaton"));
            ChoirPresentation.Emit(canvas, s, in sprites, Hymn);
        }
        return any || touchedCount > 0;
    }

    private static OwnerState Touch(int owner)
    {
        OwnerState o = Owner(owner);
        if (Array.IndexOf(touched, owner, 0, touchedCount) < 0)
        {
            o.Draw.Clear();
            touched[touchedCount++] = owner;
        }
        return o;
    }

    // The material reads Luminance's shader and noise every frame (never cached across frames).
    private static void BindMaterial()
    {
        try
        {
            Hymn.Effect = ShaderManager.GetShader(ChoirHymnMaterial.ShaderName).WrappedEffect;
            Hymn.NoiseA = MiscTexturesRegistry.TurbulentNoise.Value;
            Hymn.NoiseB = MiscTexturesRegistry.WavyBlotchNoise.Value;
        }
        catch (Exception)
        {
            Hymn.Effect = null;
        }
    }
}

// The single layer source for every owner's concert (registered while any Choir projectile or pose exists).
internal sealed class ChoirSource : IDollWeaponSource
{
    public bool Emit(DollWeaponCanvas canvas) => ChoirClient.Emit(canvas);
}

// Per projectile: interpolation between accepted ticks, note trails, cue clocks for the summon, beam residue and
// owner-local beam contact.
[Autoload(Side = ModSide.Client)]
internal sealed class ChoirVisuals : GlobalProjectile
{
    private Vector2 before, now, stageBefore, stageNow;
    private float angleBefore, angleNow, previousLife = -1, previousLive = -1;
    private bool initialized;
    private int summonCue = DollCueClock.Armed, hits;
    internal Vector2 Velocity;
    internal Vector2[]? Trail;
    internal int TrailCount, TrailHead;

    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        => entity.ModProjectile is ChoirChorister or ChoirSungNote or ChoirChorus;

    internal Vector2 Center(Projectile p, float fraction) => initialized ? Vector2.Lerp(before, now, fraction) : p.Center;
    internal Vector2 Stage(float fraction) => Vector2.Lerp(stageBefore, stageNow, fraction);
    internal float Angle(float fraction) => angleBefore + MathHelper.WrapAngle(angleNow - angleBefore) * fraction;

    public override void PostAI(Projectile p)
    {
        if (!RitualPresentationStep.IsFinal(p.numUpdates) || Main.dedServ) return;
        DollWeaponLayer.Add(ChoirClient.Source);
        if (!initialized || Vector2.DistanceSquared(now, p.Center) > 900 * 900) before = now = p.Center;
        else { before = now; now = p.Center; }
        Velocity = now - before;
        switch (p.ModProjectile)
        {
            case ChoirChorister voice:
                Vector2 stage = ChoirConcert.X(voice.Stage);
                if (!initialized) stageBefore = stageNow = stage;
                else { stageBefore = stageNow; stageNow = stage; }
                // The summon cue plays once when this voice appears (a late replica is past it and stays silent).
                if (DollCueClock.Take(ref summonCue, previousLife, voice.Life, 1))
                    DollWeaponAudio.Play("ChoirSummon", p.Center, p.owner == Main.myPlayer ? ChoirClient.SummonVolume : ChoirClient.SummonVolume * ChoirClient.PeerVolume);
                previousLife = voice.Life;
                if (voice.IsLead) ChoirClient.Concert(p, voice);
                break;
            case ChoirSungNote:
                Trail ??= new Vector2[ChoirDrawState.TrailPoints];
                Trail[TrailHead] = p.Center;
                TrailHead = (TrailHead + 1) % Trail.Length;
                TrailCount = Math.Min(Trail.Length, TrailCount + 1);
                break;
            case ChoirChorus beam:
            {
                float angle = beam.Axis.ToRotation();
                if (!initialized) angleBefore = angleNow = angle;
                else
                {
                    angleBefore = angleNow;
                    // Peers smooth the owner's six-tick aim corrections.
                    angleNow += MathHelper.WrapAngle(angle - angleNow) * (p.owner == Main.myPlayer ? 1f : .46f);
                }
                float live = beam.Live;
                if (previousLive >= 0 && previousLive < ChoirConcertRules.LiveTicks && (beam.Closing || live >= ChoirConcertRules.LiveTicks - 1))
                {
                    ChoirClient.BeginResidue(p, angleNow);
                    previousLive = -1;
                }
                else if (!beam.Closing) previousLive = live;
                if (beam.Hits != hits)
                {
                    hits = beam.Hits;
                    ChoirClient.ChorusHit(p);
                }
                break;
            }
        }
        initialized = true;
    }

    public override void OnKill(Projectile p, int timeLeft)
    {
        if (Main.dedServ || Main.gameMenu) return;
        // A note that struck (killed with life left, not fading out): sparks and the porcelain ping.
        if (p.ModProjectile is ChoirSungNote { Fading: false } && timeLeft > 0) ChoirClient.NoteHit(p);
    }
}

// The conductor: while summoning (the use animation) the baton flicks a downbeat; while the owner holds the Choir
// during a running concert the baton beats time and rises through the inhale and the chorus. The arm follows the
// baton (SetCompositeArmFront each tick for every client, a sub-tick angle through DollWeaponArmDraw).
[Autoload(Side = ModSide.Client)]
internal sealed class ChoirConductor : ModPlayer, IDollArmPose
{
    private bool flicking;
    private float u, du, clock;
    internal bool Posing { get; private set; }

    public override void PostUpdate()
    {
        bool held = Player.active && !Player.dead && Player.HeldItem.type == ModContent.ItemType<ChoirOfTheUnmade>()
            && ChoirConcert.Usable(Player);
        flicking = held && Player.itemAnimation > 0 && Player.itemAnimationMax > 0;
        clock = held ? ChoirClient.ClockOf(Player.whoAmI) : 0;
        if (flicking)
        {
            du = 1f / Player.itemAnimationMax;
            u = 1 - Player.itemAnimation / (float)Player.itemAnimationMax;
        }
        Posing = flicking || clock > 0;
        if (!Posing) return;
        Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, World(0) - MathHelper.PiOver2);
        DollWeaponArmDraw.Set(Player.whoAmI, this);
        DollWeaponLayer.Add(ChoirClient.Source);
    }

    // The baton tip angle (right-facing) at a sub-tick offset.
    private float Local(float fraction)
        => flicking ? ChoirConducting.Flick(u + fraction * du) : ChoirConducting.Conduct(clock + fraction);

    private float World(float fraction) => ChoirConducting.World(Local(fraction), Player.direction);

    public bool TryGetArmPose(Player player, float fraction, out float front, out float? back)
    {
        back = null;
        front = 0;
        if (!Posing || player.whoAmI != Player.whoAmI) return false;
        front = World(fraction) - MathHelper.PiOver2;
        return true;
    }

    internal void Fill(ChoirDrawState s, float fraction)
    {
        float angle = World(fraction);
        s.Baton = true;
        s.BatonFacing = Player.direction;
        s.BatonAngle = angle;
        s.BatonGrip = Player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, angle - MathHelper.PiOver2);
        s.BatonSwing = Math.Clamp(MathF.Abs(Local(fraction) - Local(fraction - 1)) * 4, 0, 1);
        s.BatonTrailCount = 0;
        for (int k = ChoirDrawState.BatonTrailPoints - 1; k >= 0; k--)
        {
            float back = ChoirConducting.World(Local(fraction - k * .5f), Player.direction);
            Vector2 grip = Player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, back - MathHelper.PiOver2);
            s.BatonTrail[s.BatonTrailCount++] = ChoirPresentation.BatonTipWorld(grip, back);
        }
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class ChoirClientSystem : ModSystem
{
    public override void PostUpdateProjectiles() => ChoirClient.PostUpdate();
    public override void OnWorldUnload() => ChoirClient.Clear();
    public override void Unload() => ChoirClient.Clear();
}
