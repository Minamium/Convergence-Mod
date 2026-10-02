using System;
using Convergence.Client.Encounters.FirstSeverance;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

// The nine cells are detached parts, not keyframes. All articulation, wisps,
// smoke and traces share one pose clock; SpriteBatch's caller state is untouched.
[Autoload(Side = ModSide.Client)]
internal sealed class GhostSamuraiRigArt : ModSystem
{
    private static bool surfaceParts, endingParts;
    internal const string Path = "Convergence/Assets/Textures/GhostSamurai/VioletRig";
    private static Asset<Texture2D> asset;
    private static Texture2D Texture => (asset ??= ModContent.Request<Texture2D>(Path, AssetRequestMode.ImmediateLoad)).Value;
    private static readonly Rectangle[] parts =
    {
        new(40, 80, 350, 325), new(418 + 60, 40, 305, 358), new(836 + 124, 65, 186, 325),
        new(140, 418 + 75, 145, 305), new(418 + 170, 418 + 5, 88, 410), new(836 + 25, 418 + 55, 360, 272),
        new(153, 836 + 60, 115, 285), new(418 + 22, 836 + 10, 380, 390), new(836 + 105, 836 + 14, 215, 362),
    };
    internal static bool Reduced => ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
    public override void Unload() => asset = null; // ModContent owns this alpha texture.

    internal static void Draw(SpriteBatch batch, in SamuraiRigPose pose, Vector2 screen, SamuraiRigHistory history, double tick)
    {
        if (Main.dedServ) return;
        bool reduced = Reduced;
        if (history is not null && !reduced)
        {
            int drawn = 0;
            for (int i = history.Count - 2; i >= 0 && drawn < SamuraiRigMotion.BodyEchoes; i--)
            {
                var h = history.At(i);
                float old = (float)(tick - h.Tick);
                if (old < 0 || old > 8 || h.Pose.Speed < 24) continue;
                float fade = (1 - old / 9) * .15f;
                var echo = h.Pose with { Scale = h.Pose.Scale * (1 - old * .007f), Hit = 0 };
                Form(batch, echo, screen, Color.Lerp(new Color(92, 87, 185), new Color(182, 128, 235), 1 - old / 9) * fade, true);
                drawn++;
            }
        }
        Smoke(batch, pose, screen, reduced, false, 1);
        GhostSamuraiEnergy.Body(batch, pose, screen, false);
        Color tint = Color.Lerp(Color.White, new Color(243, 214, 255), pose.Hit * .7f) * SamuraiSpriteFrames.BodyOpacity(pose.Age);
        if (!GhostSamuraiComposite.Draw(batch, pose, screen)) DrawCore(batch, pose, screen, tint);
        if (history is not null) GhostSamuraiMaterials.Trails(batch, pose, history, tick);
        if (history is not null) Trails(batch, pose, screen, history, tick, reduced);
        for (int side = -1; side <= 1; side += 2)
        {
            SamuraiBladeMotion blade = side < 0 ? pose.Left : pose.Right;
            if (blade.Trail > 0)
                SwordLightning.Draw(batch, Hand(pose, side), Tip(pose, side),
                    tick, blade.Trail, reduced, 23 + side);
        }
        Smoke(batch, pose, screen, reduced, true, 1);
        Wisps(batch, pose, screen, 1);
        GhostSamuraiEnergy.Body(batch, pose, screen, true);
    }

    internal static void DrawCore(SpriteBatch batch, in SamuraiRigPose pose, Vector2 screen, Color tint, Matrix? projection = null)
    {
        using var scope = new WorldGraphicsScope(batch);
        GhostSamuraiMaterials.Prepare(pose.Age, Math.Max(pose.Left.Charge, pose.Right.Charge), pose.Hit, 0, projection);
        surfaceParts = true;
        try { Form(batch, pose, screen, tint, false); }
        finally { surfaceParts = false; }
    }

