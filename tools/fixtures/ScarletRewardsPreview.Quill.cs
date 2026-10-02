// Offline frames of the Bloodink Quill (docs/encounters/crimson-foundry/REWARDS.md, "Rogue - Bloodink Quill"):
//   pwsh tools/preview-scarlet.ps1 -Rewards -Only quill
// A full build and its release on one scene clock: seven quills stand (dried ink, one of them a blot), the eighth flies in
// writing wet ink with the ember-gold bead and sticks (a full build warms the lips), the Sealed Score is thrown, glides
// to a halt and unseals (wax flakes, the five-line flourish), unrolls, every stroke burns from the nib back, the quills
// burst in throw order at U + S(i) and the score bursts as the Full Melody (160 px). The ink is sampled by the same
// QuillRules the game uses (QuillFlight, SampleStroke, BurnAge), emitted the way Client/.../Rewards/QuillInk.cs emits it,
// through the production ScarletRewardInk. Bodies are code stand-ins: the SR06 art is not delivered and the vanilla
// placeholder is not available offline. Not a playtest.
#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Num = System.Numerics.Vector2;
using R = Convergence.Content.Encounters.CrimsonFoundry.Rewards.CrimsonRewardRules;

internal sealed class QuillRewardsScene : IRewardsPreviewScene
{
    public string Name => "quill";
    // The score flies 22 ticks (487 px), so it unrolls at 57; bursts 57..106, the Full Melody at 113.
    public int[] Ticks { get; } = { 0, 8, 16, 24, 27, 40, 49, 51, 53, 55, 57, 59, 62, 66, 72, 85, 106, 113, 116, 122, 135, 146 };

    // Throws from one point: (angle, stop tick). The fifth is a close throw into the floor (a blot); the last is thrown at
    // scene tick 0 and sticks at its stop. Heights differ, so the playback is a melody.
    private static readonly (float Angle, float Stop)[] Throws =
    {
        (-.30f, 26), (-.12f, 27), (.05f, 26), (-.22f, 25), (1.05f, 2.2f), (.18f, 24), (-.05f, 22), (-.18f, 25),
    };
    private const int Flying = 7, ScoreThrow = 27;
    private static readonly Vector2 ThrowFrom = new(-430, 40), Cursor = new(30, -120);
    private readonly QuillInkSample[] samples = new QuillInkSample[QuillRules.MaxStrokeSamples];

    private static Vector2 X(Num v) => new(v.X, v.Y);
    private static Num N(Vector2 v) => new(v.X, v.Y);
    private static Vector2 Launch(int i) => new(MathF.Cos(Throws[i].Angle) * R.QuillSpeed, MathF.Sin(Throws[i].Angle) * R.QuillSpeed);
    private static float StopAt(int i, float tick) => i == Flying ? Math.Min(tick, Throws[i].Stop) : Throws[i].Stop;
    private static Vector2 Nib(Vector2 c, int i, float tick) => X(QuillRules.At(N(c + ThrowFrom), N(Launch(i)), StopAt(i, tick)));
    private static int ScoreFlight(Vector2 c) => QuillRules.ScoreFlightTicks(Vector2.Distance(c + ThrowFrom, c + Cursor));
    private static int Unroll(Vector2 c) => ScoreThrow + QuillRules.UnrollAge(ScoreFlight(c));
    private static Vector2 ScoreAt(Vector2 c, float age)
    {
        Vector2 heading = Vector2.Normalize(Cursor - ThrowFrom), at = c + ThrowFrom;
        int flight = ScoreFlight(c);
        for (int k = 0; k < (int)age; k++) at += heading * QuillRules.ScoreSpeedAt(k, flight);
        return at + heading * QuillRules.ScoreSpeedAt((int)age, flight) * (age - (int)age);
    }

