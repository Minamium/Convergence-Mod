// Offline frames of the Canticle Organ (docs/encounters/crimson-foundry/REWARDS.md, "Ranged - Canticle Organ"): the
// owner's mark arcs (2, 5 and 8 marks, the full one warmed, the newest head flaring), a shard's wake, then the Hymn of
// Hands planned by the real CanticleMarks ledger (round-robin in first-mark order, fifteen hands, the eighth hand on the
// fully marked target the Clasp with its crown), each hand's forming and falling smears, live slam, residue and
// droplets. The ink is CanticleRules' own commands (the ones the game emits) drawn by the production ScarletRewardInk.
// Targets are translucent hitbox stand-ins; hands are the delivered BoneHand.png when it exists, else a bone stand-in.
//
//   pwsh tools/preview-scarlet.ps1 -Rewards -Only organ
#nullable enable
using System;
using System.IO;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NVector2 = System.Numerics.Vector2;

internal sealed class OrganRewardsScene : IRewardsPreviewScene
{
    private const int Cast = 4;
    public string Name => "organ";
    public int[] Ticks { get; } = { 0, 6, 10, 13, 15, 16, 17, 22, 23, 28, 45, 70, 104, 108, 111, 113, 114, 115, 122, 140 };

    private readonly record struct Target(Vector2 Offset, int Width, int Height, int Marks);
    private static readonly Target[] Targets =
    {
        new(new Vector2(-300, 40), 70, 96, 8),
        new(new Vector2(-20, 60), 52, 60, 5),
        new(new Vector2(240, 72), 36, 40, 2),
    };

    private readonly CanticleHand[] hands = new CanticleHand[CrimsonRewardRules.MaxHands];
    private readonly int handCount;
    private readonly Sink sink = new();

    public OrganRewardsScene()
    {
        // The real ledger and schedule: first marks in the order A, B, C, then the rest; C's newest mark lands last.
        var marks = new CanticleMarks();
        for (int m = 0; m < CrimsonRewardRules.MaxMarksPerNpc; m++)
            for (int t = 0; t < Targets.Length; t++)
                if (m < Targets[t].Marks) marks.Mark(t, 1, 0, (ulong)(m * 3 + t));
        handCount = marks.Plan(new[] { true, true, true }, hands);
    }

    private static Vector2 Center(Vector2 c, int t) => c + Targets[t].Offset;
    private static NVector2 N(Vector2 v) => new(v.X, v.Y);

    public void Emit(ScarletInkCanvas canvas, Vector2 c, int tick)
    {
        sink.Canvas = canvas;
        if (tick < Cast)
        {
            // The owner's marks; C's newest note-head lands two ticks before frame 0.
            for (int t = 0; t < Targets.Length; t++)
            {
                Vector2 anchor = Center(c, t) - new Vector2(0, Targets[t].Height * .5f + CanticleRules.MarkLift);
                CanticleRules.MarkInk(sink, N(anchor), Targets[t].Marks, t == 2 ? tick + 2 : 40 + tick, t * 1.37f);
            }
            // A shard on its way to C: its last positions, 20 px per update.
            Vector2 head = Center(c, 2) + new Vector2(-520 + 40 * tick, -6);
            Span<NVector2> trail = stackalloc NVector2[4];
            for (int i = 0; i < 4; i++) trail[i] = N(head - new Vector2(20 * (3 - i), 0));
            sink.Window = 0;
            CanticleRules.ShardInk(sink, trail, 3.3f);
        }
        for (int i = 0; i < handCount; i++)
        {
            CanticleHand hand = hands[i];
            float age = tick - Cast - CrimsonRewardRules.HandSlamTick(hand.Ordinal);
            Vector2 landing = Center(c, hand.Root) + new Vector2(CanticleRules.LandingOffset(hand.Ordinal, hand.Role, Targets[hand.Root].Width), 0);
            sink.Window = CrimsonRewardRules.HandLive; // as OrganVisuals: the slam closes into its scar
            CanticleRules.HandInk(sink, N(landing), hand.Ordinal, hand.Role, Targets[hand.Root].Width, age, i * .53f + .1f, canvas.Reduced);
        }
        sink.Canvas = null;
    }

    public void Particles(ScarletRewardParticles particles, Vector2 c, int tick, bool reduced)
    {
        for (int i = 0; i < handCount; i++)
        {
            CanticleHand hand = hands[i];
            int age = tick - Cast - CrimsonRewardRules.HandSlamTick(hand.Ordinal);
            Vector2 landing = Center(c, hand.Root) + new Vector2(CanticleRules.LandingOffset(hand.Ordinal, hand.Role, Targets[hand.Root].Width), 0);
            bool clasp = hand.Role == CanticleRules.HandRole.Clasp;
            if (age == 0)
            {
                for (int k = 0; k < (clasp ? 10 : 5); k++)
                {
                    float h = CanticleRules.Hash(i, k + 41), angle = -MathF.PI / 2 + (h - .5f) * 2.6f;
                    particles.Spawn(ScarletParticleKind.BoneChip, 0, true, landing, new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (2.2f + 2.4f * CanticleRules.Hash(i, k + 43)), 26, 2, reduced, i + k);
                }
                for (int k = 0; k < 3; k++)
                    particles.Spawn(ScarletParticleKind.Ember, 0, true, landing + new Vector2((k - 1) * 14, -6), new Vector2((k - 1) * .5f, -1.2f), 16, 5, reduced, i * 5 + k);
            }
        }
        if (tick < Cast && tick % 2 == 0)
        {
            Vector2 mouth = Center(c, 2) + new Vector2(-560, -6);
            particles.Spawn(ScarletParticleKind.Smoke, 0, true, mouth, new Vector2(1.1f, -.25f), 22, 9, reduced, tick);
        }
    }

