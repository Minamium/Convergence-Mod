#nullable enable
using System;
using System.Numerics;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// How a Lacuna Testament channel ended, replicated with the controller's ExtraAI so every peer hears and sees
// the same close: a release (the player let go) or a collapse for lack of mana (the failed ritual).
internal enum LacunaEnd : byte { None, Release, Starved }

// Lacuna Testament, 2026-10 refresh (docs/encounters/first-severance/WEAPONS.md, "Magic — Lacuna Testament"): the
// single code owner of the weapon's timing, hit geometry, mana cadence and damage multipliers. The held controller
// (LacunaIrisChannel), its pellets and the client presentation all read these numbers; the art is fitted to them.
//
// Score age a is the controller's ai[0] after its AI: a press updates the new controller on the press tick, so age a
// lands on tick a - 1 (DollWeaponBudget). World offsets are px relative to the owner's centre, +y down, for an
// upright owner; `gravDir` -1 mirrors them vertically. Pure: System and System.Numerics only, linked into the
// domain tests and the offline preview.
internal static class LacunaTestamentScore
{
    internal const int Irises = 7;
    // An iris glides from the book's hole to its seat in ArriveTicks; it opens in four steps (closed, parting at
    // +FrameParting, half at +FrameHalf, open at +ShotDelay) and fires its first pellet the moment it snaps open,
    // then one every ShotPeriod. Before each later shot its petals half-close for ShotTell ticks.
    internal const int ArriveTicks = 12, FrameParting = 8, FrameHalf = 11, ShotDelay = 15, ShotPeriod = 38, ShotTell = 4;
    // 340-362 the irises fly to the muzzle and dock one by one; 362 they become the great aperture; 362-410 it charges
    // (seven ratchet clicks, then a still breath from the last click); 410 the beam opens over OpenTicks.
    internal const int Merge = 340, Formed = 362, Fire = 410, OpenTicks = 7;
    internal const int HitCadence = 10, ManaCadence = 8, BuildManaCadence = 30;
    internal const float BuildManaShare = .35f;
    // Release: damage stops on the release tick, the beam retracts into the hole over RetractTicks, the great aperture
    // breaks back into seven irises that snap shut, and the controller is gone after FadeTicks. The beam's residue
    // cools to plum within ResidueTicks of the release (half under Reduced Effects).
    internal const int FadeTicks = 20, RetractTicks = 10, ResidueTicks = 24;
    // An instant end (item change, crowd control, death, Down) leaves only a client-side collapse of CollapseTicks.
    internal const int CollapseTicks = 12;

    internal const float PelletFactor = .55f, BeamFactor = 2f;
    // Pellets: launched at PelletLaunch px/tick from the hole toward the cursor, then seek at PelletSpeed px/tick
    // (RitualTargeting, 1800 px acquisition), one hit per NPC root, PelletLife ticks (two updates per tick).
    internal const float PelletLaunch = 18, PelletSpeed = 36, PelletWidth = 18, PelletHoleOffset = 10;
    internal const int PelletLife = 150;

    // The beam, from the muzzle along the aim: Length px, ignoring tiles. Collision width OpenWidth widening (smoother
    // step) to FullWidth over WidenTicks from WidenStart, mean 116 px over that span; within ThroatLength of the muzzle
    // it collides at ThroatShare of the width. The drawn hard edge is exactly this width.
    internal const float Length = 2600, OpenWidth = 92, FullWidth = 140, ThroatLength = 48, ThroatShare = .78f;
    internal const int WidenStart = Fire + OpenTicks, WidenTicks = 353;
    // A pulse leaves the hole every PulsePeriod ticks from PulseFirst and runs down the beam at PulseSpeed px/tick.
    internal const int PulseFirst = Fire + 10, PulsePeriod = 30;
    internal const float PulseSpeed = 65, PulseTail = 100;
    // Aim turn caps (rad/tick): before the beam, then heavier as the beam widens.
    internal const float TurnBuild = .075f, TurnBeam = .033f, TurnBeamFull = .027f;

    // Geometry (px). The muzzle (beam origin, great aperture centre) is MuzzleDistance along the aim from the owner's
    // centre; the book floats BookDistance past the hand. Seats: an arch of radius SeatRadius about a point SeatLift
    // above the centre, angles SeatStartDeg + i * SeatStepDeg from the press's facing side. Docks: a rosette of
    // DockRadius about the muzzle, iris 0 at the top.
    internal const float MuzzleDistance = 150, BookDistance = 24, SeatRadius = 156, SeatLift = 10;
    internal const float SeatStartDeg = 21, SeatStepDeg = 23, DockRadius = 68, ArrivalBulge = 18, MergeLift = 60;

