// Lacrimosa's Claws energy material (Convergence.DollClawEnergy). Original repository material.
// Drawn by the shared Doll weapon layer into its half-resolution Light target: one fragment is one dot
// (2 world px) on the world-aligned grid, quantized to the Doll light ramp with a world-stable Bayer dither.
// Presentation only; nothing here decides hits. Written branchless (step/lerp only, no uniform-only 'if'),
// because FNA's MojoShader mis-translates uniform branches.
//   Pass 0 Talon  a claw-scratch ribbon: tapered tail, torn plum rim, flowing noise, sparkle, brass glints at
//                 the head and a white-hot spine; cools to plum as it fades.
//                 L = (along 0 tail..1 head, across -1..1 at the band edge, length, half width) in dots.
//                 T = (heat 0..1, fade at the tail 0..1, fade at the head 0..1, alpha).
//   Pass 1 Flare  a band hot in its middle and tapered to both ends (the clap slit and pipe bars, the crush
//                 flare), white spine, flowing noise and sparkle. L as Talon; T = (heat, fade, alpha, 0).
//   Pass 2 Ring   a torn ring inside a square quad (along/across span the diameter): white inner lip, pearl-violet
//                 to lilac body, plum outer edge. T = (radius 0..1 of the quad's half width, thickness in dots, fade, alpha).
//   Pass 3 Void   the crush's lacuna: an opaque black disc that swirls, with a one-dot pearl lip and a plum rim
//                 instead of a spine. T = (radius 0..1, swirl 0..1, fade, alpha).
// Vertex layout as DollPixel.fx Energy: POSITION0, TEXCOORD0 L, TEXCOORD1 S (S.xy dot position), TEXCOORD2 T.
matrix uWorldViewProjection;
float2 dotOrigin;     // absolute world dot of target cell (0, 0)
float clock;          // game ticks (wrapped)

static const float Tau = 6.2831853;

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

// Two octaves of value noise drifting along `flow` (dots per tick).
float Flow(float2 cell, float2 flow, float scale)
{
    float2 p = cell * scale - flow * clock * scale;
    return 0.65 * ValueNoise(p) + 0.35 * ValueNoise(p * 2.13 + 17.7);
}

