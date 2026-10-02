// Choir of the Unmade energy material. Original repository material.
// Drawn through the shared Doll weapon layer: canvas.Energy batches render into its half-resolution Light target
// with premultiplied AlphaBlend, one fragment per art dot (1 dot = 2 world px), every output a Doll palette tone.
// Presentation only; nothing here decides hits (ChoirConcertRules owns the beam's shape and timing).
//   Hymn   the chorus beam: a throat widening from the organ mouth, a seven-tone ramp across the body with a
//          white-hot spine and a one-dot plum fringe, flowing noise, one bright wavefront running out on every
//          beat, brass-gold standing waves (one per chord voice, at most six; two under Reduced Effects), sparkle,
//          a deeper hem on Fm9 and a pearl-gold bloom on the closing open fifth.
//   Iris   the organ mouth during the inhale: the mouth darkens to a void with a one-dot pearl lip while motes
//          spiral inward.
//   Trail  note and baton trails: brightest at the head, dissolving into empty dots toward the tail.
// Uniform-dependent choices are written with lerp/step (no uniform-only branch: FNA's MojoShader mistranslates
// them).
//
// Vertex layout (C# DollPixelVertex): POSITION0 float3, TEXCOORD0 L, TEXCOORD1 S, TEXCOORD2 T (float4).
//   L = (along 0..1, across -1..1, length, half width) in dots (DollWeaponCanvas.EnergyQuad / EnergyStrip);
//   S.xy = dot-space position (floor(S.xy + .25) + dotOrigin is the absolute world dot).
//   Hymn  T = (live tick, alpha, chord * 8 + strands, seed)
//   Iris  T = (inhale progress 0..1, alpha, mouth radius / quad radius, seed)
//   Trail T = (alpha, head heat 0..1, seed, 0)
matrix uWorldViewProjection;
float2 dotOrigin;     // absolute world dot of target cell (0, 0)
float clock;          // game ticks + fraction, wrapped
float reduced;        // 1 under Reduced Effects, else 0
float throat;         // organ mouth radius in dots
float throatLength;   // dots over which the throat widens to the full beam

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

static const float Tau = 6.2831853;

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

// One brass strand of the chord: a standing wave (fixed nodes along the beam, swinging in time) whose wavelength
// follows the chord voice's ratio. Returns 1 on the strand's one-dot line, 2 on its bright crests.
float Strand(float along, float across, float local, float live, float k, float ratio, float strandsOn)
{
    float active = step(k + 0.5, strandsOn);
    float wave = sin(along * Tau * ratio / 150 + k * 0.7);
    float swing = cos(live * 0.105 * (1 + k * 0.17) + k * 1.3);
    float offset = local * (0.42 + 0.07 * k) * wave * swing;
    float on = active * step(abs(across - offset), 0.55);
    return on * (1 + step(0.8, abs(wave * swing)));
}

float4 HymnPS(PrimOut i) : COLOR0
{
    float2 cell = floor(i.S.xy + 0.25) + dotOrigin;
    float len = i.L.z, halfWidth = max(i.L.w, 0.5);
    float along = i.L.x * len;
    float across = i.L.y * halfWidth;
    float live = i.T.x;
    float alpha = saturate(i.T.y);
    float chord = floor(i.T.z / 8 + 0.01);
    float strands = i.T.z - chord * 8;
    float seed = i.T.w;

    // The throat: the organ mouth's radius widening to the beam's half width over the first throatLength dots.
    float t = saturate(along / max(throatLength, 1));
    float local = lerp(min(throat, halfWidth), halfWidth, t * t * (3 - 2 * t));
    float r = abs(across) / max(local, 0.5);
    clip(min(min(along + 0.5, len + 0.5 - along), 1.0001 - r));
    float core = 1 - saturate(r);

    // Body: a bright core falling to plum at the hem, stirred by two noise fields flowing away from the mouth.
    float flowA = tex2D(noiseA, float2(along / 46 - live * 0.11, across / 30 + seed * 0.37)).r;
    float flowB = tex2D(noiseB, float2(along / 97 - live * 0.043, across / 21 - seed * 0.21)).r;
    float intensity = 0.16 + 0.84 * pow(core, 0.55) + (flowA * 0.6 + flowB * 0.4 - 0.5) * (0.42 - 0.22 * core);

    // One wavefront per beat runs from the mouth to the far end in 12 ticks; Reduced Effects keeps only the two
    // on the chord changes.
    float beat = live - floor(live / 36) * 36;
    float front = saturate(1 - abs(along - beat * len / 12) / 5) * step(beat, 13);
    float change = saturate(step(abs(live - 78), 6.5) + step(abs(live - 150), 6.5));
    intensity += front * lerp(1, change, reduced) * 0.5;

    // Chord colour: Fm9 deepens the hem, the closing open fifth blooms toward the core.
    float fm9 = step(0.5, chord) * step(chord, 1.5), fifth = step(1.5, chord);
    intensity += fifth * 0.12 * core - fm9 * 0.14 * (1 - core);
    // The plum fringe on the outermost dot.
    float fringe = step(local - 1, abs(across)) * step(1.5, local);
    intensity = lerp(intensity, 0.05, fringe);

    float band = clamp(floor(saturate(intensity) * 7 + (Bayer4(cell) - 0.5) * 0.9), 0, 6);
    float3 colour = Ramp(band);
    // The open fifth's pearl-gold bloom: the pearl band around the white core turns brass light.
    colour = lerp(colour, BrassLight, fifth * step(3.5, band) * step(band, 4.5));
    // The white-hot spine.
    colour = lerp(colour, White, step(abs(across), 0.75));

    // Brass standing waves, one per chord voice (Reduced Effects: at most two).
    float strandsOn = min(strands, lerp(6, 2, reduced));
    float s = max(max(Strand(along, across, local, live, 0, 1, strandsOn), Strand(along, across, local, live, 1, 1.5, strandsOn)),
                  max(Strand(along, across, local, live, 2, 2, strandsOn), Strand(along, across, local, live, 3, 2.5, strandsOn)));
    s = max(s, max(Strand(along, across, local, live, 4, 3, strandsOn), Strand(along, across, local, live, 5, 4, strandsOn)));
    s *= step(r, 0.92);
    colour = lerp(colour, BrassLight, step(0.5, s));
    colour = lerp(colour, Bone, step(1.5, s));

    // Sparkle.
    float sparkle = step(0.988, Hash(cell + floor(clock / 3) * float2(17.3, 5.1) + seed)) * step(r, 0.85);
    colour = lerp(colour, White, sparkle);
    return float4(colour * alpha, alpha);
}

