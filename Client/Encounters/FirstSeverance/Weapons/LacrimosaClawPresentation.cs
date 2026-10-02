#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NVector2 = System.Numerics.Vector2;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// The eight exported hand sprites (DW01 / DW01B at k=2 and k=1). The game reads them through DollWeaponTextures
// on the frame they draw; the offline preview loads the same PNGs.
internal readonly record struct LacrimosaClawSprites(Texture2D? Open, Texture2D? OpenLarge, Texture2D? Rake, Texture2D? RakeLarge,
    Texture2D? Clench, Texture2D? ClenchLarge, Texture2D? Thrust, Texture2D? ThrustLarge)
{
    internal Texture2D? For(LacrimosaPose pose, bool large) => pose switch
    {
        LacrimosaPose.Rake => large ? RakeLarge : Rake,
        LacrimosaPose.Clench => large ? ClenchLarge : Clench,
        LacrimosaPose.Thrust => large ? ThrustLarge : Thrust,
        _ => large ? OpenLarge : Open,
    };
}

// The claw's Luminance material (Assets/AutoloadedEffects/Shaders/DollClawEnergy.fx), bound per energy batch.
// The effect comes from a resolver: Luminance's ShaderManager in game, the compiled .fxc in the offline preview.
internal sealed class LacrimosaClawMaterial : IDollEnergyMaterial
{
    internal const string ShaderName = "Convergence.DollClawEnergy";
    internal const int Talon = 0, Flare = 1, Ring = 2, Void = 3;
    private readonly Func<Effect?> resolve;

    internal LacrimosaClawMaterial(Func<Effect?> resolve) => this.resolve = resolve;

    public bool Apply(GraphicsDevice device, in DollEnergyContext context, int pass)
    {
        Effect? effect = resolve();
        if (effect is null || pass < Talon || pass > Void) return false;
        DollPixelArt.Set(effect, "uWorldViewProjection", context.Projection);
        DollPixelArt.Set(effect, "dotOrigin", context.DotOrigin);
        DollPixelArt.Set(effect, "clock", (float)(context.Clock % 7200.0));
        effect.CurrentTechnique.Passes[pass].Apply();
        return true;
    }
}

// Everything one claw owner's hands need for one frame, read from the replicated controller and grasp (or made up
// by the offline preview). Ages are whole accepted ticks; the canvas supplies the render fraction.
internal struct LacrimosaClawDrawState
{
    internal Vector2 Center;          // the owner's mounted centre as drawn
    internal float GravDir;
    internal int Facing;              // the owner's direction (rest pose and idle)
    internal int Stroke;              // LacrimosaClawMotion.None or A/B/C
    internal float Aim;
    internal int Age, Duration;       // stroke age (keeps counting after the stroke) and its length
    internal int Serial;              // changes with every stroke
    internal int Beads;               // lit beads 0..6
    internal bool Owner;              // the local player's own claws (light at full strength)
    internal int ImpactSerial;        // the stroke serial of the latest contact, 0 for none
    internal Vector2 ImpactAt;
    internal bool Grasping;           // a grasp of this owner exists
    internal int GraspId, GraspAge;
    internal Vector2 GraspCenter;
    internal float GraspHalfExtent;
    internal int GraspSide;
    internal bool GraspHeld;          // an NPC was taken (an empty grasp closes on air)
    internal bool HasAimTarget;       // owner only, six beads lit: the body a grasp would take
    internal Rectangle AimTarget;
    internal double DryClick;         // canvas clock of the owner's last early right click (or -1)
    internal LacrimosaClawSprites Sprites;
    internal IDollEnergyMaterial? Material;
}

// Presentation memory of one controller on one client (what was shown, when a phase began, recent events).
internal sealed class LacrimosaClawMemory
{
    internal readonly LacrimosaHand[] Shown = new LacrimosaHand[2];
    internal readonly LacrimosaHand[] From = new LacrimosaHand[2];
    internal bool HasShown;
    internal int PhaseKey = int.MinValue;
    internal double PhaseStart;
    internal int LastBeads = -1;
    internal double BeadLit = -1000;
    internal int BeadIndex;
    internal int SeenImpact;
    internal double ImpactClock = -1000;
    internal Vector2 ImpactAt;
    internal int ClapSerial = -1;
    internal double ClapClock = -1000;
    internal Vector2 ClapAt;
    internal float ClapAxis, ClapGravDir = 1;
    internal int GraspSeen = -1;
    internal double GraspClock = -1000;
    internal bool Striking, Holding;
}

