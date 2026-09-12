#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// One disposable emitter per authority cast, not one renderer per attack phase.
// Its aperture, gathering fibers, jet and cooling wake share the same clock.
internal sealed class FirstSeveranceEmissionVisuals
{
    private sealed class Emitter
    {
        internal FirstSeveranceLanceVolley Volley;
        internal Vector2 From, Target, Velocity;
        internal float FromAngle, TargetAngle;
        internal double SampleTick;
        internal ulong? CancelledAt;
        internal Emitter(FirstSeveranceLanceVolley volley, double tick)
        {
            Volley = volley;
            From = Target = new(volley.Rays[0].X, volley.Rays[0].Y);
            FromAngle = TargetAngle = MathF.Atan2(volley.Rays[0].DirectionY, volley.Rays[0].DirectionX);
            SampleTick = tick;
        }
        internal Vector2 Position(double tick)
        {
            if (tick >= Volley.FireTick)
            {
                var head = Volley.ChargeHeadAt((ulong)Math.Min(tick, Volley.EndTick - 1d));
                float fraction = tick < Volley.EndTick - 1d ? (float)(tick - Math.Floor(tick)) : 0f;
                return new Vector2(head.X, head.Y) + new Vector2(Volley.Rays[0].DirectionX, Volley.Rays[0].DirectionY) * (fraction * 104f);
            }
            var p = Hermite(new(From.X, From.Y), new(Velocity.X, Velocity.Y), new(Target.X, Target.Y), 4f, (float)(tick - SampleTick) / 4f);
            return new(p.X, p.Y);
        }
        internal float Angle(double tick)
        {
            if (tick >= Volley.FireTick) return TargetAngle;
            return FromAngle + MathHelper.WrapAngle(TargetAngle - FromAngle) * Window(tick, SampleTick, SampleTick + 4);
        }
        internal void Accept(FirstSeveranceLanceVolley next, double tick)
        {
            if (next.MotionTick == Volley.MotionTick) { Volley = next; return; }
            Vector2 position = Position(tick);
            Velocity = (position - Position(tick - .5)) * 2;
            // Bound a delayed packet's cosmetic acceleration. Settle during the
            // harmless lock hold, never interpolate or re-aim a live hitbox.
            if (Velocity.LengthSquared() > 4096) Velocity = Vector2.Normalize(Velocity) * 64;
            From = position;
            FromAngle = Angle(tick);
            Target = new(next.Rays[0].X, next.Rays[0].Y);
            TargetAngle = MathF.Atan2(next.Rays[0].DirectionY, next.Rays[0].DirectionX);
            SampleTick = tick;
            Volley = next;
        }
    }

    private readonly List<Emitter> emitters = new(12);
    internal readonly FirstSeveranceAttackAccents Accents = new();
    private Asset<Texture2D>? atlas;
    private double clockTick;
    private long clockStamp;
    internal double RenderTick => clockTick + (Main.gamePaused ? 0 : Math.Clamp((Stopwatch.GetTimestamp() - clockStamp) * 60d / Stopwatch.Frequency, 0, 1));
    internal float Pose => Peak(e => CastPose(RenderTick, e.Volley.StartTick, e.Volley.FireTick, e.Volley.EndTick));
    internal float Kick => Peak(e => Recoil(RenderTick, e.Volley.FireTick, e.Volley.EndTick));
    private float Peak(Func<Emitter, float> value)
    {
        float result = 0;
        foreach (var emitter in emitters) result = Math.Max(result, value(emitter));
        return result;
    }
    internal void Update(FirstSeveranceLanceVolley? volley, ulong tick, IReadOnlyList<FirstSeveranceLanceVolley> spread)
    {
        clockTick = tick;
        clockStamp = Stopwatch.GetTimestamp();
        emitters.RemoveAll(e => tick > e.Volley.EndTick + 28);
        foreach (var emitter in emitters)
        {
            bool currentSpread = false;
            foreach (var cast in spread) currentSpread |= emitter.Volley.Serial == cast.Serial;
            if (!currentSpread && emitter.Volley.Serial != volley?.Serial && tick < emitter.Volley.EndTick)
                emitter.CancelledAt ??= tick;
        }
        if (volley is not null) Accept(volley, tick);
        foreach (var cast in spread)
            if (tick <= cast.EndTick + 28) Accept(cast, tick);
    }
    private void Accept(FirstSeveranceLanceVolley volley, ulong tick)
    {
        var existing = emitters.Find(e => e.Volley.Serial == volley.Serial);
        if (existing is not null) existing.Accept(volley, tick);
        else
        {
            if (emitters.Count == 12) emitters.RemoveAt(0);
            emitters.Add(new Emitter(volley, tick));
        }
    }
    internal void Clear(bool unload = false)
    {
        emitters.Clear();
        if (unload)
        {
            atlas = null; // Content manager owns the texture.
            Accents.Unload();
        }
    }

