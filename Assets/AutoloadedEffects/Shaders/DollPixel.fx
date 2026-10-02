// Doll weapon pixel layer. Original repository material.
// Shared passes of the client-only Doll weapon layer: one art dot is one pixel of a half-resolution,
// world-aligned target (1 dot = 2 world px), quantized to the Doll palette below. Inputs are
// presentation state only; nothing here decides hits.
//   FlatPass                 CPU-plotted dot runs (VertexPositionColor, premultiplied).
//   LinePass                 dot-exact lines, arcs/rings and forecast hairlines with travelling dots.
//   RampPass                 the built-in energy material: an intensity quantized to the light ramp.
//   SpritePass               point-sampled pixel sprites (rotation/flip baked into the basis) with fx.
//   CompositeArtPass         point-upscales the Art target.
//   CompositeLightPass       point-upscales the Light target with a one-dot ink outline where Art and
//                            Light are both empty and a bounded glow on tones at or above lilac.
//   CompositeLightPlainPass  the same without the glow (Reduced Effects).
//
// Vertex layout (C# DollPixelVertex): POSITION0 float3, TEXCOORD0 L, TEXCOORD1 S, TEXCOORD2 T (float4).
//   LinePass   L = (x - anchor.x, y - anchor.y, kind, tone): offsets in dots from an integer anchor
//              cell, so floor(L.xy + .25) is the fragment's exact dot whatever the rasterizer's
//              half-pixel convention. kind 0 line, 1 arc, 2 forecast line, 3 forecast arc.
//              Lines: S = (dx, dy, length, 0), the whole-dot offset to the far end and the true length.
//              Arcs:  S = (radius, start, sweep, 0) in dots / radians about the anchor cell's centre.
//              T = (thickness 1-3, alpha, progress 0..1, 0).
//   SpritePass L.xy = cell offset from the anchor cell (dots in the layer, world px when drawn
//              directly); S = the two rows of the cell -> texel basis; T.xy = texel coordinate of the
//              anchor cell's origin.
//   Energy     L = (along 0..1, across -1..1, length, half width) in dots; S.xy = dot-space position
//              (floor(S.xy + .25) + dotOrigin is the absolute world dot); S.zw and T belong to the
//              material. RampPass reads T = (intensity 0..1, or negative for void; alpha; 0; 0).
matrix uWorldViewProjection;
float2 dotOrigin;     // absolute world dot of target cell (0, 0)
float travel;         // forecast travelling-dot phase, in dots
float2 texel;         // composite: 1 / target size
float glowStrength;
float4 spriteSource;  // source rectangle x, y, width, height in texels
float2 spriteSize;    // texture size in texels
float4 spriteFx;      // flash 0..1, dissolve 0..1, hidden rows 0..1, fade 0..1
float4 spriteStyle;   // silhouette tone (-1 none), dissolve seed 0..1, hide from bottom (0/1), 0
float4 spriteSampling;// cells per dot (1 layer, 2 direct), rotated (0/1), 0, 0

sampler sceneTex : register(s0);
sampler artTex : register(s1);

static const float Tau = 6.2831853;
static const float Spacing = 12;

