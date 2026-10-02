#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

internal enum ScarletOverlayPhase : byte { Idle, Warning, Active, Residue }

// Straight (non-premultiplied) RGBA. Defaults: warning = thin white outline,
// active = red fill, residue = dark red fill. The safe zone is simply not drawn.
internal readonly record struct ScarletOverlayStyle(Vector4 Warning, Vector4 Active, Vector4 Residue, float WarningLinePixels)
{
    internal static ScarletOverlayStyle Default => new(
        new(1f, 1f, 1f, .92f), new(.93f, .10f, .16f, .55f), new(.40f, .05f, .09f, .50f), 1.6f);
}

// REFERENCE drawing of the AUTHORITATIVE hit shapes (debug only, never part of the
// shipped look). It draws exactly the CrimsonStroke capsules that
// CrimsonTechniqueGeometry.Write produces for collision, as flat translucent
// polygons with true semicircular ends, so a new material can be compared against
// the real danger area.
//
//   warning : thin white outline of the conservative forecast footprint (Born <= age < Fire)
//   active  : red fill of the live capsules                              (Fire <= age < End)
//   residue : dark red fill of everything that was lethal during the live window,
//             shown for ResidueTicks after End (harmless; the swept union is an
//             overlay convention, the Mod keeps no such collider)
//   safe    : never drawn
//
// Overlapping capsules are drawn as ONE union (stencil), so joints and crossings do
// not darken. That needs a Depth24Stencil8 render target; Draw throws otherwise.
// Plans must already be the effective ones (an Aimed plan carries its locked aim
// in Target, see CrimsonGesture.EffectivePlan).
internal sealed class ScarletGeometryOverlay : IDisposable
{
    private const int BatchVertices = 30000; // multiple of three

    private static readonly DepthStencilState MarkInside = new()
    {
        DepthBufferEnable = false, StencilEnable = true, ReferenceStencil = 1,
        StencilFunction = CompareFunction.Always, StencilPass = StencilOperation.Replace
    };
    private static readonly DepthStencilState FirstCoverOnly = new()
    {
        DepthBufferEnable = false, StencilEnable = true, ReferenceStencil = 1,
        StencilFunction = CompareFunction.NotEqual, StencilPass = StencilOperation.Replace
    };
    private static readonly BlendState NoColor = new() { ColorWriteChannels = ColorWriteChannels.None };

    private readonly GraphicsDevice device;
    private readonly BasicEffect effect;
    private readonly List<CrimsonStroke> warning = new(), active = new(), residue = new(), scratch = new();
    private VertexPositionColor[] vertices = new VertexPositionColor[BatchVertices];
    private int vertexCount;

    internal ScarletGeometryOverlay(GraphicsDevice device)
    {
        this.device = device;
        effect = new BasicEffect(device) { VertexColorEnabled = true, LightingEnabled = false, TextureEnabled = false };
    }

    public void Dispose() => effect.Dispose();

    internal static int ResidueTicks(in CrimsonGesturePlan plan)
        => plan.IsRift ? CrimsonSpatialCuts.ResidueTicks
            : plan.IsSignature ? CrimsonSignatureMoves.ResidueTicks(plan.Technique) : CrimsonRhythm.ResidueTicks;

    internal static ScarletOverlayPhase Classify(in CrimsonGesturePlan plan, float age)
    {
        if (age < plan.Born) return ScarletOverlayPhase.Idle;
        if (age < plan.Fire) return ScarletOverlayPhase.Warning;
        if (age < plan.End) return ScarletOverlayPhase.Active;
        return age < plan.End + ResidueTicks(plan) ? ScarletOverlayPhase.Residue : ScarletOverlayPhase.Idle;
    }

    // The capsules of one plan at one time, with the phase they belong to.
    internal static ScarletOverlayPhase Collect(in CrimsonGesturePlan plan, float age, List<CrimsonStroke> into)
    {
        var phase = Classify(plan, age);
        if (phase == ScarletOverlayPhase.Idle) return phase;
        Span<CrimsonStroke> buffer = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        if (phase == ScarletOverlayPhase.Warning)
            Append(buffer, CrimsonTechniqueGeometry.Write(plan, age, buffer, true), into);
        else if (phase == ScarletOverlayPhase.Active)
            Append(buffer, CrimsonTechniqueGeometry.Write(plan, age, buffer, false), into);
        else
        {
            // Every live tick, so fast carriers (ClusterVolley) leave a continuous swept band, not beads.
            for (int tick = plan.Fire; tick < plan.End; tick++)
                Append(buffer, CrimsonTechniqueGeometry.Write(plan, tick, buffer, false), into);
        }
        return phase;
    }

    private static void Append(Span<CrimsonStroke> buffer, int count, List<CrimsonStroke> into)
    {
        for (int i = 0; i < count; i++)
        {
            into.Add(buffer[i]);
        }
    }

    internal static bool StencilAvailable(GraphicsDevice device)
    {
        var targets = device.GetRenderTargets();
        DepthFormat format = targets.Length > 0 && targets[0].RenderTarget is RenderTarget2D target
            ? target.DepthStencilFormat : device.PresentationParameters.DepthStencilFormat;
        return format == DepthFormat.Depth24Stencil8;
    }

    internal void Draw(in ScarletView view, IReadOnlyList<CrimsonGesturePlan> plans)
        => Draw(view, plans, ScarletOverlayStyle.Default);

