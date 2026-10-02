#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NVector2 = System.Numerics.Vector2;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// Records one frame of Doll weapon commands into bounded arrays and draws them. World coordinates are
// pixels, angles radians (+y down). Dot space is the half-resolution grid aligned to even world
// coordinates: dot (x, y) covers world Origin + 2 * (x, y) .. + 2, so axis-aligned sprites snapped by
// DollSpritePlacement land texel-for-dot and stationary art never shimmers while the camera pans.
//  - Sprites: the Art target (Front) or direct world-space draws (Back), sorted by (depth, order).
//  - Light, in order: energy batches (by depth), then lines/arcs/forecasts, then dot runs (dots, debris).
// Over-budget commands are dropped and counted, never grown. Depends only on FNA (and the pure placement),
// so the offline preview links it.
internal sealed class DollWeaponCanvas
{
    internal const float DotScale = .5f;
    internal const int MaxSprites = 256, MaxLines = 512, MaxRects = 8192, MaxBurstPieces = 48;
    internal const int MaxEnergyBatches = 64, MaxEnergyVertices = 12288, MaxStripPoints = 32, MaxSpriteTexels = 1024;
    // Outline plus glow reach around lit dots.
    internal const int Reach = 6;
    // Shared rule: another player's damaging light draws at 65%, their void at alpha .60; bodies stay opaque.
    internal const float PeerLightAlpha = .65f, PeerVoidAlpha = .60f;
    // Forecast heads travel this many dots per tick.
    internal const float TravelSpeed = .75f;

    private const byte KindLine = 0, KindArc = 1, KindForecastLine = 2, KindForecastArc = 3;

    private struct SpriteCommand
    {
        internal Texture2D? Texture;
        internal Rectangle Source;
        internal NVector2 PivotTexel, PivotWorld;
        internal float Cos, Sin;
        internal DollFlip Flip;
        internal DollStratum Stratum;
        internal sbyte Depth;
        internal bool Rotated;
        internal float Rotation;
        internal DollSpriteFx Fx;
    }

    private struct EnergyBatch
    {
        internal IDollEnergyMaterial? Material;
        internal int Pass, Start, Count;
        internal sbyte Depth;
    }

    // Dot-space bounds of recorded content.
    private struct Area
    {
        internal float MinX, MinY, MaxX, MaxY;
        internal void Reset() { MinX = MinY = float.MaxValue; MaxX = MaxY = float.MinValue; }
        internal void Add(float x0, float y0, float x1, float y1)
        {
            MinX = MathF.Min(MinX, x0); MinY = MathF.Min(MinY, y0);
            MaxX = MathF.Max(MaxX, x1); MaxY = MathF.Max(MaxY, y1);
        }
        internal readonly bool Empty => MinX > MaxX;
    }

    internal readonly record struct Checkpoint(int Sprites, int Lines, int Rects, int Energy, int Batches);

    private readonly SpriteCommand[] sprites = new SpriteCommand[MaxSprites];
    private readonly int[] order = new int[MaxSprites];
    private readonly DollPixelVertex[] lines = new DollPixelVertex[MaxLines * 6];
    private readonly VertexPositionColor[] rects = new VertexPositionColor[MaxRects * 6];
    private readonly DollPixelVertex[] energy = new DollPixelVertex[MaxEnergyVertices];
    private readonly EnergyBatch[] batches = new EnergyBatch[MaxEnergyBatches];
    private readonly int[] batchOrder = new int[MaxEnergyBatches];
    private readonly DollPixelVertex[] spriteQuad = new DollPixelVertex[6];
    private readonly NVector2[] corners = new NVector2[4];
    private readonly DollPixelVertex[] quadCorners = new DollPixelVertex[4];
    private int spriteCount, lineCount, rectCount, energyCount, batchCount;
    private Vector2 origin;
    private int width, height;
    private Area art, light, back;

    // WeaponDrawClock.Fraction for this frame (0..1 between game ticks).
    internal float Fraction { get; private set; } = 1f;
    // Doll Reduced Effects (read-only for sources): halve debris and residue; never change bodies,
    // forecasts, live bodies, counts or audio. The layer drops the glow itself.
    internal bool Reduced { get; private set; }
    // Game ticks plus Fraction, for animation phases.
    internal double Clock { get; private set; }
    internal int Dropped { get; private set; }
    // An energy material that threw this frame (its batch was skipped); the layer logs it once.
    internal Exception? MaterialError { get; private set; }
    // World position of target cell (0, 0): even world coordinates.
    internal Vector2 Origin => origin;
    internal Vector2 DotOrigin => origin * DotScale;
    internal int Width => width;
    internal int Height => height;
    internal bool HasLight => energyCount + lineCount + rectCount > 0 && !light.Empty;
    internal bool HasArt => !art.Empty;
    internal bool HasBack => !back.Empty;
    internal Rectangle ArtArea => Bounds(art, 1);
    internal Rectangle LightArea => Bounds(light, Reach);