// BEGIN DOLL PALETTE (DollTone order; identical in every Doll*.fx and DollPixelArt.cs)
static const float3 Ink = float3(18, 16, 23) / 255;              // 0 #121017
static const float3 IronDark = float3(29, 26, 34) / 255;         // 1 #1d1a22
static const float3 Iron = float3(48, 42, 41) / 255;             // 2 #302a29
static const float3 IronLight = float3(73, 64, 74) / 255;        // 3 #49404a
static const float3 PorcelainShade = float3(156, 128, 112) / 255; // 4 #9c8070
static const float3 Porcelain = float3(208, 182, 158) / 255;     // 5 #d0b69e
static const float3 PorcelainLight = float3(244, 227, 206) / 255; // 6 #f4e3ce
static const float3 Bone = float3(252, 244, 230) / 255;          // 7 #fcf4e6
static const float3 PearlGrey = float3(201, 196, 201) / 255;     // 8 #c9c4c9
static const float3 Pearl = float3(225, 220, 224) / 255;         // 9 #e1dce0
static const float3 BrassShade = float3(104, 72, 40) / 255;      // 10 #684828
static const float3 Brass = float3(160, 123, 72) / 255;          // 11 #a07b48
static const float3 BrassLight = float3(213, 178, 121) / 255;    // 12 #d5b279
static const float3 Plum = float3(48, 24, 64) / 255;             // 13 #301840
static const float3 PlumLight = float3(90, 44, 156) / 255;       // 14 #5a2c9c
static const float3 Violet = float3(148, 88, 255) / 255;         // 15 #9458ff
static const float3 Lilac = float3(185, 156, 255) / 255;         // 16 #b99cff
static const float3 PearlViolet = float3(221, 216, 248) / 255;   // 17 #ddd8f8
static const float3 White = float3(255, 255, 255) / 255;         // 18 #ffffff
static const float3 Ruby = float3(140, 20, 28) / 255;            // 19 #8c141c
// END DOLL PALETTE

float3 Palette(float tone)
{
    return tone < 0.5 ? Ink : tone < 1.5 ? IronDark : tone < 2.5 ? Iron : tone < 3.5 ? IronLight
        : tone < 4.5 ? PorcelainShade : tone < 5.5 ? Porcelain : tone < 6.5 ? PorcelainLight : tone < 7.5 ? Bone
        : tone < 8.5 ? PearlGrey : tone < 9.5 ? Pearl : tone < 10.5 ? BrassShade : tone < 11.5 ? Brass
        : tone < 12.5 ? BrassLight : tone < 13.5 ? Plum : tone < 14.5 ? PlumLight : tone < 15.5 ? Violet
        : tone < 16.5 ? Lilac : tone < 17.5 ? PearlViolet : tone < 18.5 ? White : Ruby;
}

// The light ramp, darkest to hottest: plum, plum light, violet, lilac, pearl-violet, bone, white.
float3 Ramp(float band)
{
    return band < 0.5 ? Plum : band < 1.5 ? PlumLight : band < 2.5 ? Violet : band < 3.5 ? Lilac
        : band < 4.5 ? PearlViolet : band < 5.5 ? Bone : White;
}

float Bayer2(float2 a)
{
    a = floor(a);
    return frac(a.x / 2 + a.y * a.y * 0.75);
}

float Bayer4(float2 a)
{
    return Bayer2(0.5 * a) * 0.25 + Bayer2(a);
}

float Hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float ValueNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    f = f * f * (3 - 2 * f);
    return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + 1), f.x), f.y);
}

float InkTone(float3 rgb)
{
    return step(dot(abs(rgb - Ink), float3(1, 1, 1)), 0.05);
}

struct PrimIn { float4 P : POSITION0; float4 L : TEXCOORD0; float4 S : TEXCOORD1; float4 T : TEXCOORD2; };
struct PrimOut { float4 P : POSITION0; float4 L : TEXCOORD0; float4 S : TEXCOORD1; float4 T : TEXCOORD2; };

PrimOut PrimVS(PrimIn v)
{
    PrimOut o;
    o.P = mul(v.P, uWorldViewProjection);
    o.P.z = 0;
    o.L = v.L;
    o.S = v.S;
    o.T = v.T;
    return o;
}

