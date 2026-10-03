using System;
using System.Linq;
using Convergence.Client.Encounters.CrimsonFoundry.Rewards;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;

namespace Convergence.DomainTests;

// The Scarlet reward cue table (ScarletRewardCues) against the rules' own moments: every timed cue lands where
// REWARDS.md puts it, voices cover the designed overlaps, and the audiences follow the multiplayer rules.
internal static partial class Program
{
    private static ScarletCue RewardCue(string name) => ScarletRewardCues.Get(name);

    [DomainTest("Scarlet reward cues: 37 cues in 40 files, the owner's picks, one gain and one level offset per role")]
    private static void ScarletRewardCueTable()
    {
        var cues = ScarletRewardCues.All;
        AssertEqual(37, cues.Length, "the spec's 37 cues");
        AssertEqual(37, cues.Select(c => c.Name).Distinct(StringComparer.Ordinal).Count(), "unique names");
        AssertEqual(40, cues.Sum(c => c.Files), "OrganShot ships one file per pipe");
        AssertEqual(CanticleRules.Pipes, RewardCue(ScarletRewardCues.OrganShot).Files, "one OrganShot per pipe");
        AssertEqual("OrganShot1", RewardCue(ScarletRewardCues.OrganShot).File(0), "pipe 0");
        AssertEqual("OrganShot4", RewardCue(ScarletRewardCues.OrganShot).File(3), "pipe 3");
        AssertEqual("OrganShot4", RewardCue(ScarletRewardCues.OrganShot).File(9), "a pipe out of range clamps");
        // The owner's 2026-10-03 choices: B for these six blocks, A for every other (the whole toll ladder is one block);
        // on 2026-10-04 he chose B for the two crescent cues.
        string[] b = { "Cadence", "ReliquaryOpen", "ScytheWhip", "StaffWindup", "StaffBarline", "RiverRelease", "ScytheVolley", "CrescentBreak" };
        foreach (var cue in cues) AssertEqual(b.Contains(cue.Name) ? 'B' : 'A', cue.Take, $"{cue.Name} take");
        for (int k = 0; k < ScarletRewardCues.Tolls; k++) AssertEqual($"Toll{k}", ScarletRewardCues.Toll(k), $"toll {k}");
        AssertEqual("Toll0", ScarletRewardCues.Toll(-3), "low toll clamps");
        AssertEqual("Toll7", ScarletRewardCues.Toll(12), "high toll clamps");
        AssertThrows<ArgumentOutOfRangeException>(() => ScarletRewardCues.Get("CrownRupture"), "no Raid cue in the table");
        AssertEqual(1f, ScarletRewardCues.Gain, "files carry the designed levels");
        // Levels against the Raid (REWARDS.md#art-and-audio): each cue plays at its role's offset, the same for every cue
        // of the role, and an offset only ever lowers the owner's picked file.
        var roles = new System.Collections.Generic.Dictionary<string, ScarletCueRole>(StringComparer.Ordinal) { ["ReliquaryOpen"] = ScarletCueRole.Show };
        foreach (string name in new[] { "ScytheSwingHigh", "ScytheSwingLow", "ScytheWhip", "ScytheVolley", "CrescentBreak", "OrganShot", "BatonStroke", "CenserSummon",
            "CenserSwing", "QuillThrow", "QuillStick" })
            roles[name] = ScarletCueRole.Shot;
        foreach (string name in new[] { "ScytheWhipBrace", "StaffWindup", "HymnInhale", "BatonLift", "CenserBrace", "ScoreUnseal" })
            roles[name] = ScarletCueRole.Windup;
        foreach (string name in new[] { "StaffCut", "HandSlam", "InkIgnite", "CenserPour", "InkBlaze" })
            roles[name] = ScarletCueRole.Release;
        foreach (string name in new[] { "StaffBarline", "ChoirClasp", "RiverRelease", "CenserGrandPour", "ScoreChord", "Cadence" })
            roles[name] = ScarletCueRole.Finale;
        for (int k = 0; k < ScarletRewardCues.Tolls; k++) roles[ScarletRewardCues.Toll(k)] = ScarletCueRole.Build;
        AssertEqual(cues.Length, roles.Count, "every cue has a role");
        foreach (var cue in cues)
        {
            AssertEqual(roles[cue.Name], cue.Role, $"{cue.Name} role");
            AssertEqual(true, ScarletRewardCues.RoleDecibels(cue.Role) <= 0, $"{cue.Name} offset only lowers the file");
        }
        // The cues that sound in play share one offset, so their auditioned balance is kept (REWARDS.md#levels-against-the-raid).
        foreach (var role in new[] { ScarletCueRole.Build, ScarletCueRole.Windup, ScarletCueRole.Release })
            AssertEqual(ScarletRewardCues.ShotDecibels, ScarletRewardCues.RoleDecibels(role), $"{role} plays at the in-play offset");
        AssertEqual(ScarletRewardCues.InPlayDecibels, ScarletRewardCues.ShotDecibels, "one-shots play at the in-play offset");
        AssertNear(.398f, ScarletRewardCues.Decibels(CrimsonRewardRules.RemoteCueDecibels), .001f, "other players' cues 8 dB lower");
        foreach (var cue in cues)
        {
            AssertEqual(true, cue.Voices >= 1 && cue.Voices <= 8, $"{cue.Name} voices are bounded");
            AssertEqual(true, cue.Seconds > .2f && cue.Seconds < 2.5f, $"{cue.Name} length");
            AssertEqual(true, cue.Lead >= 0 && cue.Lead < cue.Ticks, $"{cue.Name} lead inside the file");
        }
    }

