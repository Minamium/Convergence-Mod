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
                    float charge = Window(tick, end - 55, end);
                    Vector2 mouth = Source(combat);
                    accents.Halo(batch, mouth, new Vector2(150 - charge * 88), new Color(255, 24, 66), .8f * charge);
                    Ring(batch, mouth, 18 - charge * 11, new Color(255, 130, 154) * charge, 2);
                }
            }
        }
        if (result is not { } resolved) return;
        float age = Main.GameUpdateCount - resultStarted + fraction;
        bool wasStack = resolved.LastMechanicResult is FirstSeveranceMechanicResult.StackFailed or FirstSeveranceMechanicResult.StackPassed;
        foreach (var hit in resolved.MechanicImpacts)
        {
            Vector2 target = new(hit.X, hit.Y);
            if (wasStack) DrawShards(batch, target, hit.ParticipantId.Value, 1, 1, age, hit.Failed, reduced);
            else DrawVerdictRay(batch, Source(resolved), target, hit.Failed, age, reduced);
        }
    }

    private void DrawVerdictRay(SpriteBatch batch, Vector2 origin, Vector2 target, bool failed, float age, bool reduced)
    {
        Vector2 delta = target - origin;
        float length = delta.Length();
        if (length < 1) return;
        Vector2 direction = delta / length, normal = new(-direction.Y, direction.X);
        float stop = failed ? length : Math.Max(0, length - 65);
        float release = (1 - Window(age, failed ? 4 : 2, failed ? 17 : 11));
        Color red = new(255, 22, 62);
        // Instant full-length hairline, not a travelling projectile or hit test.
        accents.Ribbon(batch, origin, direction, stop, failed ? 18 : 7, red, release * (failed ? .95f : .45f));
        Line(batch, origin, origin + direction * stop, FirstSeveranceAttackAccents.Neon(red, release), failed ? 3.2f : 1.4f);
        Line(batch, origin, origin + direction * stop, FirstSeveranceAttackAccents.Neon(Color.White, release * (failed ? .9f : .35f)), failed ? 1.1f : .6f);
        accents.Halo(batch, origin, new Vector2(failed ? 125 : 62), red, release * .7f);
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

    private void DrawShards(SpriteBatch batch, Vector2 center, int identity, double age, double duration,
        float releaseAge, bool failed, bool reduced)
    {
        EnsureFragments();
        for (int i = 0; i < 8; i++)
        {
            double birth = Births[i] * duration;
            if (releaseAge < 0 && age < birth) continue;
            float a = i * MathF.Tau / 8 + .19f * identity + .12f * MathF.Sin(i * 7.3f);
            Vector2 radial = new(MathF.Cos(a), MathF.Sin(a));
            float snap = releaseAge >= 0 ? 1 : Window(age, birth, birth + 3);
            float radius = 89 + i % 3 * 14 + (1 - snap) * 35;
            Vector2 offset = radial * new Vector2(radius, radius * .84f);
            float rotation = a + .6f + MathF.Sin(i * 9) * .35f;
            float fade = snap, scale = .43f + i % 3 * .08f;
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
            Vector2 position = center + offset;
            Texture2D texture = fragments![i];
            Color light = failed && releaseAge >= 0 ? new(255, 135, 156) : new(210, 215, 231);
            if (!reduced)
                accents.Halo(batch, position, new Vector2(64, 88) * scale, new Color(145, 155, 221), fade * .27f, rotation);
            batch.Draw(texture, position - Main.screenPosition, null, light * fade, rotation,
                new Vector2(64, 80), scale, SpriteEffects.None, 0);
            float glint = releaseAge < 0 ? 1 - Window(age, birth + 2, birth + 10) : failed ? 1 - Window(releaseAge, 6, 15) : 0;
            accents.Halo(batch, position, new Vector2(55, 6), Color.White, fade * glint * .75f, rotation);
        }
        if (failed && releaseAge >= 4)
            accents.Halo(batch, center, new Vector2(100, 145), new Color(255, 85, 113),
                Window(releaseAge, 4, 7) * (1 - Window(releaseAge, 8, 23)) * .9f);
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