// Premultiplied output; `keep` 0/1 drops the dot.
float4 Lit(float3 colour, float alpha, float keep)
{
    clip(keep * step(0.004, alpha) - 0.5);
    return float4(colour * alpha, alpha);
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

float4 TalonPS(PrimOut i) : COLOR0
{
    float2 cell = floor(i.S.xy + 0.25) + dotOrigin;
    float along = saturate(i.L.x), halfWidth = max(i.L.w, 0.5);
    float across = abs(i.L.y) * halfWidth;                     // dots from the spine
    float heat = saturate(i.T.x), alpha = saturate(i.T.w);
    float fade = saturate(lerp(i.T.y, i.T.z, along));
    float flow = Flow(cell, float2(0.9, 0.35), 0.29);
    // The ribbon tapers to its tail and its edge is torn by the noise; fading thins it.
    float edge = halfWidth * lerp(0.45, 1.0, sqrt(along)) * (0.8 + 0.3 * flow) * (1 - 0.5 * fade) + 0.35;
    float core = saturate(1 - across / edge);
    float v = (0.55 + 0.7 * core) * (0.85 + 0.15 * along) * (0.85 + 0.15 * heat) + 0.2 * (flow - 0.5);
    v = v * (1 - fade) - 0.15 * fade;
    float dither = Bayer4(cell) - 0.5;
    float band = clamp(floor(v * 7 + dither * 0.9), 0, 6);
    // Torn rim: the outermost dot is plum.
    float rim = step(edge - 0.8, across);
    band = lerp(band, 0, rim);
    // White-hot spine on the fresh part of the ribbon.
    float spine = step(across, 0.55) * step(0.2, along) * step(fade, 0.55);
    band = max(band, spine * 6);
    // Sparkle: twinkling white dots inside the body.
    float sparkle = step(0.955, Hash(cell + floor(clock * 0.5) * 7.31)) * step(0.25, core) * step(fade, 0.5);
    band = max(band, sparkle * 6);
    // Brass glints at the head (the talon's tip catching the light).
    float glint = step(0.9, along) * step(0.72, Hash(cell * 1.7 + floor(clock * 0.34))) * (1 - rim) * step(fade, 0.3);
    float3 colour = lerp(Ramp(band), BrassLight, glint);
    float keep = step(across, edge) * step(0.02, v + spine + sparkle + rim * (1 - fade));
    return Lit(colour, alpha, keep);
}

float4 FlarePS(PrimOut i) : COLOR0
{
    float2 cell = floor(i.S.xy + 0.25) + dotOrigin;
    float along = saturate(i.L.x), halfWidth = max(i.L.w, 0.5);
    float across = abs(i.L.y) * halfWidth;
    float heat = saturate(i.T.x), fade = saturate(i.T.y), alpha = saturate(i.T.z);
    float middle = 1 - abs(2 * along - 1);                     // 0 at the ends, 1 in the middle
    float flow = Flow(cell, float2(0.0, -0.6), 0.33);
    float edge = halfWidth * pow(saturate(middle), 0.45) * (0.75 + 0.35 * flow) * (1 - 0.4 * fade) + 0.35;
    float core = saturate(1 - across / edge);
    float v = (0.55 + 0.7 * core) * (0.8 + 0.2 * middle) * (0.85 + 0.15 * heat) + 0.2 * (flow - 0.5);
    v = v * (1 - fade) - 0.15 * fade;
    float dither = Bayer4(cell) - 0.5;
    float band = clamp(floor(v * 7 + dither * 0.9), 0, 6);
    float rim = step(edge - 0.8, across);
    band = lerp(band, 0, rim);
    float spine = step(across, 0.55) * step(0.18, middle) * step(fade, 0.55);
    band = max(band, spine * 6);
    float sparkle = step(0.95, Hash(cell + floor(clock * 0.5) * 5.17)) * step(0.3, core) * step(fade, 0.5);
    band = max(band, sparkle * 6);
    float keep = step(across, edge) * step(0.02, v + spine + sparkle + rim * (1 - fade));
    return Lit(Ramp(band), alpha, keep);
}

float4 RingPS(PrimOut i) : COLOR0
{
    float2 cell = floor(i.S.xy + 0.25) + dotOrigin;
    float halfWidth = max(i.L.w, 0.5);
    float2 q = float2((2 * i.L.x - 1) * i.L.z * 0.5, i.L.y * halfWidth);   // dots from the centre
    float r = length(q);
    float radius = saturate(i.T.x) * halfWidth, thickness = max(i.T.y, 1), fade = saturate(i.T.z), alpha = saturate(i.T.w);
    float angle = atan2(q.y, q.x);
    float flow = Flow(float2(angle * 9.5, r * 0.4), float2(-0.4, 0), 1);
    float torn = thickness * (0.55 + 0.65 * flow) * (1 - 0.5 * fade);
    float d = r - radius;                                      // + outside, - inside
    float body = step(-torn, d) * step(d, torn * 0.6);
    float inner = saturate((-d) / max(torn, 0.5));              // 1 at the inner lip
    float v = (0.56 + 0.7 * inner) * (1 - fade) + 0.15 * (flow - 0.5) - 0.15 * fade;
    float dither = Bayer4(cell) - 0.5;
    float band = clamp(floor(v * 7 + dither * 0.9), 0, 6);
    float lip = step(-torn, d) * step(d, -torn + 1) * step(fade, 0.5);
    band = max(band, lip * 6);
    band = lerp(band, 0, step(torn * 0.6 - 1, d));             // plum outer edge
    float sparkle = step(0.95, Hash(cell + floor(clock * 0.5) * 3.7)) * step(fade, 0.5);
    band = max(band, sparkle * body * 5);
    return Lit(Ramp(band), alpha, body * step(0.02, v + lip + 1 - step(torn * 0.6 - 1, d)));
}

float4 VoidPS(PrimOut i) : COLOR0
{
    float2 cell = floor(i.S.xy + 0.25) + dotOrigin;
    float halfWidth = max(i.L.w, 0.5);
    float2 q = float2((2 * i.L.x - 1) * i.L.z * 0.5, i.L.y * halfWidth);
    float r = length(q);
    float radius = saturate(i.T.x) * halfWidth, swirl = saturate(i.T.y), fade = saturate(i.T.z), alpha = saturate(i.T.w);
    float angle = atan2(q.y, q.x);
    // The rim wobbles a little; the inside turns.
    float wobble = (ValueNoise(float2(angle * 3 + clock * 0.05, clock * 0.07)) - 0.5) * 2 * (0.6 + swirl);
    float edge = radius * (1 - 0.6 * fade) + wobble;
    float inside = step(r, edge);
    float lip = step(edge - 1, r) * inside;                    // the one-dot pearl lip
    float rim = step(edge - 2.2, r) * (1 - lip) * inside;      // a plum band under it
    float spiral = frac(angle / Tau * 3 + r * 0.09 - clock * 0.03 * (0.5 + swirl));
    float dark = step(0.62, spiral) * step(r, edge * 0.85) * (1 - step(0.85, Hash(cell + floor(clock * 0.25))));
    float3 colour = lerp(lerp(Ink, IronDark, dark), Plum, rim);
    colour = lerp(colour, Pearl, lip);
    return Lit(colour, alpha, inside);
}

technique DollClawEnergy
{
    pass Talon { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 TalonPS(); }
    pass Flare { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 FlarePS(); }
    pass Ring { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 RingPS(); }
    pass Void { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 VoidPS(); }
}
