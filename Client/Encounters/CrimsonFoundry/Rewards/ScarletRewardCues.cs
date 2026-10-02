#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// Who hears a cue (REWARDS.md#multiplayer-readability).
internal enum ScarletCueAudience : byte
{
    Owner,    // build tolls: the owner only
    Shot,     // per-swing and per-shot cues: the owner at full level, other players RemoteShotDecibels lower with one voice
    Everyone, // windups, releases, finales, the quill's playback tolls and the reliquary: positional for everyone
}

// One shipped cue (REWARDS.md#art-and-audio): its files in Assets/Sounds/Weapons/ScarletRewards/, the take the owner
// chose on the 2026-10-03 audition, who hears it, how many voices it may hold (MaxInstances, replace oldest), the file's
// length, and Lead: the ticks from the trigger to the moment the file was built to meet (the first live tick of a
// swing, the downbeat of a windup); 0 when the attack sits at the head of the file.
internal readonly record struct ScarletCue(string Name, char Take, ScarletCueAudience Audience, int Voices, int Lead, float Seconds, int Files = 1)
{
    internal string File(int variant = 0) => Files == 1 ? Name : Name + (Math.Clamp(variant, 0, Files - 1) + 1);
    internal int Ticks => (int)MathF.Ceiling(Seconds * 60);

    // Other players' voices of this file, all of them together, in a pool apart from the local player's (Voices), so
    // another player's cue never cuts the local player's: one voice of a per-shot file (replace oldest, the multiplayer
    // rule); one owner's Voices of any other file, where a cue beyond the limit is dropped (ignore new) rather than
    // cutting a windup, release or finale that is ringing.
    internal int PeerVoices => Audience == ScarletCueAudience.Shot ? 1 : Voices;
    internal bool PeerReplacesOldest => Audience == ScarletCueAudience.Shot;
}

// The 35 cues and when they fire. Pure (no Terraria references): linked into the domain tests, which check every Lead
// against the rules' own moments. The recipe and the measurements are recorded in Assets/ATTRIBUTION.md ("Scarlet
// Invocation reward weapon audio - 2026-10-03").
internal static class ScarletRewardCues
{
    internal const string Root = "Convergence/Assets/Sounds/Weapons/ScarletRewards/";

    // The files carry the designed levels (BS.1770 maximum momentary loudness against the Raid's loudest strike, -7.6
    // LUFS measured on CrownRupture.wav, -7.7 to -8.4 for the Doll strikes this Raid plays: finales -9.6, cadence -11,
    // windups -15, cascade parts -15.5, per-shot -17, tolls -19), so every cue plays at one gain, exactly as auditioned.
    // The only offsets are below. Scarlet's own Raid sound set (feat/scarlet-sfx) plays its notes far softer: its merge
    // re-derives these levels role by role (REWARDS.md#art-and-audio; a tool test pins the Raid set they were set against).
    internal const float Gain = 1f;
    internal const float ScoreThrowDecibels = -2;   // the rolled score leaves the hand a little softer than a quill
    internal const float PartialScoreDecibels = -2; // a score burst without the Full Melody: the smaller burst

    // Shared
    internal const string ReliquaryOpen = nameof(ReliquaryOpen), Cadence = nameof(Cadence);
    internal const int Tolls = 8; // Toll0..Toll7: the E-flat sus2 ladder Eb3 F3 Bb3 Eb4 F4 Bb4 Eb5 F5
    // Scythe
    internal const string ScytheSwingHigh = nameof(ScytheSwingHigh), ScytheSwingLow = nameof(ScytheSwingLow), ScytheWhipBrace = nameof(ScytheWhipBrace),
        ScytheWhip = nameof(ScytheWhip), StaffWindup = nameof(StaffWindup), StaffCut = nameof(StaffCut), StaffBarline = nameof(StaffBarline);
    // Organ
    internal const string OrganShot = nameof(OrganShot), HymnInhale = nameof(HymnInhale), HandSlam = nameof(HandSlam), ChoirClasp = nameof(ChoirClasp);
    // Baton
    internal const string BatonStroke = nameof(BatonStroke), BatonLift = nameof(BatonLift), InkIgnite = nameof(InkIgnite), RiverRelease = nameof(RiverRelease);
    // Censer
    internal const string CenserSummon = nameof(CenserSummon), CenserSwing = nameof(CenserSwing), CenserPour = nameof(CenserPour),
        CenserBrace = nameof(CenserBrace), CenserGrandPour = nameof(CenserGrandPour);
    // Quill
    internal const string QuillThrow = nameof(QuillThrow), QuillStick = nameof(QuillStick), ScoreUnseal = nameof(ScoreUnseal),
        InkBlaze = nameof(InkBlaze), ScoreChord = nameof(ScoreChord);

