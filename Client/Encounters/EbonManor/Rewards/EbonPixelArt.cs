#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.EbonManor.Rewards;

// One vertex of an evaluated Ebon primitive; EbonPixel.fx documents L/S/T per pass.
internal struct EbonPixelVertex : IVertexType
{
    public Vector3 Position;
    public Vector4 Local, Shape, Style;

    internal static readonly VertexDeclaration Declaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(28, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
        new VertexElement(44, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2));

    VertexDeclaration IVertexType.VertexDeclaration => Declaration;
}

// Records one frame of Ebon primitives in dot space (1 dot = 2 world px, screen-aligned) into bounded
// arrays and draws them into the half-resolution target. Crescents, bands and lines are single quads
// evaluated per dot by EbonPixel.fx; stars, debris, stitches and dots are plotted here as dot runs.
// Over-budget primitives are dropped, never grown. Depends only on FNA so offline previews link it.
internal sealed partial class EbonPixelCanvas
{
    internal const float DotScale = .5f;
    internal const int MaxCrescents = 24, CrescentSections = 48, MaxBands = 48, MaxLines = 384, MaxRects = 6144;
    internal const int MaxBurstPieces = 48;
    // Outline plus glow reach around drawn dots.
    internal const int Reach = 6;
    // EbonTone indices plus the internal debris wood.
    private const byte OutlineTone = 0, SilverTone = 3, IvoryTone = 4, MoonTone = 5, GoldTone = 6, CandleTone = 7, WoodTone = 8;
    private const float ShimmerSpeed = 1.5f, ShimmerSpacing = 36f;

    private readonly EbonPixelVertex[] crescents = new EbonPixelVertex[MaxCrescents * CrescentSections * 6];
    private readonly EbonPixelVertex[] bands = new EbonPixelVertex[MaxBands * 6];
    private readonly EbonPixelVertex[] lines = new EbonPixelVertex[MaxLines * 6];
    private readonly VertexPositionColor[] rects = new VertexPositionColor[MaxRects * 6];
    private readonly Vector2[] spokes = new Vector2[(CrescentSections + 1) * 2];
    private int crescentCount, crescentVertices, bandCount, lineCount, rectCount;
    private Vector2 origin;
    private int width, height;
    private float minX, minY, maxX, maxY;

    internal readonly record struct Checkpoint(int Crescents, int CrescentVertices, int Bands, int Lines, int Rects);

    internal int Width => width;
    internal int Height => height;
    internal float ShimmerOffset { get; private set; }
    internal int Dropped { get; private set; }
    internal bool Empty => crescentVertices + bandCount + lineCount + rectCount == 0;

    internal void Begin(Vector2 screenOrigin, int dotWidth, int dotHeight, float fraction, bool reduced, double ticks)
    {
        origin = screenOrigin;
        width = dotWidth;
        height = dotHeight;
        Fraction = fraction;
        Reduced = reduced;
        ShimmerOffset = (float)(ticks * ShimmerSpeed % ShimmerSpacing);
        crescentCount = crescentVertices = bandCount = lineCount = rectCount = 0;
        Dropped = 0;
        minX = minY = float.MaxValue;
        maxX = maxY = float.MinValue;
    }

    internal Checkpoint Mark() => new(crescentCount, crescentVertices, bandCount, lineCount, rectCount);

    // Forgets what a failing source recorded; the bounds may stay a little wide.
    internal void Rewind(Checkpoint mark)
    {
        crescentCount = mark.Crescents;
        crescentVertices = mark.CrescentVertices;
        bandCount = mark.Bands;
        lineCount = mark.Lines;
        rectCount = mark.Rects;
    }