    private static readonly int[] births = { 0, 108, 183, 233, 269, 296, 316 };
    private static readonly int[] clicks = { 368, 378, 386, 392, 397, 401, 404 };
    private static readonly int[] widenBeats = { Fire + 120, Fire + 240, Fire + 360 };

    internal static int Clicks => clicks.Length;
    internal static int WidenBeats => widenBeats.Length;
    // The still breath before the beam: from the last click to the fire tick.
    internal static int Breath => clicks[^1];

    // ---- Easing (local copies, so this file stays self-contained) ---------------------------------------------

    internal static float Clamp01(float t) => float.IsFinite(t) ? Math.Clamp(t, 0f, 1f) : 0f;
    internal static float Arrive(float t) => 1 - MathF.Pow(1 - Clamp01(t), 4);
    internal static float Strike(float t) => MathF.Pow(Clamp01(t), 4);
    internal static float Smooth(float t)
    {
        t = Clamp01(t);
        return t * t * t * (10 + t * (-15 + 6 * t));
    }

    // ---- Irises -------------------------------------------------------------------------------------------------

    internal static int Birth(int iris) => births[Math.Clamp(iris, 0, Irises - 1)];
    internal static int ClickAt(int index) => clicks[Math.Clamp(index, 0, clicks.Length - 1)];
    internal static int WidenBeatAt(int index) => widenBeats[Math.Clamp(index, 0, widenBeats.Length - 1)];
    internal static int Docked(int iris) => 350 + 2 * Math.Clamp(iris, 0, Irises - 1);

    // Irises born by `age`.
    internal static int OpenCount(float age)
    {
        int count = 0;
        for (int i = 0; i < Irises; i++)
            if (age >= births[i]) count++;
        return count;
    }

    // The legacy MagicBoltAt schedule, unchanged: 9 + 6 + 4 + 3 + 2 + 1 + 1 = 26 pellets, none from Merge on.
    internal static bool ShotAt(int tick, int iris)
    {
        if ((uint)iris >= Irises || tick >= Merge) return false;
        int first = births[iris] + ShotDelay;
        return tick >= first && (tick - first) % ShotPeriod == 0;
    }

    internal static bool IsFirstShot(int tick, int iris) => (uint)iris < Irises && tick == births[iris] + ShotDelay;

    // The petals half-close ShotTell ticks before every later shot (the first shot is the opening itself).
    internal static bool TellAt(int tick, int iris) => ShotAt(tick + ShotTell, iris) && !IsFirstShot(tick + ShotTell, iris);

    internal static int ShotsAt(int tick)
    {
        int shots = 0;
        for (int i = 0; i < Irises; i++)
            if (ShotAt(tick, i)) shots++;
        return shots;
    }

    // Iris frame: -1 unborn, 0 closed, 1 parting, 2 half, 3 open (2 again during each tell).
    internal static int Frame(float age, int iris)
    {
        if ((uint)iris >= Irises || !float.IsFinite(age)) return -1;
        float local = age - births[iris];
        if (local < 0) return -1;
        if (local < FrameParting) return 0;
        if (local < FrameHalf) return 1;
        if (local < ShotDelay) return 2;
        if (age >= Merge) return 3;
        int tick = (int)MathF.Floor(age);
        for (int ahead = 1; ahead <= ShotTell; ahead++)
            if (ShotAt(tick + ahead, iris) && !IsFirstShot(tick + ahead, iris)) return 2;
        return 3;
    }

    // Seat of iris i: the arch keeps the facing of the press, so turning across vertical never flips it.
    internal static Vector2 Seat(int iris, int facing, float gravDir = 1)
    {
        float angle = (SeatStartDeg + SeatStepDeg * Math.Clamp(iris, 0, Irises - 1)) * MathF.PI / 180f;
        float side = facing < 0 ? -1 : 1, up = gravDir < 0 ? -1 : 1;
        return new Vector2(side * SeatRadius * MathF.Cos(angle), up * (-SeatLift - SeatRadius * MathF.Sin(angle)));
    }

    // Dock of iris i about the muzzle (relative to it): iris 0 at the top, the rest round the facing side.
    internal static Vector2 Dock(int iris, int facing, float gravDir = 1)
    {
        float angle = MathF.Tau * Math.Clamp(iris, 0, Irises - 1) / Irises;
        float side = facing < 0 ? -1 : 1, up = gravDir < 0 ? -1 : 1;
        return new Vector2(side * DockRadius * MathF.Sin(angle), -up * DockRadius * MathF.Cos(angle));
    }

    // 0 at the merge, 1 when iris i docks (it accelerates into its dock).
    internal static float MergeT(float age, int iris) => Strike((age - Merge) / (Docked(iris) - Merge));

