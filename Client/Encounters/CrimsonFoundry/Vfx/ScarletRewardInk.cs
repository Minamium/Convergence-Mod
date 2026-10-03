#nullable enable
using System;
using System.Runtime.InteropServices;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.CrimsonFoundry.Vfx;

// Which ScarletInk path pass draws a path (REWARDS.md#black-blood-material).
//   Live    = PathLivePass: the live black-blood river (time = ticks since that point ignited)
//   Dormant = PathDormantPass: the build look, a dried scar with a faint warm lip (time unused)
//   Residue = PathResiduePass: the harmless scar after a release (time = remaining fade 1..0)
internal enum ScarletInkLook : byte { Live, Dormant, Residue }

// One path's style. Local marks the local player's own ink (full opacity, last to be dropped); other players' dormant
// ink draws at 0.5 and their live ink and residue at 0.85. Owner is the player slot (droplet budgets); Fire adds flame
// tongues on live lips (the censer only); Warmth (0..1) is a full build's steady lip heat on dormant ink.
// Live ink that dries into a scar closes like AutoloadPass: Window is each point's live window in its own time (ticks
// since it ignited), Remaining the live ticks left for the whole path (a path whose points all stop together). Over the
// last CloseTicks the river crossfades into the scar PathResiduePass starts from, so it never snaps or leaves a burning
// cap behind a moving tail. Neither changes collision (as with the Raid's close envelope). Both unset: no close.
internal readonly record struct ScarletInkStyle(ScarletInkLook Look, bool Local, float Seed, float Opacity = 1,
    bool Fire = false, float Warmth = 0, int Owner = -1, float Window = 0, float Remaining = float.PositiveInfinity)
{
    internal ScarletInkStyle As(ScarletInkLook look) => this with { Look = look };
}

// A weapon's client visuals implement this and register with ScarletRewardInk; DrawWorld asks every emitter once per
// frame for the paths it wants drawn. Emit must only add paths (no state changes, no allocation).
internal interface IScarletInkEmitter
{
    void Emit(ScarletInkCanvas canvas, in ScarletView view);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ScarletInkVertex : IVertexType
{
    internal Vector3 Position;
    internal Vector4 Path;  // u, v, L, head
    internal Vector4 Ink;   // radius, time, seed, opacity
    internal Vector4 Style; // fire, reduced, warmth (live: drying-joint caps, 1 start 2 end), close (1 river .. 0 scar)
    internal static readonly VertexDeclaration Declaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(28, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
        new VertexElement(44, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2));
    VertexDeclaration IVertexType.VertexDeclaration => Declaration;
}

// Paths submitted for one frame. Positions are world pixels; radius in px. Begin, then Point for each sample (every
// 8-12 px, denser on curves), then End. The helpers sample lines, quadratic curves, discs and droplets for you.
internal sealed class ScarletInkCanvas
{
    internal const int MaxCandidates = 768, MaxSamples = 16384, MaxPathSamples = 1024;
    internal readonly struct PathRecord
    {
        internal readonly int First, Count, BeadSample;
        internal readonly ScarletInkStyle Style;
        internal readonly bool Bead, Droplet;
        internal PathRecord(int first, int count, in ScarletInkStyle style, bool bead, bool droplet, int beadSample = -1)
        { First = first; Count = count; Style = style; Bead = bead; Droplet = droplet; BeadSample = beadSample; }
    }

    internal readonly PathRecord[] Paths = new PathRecord[MaxCandidates + CrimsonRewardRules.MaxDroplets];
    internal readonly Vector2[] At = new Vector2[MaxSamples];
    internal readonly float[] Radius = new float[MaxSamples], Time = new float[MaxSamples], Close = new float[MaxSamples];
    private readonly int[] dropletsByOwner = new int[256];
    internal int PathCount, SampleCount;
    private int open = -1, candidates, droplets;
    private bool valid;
    private ScarletInkStyle style;

