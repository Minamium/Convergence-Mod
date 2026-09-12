// Original Convergence material. No external shader/textures copied.
// XY are the locked corridor coordinates; animation affects radiance only.
float4x4 uWorldViewProjection;
float3 beamColor;
float4 envelope; // tension, authority-live, opacity, release impulse
float2 dimensions; // physical length, half width
float flowTime;
float detail;

struct VertexInput {
    float4 position : POSITION0;
    float4 color : COLOR0;
    float2 uv : TEXCOORD0;
};
struct FragmentInput {
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
};
FragmentInput VertexMain(VertexInput input) {
    FragmentInput output;
    output.position = mul(input.position, uWorldViewProjection);
    output.uv = input.uv;
    return output;
}

float Hash(float2 p) {
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}
float Noise(float2 p) {
    float2 cell = floor(p), f = frac(p);
    f = f * f * (3 - 2 * f);
    return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
        lerp(Hash(cell + float2(0, 1)), Hash(cell + 1), f.x), f.y);
}
float Band(float value, float width) {
    float q = saturate(1 - abs(value) / width);
    return q * q;
}

float4 PixelMouth(FragmentInput input) : COLOR0 {
    float2 uv = input.uv;
    float x = uv.x * dimensions.x, y = uv.y * 2 - 1;
    float tension = envelope.x, live = envelope.y, opacity = envelope.z, kick = envelope.w;
    float3 pearl = lerp(beamColor, float3(1, .97, .94), .8);
        // A crooked longitudinal pressure fold gathers into a slit at x=150.
        // Fibers brake at the mouth, then visibly turn downstream on release.
        float mouth = (x - 150) / 70;
        float rear = saturate(-mouth);
        float side = 1 - smoothstep(.65, 1, abs(y));
        float falloff = 1 - smoothstep(.65, 1, abs(mouth) * .43);
        float warp = sin(mouth * 8 + flowTime * 2) * .08 * rear;
        float fibers = Band(y - warp - sin(mouth * 4 - flowTime) * rear * .38, .065)
            + Band(y + warp + sin(mouth * 5 + flowTime * 1.3) * rear * .5, .045);
        float slit = Band(mouth, .14 + kick * .16) * pow(saturate(1 - abs(y)), .65);
        float throat = Band(y, .20 + live * .15) * exp(-abs(mouth) * 2.7);
        float haze = exp(-mouth * mouth * 4 - y * y * 6) * (.1 + tension * .1);
        float3 light = beamColor * (haze + fibers * rear * .34 * detail + slit * .4)
            + pearl * (slit * (.45 + kick * .7) + throat * (.3 + live * .8));
        return float4(light * side * falloff * opacity * lerp(.66, 1, detail), 0);
}

float4 PixelMain(FragmentInput input) : COLOR0 {
    float2 uv = input.uv;
    float x = uv.x * dimensions.x, y = uv.y * 2 - 1;
    float tension = envelope.x, live = envelope.y, opacity = envelope.z, kick = envelope.w;
    float3 pearl = lerp(beamColor, float3(1, .97, .94), .8);
    float edge = 1 - smoothstep(.94, 1, abs(y));
    float noise = Noise(float2(x * .012 - flowTime, y * 2.4 + flowTime * .12));
    float fine = Noise(float2(x * .038 - flowTime * 2, y * 5 - flowTime * .17));
    float turn = sin(x * .018 - flowTime * 2.2 + noise * 3) * .075 * detail;
    float throat = lerp(.17, .62, smoothstep(0, 155, x));
    float skin = Band(y - turn, throat + (noise - .5) * .22 * detail);
    float hot = Band(y - turn * .4, .10 + kick * .05);
    float filaments = Band(y - turn - sin(x * .027 - flowTime * 2.1) * .34, .07)
        + Band(y + turn + sin(x * .019 - flowTime * 1.7 + 2) * .45, .045);
    float wake = .55 + .45 * fine;
    // No dark slots within the live width, no thick edge rails. The narrower
    // bright core is material detail; the continuous colored body is danger.
    float occupied = .27 + .18 * noise;
    float3 plasma = beamColor * (occupied + skin * .66 + filaments * .28 * detail)
        + pearl * (hot * .72 + skin * wake * .31 + kick * .10);
    float spine = Band(y, min(.11, 2.2 / max(dimensions.y, 1)));
    float3 forecast = beamColor * (.075 + noise * .075 + tension * .055)
        + pearl * spine * (.23 + tension * .13);
    // Movement is within the warning footprint, not dashed future "safe" lanes.
    forecast += beamColor * filaments * .035 * detail;
    float3 light = lerp(forecast, plasma, live);
    float alpha = edge * opacity * lerp(.12, .08, live);
    return float4(light * edge * opacity * lerp(.78, 1, detail), alpha);
}

technique PursuitPrism {
    pass AutoloadPass {
        VertexShader = compile vs_3_0 VertexMain();
        PixelShader = compile ps_3_0 PixelMain();
    }
    pass MouthPass {
        VertexShader = compile vs_3_0 VertexMain();
        PixelShader = compile ps_3_0 PixelMouth();
    }
}
