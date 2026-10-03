// Pale Meridian energy. Original repository material.
// The light of the Doll reward weapon Pale Meridian, rendered per dot into the shared Doll weapon layer's
// half-resolution Light target (1 dot = 2 world px, premultiplied AlphaBlend) and quantized to the Doll light ramp
// below, with a world-stable Bayer dither. Presentation only: nothing here decides a hit.
//   MeridianBodyPass     the meridian/lattice packet: a white-hot spine and head over a flowing pearl-violet body
//                        that thins to a plum rim, with drifting sparkles.
//   MeridianWakePass     a round's wake: a pearl pin fading to violet along its path.
//   MeridianGlowPass     a round glow (muzzle iris, charge, head flare, node star core) with an optional ring.
//   MeridianResiduePass  a cooling trace: violet to plum, crumbling into the noise as it cools.
// Flowing noise: the Luminance noise textures in s1 (turbulent) and s2 (wavy blotches), sampled in absolute world
// dots so the light never swims with the camera.
//
// Vertex layout (C# DollPixelVertex): L = (along 0..1, across -1..1 (beyond 1 on the grown rim), length, half width)
// in dots for bands; for MeridianGlowPass L.xy = the local square -k..k and L.z = the radius in dots. S.xy = the dot
// position (floor(S.xy + .25) + dotOrigin is the absolute world dot), S.z = the band's distance from its line origin
// in dots (noise phase), S.w = 0. T = (intensity 0..1, alpha, pass value, seed): the pass value is the ring radius
// 0..1 for the glow and the heat 0..1 for the residue. T.x/T.y match the built-in RampPass, which stands in when
// this material is missing.
// No branch depends on a uniform alone (FNA's MojoShader mis-translates them): every choice is a step/lerp.
matrix uWorldViewProjection;
float2 dotOrigin;   // absolute world dot of target cell (0, 0)
float clock;        // game ticks plus the draw fraction
float reduced;      // 1 under Reduced Effects: half the sparkles

sampler noiseA : register(s1);
sampler noiseB : register(s2);

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

// Two noise layers flowing along +u (the line's direction) in dot units; 0..1.
float Flow(float u, float w, float seed, float speed)
{
    float2 q = float2(u - clock * speed, w);
    float a = tex2D(noiseA, q * float2(0.019, 0.083) + float2(seed, seed * 0.37)).r;
    float b = tex2D(noiseB, q * float2(0.041, 0.121) + float2(0.31 - seed, 0.17) - a * 0.23).r;
    return saturate(a * 0.62 + b * 0.38);
}

// Quantizes an intensity to the ramp with the world-stable dither; returns the band (0..6) or -1 when dark.
float Band(float v, float2 cell)
{
    float d = Bayer4(cell) - 0.5;
    float x = v * 7 + d * 0.9;
    return x < 0.5 ? -1 : clamp(floor(x), 0, 6);
}

float4 Shade(float band, float alpha)
{
    clip(band + 0.5);
    clip(alpha - 0.004);
    return float4(Ramp(band) * alpha, alpha);
}

// The meridian and lattice packets. The vertex intensity runs from the tail (cooler, thinner) to the head (1).
float4 BodyPS(PrimOut i) : COLOR0
{
    float2 cell = floor(i.S.xy + 0.25) + dotOrigin;
    float along = saturate(i.L.x);
    float halfWidth = max(i.L.w, 0.5);
    float dist = abs(i.L.y) * halfWidth;                 // dots from the spine
    float lengthDots = max(i.L.z, 1);
    float alongDots = i.S.z + along * lengthDots;        // dots from the line origin
    float behindHead = (1 - along) * lengthDots;         // dots behind the drawn head
    float heat = saturate(i.T.x);
    // The body narrows toward the tail and swells at the head into a rounded packet.
    float taper = lerp(0.5, 1, saturate(along * 1.4)) + 0.25 * saturate(1 - behindHead / 6);
    float edge = max(halfWidth * taper, 1);
    float x = dist / edge;
    float flow = Flow(alongDots, sign(i.L.y) * dist, i.T.w, 1.7);
    // Profile: white spine, pearl-violet/bone body, lilac and violet flanks, a plum rim; the flow moves the boundaries.
    float profile = 1 - 0.92 * pow(saturate(x), 2.1);
    float v = heat * profile + (flow - 0.5) * 0.34 * (0.35 + x);
    float spine = step(dist, 0.75) * step(2.5, alongDots);
    float head = step(behindHead, 2.5 + 2 * (1 - x)) * step(x, 0.85);
    // Sparkles drift with the flow; Reduced Effects keeps half.
    float2 sparkCell = floor(float2(alongDots - clock * 1.3, dist * 1.7 + 11));
    float sparkle = step(0.972 + 0.014 * reduced, Hash(sparkCell + floor(clock / 4) * 13.1)) * step(x, 0.8);
    v = max(v, max(spine * heat, max(head, sparkle)));
    float band = Band(v, cell);
    // The outermost dot is the cool rim; outside the shape nothing draws.
    float rim = step(1 - 1.2 / edge, x);
    band = rim > 0.5 ? min(band, 1) : band;
    band = x > 1.001 ? -1 : band;
    return Shade(band, i.T.y);
}

