using System;
using Convergence.Client.Graphics;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

// Luminance owns the reusable render target and connects neighboring smoke
// particles. Simulation is explicitly tick-owned: its target callback is a draw
// callback, so moving particles there would run faster at higher render FPS.
[Autoload(Side = ModSide.Client)]
public sealed class GhostSamuraiMist : MetaballType
{
    internal const int Budget = 64;
    internal Guid Fight;
    internal float Clock;
    private static readonly Func<Texture2D>[] layers = { () => MiscTexturesRegistry.TurbulentNoise.Value };
    public override Func<Texture2D>[] LayerTextures => layers;
    public override string MetaballAtlasTextureToUse => string.Empty;
    public override Color EdgeColor => new(159, 125, 240);
    public override bool DrawnManually => true;
    public override bool ShouldRender => !Main.dedServ && !Main.gameMenu && Fight != Guid.Empty && ActiveParticleCount > 0 && !GhostSamuraiRigArt.Reduced;
    public override void UpdateParticle(MetaballInstance particle) => particle.Velocity = Vector2.Zero;
    public override bool ShouldKillParticle(MetaballInstance p) => p.ExtraInfo[0] >= p.ExtraInfo[1] || p.Size < 2;
    internal void Tick(float clock)
    {
        Clock = clock;
        if (GhostSamuraiRigArt.Reduced) { ClearInstances(); return; }
        foreach (var p in Particles) StepParticle(p);
        Particles.RemoveAll(ShouldKillParticle);
    }
    internal static void StepParticle(MetaballInstance p)
    {
        p.ExtraInfo[0]++; p.ExtraInfo[2] *= .962f; p.ExtraInfo[3] = p.ExtraInfo[3] * .962f - .028f;
        p.Center += new Vector2(p.ExtraInfo[2], p.ExtraInfo[3]); p.Size *= .977f;
    }
    internal void Emit(Guid fight, Vector2 at, Vector2 velocity, float size, float lifetime = 40)
    {
        if (Main.dedServ || GhostSamuraiRigArt.Reduced) return;
        if (Fight != fight) { ClearInstances(); Fight = fight; }
        if (ActiveParticleCount >= Budget) return;
        CreateParticle(at, Vector2.Zero, size, extraInfo1: lifetime, extraInfo2: velocity.X, extraInfo3: velocity.Y);
    }
    internal void ClearOwned() { ClearInstances(); Fight = Guid.Empty; Clock = 0; }
    public override void DrawInstances()
    {
        var texture = MiscTexturesRegistry.BloomCircleSmall.Value;
        foreach (var p in Particles)
            Main.spriteBatch.Draw(texture, p.Center - Main.screenPosition, null, Color.White, 0,
                texture.Size() * .5f, p.Size / texture.Width, SpriteEffects.None, 0);
    }
    public override void PrepareShaderForTarget(int layerIndex)
    {
        var shader = ShaderManager.GetShader("Convergence.SamuraiMist");
        shader.TrySetParameter("clock", (Clock + GhostSamuraiPresentation.Fraction) / 60);
        shader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2()); shader.TrySetParameter("worldOffset", Main.screenPosition);
        shader.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 1, SamplerState.LinearWrap); shader.Apply();
    }
    internal void Draw(SpriteBatch batch)
    {
        if (!ShouldRender) return;
        using var scope = new WorldGraphicsScope(batch);
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try { RenderLayerWithShader(); } finally { batch.End(); }
    }
}
