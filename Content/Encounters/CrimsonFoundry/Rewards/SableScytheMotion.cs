#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// One pose of the Sable Scythe in the aim frame: x runs along the aim, y toward the ground for a right-facing swing
// (mirrored by facing). Spin is the in-plane angle of the hook tip about the grip (the Over and Under turn it one way;
// only the Whip's draw-back eases it back while the plane rolls). Roll tilts the swing plane about the aim axis: 0 is
// the Over plane, pi the Under plane, and in between the hook rolls over the wrist (the plane turns edge-on). Hand is
// the grip's offset from the shoulder, within the native front arm's reach.
internal readonly record struct SablePose(float Spin, float Roll, Vector2 Hand);

// Where Staff Reap lays its staff: the centre (the snapped NPC's centre or the clamped cursor), the line spacing and
// the snapped NPC's index in the candidate list (-1 when nothing was within reach of the cursor).
internal readonly record struct SableStaffPlacement(Vector2 Center, float Spacing, int Target);

// One release part as cast: the starting end, the signed length (x for a staff line, +y down for the barline pair),
// the order index (5 = the barline) and the tick after the cast at which it fires.
internal readonly record struct SableCutPlan(Vector2 Start, float Length, int Index, int Fire);

// Pure rules for the Sable Scythe (docs/encounters/crimson-foundry/REWARDS.md, "Melee - Sable Scythe"): the
// figure-eight measure, the blade's capsules, the hanging staff, Staff Reap's placement and cut schedule, and the
// owner's combo. No Terraria or XNA references: the domain tests and the offline preview link this file. Numbers
// that the spec owns live in CrimsonRewardRules; the knot tables below are this weapon's own motion.
//
// The figure eight is a moulinet seen from the side: the scythe spins on in its plane, one turn per half, and between
// halves the plane rolls over the aim axis behind the hip (after an Over) or the shoulder (after an Under). The
// "angular speed" of the spec is the 3D one, |spin'| and |roll'| together, so a roll keeps the blade moving while the
// projected sweep turns round. Knots were fitted offline against the bounds below and are re-checked by the domain
// tests (Tests/Convergence.DomainTests/ScarletScytheTests.cs). Projected, the
// Over cuts down through the aim, the Under rises through it, and each roll swings the hook round a small lobe
// behind the reaper: the hook tip's path is an eight lying along the aim. Every join matches pose and speed (Hermite
// knots), the tip never slows under 3 px/tick or turns tighter than 24 px, the 3D angular speed through a roll
// stays above 25% of the cut's peak (20% through the Whip's draw-back), and the 3D angular acceleration stays under
// 0.3 rad/tick^2. Each part is a pure function of (stroke, age), so every client rebuilds it from the replicated ai.
internal static class SableScytheMotion
{
    // ---- Measure: Over, Under, Over, Under, Whip ------------------------------------------------------------
    internal const int Over = 0, Under = 1, Whip = 2, Strokes = CrimsonRewardRules.MeasureStrokes;
    internal const int MaximumDuration = CrimsonRewardRules.WhipTicks;

    internal static int Kind(int index) => Index(index) == Strokes - 1 ? Whip : (Index(index) & 1) == 0 ? Over : Under;
    internal static int Next(int index) => (Index(index) + 1) % Strokes;
    internal static int Duration(int index) => Kind(index) == Whip ? CrimsonRewardRules.WhipTicks : CrimsonRewardRules.StrokeTicks;
    internal static int LiveStart(int index) => Kind(index) == Whip ? CrimsonRewardRules.WhipLiveStart : CrimsonRewardRules.StrokeLiveStart;
    internal static int LiveEnd(int index) => Kind(index) == Whip ? CrimsonRewardRules.WhipLiveEnd : CrimsonRewardRules.StrokeLiveEnd;
    // The blade is live on [LiveStart, LiveEnd]; the swept test samples the tick that just elapsed.
    internal static bool Live(int index, int age) => age > LiveStart(index) && age <= LiveEnd(index);
    internal static float Multiplier(int index) => Kind(index) == Whip ? CrimsonRewardRules.WhipMultiplier : CrimsonRewardRules.StrokeMultiplier;
    // The Whip's crescent: written by the hook tip over the lash, live until CrescentLiveEnd, once per root.
    internal static bool CrescentLive(int index, int age)
        => Kind(index) == Whip && age > CrimsonRewardRules.WhipLiveStart && age <= CrimsonRewardRules.CrescentLiveEnd;
    private static int Index(int index) => Math.Clamp(index, 0, Strokes - 1);