    public void Sprites(PreviewRenderer renderer, ScarletView view, Vector2 c, int tick)
    {
        var batch = renderer.Batch;
        Matrix world = Matrix.CreateTranslation(-view.ScreenPosition.X, -view.ScreenPosition.Y, 0) * view.GameView;
        Texture2D? art = Final(renderer, "Items/ScarletRewards/BoneHand");
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, world);
        // NPC hitboxes (NPCs draw over the ink in game).
        for (int t = 0; t < Targets.Length; t++)
        {
            Vector2 at = Center(c, t);
            var box = new Rectangle((int)(at.X - Targets[t].Width / 2f), (int)(at.Y - Targets[t].Height / 2f), Targets[t].Width, Targets[t].Height);
            batch.Draw(renderer.Pixel, box, new Color(60, 54, 70) * .55f);
            Outline(batch, renderer.Pixel, box, new Color(200, 196, 220) * .8f);
        }
        for (int i = 0; i < handCount; i++)
        {
            CanticleHand hand = hands[i];
            float age = tick - Cast - CrimsonRewardRules.HandSlamTick(hand.Ordinal);
            if (age < -CrimsonRewardRules.HandLead || age >= CrimsonRewardRules.HandLive) continue;
            bool clasp = hand.Role == CanticleRules.HandRole.Clasp;
            Vector2 landing = Center(c, hand.Root) + new Vector2(CanticleRules.LandingOffset(hand.Ordinal, hand.Role, Targets[hand.Root].Width), 0);
            float form = CanticleRules.Forming(age);
            for (int b = 0; b < (clasp ? CrimsonRewardRules.ClaspHands : 1); b++)
            {
                float x = clasp ? CanticleRules.ClaspHandOffset(b, Math.Min(age, 0), Targets[hand.Root].Width) : 0;
                Vector2 palm = landing + new Vector2(x, -CanticleRules.FallHeight(age));
                bool mirror = clasp ? CrimsonRewardRules.ArmSide(b) < 0 : CrimsonRewardRules.ArmSide(hand.Ordinal) < 0;
                if (art is not null)
                    batch.Draw(art, palm, null, Color.White * form, 0, new Vector2(art.Width * .5f, art.Height * .6f), 2 * (.72f + .28f * form),
                        mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
                else StandIn(batch, renderer.Pixel, palm, form);
            }
        }
        batch.End();
    }

    private static Texture2D? Final(PreviewRenderer renderer, string name)
    {
        string path = Path.Combine(renderer.Root, "Assets/Textures", name + ".png");
        return File.Exists(path) ? renderer.Assets.GetTexture(name) : null;
    }

    // A palm-down bone hand at the 2 px dot: crimson cuff, palm, four fingers pointing down.
    private static void StandIn(SpriteBatch batch, Texture2D pixel, Vector2 palm, float form)
    {
        float s = .72f + .28f * form;
        Color bone = new Color(232, 222, 196) * form, shade = new Color(150, 136, 118) * form, cuff = new Color(150, 16, 26) * form, ink = new Color(30, 6, 10) * form;
        void Rect(float x, float y, float w, float h, Color color)
            => batch.Draw(pixel, new Rectangle((int)(palm.X + x * s), (int)(palm.Y + y * s), Math.Max(1, (int)(w * s)), Math.Max(1, (int)(h * s))), color);
        Rect(-15, -22, 30, 32, ink);
        Rect(-12, -20, 24, 8, cuff);
        Rect(-13, -12, 26, 16, bone);
        Rect(-13, 2, 26, 2, shade);
        for (int f = 0; f < 4; f++)
        {
            Rect(-13 + f * 7, 4, 5, 20, ink);
            Rect(-12 + f * 7, 4, 3, 18, bone);
        }
    }

    private static void Outline(SpriteBatch batch, Texture2D pixel, Rectangle box, Color color)
    {
        batch.Draw(pixel, new Rectangle(box.X, box.Y, box.Width, 2), color);
        batch.Draw(pixel, new Rectangle(box.X, box.Bottom - 2, box.Width, 2), color);
        batch.Draw(pixel, new Rectangle(box.X, box.Y, 2, box.Height), color);
        batch.Draw(pixel, new Rectangle(box.Right - 2, box.Y, 2, box.Height), color);
    }

    // CanticleRules' ink commands onto the production canvas, as the game's OrganInkSink does (local owner 0).
    private sealed class Sink : ICanticleInk
    {
        internal ScarletInkCanvas? Canvas;
        internal float Window;
        private static ScarletInkLook Look(CanticleInkLook look) => look switch
        {
            CanticleInkLook.Live => ScarletInkLook.Live,
            CanticleInkLook.Dormant => ScarletInkLook.Dormant,
            _ => ScarletInkLook.Residue,
        };
        public bool Begin(CanticleInkLook look, float seed, float opacity = 1, float warmth = 0)
            => Canvas is not null && Canvas.Begin(new ScarletInkStyle(Look(look), true, seed, opacity, false, warmth, 0,
                look == CanticleInkLook.Live ? Window : 0));
        public void Point(NVector2 at, float radius, float time) => Canvas?.Point(new Vector2(at.X, at.Y), radius, time);
        public void End(bool bead = false) => Canvas?.End(bead);
        public void Droplet(NVector2 from, NVector2 to, float radius, float time, float seed)
            => Canvas?.Droplet(new ScarletInkStyle(ScarletInkLook.Live, true, seed, 1, false, 0, 0), new Vector2(from.X, from.Y), new Vector2(to.X, to.Y), radius, time);
    }
}