    // Drawn dots plus outline/glow reach, clipped to the target.
    internal Rectangle DotBounds
    {
        get
        {
            if (Empty || minX > maxX) return Rectangle.Empty;
            int x0 = Math.Max(0, (int)MathF.Floor(minX) - Reach), y0 = Math.Max(0, (int)MathF.Floor(minY) - Reach);
            int x1 = Math.Min(width, (int)MathF.Ceiling(maxX) + Reach), y1 = Math.Min(height, (int)MathF.Ceiling(maxY) + Reach);
            return x1 > x0 && y1 > y0 ? new Rectangle(x0, y0, x1 - x0, y1 - y0) : Rectangle.Empty;
        }
    }

    // Draws the frame into the bound, cleared half-resolution target (premultiplied alpha blending).
    internal void Draw(GraphicsDevice device, Effect effect, Texture2D noise, int targetWidth, int targetHeight)
    {
        EbonPixelArt.Set(effect, "uWorldViewProjection", Matrix.CreateOrthographicOffCenter(0, targetWidth, targetHeight, 0, -1, 1));
        EbonPixelArt.Set(effect, "shimmerOffset", ShimmerOffset);
        EbonPixelArt.Set(effect, "reduced", Reduced ? 1f : 0f);
        device.Textures[1] = noise;
        device.SamplerStates[1] = SamplerState.LinearWrap;
        if (crescentVertices > 0)
        {
            EbonPixelArt.Apply(effect, "AutoloadPass");
            device.DrawUserPrimitives(PrimitiveType.TriangleList, crescents, 0, crescentVertices / 3);
        }
        if (bandCount > 0)
        {
            EbonPixelArt.Apply(effect, "BandPass");
            device.DrawUserPrimitives(PrimitiveType.TriangleList, bands, 0, bandCount * 2);
        }
        if (lineCount > 0)
        {
            EbonPixelArt.Apply(effect, "LinePass");
            device.DrawUserPrimitives(PrimitiveType.TriangleList, lines, 0, lineCount * 2);
        }
        if (rectCount > 0)
        {
            EbonPixelArt.Apply(effect, "FlatPass");
            device.DrawUserPrimitives(PrimitiveType.TriangleList, rects, 0, rectCount * 2);
        }
    }

    partial void CrescentCore(Vector2 root, float a0, float a1, float innerRadius, float outerRadius, float fade, float heat, int seed)
    {
        float sweep = a1 - a0;
        if (!(fade < 1f) || !(MathF.Abs(sweep) >= 1e-4f) || !(outerRadius > innerRadius) || !Finite(root)) return;
        if (crescentCount >= MaxCrescents) { Dropped++; return; }
        sweep = Math.Clamp(sweep, -MathF.Tau, MathF.Tau);
        Vector2 r = ToDot(root);
        float inner = MathF.Max(innerRadius, 0f) * DotScale, outer = outerRadius * DotScale;
        int sections = Math.Clamp((int)MathF.Ceiling(MathF.Abs(sweep) * outer / 3f), 4, CrescentSections);
        float step = sweep / sections;
        // Spokes reach past the true band so the polygon never shaves the evaluated rim.
        float spokeIn = MathF.Max(inner - 1.5f, 0f), spokeOut = (outer + 2.5f) / MathF.Cos(MathF.Min(MathF.Abs(step) * .5f, .6f));
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        for (int i = 0; i <= sections; i++)
        {
            float angle = a0 + step * i;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 a = r + direction * spokeIn, b = r + direction * spokeOut;
            spokes[i * 2] = a;
            spokes[i * 2 + 1] = b;
            x0 = MathF.Min(x0, MathF.Min(a.X, b.X)); x1 = MathF.Max(x1, MathF.Max(a.X, b.X));
            y0 = MathF.Min(y0, MathF.Min(a.Y, b.Y)); y1 = MathF.Max(y1, MathF.Max(a.Y, b.Y));
        }
        if (!Include(x0, y0, x1, y1)) return;
        Vector2 anchor = Floor(r);
        uint hash = Hash(seed);
        Vector4 shape = new(r.X - anchor.X, r.Y - anchor.Y, inner, outer);
        Vector4 style = new(Math.Clamp(fade, 0f, 1f), Math.Clamp(heat, 0f, 1f), (hash & 0xFFFF) / 65536f, (hash >> 16) / 65536f);
        float arc = MathF.Abs(sweep) * outer;
        int v = crescentVertices;
        for (int i = 1; i <= sections; i++)
        {
            float u0 = (i - 1f) / sections, u1 = (float)i / sections;
            EbonPixelVertex in0 = Prim(spokes[i * 2 - 2], anchor, new Vector2(u0, arc), shape, style);
            EbonPixelVertex out0 = Prim(spokes[i * 2 - 1], anchor, new Vector2(u0, arc), shape, style);
            EbonPixelVertex in1 = Prim(spokes[i * 2], anchor, new Vector2(u1, arc), shape, style);
            EbonPixelVertex out1 = Prim(spokes[i * 2 + 1], anchor, new Vector2(u1, arc), shape, style);
            crescents[v++] = in0; crescents[v++] = out0; crescents[v++] = in1;
            crescents[v++] = in1; crescents[v++] = out0; crescents[v++] = out1;
        }
        crescentVertices = v;
        crescentCount++;
    }