    private static Vector2 Root(in SamuraiRigPose p, Vector2 screen) => new Vector2(p.X, p.Y) - screen;
    private static Vector2 Offset(Vector2 v, in SamuraiRigPose p) => v.RotatedBy(p.Lean) * p.Scale;
    private static void Form(SpriteBatch batch, in SamuraiRigPose p, Vector2 screen, Color tint, bool echo)
    {
        Vector2 root = Root(p, screen);
        Ornaments(batch, p, screen, tint, 0);
        // Independent hem strips bend gently without moving the armor or hands.
        Hem(batch, p, screen, tint, 1);
        Part(batch, 0, root, new(.5f, .40f), new Vector2(130, 132) * p.Scale, p.Lean, tint);
        Head(batch, p, screen, tint);
        Arms(batch, p, screen, tint);
        Swords(batch, p, screen, tint, !echo);
        if (p.Hit > 0 && !echo && !surfaceParts)
        {
            Vector2 a = root + Offset(new(-15, -36), p), b = root + Offset(new(12, -5), p);
            GhostSamuraiVisuals.Stroke(batch, a, b, 2, new Color(238, 222, 255) * p.Hit * .7f);
        }
    }

    private static void Head(SpriteBatch batch, in SamuraiRigPose p, Vector2 screen, Color tint)
        => Part(batch, 1, Root(p, screen) + Offset(new(0, -39), p), new(.5f, .88f), new Vector2(112, 122) * p.Scale,
            p.Lean * .45f - p.Lag * .25f + MathF.Sin(p.Age * .029f) * .015f, tint);

    private static void Hem(SpriteBatch batch, in SamuraiRigPose p, Vector2 screen, Color tint, float remaining)
    {
        Rectangle source = parts[7];
        const int strips = 6;
        for (int i = 0; i < strips; i++)
        {
            int y = i * source.Height / strips, next = (i + 1) * source.Height / strips;
            float flow = i / (float)strips;
            float opacity = SamuraiRigMotion.Smooth((remaining - flow) * strips);
            Vector2 position = Root(p, screen) + Offset(new(MathF.Sin(p.Age * .044f - flow * 3) * flow * 9
                - p.Lag * flow * 35 - p.Lean * flow * flow * 90, 57 + flow * 180), p);
            var cell = new Rectangle(source.X, source.Y + y, source.Width, next - y);
            DrawPart(batch, 7, position, cell, tint * opacity, p.Lean, new(source.Width * .5f, 0),
                new Vector2(172f / source.Width, 180f / source.Height) * p.Scale, false);
        }
    }

    private static void Ornaments(SpriteBatch batch, in SamuraiRigPose p, Vector2 screen, Color tint, float scatter)
    {
        Vector2 root = Root(p, screen);
        float rotation = p.Lean * .35f + MathF.Sin(p.Age * .021f + 1.1f) * .025f;
        Part(batch, 5, root + new Vector2(0, -69), new(.5f, .7f), new Vector2(218, 164) * p.Scale, rotation, tint * (1 - scatter));
        for (int i = 0; i < 5; i++)
        {
            float angle = MathHelper.Pi + i * MathHelper.Pi / 4;
            Vector2 direction = angle.ToRotationVector2();
            Vector2 anchor = root + new Vector2(direction.X * 97, -58 + direction.Y * 121).RotatedBy(rotation) * p.Scale;
            anchor += direction * scatter * (80 + i * 9) + new Vector2(0, scatter * scatter * 35);
            float sway = MathF.Sin(p.Age * .052f + i * 1.27f) * .10f - p.Lag * 1.5f;
            sway += MathF.Sin(p.Age * .8f + i) * p.Hit * .3f + scatter * (i - 2);
            if (surfaceParts && !endingParts && GhostSamuraiPresentation.Talisman(i, out var tether, out var tetherAngle))
            { anchor = tether - screen; sway = tetherAngle; }
            Part(batch, 6, anchor, new(.5f, .04f), new Vector2(18, 48) * p.Scale, sway, tint);
        }
    }