    internal bool Reduced { get; private set; }

    internal void Reset(bool reduced)
    {
        PathCount = SampleCount = candidates = droplets = 0; open = -1; Reduced = reduced;
        Array.Clear(dropletsByOwner);
    }

    // Start a path. False when this frame's candidate store is full: the path is dropped (Point/End become no-ops).
    internal bool Begin(in ScarletInkStyle pathStyle)
    {
        if (open >= 0) End();
        if (candidates >= MaxCandidates || PathCount >= Paths.Length || SampleCount >= MaxSamples) { open = -2; return false; }
        style = pathStyle; open = SampleCount; valid = float.IsFinite(pathStyle.Seed) && float.IsFinite(pathStyle.Opacity);
        return true;
    }

    // One sample: world position, radius, and time (live: ticks since this point ignited; residue: fade 1..0).
    internal void Point(Vector2 at, float radius, float time = 0)
    {
        if (open < 0) return;
        if (SampleCount >= MaxSamples || SampleCount - open >= MaxPathSamples) return;
        if (!float.IsFinite(at.X) || !float.IsFinite(at.Y) || !float.IsFinite(radius) || !float.IsFinite(time)) { valid = false; return; }
        At[SampleCount] = at; Radius[SampleCount] = Math.Max(0, radius); Time[SampleCount] = time;
        Close[SampleCount] = ScarletRewardInk.CloseAt(style, time);
        SampleCount++;
    }

    // Finish the path; bead = true puts the ember-gold writing head at its last sample (ink still being written), or at
    // sample `beadSample` of this path when one is given (the Sable Scythe crescent carries it on its apex).
    internal void End(bool bead = false, int beadSample = -1)
    {
        if (open == -2) { open = -1; return; }
        if (open < 0) return;
        int count = SampleCount - open;
        if (count <= 0 || !valid || style.Opacity <= .001f) SampleCount = open;
        else { Paths[PathCount++] = new PathRecord(open, count, style, bead, false, beadSample < count ? beadSample : -1); candidates++; }
        open = -1;
    }

    // A straight path sampled about every 10 px, radius and time interpolated from a to b.
    internal void Line(in ScarletInkStyle pathStyle, Vector2 a, Vector2 b, float radiusA, float radiusB, float timeA, float timeB, bool bead = false)
    {
        if (!Begin(pathStyle)) return;
        int n = Math.Clamp((int)MathF.Ceiling(Vector2.Distance(a, b) / 10f) + 1, 2, MaxPathSamples);
        for (int i = 0; i < n; i++)
        {
            float s = i / (float)(n - 1);
            Point(Vector2.Lerp(a, b, s), MathHelper.Lerp(radiusA, radiusB, s), MathHelper.Lerp(timeA, timeB, s));
        }
        End(bead);
    }

    // A quadratic curve a -> b with control c, sampled about every 10 px and more densely where it bends.
    internal void Quadratic(in ScarletInkStyle pathStyle, Vector2 a, Vector2 c, Vector2 b, float radiusA, float radiusB, float timeA, float timeB, bool bead = false)
    {
        if (!Begin(pathStyle)) return;
        float length = Vector2.Distance(a, c) + Vector2.Distance(c, b), bend = (a - 2 * c + b).Length();
        int n = Math.Clamp((int)MathF.Ceiling(length / 10f + bend / 16f) + 1, 3, MaxPathSamples);
        for (int i = 0; i < n; i++)
        {
            float s = i / (float)(n - 1), r = 1 - s;
            Point(r * r * a + 2 * r * s * c + s * s * b, MathHelper.Lerp(radiusA, radiusB, s), MathHelper.Lerp(timeA, timeB, s));
        }
        End(bead);
    }

    // A zero-length capsule: a round blot or a burst.
    internal void Disc(in ScarletInkStyle pathStyle, Vector2 center, float radius, float time)
    {
        if (!Begin(pathStyle)) return;
        Point(center, radius, time);
        End();
    }

