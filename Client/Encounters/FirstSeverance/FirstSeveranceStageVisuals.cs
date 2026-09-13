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
        var suspension=FirstSeveranceShellSurface.Suspension(tick,reduced);
        float poseWeight=1-Window(age,.1,.4);
        float shellRoll=suspension.Roll*poseWeight;
        Vector2 fixedCenter=center;
        FirstSeveranceDollVisuals.DrawShellCords(batch,new(combat.CoreX,combat.CoreY),fixedCenter,tick,1,
            fade*(1-Window(age,.24,.58)),shellRoll,poseWeight,reduced);
        center+=new Vector2(suspension.Offset.X,suspension.Offset.Y)*poseWeight;
        Color ion = Color.Lerp(new Color(179, 165, 158), new Color(229, 197, 147), charge * .65f);
        accents.Halo(batch, center, new Vector2(510 + charge * 65), ion, .20f * fade);
        // Adjacent alpha-filtered petals darken each other's edge at rest. Draw
        // the original intact surface until they actually begin to separate.
        if(split<=.001f)
            batch.Draw(shellTexture!,center-Main.screenPosition,null,Color.White*fade,shellRoll,
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
            Vector2 Map(Vector2 p) => center + (hinge + offset + ((p - hinge) * squash).RotatedBy(roll)).RotatedBy(shellRoll);
            batch.Draw(plates![index], center + (hinge + offset).RotatedBy(shellRoll) - Main.screenPosition, null,
                Color.Lerp(Color.White, new Color(227, 240, 255), charge * .25f) * fade, roll+shellRoll,
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
        bool warning = tick < grid.FireTick;
        Color color = grid.Pattern % 2 == 0 ? new(128, 183, 255) : new(193, 123, 255);
        for (int line = 0; line < grid.Rays.Count; line++)
        {
            ulong start = grid.RevealTick(line), fire = grid.LineFireTick(line);
            if (tick < start) continue;
            bool forecast = tick < fire;
            bool live = combat.GridVolley is not null && grid.LineIsLive(line, authorityTick);
            if (forecast)
            {
                var ray = grid.Rays[line];
                FirstSeveranceBeamMaterial.DrawTooth(batch, accents, new(ray.X, ray.Y), new(ray.DirectionX, ray.DirectionY),
                    ray.Length, ray.HalfWidth, tick - start, CastTension(tick, start, fire), 0,
                    Arrive(tick - start, 2), color, reduced);
            }
            else if (live)
                FirstSeveranceRaidVfx.GridRibbon(batch, grid.PulseAt(line, tick), tick - fire, color, reduced);
        }
        foreach (var full in grid.CoreBeams)
            FirstSeveranceCoreCannonVisuals.Draw(batch, accents, full, tick, grid.StartTick, grid.FireTick, grid.CoreEndTick,
                combat.GridVolley is not null && authorityTick >= grid.FireTick && authorityTick < grid.CoreEndTick, reduced);
    }

    private FirstSeveranceCoreCannonVolley? fadingCannon;
    internal void DrawFinalCannon(SpriteBatch batch, FirstSeveranceCombatProjection combat, double tick,
        ulong authorityTick, FirstSeveranceAttackAccents accents, bool reduced)
    {
        if (combat.Substate != FirstSeveranceSubstate.FinalBullets) { fadingCannon = null; return; }
        if (combat.CoreCannon is { } received) fadingCannon = received;
        if (fadingCannon is not { } cannon || tick >= cannon.EndTick + 12d) return;
        FirstSeveranceCoreCannonVisuals.Draw(batch, accents, cannon.Ray, tick, cannon.StartTick, cannon.FireTick, cannon.EndTick,
            combat.CoreCannon is not null && authorityTick >= cannon.FireTick && authorityTick < cannon.EndTick, reduced);
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
        fadingCannon = null;
        fadingGrid = null;
        if (!unload || plates is null) return;
        var old = plates; plates = null; shellTexture = null; // Content manager owns the original.
        Main.QueueMainThreadAction(() => { foreach (var plate in old) plate.Dispose(); });
    }
}