    partial void BandCore(Vector2 a, Vector2 b, float width, EbonBandMode mode, float progress, float alpha, int seed)
    {
        if (!(alpha > .004f) || !(progress > 0f) || !Finite(a) || !Finite(b)) return;
        if (bandCount >= MaxBands) { Dropped++; return; }
        Vector2 start = ToDot(a), delta = ToDot(b) - start;
        float length = delta.Length();
        Vector2 direction = length > 1e-3f ? delta / length : Vector2.UnitX, normal = new(-direction.Y, direction.X);
        float half = MathF.Max(width * DotScale * .5f, .5f), head = length * MathF.Min(progress, 1f);
        float side = half + 4f, back = 3f, front = head + 4f;
        Vector2 p0 = start - direction * back - normal * side, p1 = start + direction * front - normal * side;
        Vector2 p2 = start - direction * back + normal * side, p3 = start + direction * front + normal * side;
        if (!IncludeQuad(p0, p1, p2, p3)) return;
        Vector2 anchor = Floor(start);
        Vector2 local = new(mode == EbonBandMode.Tear ? 1f : 0f, (Hash(seed) & 0xFFFF) / 65536f);
        Vector4 shape = new(start.X - anchor.X, start.Y - anchor.Y, direction.X, direction.Y);
        Vector4 style = new(length, half, head, MathF.Min(alpha, 1f));
        Quad(bands, bandCount++ * 6, p0, p1, p2, p3, anchor, local, shape, style);
    }

    partial void ThreadCore(Vector2 a, Vector2 b, EbonTone tone, int thickness, float alpha, float shimmer)
        => Line(0f, a, b, (byte)tone, thickness, alpha, Math.Clamp(shimmer, 0f, 1f), 0f);

    partial void StringCore(Vector2 a, Vector2 b, EbonTone tone, float amplitude, float phase, int thickness, float alpha)
        => Line(1f, a, b, (byte)tone, thickness, alpha, amplitude * DotScale, phase);

    partial void RingCore(Vector2 center, float radius, EbonTone tone, int thickness, float alpha)
    {
        if (!(alpha > .004f) || !(radius >= 0f) || !Finite(center)) return;
        if (lineCount >= MaxLines) { Dropped++; return; }
        thickness = Math.Clamp(thickness, 1, 3);
        Vector2 cell = Floor(ToDot(center)), middle = cell + new Vector2(.5f);
        float r = radius * DotScale, extent = r + thickness + 1.5f;
        Vector2 p0 = middle + new Vector2(-extent, -extent), p1 = middle + new Vector2(extent, -extent);
        Vector2 p2 = middle + new Vector2(-extent, extent), p3 = middle + new Vector2(extent, extent);
        if (!IncludeQuad(p0, p1, p2, p3)) return;
        Quad(lines, lineCount++ * 6, p0, p1, p2, p3, cell, new Vector2(2f, (byte)tone),
            new Vector4(r, 0f, 0f, 0f), new Vector4(thickness, MathF.Min(alpha, 1f), 0f, 0f));
    }

