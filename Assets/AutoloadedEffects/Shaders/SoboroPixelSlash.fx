// Soboro pixel slash. Original repository material.
// AutoloadPass evaluates the swept crescent once per art pixel inside a
// half-resolution target and quantizes it to a six-tone violet palette.
// CompositePass upscales that target with point sampling, adds the dark
// one-pixel outline and a restrained violet glow. Inputs are the accepted cut
// clock and swept blade path; nothing here decides hits.
matrix uWorldViewProjection;
float cutStep;
float fade;
float thick;
float tau;
float echo;
float reduced;
float2 noiseShift;
float2 texel;
float glowStrength;

sampler sceneTex : register(s0);
sampler noiseTex : register(s1);

static const float3 Tone1 = float3(0.086, 0.020, 0.188); // outline
static const float3 Tone2 = float3(0.243, 0.063, 0.541); // deep violet (dithered)
static const float3 Tone3 = float3(0.439, 0.157, 0.871); // violet
static const float3 Tone4 = float3(0.667, 0.424, 1.000); // lilac
static const float3 Tone5 = float3(0.863, 0.769, 1.000); // pale
static const float3 Tone6 = float3(1.000, 1.000, 1.000); // white-hot edge

struct SlashIn { float4 P : POSITION0; float2 U : TEXCOORD0; float4 D : TEXCOORD1; };
struct SlashOut { float4 P : POSITION0; float2 U : TEXCOORD0; float4 D : TEXCOORD1; float2 Q : TEXCOORD2; };

SlashOut SlashVS(SlashIn v)
{
    SlashOut o;
    o.P = mul(v.P, uWorldViewProjection);
    o.P.z = 0;
    o.U = v.U;
    o.D = v.D;
    o.Q = v.P.xy;
    return o;
}

float3 Tone(float level)
{
    return level > 5.5 ? Tone6 : level > 4.5 ? Tone5 : level > 3.5 ? Tone4 : level > 2.5 ? Tone3 : Tone2;
}