// Lines are anchored at the first end's cell with S.xy the whole-dot offset to the other end, so the
// major-axis test is exact (no gaps, no doubled steps). Arcs use an octant distance about the anchor
// cell's centre and an angle window. Forecasts are one-dot pearl-violet hairlines whose white heads
// travel along them every Spacing dots; progress draws them from the first end.
float4 LinePS(PrimOut i) : COLOR0
{
    float2 k = floor(i.L.xy + 0.25);
    float kind = i.L.z, tone = i.L.w;
    float thick = i.T.x, alpha = i.T.y, progress = saturate(i.T.z);
    float keep = 0, along = 0;
    if (kind < 0.5 || (kind > 1.5 && kind < 2.5))
    {
        float2 d = floor(i.S.xy + 0.5);
        float xMajor = step(abs(d.y), abs(d.x));
        float dM = lerp(d.y, d.x, xMajor), dm = lerp(d.x, d.y, xMajor);
        float cM = lerp(k.y, k.x, xMajor), cm = lerp(k.x, k.y, xMajor);
        float sg = dM < 0 ? -1 : 1, aM = abs(dM);
        float t = cM * sg;
        float inRange = step(-0.01, t) * step(t, aM * progress + 0.01);
        // integer form of -thick/2 <= minor offset < thick/2
        float x2 = 2 * (cm * dM - cM * dm) * sg;
        float onLine = inRange * step(-thick * aM - 0.5, x2) * step(x2, thick * aM - 0.5);
        keep = aM < 0.5 ? step(abs(k.x) + abs(k.y), 0.5) : onLine;
        along = t * i.S.z / max(aM, 1);
    }
    else
    {
        float r = i.S.x, start = i.S.y, sweep = i.S.z, h = 0.5 * thick;
        float2 q = abs(k);
        float delta = q.y >= q.x ? q.y - sqrt(max(r * r - q.x * q.x, 0)) : q.x - sqrt(max(r * r - q.y * q.y, 0));
        float onRing = r < 0.75 ? step(q.x + q.y, 0.5) : step(-h, delta) * (1 - step(h, delta));
        // the window runs from start in the sweep's direction (y points down: positive turns clockwise)
        float rel = frac((sweep < 0 ? -1 : 1) * (atan2(k.y, k.x) - start) / Tau) * Tau;
        float extent = abs(sweep) * progress;
        float inArc = extent >= Tau - 0.001 ? 1 : step(rel, extent + 0.5 / max(r, 1));
        keep = onRing * inArc;
        along = rel * max(r, 1);
    }
    float forecast = step(1.5, kind);
    float head = step(frac((along - travel) / Spacing) * Spacing, 1) * forecast;
    tone = head > 0.5 ? 18 : tone;
    clip(keep * step(0.004, alpha) - 0.5);
    return float4(Palette(tone) * alpha, alpha);
}

// Built-in energy material: T.x intensity is quantized to the seven ramp tones with a world-stable
// 4x4 Bayer dither across band edges; faint intensity dissolves into empty dots. A negative intensity
// is void: ink at its core (below -0.5) and plum toward its rim, opaque enough to hide what is behind.
float4 RampPS(PrimOut i) : COLOR0
{
    float2 cell = floor(i.S.xy + 0.25) + dotOrigin;
    float alpha = saturate(i.T.y);
    float dither = Bayer4(cell) - 0.5;
    float v = saturate(i.T.x) * 7 + dither * 0.9;
    float band = clamp(floor(v), 0, 6);
    float isVoid = step(i.T.x, -0.001);
    float3 voidTone = i.T.x + dither * 0.3 < -0.5 ? Ink : Plum;
    clip(min(max(v - 0.5, isVoid - 0.5), alpha - 0.004));
    return float4((isVoid > 0.5 ? voidTone : Ramp(band)) * alpha, alpha);
}

// One texel of the sprite's source rectangle with hidden rows applied (straight colour, alpha 0 or 1).
float4 SpriteTexel(float2 j)
{
    float width = spriteSource.z, height = spriteSource.w;
    float inside = step(0, j.x) * step(0, j.y) * step(j.x, width - 1) * step(j.y, height - 1);
    float row = spriteStyle.z > 0.5 ? height - 1 - j.y : j.y;
    float shown = inside * step(spriteFx.z * height, row + 0.001);
    float2 jj = clamp(j, 0, float2(width, height) - 1);
    float4 c = tex2Dlod(sceneTex, float4((spriteSource.xy + jj + 0.5) / spriteSize, 0, 0));
    return float4(c.rgb / max(c.a, 0.004), shown * step(0.5, c.a));
}

// Porcelain-crumble value of a texel (0..1): texels below the dissolve amount are gone.
float Crumble(float2 j)
{
    float2 seed = float2(spriteStyle.y * 97.13, spriteStyle.y * 41.7);
    return 0.65 * ValueNoise(j * 0.25 + seed) + 0.35 * Hash(j + seed * 3.1);
}

