// Ebon reward pixel layer. Original repository material.
// The primitive passes evaluate one Ebon art dot per pixel of a screen-aligned
// half-resolution target (1 dot = 2 world px) and quantize it to the Ebon
// palette: swept crescents, chalk/tear bands and 1-3 dot threads, strings and
// rings. FlatPass fills dot runs plotted on the CPU (stars, debris, stitches).
// CompositePass point-upscales the target with a one-dot navy outline and a
// small bounded glow. Inputs are presentation state only; nothing here decides hits.
matrix uWorldViewProjection;
float shimmerOffset;
float reduced;
float2 texel;
float glowStrength;

sampler sceneTex : register(s0);
sampler noiseTex : register(s1);

static const float PI = 3.14159265;
static const float ShimmerSpacing = 36;
static const float3 Outline = float3(0.055, 0.047, 0.102);  // 14,12,26
static const float3 Charcoal = float3(0.173, 0.157, 0.227); // 44,40,58
static const float3 Rose = float3(0.769, 0.439, 0.525);     // 196,112,134
static const float3 Silver = float3(0.690, 0.714, 0.800);   // 176,182,204
static const float3 Ivory = float3(0.933, 0.902, 0.839);    // 238,230,214
static const float3 Moon = float3(0.980, 0.988, 1.000);     // 250,252,255
static const float3 Gold = float3(0.776, 0.643, 0.361);     // 198,164,92
static const float3 Candle = float3(1.000, 0.769, 0.439);   // 255,196,112
static const float3 Wood = float3(0.478, 0.322, 0.220);     // 122,82,56 (debris only)

// Tone indices follow EbonTone; 8 is the internal debris wood.
float3 Palette(float tone)
{
    return tone < 0.5 ? Outline : tone < 1.5 ? Charcoal : tone < 2.5 ? Rose : tone < 3.5 ? Silver
        : tone < 4.5 ? Ivory : tone < 5.5 ? Moon : tone < 6.5 ? Gold : tone < 7.5 ? Candle : Wood;
}

// One step up a tone ramp: navy -> charcoal -> silver -> ivory -> moon; rose -> ivory; wood -> gold -> candle -> moon.
float Up(float tone)
{
    return tone < 0.5 ? 1 : tone < 1.5 ? 3 : tone < 3.5 ? 4 : tone < 5.5 ? 5 : tone < 6.5 ? 7 : tone < 7.5 ? 5 : 6;
}

float Noise(float2 uv, int channel)
{
    float4 n = tex2Dlod(noiseTex, float4(uv, 0, 0));
    return channel == 0 ? n.r : channel == 1 ? n.g : n.b;
}

float Is(float tone, float value)
{
    return step(abs(tone - value), 0.5);
}

// L.xy: target position relative to an integer anchor cell, so floor(L.xy + .25) is this
// fragment's exact dot offset whatever the rasterizer's half-pixel convention.
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

