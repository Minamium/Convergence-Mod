// Doll Lacuna Testament void material. Original repository material.
// The Lacuna Testament's energy (docs/encounters/first-severance/WEAPONS.md, "Magic - Lacuna Testament"), evaluated
// per dot in the Doll weapon layer's half-resolution Light target (premultiplied AlphaBlend, so the black void hides
// what is behind it). A void material: a black core with plum streaks drifting inward and rare pearl sparks, a
// one-dot pearl lip instead of a white spine, violet/pearl rims whose folds flow outward with flowing Luminance
// noise, a one-dot pearl silhouette exactly on the collision edge and a dithered violet halo outside it (the halo
// never hits). Every colour is a Doll palette tone. Presentation only: nothing here decides a hit.
// Written without uniform-only branches (FNA's MojoShader mistranslates them); selections are arithmetic. No
// preshader either (the FNA effect runtime mis-evaluates some fx_2_0 preshader code; tools tests check the export has
// none): every uniform-only quantity (the flow time and the sparkle thresholds, which depend on Reduced Effects) is
// computed on the CPU by LacunaEnergy.Apply, and the vertex shader folds the uniforms into its outputs with vertex
// data (the dot origin into S.xy; `timing` times the position's w, which is 1 for the three-component positions the
// layer writes), so the pixel shaders read them as interpolants and no expression of uniforms alone is left to hoist.
//
// Vertex layout (C# DollPixelVertex; LacunaPresentation builds every vertex):
//   Every pass: S.xy = the vertex's dot in the target (the vertex shader adds the dot origin), K = timing.
//   BeamPass   L = (along, across, reach, half edge) in dots from the muzzle; S = (dot x, dot y, pulse 1, pulse 2)
//              with the pulses in dots along the beam (far negative = none); T = (hot 0..1, light alpha, void alpha,
//              mask): mask = whole dots of the radius about the muzzle inside which the beam is hidden (the great
//              aperture or the rosette in front of it), plus half the lip thickness (the larger component of the
//              beam normal, so the lip is one dot on every slope) as its fraction.
//   MouthPass  L = (dx, dy, radius, spiral 0..1) in dots from the hole's centre; S = (dot x, dot y, black share 0..1,
//              spin phase); T = (hot, light alpha, void alpha, lip brightness 0..1).
//   WakePass   EnergyStrip layout: L = (u tail 0 .. head 1, across -1..1 of the grown half, length, half width) in
//              dots; S = (dot x, dot y, 0, 0); T = (hot, light alpha, void alpha, seed).
matrix uWorldViewProjection;
float2 dotOrigin;     // absolute world dot of target cell (0, 0): world-stable dither and sparks
// x: flow time in seconds (wrapped hourly; slowed to .45 under Reduced Effects); y: the void spark threshold (2 = none
// under Reduced Effects); z: this pass's twinkle threshold (fewer twinkles under Reduced Effects); w: unused.
float4 timing;

sampler cloudTex : register(s1);   // Luminance WavyBlotchNoise
sampler flowTex : register(s2);    // Luminance TurbulentNoise

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

float Cloud(float2 uv)
{
    return tex2Dlod(cloudTex, float4(uv, 0, 0)).r;
}

float Flow(float2 uv)
{
    return tex2Dlod(flowTex, float4(uv, 0, 0)).r;
}

// One void dot: ink, with plum streaks where `drift` (a noise field moving toward the centre) is high, iron speckle
// and, rarely, a pearl spark (none under Reduced Effects).
float3 VoidTone(float2 cell, float drift, float t, float sparkCut, float dither)
{
    float streak = step(0.58, drift + 0.08 * dither);
    float vein = step(0.78, drift + 0.05 * dither);
    float speck = step(0.95, Hash(cell + floor(t * 7)));
    float spark = step(sparkCut, Hash(cell * 1.37 + floor(t * 9) * 5.1));
    return spark > 0.5 ? PearlViolet : vein > 0.5 ? PlumLight : streak > 0.5 ? Plum : speck > 0.5 ? IronDark : Ink;
}

struct PrimIn { float4 P : POSITION0; float4 L : TEXCOORD0; float4 S : TEXCOORD1; float4 T : TEXCOORD2; };
struct PrimOut { float4 P : POSITION0; float4 L : TEXCOORD0; float4 S : TEXCOORD1; float4 T : TEXCOORD2; float4 K : TEXCOORD3; };

PrimOut PrimVS(PrimIn v)
{
    PrimOut o;
    o.P = mul(v.P, uWorldViewProjection);
    o.P.z = 0;
    o.L = v.L;
    o.S = float4(v.S.xy + dotOrigin, v.S.zw);
    o.T = v.T;
    o.K = timing * v.P.w;
    return o;
}