    // Soboro's sub-tick policy: a tick draws the span it just swept, [age - 1, age]; a live tick (LiveStart + 1 ..
    // LiveEnd) therefore never shows the windup, and a recovery tick never shows a live pose.
    internal static float DrawAge(int index, int ageTick, float fraction) => Math.Clamp(ageTick - 1f + fraction, 0f, Duration(index));
    // The last tick a stroke can still damage: a release pressed during it begins then (the Whip waits for its crescent).
    internal static int LastDamage(int index) => Kind(index) == Whip ? CrimsonRewardRules.CrescentLiveEnd : LiveEnd(index);

    // ---- Knots: (tick, value, value per tick) ------------------------------------------------------------------
    // Over. Spin: from behind (mid-roll) over the shoulder, through the aim at 9 at the cut's peak speed, down and
    // round behind the hip; the Under spins the same wheel one turn later. Roll: the plane finishes rolling into the
    // Over plane by 5 and starts rolling toward the Under plane at 13 (each roll is centred 0.25 tick after a join).
    // Hand: rises through the Over (the Mantle's high slash floats) and loops back behind the hip.
    private const float Tau = MathF.PI * 2;
    private const float RollCentre = .25f, RollHalf = 4.75f;
    private static readonly float[] OverSpin = { 0, -1.90f, .21f, 5, -1.16f, .25f, 9, 0, .46f, 13, 1.90f, .47f, 18, Tau - 1.90f, .21f };
    private static readonly float[] OverRoll =
        { RollCentre - RollHalf, -MathF.PI, 0, RollCentre + RollHalf, 0, 0, 18 + RollCentre - RollHalf, 0, 0, 18 + RollCentre + RollHalf, MathF.PI, 0 };
    // The roll at an Over's first tick (mid-roll between the centred knots), shared by the Whip's first and last knots.
    private static readonly float StartRoll = Evaluate(OverRoll, 0), StartRollSpeed = Slope(OverRoll, 0);
    private static readonly float[] OverHandX = { 0, -10, 1.2f, 9, 10, 0, 18, -10, 1.2f };
    private static readonly float[] OverHandY = { 0, 11.4f, .9f, 9, -3, -.4f, 18, -11.4f, -.9f };
    // The Under is the Over mirrored across the aim: the plane rolled by pi and the hand sinking (the low slash sinks).
    private static readonly float[] UnderRoll = Shift(OverRoll, MathF.PI);
    private static readonly float[] UnderHandY = Shift(OverHandY, 0, -1);

    // Whip: the hook keeps rising behind the head, then draws back on 6-12 (the Mantle's brace: the spin eases back
    // while the plane rolls toward edge-on, so the 3D speed stays above 20% of the lash). It lashes forward through the
    // aim on 14-20 in that tilted plane (a flat lash, the crescent curling back toward the reaper) while the arm thrusts
    // 24 px forward, then spins on and rolls through the Under plane into the Over's starting pose, one turn and one
    // roll later.
    private static readonly float[] WhipSpin =
        { 0, -1.90f, .21f, 6, -1.613f, -.096f, 12, -1.741f, .236f, 17, 0, .652f, 20, 1.346f, .211f, 28, Tau - 1.90f, .21f };
    private static readonly float[] WhipRoll =
        { 0, StartRoll, StartRollSpeed, 6, -.269f, .184f, 13, 1.098f, .005f, 17, 1.104f, .004f, 21, 2.205f, .554f, 28, StartRoll + Tau, StartRollSpeed };
    private static readonly float[] WhipHandX = { 0, -10, 1.2f, 10, -18.859f, 0, 14, -18.859f, 0, 20, 5.141f, -1.227f, 28, -10, 1.2f };
    private static readonly float[] WhipHandY = { 0, 11.4f, .9f, 10, -6.653f, 0, 20, 8.372f, -1.633f, 28, 11.4f, .9f };

