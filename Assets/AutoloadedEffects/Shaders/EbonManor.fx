// Original Ebon Manor materials: the moonlit hall, woven furniture, quiet silk
// warnings (veils, hairlines), shears tears, stitch hoops, glints and the
// waltz's turn fan and afterglow.
// Every danger footprint is drawn from accepted plan geometry; noise only
// moves pigment inside it.
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
float view; // world px per screen px (1 / game zoom); fine lines never thin below ~.75 screen px
// Uniform-only selections (clip on/off, blade, outer fan) arrive as sentinel
// values from the CPU rather than ternaries: the FNA effect runtime
// mis-evaluates some fx_2_0 preshader code.
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

// ---- Warnings: quiet moonlit silk -------------------------------------------
// One element per hazard, exactly on the accepted footprint and with no
// boundary lines, dashes or rails. Mid-tone colours are composited over the
// hall (never added): a veil lifts the dark hall and tints the bright windows,
// and overlapping veils converge on the same film instead of adding to white.
// Cool silver-lilac warms to dusty rose only in the last beat.
float3 VeilTone(float progress) { return lerp(float3(.56, .58, .80), float3(.78, .60, .70), smoothstep(.66, 1, progress)); }
float3 HairTone(float progress) { return lerp(float3(.84, .86, 1), float3(1, .80, .87), smoothstep(.66, 1, progress)); }
float Fine() { return .75 * view; }

// Film density over a footprint: even to the true edge (anti-aliased within
// 2 px, no brighter hem). inside: px from the edge. m: material coordinates in
// px (x along the footprint); a fine silk sheen of long streaks drifts along it.
float Film(float inside, float2 m, float seed, float motion)
{
    float hem = smoothstep(0, 2, inside);
    float n = tex2D(noise, m * float2(.0024, .0042) + float2(-clock * .03 * motion + seed, seed * .7)).r;
    float f = tex2D(detail, m * float2(.0015, .035) + float2(-clock * .05 * motion, seed * 1.3)).r;
    return hem * (.86 + .22 * n + .20 * (f - .5));
}

// Broad straight footprint (thrown furniture route, chandelier fall, shears
// band): a feathered silk veil, fullest along its spine, with one soft swell
// of light drifting toward the far end. U.x runs along, U.y across the full
// diameter. shape: length, radius, progress, live (furniture in flight).
// signal: opacity, clip radius (> 0: fade out inside a disc of this radius
// centred on the far end, so a fall corridor meets its burst without
// stacking; -1e4 = none), reduced, seed.
float4 VeilPS(VO i) : COLOR0
{
    float along = i.U.x * shape.x;
    float across = (i.U.y * 2 - 1) * shape.y;
    float inside = shape.y - abs(across);
    float progress = shape.z, live = shape.w, motion = 1 - signal.z * .7;
    float film = Film(inside, float2(along, across), signal.w, motion) * (.85 + .15 * cos(abs(across) / shape.y * 3.14159));
    float run = frac(along / 640 - clock * (.10 + .40 * progress + .9 * live) * motion + signal.w);
    float swell = exp2(-pow((run - .5) * 640 / 150, 2)) * (.16 + .24 * live);
    float clip = smoothstep(signal.y - 6, signal.y + 10, length(float2(shape.x - along, across)));
    float ends = smoothstep(0, 18, along) * smoothstep(0, 18, shape.x - along + saturate(signal.y) * 1e5) * clip;
    float density = lerp(lerp(.12, .22, progress * progress), .17, live);
    float3 tone = lerp(VeilTone(progress), float3(.84, .58, .68), live);
    float a = saturate(density * (film + swell)) * ends;
    return float4(tone * a, a) * signal.x * i.C;
}

// Thread-width footprint (loom string, web strand, waltz spoke): one hairline
// strung along the true centre, a faint film over the honest band, a small
// glint running along it and a bead at the tip while it is being strung. A
// faint dark under-shadow keeps it legible over the bright windows (invisible
// on the dark hall). shape: length, radius, progress, reveal (1 = strung
// anchor to anchor). signal: opacity, tremble px, reduced, seed.
float4 HairlinePS(VO i) : COLOR0
{
    float x = i.U.x;
    float along = x * shape.x;
    float across = (i.U.y * 2 - 1) * shape.y;
    float inside = shape.y - abs(across);
    float progress = shape.z, motion = 1 - signal.z * .7;
    float wave = sin(x * 3.14159) * sin(x * 9.42478 + clock * 60 * (.10 + .30 * progress) * motion) * signal.y;
    float pc = abs(across - wave);
    float core = exp2(-pow(pc / max(.78 + .25 * progress, Fine()), 2)) * (.30 + .42 * progress);
    float halo = exp2(-pow(pc / 3.2, 2)) * (.05 + .06 * progress);
    float shade = exp2(-pow(pc / max(2.6, 3 * Fine()), 2)) * .17;
    float film = Film(inside, float2(along, across), signal.w, motion) * lerp(.05, .12, progress * progress);
    float run = frac(along / 560 - clock * (.16 + .62 * progress) * motion + signal.w);
    float glint = exp2(-pow((run - .5) * 560 / 11, 2)) * exp2(-pow(pc / max(1.7, 1.4 * Fine()), 2)) * (.45 + .45 * progress);
    float strung = step(.999, shape.w);
    float tip = exp2(-pow((shape.x - along) / 7, 2)) * exp2(-pow(pc / 2.2, 2)) * (1 - strung);
    float ends = smoothstep(0, 8, along) * lerp(1, smoothstep(0, 8, shape.x - along), strung);
    float3 c = VeilTone(progress) * film + HairTone(progress) * (core + halo) + float3(1, .97, 1) * (glint + tip);
    float a = saturate(film + core * .9 + halo * .5 + (glint + tip) * .4 + shade);
    return float4(c, a) * ends * signal.x * i.C;
}