    partial void DotCore(Vector2 at, EbonTone tone, int size, float alpha)
    {
        if (!(alpha > .004f) || !Finite(at)) return;
        size = Math.Clamp(size, 1, 8);
        Point c = Cell(at);
        Rect(c.X - (size - 1) / 2, c.Y - (size - 1) / 2, size, size, (byte)tone, alpha);
    }

    // Rays grow out fast, then retreat from the centre while cooling moon -> ivory -> silver.
    // A navy rim is laid first so the star reads over pale crescents and chalk.
    partial void StarCore(Vector2 at, float radius, float age01, int seed)
    {
        if (!(age01 >= 0f) || age01 >= 1f || !(radius > 0f) || !Finite(at)) return;
        Vector2 c = ToDot(at);
        uint hash = Hash(seed);
        int rays = Reduced ? 4 : 4 + (int)(hash % 3u);
        float spin = -MathF.PI / 2f + ((hash >> 8) & 255) / 256f * .5f;
        float full = radius * DotScale, p = age01;
        float reach = full * (p < .18f ? .35f + .65f * p / .18f : 1f - (p - .18f) * .55f);
        float skip = reach * MathF.Max(0f, p - .35f) * 1.15f;
        byte tone = p < .3f ? MoonTone : p < .65f ? IvoryTone : SilverTone;
        for (int pass = 0; pass < 2; pass++)
        {
            bool rim = pass == 0;
            if (rim && p >= .6f) continue;
            for (int r = 0; r < rays; r++)
            {
                float angle = spin + r * MathF.Tau / rays;
                float length = rays % 2 == 0 && r % 2 == 1 ? .55f : 1f;
                Vector2 d = new(MathF.Cos(angle), MathF.Sin(angle));
                Segment(ToPoint(c + d * skip), ToPoint(c + d * reach * length), tone, 1f, rim);
            }
        }
        if (p < .45f)
        {
            Point o = ToPoint(c);
            Rect(o.X, o.Y, 1, 1, MoonTone, 1f);
            if (full >= 6f && p < .3f)
            {
                Rect(o.X - 1, o.Y, 3, 1, MoonTone, 1f);
                Rect(o.X, o.Y - 1, 1, 3, MoonTone, 1f);
            }
        }
    }