    internal static Color AttackColor(FirstSeveranceLanceVolley v)
        => v.Kind == FirstSeveranceAttackKind.PursuitPrism
            ? PrismColor(FirstSeveranceAttackPatterns.PrismColorIndex(v.Step))
            : FirstSeveranceAttackAccents.Cyan;

    internal static Color PrismColor(int index) => index switch
        { 0 => new(255, 58, 108), 1 => new(100, 125, 255), 2 => new(50, 255, 196), _ => new(255, 205, 88) };

    internal void Draw(SpriteBatch batch, ulong authorityTick, bool reduced)
    {
        if (Main.dedServ || emitters.Count == 0) return;
        atlas ??= ModContent.Request<Texture2D>("Convergence/Assets/Textures/VFX/EmissionAtlas", AssetRequestMode.ImmediateLoad);
        double now = RenderTick;
        foreach (Emitter e in emitters)
        {
            var v = e.Volley;
            if (now < v.StartTick) continue;
            float open = Aperture(now, v.StartTick, v.FireTick, v.EndTick);
            float emission = Emission(now, v.FireTick, v.EndTick);
            float warning = Window(now, v.StartTick, v.StartTick + 4d) * (1 - Window(now, v.FireTick - 1d, v.FireTick + 5d));
            float cooling = 1 - Window(now, v.EndTick, v.EndTick + 24d);
            if (e.CancelledAt is { } cancelled)
            {
                float fade = 1 - Window(now, cancelled, cancelled + 18d);
                open = Aperture(cancelled, v.StartTick, v.FireTick, v.EndTick) * fade;
                emission = Emission(cancelled, v.FireTick, v.EndTick) * fade * .35f;
                warning *= fade;
                cooling *= fade;
            }
            Color color = AttackColor(v);
            if (v.IsCharge)
            {
                DrawCharge(batch, e, now, authorityTick, open, emission, warning, cooling, color, reduced);
                continue;
            }
            if (v.Kind == FirstSeveranceAttackKind.Stillness)
            {
                DrawCurtainComb(batch, e, now, authorityTick, color, reduced);
                continue;
            }
            foreach (var ray in v.Rays)
                DrawLockedRay(batch, ray, now, v.StartTick, v.FireTick, v.EndTick,
                    e.CancelledAt is null && v.IsFiring(authorityTick), e.CancelledAt is not null,
                    open, emission, warning, cooling, color, reduced);
        }
    }


    // Both the initial pursuit and Final score use this exact aperture, plasma,
    // warning footprint and release envelope. Ownership/assignment stay outside.
    internal void DrawPrismRay(SpriteBatch batch, FirstSeveranceLanceRay ray, double now,
        ulong start, ulong fire, ulong endTick, bool active, Color color, bool reduced)
    {
        if (Main.dedServ || now < start) return;
        atlas ??= ModContent.Request<Texture2D>("Convergence/Assets/Textures/VFX/EmissionAtlas", AssetRequestMode.ImmediateLoad);
        DrawLockedRay(batch, ray, now, start, fire, endTick, active, false,
            Aperture(now, start, fire, endTick), Emission(now, fire, endTick),
            Window(now, start, start + 4) * (1 - Window(now, fire - 1, fire + 5)),
            1 - Window(now, endTick, endTick + 24), color, reduced);
    }

