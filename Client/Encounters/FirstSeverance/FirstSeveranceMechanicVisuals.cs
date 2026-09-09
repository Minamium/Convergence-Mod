#nullable enable
using System;
using System.Diagnostics;
using Convergence.Content.Encounters.FirstSeverance;
using Convergence.Common.Raids.Revive;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// Disposable presentation only. Resolve positions/failures arrive from authority;
// local movement is used only to attach harmless anticipation to a visible body.
internal sealed class FirstSeveranceMechanicVisuals
{
    private static readonly float[] Births = { .035f, .14f, .18f, .37f, .46f, .69f, .73f, .88f };
    private readonly FirstSeveranceAttackAccents accents = new();
    private Texture2D[]? fragments;
    private FirstSeveranceCombatProjection? result;
    private ulong resultStarted;
    private FirstSeveranceCombatProjection? current;
    private double tick;
    private long stamp;

    internal static Vector2 Source(FirstSeveranceCombatProjection combat)
        => CoreCenter(combat) - new Vector2(0, combat.BossPhase is FirstSeveranceBossPhase.Distant or FirstSeveranceBossPhase.Final ? 190 : 0);

    internal void Update(FirstSeveranceCombatProjection? combat, double now)
    {
        current = combat;
        tick = now;
        stamp = Stopwatch.GetTimestamp();
        if (result is not null && Main.GameUpdateCount - resultStarted > 70) result = null;
    }

    internal void Accept(FirstSeveranceCombatProjection combat)
    {
        result = combat;
        resultStarted = Main.GameUpdateCount;
    }

    internal static int ShardBeat(double age, double duration)
    {
        int beat = -1;
        for (int i = 0; i < Births.Length; i++) if (age >= Births[i] * duration) beat = i;
        return beat;
    }

    internal void Draw(SpriteBatch batch, bool reduced)
    {
        if (Main.dedServ) return;
        float fraction = Main.gamePaused ? 0 : (float)Math.Clamp((Stopwatch.GetTimestamp() - stamp) * 60d / Stopwatch.Frequency, 0, 1);
        double tick = this.tick + fraction;
        if (current is { } combat)
        {
            var window = FirstSeveranceSafeWindows.At(combat.Substate, combat.ActionIndex, combat.ActionStartedTick,
                (ulong)Math.Max(0, tick), combat.CoreX, combat.CoreY);
            bool stack = window?.Kind == FirstSeveranceSafeMechanic.Stack || combat.Substate == FirstSeveranceSubstate.Stack;
            bool spread = window?.Kind == FirstSeveranceSafeMechanic.Spread || combat.Substate == FirstSeveranceSubstate.Spread;
            double start = window?.StartTick ?? combat.ActionStartedTick;
            double end = window?.ResolveTick ?? combat.ResolveTick;
            if ((stack || spread) && tick < end)
            {
                foreach (var member in combat.Participants)
                {
                    if (!member.IsConnected || member.CombatState != RaidParticipantCombatState.Alive) continue;
                    Player player = Main.player[member.ServerWhoAmI];
                    if (!player.active) continue;
                    if (stack) DrawShards(batch, player.Center, member.ParticipantId.Value, tick - start, end - start, -1, false, reduced);
                }
                // A compressed ruby charge visibly belongs to the Boss, without
                // pretending the unavoidable result ray is a dodgeable telegraph.
                if (spread)
                {
                    float charge = CastTension(tick, end - 55, end);
                    Vector2 mouth = Source(combat);
                    accents.CastSeal(batch, mouth, tick, end - 55, end, new Color(255, 24, 66), reduced, .55f);
                    accents.Halo(batch, mouth, new Vector2(150 - charge * 88), new Color(255, 24, 66), .8f * charge);
                    Ring(batch, mouth, 18 - charge * 11, new Color(255, 130, 154) * charge, 2);
                }
            }
        }
        if (result is not { } resolved) return;
        float age = Main.GameUpdateCount - resultStarted + fraction;
        bool wasStack = resolved.LastMechanicResult is FirstSeveranceMechanicResult.StackFailed or FirstSeveranceMechanicResult.StackPassed;
        if (!wasStack && resolved.MechanicImpacts.Count > 0)
            DrawExecutionFlash(batch, Source(resolved), age, reduced);
        foreach (var hit in resolved.MechanicImpacts)
        {
            Vector2 target = new(hit.X, hit.Y);
            if (wasStack) DrawShards(batch, target, hit.ParticipantId.Value, 1, 1, age, hit.Failed, reduced);
            else DrawVerdictRay(batch, Source(resolved), target, hit.Failed, age, reduced);
        }
    }

