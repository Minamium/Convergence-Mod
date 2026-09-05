#nullable enable

using System;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

// No world actors, gameplay callbacks, shaders, or network traffic. All spatial
// facts come from the read-only snapshot; animation is disposable client state.
internal sealed class FirstSeveranceBossVisuals
{
    private const string BodyPath = "Convergence/Assets/Textures/NPCs/NullCantorBody";
    private static readonly Color Ice = new(128, 231, 246);
    private static readonly Color Gold = new(155, 132, 88);
    private static readonly Color Danger = new(255, 92, 62);
    private Asset<Texture2D>? body;
    private Texture2D? glow;
    private FightId fight;
    private uint soundedCharge;
    private uint soundedFire;
    private FirstSeveranceSubstate previousPhase;
    private float exposure;
    private Vector2 lastCenter;
    private int endingTicks;
    private int phaseImpactTicks;

    internal float PhaseShake => 9f * MathF.Pow(phaseImpactTicks / 24f, 2f);

    internal static Vector2 CoreCenter(FirstSeveranceCombatProjection combat)
        => new(combat.CoreX, combat.CoreY - FirstSeveranceLanceTuning.BossHeightAboveCore);

    internal void Update(FirstSeveranceClientStateSystem state)
    {
        if (Main.dedServ)
            return;
        FirstSeveranceCombatProjection? combat = state.Combat;
        if (combat is null)
        {
            phaseImpactTicks = 0;
            if (!fight.IsNone)
            {
                fight = FightId.None;
                endingTicks = 45;
            }
            if (endingTicks > 0)
                endingTicks--;
            return;
        }

        if (fight != combat.FightId)
        {
            fight = combat.FightId;
            soundedCharge = soundedFire = 0;
            previousPhase = FirstSeveranceSubstate.None;
            exposure = 0f;
            endingTicks = 0;
            phaseImpactTicks = 0;
        }
        if (phaseImpactTicks > 0)
            phaseImpactTicks--;
        lastCenter = CoreCenter(combat);
        exposure = MathHelper.Lerp(exposure,
            combat.Substate == FirstSeveranceSubstate.CoreExposure ? 1f : 0f, 0.32f);
        bool nearby = Vector2.DistanceSquared(Main.LocalPlayer.Center, lastCenter) < 2400f * 2400f;
        if (previousPhase != combat.Substate)
        {
            if (nearby && combat.Substate == FirstSeveranceSubstate.CoreExposure)
            {
                phaseImpactTicks = 24;
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.95f, Pitch = -0.25f }, lastCenter);
            }
            previousPhase = combat.Substate;
        }
        if (combat.LanceVolley is not { } volley)
            return;
        ulong tick = state.EstimatedAuthorityTick;
        if (volley.Serial != soundedCharge)
        {
            soundedCharge = volley.Serial;
            if (nearby && tick >= volley.StartTick && tick < volley.FireTick)
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.6f, Pitch = 0.1f }, lastCenter);
        }
        if (volley.Serial != soundedFire && tick >= volley.FireTick)
        {
            soundedFire = volley.Serial;
            // Do not replay an expired shot after a snapshot repair or pause.
            if (nearby && volley.IsFiring(tick))
            {
                SoundEngine.PlaySound(SoundID.Item33 with { Volume = 1f, Pitch = -0.3f }, lastCenter);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.35f, Pitch = -0.5f }, lastCenter);
            }
        }
    }

    internal void Draw(SpriteBatch batch, FirstSeveranceCombatProjection combat, ulong tick)
    {
        EnsureAssets();
        bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
        Vector2 center = CoreCenter(combat);
        float time = (float)(tick % 216000) / 60f;
        float intro = combat.Substate == FirstSeveranceSubstate.SpawnIntro
            ? 1f - Math.Clamp((float)((double)combat.ResolveTick - tick)
                / FirstSeveranceEncounterPlan.Instance.Timing.SpawnIntroTicks, 0f, 1f) : 1f;
        float reveal = 1f - MathF.Pow(1f - Math.Min(1f, intro * 2.5f), 3f);
        float breath = MathF.Sin(time * 2.2f);
        float recoil = combat.LanceVolley is { } shot && shot.IsFiring(tick)
            ? MathF.Pow(1f - (tick - shot.FireTick) / (float)FirstSeveranceLanceTuning.ActiveTicks, 3f)
            : 0f;

        if (!reduced)
        {
            Glow(batch, center, 1250f, new Color(1, 7, 16) * (0.85f * reveal));
            Glow(batch, center, 980f, new Color(7, 19, 30) * (0.7f * reveal));
            DrawAurora(batch, center, time, reveal);
        }
        DrawSeal(batch, center, time, reveal, reduced);
        if (!reduced && phaseImpactTicks > 0)
        {
            float impact = 1f - phaseImpactTicks / 24f;
            float spread = 1f - MathF.Pow(1f - impact, 3f);
            Ring(batch, center, 100 + spread * 780, Ice * (1f - impact) * 0.85f, 5f);
            Ring(batch, center, 85 + spread * 590, Gold * (1f - impact), 3f, time, 12);
            Glow(batch, center, 600 * (1f - impact), Additive(Ice, (1f - impact) * 0.8f));
        }

        Texture2D texture = body!.Value;
        // The generated core is at (50%, 44%), not the canvas midpoint. Keep the
        // live hitbox fixed there; only the decorative casing/side structures move.
        Vector2 origin = new(texture.Width * 0.5f, texture.Height * 0.44f);
        float scale = 820f / texture.Height * (0.82f + 0.18f * reveal);
        Color tint = Color.Lerp(new Color(127, 157, 171), Color.White, exposure * 0.85f) * reveal;
        float rotation = breath * 0.008f;
        int split = (int)(texture.Width * 0.3f);
        DrawSlice(batch, texture, new Rectangle(0, 0, split, texture.Height), center,
            origin, scale, rotation - exposure * 0.032f, tint,
            new Vector2(-exposure * 65f - recoil * 18f, breath * 3f));
        DrawSlice(batch, texture, new Rectangle(split, 0, texture.Width - split * 2, texture.Height),
            center, origin, scale, rotation, tint, Vector2.Zero);
        DrawSlice(batch, texture, new Rectangle(texture.Width - split, 0, split, texture.Height),
            center, origin, scale, rotation + exposure * 0.032f, tint,
            new Vector2(exposure * 65f + recoil * 18f, -breath * 3f));

        // A dark aperture makes the single damage target readable over any biome.
        Glow(batch, center, 183f, Color.Black * reveal);
        Color coreColor = Color.Lerp(Gold, Ice, exposure);
        if (exposure > 0.02f)
        {
            Glow(batch, center, 270f, Additive(Ice, exposure * 0.42f * reveal));
            Ring(batch, center, 76f + 4f * breath, coreColor * reveal, 2.5f, time * 0.18f, 8);
        }
        else
        {
            Ring(batch, center, 81f, Gold * (0.6f * reveal), 3f, -time * 0.08f, 8);
            // Closed diagonal braces versus four separated hitbox corners.
            Line(batch, center + new Vector2(-43, -43), center + new Vector2(43, 43), Gold * reveal, 4f);
            Line(batch, center + new Vector2(43, -43), center + new Vector2(-43, 43), Gold * reveal, 4f);
        }
        DrawCoreCorners(batch, center, coreColor * reveal, exposure);

        int shards = reduced ? 8 : 40;
        for (int index = 0; index < shards; index++)
        {
            float angle = index * 2.399963f + time * (index % 2 == 0 ? 0.2f : -0.15f);
            float radius = 300f + index % 5 * 39f + exposure * 38f;
            Vector2 point = center + Unit(angle) * radius + new Vector2(0, MathF.Sin(time + index) * 14f);
            Line(batch, point - Unit(angle + 0.4f) * 9f, point + Unit(angle + 0.4f) * 9f,
                Ice * (0.3f * reveal), index % 3 + 2f);
        }
        DrawLances(batch, combat.LanceVolley, tick, reduced);
    }

    private void DrawSeal(SpriteBatch batch, Vector2 center, float time, float reveal, bool reduced)
    {
        float radius = (460f + exposure * 38f) * (0.7f + 0.3f * reveal);
        float spin = time * 0.18f;
        Ring(batch, center, radius, new Color(19, 37, 46) * reveal, 14f, spin, 12);
        Ring(batch, center, radius - 9f, Ice * (0.26f * reveal), 1.8f, spin, 12);
        Ring(batch, center, radius + 7f, Gold * (0.33f * reveal), 2f, spin, 12);
        if (!reduced)
        {
            Ellipse(batch, center, new Vector2(radius + 80f, radius * 0.38f), -0.55f + spin,
                Ice * (0.2f * reveal), 1.7f);
            Ellipse(batch, center, new Vector2(radius + 60f, radius * 0.38f), 0.55f - spin,
                Gold * (0.18f * reveal), 1.7f);
        }
        for (int index = 0; index < 48; index++)
        {
            float angle = index * MathHelper.TwoPi / 48f + spin;
            Vector2 direction = Unit(angle);
            float tickLength = index % 4 == 0 ? 22f : 7f;
            Line(batch, center + direction * (radius - tickLength), center + direction * radius,
                Ice * (0.34f * reveal), index % 4 == 0 ? 3f : 1.5f);
        }
        for (int index = 0; index < 6; index++)
        {
            float angle = index * MathHelper.TwoPi / 6f - MathHelper.PiOver2 + spin;
            Vector2 direction = Unit(angle);
            Vector2 tangent = new(-direction.Y, direction.X);
            Vector2 anchor = center + direction * (radius + 18f);
            Line(batch, anchor - direction * 30, anchor + direction * 34, new Color(30, 44, 51) * reveal, 30f);
            Line(batch, anchor - direction * 24, anchor + direction * 26, Gold * (0.65f * reveal), 6f);
            Line(batch, anchor - tangent * 22, anchor + tangent * 22, Ice * (0.5f * reveal), 3f);
            if (exposure < 0.9f)
                Line(batch, center + direction * 104, anchor - direction * 34,
                    Ice * (0.15f * reveal * (1f - exposure)), 1.7f);
        }
    }

    private static void DrawAurora(SpriteBatch batch, Vector2 center, float time, float opacity)
    {
        for (int ribbon = 0; ribbon < 4; ribbon++)
        {
            Vector2 previous = default;
            for (int index = 0; index <= 48; index++)
            {
                float x = (index / 48f - 0.5f) * 1800f;
                float y = MathF.Sin(x * 0.004f + time * 0.15f + ribbon * 0.8f) * 110f - 240f + ribbon * 78f;
                Vector2 next = center + new Vector2(x, y);
                if (index > 0)
                    Line(batch, previous, next, new Color(55, 150, 174) * (0.022f * opacity), 42f);
                previous = next;
            }
        }
    }

    private void DrawLances(SpriteBatch batch, FirstSeveranceLanceVolley? volley, ulong tick, bool reduced)
    {
        if (volley is null || tick < volley.StartTick || tick >= volley.EndTick)
            return;
        bool firing = volley.IsFiring(tick);
        float charge = Math.Clamp((float)(tick - volley.StartTick) / FirstSeveranceLanceTuning.TelegraphTicks, 0f, 1f);
        foreach (FirstSeveranceLanceRay ray in volley.Rays)
        {
            Vector2 start = new(ray.X, ray.Y);
            Vector2 direction = new(ray.DirectionX, ray.DirectionY);
            Vector2 normal = new(-ray.DirectionY, ray.DirectionX);
            Vector2 end = start + direction * FirstSeveranceLanceTuning.Length;
            float halfWidth = FirstSeveranceLanceTuning.HalfWidth;
            if (!firing)
            {
                // Whole future hit corridor is visible from the first warning tick.
                Line(batch, start, end, Danger * (0.12f + charge * 0.12f), halfWidth * 2);
                Line(batch, start, end, Color.Lerp(Danger, Color.White, charge * 0.75f), 2f + charge * 2f);
                DashedLine(batch, start + normal * halfWidth, end + normal * halfWidth, Danger * 0.85f, 1.8f);
                DashedLine(batch, start - normal * halfWidth, end - normal * halfWidth, Danger * 0.85f, 1.8f);
                // Travelling chevrons show direction without changing the locked aim.
                for (int index = 1; index < 10; index++)
                {
                    Vector2 point = start + direction * (index * 180f + charge * 280f);
                    Line(batch, point - direction * 14 + normal * 9, point, Danger * 0.55f, 2f);
                    Line(batch, point - direction * 14 - normal * 9, point, Danger * 0.55f, 2f);
                }
                Glow(batch, start, 110f + charge * 220f, Additive(Danger, 0.75f * charge));
                Ring(batch, start, 160f - MathF.Sqrt(charge) * 130f, Danger * (0.6f + charge * 0.4f), 3f);
                if (!reduced)
                    Ring(batch, start, 80f + charge * 20f, Gold * charge, 4f, charge * 1.5f, 8);
                continue;
            }

            float age = (tick - volley.FireTick) / (float)FirstSeveranceLanceTuning.ActiveTicks;
            float intensity = 1f - age * 0.4f;
            if (!reduced)
            {
                Line(batch, start, end, Additive(Danger, 0.18f * intensity), 250f);
                Line(batch, start, end, Additive(Danger, 0.35f * intensity), 154f);
            }
            Line(batch, start, end, new Color(246, 82, 59) * intensity, halfWidth * 2f);
            Line(batch, start, end, new Color(255, 205, 168) * intensity, 66f);
            Line(batch, start, end, Color.White * intensity, 38f);
            Line(batch, start + normal * halfWidth, end + normal * halfWidth, Color.White * 0.9f, 2f);
            Line(batch, start - normal * halfWidth, end - normal * halfWidth, Color.White * 0.9f, 2f);
            Glow(batch, start, reduced ? 170f : 560f, Additive(new Color(255, 194, 147), intensity));
            Ellipse(batch, start + direction * (65f + age * 100f), new Vector2(20, 60 + age * 70),
                MathF.Atan2(direction.Y, direction.X), Color.White * (1f - age), 3f);
            if (!reduced)
            {
                float shock = 1f - MathF.Pow(1f - age, 3f);
                Ring(batch, start, 60f + shock * 370f, Danger * (1f - age) * 0.8f, 5f);
                for (int index = 0; index < 28; index++)
                {
                    float side = index % 2 == 0 ? 1f : -1f;
                    Vector2 p = start + direction * (index * 85f + shock * 520f)
                        + normal * side * (58f + shock * (28 + index % 4 * 18));
                    Line(batch, p, p + direction * (65 + index % 3 * 28),
                        Additive(Danger, intensity * 0.8f), 3f);
                }
            }
        }
    }

    internal void DrawEnding(SpriteBatch batch)
    {
        if (endingTicks <= 0 || glow is null)
            return;
        float progress = 1f - endingTicks / 45f;
        Ring(batch, lastCenter, 80f + progress * 620f, Ice * (1f - progress) * 0.75f, 3f);
        Ring(batch, lastCenter, 80f + progress * 460f, Gold * (1f - progress) * 0.4f, 2f);
        Glow(batch, lastCenter, 440f * (1f - progress), Additive(Ice, (1f - progress) * 0.45f));
    }

    internal static void DrawCoreCorners(SpriteBatch batch, Vector2 center, Color color, float open)
    {
        float radius = 72f; // Exact 144x144 NPC hitbox, not the 820px casing.
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                Vector2 corner = center + new Vector2(x, y) * radius;
                Line(batch, corner, corner - new Vector2(x * (18 + open * 8), 0), color, 3f);
                Line(batch, corner, corner - new Vector2(0, y * (18 + open * 8)), color, 3f);
            }
    }

    private static void DrawSlice(SpriteBatch batch, Texture2D texture, Rectangle source, Vector2 center,
        Vector2 origin, float scale, float rotation, Color color, Vector2 offset)
        => batch.Draw(texture, center + offset - Main.screenPosition, source, color, rotation,
            origin - new Vector2(source.X, source.Y), scale, SpriteEffects.None, 0f);

    internal static void Line(SpriteBatch batch, Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 delta = end - start;
        batch.Draw(TextureAssets.MagicPixel.Value, start - Main.screenPosition,
            new Rectangle(0, 0, 1, 1), color, MathF.Atan2(delta.Y, delta.X), new Vector2(0, 0.5f),
            new Vector2(delta.Length(), width), SpriteEffects.None, 0f);
    }

    internal static void Ring(SpriteBatch batch, Vector2 center, float radius, Color color,
        float width = 2f, float rotation = 0f, int gaps = 0)
    {
        const int count = 96;
        for (int index = 0; index < count; index++)
        {
            if (gaps > 0 && index % (count / gaps) < 2)
                continue;
            float a = rotation + index * MathHelper.TwoPi / count;
            float b = rotation + (index + 1) * MathHelper.TwoPi / count;
            Line(batch, center + Unit(a) * radius, center + Unit(b) * radius, color, width);
        }
    }

    private static void Ellipse(SpriteBatch batch, Vector2 center, Vector2 radius, float rotation, Color color, float width)
    {
        Vector2 previous = center + Rotate(new Vector2(radius.X, 0), rotation);
        for (int index = 1; index <= 96; index++)
        {
            Vector2 point = center + Rotate(Unit(index * MathHelper.TwoPi / 96f) * radius, rotation);
            Line(batch, previous, point, color, width);
            previous = point;
        }
    }

    private static void DashedLine(SpriteBatch batch, Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        Vector2 direction = delta / length;
        for (float at = 0; at < length; at += 44f)
            Line(batch, start + direction * at, start + direction * Math.Min(length, at + 26f), color, width);
    }

    private void Glow(SpriteBatch batch, Vector2 center, float diameter, Color color)
        => batch.Draw(glow!, center - Main.screenPosition, null, color, 0f,
            new Vector2(glow!.Width * 0.5f), diameter / glow.Width, SpriteEffects.None, 0f);

    private static Vector2 Unit(float radians) => new(MathF.Cos(radians), MathF.Sin(radians));
    private static Vector2 Rotate(Vector2 point, float radians)
        => new(point.X * MathF.Cos(radians) - point.Y * MathF.Sin(radians),
            point.X * MathF.Sin(radians) + point.Y * MathF.Cos(radians));
    private static Color Additive(Color color, float opacity) => new Color(color.R, color.G, color.B, 0) * opacity;

    private void EnsureAssets()
    {
        body ??= ModContent.Request<Texture2D>(BodyPath, AssetRequestMode.ImmediateLoad);
        if (glow is not null)
            return;
        // Render-thread-only procedural falloff. Not a borrowed texture or effect.
        const int size = 128;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = new Vector2(x - 63.5f, y - 63.5f).Length() / 64f;
                float alpha = MathF.Pow(Math.Max(0f, 1f - distance), 2f);
                pixels[y * size + x] = Color.White * alpha;
            }
        glow = new Texture2D(Main.instance.GraphicsDevice, size, size);
        glow.SetData(pixels);
    }

    internal void Reset(bool unload = false)
    {
        fight = FightId.None;
        previousPhase = FirstSeveranceSubstate.None;
        soundedCharge = soundedFire = 0;
        endingTicks = 0;
        phaseImpactTicks = 0;
        exposure = 0;
        if (unload)
        {
            Texture2D? oldGlow = glow;
            glow = null;
            body = null; // ReLogic owns the requested body texture.
            if (oldGlow is not null)
                Main.QueueMainThreadAction(oldGlow.Dispose);
        }
    }
}