    internal void Draw(in ScarletView view, IReadOnlyList<CrimsonGesturePlan> plans, in ScarletOverlayStyle style)
    {
        if (!StencilAvailable(view.Device))
            throw new InvalidOperationException("scarlet.overlay_needs_stencil: bind a Depth24Stencil8 render target.");
        warning.Clear(); active.Clear(); residue.Clear();
        for (int i = 0; i < plans.Count; i++)
        {
            scratch.Clear();
            var phase = Collect(plans[i], view.Clock, scratch);
            if (phase == ScarletOverlayPhase.Warning) warning.AddRange(scratch);
            else if (phase == ScarletOverlayPhase.Active) active.AddRange(scratch);
            else if (phase == ScarletOverlayPhase.Residue) residue.AddRange(scratch);
        }
        if (warning.Count + active.Count + residue.Count == 0) return;

        var blend = device.BlendState;
        var depth = device.DepthStencilState;
        var raster = device.RasterizerState;
        try
        {
            effect.World = Matrix.Identity;
            effect.View = Matrix.CreateTranslation(-view.ScreenPosition.X, -view.ScreenPosition.Y, 0) * view.GameView;
            effect.Projection = view.Projection;
            device.RasterizerState = RasterizerState.CullNone;
            if (residue.Count > 0) Fill(view, residue, style.Residue);
            if (active.Count > 0) Fill(view, active, style.Active);
            if (warning.Count > 0) Outline(view, warning, style.Warning, style.WarningLinePixels);
        }
        finally
        {
            device.BlendState = blend;
            device.DepthStencilState = depth;
            device.RasterizerState = raster;
        }
    }

    private void Fill(in ScarletView view, List<CrimsonStroke> strokes, Vector4 color)
    {
        device.Clear(ClearOptions.Stencil, Color.Transparent, 1f, 0);
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = FirstCoverOnly;
        Emit(strokes, 0, view.Zoom, Premultiply(color));
        Submit();
    }

    // Outline = (capsules grown by the line width) minus (the capsules themselves).
    private void Outline(in ScarletView view, List<CrimsonStroke> strokes, Vector4 color, float linePixels)
    {
        device.Clear(ClearOptions.Stencil, Color.Transparent, 1f, 0);
        device.BlendState = NoColor;
        device.DepthStencilState = MarkInside;
        Emit(strokes, 0, view.Zoom, Color.Transparent);
        Submit();
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = FirstCoverOnly;
        Emit(strokes, linePixels / Math.Max(.01f, view.Zoom), view.Zoom, Premultiply(color));
        Submit();
    }

    private static Color Premultiply(Vector4 c) => new(new Vector4(c.X * c.W, c.Y * c.W, c.Z * c.W, c.W));

    private void Emit(List<CrimsonStroke> strokes, float grow, float zoom, Color color)
    {
        for (int i = 0; i < strokes.Count; i++)
        {
            AddCapsule(strokes[i], grow, zoom, color);
        }
    }

    private void Submit()
    {
        if (vertexCount == 0) return;
        effect.CurrentTechnique.Passes[0].Apply();
        device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertexCount / 3);
        vertexCount = 0;
    }

    private void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        vertices[vertexCount++] = new(new Vector3(a, 0), color);
        vertices[vertexCount++] = new(new Vector3(b, 0), color);
        vertices[vertexCount++] = new(new Vector3(c, 0), color);
    }

    // Fan of triangles on an arc. The first and last rim points are passed in exactly (not recomputed
    // with sin/cos) so they coincide bit for bit with the neighbouring quad: no pixel cracks.
    private void Fan(Vector2 center, Vector2 axis, Vector2 normal, float radius, float from, float to, int segments,
        Vector2 first, Vector2 last, Color color)
    {
        Vector2 previous = first;
        for (int i = 1; i <= segments; i++)
        {
            float angle = from + (to - from) * i / segments;
            Vector2 next = i == segments ? last : center + (axis * MathF.Cos(angle) + normal * MathF.Sin(angle)) * radius;
            Triangle(center, previous, next, color);
            previous = next;
        }
    }

    // A rectangle plus two real half discs: the exact shape CrimsonTechniqueGeometry.Intersects tests.
    private void AddCapsule(in CrimsonStroke stroke, float grow, float zoom, Color color)
    {
        float radius = stroke.Radius + grow;
        if (!(radius > 0)) return;
        Vector2 a = new(stroke.A.X, stroke.A.Y), b = new(stroke.B.X, stroke.B.Y);
        Vector2 delta = b - a;
        float length = delta.Length();
        int half = Math.Clamp((int)MathF.Ceiling(MathF.Sqrt(radius * zoom) * 2.4f), 4, 40);
        if (vertexCount + 12 + 6 * half > BatchVertices) Submit();
        if (length < .001f)
        {
            Vector2 start = a + Vector2.UnitX * radius;
            Fan(a, Vector2.UnitX, Vector2.UnitY, radius, 0, MathF.Tau, half * 2, start, start, color);
            return;
        }
        Vector2 axis = delta / length, normal = new(-axis.Y, axis.X);
        // The body is cut through both centres so every seam with a cap shares its vertices exactly
        // (a quad's long edge would make a T-junction at the centre and can drop single pixels).
        Triangle(a, a + normal * radius, b + normal * radius, color);
        Triangle(a, b + normal * radius, b, color);
        Triangle(a, b, b - normal * radius, color);
        Triangle(a, b - normal * radius, a - normal * radius, color);
        Fan(b, axis, normal, radius, -MathF.PI * .5f, MathF.PI * .5f, half, b - normal * radius, b + normal * radius, color);
        Fan(a, axis, normal, radius, MathF.PI * .5f, MathF.PI * 1.5f, half, a + normal * radius, a - normal * radius, color);
    }
}