    [DomainTest("Scarlet reward cues: tolls are the owner's, swings and shots are per-shot, the rest is heard by everyone")]
    private static void ScarletRewardCueAudiences()
    {
        string[] shots = { "ScytheSwingHigh", "ScytheSwingLow", "ScytheWhipBrace", "ScytheWhip", "ScytheVolley", "CrescentBreak", "OrganShot", "BatonStroke", "CenserSummon",
            "QuillThrow", "QuillStick" };
        foreach (var cue in ScarletRewardCues.All)
        {
            var expected = cue.Name.StartsWith("Toll", StringComparison.Ordinal) ? ScarletCueAudience.Owner
                : shots.Contains(cue.Name) ? ScarletCueAudience.Shot : ScarletCueAudience.Everyone;
            AssertEqual(expected, cue.Audience, $"{cue.Name} audience");
            // Other players' voices are a pool of their own: one voice of a per-shot file (replace oldest), one owner's
            // voices of any other file (ignore new), so another player's cue never cuts the local player's.
            bool shot = expected == ScarletCueAudience.Shot;
            AssertEqual(shot ? 1 : cue.Voices, cue.PeerVoices, $"{cue.Name} other players' voices");
            AssertEqual(shot, cue.PeerReplacesOldest, $"{cue.Name} other players' limit");
        }
    }

