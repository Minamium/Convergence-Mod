#nullable enable

using System;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceBossVisuals;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// Original code-native fractured shell and lattice optics. All lifetime/geometry
// is read from the accepted stage; none of these textures is a gameplay actor.
internal sealed class FirstSeveranceStageVisuals
{
    private Texture2D[]? plates;
    private FirstSeveranceGridVolley? fadingGrid;
    internal static float RuptureAge(FirstSeveranceCombatProjection combat, double tick)
        => Math.Clamp((float)((tick - combat.BossPhaseStartedTick) / FirstSeveranceBossPhasePlan.Instance.Get(combat.BossPhase).TransitionTicks), 0, 1);
    internal static float CameraWeight(float age) => Ease(age / .18f) * (1 - Ease((age - .78f) / .22f));

    internal void DrawShell(SpriteBatch batch, FirstSeveranceCombatProjection combat, Vector2 center,
        double tick, float reveal, float cast, FirstSeveranceAttackAccents accents, bool reduced)
    {
        bool breaking = combat.Substate == FirstSeveranceSubstate.PhaseTransition && combat.BossPhase == FirstSeveranceBossPhase.Unbound;
        if (!breaking && combat.BossPhase != FirstSeveranceBossPhase.Sealed) return;
        EnsureShell();
        float age = breaking ? RuptureAge(combat, tick) : 0;
        float split = EclosionPeel(age);
        float fade = reveal * (1 - Window(age, .73, .99));
        float charge = breaking ? EclosionPry(age) : cast;
        float breath = MathF.Sin((float)(tick % 36000) * .021f);
        float radius = 246 + breath * 2 + charge * 5;
        Color ion = Color.Lerp(new Color(91, 213, 223), new Color(255, 172, 112), charge * .65f);
        accents.Halo(batch, center, new Vector2(510 + charge * 65), ion, .20f * fade);
        for (int index = 0; index < 8; index++)
        {
            float angle = (index + .5f) * MathF.Tau / 8 - MathF.PI;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            float side = direction.X < 0 ? -1 : 1;
            // Hinged petals peel around their outer rims under the hands. They
            // compress in perspective, rather than exploding radially as tiles.
            Vector2 hinge = direction * (radius * .82f);
            Vector2 offset = new(side * split * (75 + MathF.Abs(direction.Y) * 35),
                split * (30 + Math.Max(0, direction.Y) * 85));
            float roll = side * split * (.15f + MathF.Abs(direction.Y) * .36f);
            Vector2 squash = new(1 - split * .58f, 1 - split * .22f);
            Vector2 Map(Vector2 p) => center + hinge + offset + ((p - hinge) * squash).RotatedBy(roll);
            batch.Draw(plates![index], center + hinge + offset - Main.screenPosition, null,
                Color.Lerp(Color.White, new Color(227, 240, 255), charge * .25f) * fade, roll,
                new Vector2(512) + direction * (512 * .82f),
                squash * ((radius * 2) / 1024f), SpriteEffects.None, 0);
            float ringAngle = index * MathF.Tau / 8 + .03f * breath;
            Vector2 rim = Map(new Vector2(MathF.Cos(ringAngle), MathF.Sin(ringAngle)) * (radius + 12));
            if (!breaking)
                FirstSeveranceAttackAccents.Arc(batch, center, radius + 12, ringAngle, .57f, Gold * fade * .72f, 5);
            else if (!reduced)
            {
                // Stretched membranes remain attached to the retreating casing.
                float tether = Window(age, .20, .36) * (1 - Window(age, .58, .76));
                Vector2 root = center + new Vector2(side * 85, -35 - EclosionEmerge(age) * 60);
                Vector2 last = root;
                for (int n = 1; n <= 18; n++)
                {
                    float t = n / 18f;
                    Vector2 next = Vector2.Lerp(root, rim, t) + new Vector2(0, MathF.Sin(t * MathF.PI) * (50 - split * 90));
                    Line(batch, last, next, FirstSeveranceAttackAccents.Neon(ion, tether * .65f), 1.5f);
                    last = next;
                }
            }
        }
        if (breaking)
        {
            float pressure = Window(age, .12, .34) * (1 - Window(age, .55, .88));
            accents.Halo(batch, center - new Vector2(0, EclosionEmerge(age) * 120),
                new Vector2(95 + split * 145, 360 + split * 240), ion, pressure * (reduced ? .24f : .5f));
        }
    }