// U.x: 0 at the start of the swing .. 1 at the blade (shape only)
// U.y: radius / reach
// D.x: ticks since the tip crossed this point (cooling)
// D.y: ticks after release when it crossed (stable noise coordinate)
// D.z: master opacity
float4 SlashPS(SlashOut i) : COLOR0
{
    float s = saturate(i.U.x);
    // after the cut, fragments drift a little outward while they cool
    float rho = i.U.y - 0.06 * pow(saturate(fade), 0.8);
    float trail = max(i.D.x, 0);
    float crossed = i.D.y;
    float shape = pow(max(sin(3.14159265 * pow(max(s, 0.0001), 0.72)), 0.0001), 0.62);
    float thickness = thick * shape * (1 - 0.45 * fade);

    float edge = tex2D(noiseTex, float2(crossed * 0.085, rho * 0.44) + noiseShift).r;
    float splinter = tex2D(noiseTex, float2(crossed * 0.20, rho * 0.19) + noiseShift * 1.7).g;
    float streak = tex2D(noiseTex, float2(crossed * 0.019, rho * 1.25) + noiseShift * 0.6).b;
    float dissolve = tex2D(noiseTex, float2(crossed * 0.11 + 0.31, rho * 0.62) - noiseShift).g;

    float inner = 1 - thickness + (edge - 0.5) * 0.34 * (0.35 + thickness)
        + max(splinter - 0.5, 0) * 0.30 * thickness;
    float outer = 1.0 + 0.02 * sin(crossed * 1.6 + cutStep * 2.1)
        + 0.012 * sin(crossed * 3.9 - cutStep) + 0.05 * shape;
    float heat = exp(-trail / (tau * 1.6)) * pow(saturate(1 - fade), 1.6);
    float across = saturate((rho - inner) / max(outer - inner, 0.001));
    float value = (0.22 + 0.78 * heat) * (0.42 + 0.58 * pow(across, 0.9)) * (0.62 + 0.76 * streak);
    // the outer rim is the tip's path: a bright line while the cut is fresh
    float rimLine = step(outer - rho, 0.030 + 0.03 * heat) * step(fade, 0.45);
    value = lerp(value, max(value, 0.70 + 0.3 * heat), rimLine);
    float level = value > 0.74 ? 6 : value > 0.56 ? 5 : value > 0.40 ? 4 : value > 0.26 ? 3 : 2;
    float inBand = step(inner, rho) * step(rho, outer);

    // heavy finisher: a thinner second crescent inside the first
    float eCenter = 0.60 + 0.03 * sin(crossed * 0.9);
    float eHalf = (0.05 * shape + 0.008) * (1 - saturate(fade * 1.5)) * (1 - reduced * 0.35);
    float inEcho = echo * step(abs(rho - eCenter), eHalf);
    float eValue = (0.25 + 0.75 * heat) * (0.8 + 0.3 * streak);
    float eLevel = eValue > 0.72 ? 5 : eValue > 0.5 ? 4 : eValue > 0.3 ? 3 : 2;
    level = inBand > 0.5 ? max(level, eLevel * inEcho) : eLevel;

    // torn decay after the live window, from the old tail toward the blade
    float tear = saturate(fade * 1.45 - s * 0.3);
    // no tear while live: the noise contains exact zeros, which must not punch holes
    float alive = step(pow(tear, 1.1), dissolve + 0.001) * saturate(inBand + inEcho);
    // the deepest tone is a checker dither whose holes take the composite
    // outline; once the cut is tearing out it is dropped so fragments stay clean
    float checker = fmod(floor(i.Q.x) + floor(i.Q.y), 2);
    float deep = level > 2.5 ? 1 : (1 - checker) * step(fade, 0.3);
    float keep = alive * deep;
    clip(keep - 0.5);
    return float4(Tone(level), 1) * i.D.z;
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

float Brightness(float4 c)
{
    return c.a * saturate(dot(c.rgb, float3(0.3, 0.3, 0.4)) * 1.6 - 0.6);
}

float4 CompositePS(CompositeOut i) : COLOR0
{
    float4 c = tex2D(sceneTex, i.U);
    float4 east = tex2D(sceneTex, i.U + float2(texel.x, 0)), west = tex2D(sceneTex, i.U - float2(texel.x, 0));
    float4 south = tex2D(sceneTex, i.U + float2(0, texel.y)), north = tex2D(sceneTex, i.U - float2(0, texel.y));
    float neighbours = east.a + west.a + south.a + north.a;
    // a pinhole enclosed by bright pixels is filled, not outlined (no dark specks);
    // holes inside the dark dither keep the outline tone
    float4 average = (east + west + south + north) * 0.25;
    float enclosedBright = step(3.5, neighbours) * step(0.45, dot(average.rgb, float3(0.3, 0.3, 0.4)));
    float glow = 0;
    [unroll] for (int k = 0; k < 8; k++)
    {
        float angle = k * 0.785398 + 0.39;
        float2 direction = float2(cos(angle), sin(angle)) * texel;
        glow += Brightness(tex2D(sceneTex, i.U + direction * 2.2)) + 0.6 * Brightness(tex2D(sceneTex, i.U + direction * 4.6));
    }
    glow = saturate(glow / 12.8 * glowStrength);
    float4 outline = float4(Tone1, 1);
    float4 halo = float4(float3(0.52, 0.28, 1.0) * glow, glow);
    float4 empty = neighbours > 0.5 ? outline : halo;
    empty = enclosedBright > 0.5 ? float4(average.rgb, 1) : empty;
    return c.a > 0.5 ? c : empty;
}

technique SoboroPixelSlash
{
    pass AutoloadPass { VertexShader = compile vs_3_0 SlashVS(); PixelShader = compile ps_3_0 SlashPS(); }
    pass CompositePass { VertexShader = compile vs_3_0 CompositeVS(); PixelShader = compile ps_3_0 CompositePS(); }
}
