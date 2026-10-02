#nullable enable
using System;
using System.Numerics;
using static Convergence.Content.Encounters.CrimsonFoundry.Rewards.CrimsonRewardRules;

namespace Convergence.Content.Encounters.CrimsonFoundry.Rewards;

// Pure rules for the Canticle Organ (REWARDS.md, "Ranged - Canticle Organ"; "Implementation shape"): no Terraria or
// XNA references, linked into Tests/Convergence.DomainTests and the offline preview. The numbers live in
// CrimsonRewardRules; this file holds the weapon's own curves, the mark ledger, the hymn schedule, the BoneHand codec
// and the ink it draws (as commands to an ICanticleInk sink, so the game and the offline preview draw the same paths).
internal static class CanticleRules
{
    // ---- Shards ----------------------------------------------------------------------------------------------
    internal const int Pipes = 4;
    // timeLeft counts updates, and a shard has one extra update per tick: 40 ticks of flight.
    internal static int ShardTimeLeft => ShardLife * (1 + ShardExtraUpdates);
    internal static float ShardRange => ShardSpeed * (1 + ShardExtraUpdates) * ShardLife; // 1,600 px
    // The four pipes fire in turn, top to bottom.
    internal static int NextPipe(int pipe) => (Math.Clamp(pipe, 0, Pipes - 1) + 1) % Pipes;

    // Pipe mouths relative to the grip in the gun's own frame: x along the aim, y across (negative is the top of a gun
    // aimed right). The drawn organ is scaled and anchored so its mouths land here (the planned SR03 held organ, 40 x 18
    // logical at the 2 px dot, puts its mouths about 56 px ahead of the grip).
    internal const float MouthForward = 56;
    internal static float MouthAcross(int pipe) => -18 + 5 * Math.Clamp(pipe, 0, Pipes - 1);
    // World offset of a mouth from the grip. A gun aimed left is drawn flipped vertically, so its pipes stay on top.
    internal static Vector2 MouthOffset(int pipe, float aim)
    {
        float c = MathF.Cos(aim), s = MathF.Sin(aim);
        float across = MouthAcross(pipe) * (c < 0 ? -1 : 1);
        return new Vector2(c * MouthForward - s * across, s * MouthForward + c * across);
    }

    // ---- Aim: a critically damped spring toward the cursor -----------------------------------------------------
    internal const float AimOmega = .7f; // per tick: ~92% of a flick in 6 ticks, never past the cursor
    internal static float WrapAngle(float angle) => float.IsFinite(angle) ? MathF.IEEERemainder(angle, MathF.Tau) : 0;
    // One tick of the exact critically damped solution (dt = 1), measured the short way round the circle.
    internal static (float Angle, float Velocity) SpringAim(float angle, float velocity, float target, float omega = AimOmega)
    {
        if (!float.IsFinite(angle) || !float.IsFinite(velocity) || !float.IsFinite(target)) return (WrapAngle(target), 0);
        float d = WrapAngle(angle - target), e = MathF.Exp(-omega), k = velocity + omega * d;
        return (WrapAngle(target + (d + k) * e), (velocity - omega * k) * e);
    }
    // Draw interpolation between two aims, the short way round.
    internal static float LerpAngle(float from, float to, float t) => WrapAngle(from + WrapAngle(to - from) * Math.Clamp(t, 0, 1));

    // ---- Pose ------------------------------------------------------------------------------------------------
    // The firing pipe kicks back KickDistance px and springs home, critically damped (it never passes its rest).
    internal const float KickDistance = 2, KickOmega = .6f;
    internal static float Kick(float ticksSinceShot)
        => ticksSinceShot < 0 || !float.IsFinite(ticksSinceShot) ? 0 : KickDistance * (1 + KickOmega * ticksSinceShot) * MathF.Exp(-KickOmega * ticksSinceShot);
    // The hymn: the organ lifts and inhales over HymnInhale ticks while the heart-gem brightens, then settles back by
    // the end of the 24-tick use. Inhale is 0..1; HymnLift radians at full inhale.
    internal const float HymnLift = .5f;
    internal static float Inhale(float ticksSinceCast)
    {
        float t = ticksSinceCast;
        if (!float.IsFinite(t) || t <= 0 || t >= HymnUseTicks) return 0;
        return t < HymnInhale ? Smooth(t / HymnInhale) : 1 - Smooth((t - HymnInhale) / (HymnUseTicks - HymnInhale));
    }