    internal static SablePose Pose(int index, float age)
    {
        switch (Kind(index))
        {
            case Whip:
                return new SablePose(Evaluate(WhipSpin, age), Evaluate(WhipRoll, age), new Vector2(Evaluate(WhipHandX, age), Evaluate(WhipHandY, age)));
            case Under:
                return new SablePose(Evaluate(OverSpin, age), Evaluate(UnderRoll, age), new Vector2(Evaluate(OverHandX, age), Evaluate(UnderHandY, age)));
            default:
                return new SablePose(Evaluate(OverSpin, age), Evaluate(OverRoll, age), new Vector2(Evaluate(OverHandX, age), Evaluate(OverHandY, age)));
        }
    }

    // ---- Geometry ---------------------------------------------------------------------------------------------
    // The hook in the tip frame (x along grip -> hook tip, y toward the side the tip leads; unit = Reach). Measured from
    // the placeholder (the Death Sickle sprite, grip at texel (7, 57), hook tip at (55, 53.5)); the exporter re-measures
    // them from SR02 when the art is delivered. The blade is three capsules 26 px wide from the root at the head of the
    // haft to the hook tip; the haft does not hurt.
    internal static readonly Vector2[] BladeKnots = { new(1.10f, -.72f), new(1.22f, -.45f), new(1.17f, -.18f), new(1f, 0) };
    internal static float Reach => CrimsonRewardRules.ScytheReach;
    internal static float BladeRadius => CrimsonRewardRules.BladeWidth * .5f;

    // A point of the hook (tip frame) in the aim frame: rotate by the spin, project the rolled plane, add the hand.
    internal static Vector2 Project(in SablePose pose, Vector2 local)
    {
        float c = MathF.Cos(pose.Spin), s = MathF.Sin(pose.Spin);
        Vector2 q = new Vector2(local.X * c - local.Y * s, (local.X * s + local.Y * c) * MathF.Cos(pose.Roll)) * Reach;
        return pose.Hand + q;
    }
    internal static Vector2 Tip(in SablePose pose) => Project(pose, Vector2.UnitX);
    internal static void Blade(in SablePose pose, Span<Vector2> knots)
    {
        for (int i = 0; i < BladeKnots.Length && i < knots.Length; i++) knots[i] = Project(pose, BladeKnots[i]);
    }

    // The aim frame to the world: rotate by the aim, mirror across the aim for a left-facing swing.
    internal static Vector2 ToWorld(Vector2 local, float aim, int facing)
    {
        float c = MathF.Cos(aim), s = MathF.Sin(aim), y = facing < 0 ? -local.Y : local.Y;
        return new Vector2(local.X * c - y * s, local.X * s + y * c);
    }
    internal static int Facing(float aim) => MathF.Cos(aim) < 0 ? -1 : 1;

    // Does the blade at this pose touch a box (all in the aim frame of the caller's choosing, already in world space
    // when the knots are)? Three capsules of radius BladeRadius.
    internal static bool BladeTouches(ReadOnlySpan<Vector2> knots, Vector2 min, Vector2 max)
    {
        for (int i = 0; i + 1 < knots.Length; i++)
            if (CrimsonRewardRules.CapsuleTouchesBox(knots[i], knots[i + 1], BladeRadius, min, max)) return true;
        return false;
    }

