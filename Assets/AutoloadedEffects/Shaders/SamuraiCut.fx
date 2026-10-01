// Ghost Samurai pixel-art sword cuts. Original repository material.
// Each pass evaluates the accepted hazard once per world-aligned 2x2 art pixel,
// quantizes it to Soboro's violet palette and lights only art pixels that lie
// wholly inside the gameplay footprint. Forecast, live cut and torn residue
// follow the same Fire/End clock; nothing here decides hits.
matrix uWorldViewProjection;
float2 uScreenPosition;
float2 frameOrigin;
float2 frameX;
float2 frameY;
float clock;
float4 phase; // ticks relative to Fire, live duration, forecast progress, reduced
float4 shape; // stroke: length, half width, seed, route | field: radius, inner radius, facing, wind | wave: length, half width, seed, 0
sampler cloud : register(s1);
sampler veins : register(s2);

static const float3 Tone1 = float3(0.086, 0.020, 0.188); // contour
static const float3 Tone2 = float3(0.243, 0.063, 0.541); // deep violet
static const float3 Tone3 = float3(0.439, 0.157, 0.871); // violet
static const float3 Tone4 = float3(0.667, 0.424, 1.000); // lilac
static const float3 Tone5 = float3(0.863, 0.769, 1.000); // pale
static const float3 Tone6 = float3(1.000, 1.000, 1.000); // white-hot edge
static const float Inset = 1.42;  // half diagonal of an art pixel: lit cells stay inside the footprint
static const float Residue = 16;

struct VI { float4 P : POSITION0; float4 C : COLOR0; float2 U : TEXCOORD0; };
struct VO { float4 P : POSITION0; float4 C : COLOR0; float2 U : TEXCOORD0; float2 S : TEXCOORD1; };

VO VS(VI v)
{
    VO o;
    o.P = mul(v.P, uWorldViewProjection);
    o.C = v.C;
    o.U = v.U;
    o.S = v.P.xy;
    return o;
}

// Art pixels are fixed to the world, so a moving camera never shifts them.
float2 Cell(float2 s) { return floor((s + uScreenPosition) * .5); }
float2 Center(float2 cell) { return cell * 2 + 1 - uScreenPosition; }
float2 Local(float2 cell)
{
    float2 d = Center(cell) - frameOrigin;
    return float2(dot(d, frameX), dot(d, frameY));
}

float B2(float2 p) { return fmod(2 * p.x + 3 * p.y, 4); }
float Bayer(float2 cell)
{
    float2 c = cell - 4 * floor(cell * .25);
    float2 hi = floor(c * .5);
    return (4 * B2(c - 2 * hi) + B2(hi) + .5) / 16;
}

// 0 = empty, 1 = contour, 2..6 = the violet ramp
float3 Tone(float level)
{
    return level > 5.5 ? Tone6 : level > 4.5 ? Tone5 : level > 3.5 ? Tone4 : level > 2.5 ? Tone3 : level > 1.5 ? Tone2 : Tone1;
}

float Cooling() { return 1 - saturate((phase.x - 2) / max(phase.y - 2, 1)); }
float Fade() { return saturate((phase.x - phase.y) / Residue); }

// A marching contour one art pixel inside the footprint, a dark inner contour
// for bright backgrounds and an ordered-dither fill that thickens toward Fire.
// The last eight ticks blink unless effects are reduced.
float Forecast(float d, float s, float2 cell, float core)
{
    float progress = saturate(phase.z);
    float rim = step(d, Inset + 2);
    float shade = step(d, Inset + 4) * (1 - rim);
    float march = step(frac((s * .5 + clock * 12) / 6), .62);
    float blink = (1 - phase.w) * step(-8, phase.x) * step(frac(phase.x * .25), .5);
    float fill = step(Bayer(cell), .04 + .26 * pow(progress, 1.6));
    float incision = step(core, 2 + 2 * progress) * step(.15, progress);
    float level = fill * 2;
    level = max(level, incision * (progress > .75 ? 5 : 4));
    level = shade > .5 ? 1 : level;
    level = rim > .5 ? (blink > .5 ? 6 : march > .5 ? 4 : 2) : level;
    return level;
}

// Torn cooling fragments: already broken at End, deep tones only, thinning
// to a checker once half cold.
float Tear(float keep, float grain, float2 cell)
{
    float fade = Fade();
    float checker = fmod(cell.x + cell.y, 2);
    float alive = keep * step(.38 + fade * .72, grain) * max(step(fade, .45), 1 - checker);
    return alive * (grain > .8 ? 3 : 2);
}