    private void DrawLockedRay(SpriteBatch batch, FirstSeveranceLanceRay ray, double now,
        double start, double fire, double endTick, bool active, bool cancelled,
        float open, float emission, float warning, float cooling, Color color, bool reduced)
    {
        Vector2 origin = new(ray.X, ray.Y), direction = new(ray.DirectionX, ray.DirectionY);
        Vector2 normal = new(-direction.Y, direction.X);
        float angle = MathF.Atan2(direction.Y, direction.X);
        DrawAperture(batch, origin, direction, now, start, fire, open, emission, color, reduced);
        Vector2 end = origin + direction * ray.Length;

        // The locked corridor is always readable, including a late
        // snapshot's first frame. Retired light is dim and has no rails.
        if (!active && !cancelled && now < fire)
            DrawWarning(batch, origin, direction, ray.Length, ray.HalfWidth,
                now, start, fire, color, Math.Max(.6f, warning), reduced);
        if (now >= fire && ray.HalfWidth >= 120)
            FirstSeveranceHazardSurface.Draw(batch, Accents, origin, direction, ray.Length, ray.HalfWidth,
                now - start, 1, active ? emission : emission * .15f, cooling, color, reduced);
        float light = active ? .85f + emission * .15f : emission * .12f;
        if (now < fire) light = 0;
        float width = ray.HalfWidth * (0.08f + .92f * emission);
        // Saturated plasma skin, white-hot inner spine, soft gradient
        // strictly INSIDE the authority corridor, even on broad curtains.
        if (ray.HalfWidth < 120)
            FirstSeveranceBeamMaterial.Draw(batch, Accents, origin, direction, ray.Length, ray.HalfWidth,
                now - start, 1, emission, light, color, reduced);
        // Flowing, tapered strands; the same converging fibers survive
        // the launch and accelerate down the barrel. No static stretch.
        int strands = reduced ? 3 : 7;
        for (int fiber = 0; fiber < strands; fiber++)
        {
            Vector2 previous = origin;
            const int segments = 48;
            for (int s = 1; s <= segments; s++)
            {
                float t = s / (float)segments;
                float phase = t * 23 - FlowPhase(now - start, .42f, emission) + fiber * 2.4f;
                float taper = MathF.Sin(MathF.PI * t) * .55f + .15f;
                float offset = (MathF.Sin(phase) * .28f + (fiber / (float)strands - .5f)) * width * taper;
                Vector2 next = origin + direction * (ray.Length * t) + normal * offset;
                Line(batch, previous, next, FirstSeveranceAttackAccents.Neon(
                    fiber == 0 ? Color.White : color, (.06f * warning + light * .65f) / (fiber * .17f + 1)),
                    1.1f + emission * (fiber == 0 ? 3.5f : 1.2f));
                previous = next;
            }
        }
        // Textured turbulent knots move along the filament and dissolve
        // at the endpoints. No bloom is placed in the stillness safe lane.
        for (int knot = 0; knot < (reduced ? 2 : 5); knot++)
        {
            float t = Frac((float)(now - start) * .023f + knot * .217f);
            float alpha = MathF.Sin(t * MathF.PI) * light * .55f;
            Sprite(batch, new Rectangle(5, 12, 980, 455), origin + direction * (t * ray.Length),
                new Vector2(700, width * 1.55f), angle, FirstSeveranceAttackAccents.Neon(color, alpha), new Vector2(.96f, .55f));
        }
        // Opening ring is born at the mouth then travels down the jet.
        float age = (float)(now - fire);
        float shock = Window(now, fire - 2d, fire + 3d) * (1 - Window(now, fire + 3d, endTick + 12d));
        if (shock > 0)
            Sprite(batch, new Rectangle(1010, 8, 235, 527), origin + direction * (Math.Max(0, age) * 42),
                new Vector2(36 + Math.Max(age, 0) * 2, Math.Min(ray.HalfWidth * 1.8f, 120 + age * 3)), angle,
                FirstSeveranceAttackAccents.Neon(color, shock * .85f), new(.5f));
    }