    // 3D angular speed and acceleration of the scythe: the spin about the plane normal plus the roll about the aim.
    internal static float AngularSpeed(float spinSpeed, float rollSpeed) => MathF.Sqrt(spinSpeed * spinSpeed + rollSpeed * rollSpeed);
    internal static float AngularAcceleration(float spinSpeed, float rollSpeed, float spinAcceleration, float rollAcceleration)
        => MathF.Sqrt(spinAcceleration * spinAcceleration + spinSpeed * spinSpeed * rollSpeed * rollSpeed + rollAcceleration * rollAcceleration);

    // ---- Blends -----------------------------------------------------------------------------------------------
    // Interpolate two poses along the shorter way round (spin and roll), the hand linearly.
    internal static SablePose Blend(in SablePose from, in SablePose to, float weight)
    {
        float w = Math.Clamp(weight, 0, 1);
        return new SablePose(from.Spin + Wrap(to.Spin - from.Spin) * w, from.Roll + Wrap(to.Roll - from.Roll) * w,
            Vector2.Lerp(from.Hand, to.Hand, w));
    }
    // Re-express a pose cast along one aim in the frame of another (a release's aim, the next stroke's aim). Exact in
    // the Over and Under planes; mid-roll it is a visual approximation, used only by harmless windups.
    internal static SablePose Reframe(in SablePose pose, float fromAim, int fromFacing, float toAim, int toFacing)
    {
        float roll = pose.Roll;
        Vector2 hand = pose.Hand;
        if (fromFacing != toFacing) { roll = MathF.PI - roll; hand.Y = -hand.Y; }
        float turn = (toFacing < 0 ? -1 : 1) * Wrap(fromAim - toAim), c = MathF.Cos(turn), s = MathF.Sin(turn);
        hand = new Vector2(hand.X * c - hand.Y * s, hand.X * s + hand.Y * c);
        return new SablePose(pose.Spin + turn * MathF.Cos(roll), roll, hand);
    }
    internal static float Wrap(float angle) => MathF.IEEERemainder(angle, Tau);

    // ---- Staff Reap: the release pose ---------------------------------------------------------------------------
    // Windup 0-16: the reaper draws the scythe high behind and whips it forward at 16; then it holds its follow-through
    // until the release ends (56 for a full staff). The first 12 ticks blend from wherever the blade was.
    internal const int ReleaseBlend = 12;
    private static readonly float[] ReleaseSpin = { 0, -2.0f, -.04f, 12, -2.55f, -.02f, 16, -1.2f, .55f, 19, .35f, .25f, 24, .62f, .03f, 56, .8f, 0 };
    private static readonly float[] ReleaseHandX = { 0, -4, -.6f, 12, -12, 0, 16, -2, 2.6f, 20, 12, .4f, 56, 10, 0 };
    private static readonly float[] ReleaseHandY = { 0, -2, -.5f, 12, -12, 0, 16, -8, .8f, 20, -2, .3f, 56, 0, 0 };
    internal static SablePose ReleasePose(float age)
        => new(Evaluate(ReleaseSpin, age), 0, new Vector2(Evaluate(ReleaseHandX, age), Evaluate(ReleaseHandY, age)));
    internal static SablePose ReleasePose(float age, in SablePose from)
        => Blend(from, ReleasePose(age), CrimsonRewardRules.Smooth(age / ReleaseBlend));
    internal static int ReleaseTicks(int lines) => CrimsonRewardRules.StaffReleaseTicks(lines);