    public void Emit(ScarletInkCanvas canvas, Vector2 c, int tick)
    {
        int unroll = Unroll(c);
        float tau = tick - unroll;
        bool full = tick >= Throws[Flying].Stop;
        for (int i = 0; i < Throws.Length; i++)
        {
            float seed = i * 1.37f;
            int count = QuillRules.SampleStroke(N(c + ThrowFrom), N(Launch(i)), StopAt(i, tick), samples, out float length);
            var s = new ReadOnlySpan<QuillInkSample>(samples, 0, count);
            bool writing = i == Flying && tick < Throws[i].Stop, claimed = tick >= ScoreThrow;
            Vector2 nib = X(s[^1].At);
            if (tau < 0 || writing)
            {
                var dormant = new ScarletInkStyle(ScarletInkLook.Dormant, true, seed, 1, false, claimed || full ? 1 : 0, 0);
                if (!writing && QuillRules.IsBlot(length)) { canvas.Disc(dormant, nib, QuillRules.BlotRadius, 0); continue; }
                canvas.Begin(dormant);
                for (int k = 0; k < count; k++) canvas.Point(X(s[k].At), s[k].Radius);
                canvas.End(writing);
                continue;
            }
            if (QuillRules.IsBlot(length)) Burst(canvas, seed, nib, R.BurnRadius, tau, R.BurnLive);
            else Burn(canvas, s, seed, tau);
            Burst(canvas, seed + .5f, nib, R.QuillBurstRadius, tau - QuillRules.BurstStart(i), QuillRules.QuillBurstLive);
        }
        // The score: the flourish through the windup, then its burst after the last quill.
        float age = tick - ScoreThrow;
        if (age >= 0)
        {
            Vector2 at = ScoreAt(c, age);
            float windup = age - ScoreFlight(c);
            if (windup >= 0 && windup < R.ScoreWindup && QuillRules.FlourishOpacity(windup) > .01f)
                for (int line = 0; line < 5; line++)
                {
                    float y = QuillRules.FlourishRow(line), half = QuillRules.FlourishHalfLength(windup);
                    var style = new ScarletInkStyle(ScarletInkLook.Dormant, true, 9 + line * 1.3f, QuillRules.FlourishOpacity(windup), false, 0, 0);
                    canvas.Line(style, at + new Vector2(-6, y), at + new Vector2(-half, y), 2.6f, 2f, 0, 0, true);
                    canvas.Line(style, at + new Vector2(6, y), at + new Vector2(half, y), 2.6f, 2f, 0, 0, true);
                }
            Burst(canvas, 11.3f, at, QuillRules.ScoreBurstRadius(Throws.Length), tau - QuillRules.ScoreBurstStart(Throws.Length), QuillRules.ScoreBurstLive);
        }
        // Droplets: a few as the eighth quill enters, a ring at every burst.
        Splash(canvas, Nib(c, Flying, tick), tick - Throws[Flying].Stop, -2.9f, 1.1f, 2.4f, 4, false, 3.1f);
        for (int i = 0; i < Throws.Length; i++)
            Splash(canvas, Nib(c, i, tick) + new Vector2(0, -6), tau - QuillRules.BurstStart(i), -MathF.PI / 2, MathF.Tau * .8f, 3.6f, 8, true, i * 2.1f);
        Splash(canvas, ScoreAt(c, Math.Max(0, age)), tau - QuillRules.ScoreBurstStart(Throws.Length), -MathF.PI / 2, MathF.Tau * .9f, 5.4f, 14, true, 7.7f);
    }

    // [0, a) waits dormant, [a, b) burns (exactly the colliding samples), [b - 1, c) dries.
    private static void Burn(ScarletInkCanvas canvas, ReadOnlySpan<QuillInkSample> s, float seed, float tau)
    {
        int a = 0, b, c, n = s.Length;
        while (a < n && QuillRules.BurnAge(tau, s[a].FromNib) < 0) a++;
        for (b = a; b < n && QuillRules.BurnAge(tau, s[b].FromNib) < R.BurnLive; b++) { }
        for (c = b; c < n && QuillRules.BurnAge(tau, s[c].FromNib) < R.BurnLive + QuillRules.Residue; c++) { }
        if (a > 0 && canvas.Begin(new ScarletInkStyle(ScarletInkLook.Dormant, true, seed, 1, false, 1, 0)))
        {
            for (int i = 0; i <= Math.Min(a, n - 1); i++) canvas.Point(X(s[i].At), s[i].Radius);
            canvas.End();
        }
        if (c > b && canvas.Begin(new ScarletInkStyle(ScarletInkLook.Residue, true, seed)))
        {
            for (int i = Math.Max(0, b - 1); i < c; i++)
                canvas.Point(X(s[i].At), R.BurnRadius, Math.Clamp(1 - (QuillRules.BurnAge(tau, s[i].FromNib) - R.BurnLive) / QuillRules.Residue, 0, 1));
            canvas.End();
        }
        if (b > a && canvas.Begin(new ScarletInkStyle(ScarletInkLook.Live, true, seed, Window: CrimsonRewardRules.BurnLive)))
        {
            for (int i = a; i < b; i++) canvas.Point(X(s[i].At), R.BurnRadius, Math.Max(0, QuillRules.BurnAge(tau, s[i].FromNib)));
            canvas.End();
        }
    }