    private void DrawExecutionFlash(SpriteBatch batch, Vector2 mouth, float age, bool reduced)
    {
        // Same launch on success/failure, once per verdict rather than per player.
        if (age >= 6) return;
        float flash = (1 - Window(age, 1, 6)) * (reduced ? .22f : 1);
        float reach = 175 + 165 * Window(age, 0, 2);
        for (int axis = 0; axis < 2; axis++)
        {
            Vector2 direction = axis == 0 ? Vector2.UnitX : Vector2.UnitY;
            float length = reach * (axis == 0 ? 1 : .73f);
            accents.Halo(batch, mouth, new Vector2(length * 2.4f, 20), Color.White,
                flash * .85f, axis * MathF.PI * .5f);
            for (int side = -1; side <= 1; side += 2)
                for (int n = 0; n < 10; n++)
                {
                    float t = n / 10f;
                    Line(batch, mouth + direction * (length * t * side),
                        mouth + direction * (length * (n + 1) / 10 * side),
                        FirstSeveranceAttackAccents.Neon(Color.White, flash * (1 - t)), 1 + (1 - t) * 4);
                }
        }
        accents.Halo(batch, mouth, new Vector2(90), Color.White, flash);
    }

    private void DrawVerdictRay(SpriteBatch batch, Vector2 origin, Vector2 target, bool failed, float age, bool reduced)
    {
        Vector2 delta = target - origin;
        float length = delta.Length();
        if (length < 1) return;
        Vector2 direction = delta / length, normal = new(-direction.Y, direction.X);
        float stop = failed ? length : Math.Max(0, length - 100);
        float release = 1 - Window(age, 4, 17);
        Color red = new(255, 22, 62);
        // Instant full-length hairline, not a travelling projectile or hit test.
        accents.Ribbon(batch, origin, direction, stop, 18, red, release * .95f);
        Line(batch, origin, origin + direction * stop, FirstSeveranceAttackAccents.Neon(red, release), 3.2f);
        Line(batch, origin, origin + direction * stop, FirstSeveranceAttackAccents.Neon(Color.White, release * .9f), 1.1f);
        accents.Halo(batch, origin, new Vector2(125), red, release * .7f);
        if (!failed)
        {
            DrawDissipation(batch, origin + direction * stop, direction, age, reduced);
            return;
        }
        float decay = 1 - Window(age, 12, failed ? 38 : 32);
        Vector2 impact = origin + direction * stop;
        for (int i = 0; i < (reduced ? 5 : 13); i++)
        {
            float seed = i * 2.39996f;
            Vector2 drift = failed ? new Vector2(MathF.Cos(seed), MathF.Sin(seed)) * age * (1.7f + i * .09f)
                : normal * (MathF.Sin(seed) * (8 + age * 1.8f)) - direction * (i * 2 + age * .55f);
            Vector2 point = impact + drift;
            accents.Halo(batch, point, new Vector2(failed ? 17 : 24), failed ? red : new Color(188, 100, 137), decay * (failed ? .65f : .24f));
            if (failed) Line(batch, point, point - Vector2.Normalize(drift + new Vector2(.001f)) * 9, FirstSeveranceAttackAccents.Neon(red, decay), 1.5f);
        }
    }