    // ---- BoneHand codec ----------------------------------------------------------------------------------------
    // ai[1] = ordinal (0-15, the hand's place in the hymn) + MaxHands x role. The Clasp is a fully marked NPC's eighth
    // hand; Cadence marks the last hand of a hymn without a Clasp, so every peer plays the cadence with it.
    internal enum HandRole : byte { Hand, Clasp, Cadence }
    internal const int HandRoles = 3;
    internal static int MaxHandCode => MaxHands * HandRoles - 1;
    internal static float HandCode(int ordinal, HandRole role)
    {
        if (ordinal < 0 || ordinal >= MaxHands || (int)role >= HandRoles) throw new ArgumentOutOfRangeException(nameof(ordinal));
        return ordinal + MaxHands * (int)role;
    }
    internal static bool TryHand(float code, out int ordinal, out HandRole role)
    {
        ordinal = 0; role = HandRole.Hand;
        if (!ValidInteger(code, 0, MaxHandCode)) return false;
        int value = (int)code;
        ordinal = value % MaxHands; role = (HandRole)(value / MaxHands);
        return true;
    }

    // ai[2] = age, the tick relative to the hand's slam (0); a negative age is the wait. The owner spawns every hand at
    // the cast with SpawnAge, and each AI tick adds one before reading it, so hand k slams HandSlamTick(k) ticks after
    // the cast. A hand lives until HandTail, so the last of 16 is gone HandMaxLife (145) ticks after the cast.
    internal static int HandTail => HandMaxLife - HandSlamTick(MaxHands - 1); // 30: live 3, dry 24, slack
    internal static int SpawnAge(int ordinal) => -HandSlamTick(Math.Clamp(ordinal, 0, MaxHands - 1)) - 1;
    internal static bool ValidAge(float age) => ValidInteger(age, SpawnAge(MaxHands - 1), HandTail);
    // Live on ticks 1-3 after the slam: radius x InkOpen(1, 2, 3) = 1/3, 2/3, full.
    internal static bool IsLive(int age) => age >= 1 && age <= HandLive;
    // A hand has started once it lands; the owner's death or Down retires only the hands that have not.
    internal static bool Landed(int age) => age >= 0;
    internal static bool RetireOnOwnerLoss(int age) => !Landed(age);
    internal static bool Expired(int age) => age > HandTail;
    // Hands exist only from HandLead (8) ticks before their slam: no hand ever hovers over an enemy.
    internal static bool Visible(float age) => age >= -HandLead && age <= HandTail;
    // Pending: a hymn whose hands have not all landed blocks the next cast.
    internal static bool Pending(int age) => age < 0;

    // Height of the palm above its landing point: held at HandDrop while it condenses (HandForm ticks), then a fall
    // that starts at rest and accelerates (quadratic in time) onto the landing point on the slam.
    internal static float FallHeight(float age)
    {
        float u = Math.Clamp((age + HandFall) / HandFall, 0, 1);
        return HandDrop * (1 - u * u);
    }
    internal static float Forming(float age) => Math.Clamp((age + HandLead) / HandForm, 0, 1);

    // Each hand lands HandOffset(ordinal, width) from its NPC's centre; the Clasp lands on the centre itself.
    internal static float LandingOffset(int ordinal, HandRole role, float npcWidth)
        => role == HandRole.Clasp ? 0 : HandOffset(ordinal, Math.Max(0, npcWidth));
    // The Clasp's four hands fall together from the four arm offsets and close on the target as they fall.
    internal const float ClaspClose = .3f;
    internal static float ClaspHandOffset(int hand, float age, float npcWidth)
    {
        float from = HandOffset(hand, Math.Max(0, npcWidth));
        float u = Math.Clamp((age + HandFall) / HandFall, 0, 1);
        return from * (1 - (1 - ClaspClose) * Smooth(u));
    }

