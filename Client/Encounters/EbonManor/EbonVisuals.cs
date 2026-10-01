#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Convergence.Content.Encounters.EbonManor;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace Convergence.Client.Encounters.EbonManor;

public sealed class EbonVisualConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    [DefaultValue(false)] public bool ReducedEffects;
    [DefaultValue(true)] public bool ScreenShake = true;
}

// Client presentation owner: accepted-clock sound cues, local shake and
// framing, secondary motion updates, the world pass and the physical-pixel
// field overlay. Everything resets on a new Fight, world unload or Mod unload.
[Autoload(Side = ModSide.Client)]
internal sealed class EbonVisuals : ModSystem
{
    private Guid fight;
    private int previous = -1, lastPhaseAt = -1;
    private EbonPhase lastPhase;
    private Vector2 lastRoot;
    private ulong lastUpdate;
    private Matrix worldToViewport;
    private Guid projectedFight;
    private readonly List<ReLogic.Utilities.SlotId> voices = new();
    private readonly Dictionary<string, int> lastCue = new();
    private readonly Dictionary<(EbonAttackKind, int, bool), int> playedAttack = new();
    private readonly List<ScreenShakeSystem.ShakeInfo> shakes = new(8);
    private int lastStitch = -1, lastVerdict = -1;
    private static int lastTick;
    private static long received;
    internal static bool Reduced => ModContent.GetInstance<EbonVisualConfig>().ReducedEffects;
    internal static bool Local(EbonBoss boss) => Array.Exists(boss.State.Members ?? Array.Empty<EbonMember>(), m => m.Slot == Main.myPlayer);
    internal static float RenderAge(EbonBoss boss)
    {
        int tick = (int)boss.VisualAge;
        if (lastTick != tick) { lastTick = tick; received = Stopwatch.GetTimestamp(); }
        return tick + (Main.gamePaused ? 0 : (float)Math.Clamp((Stopwatch.GetTimestamp() - received) / (double)Stopwatch.Frequency * 60, 0, 1));
    }

    public override void PostUpdateEverything()
    {
        var boss = EbonPackets.Boss;
        if (boss is null || !boss.Fresh) { Reset(); return; }
        var s = boss.State;
        int age = (int)boss.VisualAge;
        if (fight != s.Fight) { Reset(); fight = s.Fight; previous = age - 1; lastPhase = s.Phase; lastPhaseAt = s.PhaseAt; }
        var pose = EbonNoirette.Resolve(boss, age);
        // Remember where she stood before an act change moved her to the centre.
        if (s.Phase != lastPhase || s.PhaseAt != lastPhaseAt)
        {
            if (s.Phase > lastPhase && lastRoot != Vector2.Zero) { EbonCeremony.Departure = lastRoot; EbonCeremony.DepartureEpoch = s.PhaseAt; }
            lastPhase = s.Phase; lastPhaseAt = s.PhaseAt;
        }
        if (!s.Transition(age) && !pose.Hidden) lastRoot = pose.Root + pose.Offset;
        if (lastUpdate != Main.GameUpdateCount && !Main.gamePaused)
        {
            lastUpdate = Main.GameUpdateCount;
            EbonNoirette.Update(pose.Root + pose.Offset);
            EbonThreads.Update(boss, pose, age);
        }
        if (!Local(boss)) { previous = age; return; }
        ModContent.GetInstance<EbonMusicScene>().UpdateFade(boss, age);
        Cues(boss, age);
        Camera(boss, age);
        if (Reduced || !ModContent.GetInstance<EbonVisualConfig>().ScreenShake) foreach (var info in shakes) info.ShakeStrength = 0;
        shakes.RemoveAll(info => info.ShakeStrength <= .01f);
        for (int i = voices.Count - 1; i >= 0; i--) if (!SoundEngine.TryGetActiveSound(voices[i], out _)) voices.RemoveAt(i);
        previous = age;
    }

    private bool Crossed(int tick, int age) => tick >= 0 && previous < tick && age >= tick && age - tick < 8;

