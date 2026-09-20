using System;
using Convergence.Common.Raids.Arena;

namespace Convergence.Content.Encounters.CrimsonFoundry;

// Musical composition only. The old four-beat pulse stays reusable and tested.
internal static class CrimsonChoreography
{
    internal const int OpeningTicks = 900, SummonAt = 460;
    internal const float SideHalfWidth = 140, SafeHalfWidth = 96;
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
        var basic = CrimsonRhythm.Create(score, earliest, serial, final);
        var beats = CrimsonRhythm.NextBeats(score, Math.Max(0, earliest - .499999d), 9);
        int At(int i) => (int)Math.Round(beats[i]);
        return new(basic.Start, At(8), CrimsonRhythmKind.Groove, Array.AsReadOnly(new[] {
            basic.Hits[0], basic.Hits[1], new CrimsonRhythmHit(At(4), At(6), At(8), 2) }));
    }
    internal static CrimsonTechnique Technique(int phase, int phrase, int note)
        => note == 2 ? CrimsonTechnique.SideBeams
            : phase == 1 && phrase % 2 == 0 ? CrimsonTechnique.SpatialRift : CrimsonTechnique.TrackingBeam;
    // Same capped eighteen-tick velocity lead as Doll's eight pursuit prisms.
    // One authority observation at the warning; never drag a shown forecast.
    internal static CrimsonPoint Predict(RaidFieldGeometry field, CrimsonPoint position, CrimsonPoint velocity)
        => CrimsonTechniqueGeometry.Clamp(field, position + new CrimsonPoint(
            Math.Clamp(velocity.X * 18, -220, 220), Math.Clamp(velocity.Y * 18, -160, 160)), 100);
    internal static CrimsonPoint Direction(int phrase, int note)
        => ((phrase + note) & 3) switch {
            0 => new(0, 1), 1 => new(1, 0), 2 => new(.70710678f, .70710678f), _ => new(-.70710678f, .70710678f) };
    internal static CrimsonStroke Side(in CrimsonGesturePlan p, float age, bool forecast, int side, bool upper = false)
    {
        float offset = SafeHalfWidth + SideHalfWidth;
        float center = Math.Clamp(p.Target.X, p.Field.Left + offset + SideHalfWidth + 16,
            p.Field.Right - offset - SideHalfWidth - 16);
        float width = forecast ? 1 : CrimsonInvocation.Ease((age - p.Fire) / 10)
            * (1 - CrimsonInvocation.Ease((age - (p.End - 15)) / 15));
        float reach = forecast ? 1 : CrimsonInvocation.Ease((age - p.Fire) / 7);
        var a = new CrimsonPoint(center + side * offset, p.Target.Y);
        float end = upper ? p.Field.Top : p.Field.Bottom;
        return new(a, new(a.X, a.Y + (end - a.Y) * reach), SideHalfWidth * width);
    }
    internal static (float X, float Y) ClampParticipant(RaidFieldGeometry field, float x, float y, int width, int height)
        // The floor is the support tile top, not an invisible floor two pixels above it.
        => field.ClampBody(x, y, width, height, 0);
}
