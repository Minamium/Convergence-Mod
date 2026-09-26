using System;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Musical composition only. The old four-beat pulse stays reusable and tested.
internal static class CrimsonChoreography
{
    internal const int OpeningTicks = 900, SummonAt = 460;
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
    internal static CrimsonRhythmPhrase Create(CrimsonScore score, int earliest, int serial, bool final)
    {
        if (earliest < 0 || serial < 0) throw new ArgumentOutOfRangeException();
        var beats = CrimsonRhythm.NextBeats(score, Math.Max(0, earliest - .499999d), 9);
        int At(int i) => (int)Math.Round(beats[i]);
        var hits = new CrimsonRhythmHit[BasicNotes + 1];
        for (int i = 0; i < BasicNotes; i++) {
            if (At(i+1)-At(i) is < CrimsonRhythm.MinimumWarningTicks or > CrimsonRhythm.MaximumWarningTicks)
                throw new InvalidOperationException("crimson.rhythm_unreadable_score");
            hits[i] = new(At(i), At(i + 1), At(i + 1) + CrimsonRhythm.LiveTicks, 1);
        }
        hits[BasicNotes] = new(At(4), At(6), At(8), 2);
        return new(At(0), At(8), CrimsonRhythmKind.Groove, Array.AsReadOnly(hits));
    }
    internal static CrimsonTechnique Technique(int phase, int phrase, int note)
        => note == BasicNotes ? CrimsonTechnique.SideBeams
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
