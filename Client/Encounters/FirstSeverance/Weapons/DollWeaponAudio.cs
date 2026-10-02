#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// The one cue player of the Doll reward weapons and the Doll companion. Cues are rendered by
// tools/generate_doll_weapon_sfx.py into Assets/Sounds/Weapons/DollWeapons/ (never DollTheater).
// - SoundStyle is built only on a client outside the main menu: a Dedicated Server never touches audio.
// - A cue whose file is not packaged is skipped (HasAsset, cached), never thrown.
// - Volume is applied as given (no doubling); Reduced Effects never changes what is heard.
// - IgnoreNew with a per-cue instance limit: a playing voice is never cut without a ramp
//   (ReplaceOldest stops matching voices abruptly, which clicks; see AzureAudio).
// - At most VoiceCap voices are tracked; beyond that a new voice is refused, not swapped in.
// - Owner priority: another player's weapon cues (PlayFor with a peer owner; Sustain for a peer) never take the last
//   OwnerReserve voices, and a peer one-shot uses its own per-cue pool (PeerInstances under a separate identifier), so
//   a full lobby cannot starve the local player's own cues of voices or instances.
internal static class DollWeaponAudio
{
    internal const string Root = "Convergence/Assets/Sounds/Weapons/DollWeapons/";
    internal const int VoiceCap = 32, DefaultInstances = 2;
    internal const int OwnerReserve = 8, PeerInstances = 1;
    // Voices one cue may hold at once; a weapon adds its cues here. Unlisted cues allow DefaultInstances.
    // The limit is per cue file (one SoundStyle.Identifier), and IgnoreNew drops the voice that would exceed it.
    // A cue played as an arpeggio, a fast ratchet or any overlapping tail therefore needs a larger entry than
    // DefaultInstances, or its later notes are silently dropped while the earlier ones still ring.
    private static readonly Dictionary<string, int> instances = new(StringComparer.Ordinal)
    {
        ["CompanionSummon"] = 2,
        // Lacuna Testament: openings and pellets of seven irises overlap near the end of the build (up to ~9 a second).
        ["LacunaIrisWarn"] = 3, ["LacunaIrisFire"] = 4, ["LacunaIrisTine"] = 4, ["LacunaPelletWarn"] = 4, ["LacunaPelletFire"] = 6,
        ["LacunaPelletHit"] = 6, ["LacunaBeamHit"] = 3,
        // Pale Meridian: one file per ladder step; a pitch can return while its last note still rings.
        ["MeridianNote0"] = 3, ["MeridianNote1"] = 3, ["MeridianNote2"] = 3, ["MeridianNote3"] = 3, ["MeridianNote4"] = 3,
        ["MeridianNote5"] = 3, ["MeridianNote6"] = 3, ["MeridianNote7"] = 3, ["MeridianNote8"] = 3,
        ["MeridianHit"] = 3, ["MeridianHitHeavy"] = 3,
        // Last Witness: a testimony shot or a shard hit of one owner may overlap another owner's.
        ["WitnessTestimonyFire"] = 3, ["WitnessShardHit"] = 4,
    };
    private static readonly Dictionary<string, bool> present = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, SoundStyle> styles = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, SoundStyle> peerStyles = new(StringComparer.Ordinal);
    private static readonly List<SlotId> voices = new(VoiceCap);
    private static readonly Dictionary<LeaseKey, Lease> sustains = new();
    private static readonly List<LeaseKey> ended = new();

    // Which loop a Sustain call belongs to. A struct key, so the per-tick lookup allocates nothing.
    private readonly record struct LeaseKey(int Owner, int Identity, string Cue);

    private sealed class Lease
    {
        internal SlotId Voice;
        internal Vector2 Position;
        internal float Gain;
        internal ulong Touched;
    }

    private static bool Audible => !Main.dedServ && !Main.gameMenu;

    internal static int Instances(string cue) => instances.TryGetValue(cue, out int max) ? max : DefaultInstances;

    // One-shot at a world position. `pitch` is SoundStyle.Pitch (an octave fraction); tonal cues keep variance 0.
    internal static SlotId Play(string cue, Vector2 at, float volume, float pitch = 0, float variance = 0)
    {
        if (!Audible || !Exists(cue) || !Admit()) return SlotId.Invalid;
        SoundStyle style = Style(cue);
        style.Volume = Math.Clamp(volume, 0f, 1f);
        style.Pitch = Math.Clamp(pitch, -1f, 1f);
        style.PitchVariance = Math.Max(0f, variance);
        SlotId voice = SoundEngine.PlaySound(style, at);
        Track(cue, voice, style.Volume);
        return voice;
    }

    // A one-shot of player `owner`'s weapon: the local player's own cue plays as Play does; another player's cue is
    // admitted only while OwnerReserve voices stay free and draws on its own pool (PeerInstances per cue, identifier
    // "...:Peer:<cue>"), so it never uses up the voices or per-cue instances the local player's cues need.
    internal static SlotId PlayFor(int owner, string cue, Vector2 at, float volume)
    {
        if (owner == Main.myPlayer) return Play(cue, at, volume);
        if (!Audible || !Exists(cue) || !Admit() || voices.Count >= VoiceCap - OwnerReserve) return SlotId.Invalid;
        SoundStyle style = PeerStyle(cue);
        style.Volume = Math.Clamp(volume, 0f, 1f);
        SlotId voice = SoundEngine.PlaySound(style, at);
        Track(cue, voice, style.Volume);
        return voice;
    }

