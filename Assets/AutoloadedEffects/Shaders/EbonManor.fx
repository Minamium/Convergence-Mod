// Original Ebon Manor materials: the moonlit hall, woven furniture, tailor's
// chalk lanes, shears tears and stitch hoops. Every danger footprint is drawn
// from accepted plan geometry; noise only moves pigment inside it.
matrix uWorldViewProjection;
sampler art : register(s0);
sampler noise : register(s1);
sampler detail : register(s2);
sampler second : register(s3);
float clock;
float4 signal;
float4 shape;
float4 region;
float3 tint;
float weaveDensity;
struct QI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(QI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.C=v.C; o.U=v.U; return o; }

float4 Region(float2 p) { return tex2D(art, region.xy + saturate(p) * region.zw); }

// Warp and weft threads appear first, then the surface fills between them.
// reveal 0 = only loose silk, 1 = the finished authored surface.
float Weave(float2 p, float reveal, float seed, out float lattice, out float edge)
{
    float n = tex2D(noise, p * 1.7 + float2(seed * .37, seed * .11)).r;
    float warp = abs(frac((p.x + p.y) * weaveDensity + n * .5) - .5) * 2;
    float weft = abs(frac((p.x - p.y) * weaveDensity - n * .5) - .5) * 2;
    float thread = min(warp, weft);
    float field = n * .6 + thread * .4;
    float r = reveal * 1.3 - .15;
    float solid = smoothstep(field - .04, field + .04, r);
    lattice = (1 - smoothstep(.08, .22, thread)) * smoothstep(field - .45, field - .1, r) * (1 - solid) * saturate(reveal * 4);
    edge = exp(-abs(r - field) * 26) * (1 - solid) * step(.002, reveal);
    return solid;
}

// signal: opacity, weave, flash, seed. shape: texel step xy, shade, rim suppression.
float4 PropPS(VO i) : COLOR0
{
    float2 p = i.U;
    float4 a = Region(p);
    float lattice, edge;
    float solid = Weave(p, signal.y, signal.w, lattice, edge);
    float rim = saturate(a.a - Region(p - float2(shape.x, 0)).a) + saturate(a.a - Region(p - float2(0, shape.y)).a);
    float3 c = a.rgb * solid * (1 - shape.z * .4);
    c += float3(.62, .68, .84) * saturate(rim) * .42 * solid * (1 - shape.w);
    c += float3(.90, .91, .98) * (lattice * .75 + edge * 1.1) * a.a;
    c = lerp(c, float3(1, .92, .94) * a.a, saturate(signal.z) * .7 * solid);
    float alpha = a.a * saturate(solid + lattice * .7 + edge * .8);
    return float4(c, alpha) * signal.x * i.C;
}

// Noirette's authored pixels, sampled at texel centres so the weave dissolves
// whole logical pixels. signal: opacity, weave, reduced, seed. shape: flash, rim, cell size.
float4 BodyPS(VO i) : COLOR0
{
    float2 cell = shape.zw;
    float2 q = (floor(saturate(i.U) * cell) + .5) / cell;
    float4 a = Region(q);
    float lattice, edge;
    float solid = Weave(q * float2(.75, 1), signal.y, signal.w, lattice, edge);
    float2 texel = 1 / cell;
    float rim = saturate(a.a - Region(q - float2(texel.x, 0)).a) * .6 + saturate(a.a - Region(q - float2(0, texel.y)).a);
    float3 c = a.rgb * solid;
    c += float3(.60, .66, .82) * saturate(rim) * shape.y * solid;
    c += float3(.92, .92, 1) * (lattice * .85 + edge) * a.a;
    c = lerp(c, float3(1, .90, .93) * a.a, saturate(shape.x) * .55 * solid);
    float alpha = a.a * saturate(solid + lattice * .7 + edge * .9);
    return float4(c, alpha) * signal.x * i.C;
}

// A dark silk haze behind Noirette with a thin moonlit ring: contrast, not glow.
// signal: opacity, ring strength, reduced, seed.
float4 AuraPS(VO i) : COLOR0
{
    float2 p = i.U * 2 - 1;
    float r = length(p);
    float n = tex2D(noise, i.U * 1.3 + float2(clock * .03 + signal.w, -clock * .05)).r;
    float m = tex2D(detail, i.U * 2.1 + float2(-clock * .04, clock * .02) + n * .2).r;
    float smoke = saturate(1 - r);
    smoke = smoke * smoke * (.55 + .45 * n) * (1 - signal.z * .4);
    float ring = exp2(-pow((r - .56 - (m - .5) * .06) * 22, 2)) * signal.y * (.5 + .5 * m);
    float a = smoke * .42 * signal.x;
    float3 c = float3(.012, .008, .016) * a + float3(.55, .60, .80) * ring * .10 * signal.x;
    return float4(c, a) * i.C;
}

