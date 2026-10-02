#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Encounters.FirstSeverance;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace Convergence.Client.Encounters.GhostSamurai;

// The Ghost Samurai cinematics for those who fight it: the summoning, each change of
// form, the victory and the wipe. The HUD steps aside behind letterbox bars only for the
// cut's frames (a layer that stops the rest of the HUD that frame; no hideUI, input or
// zoom flag), the camera eases onto the samurai and back, and a title is set. Players
// keep control; the summon and change-of-form cuts sit inside the authority's harmless
// windows and the endings play over a fight that is already over.
[Autoload(Side = ModSide.Client)]
internal sealed class GhostSamuraiCinema : ModSystem
{
    private const string SealLayer = "Convergence: Samurai seal";
    private static SamuraiCut cut;
    private static Guid fight, seenFight;
    private static SamuraiPhase seenPhase, cutPhase;
    private static ulong startTick, systemTick = ulong.MaxValue;
    private static Vector2 focus, lastFocus, aim;
    private static bool focusSet;
    private static int cues;
    private static float shake, ringAt = -1, ringScale;
    private static readonly Vector2[] rings = new Vector2[2];
    private static int ringCount;
    private static string title = string.Empty, epithet = string.Empty;
    private static LegacyGameInterfaceLayer? layer;

    private static float Time => cut == SamuraiCut.None ? 0 : Main.GameUpdateCount - startTick - 1 + GhostSamuraiPresentation.Fraction;
    private static bool Shaking => !GhostSamuraiRigArt.Reduced && ModContent.GetInstance<FirstSeveranceVisualConfig>().ScreenShake;