    [DomainTest("Scarlet reward cues land on the spec's moments")]
    private static void ScarletRewardCueMoments()
    {
        // Sable Scythe: the breath peaks on the first live tick; the Whip's brace peaks as the draw-back ends, before the
        // lash; the lash peaks inside its live window; Staff Reap's windup peaks on the forward whip at tick 16; a line's
        // cut peaks while its head crosses; the barline while it falls.
        AssertEqual(CrimsonRewardRules.StrokeLiveStart, ScarletRewardCues.ScytheSwingAt + RewardCue("ScytheSwingHigh").Lead, "Over breath");
        AssertEqual(CrimsonRewardRules.StrokeLiveStart, ScarletRewardCues.ScytheSwingAt + RewardCue("ScytheSwingLow").Lead, "Under breath");
        int brace = ScarletRewardCues.ScytheWhipBraceAt + RewardCue("ScytheWhipBrace").Lead;
        AssertEqual(true, brace >= CrimsonRewardRules.WhipDrawEnd && brace < CrimsonRewardRules.WhipLiveStart, $"brace peaks before the lash ({brace})");
        AssertEqual(CrimsonRewardRules.WhipDrawStart, ScarletRewardCues.ScytheWhipBraceAt, "brace starts with the draw-back");
        int lash = ScarletRewardCues.ScytheWhipAt + RewardCue("ScytheWhip").Lead;
        AssertEqual(true, lash > CrimsonRewardRules.WhipLiveStart && lash <= CrimsonRewardRules.WhipLiveEnd, $"lash inside the live window ({lash})");
        // The volley's tear is built to its head (Lead 0): it fires as the lash arc sheds its five crescents, after the lash's
        // own cue has peaked; a crescent's break is built to its head too.
        AssertEqual(CrimsonRewardRules.VolleyAge, ScarletRewardCues.ScytheVolleyAt + RewardCue("ScytheVolley").Lead, "the volley's tear as the lash arc sheds it");
        AssertEqual(true, ScarletRewardCues.ScytheVolleyAt > lash, "the volley follows the lash's peak");
        AssertEqual(0, RewardCue("CrescentBreak").Lead, "a break sits at the head of its file");
        AssertEqual(CrimsonRewardRules.StaffWindup, RewardCue("StaffWindup").Lead, "Staff Reap windup");
        AssertEqual(true, RewardCue("StaffCut").Lead <= CrimsonRewardRules.StaffHeadTicks, "a line cuts while its head crosses");
        AssertEqual(true, RewardCue("StaffBarline").Lead <= CrimsonRewardRules.BarlineFall, "the barline strikes as it falls");
        // Canticle Organ: the inhale peaks on the first slam; a slam peaks while its hand is live.
        AssertEqual(CrimsonRewardRules.HymnInhale, RewardCue("HymnInhale").Lead, "Hymn inhale");
        AssertEqual(CrimsonRewardRules.HandFirstSlam, RewardCue("HymnInhale").Lead, "the first hand slams on the inhale's peak");
        AssertEqual(true, RewardCue("HandSlam").Lead <= CrimsonRewardRules.HandLive, "slam");
        // Scarlet Baton: the swish peaks while the pen writes; the lift on the downbeat; an ignition as it swells full;
        // the river's surge reaches the cadence while its head still runs.
        int swish = ScarletRewardCues.BatonStrokeAt + RewardCue("BatonStroke").Lead;
        AssertEqual(true, swish > BatonRules.WriteStart && swish < BatonRules.WriteEnd, $"swish while writing ({swish})");
        AssertEqual(CrimsonRewardRules.TuttiLift, RewardCue("BatonLift").Lead, "Tutti downbeat");
        AssertEqual(CrimsonRewardRules.IgniteSwell, RewardCue("InkIgnite").Lead, "ignition swells");
        AssertEqual(true, RewardCue("RiverRelease").Lead < CrimsonRewardRules.RiverHeadTicks, "river surge");
        // Ember Censer: the swing breath peaks on the pour; the brace on the Grand Pour.
        AssertEqual(CrimsonRewardRules.SwingCueLead, RewardCue("CenserSwing").Lead, "censer swing");
        AssertEqual(CrimsonRewardRules.GrandBrace, RewardCue("CenserBrace").Lead, "censer brace");
        // Bloodink Quill: the unseal peaks as the ink catches (U); the blaze while the ink burns.
        AssertEqual(CrimsonRewardRules.ScoreWindup, RewardCue("ScoreUnseal").Lead, "score unseal");
        AssertEqual(true, RewardCue("InkBlaze").Lead < CrimsonRewardRules.BurnLive, "ink blaze");
        // The reliquary's show: the cadence as the ink lines ignite at tick 20.
        AssertEqual(CrimsonRewardRules.ShowIgnite, RewardCue("ReliquaryOpen").Lead, "reliquary cadence");
    }