    // ---- The hanging staff (the build) --------------------------------------------------------------------------
    // Lines engraved so far, `age` ticks after the last engraving: they last 360 ticks, then drain one per 30.
    internal static int LinesAt(int engraved, float age)
    {
        int lines = Math.Clamp(engraved, 0, CrimsonRewardRules.StaffLines);
        if (age < CrimsonRewardRules.StaffLineLife) return lines;
        int drained = (int)MathF.Floor((age - CrimsonRewardRules.StaffLineLife) / CrimsonRewardRules.StaffDrainTicks);
        return Math.Max(0, lines - drained);
    }
    // The staff carrier's whole life after the last engraving: 360 + 30 per line.
    internal static int StaffLife(int engraved)
        => CrimsonRewardRules.StaffLineLife + Math.Clamp(engraved, 0, CrimsonRewardRules.StaffLines) * CrimsonRewardRules.StaffDrainTicks;
    // Line i (engraving order, middle first) of the hanging staff: its centre in the aim-free player frame (x behind
    // the shoulder when facing right, y down). Five 56 px lines, 8 px apart, 40 px behind the shoulder.
    internal static Vector2 HangingLine(int i, int facing)
        => new(-(facing < 0 ? -1 : 1) * CrimsonRewardRules.StaffBehind, CrimsonRewardRules.StaffRow(Math.Clamp(i, 0, 4)) * CrimsonRewardRules.StaffLineGap);