// Tailor's chalk lane on an accepted straight footprint. U.x runs along,
// U.y across the full diameter. shape: length, radius, warning progress, live.
// signal: opacity, heat, reduced, seed.
float4 LanePS(VO i) : COLOR0
{
    float along = i.U.x * shape.x;
    float d = abs(i.U.y * 2 - 1);
    float px = d * shape.y;
    float inside = shape.y - px;
    float n = tex2D(noise, float2(along * .004 - clock * .05, i.U.y * .8 + signal.w)).r;
    float dash = step(frac((along - clock * 20 * (1 - signal.z)) / 26), .56);
    float edgeLine = exp2(-pow((inside - 2.2) / 1.3, 2)) * dash;
    float core = exp2(-pow(px / (.9 + shape.z * .7), 2)) * (.20 + .80 * shape.z);
    float run = frac(along / 220 - clock * (.3 + 1.4 * shape.z));
    float glint = pow(saturate(1 - abs(run - .5) * 2), 12) * shape.z * core;
    float body = (.05 + .10 * shape.z) * (.65 + .35 * n) * (1 - smoothstep(.97, 1, d));
    float rimGlow = exp2(-pow(inside / 6, 2)) * (.16 + .30 * shape.z) * (1 - smoothstep(.99, 1, d));
    float ends = smoothstep(0, 24, along) * smoothstep(0, 24, shape.x - along);
    float3 chalk = lerp(tint, float3(.92, .56, .64), saturate(shape.z * shape.z * signal.y));
    float3 c = chalk * (edgeLine * .95 + core * .8 + body + rimGlow) + float3(1, .97, .98) * glint;
    float a = saturate(edgeLine * .85 + core * .6 + body * 1.4 + rimGlow + glint) * ends;
    // Live: the whole honest band is lit, brightest along its spine.
    float fill = shape.w * (1 - smoothstep(.93, 1, d));
    float3 hot = lerp(float3(.96, .62, .70), float3(1, .97, .98), exp2(-pow(px / max(1, shape.y * .38), 2)));
    c = lerp(c, hot * (.72 + .28 * n), fill);
    a = lerp(a, .82 * fill + .08, shape.w);
    return float4(c * a, a) * signal.x * i.C;
}

// A live shears cut: a ragged slit through the accepted band with fibres at the
// torn edges. shape: length, radius, ticks since fire, live duration. signal: opacity, -, reduced, seed.
float4 TearPS(VO i) : COLOR0
{
    float along = i.U.x * shape.x;
    float across = i.U.y * 2 - 1;
    float d = abs(across);
    float t = saturate(shape.z / max(1, shape.w));
    float open = 1 - exp(-shape.z / 1.8);
    float n = tex2D(noise, float2(along * .005 + signal.w, i.U.y * .9 + clock * .08)).r;
    float f = tex2D(detail, float2(along * .012, i.U.y * 2.2 - clock * .3)).r;
    float ragged = d + (n - .5) * .16;
    float slit = 1 - smoothstep(open * .96 - .04, open * .96 + .02, ragged);
    float edge = exp(-abs(ragged - open * .96) * 24) * open;
    float core = exp2(-pow(d / (.06 + .16 * open), 2));
    float fibres = pow(saturate(1 - abs(frac(along * .045 + n * 2) - .5) * 5), 3) * slit * (1 - core) * (1 - signal.z * .6);
    float fade = 1 - smoothstep(.6, 1, t);
    float3 c = float3(.035, .025, .02) * slit + float3(1, .95, .92) * core * 1.2
        + lerp(float3(.94, .60, .56), float3(1, .93, .84), f) * edge * 1.2 + float3(1, .94, .86) * fibres * .55;
    float a = saturate(slit * .78 + core + edge + fibres * .5);
    return float4(c, a) * fade * signal.x * i.C;
}

