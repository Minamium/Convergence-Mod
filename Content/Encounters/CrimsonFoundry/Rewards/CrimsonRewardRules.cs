#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

public enum CrimsonRewardKind { Melee, Ranged, Magic, Summon, Rogue }

// Pure tuning and geometry for the Scarlet Invocation reward set (docs/encounters/crimson-foundry/REWARDS.md).
// Every number in that document lives here, grouped by section. "An N px disc" there is a disc of radius N
// (the Clasp's 64 px disc meets its splash crown, which starts 64 px out). Nominal raw, zero-defense starting values, not
// measured DPS. No Terraria/XNA references: the domain tests and the offline preview link this file. It shares no
// code with the Ebon reward rules (the two sets stay independent).
internal static class CrimsonRewardRules
{
    // ---- Timing: Graceful Ordeal's sixteenths, counted from the release itself -------------------------------
    // S(k) = round(k x 225/32) in integers. Only a release cascade uses it; nothing reads a music or world clock.
    internal static int S(int k) => k >= 0 ? (225 * k + 16) / 32 : throw new ArgumentOutOfRangeException(nameof(k));
    internal static int Beat => S(4); // 28

    // ---- Power -------------------------------------------------------------------------------------------------
    internal const int SellGold = 40, Knockback = 6, Crit = 8, SummonCrit = 0;
    internal static int Damage(CrimsonRewardKind kind) => kind switch
    {
        CrimsonRewardKind.Melee => 3600,
        CrimsonRewardKind.Ranged => 1900,
        CrimsonRewardKind.Magic => 2400,
        CrimsonRewardKind.Summon => 1000,
        CrimsonRewardKind.Rogue => 1800,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
    // useTime = useAnimation. The scythe's strokes own their own clocks (2); the hymn and the tutti set their own.
    internal static int UseTicks(CrimsonRewardKind kind) => kind switch
    {
        CrimsonRewardKind.Melee => 2,
        CrimsonRewardKind.Ranged => 14,
        CrimsonRewardKind.Magic => 18,
        CrimsonRewardKind.Summon => 30,
        CrimsonRewardKind.Rogue => 18,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
    internal static int CritFor(CrimsonRewardKind kind) => kind == CrimsonRewardKind.Summon ? SummonCrit : Crit;
    // Secondary hits always derive from the live weapon damage, at least 1.
    internal static int Scaled(int damage, float factor)
        => (int)Math.Clamp(Math.Round((double)Math.Max(0, damage) * factor), 1, int.MaxValue / 4);

    // ---- Shared projectile contract ----------------------------------------------------------------------------
    internal const int HeldLagTicks = 6;       // a remote held projectile tolerates this much held-item lag
    internal const int ReliquaryShareChance = 20; // percent per weapon, independent of Luck
    internal const int RootLedgerCapacity = 64; // distinct roots one release part may hit
    internal static bool Valid(float value, float min, float max) => float.IsFinite(value) && value >= min && value <= max;
    internal static bool ValidInteger(float value, int min, int max) => Valid(value, min, max) && value == MathF.Floor(value);

    // ---- Black-blood material ----------------------------------------------------------------------------------
    // A live point opens over 3 ticks (no overshoot): draw radius = collide radius = radius x InkOpen(t).
    internal const int InkOpenTicks = 3, ResidueMin = 20, ResidueMax = 24;
    internal const float DormantFade = .6f;
    internal static float InkOpen(float ticksSinceIgnition) => Math.Clamp(ticksSinceIgnition / InkOpenTicks, 0, 1);

    // ---- Multiplayer readability -------------------------------------------------------------------------------
    internal const float LocalOpacity = 1, RemoteDormantOpacity = .5f, RemoteLiveOpacity = .85f;
    internal const float RemoteShotDecibels = -8; // per-swing/per-shot cues for other players, one voice
    internal static float Decibels(float db) => MathF.Pow(10, db / 20);

    // ---- Effect bounds (per frame, all owners together) --------------------------------------------------------
    internal const int MaxInkPaths = 256, MaxStripVertices = 8192, SampleSpacingMin = 8, SampleSpacingMax = 12;
    internal const int MaxDropletsPerOwner = 48, MaxDroplets = 160, MaxParticlesPerOwner = 200, MaxParticles = 600;
    internal static int ReducedCount(int count, bool reduced) => reduced ? count / 2 : count;

    // ---- Scarlet Score Reliquary -------------------------------------------------------------------------------
    internal const int DropSpacing = 36, DropLift = 180, DropSize = 32;
    // One reliquary per frozen member, centred on the pedestal ground, DropSpacing apart, DropLift up.
    internal static int DropLeft(int groundX, int index, int count) => groundX + index * DropSpacing - count * DropSpacing / 2;
    internal static int DropTop(int groundY) => groundY - DropLift;
    internal const int ShowLife = 60, ShowMax = 4, ShowSettle = 4, ShowRelease = 10, ShowIgnite = 20, ShowBurnStart = 44;
    internal const float ShowHover = 70, ShowLidDegrees = 110, ShowLidTicks = 8, ShowInkRise = 120;
    internal const int ShowInkLines = 5, ShowSealCracks = 3;

    // ---- Melee: Sable Scythe -----------------------------------------------------------------------------------
    internal const int StrokeTicks = 18, StrokeLiveStart = 5, StrokeLiveEnd = 13, WhipTicks = 28;
    internal const int WhipDrawStart = 6, WhipDrawEnd = 12, WhipLiveStart = 14, WhipLiveEnd = 20, CrescentLiveEnd = 26;
    internal const float StrokeMultiplier = 1, WhipMultiplier = 1.8f, CrescentMultiplier = .6f, CrescentRadius = 16, WhipThrust = 24;
    internal const int CrescentScar = 20, ComboIdleReset = 60, MeasureStrokes = 5;
    internal const float MinTipSpeed = 3, MinTurnRadius = 24, RollSpeedFloor = .25f, DrawBackSpeedFloor = .2f, MaxAngularAcceleration = .3f;
    internal const float ScytheReach = 136, BladeWidth = 26; internal const int BladeCapsules = 3, SweepSubsamples = 9, StrokeImmunity = 24;
    internal const int StaffLines = 5, StaffLineLife = 360, StaffDrainTicks = 30;
    internal const float StaffBehind = 40, StaffLineLength = 56, StaffLineGap = 8;
    internal const int StaffInputGrace = 8, StaffWindup = 16, StaffHeadTicks = 4, StaffLineLive = 10, StaffScar = 20;
    internal const float StaffCursorRange = 720, StaffSnap = 96, StaffSpacingMin = 14, StaffSpacingMax = 36;
    internal const float StaffHalfLength = 420, StaffRadius = 12, StaffLineMultiplier = .8f;
    internal const float BarlineGap = 28, BarlineExtra = 48, BarlineRadius = 16, BarlineMultiplier = 2, BarlineShake = 3;
    internal const int BarlineFall = 3, BarlineLive = 12, BarlineHold = 56, PartialRelease = 8, MaxStaffCuts = 6, StaffCutLife = 90;
    internal static int StaffLineTick(int k) => StaffWindup + S(k);       // the k-th line to fire (0 = the middle)
    internal static int BarlineTick => StaffWindup + S(StaffLines);       // 51
    internal static float StaffSpacing(float hitboxHeight) => Math.Clamp(hitboxHeight / 4, StaffSpacingMin, StaffSpacingMax);
    // Middle first, then outward: line order k -> staff row offset (-2..2), alternating above and below.
    internal static int StaffRow(int k) => k switch { 0 => 0, 1 => -1, 2 => 1, 3 => -2, 4 => 2, _ => throw new ArgumentOutOfRangeException(nameof(k)) };
    internal static float MeasureMultiplier => 2 * StrokeMultiplier * 2 + WhipMultiplier + CrescentMultiplier; // 6.4
    internal static int MeasureTicks => 4 * StrokeTicks + WhipTicks;                                           // 100
    // A release of n lines: the multiplier and its busy ticks (full: until the follow-through ends at 56).
    internal static float StaffReleaseMultiplier(int lines)
        => Math.Clamp(lines, 0, StaffLines) * StaffLineMultiplier + (lines >= StaffLines ? BarlineMultiplier : 0);
    internal static int StaffReleaseTicks(int lines)
        => lines <= 0 ? 0 : lines >= StaffLines ? BarlineHold : StaffLineTick(lines - 1) + PartialRelease;

    // ---- Ranged: Canticle Organ --------------------------------------------------------------------------------
    internal const float ShardSpeed = 20, ShardMultiplier = 1; internal const int ShardExtraUpdates = 1, ShardLife = 40, MaxShardsInFlight = 3;
    internal const int MaxMarksPerNpc = 8, MaxMarkedNpcs = 8, MarkLife = 360;
    internal const int HymnUseTicks = 24, HymnInhale = 10, MaxHands = 16, HandFirstSlam = 10, HandForm = 2, HandLead = 8, HandFall = 6, HandLive = 3;
    internal const float HymnRange = 2000, HandDrop = 240, HandRadius = 48, HandMultiplier = 2.25f, ArmOffset = .25f, ArmOffsetMax = 40;
    internal const float ClaspRadius = 64, ClaspMultiplier = 4.5f, CrownInner = 64, CrownOuter = 140, CrownRadius = 18, CrownMultiplier = 1, ClaspShake = 2.5f;
    internal const int CrownSpokes = 6, ClaspHands = 4, HandMaxLife = 145;
    internal static int HandSlamTick(int k) => HandFirstSlam + S(k);
    // The Choir's four arms take turns: outer left, inner right, inner left, outer right (sign x outer/inner).
    internal static float ArmSide(int k) => (k & 3) switch { 0 => -1f, 1 => .5f, 2 => -.5f, _ => 1f };
    internal static float HandOffset(int k, float npcWidth) => ArmSide(k) * Math.Min(npcWidth * ArmOffset, ArmOffsetMax);
    // Hands per hymn for `marks` on one NPC: the eighth on a fully marked NPC is the Clasp.
    internal static float HymnMultiplier(int marks)
    {
        marks = Math.Clamp(marks, 0, MaxMarksPerNpc);
        return marks >= MaxMarksPerNpc ? (MaxMarksPerNpc - 1) * HandMultiplier + ClaspMultiplier : marks * HandMultiplier;
    }
    internal static float OrganBuildMultiplier(int marks) => Math.Clamp(marks, 0, MaxMarksPerNpc) * ShardMultiplier;
    internal static int OrganBuildTicks(int marks) => Math.Clamp(marks, 0, MaxMarksPerNpc) * UseTicks(CrimsonRewardKind.Ranged);

    // ---- Magic: Scarlet Baton ----------------------------------------------------------------------------------
    internal const int BatonMana = 8, Gestures = 4, GestureTicks = 18, Anticipation = 3, Sweep = 8, FollowThrough = 7, BatonIdleReset = 90;
    internal const float BatonCursorRange = 900, StrokeChord = 180, StrokeTurnDegrees = 35, StrokeBendMax = 64, SweepWindow = 6;
    internal const int WriteTicks = 6, WriteLive = 6, StrokeLife = 480, StrokeFade = 60, MaxStrokes = 8, StrokeDryAway = 24;
    internal const float WriteRadius = 10, WriteMultiplier = .5f;
    internal const int TuttiTicks = 24, TuttiLift = 8, IgniteSwell = 3, IgniteLive = 14, IgniteScar = 24;
    internal const float IgniteRadiusFrom = 8, IgniteRadius = 24, IgniteMultiplier = 1.75f;
    internal const float RiverRadius = 28, RiverMaxRun = 900, RiverMultiplier = 5, RiverShake = 4;
    internal const int RiverHeadTicks = 24, RiverLive = 14, RiverMaxVertices = 600, RiverMaxLife = 135, MaxBatonStrokes = 9;
    internal static int IgniteTick(int i) => TuttiLift + S(i);
    internal static int RiverTick => TuttiLift + S(MaxStrokes + 1); // 71
    internal static float TuttiMultiplier(int strokes)
    {
        strokes = Math.Clamp(strokes, 0, MaxStrokes);
        return strokes * IgniteMultiplier + (strokes == MaxStrokes ? RiverMultiplier : 0);
    }
    internal static float BatonBuildMultiplier(int strokes) => Math.Clamp(strokes, 0, MaxStrokes) * WriteMultiplier;
    internal static int BatonBuildTicks(int strokes) => Math.Clamp(strokes, 0, MaxStrokes) * GestureTicks;

    // ---- Summon: Ember Censer ----------------------------------------------------------------------------------
    internal const int CenserMana = 10, CenserSlots = 1, CenserPerRow = 6;
    internal const float CenserBehind = 40, CenserStep = 34, CenserLift = 70, CenserOddLift = 8, CenserRowLift = 40;
    internal const float CenserSeek = 1200, CenserKeep = 1600, CenserStation = 200, CenserSpreadMax = 72, CenserSpreadPad = 96;
    internal const float BowlDrop = 60, SwingFrom = 25, SwingTo = 55, GrandSwing = 75, PourTip = 35;
    internal const int SwingPeriod = 64, ApexInterval = 32, PourTipTicks = 4, PourLive = 16, PourScar = 24, GrandBrace = 6, GrandLive = 20, GrandEvery = 4;
    internal const float PourFall = 40, PourSample = 8, PourMaxDepth = 600, PourRadius = 24, PourMultiplier = 1;
    internal const float GrandRadius = 56, GrandFall = 60, GrandMultiplier = 2.2f, GrandShake = 1.5f;
    internal const int GrandShakeInterval = 20, PourCueInterval = 7, SwingCueInterval = 14, GrandCueInterval = 20, SwingCueLead = 10;
    internal static int CenserCycleTicks => 3 * ApexInterval + (ApexInterval + GrandBrace); // 134
    internal static float CenserCycleMultiplier => (GrandEvery - 1) * PourMultiplier + GrandMultiplier; // 5.2
    // Rank r of n censers sharing a target sits this far from the target's centre.
    internal static float CenserSpread(int rank, int count, float width)
        => count <= 0 ? 0 : (rank - (count - 1) * .5f) * Math.Min(CenserSpreadMax, (width + CenserSpreadPad) / count);
    internal static Vector2 CenserIdleOffset(int slot, int facing)
    {
        int row = Math.Max(0, slot) / CenserPerRow, i = Math.Max(0, slot) % CenserPerRow;
        float x = -(facing == -1 ? -1 : 1) * (CenserBehind + CenserStep * i);
        float y = -(CenserLift + (i % 2 == 1 ? CenserOddLift : 0) + row * CenserRowLift);
        return new Vector2(x, y);
    }

    // ---- Rogue: Bloodink Quill ---------------------------------------------------------------------------------
    internal const float QuillSpeed = 20, QuillGravity = .25f, QuillMaxFall = 16, QuillMultiplier = 1;
    internal const int QuillStraight = 12, QuillFlight = 60, QuillLife = 480, QuillFade = 60, MaxQuills = 8, MaxQuillsInFlight = 4;
    internal const float InkNib = 9, InkTail = 3, InkBlot = 48;
    internal const float ScoreSpeed = 22; internal const int ScoreFlight = 30, ScoreWindup = 8;
    internal const float BurnSpeed = 180, BurnRadius = 18, BurnMultiplier = 1.75f; internal const int BurnLive = 12;
    internal const float QuillBurstRadius = 56, QuillBurstMultiplier = 1.5f;
    internal const float ScoreBurstRadius = 120, ScoreBurstMultiplier = 2, MelodyRadius = 160, MelodyMultiplier = 3, MelodyShake = 3.5f;
    internal const int QuillSerials = 1024, OffsetHalf = 512;
    internal static int QuillBurstTick(int unroll, int i) => unroll + S(i);
    internal static int ScoreBurstTick(int unroll, int quills) => unroll + S(Math.Max(0, quills));
    internal static float SealedScoreMultiplier(int quills, bool onInk)
    {
        quills = Math.Clamp(quills, 0, MaxQuills);
        float ink = onInk ? quills * BurnMultiplier : 0;
        return ink + quills * QuillBurstMultiplier + (quills == MaxQuills ? MelodyMultiplier : ScoreBurstMultiplier);
    }
    internal static float QuillBuildMultiplier(int quills) => Math.Clamp(quills, 0, MaxQuills) * QuillMultiplier;
    internal static int QuillBuildTicks(int quills) => Math.Clamp(quills, 0, MaxQuills) * UseTicks(CrimsonRewardKind.Rogue);

    // ---- Companion: Scarlet Covenant ---------------------------------------------------------------------------
    internal const int CovenantDamage = 1500, CovenantMana = 10, CovenantSlots = 10;

    // ---- Nominal parity (REWARDS.md#nominal-parity) -----------------------------------------------------------
    // Raw damage per busy tick on one target at zero defense: base x multiplier / ticks.
    internal static float PerTick(int baseDamage, float multiplier, float ticks) => ticks <= 0 ? 0 : baseDamage * multiplier / ticks;

    // ---- Codecs (exact integers in a float, |value| < 2^24) ---------------------------------------------------
    // Two signed integers within +-half packed into one exact integer (the quill offset, the baton's bend/skew).
    internal static float PackPair(int x, int y, int half)
    {
        if (half is < 1 or > 2047 || Math.Abs(x) > half || Math.Abs(y) > half) throw new ArgumentOutOfRangeException();
        int span = half * 2 + 1;
        return (x + half) + (y + half) * span;
    }
    internal static bool TryUnpackPair(float packed, int half, out int x, out int y)
    {
        x = y = 0;
        if (half is < 1 or > 2047) return false;
        int span = half * 2 + 1;
        if (!ValidInteger(packed, 0, span * span - 1)) return false;
        int value = (int)packed;
        x = value % span - half; y = value / span - half;
        return true;
    }
    // Serials run 0..1023 and compare modulo 1024: is `a` newer than `b`?
    internal static int NextSerial(int serial) => (serial + 1) & (QuillSerials - 1);
    internal static bool SerialNewer(int a, int b)
    {
        int delta = (a - b) & (QuillSerials - 1);
        return delta != 0 && delta < QuillSerials / 2;
    }

    // ---- Geometry: capsules (segment + radius), discs (zero-length capsules) and hitboxes ---------------------
    internal static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a; float len = ab.LengthSquared();
        float t = len < 1e-6f ? 0 : Math.Clamp(Vector2.Dot(p - a, ab) / len, 0, 1);
        return Vector2.Distance(p, a + ab * t);
    }
    internal static bool DiscTouchesBox(Vector2 center, float radius, Vector2 min, Vector2 max)
        => BoxDistance(center, min, max) <= radius;
    // Exact: does an axis-aligned box (min, max) touch the capsule a-b of `radius`?
    internal static bool CapsuleTouchesBox(Vector2 a, Vector2 b, float radius, Vector2 min, Vector2 max)
        => SegmentBoxDistance(a, b, min, max) <= radius;
    internal static float BoxDistance(Vector2 p, Vector2 min, Vector2 max)
    {
        Vector2 outside = Vector2.Max(Vector2.Max(min - p, p - max), Vector2.Zero);
        return outside.Length();
    }
    // Shortest distance between a segment and a box: zero when they meet, otherwise reached at a segment end
    // or at a box corner (both shapes are convex).
    internal static float SegmentBoxDistance(Vector2 a, Vector2 b, Vector2 min, Vector2 max)
    {
        if (SegmentMeetsBox(a, b, min, max)) return 0;
        float best = Math.Min(BoxDistance(a, min, max), BoxDistance(b, min, max));
        best = Math.Min(best, SegmentDistance(min, a, b));
        best = Math.Min(best, SegmentDistance(max, a, b));
        best = Math.Min(best, SegmentDistance(new Vector2(min.X, max.Y), a, b));
        return Math.Min(best, SegmentDistance(new Vector2(max.X, min.Y), a, b));
    }
    // Slab test (Liang-Barsky) of the closed segment against the closed box.
    internal static bool SegmentMeetsBox(Vector2 a, Vector2 b, Vector2 min, Vector2 max)
    {
        float t0 = 0, t1 = 1;
        Vector2 d = b - a;
        return Clip(-d.X, a.X - min.X) && Clip(d.X, max.X - a.X) && Clip(-d.Y, a.Y - min.Y) && Clip(d.Y, max.Y - a.Y);
        bool Clip(float p, float q)
        {
            if (MathF.Abs(p) < 1e-9f) return q >= 0;
            float r = q / p;
            if (p < 0) { if (r > t1) return false; if (r > t0) t0 = r; }
            else { if (r < t0) return false; if (r < t1) t1 = r; }
            return true;
        }
    }
    internal static float Smooth(float x)
    {
        x = Math.Clamp(x, 0, 1);
        return x * x * x * (10 + x * (-15 + 6 * x));
    }
}

// One release part's hit ledger: each realLife root counts once, however many segments it has.
internal sealed class CrimsonRootLedger
{
    private readonly int[] roots;
    private int count;
    internal CrimsonRootLedger(int capacity = CrimsonRewardRules.RootLedgerCapacity) => roots = new int[Math.Clamp(capacity, 1, 1024)];
    internal int Count => count;
    internal bool Contains(int root)
    {
        for (int i = 0; i < count; i++) if (roots[i] == root) return true;
        return false;
    }
    // True when the root is new (and is now recorded); false when it already took this part's hit or the ledger is full.
    internal bool TryAdd(int root)
    {
        if (root < 0 || Contains(root) || count >= roots.Length) return false;
        roots[count++] = root;
        return true;
    }
    internal void Clear() => count = 0;
}