    private static void Burst(ScarletInkCanvas canvas, float seed, Vector2 at, float radius, float since, int live)
    {
        if (since < 0) return;
        if (since < live) canvas.Disc(new ScarletInkStyle(ScarletInkLook.Live, true, seed, Window: live), at, radius, since);
        else if (since < live + QuillRules.Residue)
            canvas.Disc(new ScarletInkStyle(ScarletInkLook.Residue, true, seed), at, radius, 1 - (since - live) / QuillRules.Residue);
    }

    private static void Splash(ScarletInkCanvas canvas, Vector2 at, float age, float angle, float spread, float speed, int count, bool live, float seed)
    {
        if (age < 0 || age >= 22) return;
        var style = new ScarletInkStyle(live ? ScarletInkLook.Live : ScarletInkLook.Dormant, true, seed, 1, false, 0, 0);
        for (int k = 0; k < count; k++)
        {
            float h = ScarletRewardParticles.Hash(seed, k * 3 + 1), g = ScarletRewardParticles.Hash(seed, k * 3 + 2);
            float a = angle + (h - .5f) * spread, v = speed * (.55f + .75f * g);
            Vector2 velocity = new(MathF.Cos(a) * v, MathF.Sin(a) * v);
            Vector2 now = at + velocity * age + new Vector2(0, .3f * age * age);
            float before = Math.Max(0, age - 1.5f);
            Vector2 from = at + velocity * before + new Vector2(0, .3f * before * before);
            canvas.Droplet(style, from, now, 2.6f * (.6f + .4f * g) * (1 - age / 22f) + .4f, live ? age : 0);
        }
    }

    public void Particles(ScarletRewardParticles particles, Vector2 c, int tick, bool reduced)
    {
        int unroll = Unroll(c);
        float tau = tick - unroll;
        // Embers off the burning ink.
        if (tau >= 0)
            for (int i = 0; i < Throws.Length; i++)
            {
                int count = QuillRules.SampleStroke(N(c + ThrowFrom), N(Launch(i)), StopAt(i, tick), samples, out _);
                int k = (int)(ScarletRewardParticles.Hash(i, tick) * count) % count;
                if (QuillRules.Burning(QuillRules.BurnAge(tau, samples[k].FromNib)))
                {
                    float h = ScarletRewardParticles.Hash(i + 20, tick);
                    particles.Spawn(ScarletParticleKind.Ember, 0, true, X(samples[k].At) + new Vector2((h - .5f) * 20, -6), new Vector2((h - .5f) * 1.2f, -.8f - .8f * h),
                        16 + 12 * h, 5, reduced, tick + i);
                }
                if (tau == QuillRules.BurstStart(i))
                    for (int e = 0; e < 6; e++)
                    {
                        float h = ScarletRewardParticles.Hash(i, e + 40), a = h * MathF.Tau;
                        particles.Spawn(ScarletParticleKind.Ember, 0, true, Nib(c, i, tick) + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 20,
                            new Vector2(MathF.Cos(a) * 1.4f, -1.2f - h), 20 + 10 * h, 5, reduced, i * 9 + e);
                    }
            }
        // Wax flakes as the seal splits, four ticks into the windup.
        if (tick - ScoreThrow == ScoreFlight(c) + 4)
        {
            Vector2 seal = ScoreAt(c, tick - ScoreThrow);
            for (int i = 0; i < 8; i++)
            {
                float h = ScarletRewardParticles.Hash(7, i + 60);
                particles.Spawn(ScarletParticleKind.WaxFlake, 0, true, seal + new Vector2((i % 2 == 0 ? -1 : 1) * 3, 0),
                    new Vector2((i % 2 == 0 ? -1 : 1) * (.6f + 1.4f * h), -1.6f - 1.6f * h), 26, 2, reduced, i);
            }
        }
        if (tau == QuillRules.ScoreBurstStart(Throws.Length))
        {
            Vector2 at = ScoreAt(c, tick - ScoreThrow);
            for (int i = 0; i < 18; i++)
            {
                float h = ScarletRewardParticles.Hash(11, i + 80), a = h * MathF.Tau, reach = R.MelodyRadius * (.3f + .6f * h);
                particles.Spawn(ScarletParticleKind.Ember, 0, true, at + new Vector2(MathF.Cos(a), MathF.Sin(a)) * reach,
                    new Vector2(MathF.Cos(a) * 1.2f, -1 - 1.4f * h), 22 + 12 * h, 6, reduced, 100 + i);
            }
            for (int i = 0; i < 4; i++)
                particles.Spawn(ScarletParticleKind.Smoke, 0, true, at + new Vector2((i - 1.5f) * 30, 0), new Vector2(0, -.6f), 36, 22, reduced, 140 + i);
        }
    }