    // Collision and drawing share these: an N px disc is radius N, opening over InkOpenTicks (draw equals collide).
    internal static float SlamRadius(bool clasp, float age) => (clasp ? ClaspRadius : HandRadius) * InkOpen(age);
    internal static float CrownRadiusAt(float age) => CrownRadius * InkOpen(age);
    // The splash crown: six radial capsules from CrownInner to CrownOuter, the first straight up, 60 degrees apart.
    internal static Vector2 CrownDirection(int spoke)
    {
        float a = -MathF.PI / 2 + spoke * MathF.Tau / CrownSpokes;
        return new Vector2(MathF.Cos(a), MathF.Sin(a));
    }
    // Does the slam at `palm` touch the hitbox (min, max) on this integer age?
    internal static bool SlamTouches(Vector2 palm, bool clasp, int age, Vector2 min, Vector2 max)
    {
        if (!IsLive(age)) return false;
        if (DiscTouchesBox(palm, SlamRadius(clasp, age), min, max)) return true;
        if (!clasp) return false;
        float r = CrownRadiusAt(age);
        for (int i = 0; i < CrownSpokes; i++)
        {
            Vector2 d = CrownDirection(i);
            if (CapsuleTouchesBox(palm + d * CrownInner, palm + d * CrownOuter, r, min, max)) return true;
        }
        return false;
    }
    // Damage multiplier of one slam hit: a hand x2.25 to every root it touches; the Clasp x4.5 to its target's root and
    // x1.0 to any other root its disc or crown touches.
    internal static float SlamMultiplier(HandRole role, bool targetRoot)
        => role == HandRole.Clasp ? (targetRoot ? ClaspMultiplier : CrownMultiplier) : HandMultiplier;

    // ---- Marks (owner only) -------------------------------------------------------------------------------------
    // The owner's view: one short dormant ink arc above each marked NPC, its width swelling into a note-head at each
    // mark. Eight slots centred over the NPC; the arc is written out to the newest mark.
    internal const float MarkLift = 24, MarkSlotGap = 12, MarkBow = 7, MarkTail = 7, MarkBody = 2.6f, MarkHead = 8.5f, MarkHeadWidth = 3.4f;
    internal const float MarkSampleGap = 2.5f, MarkFlarePeak = .9f, MarkFlareTicks = 4, MarkFadeTicks = 30;
    internal static float MarkSlotX(int slot) => (slot - (MaxMarksPerNpc - 1) * .5f) * MarkSlotGap;
    internal static float MarkArcY(float x)
    {
        float half = -MarkSlotX(0) + MarkTail;
        float s = Math.Clamp(x / half, -1, 1);
        return -MarkBow * (1 - s * s);
    }
    // Arc width at x for `marks` marks; the newest head is scaled by `flare`.
    internal static float MarkRadius(float x, int marks, float flare)
    {
        float r = MarkBody;
        marks = Math.Clamp(marks, 0, MaxMarksPerNpc);
        for (int i = 0; i < marks; i++)
        {
            float d = (x - MarkSlotX(i)) / MarkHeadWidth;
            float head = MarkHead * (i == marks - 1 ? flare : 1);
            r = MathF.Max(r, head * MathF.Exp(-d * d));
        }
        return r;
    }
    // The newest head flares as it lands and settles in a few ticks (no period).
    internal static float MarkFlare(float ticksSinceNewest)
        => ticksSinceNewest < 0 || !float.IsFinite(ticksSinceNewest) ? 1 : 1 + MarkFlarePeak * MathF.Exp(-ticksSinceNewest / MarkFlareTicks);
    // Marks fade out over their last MarkFadeTicks instead of popping when they expire.
    internal static float MarkOpacity(float ticksSinceNewest)
        => Smooth((MarkLife - ticksSinceNewest) / MarkFadeTicks);

    // ---- Ink (shared by the game and the offline preview) -----------------------------------------------------------
    internal const float ShardWake = 4, ShardTail = 1.4f;
    internal const float FormSmear = 14, ClaspSmear = 34, FallSmearLag = 1.6f, FallSmearMax = 56;
    internal const int Residue = 24, DropletLife = 18, HandDroplets = 6, ClaspDroplets = 10;

    // The shard's wake: a short live path over its last positions (oldest first), radius 4 at the shard, all of it
    // inside the path it really flew. Live look, already open (t >= 3), cooling toward the tail.
    internal static void ShardInk(ICanticleInk ink, ReadOnlySpan<Vector2> trail, float seed)
    {
        if (trail.Length < 2 || !ink.Begin(CanticleInkLook.Live, seed)) return;
        for (int i = 0; i < trail.Length; i++)
        {
            float s = i / (trail.Length - 1f);
            ink.Point(trail[i], ShardTail + (ShardWake - ShardTail) * s, 6 - 2.6f * s);
        }
        ink.End();
    }

