// Last Witness energy material. Original repository material.
// The light of the refreshed Last Witness in the client-only Doll weapon layer: the thrown blade's wake and spin
// arc, the testimony threads, the shard tails and the judgement's edges and execution fill. Rendered per dot into
// the layer's half-resolution Light target (1 dot = 2 world px) and quantized to the Doll palette below, so the
// layer's ink outline and glow apply. Inputs are presentation state only; nothing here decides hits.
// Branchless on uniforms (FNA/MojoShader mis-translates uniform-only branches): `reduced` only scales.
//   pass 0 RibbonPass  a band along a spine: white-hot spine, bone and pearl-violet core cooling through lilac and
//                      violet to a plum fringe; two octaves of world-locked flowing value noise move the band
//                      edges; a few dots sparkle white on short beats; an optional write head draws it from its
//                      tail with a hot flare riding behind the head; the tail narrows and cools.
//   pass 1 FillPass    the judgement's execution: opaque pearl craquelure (the edges of a world-locked cell field)
//                      over a translucent porcelain-violet ground, the cracks drawn toward the centre as it
//                      collapses around a small black eye with a one-dot pearl lip (the void rule).
//
// Vertex layout (C# DollPixelVertex): POSITION0 float3 in dots, TEXCOORD0 L, TEXCOORD1 S, TEXCOORD2 T (float4).
//   RibbonPass L = (along 0..1, across -1..1 at the band edge, length in dots, half width in dots) as written by
//              DollWeaponCanvas.EnergyQuad/EnergyStrip; S.xy = dot position (floor(S.xy + .25) + dotOrigin is the
//              absolute world dot); T = (intensity 0..1, alpha, write head 0..1 (above 1: fully written), taper 0..1).
//   FillPass   L = (x, y from the centre in dots, circumradius in dots, ground alpha 0..1); S = (dot position,
//              eye radius in dots (0: shut), void alpha 0..1); T = (intensity 0..1, alpha, collapse 0..1, seed 0..1).
//              Every value comes per vertex from the CPU (WitnessPresentation.Fill).
matrix uWorldViewProjection;
float2 dotOrigin;   // absolute world dot of target cell (0, 0)
float clock;        // game ticks plus the draw fraction, wrapped by the caller
float reduced;      // 1 under Reduced Effects: fewer sparkles (bodies and counts unchanged)

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

// Distance between the nearest and second-nearest feature point of a jittered cell field (small on a cell edge).
float CellEdge(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float d1 = 8, d2 = 8;
    [unroll] for (int y = -1; y <= 1; y++)
    {
        [unroll] for (int x = -1; x <= 1; x++)
        {
            float2 o = float2(x, y);
            float2 r = o + float2(Hash(i + o), Hash(i + o + 19.19)) - f;
            float d = dot(r, r);
            d2 = min(d2, max(d, d1));
            d1 = min(d1, d);
        }
    }
    return sqrt(d2) - sqrt(d1);
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

float4 RibbonPS(PrimOut i) : COLOR0
{
    float2 cell = floor(i.S.xy + 0.25) + dotOrigin;
    float along = i.L.x;
    float halfWidth = max(i.L.w, 0.5);
    float intensity = saturate(i.T.x), alpha = saturate(i.T.y), head = i.T.z, taper = saturate(i.T.w);
    // The band narrows toward its tail; edge runs 0 on the spine to 1 on the rim.
    float width = max(halfWidth * lerp(1 - taper, 1, saturate(along)), 0.5);
    float across = abs(i.L.y) * halfWidth;
    float edge = across / width;
    // Flowing noise, locked to world dots and drifting through them.
    float n = ValueNoise(cell * 0.23 + float2(clock * 0.31, -clock * 0.17)) * 0.62
        + ValueNoise(cell * 0.61 + float2(-clock * 0.53, clock * 0.29)) * 0.38;
    float heat = intensity * (1.26 - 0.96 * edge + (n - 0.5) * 0.4);
    // Write head: only the written part shows, with a hot flare riding just behind the head.
    float written = step(along, head);
    float flare = saturate(1 - (head - along) * 9) * written * step(head, 1);
    heat += flare * 0.45 * intensity;
    // The tail cools (more so on a tapered band: a comet from a white head to a plum tail).
    heat *= lerp(1 - 0.55 * taper, 1, saturate(along * 1.4));
    float dither = Bayer4(cell) - 0.5;
    float v = heat * 7 + dither * 0.9;
    float band = clamp(floor(v), 0, 6);
    // A white-hot spine along the hot part of the band, and sparkles on short beats.
    float spine = step(across, 0.55) * step(0.45, intensity) * step(0.3 + 0.35 * taper, along);
    float sparkle = step(0.965 + 0.02 * reduced, Hash(cell * 1.37 + floor(clock * 0.5) * 17.3)) * step(0.35, heat);
    band = max(band, 6 * max(spine, sparkle));
    clip(min(min(v - 0.5, 1.02 - edge), min(written - 0.5, alpha - 0.004)));
    return float4(Ramp(band) * alpha, alpha);
}

float4 FillPS(PrimOut i) : COLOR0
{
    float2 cell = floor(i.S.xy + 0.25) + dotOrigin;
    float2 rel = i.L.xy;
    float radius = max(i.L.z, 1), ground = saturate(i.L.w);
    float eye = i.S.z, voidAlpha = saturate(i.S.w);
    float intensity = saturate(i.T.x), alpha = saturate(i.T.y), collapse = saturate(i.T.z), seed = i.T.w;
    float distance = length(rel), r = distance / radius;
    // Craquelure: the edges of a world-locked cell field, drawn toward the centre as the fill collapses.
    float2 p = cell / 7.0 + seed * 31.7 + rel * (collapse * 0.32 / radius) * 7.0;
    float crack = 1 - saturate(CellEdge(p) * 4.5);
    float n = ValueNoise(cell * 0.3 + float2(clock * 0.2, -clock * 0.11));
    // Pale porcelain between the cracks, white-hot cracks, a violet rim toward the corners.
    float heat = intensity * (0.72 + 0.5 * crack + 0.2 * (n - 0.5)) * (1 - 0.32 * r);
    float dither = Bayer4(cell) - 0.5;
    float v = heat * 7 + dither * 0.9;
    float band = clamp(floor(v), 0, 6);
    // The black eye at the centre (its radius comes from the CPU), ringed by a one-dot pearl lip.
    float open = step(0.75, eye);
    float inEye = step(distance, eye) * open;
    float lip = step(eye, distance) * step(distance, eye + 1) * open;
    float3 colour = Ramp(band);
    colour = lip > 0.5 ? Pearl : colour;
    colour = inEye > 0.5 ? Ink : colour;
    // Only the cracks and the lip are opaque; the ground between them shows the world through it, and the void
    // takes its own alpha (another player's at .60).
    float a = inEye > 0.5 ? voidAlpha : alpha * max(max(step(0.5, crack), lip), ground);
    clip(min(max(v - 0.5, max(inEye, lip) - 0.5), a - 0.004));
    return float4(colour * a, a);
}

technique DollWitnessEnergy
{
    pass RibbonPass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 RibbonPS(); }
    pass FillPass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 FillPS(); }
}