// Circular footprint veil (chandelier burst) with a slow breath gathering
// toward the landing point. shape: radius px, progress. signal: opacity, -, reduced, seed.
float4 DiscPS(VO i) : COLOR0
{
    float2 p = (i.U * 2 - 1) * shape.x;
    float r = length(p);
    float inside = shape.x - r;
    float progress = shape.y, motion = 1 - signal.z * .7;
    float film = Film(inside, p, signal.w, motion) * (.85 + .15 * cos(saturate(r / shape.x) * 3.14159));
    float breath = exp2(-pow((r / shape.x - frac(-clock * .35 * motion + signal.w)) * 5, 2)) * .22 * progress * motion;
    float density = lerp(.10, .22, progress * progress);
    float a = saturate(density * (film + breath)) * step(0, inside);
    return float4(VeilTone(progress) * a, a) * signal.x * i.C;
}

// A live shears cut: a ragged dark slit through the accepted band, a fine
// white seam whose light flows with the energy along it, a soft moon-silver
// bloom, quiet dusty-rose torn edges and fine fibres hanging only from the
// edges; a glint rides with the blades. shape: length, radius, ticks since
// fire, live duration. signal: opacity, blade position px along (-1e4 = none), reduced, seed.
float4 TearPS(VO i) : COLOR0
{
    float along = i.U.x * shape.x;
    float across = i.U.y * 2 - 1;
    float d = abs(across);
    float t = saturate(shape.z / max(1, shape.w));
    float open = 1 - exp(-shape.z / 1.8);
    float calm = 1 - signal.z * .7;
    float n = tex2D(noise, float2(along * .005 + signal.w, i.U.y * .9 + clock * .08)).r;
    float f = tex2D(detail, float2(along * .012, i.U.y * 2.2 - clock * .3)).r;
    float flow = tex2D(noise, float2(along * .0036 - clock * 1.9 * calm, .21 + signal.w)).r;
    float energy = smoothstep(.25, .85, flow);
    float ragged = d + (n - .5) * .16;
    float rim = open * .96;
    float slit = 1 - smoothstep(rim - .04, rim + .02, ragged);
    float edge = exp(-abs(ragged - rim) * 14) * open;
    float px = d * shape.y;
    float core = exp2(-pow(px / (max(.9, Fine()) + 1.2 * open), 2)) * (.8 + .35 * energy);
    float bloom = exp2(-pow(px / (9 + 16 * open), 2));
    float fibres = pow(saturate(1 - abs(frac(along * .05 + n * 2) - .5) * 6), 4) * exp(-max(0, rim - ragged) * 14)
        * slit * (1 - signal.z * .6);
    float blade = exp2(-pow((along - signal.y) / 26, 2));
    float glint = blade * (exp2(-pow(px / 3, 2)) + exp2(-pow(px / 14, 2)) * .3);
    float fade = 1 - smoothstep(.6, 1, t);
    float3 c = float3(.025, .018, .035) * slit + float3(1, .985, 1) * (core + glint) + float3(.74, .80, 1) * bloom * (.30 + .14 * energy)
        + lerp(float3(.80, .60, .70), float3(.80, .84, .98), f) * edge * .35 + float3(.86, .88, 1) * fibres * .30;
    float a = saturate(slit * .58 + core + glint * .6 + edge * .3 + fibres * .28 + bloom * .08);
    return float4(c, a) * fade * signal.x * i.C;
}