// Stitch hoops. shape: kind (0 stack, 1 spread), clock progress, radius, reduced.
// signal: opacity, verdict flash, failed, seed.
float4 RingPS(VO i) : COLOR0
{
    float2 p = i.U * 2 - 1;
    float r = length(p);
    float ang = atan2(p.y, p.x);
    float inside = (1 - r) * shape.z;
    float perimeter = 6.2831853 * shape.z;
    float along = (ang / 6.2831853 + .5) * perimeter;
    float dir = shape.x < .5 ? -1 : 1;
    float motion = 1 - shape.w * .7;
    float dash = step(frac((along + dir * clock * 24 * motion) / 28), .55);
    float boundary = exp2(-pow((inside - 2.4) / 1.5, 2));
    float running = exp2(-pow((inside - 10) / 2.2, 2)) * dash;
    // A broken inner ring contracts from 92% to 12% as the call closes: a clock only.
    float clockR = lerp(.92, .12, saturate(shape.y));
    float broken = step(frac(ang * 12 / 6.2831853 + clock * .08 * dir * motion), .72);
    float clockRing = exp2(-pow((r - clockR) * shape.z / 2.2, 2)) * broken;
    float n = tex2D(noise, i.U * 2.3 + float2(clock * .02, signal.w)).r;
    float under = (1 - smoothstep(.95, 1, r)) * (.09 + .05 * n) * (1 - shape.w * .3);
    float3 thread = lerp(tint, float3(1, .98, .95), .35);
    float3 c = thread * (boundary + running * .75 + clockRing * .55) + float3(1, .96, .97) * boundary * .25;
    float lit = saturate(boundary * .95 + running * .6 + clockRing * .45);
    float flash = signal.y * (1 - smoothstep(.9, 1, r));
    float3 verdict = signal.z > .5 ? float3(.95, .45, .55) : float3(.98, .88, .64);
    c = c * lit + verdict * flash * (.4 + .6 * n);
    float a = saturate(lit + under + flash * .5);
    return float4(c, a) * signal.x * i.C;
}

// Glass, wood and silk fragments on analytic arcs. shape.x: kind. signal: opacity, glint.
float4 ShardPS(VO i) : COLOR0
{
    float2 p = i.U;
    float env = saturate(1 - abs(p.y - .5) * 2 / max(.05, sin(p.x * 3.14159)));
    float a = saturate(env * 4) * smoothstep(0, .1, p.x) * smoothstep(1, .9, p.x);
    float glint = pow(saturate(sin(p.x * 3.14159 + signal.y)), 10);
    float3 glass = lerp(float3(.50, .56, .68), float3(1, 1, 1), pow(env, 3) * .7 + glint * .5);
    float3 wood = lerp(float3(.13, .08, .05), float3(.50, .36, .20), env) + float3(.3, .25, .2) * glint * .3;
    float3 silk = float3(.92, .92, .99) * (.55 + env * .45);
    float3 c = shape.x < .5 ? glass : shape.x < 1.5 ? wood : silk;
    return float4(c * a, a) * signal.x * i.C;
}

// Radial light. shape: ring radius (0 = soft disc), ring width, additive share.
float4 GlowPS(VO i) : COLOR0
{
    float2 p = i.U * 2 - 1;
    float r = length(p);
    float g = shape.x <= 0 ? exp2(-r * r * 4.5) : exp2(-pow((r - shape.x) / max(.01, shape.y), 2));
    g *= 1 - smoothstep(.9, 1, r);
    return float4(tint * g, g * (1 - shape.z)) * signal.x * i.C;
}

