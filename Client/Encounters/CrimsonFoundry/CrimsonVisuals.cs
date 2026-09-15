#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace Convergence.Client.Encounters.CrimsonFoundry;

public sealed class CrimsonVisualConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    [DefaultValue(false)] public bool ReducedEffects;
    [DefaultValue(true)] public bool ScreenShake = true;
}

[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonVisuals : ModSystem
{
    private Guid fight;
    private int previousAge = -1, lastFire = -100, endingAt = -1;
    private float shake;
    private static readonly Rectangle Pixel = new(0, 0, 1, 1);
    internal static bool Reduced => ModContent.GetInstance<CrimsonVisualConfig>().ReducedEffects;
    internal static bool Local(CrimsonBoss b) => Array.Exists(b.State.Members, m => m.Slot == Main.myPlayer);
    private static float Ease(float v) { v = Math.Clamp(v, 0, 1); return v * v * (3 - 2 * v); }

    public override void PostUpdateEverything()
    {
        var boss = CrimsonPackets.Boss;
        if (boss is null || !Local(boss)) { Reset(); return; }
        int age = (int)boss.VisualAge;
        if (fight != boss.State.Fight) { Reset(); fight = boss.State.Fight; previousAge = age - 1; }
        shake *= .80f;
        if (boss.State.PurgeTick >= 0 && previousAge < boss.State.PurgeTick && age >= boss.State.PurgeTick && age - boss.State.PurgeTick < 8)
        { Cue("ShellBreak", .65f); shake = 11; }
        if (boss.State.MusicStart >= 0 && previousAge < boss.State.MusicStart && age >= boss.State.MusicStart && age - boss.State.MusicStart < 8)
        { Cue("RaidDesignation", .36f); shake = 5; }
        if (boss.State.Stage is CrimsonStage.Victory or CrimsonStage.Defeat && endingAt < 0)
        {
            endingAt = age;
            Cue(boss.State.Stage == CrimsonStage.Victory ? "RaidVictory" : "RaidDefeat", .60f); shake = 8;
        }
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is not CrimsonAttack a || a.Hazard.Fight != fight) continue;
            if (previousAge < a.Hazard.Fire && age >= a.Hazard.Fire && age - a.Hazard.Fire < 5 && age - lastFire > 5)
            { lastFire = age; Cue(a.Hazard.Shape == CrimsonShape.Bolt ? "LanceFire" : "FinalSlicerFire", .48f); shake = Math.Max(shake, 3.5f); }
        }
        previousAge = age;
    }
    private static void Cue(string asset, float gain) => SoundEngine.PlaySound(new SoundStyle("Convergence/Assets/Sounds/FirstSeverance/" + asset)
    { Volume = gain, MaxInstances = 2, PlayOnlyIfFocused = true, PauseBehavior = PauseBehavior.StopWhenGamePaused });
    private void Reset() { fight = Guid.Empty; previousAge = -1; lastFire = -100; endingAt = -1; shake = 0; }
    public override void OnWorldUnload() => Reset();
    public override void ClearWorld() => Reset();
    public override void ModifyScreenPosition()
    {
        if (Reduced || !ModContent.GetInstance<CrimsonVisualConfig>().ScreenShake || shake < .05f) return;
        float t = Main.GameUpdateCount % 6000;
        Main.screenPosition += new Vector2(MathF.Sin(t * 2.3f), MathF.Cos(t * 1.9f)) * Math.Min(shake, 11);
    }
    public override void PostDrawTiles()
    {
        var boss = CrimsonPackets.Boss;
        if (Main.gameMenu || boss is null) return;
        var batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        CrimsonEnergy.Begin();
        try
        {
            float age = boss.VisualAge;
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile is not CrimsonAttack attack || attack.Hazard.Fight != boss.State.Fight) continue;
                var h = attack.Hazard;
                if (age >= h.End + 8 || age < h.Born) continue;
                bool live = age >= h.Fire;
                Vector2 origin = new(h.X, h.Y), direction = new(h.DX, h.DY);
                float length = h.Length, width = h.Width, opacity = 1;
                if (live)
                {
                    width = h.HitWidth(age);
                    length = h.Reach(age);
                    if (h.Shape == CrimsonShape.Bolt) origin += direction * Math.Max(0, h.Travel(age) - 110);
                    if (age >= h.End) opacity = 1 - Ease((age - h.End) / 8);
                }
                CrimsonEnergy.Add(origin, direction, length, width, age, h.Fire, h.End, opacity, Reduced);
                if (!Reduced && live)
                {
                    // Source flash is brief, directed and never a HUD reticle.
                    float flash = MathF.Exp(-(age - h.Fire) / 3.5f);
                    Stroke(batch, origin - direction * 12, origin + direction * 30, new Color(255, 197, 170, 0) * flash, 8);
                    Vector2 n = new(-direction.Y, direction.X);
                    Stroke(batch, origin - n * 26 * flash, origin + n * 26 * flash, new Color(255, 223, 208, 0) * flash, 2);
                }
            }
            CrimsonEnergy.Draw(batch);
        }
        finally { batch.End(); }
    }
    internal static void Stroke(SpriteBatch batch, Vector2 from, Vector2 to, Color color, float width)
    {
        Vector2 d = to - from;
        batch.Draw(TextureAssets.MagicPixel.Value, from - Main.screenPosition, Pixel, color, d.ToRotation(), new Vector2(0, .5f), new Vector2(d.Length(), width), SpriteEffects.None, 0);
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        var boss = CrimsonPackets.Boss;
        if (Main.gameMenu || boss is null || !Local(boss)) return;
        layers.Insert(0, new LegacyGameInterfaceLayer("Convergence: Crimson preparation", () => Overlay(boss), InterfaceScaleType.None));
    }
    private bool Overlay(CrimsonBoss boss)
    {
        var batch = Main.spriteBatch; var state = boss.State; float age = boss.VisualAge;
        var view = Main.instance.GraphicsDevice.Viewport;
        bool intro = state.MusicStart >= 0 && age >= state.MusicStart && age < state.MusicStart + CrimsonRegistration.Score.IntroTicks;
        bool cinematic = state.Stage == CrimsonStage.Deployment || intro || endingAt >= 0;
        if (cinematic)
        {
            float clock = endingAt >= 0 ? age - endingAt : intro ? age - state.MusicStart : age;
            float duration = intro ? CrimsonRegistration.Score.IntroTicks : 150;
            float alpha = Math.Min(Ease(clock / 22), Ease((duration - clock) / 32));
            Fill(new(0, 0, view.Width, (int)(view.Height * .11f)), Color.Black * alpha);
            Fill(new(0, (int)(view.Height * .89f), view.Width, (int)(view.Height * .12f)), Color.Black * alpha);
            if (intro)
            {
                float title = Ease((clock - 80) / 30) * (1 - Ease((clock - 365) / 45));
                Utils.DrawBorderString(batch, "CRIMSON FOUNDRY", new(view.Width * .5f, view.Height * .83f), new Color(248, 206, 194) * title, 1.12f, .5f);
            }
            return false; // Frame-local HUD suppression only, no input/settings flags.
        }
        if (state.Stage != CrimsonStage.Ready) return true;
        foreach (var m in state.Members)
        {
            Player p = Main.player[m.Slot]; if (!p.active) continue;
            Vector2 pos = Vector2.Transform(p.Top - Main.screenPosition, Main.GameViewMatrix.TransformationMatrix) - new Vector2(0, 34);
            if (m.Slot != Main.myPlayer)
            {
                if (m.Ready) Utils.DrawBorderString(batch, "Ready!", pos, new Color(255, 174, 158), .68f, .5f);
                continue;
            }
            int ready = 0; foreach (var peer in state.Members) if (peer.Ready) ready++;
            var button = new Rectangle((int)Math.Clamp(pos.X - 78, 4, view.Width - 160), (int)Math.Clamp(pos.Y - 8, 4, view.Height - 36), 156, 30);
            bool hover = button.Contains(Main.mouseX, Main.mouseY);
            Fill(button, (hover ? new Color(52, 21, 26) : new Color(15, 14, 18)) * .94f);
            Fill(new(button.X, button.Bottom - 2, button.Width * ready / state.Members.Length, 2), new Color(222, 63, 77));
            Utils.DrawBorderString(batch, (m.Ready ? "Ready!" : "READY") + $"  {ready}/{state.Members.Length}", new(button.Center.X, button.Y + 6), new Color(255, 205, 188), .68f, .5f);
            if (hover)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (Main.mouseLeft && Main.mouseLeftRelease) { Main.mouseLeftRelease = false; CrimsonPackets.Ready(!m.Ready); }
            }
        }
        return true;
        void Fill(Rectangle rect, Color color) => batch.Draw(TextureAssets.MagicPixel.Value, rect, Pixel, color);
    }
}

