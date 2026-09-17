#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Own Scarlet scene: ash apse -> velvet vault -> thorn void -> eclipse.
internal sealed class CrimsonSky : CustomSky
{
    internal const string Key = "Convergence:ScarletInvocation";
    private Guid fight;
    private bool requested;
    private float fade, age;
    private int phase;
    private readonly float[] weights = new float[4];
    internal void Bind(CrimsonBoss? boss)
    {
        bool valid = boss is not null && boss.Fresh && CrimsonVisuals.Local(boss)
            && boss.State.Contains(Main.myPlayer) && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost;
        if (!valid) { requested = false; return; }
        if (fight != boss!.State.Fight) { Reset(); fight = boss.State.Fight; }
        requested = true; phase = boss.State.Phase; age = boss.VisualAge;
    }
    public override void Activate(Vector2 position, params object[] args) => requested = true;
    public override void Deactivate(params object[] args) => requested = false;
    public override bool IsActive() => requested || fade > 0;
    public override void Reset() { requested = false; fade = age = 0; fight = Guid.Empty; Array.Clear(weights); }
    public override float GetCloudAlpha() => 1 - fade * .95f;
    public override void Update(GameTime gameTime)
    {
        if (Main.gameMenu || Main.dedServ) { Reset(); return; }
        fade = Math.Clamp(fade + (requested ? .022f : -.045f), 0, 1);
        for (int i = 0; i < 4; i++) weights[i] = MathHelper.Lerp(weights[i], i == phase ? 1 : 0, .035f);
    }
    public override void Draw(SpriteBatch batch, float minDepth, float maxDepth)
    {
        if (Main.dedServ || fade <= 0 || minDepth >= 0 || maxDepth < 0) return;
        float w = Main.screenWidth, h = Main.screenHeight, time = age / 60;
        bool reduced = CrimsonVisuals.Reduced;
        float sway = reduced ? 0 : MathF.Sin(time * .11f) * 12;
        batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, (int)w, (int)h), new Rectangle(0, 0, 1, 1), new Color(12, 5, 17) * fade);
        var noise = MiscTexturesRegistry.WavyBlotchNoise.Value;
        var fog = MiscTexturesRegistry.TurbulentNoise.Value;
        var bloom = MiscTexturesRegistry.BloomCircleSmall.Value;
        for (int layer = 0; layer < (reduced ? 2 : 5); layer++)
        {
            float depth = layer / 4f;
            var tex = layer % 2 == 0 ? noise : fog;
            Vector2 center = new(w * (.5f + MathF.Sin(time * .023f + layer) * .10f), h * (.2f + depth * .18f));
            batch.Draw(tex, center, null, new Color(87, 30 + phase * 7, 57 + phase * 11) * (fade * .075f),
                MathF.Sin(time * .013f + layer) * .1f, tex.Size() * .5f,
                new Vector2(w * 1.8f / tex.Width, h * .5f / tex.Height), SpriteEffects.None, 0);
        }
        float ember = weights[0] + weights[3] * .5f;
        float cloth = weights[1] + weights[3] * .4f;
        float thorn = weights[2] + weights[3] * .65f;
        // Low-contrast distant architecture, not copies of damage telegraphs.
        for (int arch = 0; arch < 7; arch++)
        {
            float x = (arch + .5f) * w / 7 + sway * .3f;
            float top = h * (.22f + (arch % 3) * .065f);
            Color stone = new Color(75, 35, 40) * (fade * (.24f + ember * .24f));
            Line(new(x - 32, h), new(x - 32, top + 95), 15, stone);
            Line(new(x + 32, h), new(x + 32, top + 95), 15, stone);
            Line(new(x - 32, top + 95), new(x, top), 13, stone);
            Line(new(x, top), new(x + 32, top + 95), 13, stone);
        }
        for (int fold = 0; fold < 10; fold++)
        {
            float x = (fold + .2f) * w / 9;
            Vector2 last = new(x, -25);
            for (int i = 1; i <= 14; i++)
            {
                float t = i / 14f;
                Vector2 p = new(x + MathF.Sin(t * 4 + time * .20f + fold) * t * 90, t * h * (.55f + fold % 3 * .1f));
                Line(last, p, 48 - t * 20, new Color(41, 21, 48) * (fade * cloth * .5f)); last = p;
            }
        }
        for (int root = 0; root < 9; root++)
        {
            float x = root * w / 8;
            Vector2 previous = new(x, h);
            for (int i = 1; i <= 17; i++)
            {
                float t = i / 17f;
                Vector2 p = new(x + MathF.Sin(t * 6 + root + time * .1f) * t * 110, h * (1 - t * .74f));
                var ink = new Color(85, 34, 72) * (fade * thorn * .3f);
                Line(previous, p, 11 * (1 - t) + 2, ink);
                if (i % 4 == 0) Line(p, p + new Vector2((root % 2 == 0 ? -1 : 1) * 35, -28), 3, ink);
                previous = p;
            }
        }
        if (!reduced)
            for (int mote = 0; mote < 38; mote++)
            {
                float u = (mote * .618034f + time * .013f) % 1;
                float v = ((mote * .173f - time * .021f) % 1 + 1) % 1;
                batch.Draw(bloom, new Vector2(w * u, h * v), null,
                    new Color(194, 63, 69, 0) * (fade * (.07f + ember * .10f)),
                    0, bloom.Size() * .5f, (3 + mote % 4) / (float)bloom.Width, SpriteEffects.None, 0);
            }
        if (weights[3] > .005f)
        {
            Vector2 center = new(w * .5f, h * .25f);
            batch.Draw(bloom, center, null, new Color(180, 35, 80, 0) * (fade * weights[3] * .23f),
                0, bloom.Size() * .5f, new Vector2(w * .55f, h * .65f) / bloom.Size(), SpriteEffects.None, 0);
            var disk = CrimsonGestureVisuals.Disk();
            batch.Draw(disk, center, null, new Color(7, 3, 13) * (fade * weights[3]), 0, disk.Size() * .5f,
                h * .29f / disk.Width, SpriteEffects.None, 0);
        }
        void Line(Vector2 a, Vector2 b, float width, Color color)
        {
            var d = b - a;
            batch.Draw(TextureAssets.MagicPixel.Value, a, new Rectangle(0, 0, 1, 1), color,
                d.ToRotation(), new Vector2(0, .5f), new Vector2(d.Length(), width), SpriteEffects.None, 0);
        }
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonSkySystem : ModSystem
{
    private CrimsonSky? sky;
    private bool activated;
    public override void Load()
    {
        if (!Main.dedServ) SkyManager.Instance[CrimsonSky.Key] = sky = new CrimsonSky();
    }
    public override void PostUpdateEverything()
    {
        if (sky is null) return;
        var boss = Main.gameMenu ? null : CrimsonPackets.Boss;
        bool want = boss is not null && boss.Fresh && CrimsonVisuals.Local(boss)
            && boss.State.Contains(Main.myPlayer) && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost;
        sky.Bind(boss);
        if (want && !activated) SkyManager.Instance.Activate(CrimsonSky.Key, Vector2.Zero);
        else if (!want && activated) SkyManager.Instance.Deactivate(CrimsonSky.Key);
        activated = want;
    }
    public override void OnWorldUnload() { sky?.Reset(); activated = false; }
    public override void ClearWorld() { sky?.Reset(); activated = false; }
    public override void Unload() { sky?.Reset(); sky = null; activated = false; }
}
