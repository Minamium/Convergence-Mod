using System;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Musical composition only: every phrase is two whole bars of the 128 BPM grid.
internal static class CrimsonChoreography
{
    // The intro's eight bars before Act I unlocks; Ember Crown emerges on bar four.
    internal static readonly int OpeningTicks = CrimsonMeter.BarTick(CrimsonMeter.OpeningBars);
    internal static readonly int SummonAt = CrimsonMeter.BarTick(CrimsonMeter.SummonBar);
    internal const float SideHalfWidth = 140, SealOffset = 470;
    internal const int BasicNotes = 4;
    internal static CrimsonPoint Conductor(RaidFieldGeometry field) => new(field.CenterX, field.CenterY);
    internal static CrimsonPoint SummoningGate(RaidFieldGeometry field) => new(field.CenterX, field.CenterY - 200);
    internal static float OpeningAge(float age, int musicStart)
        => musicStart < 0 ? -1 : age - musicStart + CrimsonInvocation.MusicLeadTicks;
    internal static float Reveal(float opening) => CrimsonInvocation.Ease((opening - 100) / 150);
    internal static float Backdrop(float opening) => CrimsonInvocation.Ease((opening - 300) / 180);
    internal static float Seal(float opening) => CrimsonInvocation.Ease((opening - 230) / 170)
        * (1 - CrimsonInvocation.Ease((opening - 720) / 180));
    // A phrase begins on the first bar head at or after earliest (a tick that rounds to
    // a bar head belongs to that bar), so music sections and phrases share boundaries.
    internal static CrimsonRhythmPhrase Create(int earliest, int serial, bool final)
    {
        if (earliest < 0 || serial < 0) throw new ArgumentOutOfRangeException();
        int first = CrimsonMeter.BarAtOrAfter(Math.Max(0, earliest - .499999d)) * CrimsonMeter.BeatsPerBar;
        int At(int i) => CrimsonMeter.BeatTick(first + i);
        var hits = new CrimsonRhythmHit[BasicNotes + 1];
        for (int i = 0; i < BasicNotes; i++)
            hits[i] = new(At(i), At(i + 1), At(i + 1) + CrimsonRhythm.LiveTicks, 1);
        hits[BasicNotes] = new(At(4), At(6), At(8), 2);
        return new(At(0), At(8), CrimsonRhythmKind.Groove, Array.AsReadOnly(hits));
    }
    // Every third phrase of Acts I-III (serial % 3 == 0) trades its four basic notes for the Act's
    // signature move; the closing crossflow is never replaced. Final has its own pairs.
    internal static CrimsonTechnique Technique(int phase, int phrase, int note)
        => note == BasicNotes ? CrimsonTechnique.SideBeams
            : CrimsonSignatureMoves.IsSignaturePhrase(phase, phrase) ? CrimsonSignatureMoves.ForAct(phase)
            : phase == 2 ? CrimsonTechnique.ChoirRakes
            : phase == 1 ? CrimsonTechnique.SpatialRift : CrimsonTechnique.TrackingBeam;
    // Same capped eighteen-tick velocity lead as Doll's eight pursuit prisms.
    // One authority observation at the warning; never drag a shown forecast.
    internal static CrimsonPoint Predict(RaidFieldGeometry field, CrimsonPoint position, CrimsonPoint velocity)
        => CrimsonTechniqueGeometry.Clamp(field, position + new CrimsonPoint(
            Math.Clamp(velocity.X * 18, -220, 220), Math.Clamp(velocity.Y * 18, -160, 160)), 100);
    internal static CrimsonPoint Direction(int phrase, int note)
        => ((phrase + note) & 3) switch {
            0 => new(0, 1), 1 => new(1, 0), 2 => new(.70710678f, .70710678f), _ => new(-.70710678f, .70710678f) };
    internal static CrimsonStroke Side(in CrimsonGesturePlan p, float age, bool forecast)
    {
        float center = Math.Clamp(p.Target.X, p.Field.Left + SealOffset + 110, p.Field.Right - SealOffset - 110);
        float y = Math.Clamp(p.Target.Y, p.Field.Top + SideHalfWidth + 20, p.Field.Bottom - SideHalfWidth - 20);
        float width = forecast ? 1 : CrimsonInvocation.Ease((age - p.Fire) / 10)
            * (1 - CrimsonInvocation.Ease((age - (p.End - 15)) / 15));
        float reach = forecast ? 1 : CrimsonInvocation.Ease((age - p.Fire) / 7);
        var a = new CrimsonPoint(center + SealOffset, y);
        return new(a, new(a.X - SealOffset * 2 * reach, y), SideHalfWidth * width);
    }
    internal static (float X, float Y) ClampParticipant(RaidFieldGeometry field, float x, float y, int width, int height)
        // The floor is the support tile top, not an invisible floor two pixels above it.
        => field.ClampBody(x, y, width, height, 0);
}