    // One NPC's mark arc. `anchor` is the arc's centre (MarkLift above the NPC's top).
    internal static void MarkInk(ICanticleInk ink, Vector2 anchor, int marks, float ticksSinceNewest, float seed)
    {
        marks = Math.Clamp(marks, 0, MaxMarksPerNpc);
        float opacity = MarkOpacity(ticksSinceNewest);
        if (marks == 0 || opacity <= .01f) return;
        if (!ink.Begin(CanticleInkLook.Dormant, seed, opacity, marks == MaxMarksPerNpc ? 1 : 0)) return;
        float from = MarkSlotX(0) - MarkTail, to = MarkSlotX(marks - 1) + MarkTail, flare = MarkFlare(ticksSinceNewest);
        int n = Math.Clamp((int)MathF.Ceiling((to - from) / MarkSampleGap) + 1, 2, 64);
        for (int i = 0; i < n; i++)
        {
            float x = from + (to - from) * i / (n - 1);
            ink.Point(anchor + new Vector2(x, MarkArcY(x)), MarkRadius(x, marks, flare), 0);
        }
        ink.End();
    }

    // Everything one BoneHand draws in ink at a (fractional) age. `landing` is its landing point: the NPC's centre
    // plus LandingOffset before the slam, then the replicated slam point.
    //   -8..-6   the forming smear condenses 240 px above (2 ticks, written with a bead), then fades as the hand falls
    //   -6..0    a short dormant smear trails each falling hand
    //    0..3    live disc (48, the Clasp 64 plus its crown) with its ignition blaze: exactly the collision footprint
    //    3..27   it dries into a residue scar; droplets leap and fall for 18 ticks
    internal static void HandInk(ICanticleInk ink, Vector2 landing, int ordinal, HandRole role, float npcWidth, float age, float seed, bool reduced)
    {
        if (!float.IsFinite(age) || !Visible(age)) return;
        bool clasp = role == HandRole.Clasp;
        Vector2 top = landing - new Vector2(0, HandDrop);
        if (age < -1)
        {
            float form = Forming(age), fade = 1 - Smooth((age + HandFall) / (HandFall - 1f));
            float half = (clasp ? ClaspSmear : FormSmear) * (.35f + .65f * form);
            if (fade > .01f && ink.Begin(CanticleInkLook.Dormant, seed, fade))
            {
                const int n = 9;
                for (int i = 0; i < n; i++)
                {
                    float s = i / (n - 1f), x = (s * 2 - 1) * half;
                    ink.Point(top + new Vector2(x, -4 * (1 - (s * 2 - 1) * (s * 2 - 1))), 3 + 2.5f * (1 - MathF.Abs(s * 2 - 1)), 0);
                }
                ink.End(bead: form < 1);
            }
        }
        if (age > -HandFall && age < 0)
        {
            int bodies = clasp ? ClaspHands : 1;
            for (int b = 0; b < bodies; b++)
            {
                Vector2 palm = landing + new Vector2(clasp ? ClaspHandOffset(b, age, npcWidth) : 0, -FallHeight(age));
                float back = age - FallSmearLag;
                Vector2 tail = landing + new Vector2(clasp ? ClaspHandOffset(b, back, npcWidth) : 0, -FallHeight(back));
                Vector2 along = tail - palm;
                float length = along.Length();
                if (length < 2) continue;
                if (length > FallSmearMax) tail = palm + along * (FallSmearMax / length);
                Line(ink, CanticleInkLook.Dormant, seed + b * .37f, tail, palm, 2, 5, 0, 0);
            }
        }
        if (age < 0) return;
        float radius = clasp ? ClaspRadius : HandRadius;
        if (age < HandLive)
        {
            Disc(ink, CanticleInkLook.Live, seed, landing, radius, age);
            if (clasp)
                for (int i = 0; i < CrownSpokes; i++)
                {
                    Vector2 d = CrownDirection(i);
                    Line(ink, CanticleInkLook.Live, seed + i, landing + d * CrownInner, landing + d * CrownOuter, CrownRadius, CrownRadius, age, age);
                }
        }
        else if (age < HandLive + Residue)
        {
            float fade = 1 - (age - HandLive) / Residue;
            Disc(ink, CanticleInkLook.Residue, seed, landing, radius, fade);
            if (clasp)
                for (int i = 0; i < CrownSpokes; i++)
                {
                    Vector2 d = CrownDirection(i);
                    Line(ink, CanticleInkLook.Residue, seed + i, landing + d * CrownInner, landing + d * CrownOuter, CrownRadius, CrownRadius, fade, fade);
                }
        }
        if (age > .5f && age < DropletLife)
        {
            int count = ReducedCount(clasp ? ClaspDroplets : HandDroplets, reduced);
            for (int i = 0; i < count; i++)
            {
                Vector2 now = Droplet(landing, radius, i, seed, age), before = Droplet(landing, radius, i, seed, Math.Max(0, age - 1.5f));
                ink.Droplet(before, now, 2f, age, seed + i);
            }
        }
    }