// Shared Global types contain no instance state (tML loader validation).
[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonBossVisuals : GlobalNPC
{
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.ModNPC is CrimsonBoss;
    public override bool PreDraw(NPC npc, SpriteBatch batch, Vector2 screenPos, Color drawColor)
    {
        if (npc.ModNPC is not CrimsonBoss boss) return true;
        var state = boss.State; float age = boss.VisualAge;
        Texture2D heavy = ModContent.Request<Texture2D>("Convergence/Assets/Textures/CrimsonFoundry/FoundryEngine").Value;
        Texture2D light = ModContent.Request<Texture2D>("Convergence/Assets/Textures/CrimsonFoundry/FoundryUnbound").Value;
        float purge = state.PurgeTick < 0 ? -1 : age - state.PurgeTick;
        float deployment = Math.Clamp(age / 110, 0, 1);
        deployment = 1 - MathF.Pow(1 - deployment, 4);
        Vector2 root = npc.Center - screenPos + new Vector2(0, (1 - deployment) * -160);
        float pulse = state.MusicStart < 0 ? 0 : CrimsonRegistration.Score.Pulse(age - state.MusicStart);
        float rotation = Math.Clamp(npc.velocity.X * .006f, -.28f, .28f) + MathF.Sin(age * .016f) * .018f;
        float ending = state.Stage is CrimsonStage.Victory or CrimsonStage.Defeat ? .5f + .5f * MathF.Sin(age * .12f) : 1;
        float alpha = deployment * ending;
        if (purge < 0)
            batch.Draw(heavy, root, null, Color.White * alpha, rotation, heavy.Size() * .5f, .34f, SpriteEffects.None, 0);
        else
        {
            float emergence = Math.Clamp(purge / 36, 0, 1);
            float scale = .235f * (.76f + .24f * (1 - MathF.Pow(1 - emergence, 3)));
            if (!CrimsonVisuals.Reduced)
                for (int i = 4; i > 0; i--) batch.Draw(light, root - npc.velocity * i * .7f, null,
                    new Color(155, 25, 42, 0) * (alpha * .09f), rotation, light.Size() * .5f, scale, SpriteEffects.None, 0);
            batch.Draw(light, root, null, Color.White * alpha * emergence, rotation, light.Size() * .5f, scale, SpriteEffects.None, 0);
            if (purge < 80)
            {
                float travel = MathF.Pow(Math.Clamp(purge / 60, 0, 1), 1.7f);
                for (int y = 0; y < 3; y++) for (int x = 0; x < 4; x++)
                {
                    var rect = new Rectangle(x * heavy.Width / 4, y * heavy.Height / 3, heavy.Width / 4, heavy.Height / 3);
                    Vector2 local = new Vector2(rect.Center.X - heavy.Width * .5f, rect.Center.Y - heavy.Height * .5f) * .34f;
                    Vector2 outward = local.SafeNormalize(new Vector2(x % 2 == 0 ? -1 : 1, -1));
                    Vector2 position = root + local + outward * travel * (260 + x * 28) + new Vector2(0, travel * travel * 150);
                    batch.Draw(heavy, position, rect, Color.White * (1 - Math.Clamp(purge / 80, 0, 1)), rotation + travel * (x - 1.5f),
                        rect.Size() * .5f, .34f, SpriteEffects.None, 0);
                }
            }
        }
        // Red exhaust filaments are code-native and aligned with the moving hull.
        if (pulse > .05f && !CrimsonVisuals.Reduced)
        {
            Vector2 flame = npc.Center + new Vector2(16, 65);
            for (int i = -2; i <= 2; i++)
                CrimsonVisuals.Stroke(batch, flame + new Vector2(i * 11, 0), flame + new Vector2(i * 17, 30 + 55 * pulse), new Color(245, 27, 54, 0) * pulse * .4f, 3);
        }
        return false;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonAttackVisuals : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is CrimsonAttack;
    public override bool PreDraw(Projectile projectile, ref Color lightColor) => false;
}