// The beam, across its width: void core | one-dot pearl lip | rim (bright folds flowing outward over a violet
// ground, white-hot at the opening) | one-dot pearl silhouette on the collision edge | dithered halo.
float4 BeamPS(PrimOut i) : COLOR0
{
    float along = i.L.x, signedAcross = i.L.y, reach = i.L.z, halfEdge = max(i.L.w, 0.5);
    float x = abs(signedAcross);
    float hot = saturate(i.T.x), lightA = saturate(i.T.y), voidA = saturate(i.T.z);
    float maskRadius = floor(i.T.w);
    float lipHalf = clamp(frac(i.T.w), 0.35, 0.5);
    float2 cell = floor(i.S.xy + 0.25);
    float dither = Bayer4(cell) - 0.5;
    float t = i.K.x;

    // Pulses run down the beam: the core swells and the rims and halo flare inside one.
    float pa = (along - i.S.z) / 18, pb = (along - i.S.w) / 18;
    float pulse = max(exp2(-pa * pa), exp2(-pb * pb));
    // The throat (first 24 dots) is the narrower collision. The drawn edge is the collision width all the way to the
    // far end; only the core closes over the last 36 dots, so the end reads as a capped band of rim.
    float tip = saturate((reach - along) / 36);
    float edge = halfEdge * (along < 24 ? 0.78 : 1);
    // The core's wobble depends on the distance along the beam (and the side) only, so every cross-section of a
    // straight beam crosses exactly one lip dot per side.
    float n = Cloud(float2(along / 96 - 0.21 * t, signedAcross < 0 ? 0.61 : 0.13));
    float f = Flow(float2(along / 64 - 0.62 * t, x / 22 - 0.35 * t));
    float core = edge * (0.42 + 0.12 * (n - 0.5) + 0.07 * pulse) * (1 - 0.6 * hot) * sqrt(tip);

    float isVoid = step(x, core - lipHalf - 0.001);
    float isLip = step(abs(x - core), lipHalf);
    float isRim = step(core + lipHalf, x) * step(x, edge - lipHalf - 0.001);
    float isEdge = step(abs(x - edge), lipHalf);
    float haloWidth = edge * (0.22 + 0.12 * pulse + 0.1 * hot);
    float isHalo = step(edge + lipHalf, x) * step(x, edge + haloWidth);

    float drift = Flow(float2(along / 48 + 0.4 * t, x / 9 + 0.9 * t));
    float3 voidTone = VoidTone(cell, drift, t, i.K.y, dither);

    float r01 = saturate((x - core) / max(edge - core, 1));
    float fold = pow(saturate(1 - abs(sin((x - core) * 0.62 - 3.1 * t + 5 * n + 2.2 * f)) * 1.6), 2);
    // A violet seam just outside the lip sets the pearl lip off; the rim brightens to bone and pearl, then cools
    // toward the silhouette.
    float level = 0.34 + 0.44 * smoothstep(0, 0.3, r01) - 0.18 * smoothstep(0.6, 1, r01)
        + 0.26 * fold + 0.22 * (f - 0.5) + 0.28 * pulse + 0.55 * hot;
    float band = clamp(floor(level * 7 + dither * 0.9), 1, 6);
    float twinkle = step(i.K.z, Hash(cell + floor(t * 12) * 3.1)) * step(r01, 0.6);
    float3 rimTone = twinkle > 0.5 ? White : Ramp(band);
    float3 lipTone = hot + pulse > 0.55 ? White : Pearl;
    float3 edgeTone = pulse + hot > 0.5 ? Bone : PearlViolet;

    float h01 = saturate((x - edge) / max(haloWidth, 1));
    float haloLevel = (1 - h01) * (0.55 + 0.45 * pulse + 0.4 * hot);
    float haloKeep = step(Bayer4(cell) + 0.02, haloLevel * (0.65 + 0.35 * n));
    float3 haloTone = Ramp(clamp(floor(haloLevel * 3.2 + dither * 0.9), 0, 2));

    float3 rgb = isVoid > 0.5 ? voidTone : isLip > 0.5 ? lipTone : isRim > 0.5 ? rimTone : isEdge > 0.5 ? edgeTone : haloTone;
    float lit = saturate(isLip + isRim + isEdge);
    float alpha = isVoid > 0.5 ? voidA : lit > 0.5 ? lightA : isHalo * haloKeep * lightA;
    // Within the great aperture the beam is hidden: its hole is the mouth's own void and its brass ring stays in
    // front of its own light, so the beam pours out past the ring (the rosette of irises, while it breaks up).
    float r = length(float2(along, signedAcross));
    float hidden = step(0.5, maskRadius) * step(r, maskRadius + 0.5);
    float keep = step(0.004, alpha) * (1 - hidden) * step(0, along) * step(along, reach + 0.5);
    clip(keep - 0.5);
    return float4(rgb * alpha, alpha);
}