// Draws Lacrimosa's Claws into the shared Doll weapon layer: the floating hands (pixel sprites, two integer rungs
// swapped under a pearl flash), the heart beads, the live scratches of the rakes, the clap's slit, ring and pipe
// bars, the grasp's forecast, flight wakes, held core and crush, and their residue. Pure FNA: no Terraria types, so
// tools/fixtures/DollClawsPreview.cs renders exactly this code offline. Hits never read anything here.
internal static class LacrimosaClawPresentation
{
    internal const float PeerLight = DollWeaponCanvas.PeerLightAlpha, PeerVoid = DollWeaponCanvas.PeerVoidAlpha;
    internal const int RakeResidue = 10, ClapLife = 16, ImpactLife = 10, ReturnTicks = 14, TurnTicks = 8;
    // The crush's light and debris cool to plum and end by the grasp's last tick (the grasp projectile's lifetime
    // after the crush), so nothing is cut off when it despawns; Reduced Effects halves it.
    internal const int CrushResidue = LacrimosaClawMotion.GraspEnd - LacrimosaClawMotion.GraspCrush, CrushHot = 6;
    internal const int HeartbeatPeriod = 48, PingTicks = 10, DryTicks = 10;
    private const int TrailSamples = 12;
    // Base ticks of a rake's path kept as its scratch.
    private const float TrailTicks = 5;
    private static readonly Vector2[] spine = new Vector2[TrailSamples];

    // The three rake scratches run beside the longest talon's path, offset along the hand axis (px from its tip),
    // so they read as parallel cuts rather than one band.
    private static readonly float[] ScratchOffsets = { -30, 0, 22 };

    internal static bool Emit(DollWeaponCanvas canvas, in LacrimosaClawDrawState state, LacrimosaClawMemory memory)
    {
        float fraction = canvas.Fraction;
        double clock = canvas.Clock;
        bool reduced = canvas.Reduced;
        float light = state.Owner ? 1f : PeerLight, voidAlpha = state.Owner ? 1f : PeerVoid;
        int stroke = state.Stroke;
        int duration = stroke == LacrimosaClawMotion.None ? 0 : LacrimosaClawMotion.ValidDuration(stroke, state.Duration);
        bool striking = stroke != LacrimosaClawMotion.None && state.Age < duration;
        float graspAge = state.Grasping ? MathF.Max(0, state.GraspAge - 1 + fraction) : 0;
        bool holding = state.Grasping && graspAge < LacrimosaClawMotion.GraspRelease;
        int facing = striking ? LacrimosaClawMotion.Facing(state.Aim) : state.Facing < 0 ? -1 : 1;
        float mirror = LacrimosaClawMotion.Mirror(facing, state.GravDir);
        float aim = striking ? state.Aim : facing < 0 ? MathF.PI : 0;
        float baseAge = striking ? LacrimosaClawMotion.DrawBaseAge(stroke, state.Age, duration, fraction) : 0;

        // Phases: a stroke (its serial), a grasp (its id), rest (its facing). On a change the hands blend from where
        // they were shown.
        int key = holding ? 1_000_000 + state.GraspId : striking ? state.Serial : -2 - (facing < 0 ? 1 : 0);
        if (key != memory.PhaseKey)
        {
            memory.PhaseKey = key;
            memory.PhaseStart = clock;
            for (int h = 0; h < 2; h++) memory.From[h] = memory.HasShown ? memory.Shown[h] : Rest(state, h, facing, mirror, clock);
        }
        memory.Striking = striking && !holding;
        memory.Holding = holding;
        float inPhase = (float)(clock - memory.PhaseStart);

        // Bead bookkeeping (a bead that lights pings; the meter is drawn the same on both hands).
        if (state.Beads > memory.LastBeads && memory.LastBeads >= 0)
        {
            memory.BeadLit = clock;
            memory.BeadIndex = Math.Clamp(state.Beads - 1, 0, LacrimosaHeart.Beads - 1);
        }
        memory.LastBeads = state.Beads;
        if (state.ImpactSerial != 0 && state.ImpactSerial != memory.SeenImpact)
        {
            memory.SeenImpact = state.ImpactSerial;
            memory.ImpactClock = clock;
            memory.ImpactAt = state.ImpactAt;
        }

        // ---- Hands ------------------------------------------------------------------------------
        for (int hand = 0; hand < 2; hand++)
        {
            LacrimosaHand frame;
            if (holding)
            {
                LacrimosaHand rest = Rest(state, hand, facing, mirror, clock);
                LacrimosaHand from = memory.From[hand];
                LacrimosaHand grasp = LacrimosaClawMotion.GraspHand(hand, graspAge, N(state.GraspCenter), state.GraspHalfExtent, state.GraspSide,
                    from.Wrist, from.Axis, rest.Wrist, rest.Axis);
                frame = grasp;
            }
            else if (striking)
            {
                LacrimosaHand track = World(state, LacrimosaClawMotion.Frame(stroke, hand, baseAge), aim, facing);
                // On track by the live start (the idle hand within TurnTicks): the hit shape is never blended.
                float settle = LacrimosaClawMotion.Active(stroke, hand) ? LacrimosaClawMotion.LiveStart(stroke) : TurnTicks;
                float w = LacrimosaClawMotion.Smooth(baseAge / settle);
                frame = Blend(memory.From[hand], track, w);
            }
            else
            {
                float w = LacrimosaClawMotion.Smooth(inPhase / ReturnTicks);
                frame = Blend(memory.From[hand], Rest(state, hand, facing, mirror, clock), w);
            }
            memory.Shown[hand] = frame;
        }
        memory.HasShown = true;

        // Draw order: in a rake the idle hand first; in the clap and the grasp the right hand first.
        int first = striking && stroke == LacrimosaClawMotion.RakeDown ? LacrimosaClawMotion.Left : LacrimosaClawMotion.Right;
        if (striking && stroke == LacrimosaClawMotion.RakeUp) first = LacrimosaClawMotion.Right;
        for (int n = 0; n < 2; n++)
        {
            int hand = n == 0 ? first : 1 - first;
            DrawHand(canvas, state, memory, hand, memory.Shown[hand], holding ? state.GraspSide : mirror, holding, graspAge, clock, light, n);
        }

        // ---- Light --------------------------------------------------------------------------------
        if (state.Material is { } material)
        {
            if (striking && stroke != LacrimosaClawMotion.Clap) RakeScratches(canvas, state, material, stroke, baseAge, aim, facing, mirror, light, reduced);
            if (striking && stroke == LacrimosaClawMotion.Clap) ClapDrive(canvas, state, memory, material, baseAge, aim, facing, mirror, light, clock);
            ClapBurst(canvas, memory, material, clock, light, reduced);
            if (state.Grasping) Grasp(canvas, state, memory, material, graspAge, clock, light, voidAlpha, reduced);
        }
        Impact(canvas, memory, clock, light);
        if (state.HasAimTarget && !state.Grasping) Bracket(canvas, state.AimTarget, clock);
        return true;
    }

