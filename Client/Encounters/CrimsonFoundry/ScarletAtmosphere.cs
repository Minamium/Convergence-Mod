#nullable enable
using System;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Luminance owns render-target allocation/disposal and per-particle updates.
public abstract class ScarletResidue : MetaballType
{
    internal Guid Fight;
    internal int Epoch;
    protected abstract int Species { get; }
    private static readonly Func<Texture2D>[] layers = { () => MiscTexturesRegistry.TurbulentNoise.Value };
    public override Func<Texture2D>[] LayerTextures => layers;
    public override string MetaballAtlasTextureToUse => string.Empty;
    public override Color EdgeColor => ScarletMaterials.Palette(Species);
    public override bool DrawnManually => true;
    public override bool ShouldRender => ActiveParticleCount > 0 && !CrimsonVisuals.Reduced
        && ScarletArticulation.Participant(CrimsonPackets.Boss) && CrimsonPackets.Boss!.State.Fight == Fight
        && CrimsonPackets.Boss.State.PhaseStart == Epoch;
    public override void DrawInstances()
    {
        var texture = MiscTexturesRegistry.BloomCircleSmall.Value;
        foreach (var p in Particles)
            Main.spriteBatch.Draw(texture, p.Center - Main.screenPosition, null, Color.White, 0,
                texture.Size() * .5f, p.Size / texture.Width, SpriteEffects.None, 0);
    }
    public override bool ShouldKillParticle(MetaballInstance p) => p.Size < 2 || p.ExtraInfo[0] >= p.ExtraInfo[1];
    public override void UpdateParticle(MetaballInstance p)
    {
        // Parallel callback: mutate only the particle provided by Luminance.
        p.ExtraInfo[0]++; p.Velocity *= .965f;
        p.Velocity.Y += Species == 0 ? -.045f : .035f;
        p.Size *= Species == 0 ? .965f : .976f;
    }
    public override void PrepareShaderForTarget(int layerIndex)
    {
        var shader = ShaderManager.GetShader("Convergence.ScarletResidue");
        shader.TrySetParameter("clock", (CrimsonPackets.Boss?.VisualAge ?? 0) / 60f);
        shader.TrySetParameter("species", (float)Species);
        shader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
        shader.TrySetParameter("worldOffset", Main.screenPosition);
        shader.SetTexture(MiscTexturesRegistry.TurbulentNoise.Value, 1, SamplerState.LinearWrap);
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 2, SamplerState.LinearWrap);
        shader.Apply();
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class ScarletCinderResidue : ScarletResidue { protected override int Species => 0; }
[Autoload(Side = ModSide.Client)]
public sealed class ScarletInkResidue : ScarletResidue { protected override int Species => 2; }

[Autoload(Side = ModSide.Client)]
internal sealed class ScarletAtmosphere : ModSystem
{
    private ScarletCinderResidue? cinders;
    private ScarletInkResidue? ink;
    private Guid fight;
    private int epoch = -1;
    private float impactAt = -100;
    internal static float Impulse(float age) => MathF.Exp(-Math.Max(0, age - ModContent.GetInstance<ScarletAtmosphere>().impactAt) / 10);
    public override void PostUpdateEverything()
    {
        var boss = CrimsonPackets.Boss;
        if (!ScarletArticulation.Participant(boss)) { Reset(); return; }
        if (fight != boss!.State.Fight || epoch != boss.State.PhaseStart)
        { Reset(); fight = boss.State.Fight; epoch = boss.State.PhaseStart; }
        if (CrimsonVisuals.Reduced) { cinders?.ClearInstances(); ink?.ClearInstances(); }
    }
    internal static void Emit(in CrimsonGesturePlan plan, Vector2 center)
    {
        if (Main.dedServ || CrimsonVisuals.Reduced || !ScarletArticulation.Participant(CrimsonPackets.Boss)) return;
        var self = ModContent.GetInstance<ScarletAtmosphere>();
        self.impactAt = CrimsonPackets.Boss!.VisualAge;
        if (plan.Source is not (0 or 2)) return;
        ScarletResidue particles = plan.Source == 0
            ? self.cinders ??= ModContent.GetInstance<ScarletCinderResidue>()
            : self.ink ??= ModContent.GetInstance<ScarletInkResidue>();
        if (particles.Fight != plan.Fight || particles.Epoch != plan.Epoch)
        { particles.ClearInstances(); particles.Fight = plan.Fight; particles.Epoch = plan.Epoch; }
        Span<CrimsonStroke> strokes = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        int count = CrimsonTechniqueGeometry.Write(plan, plan.End - .01f, strokes);
        int budget = Math.Min(plan.Technique == CrimsonTechnique.CrownRain ? 6 : 10, 96 - particles.ActiveParticleCount);
        for (int i = 0; i < budget; i++)
        {
            float angle = plan.Phrase * .71f + plan.Pulse + i * 2.399963f;
            Vector2 at = count > 0 ? CrimsonGestureVisuals.V(strokes[(i * 7) % count].B) : center;
            Vector2 velocity = angle.ToRotationVector2() * (1.4f + i % 3);
            particles.CreateParticle(at + velocity * 3, velocity,
                plan.Source == 0 ? 27 + i % 3 * 9 : 21 + i % 4 * 7, extraInfo1: 45 + i % 3 * 7);
        }
    }
    internal static void Draw(SpriteBatch batch)
    {
        var self = ModContent.GetInstance<ScarletAtmosphere>();
        if (CrimsonVisuals.Reduced) return;
        using var scope = new ScarletGraphicsScope(batch);
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            if (self.cinders?.ShouldRender == true) self.cinders.RenderLayerWithShader();
            if (self.ink?.ShouldRender == true) self.ink.RenderLayerWithShader();
        }
        finally { batch.End(); }
    }
    private void Reset() { cinders?.ClearInstances(); ink?.ClearInstances(); fight = Guid.Empty; epoch = -1; impactAt = -100; }
    public override void OnWorldUnload() => Reset();
    public override void ClearWorld() => Reset();
    public override void Unload() { Reset(); cinders = null; ink = null; }
}