    internal void Begin(Vector2 screenPosition, int screenWidth, int screenHeight, float fraction, bool reduced, double clock)
    {
        Array.Clear(sprites, 0, spriteCount);
        Array.Clear(batches, 0, batchCount);
        origin = new Vector2(MathF.Floor(screenPosition.X * DotScale), MathF.Floor(screenPosition.Y * DotScale)) / DotScale;
        width = Math.Max(1, screenWidth / 2 + 2);
        height = Math.Max(1, screenHeight / 2 + 2);
        Fraction = fraction;
        Reduced = reduced;
        Clock = clock;
        spriteCount = lineCount = rectCount = energyCount = batchCount = 0;
        Dropped = 0;
        MaterialError = null;
        art.Reset();
        light.Reset();
        back.Reset();
    }

    // Drops every reference (textures, materials) recorded by the last frame.
    internal void Clear()
    {
        Array.Clear(sprites, 0, spriteCount);
        Array.Clear(batches, 0, batchCount);
        spriteCount = lineCount = rectCount = energyCount = batchCount = 0;
        art.Reset();
        light.Reset();
        back.Reset();
    }

    internal Checkpoint Mark() => new(spriteCount, lineCount, rectCount, energyCount, batchCount);

    // Forgets what a failing source recorded; the bounds may stay a little wide.
    internal void Rewind(Checkpoint mark)
    {
        Array.Clear(sprites, mark.Sprites, spriteCount - mark.Sprites);
        Array.Clear(batches, mark.Batches, batchCount - mark.Batches);
        spriteCount = mark.Sprites;
        lineCount = mark.Lines;
        rectCount = mark.Rects;
        energyCount = mark.Energy;
        batchCount = mark.Batches;
        if (batchCount > 0)
        {
            ref EnergyBatch last = ref batches[batchCount - 1];
            last.Count = Math.Min(last.Count, energyCount - last.Start);
        }
    }

    // Grows the Light bounds by every recorded energy vertex; call once after all sources emitted. The raw
    // Energy() span is the weapons' to fill, so a NaN or infinite vertex is skipped here rather than letting it
    // poison the bounds (an empty LightArea would hide every light of the frame).
    internal void EndRecording()
    {
        for (int i = 0; i < energyCount; i++)
        {
            Vector3 p = energy[i].Position;
            if (!float.IsFinite(p.X) || !float.IsFinite(p.Y)) continue;
            light.Add(p.X, p.Y, p.X, p.Y);
        }
    }

    internal Vector2 ToDot(Vector2 world) => (world - origin) * DotScale;

    // ---- Sprites -------------------------------------------------------------------------------

    // A pixel sprite with its pivot at pivotWorld, rotated about the pivot after `flip`. Axis-aligned
    // placements snap to the dot grid. Reversed gravity: see DollSpritePlacement.ApplyGravity.
    internal void Sprite(in DollSprite sprite, Vector2 pivotWorld, float rotation, DollFlip flip,
        DollStratum stratum = DollStratum.Front, sbyte depth = 0, in DollSpriteFx fx = default)
    {
        Texture2D? texture = sprite.Texture;
        Rectangle source = sprite.Source;
        // Invalid or oversize input is a rejected command and counts as dropped (a bug in the caller, visible
        // through Dropped); a sprite that is merely fully faded, hidden or dissolved just draws nothing.
        if (texture is null || texture.IsDisposed || source.Width <= 0 || source.Height <= 0
            || source.Width > MaxSpriteTexels || source.Height > MaxSpriteTexels || source.X < 0 || source.Y < 0
            || source.Right > texture.Width || source.Bottom > texture.Height
            || !Finite(pivotWorld) || !Finite(sprite.PivotTexel) || !float.IsFinite(rotation)
            || !float.IsFinite(fx.Fade) || !float.IsFinite(fx.Hide) || !float.IsFinite(fx.Dissolve)
            || !float.IsFinite(fx.Flash)) { Dropped++; return; }
        if (fx.Fade >= 1f || fx.Hide >= 1f || fx.Dissolve >= 1f) return;
        if (spriteCount >= MaxSprites) { Dropped++; return; }
        NVector2 pivotTexel = new(sprite.PivotTexel.X, sprite.PivotTexel.Y);
        NVector2 pivot = DollSpritePlacement.Snap(new NVector2(pivotWorld.X, pivotWorld.Y), pivotTexel, rotation, flip);
        DollSpritePlacement.Basis(rotation, out float cos, out float sin);
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        Corners(pivot, pivotTexel, cos, sin, flip, source.Width, source.Height, 0);
        foreach (NVector2 corner in corners)
        {
            float x = (corner.X - origin.X) * DotScale, y = (corner.Y - origin.Y) * DotScale;
            x0 = MathF.Min(x0, x); y0 = MathF.Min(y0, y); x1 = MathF.Max(x1, x); y1 = MathF.Max(y1, y);
        }
        if (!(x1 >= -1 && y1 >= -1 && x0 <= width + 1 && y0 <= height + 1)) return;
        if (stratum == DollStratum.Back) back.Add(x0, y0, x1, y1);
        else art.Add(x0, y0, x1, y1);
        sprites[spriteCount++] = new SpriteCommand
        {
            Texture = texture, Source = source, PivotTexel = pivotTexel, PivotWorld = pivot, Cos = cos, Sin = sin,
            Flip = flip, Stratum = stratum, Depth = depth, Rotated = DollSpritePlacement.QuarterTurns(rotation) < 0,
            Rotation = rotation, Fx = fx,
        };
    }