    // ---- Frames -----------------------------------------------------------------------------------

    private static LacrimosaHand Rest(in LacrimosaClawDrawState state, int hand, int facing, float mirror, double clock)
    {
        LacrimosaHand rest = LacrimosaClawMotion.Rest(hand);
        // A 2 px breathing bob in whole pixels, the hands out of phase.
        float bob = MathF.Round(MathF.Sin((float)(clock * MathF.Tau / 120.0) + hand * 1.7f) * 2f);
        LacrimosaHand world = World(state, rest, facing < 0 ? MathF.PI : 0, facing);
        return world with { Wrist = world.Wrist + new NVector2(0, bob * (state.GravDir < 0 ? -1 : 1)) };
    }

    private static LacrimosaHand World(in LacrimosaClawDrawState state, LacrimosaHand local, float aim, int facing)
    {
        NVector2 offset = LacrimosaClawMotion.ToWorld(local.Wrist, aim, facing, state.GravDir);
        return local with
        {
            Wrist = N(state.Center) + offset,
            Axis = LacrimosaClawMotion.AxisToWorld(local.Axis, aim, facing, state.GravDir),
        };
    }

    private static LacrimosaHand Blend(LacrimosaHand from, LacrimosaHand to, float w)
        => w >= 1 ? to : to with
        {
            Wrist = NVector2.Lerp(from.Wrist, to.Wrist, w),
            Axis = LacrimosaClawMotion.LerpAngle(from.Axis, to.Axis, w),
        };