    private void DrawCurtainComb(SpriteBatch batch, Emitter e, double now, ulong authorityTick,
        Color color, bool reduced)
    {
        var v = e.Volley;
        double clock = e.CancelledAt is { } cancelled ? Math.Min(now, cancelled) : now;
        float cancelledFade = e.CancelledAt is { } ended ? 1 - Window(now, ended, ended + 18d) : 1;
        foreach (var curtain in v.Rays)
            for (int lane = 0; lane < FirstSeveranceCurtainComb.LaneCount; lane++)
            {
                ulong start = FirstSeveranceCurtainComb.RevealTick(v, lane);
                ulong fire = FirstSeveranceCurtainComb.FireTick(v, lane);
                ulong end = FirstSeveranceCurtainComb.EndTick(v, lane);
                if (clock < start) continue;
                var ray = FirstSeveranceCurtainComb.Ray(curtain, lane);
                Vector2 origin = new(ray.X, ray.Y), direction = new(ray.DirectionX, ray.DirectionY);
                bool live = e.CancelledAt is null && FirstSeveranceCurtainComb.IsLive(v, lane, authorityTick);
                float charge = Window(clock, start, fire);
                float release = Emission(clock, fire, end);
                float born = .65f + .35f * Window(clock, start, start + 2d);
                float power = (clock < fire ? born : live ? 1 : release * .10f) * cancelledFade;
                Color tint = Color.Lerp(color, new Color(174, 152, 255), MathF.Abs(lane - FirstSeveranceCurtainComb.CenterLane) / 36f);
                FirstSeveranceBeamMaterial.DrawTooth(batch, Accents, origin, direction, ray.Length, ray.HalfWidth,
                    clock - start, charge, live ? release : 0, power, tint, reduced);
                // Each tooth launches its own bounded luminous knot along the
                // axis. Its narrow mask never blooms across the central safe gap.
                float shock = Window(clock, fire, fire + 2d) * (1 - Window(clock, fire + 5d, end));
                if (shock > 0 && e.CancelledAt is null)
                {
                    float travel = Math.Clamp((float)(clock - fire) * 180, 0, ray.Length - 100);
                    Accents.Ribbon(batch, origin + direction * travel, direction, 100,
                        ray.HalfWidth * 1.5f, Color.White, shock * .85f);
                }
            }
    }

    private void DrawCharge(SpriteBatch batch, Emitter e, double now, ulong authorityTick,
        float open, float emission, float warning, float cooling, Color color, bool reduced)
    {
        var v = e.Volley;
        Vector2 head = e.Position(now);
        float angle = e.Angle(now);
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 normal = new(-direction.Y, direction.X);
        // Reconstruct the locked launch aperture even from a later motion sample.
        var sample = v.Rays[0];
        float launchedTicks = v.MotionTick >= v.FireTick ? v.MotionTick - v.FireTick + 1 : 0;
        Vector2 mouth = now < v.FireTick ? head : new Vector2(sample.X, sample.Y) - direction * (104 * launchedTicks);
        DrawAperture(batch, mouth, direction, now, v.StartTick, v.FireTick, open, emission, color, reduced);
        if (now < v.FireTick && e.CancelledAt is null)
            DrawWarning(batch, head, direction, 1600, 50, now, v.StartTick, v.FireTick, color, Math.Max(.6f, warning), reduced);
        float charge = CastTension(now, v.StartTick, v.FireTick);
        // The gathered body deforms continuously into a long needle at launch.
        float stretch = 1 + emission * 1.35f;
        float opacity = Window(now, v.StartTick, v.StartTick + 5d) * cooling;
        Sprite(batch, new Rectangle(5, 12, 980, 455), head, new Vector2((220 + charge * 100) * stretch, 120 - emission * 24),
            angle, color * opacity, new(.97f, .55f));
        Accents.Halo(batch, head - direction * 35, new Vector2(210 + emission * 160, 104), color,
            opacity * .75f, angle);
        Accents.Ribbon(batch, head - direction * (140 + emission * 140), direction,
            140 + emission * 140, 45, Color.White, opacity * (.35f + emission * .6f));
        if (!reduced)
            Sprite(batch, new Rectangle(5, 12, 980, 455), head - direction * 80, new Vector2(540 * stretch, 90),
                angle, FirstSeveranceAttackAccents.Neon(color, opacity * emission * .4f), new(.97f, .55f));
        // Filaments stream out of the gathered body, not a disconnected trail.
        for (int f = 0; f < (reduced ? 2 : 5); f++)
        {
            Vector2 previous = head;
            for (int s = 1; s <= 32; s++)
            {
                float t = s / 32f;
                float wave = MathF.Sin(t * 16 + (float)now * .27f + f * 1.7f);
                Vector2 p = head - direction * (t * (240 + emission * 540)) + normal * (wave * 32 * t + (f - 2) * 7 * t);
                Line(batch, previous, p, FirstSeveranceAttackAccents.Neon(color, opacity * (1 - t) * .8f), (1 - t) * 3 + .5f);
                previous = p;
            }
        }
        if (e.CancelledAt is null && v.IsFiring(authorityTick))
        {
            var hit = v.RayAt(0, authorityTick);
            Accents.Ribbon(batch, new(hit.X, hit.Y), new(hit.DirectionX, hit.DirectionY),
                hit.Length, hit.HalfWidth * 2, Ice, .7f);
        }
    }

