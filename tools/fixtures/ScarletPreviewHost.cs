// Engine shim for the Scarlet rig harness (tools/preview-scarlet-rigs.ps1). The production rig, forecast and
// seal files are linked UNCHANGED; this file replaces only the Terraria / tModLoader / ReLogic / Luminance
// surface they touch, over a hidden FNA D3D11 device, plus two tiny stubs (CrimsonVisuals.Reduced and
// ScarletMaterials.WorldMatrix / Palette, the latter verbatim from ScarletMaterials.cs) and a one-line partial of
// CrimsonRig that calls the production LoadPerformer of the linked CrimsonRig.Performer.cs. Promoted from the
// 2026-10-02 spike that proved the real rigs render offline with zero compile errors.
//
// Self-checks the harness relies on (reported in gates.json, "self"):
//   * every draw that reaches the device through Main.instance.GraphicsDevice runs with the viewport and the
//     scissor equal to the bound render target (FNA resets both on SetRenderTarget; the check proves nothing
//     else changed them);
//   * every name a production file sets through ManagedShader.TrySetParameter is recorded with its width and
//     later compared with the compiled .fxc parameters (a width mismatch fails; a missing name is listed,
//     because the compiler drops unused uniforms and TrySetParameter ignores them exactly like Luminance).
// Draws and vertices are counted per tag for the G12 budget. Offline review only: not a playtest.
#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal static class RigHost
{
    internal static GraphicsDevice Device;
    internal static PreviewDevice Wrapped;
    internal static PreviewAssets Assets;

    // G12 accounting: draws / vertices per tag ("crown", "mantle", "choir", "vespera", "field").
    internal static string Tag = "";
    internal static readonly Dictionary<string, (int Draws, int Vertices)> Counts = new();
    // The same per tag and pass ("crown/ScarletApparitions.SparkPass"): G7 (no Fire-born particles under Reduced) and
    // the G12 vertex breakdown.
    internal static readonly Dictionary<string, (int Draws, int Vertices)> PassCounts = new();
    internal static void ResetCounts() { Counts.Clear(); PassCounts.Clear(); }

    // The last pass a ManagedShader applied (every production draw applies its pass right before drawing).
    internal static string LastShader = "", LastPass = "";

    // When set, a draw whose (shader, pass) is rejected is skipped: body-only masks render just the skinned mesh.
    internal static Func<string, string, bool> PassFilter;

    // When set, the positions of every skinned body mesh draw (AutoloadPass of the apparition / choir shaders) are
    // appended here (G7: Reduced Effects must not move a vertex).
    internal static List<Vector2> MeshCapture;

    // Viewport / scissor self-check.
    internal static int Checked, Violations;
    internal static string FirstViolation;
    internal static void CheckTarget(GraphicsDevice device)
    {
        var targets = device.GetRenderTargets();
        if (targets.Length == 0 || targets[0].RenderTarget is not RenderTarget2D target) return;
        var viewport = device.Viewport; var scissor = device.ScissorRectangle;
        Checked++;
        if (viewport.X != 0 || viewport.Y != 0 || viewport.Width != target.Width || viewport.Height != target.Height
            || scissor.X != 0 || scissor.Y != 0 || scissor.Width != target.Width || scissor.Height != target.Height)
        {
            Violations++;
            FirstViolation ??= $"viewport {viewport.X},{viewport.Y} {viewport.Width}x{viewport.Height} scissor {scissor} target {target.Width}x{target.Height}";
        }
    }

    // Shader parameter audit: (shader stem, name, width) as production set them.
    internal static readonly HashSet<(string Shader, string Name, int Width)> Parameters = new();
    internal static readonly HashSet<(string Shader, string Pass)> Passes = new();
}