    private static NVector2 N(Vector2 v) => new(v.X, v.Y);
    private static Vector2 X(NVector2 v) => new(v.X, v.Y);
    private static Vector2 Unit(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));

    // ---- Hands and beads --------------------------------------------------------------------------

    private static void DrawHand(DollWeaponCanvas canvas, in LacrimosaClawDrawState state, LacrimosaClawMemory memory, int hand,
        LacrimosaHand frame, float mirror, bool holding, float graspAge, double clock, float light, int depth)
    {
        Texture2D? texture = state.Sprites.For(frame.Pose, frame.Large);
        LacrimosaSpriteArt art = LacrimosaClawArt.Sprite(frame.Pose, frame.Large);
        if (texture is null) return;
        DollFlip flip = LacrimosaClawArt.Flip(hand == LacrimosaClawMotion.Left, mirror);
        float rotation = LacrimosaClawArt.Rotation(frame.Pose, art, frame.Axis, flip);
        var sprite = new DollSprite(texture, new Rectangle(0, 0, texture.Width, texture.Height), new Vector2(art.Pivot.X, art.Pivot.Y));
        canvas.Sprite(sprite, X(frame.Wrist), rotation, flip, DollStratum.Front, (sbyte)depth, new DollSpriteFx { Flash = frame.Flash });

        // Beads: lit beads glow pearl-violet over the art's dark glass; the newest pings; a full meter beats.
        int lit = state.Beads;
        bool spent = false;
        if (holding)
        {
            // The six beads discharge one per tick-and-a-half, the squeezes relight them two by two, the crush
            // flashes them and they are spent.
            lit = graspAge < 3 ? Math.Max(0, 6 - (int)(graspAge * 2.5f)) : 0;
            foreach (int squeeze in LacrimosaClawMotion.SqueezeBeats) if (graspAge >= squeeze) lit += 2;
            lit = Math.Min(lit, LacrimosaHeart.Beads);
            spent = graspAge >= LacrimosaClawMotion.GraspCrushEnd;
            if (spent) lit = 0;
        }
        float sinceLit = (float)(clock - memory.BeadLit);
        float beat = (float)(clock % HeartbeatPeriod);
        bool full = !holding && state.Beads >= LacrimosaHeart.Beads;
        bool pulse = full && (beat < 3 || beat >= 8 && beat < 11);
        bool crushFlash = holding && graspAge >= LacrimosaClawMotion.GraspCrush && graspAge < LacrimosaClawMotion.GraspCrushEnd;
        bool dry = state.DryClick >= 0 && clock - state.DryClick < DryTicks && ((int)(clock - state.DryClick) & 1) == 0;
        int size = frame.Large ? 2 : 1;
        for (int i = 0; i < art.Beads.Length; i++)
        {
            NVector2 at = LacrimosaClawArt.Anchor(art.Beads[i], frame.Pose, art, frame.Wrist, frame.Axis, flip);
            if (i < lit)
            {
                DollTone tone = pulse || crushFlash || sinceLit < 3 && i == memory.BeadIndex ? DollTone.White : DollTone.PearlViolet;
                canvas.Dot(X(at), tone, size, light);
                if (i == memory.BeadIndex && sinceLit < PingTicks && !holding)
                    canvas.Ring(X(at), 3 + sinceLit * 1.4f, sinceLit < 4 ? DollTone.Lilac : DollTone.Violet, 1, light * (1 - sinceLit / PingTicks));
            }
            else if (dry) canvas.Dot(X(at), DollTone.PlumLight, size, light);
        }
        if (pulse && !canvas.Reduced)
        {
            NVector2 grab = LacrimosaClawArt.Anchor(new System.Numerics.Vector2(
                (art.Beads[0].X + art.Beads[^1].X) * .5f, (art.Beads[0].Y + art.Beads[^1].Y) * .5f), frame.Pose, art, frame.Wrist, frame.Axis, flip);
            float t = beat < 8 ? beat : beat - 8;
            canvas.Ring(X(grab), (frame.Large ? 16 : 10) + t * 3, DollTone.Violet, 1, light * (1 - t / 3f));
        }
    }

    // ---- Rakes ------------------------------------------------------------------------------------

    // Three scratches from the talons over the live window, lingering RakeResidue base ticks and cooling to plum.
    private static void RakeScratches(DollWeaponCanvas canvas, in LacrimosaClawDrawState state, IDollEnergyMaterial material, int stroke,
        float baseAge, float aim, int facing, float mirror, float light, bool reduced)
    {
        int hand = stroke == LacrimosaClawMotion.RakeUp ? LacrimosaClawMotion.Left : LacrimosaClawMotion.Right;
        // The scratch covers exactly the path the hits swept: from the live start to the last live tick's sample.
        float start = LacrimosaClawMotion.LiveStart(stroke);
        float end = LacrimosaClawMotion.LastLiveBaseAge(stroke, state.Duration);
        float residue = reduced ? RakeResidue / 2f : RakeResidue;
        if (baseAge < start || baseAge > end + residue) return;
        float head = MathF.Min(baseAge, end), tail = MathF.Max(start - .6f, head - TrailTicks);
        if (head - tail < .05f) return;
        float fadeHead = Math.Clamp((baseAge - end) / residue, 0, 1);
        float fadeTail = Math.Clamp(fadeHead + .6f * (head - tail) / (residue + TrailTicks), 0, 1);
        LacrimosaSpriteArt art = LacrimosaClawArt.RakeLarge;
        DollFlip flip = LacrimosaClawArt.Flip(hand == LacrimosaClawMotion.Left, mirror);
        int talon = Math.Max(0, LacrimosaClawArt.AxisTip(LacrimosaPose.Rake, art));
        for (int r = 0; r < ScratchOffsets.Length; r++)
        {
            for (int i = 0; i < TrailSamples; i++)
            {
                float t = tail + (head - tail) * i / (TrailSamples - 1);
                LacrimosaHand frame = World(state, LacrimosaClawMotion.Frame(stroke, hand, t), aim, facing);
                NVector2 tip = LacrimosaClawArt.Anchor(art.Tips[talon], LacrimosaPose.Rake, art, frame.Wrist, frame.Axis, flip);
                spine[i] = X(tip + new NVector2(MathF.Cos(frame.Axis), MathF.Sin(frame.Axis)) * ScratchOffsets[r]);
            }
            float width = r == 1 ? 16 : 12;
            float heat = r == 1 ? 1f : .85f;
            canvas.EnergyStrip(material, LacrimosaClawMaterial.Talon, spine, width, new Vector4(heat, fadeTail, fadeHead, light), 1);
        }
    }

    // ---- Clap -------------------------------------------------------------------------------------

    // While the palms drive together, each hand's fingertips and heel leave a pressure streak; on contact the
    // source remembers the meeting point so the burst plays out even when the next stroke starts.
    private static void ClapDrive(DollWeaponCanvas canvas, in LacrimosaClawDrawState state, LacrimosaClawMemory memory, IDollEnergyMaterial material,
        float baseAge, float aim, int facing, float mirror, float light, double clock)
    {
        float start = LacrimosaClawMotion.LiveStart(LacrimosaClawMotion.Clap), contact = LacrimosaClawMotion.ClapContact;
        if (baseAge >= contact && memory.ClapSerial != state.Serial)
        {
            memory.ClapSerial = state.Serial;
            memory.ClapClock = clock - (baseAge - contact);
            NVector2 local = new(84 + LacrimosaClawMotion.ClapTip * .5f, 0);
            memory.ClapAt = X(N(state.Center) + LacrimosaClawMotion.ToWorld(local, aim, facing, state.GravDir));
            memory.ClapAxis = aim;
            memory.ClapGravDir = state.GravDir < 0 ? -1 : 1;
        }
        if (baseAge < start - 2 || baseAge > contact + 3) return;
        float head = MathF.Min(baseAge, contact), tail = MathF.Max(start - 3, head - 5);
        if (head - tail < .05f) return;
        float fade = Math.Clamp((baseAge - contact) / 3f, 0, 1);
        LacrimosaSpriteArt art = LacrimosaClawArt.ThrustLarge;
        for (int hand = 0; hand < 2; hand++)
        {
            DollFlip flip = LacrimosaClawArt.Flip(hand == LacrimosaClawMotion.Left, mirror);
            int tip = Math.Max(0, LacrimosaClawArt.AxisTip(LacrimosaPose.Thrust, art));
            for (int i = 0; i < TrailSamples; i++)
            {
                float t = tail + (head - tail) * i / (TrailSamples - 1);
                LacrimosaHand frame = World(state, LacrimosaClawMotion.Frame(LacrimosaClawMotion.Clap, hand, t), aim, facing);
                spine[i] = X(LacrimosaClawArt.Anchor(art.Tips[tip], LacrimosaPose.Thrust, art, frame.Wrist, frame.Axis, flip));
            }
            canvas.EnergyStrip(material, LacrimosaClawMaterial.Talon, spine, 14, new Vector4(.85f, Math.Min(1, fade + .55f), fade, light), 1);
        }
    }

    // A pearl slit between the palms, a violet ring and three organ-pipe breaths rising from it.
    private static void ClapBurst(DollWeaponCanvas canvas, LacrimosaClawMemory memory, IDollEnergyMaterial material, double clock, float light, bool reduced)
    {
        float t = (float)(clock - memory.ClapClock);
        float life = reduced ? ClapLife * .5f : ClapLife;
        if (t < 0 || t > life) return;
        float grow = LacrimosaClawMotion.Smooth(t / 10f), fade = Math.Clamp(t / life, 0, 1);
        Vector2 along = Unit(memory.ClapAxis), across = new(-along.Y, along.X);
        Vector2 at = memory.ClapAt;
        float scale = reduced ? .8f : 1f;
        float slit = (120 + 140 * grow) * scale;
        canvas.EnergyQuad(material, LacrimosaClawMaterial.Flare, at - along * slit * .5f, at + along * slit * .5f, 10 + 8 * (1 - grow),
            new Vector4(1f, fade, light, 0), 2);
        float radius = 18 + 72 * grow * scale;
        Vector2 ringA = at - Vector2.UnitX * (radius + 8), ringB = at + Vector2.UnitX * (radius + 8);
        canvas.EnergyQuad(material, LacrimosaClawMaterial.Ring, ringA, ringB, (radius + 8) * 2,
            new Vector4(radius / (radius + 8), 2.5f - fade, fade, light), 2);
        if (reduced) return;
        // Three organ-pipe breaths stand on the slit's upper side (the owner's up), the tallest in the middle, each
        // tapered, leaning a little outward and lifting off the slit as it cools, in turn: pipes speaking, never
        // marks crossing a line (that read as a ruler) nor a ruled set of even lines (Scarlet's stave).
        Vector2 up = across.Y * memory.ClapGravDir <= 0 ? across : -across;
        for (int k = -1; k <= 1; k++)
        {
            float delay = MathF.Abs(k) * 1.5f, rise = LacrimosaClawMotion.Smooth((t - delay) / 5f);
            if (rise <= 0) continue;
            float height = (k == 0 ? 64 : k < 0 ? 46 : 38) * rise * (1 - .5f * fade);
            Vector2 lean = Vector2.Normalize(up + along * (.24f * k));
            Vector2 foot = at + along * (k < 0 ? -30 : k * 36) + up * (3 + 16 * fade);
            canvas.EnergyQuad(material, LacrimosaClawMaterial.Flare, foot, foot + lean * height, 10 - 4 * fade,
                new Vector4(.8f, Math.Min(1, fade * 1.2f), light, 0), 2);
        }
        canvas.Burst(at, 7019 + (int)memory.ClapClock, 14, t, life, 3.6f, .02f, DollShardKind.Spark, MathF.Tau, 0);
    }

    // ---- Grasp ------------------------------------------------------------------------------------

    private static void Grasp(DollWeaponCanvas canvas, in LacrimosaClawDrawState state, LacrimosaClawMemory memory, IDollEnergyMaterial material,
        float age, double clock, float light, float voidAlpha, bool reduced)
    {
        if (state.GraspId != memory.GraspSeen)
        {
            memory.GraspSeen = state.GraspId;
            memory.GraspClock = clock - age;
        }
        Vector2 center = state.GraspCenter;
        // Forecast: the crush ellipse as a one-dot pearl-violet hairline with travelling heads, until the crush.
        if (age < LacrimosaClawMotion.GraspCrush)
        {
            float progress = Math.Clamp(age / 6f + .05f, 0, 1);
            const int segments = 28;
            for (int s = 0; s < segments; s++)
            {
                float a0 = s * MathF.Tau / segments, a1 = (s + 1) * MathF.Tau / segments;
                float drawn = Math.Clamp(progress * segments - s, 0, 1);
                if (drawn <= 0) break;
                Vector2 p0 = center + new Vector2(MathF.Cos(a0) * LacrimosaClawMotion.CrushRadiusX, MathF.Sin(a0) * LacrimosaClawMotion.CrushRadiusY);
                Vector2 p1 = center + new Vector2(MathF.Cos(a1) * LacrimosaClawMotion.CrushRadiusX, MathF.Sin(a1) * LacrimosaClawMotion.CrushRadiusY);
                canvas.Forecast(p0, p1, drawn, light);
            }
        }
        // Flight wakes behind both hands (violet-pearl), sampled from the flight curve itself.
        if (age >= LacrimosaClawMotion.GraspFlight && age < LacrimosaClawMotion.GraspArrive + 3)
        {
            // The wake trails the hand (its head 1.5 ticks behind), so the flying hand stays clear of it.
            float head = MathF.Min(age - 1.5f, LacrimosaClawMotion.GraspArrive), tail = MathF.Max(LacrimosaClawMotion.GraspFlight, head - 5);
            float fade = Math.Clamp((age - LacrimosaClawMotion.GraspArrive) / 3f, 0, 1);
            for (int hand = 0; hand < 2 && head - tail > .05f; hand++)
            {
                LacrimosaHand from = memory.From[hand];
                LacrimosaHand rest = memory.Shown[hand];
                for (int i = 0; i < TrailSamples; i++)
                {
                    float t = tail + (head - tail) * i / (TrailSamples - 1);
                    LacrimosaHand frame = LacrimosaClawMotion.GraspHand(hand, t, N(center), state.GraspHalfExtent, state.GraspSide,
                        from.Wrist, from.Axis, rest.Wrist, rest.Axis);
                    spine[i] = X(frame.Wrist + new NVector2(MathF.Cos(frame.Axis), MathF.Sin(frame.Axis)) * 70);
                }
                canvas.EnergyStrip(material, LacrimosaClawMaterial.Talon, spine, 14, new Vector4(.7f, Math.Min(1, fade + .5f), fade, light), 1);
            }
        }
        // Contact: a star where the fists close (an empty grasp claps on air: a smaller, duller star).
        float contact = age - LacrimosaClawMotion.GraspContact;
        if (contact >= 0 && contact < ImpactLife) Star(canvas, center, state.GraspHeld ? 34 : 20, contact / ImpactLife, light);
        if (contact >= 0 && contact < 14 && state.GraspHeld)
            canvas.Burst(center, 811 + state.GraspId, 8, contact, 14, 3f, .14f, DollShardKind.Porcelain);
        // Hold: a dark core grows between the fists; each squeeze sends a small ring.
        float crush = age - LacrimosaClawMotion.GraspCrush;
        if (age >= LacrimosaClawMotion.GraspContact && crush < 0)
        {
            float swell = 8 + 14 * LacrimosaClawMotion.Smooth((age - LacrimosaClawMotion.GraspContact) / 20f)
                + 12 * LacrimosaClawMotion.Smooth((age - LacrimosaClawMotion.GraspBrace) / 2f);
            VoidDisc(canvas, material, center, swell, .3f, 0, voidAlpha);
            foreach (int beat in LacrimosaClawMotion.SqueezeBeats)
            {
                float since = age - beat;
                if (since < 0 || since >= 6) continue;
                float radius = swell + 6 + since * 5;
                canvas.EnergyQuad(material, LacrimosaClawMaterial.Ring, center - Vector2.UnitX * (radius + 6), center + Vector2.UnitX * (radius + 6),
                    (radius + 6) * 2, new Vector4(radius / (radius + 6), 1.5f, since / 6f, light), 2);
            }
        }
        // Crush: the lacuna opens inside a violet ring, a vertical pearl flare grows to 330 px, porcelain shatters.
        // Hot for CrushHot ticks, then everything cools to plum and the debris ends by `residue` (the grasp's end).
        float residue = reduced ? CrushResidue / 2f : CrushResidue;
        if (crush >= 0 && crush < residue)
        {
            float grow = LacrimosaClawMotion.Smooth(crush / 12f), fade = Math.Clamp((crush - CrushHot) / (residue - CrushHot), 0, 1);
            float scale = reduced ? .8f : 1f;
            // The fists slam on a small dark core; the lacuna opens as they burst apart, then closes by `residue`.
            float half = residue * .5f;
            float lacuna = (14 + 48 * LacrimosaClawMotion.Smooth((crush - 3) / 6f)) * (1 - LacrimosaClawMotion.Smooth((crush - half) / half));
            if (lacuna > 2) VoidDisc(canvas, material, center, lacuna * scale, .8f, Math.Clamp((crush - residue * .4f) / half, 0, 1), voidAlpha);
            float ring = (60 + 90 * grow) * scale;
            canvas.EnergyQuad(material, LacrimosaClawMaterial.Ring, center - Vector2.UnitX * (ring + 10), center + Vector2.UnitX * (ring + 10),
                (ring + 10) * 2, new Vector4(ring / (ring + 10), 3.5f - 2 * fade, fade, light), 3);
            float flare = 330 * grow * scale;
            canvas.EnergyQuad(material, LacrimosaClawMaterial.Flare, center - Vector2.UnitY * flare * .5f, center + Vector2.UnitY * flare * .5f,
                16 - 6 * grow, new Vector4(1f, fade, light, 0), 3);
            if (crush < 3) Star(canvas, center, 46, crush / 3f, light);
            canvas.Burst(center, 4093 + state.GraspId, 24, crush, residue, 5.2f, .16f, DollShardKind.Porcelain);
            canvas.Burst(center, 4099 + state.GraspId, 16, crush, residue * .9f, 6.5f, .03f, DollShardKind.Spark);
            canvas.Burst(center, 4111 + state.GraspId, 10, crush, residue, 2.4f, -.02f, DollShardKind.Pearl);
        }
    }

    private static void VoidDisc(DollWeaponCanvas canvas, IDollEnergyMaterial material, Vector2 center, float radius, float swirl, float fade, float alpha)
    {
        float half = radius + 4;
        canvas.EnergyQuad(material, LacrimosaClawMaterial.Void, center - Vector2.UnitX * half, center + Vector2.UnitX * half, half * 2,
            new Vector4(radius / half, swirl, fade, alpha), 2);
    }

    // ---- Contacts and aim ---------------------------------------------------------------------------

    private static void Impact(DollWeaponCanvas canvas, LacrimosaClawMemory memory, double clock, float light)
    {
        float t = (float)(clock - memory.ImpactClock);
        if (t < 0 || t >= ImpactLife) return;
        Star(canvas, memory.ImpactAt, 22, t / ImpactLife, light);
        canvas.Burst(memory.ImpactAt, 313 + memory.SeenImpact, 4, t, ImpactLife, 2.6f, .12f, DollShardKind.Porcelain);
    }

    // A small violet contact star: four long and four short spokes, white at first, shrinking.
    private static void Star(DollWeaponCanvas canvas, Vector2 at, float size, float t, float light)
    {
        float reach = size * (1 - t * .6f);
        DollTone hot = t < .3f ? DollTone.White : t < .6f ? DollTone.PearlViolet : DollTone.Lilac;
        for (int k = 0; k < 8; k++)
        {
            float angle = k * MathF.PI / 4 + .2f;
            float length = (k % 2 == 0 ? reach : reach * .45f);
            canvas.Line(at, at + Unit(angle) * length, k % 2 == 0 ? hot : DollTone.Violet, k % 2 == 0 && t < .4f ? 2 : 1, light);
        }
        canvas.Dot(at, DollTone.White, t < .5f ? 3 : 2, light);
    }

    // Owner only, six beads lit: brass corner brackets on the body a grasp would take.
    private static void Bracket(DollWeaponCanvas canvas, Rectangle box, double clock)
    {
        float breathe = 6 + MathF.Round(MathF.Sin((float)(clock * .15)) * 2);
        float x0 = box.Left - breathe, y0 = box.Top - breathe, x1 = box.Right + breathe, y1 = box.Bottom + breathe;
        float arm = MathF.Min(14, MathF.Min(x1 - x0, y1 - y0) * .3f);
        for (int corner = 0; corner < 4; corner++)
        {
            bool right = (corner & 1) != 0, bottom = (corner & 2) != 0;
            float x = right ? x1 : x0, y = bottom ? y1 : y0, dx = right ? -arm : arm, dy = bottom ? -arm : arm;
            canvas.Line(new Vector2(x, y), new Vector2(x + dx, y), DollTone.BrassLight, 1);
            canvas.Line(new Vector2(x, y), new Vector2(x, y + dy), DollTone.BrassLight, 1);
        }
    }

    // Composite arm rotations toward the shown wrists, in DollWeaponArmDraw's convention: the angle an upright owner
    // would use (SetCompositeArm: world angle - pi/2); reversed gravity is mirrored back here and again by the draw.
    internal static bool ArmPose(LacrimosaClawMemory memory, Vector2 shoulder, float gravDir, out float front, out float? back)
    {
        front = 0;
        back = null;
        if (!memory.HasShown || !(memory.Striking || memory.Holding)) return false;
        Vector2 right = X(memory.Shown[LacrimosaClawMotion.Right].Wrist) - shoulder, left = X(memory.Shown[LacrimosaClawMotion.Left].Wrist) - shoulder;
        if (gravDir < 0) { right.Y = -right.Y; left.Y = -left.Y; }
        front = MathF.Atan2(right.Y, right.X) - MathF.PI / 2;
        back = MathF.Atan2(left.Y, left.X) - MathF.PI / 2;
        return true;
    }
}
