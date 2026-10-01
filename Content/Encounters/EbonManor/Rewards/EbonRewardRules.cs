#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.EbonManor.Rewards;

public enum EbonRewardKind { Melee, Ranged, Magic, Summon, Rogue }

// Pure tuning and geometry for the Ebon Manor reward set (docs/encounters/ebon-manor/REWARDS.md).
// Nominal raw, zero-defense budgets, not measured DPS. No Terraria/XNA references: the domain
// tests link this file.
internal static class EbonRewardRules
{
    // AutoMatador's tempo drives every weapon's build-up so the set feels musical.
    internal const float BeatTicks = 28.8f, SixteenthTicks = 7.2f;
    internal const int NoteCount = 8; // Note0..Note7: B3 D4 F#4 B4 D5 F#5 B5 D6

    internal static int Damage(EbonRewardKind kind) => kind switch
    {
        EbonRewardKind.Melee => 3600,
        EbonRewardKind.Ranged => 1900,
        EbonRewardKind.Magic => 2400,
        EbonRewardKind.Summon => 1000,
        EbonRewardKind.Rogue => 1800,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
    internal static int UseTicks(EbonRewardKind kind) => kind switch
    {
        EbonRewardKind.Melee => 18,
        EbonRewardKind.Ranged => 16,
        EbonRewardKind.Magic => 20,
        EbonRewardKind.Summon => 30,
        EbonRewardKind.Rogue => 18,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
    internal static int Scaled(int damage, float factor)
        => (int)Math.Clamp(Math.Round((double)Math.Max(0, damage) * factor), 1, int.MaxValue / 4);

    // Beat clock shared by the chandeliers and the companion. Fractional beats since tick 0.
    internal static double Beats(double tick) => tick / BeatTicks;
    internal static int BeatIndex(double tick) => (int)Math.Floor(Beats(tick));
    internal static double NextBeatTick(double tick) => Math.Ceiling(tick / BeatTicks - 1e-9) * BeatTicks;
    // Arpeggio step for the n-th building hit, wrapping on the 8 tuned notes.
    internal static int Note(int step) => ((step % NoteCount) + NoteCount) % NoteCount;

    // --- Melee: Moonshear ---------------------------------------------------------------
    internal const float ShearReach = 210, ShearWidth = 26, SnipMultiplier = 2.2f;
    internal const int ComboResetTicks = 80, StrokeImmunity = 24;
    internal const int MaxMarks = 5, MarkLife = 360;
    internal const float CutRange = 900, CutWidth = 64, CutMultiplier = 1.5f, PopMultiplier = .8f;
    internal const int CutForecast = 18, CutTravel = 12, CutLive = 10, PopSpacing = 4, CutCooldown = 90;

    // --- Ranged: Moonloom Harp ----------------------------------------------------------
    internal const int MaxStrings = 8, StringLife = 480, StringFade = 60, PluckLive = 10;
    internal const float ArrowRange = 1600, PluckWidth = 20, PluckMultiplier = 3f, ArrowSpeed = 22;
    internal static int PluckTick(int index) => (int)MathF.Round(index * SixteenthTicks);

    // --- Magic: Ebon Thimble ------------------------------------------------------------
    internal const int Pieces = 8, FirstLift = 6, ManaPerPiece = 12;
    internal const float YankSpeed = 10, YankAcceleration = 1.2f, YankMax = 40, PieceMultiplier = 2.5f;
    internal const float PianoHeight = 520, PianoSpeed = 6, PianoGravity = .8f, PianoRadius = 220, PianoMultiplier = 20;
    internal const int PianoDelaySixteenths = 2;
    internal static int LiftTick(int piece) => piece <= 0 ? FirstLift : FirstLift + (int)MathF.Round(piece * BeatTicks);
    internal static int YankTick(int piece) => (int)MathF.Round(piece * SixteenthTicks);
    internal static int PianoTick(int pieces) => (int)MathF.Round((pieces - 1 + PianoDelaySixteenths) * SixteenthTicks);

    // --- Summon: Ballroom Chandelier ----------------------------------------------------
    internal const float HoverHeight = 260, DropSpeed = 2, DropGravity = 1.1f, DropMax = 30, ShatterRadius = 110, ShatterMultiplier = 4.5f;
    internal const int SettleTicks = 14, ReweaveTicks = 24, CycleBeats = 4;
    // Chandelier i owns beat (i mod 4) of each 4-beat cycle; the next drop tick at or after `tick`.
    internal static double DropTick(int index, double tick)
    {
        double cycle = CycleBeats * BeatTicks, offset = (((index % CycleBeats) + CycleBeats) % CycleBeats) * BeatTicks;
        double k = Math.Ceiling((tick - offset) / cycle - 1e-9);
        return offset + k * cycle;
    }

    // --- Rogue: Severing Silk -----------------------------------------------------------
    internal const int SpoolLife = 70, MaxStrands = 10, StrandLife = 360, TightenTicks = 6, ScissorsLife = 30;
    internal const float SpoolSpeed = 18, SpoolGravity = .12f, StrandWidth = 24, SeverMultiplier = 4, ScissorsMultiplier = 2, ScissorsRadius = 80, ScissorsSpeed = 24;

    // --- Companion: The Last Waltz ------------------------------------------------------
    internal const int CompanionSlots = 10, CompanionDamageScale = 10, ScoreBeats = 8, Spokes = 6, SpokeImmunity = 8;
    internal const float FlingMultiplier = .6f, SpokeMultiplier = .6f;
    internal static int ScoreTicks => (int)MathF.Round(ScoreBeats * BeatTicks); // 230

    // --- Geometry ---------------------------------------------------------------------
    internal static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a; float len = ab.LengthSquared();
        float t = len < 1e-6f ? 0 : Math.Clamp(Vector2.Dot(p - a, ab) / len, 0, 1);
        return Vector2.Distance(p, a + ab * t);
    }
    // Does an axis-aligned box (min, max) touch a segment thickened to `width`?
    internal static bool BoxTouchesSegment(Vector2 min, Vector2 max, Vector2 a, Vector2 b, float width)
    {
        Vector2 center = (min + max) * .5f, half = (max - min) * .5f;
        float reach = width * .5f;
        // Sample along the segment: exact enough for 2 px collision and cheap.
        float length = Vector2.Distance(a, b);
        int steps = Math.Clamp((int)(length / 8f) + 1, 1, 256);
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
            Vector2 d = Vector2.Abs(p - center) - half;
            Vector2 outside = Vector2.Max(d, Vector2.Zero);
            if (outside.Length() <= reach) return true;
        }
        return false;
    }
    internal static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 point)
    {
        point = default;
        Vector2 r = b - a, s = d - c;
        float denominator = r.X * s.Y - r.Y * s.X;
        if (MathF.Abs(denominator) < 1e-6f) return false;
        Vector2 q = c - a;
        float t = (q.X * s.Y - q.Y * s.X) / denominator, u = (q.X * r.Y - q.Y * r.X) / denominator;
        if (t < 0 || t > 1 || u < 0 || u > 1) return false;
        point = a + r * t;
        return true;
    }
    internal static float Smooth(float x)
    {
        x = Math.Clamp(x, 0, 1);
        return x * x * x * (10 + x * (-15 + 6 * x));
    }
}