    // A droplet: a tiny two-point path in the same batch, counted against the droplet budgets (48 per owner, 160 in
    // all, halved by Reduced Effects) instead of the path cap. False when over budget.
    internal bool Droplet(in ScarletInkStyle pathStyle, Vector2 from, Vector2 to, float radius, float time)
    {
        if (open >= 0) End();
        int owner = pathStyle.Owner is >= 0 and < 255 ? pathStyle.Owner : 255;
        if (droplets >= CrimsonRewardRules.ReducedCount(CrimsonRewardRules.MaxDroplets, Reduced)
            || dropletsByOwner[owner] >= CrimsonRewardRules.ReducedCount(CrimsonRewardRules.MaxDropletsPerOwner, Reduced)
            || PathCount >= Paths.Length || SampleCount + 2 > MaxSamples) return false;
        if (!float.IsFinite(from.X + from.Y + to.X + to.Y + radius + time)) return false;
        int first = SampleCount;
        At[first] = from; At[first + 1] = to; Radius[first] = Radius[first + 1] = Math.Max(0, radius); Time[first] = Time[first + 1] = time;
        Close[first] = Close[first + 1] = 1;
        SampleCount += 2;
        Paths[PathCount++] = new PathRecord(first, 2, pathStyle, false, true);
        droplets++; dropletsByOwner[owner]++;
        return true;
    }
}

// Reward black blood (REWARDS.md#black-blood-material, #layering, #effect-bounds) on the Terraria-free Vfx seam, so
// the offline preview renders exactly what the game draws. Each pass draws all of its paths as one triangle strip in
// a single call (at most three draws a frame); over the per-frame caps (256 paths, 8,192 strip vertices) paths are
// dropped in REWARDS.md's order: other players' dormant ink, residue, other players' live ink, the local player's
// dormant ink. The local player's live ink is never dropped. Fixed arrays only; nothing is allocated per frame and no
// render target is owned. The caller saves and restores device state (the Terraria host does).
internal sealed partial class ScarletRewardInk
{
    // Live ink closes over its last CloseTicks (AutoloadPass's envelope; half the window when the window is shorter).
    internal const float CloseTicks = 6;
    internal static float CloseAt(in ScarletInkStyle style, float time)
    {
        if (style.Look != ScarletInkLook.Live) return 1;
        float remaining = style.Remaining, span = CloseTicks;
        if (style.Window > 0) { remaining = Math.Min(remaining, style.Window - time); span = Math.Min(span, style.Window * .5f); }
        if (!(remaining < span)) return 1;
        float x = Math.Clamp(remaining / span, 0, 1);
        return x * x * (3 - 2 * x);
    }

    // Strip half-width beyond the radius: the anti-aliased rim and the lip halo; fire adds room for flame tongues.
    internal static float HalfWidth(float radius, bool fire) => radius * 1.12f + 10 + (fire ? radius * .6f + 8 : 0);
    internal static int StripVertices(int samples) => 2 * (samples + 2) + 2;
    // Which caps of a live path are drying joints (1 the start, 2 the end): the end that has closed further.
    internal static float DryingJoints(float closeFirst, float closeLast)
        => (closeFirst < closeLast - .05f ? 1 : 0) + (closeLast < closeFirst - .05f ? 2 : 0);
    internal static string PassName(ScarletInkLook look) => look switch
    {
        ScarletInkLook.Live => "PathLivePass", ScarletInkLook.Dormant => "PathDormantPass", _ => "PathResiduePass",
    };
    internal static float RemoteOpacity(ScarletInkLook look)
        => look == ScarletInkLook.Dormant ? CrimsonRewardRules.RemoteDormantOpacity : CrimsonRewardRules.RemoteLiveOpacity;

    // Drop class: lower drops first. 4 (local live) is never dropped.
    internal static int DropClass(in ScarletInkStyle style) => style.Look switch
    {
        ScarletInkLook.Residue => 1,
        ScarletInkLook.Dormant => style.Local ? 3 : 0,
        _ => style.Local ? 4 : 2,
    };