float StrokeBody(float2 p, float t, float2 cell, out float edge)
{
    float L = shape.x, R = shape.y, seed = shape.z;
    float u = p.x / max(L, 1), v = p.y / max(R, 1);
    // neighbouring grid cuts run opposite ways; the shared hit tick is unchanged
    u = lerp(u, 1 - u, step(.5, frac(seed * .113)));
    float front = saturate(t / 3) * 1.25 - .12 - .12 * (1 - v * v);
    float swept = step(u, front);
    edge = swept * step(front - .045, u) * step(abs(p.y), R - Inset - 2);
    float cooling = Cooling();
    // a blade profile: full width through the middle, drawn to points at both ends
    float taper = pow(saturate(sin(3.14159 * saturate(p.x / max(L, 1)))), .35);
    float body = R * (.3 + .7 * cooling) * taper;
    float a = abs(p.y) / max(body, 1);
    // speed lines run along the cut and push the tones outward
    float streak = tex2D(cloud, float2(p.x * .0025 - clock * 3.1 + seed, p.y * .045)).r;
    float lift = (streak - .5) * .35 * (1 - phase.w);
    float level = a < .16 + lift * .3 ? 6 : a < .38 + lift ? 5 : a < .68 + lift ? 4 : 3;
    level = cooling < .4 ? min(level, 5) : level; // the white core cools first
    return swept * step(abs(p.y), body) * level;
}

float4 Stroke(VO i) : COLOR0
{
    float2 cell = Cell(i.S), p = Local(cell);
    float L = shape.x, R = shape.y, t = phase.x;
    float d = min(R - abs(p.y), min(p.x, L - p.x));
    float level = 0, edge;
    if (t < 0)
        level = Forecast(d, p.x + abs(p.y), cell, abs(p.y));
    else if (shape.w > .5)
        level = 0; // routes are forecasts only; the travelling projectile owns its cut
    else if (t < phase.y)
    {
        level = step(Bayer(cell), .3) * 2;          // the whole footprint stays readable until End
        level = max(level, step(d, Inset + 2) * 4);
        level = max(level, StrokeBody(p, t, cell, edge));
        level = max(level, edge * 6);
    }
    else
    {
        // two unrelated noise periods, stretched along the cut, so fragments never tile
        float grain = tex2D(veins, float2(p.x * .0023 + shape.z, p.y * .03 + Fade() * .2)).r * .65
            + tex2D(cloud, float2(p.x * .0061 - shape.z, p.y * .05)).r * .35;
        float taper = pow(saturate(sin(3.14159 * saturate(p.x / max(L, 1)))), .35);
        float keep = step(abs(p.y), R * (.22 + .28 * Fade()) * taper);
        level = Tear(keep, grain, cell);
    }
    clip(min(d - Inset, level - .5));
    return float4(Tone(level), 1) * i.C;
}

float FieldCuts(float2 q, float r, float t)
{
    float R = shape.x, facing = shape.z, cooling = Cooling();
    float level = 0;
    if (abs(facing) > .5)
    {
        // Frontal cleave: one enormous blade falls from above to below on the facing side.
        float angle = atan2(q.y, q.x * facing);
        float fall = lerp(-1.75, 1.75, saturate(t / 4));
        float behind = fall - angle;               // radians behind the falling blade
        float k = behind / (.25 + 1.2 * cooling);  // 0 at the blade .. 1 at the end of its trail
        // tangential speed lines: slow along the arc, fast across radii
        float streak = tex2D(cloud, float2(angle * .6 - clock * .5, r * .02)).r;
        float lift = (streak - .5) * .5 * (1 - phase.w);
        float trail = step(0, behind) * step(k, 1);
        level = trail * (k < .12 + lift * .2 ? 5 : k < .45 + lift ? 4 : 3);
        level = max(level, step(0, behind) * step(behind * r, 6) * 6);
        [unroll] for (int e = 1; e <= 2; e++)
        {
            float echo = behind - e * .16;
            level = max(level, trail * step(0, echo) * step(echo * r, 3) * (6 - e) * (1 - phase.w * (e - 1)));
        }
    }
    else if (shape.w > .5)
    {
        // Kamaitachi: curved wind blades wheel around the centre.
        float n = tex2D(cloud, q * .004 + float2(-clock * 1.3, clock * .2)).r;
        float crest = frac((atan2(q.y, q.x) * 3 - r * .006 - t * .32 + n * .6) / 6.2831853);
        float reach = .08 + .26 * cooling;
        level = step(crest, .035) * 6;
        level = max(level, step(crest, .1) * 5);
        level = max(level, step(crest, reach) * (3 + step(.62, n)));
    }
    else
    {
        // Straight slashes: staggered strokes cross the area and grow along their length.
        [unroll] for (int k = 0; k < 4; k++)
        {
            float a = .42 + k * 1.21;
            float2 n = float2(cos(a), sin(a));
            float offset = (k - 1.5) * R * .3;
            float across = abs(dot(q, n) - offset);
            float along = dot(q, float2(-n.y, n.x));
            float born = k * .9;
            float grown = step(born, t) * step(along, saturate((t - born) / 2.2) * R * 2.2 - R * 1.1);
            float width = 2 + max(3, R * .045) * cooling;
            float use = 1 - phase.w * step(1.5, k);
            float cut = step(across, 2.5) * 6;
            cut = max(cut, step(across, width) * 4);
            cut = max(cut, step(across, width * 2.2) * 3);
            level = max(level, grown * use * cut);
        }
    }
    return level;
}