    // Closed-form debris (drag + gravity) so a source only needs its own clock; nothing is retained.
    partial void BurstCore(Vector2 origin, int seed, int count, float t, float life, float speed, float gravity, EbonShardKind kind, float spread, float heading)
    {
        if (!(t >= 0f) || !(life > 0f) || t > life || count <= 0 || !Finite(origin)) return;
        count = Math.Min(count, MaxBurstPieces);
        (float drag, float fall) = kind switch
        {
            EbonShardKind.Glass => (.035f, 1f),
            EbonShardKind.Wood => (.03f, 1.15f),
            EbonShardKind.Silk => (.09f, .3f),
            EbonShardKind.Spark => (.05f, .55f),
            _ => (.08f, .22f),
        };
        float travel = (1f - MathF.Exp(-drag * t)) / drag, keep = MathF.Exp(-drag * t);
        for (int k = 0; k < count; k++)
        {
            var random = new Random32(seed * 7919 + k * 104729 + (int)kind * 31337);
            float lifeK = life * random.Range(.55f, 1f);
            float angle = heading + (random.Next() - .5f) * spread, v = speed * random.Range(.35f, 1f);
            float turn = random.Range(0f, MathF.Tau) + random.Range(-.35f, .35f) * t;
            float pick = random.Next();
            if (t > lifeK) continue;
            Vector2 velocity = new(MathF.Cos(angle) * v, MathF.Sin(angle) * v);
            Vector2 world = origin + velocity * travel + new Vector2(0f, .5f * gravity * fall * t * t);
            Vector2 now = velocity * keep + new Vector2(0f, gravity * fall * t);
            float age = t / lifeK, alpha = age < .7f ? 1f : 1f - (age - .7f) / .3f;
            switch (kind)
            {
                case EbonShardKind.Glass:
                {
                    // a 2-4 dot sliver spinning in place, moon-tipped; fresh shards flash white
                    int length = 2 + (int)(pick * 3f);
                    Point a = Cell(world), b = ToPoint(ToDot(world) + new Vector2(MathF.Cos(turn), MathF.Sin(turn)) * (length - 1));
                    byte body = age < .15f ? MoonTone : pick < .55f ? SilverTone : IvoryTone;
                    Segment(a, b, body, alpha, false);
                    Rect(b.X, b.Y, 1, 1, MoonTone, alpha);
                    break;
                }
                case EbonShardKind.Wood:
                {
                    int quarter = (int)MathF.Floor(turn / (MathF.PI / 2f)) & 3;
                    bool square = pick < .3f;
                    int w = square || quarter % 2 == 0 ? 2 : 1, h = square || quarter % 2 == 1 ? 2 : 1;
                    Point a = Cell(world);
                    Rect(a.X, a.Y, w, h, WoodTone, alpha);
                    if ((quarter + k) % 2 == 0) Rect(a.X + w - 1, a.Y, 1, 1, GoldTone, alpha);
                    break;
                }
                case EbonShardKind.Silk:
                {
                    // a curl: three short chords of a two-dot arc, turning as it floats
                    Vector2 centre = ToDot(world);
                    Point previous = ToPoint(centre + new Vector2(MathF.Cos(turn), MathF.Sin(turn)) * 1.8f);
                    for (int j = 1; j <= 3; j++)
                    {
                        float theta = turn + j * .8f;
                        Point next = ToPoint(centre + new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * 1.8f);
                        Segment(previous, next, j == 3 ? SilverTone : IvoryTone, alpha, false);
                        previous = next;
                    }
                    break;
                }
                case EbonShardKind.Spark:
                {
                    Point head = Cell(world);
                    if (!Reduced && now.LengthSquared() > 1.44f)
                        Segment(ToPoint(ToDot(world - now * 1.4f)), head, GoldTone, alpha, false);
                    Rect(head.X, head.Y, 1, 1, age < .2f ? MoonTone : age < .6f ? CandleTone : GoldTone, alpha);
                    break;
                }
                default:
                {
                    // lace specks sway and twinkle
                    Point a = Cell(world + new Vector2(MathF.Sin(t * .15f + k) * 3f, 0f));
                    bool twinkle = (k + (int)(t / 4f)) % 3 == 0;
                    Rect(a.X, a.Y, 1, 1, twinkle ? MoonTone : IvoryTone, alpha);
                    break;
                }
            }
        }
    }

    partial void StitchCore(Vector2 at, int size, EbonTone tone, float alpha)
    {
        if (!(alpha > .004f) || !Finite(at)) return;
        size = Math.Clamp(size, 1, 6);
        Point c = Cell(at);
        Segment(new Point(c.X - size, c.Y - size), new Point(c.X + size, c.Y + size), (byte)tone, alpha, false);
        Segment(new Point(c.X - size, c.Y + size), new Point(c.X + size, c.Y - size), (byte)tone, alpha, false);
        Rect(c.X, c.Y, 1, 1, tone == EbonTone.Moon ? IvoryTone : MoonTone, alpha);
    }