    private void DrawAperture(SpriteBatch batch, Vector2 mouth, Vector2 direction, double tick,
        double start, double fire, float open, float emission, Color color, bool reduced)
    {
        float angle = MathF.Atan2(direction.Y, direction.X);
        Vector2 normal = new(-direction.Y, direction.X);
        float radius = 15 + open * 58;
        float imminent = PreRelease(tick, fire, Math.Min(16, fire - start));
        Accents.ChargeFracture(batch, mouth, tick, start, fire, color, reduced, .75f);
        Accents.Halo(batch, mouth, new Vector2(160 + open * 170, 120 + open * 160), color,
            open * (reduced ? .24f : .6f), angle);
        Sprite(batch, new Rectangle(1010, 8, 235, 527), mouth, new Vector2(8 + open * 38, radius * 2),
            angle, FirstSeveranceAttackAccents.Neon(color, open), new(.5f));
        float gathering = open * (1 - Window(tick, fire, fire + 10d));
        for (int f = 0; f < (reduced ? 3 : 8); f++)
        {
            Vector2 previous = mouth;
            for (int s = 1; s <= 24; s++)
            {
                float t = s / 24f;
                float phase = f * MathHelper.TwoPi / 8 + (float)(tick - start) * (.04f + open * .10f) + t * 3;
                float reach = 60 + 190 * open;
                Vector2 p = mouth - direction * (t * reach) + normal * (MathF.Sin(phase) * t * radius * 1.5f);
                Line(batch, previous, p, FirstSeveranceAttackAccents.Neon(color, gathering * (1 - t)), 1 + open * 2.2f);
                previous = p;
            }
        }
        // Iris blades contract while the aperture itself remains open through
        // the jet's decay. All transforms use the same smooth phase envelopes.
        for (int blade = 0; blade < 6; blade++)
        {
            float a = blade * MathHelper.TwoPi / 6 + open * .85f;
            Vector2 edge = mouth + new Vector2(MathF.Cos(a) * .32f, MathF.Sin(a)) .RotatedBy(angle) * (radius + 10);
            Vector2 tip = mouth + (edge - mouth) * (.68f - emission * .18f);
            Line(batch, edge, tip, Color.Lerp(color, Color.White, imminent) * open, 3);
        }
    }

    private void DrawWarning(SpriteBatch batch, Vector2 origin, Vector2 direction, float length,
        float halfWidth, double tick, double start, double fire, Color color, float warning, bool reduced)
    {
        float gather = Window(tick, start, fire);
        if (halfWidth >= 120)
        {
            FirstSeveranceHazardSurface.Draw(batch, Accents, origin, direction, length, halfWidth,
                tick - start, gather, 0, warning, color, reduced);
            return;
        }
        FirstSeveranceBeamMaterial.Draw(batch, Accents, origin, direction, length, halfWidth,
            tick - start, gather, 0, warning, color, reduced);
    }

    private void Sprite(SpriteBatch batch, Rectangle source, Vector2 position, Vector2 size, float angle, Color color, Vector2 pivot)
    {
        Texture2D texture = atlas!.Value;
        // Measured atlas rectangles, not an assumed equal-cell generation grid.
        float factor = texture.Width / 1254f;
        source = new((int)(source.X * factor), (int)(source.Y * factor), (int)(source.Width * factor), (int)(source.Height * factor));
        batch.Draw(texture, position - Main.screenPosition, source, color, angle,
            new Vector2(source.Width, source.Height) * pivot, size / new Vector2(source.Width, source.Height), SpriteEffects.None, 0);
    }
    private static float Frac(float value) => value - MathF.Floor(value);
}