// A round's wake along its path (L.x 0 at the oldest point, 1 at the head).
float4 WakePS(PrimOut i) : COLOR0
{
    float2 cell = floor(i.S.xy + 0.25) + dotOrigin;
    float along = saturate(i.L.x);
    float halfWidth = max(i.L.w, 0.5);
    float edge = max(halfWidth * (0.35 + 0.65 * along), 0.6);
    float dist = abs(i.L.y) * halfWidth;
    float x = dist / edge;
    float flow = Flow(along * i.L.z, dist, i.T.w, 2.6);
    float v = saturate(i.T.x) * (pow(along, 1.6) * (1 - 0.8 * x) + (flow - 0.5) * 0.3);
    v = max(v, step(0.9, along) * step(dist, 0.75));
    float band = Band(v, cell);
    band = x > 1.001 ? -1 : band;
    return Shade(band, i.T.y);
}

// A round glow: hot centre, a swirling falloff and an optional thin ring at T.z of the radius.
float4 GlowPS(PrimOut i) : COLOR0
{
    float2 cell = floor(i.S.xy + 0.25) + dotOrigin;
    float radius = max(i.L.z, 0.5);
    float r = length(i.L.xy);
    float angle = atan2(i.L.y, i.L.x);
    float flow = Flow(angle * radius * 0.5, r * radius, i.T.w, 1.1);
    float v = saturate(i.T.x) * (pow(saturate(1 - r), 1.3) * 1.15 + (flow - 0.5) * 0.4 * saturate(1 - r));
    float ringRadius = i.T.z;
    float ring = step(0.01, ringRadius) * step(abs(r - ringRadius) * radius, 0.6);
    v = max(v, ring * saturate(i.T.x + 0.35));
    float band = Band(v, cell);
    band = r > 1.001 ? -1 : band;
    return Shade(band, i.T.y);
}

// A cooling trace: violet when fresh, plum when cold, crumbling into dark gaps as the heat goes.
float4 ResiduePS(PrimOut i) : COLOR0
{
    float2 cell = floor(i.S.xy + 0.25) + dotOrigin;
    float along = saturate(i.L.x);
    float halfWidth = max(i.L.w, 0.5);
    float dist = abs(i.L.y) * halfWidth;
    float x = dist / halfWidth;
    float heat = saturate(i.T.z);
    float alongDots = i.S.z + along * max(i.L.z, 1);
    float flow = Flow(alongDots, dist, i.T.w, 0.35);
    float keep = step(1 - heat * 1.15, flow * (1 - 0.5 * x));
    float v = (0.08 + heat * 0.42) * (1 - 0.5 * x) + (flow - 0.5) * 0.12;
    float band = keep > 0.5 ? max(Band(v, cell), 0) : -1;
    band = x > 1.001 ? -1 : band;
    return Shade(band, i.T.y);
}

technique DollMeridianEnergy
{
    pass MeridianBodyPass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 BodyPS(); }
    pass MeridianWakePass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 WakePS(); }
    pass MeridianGlowPass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 GlowPS(); }
    pass MeridianResiduePass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 ResiduePS(); }
}