    // Threads and strings are anchored at the first end's dot; the shader tests whole-dot offsets.
    private void Line(float kind, Vector2 a, Vector2 b, byte tone, int thickness, float alpha, float z, float w)
    {
        if (!(alpha > .004f) || !Finite(a) || !Finite(b) || !float.IsFinite(z) || !float.IsFinite(w)) return;
        if (lineCount >= MaxLines) { Dropped++; return; }
        thickness = Math.Clamp(thickness, 1, 3);
        Vector2 ca = Floor(ToDot(a)), delta = Floor(ToDot(b)) - ca;
        float length = delta.Length();
        Vector2 direction = length > .5f ? delta / length : Vector2.UnitX, normal = new(-direction.Y, direction.X);
        float margin = thickness * .75f + 1.5f, side = margin + (kind > .5f ? MathF.Abs(z) * 1.1f + 1f : 0f);
        Vector2 from = ca + new Vector2(.5f) - direction * margin, to = ca + new Vector2(.5f) + delta + direction * margin;
        Vector2 p0 = from - normal * side, p1 = to - normal * side, p2 = from + normal * side, p3 = to + normal * side;
        if (!IncludeQuad(p0, p1, p2, p3)) return;
        Quad(lines, lineCount++ * 6, p0, p1, p2, p3, ca, new Vector2(kind, tone),
            new Vector4(delta.X, delta.Y, length, 0f), new Vector4(thickness, MathF.Min(alpha, 1f), z, w));
    }

    // Bresenham, merged into runs along the major axis; a rim lays the navy 4-neighbourhood instead.
    private void Segment(Point a, Point b, byte tone, float alpha, bool rim)
    {
        int dx = Math.Abs(b.X - a.X), dy = -Math.Abs(b.Y - a.Y);
        int sx = a.X < b.X ? 1 : -1, sy = a.Y < b.Y ? 1 : -1, error = dx + dy;
        bool horizontal = dx >= -dy;
        Point start = a, last = a;
        for (int guard = 0; guard < 2048; guard++)
        {
            if (guard > 0 && (horizontal ? a.Y != last.Y : a.X != last.X))
            {
                Run(start, last, tone, alpha, rim);
                start = a;
            }
            last = a;
            if (a == b) break;
            int e2 = 2 * error;
            if (e2 >= dy) { error += dy; a.X += sx; }
            if (e2 <= dx) { error += dx; a.Y += sy; }
        }
        Run(start, last, tone, alpha, rim);
    }

    private void Run(Point from, Point to, byte tone, float alpha, bool rim)
    {
        int x = Math.Min(from.X, to.X), y = Math.Min(from.Y, to.Y);
        int w = Math.Abs(to.X - from.X) + 1, h = Math.Abs(to.Y - from.Y) + 1;
        if (!rim)
        {
            Rect(x, y, w, h, tone, alpha);
            return;
        }
        Rect(x - 1, y, w + 2, h, OutlineTone, alpha);
        Rect(x, y - 1, w, h + 2, OutlineTone, alpha);
    }

    private void Rect(int x, int y, int w, int h, byte tone, float alpha)
    {
        if (rectCount >= MaxRects) { Dropped++; return; }
        if (!Include(x, y, x + w, y + h)) return;
        Color color = EbonPixelArt.Palette[Math.Min((int)tone, EbonPixelArt.Palette.Length - 1)] * Math.Clamp(alpha, 0f, 1f);
        int v = rectCount++ * 6;
        rects[v] = new VertexPositionColor(new Vector3(x, y, 0f), color);
        rects[v + 1] = new VertexPositionColor(new Vector3(x + w, y, 0f), color);
        rects[v + 2] = new VertexPositionColor(new Vector3(x, y + h, 0f), color);
        rects[v + 3] = rects[v + 2];
        rects[v + 4] = rects[v + 1];
        rects[v + 5] = new VertexPositionColor(new Vector3(x + w, y + h, 0f), color);
    }