    // Residue lies under builds, builds under live ink.
    private static readonly ScarletInkLook[] PassOrder = { ScarletInkLook.Residue, ScarletInkLook.Dormant, ScarletInkLook.Live };

    internal readonly ScarletInkCanvas Canvas = new();
    private readonly bool[] dropped = new bool[ScarletInkCanvas.MaxCandidates + CrimsonRewardRules.MaxDroplets];
    private readonly ScarletInkVertex[] vertices = new ScarletInkVertex[CrimsonRewardRules.MaxStripVertices + CrimsonRewardRules.MaxDroplets * 10];
    private readonly int[] passStart = new int[3], passCount = new int[3];
    private Vector2 previousDirection;

    internal int DrawnPaths { get; private set; }
    internal int DroppedPaths { get; private set; }
    internal int DrawnVertices { get; private set; }

    // Start a frame: clears the canvas for the emitters.
    internal ScarletInkCanvas Begin(in ScarletView view)
    {
        Canvas.Reset(view.Reduced);
        return Canvas;
    }

    // Draw everything submitted since Begin. Sets blend, depth, rasterizer and noise samplers 1-2; restores nothing.
    internal void Draw(in ScarletView view, IScarletAssets assets)
    {
        Select();
        Build(view.Reduced);
        DrawnVertices = passCount[0] + passCount[1] + passCount[2];
        if (DrawnVertices == 0) return;
        var device = view.Device;
        var shader = assets.GetShader("ScarletInk");
        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
        device.Textures[1] = assets.GetTexture("Noise/TurbulentNoise"); device.SamplerStates[1] = SamplerState.LinearWrap;
        device.Textures[2] = assets.GetTexture("Noise/WavyBlotchNoise"); device.SamplerStates[2] = SamplerState.LinearWrap;
        shader.Set("uWorldViewProjection", view.WorldClip);
        shader.Set("clock", view.Clock / 60f);
        foreach (var look in PassOrder)
        {
            int pass = (int)look;
            if (passCount[pass] < 3) continue;
            shader.Apply(PassName(look));
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, passStart[pass], passCount[pass] - 2);
        }
    }

    // Apply the per-frame caps in REWARDS.md's drop order. Droplets are outside the path cap (their own budgets).
    private void Select()
    {
        var c = Canvas;
        int paths = 0, verts = 0;
        for (int i = 0; i < c.PathCount; i++)
        {
            dropped[i] = false;
            if (c.Paths[i].Droplet) continue;
            paths++; verts += StripVertices(c.Paths[i].Count);
        }
        int drops = 0;
        for (int cls = 0; cls < 4 && (paths > CrimsonRewardRules.MaxInkPaths || verts > CrimsonRewardRules.MaxStripVertices); cls++)
            for (int i = c.PathCount - 1; i >= 0 && (paths > CrimsonRewardRules.MaxInkPaths || verts > CrimsonRewardRules.MaxStripVertices); i--)
            {
                ref readonly var p = ref c.Paths[i];
                if (p.Droplet || dropped[i] || DropClass(p.Style) != cls) continue;
                dropped[i] = true; paths--; verts -= StripVertices(p.Count); drops++;
            }
        DroppedPaths = drops;
    }

    private void Build(bool reduced)
    {
        var c = Canvas;
        int cursor = 0, drawn = 0;
        int limit = vertices.Length;
        foreach (var look in PassOrder)
        {
            int pass = (int)look;
            passStart[pass] = cursor;
            int start = cursor;
            for (int i = 0; i < c.PathCount; i++)
            {
                ref readonly var p = ref c.Paths[i];
                if (dropped[i] || p.Style.Look != look) continue;
                int need = StripVertices(p.Count);
                if (cursor + need > limit) { dropped[i] = true; continue; } // only the local live overflow can reach here
                cursor = Strip(p, cursor, cursor > start, reduced);
                drawn++;
            }
            passCount[pass] = cursor - start;
        }
        DrawnPaths = drawn;
    }