float4 Field(VO i) : COLOR0
{
    float2 cell = Cell(i.S);
    float2 q = Center(cell) - frameOrigin;
    float R = shape.x, r = length(q), t = phase.x;
    float d = R - r;
    if (shape.y > .5) d = min(d, r - shape.y);
    if (abs(shape.z) > .5) d = min(d, q.x * shape.z);
    float level;
    if (t < 0)
        level = Forecast(d, atan2(q.y, q.x) * r, cell, 1e5);
    else if (t < phase.y)
    {
        level = step(Bayer(cell), .26) * 2;
        level = max(level, step(d, Inset + 2) * 4);
        level = max(level, FieldCuts(q, r, t));
    }
    else
    {
        float grain = tex2D(veins, q * .0041 + Fade() * .15).r * .65 + tex2D(cloud, q * .009).r * .35;
        level = Tear(step(2.5, FieldCuts(q, r, phase.y - 1)), grain, cell);
    }
    clip(min(d - Inset, level - .5));
    return float4(Tone(level), 1) * i.C;
}

float WaveBlade(float2 p, float2 cell)
{
    float L = shape.x, R = shape.y;
    float v = p.y / max(R, 1);
    // a thick crescent whose edge bulges forward in the middle, with a torn tail
    float ahead = p.x - L * (.42 - .3 * v * v);
    float k = -ahead / max(L * (.35 + .3 * (1 - v * v)), 1); // 0 at the edge .. 1 at the back
    float streak = tex2D(cloud, float2(p.x * .008 - clock * 3.8, p.y * .045 + shape.z)).r;
    float lift = (streak - .5) * .4 * (1 - phase.w);
    float level = step(0, k) * step(k, 1) * (k < .1 ? 6 : k < .3 + lift * .3 ? 5 : k < .62 + lift ? 4 : 3);
    level = max(level, step(1, k) * step(k, 1.9) * step(.42 + .3 * (k - 1), streak) * 2);
    return level;
}

// A spirit-fire wisp: a white-cored flame whose head is the hit circle and
// whose flickering tail trails behind its heading. The tail is decoration only.
float4 Wisp(VO i) : COLOR0
{
    float2 cell = Cell(i.S), p = Local(cell);
    float R = shape.x;
    float n = tex2D(cloud, float2(p.x * .03 - clock * 4.5 + shape.z, p.y * .05)).r;
    // the head is drawn back into a flame, still no larger than the hit circle ahead
    float head = length(float2(p.x < 0 ? p.x * .72 : p.x, p.y)) / max(R, 1);
    // tail half width shrinks with distance behind the head and flickers
    float behind = saturate(-p.x / (R * 3.2 * (.82 + .3 * n)));
    float tail = step(p.x, 0) * step(abs(p.y), R * pow(1 - behind, .8) * (.72 + .4 * n)) * step(behind, .999);
    float level = step(head, 1) * (head < .3 ? 6 : head < .58 ? 5 : head < .84 ? 4 : 3);
    level = max(level, tail * (behind < .3 ? 4 : behind < .65 ? 3 : 2));
    // a dark contour keeps the head readable on bright backgrounds
    level = max(level, step(head, 1.18) * step(1, head) * (1 - tail));
    clip(level - .5);
    return float4(Tone(level), 1) * i.C;
}

float4 Wave(VO i) : COLOR0
{
    float2 cell = Cell(i.S), p = Local(cell);
    float L = shape.x, R = shape.y, t = phase.x;
    float d = min(R - abs(p.y), L * .5 - abs(p.x));
    float level = WaveBlade(p, cell);
    if (t >= phase.y)
    {
        float grain = tex2D(veins, float2(p.y * .0047 + shape.z, p.x * .03 + Fade() * .2)).r * .65
            + tex2D(cloud, float2(p.y * .011 - shape.z, p.x * .05)).r * .35;
        level = Tear(step(2.5, level), grain, cell);
    }
    clip(min(d - Inset, level - .5));
    return float4(Tone(level), 1) * i.C;
}

technique SamuraiCut
{
    pass AutoloadPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 Stroke(); }
    pass FieldPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 Field(); }
    pass WavePass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 Wave(); }
    pass WispPass { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 Wisp(); }
}