    private static void Quad(EbonPixelVertex[] buffer, int at, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
        Vector2 anchor, Vector2 local, Vector4 shape, Vector4 style)
    {
        buffer[at] = Prim(p0, anchor, local, shape, style);
        buffer[at + 1] = Prim(p1, anchor, local, shape, style);
        buffer[at + 2] = Prim(p2, anchor, local, shape, style);
        buffer[at + 3] = buffer[at + 2];
        buffer[at + 4] = buffer[at + 1];
        buffer[at + 5] = Prim(p3, anchor, local, shape, style);
    }

    private static EbonPixelVertex Prim(Vector2 position, Vector2 anchor, Vector2 local, Vector4 shape, Vector4 style) => new()
    {
        Position = new Vector3(position, 0f),
        Local = new Vector4(position.X - anchor.X, position.Y - anchor.Y, local.X, local.Y),
        Shape = shape,
        Style = style,
    };

    private bool IncludeQuad(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        => Include(MathF.Min(MathF.Min(p0.X, p1.X), MathF.Min(p2.X, p3.X)), MathF.Min(MathF.Min(p0.Y, p1.Y), MathF.Min(p2.Y, p3.Y)),
            MathF.Max(MathF.Max(p0.X, p1.X), MathF.Max(p2.X, p3.X)), MathF.Max(MathF.Max(p0.Y, p1.Y), MathF.Max(p2.Y, p3.Y)));

    // Culls against the target (with outline/glow reach) and grows the composite bounds.
    private bool Include(float x0, float y0, float x1, float y1)
    {
        if (!(x1 >= -Reach && y1 >= -Reach && x0 <= width + Reach && y0 <= height + Reach)) return false;
        minX = MathF.Min(minX, x0); minY = MathF.Min(minY, y0);
        maxX = MathF.Max(maxX, x1); maxY = MathF.Max(maxY, y1);
        return true;
    }

    private Vector2 ToDot(Vector2 world) => (world - origin) * DotScale;
    private Point Cell(Vector2 world) => ToPoint(ToDot(world));
    private static Point ToPoint(Vector2 dot) => new((int)MathF.Floor(dot.X), (int)MathF.Floor(dot.Y));
    private static Vector2 Floor(Vector2 v) => new(MathF.Floor(v.X), MathF.Floor(v.Y));
    private static bool Finite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);

    private static uint Hash(int seed)
    {
        uint h = (uint)seed * 747796405u + 2891336453u;
        h = ((h >> (int)((h >> 28) + 4u)) ^ h) * 277803737u;
        return (h >> 22) ^ h;
    }

    // Allocation-free deterministic sequence (xorshift32); drawing never uses gameplay RNG.
    private struct Random32
    {
        private uint state;

        internal Random32(int seed)
        {
            state = Hash(seed);
            if (state == 0) state = 1;
        }

        internal float Next()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0xFFFFFF) / 16777216f;
        }

        internal float Range(float low, float high) => low + (high - low) * Next();
    }
}

// Palette, composite and device-state helpers shared by the game layer and offline previews.
internal static class EbonPixelArt
{
    internal const string ShaderName = "Convergence.EbonPixel";
    internal const string NoisePath = "Convergence/Assets/Textures/Items/DXOboro/SlashNoise";

    // EbonTone order, then the internal debris wood (index 8). Must match EbonPixel.fx.
    internal static readonly Color[] Palette =
    {
        new(14, 12, 26), new(44, 40, 58), new(196, 112, 134), new(176, 182, 204), new(238, 230, 214),
        new(250, 252, 255), new(198, 164, 92), new(255, 196, 112), new(122, 82, 56),
    };

    private static readonly VertexPositionTexture[] quad = new VertexPositionTexture[6];