// What production code sees as Main.instance.GraphicsDevice: the real device, forwarded, with every
// DrawUserPrimitives counted and checked. WorldGraphicsScope stores it as a GraphicsDevice through the
// implicit conversion, so the scope saves and restores the real device state exactly as in game.
internal sealed class PreviewDevice
{
    internal readonly GraphicsDevice Inner;
    internal PreviewDevice(GraphicsDevice inner) => Inner = inner;
    public static implicit operator GraphicsDevice(PreviewDevice device) => device.Inner;
    public BlendState BlendState { get => Inner.BlendState; set => Inner.BlendState = value; }
    public DepthStencilState DepthStencilState { get => Inner.DepthStencilState; set => Inner.DepthStencilState = value; }
    public RasterizerState RasterizerState { get => Inner.RasterizerState; set => Inner.RasterizerState = value; }
    public Viewport Viewport => Inner.Viewport;
    public Rectangle ScissorRectangle { get => Inner.ScissorRectangle; set => Inner.ScissorRectangle = value; }
    public TextureCollection Textures => Inner.Textures;
    public SamplerStateCollection SamplerStates => Inner.SamplerStates;

    public void DrawUserPrimitives<T>(PrimitiveType type, T[] vertices, int offset, int primitives) where T : struct, IVertexType
    {
        RigHost.CheckTarget(Inner);
        if (RigHost.PassFilter is { } filter && !filter(RigHost.LastShader, RigHost.LastPass)) return;
        int count = type == PrimitiveType.TriangleList ? primitives * 3 : primitives + 2;
        var tally = RigHost.Counts.TryGetValue(RigHost.Tag, out var found) ? found : default;
        RigHost.Counts[RigHost.Tag] = (tally.Draws + 1, tally.Vertices + count);
        string key = RigHost.Tag + "/" + RigHost.LastShader + "." + RigHost.LastPass;
        var passTally = RigHost.PassCounts.TryGetValue(key, out var seen) ? seen : default;
        RigHost.PassCounts[key] = (passTally.Draws + 1, passTally.Vertices + count);
        if (RigHost.MeshCapture is { } capture && RigHost.LastPass == "AutoloadPass"
            && RigHost.LastShader is "ScarletApparitions" or "ScarletChoir" && vertices is VertexPositionColorTexture[] mesh)
            for (int i = offset; i < offset + count; i++) capture.Add(new Vector2(mesh[i].Position.X, mesh[i].Position.Y));
        Inner.DrawUserPrimitives(type, vertices, offset, primitives);
    }
}

namespace ReLogic.Content
{
    public enum AssetRequestMode { AsyncLoad, ImmediateLoad }
    // Always loaded: the rigs request ImmediateLoad, so the AsyncLoad pitfall cannot occur on these paths.
    public sealed class Asset<T> { public T Value; }
}

namespace Terraria
{
    internal static class Main
    {
        public static bool dedServ;
        public static Vector2 screenPosition;
        public static readonly Host instance = new();
        public static readonly Camera GameViewMatrix = new();
        // Terraria's world rasterizer (not gravity-flipped).
        public static RasterizerState Rasterizer => RasterizerState.CullCounterClockwise;
        public sealed class Host { public PreviewDevice GraphicsDevice => RigHost.Wrapped; }
        public sealed class Camera { public Matrix TransformationMatrix = Matrix.Identity; public Vector2 Zoom = Vector2.One; }
    }

    internal static class Utils
    {
        public static Vector2 RotatedBy(this Vector2 v, double radians)
        {
            float c = MathF.Cos((float)radians), s = MathF.Sin((float)radians);
            return new(v.X * c - v.Y * s, v.X * s + v.Y * c);
        }
        public static Vector2 Size(this Texture2D texture) => new(texture.Width, texture.Height);
    }
}

namespace Terraria.ModLoader
{
    internal static class ModContent
    {
        // "Convergence/Assets/Textures/<name>" -> Assets/Textures/<name>.png, premultiplied like tModLoader does.
        public static ReLogic.Content.Asset<T> Request<T>(string path, ReLogic.Content.AssetRequestMode mode = ReLogic.Content.AssetRequestMode.AsyncLoad) where T : class
        {
            const string prefix = "Convergence/Assets/Textures/";
            if (!path.StartsWith(prefix, StringComparison.Ordinal)) throw new ArgumentException("rig harness has no asset " + path);
            return new() { Value = (T)(object)RigHost.Assets.GetTexture(path[prefix.Length..]) };
        }
    }
}

