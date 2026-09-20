#nullable enable
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.AzureCathedral;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.AzureCathedral;

internal sealed class AzureSky : CustomSky
{
    internal const string Key = "Convergence:AzureCathedral";
    private float fade, age;
    private bool requested;
    public override void Activate(Vector2 position, params object[] args) => requested = true;
    public override void Deactivate(params object[] args) => requested = false;
    public override bool IsActive() => requested || fade > 0;
    public override void Reset() { requested = false; fade = age = 0; }
    public override float GetCloudAlpha() => 1 - fade;
    public override void Update(GameTime gameTime)
    {
        if (Main.gameMenu) { Reset(); return; }
        var girl = AzurePackets.Boss;
        requested = girl is { Fresh: true } && AzureVisuals.Local(girl);
        fade = MathHelper.Clamp(fade + (requested ? .013f : -.04f), 0, 1);
        if (girl is not null) age = AzureVisuals.RenderAge(girl);
    }
    public override void Draw(SpriteBatch batch, float minDepth, float maxDepth)
    {
        if (Main.dedServ || fade <= 0 || minDepth >= 0 || maxDepth < 0) return;
        var art = ModContent.Request<Texture2D>("Convergence/Assets/Textures/AzureCathedral/Cathedral").Value;
        using var scope = new WorldGraphicsScope(batch);
        var shader = AzureMaterials.Begin(true);
        shader.SetTexture(art, 0, SamplerState.LinearClamp); shader.TrySetParameter("clock", age / 60);
        shader.TrySetParameter("signal", new Vector4(fade, 0, AzureVisuals.Reduced ? 1 : 0, 0));
        float scale = System.Math.Max(Main.screenWidth/(float)art.Width,Main.screenHeight/(float)art.Height)*1.04f;
        Vector2 size = art.Size()*scale;
        Vector2 drift = AzureVisuals.Reduced ? Vector2.Zero : new Vector2(System.MathF.Sin(age*.0017f)*10, System.MathF.Cos(age*.0011f)*6);
        AzureMaterials.Quad(shader, new Vector2(Main.screenWidth,Main.screenHeight)*.5f+drift,size,0,new(0,0,1,1),Color.White,"BackdropPass");
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class AzureSkySystem : ModSystem
{
    private AzureSky? sky;
    private bool active;
    public override void Load() => SkyManager.Instance[AzureSky.Key] = sky = new AzureSky();
    public override void PostUpdateEverything()
    {
        bool want = !Main.gameMenu && AzurePackets.Boss is { Fresh: true } girl && AzureVisuals.Local(girl);
        if (want && !active) SkyManager.Instance.Activate(AzureSky.Key, Vector2.Zero);
        if (!want && active) SkyManager.Instance.Deactivate(AzureSky.Key);
        active = want;
    }
    public override void ClearWorld() { sky?.Reset(); active = false; }
    public override void OnWorldUnload() => ClearWorld();
    public override void Unload() { ClearWorld(); sky = null; }
}

[Autoload(Side = ModSide.Client)]
internal sealed class AzureMusicScene : ModSceneEffect
{
    public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
    public override int Music => AzurePackets.Boss is { } girl && girl.State.MusicStart >= 0 && girl.VisualAge >= girl.State.MusicStart
        && girl.State.Stage is AzureStage.Countdown or AzureStage.Performance
        ? MusicLoader.GetMusicSlot(Mod, "Assets/Music/AzureCathedral/WhiteNight") : 0;
    public override bool IsSceneEffectActive(Player player) => !Main.gameMenu && AzurePackets.Boss is { Fresh: true } girl
        && System.Array.Exists(girl.State.Members, m => m.Slot == player.whoAmI);
}