    // A single-note cue recorded at ladder step `root`, played at ladder step `step` (DollWeaponTuning, 0 cents).
    // Every note of one cue shares that cue's `instances` limit (default 2, IgnoreNew): a weapon that plays this
    // cue as an arpeggio must first give the cue a larger entry there, or the third overlapping note is dropped.
    internal static SlotId Note(string cue, int root, int step, Vector2 at, float volume)
        => DollWeaponTuning.Reachable(step, root) ? Play(cue, at, volume, DollWeaponTuning.Pitch(step, root)) : SlotId.Invalid;

    // A looped voice owned by (owner, identity, cue). Call it every tick while the loop should sound: the voice
    // follows `at` and takes the new volume, and it stops by itself two ticks after the last call (a killed
    // projectile, a lost owner, a world change), so a missed Stop never leaves it ringing.
    // The loop's SoundStyle.Volume is fixed at 1 and the gain lives on the lease and ActiveSound.Volume (which
    // multiplies it), so a loop may start at 0 and fade in; no call divides by the style volume. A call that
    // finds a live lease builds no string; the identifier is built only when a voice starts.
    internal static void Sustain(ref SlotId slot, string cue, int owner, int identity, Vector2 at, float volume)
    {
        if (!Audible || !Exists(cue)) { Stop(ref slot); return; }
        LeaseKey key = new(owner, identity, cue);
        float gain = Math.Clamp(volume, 0f, 1f);
        if (sustains.TryGetValue(key, out var lease) && SoundEngine.TryGetActiveSound(lease.Voice, out var active) && active.IsPlaying)
        {
            lease.Position = at;
            lease.Gain = gain;
            lease.Touched = Main.GameUpdateCount;
            active.Volume = gain;
            slot = lease.Voice;
            return;
        }
        sustains.Remove(key);
        slot = SlotId.Invalid;
        if (!Admit() || owner != Main.myPlayer && voices.Count >= VoiceCap - OwnerReserve) return;
        var started = new Lease { Position = at, Gain = gain, Touched = Main.GameUpdateCount };
        SoundStyle style = new(Root + cue)
        {
            Identifier = $"Convergence:DollWeapon:Sustain:{owner}:{identity}:{cue}",
            IsLooped = true, MaxInstances = 1, Volume = 1f,
            SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
            PauseBehavior = PauseBehavior.StopWhenGamePaused, PlayOnlyIfFocused = true,
        };
        started.Voice = SoundEngine.PlaySound(style, at, sound =>
        {
            sound.Position = started.Position;
            sound.Volume = started.Gain;
            return Main.GameUpdateCount - started.Touched <= 2;
        });
        if (!started.Voice.IsValid) return;
        sustains[key] = started;
        slot = started.Voice;
        Track(cue, started.Voice, gain);
    }

    internal static void Stop(ref SlotId slot)
    {
        if (SoundEngine.TryGetActiveSound(slot, out var sound)) sound.Stop();
        slot = SlotId.Invalid;
    }

    // World change: every tracked voice stops. Idempotent.
    internal static void StopAll()
    {
        foreach (var id in voices) if (SoundEngine.TryGetActiveSound(id, out var sound)) sound.Stop();
        voices.Clear();
        sustains.Clear();
    }

    // Mod unload: also forget the cached styles and asset lookups. Idempotent.
    internal static void Reset()
    {
        StopAll();
        styles.Clear();
        peerStyles.Clear();
        present.Clear();
    }

    private static SoundStyle Style(string cue)
    {
        if (!styles.TryGetValue(cue, out var style))
            styles[cue] = style = new SoundStyle(Root + cue)
            {
                Identifier = "Convergence:DollWeapon:" + cue, MaxInstances = Instances(cue),
                SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
                PauseBehavior = PauseBehavior.StopWhenGamePaused, PlayOnlyIfFocused = true,
            };
        return style;
    }

    // The cached one-shot style under the peer identifier and limit (IgnoreNew and the rest unchanged).
    private static SoundStyle PeerStyle(string cue)
    {
        if (!peerStyles.TryGetValue(cue, out var style))
        {
            style = Style(cue);
            style.Identifier = "Convergence:DollWeapon:Peer:" + cue;
            style.MaxInstances = PeerInstances;
            peerStyles[cue] = style;
        }
        return style;
    }

    private static bool Exists(string cue)
    {
        if (!present.TryGetValue(cue, out bool ok))
            present[cue] = ok = ModContent.HasAsset(Root + cue);
        return ok;
    }

    // Forget finished voices and their leases; a new voice is admitted only below the cap.
    private static bool Admit()
    {
        voices.RemoveAll(id => !Playing(id));
        foreach (var (key, lease) in sustains) if (!Playing(lease.Voice)) ended.Add(key);
        foreach (var key in ended) sustains.Remove(key);
        ended.Clear();
        return voices.Count < VoiceCap;
    }

    private static bool Playing(SlotId id) => SoundEngine.TryGetActiveSound(id, out var sound) && sound.IsPlaying;

    private static void Track(string cue, SlotId voice, float gain)
    {
        if (!voice.IsValid) return;
        voices.Add(voice);
        RitualAudioDiagnostics.Track(cue, voice, gain);
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class DollWeaponAudioSystem : ModSystem
{
    public override void OnWorldUnload() => DollWeaponAudio.StopAll();
    public override void Unload() => DollWeaponAudio.Reset();
}
