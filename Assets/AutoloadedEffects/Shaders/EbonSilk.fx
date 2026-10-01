// Original silk thread material for Luminance primitive ribbons. The ribbon is
// decorative: accepted lanes, chains and spokes own every danger footprint.
// Classic silk (razor.x = 0) is the hanging, control and ceremony thread, and
// every harmless strand.
// Razor silk (razor.x = the honest hit radius in px, razor.z < 0) is a live,
// damaging strand: a thin white-hot core in a soft moon-silver bloom, a band
// gathered toward the core that still reaches the true radius with a faint
// dusty-rose fringe, energy and fine filaments flowing along it, small star
// glints and twinkles riding it, a snap bead racing in from both anchors that
// ignites the core, and a decaying twang of the core only (the ribbon itself
// stays on the honest straight line).
// Frayed silk (razor.z = 0..1) is a strand that stopped dealing damage: no
// band and no white heat, it thins, parts and frays into drifting motes.
matrix uWorldViewProjection;
sampler noise : register(s1);
float clock, completionScale;
float4 signal; // opacity, tension, heat, seed
float3 tint;
float2 footprint; // world length, half-width
float4 razor; // hit radius px (0 = classic silk), ticks since snap, fray (<0 live, 0..1 dissolving), reduced
float2 twang; // core amplitude px, phase rate per tick
float4 razorEx; // fade the start end (0/1), fade the end end (0/1), end fade length px, core sigma px (>= .75 screen px)
float4 snap; // live: idle core floor, bead front px from each anchor, bead strength, flash | frayed: strand strength, mote strength
float4 breaks; // frayed: first gap centre px, half-width px, second gap centre px, half-width px (negative = closed)
struct PI { float4 P:POSITION0; float4 C:COLOR0; float3 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(PI v)
{
    VO o = (VO)0; o.P = mul(v.P, uWorldViewProjection); o.P.z = 0; o.C = v.C;
    o.U = float2(v.U.x * completionScale, (v.U.y - .5) / max(.001, v.U.z) + .5);
    return o;
}

float4 Classic(VO i)
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

// Sin-free hash: stable for large seeds and cells on every GPU.
float Hash(float x)
{
    x = frac(x * .1031 + frac(signal.w * .618) * 7.13);
    x *= x + 33.33;
    x *= x + x;
    return frac(x);
}
// Ends that finish in open space (a hub, a broken end) fade over razorEx.z px;
// anchored ends stay square.
float EndFade(float along, float L)
{
    return lerp(1, smoothstep(0, razorEx.z, along), razorEx.x) * lerp(1, smoothstep(0, razorEx.z, L - along), razorEx.y);
}

// Every uniform-only quantity (snap timing, gaps, gains, sigma floors) is
// computed on the CPU (EbonMaterials) and passed in: the FNA effect runtime
// mis-evaluates some fx_2_0 preshader code, so none is generated here.
float4 Razor(VO i)
{
    float x = saturate(i.U.x);
    float L = footprint.x, R = max(1, razor.x);
    float along = x * L;
    float across = (i.U.y * 2 - 1) * footprint.y;
    float pa = abs(across), s = pa / R;
    float calm = 1 - razor.w * .7, tick = clock * 60, fine = razorEx.w, flash = snap.w;
    // Twang: a decaying standing wave moves the core inside the honest band.
    float wave = sin(x * 3.14159) * sin(x * 9.42478 + tick * twang.y) * twang.x;
    float pc = abs(across - wave);
    // Energy flowing along the strand and two fine fibres twisting round the core.
    float n1 = tex2D(noise, float2(along * .0036 - clock * 1.9 * calm, .21 + signal.w)).r;
    float n2 = tex2D(noise, float2(along * .0022 - clock * 1.2 * calm, .63 + signal.w * 1.7)).r;
    float energy = smoothstep(.2, .9, n1 * .75 + n2 * .35);
    // Snap taut: a bead of light races in from both anchors and ignites the
    // core behind it; ahead of it the core idles low. Band and glow show the
    // honest footprint from the first tick.
    float fromEnd = min(along, L - along);
    float ignite = lerp(snap.x, 1, 1 - smoothstep(snap.y - 6, snap.y + 10, fromEnd));
    // The bead is a short comet: crisp ahead, a longer tail toward its anchor.
    float ahead = fromEnd - snap.y;
    float bead = exp2(-pow(ahead / lerp(30, 9, step(0, ahead)), 2)) * exp2(-pow(pa / (R * .75), 2)) * snap.z;

    float core = exp2(-pow(pc / (fine * (1 + .35 * flash)), 2)) * ignite * (.8 + .4 * energy);
    float glow = exp2(-pow(pc / (1.9 + .6 * flash), 2));
    float bloom = exp2(-pow(pa / (R * .6), 2));
    // The honest band: light gathered toward the core that still reaches the
    // true radius (50% within 2.3 px of it), tinted dusty rose at its edge.
    float band = 1 - smoothstep(R - 5, R + .5, pa);
    float fringe = smoothstep(.35, 1, s);
    float filaments = (exp2(-pow((across - sin(along * .021 - tick * .21 * calm + signal.w * 7) * R * .55) / .7, 2))
        + exp2(-pow((across + sin(along * .017 - tick * .17 * calm + signal.w * 3) * R * .45) / .7, 2))) * band * calm;
    // Running glints: small four-point stars carried along by the energy.
    float run = frac(along / 360 - clock * 2.4 * calm + signal.w);
    float d = (run - .5) * 360;
    float star = exp2(-(d * d + pc * pc) / 8) + exp2(-pow(pc / fine, 2)) * exp2(-abs(d) / 16) * .6
        + exp2(-pow(d / (fine * 1.5), 2)) * exp2(-pa / 5) * .45;
    float tcell = floor(along / 23), ht = Hash(tcell + 9.1);
    float2 tw = float2(along - (tcell + .5) * 23, across - (ht - .5) * R * 1.2);
    float twinkle = exp2(-dot(tw, tw) / 2.6) * pow(saturate(sin(tick * (.25 + .2 * ht) + ht * 60)), 16)
        * step(.6, Hash(tcell + 3.3)) * band * (1 - razor.w);

    float3 white = float3(1, .985, 1), rose = float3(.92, .56, .68);
    float lit = 1 - razor.w * .3;
    float bandA = band * (.03 + .16 * pow(saturate(1 - s), 2) + .05 * energy);
    float3 c = white * (core * (.95 + .4 * flash) + star * (.7 + .4 * energy) + twinkle)
        + lerp(tint, white, exp2(-pc * pc / 9)) * bead
        + tint * glow * (.28 + .35 * energy + .18 * flash) * lit
        + tint * bloom * (.10 + .05 * energy + .10 * flash) * lit
        + lerp(tint, rose, fringe) * bandA * 1.35 + rose * band * fringe * (.05 + .04 * energy)
        + tint * filaments * (.06 + .12 * energy);
    float a = saturate(core * .9 + bandA + glow * .08 + bead * .3 + star * .35);
    return float4(c, a) * EndFade(along, L) * signal.x * i.C;
}

// No longer dangerous: no band, no white-hot core, no glints. A cool silver
// strand that thins evenly and parts at one or two points (breaks), its free
// end receding, while sparse motes drift off it and wink out.
float4 Frayed(VO i)
{
    float x = saturate(i.U.x);
    float L = footprint.x, R = max(1, razor.x);
    float along = x * L;
    float across = (i.U.y * 2 - 1) * footprint.y;
    float pa = abs(across);
    float f = razor.z, tick = clock * 60;
    float keep = smoothstep(breaks.y, breaks.y + 14, abs(along - breaks.x)) * smoothstep(breaks.w, breaks.w + 14, abs(along - breaks.z));
    float strand = keep * EndFade(along, L) * snap.x;
    float core = exp2(-pow(pa / razorEx.w, 2)) * .5;
    float glow = exp2(-pow(pa / 2.4, 2)) * .10;
    float cell = floor(along / 38);
    float h1 = Hash(cell), h2 = Hash(cell + .37), h3 = Hash(cell + .71);
    float2 mote = float2(along - (cell + .5 + (h2 - .5) * .7) * 38 - (h1 - .5) * 30 * f,
        across - (h1 - .5) * 1.6 * (R + 8) * f - (h2 - .5) * 3);
    float motes = exp2(-dot(mote, mote) / 2.2) * step(h3, .45 - razor.w * .25) * snap.y
        * (.55 + .45 * sin(tick * (.3 + .3 * h3) + h2 * 40));
    float3 c = tint * (core + glow) * strand + lerp(tint, float3(1, 1, 1), .4) * motes * .8;
    float a = saturate((core + glow * .4) * strand + motes * .45);
    return float4(c, a) * signal.x * i.C;
}

// Branchless on purpose: a dynamic if/else here is mistranslated by the FNA
// effect runtime (the frayed strand came out as classic silk). All three are
// cheap on thin ribbons and the select compiles to cmp.
float4 PS(VO i) : COLOR0
{
    float4 classic = Classic(i), live = Razor(i), dead = Frayed(i);
    return razor.x <= 0 ? classic : razor.z >= 0 ? dead : live;
}
technique EbonSilk { pass AutoloadPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 PS(); } }
