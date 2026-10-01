using System;
using System.Numerics;
using Convergence.Content.Encounters.EbonManor.Rewards;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Ebon Thimble lifts one piece per beat, starting at six ticks, and stops at eight")]
    private static void EbonThimbleLiftTiming()
    {
        AssertEqual(false, EbonThimbleScore.LiftDue(5, 0), "no piece before the first six ticks");
        AssertEqual(true, EbonThimbleScore.LiftDue(6, 0), "the first piece rises at six ticks");
        int previous = EbonRewardRules.LiftTick(0);
        for (int piece = 1; piece < EbonRewardRules.Pieces; piece++)
        {
            int tick = EbonRewardRules.LiftTick(piece);
            AssertEqual(true, tick - previous is 28 or 29, "one piece per beat (28.8 ticks)");
            AssertEqual(false, EbonThimbleScore.LiftDue(tick - 1, piece), "a piece never rises early");
            AssertEqual(true, EbonThimbleScore.LiftDue(tick, piece), "a piece rises on its beat");
            previous = tick;
        }
        AssertEqual(false, EbonThimbleScore.LiftDue(100_000, EbonRewardRules.Pieces), "a ninth piece is never lifted");
        AssertEqual(false, EbonThimbleScore.LiftDue(100_000, -1), "invalid counts lift nothing");
        AssertEqual(true, EbonThimbleScore.Finale(EbonRewardRules.Pieces), "eight raised pieces arm the finale");
        AssertEqual(false, EbonThimbleScore.Finale(EbonRewardRules.Pieces - 1), "seven do not");
    }

    [DomainTest("Ebon Thimble yanks one piece per sixteenth and drops the piano two sixteenths after the last")]
    private static void EbonThimbleYankTiming()
    {
        AssertEqual(0, EbonRewardRules.YankTick(0), "the first piece leaves on the release itself");
        int previous = 0;
        for (int piece = 1; piece < EbonRewardRules.Pieces; piece++)
        {
            int tick = EbonRewardRules.YankTick(piece);
            AssertEqual(true, tick - previous is 7 or 8, "one yank per sixteenth (7.2 ticks)");
            AssertEqual(true, EbonThimbleScore.LaunchDue(tick, piece) && !EbonThimbleScore.LaunchDue(tick - 1, piece), "launch on its own tick");
            previous = tick;
        }
        int gap = EbonThimbleScore.PianoAppears - EbonRewardRules.YankTick(EbonRewardRules.Pieces - 1);
        AssertEqual(true, gap is 14 or 15, "the piano appears two sixteenths after the last piece");
        AssertEqual(EbonThimbleScore.LastLaunch(EbonRewardRules.Pieces) + EbonThimbleScore.RecoveryTicks,
            EbonThimbleScore.VolleyEnd(EbonRewardRules.Pieces), "the throw pose lasts until the last yank has settled");
        AssertEqual(0, EbonThimbleScore.LastLaunch(0), "an empty volley has no launches");
    }

    [DomainTest("Ebon Thimble yank accelerates from 10 px per tick to the 40 px cap")]
    private static void EbonThimbleYankSpeed()
    {
        float speed = EbonRewardRules.YankSpeed, previous = speed;
        int ticks = 0;
        while (speed < EbonRewardRules.YankMax)
        {
            speed = EbonThimbleScore.NextSpeed(speed);
            AssertEqual(true, speed > previous && speed <= EbonRewardRules.YankMax, "monotone and capped");
            previous = speed;
            ticks++;
            AssertEqual(true, ticks < 100, "reaches the cap");
        }
        AssertEqual(EbonRewardRules.YankMax, EbonThimbleScore.NextSpeed(EbonRewardRules.YankMax), "the cap holds");
        AssertEqual(true, ticks is 25 or 26, "10 + 1.2 t reaches 40 px per tick after about 25 ticks");
        AssertEqual(25, EbonThimbleScore.TicksToCover(600, EbonRewardRules.YankSpeed, EbonRewardRules.YankAcceleration, EbonRewardRules.YankMax),
            "a piece crosses a screen of 600 px in 25 ticks");
        AssertEqual(true, EbonThimbleScore.NextSpeed(0) <= EbonRewardRules.YankAcceleration + .001f, "a stalled piece does not jump back to full speed");
    }

    [DomainTest("Ebon Thimble grand piano falls its 520 px in half a second and lands at the cursor height")]
    private static void EbonThimblePianoFall()
    {
        AssertEqual(30, EbonThimbleScore.TicksToCover(EbonRewardRules.PianoHeight, EbonRewardRules.PianoSpeed,
            EbonRewardRules.PianoGravity, EbonThimbleScore.PianoMaxSpeed), "6 px/tick plus 0.8 px/tick^2 over 520 px");
        AssertEqual(65, EbonThimbleScore.PianoAppears, "the piano appears nine sixteenths after the release");
        float v = EbonRewardRules.PianoSpeed;
        for (int i = 0; i < 200; i++) v = EbonThimbleScore.NextFall(v);
        AssertEqual(EbonThimbleScore.PianoMaxSpeed, v, "terminal speed stops tile tunnelling");

        var (start, targetY) = EbonThimbleScore.PianoSpawn(new Vector2(2000, 3000), 100_000, 20_000);
        AssertEqual(2000f, start.X, "directly above the release cursor");
        AssertEqual(3000f, targetY, "lands at the cursor height");
        AssertEqual(EbonRewardRules.PianoHeight, targetY - start.Y, "waits 520 px above the cursor");
        var (cornerStart, cornerTarget) = EbonThimbleScore.PianoSpawn(new Vector2(-50, 20), 100_000, 20_000);
        AssertEqual(true, cornerStart.X >= EbonThimbleScore.PianoMargin && cornerStart.Y >= EbonThimbleScore.PianoMargin
            && cornerTarget >= EbonThimbleScore.PianoMargin, "clamped inside the world");
        AssertEqual(false, EbonThimbleScore.PianoLanded(2999, 3000), "still falling above the cursor height");
        AssertEqual(true, EbonThimbleScore.PianoLanded(3000, 3000), "lands on reaching the cursor height");
    }

    [DomainTest("Ebon Thimble piano crash hits a 220 px circle")]
    private static void EbonThimblePianoCircle()
    {
        float r = EbonRewardRules.PianoRadius;
        AssertEqual(true, EbonThimbleScore.CircleTouchesBox(Vector2.Zero, r, new Vector2(r - 1, -10), new Vector2(r + 40, 10)),
            "a body whose edge is inside the radius is hit");
        AssertEqual(false, EbonThimbleScore.CircleTouchesBox(Vector2.Zero, r, new Vector2(r + 1, -10), new Vector2(r + 40, 10)),
            "a body just outside is not");
        AssertEqual(true, EbonThimbleScore.CircleTouchesBox(Vector2.Zero, r, new Vector2(-5, -5), new Vector2(5, 5)),
            "a body at the centre is hit");
        AssertEqual(false, EbonThimbleScore.CircleTouchesBox(Vector2.Zero, r, new Vector2(r * .75f, r * .75f), new Vector2(r, r)),
            "the circle is not its bounding square");
    }

    [DomainTest("Ebon Thimble fan keeps eight separate slots above and behind the owner and mirrors with the facing")]
    private static void EbonThimbleFan()
    {
        foreach (float tick in new[] { 0f, 50f, 333f, 6000f })
        {
            var right = new Vector2[EbonRewardRules.Pieces];
            int behind = 0;
            for (int i = 0; i < right.Length; i++)
            {
                right[i] = EbonThimbleScore.Slot(i, 1, tick);
                var left = EbonThimbleScore.Slot(i, -1, tick);
                AssertEqual(true, MathF.Abs(left.X + right[i].X) < .001f && MathF.Abs(left.Y - right[i].Y) < .001f, "mirrored with the facing");
                AssertEqual(true, float.IsFinite(right[i].X) && float.IsFinite(right[i].Y) && right[i].Y < -30f, "finite and above the owner");
                if (right[i].X < 0) behind++;
            }
            AssertEqual(true, behind >= 5, "most of the fan hangs behind");
            for (int a = 0; a < right.Length; a++)
                for (int b = a + 1; b < right.Length; b++)
                    AssertEqual(true, Vector2.Distance(right[a], right[b]) > 70f, "neighbouring pieces do not stack");
        }
        AssertEqual(EbonThimbleScore.Slot(0, 1, 7), EbonThimbleScore.Slot(-3, 1, 7), "out-of-range indices clamp");
    }

    [DomainTest("Ebon Thimble pieces rise from the hand to their slot with a small overshoot")]
    private static void EbonThimbleLiftEase()
    {
        AssertEqual(true, MathF.Abs(EbonThimbleScore.LiftEase(0)) < .001f, "starts at the fingertip");
        AssertEqual(true, MathF.Abs(EbonThimbleScore.LiftEase(EbonThimbleScore.LiftEaseTicks) - 1) < .001f, "arrives on its slot");
        float peak = 0;
        for (int age = 0; age <= EbonThimbleScore.LiftEaseTicks; age++)
        {
            float eased = EbonThimbleScore.LiftEase(age);
            peak = MathF.Max(peak, eased);
            if (age > 0) AssertEqual(true, eased >= EbonThimbleScore.LiftEase(age - 1) - .06f, "no backward jump while rising");
        }
        AssertEqual(true, peak > 1.005f && peak < 1.1f, "a small overshoot, not a bounce");
        AssertEqual(EbonThimbleScore.LiftEase(EbonThimbleScore.LiftEaseTicks), EbonThimbleScore.LiftEase(500), "stays on the slot afterwards");
    }

    [DomainTest("Ebon Thimble winds up before each yank except the first")]
    private static void EbonThimbleWind()
    {
        for (int armed = 0; armed < 120; armed++)
            AssertEqual(0f, EbonThimbleScore.Wind(armed, 0), "the first piece leaves on the release without a wind-up");
        int launch = EbonRewardRules.YankTick(3);
        AssertEqual(0f, EbonThimbleScore.Wind(launch - EbonThimbleScore.WindTicks - 1, 3), "calm before the wind-up");
        AssertEqual(0f, EbonThimbleScore.Wind(launch, 3), "released on the launch tick");
        float previous = 0;
        for (int armed = launch - EbonThimbleScore.WindTicks; armed < launch; armed++)
        {
            float wind = EbonThimbleScore.Wind(armed, 3);
            AssertEqual(true, wind > previous && wind <= 1.0001f, "the draw-back builds each tick");
            previous = wind;
        }
        AssertEqual(true, MathF.Abs(previous - 1) < .0001f, "full draw on the last tick");
    }

    [DomainTest("Ebon Thimble arm is raised while holding, aims during the throw and flicks on each event")]
    private static void EbonThimbleArm()
    {
        float raised = EbonThimbleScore.RaisedAngle(1);
        AssertEqual(true, raised < 0 && raised > -MathF.PI / 2, "up and forward when facing right");
        AssertEqual(true, EbonThimbleScore.RaisedAngle(-1) < -MathF.PI / 2 && EbonThimbleScore.RaisedAngle(-1) > -MathF.PI, "up and forward when facing left");
        float hold = EbonThimbleScore.ArmAngle(1, 0, 0, 0, 0);
        AssertEqual(true, MathF.Abs(hold - raised) < MathF.Abs(0 - raised), "holding keeps the arm nearer the raised pose than the aim");
        AssertEqual(true, MathF.Abs(EbonThimbleScore.ArmAngle(1, .4f, 1, 0, 0) - .4f) < .0001f, "the throw points at the cursor");
        AssertEqual(true, EbonThimbleScore.ArmAngle(1, 0, 0, 1, 0) < hold, "the flick lifts the right-facing hand up and back");
        AssertEqual(true, EbonThimbleScore.ArmAngle(-1, MathF.PI, 0, 1, 0) > EbonThimbleScore.ArmAngle(-1, MathF.PI, 0, 0, 0), "and the left-facing hand");
        AssertEqual(1f, EbonThimbleScore.Kick(0), "full flick on the event tick");
        AssertEqual(0f, EbonThimbleScore.Kick(10), "settled ten ticks later");
        AssertEqual(0f, EbonThimbleScore.Kick(-1), "nothing before the event");
        AssertEqual(1f, EbonThimbleScore.LaunchKick(EbonRewardRules.YankTick(4), 8), "each yank flicks the hand");
        AssertEqual(0f, EbonThimbleScore.LaunchKick(EbonThimbleScore.VolleyEnd(8), 8), "calm after the volley");
        AssertEqual(0f, EbonThimbleScore.Throw(0, 8), "the pose has not started on the release tick");
        AssertEqual(1f, EbonThimbleScore.Throw(5, 8), "pointing at the cursor during the volley");
        AssertEqual(0f, EbonThimbleScore.Throw(EbonThimbleScore.VolleyEnd(8), 8), "relaxed when the controller leaves");
        AssertEqual(0f, EbonThimbleScore.LiftKick(0, 0), "no flick before the first lift");
    }

    [DomainTest("Ebon Thimble shatters wood, glass and gilt wire by furniture")]
    private static void EbonThimbleMaterials()
    {
        var expected = new[]
        {
            EbonThimbleScore.Material.Wood, EbonThimbleScore.Material.Glass, EbonThimbleScore.Material.Glass,
            EbonThimbleScore.Material.Wood, EbonThimbleScore.Material.Wire, EbonThimbleScore.Material.Glass,
            EbonThimbleScore.Material.Wood, EbonThimbleScore.Material.Wood,
        };
        for (int i = 0; i < expected.Length; i++)
            AssertEqual(expected[i], EbonThimbleScore.MaterialOf(i), $"material of piece {i}");
        AssertEqual(EbonThimbleScore.MaterialOf(7), EbonThimbleScore.MaterialOf(-1), "negative indices wrap");
        AssertEqual(EbonThimbleScore.MaterialOf(0), EbonThimbleScore.MaterialOf(8), "indices wrap");
        for (int i = 1; i < 8; i++)
            AssertEqual(true, EbonThimbleScore.Spin(i) != EbonThimbleScore.Spin(i - 1) && MathF.Sign(EbonThimbleScore.Spin(i)) != MathF.Sign(EbonThimbleScore.Spin(i - 1)),
                "neighbouring pieces tumble opposite ways");
    }

    [DomainTest("Ebon Thimble fingertip pulse is phased so that it peaks on each lift")]
    private static void EbonThimbleBeatPhase()
    {
        AssertEqual(0f, EbonThimbleScore.BeatPhase(EbonRewardRules.FirstLift), "the pulse restarts on the first lift");
        for (int piece = 0; piece < EbonRewardRules.Pieces; piece++)
        {
            float phase = EbonThimbleScore.BeatPhase(EbonRewardRules.LiftTick(piece));
            AssertEqual(true, phase < .03f || phase > .97f, $"lift {piece} lands on the pulse peak, not 6 ticks after it");
        }
        AssertEqual(true, MathF.Abs(EbonThimbleScore.BeatPhase(EbonRewardRules.FirstLift + EbonRewardRules.BeatTicks * .5f) - .5f) < .001f,
            "half a beat later");
        for (float tick = -60; tick < 4000; tick += 3.7f)
        {
            float phase = EbonThimbleScore.BeatPhase(tick);
            AssertEqual(true, phase >= 0 && phase < 1, "always inside the beat");
        }
        AssertEqual(-1, EbonThimbleScore.BeatIndex(EbonRewardRules.FirstLift - 1), "the ticks before the first lift are the beat before it");
        AssertEqual(0, EbonThimbleScore.BeatIndex(EbonRewardRules.FirstLift), "the first lift starts beat 0");
        AssertEqual(1, EbonThimbleScore.BeatIndex(EbonRewardRules.FirstLift + EbonRewardRules.BeatTicks + .01f), "one beat later");
    }

    [DomainTest("Ebon Thimble resends the aim for a slow cursor sweep")]
    private static void EbonThimbleAimResend()
    {
        // One degree per tick is far below the per-tick threshold, so only a comparison with the last sent aim adds up.
        AssertEqual(false, EbonThimbleScore.AimDue(new Vector2(MathF.Cos(MathF.PI / 180), MathF.Sin(MathF.PI / 180)), Vector2.UnitX, 1),
            "one degree alone is not worth a packet");
        AssertEqual(true, EbonThimbleScore.AimDue(Vector2.UnitX, Vector2.UnitX, EbonThimbleScore.AimResendTicks), "a steady aim is still resent four times a second");
        Vector2 sent = Vector2.Zero;
        int resends = 0;
        float worst = 0;
        for (int age = 0; age < 240; age++)
        {
            float angle = age * MathF.PI / 180f;
            var wanted = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            if (EbonThimbleScore.AimDue(wanted, sent, age)) { sent = wanted; resends++; }
            worst = MathF.Max(worst, MathF.Acos(Math.Clamp(Vector2.Dot(wanted, Vector2.Normalize(sent)), -1, 1)));
        }
        AssertEqual(true, worst < 7f * MathF.PI / 180f, "peers never see an aim more than seven degrees stale");
        AssertEqual(true, resends >= 240 / EbonThimbleScore.AimResendTicks, "at least four resends a second");
        AssertEqual(true, resends < 60, "but not a packet every tick");
    }

    [DomainTest("Ebon Thimble tap shorter than the first lift still raises the piece it paid for")]
    private static void EbonThimbleTap()
    {
        for (int age = 0; age < EbonRewardRules.FirstLift; age++)
            AssertEqual(true, !EbonThimbleScore.LiftDue(age, 0) && EbonThimbleScore.OwesFirstPiece(0, EbonThimbleScore.Channeling),
                "a release before the first lift owes the first piece");
        AssertEqual(false, EbonThimbleScore.OwesFirstPiece(1, EbonThimbleScore.Channeling), "a raised piece is already paid for");
        AssertEqual(false, EbonThimbleScore.OwesFirstPiece(0, EbonThimbleScore.Spent), "a failed spawn is not retried");
        AssertEqual(false, EbonThimbleScore.OwesFirstPiece(0, EbonThimbleScore.Releasing), "a volley in progress owes nothing");
    }

    [DomainTest("Ebon Thimble hangs furniture from the measured eyelet and faces the piano keyboard at the owner")]
    private static void EbonThimbleArtAnchors()
    {
        AssertEqual(EbonRewardRules.Pieces, EbonThimbleScore.FurnitureEyelets.Length, "one eyelet per piece of furniture");
        for (int i = 0; i < EbonThimbleScore.FurnitureEyelets.Length; i++)
        {
            Vector2 eyelet = EbonThimbleScore.FurnitureEyelet(i);
            AssertEqual(true, eyelet.X >= 23.5f && eyelet.X <= 24f, $"eyelet {i} is on the centre line of its 48x64 cell");
            AssertEqual(true, eyelet.Y >= 12f && eyelet.Y <= 22f, $"eyelet {i} is on top of its prop, above the cell centre");
        }
        AssertEqual(EbonThimbleScore.FurnitureEyelet(7), EbonThimbleScore.FurnitureEyelet(-1), "negative indices wrap");
        AssertEqual(EbonThimbleScore.FurnitureEyelet(0), EbonThimbleScore.FurnitureEyelet(8), "indices wrap");
        AssertEqual(new Vector2(65.5f, 0f), EbonThimbleScore.PianoEyeletFor(76, false), "the ring on the tip of the lid");
        AssertEqual(new Vector2(10.5f, 0f), EbonThimbleScore.PianoEyeletFor(76, true), "and on the other side when mirrored");
        AssertEqual(true, EbonThimbleScore.PianoMirrored(1000, 1200), "an owner on the right sees the keyboard end on the right");
        AssertEqual(false, EbonThimbleScore.PianoMirrored(1000, 800), "an owner on the left keeps the art as drawn");
        AssertEqual(false, EbonThimbleScore.PianoMirrored(1000, 1000), "directly below keeps the art as drawn");
    }

    [DomainTest("Ebon Thimble rejects malformed replicated state")]
    private static void EbonThimbleStateValidation()
    {
        AssertEqual(true, EbonThimbleScore.ValidPiece(0, EbonThimbleScore.Hanging, 0), "fresh piece");
        AssertEqual(true, EbonThimbleScore.ValidPiece(7, EbonThimbleScore.Dropped, 23), "last piece, dropped");
        AssertEqual(false, EbonThimbleScore.ValidPiece(8, 0, 0), "piece index out of range");
        AssertEqual(false, EbonThimbleScore.ValidPiece(-1, 0, 0), "negative piece index");
        AssertEqual(false, EbonThimbleScore.ValidPiece(3, 5, 0), "unknown state");
        AssertEqual(false, EbonThimbleScore.ValidPiece(3, 2, -1), "negative age");
        AssertEqual(false, EbonThimbleScore.ValidPiece(3.5f, 2, 0), "fractional index");
        AssertEqual(false, EbonThimbleScore.ValidPiece(float.NaN, 2, 0), "NaN index");
        AssertEqual(false, EbonThimbleScore.ValidPiece(3, 2, float.PositiveInfinity), "infinite age");
        AssertEqual(true, EbonThimbleScore.ValidController(0, 0, EbonThimbleScore.Channeling), "fresh controller");
        AssertEqual(true, EbonThimbleScore.ValidController(64, 8, EbonThimbleScore.Releasing), "finished volley");
        AssertEqual(false, EbonThimbleScore.ValidController(0, 9, 0), "nine pieces");
        AssertEqual(false, EbonThimbleScore.ValidController(0, 0, 3), "unknown phase");
        AssertEqual(false, EbonThimbleScore.ValidController(float.NaN, 0, 0), "NaN age");
        AssertEqual(true, EbonThimbleScore.ValidPiano(5, 3000, 0), "waiting piano");
        AssertEqual(true, EbonThimbleScore.ValidPiano(90, 3000, 7), "crashed piano");
        AssertEqual(true, EbonThimbleScore.ValidPiano(90, 3000, -4), "dropped piano");
        AssertEqual(false, EbonThimbleScore.ValidPiano(-1, 3000, 0), "negative age");
        AssertEqual(false, EbonThimbleScore.ValidPiano(5, float.NaN, 0), "NaN landing height");
        AssertEqual(false, EbonThimbleScore.ValidPiano(5, 3000, 1e9f), "absurd state");
    }
}