    private void DrawDissipation(SpriteBatch batch, Vector2 center, Vector2 direction, float age, bool reduced)
    {
        if (age >= 60) return;
        Vector2 normal = new(-direction.Y, direction.X);
        float opening = 1 - MathF.Exp(-age * .14f), fade = 1 - Window(age, 18, 60);
        Color rose = new(255, 92, 147);
        // A split, transverse plume catches the ray before the body. Fine
        // curling trails and glass sparks retain negative space, not a filled ball.
        for (int i = 0; i < (reduced ? 10 : 28); i++)
        {
            float seed = i * 2.39996f;
            float side = i % 2 == 0 ? 1 : -1;
            float reach = (45 + i % 7 * 13) * opening;
            Vector2 point = center + normal * (side * reach)
                - direction * (age * (.45f + i % 4 * .24f) + (1 - MathF.Cos(seed)) * 16);
            Vector2 last = center;
            for (int n = 1; n <= 8; n++)
            {
                float t = n / 8f;
                Vector2 next = Vector2.Lerp(center, point, t) - direction *
                    (MathF.Sin(t * MathF.PI) * (13 + i % 5 * 6) * opening);
                Line(batch, last, next, FirstSeveranceAttackAccents.Neon(rose, fade * .25f * t), 4);
                Line(batch, last, next, FirstSeveranceAttackAccents.Neon(Color.White, fade * .65f * t), 1.1f);
                last = next;
            }
            accents.Halo(batch, point, new Vector2(22, 5), rose, fade * .65f, seed + age * .06f);
        }
        accents.Halo(batch, center, new Vector2(22, 80 + opening * 145), rose,
            (1 - Window(age, 3, 22)) * (reduced ? .3f : .8f), direction.ToRotation());
    }