float InkTexel(float2 j)
{
    float4 c = SpriteTexel(j);
    return c.a * InkTone(c.rgb);
}

// Point sampling, one texel per dot. A rotated sprite keeps its ink ring closed: a coloured dot that
// touches an empty dot (4-neighbourhood, through the same basis) turns ink when its texel lies next to
// the art's ink, which is where a skipped outline texel exposes it; a colour the art itself puts against
// transparency (a brass hole rim) stays. A coloured dot between two diagonally touching ink texels turns
// ink too. Axis-aligned sprites are exact and left as authored.
float4 SpritePS(PrimOut i) : COLOR0
{
    float2 p = floor(i.L.xy + 0.25) + 0.5;
    float2 u = float2(dot(i.S.xy, p), dot(i.S.zw, p)) + i.T.xy;
    float2 j = floor(u);
    float4 c = SpriteTexel(j);
    // Dissolve removes texels in crumble order (no uniform branch: every dot pays one noise lookup);
    // the crumbling edge of the body shows porcelain shade.
    float crumble = Crumble(j), dissolving = step(0.0001, spriteFx.y);
    clip(c.a - 0.5 - dissolving * step(crumble, spriteFx.y));
    float colour = 1 - InkTone(c.rgb);
    c.rgb = dissolving * step(crumble, spriteFx.y + 0.1) * colour > 0.5 ? PorcelainShade : c.rgb;
    if (spriteSampling.y > 0.5 && colour > 0.5)
    {
        float2 dx = float2(i.S.x, i.S.z) * spriteSampling.x, dy = float2(i.S.y, i.S.w) * spriteSampling.x;
        float open = (1 - SpriteTexel(floor(u + dx)).a) + (1 - SpriteTexel(floor(u - dx)).a)
            + (1 - SpriteTexel(floor(u + dy)).a) + (1 - SpriteTexel(floor(u - dy)).a);
        float inked = InkTexel(j + float2(1, 0)) + InkTexel(j - float2(1, 0)) + InkTexel(j + float2(0, 1)) + InkTexel(j - float2(0, 1));
        float pinch = max(InkTexel(floor(u + float2(-0.25, -0.25))) * InkTexel(floor(u + float2(0.25, 0.25))),
            InkTexel(floor(u + float2(0.25, -0.25))) * InkTexel(floor(u + float2(-0.25, 0.25))));
        float toInk = max(step(0.5, open) * step(0.5, inked), pinch);
        c.rgb = toInk > 0.5 ? Ink : c.rgb;
        colour *= 1 - toInk;
    }
    // A silhouette keeps the ink ring and fills the rest with one tone.
    c.rgb = spriteStyle.x >= 0 && colour > 0.5 ? Palette(spriteStyle.x) : c.rgb;
    // Flash steps toward pearl and white in palette steps; ink stays.
    float flash = spriteFx.x;
    float level = dot(c.rgb, float3(0.3, 0.59, 0.11)) + flash * 1.5;
    float3 flashed = level > 1.6 ? White : level > 1.15 ? Pearl : level > 0.75 ? PearlGrey : c.rgb;
    c.rgb = flash > 0 && colour > 0.5 ? flashed : c.rgb;
    // Fade in four dithered steps, locked to the sprite's texels.
    float steps = floor((1 - saturate(spriteFx.w)) * 4 + 0.5) / 4;
    clip(steps - Bayer2(floor(u)) - 0.01);
    return float4(c.rgb, 1);
}

struct FlatIn { float4 P : POSITION0; float4 C : COLOR0; };
struct FlatOut { float4 P : POSITION0; float4 C : COLOR0; };

FlatOut FlatVS(FlatIn v)
{
    FlatOut o;
    o.P = mul(v.P, uWorldViewProjection);
    o.P.z = 0;
    o.C = v.C;
    return o;
}

float4 FlatPS(FlatOut i) : COLOR0
{
    return i.C;
}

struct CompositeIn { float4 P : POSITION0; float2 U : TEXCOORD0; };
struct CompositeOut { float4 P : POSITION0; float2 U : TEXCOORD0; };

