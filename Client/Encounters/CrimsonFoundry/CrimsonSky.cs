using ScarletGraphicsScope = Convergence.Client.Graphics.WorldGraphicsScope;
#nullable enable
using System;
using System.Linq;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

internal sealed class CrimsonSky : CustomSky
{
    internal const string Key = "Convergence:ScarletInvocation";
    internal const string PaintingPath = "Assets/Textures/Backgrounds/ScarletSanctum.png";
    internal bool Available;
    private Guid fight;
    private bool requested;
    private float fade, age, beat;
    private int phase;
    private Vector4 weights = Vector4.UnitX;
    private Asset<Texture2D>? painting;
    internal void Bind(CrimsonBoss? boss)
    {
        if (!Available || !ScarletArticulation.Participant(boss)) { requested = false; return; }
        if (fight != boss!.State.Fight) { Reset(); fight = boss.State.Fight; }
        requested = true; phase = boss.State.Phase; age = CrimsonVisuals.RenderAge(boss);
        beat = boss.State.MusicStart >= 0 ? CrimsonRegistration.Score.Pulse(Math.Max(0, age - boss.State.MusicStart)) : 0;
    }
    public override void Activate(Vector2 position, params object[] args) => requested = Available;
    public override void Deactivate(params object[] args) => requested = false;
    public override bool IsActive() => Available && (requested || fade > 0);
    public override void Reset() { requested = false; fade = age = beat = 0; fight = Guid.Empty; weights = Vector4.UnitX; }
    internal void Unload() { Reset(); painting = null; Available = false; }
    public override float GetCloudAlpha() => 1 - fade * .94f;
    public override void Update(GameTime gameTime)
    {
        if (Main.gameMenu || Main.dedServ) { Reset(); return; }
        fade = Math.Clamp(fade + (requested ? .018f : -.035f), 0, 1);
        Vector4 target = phase switch { 0 => Vector4.UnitX, 1 => Vector4.UnitY, 2 => Vector4.UnitZ, _ => Vector4.UnitW };
        weights = Vector4.Lerp(weights, target, .028f);
    }
    public override void Draw(SpriteBatch batch, float minDepth, float maxDepth)
    {
        if (Main.dedServ || !Available || fade <= 0 || minDepth >= 0 || maxDepth < 0) return;
        painting ??= ModContent.Request<Texture2D>("Convergence/Assets/Textures/Backgrounds/ScarletSanctum", AssetRequestMode.ImmediateLoad);
        var art = painting.Value;
        bool reduced = CrimsonVisuals.Reduced;
        float scale = Math.Max(Main.screenWidth / (float)art.Width, Main.screenHeight / (float)art.Height) * 1.10f;
        Vector2 size = new(Main.screenWidth / (art.Width * scale), Main.screenHeight / (art.Height * scale));
        Vector2 drift = reduced ? Vector2.Zero : new Vector2(MathF.Sin(Main.screenPosition.X * .00035f + age * .0009f) * .012f,
            MathF.Sin(Main.screenPosition.Y * .00040f + age * .0011f) * .008f);
        Vector2 origin = (Vector2.One - size) * .5f + drift;
        using var scope = new ScarletGraphicsScope(batch);
        var shader = ShaderManager.GetShader("Convergence.ScarletBackdrop");
        shader.TrySetParameter("uWorldViewProjection", Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1));
        shader.TrySetParameter("crop", new Vector4(origin, size.X, size.Y));
        shader.TrySetParameter("phaseWeights", weights);
        shader.TrySetParameter("signal", new Vector4(fade, reduced ? 0 : beat, reduced ? 0 : ScarletAtmosphere.Impulse(age), reduced ? 1 : 0));
        shader.TrySetParameter("clock", age / 60);
        shader.SetTexture(art, 0, SamplerState.LinearClamp);
        shader.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 1, SamplerState.LinearWrap);
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 2, SamplerState.LinearWrap);
        ScarletMaterials.DrawQuad(shader, Vector2.Zero, new(Main.screenWidth, Main.screenHeight));
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonSkySystem : ModSystem
{
    private CrimsonSky? sky;
    private bool activated;
    public override void Load()
    {
        if (Main.dedServ) return;
        // tML packs PNGs as .rawimg. Accept the compiled asset as well as a
        // loose PNG, or an imported background still remains invisible.
        bool available = Mod.GetFileNames().Any(path => path == CrimsonSky.PaintingPath
            || path == "Assets/Textures/Backgrounds/ScarletSanctum.rawimg");
        SkyManager.Instance[CrimsonSky.Key] = sky = new CrimsonSky { Available = available };
        if (!available) Mod.Logger.Warn("scarlet.background_asset_missing: approved ScarletSanctum.png has not been imported; retain the normal world sky, never substitute another raid's painting.");
    }
    public override void PostUpdateEverything()
    {
        if (sky is null) return;
        var boss = Main.gameMenu ? null : CrimsonPackets.Boss;
        bool want = sky.Available && ScarletArticulation.Participant(boss);
        sky.Bind(boss);
        if (want && !activated) SkyManager.Instance.Activate(CrimsonSky.Key, Vector2.Zero);
        else if (!want && activated) SkyManager.Instance.Deactivate(CrimsonSky.Key);
        activated = want;
    }
    public override void OnWorldUnload() { sky?.Reset(); activated = false; }
    public override void ClearWorld() { sky?.Reset(); activated = false; }
    public override void Unload() { sky?.Unload(); sky = null; activated = false; }
}