// Crescent. L.z: 0 at the tail .. 1 at the leading edge; L.w: arc length in dots.
// S: root fraction inside the anchor cell, inner and outer radius in dots. T: fade, heat, noise shift.
// Tones step inward from the blade's path (moon rim, ivory, silver, charcoal) and darken toward the tail.
float4 CrescentPS(PrimOut i) : COLOR0
{
    float2 k = floor(i.L.xy + 0.25);
    float2 p = k + 0.5 - i.S.xy;
    float inner = i.S.z, outer = i.S.w, band = max(outer - inner, 1);
    float s = saturate(i.L.z), along = s * i.L.w;
    float fade = saturate(i.T.x), heat = saturate(i.T.y);
    float2 shift = i.T.zw;
    // dots inward from the (slightly wavering) rim; cooling fragments drift a little outward
    float depth = outer - length(p) + 0.06 * band * pow(fade, 0.8) - 0.5 * sin(along * 0.19 + shift.x * 6.283);
    float shape = sin(PI * pow(max(s, 0.0001), 1.25));
    float thick = band * 0.92 * shape * (1 - 0.4 * fade);

    float edge = Noise(float2(along * 0.012, depth * 0.02) + shift, 0);
    float streak = Noise(float2(along * 0.004, depth / band * 1.2) + shift * 0.6, 2);
    float dissolve = Noise(float2(along * 0.01 + 0.31, depth / band * 0.5) - shift, 1);
    float warm = Noise(float2(along * 0.008 + 0.57, depth / band * 0.3) + shift.yx, 0);

    float limit = thick * (1 + (edge - 0.5) * 0.35);
    float inBand = step(0, depth) * step(depth, limit);
    float a = saturate(1 - depth / max(limit, 0.5));
    float lead = pow(s, 0.85);
    float value = (0.3 + 0.7 * lead) * (0.35 + 0.65 * pow(a, 1.2)) * (1 - 0.4 * fade) + (streak - 0.5) * 0.16;
    // the rim is the blade's path: one bright dot while fresh
    float rim = step(depth, 1.05) * step(0.16, s) * step(fade, 0.5);
    value = rim > 0.5 ? max(value, 0.62 + 0.3 * lead) : value;
    float tone = value > 0.72 ? 5 : value > 0.5 ? 4 : value > 0.33 ? 3 : 1;
    // heat warms silver first, then ivory, toward dusty rose; moon stays the hottest edge
    float warmth = heat * (0.5 + 0.5 * lead);
    float toRose = Is(tone, 3) * step(0.2 + 0.3 * warm, warmth) + Is(tone, 4) * step(0.6 + 0.3 * warm, warmth);
    tone = toRose > 0.5 ? 2 : tone;

    // torn decay from the old tail toward the leading edge; no tear while fresh
    float tear = saturate(fade * 1.6 - s * 0.6);
    float alive = step(pow(tear, 1.1), dissolve + 0.001);
    // the darkest charcoal is a checker whose holes take the composite navy outline
    float checker = step(0.25, frac((k.x + k.y) * 0.5));
    float deep = value > 0.2 ? 1 : checker * step(fade, 0.3);
    clip(inBand * alive * deep - 0.5);
    return float4(Palette(tone), 1);
}

// Tear geometry along the rip: lens-shaped opening with per-side swell and zig-zag teeth (dots).
float TearLens(float along, float hw, float head)
{
    return pow(saturate(along / (hw * 1.6 + 2)), 0.6) * pow(saturate((head - along) / (hw * 2.2 + 3)), 0.7);
}

float Tooth(float along, float side, float seed)
{
    float period = 5 + 2 * Noise(float2(along * 0.04 + seed, 0.77 + side * 0.13), 1);
    return abs(frac(along / period + side * 0.5 + seed) * 2 - 1);
}

float TearOpen(float along, float side, float hw, float head, float seed)
{
    float lens = TearLens(along, hw, head);
    float swell = Noise(float2(along * 0.025 + seed, 0.21 + side * 0.4), 0);
    return hw * lens * (0.72 + 0.28 * swell) + (Tooth(along, side, seed) - 0.5) * 2 * min(2.5, hw * 0.25) * saturate(lens * 3);
}

