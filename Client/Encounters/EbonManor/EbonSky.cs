#nullable enable
using System;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.EbonManor;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.EbonManor;

internal sealed class EbonSky : CustomSky
{
    internal const string Key = "Convergence:EbonManor";
    private float fade, age;
    private EbonHallLight light;
    private bool requested;
    internal bool IsSceneRequested => requested;
    public override void Activate(Vector2 position, params object[] args) => requested = true;
    public override void Deactivate(params object[] args) => requested = false;
    public override bool IsActive() => requested || fade > 0;
    public override void Reset() { requested = false; fade = age = 0; light = default; }
    public override float GetCloudAlpha() => 1 - fade;
    public override void Update(GameTime gameTime)
    {
        if (Main.gameMenu) { Reset(); return; }
        var boss = EbonPackets.Boss;
        bool owned = boss is { Fresh: true } && EbonVisuals.Local(boss) && boss.State.MusicStart >= 0 && boss.VisualAge >= boss.State.MusicStart;
        if (!owned) requested = false;
        if (owned) { age = EbonVisuals.RenderAge(boss!); light = EbonHall.Light(boss!.State, age); }
        float target = requested && owned ? light.Fade : 0;
        fade = MathHelper.Clamp(fade + MathHelper.Clamp(target - fade, -.03f, .02f), 0, 1);
    }
    public override void Draw(SpriteBatch batch, float minDepth, float maxDepth)
    {
        if (Main.dedServ || fade <= 0 || minDepth >= 0 || maxDepth < 0) return;
        var hall = EbonMaterials.Texture("ManorHall");
        var final = EbonMaterials.Texture("ManorHallFinal");
        var frame = EbonMaterials.Texture("ManorFrame");
        using var scope = new WorldGraphicsScope(batch);
        var shader = EbonMaterials.Manor(age, EbonMaterials.Screen);
        bool reduced = EbonVisuals.Reduced;
        Vector2 screen = new(Main.screenWidth, Main.screenHeight);
        // Bounded parallax against the hall's own field, not world time.
        Vector2 look = Vector2.Zero;
        if (EbonPackets.Boss is { } boss)
        {
            var f = boss.State.Field;
            Vector2 center = Main.screenPosition + screen * .5f;
            look = new Vector2((center.X - f.CenterX) / Math.Max(1, f.Right - f.Left), (center.Y - f.CenterY) / Math.Max(1, f.Bottom - f.Top));
            look = Vector2.Clamp(look, new(-.6f), new(.6f));
        }
        Vector4 Region(float overscan, float depth)
        {
            float scale = Math.Max(screen.X / hall.Width, screen.Y / hall.Height) * overscan;
            Vector2 size = new(screen.X / (hall.Width * scale), screen.Y / (hall.Height * scale));
            Vector2 spare = Vector2.One - size;
            Vector2 at = spare * .5f + look * spare * .5f * depth;
            if (!reduced) at += new Vector2(MathF.Sin(age * .0017f), MathF.Cos(age * .0011f)) * spare * .04f;
            at = Vector2.Clamp(at, Vector2.Zero, spare);
            return new(at.X, at.Y, size.X, size.Y);
        }
        shader.SetTexture(hall, 0, SamplerState.LinearClamp);
        shader.SetTexture(final, 3, SamplerState.LinearClamp);
        shader.TrySetParameter("region", Region(1.08f, .5f));
        shader.TrySetParameter("signal", new Vector4(fade, light.Tear, reduced ? 1 : 0, light.Tension));
        shader.TrySetParameter("shape", new Vector4(light.Candles, light.Pulse, light.Exposure, reduced ? light.Flash * .5f : light.Flash));
        EbonMaterials.Quad(shader, "BackdropPass", screen * .5f, screen, 0, false);
        shader.SetTexture(frame, 0, SamplerState.LinearClamp);
        shader.TrySetParameter("region", Region(1.04f, 1));
        shader.TrySetParameter("signal", new Vector4(fade, 0, reduced ? 1 : 0, 0));
        shader.TrySetParameter("shape", new Vector4(0, 0, Math.Min(1, light.Exposure), 0));
        EbonMaterials.Quad(shader, "FramePass", screen * .5f, screen, 0, false);
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class EbonSkySystem : ModSystem
{
    private EbonSky? sky;
    public override void Load() => SkyManager.Instance[EbonSky.Key] = sky = new EbonSky();
    public override void PostUpdateEverything()
    {
        bool want = !Main.gameMenu && EbonPackets.Boss is { Fresh: true } boss && EbonVisuals.Local(boss)
            && boss.State.MusicStart >= 0 && boss.VisualAge >= boss.State.MusicStart;
        if (Synchronize(want) && want && EbonPackets.Boss is { } owner)
            EbonPackets.Log($"event=SkyActivated fight={owner.State.Fight} age={(int)owner.VisualAge}");
    }
    private bool Synchronize(bool want)
    {
        // Native Reset/DeactivateAll changes the CustomSky, not a cached flag here.
        if (sky is null || sky.IsSceneRequested == want) return false;
        if (want) SkyManager.Instance.Activate(EbonSky.Key, Vector2.Zero);
        else SkyManager.Instance.Deactivate(EbonSky.Key);
        return true;
    }
    public override void ClearWorld() => sky?.Reset();
    public override void OnWorldUnload() => ClearWorld();
    public override void Unload() { ClearWorld(); sky = null; }
}

// One OGG per act (bar-exact LOOPSTART/LOOPEND) and the outro for the curtain.
// Only our own tracks' fades are shaped; the player's volume setting is never touched.
[Autoload(Side = ModSide.Client)]
internal sealed class EbonMusicScene : ModSceneEffect
{
    public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
    private static string Cue(EbonPhase phase) => phase switch { EbonPhase.ActTwo => "ActTwo", EbonPhase.Finale => "Finale", _ => "ActOne" };
    internal static string Cue(in EbonState s) => s.Stage == EbonStage.Victory ? "Curtain" : Cue(s.Phase);
    private int Slot(string cue) => MusicLoader.GetMusicSlot(Mod, "Assets/Music/EbonManor/" + cue);
    public override int Music => EbonPackets.Boss is { } boss && boss.State.MusicStart >= 0 && boss.VisualAge >= boss.State.MusicStart
        ? Slot(Cue(boss.State)) : 0;
    internal void UpdateFade(EbonBoss boss, float age)
    {
        var s = boss.State;
        if (s.MusicStart < 0) return;
        if (s.Stage == EbonStage.Defeat && s.EndAt >= 0)
        {
            Ceiling(Slot(Cue(s.Phase)), 1 - EbonVisualsMath.Ease((age - s.EndAt) / 150));
            return;
        }
        if (s.Stage == EbonStage.Victory && s.EndAt >= 0)
        {
            Ceiling(Slot(Cue(s.Phase)), 1 - EbonVisualsMath.Ease((age - s.EndAt) / 24));
            if (age - s.EndAt < 90) Floor(Slot("Curtain"), EbonVisualsMath.Ease((age - s.EndAt) / 16));
            return;
        }
        if (s.Phase != EbonPhase.ActOne && s.PhaseAt >= 0 && age - s.PhaseAt < 150)
        {
            // A one-beat handoff: the next act's lead-in bars start at full weight.
            Ceiling(Slot(Cue(s.Phase == EbonPhase.Finale ? EbonPhase.ActTwo : EbonPhase.ActOne)), 1 - EbonVisualsMath.Ease((age - s.PhaseAt) / 26));
            Floor(Slot(Cue(s.Phase)), EbonVisualsMath.Ease((age - s.PhaseAt) / 20));
        }
        else if (s.Phase == EbonPhase.ActOne && age - s.MusicStart < 150) Floor(Slot("ActOne"), EbonVisualsMath.Ease((age - s.MusicStart) / 30));
    }
    private static void Ceiling(int slot, float value)
    { if (slot > 0 && slot < Main.musicFade.Length) Main.musicFade[slot] = Math.Min(Main.musicFade[slot], Math.Clamp(value, 0, 1)); }
    private static void Floor(int slot, float value)
    { if (slot > 0 && slot < Main.musicFade.Length && Main.curMusic == slot) Main.musicFade[slot] = Math.Max(Main.musicFade[slot], Math.Clamp(value, 0, 1)); }
    public override bool IsSceneEffectActive(Player player) => !Main.gameMenu && EbonPackets.Boss is { Fresh: true } boss
        && Array.Exists(boss.State.Members, m => m.Slot == player.whoAmI);
}