namespace Luminance.Assets
{
    internal static class MiscTexturesRegistry
    {
        private static ReLogic.Content.Asset<Texture2D> A(string name) => new() { Value = RigHost.Assets.GetTexture(name) };
        public static ReLogic.Content.Asset<Texture2D> WavyBlotchNoise => A("Noise/WavyBlotchNoise");
        public static ReLogic.Content.Asset<Texture2D> TurbulentNoise => A("Noise/TurbulentNoise");
        public static ReLogic.Content.Asset<Texture2D> DendriticNoiseZoomedOut => A("Noise/DendriticNoiseZoomedOut");
        public static ReLogic.Content.Asset<Texture2D> BloomCircleSmall => A("Luminance/BloomCircleSmall");
    }
}

namespace Luminance.Core.Graphics
{
    internal static class ShaderManager
    {
        private static readonly Dictionary<string, ManagedShader> shaders = new();
        // "Convergence.ScarletChoir" -> Assets/AutoloadedEffects/Shaders/ScarletChoir.fxc
        public static ManagedShader GetShader(string name)
        {
            const string prefix = "Convergence.";
            if (!name.StartsWith(prefix, StringComparison.Ordinal)) throw new ArgumentException("rig harness has no shader " + name);
            if (!shaders.TryGetValue(name, out var shader))
                shaders[name] = shader = new ManagedShader(name[prefix.Length..], RigHost.Assets.GetEffect(name[prefix.Length..]));
            return shader;
        }
    }

    // Luminance's ManagedShader as the rigs use it: TrySetParameter writes straight into the Effect (an unknown
    // name is ignored), SetTexture binds the device slot directly and Apply applies one pass.
    internal sealed class ManagedShader
    {
        internal readonly string Name;
        internal readonly Effect Effect;
        internal ManagedShader(string name, Effect effect) { Name = name; Effect = effect; }
        public void TrySetParameter(string name, float value) { Note(name, 1); Effect.Parameters[name]?.SetValue(value); }
        public void TrySetParameter(string name, Vector2 value) { Note(name, 2); Effect.Parameters[name]?.SetValue(value); }
        public void TrySetParameter(string name, Vector3 value) { Note(name, 3); Effect.Parameters[name]?.SetValue(value); }
        public void TrySetParameter(string name, Vector4 value) { Note(name, 4); Effect.Parameters[name]?.SetValue(value); }
        public void TrySetParameter(string name, Matrix value) { Note(name, 16); Effect.Parameters[name]?.SetValue(value); }
        public void SetTexture(Texture texture, int slot, SamplerState sampler)
        {
            RigHost.Device.Textures[slot] = texture; RigHost.Device.SamplerStates[slot] = sampler;
        }
        public void Apply(string pass = "AutoloadPass")
        {
            RigHost.Passes.Add((Name, pass));
            Effect.CurrentTechnique.Passes[pass].Apply();
            RigHost.LastShader = Name; RigHost.LastPass = pass;
        }
        private void Note(string name, int width) => RigHost.Parameters.Add((Name, name, width));
    }
}

namespace Convergence.Client.Encounters.CrimsonFoundry
{
    // The harness's part of CrimsonRig: CrimsonRig.Performer.cs (Vespera, the free apparitions, the plan-list Signal)
    // is linked unchanged; the boss half (CrimsonRig.cs, whose Load calls LoadPerformer) is not. This calls the
    // production loader (ModContent.Request through the shim above) and adds nothing else to the class.
    internal static partial class CrimsonRig
    {
        internal static void LoadPreviewPerformer() => LoadPerformer();
    }

    // CrimsonVisuals.cs is a ModSystem; the rigs only read this one static.
    internal static class CrimsonVisuals { internal static bool Reduced; }

    internal static class ScarletMaterials
    {
        // Verbatim from ScarletMaterials.cs (WorldMatrix and Palette); the rest of that file is Luminance trail code.
        internal static Matrix WorldMatrix => Terraria.Main.GameViewMatrix.TransformationMatrix *
            Matrix.CreateOrthographicOffCenter(0, Terraria.Main.instance.GraphicsDevice.Viewport.Width, Terraria.Main.instance.GraphicsDevice.Viewport.Height, 0, -1, 1);
        internal static Color Palette(int source) => source switch
        {
            0 => new(255, 48, 74), 1 => new(245, 78, 116),
            2 => new(222, 55, 137), _ => new(255, 84, 120)
        };
    }
}
