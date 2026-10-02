// Builds one physical phrase the way CrimsonRuntime.SchedulePhrase does, using the
// production authority (CrimsonChoreography.Create / CrimsonEnsemble.Technique / NoteEnd /
// CrimsonTechniqueGeometry.Stage / CrimsonGesturePlan.Validate). Only the Terraria-side
// bookkeeping (projectile slots, NPC poses, network identities) is replaced by constants.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Convergence.Common.Raids.Arena;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;

// A stand-in player: world position of the body centre and its velocity (px/tick).
internal readonly record struct PreviewPlayer(string Name, Vector2 Center, Vector2 Velocity);

internal sealed record PreviewPhrase(
    string Name, int Phase, int Serial, PreviewPlayer Player, int MusicStart,
    int[] BeatTicks, CrimsonGesturePlan[] Plans)
{
    // First warning and last residue of the phrase, absolute ticks.
    internal int FirstBorn => Array.ConvertAll(Plans, p => p.Born).Min();
    internal int LastEnd => Array.ConvertAll(Plans, p => p.End + ScarletOverlayEnd(p)).Max();
    private static int ScarletOverlayEnd(CrimsonGesturePlan p)
        => p.IsRift ? CrimsonSpatialCuts.ResidueTicks : CrimsonRhythm.ResidueTicks;
}

internal static class PreviewPlanner
{
    internal const int GroundX = 12800, GroundY = 6400; // field 11520..14080 x 5280..6400 (160 x 70 tiles)
    internal static readonly Guid Fight = new("5c4a1e00-0000-4000-8000-0000000000a1");
    internal static readonly Guid Connection = new("5c4a1e00-0000-4000-8000-0000000000c1");
    internal static RaidFieldGeometry Field => RaidFieldGeometry.FromGround(GroundX, GroundY);

    internal static CrimsonScore LoadScore(string root)
        => CrimsonScore.Read(File.ReadAllBytes(Path.Combine(root, "Assets/Music/CrimsonFoundry/Score.json")));

    // Constant tempo from musicStart: beat k = k * 3600 / bpm ticks (28.125 at 128 BPM).
    // CrimsonScore.BeatTicks is int[], so each beat is rounded to a whole tick (28/29 alternating).
    internal static CrimsonScore ConstantScore(double bpm)
    {
        double beat = 3600d / bpm;
        const int beats = 256; // 32 beats x 8 = 7200 ticks at 128 BPM, far longer than any phrase
        var ticks = new int[beats];
        var energy = new float[beats];
        for (int k = 0; k < beats; k++) { ticks[k] = (int)Math.Round(k * beat); energy[k] = 1f; }
        return new CrimsonScore
        {
            SampleRate = 48000, LoopStartSample = 0, LoopEndSample = (int)Math.Round(beats * beat * 800),
            IntroTicks = 900, BeatTicks = ticks, Energy = energy
        };
    }

    internal static PreviewPlayer[] Players()
    {
        var f = Field;
        // Players stand on the floor: body centre 21 px above the field bottom (a 20 x 42 hitbox).
        return new[]
        {
            new PreviewPlayer("center", new(f.CenterX + 160, f.Bottom - 21), Vector2.Zero),
            new PreviewPlayer("edge", new(f.Left + 150, f.Bottom - 21), Vector2.Zero)
        };
    }

    // phase: 0..2 = Acts I..III, 3 = Final. serial = the 1-based phrase serial CrimsonRuntime hands to the techniques.
    internal static PreviewPhrase Build(string name, CrimsonScore score, int phase, int serial,
        PreviewPlayer player, int scoreStart, int musicStart = 3000)
    {
        var field = Field;
        var rhythm = CrimsonChoreography.Create(score, scoreStart, serial - 1, phase == 3);
        int count = rhythm.Hits.Count + (phase == 3 ? CrimsonChoreography.BasicNotes : 0);
        var sources = new int[count];
        var counts = new int[4]; var steps = new int[4]; var first = new int[4]; var last = new int[4];
        Array.Fill(first, int.MaxValue);
        for (int i = 0; i < count; i++)
        {
            int source = phase < 3 ? phase : 3; // Final: ActiveSource only admits source 3
            sources[i] = source; counts[source]++;
            var note = rhythm.Hits[i % rhythm.Hits.Count];
            var technique = CrimsonEnsemble.Technique(phase, serial, i % rhythm.Hits.Count, i >= rhythm.Hits.Count);
            first[source] = Math.Min(first[source], musicStart + note.Fire);
            last[source] = Math.Max(last[source], musicStart + CrimsonEnsemble.NoteEnd(technique, note));
        }
        var staging = new CrimsonPoint[4];
        var focus = new CrimsonPoint(player.Center.X, player.Center.Y);
        for (int source = 0; source < 4; source++)
        {
            if (counts[source] == 0) continue;
            staging[source] = CrimsonTechniqueGeometry.Stage(field, focus, CrimsonTechnique.TrackingBeam, serial);
            if (phase == 3) staging[source] = source == 3 ? CrimsonChoreography.Conductor(field) : CrimsonEnsemble.Binding(field, source);
        }
        int epoch = musicStart;
        var plans = new CrimsonGesturePlan[count];
        for (int i = 0; i < count; i++)
        {
            int source = sources[i], note = i % rhythm.Hits.Count;
            var hit = rhythm.Hits[note];
            var technique = CrimsonEnsemble.Technique(phase, serial, note, i >= rhythm.Hits.Count);
            bool aimedIdentity = CrimsonGesturePlan.NeedsTargetIdentity(technique);
            // The EFFECTIVE aim: what the server locks at Born (CrimsonGesture.AI), not the
            // coarse clamp stored when the phrase is scheduled.
            var position = new CrimsonPoint(player.Center.X, player.Center.Y);
            var velocity = new CrimsonPoint(player.Velocity.X, player.Velocity.Y);
            CrimsonPoint aim = CrimsonTechniqueGeometry.Clamp(field, position, 100);
            if (technique is CrimsonTechnique.TrackingBeam or CrimsonTechnique.SpatialRift)
                aim = CrimsonChoreography.Predict(field, position, velocity);
            if (technique == CrimsonTechnique.ChoirRakes) aim = new(field.CenterX, field.CenterY);
            int end = CrimsonEnsemble.NoteEnd(technique, hit);
            int begin = Math.Max(epoch, musicStart + rhythm.Start - CrimsonRhythm.LookAheadTicks);
            plans[i] = new CrimsonGesturePlan(Fight, 0, epoch, serial, (byte)i, (byte)source,
                technique, (byte)steps[source]++, (byte)counts[source], hit.Accent,
                begin, musicStart + hit.Warning, musicStart + hit.Fire, musicStart + end,
                first[source], last[source], staging[source], staging[source], aim,
                GroundX, GroundY, CrimsonPlaytestTuning.AttackDamage,
                aimedIdentity ? (short)0 : (short)-1, aimedIdentity ? Connection : Guid.Empty);
            plans[i].Validate();
        }
        var beats = CrimsonRhythm.NextBeats(score, Math.Max(0, scoreStart - .499999d), 9);
        var absolute = new int[beats.Length];
        for (int i = 0; i < beats.Length; i++) absolute[i] = musicStart + (int)Math.Round(beats[i]);
        return new PreviewPhrase(name, phase, serial, player, musicStart, absolute, plans);
    }
}