CompositeOut CompositeVS(CompositeIn v)
{
    CompositeOut o;
    o.P = mul(v.P, uWorldViewProjection);
    o.P.z = 0;
    o.U = v.U;
    return o;
}

float4 CompositeArtPS(CompositeOut i) : COLOR0
{
    return tex2D(sceneTex, i.U);
}

// Only cool tones at or above lilac glow (lilac, pearl-violet, white): porcelain, bone and pearl
// debris stay matte. Premultiplied input.
float Glow(float4 c)
{
    float3 rgb = c.rgb / max(c.a, 0.004);
    float luma = dot(rgb, float3(0.3, 0.3, 0.4));
    float cool = max(saturate((rgb.b - rgb.r) * 20), step(0.97, luma));
    return c.a * step(0.79, luma) * cool;
}

// The glow is a compile-time switch: the plain variant has no sampling loop at all.
float4 CompositeLight(float2 uv, bool withGlow)
{
    float4 c = tex2D(sceneTex, uv);
    float art = tex2D(artTex, uv).a;
    float4 east = tex2D(sceneTex, uv + float2(texel.x, 0)), west = tex2D(sceneTex, uv - float2(texel.x, 0));
    float4 south = tex2D(sceneTex, uv + float2(0, texel.y)), north = tex2D(sceneTex, uv - float2(0, texel.y));
    float touch = max(max(east.a, west.a), max(south.a, north.a));
    float solid = step(0.5, east.a) + step(0.5, west.a) + step(0.5, south.a) + step(0.5, north.a);
    // a pinhole enclosed by pale light is filled, not outlined (no dark specks)
    float4 average = (east + west + south + north) * 0.25;
    float enclosedBright = step(3.5, solid) * step(0.45, dot(average.rgb, float3(0.3, 0.3, 0.4)));
    float4 halo = 0;
    if (withGlow)
    {
        float3 glowColour = 0;
        float glow = 0;
        [unroll] for (int n = 0; n < 8; n++)
        {
            float angle = n * 0.785398 + 0.39;
            float2 direction = float2(cos(angle), sin(angle)) * texel;
            float4 nearSample = tex2D(sceneTex, uv + direction * 2.2), farSample = tex2D(sceneTex, uv + direction * 4.6);
            float gn = Glow(nearSample), gf = 0.6 * Glow(farSample);
            glow += gn + gf;
            glowColour += nearSample.rgb / max(nearSample.a, 0.004) * gn + farSample.rgb / max(farSample.a, 0.004) * gf;
        }
        glowColour /= max(glow, 0.001);
        glow = saturate(glow / 12.8) * glowStrength * (art > 0.004 ? 0.5 : 1);
        halo = float4(glowColour * glow, glow);
    }
    // the outline never draws over the art, which carries its own
    float4 empty = touch > 0.004 && art < 0.004 ? float4(Ink * touch, touch) : halo;
    empty = enclosedBright > 0.5 ? average : empty;
    return c.a > 0.004 ? c : empty;
}

float4 CompositeLightPS(CompositeOut i) : COLOR0
{
    return CompositeLight(i.U, true);
}

float4 CompositeLightPlainPS(CompositeOut i) : COLOR0
{
    return CompositeLight(i.U, false);
}

technique DollPixel
{
    pass FlatPass { VertexShader = compile vs_3_0 FlatVS(); PixelShader = compile ps_3_0 FlatPS(); }
    pass LinePass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 LinePS(); }
    pass RampPass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 RampPS(); }
    pass SpritePass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 SpritePS(); }
    pass CompositeArtPass { VertexShader = compile vs_3_0 CompositeVS(); PixelShader = compile ps_3_0 CompositeArtPS(); }
    pass CompositeLightPass { VertexShader = compile vs_3_0 CompositeVS(); PixelShader = compile ps_3_0 CompositeLightPS(); }
    pass CompositeLightPlainPass { VertexShader = compile vs_3_0 CompositeVS(); PixelShader = compile ps_3_0 CompositeLightPlainPS(); }
}