    internal static string Toll(int step) => "Toll" + Math.Clamp(step, 0, Tolls - 1);

    // When each weapon fires its timed cues, in its own clock (a stroke's time, a gesture's clock), so Lead lands on the
    // spec's moment: the swing breath on the first live tick, the Whip's lash inside its live window.
    internal const int ScytheSwingAt = 0;
    internal const int ScytheWhipBraceAt = CrimsonRewardRules.WhipDrawStart;
    internal const int ScytheWhipAt = CrimsonRewardRules.WhipLiveStart;
    internal const int BatonStrokeAt = 0; // the gesture's first tick, so the swish peaks while the pen writes

    private const ScarletCueAudience Owner = ScarletCueAudience.Owner, Shot = ScarletCueAudience.Shot, Everyone = ScarletCueAudience.Everyone;

    internal static readonly ScarletCue[] All =
    {
        new(ReliquaryOpen, 'B', Everyone, 4, CrimsonRewardRules.ShowIgnite, 2.303f), // crack and lid at 10, the cadence at 20
        new("Toll0", 'A', Owner, 8, 0, .973f),
        new("Toll1", 'A', Owner, 8, 0, .973f),
        new("Toll2", 'A', Owner, 8, 0, .973f),
        new("Toll3", 'A', Owner, 8, 0, .973f),
        new("Toll4", 'A', Owner, 8, 0, .973f),
        new("Toll5", 'A', Owner, 8, 0, .973f),
        new("Toll6", 'A', Owner, 8, 0, .973f),
        new("Toll7", 'A', Owner, 8, 0, .973f),
        new(Cadence, 'B', Everyone, 2, 0, 2.303f),
        new(ScytheSwingHigh, 'A', Shot, 2, 5, .423f),
        new(ScytheSwingLow, 'A', Shot, 2, 5, .423f),
        new(ScytheWhipBrace, 'A', Shot, 2, 7, .263f),
        new(ScytheWhip, 'B', Shot, 2, 2, .553f),
        new(StaffWindup, 'B', Everyone, 2, 16, .623f),
        new(StaffCut, 'A', Everyone, 4, 2, .323f),
        new(StaffBarline, 'B', Everyone, 2, 2, 2.103f),
        new(OrganShot, 'A', Shot, 2, 0, .263f, Files: 4), // one file per pipe, OrganShot1..OrganShot4
        new(HymnInhale, 'A', Everyone, 2, 10, .623f),
        new(HandSlam, 'A', Everyone, 4, 0, .403f),
        new(ChoirClasp, 'A', Everyone, 2, 0, 2.203f),
        new(BatonStroke, 'A', Shot, 2, 6, .503f),
        new(BatonLift, 'A', Everyone, 2, 8, .503f),
        new(InkIgnite, 'A', Everyone, 4, 3, .383f),
        new(RiverRelease, 'B', Everyone, 2, 18, 2.403f), // the surge lands on the cadence 0.3 s in, while the head runs
        new(CenserSummon, 'A', Shot, 2, 0, 1.003f),
        new(CenserSwing, 'A', Everyone, 3, CrimsonRewardRules.SwingCueLead, .323f),
        new(CenserPour, 'A', Everyone, 5, 0, .503f),
        new(CenserBrace, 'A', Everyone, 2, CrimsonRewardRules.GrandBrace, .303f),
        new(CenserGrandPour, 'A', Everyone, 4, 0, 1.303f),
        new(QuillThrow, 'A', Shot, 3, 2, .263f),
        new(QuillStick, 'A', Shot, 3, 0, .243f),
        new(ScoreUnseal, 'A', Everyone, 2, CrimsonRewardRules.ScoreWindup, .403f),
        new(InkBlaze, 'A', Everyone, 2, 6, .753f),
        new(ScoreChord, 'A', Everyone, 2, 0, 2.303f),
    };

    internal static ScarletCue Get(string name)
    {
        foreach (ScarletCue cue in All)
            if (cue.Name == name) return cue;
        throw new ArgumentOutOfRangeException(nameof(name), name, "not a Scarlet reward cue");
    }

    internal static float Decibels(float db) => MathF.Pow(10, db / 20);
}
