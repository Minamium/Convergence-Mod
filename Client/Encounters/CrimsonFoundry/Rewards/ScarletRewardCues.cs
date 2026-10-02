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

// How loud a cue plays against the Raid's own sound set (REWARDS.md#art-and-audio, "Levels against the Raid"). Every cue
// of one role plays at that role's offset, so the balance the owner auditioned inside a role is kept.
internal enum ScarletCueRole : byte
{
    Build,   // the build tolls: under every one-shot
    Shot,    // one-shot weapon cues (swings, shots, sticks, strokes, the censer's summon and swing): at least 3 dB under ScarletImpact
    Windup,  // windups and braces: no louder than ScarletForetell + 2 dB
    Release, // the parts of a release cascade: under ScarletImpact
    Finale,  // the finales and the shared Cadence: no louder than ScarletCrossflowRelease
    Show,    // the reliquary's opening show: no louder than ScarletVictory
}

// One shipped cue (REWARDS.md#art-and-audio): its files in Assets/Sounds/Weapons/ScarletRewards/, the take the owner
// chose on the 2026-10-03 audition, who hears it, its level role, how many voices it may hold (MaxInstances, replace
// oldest), the file's length, and Lead: the ticks from the trigger to the moment the file was built to meet (the first
// live tick of a swing, the downbeat of a windup); 0 when the attack sits at the head of the file.
internal readonly record struct ScarletCue(string Name, char Take, ScarletCueAudience Audience, ScarletCueRole Role, int Voices, int Lead, float Seconds, int Files = 1)
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

    // The files are the owner's picks byte for byte and are never re-rendered for level. They play at Gain times their
    // role's offset (RoleDecibels), staged against the Raid's own sound set as the Raid plays it (ScarletSounds, gain 1),
    // by maximum 400 ms momentary loudness on the recipe's meter and on BS.1770 (REWARDS.md#art-and-audio has both
    // tables; a tool test holds the relations on both): every cue that sounds in play (tolls, one-shots, windups,
    // cascade parts) plays 8.5 dB under its file, so their auditioned balance is kept, and the finales, the Cadence and
    // the reliquary's show 3 dB under theirs.
    internal const float Gain = 1f;
    internal const float BuildDecibels = -8.5f, ShotDecibels = -8.5f, WindupDecibels = -8.5f, ReleaseDecibels = -8.5f;
    internal const float FinaleDecibels = -3f, ShowDecibels = -3f;
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
    private const ScarletCueRole Build = ScarletCueRole.Build, OneShot = ScarletCueRole.Shot, Windup = ScarletCueRole.Windup, Release = ScarletCueRole.Release,
        Finale = ScarletCueRole.Finale, Show = ScarletCueRole.Show;

    internal static readonly ScarletCue[] All =
    {
        new(ReliquaryOpen, 'B', Everyone, Show, 4, CrimsonRewardRules.ShowIgnite, 2.303f), // crack and lid at 10, the cadence at 20
        new("Toll0", 'A', Owner, Build, 8, 0, .973f),
        new("Toll1", 'A', Owner, Build, 8, 0, .973f),
        new("Toll2", 'A', Owner, Build, 8, 0, .973f),
        new("Toll3", 'A', Owner, Build, 8, 0, .973f),
        new("Toll4", 'A', Owner, Build, 8, 0, .973f),
        new("Toll5", 'A', Owner, Build, 8, 0, .973f),
        new("Toll6", 'A', Owner, Build, 8, 0, .973f),
        new("Toll7", 'A', Owner, Build, 8, 0, .973f),
        new(Cadence, 'B', Everyone, Finale, 2, 0, 2.303f),
        new(ScytheSwingHigh, 'A', Shot, OneShot, 2, 5, .423f),
        new(ScytheSwingLow, 'A', Shot, OneShot, 2, 5, .423f),
        new(ScytheWhipBrace, 'A', Shot, Windup, 2, 7, .263f),
        new(ScytheWhip, 'B', Shot, OneShot, 2, 2, .553f),
        new(StaffWindup, 'B', Everyone, Windup, 2, 16, .623f),
        new(StaffCut, 'A', Everyone, Release, 4, 2, .323f),
        new(StaffBarline, 'B', Everyone, Finale, 2, 2, 2.103f),
        new(OrganShot, 'A', Shot, OneShot, 2, 0, .263f, Files: 4), // one file per pipe, OrganShot1..OrganShot4
        new(HymnInhale, 'A', Everyone, Windup, 2, 10, .623f),
        new(HandSlam, 'A', Everyone, Release, 4, 0, .403f),
        new(ChoirClasp, 'A', Everyone, Finale, 2, 0, 2.203f),
        new(BatonStroke, 'A', Shot, OneShot, 2, 6, .503f),
        new(BatonLift, 'A', Everyone, Windup, 2, 8, .503f),
        new(InkIgnite, 'A', Everyone, Release, 4, 3, .383f),
        new(RiverRelease, 'B', Everyone, Finale, 2, 18, 2.403f), // the surge lands on the cadence 0.3 s in, while the head runs
        new(CenserSummon, 'A', Shot, OneShot, 2, 0, 1.003f),
        new(CenserSwing, 'A', Everyone, OneShot, 3, CrimsonRewardRules.SwingCueLead, .323f),
        new(CenserPour, 'A', Everyone, Release, 5, 0, .503f),
        new(CenserBrace, 'A', Everyone, Windup, 2, CrimsonRewardRules.GrandBrace, .303f),
        new(CenserGrandPour, 'A', Everyone, Finale, 4, 0, 1.303f),
        new(QuillThrow, 'A', Shot, OneShot, 3, 2, .263f),
        new(QuillStick, 'A', Shot, OneShot, 3, 0, .243f),
        new(ScoreUnseal, 'A', Everyone, Windup, 2, CrimsonRewardRules.ScoreWindup, .403f),
        new(InkBlaze, 'A', Everyone, Release, 2, 6, .753f),
        new(ScoreChord, 'A', Everyone, Finale, 2, 0, 2.303f),
    };

    internal static ScarletCue Get(string name)
    {
        foreach (ScarletCue cue in All)
            if (cue.Name == name) return cue;
        throw new ArgumentOutOfRangeException(nameof(name), name, "not a Scarlet reward cue");
    }

    // A role's playback offset against the Raid's sound set, in dB.
    internal static float RoleDecibels(ScarletCueRole role) => role switch
    {
        ScarletCueRole.Build => BuildDecibels,
        ScarletCueRole.Shot => ShotDecibels,
        ScarletCueRole.Windup => WindupDecibels,
        ScarletCueRole.Release => ReleaseDecibels,
        ScarletCueRole.Finale => FinaleDecibels,
        ScarletCueRole.Show => ShowDecibels,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "not a Scarlet reward cue role"),
    };

    internal static float Decibels(float db) => MathF.Pow(10, db / 20);
}