    // Code stand-ins for the SR06 bodies at their planned on-screen size (24x6 and 20x10 logical at 2 px): a black
    // crimson-edged quill with a tarnished-gold nib, the nib on the stick point; a parchment roll with a red seal.
    public void Sprites(PreviewRenderer renderer, ScarletView view, Vector2 c, int tick)
    {
        var batch = renderer.Batch;
        Matrix world = Matrix.CreateTranslation(-view.ScreenPosition.X, -view.ScreenPosition.Y, 0) * view.GameView;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, world);
        float tau = tick - Unroll(c);
        for (int i = 0; i < Throws.Length; i++)
        {
            if (tau >= QuillRules.BurstStart(i)) continue; // spent into its burst
            Vector2 heading = X(QuillRules.VelocityAt(N(Launch(i)), Math.Max(StopAt(i, tick), 1e-3f)));
            float heat = R.Smooth((tau - QuillRules.BurstStart(i) + R.S(1)) / R.S(1));
            DrawQuill(batch, renderer.Pixel, Nib(c, i, tick), MathF.Atan2(heading.Y, heading.X), heat);
        }
        float age = tick - ScoreThrow;
        if (age >= 0 && tau < QuillRules.ScoreBurstStart(Throws.Length))
        {
            float windup = age - ScoreFlight(c), open = R.Smooth((windup - 3) / 5);
            DrawScore(batch, renderer.Pixel, ScoreAt(c, age), windup < 0 ? age * .3f : 0, open, windup >= 0 && windup < 4);
        }
        batch.End();
    }

    private static void DrawQuill(SpriteBatch batch, Texture2D pixel, Vector2 nib, float angle, float heat)
    {
        Vector2 along = new(MathF.Cos(angle), MathF.Sin(angle)), across = new(-along.Y, along.X);
        Color vane = Color.Lerp(new Color(28, 22, 26), new Color(90, 24, 20), heat), edge = new Color(132, 16, 26), gold = new Color(176, 132, 60);
        for (int k = 0; k < 22; k++)
        {
            Vector2 at = nib - along * (4 + k * 2);
            float width = k < 4 ? 2 : k > 18 ? 2 : 4 + (k % 3 == 0 ? 1 : 0);
            Dot(batch, pixel, at, angle, new Vector2(2, width * 2), vane);
            Dot(batch, pixel, at + across * width, angle, new Vector2(2, 2), edge);
        }
        Dot(batch, pixel, nib - along * 2, angle, new Vector2(4, 3), gold);
        Dot(batch, pixel, nib, angle, new Vector2(2, 2), new Color(10, 4, 6));
    }

    private static void DrawScore(SpriteBatch batch, Texture2D pixel, Vector2 at, float angle, float open, bool cracked)
    {
        Vector2 size = new(40 * (1 + .45f * open), 20 * (1 - .15f * open));
        batch.Draw(pixel, at, null, new Color(214, 200, 168), angle, new Vector2(.5f), size, SpriteEffects.None, 0);
        for (int line = -2; line <= 2; line++)
            batch.Draw(pixel, at + new Vector2(0, line * 3), null, new Color(60, 46, 40) * .6f, angle, new Vector2(.5f), new Vector2(size.X - 6, 1), SpriteEffects.None, 0);
        if (open < .5f) batch.Draw(pixel, at, null, cracked ? new Color(220, 60, 40) : new Color(150, 14, 22), angle, new Vector2(.5f), new Vector2(8, 8), SpriteEffects.None, 0);
    }

    private static void Dot(SpriteBatch batch, Texture2D pixel, Vector2 at, float angle, Vector2 size, Color color)
        => batch.Draw(pixel, at, null, color, angle, new Vector2(.5f), size, SpriteEffects.None, 0);
}