    private void DrawShards(SpriteBatch batch, Vector2 center, int identity, double age, double duration,
        float releaseAge, bool failed, bool reduced)
    {
        EnsureFragments();
        Span<Vector2> positions = stackalloc Vector2[8];
        Span<float> opacity = stackalloc float[8];
        opacity.Clear();
        float clock = (float)(releaseAge < 0 ? age : releaseAge);
        float agitation = releaseAge < 0 ? 1 : failed ? 1 - Window(releaseAge, 0, 9) : 0;
        for (int i = 0; i < 8; i++)
        {
            double birth = Births[i] * duration;
            if (releaseAge < 0 && age < birth) continue;
            float a = i * MathF.Tau / 8 + .19f * identity + .12f * MathF.Sin(i * 7.3f);
            Vector2 radial = new(MathF.Cos(a), MathF.Sin(a));
            float snap = releaseAge >= 0 ? 1 : Window(age, birth, birth + 3);
            float radius = 110 + i % 3 * 18 + (1 - snap) * 35;
            Vector2 offset = radial * new Vector2(radius, radius * .84f);
            float rotation = a + .6f + MathF.Sin(i * 9) * .35f;
            float fade = snap, scale = .71f + i % 3 * .13f;
            if (releaseAge >= 0)
            {
                float collapse = failed ? Window(releaseAge, 0, 7) : 0;
                float fall = Math.Max(0, releaseAge - (failed ? 7 : i % 3 * 3));
                offset *= 1 - collapse * .94f;
                offset += new Vector2(radial.X * fall * (failed ? 2.4f : .6f), .075f * fall * fall - (failed ? fall * 1.7f : 0));
                rotation += fall * (i % 2 == 0 ? 1 : -1) * (failed ? .095f : .025f);
                scale *= 1 - Window(fall, 0, 40) * (failed ? .65f : .22f);
                fade = 1 - Window(releaseAge, failed ? 15 : 28, failed ? 48 : 70);
            }
            // Independent, smoothly interpolated stick/slip vibration. Keep the
            // authored shell texture readable instead of covering it in bloom.
            float seed = i * 31.7f + identity * 93.1f;
            float tension = .35f + .65f * Window(age, duration * .45, duration);
            float chatter = agitation * tension * (reduced ? .4f : 1);
            offset += new Vector2(Noise(clock * .61f + seed), Noise(clock * .79f + seed + 9)) * chatter * 8;
            rotation += Noise(clock * .43f + seed + 19) * chatter * .065f;
            Vector2 position = center + offset;
            positions[i] = position;
            opacity[i] = fade;
            Texture2D texture = fragments![i];
            Color light = failed && releaseAge >= 0 ? new(255, 135, 156) : new(210, 215, 231);
            if (!reduced)
                accents.Halo(batch, position, new Vector2(64, 88) * scale, new Color(145, 155, 221), fade * .27f, rotation);
            batch.Draw(texture, position - Main.screenPosition, null, light * fade, rotation,
                new Vector2(64, 80), scale, SpriteEffects.None, 0);
            float glint = releaseAge < 0 ? 1 - Window(age, birth + 2, birth + 10) : failed ? 1 - Window(releaseAge, 6, 15) : 0;
            accents.Halo(batch, position, new Vector2(55, 6), Color.White, fade * glint * .75f, rotation);
        }
        if (agitation > 0)
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8;
                if (opacity[i] <= 0 || opacity[next] <= 0 || (reduced && i % 2 != 0)) continue;
                float seed = i * 31.7f + identity * 93.1f;
                float contact = Window(Noise(clock * .19f + seed), -.05, .6);
                float strength = contact * Math.Min(opacity[i], opacity[next]) * agitation * (reduced ? .4f : 1.2f);
                if (strength < .03f) continue;
                Vector2 gap = positions[next] - positions[i];
                // Short filament starts/ends on the shard edges, not a solid ring.
                Vector2 start = positions[i] + gap * .23f, end = positions[next] - gap * .23f;
                DrawFrictionArc(batch, start, end, clock, seed, strength);
                if (!reduced)
                    DrawFrictionArc(batch, Vector2.Lerp(start, end, .48f),
                        Vector2.Lerp(start, end, .68f) + (start - center) * .48f,
                        clock + 7, seed + 13, strength * .8f);
            }
        if (failed && releaseAge >= 4)
            accents.Halo(batch, center, new Vector2(100, 145), new Color(255, 85, 113),
                Window(releaseAge, 4, 7) * (1 - Window(releaseAge, 8, 23)) * .9f);
    }

    private void DrawFrictionArc(SpriteBatch batch, Vector2 start, Vector2 end, float clock, float seed, float strength)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length < 1) return;
        Vector2 normal = new(-delta.Y / length, delta.X / length), last = start;
        Color cold = new(161, 193, 255);
        for (int n = 1; n <= 7; n++)
        {
            float t = n / 7f;
            Vector2 point = Vector2.Lerp(start, end, t) + normal *
                (Noise(clock * .37f + seed + n * 9.37f) * 18 * MathF.Sin(t * MathF.PI));
            Line(batch, last, point, FirstSeveranceAttackAccents.Neon(cold, strength * .30f), 8);
            Line(batch, last, point, FirstSeveranceAttackAccents.Neon(Color.White, strength * .95f), 1.8f);
            last = point;
        }
        accents.Halo(batch, start, new Vector2(35, 7), cold, strength * .75f, delta.ToRotation());
    }

    private static float Noise(float time)
    {
        float cell = MathF.Floor(time), blend = time - cell;
        blend = blend * blend * (3 - 2 * blend);
        static float Hash(float x) { float v = MathF.Sin(x * 12.9898f + 78.233f) * 43758.5453f; return (v - MathF.Floor(v)) * 2 - 1; }
        return MathHelper.Lerp(Hash(cell), Hash(cell + 1), blend);
    }

    private void EnsureFragments()
    {
        if (fragments is not null) return;
        var source = ModContent.Request<Texture2D>("Convergence/Assets/Textures/NPCs/NullCantorShell").Value;
        var pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        fragments = new Texture2D[8];
        for (int i = 0; i < 8; i++)
        {
            var data = new Color[128 * 160];
            float angle = i * MathF.Tau / 8;
            for (int y = 0; y < 160; y++)
                for (int x = 0; x < 128; x++)
                {
                    float u = (x - 64f) / 64, v = (y - 80f) / 80;
                    // Faceted asymmetric splinter mask over authored microfractures.
                    if (Math.Abs(u + v * .23f) > .70f - Math.Abs(v) * .43f || v < -.92f || v > .89f) continue;
                    int sx = (int)((.5f + MathF.Cos(angle) * .28f) * source.Width + u * source.Width * .085f);
                    int sy = (int)((.5f + MathF.Sin(angle) * .28f) * source.Height + v * source.Height * .12f);
                    data[y * 128 + x] = pixels[Math.Clamp(sy, 0, source.Height - 1) * source.Width + Math.Clamp(sx, 0, source.Width - 1)];
                }
            fragments[i] = new Texture2D(Main.instance.GraphicsDevice, 128, 160);
            fragments[i].SetData(data);
        }
    }

    internal void Reset(bool unload = false)
    {
        current = result = null;
        if (!unload) return;
        accents.Unload();
        var old = fragments; fragments = null;
        if (old is not null) Main.QueueMainThreadAction(() => { foreach (var texture in old) texture.Dispose(); });
    }
}