    // Where iris i is drawn, relative to the owner's centre: it leaves `bookHole` at its birth and glides (Arrive,
    // with a bulge away from the owner) to its seat, rests there, and from Merge flies along an arc to its dock at
    // `muzzle`. Continuous in age; pellets only leave a seated iris.
    internal static Vector2 IrisOffset(int iris, float age, int facing, float gravDir, Vector2 bookHole, Vector2 muzzle)
    {
        Vector2 seat = Seat(iris, facing, gravDir);
        float up = gravDir < 0 ? -1 : 1;
        float local = age - Birth(iris);
        Vector2 at;
        if (local < ArriveTicks)
        {
            float t = Clamp01(local / ArriveTicks), eased = Arrive(t);
            Vector2 across = seat - bookHole;
            Vector2 normal = across.LengthSquared() > 1e-6f ? Vector2.Normalize(new Vector2(across.Y, -across.X)) : -Vector2.UnitY;
            if (normal.Y * up > 0) normal = -normal;
            at = bookHole + across * eased + normal * (ArrivalBulge * MathF.Sin(MathF.PI * t));
        }
        else at = seat;
        if (age <= Merge) return at;
        Vector2 dock = muzzle + Dock(iris, facing, gravDir);
        Vector2 control = (seat + dock) * .5f + new Vector2(0, -MergeLift * up);
        float m = MergeT(age, iris);
        return (1 - m) * (1 - m) * seat + 2 * (1 - m) * m * control + m * m * dock;
    }

    // ---- Charge and beam ----------------------------------------------------------------------------------------

    internal static float Opening(float age) => Arrive((age - Fire) / OpenTicks);
    internal static float Reach(float age) => Length * Opening(age);
    internal static float Widen(float age) => Smooth((age - WidenStart) / WidenTicks);
    // Collision (and drawn hard-edge) width of the beam body at `age`; 0 before it opens.
    internal static float Width(float age) => Opening(age) * (OpenWidth + (FullWidth - OpenWidth) * Widen(age));
    internal static float CollisionWidth(float along, float width) => along < ThroatLength ? ThroatShare * width : width;
    internal static float TurnCap(float age) => age < Fire ? TurnBuild : TurnBeam + (TurnBeamFull - TurnBeam) * Widen(age);

    // Live: the controller is not fading and the beam has opened (Width > 0 from age Fire + 1).
    internal static bool Live(float age, float fadeState) => fadeState >= 0 && age > Fire && Width(age) > 0;

    // The ring's ratchet: seven clicks in the charge, then one more notch at each widen beat; one step = pi/24.
    internal static int ChargeClick(int tick) => Array.IndexOf(clicks, tick);
    internal static int WidenBeat(int tick) => Array.IndexOf(widenBeats, tick);
    internal static int RatchetSteps(float age)
    {
        int steps = 0;
        foreach (int click in clicks) if (age >= click) steps++;
        foreach (int beat in widenBeats) if (age >= beat) steps++;
        return steps;
    }
    internal const float RatchetStep = MathF.PI / 24;

    // How much of the great aperture's hole is black: grows through the charge and fills it by the last click.
    internal static float VoidDepth(float age) => age < Formed ? 0 : .15f + .85f * MathF.Pow(Clamp01((age - Formed) / (Breath - Formed)), 3);
    // The quiet tension beat: no click, no spiral, nothing moves.
    internal static bool Tension(float age) => age >= Breath && age < Fire;
    // How hard light spirals into the hole: at once half, full by the last click, none in the breath.
    internal static float Spiral(float age) => age < Formed || age >= Breath ? 0 : .5f + .5f * Smooth((age - Formed) / (Breath - Formed));

    // The beam's leading flash: white-hot at the opening, cool violet 10 ticks later.
    internal static float Hot(float age) => age < Fire ? 0 : 1 - Clamp01((age - Fire) / 10);

    // Up to two pulses in flight (px along the beam), or -1.
    internal static void PulseBands(float age, out float first, out float second)
    {
        first = second = -1;
        if (!(age >= PulseFirst)) return;
        int newest = (int)MathF.Floor((age - PulseFirst) / PulsePeriod);
        for (int k = newest; k >= Math.Max(0, newest - 2); k--)
        {
            float along = (age - (PulseFirst + k * PulsePeriod)) * PulseSpeed;
            if (along > Length + PulseTail) continue;
            if (first < 0) first = along;
            else if (second < 0) second = along;
        }
    }

    // ---- Release --------------------------------------------------------------------------------------------------