float4 IrisPS(PrimOut i) : COLOR0
{
    float2 cell = floor(i.S.xy + 0.25) + dotOrigin;
    float radius = max(i.L.w, 1);
    float2 p = float2(i.L.x * i.L.z - i.L.z * 0.5, i.L.y * i.L.w);
    float d = length(p);
    float rho = d / radius;
    float progress = saturate(i.T.x);
    float alpha = saturate(i.T.y);
    float mouth = saturate(i.T.z);
    float seed = i.T.w;
    float dither = Bayer4(cell) - 0.5;

    // The void inside the mouth darkens over the inhale: ink at the centre, plum toward the lip.
    float inside = step(rho, mouth);
    float3 voidTone = lerp(Plum, Ink, step(rho + dither * 0.12, mouth * 0.62));
    // The one-dot pearl lip on the mouth's rim.
    float lip = step(abs(d - mouth * radius), 0.6);
    // Motes spiralling inward on four arms.
    float angle = atan2(p.y, p.x);
    float s = angle / Tau * 4 + rho * 5 + clock * 0.09 + seed;
    float arm = step(0.8, frac(s));
    float sparse = step(0.5, Hash(floor(cell * 0.5) + floor(clock / 4)));
    float mote = arm * sparse * step(mouth + 0.1, rho) * step(rho, 1) * progress;

    float a = max(max(inside * progress, lip * (0.45 + 0.55 * progress)), mote) * alpha;
    clip(a - 0.004);
    float3 colour = lerp(voidTone, lerp(Lilac, White, step(0.93, frac(s))), mote * (1 - inside));
    colour = lerp(colour, lerp(Lilac, PearlViolet, step(0.5, progress)), lip);
    return float4(colour * a, a);
}

float4 TrailPS(PrimOut i) : COLOR0
{
    float2 cell = floor(i.S.xy + 0.25) + dotOrigin;
    float along = saturate(i.L.x);
    float across = abs(i.L.y);
    float alpha = saturate(i.T.x);
    float heat = saturate(i.T.y);
    float seed = i.T.z;
    float flow = tex2D(noiseA, float2(i.L.x * i.L.z / 18 - clock * 0.15, seed * 0.41)).r;
    float intensity = heat * pow(along, 1.4) * (1 - across * 0.7) + (flow - 0.5) * 0.3 * along;
    float v = saturate(intensity) * 7 + (Bayer4(cell) - 0.5) * 0.9;
    clip(min(v - 0.6, 1.0001 - across));
    float3 colour = Ramp(clamp(floor(v), 0, 6));
    float sparkle = step(0.97, Hash(cell + floor(clock / 2) + seed)) * step(0.4, along);
    colour = lerp(colour, White, sparkle);
    return float4(colour * alpha, alpha);
}

technique DollChoirEnergy
{
    pass Hymn { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 HymnPS(); }
    pass Iris { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 IrisPS(); }
    pass Trail { VertexShader = compile vs_3_0 PrimVS(); PixelShader = compile ps_3_0 TrailPS(); }
}
