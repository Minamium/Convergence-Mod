#nullable enable
using System;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// The six heart beads on the back of Lacrimosa's hands: one meter per player (the owner's bookkeeping; the kata
// controller replicates the lit count). 360 units, 60 per bead.
//  - A stroke that connects adds 12 units (a rake) or 24 (the clap), once per stroke however many NPCs it hits,
//    times the streak's tempo (x1 for its first two connecting strokes, x1.25 for the 3rd-5th, x1.5 from the 6th)
//    and times (stroke ticks / base ticks), so attack speed never changes the fill per second. A streak ends
//    StreakWindow ticks after its last connecting stroke and restarts after every grasp.
//  - While the claws are held and usable and no grasp is active, the meter also gains 1 unit every 4 ticks
//    (empty to full in 24 s without a single hit).
//  - Frozen while unequipped, unusable (Down, crowd control) or grasping; death and world entry empty it.
// Pure: linked into the domain tests.
internal sealed class LacrimosaHeart
{
    internal const int UnitsPerBead = 60, Beads = 6, MaxUnits = UnitsPerBead * Beads, PassiveEvery = 4, StreakWindow = 75;

    private int passive, streak;
    private ulong lastLand, lastSerial;
    private bool landed, hasSerial;

    internal int Units { get; private set; }
    internal int Lit => Units / UnitsPerBead;
    internal bool Ready => Units >= MaxUnits;
    internal int Streak => streak;

    internal static int StrokeUnits(int stroke) => stroke == LacrimosaClawMotion.Clap ? 24 : 12;
    // Tempo in quarters: 4 (x1), 5 (x1.25), 6 (x1.5).
    internal static int TempoQuarters(int streak) => streak < 2 ? 4 : streak < 5 ? 5 : 6;

    internal void Tick(bool held, bool usable, bool grasping)
    {
        if (!held || !usable || grasping || Ready) return;
        if (++passive < PassiveEvery) return;
        passive = 0;
        Units = Math.Min(MaxUnits, Units + 1);
    }

    // A stroke connected. Serial: the stroke's identity (a repeat is ignored). Returns the units added.
    internal int Land(ulong serial, int stroke, int duration, ulong now)
    {
        if (hasSerial && serial == lastSerial) return 0;
        hasSerial = true;
        lastSerial = serial;
        stroke = LacrimosaClawMotion.Stroke(stroke);
        if (landed && (now < lastLand || now - lastLand > StreakWindow)) streak = 0;
        int quarters = TempoQuarters(streak);
        streak++;
        lastLand = now;
        landed = true;
        int ticks = LacrimosaClawMotion.ValidDuration(stroke, duration);
        int add = StrokeUnits(stroke) * quarters * ticks / (4 * LacrimosaClawMotion.BaseTicks(stroke));
        add = Math.Clamp(add, 0, MaxUnits - Units);
        Units += add;
        return add;
    }

    // Spends a full meter on a grasp; the streak restarts.
    internal bool Spend()
    {
        if (!Ready) return false;
        Units = 0;
        passive = 0;
        streak = 0;
        landed = false;
        return true;
    }

    internal void Reset()
    {
        Units = 0;
        passive = 0;
        streak = 0;
        landed = false;
        hasSerial = false;
        lastSerial = 0;
        lastLand = 0;
    }
}

// The kata counter: A -> B -> C -> A. The next press after ComboResetTicks without a stroke (counted from the
// last stroke's end) starts at A again, and so does the first stroke after a grasp. Owner bookkeeping only.
internal sealed class LacrimosaCombo
{
    private int next;
    private ulong idleFrom;
    private bool used;

    // A clock that went backwards (a new world) also restarts at A.
    internal int Peek(ulong now)
        => !used || now > idleFrom + LacrimosaClawMotion.ComboResetTicks || idleFrom > now + LacrimosaClawMotion.MaxTicks
            ? LacrimosaClawMotion.RakeDown : next;

    internal int Take(ulong now, Func<int, int> duration)
    {
        ArgumentNullException.ThrowIfNull(duration);
        int stroke = Peek(now);
        next = (stroke + 1) % LacrimosaClawMotion.Strokes;
        idleFrom = now + (ulong)Math.Max(0, duration(stroke));
        used = true;
        return stroke;
    }

    internal void Restart() => used = false;

    internal void Reset()
    {
        next = LacrimosaClawMotion.RakeDown;
        idleFrom = 0;
        used = false;
    }
}

// The claws' nominal score: what the controller does tick by tick for a player who holds the attack on one target
// that every stroke reaches and grasps `graspDelay` ticks after the beads fill. Raw damage before defense, no crit,
// one target. Tick order follows the game: the owner's item use (a grasp, else the next stroke once the last one
// has finished and no grasp holds the hands), then the meter (ModPlayer.PostUpdate), then the projectiles' AI
// (age + 1; a stroke lands on its first live age, the grasp's contact and crush on theirs). Tick 0 is the press:
// the claws are already held, the beads empty, the combo fresh.
internal static class LacrimosaClawScore
{
    internal static int[] Timeline(int ticks, int graspDelay = 0, float speed = 1, int damage = LacrimosaClawMotion.BaseDamage,
        bool hits = true, int[]? graspTicks = null)
    {
        if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
        var raw = new int[ticks];
        var heart = new LacrimosaHeart();
        var combo = new LacrimosaCombo();
        int stroke = LacrimosaClawMotion.None, age = 0, duration = 0, grasp = -1, readyAt = -1, grasps = 0;
        ulong serial = 0;
        bool landed = false;
        for (int tick = 0; tick < ticks; tick++)
        {
            bool graspBusy = grasp >= 0 && LacrimosaClawMotion.GraspBusy(grasp);
            if (heart.Ready && readyAt < 0) readyAt = tick;
            if (!graspBusy && heart.Ready && tick >= readyAt + graspDelay && heart.Spend())
            {
                grasp = 0;
                graspBusy = true;
                readyAt = -1;
                stroke = LacrimosaClawMotion.None;
                combo.Restart();
                if (graspTicks is not null && grasps < graspTicks.Length) graspTicks[grasps] = tick;
                grasps++;
            }
            else if (!graspBusy && (stroke == LacrimosaClawMotion.None || age >= duration))
            {
                stroke = combo.Take((ulong)tick, s => LacrimosaClawMotion.Duration(s, speed));
                duration = LacrimosaClawMotion.Duration(stroke, speed);
                age = 0;
                serial++;
                landed = false;
            }
            heart.Tick(true, true, graspBusy);
            if (stroke != LacrimosaClawMotion.None && age < duration)
            {
                age++;
                if (hits && !landed && age == LacrimosaClawMotion.FirstLiveAge(stroke, duration))
                {
                    landed = true;
                    raw[tick] += RitualArmamentRules.ScaledDamage(damage, LacrimosaClawMotion.Multiplier(stroke));
                    heart.Land(serial, stroke, duration, (ulong)tick);
                }
            }
            if (grasp >= 0)
            {
                grasp++;
                if (hits && grasp == LacrimosaClawMotion.GraspContact)
                    raw[tick] += RitualArmamentRules.ScaledDamage(damage, LacrimosaClawMotion.ContactMultiplier);
                if (hits && grasp == LacrimosaClawMotion.GraspCrush)
                    raw[tick] += RitualArmamentRules.ScaledDamage(damage, LacrimosaClawMotion.CrushMultiplier);
                if (grasp >= LacrimosaClawMotion.GraspEnd) grasp = -1;
            }
        }
        return raw;
    }
}