    internal static Vector2 Shoulder(in SamuraiRigPose p, int side)
    {
        var blade = side < 0 ? p.Left : p.Right;
        return new Vector2(p.X, p.Y) + Offset(new Vector2(side * (44 + blade.Charge * 7),
            -38 - blade.Charge * 7), p);
    }
    internal static Vector2 Hand(in SamuraiRigPose p, int side)
    {
        var blade = side < 0 ? p.Left : p.Right;
        // The shoulder leads the blade into a cut; the wrist keeps the sword's
        // separate angle. The hand remains within the fixed two-bone reach.
        float drift = MathF.Sin(p.Age * .051f + side * 2.2f) * (1 - blade.Trail) * .035f;
        float armAngle = blade.Angle + side * .30f + blade.ArmLead + drift;
        return Shoulder(p, side) + Offset(armAngle.ToRotationVector2() * (86 + blade.Reach), p);
    }
    internal static Vector2 Tip(in SamuraiRigPose p, int side)
    {
        var blade = side < 0 ? p.Left : p.Right;
        return Hand(p, side) + (blade.Angle + p.Lean).ToRotationVector2() * (145 * blade.Size * p.Scale);
    }
    private static void Arms(SpriteBatch batch, in SamuraiRigPose p, Vector2 screen, Color tint)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 shoulder = Shoulder(p, side), hand = Hand(p, side);
            var joint = SamuraiRigMotion.SolveElbow(shoulder.X, shoulder.Y, hand.X, hand.Y, side, p.Scale);
            Vector2 elbow = new(joint.X, joint.Y);
            Segment(batch, 2, shoulder - screen, elbow - screen, 37 * p.Scale, tint, side < 0);
            Segment(batch, 3, elbow - screen, hand - screen, 25 * p.Scale, tint, side < 0);
        }
    }
    private static void Segment(SpriteBatch batch, int part, Vector2 from, Vector2 to, float width, Color tint, bool flip)
        => Part(batch, part, from, new(.5f, .10f), new(width, Vector2.Distance(from, to) / .80f),
            (to - from).ToRotation() - MathHelper.PiOver2, tint, flip);
    private static void Swords(SpriteBatch batch, in SamuraiRigPose p, Vector2 screen, Color tint, bool glow)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            var blade = side < 0 ? p.Left : p.Right;
            Vector2 position = Hand(p, side) - screen;
            float rotation = blade.Angle + p.Lean + MathHelper.PiOver2;
            Vector2 size = new Vector2(38, 172) * blade.Size * p.Scale;
            if (glow && blade.Charge > .1f)
            {
                Part(batch, 4, position, new(.40f, .85f), size * 1.06f, rotation,
                    new Color(147, 91, 220) * blade.Charge * .35f);
            }
            Part(batch, 4, position, new(.40f, .85f), size, rotation, tint);
        }
    }

    private static void Trails(SpriteBatch batch, in SamuraiRigPose p, Vector2 screen, SamuraiRigHistory history, double tick, bool reduced)
    {
        for (int side = -1; side <= 1; side += 2)
        for (int i = 0; i < history.Count; i++)
        {
            var sample = history.At(i);
            float age = (float)(tick - sample.Tick);
            if (age < 0) continue;
            var blade = side < 0 ? sample.Pose.Left : sample.Pose.Right;
            if (blade.Trail <= 0) continue;
            var next = i + 1 < history.Count && history.At(i + 1).Tick <= tick ? history.At(i + 1).Pose : p;
            Vector2 from = Tip(sample.Pose, side) - screen, to = Tip(next, side) - screen;
            if (Vector2.DistanceSquared(from, to) > 360 * 360) continue;
            float fade = SamuraiRigMotion.Fade(age) * blade.Trail;
            // The full ribbon is a Luminance primitive. Reduced effects retain
            // a thin readable trace without duplicating its broad glow.
            if (reduced) GhostSamuraiVisuals.Stroke(batch, from, to, 5, new Color(125, 107, 240) * fade * .65f);
            GhostSamuraiVisuals.Stroke(batch, from, to, 2.5f, new Color(239, 220, 255) * fade * .85f);
            if (!reduced && i % 3 == 0)
                Part(batch, 4, Hand(sample.Pose, side) - screen, new(.40f, .85f), new Vector2(38, 172) * blade.Size * sample.Pose.Scale,
                    blade.Angle + sample.Pose.Lean + MathHelper.PiOver2, new Color(155, 103, 229) * fade * .22f);
        }
    }

    private static void Wisps(SpriteBatch batch, in SamuraiRigPose p, Vector2 screen, float opacity)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            var blade = side < 0 ? p.Left : p.Right;
            Vector2 root = Root(p, screen);
            Vector2 free = root + new Vector2(side * (110 + p.Hit * 24) - p.Lag * 40,
                -14 + MathF.Sin(p.Age * .047f + side * 1.7f) * 17);
            Vector2 nearBlade = Vector2.Lerp(Hand(p, side), Tip(p, side), .65f) - screen;
            Vector2 position = Vector2.Lerp(free, nearBlade, blade.Charge * .62f);
            Part(batch, 8, position, new(.5f, .78f), new(30, 59), -p.Lag, Color.White * opacity * .82f);
            if (!Reduced)
                for (int i = 1; i <= 3; i++)
                    Part(batch, 8, position - new Vector2(p.Lag * i * 40, -i * 5), new(.5f, .78f),
                        new Vector2(25, 50) * (1 - i * .12f), -p.Lag, new Color(154, 122, 247) * opacity * (.18f / i));
        }
    }
    private static void Smoke(SpriteBatch batch, in SamuraiRigPose p, Vector2 screen, bool reduced, bool front, float opacity)
    {
        int count = reduced ? 3 : 7;
        float charge = Math.Max(p.Left.Charge, p.Right.Charge);
        for (int i = 0; i < count; i++)
        {
            float flow = (p.Age * .012f + i * .173f) % 1;
            float angle = i * 2.39f + p.Age * .008f;
            float radius = (front ? 55 : 100) + p.Hit * 24;
            Vector2 position = Root(p, screen) + new Vector2(MathF.Cos(angle) * radius - p.Lag * 95 * flow,
                136 - flow * 150 - charge * flow * 95);
            float fade = MathF.Sin(flow * MathHelper.Pi) * (front ? .11f : .22f) * opacity;
            Part(batch, 8, position, new(.5f, .6f), new(27 + flow * 25, 58 + flow * 30), p.Lag + MathF.Sin(angle) * .2f,
                new Color(158, 114, 235) * fade);
        }
    }

    // swords: false when the blades are drawn falling by DrawFallenBlades. departing: the
    // samurai leaves a lost fight whole, without the crack or the scattering ornaments.
    internal static void DrawDeath(SpriteBatch batch, SamuraiRigPose p, Vector2 screen, float age, bool swords = true, bool departing = false)
    {
        if (Main.dedServ) return;
        float lower = SamuraiRigMotion.Smooth(age / 24), scatter = departing ? 0 : SamuraiRigMotion.Smooth((age - 26) / 38);
        p = p with { Left = p.Left with { Angle = SamuraiRigMotion.Angle(p.Left.Angle, 2.16f, lower), Charge = 0 },
            Right = p.Right with { Angle = SamuraiRigMotion.Angle(p.Right.Angle, .98f, lower), Charge = 0 }, Hit = 0, Speed = 0 };
        float armor = 1 - SamuraiRigMotion.Smooth((age - 34) / 35);
        float head = 1 - SamuraiRigMotion.Smooth((age - 67) / 20);
        float end = 1 - SamuraiRigMotion.Smooth((age - 82) / 14);
        var smokePose = p with { Age = p.Age + age, Hit = scatter };
        Smoke(batch, smokePose, screen, Reduced, false, end);
        GhostSamuraiEnergy.Body(batch, smokePose, screen, false, end * (1 + scatter));
        using (var scope = new WorldGraphicsScope(batch))
        {
            GhostSamuraiMaterials.Prepare(p.Age + age, 0, 0, SamuraiRigMotion.Smooth((age - 34) / 54));
            surfaceParts = endingParts = true;
            try
            {
                Ornaments(batch, smokePose, screen, Color.White * (1 - SamuraiRigMotion.Smooth((age - 48) / 22)), scatter);
                Hem(batch, smokePose, screen, Color.White * armor, 1 - SamuraiRigMotion.Smooth((age - 32) / 29));
                Part(batch, 0, Root(p, screen), new(.5f, .40f), new Vector2(130, 132) * p.Scale, p.Lean, Color.White * armor);
                Arms(batch, p, screen, Color.White * armor);
                if (swords) Swords(batch, p, screen, Color.White * armor, false);
                Head(batch, p, screen, Color.White * head);
            }
            finally { surfaceParts = endingParts = false; }
        }
        float crack = departing ? 0 : SamuraiRigMotion.Smooth((age - 18) / 12) * (1 - SamuraiRigMotion.Smooth((age - 60) / 20));
        Vector2 root = Root(p, screen);
        GhostSamuraiVisuals.Stroke(batch, root + new Vector2(-11, -60), root + new Vector2(7, -26), 3, new Color(223, 185, 255) * crack);
        GhostSamuraiVisuals.Stroke(batch, root + new Vector2(7, -26), root + new Vector2(-6, 3), 2, new Color(237, 216, 255) * crack);
        if (age >= 52)
            Part(batch, 8, root + new Vector2(0, -66 - (age - 52) * .35f), new(.5f, .78f), new(45, 84), 0,
                Color.White * SamuraiRigMotion.Smooth((age - 52) / 20) * end);
        Wisps(batch, smokePose, screen, end * (1 - scatter * .8f));
        GhostSamuraiEnergy.Body(batch, smokePose, screen, true, end);
    }

    // Victory: the blades leave the dissolving hands and plant in the floor of the seal,
    // joining the weapons of the old battle; they melt with the seal.
    internal static void DrawFallenBlades(SpriteBatch batch, SamuraiRigPose p, Vector2 screen, float age,
        float floorY, float left, float right)
    {
        if (Main.dedServ) return;
        float lower = SamuraiRigMotion.Smooth(Math.Min(age, SamuraiCinematics.BladeRelease) / 24f);
        p = p with { Left = p.Left with { Angle = SamuraiRigMotion.Angle(p.Left.Angle, 2.16f, lower), Charge = 0 },
            Right = p.Right with { Angle = SamuraiRigMotion.Angle(p.Right.Angle, .98f, lower), Charge = 0 }, Hit = 0, Speed = 0 };
        float melt = SamuraiRigMotion.Smooth((age - SamuraiCinematics.BladeFade) / 40);
        if (melt >= 1) return;
        Span<Vector4> blades = stackalloc Vector4[2];
        for (int side = -1; side <= 1; side += 2)
        {
            var blade = side < 0 ? p.Left : p.Right;
            Vector2 hand = Hand(p, side);
            float length = 145 * blade.Size * p.Scale;
            var (x, y, angle) = SamuraiCinematics.FallenBlade(age, hand.X, hand.Y, blade.Angle + p.Lean, length,
                side, p.X, floorY, left, right);
            blades[(side + 1) / 2] = new(x, y, angle, blade.Size * p.Scale);
        }
        using (var scope = new WorldGraphicsScope(batch))
        {
            GhostSamuraiMaterials.Prepare(p.Age + age, 0, 0, melt);
            surfaceParts = endingParts = true;
            try
            {
                foreach (var b in blades)
                    Part(batch, 4, new Vector2(b.X, b.Y) - screen, new(.40f, .85f), new Vector2(38, 172) * b.W, b.Z + MathHelper.PiOver2, Color.White);
            }
            finally { surfaceParts = endingParts = false; }
        }
        // the moment of planting flashes down each blade
        float flash = age >= SamuraiCinematics.BladeLand ? MathF.Exp(-(age - SamuraiCinematics.BladeLand) / 6) * (1 - melt) : 0;
        if (flash > .02f)
            foreach (var b in blades)
            {
                Vector2 hilt = new(b.X, b.Y), tip = hilt + b.Z.ToRotationVector2() * 145 * b.W;
                GhostSamuraiVisuals.Stroke(batch, hilt - screen, tip - screen, 3, new Color(239, 228, 255) * flash);
            }
    }

    private static void Part(SpriteBatch batch, int index, Vector2 position, Vector2 pivot, Vector2 size,
        float rotation, Color tint, bool flip = false)
    {
        Rectangle source = parts[index];
        DrawPart(batch, index, position, source, tint, rotation, new Vector2(source.Width, source.Height) * pivot,
            size / new Vector2(source.Width, source.Height), flip);
    }
    private static void DrawPart(SpriteBatch batch, int part, Vector2 position, Rectangle source, Color tint, float rotation, Vector2 origin, Vector2 scale, bool flip)
    {
        if (surfaceParts) GhostSamuraiMaterials.Part(Texture, part, position, source, tint, rotation, origin, scale, flip);
        else batch.Draw(Texture, position, source, tint, rotation, origin, scale, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
    }
}