    // World corners of the texel rectangle grown by `margin` texels.
    private void Corners(NVector2 pivot, NVector2 pivotTexel, float cos, float sin, DollFlip flip, int w, int h, float margin)
    {
        for (int i = 0; i < 4; i++)
        {
            NVector2 texel = new(i % 2 == 0 ? -margin : w + margin, i < 2 ? -margin : h + margin);
            NVector2 local = DollSpritePlacement.Mirror((texel - pivotTexel) * DollSpritePlacement.WorldPerTexel, flip);
            corners[i] = pivot + DollSpritePlacement.Rotate(local, cos, sin);
        }
    }

    // Front sprites into the bound, cleared Art target.
    internal void DrawArt(GraphicsDevice device, Effect effect, int targetWidth, int targetHeight)
    {
        DollPixelArt.Set(effect, "uWorldViewProjection", Matrix.CreateOrthographicOffCenter(0, targetWidth, targetHeight, 0, -1, 1));
        DrawSprites(device, effect, DollStratum.Front, Vector2.Zero, true);
    }

    // Back sprites straight into the caller's target in world space: vertices in screen px, `view` is
    // Main.GameViewMatrix.TransformationMatrix (zoom about the screen centre).
    internal void DrawBack(GraphicsDevice device, Effect effect, Vector2 screenPosition, Matrix view)
    {
        DollPixelArt.Set(effect, "uWorldViewProjection",
            view * Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
        DrawSprites(device, effect, DollStratum.Back, screenPosition, false);
    }

    private void DrawSprites(GraphicsDevice device, Effect effect, DollStratum stratum, Vector2 screenPosition, bool layer)
    {
        int count = Sort(stratum);
        for (int n = 0; n < count; n++)
        {
            ref SpriteCommand s = ref sprites[order[n]];
            Texture2D texture = s.Texture!;
            BuildQuad(in s, screenPosition, layer);
            DollSpriteFx fx = s.Fx;
            DollPixelArt.Set(effect, "spriteSource", new Vector4(s.Source.X, s.Source.Y, s.Source.Width, s.Source.Height));
            DollPixelArt.Set(effect, "spriteSize", new Vector2(texture.Width, texture.Height));
            DollPixelArt.Set(effect, "spriteFx", new Vector4(Math.Clamp(fx.Flash, 0f, 1f), Math.Clamp(fx.Dissolve, 0f, 1f),
                Math.Clamp(fx.Hide, 0f, 1f), Math.Clamp(fx.Fade, 0f, 1f)));
            DollPixelArt.Set(effect, "spriteStyle", new Vector4(fx.Silhouette is { } tone ? (float)tone : -1f,
                (Hash(fx.Seed) & 0xFFFF) / 65536f, fx.HideFromBottom ? 1f : 0f, 0f));
            DollPixelArt.Set(effect, "spriteSampling", new Vector4(layer ? 1f : 2f, s.Rotated ? 1f : 0f, 0f, 0f));
            device.Textures[0] = texture;
            device.SamplerStates[0] = SamplerState.PointClamp;
            DollPixelArt.Apply(effect, "SpritePass");
            device.DrawUserPrimitives(PrimitiveType.TriangleList, spriteQuad, 0, 2);
        }
    }

    // The sprite's quad (grown by one texel) with the cell -> texel basis. Layer cells are dots; direct
    // cells are world px. texel = pivotTexel + F R(-rotation) (world - pivotWorld) / 2.
    private void BuildQuad(in SpriteCommand s, Vector2 screenPosition, bool layer)
    {
        Corners(s.PivotWorld, s.PivotTexel, s.Cos, s.Sin, s.Flip, s.Source.Width, s.Source.Height, 1);
        float fx = (s.Flip & DollFlip.Horizontal) != 0 ? -1 : 1, fy = (s.Flip & DollFlip.Vertical) != 0 ? -1 : 1;
        // rows of F R(-rotation), per world px
        float m00 = fx * s.Cos * .5f, m01 = fx * s.Sin * .5f, m10 = -fy * s.Sin * .5f, m11 = fy * s.Cos * .5f;
        float cell = layer ? 1f / DotScale : 1f;
        float minX = float.MaxValue, minY = float.MaxValue;
        for (int i = 0; i < 4; i++)
        {
            NVector2 c = corners[i];
            float x = layer ? (c.X - origin.X) * DotScale : c.X, y = layer ? (c.Y - origin.Y) * DotScale : c.Y;
            minX = MathF.Min(minX, x); minY = MathF.Min(minY, y);
        }
        float anchorX = MathF.Floor(minX), anchorY = MathF.Floor(minY);
        // world position of the anchor cell's origin, relative to the pivot (small numbers, exact for snapped art)
        float worldX = layer ? origin.X - s.PivotWorld.X + anchorX * cell : anchorX - s.PivotWorld.X;
        float worldY = layer ? origin.Y - s.PivotWorld.Y + anchorY * cell : anchorY - s.PivotWorld.Y;
        Vector4 shape = new(m00 * cell, m01 * cell, m10 * cell, m11 * cell);
        Vector4 style = new(s.PivotTexel.X + m00 * worldX + m01 * worldY, s.PivotTexel.Y + m10 * worldX + m11 * worldY, 0f, 0f);
        for (int i = 0; i < 4; i++)
        {
            NVector2 c = corners[i];
            float x = layer ? (c.X - origin.X) * DotScale : c.X, y = layer ? (c.Y - origin.Y) * DotScale : c.Y;
            Vector3 position = layer ? new Vector3(x, y, 0f) : new Vector3(c.X - screenPosition.X, c.Y - screenPosition.Y, 0f);
            quadCorners[i] = new DollPixelVertex
            {
                Position = position,
                Local = new Vector4(x - anchorX, y - anchorY, 0f, 0f),
                Shape = shape,
                Style = style,
            };
        }
        spriteQuad[0] = quadCorners[0];
        spriteQuad[1] = quadCorners[1];
        spriteQuad[2] = quadCorners[2];
        spriteQuad[3] = quadCorners[2];
        spriteQuad[4] = quadCorners[1];
        spriteQuad[5] = quadCorners[3];
    }

    // Degraded fallback (no shader or targets): plain point-sampled sprites at 2 px per texel through
    // the caller's SpriteBatch (begun with PointClamp and the game view). Keeps hide and fade only.
    internal void DrawSpritesDirect(SpriteBatch batch, DollStratum stratum, Vector2 screenPosition)
    {
        int count = Sort(stratum);
        for (int n = 0; n < count; n++)
        {
            ref SpriteCommand s = ref sprites[order[n]];
            Rectangle source = s.Source;
            NVector2 pivot = s.PivotTexel;
            int hidden = (int)MathF.Ceiling(Math.Clamp(s.Fx.Hide, 0f, 1f) * source.Height - .001f);
            if (hidden >= source.Height) continue;
            if (!s.Fx.HideFromBottom)
            {
                source.Y += hidden;
                pivot.Y -= hidden;
            }
            source.Height -= hidden;
            NVector2 anchor = DollSpritePlacement.MirrorAnchor(pivot, new NVector2(source.Width, source.Height), s.Flip);
            float opacity = MathF.Floor((1f - Math.Clamp(s.Fx.Fade, 0f, 1f)) * 4f + .5f) / 4f;
            if (opacity <= 0f) continue;
            SpriteEffects effects = ((s.Flip & DollFlip.Horizontal) != 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None)
                | ((s.Flip & DollFlip.Vertical) != 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
            batch.Draw(s.Texture!, new Vector2(s.PivotWorld.X - screenPosition.X, s.PivotWorld.Y - screenPosition.Y), source,
                Color.White * opacity, s.Rotation, new Vector2(anchor.X, anchor.Y), DollSpritePlacement.WorldPerTexel, effects, 0f);
        }
    }

    // Insertion sort by (depth, record order) into `order`; returns the stratum's count. No allocation.
    private int Sort(DollStratum stratum)
    {
        int count = 0;
        for (int i = 0; i < spriteCount; i++)
        {
            if (sprites[i].Stratum != stratum) continue;
            int j = count++;
            while (j > 0 && sprites[order[j - 1]].Depth > sprites[i].Depth)
            {
                order[j] = order[j - 1];
                j--;
            }
            order[j] = i;
        }
        return count;
    }

    // ---- Light ---------------------------------------------------------------------------------

    // Draws the frame's light into the bound, cleared Light target (premultiplied AlphaBlend).
    internal void DrawLight(GraphicsDevice device, Effect effect, int targetWidth, int targetHeight)
    {
        Matrix projection = Matrix.CreateOrthographicOffCenter(0, targetWidth, targetHeight, 0, -1, 1);
        if (batchCount > 0)
        {
            var context = new DollEnergyContext(projection, DotOrigin, Clock, Fraction, Reduced, effect);
            int count = 0;
            for (int i = 0; i < batchCount; i++)
            {
                int j = count++;
                while (j > 0 && batches[batchOrder[j - 1]].Depth > batches[i].Depth)
                {
                    batchOrder[j] = batchOrder[j - 1];
                    j--;
                }
                batchOrder[j] = i;
            }
            for (int n = 0; n < count; n++)
            {
                EnergyBatch batch = batches[batchOrder[n]];
                if (batch.Material is null || batch.Count < 3) continue;
                try
                {
                    if (batch.Material.Apply(device, in context, batch.Pass))
                        device.DrawUserPrimitives(PrimitiveType.TriangleList, energy, batch.Start, batch.Count / 3);
                }
                catch (Exception exception)
                {
                    MaterialError ??= exception;
                }
                device.BlendState = BlendState.AlphaBlend;
                device.DepthStencilState = DepthStencilState.None;
                device.RasterizerState = RasterizerState.CullNone;
            }
        }
        DollPixelArt.Set(effect, "uWorldViewProjection", projection);
        DollPixelArt.Set(effect, "travel", (float)(Clock * TravelSpeed % 3600.0));
        if (lineCount > 0)
        {
            DollPixelArt.Apply(effect, "LinePass");
            device.DrawUserPrimitives(PrimitiveType.TriangleList, lines, 0, lineCount * 2);
        }
        if (rectCount > 0)
        {
            DollPixelArt.Apply(effect, "FlatPass");
            device.DrawUserPrimitives(PrimitiveType.TriangleList, rects, 0, rectCount * 2);
        }
    }

    internal void Dot(Vector2 at, DollTone tone, int size = 1, float alpha = 1)
    {
        if (!(alpha > .004f) || !Finite(at)) return;
        size = Math.Clamp(size, 1, 8);
        Point c = Cell(at);
        Rect(c.X - (size - 1) / 2, c.Y - (size - 1) / 2, size, size, (byte)tone, alpha);
    }

    // A straight line, thickness 1-3 dots, exact under any slope.
    internal void Line(Vector2 a, Vector2 b, DollTone tone, int thickness = 1, float alpha = 1)
        => LineCore(KindLine, a, b, (byte)tone, thickness, alpha, 1f);

    internal void Ring(Vector2 center, float radius, DollTone tone, int thickness = 1, float alpha = 1)
        => ArcCore(KindArc, center, radius, 0f, MathF.Tau, (byte)tone, thickness, alpha, 1f);

    // An arc from angle a0 toward a1 (either direction, at most a full turn).
    internal void Arc(Vector2 center, float radius, float a0, float a1, DollTone tone, int thickness = 1, float alpha = 1)
        => ArcCore(KindArc, center, radius, a0, a1, (byte)tone, thickness, alpha, 1f);

    // A forecast: one-dot pearl-violet hairline from a toward b, drawn to `progress`, with white heads
    // travelling toward b.
    internal void Forecast(Vector2 a, Vector2 b, float progress = 1, float alpha = 1)
        => LineCore(KindForecastLine, a, b, (byte)DollTone.PearlViolet, 1, alpha, progress);

    // A forecast ring or arc from a0 toward a1, drawn to `progress`.
    internal void ForecastArc(Vector2 center, float radius, float a0, float a1, float progress = 1, float alpha = 1)
        => ArcCore(KindForecastArc, center, radius, a0, a1, (byte)DollTone.PearlViolet, 1, alpha, progress);

    private void LineCore(byte kind, Vector2 a, Vector2 b, byte tone, int thickness, float alpha, float progress)
    {
        if (!(alpha > .004f) || !(progress > 0f) || !Finite(a) || !Finite(b)) return;
        if (lineCount >= MaxLines) { Dropped++; return; }
        thickness = Math.Clamp(thickness, 1, 3);
        Vector2 ca = Floor(ToDot(a)), delta = Floor(ToDot(b)) - ca;
        float length = delta.Length();
        Vector2 direction = length > .5f ? delta / length : Vector2.UnitX, normal = new(-direction.Y, direction.X);
        float margin = thickness * .75f + 1.5f;
        Vector2 from = ca + new Vector2(.5f) - direction * margin, to = ca + new Vector2(.5f) + delta + direction * margin;
        Vector2 p0 = from - normal * margin, p1 = to - normal * margin, p2 = from + normal * margin, p3 = to + normal * margin;
        if (!IncludeQuad(p0, p1, p2, p3)) return;
        Quad(lines, lineCount++ * 6, p0, p1, p2, p3, ca, new Vector2(kind, tone),
            new Vector4(delta.X, delta.Y, length, 0f), new Vector4(thickness, MathF.Min(alpha, 1f), MathF.Min(progress, 1f), 0f));
    }

    private void ArcCore(byte kind, Vector2 center, float radius, float a0, float a1, byte tone, int thickness, float alpha, float progress)
    {
        float sweep = Math.Clamp(a1 - a0, -MathF.Tau, MathF.Tau);
        if (!(alpha > .004f) || !(progress > 0f) || !(radius >= 0f) || !Finite(center) || !float.IsFinite(a0)
            || !(MathF.Abs(sweep) > 1e-4f)) return;
        if (lineCount >= MaxLines) { Dropped++; return; }
        thickness = Math.Clamp(thickness, 1, 3);
        Vector2 cell = Floor(ToDot(center)), middle = cell + new Vector2(.5f);
        float r = radius * DotScale, extent = r + thickness + 1.5f;
        Vector2 p0 = middle + new Vector2(-extent, -extent), p1 = middle + new Vector2(extent, -extent);
        Vector2 p2 = middle + new Vector2(-extent, extent), p3 = middle + new Vector2(extent, extent);
        if (!IncludeQuad(p0, p1, p2, p3)) return;
        Quad(lines, lineCount++ * 6, p0, p1, p2, p3, cell, new Vector2(kind, tone),
            new Vector4(r, a0, sweep, 0f), new Vector4(thickness, MathF.Min(alpha, 1f), MathF.Min(progress, 1f), 0f));
    }

    // Deterministic analytic debris (no retained particles): `count` pieces born at `origin`, evaluated at
    // `t` ticks of `life`, initial `speed` px/tick spread over `spread` radians around `heading`, `gravity`
    // px/tick^2. Reduced Effects halves the count. Pieces vanish in dithered order near the end of life.
    internal void Burst(Vector2 origin, int seed, int count, float t, float life, float speed, float gravity, DollShardKind kind,
        float spread = MathF.Tau, float heading = 0)
    {
        if (!(t >= 0f) || !(life > 0f) || t > life || count <= 0 || !Finite(origin) || !float.IsFinite(speed)
            || !float.IsFinite(gravity) || !float.IsFinite(spread) || !float.IsFinite(heading)) return;
        count = Reduced ? (count + 1) / 2 : count;
        if (count > MaxBurstPieces)
        {
            // Truncated to the cap: counted once per call.
            count = MaxBurstPieces;
            Dropped++;
        }
        (float drag, float fall) = kind switch
        {
            DollShardKind.Porcelain => (.035f, 1f),
            DollShardKind.Brass => (.03f, 1.2f),
            DollShardKind.Pearl => (.08f, .25f),
            _ => (.05f, .5f),
        };
        float travel = (1f - MathF.Exp(-drag * t)) / drag, keep = MathF.Exp(-drag * t);
        for (int k = 0; k < count; k++)
        {
            var random = new Random32(seed * 7919 + k * 104729 + (int)kind * 31337);
            float lifeK = life * random.Range(.55f, 1f);
            float angle = heading + (random.Next() - .5f) * spread, v = speed * random.Range(.35f, 1f);
            float turn = random.Range(0f, MathF.Tau) + random.Range(-.35f, .35f) * t;
            float pick = random.Next(), vanish = random.Next();
            if (t > lifeK) continue;
            float age = t / lifeK;
            if (age > .7f && vanish < (age - .7f) / .3f) continue;
            Vector2 velocity = new(MathF.Cos(angle) * v, MathF.Sin(angle) * v);
            Vector2 world = origin + velocity * travel + new Vector2(0f, .5f * gravity * fall * t * t);
            Vector2 now = velocity * keep + new Vector2(0f, gravity * fall * t);
            Point head = Cell(world);
            switch (kind)
            {
                case DollShardKind.Porcelain:
                {
                    // a 2-3 dot sliver spinning in place; fresh pieces flash bone
                    int length = 2 + (int)(pick * 2f);
                    Point tail = ToPoint(ToDot(world) + new Vector2(MathF.Cos(turn), MathF.Sin(turn)) * (length - 1));
                    byte body = (byte)(age < .15f ? DollTone.Bone : pick < .5f ? DollTone.Porcelain : DollTone.PorcelainLight);
                    Segment(head, tail, body, 1f);
                    Rect(tail.X, tail.Y, 1, 1, (byte)DollTone.PorcelainShade, 1f);
                    break;
                }
                case DollShardKind.Brass:
                {
                    int quarter = (int)MathF.Floor(turn / (MathF.PI / 2f)) & 3;
                    bool square = pick < .3f;
                    int w = square || quarter % 2 == 0 ? 2 : 1, h = square || quarter % 2 == 1 ? 2 : 1;
                    Rect(head.X, head.Y, w, h, (byte)DollTone.Brass, 1f);
                    if ((quarter + k) % 2 == 0) Rect(head.X + w - 1, head.Y, 1, 1, (byte)DollTone.BrassLight, 1f);
                    break;
                }
                case DollShardKind.Pearl:
                {
                    // pearl beads sway and twinkle
                    Point at = Cell(world + new Vector2(MathF.Sin(t * .15f + k) * 3f, 0f));
                    bool twinkle = (k + (int)(t / 4f)) % 3 == 0;
                    Rect(at.X, at.Y, 1, 1, (byte)(twinkle ? DollTone.White : age < .5f ? DollTone.PearlViolet : DollTone.Pearl), 1f);
                    break;
                }
                default:
                {
                    if (!Reduced && now.LengthSquared() > 1.44f)
                        Segment(ToPoint(ToDot(world - now * 1.4f)), head, (byte)(age < .5f ? DollTone.Lilac : DollTone.Violet), 1f);
                    Rect(head.X, head.Y, 1, 1, (byte)(age < .25f ? DollTone.White : age < .6f ? DollTone.PearlViolet : DollTone.Lilac), 1f);
                    break;
                }
            }
        }
    }

    // ---- Energy hook ---------------------------------------------------------------------------

    // Reserves `triangles` triangles (3 vertices each) drawn with `material`/`pass` into the Light target,
    // positions in dot space (ToDot). Empty when over budget (counted). Consecutive calls with the same
    // material, pass and depth share one draw.
    internal Span<DollPixelVertex> Energy(IDollEnergyMaterial material, int pass, int triangles, sbyte depth = 0)
    {
        int vertices = triangles * 3;
        if (material is null || triangles <= 0) return Span<DollPixelVertex>.Empty;
        if (energyCount + vertices > MaxEnergyVertices) { Dropped++; return Span<DollPixelVertex>.Empty; }
        bool extend = batchCount > 0 && ReferenceEquals(batches[batchCount - 1].Material, material)
            && batches[batchCount - 1].Pass == pass && batches[batchCount - 1].Depth == depth
            && batches[batchCount - 1].Start + batches[batchCount - 1].Count == energyCount;
        if (!extend)
        {
            if (batchCount >= MaxEnergyBatches) { Dropped++; return Span<DollPixelVertex>.Empty; }
            batches[batchCount++] = new EnergyBatch { Material = material, Pass = pass, Start = energyCount, Depth = depth };
        }
        batches[batchCount - 1].Count += vertices;
        Span<DollPixelVertex> span = energy.AsSpan(energyCount, vertices);
        span.Clear();
        energyCount += vertices;
        return span;
    }

    // A band of `width` world px from a to b. L = (along 0..1, across -1..1, length, half width) in dots,
    // S.xy = dot position, T = style. The quad is one dot wider on every side than the band.
    internal bool EnergyQuad(IDollEnergyMaterial material, int pass, Vector2 a, Vector2 b, float width, Vector4 style, sbyte depth = 0)
    {
        if (!Finite(a) || !Finite(b) || !(width > 0f) || !float.IsFinite(width)) return false;
        Span<DollPixelVertex> quad = Energy(material, pass, 2, depth);
        if (quad.IsEmpty) return false;
        Vector2 da = ToDot(a), db = ToDot(b), delta = db - da;
        float length = delta.Length(), half = width * DotScale * .5f, grown = half + 1f;
        Vector2 direction = length > 1e-3f ? delta / length : Vector2.UnitX, normal = new(-direction.Y, direction.X);
        float along0 = length > 0 ? -1f / length : 0f, along1 = length > 0 ? 1f + 1f / length : 1f;
        float across = grown / MathF.Max(half, .5f);
        Vector2 back = da - direction, front = db + direction;
        EnergyVertex(ref quad[0], back - normal * grown, new Vector4(along0, -across, length, half), style);
        EnergyVertex(ref quad[1], front - normal * grown, new Vector4(along1, -across, length, half), style);
        EnergyVertex(ref quad[2], back + normal * grown, new Vector4(along0, across, length, half), style);
        quad[3] = quad[2];
        quad[4] = quad[1];
        EnergyVertex(ref quad[5], front + normal * grown, new Vector4(along1, across, length, half), style);
        return true;
    }

    // A band of `width` world px along a polyline spine (2..MaxStripPoints points). Joints use the
    // averaged normal; L.x runs 0..1 over the whole spine.
    internal bool EnergyStrip(IDollEnergyMaterial material, int pass, ReadOnlySpan<Vector2> spine, float width, Vector4 style, sbyte depth = 0)
    {
        int points = spine.Length;
        if (points > MaxStripPoints) { Dropped++; return false; }
        if (points < 2 || !(width > 0f) || !float.IsFinite(width)) return false;
        float total = 0f;
        for (int i = 0; i < points; i++)
        {
            if (!Finite(spine[i])) return false;
            if (i > 0) total += Vector2.Distance(ToDot(spine[i - 1]), ToDot(spine[i]));
        }
        Span<DollPixelVertex> strip = Energy(material, pass, (points - 1) * 2, depth);
        if (strip.IsEmpty) return false;
        float half = width * DotScale * .5f, grown = half + 1f, across = grown / MathF.Max(half, .5f), walked = 0f;
        Vector2 previousLeft = default, previousRight = default;
        Vector4 previousLocal = default;
        for (int i = 0; i < points; i++)
        {
            Vector2 p = ToDot(spine[i]);
            Vector2 inbound = i > 0 ? p - ToDot(spine[i - 1]) : ToDot(spine[1]) - p;
            Vector2 outbound = i < points - 1 ? ToDot(spine[i + 1]) - p : inbound;
            Vector2 tangent = SafeNormal(inbound) + SafeNormal(outbound);
            tangent = tangent.LengthSquared() > 1e-6f ? Vector2.Normalize(tangent) : SafeNormal(outbound);
            Vector2 normal = new(-tangent.Y, tangent.X);
            if (i > 0) walked += inbound.Length();
            float along = total > 0f ? walked / total : 0f;
            Vector2 left = p - normal * grown, right = p + normal * grown;
            if (i > 0)
            {
                int v = (i - 1) * 6;
                EnergyVertex(ref strip[v], previousLeft, previousLocal with { Y = -across }, style);
                EnergyVertex(ref strip[v + 1], left, new Vector4(along, -across, total, half), style);
                EnergyVertex(ref strip[v + 2], previousRight, previousLocal with { Y = across }, style);
                strip[v + 3] = strip[v + 2];
                strip[v + 4] = strip[v + 1];
                EnergyVertex(ref strip[v + 5], right, new Vector4(along, across, total, half), style);
            }
            previousLeft = left;
            previousRight = right;
            previousLocal = new Vector4(along, 0f, total, half);
        }
        return true;
    }

    private static void EnergyVertex(ref DollPixelVertex vertex, Vector2 position, Vector4 local, Vector4 style)
    {
        vertex.Position = new Vector3(position, 0f);
        vertex.Local = local;
        vertex.Shape = new Vector4(position.X, position.Y, 0f, 0f);
        vertex.Style = style;
    }

    private static Vector2 SafeNormal(Vector2 v) => v.LengthSquared() > 1e-8f ? Vector2.Normalize(v) : Vector2.UnitX;

    // ---- Shared plotting -----------------------------------------------------------------------

    // Bresenham, merged into runs along the major axis.
    private void Segment(Point a, Point b, byte tone, float alpha)
    {
        int dx = Math.Abs(b.X - a.X), dy = -Math.Abs(b.Y - a.Y);
        int sx = a.X < b.X ? 1 : -1, sy = a.Y < b.Y ? 1 : -1, error = dx + dy;
        bool horizontal = dx >= -dy;
        Point start = a, last = a;
        for (int guard = 0; guard < 2048; guard++)
        {
            if (guard > 0 && (horizontal ? a.Y != last.Y : a.X != last.X))
            {
                Run(start, last, tone, alpha);
                start = a;
            }
            last = a;
            if (a == b) break;
            int e2 = 2 * error;
            if (e2 >= dy) { error += dy; a.X += sx; }
            if (e2 <= dx) { error += dx; a.Y += sy; }
        }
        Run(start, last, tone, alpha);
    }

    private void Run(Point from, Point to, byte tone, float alpha)
        => Rect(Math.Min(from.X, to.X), Math.Min(from.Y, to.Y), Math.Abs(to.X - from.X) + 1, Math.Abs(to.Y - from.Y) + 1, tone, alpha);

    private void Rect(int x, int y, int w, int h, byte tone, float alpha)
    {
        if (rectCount >= MaxRects) { Dropped++; return; }
        if (!IncludeLight(x, y, x + w, y + h)) return;
        Color color = DollPixelArt.Palette[Math.Min((int)tone, DollPixelArt.Palette.Length - 1)] * Math.Clamp(alpha, 0f, 1f);
        int v = rectCount++ * 6;
        rects[v] = new VertexPositionColor(new Vector3(x, y, 0f), color);
        rects[v + 1] = new VertexPositionColor(new Vector3(x + w, y, 0f), color);
        rects[v + 2] = new VertexPositionColor(new Vector3(x, y + h, 0f), color);
        rects[v + 3] = rects[v + 2];
        rects[v + 4] = rects[v + 1];
        rects[v + 5] = new VertexPositionColor(new Vector3(x + w, y + h, 0f), color);
    }

    private static void Quad(DollPixelVertex[] buffer, int at, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
        Vector2 anchor, Vector2 local, Vector4 shape, Vector4 style)
    {
        buffer[at] = Prim(p0, anchor, local, shape, style);
        buffer[at + 1] = Prim(p1, anchor, local, shape, style);
        buffer[at + 2] = Prim(p2, anchor, local, shape, style);
        buffer[at + 3] = buffer[at + 2];
        buffer[at + 4] = buffer[at + 1];
        buffer[at + 5] = Prim(p3, anchor, local, shape, style);
    }

    private static DollPixelVertex Prim(Vector2 position, Vector2 anchor, Vector2 local, Vector4 shape, Vector4 style) => new()
    {
        Position = new Vector3(position, 0f),
        Local = new Vector4(position.X - anchor.X, position.Y - anchor.Y, local.X, local.Y),
        Shape = shape,
        Style = style,
    };

    private bool IncludeQuad(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        => IncludeLight(MathF.Min(MathF.Min(p0.X, p1.X), MathF.Min(p2.X, p3.X)), MathF.Min(MathF.Min(p0.Y, p1.Y), MathF.Min(p2.Y, p3.Y)),
            MathF.Max(MathF.Max(p0.X, p1.X), MathF.Max(p2.X, p3.X)), MathF.Max(MathF.Max(p0.Y, p1.Y), MathF.Max(p2.Y, p3.Y)));

    // Culls against the target (with outline/glow reach) and grows the Light bounds.
    private bool IncludeLight(float x0, float y0, float x1, float y1)
    {
        if (!(x1 >= -Reach && y1 >= -Reach && x0 <= width + Reach && y0 <= height + Reach)) return false;
        light.Add(x0, y0, x1, y1);
        return true;
    }

    private Rectangle Bounds(Area area, int reach)
    {
        if (area.Empty) return Rectangle.Empty;
        int x0 = Math.Max(0, (int)MathF.Floor(area.MinX) - reach), y0 = Math.Max(0, (int)MathF.Floor(area.MinY) - reach);
        int x1 = Math.Min(width, (int)MathF.Ceiling(area.MaxX) + reach), y1 = Math.Min(height, (int)MathF.Ceiling(area.MaxY) + reach);
        return x1 > x0 && y1 > y0 ? new Rectangle(x0, y0, x1 - x0, y1 - y0) : Rectangle.Empty;
    }

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