// Stitch hoops: one continuous thread on the true radius, a lace band just
// inside it and a continuous clock thread contracting from 92% to 12%.
// shape: kind (0 stack, 1 spread), clock progress, radius, reduced.
// signal: opacity, verdict flash, failed, seed.
float4 RingPS(VO i) : COLOR0
{
    float2 p = i.U * 2 - 1;
    float r = length(p);
    float ang = atan2(p.y, p.x);
    float inside = (1 - r) * shape.z;
    float dir = shape.x < .5 ? -1 : 1;
    float motion = 1 - shape.w * .7;
    float boundary = exp2(-pow((inside - 2.4) / 1.3, 2));
    // Scalloped lace band: arcs and small eyelets, slowly turning.
    float perimeter = 6.2831853 * shape.z;
    float along = (ang / 6.2831853 + .5) * perimeter + dir * clock * 16 * motion;
    float cell = frac(along / 22);
    float scallop = 9 + 4 * sin(cell * 3.14159);
    float band = smoothstep(scallop + 1.5, scallop - .5, inside) * smoothstep(3.2, 4.6, inside);
    float eyelet = 1 - smoothstep(1.4, 2.4, length(float2((cell - .5) * 22, inside - 7.5)));
    float lace = band * (.30 + .45 * (1 - eyelet)) + exp2(-pow((inside - scallop) / 1.0, 2)) * .7;
    float clockR = lerp(.92, .12, saturate(shape.y));
    float clockRing = exp2(-pow((r - clockR) * shape.z / 1.8, 2));
    float clockGlow = exp2(-pow((r - clockR) * shape.z / 9, 2)) * .25;
    float n = tex2D(noise, i.U * 2.3 + float2(clock * .02, signal.w)).r;
    float under = (1 - smoothstep(.95, 1, r)) * (.08 + .05 * n) * (1 - shape.w * .3);
    float3 thread = lerp(tint, float3(1, .98, .95), .35);
    float3 c = thread * (boundary + lace * .55 + clockRing * .7 + clockGlow) + float3(1, .96, .97) * boundary * .25;
    float lit = saturate(boundary * .95 + lace * .45 + clockRing * .6 + clockGlow * .6);
    float flash = signal.y * (1 - smoothstep(.9, 1, r));
    float3 verdict = signal.z > .5 ? float3(.95, .45, .55) : float3(.98, .88, .64);
    c = c * lit + verdict * flash * (.4 + .6 * n);
    float a = saturate(lit + under + flash * .5);
    return float4(c, a) * signal.x * i.C;
}

// Angular afterglow / turn preview of a waltz spoke. U.x radial (0 hub ..
// 1 wall), U.y angular (0 at the spoke .. 1 at the wedge's far edge). Purely
// decorative and faint: a soft fan that trails or leans, never covers a live
// spoke and never draws its own edge. signal: opacity, outer-only start (-1 =
// the whole wedge; e.g. .6 keeps only the rim nearest the walls), reduced, seed.
float4 SweepPS(VO i) : COLOR0
{
    float r = i.U.x, v = i.U.y;
    float n = tex2D(noise, float2(r * 2.2 - clock * .6 * (1 - signal.z * .7) + signal.w, v * .5)).r;
    float fall = pow(saturate(1 - v), 2.2);
    float outer = smoothstep(signal.y, signal.y + .2, r);
    float radial = smoothstep(0, .06, r) * (1 - smoothstep(.85, 1, r)) * outer;
    float a = fall * radial * (.6 + .4 * n) * signal.x;
    return float4(tint * a, a * .55) * i.C;
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

// A small crisp four-point glint (anchor pins, web crossings, spoke tips): a
// tiny hot core and two hairline rays along the quad axes, never a blob. Core
// and ray widths are in pixels (never under ~.9 px, wider when zoomed out).
// signal: opacity, ray strength, size px. tint: the cool colour of the halo.
float4 GlintPS(VO i) : COLOR0
{
    float2 p = i.U * 2 - 1;
    float2 q = p * max(1, signal.z) * .5;
    float r2 = dot(p, p);
    float w = max(.9, Fine()), cs = max(w, signal.z * .06);
    float core = exp2(-dot(q, q) / (cs * cs));
    float halo = exp2(-r2 * 7) * .20;
    float rays = (exp2(-pow(q.y / w, 2)) * pow(saturate(1 - abs(p.x)), 2.5)
        + exp2(-pow(q.x / w, 2)) * pow(saturate(1 - abs(p.y)), 2.5)) * signal.y;
    float g = core + halo + rays * .55;
    float3 c = lerp(tint, float3(1, .99, 1), saturate(core * 1.4 + rays * .6));
    return float4(c * g, saturate(core + rays * .5) * .6) * signal.x * i.C;
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
        strands += exp2(-pow((off + vib) * 380, 2)) * (.45 + .55 * frac(k * .618 + clock * .05));
    }
    // Soft and dim, so the hall's own silk never reads like a warning hairline.
    strands *= saturate(signal.w) * (1 - smoothstep(.6, .95, uv.y)) * .12;

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
    pass VeilPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 VeilPS(); }
    pass HairlinePass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 HairlinePS(); }
    pass DiscPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 DiscPS(); }
    pass TearPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 TearPS(); }
    pass RingPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 RingPS(); }
    pass ShardPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 ShardPS(); }
    pass GlowPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 GlowPS(); }
    pass BackdropPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 BackdropPS(); }
    pass FramePass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 FramePS(); }
    pass SweepPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 SweepPS(); }
    pass GlintPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 GlintPS(); }
}