    // One path as a strip: a cap section before the first sample, one section per sample, a cap section after the
    // last. u is the arc length (negative / beyond L on the caps), v the signed offset across.
    private int Strip(in ScarletInkCanvas.PathRecord p, int cursor, bool join, bool reduced)
    {
        var c = Canvas;
        int first = p.First, n = p.Count, last = first + n - 1;
        float length = 0, beadU = 0;
        for (int i = first + 1; i <= last; i++)
        {
            length += Vector2.Distance(c.At[i - 1], c.At[i]);
            if (i - first == p.BeadSample) beadU = length;
        }
        var s = p.Style;
        float opacity = s.Opacity * (s.Local ? CrimsonRewardRules.LocalOpacity : RemoteOpacity(s.Look));
        float head = p.Bead ? (p.BeadSample >= 0 ? beadU : length) : -1;
        // Live ink has no warmth; its third channel marks a drying joint: an end that has dried further than the other is
        // where a moving tail meets the scar behind it, which already has a round cap there.
        float third = Math.Clamp(s.Warmth, 0, 1);
        if (s.Look == ScarletInkLook.Live) third = DryingJoints(c.Close[first], c.Close[last]);
        var style = new Vector4(s.Fire ? 1 : 0, reduced ? 1 : 0, third, 0);
        // Paths share one strip per pass: the previous path ended on a repeated last vertex, and this one starts on a
        // repeated first vertex, so the triangles between them are degenerate.
        int leading = join ? cursor++ : -1;
        previousDirection = Vector2.UnitX;
        float u = 0;
        Vector2 firstDirection = Direction(first, first, last);
        float capStart = HalfWidth(c.Radius[first], s.Fire);
        cursor = Section(cursor, c.At[first] - firstDirection * capStart, firstDirection, -capStart, first, length, head, s.Seed, opacity, style);
        if (leading >= 0) vertices[leading] = vertices[leading + 1];
        for (int i = first; i <= last; i++)
        {
            if (i > first) u += Vector2.Distance(c.At[i - 1], c.At[i]);
            cursor = Section(cursor, c.At[i], Direction(i, first, last), u, i, length, head, s.Seed, opacity, style);
        }
        Vector2 lastDirection = Direction(last, first, last);
        float capEnd = HalfWidth(c.Radius[last], s.Fire);
        cursor = Section(cursor, c.At[last] + lastDirection * capEnd, lastDirection, length + capEnd, last, length, head, s.Seed, opacity, style);
        vertices[cursor] = vertices[cursor - 1]; // the repeated last vertex for the next path's joint
        return cursor + 1;
    }

    private Vector2 Direction(int i, int first, int last)
    {
        var c = Canvas;
        Vector2 d = c.At[Math.Min(i + 1, last)] - c.At[Math.Max(i - 1, first)];
        float len = d.Length();
        if (len > 1e-3f) previousDirection = d / len;
        return previousDirection;
    }

    private int Section(int cursor, Vector2 at, Vector2 direction, float u, int sample, float length, float head, float seed, float opacity, Vector4 style)
    {
        var c = Canvas;
        float radius = c.Radius[sample];
        float w = HalfWidth(radius, style.X > .5f);
        Vector2 normal = new(-direction.Y, direction.X);
        var ink = new Vector4(radius, c.Time[sample], seed, opacity);
        style.W = c.Close[sample];
        vertices[cursor] = new ScarletInkVertex { Position = new Vector3(at + normal * w, 0), Path = new Vector4(u, w, length, head), Ink = ink, Style = style };
        vertices[cursor + 1] = new ScarletInkVertex { Position = new Vector3(at - normal * w, 0), Path = new Vector4(u, -w, length, head), Ink = ink, Style = style };
        return cursor + 2;
    }
}