    [DomainTest("Scarlet crescent cues: the volley and the break are shipped per-swing one-shots with files of their own")]
    private static void ScarletRewardCrescentCues()
    {
        var volley = RewardCue(ScarletRewardCues.ScytheVolley);
        var broke = RewardCue(ScarletRewardCues.CrescentBreak);
        foreach (var cue in new[] { volley, broke })
        {
            AssertEqual(true, ScarletRewardCues.All.Contains(cue), $"{cue.Name} is one of the 37 shipped");
            AssertEqual('B', cue.Take, $"{cue.Name}: the owner chose take B on 2026-10-04");
            AssertEqual(ScarletCueAudience.Shot, cue.Audience, $"{cue.Name} is a per-swing cue");
            AssertEqual(ScarletCueRole.Shot, cue.Role, $"{cue.Name} is a one-shot");
            AssertEqual(ScarletRewardCues.InPlayDecibels, ScarletRewardCues.RoleDecibels(cue.Role), $"{cue.Name} plays at the in-play offset");
            AssertEqual(1, cue.PeerVoices, $"{cue.Name}: other players share one voice");
            AssertEqual(true, cue.PeerReplacesOldest, $"{cue.Name}: another player's cue replaces their own oldest");
            AssertEqual(0, cue.Lead, $"{cue.Name} sits at the head of its file");
            AssertEqual(1, cue.Files, $"{cue.Name} is one file");
            AssertEqual(cue.Name, cue.File(), $"{cue.Name} plays its own file, never StaffCut's");
        }
        AssertEqual(2, volley.Voices, "ScytheVolley: 2 voices");
        AssertEqual(3, broke.Voices, "CrescentBreak: 3 voices");
        AssertNear(.503f, volley.Seconds, .0005f, "ScytheVolley: 0.503 s");
        AssertNear(.303f, broke.Seconds, .0005f, "CrescentBreak: 0.303 s");
        AssertEqual(31, volley.Ticks, "ScytheVolley ticks");
        AssertEqual(19, broke.Ticks, "CrescentBreak ticks");
        AssertEqual(6, ScarletRewardCues.CrescentBreakSpacing, "a break rings at most once per 6 ticks per owner");
        // Three voices at that spacing ring through all of the file but its last tick, so a held loop never cuts a ringing break early.
        AssertEqual(true, broke.Voices * ScarletRewardCues.CrescentBreakSpacing >= broke.Ticks - 1, "the break's voices cover its file at the throttle's rate");
        AssertEqual(volley, RewardCue("ScytheVolley"), "found by name");
        AssertEqual(broke, RewardCue("CrescentBreak"), "found by name");
        // A voice pool is its file's name, so no two cues share one: the crescent cues never take a Staff Reap cut's voices.
        var files = ScarletRewardCues.All.SelectMany(c => Enumerable.Range(0, c.Files).Select(i => c.File(i))).ToArray();
        AssertEqual(40, files.Distinct(StringComparer.Ordinal).Count(), "every file belongs to one cue");
    }

    [DomainTest("Scarlet reward cue voices cover each cascade, the censer budget and a melody of one toll")]
    private static void ScarletRewardCueVoices()
    {
        static int Overlap(ScarletCue cue, int interval) => (cue.Ticks + interval - 1) / interval;
        // Cascade parts fire S(1) = 7 ticks apart: every part still rings when the next starts.
        foreach (string name in new[] { "StaffCut", "HandSlam", "InkIgnite" })
            AssertEqual(true, RewardCue(name).Voices >= Overlap(RewardCue(name), CrimsonRewardRules.S(1)), $"{name} voices");
        // One owner's censers sound at most one pour per 7 ticks and one Grand Pour per 20.
        AssertEqual(true, RewardCue("CenserPour").Voices >= Overlap(RewardCue("CenserPour"), CrimsonRewardRules.PourCueInterval), "pour voices");
        AssertEqual(true, RewardCue("CenserGrandPour").Voices >= Overlap(RewardCue("CenserGrandPour"), CrimsonRewardRules.GrandCueInterval), "grand voices");
        AssertEqual(true, RewardCue("CenserSwing").Voices >= Overlap(RewardCue("CenserSwing"), CrimsonRewardRules.SwingCueInterval), "swing voices");
        // Eight quills at one height play one toll eight times, a sixteenth apart.
        for (int k = 0; k < ScarletRewardCues.Tolls; k++)
            AssertEqual(true, RewardCue(ScarletRewardCues.Toll(k)).Voices >= CrimsonRewardRules.MaxQuills, $"Toll{k} voices");
        AssertEqual(true, RewardCue("ReliquaryOpen").Voices >= CrimsonRewardRules.ShowMax, "one voice per reliquary show");
    }
}