    // ai[2]: 0 live, -1 .. -FadeTicks fading.
    internal static bool ValidFade(float fadeState) => float.IsFinite(fadeState) && fadeState <= 0 && fadeState >= -FadeTicks;
    // Beam length kept `fade` ticks after a release (it retracts into the hole), and its width share.
    internal static float Retract(float fade) => 1 - Arrive(fade / RetractTicks);
    internal static float RetractWidth(float fade) => 1 - .7f * Clamp01(fade / RetractTicks);
    // During a fade, iris i snaps shut (frames 3 -> 0 over SnapTicks), staggered SnapStagger ticks in reverse order.
    internal const int SnapTicks = 6, SnapStagger = 2;
    internal static int ClosingFrame(int iris, float fade, int openFrame)
    {
        float local = fade - SnapStagger * (Irises - 1 - Math.Clamp(iris, 0, Irises - 1));
        if (local <= 0) return openFrame;
        int closed = (int)MathF.Floor(local / SnapTicks * 4);
        return Math.Max(0, Math.Min(openFrame, 3 - closed));
    }

    // Running dry (LacunaEnd.Starved) looks different from a release: the beam gutters out where it is instead of
    // retracting (lit, a gap, one weaker flash, gone after StarveTicks; under Reduced Effects a plain fade with no
    // flicker, at most 1.5 flashes either way), and the irises or the great aperture crack apart over CollapseTicks
    // instead of snapping shut.
    internal const int StarveTicks = 6;
    internal static float StarveFlicker(float fade, bool reduced)
    {
        if (!(fade >= 0) || fade >= StarveTicks) return 0;
        if (reduced) return 1 - fade / StarveTicks;
        return fade < 2 ? 1 - .1f * fade : fade < 4 ? 0 : .5f;
    }

    // ---- Mana -----------------------------------------------------------------------------------------------------

    // The legacy MagicPays cadence: every 30 ticks during construction, then every 8 from the fire tick.
    internal static bool Pays(int tick) => tick > 0 && (tick < Fire ? tick % BuildManaCadence == 0 : (tick - Fire) % ManaCadence == 0);
    internal static int ManaCost(int modifiedCost, int tick)
        => tick < Fire ? (int)MathF.Ceiling(Math.Max(0, modifiedCost) * BuildManaShare) : Math.Max(0, modifiedCost);

    // ---- Damage -------------------------------------------------------------------------------------------------

    internal static int PelletDamage(int weaponDamage) => RitualArmamentRules.ScaledDamage(weaponDamage, PelletFactor);
    internal static int BeamDamage(int weaponDamage) => RitualArmamentRules.ScaledDamage(weaponDamage, BeamFactor);
    // First tick the beam can land a hit (Opening(Fire) = 0).
    internal const int FirstBeamHit = Fire + 1;

    // Raw damage landing at `age` on one target held in the beam for a channel held from age 1, before defense:
    // pellets on their spawn age, beam hits from FirstBeamHit every HitCadence.
    internal static double RawAt(int age, int weaponDamage)
    {
        double raw = ShotsAt(age) * (double)PelletDamage(weaponDamage);
        if (age >= FirstBeamHit && (age - FirstBeamHit) % HitCadence == 0) raw += BeamDamage(weaponDamage);
        return raw;
    }

    // ---- Cue schedule (client audio, keyed to the accepted age through DollCueClock) ------------------------------

    // The latest birth tick at or before `age` and its iris, or -1.
    internal static int LatestBirth(float age, out int iris)
    {
        iris = -1;
        for (int i = Irises - 1; i >= 0; i--)
            if (age >= births[i]) { iris = i; return births[i]; }
        return -1;
    }

    // The latest first shot (the opening) at or before `age`, or -1.
    internal static int LatestOpening(float age, out int iris)
    {
        iris = -1;
        int best = -1;
        for (int i = 0; i < Irises; i++)
        {
            int tick = births[i] + ShotDelay;
            if (tick <= age && tick > best) { best = tick; iris = i; }
        }
        return best;
    }

    // The latest later shot (or, with `tell`, the latest tell) at or before `age`, or -1.
    internal static int LatestShot(float age, bool tell, out int iris)
    {
        iris = -1;
        if (!float.IsFinite(age)) return -1;
        for (int tick = Math.Min((int)MathF.Floor(age), Merge); tick >= 0; tick--)
        {
            for (int i = 0; i < Irises; i++)
            {
                int shot = tell ? tick + ShotTell : tick;
                if (ShotAt(shot, i) && !IsFirstShot(shot, i)) { iris = i; return tick; }
            }
        }
        return -1;
    }

    // The latest widen beat at or before `age` and its index, or -1.
    internal static int LatestWiden(float age, out int index)
    {
        index = -1;
        for (int k = widenBeats.Length - 1; k >= 0; k--)
            if (age >= widenBeats[k]) { index = k; return widenBeats[k]; }
        return -1;
    }
}