    // A black-blood droplet leaping from the slam's lip and falling back (gravity .32 px/tick^2).
    internal static Vector2 Droplet(Vector2 landing, float radius, int i, float seed, float t)
    {
        float h1 = Hash(seed, i * 3 + 1), h2 = Hash(seed, i * 3 + 2);
        float angle = -MathF.PI / 2 + (h1 - .5f) * 2.4f, speed = 3.2f + 2.6f * h2;
        Vector2 dir = new(MathF.Cos(angle), MathF.Sin(angle));
        return landing + dir * (radius * .55f) + dir * (speed * t) + new Vector2(0, .32f * t * t);
    }

    internal static void Line(ICanticleInk ink, CanticleInkLook look, float seed, Vector2 a, Vector2 b, float radiusA, float radiusB, float timeA, float timeB)
    {
        if (!ink.Begin(look, seed)) return;
        int n = Math.Clamp((int)MathF.Ceiling(Vector2.Distance(a, b) / 10f) + 1, 2, 64);
        for (int i = 0; i < n; i++)
        {
            float s = i / (n - 1f);
            ink.Point(Vector2.Lerp(a, b, s), radiusA + (radiusB - radiusA) * s, timeA + (timeB - timeA) * s);
        }
        ink.End();
    }

    internal static void Disc(ICanticleInk ink, CanticleInkLook look, float seed, Vector2 center, float radius, float time)
    {
        if (!ink.Begin(look, seed)) return;
        ink.Point(center, radius, time);
        ink.End();
    }

    // Deterministic 0..1 hash (no shared clock, no period).
    internal static float Hash(float seed, int salt)
    {
        uint h = unchecked((uint)BitConverter.SingleToInt32Bits(seed) * 0x9E3779B1u ^ (uint)salt * 0x85EBCA77u);
        h ^= h >> 15; h = unchecked(h * 0x2C1B3C6Du); h ^= h >> 12; h = unchecked(h * 0x297A2D39u); h ^= h >> 15;
        return (h & 0xFFFFFF) / 16777216f;
    }
}

// Which ScarletInk path pass draws a path; the client maps it onto ScarletInkLook.
internal enum CanticleInkLook : byte { Live, Dormant, Residue }

// Where the organ's ink goes: the game's ScarletInkCanvas (Client) or a test/preview recorder. Positions are world
// pixels; time is ticks since ignition (live), the remaining fade 1..0 (residue) or unused (dormant).
internal interface ICanticleInk
{
    bool Begin(CanticleInkLook look, float seed, float opacity = 1, float warmth = 0);
    void Point(Vector2 at, float radius, float time);
    void End(bool bead = false);
    void Droplet(Vector2 from, Vector2 to, float radius, float time, float seed);
}

// One mark entry: the NPC root slot with the type and incarnation that earned it, its marks, the tick of its newest
// mark (expiry and eviction) and the order of its first mark (the hymn's round-robin).
internal readonly record struct CanticleMark(int Root, int Type, ulong Incarnation, int Count, ulong Newest, long Order);

// What one shard hit did: the NPC's marks now, whether a mark was added (false at the cap, which only refreshes its
// expiry), and the root whose marks were evicted to make room (-1 for none).
internal readonly record struct CanticleMarkResult(int Count, bool Added, int Evicted);

// One hand of a hymn: the ledger entry it spends a mark of, the NPC root, its ordinal and its role.
internal readonly record struct CanticleHand(int Entry, int Root, int Ordinal, CanticleRules.HandRole Role);

// The owner's mark ledger (REWARDS.md, "Marks"): up to MaxMarksPerNpc marks on each of up to MaxMarkedNpcs NPCs,
// keyed by root, type and incarnation. Marking a ninth NPC drops the NPC whose newest mark is oldest. Marks expire
// MarkLife ticks after that NPC's newest mark. Entries are kept in the order of their first mark. Fixed arrays only.
internal sealed class CanticleMarks
{
    private readonly CanticleMark[] entries = new CanticleMark[CrimsonRewardRules.MaxMarkedNpcs];
    private int count;
    private long order;

