// Original silk thread material for Luminance primitive ribbons. The ribbon is
// decorative: accepted lanes, chains and spokes own every danger footprint.
matrix uWorldViewProjection;
sampler noise : register(s1);
float clock, completionScale;
float4 signal; // opacity, tension, heat, seed
float3 tint;
float2 footprint; // world length, half-width
struct PI { float4 P:POSITION0; float4 C:COLOR0; float3 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(PI v)
{
    VO o = (VO)0; o.P = mul(v.P, uWorldViewProjection); o.P.z = 0; o.C = v.C;
    o.U = float2(v.U.x * completionScale, (v.U.y - .5) / max(.001, v.U.z) + .5);
    return o;
}
float4 PS(VO i) : COLOR0
{
    float x = saturate(i.U.x);
    float px = abs(i.U.y * 2 - 1) * footprint.y;
    float along = x * footprint.x;
    float n = tex2D(noise, float2(along * .006 - clock * .4, i.U.y * .35 + signal.w)).r;
    float coreWidth = .55 + signal.z * 1.2 + signal.y * .25;
    float core = exp2(-pow(px / coreWidth, 2));
    float halo = exp2(-pow(px / max(1, footprint.y * .6), 2)) * (.10 + .24 * signal.z + .08 * signal.y);
    float run = frac(along / 180 - clock * (.35 + 1.6 * signal.y) + signal.w);
    float glint = pow(saturate(1 - abs(run - .5) * 2), 14) * (.25 + .75 * signal.y) * core;
    float3 silk = lerp(tint, float3(1, 1, 1), .65);
    float3 c = tint * halo * (.8 + .4 * n) + silk * core * (.55 + .35 * signal.y + .45 * signal.z) + float3(1, .97, 1) * glint;
    float a = saturate(core * .85 + halo * .8 + glint * .5);
    return float4(c, a * .78) * signal.x * i.C;
}
technique EbonSilk { pass AutoloadPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 PS(); } }