    internal void DrawGrid(SpriteBatch batch, FirstSeveranceCombatProjection combat, double tick,
        ulong authorityTick, FirstSeveranceAttackAccents accents, bool reduced)
    {
        if (combat.Substate != FirstSeveranceSubstate.Lattice) { fadingGrid = null; return; }
        if (combat.GridVolley is { } received) fadingGrid = received;
        if (fadingGrid is not { } grid || tick >= grid.EndTick + 20d) return;
        float born = .75f + .25f * Window(tick, grid.StartTick, grid.StartTick + 6d);
        float gather = Window(tick, grid.StartTick, grid.FireTick);
        float emission = Emission(tick, grid.FireTick, grid.EndTick);
        bool active = combat.GridVolley is not null && grid.IsFiring(authorityTick);
        bool warning = tick < grid.FireTick;
        Color color = grid.Pattern % 2 == 0 ? new(64, 229, 255) : new(193, 123, 255);
        foreach (var ray in grid.Rays)
        {
            Vector2 origin = new(ray.X, ray.Y), direction = new(ray.DirectionX, ray.DirectionY);
            float power = warning ? born : active ? 1 : emission * .10f;
            FirstSeveranceBeamMaterial.DrawTooth(batch, accents, origin, direction, ray.Length, ray.HalfWidth,
                tick - grid.StartTick, gather, active ? emission : 0, power, color, reduced);
        }
        Vector2 core = CoreCenter(combat);
        accents.CastSeal(batch, core, tick, grid.StartTick, grid.FireTick, color, reduced, 1.6f);
        foreach (var ray in grid.CoreBeams)
        {
            Vector2 origin = new(ray.X, ray.Y), direction = new(ray.DirectionX, ray.DirectionY);
            Vector2 normal = new(-direction.Y, direction.X);
            Color hot = new(255, 87, 190), inner = new(225, 206, 255);
            float release = active ? Math.Max(.7f, emission) : warning ? 0 : emission * .10f;
            // Entire 144px corridor is foretold; the outer bloom is not a hitbox.
            FirstSeveranceBeamMaterial.DrawVolume(batch, accents, origin, direction, ray.Length, ray.HalfWidth,
                tick - grid.StartTick, gather, active ? emission : 0, warning ? born : release, hot, reduced);
            if (!warning)
            {

                for (int strand = -1; strand <= 1; strand += 2)
                {
                    Vector2 last = origin;
                    for (int n = 1; n <= 48; n++)
                    {
                        float distance = ray.Length * n / 48;
                        Vector2 next = origin + direction * distance + normal *
                            (strand * 39 + MathF.Sin(distance * .032f - (float)(tick % 3600) * .5f + strand) * 10);
                        Line(batch, last, next, FirstSeveranceAttackAccents.Neon(inner, release * .75f), 4);
                        last = next;
                    }
                }
            }
            accents.Halo(batch, origin, new Vector2(180 + gather * 150), inner,
                (warning ? born * gather * .34f : release * .75f) * (reduced ? .4f : 1));
        }
    }

    private void EnsureShell()
    {
        if (plates is not null || Main.dedServ) return;
        const int size = 1024;
        var original = ModContent.Request<Texture2D>("Convergence/Assets/Textures/NPCs/NullCantorShell",
            ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        var pixels = new Color[original.Width * original.Height];
        original.GetData(pixels);
        var data = new Color[8][];
        for (int i = 0; i < 8; i++) data[i] = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - 511.5f) / 511f, ny = (y - 511.5f) / 511f;
                float radius = MathF.Sqrt(nx * nx + ny * ny);
                float angle = MathF.Atan2(ny, nx) + MathF.PI;
                float warped = angle + .045f * MathF.Sin(radius * 17 + angle * 5);
                float sector = (warped + MathF.Tau) % MathF.Tau / MathF.Tau * 8;
                int index = (int)sector;
                // Preserve authored irregular microfractures and alpha. Sector
                // masks have no gaps until the shell is physically peeled apart.
                data[index][y * size + x] = pixels[(y * original.Height / size) * original.Width + x * original.Width / size];
            }
        plates = new Texture2D[8];
        for (int i = 0; i < 8; i++)
        {
            plates[i] = new Texture2D(Main.instance.GraphicsDevice, size, size);
            plates[i].SetData(data[i]);
        }
    }

    internal void Reset(bool unload = false)
    {
        fadingGrid = null;
        if (!unload || plates is null) return;
        var old = plates; plates = null;
        Main.QueueMainThreadAction(() => { foreach (var plate in old) plate.Dispose(); });
    }
}