    internal int Count => count;
    internal CanticleMark this[int index] => (uint)index < (uint)count ? entries[index] : throw new ArgumentOutOfRangeException(nameof(index));
    internal int Total
    {
        get
        {
            int total = 0;
            for (int i = 0; i < count; i++) total += entries[i].Count;
            return total;
        }
    }

    internal int IndexOf(int root)
    {
        for (int i = 0; i < count; i++) if (entries[i].Root == root) return i;
        return -1;
    }
    internal int MarksOn(int root) { int i = IndexOf(root); return i < 0 ? 0 : entries[i].Count; }

    internal CanticleMarkResult Mark(int root, int type, ulong incarnation, ulong now)
    {
        if (root < 0) return new CanticleMarkResult(0, false, -1);
        int evicted = -1, i = IndexOf(root);
        // The slot now holds a different NPC: its marks were never this one's.
        if (i >= 0 && (entries[i].Type != type || entries[i].Incarnation != incarnation)) { RemoveAt(i); i = -1; }
        if (i < 0)
        {
            if (count >= entries.Length)
            {
                int stale = Stalest();
                evicted = entries[stale].Root;
                RemoveAt(stale);
            }
            i = count++;
            entries[i] = new CanticleMark(root, type, incarnation, 0, now, order++);
        }
        var e = entries[i];
        bool added = e.Count < CrimsonRewardRules.MaxMarksPerNpc;
        entries[i] = e with { Count = Math.Min(CrimsonRewardRules.MaxMarksPerNpc, e.Count + 1), Newest = now };
        return new CanticleMarkResult(entries[i].Count, added, evicted);
    }

    // The entry whose newest mark is oldest; ties go to the earliest first mark.
    private int Stalest()
    {
        int best = 0;
        for (int i = 1; i < count; i++) if (entries[i].Newest < entries[best].Newest) best = i;
        return best;
    }

    // Drop every entry whose newest mark is MarkLife ticks old (or from a clock that went backwards).
    internal int Expire(ulong now)
    {
        int removed = 0;
        for (int i = count - 1; i >= 0; i--)
            if (now < entries[i].Newest || now - entries[i].Newest >= (ulong)CrimsonRewardRules.MarkLife) { RemoveAt(i); removed++; }
        return removed;
    }

    internal void RemoveAt(int index)
    {
        if ((uint)index >= (uint)count) return;
        for (int i = index; i < count - 1; i++) entries[i] = entries[i + 1];
        entries[--count] = default;
    }

    internal void Clear()
    {
        Array.Clear(entries);
        count = 0;
    }

    // The hymn (REWARDS.md, "Schedule"): one hand per mark, round-robin over the marked NPCs in the order of their first
    // mark, at most MaxHands; entries out of range (inRange[i] false) are skipped and keep their marks. A fully marked
    // NPC's eighth hand is the Clasp; the last hand of a hymn without one carries the cadence. Returns the hand count.
    internal int Plan(ReadOnlySpan<bool> inRange, Span<CanticleHand> hands)
    {
        int limit = Math.Min(hands.Length, CrimsonRewardRules.MaxHands), n = 0;
        for (int round = 0; round < CrimsonRewardRules.MaxMarksPerNpc && n < limit; round++)
            for (int i = 0; i < count && n < limit; i++)
            {
                if (i >= inRange.Length || !inRange[i] || entries[i].Count <= round) continue;
                var role = round == CrimsonRewardRules.MaxMarksPerNpc - 1 ? CanticleRules.HandRole.Clasp : CanticleRules.HandRole.Hand;
                hands[n] = new CanticleHand(i, entries[i].Root, n, role);
                n++;
            }
        if (n > 0 && hands[n - 1].Role != CanticleRules.HandRole.Clasp) hands[n - 1] = hands[n - 1] with { Role = CanticleRules.HandRole.Cadence };
        return n;
    }

    // Spend the marks a plan used; NPCs left with none leave the ledger, extra marks stay with their own expiry.
    internal void Spend(ReadOnlySpan<CanticleHand> hands)
    {
        Span<int> spent = stackalloc int[CrimsonRewardRules.MaxMarkedNpcs];
        foreach (var hand in hands)
            if ((uint)hand.Entry < (uint)count && entries[hand.Entry].Root == hand.Root) spent[hand.Entry]++;
        for (int i = count - 1; i >= 0; i--)
        {
            if (spent[i] == 0) continue;
            int left = entries[i].Count - spent[i];
            if (left <= 0) RemoveAt(i);
            else entries[i] = entries[i] with { Count = left };
        }
    }
}