    // Point-upscales the half-resolution layer over `screenBounds` (screen pixels, before `view`)
    // with the one-dot navy outline and a small glow (none when reduced).
    internal static void Composite(GraphicsDevice device, Effect effect, Texture2D scene, Rectangle screenBounds, Matrix view, bool reduced)
    {
        float width = scene.Width, height = scene.Height;
        VertexPositionTexture Corner(int x, int y)
            => new(new Vector3(x, y, 0), new Vector2(x * EbonPixelCanvas.DotScale / width, y * EbonPixelCanvas.DotScale / height));
        quad[0] = Corner(screenBounds.Left, screenBounds.Top);
        quad[1] = Corner(screenBounds.Right, screenBounds.Top);
        quad[2] = Corner(screenBounds.Left, screenBounds.Bottom);
        quad[3] = quad[2];
        quad[4] = quad[1];
        quad[5] = Corner(screenBounds.Right, screenBounds.Bottom);
        Set(effect, "uWorldViewProjection", view * Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
        Set(effect, "texel", new Vector2(1f / width, 1f / height));
        Set(effect, "glowStrength", reduced ? 0f : .5f);
        device.Textures[0] = scene;
        device.SamplerStates[0] = SamplerState.PointClamp;
        Apply(effect, "CompositePass");
        device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
    }

    internal static void Set(Effect effect, string name, float value) => effect.Parameters[name]?.SetValue(value);
    internal static void Set(Effect effect, string name, Vector2 value) => effect.Parameters[name]?.SetValue(value);
    internal static void Set(Effect effect, string name, Matrix value) => effect.Parameters[name]?.SetValue(value);
    internal static void Apply(Effect effect, string pass) => effect.CurrentTechnique.Passes[pass].Apply();
}

// Caller's device state around the layer's target and composite. Render targets are read through FNA's
// allocation-free accessor into fixed arrays (FNA allows at most four bound targets).
internal readonly struct EbonDeviceState
{
    private static readonly RenderTargetBinding[][] targets =
    {
        Array.Empty<RenderTargetBinding>(), new RenderTargetBinding[1], new RenderTargetBinding[2],
        new RenderTargetBinding[3], new RenderTargetBinding[4],
    };

    private readonly BlendState blend;
    private readonly DepthStencilState depth;
    private readonly RasterizerState raster;
    private readonly Rectangle scissor;
    private readonly SamplerState sampler0, sampler1;
    private readonly Texture? texture0, texture1;
    private readonly VertexBufferBinding[] vertices;
    private readonly IndexBuffer? indices;
    private readonly int targetCount;

    private EbonDeviceState(GraphicsDevice device, bool withTargets)
    {
        blend = device.BlendState;
        depth = device.DepthStencilState;
        raster = device.RasterizerState;
        scissor = device.ScissorRectangle;
        sampler0 = device.SamplerStates[0];
        sampler1 = device.SamplerStates[1];
        texture0 = device.Textures[0];
        texture1 = device.Textures[1];
        vertices = device.GetVertexBuffers();
        indices = device.Indices;
        targetCount = -1;
        if (!withTargets) return;
        targetCount = Math.Min(device.GetRenderTargetsNoAllocEXT(null), targets.Length - 1);
        device.GetRenderTargetsNoAllocEXT(targets[targetCount]);
    }

    internal static EbonDeviceState Capture(GraphicsDevice device, bool withTargets) => new(device, withTargets);

    internal void Restore(GraphicsDevice device)
    {
        if (targetCount == 0) device.SetRenderTarget(null);
        else if (targetCount > 0) device.SetRenderTargets(targets[targetCount]);
        device.Textures[0] = texture0;
        device.Textures[1] = texture1;
        device.SamplerStates[0] = sampler0;
        device.SamplerStates[1] = sampler1;
        device.SetVertexBuffers(vertices);
        device.Indices = indices;
        device.BlendState = blend;
        device.DepthStencilState = depth;
        device.RasterizerState = raster;
        device.ScissorRectangle = scissor;
    }
}
