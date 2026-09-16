#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Physical silhouettes lead; Luminance-managed plasma is localized to impacts.
[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonGestureVisuals : ModSystem
{
    private Guid fight;
    private int previous = -1;
    private float shake;
    private readonly HashSet<(int Phrase, byte Pulse, bool Fire)> heard = new();
    private readonly List<(SlotId Id, int Until)> voices = new();
    private static Texture2D? disk;
    internal static Vector2 V(CrimsonPoint p) => new(p.X, p.Y);
    internal static Color Palette(int source) => source switch
    {
        0 => new(255, 171, 94), 1 => new(178, 169, 215),
        2 => new(221, 90, 159), _ => new(255, 92, 113)
    };
    public override void PostUpdateEverything()
    {
        var boss = CrimsonPackets.Boss;
        if (boss is null || !boss.Fresh || !CrimsonVisuals.Local(boss)) { Reset(); return; }
        int age = (int)boss.VisualAge;
        if (fight != boss.State.Fight) { Reset(); fight = boss.State.Fight; previous = age - 1; }
        shake *= .78f;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not CrimsonGesture gesture || !gesture.TryBoss(out var owner) || owner != boss) continue;
            var p = gesture.Plan;
            Cue(p.Born, false); Cue(p.Fire, true);
            void Cue(int tick, bool impact)
            {
                if (previous >= tick || age < tick || age - tick > 3 || !heard.Add((p.Phrase, p.Pulse, impact))) return;
                string asset = impact ? p.Source switch
                {
                    0 => "PylonBreak", 1 => "SwordImpale", 2 => "BladeUnsheathe", _ => "Beams/SpreadScatter"
                } : p.Source switch { 0 => "EnergyLock", 1 => "BladeUnsheathe", 2 => "LanceCharge", _ => "EnergyGather" };
                float gain = (impact ? .24f : .13f) + p.Accent * .025f;
                if (voices.Count < 24)
                {
                    var id = SoundEngine.PlaySound(new SoundStyle("Convergence/Assets/Sounds/FirstSeverance/" + asset)
                    {
                        Volume = gain, MaxInstances = 3, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
                        PlayOnlyIfFocused = true, PauseBehavior = PauseBehavior.StopWhenGamePaused
                    });
                    voices.Add((id, tick + (impact ? 14 : 9)));
                }
                if (impact) shake = Math.Max(shake, 2 + p.Accent);
            }
        }
        if (heard.Count > 96)
        {
            int latest = 0;
            foreach (var h in heard) latest = Math.Max(latest, h.Phrase);
            heard.RemoveWhere(x => x.Phrase < latest - 2);
        }
        for (int i = voices.Count - 1; i >= 0; i--)
        {
            var v = voices[i];
            if (!SoundEngine.TryGetActiveSound(v.Id, out var sound)) { voices.RemoveAt(i); continue; }
            if (age >= v.Until) { sound.Stop(); voices.RemoveAt(i); }
            else sound.Volume = Math.Min(sound.Volume, Math.Clamp((v.Until - age) / 5f, 0, 1));
        }
        previous = age;
    }
    public override void ModifyScreenPosition()
    {
        if (CrimsonVisuals.Reduced || !ModContent.GetInstance<CrimsonVisualConfig>().ScreenShake || shake < .05f) return;
        float t = Main.GameUpdateCount % 6000;
        Main.screenPosition += new Vector2(MathF.Sin(t * 2.3f), MathF.Cos(t * 1.9f)) * Math.Min(shake, 5);
    }
    private void Reset()
    {
        foreach (var v in voices) if (SoundEngine.TryGetActiveSound(v.Id, out var sound)) sound.Stop();
        voices.Clear(); heard.Clear(); fight = Guid.Empty; previous = -1; shake = 0;
    }
    public override void OnWorldUnload() => Reset();
    public override void ClearWorld() => Reset();
    public override void Unload()
    {
        Reset(); var old = disk; disk = null;
        if (old is not null) Main.QueueMainThreadAction(old.Dispose);
    }
    internal static Texture2D Disk()
    {
        if (disk is not null) return disk;
        const int size = 64; var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float radius = new Vector2(x - 31.5f, y - 31.5f).Length();
            pixels[y * size + x] = Color.White * Math.Clamp(31.5f - radius, 0, 1);
        }
        // Called only by drawing; never allocate GPU resources on a loader worker.
        disk = new Texture2D(Main.instance.GraphicsDevice, size, size); disk.SetData(pixels); return disk;
    }
    public override void PostDrawTiles()
    {
        var boss = CrimsonPackets.Boss;
        if (Main.dedServ || Main.gameMenu || boss is null || !boss.Fresh) return;
        float age = CrimsonVisuals.RenderAge(boss);
        var batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        CrimsonEnergy.Begin();
        try
        {
            Span<CrimsonStroke> strokes = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.ModProjectile is not CrimsonGesture gesture || !gesture.TryBoss(out var owner) || owner != boss) continue;
                var p = gesture.Plan;
                if (age < p.Born || age >= p.End + 10) continue;
                bool warning = age < p.Fire;
                float alpha = warning ? .14f + .40f * MathF.Exp(-(age - p.Born) / 4)
                    : age < p.End ? 1 : 1 - CrimsonInvocation.Ease((age - p.End) / 10);
                float sample = warning ? p.Fire : Math.Min(age, p.End - .001f);
                if (age >= p.End && p.Technique is CrimsonTechnique.ChoirHook or CrimsonTechnique.ChoirThrust or CrimsonTechnique.MantleScissors)
                    sample = p.Fire + (p.End - p.Fire - 1) * (1 - CrimsonInvocation.Ease((age - p.End) / 10));
                int count = CrimsonTechniqueGeometry.Write(p, sample, strokes, warning);
                Color color = Palette(p.Source);
                for (int i = 0; i < count; i++)
                {
                    var s = strokes[i];
                    if (warning)
                    {
                        Capsule(batch, s, new Color(12, 8, 18) * Math.Min(.68f, alpha + .14f));
                        Capsule(batch, s with { Radius = Math.Max(1.2f, s.Radius - 2) }, color * (alpha * .48f));
                    }
                    else
                    {
                        Capsule(batch, s, new Color(24, 9, 22) * alpha);
                        Capsule(batch, s with { Radius = s.Radius * .78f }, Color.Lerp(color, Color.White, .28f) * alpha);
                        if (p.Technique is CrimsonTechnique.ChoirRend or CrimsonTechnique.MantleScissors)
                            Capsule(batch, s with { Radius = 2 }, new Color(255, 223, 233, 0) * alpha);
                        if (p.Source == 2 && i % 4 == 0 && p.Technique != CrimsonTechnique.ChoirRend)
                        {
                            var v = s.B - s.A;
                            var n = new CrimsonPoint(-v.Y, v.X) * (15 / MathF.Sqrt(Math.Max(1, v.LengthSquared)));
                            Capsule(batch, new(s.B, s.B + n, 3), new Color(231, 209, 200) * alpha);
                        }
                    }
                }
                if (p.Technique == CrimsonTechnique.CrownCinders && warning) DrawCinders(batch, p, age, color);
                if (p.Technique == CrimsonTechnique.CrownRain && !warning) DrawShardHeads(batch, strokes[..count], color, alpha);
                if (!warning && !CrimsonVisuals.Reduced)
                {
                    float impulse = MathF.Exp(-(age - p.Fire) / 5);
                    var at = p.MovesBody ? p.Body(age) : p.Target;
                    CrimsonEnergy.AddCore(V(at), 26 + impulse * (p.Source == 0 ? 100 : 55), age, 0, impulse, alpha * .50f, false);
                    var glow = MiscTexturesRegistry.BloomCircleSmall.Value;
                    for (int i = 0; i < 9; i++)
                    {
                        var v = CrimsonPoint.Polar(i * MathF.Tau / 9 + p.Pulse, (age - p.Fire) * (3 + i % 3));
                        batch.Draw(glow, V(at + v) - Main.screenPosition, null, color * (alpha * impulse * .32f),
                            i, glow.Size() * .5f, (5 + p.Accent * 2) / (float)glow.Width, SpriteEffects.None, 0);
                    }
                }
            }
            // Only localized AddCore commands are queued, never beam rays.
            CrimsonEnergy.Draw(batch);
        }
        finally { batch.End(); }
    }
    private static void DrawCinders(SpriteBatch batch, CrimsonGesturePlan p, float age, Color color)
    {
        float t = Math.Clamp((age - p.Born) / (p.Fire - p.Born), 0, 1);
        for (int i = -1; i <= 1; i++)
        {
            var end = CrimsonTechniqueGeometry.Clamp(p.Field, p.Target + new CrimsonPoint(i * 260, (p.Pulse % 2 == 0 ? -1 : 1) * 90), 125);
            var c = CrimsonPoint.Lerp(p.Stage, end, .5f) + new CrimsonPoint(0, -180);
            var at = CrimsonTechniqueGeometry.Bezier(p.Stage, c, end, t);
            Capsule(batch, new(at, at, 18 + t * 9), color * .75f);
        }
    }
    private static void DrawShardHeads(SpriteBatch batch, ReadOnlySpan<CrimsonStroke> strokes, Color color, float alpha)
    {
        foreach (var s in strokes)
        {
            var tip = V(s.B);
            for (int i = 0; i < 5; i++)
            {
                float width = (5 - i) * 3;
                batch.Draw(TextureAssets.MagicPixel.Value, tip - Main.screenPosition - new Vector2(0, (5 - i) * 11),
                    new Rectangle(0, 0, 1, 1), Color.Lerp(color, Color.White, .6f) * alpha, 0, new Vector2(.5f, 0),
                    new Vector2(width, 12), SpriteEffects.None, 0);
            }
        }
    }
    internal static void Capsule(SpriteBatch batch, in CrimsonStroke s, Color color)
    {
        var a = V(s.A) - Main.screenPosition; var b = V(s.B) - Main.screenPosition;
        var delta = b - a; var tex = Disk();
        if (delta.LengthSquared() > .01f)
            batch.Draw(TextureAssets.MagicPixel.Value, a, new Rectangle(0, 0, 1, 1), color, delta.ToRotation(),
                new Vector2(0, .5f), new Vector2(delta.Length(), s.Radius * 2), SpriteEffects.None, 0);
        batch.Draw(tex, a, null, color, 0, tex.Size() * .5f, s.Radius * 2 / tex.Width, SpriteEffects.None, 0);
        batch.Draw(tex, b, null, color, 0, tex.Size() * .5f, s.Radius * 2 / tex.Width, SpriteEffects.None, 0);
    }
}