// The hall. U is the screen quad; region maps it into the painting (parallax).
// signal: fade, tear, reduced, tension. shape: candles, pulse, exposure, flash.
float4 BackdropPS(VO i) : COLOR0
{
    float2 uv = region.xy + i.U * region.zw;
    float open = signal.y;
    float n0 = tex2D(noise, float2(uv.y * 2.3, .37)).r;
    float n1 = tex2D(detail, float2(uv.y * 11, .61)).r;
    float seam = abs(uv.x - .5) - ((n0 - .5) * .045 + (n1 - .5) * .018) * saturate(open * 3);
    float gap = open * .64;
    float torn = step(seam, gap);
    float2 old = uv;
    old.x = .5 + sign(uv.x - .5) * max(0, abs(uv.x - .5) - gap * .3);
    float3 color = lerp(tex2D(art, old).rgb, tex2D(second, uv).rgb, torn);
    float tearing = saturate(open * 8) * (1 - saturate((open - .97) * 40));
    float seamLight = (exp(-abs(seam - gap) * 520) * .65 + exp(-abs(seam - gap) * 70) * .10) * tearing;
    float curl = exp(-max(0, seam - gap) * 45) * (1 - torn) * tearing;
    float fibres = pow(saturate(1 - abs(frac(uv.y * 140 + n1 * 4) - .5) * 7), 5)
        * smoothstep(gap - .025, gap, seam) * torn * tearing;

    // Painted candle flames breathe; the opening lights them outward from the centre.
    float warm = saturate((color.r - color.b * 1.15) * 3.5) * smoothstep(.30, .75, max(color.r, color.g));
    float ignite = smoothstep(abs(uv.x - .5) * 1.1, abs(uv.x - .5) * 1.1 + .05, shape.x);
    float flick = .78 + .22 * tex2D(noise, float2(uv.x * 7 + clock * .9, uv.y * 5 - clock * 1.7)).r;
    color *= 1 - warm * (1 - ignite * flick * 1.25);

    // Moonlight shafts from the great window (or the torn roof in the Finale).
    float2 L = lerp(float2(.5, .2), float2(.53, .09), torn);
    float2 d = uv - L;
    float ang = atan2(d.x, d.y * 1.6);
    float dist = length(d * float2(1, .8));
    float rays = tex2D(noise, float2(ang * 1.4 + clock * .004, .5 + clock * .002)).r;
    rays = pow(saturate(rays * 1.3 - .25), 3);
    float cone = (1 - smoothstep(.1, .75, abs(ang))) * smoothstep(0, .12, d.y) * exp(-dist * 1.6);
    float shafts = rays * cone * (1 - signal.z * .5);
    float2 mote = uv * float2(3.2, 1.8) + float2(clock * .004, -clock * .008);
    float motes = pow(saturate(tex2D(detail, mote).r), 16) * 5 * cone * (1 - signal.z);
    float fogMask = smoothstep(.70, .96, uv.y);
    float fog = fogMask * (.35 + .65 * tex2D(noise, float2(uv.x * 1.5 - clock * .01, uv.y * 3 + clock * .004)).r);

    // From the second act, silver strands hang taut across the upper hall.
    float strands = 0;
    [unroll] for (int k = 0; k < 5; k++)
    {
        float a0 = .35 + k * .41;
        float2 dir = float2(cos(a0), sin(a0));
        float off = dot(uv - float2(.5, .35), float2(-dir.y, dir.x)) - (k - 2) * .09;
        float vib = sin(dot(uv, dir) * 40 + clock * (3 + k)) * .0012 * (.3 + shape.y);
        strands += exp2(-pow((off + vib) * 900, 2)) * (.45 + .55 * frac(k * .618 + clock * .05));
    }
    strands *= saturate(signal.w) * (1 - smoothstep(.6, .95, uv.y)) * .32;

    float3 moon = float3(.62, .70, .90);
    color *= shape.z;
    color += moon * (shafts * .17 + motes * .45) * (.45 + .55 * shape.z);
    color = lerp(color, color * .8 + float3(.10, .11, .15) * fog, fog * .5);
    color *= 1 - curl * .55;
    color += moon * strands + float3(1, .96, .90) * (seamLight + fibres * .3);
    color += float3(1, .95, .97) * shape.w * .30;
    float2 v = i.U * 2 - 1;
    color *= 1 - .45 * pow(saturate(length(v * float2(.8, 1)) - .3), 1.5);
    return float4(color * signal.x, signal.x) * i.C;
}

// Foreground proscenium: curtains sway at the screen edges, darker than the hall.
// signal: fade, -, reduced, -. shape: -, -, exposure, -.
float4 FramePS(VO i) : COLOR0
{
    float2 uv = region.xy + i.U * region.zw;
    float side = saturate((abs(uv.x - .5) - .36) * 8);
    float sway = sin(uv.y * 5 + clock * .7) * .006 + sin(uv.y * 11 - clock * 1.1) * .002;
    uv.x += sway * side * uv.y * uv.y * (1 - signal.z);
    float4 a = tex2D(art, uv);
    float n = tex2D(noise, uv * float2(2, 3) + clock * .01).r;
    float3 c = a.rgb * (.60 + .12 * n) * lerp(.35, 1, shape.z);
    float lip = saturate(a.a - tex2D(art, uv + float2(sign(.5 - uv.x) * .004, 0)).a);
    c += float3(.55, .62, .80) * lip * .5 * shape.z;
    return float4(c, a.a) * signal.x * i.C;
}

technique EbonManor
{
    pass AutoloadPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 PropPS(); }
    pass BodyPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 BodyPS(); }
    pass AuraPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 AuraPS(); }
    pass LanePass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 LanePS(); }
    pass TearPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 TearPS(); }
    pass RingPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 RingPS(); }
    pass ShardPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 ShardPS(); }
    pass GlowPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 GlowPS(); }
    pass BackdropPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 BackdropPS(); }
    pass FramePass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 FramePS(); }
}
