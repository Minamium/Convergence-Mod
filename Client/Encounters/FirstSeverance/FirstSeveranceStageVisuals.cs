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
    private Texture2D? shellTexture;
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
        float radius = FirstSeveranceShellSurface.Radius(tick,charge);
        Vector2 footprint = new(FirstSeveranceShellSurface.Aspect,1);
        Color ion = Color.Lerp(new Color(179, 165, 158), new Color(229, 197, 147), charge * .65f);
        accents.Halo(batch, center, new Vector2(510 + charge * 65), ion, .20f * fade);
        for (int side=-1;side<=1;side+=2)
            FirstSeveranceDollVisuals.Cord(batch,center+new Vector2(side*460,-374),
                center+new Vector2(side*66,-215),fade,(float)tick/60,side,side<0?13:2,reduced);
        // Adjacent alpha-filtered petals darken each other's edge at rest. Draw
        // the original intact surface until they actually begin to separate.
        if(split<=.001f)
            batch.Draw(shellTexture!,center-Main.screenPosition,null,Color.White*fade,0,
                new Vector2(shellTexture!.Width,shellTexture.Height)*.5f,
                footprint*(radius*2)/new Vector2(shellTexture.Width,shellTexture.Height),SpriteEffects.None,0);
        else for (int index = 0; index < 8; index++)
        {
            float angle = (index + .5f) * MathF.Tau / 8 - MathF.PI;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            float side = direction.X < 0 ? -1 : 1;
            // Hinged petals peel around their outer rims under the hands. They
            // compress in perspective, rather than exploding radially as tiles.
            Vector2 hinge = direction * footprint * (radius * .82f);
            Vector2 offset = new(side * split * (75 + MathF.Abs(direction.Y) * 35),
                split * (30 + Math.Max(0, direction.Y) * 85));
            float roll = side * split * (.15f + MathF.Abs(direction.Y) * .36f);
            Vector2 squash = new(1 - split * .58f, 1 - split * .22f);
            Vector2 Map(Vector2 p) => center + hinge + offset + ((p - hinge) * squash).RotatedBy(roll);
            batch.Draw(plates![index], center + hinge + offset - Main.screenPosition, null,
                Color.Lerp(Color.White, new Color(227, 240, 255), charge * .25f) * fade, roll,
                new Vector2(FirstSeveranceShellSurface.MaskSize*.5f) * (Vector2.One+direction*.82f),
                squash * footprint * ((radius * 2) / FirstSeveranceShellSurface.MaskSize), SpriteEffects.None, 0);
            float ringAngle = index * MathF.Tau / 8 + .03f * breath;
            Vector2 rim = Map(new Vector2(MathF.Cos(ringAngle), MathF.Sin(ringAngle)) * footprint * (radius + 12));
            if (breaking && !reduced)
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
            // The body keeps its accepted material. A graduated neck replaces
            // the square cut at the Boss, without changing the collision ray.
            float neck = Math.Min(180, ray.Length);
            FirstSeveranceBeamMaterial.DrawVolume(batch, accents, origin + direction * neck, direction, ray.Length - neck, ray.HalfWidth,
                tick - grid.StartTick, gather, active ? emission : 0, warning ? born : release, hot, reduced);
            DrawCoreMouth(batch, accents, origin, direction, ray.HalfWidth, neck, tick, grid,
                warning ? born : release, active ? emission : 0, hot, reduced);
            if (!warning)
            {

                for (int strand = -1; strand <= 1; strand += 2)
                {
                    Vector2 last = origin;
                    for (int n = 1; n <= 48; n++)
                    {
                        float distance = ray.Length * n / 48;
                        float opening = Window(distance, 0, 180);
                        Vector2 next = origin + direction * distance + normal * opening *
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

    private static void DrawCoreMouth(SpriteBatch batch, FirstSeveranceAttackAccents accents,
        Vector2 origin, Vector2 direction, float halfWidth, float neck, double tick, FirstSeveranceGridVolley grid,
        float opacity, float emission, Color color, bool reduced)
    {
        Vector2 normal = new(-direction.Y, direction.X);
        float charge = CastTension(tick, grid.StartTick, grid.FireTick);
        float open = Aperture(tick, grid.StartTick, grid.FireTick, grid.EndTick);
        float time = (float)(tick - grid.StartTick);
        // The dim full-width substrate still declares the exact danger volume;
        // its live filaments emerge from a lens buried within the chest.
        accents.Ribbon(batch, origin, direction, neck, halfWidth * 2, color, opacity * .30f);
        for (int lane = 0; lane < (reduced ? 7 : 13); lane++)
        {
            float across = (lane / (float)(reduced ? 6 : 12) * 2 - 1) * halfWidth * .83f;
            Vector2 last = origin - direction * 20 + normal * (across * .08f);
            for (int n = 1; n <= 24; n++)
            {
                float t = n / 24f;
                float opening = Window(t, 0, 1);
                float wave = MathF.Sin(t * 14 - FlowPhase(time, .25f, emission) + lane * 1.73f)
                    * MathF.Sin(t * MathF.PI) * 7;
                Vector2 next = origin + direction * (-20 + (neck + 20) * t)
                    + normal * (across * (.08f + .92f * opening) + wave);
                float flow = .7f + .3f * MathF.Sin(t * 16 - time * .22f + lane);
                Line(batch, last, next, FirstSeveranceAttackAccents.Neon(color, opacity * flow * .55f), 4 + emission * 2);
                Line(batch, last, next, FirstSeveranceAttackAccents.Neon(Color.Lerp(color, Color.White, .6f),
                    opacity * flow * (.34f + emission * .56f)), 1 + emission * 1.5f);
                last = next;
            }
        }
        for (int i = 0; i < (reduced ? 3 : 7); i++)
        {
            float a = i * MathF.Tau / 7 + time * .022f;
            Vector2 mouth = origin + direction * (MathF.Cos(a) * (8 + open * 12))
                + normal * (MathF.Sin(a) * (15 + open * 40));
            Vector2 tail = origin - direction * (70 + charge * 45) + normal * (MathF.Sin(a - .7f) * 80);
            Vector2 last = tail;
            for (int n = 1; n <= 12; n++)
            {
                float t = n / 12f;
                Vector2 next = Vector2.Lerp(tail, mouth, t) + normal * MathF.Sin(t * MathF.PI) * MathF.Cos(a) * 18;
                Line(batch, last, next, FirstSeveranceAttackAccents.Neon(color, open * (1 - emission * .4f) * t * .7f), 1.4f);
                last = next;
            }
        }
        accents.Halo(batch, origin, new Vector2(80 + open * 90, 35 + open * 60), color,
            open * .70f, direction.ToRotation());
        accents.Halo(batch, origin + direction * 12, new Vector2(32, 60 + open * 40), Color.White,
            opacity * (.20f + emission * .55f), direction.ToRotation());
    }

    private void EnsureShell()
    {
        if (plates is not null || Main.dedServ) return;
        const int size = FirstSeveranceShellSurface.MaskSize;
        var original = ModContent.Request<Texture2D>(FirstSeveranceShellSurface.TexturePath,
            ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        shellTexture=original;
        var pixels = new Color[original.Width * original.Height];
        original.GetData(pixels);
        var data = new Color[8][];
        for (int i = 0; i < 8; i++) data[i] = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int index = FirstSeveranceShellSurface.Sector(x,y,size);
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
        var old = plates; plates = null; shellTexture = null; // Content manager owns the original.
        Main.QueueMainThreadAction(() => { foreach (var plate in old) plate.Dispose(); });
    }
}
