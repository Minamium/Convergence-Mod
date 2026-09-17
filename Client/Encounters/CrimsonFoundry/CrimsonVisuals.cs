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
using System.Diagnostics;
using Luminance.Assets;

namespace Convergence.Client.Encounters.CrimsonFoundry;

public sealed class CrimsonVisualConfig : ModConfig
{
    [System.ComponentModel.DefaultValue(true)] public bool CinematicCamera { get; set; } = true;

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
    private readonly List<(ReLogic.Utilities.SlotId Id, int Stop)> voices = new();
    private int lastCharge = -100;
    private static int lastClock;
    private static long clockReceived;
    internal static float RenderAge(CrimsonBoss boss)
    {
        int tick = (int)boss.VisualAge;
        if (tick != lastClock) { lastClock = tick; clockReceived = Stopwatch.GetTimestamp(); }
        return tick + (Main.gamePaused ? 0f : (float)Math.Clamp((Stopwatch.GetTimestamp() - clockReceived) / (double)Stopwatch.Frequency * 60, 0, 1));
    }
    public override void PostSetupContent() => CrimsonRig.Load();
    public override void Unload() { Reset(); CrimsonRig.Unload(); }
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
        if (boss.State.FinalStart >= 0 && previousAge < boss.State.FinalStart && age >= boss.State.FinalStart && age - boss.State.FinalStart < 8)
        { Cue("RaidDesignation", .28f, age + 90); shake = 8; }
        foreach (NPC n in Main.ActiveNPCs)
            if (n.ModNPC is CrimsonEffigy e && e.State.Fight == fight && previousAge < e.State.Born + 12
                && age >= e.State.Born + 12 && age - e.State.Born < 18)
            { Cue("Beams/PortalFire", .30f, age + 55); shake = 6; }
        if (boss.State.MusicStart >= 0 && previousAge < boss.State.MusicStart && age >= boss.State.MusicStart && age - boss.State.MusicStart < 8)
        { Cue("RaidDesignation", .24f, age + 120); shake = 5; }
        if (boss.State.Stage is CrimsonStage.Victory or CrimsonStage.Defeat && endingAt < 0)
        {
            endingAt = age;
            Cue(boss.State.Stage == CrimsonStage.Victory ? "RaidVictory" : "RaidDefeat", .40f, age + 150); shake = 8;
        }
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is not CrimsonAttack a || a.Hazard.Fight != fight) continue;
            int chargeTick = a.Hazard.Born;
            if (previousAge < chargeTick && age >= chargeTick && age - chargeTick < 5 && lastCharge != chargeTick)
            { lastCharge = chargeTick; Cue("Beams/PortalCharge", .20f + a.Hazard.Accent * .035f, chargeTick + 10); }
            if (previousAge < a.Hazard.Fire && age >= a.Hazard.Fire && age - a.Hazard.Fire < 5 && lastFire != a.Hazard.Fire)
            { lastFire = a.Hazard.Fire; Cue("Beams/PortalFire", .28f + a.Hazard.Accent * .05f, a.Hazard.End + 8); shake = Math.Max(shake, 2.5f + a.Hazard.Accent * 1.5f); }
        }
        for (int i = voices.Count - 1; i >= 0; i--)
        {
            var voice = voices[i];
            if (!SoundEngine.TryGetActiveSound(voice.Id, out var sound)) { voices.RemoveAt(i); continue; }
            if (age >= voice.Stop) { sound.Stop(); voices.RemoveAt(i); }
            else sound.Volume = Math.Min(sound.Volume, Math.Clamp((voice.Stop - age) / 14f, 0, 1));
        }
        previousAge = age;
    }
    private void Cue(string asset, float gain, int stop)
    {
        if (voices.Count >= 32) return;
        voices.Add((SoundEngine.PlaySound(new SoundStyle("Convergence/Assets/Sounds/FirstSeverance/" + asset)
        { Volume = gain, MaxInstances = 3, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PlayOnlyIfFocused = true, PauseBehavior = PauseBehavior.StopWhenGamePaused }), stop));
    }
    private void Reset()
    {
        foreach (var voice in voices) if (SoundEngine.TryGetActiveSound(voice.Id, out var sound)) sound.Stop();
        voices.Clear(); fight = Guid.Empty; previousAge = -1; lastFire = lastCharge = -100; endingAt = -1; shake = 0;
    }
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
            float age = RenderAge(boss);
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile is not CrimsonAttack attack || attack.Hazard.Fight != boss.State.Fight) continue;
                var h = attack.Hazard;
                if (h.Epoch != boss.State.PhaseStart || !boss.Fresh || age >= h.End + 8 || age < h.Born
                    || !CrimsonPhaseRules.ActiveSource(boss.State.Phase, boss.State.DefeatedMask, boss.State.PerformerDefeated, h.Source)) continue;
                bool live = age >= h.Fire;
                Vector2 origin = new(h.X, h.Y), direction = new(h.DX, h.DY);
                float length = h.Length, width = h.Width, opacity = live ? 1 : .28f + .72f * MathF.Exp(-(age - h.Born) / 4);
                if (live)
                {
                    width = h.HitWidth(age);
                    length = h.Reach(age);
                    if (h.Shape == CrimsonShape.Bolt) origin += direction * Math.Max(0, h.Travel(age) - CrimsonHazard.BoltTail);
                    if (age >= h.End) opacity = 1 - Ease((age - h.End) / 8);
                }
                CrimsonEnergy.Add(origin, direction, length, width, age, h.Fire, h.End, opacity, Reduced);
                if (!live)
                {
                    Vector2 normal = new(-direction.Y, direction.X);
                    Color edge = new Color(255, 108, 115, 0) * (.15f + .55f * MathF.Exp(-(age - h.Born) / 4));
                    for (int side = -1; side <= 1; side += 2)
                        Stroke(batch, origin + normal * width * side, origin + direction * length + normal * width * side, edge, 1.25f);
                }
                if (!Reduced)
                {
                    var glow = MiscTexturesRegistry.BloomCircleSmall.Value;
                    int sparks = live ? 12 : 8;
                    for (int i = 0; i < sparks; i++)
                    {
                        float u = ((i * .173f + age * (live ? .024f : .001f) + h.Born * .001f) % 1 + 1) % 1;
                        Vector2 n = new(-direction.Y, direction.X);
                        Vector2 at = origin + direction * length * u + n * MathF.Sin(i * 4.1f + age * .12f) * width * .72f;
                        float intensity = live ? .45f : .20f * Ease((age - h.Born) / 20);
                        Vector2 size = live ? new(12, 2.5f) : new(3, 3);
                        batch.Draw(glow, at - Main.screenPosition, null, new Color(255, 166, 155, 0) * intensity * opacity,
                            direction.ToRotation(), glow.Size() * .5f, size * 2 / glow.Size(), SpriteEffects.None, 0);
                    }
                }
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
        // Physical pixels, transformed ONCE; UI scale never enters world masks.
        var field = state.Field;
        Vector2 tl = Vector2.Transform(new Vector2(field.Left, field.Top) - Main.screenPosition, Main.GameViewMatrix.TransformationMatrix);
        Vector2 br = Vector2.Transform(new Vector2(field.Right, field.Bottom) - Main.screenPosition, Main.GameViewMatrix.TransformationMatrix);
        int left = Math.Clamp((int)MathF.Floor(tl.X), 0, view.Width), right = Math.Clamp((int)MathF.Ceiling(br.X), 0, view.Width);
        int top = Math.Clamp((int)MathF.Floor(tl.Y), 0, view.Height), bottom = Math.Clamp((int)MathF.Ceiling(br.Y), 0, view.Height);
        // Eliminated/respawned spectators can be outside the stage. Never cover
        // their entire unrelated world view with the participant-only mask.
        if (state.Contains(Main.myPlayer) && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost)
        {
            Fill(new(0, 0, view.Width, top), Color.Black); Fill(new(0, bottom, view.Width, view.Height - bottom), Color.Black);
            Fill(new(0, top, left, Math.Max(0, bottom - top)), Color.Black);
            Fill(new(right, top, view.Width - right, Math.Max(0, bottom - top)), Color.Black);
            Color edge = new(213, 53, 67);
            if (tl.X >= 0 && tl.X < view.Width) Fill(new(left, top, 2, Math.Max(0, bottom - top)), edge);
            if (br.X > 0 && br.X <= view.Width) Fill(new(Math.Max(0, right - 2), top, 2, Math.Max(0, bottom - top)), edge);
            if (tl.Y >= 0 && tl.Y < view.Height) Fill(new(left, top, Math.Max(0, right - left), 2), edge);
            if (br.Y > 0 && br.Y <= view.Height) Fill(new(left, Math.Max(0, bottom - 2), Math.Max(0, right - left), 2), edge);
        }
        bool intro = state.MusicStart >= 0 && age < state.MusicStart + CrimsonRegistration.Score.IntroTicks;
        bool manifest = state.FinalStart >= 0 && age < state.FinalStart + CrimsonInvocation.ManifestTicks;
        bool cinematic = state.Stage == CrimsonStage.Deployment || intro || manifest || endingAt >= 0;
        if (cinematic)
        {
            float clock = endingAt >= 0 ? age - endingAt : manifest ? age - state.FinalStart : intro ? age - state.MusicStart : age;
            float alpha = endingAt >= 0 || manifest ? Math.Min(Ease(clock / 22), Ease((150 - clock) / 32))
                : CrimsonInvocation.OpeningBars(state.Stage, age, state.MusicStart, CrimsonRegistration.Score.IntroTicks);
            Fill(new(0, 0, view.Width, (int)(view.Height * .11f)), Color.Black * alpha);
            Fill(new(0, (int)(view.Height * .89f), view.Width, (int)(view.Height * .12f)), Color.Black * alpha);
            if (intro)
            {
                float title = Ease((clock - 80) / 30) * (1 - Ease((clock - 365) / 45));
                Utils.DrawBorderString(batch, Terraria.Localization.Language.GetTextValue("Mods.Convergence.CrimsonFoundry.RaidTitle"), new(view.Width * .5f, view.Height * .83f), new Color(248, 206, 194) * title, 1.12f, .5f);
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
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.ModNPC is CrimsonBoss or CrimsonEffigy;
    public override bool PreDraw(NPC npc, SpriteBatch batch, Vector2 screenPos, Color drawColor)
    {
        return npc.ModNPC switch { CrimsonBoss boss => CrimsonRig.Draw(boss, batch, screenPos),
            CrimsonEffigy effigy => CrimsonRig.DrawEffigy(effigy, batch, screenPos), _ => true };
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonAttackVisuals : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is CrimsonAttack;
    public override bool PreDraw(Projectile projectile, ref Color lightColor) => false;
}