// A hole: void inside a one-dot pearl lip (an octant-exact pixel circle). Violet arms spiral inward over the outer
// share while the hole charges; the black share at the centre grows with `depth`. `hot` flashes it pearl and white.
float4 MouthPS(PrimOut i) : COLOR0
{
    float2 p = i.L.xy;
    float radius = max(i.L.z, 0.5), spiral = saturate(i.L.w), depth = saturate(i.S.z), spin = i.S.w;
    float hot = saturate(i.T.x), lightA = saturate(i.T.y), voidA = saturate(i.T.z), boost = saturate(i.T.w);
    float2 cell = floor(i.S.xy + 0.25);
    float dither = Bayer4(cell) - 0.5;
    float t = i.K.x;
    float2 q = abs(p);
    float delta = q.y >= q.x ? q.y - sqrt(max(radius * radius - q.x * q.x, 0)) : q.x - sqrt(max(radius * radius - q.y * q.y, 0));
    float inside = step(delta, 0);
    float isLip = inside * step(-1, delta);
    float r = length(p) / radius;
    float theta = atan2(p.y, p.x);
    float n = Cloud(cell / 40 + float2(0.05 * t, -0.03 * t));
    // Arms wind inward: a fixed arm phase moves toward the centre as time grows.
    float arms = pow(saturate(sin(3 * theta + 9 * r + t * (2.2 + 5 * spiral) + spin + 2.5 * n)), 2.5) * spiral;
    float annulus = saturate((r - depth) * 6);
    // A flash lights a ring inside the lip; the centre stays black.
    float armLevel = arms * annulus + 0.8 * hot * saturate((r - 0.45) * 4);
    float isArm = step(0.2, armLevel + 0.15 * dither);
    float band = clamp(floor((0.32 + 0.6 * armLevel) * 7 + dither * 0.9), 1, 6);
    float drift = Flow(float2(r * 0.6 + 0.5 * t, theta / 6.2831853 * 2 + 0.07 * t));
    float3 voidTone = VoidTone(cell, drift, t, i.K.y, dither);
    float3 inner = isArm > 0.5 ? Ramp(band) : voidTone;
    float3 lipTone = hot > 0.3 ? White : boost > 0.5 ? PearlViolet : Pearl;
    float3 rgb = isLip > 0.5 ? lipTone : inner;
    float alpha = saturate(isLip + isArm) > 0.5 ? lightA : voidA;
    clip(inside * step(0.004, alpha) - 0.5);
    return float4(rgb * alpha, alpha);
}

// A pellet's wake, tail to head: a pearl rim along its head half, a thin void line behind the head, violet and
// lilac folds flowing back, plum and dissolving at the tail.
float4 WakePS(PrimOut i) : COLOR0
{
    float u = saturate(i.L.x), halfWidth = max(i.L.w, 0.5), lengthDots = i.L.z;
    float x = abs(i.L.y) * halfWidth;
    float hot = saturate(i.T.x), lightA = saturate(i.T.y), voidA = saturate(i.T.z), seed = i.T.w;
    float2 cell = floor(i.S.xy + 0.25);
    float dither = Bayer4(cell) - 0.5;
    float t = i.K.x;
    float hw = halfWidth * (0.25 + 0.75 * u * u);
    float f = Flow(float2(u * lengthDots / 20 - 1.4 * t, x / 6 + seed));
    float fold = pow(saturate(1 - abs(sin(u * lengthDots * 0.45 + 4.2 * t + 3 * f)) * 1.5), 2);
    float coreWidth = hw * 0.42 * saturate((u - 0.55) * 2.5);
    float isVoid = step(x, coreWidth - 0.5);
    float isEdge = step(abs(x - hw), 0.5) * step(0.5, u);
    float inside = step(x, hw + 0.5);
    float level = 0.14 + 0.56 * u + 0.24 * (f - 0.5) + 0.22 * fold + 0.5 * hot;
    float band = clamp(floor(level * 7 + dither * 0.9), 0, 6);
    float twinkle = step(i.K.z, Hash(cell + floor(t * 14) * 2.3)) * step(0.35, u);
    float3 bodyTone = twinkle > 0.5 ? White : Ramp(band);
    float3 rgb = isVoid > 0.5 ? VoidTone(cell, f, t, i.K.y, dither) : isEdge > 0.5 ? (u > 0.85 ? White : PearlViolet) : bodyTone;
    float alpha = isVoid > 0.5 ? voidA : lightA;
    // The tail dissolves dot by dot.
    float keep = inside * step(Bayer4(cell) * 0.9, saturate(u * 3.2)) * step(0.004, alpha);
    clip(keep - 0.5);
    return float4(rgb * alpha, alpha);
}

technique DollLacunaEnergy
{
    pass BeamPass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 BeamPS(); }
    pass MouthPass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 MouthPS(); }
    pass WakePass { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 WakePS(); }
}