    public override void Load()
    {
        if (!Main.dedServ) layer = new LegacyGameInterfaceLayer("Convergence: Samurai cinema", DrawLayer, InterfaceScaleType.None);
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ) return;
        if (Main.gameMenu) { Stop(); return; }
        if (Main.gamePaused) return;
        ulong now = Main.GameUpdateCount;
        if (systemTick == now) return;
        systemTick = now;
        GhostSamuraiBoss? boss = null;
        if (GhostSamuraiPresentation.Ending is { Participant: true } ending)
        {
            var kind = ending.Victory ? SamuraiCut.Victory : SamuraiCut.Defeat;
            if (cut != kind || fight != ending.Fight)
                Begin(kind, ending.Fight, ending.Since, new(ending.X, Math.Clamp((ending.Y + ending.Floor) * .5f, ending.Y + 40, ending.Y + 300)));
        }
        else if ((boss = GhostSamuraiMusicScene.Listening(Main.LocalPlayer)) is not null)
        {
            if (seenFight != boss.Fight)
            {
                // A fresh summoning is staged; walking into a running fight is not.
                seenFight = boss.Fight; seenPhase = boss.Phase;
                float age = boss.VisualAge;
                if (age < SamuraiCinematics.SummonDuration - 60)
                    Begin(SamuraiCut.Summon, boss.Fight, now - (ulong)Math.Max(0, age), Framing(boss, SamuraiCut.Summon));
            }
            else if (boss.Phase != seenPhase)
            {
                seenPhase = boss.Phase;
                if (boss.TransitionRemaining > 0)
                {
                    cutPhase = boss.Phase;
                    Begin(SamuraiCut.Phase, boss.Fight, now - (ulong)(GhostSamuraiRules.TransitionTime - boss.TransitionRemaining),
                        Framing(boss, SamuraiCut.Phase));
                }
            }
            if (cut is SamuraiCut.Summon or SamuraiCut.Phase && fight == boss.Fight) aim = Framing(boss, cut);
        }
        else if (cut is SamuraiCut.Summon or SamuraiCut.Phase) cut = SamuraiCut.None;   // the fight ended or we left it
        shake *= .88f;
        if (cut == SamuraiCut.None) return;
        float t = now - startTick;
        if (t >= SamuraiCinematics.Duration(cut)) { cut = SamuraiCut.None; return; }
        lastFocus = focusSet ? focus : aim;
        focus = focusSet ? Vector2.Lerp(focus, aim, .14f) : aim;
        focusSet = true;
        Cues(t, boss);
    }

    private static Vector2 Framing(GhostSamuraiBoss boss, SamuraiCut kind)
    {
        Vector2 c = boss.PresentationCenter;
        // the summoning keeps the floor of the seal in frame beneath the samurai
        return kind == SamuraiCut.Summon ? new(c.X, MathHelper.Lerp(c.Y, boss.Arena.Bottom, .3f)) : new(c.X, c.Y + 40);
    }

    private static void Begin(SamuraiCut kind, Guid id, ulong since, Vector2 at)
    {
        cut = kind; fight = id; startTick = since; aim = at; focusSet = false; cues = 0; ringAt = -1;
        string key = kind switch
        {
            SamuraiCut.Summon => "Summon",
            SamuraiCut.Phase => cutPhase == SamuraiPhase.Phase3 ? "PhaseThree" : "PhaseTwo",
            SamuraiCut.Victory => "Victory",
            _ => "Defeat",
        };
        title = kind == SamuraiCut.Summon ? Lang.GetNPCNameValue(ModContent.NPCType<GhostSamuraiBoss>())
            : Language.GetTextValue($"Mods.Convergence.GhostSamurai.Cinema.{key}Title");
        epithet = Language.GetTextValue($"Mods.Convergence.GhostSamurai.Cinema.{key}Epithet");
    }

    // Each cue sounds once, and only near its moment (a late joiner does not hear a backlog).
    private static void Cues(float t, GhostSamuraiBoss? boss)
    {
        var mist = ModContent.GetInstance<GhostSamuraiMist>();
        Vector2 body = boss?.PresentationCenter ?? aim;
        switch (cut)
        {
            case SamuraiCut.Summon:
                if (t >= SamuraiCinematics.ManifestStart && t < SamuraiCinematics.ManifestEnd - 20 && (int)t % 4 == 0)
                {
                    // spirit mist drawn in from all sides while the samurai gathers itself
                    Vector2 d = (t * 1.37f).ToRotationVector2();
                    mist.Emit(fight, body + d * (110 + (int)t % 3 * 18), -d * 3.2f, 26, 36);
                }
                if (Due(0, t, SamuraiCinematics.ManifestStart)) Play(GhostSamuraiAudio.ChimeSound with { Volume = .85f, Pitch = -.55f }, body);
                if (Due(1, t, SamuraiCinematics.SummonShout)) Play(GhostSamuraiAudio.DashShoutSound, body);
                if (Due(2, t, SamuraiCinematics.SummonShout + 6)) { Play(GhostSamuraiAudio.ShockSound, body); Burst(body, 9, 1.2f, mist); }
                break;
            case SamuraiCut.Phase:
                if (Due(0, t, SamuraiCinematics.PhaseShout)) Play(GhostSamuraiAudio.DashShoutSound, body);
                if (Due(1, t, SamuraiCinematics.PhaseBurst))
                {
                    Play(GhostSamuraiAudio.ShockSound, body);
                    Play(GhostSamuraiAudio.ChimeSound with { Volume = .8f, Pitch = -.3f }, body);
                    Burst(body, 7, 1, mist);
                }
                break;
            case SamuraiCut.Victory:
                if (Due(0, t, SamuraiCinematics.BladeLand) && GhostSamuraiPresentation.Ending is { } won)
                {
                    Play(GhostSamuraiAudio.SlashSound with { Volume = .6f, Pitch = -.4f }, aim);
                    ringCount = 0;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 plant = new(won.X + side * SamuraiCinematics.BladePlantOffset, won.Floor);
                        rings[ringCount++] = plant;
                        for (int i = 0; i < 5; i++)
                            mist.Emit(fight, plant + new Vector2((i - 2) * 9, -4), new Vector2((i - 2) * .7f, -1.2f), 20, 30);
                    }
                    ringAt = t; ringScale = .18f;
                    if (Shaking) shake = Math.Max(shake, 3);
                }
                if (Due(1, t, SamuraiCinematics.RequiemBell)) Play(GhostSamuraiAudio.ChimeSound with { Volume = .8f, Pitch = -.7f }, aim);
                break;
            case SamuraiCut.Defeat:
                if (Due(0, t, SamuraiCinematics.DefeatBell)) Play(GhostSamuraiAudio.ChimeSound with { Volume = .75f, Pitch = -.8f }, aim);
                break;
        }
    }

    private static bool Due(int bit, float t, float at)
    {
        if (t < at || (cues & 1 << bit) != 0) return false;
        cues |= 1 << bit;
        return t < at + 12;
    }

    private static void Play(SoundStyle style, Vector2 at) => SoundEngine.PlaySound(style, at);

    private static void Burst(Vector2 at, float strength, float scale, GhostSamuraiMist mist)
    {
        rings[0] = at; ringCount = 1; ringAt = Main.GameUpdateCount - startTick; ringScale = scale;
        for (int i = 0; i < 18; i++)
        {
            Vector2 d = (i * MathHelper.TwoPi / 18).ToRotationVector2();
            mist.Emit(fight, at + d * 30, d * 3.4f, 30, 34);
        }
        if (Shaking) shake = Math.Max(shake, strength);
    }

    public override void ModifyScreenPosition()
    {
        if (cut == SamuraiCut.None || Main.gameMenu || !focusSet) return;
        float w = SamuraiCinematics.Camera(cut, Time);
        Vector2 at = Vector2.Lerp(lastFocus, focus, GhostSamuraiPresentation.Fraction);
        if (w > 0) Main.screenPosition = Vector2.Lerp(Main.screenPosition, at - new Vector2(Main.screenWidth, Main.screenHeight) * .5f, w);
        if (shake > .05f)
        {
            float v = (float)Main.timeForVisualEffects;
            Main.screenPosition += new Vector2(MathF.Sin(v * 1.9f) + MathF.Sin(v * 3.7f) * .5f, MathF.Cos(v * 2.3f) + MathF.Sin(v * 4.1f) * .5f) * shake * .67f;
        }
    }

    // The shockwave of a shout or a planted blade, behind actors and inside the seal.
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu || cut == SamuraiCut.None || ringAt < 0) return;
        float age = Time - ringAt;
        const float Life = 34;
        if (age < 0 || age > Life) return;
        float k = age / Life, fade = MathF.Pow(1 - k, 1.5f);
        var batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            for (int i = 0; i < ringCount; i++)
            {
                Vector2 c = rings[i] - Main.screenPosition;
                float radius = ringScale * (40 + 1100 * SamuraiRigMotion.Out(k));
                GhostSamuraiVisuals.Ring(batch, c, radius, 2 + 9 * (1 - k) * ringScale, new Color(147, 91, 220) * fade * .55f);
                GhostSamuraiVisuals.Ring(batch, c, radius * .97f, 1.5f + 2 * (1 - k), new Color(239, 228, 255) * fade * .8f);
            }
        }
        finally { batch.End(); }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (layer is null || Main.gameMenu || cut == SamuraiCut.None || SamuraiCinematics.Bars(cut, Time) <= .01f) return;
        // after the seal (which must still cover the outside), before every other HUD layer
        layers.Insert(layers.FindIndex(l => l.Name == SealLayer) + 1, layer);
    }

    private static bool DrawLayer()
    {
        if (cut == SamuraiCut.None) return true;
        float t = Time, bars = SamuraiCinematics.Bars(cut, t);
        if (bars <= .01f) return true;
        var batch = Main.spriteBatch;
        var viewport = Main.instance.GraphicsDevice.Viewport;
        float w = viewport.Width, h = viewport.Height, band = h * .11f * bars;
        Fill(batch, 0, 0, w, band, Color.Black);
        Fill(batch, 0, h - band, w, band + 1, Color.Black);
        float a = SamuraiCinematics.Title(cut, t);
        if (a > .01f) Caption(batch, w, h, a);
        return false;   // the rest of the HUD steps aside for this frame only
    }

    private static void Caption(SpriteBatch batch, float w, float h, float a)
    {
        float s = h / 1080f;
        string big = Spaced(title);
        var font = FontAssets.DeathText.Value;
        float scale = .86f * s;
        Vector2 size = font.MeasureString(big) * scale;
        Vector2 at = new(w * .5f, h * .78f);
        float open = SamuraiRigMotion.Out(a);
        float rule = (90 + 150 * open) * s, gap = size.X * .5f + 26 * s;
        var violet = new Color(147, 91, 220) * a;
        Fill(batch, at.X - gap - rule, at.Y - 1 * s, rule, 2 * s, violet);
        Fill(batch, at.X + gap, at.Y - 1 * s, rule, 2 * s, violet);
        Fill(batch, at.X - gap - 5 * s, at.Y - 3 * s, 6 * s, 6 * s, new Color(239, 228, 255) * a);
        Fill(batch, at.X + gap - 1 * s, at.Y - 3 * s, 6 * s, 6 * s, new Color(239, 228, 255) * a);
        Utils.DrawBorderStringBig(batch, big, at + new Vector2(0, (1 - open) * 10 * s), new Color(236, 228, 255) * a, scale, .5f, .5f);
        Utils.DrawBorderString(batch, epithet, at + new Vector2(0, size.Y * .5f + 22 * s), new Color(196, 176, 245) * a, 1.05f * s, .5f, .5f);
    }

    // CJK titles breathe with a space between glyphs; Latin titles are set in capitals.
    private static string Spaced(string text)
    {
        if (text.Length == 0) return text;
        bool latin = text[0] < 0x2E80;
        return latin ? text.ToUpperInvariant() : string.Join(" ", text.ToCharArray());
    }

    private static void Fill(SpriteBatch batch, float x, float y, float width, float height, Color color)
    {
        if (width <= 0 || height <= 0) return;
        batch.Draw(TextureAssets.MagicPixel.Value, new Vector2(x, y), new Rectangle(0, 0, 1, 1), color, 0, Vector2.Zero,
            new Vector2(width, height), SpriteEffects.None, 0);
    }

    private static void Stop()
    {
        cut = SamuraiCut.None; fight = seenFight = Guid.Empty; seenPhase = cutPhase = default;
        startTick = 0; systemTick = ulong.MaxValue; focusSet = false; cues = 0; shake = 0; ringAt = -1; ringCount = 0;
    }
    public override void ClearWorld() => Stop();
    public override void OnWorldUnload() => Stop();
    public override void Unload() { Stop(); layer = null; }
}