    // ---- Staff Reap: placement ------------------------------------------------------------------------------------
    // C is the cursor clamped within 720 px of the player. If a chaseable NPC's hitbox lies within 96 px of C, the staff
    // centres on the nearest such NPC and its spacing becomes clamp(h / 4, 14, 36); otherwise it centres on C at 36.
    internal static Vector2 ClampCursor(Vector2 player, Vector2 cursor)
    {
        Vector2 d = cursor - player;
        float length = d.Length();
        if (!float.IsFinite(length)) return player;
        return length > CrimsonRewardRules.StaffCursorRange ? player + d / length * CrimsonRewardRules.StaffCursorRange : cursor;
    }
    internal static SableStaffPlacement Place(Vector2 player, Vector2 cursor, ReadOnlySpan<Vector2> boxMin, ReadOnlySpan<Vector2> boxMax)
    {
        Vector2 c = ClampCursor(player, cursor);
        int best = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < boxMin.Length && i < boxMax.Length; i++)
        {
            float d = CrimsonRewardRules.BoxDistance(c, boxMin[i], boxMax[i]);
            if (d <= CrimsonRewardRules.StaffSnap && d < bestDistance) { best = i; bestDistance = d; }
        }
        if (best < 0) return new SableStaffPlacement(c, CrimsonRewardRules.StaffSpacingMax, -1);
        Vector2 min = boxMin[best], max = boxMax[best];
        return new SableStaffPlacement((min + max) * .5f, CrimsonRewardRules.StaffSpacing(max.Y - min.Y), best);
    }

    // ---- Staff Reap: the cut schedule ---------------------------------------------------------------------------
    // Line k (0 = the middle, then outward) fires at 16 + S(k), alternately from the player's side and the far side;
    // with five lines the Final Barline drops through the staff at the centre at 51.
    internal static int Cuts(int lines) => Math.Clamp(lines, 0, CrimsonRewardRules.StaffLines) + (lines >= CrimsonRewardRules.StaffLines ? 1 : 0);
    internal static SableCutPlan Cut(int k, in SableStaffPlacement staff, float playerX)
    {
        if (k == CrimsonRewardRules.StaffLines)
        {
            float half = BarlineHeight(staff.Spacing) * .5f;
            return new SableCutPlan(staff.Center - new Vector2(0, half), half * 2, k, CrimsonRewardRules.BarlineTick);
        }
        float side = playerX <= staff.Center.X ? -1 : 1; // the player's end of the line
        if ((k & 1) == 1) side = -side;                  // odd lines run from the far side
        float y = staff.Center.Y + CrimsonRewardRules.StaffRow(k) * staff.Spacing;
        var start = new Vector2(staff.Center.X + side * CrimsonRewardRules.StaffHalfLength, y);
        return new SableCutPlan(start, -side * CrimsonRewardRules.StaffHalfLength * 2, k, CrimsonRewardRules.StaffLineTick(k));
    }
    internal static float BarlineHeight(float spacing) => 4 * spacing + CrimsonRewardRules.BarlineExtra;

    // A staff line t ticks after it fired: its head crosses the whole length in 4 ticks; each point is live for 10
    // ticks after the head passes, then dries into a 20-tick scar. Fractions run from the starting end (0) to 1.
    internal static float LineHead(float t) => Math.Clamp(t / CrimsonRewardRules.StaffHeadTicks, 0, 1);
    internal static float LineIgnited(float t, float fraction) => t - fraction * CrimsonRewardRules.StaffHeadTicks;
    internal static bool LineLive(float t) => t >= 0 && t <= CrimsonRewardRules.StaffHeadTicks + CrimsonRewardRules.StaffLineLive;
    internal static float LineTail(float t) => Math.Clamp((t - CrimsonRewardRules.StaffLineLive) / CrimsonRewardRules.StaffHeadTicks, 0, 1);
    // The barline pair falls through its height in 3 ticks and stays live for 12 after it lands.
    internal static float BarlineHead(float t) => Math.Clamp(t / CrimsonRewardRules.BarlineFall, 0, 1);
    internal static float BarlineIgnited(float t, float fraction) => t - fraction * CrimsonRewardRules.BarlineFall;
    internal static bool BarlineLive(float t) => t >= 0 && t <= CrimsonRewardRules.BarlineFall + CrimsonRewardRules.BarlineLive;
    // Live ink radius at a point: the part's radius opened over 3 ticks since that point ignited (draw = collide).
    internal static float LiveRadius(float radius, float ignited) => ignited < 0 ? 0 : radius * CrimsonRewardRules.InkOpen(ignited);
    // Ticks after its own fire tick until a cut's scar has dried.
    internal static int CutVisible(int index)
        => index == CrimsonRewardRules.StaffLines
            ? CrimsonRewardRules.BarlineFall + CrimsonRewardRules.BarlineLive + CrimsonRewardRules.StaffScar
            : CrimsonRewardRules.StaffHeadTicks + CrimsonRewardRules.StaffLineLive + CrimsonRewardRules.StaffScar;
    // With a full staff the lines hold their scars until the barline has dried, so the whole staff dries together.
    internal static int StaffDryTick(int lines) => lines >= CrimsonRewardRules.StaffLines
        ? CrimsonRewardRules.BarlineTick + CrimsonRewardRules.BarlineFall + CrimsonRewardRules.BarlineLive : -1;
    // Ticks after its own fire tick until a cut of a release of `lines` has finished drying (and may be retired).
    internal static int CutEnd(int index, int lines)
        => lines >= CrimsonRewardRules.StaffLines && index < CrimsonRewardRules.StaffLines
            ? StaffDryTick(lines) - CrimsonRewardRules.StaffLineTick(index) + CrimsonRewardRules.StaffScar
            : CutVisible(index);
    // A dried point's scar fade (1..0): its own 20 ticks after it stops being live, or, on a full staff, held at 1
    // until the barline has dried and then the whole staff together.
    internal static float LineScarFade(float t, float fraction, int index, int lines)
    {
        float fade = lines >= CrimsonRewardRules.StaffLines
            ? 1 - Math.Max(0, t + CrimsonRewardRules.StaffLineTick(index) - StaffDryTick(lines)) / CrimsonRewardRules.StaffScar
            : 1 - Math.Max(0, LineIgnited(t, fraction) - CrimsonRewardRules.StaffLineLive) / CrimsonRewardRules.StaffScar;
        return Math.Clamp(fade, 0, 1);
    }

    // Does a cut's live ink touch a box, `t` ticks after it fired? Sampled as short capsules whose radius is the
    // smaller end's opened radius, so the collision never exceeds the drawn ink.
    internal static bool CutTouches(in SableCutPlan cut, float t, Vector2 min, Vector2 max)
    {
        bool barline = cut.Index == CrimsonRewardRules.StaffLines;
        if (barline ? !BarlineLive(t) : !LineLive(t)) return false;
        float head = barline ? BarlineHead(t) : LineHead(t);
        float tail = barline ? 0 : LineTail(t);
        if (head <= tail) return false;
        const int pieces = 16;
        float radius = barline ? CrimsonRewardRules.BarlineRadius : CrimsonRewardRules.StaffRadius;
        for (int side = 0; side < (barline ? 2 : 1); side++)
        {
            Vector2 offset = barline ? new Vector2((side == 0 ? -.5f : .5f) * CrimsonRewardRules.BarlineGap, 0) : Vector2.Zero;
            Vector2 along = barline ? new Vector2(0, cut.Length) : new Vector2(cut.Length, 0);
            for (int i = 0; i < pieces; i++)
            {
                float f0 = tail + (head - tail) * i / pieces, f1 = tail + (head - tail) * (i + 1) / pieces;
                float r0 = LiveRadius(radius, barline ? BarlineIgnited(t, f0) : LineIgnited(t, f0));
                float r1 = LiveRadius(radius, barline ? BarlineIgnited(t, f1) : LineIgnited(t, f1));
                float r = MathF.Min(r0, r1);
                if (r <= 0) continue;
                if (CrimsonRewardRules.CapsuleTouchesBox(cut.Start + offset + along * f0, cut.Start + offset + along * f1, r, min, max)) return true;
            }
        }
        return false;
    }

    // ---- Commitment (REWARDS.md, "Commitment") -------------------------------------------------------------------
    // A release part that has not started (negative age) dies with its owner's death or Down; a started part finishes
    // its window. The staff survives Down and item swaps; only death (or leaving the world) clears it.
    internal static bool CutSurvives(float age, bool ownerUsable) => age >= 0 || ownerUsable;
    internal static bool StaffSurvives(bool ownerActive, bool ownerDead) => ownerActive && !ownerDead;
    // Strokes and the release pose are held: they stop whenever the owner cannot act or stops holding the scythe.
    internal static bool HeldSurvives(bool ownerCanAct, bool holding) => ownerCanAct && holding;

    // ---- Replicated data (every projectile checks its ai and kills itself on invalid data) -----------------------
    internal const float AimLimit = 8;
    internal const int FromStride = 32;
    // SableStroke: ai = (stroke index, aim, age); velocity = (what came before: -1, 0 or 1-5 lines; its aim).
    internal static bool ValidStroke(float stroke, float aim, float age, float from, float fromAim)
        => CrimsonRewardRules.ValidInteger(stroke, 0, Strokes - 1) && CrimsonRewardRules.Valid(aim, -AimLimit, AimLimit)
            && CrimsonRewardRules.Valid(age, 0, MaximumDuration + 1) && CrimsonRewardRules.ValidInteger(from, -1, CrimsonRewardRules.StaffLines)
            && CrimsonRewardRules.Valid(fromAim, -AimLimit, AimLimit);
    // SableStaff: ai = (lines at the last engraving, ticks since it, unused).
    internal static bool ValidStaff(float lines, float since, float unused)
        => CrimsonRewardRules.ValidInteger(lines, 1, CrimsonRewardRules.StaffLines)
            && CrimsonRewardRules.Valid(since, 0, StaffLife(CrimsonRewardRules.StaffLines) + 2) && CrimsonRewardRules.Valid(unused, -1, 1);
    // SableRelease: ai = (aim, lines spent, age); velocity = (the interrupted stroke as index * 32 + age, or -1; its aim).
    internal static bool ValidRelease(float aim, float lines, float age, float from, float fromAim)
        => CrimsonRewardRules.Valid(aim, -AimLimit, AimLimit) && CrimsonRewardRules.ValidInteger(lines, 1, CrimsonRewardRules.StaffLines)
            && CrimsonRewardRules.Valid(age, 0, CrimsonRewardRules.BarlineHold + 1)
            && CrimsonRewardRules.ValidInteger(from, -1, (Strokes - 1) * FromStride + FromStride - 1)
            && CrimsonRewardRules.Valid(fromAim, -AimLimit, AimLimit);
    // StaffCut: ai = (signed length, index + 8 x lines spent, age); a barline only with five lines, a line only below them.
    internal const int CutStride = 8;
    internal static bool ValidCut(float length, float code, float age)
    {
        if (!CrimsonRewardRules.ValidInteger(code, CutStride, CutStride * CrimsonRewardRules.StaffLines + CrimsonRewardRules.StaffLines)
            || !CrimsonRewardRules.Valid(age, -CrimsonRewardRules.BarlineTick - 1, CrimsonRewardRules.StaffCutLife)
            || !CrimsonRewardRules.Valid(length, -CrimsonRewardRules.StaffHalfLength * 2 - 1, CrimsonRewardRules.StaffHalfLength * 2 + 1)) return false;
        int index = (int)code % CutStride, lines = (int)code / CutStride;
        if (index == CrimsonRewardRules.StaffLines)
            return lines == CrimsonRewardRules.StaffLines && length >= BarlineHeight(CrimsonRewardRules.StaffSpacingMin) - 1
                && length <= BarlineHeight(CrimsonRewardRules.StaffSpacingMax) + 1;
        return index < lines && MathF.Abs(MathF.Abs(length) - CrimsonRewardRules.StaffHalfLength * 2) <= 1;
    }

    // ---- Hermite evaluation (Moonshear's form) -------------------------------------------------------------------
    internal static float Evaluate(float[] knots, float t)
    {
        int last = knots.Length - 3, i = 0;
        t = Math.Clamp(t, knots[0], knots[last]);
        while (i < last - 3 && t > knots[i + 3]) i += 3;
        float t0 = knots[i], a = knots[i + 1], sa = knots[i + 2];
        float t1 = knots[i + 3], b = knots[i + 4], sb = knots[i + 5];
        float span = t1 - t0, u = Math.Clamp((t - t0) / span, 0f, 1f), u2 = u * u, u3 = u2 * u;
        return (2 * u3 - 3 * u2 + 1) * a + (u3 - 2 * u2 + u) * span * sa + (-2 * u3 + 3 * u2) * b + (u3 - u2) * span * sb;
    }

    // The Hermite's value per tick at t (used to start the Whip exactly on the Over's roll).
    internal static float Slope(float[] knots, float t)
    {
        int last = knots.Length - 3, i = 0;
        t = Math.Clamp(t, knots[0], knots[last]);
        while (i < last - 3 && t > knots[i + 3]) i += 3;
        float t0 = knots[i], a = knots[i + 1], sa = knots[i + 2];
        float t1 = knots[i + 3], b = knots[i + 4], sb = knots[i + 5];
        float span = t1 - t0, u = Math.Clamp((t - t0) / span, 0f, 1f), u2 = u * u;
        return ((6 * u2 - 6 * u) * a + (3 * u2 - 4 * u + 1) * span * sa + (-6 * u2 + 6 * u) * b + (3 * u2 - 2 * u) * span * sb) / span;
    }

    private static float[] Shift(float[] knots, float add, float scale = 1)
    {
        var copy = (float[])knots.Clone();
        for (int i = 1; i < copy.Length; i += 3) { copy[i] = copy[i] * scale + add; copy[i + 1] *= scale; }
        return copy;
    }
}

// The owner's measure counter: Over, Under, Over, Under, Whip, restarting after ComboIdleReset idle ticks from a
// stroke's end, an item change, a release or death.
internal sealed class SableCombo
{
    private int next;
    private ulong idleFrom;
    private bool used;

    internal int Next => used ? next : 0;

    internal int Take(ulong now)
    {
        if (!used || now > idleFrom + (ulong)CrimsonRewardRules.ComboIdleReset) next = 0;
        int stroke = next;
        next = SableScytheMotion.Next(next);
        idleFrom = now + (ulong)SableScytheMotion.Duration(stroke);
        used = true;
        return stroke;
    }

    internal void Reset() { next = 0; idleFrom = 0; used = false; }
}