    private void Cues(EbonBoss boss, int age)
    {
        var s = boss.State;
        if (s.MusicStart >= 0)
        {
            // Entrance accents ride the intro's own landmarks (see EbonIntro).
            int bar(float n) => EbonIntro.Tick(s, n);
            if (Crossed(bar(EbonIntro.Burst), age)) { Play("ThreadSnap", .42f, -.1f); Shake(boss.NPC.Center, 3, Vector2.UnitY); }
            if (Crossed(bar(EbonIntro.Break), age)) Play("Weave", .55f);
            if (Crossed(bar(EbonIntro.Bloom), age)) Play("WaltzOpen", .42f);
            if (Crossed(bar(EbonIntro.Hush), age)) Play("SilkCast", .5f, .15f);
            if (Crossed(s.UnlockAt, age)) { Play("SilkBurst", .6f); Shake(boss.NPC.Center, 6, Vector2.UnitY); }
        }
        if (s.Phase != EbonPhase.ActOne && s.PhaseAt >= 0)
        {
            if (Crossed(s.PhaseAt, age)) { Play("ActChange", .62f); Shake(boss.NPC.Center, 5, Vector2.UnitX); }
            if (s.Phase == EbonPhase.Finale && Crossed(s.PhaseAt + (int)EbonRules.BarTicks, age)) Play("ManorTear", .66f);
            if (Crossed(s.Epoch, age)) { Play("SilkBurst", .62f, s.Phase == EbonPhase.Finale ? -.1f : 0); Shake(boss.NPC.Center, s.Phase == EbonPhase.Finale ? 11 : 6, Vector2.UnitY); }
        }
        if (s.EndAt >= 0)
        {
            if (s.Stage == EbonStage.Victory)
            {
                if (Crossed(s.EndAt, age)) { Play("CurtainFall", .66f); Shake(boss.NPC.Center, 8, Vector2.UnitY); }
                for (int k = 0; k < 6; k++)
                    if (Crossed(s.EndAt + 24 + (int)MathF.Round(k * EbonRules.BeatTicks), age)) Play("ThreadSnap", .5f, k * .05f);
            }
            else if (Crossed(s.EndAt, age)) PlayShared("RaidDefeat", .45f);
        }
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is EbonStitch m && m.Plan.Fight == s.Fight)
            {
                if (lastStitch != m.Plan.Born && Crossed(m.Plan.Born, age)) { lastStitch = m.Plan.Born; Play("StitchCall", .5f, m.Plan.Kind == EbonStitchKind.Spread ? .12f : 0); }
                if (m.Resolved && lastVerdict != m.Plan.Fire && age >= m.Plan.Fire && age < m.Plan.End)
                {
                    lastVerdict = m.Plan.Fire;
                    bool failed = m.FailedMask != 0;
                    Play(m.Plan.Kind == EbonStitchKind.Stack && !failed ? "StitchBind" : "StitchTear", .55f);
                    Shake(Main.LocalPlayer.Center, failed ? 7 : 3, Vector2.UnitY);
                }
            }
            if (p.ModProjectile is not EbonAttack a || a.Plan.Fight != s.Fight || !a.TryBoss(out _)) continue;
            var h = a.Plan;
            int beat = (int)MathF.Round((h.Born - s.Epoch) / EbonRules.BeatTicks);
            if (Crossed(h.Born, age) && Once(h.Kind, h.Born, false))
                switch (h.Kind)
                {
                    case EbonAttackKind.Thread: Play("SilkCast", .42f, Scale(beat)); break;
                    case EbonAttackKind.Chandelier: Play("ChandelierCreak", .5f); break;
                    case EbonAttackKind.Loom: Play("LoomTighten", .5f); break;
                    case EbonAttackKind.Shears: Play("ShearsOpen", .52f); break;
                    case EbonAttackKind.Waltz: Play("WaltzOpen", .56f); break;
                    case EbonAttackKind.Web when h.Variant == 0: Play("WebWeave", .55f); break;
                }
            if (Crossed(h.Fire, age) && Once(h.Kind, h.Fire, true))
                switch (h.Kind)
                {
                    case EbonAttackKind.Thread: Play("ThreadYank", .5f, Scale(beat)); break;
                    case EbonAttackKind.Chandelier: Play("ThreadSnap", .56f); break;
                    case EbonAttackKind.Loom: Play("LoomTwang", .6f); Shake(Main.LocalPlayer.Center, 2.5f, Vector2.UnitX); break;
                    case EbonAttackKind.Shears: Play("ShearsSnip", .64f); Shake(Main.LocalPlayer.Center, 5, h.Angle.ToRotationVector2()); break;
                    case EbonAttackKind.Waltz: Play("WaltzRelease", .55f); break;
                    case EbonAttackKind.Web: Play("WebSever", .62f); Shake(Main.LocalPlayer.Center, 4.5f, h.Angle.ToRotationVector2()); break;
                }
            switch (h.Kind)
            {
                case EbonAttackKind.Thread when Crossed(EbonScene.CrashTick(h), age):
                {
                    var at = EbonScene.CrashPoint(h);
                    EbonScene.Add(s.Fight, new(true, EbonAftermathKind.PropCrash, at, at, h.Angle, EbonScene.CrashTick(h), h.Born * 31 + h.Variant, h.Variant));
                    Play("PropCrash", .52f, (h.Variant - 3) * .04f, 0, at);
                    Shake(at, 3.5f, h.Angle.ToRotationVector2());
                    break;
                }
                case EbonAttackKind.Chandelier when Crossed(EbonGeometry.ImpactTick(h), age):
                {
                    var at = EbonGeometry.Burst(h);
                    EbonScene.Add(s.Fight, new(true, EbonAftermathKind.ChandelierWreck, at, at, (h.Born % 2 == 0 ? 1 : -1) * .35f, EbonGeometry.ImpactTick(h), h.Born * 17, h.Variant));
                    Play("ChandelierShatter", .66f, 0, 0, at);
                    Shake(at, 7, Vector2.UnitY);
                    break;
                }
                case EbonAttackKind.Loom or EbonAttackKind.Web when Crossed(h.End, age) && EbonScene.Segment(s, h, out var la, out var lb):
                    EbonScene.Add(s.Fight, new(true, EbonAftermathKind.LoomSnap, la, lb, 0, h.End, h.Born + (int)h.X, 0));
                    break;
                case EbonAttackKind.Shears when Crossed(h.End, age) && EbonScene.Segment(s, h, out var sa, out var sb):
                    EbonScene.Add(s.Fight, new(true, EbonAftermathKind.ShearsHeal, sa, sb, h.Width, h.End, h.Born, 0));
                    break;
                case EbonAttackKind.Waltz when Crossed(h.End, age):
                {
                    var c = EbonGeometry.Center(s, h.End);
                    for (int k = 0; k < h.Variant; k++)
                    {
                        var d = EbonGeometry.SpokeAngle(h, k, h.End).ToRotationVector2();
                        if (s.Field.ClipAxis(c.X, c.Y, d.X, d.Y, out _, out float last) && last > 40)
                            EbonScene.Add(s.Fight, new(true, EbonAftermathKind.SpokeRelease, c + d * 40, c + d * Math.Min(last, h.Length), 0, h.End, h.Born + k, k));
                    }
                    Play("ThreadSnap", .45f, .2f);
                    break;
                }
            }
        }
    }
    // Plucks follow a minor arpeggio across each bar (semitones as octave fractions).
    private static readonly int[] arpeggio = { 0, 3, 7, 10, 12, 10, 7, 3 };
    private static float Scale(int beat) => arpeggio[((beat % 8) + 8) % 8] / 12f;
    private bool Once(EbonAttackKind kind, int tick, bool fire)
    {
        if (playedAttack.TryGetValue((kind, tick, fire), out _)) return false;
        if (playedAttack.Count > 96) playedAttack.Clear();
        playedAttack[(kind, tick, fire)] = tick; return true;
    }

    private void Play(string cue, float volume, float pitch = 0, int spacing = 0, Vector2? at = null)
    {
        if (Main.dedServ || voices.Count >= 24) return;
        int now = (int)(EbonPackets.Boss?.VisualAge ?? 0);
        if (spacing > 0 && lastCue.TryGetValue(cue, out int last) && now - last < spacing) return;
        lastCue[cue] = now;
        voices.Add(SoundEngine.PlaySound(new SoundStyle("Convergence/Assets/Sounds/EbonManor/" + cue)
        { Volume = volume * (Reduced ? .75f : 1), Pitch = pitch, MaxInstances = 4, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest }, at));
    }
    private void PlayShared(string cue, float volume)
    {
        if (Main.dedServ || voices.Count >= 24) return;
        voices.Add(SoundEngine.PlaySound(new SoundStyle("Convergence/Assets/Sounds/FirstSeverance/" + cue) { Volume = volume * (Reduced ? .75f : 1), MaxInstances = 1 }));
    }
    private void Shake(Vector2 at, float strength, Vector2 direction)
    {
        if (Main.dedServ || Reduced || !ModContent.GetInstance<EbonVisualConfig>().ScreenShake || shakes.Count >= 6) return;
        // Full-field hazards are felt from the far side of the hall.
        var center = Vector2.Lerp(Main.LocalPlayer.Center, at, .2f);
        shakes.Add(ScreenShakeSystem.StartShakeAtPoint(center, strength, shakeDirection: direction.SafeNormalize(Vector2.UnitY),
            angularVariance: .7f, shakeStrengthDissipationIncrement: .5f));
    }

    // Framing toward the stage during the entrance, act changes and the curtain.
    private static void Camera(EbonBoss boss, int age)
    {
        var s = boss.State;
        if (Main.LocalPlayer.dead || Main.LocalPlayer.ghost) return;
        float weight = 0;
        if (s.Stage == EbonStage.Countdown && s.MusicStart >= 0)
        {
            float t = age - s.MusicStart;
            weight = EbonVisualsMath.Ease(t / 60) * EbonVisualsMath.Ease((EbonRules.Intro + 20 - t) / 90);
        }
        else if (s.Transition(age))
        {
            float t = age - s.PhaseAt, lead = EbonRules.Lead(s.Phase);
            weight = .75f * EbonVisualsMath.Ease(t / 30) * EbonVisualsMath.Ease((lead - t) / 50);
        }
        else if (s.Stage == EbonStage.Victory && s.EndAt >= 0)
        {
            float t = age - s.EndAt;
            weight = .8f * EbonVisualsMath.Ease(t / 50) * EbonVisualsMath.Ease((EbonRules.VictoryEnding - t) / 70);
        }
        if (weight > .001f) CameraPanSystem.PanTowards(new(s.Field.CenterX, s.Field.Top + (s.Field.Bottom - s.Field.Top) * .42f), weight);
    }

    private void Reset()
    {
        foreach (var id in voices) if (SoundEngine.TryGetActiveSound(id, out var voice)) voice.Stop();
        voices.Clear(); lastCue.Clear(); playedAttack.Clear();
        foreach (var info in shakes) info.ShakeStrength = 0;
        shakes.Clear();
        fight = projectedFight = Guid.Empty; previous = lastPhaseAt = lastStitch = lastVerdict = -1; lastRoot = Vector2.Zero; lastUpdate = 0;
        EbonCeremony.Departure = null; EbonCeremony.DepartureEpoch = -1;
        EbonNoirette.Reset(); EbonThreads.Reset(); EbonScene.Reset();
    }
    public override void ClearWorld() => Reset();
    public override void OnWorldUnload() => Reset();
    public override void Unload() { Reset(); EbonMaterials.Reset(); }

    public override void PostDrawTiles()
    {
        var boss = EbonPackets.Boss;
        if (Main.gameMenu || boss is null || !boss.Fresh) return;
        // Capture in the world pass; UI layers may change screen metrics.
        worldToViewport = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0) * Main.GameViewMatrix.TransformationMatrix;
        projectedFight = boss.State.Fight;
        var batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            float age = RenderAge(boss);
            EbonScene.Draw(batch, boss, EbonNoirette.Resolve(boss, age), age);
        }
        finally { batch.End(); }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        var boss = EbonPackets.Boss;
        if (Main.gameMenu || boss is null || !boss.Fresh || !Local(boss)) return;
        layers.Insert(0, new LegacyGameInterfaceLayer("Convergence: Ebon field", () => Overlay(boss), InterfaceScaleType.None));
    }

    private bool Overlay(EbonBoss boss)
    {
        var batch = Main.spriteBatch; var s = boss.State; var f = s.Field; float age = RenderAge(boss);
        var v = Main.instance.GraphicsDevice.Viewport;
        if (projectedFight != s.Fight) return true;
        Vector2 p0 = Vector2.Transform(new(f.Left, f.Top), worldToViewport), p1 = Vector2.Transform(new(f.Right, f.Top), worldToViewport);
        Vector2 p2 = Vector2.Transform(new(f.Left, f.Bottom), worldToViewport), p3 = Vector2.Transform(new(f.Right, f.Bottom), worldToViewport);
        Vector2 a = Vector2.Min(Vector2.Min(p0, p1), Vector2.Min(p2, p3)), b = Vector2.Max(Vector2.Max(p0, p1), Vector2.Max(p2, p3));
        int l = Math.Clamp((int)a.X, 0, v.Width), r = Math.Clamp((int)b.X, 0, v.Width), t = Math.Clamp((int)a.Y, 0, v.Height), bottom = Math.Clamp((int)b.Y, 0, v.Height);
        if (s.Contains(Main.myPlayer) && !Main.LocalPlayer.dead)
        {
            Fill(new(0, 0, v.Width, t), Color.Black); Fill(new(0, bottom, v.Width, v.Height - bottom), Color.Black);
            Fill(new(0, t, l, Math.Max(0, bottom - t)), Color.Black); Fill(new(r, t, v.Width - r, Math.Max(0, bottom - t)), Color.Black);
            var trim = new Color(196, 176, 150);
            if (a.X >= 0) Fill(new(l, t, 2, Math.Max(0, bottom - t)), trim);
            if (b.X <= v.Width) Fill(new(Math.Max(0, r - 2), t, 2, Math.Max(0, bottom - t)), trim);
            if (a.Y >= 0) Fill(new(l, t, Math.Max(0, r - l), 2), trim);
        }
        bool opening = s.Stage == EbonStage.Countdown, ending = s.EndAt >= 0, deploy = s.Stage == EbonStage.Deployment, turning = s.Transition(age);
        if (opening || ending || deploy || turning)
        {
            float clock = ending ? age - s.EndAt : turning ? age - s.PhaseAt : opening ? age - s.MusicStart : age;
            float duration = ending ? (s.Stage == EbonStage.Victory ? EbonRules.VictoryEnding : EbonRules.Ending)
                : turning ? EbonRules.Lead(s.Phase) : opening ? EbonRules.Intro : EbonRules.Deploy;
            float opacity = EbonVisualsMath.Ease(clock / 30) * EbonVisualsMath.Ease((duration - clock) / 45) * (turning ? .7f : 1);
            Fill(new(0, 0, v.Width, (int)(v.Height * .1f)), Color.Black * opacity);
            Fill(new(0, (int)(v.Height * .9f), v.Width, (int)(v.Height * .11f)), Color.Black * opacity);
            var ivory = new Color(232, 222, 206);
            if (opening)
            {
                // The raid title over the quiet opening motif; her name as she is woven in.
                float bars = EbonIntro.Bars(s, age);
                float title = EbonVisualsMath.Ease((bars - .25f) / .6f) * (1 - EbonVisualsMath.Ease((bars - 3.2f) / .5f));
                Utils.DrawBorderString(batch, "WALTZ OF THE EBON MANOR", new(v.Width * .5f, v.Height * .84f), ivory * title, .92f, .5f);
                float name = EbonVisualsMath.Ease((bars - 9.6f) / .5f) * (1 - EbonVisualsMath.Ease((bars - 12.2f) / .5f));
                if (name > .01f)
                    Utils.DrawBorderString(batch, Lang.GetNPCNameValue(ModContent.NPCType<EbonBoss>()), new(v.Width * .5f, v.Height * .84f),
                        new Color(236, 206, 210) * name, .8f, .5f);
            }
            if (turning)
            {
                float title = EbonVisualsMath.Ease((clock - 20) / 30) * EbonVisualsMath.Ease((duration - 30 - clock) / 30);
                Utils.DrawBorderString(batch, s.Phase == EbonPhase.Finale ? "FINALE" : "ACT II", new(v.Width * .5f, v.Height * .84f), ivory * title, .86f, .5f);
            }
            if (ending && s.Stage == EbonStage.Victory)
            {
                float title = EbonVisualsMath.Ease((clock - 150) / 40) * EbonVisualsMath.Ease((duration - 20 - clock) / 50);
                Utils.DrawBorderString(batch, "THE CURTAIN FALLS", new(v.Width * .5f, v.Height * .84f), ivory * title, .86f, .5f);
            }
            if (!turning) return false;
        }
        if (s.Stage != EbonStage.Ready) return true;
        int count = 0; foreach (var member in s.Members) if (member.Ready) count++;
        foreach (var m in s.Members)
        {
            var player = Main.player[m.Slot]; if (!player.active) continue;
            Vector2 pos = Vector2.Transform(player.Top - new Vector2(0, 24), worldToViewport);
            if (m.Ready) Utils.DrawBorderString(batch, "Ready!", pos, new Color(236, 214, 196), .70f, .5f);
            if (m.Slot != Main.myPlayer) continue;
            // Physical-pixel pill (the Doll/Azure contract): no UI-scale inheritance.
            var button = new Rectangle(v.Width / 2 - 100, 64, 200, 36);
            bool hover = button.Contains(Main.mouseX, Main.mouseY);
            var accent = new Color(226, 196, 180);
            Fill(button, hover ? new Color(52, 34, 44) : new Color(22, 16, 22));
            Fill(new(button.X + 12, button.Bottom - 1, (button.Width - 24) * count / s.Members.Length, 1), accent);
            Fill(new(button.X + 14, button.Y + 13, 6, 6), accent * (m.Ready ? 1 : .25f));
            Utils.DrawBorderString(batch, "READY", new(button.X + 30, button.Y + 8), accent, .7f);
            Utils.DrawBorderString(batch, $"{count}/{s.Members.Length}", new(button.Right - 13, button.Y + 8), Color.Silver, .7f, 1);
            if (hover) { Main.LocalPlayer.mouseInterface = true; if (Main.mouseLeft && Main.mouseLeftRelease) { Main.mouseLeftRelease = false; EbonPackets.Ready(!m.Ready); } }
        }
        return true;
        void Fill(Rectangle rect, Color c) => batch.Draw(TextureAssets.MagicPixel.Value, rect, new Rectangle(0, 0, 1, 1), c);
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class EbonActorVisuals : GlobalNPC
{
    public override bool AppliesToEntity(NPC n, bool lateInstantiation) => n.ModNPC is EbonBoss;
    public override bool PreDraw(NPC npc, SpriteBatch batch, Vector2 screen, Color drawColor)
    {
        if (npc.ModNPC is not EbonBoss boss || boss.State.Fight == Guid.Empty) return false;
        float age = EbonVisuals.RenderAge(boss);
        EbonNoirette.Draw(batch, boss, EbonNoirette.Resolve(boss, age), age);
        return false;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class EbonProjectileVisuals : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile p, bool lateInstantiation) => p.ModProjectile is EbonAttack;
    public override bool PreDraw(Projectile p, ref Color lightColor) => false;
}