// Band along its direction. L.z: 0 chalk, 1 tear; L.w: seed shift.
// S: start fraction inside the anchor cell, unit direction. T: length, half width, head (progress) in dots, alpha.
float4 BandPS(PrimOut i) : COLOR0
{
    float2 k = floor(i.L.xy + 0.25);
    float2 p = k + 0.5 - i.S.xy;
    float2 dir = i.S.zw, nrm = float2(-dir.y, dir.x);
    float along = dot(p, dir), across = dot(p, nrm), d = abs(across);
    float hw = max(i.T.y, 0.5), head = i.T.z, alpha = i.T.w, seed = i.L.w;
    float side = step(0, across);
    float tone = 4, a = alpha, keep = 0;
    if (i.L.z < 0.5)
    {
        // Tailor's chalk: one continuous stroke; dry-brush grain varies tone and edge, never coverage.
        // A wide lane is a ruled centre line and edges over faint dust.
        float wob = Noise(float2(along * 0.035 + seed, 0.13 + side * 0.5), 0);
        float end = Noise(float2(0.61 + seed, d * 0.21 + 0.4), 1);
        float grain = Noise(float2(along * 0.011 + seed * 1.3, across * 0.23 + 0.71), 2);
        float edge = hw - 0.4 + (wob - 0.5) * min(1.8, hw * 0.45);
        float inLength = step(-0.5, along) * step(along, head + 0.5 + (end - 0.5) * min(3, hw));
        float wide = step(8, hw);
        float core = step(d, max(0.5, hw * 0.12) + 0.01);
        float lip = step(edge - 1.0, d) * wide;
        float ruled = saturate(core + lip + (1 - wide));
        float g = grain + 0.3 * exp(-max(head - along, 0) / 14);
        tone = ruled > 0.5 ? (g > 0.8 && reduced < 0.5 ? 5 : g > 0.32 ? 4 : 3) : 3;
        a = alpha * (ruled > 0.5 ? 1 : lerp(0.1, 0.24, grain) * (1 - 0.3 * reduced));
        keep = inLength * step(d, edge);
    }
    else
    {
        // A live rip: zig-zag ivory lips around a dark opening, lens-shaped, tapering at both ends.
        // The lip spans toward both neighbouring columns so steep teeth never break it into dots.
        float tooth = Tooth(along, side, seed);
        float open = TearOpen(along, side, hw, head, seed);
        float openL = TearOpen(along - 1, side, hw, head, seed), openR = TearOpen(along + 1, side, hw, head, seed);
        float lo = min(open, 0.5 * (open + min(openL, openR))), hi = max(open, 0.5 * (open + max(openL, openR)));
        float lipWidth = hw > 8 ? 2 : 1;
        float lens = TearLens(along, hw, head);
        float lip = step(lo, d) * step(d, hi + lipWidth + 0.2) * step(0.2, lens);
        float fray = step(0.88, tooth) * step(0.55, Noise(float2(along * 0.13 + seed, side * 0.5 + 0.17), 2))
            * step(hi + lipWidth + 0.2, d) * step(d, hi + lipWidth + 1.2) * step(4, hw);
        float inside = step(d, lo) * step(0.75, lo);
        tone = lip > 0.5 ? (tooth > 0.85 && reduced < 0.5 ? 5 : step(hi + lipWidth - 0.8, d) > 0.5 ? 4 : 3)
            : fray > 0.5 ? 4 : step(lo - 1.5, d) > 0.5 ? 1 : 0;
        keep = step(-0.5, along) * step(along, head + 0.5) * saturate(inside + lip + fray);
    }
    clip(keep * step(0.004, a) - 0.5);
    return float4(Palette(tone) * a, a);
}

// Threads (L.z 0), strings (1) and rings (2); L.w: tone. Lines are anchored at the first end's
// cell and S.xy is the whole-dot offset to the other end, so the major-axis test is exact.
// T: thickness, alpha, shimmer (thread) or amplitude in dots (string), phase (string).
// S for rings: radius in dots around the anchor cell's centre.
float4 LinePS(PrimOut i) : COLOR0
{
    float2 k = floor(i.L.xy + 0.25);
    float kind = i.L.z, tone = i.L.w, thick = i.T.x, alpha = i.T.y;
    float keep = 0;
    if (kind > 1.5)
    {
        float r = i.S.x, h = 0.5 * thick;
        float2 q = abs(k);
        float delta = q.y >= q.x ? q.y - sqrt(max(r * r - q.x * q.x, 0)) : q.x - sqrt(max(r * r - q.y * q.y, 0));
        keep = r < 0.75 ? step(q.x + q.y, 0.5) : step(-h, delta) * (1 - step(h, delta));
    }
    else
    {
        float2 d = floor(i.S.xy + 0.5);
        float xMajor = step(abs(d.y), abs(d.x));
        float dM = lerp(d.y, d.x, xMajor), dm = lerp(d.x, d.y, xMajor);
        float cM = lerp(k.y, k.x, xMajor), cm = lerp(k.x, k.y, xMajor);
        float sg = dM < 0 ? -1 : 1, aM = abs(dM);
        float t = cM * sg;
        float inRange = step(-0.01, t) * step(t, aM + 0.01);
        float s = t / max(aM, 1);
        if (aM < 0.5)
        {
            keep = step(abs(k.x) + abs(k.y), 0.5);
        }
        else if (kind < 0.5)
        {
            // integer form of -thick/2 <= minor offset < thick/2
            float x2 = 2 * (cm * dM - cM * dm) * sg;
            keep = inRange * step(-thick * aM - 0.5, x2) * step(x2, thick * aM - 0.5);
            float lit = i.T.z;
            float q = frac((s * i.S.z - shimmerOffset) / ShimmerSpacing) * ShimmerSpacing;
            float pulse = lit > 0.001 ? 1 + lit * (reduced > 0.5 ? 3 : 7) : 0;
            float bright = step(q, pulse);
            float centre = bright * step(pulse * 0.3, q) * step(q, pulse * 0.7) * step(0.5, lit) * (1 - reduced);
            tone = bright > 0.5 ? (centre > 0.5 ? Up(Up(tone)) : Up(tone)) : tone;
        }
        else
        {
            // standing half-wave displaced along the left normal of (b - a)
            float wave = i.T.z * cos(i.T.w);
            float stretch = i.S.z / dM * (xMajor > 0.5 ? 1 : -1);
            float shift = wave * sin(PI * s) * stretch;
            float slope = dm / dM + wave * PI * cos(PI * s) * stretch / dM;
            float delta = cm - (cM * dm / dM + shift);
            float h = 0.5 * thick * max(1, abs(slope));
            keep = inRange * step(-h, delta) * (1 - step(h, delta));
        }
    }
    clip(keep * step(0.004, alpha) - 0.5);
    return float4(Palette(tone) * alpha, alpha);
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

// Only pale tones glow; premultiplied colour keeps faded dots dim.
float Bright(float4 c)
{
    return saturate(dot(c.rgb, float3(0.3, 0.3, 0.4)) * 1.6 - 0.6);
}

float4 CompositePS(CompositeOut i) : COLOR0
{
    float4 c = tex2D(sceneTex, i.U);
    float4 east = tex2D(sceneTex, i.U + float2(texel.x, 0)), west = tex2D(sceneTex, i.U - float2(texel.x, 0));
    float4 south = tex2D(sceneTex, i.U + float2(0, texel.y)), north = tex2D(sceneTex, i.U - float2(0, texel.y));
    float touch = max(max(east.a, west.a), max(south.a, north.a));
    float solid = step(0.5, east.a) + step(0.5, west.a) + step(0.5, south.a) + step(0.5, north.a);
    // a pinhole enclosed by pale dots is filled, not outlined (no dark specks)
    float4 average = (east + west + south + north) * 0.25;
    float enclosedBright = step(3.5, solid) * step(0.45, dot(average.rgb, float3(0.3, 0.3, 0.4)));
    float3 glowColour = 0;
    float glow = 0;
    [unroll] for (int n = 0; n < 8; n++)
    {
        float angle = n * 0.785398 + 0.39;
        float2 direction = float2(cos(angle), sin(angle)) * texel;
        float4 nearSample = tex2D(sceneTex, i.U + direction * 2.2), farSample = tex2D(sceneTex, i.U + direction * 4.6);
        float bn = Bright(nearSample), bf = 0.6 * Bright(farSample);
        glow += bn + bf;
        glowColour += nearSample.rgb * bn + farSample.rgb * bf;
    }
    glowColour /= max(glow, 0.001);
    glow = saturate(glow / 12.8) * glowStrength;
    float4 halo = float4(glowColour * glow, glow);
    float4 empty = touch > 0.004 ? float4(Outline * touch, touch) : halo;
    empty = enclosedBright > 0.5 ? average : empty;
    return c.a > 0.004 ? c : empty;
}

technique EbonPixel
{
    pass AutoloadPass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 CrescentPS(); }
    pass BandPass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 BandPS(); }
    pass LinePass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 LinePS(); }
    pass FlatPass { VertexShader = compile vs_3_0 FlatVS(); PixelShader = compile ps_3_0 FlatPS(); }
    pass CompositePass { VertexShader = compile vs_3_0 CompositeVS(); PixelShader = compile ps_3_0 CompositePS(); }
}
