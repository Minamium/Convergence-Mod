// Original Convergence material. Luminance supplies the two noise maps at runtime.
// Warning/live geometry is immutable. Flow, corona and sparks are radiance only.
float4x4 uWorldViewProjection;
float3 beamColor;
float4 envelope; // tension, authority-live, opacity, release impulse
float2 dimensions; // physical length, half width of this pass
float flowTime;
float detail;
sampler cloudNoise : register(s1);
sampler fractureNoise : register(s2);

struct VertexInput { float4 position : POSITION0; float4 color : COLOR0; float2 uv : TEXCOORD0; };
struct FragmentInput { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
FragmentInput VertexMain(VertexInput input) {
    FragmentInput output;
    output.position = mul(input.position, uWorldViewProjection);
    output.uv = input.uv;
    return output;
}

// Two differently advected scales: flowing volume under a torn, faster skin.
// Coordinates use world lengths, not the stretched quad's aspect ratio.
float3 Flow(float x, float y) {
    float cloud = tex2D(cloudNoise, float2(x * .0027 - flowTime * .13, y * .48 + flowTime * .023)).r;
    float fine = tex2D(fractureNoise, float2(x * .009 - flowTime * .36 + cloud * .17,
        y * 1.6 + cloud * .38 - flowTime * .038)).r;
    float folds = tex2D(cloudNoise, float2(x * .0048 - flowTime * .23, y * 1.1 - cloud * .3)).r;
    return float3(cloud, fine, folds);
}

float4 PixelBody(FragmentInput input) : COLOR0 {
    float x = input.uv.x * dimensions.x, y = input.uv.y * 2 - 1;
    float3 n = Flow(x, y);
    float warp = (n.x - .5) * .15 * detail;
    float radial = abs(y - warp * (1 - abs(y)));
    // The occupied body reaches the locked boundary; only the last 2px feather.
    // No moving gaps, growing damage front, constant rectangular fill or rails.
    float boundary = 1 - smoothstep(1 - 2 / max(dimensions.y, 2), 1, abs(y));
    float mantle = pow(saturate(1 - radial * radial), .8);
    float convection = exp2(-radial * radial * 4.5) * (.22 + n.x * .58);
    float strata = pow(saturate(n.y * 1.35), 3) * (.14 + n.z * .6) * detail;
    float core = exp2(-pow(radial, 1.6) * 25) * (.58 + n.z * .48);
    float lace = pow(saturate(n.z * 1.65 - n.y * .45), 4) * exp2(-radial * radial * 3.5);
    float3 pearl = lerp(beamColor, float3(1, .98, .94), .88);
    float3 light = beamColor * mantle * (.12 + convection + strata * .5)
        + pearl * (core * .86 + lace * .24 + envelope.w * .23 * exp2(-radial * radial * 5));
    return float4(light * boundary * envelope.z, 0);
}

float4 PixelForecast(FragmentInput input) : COLOR0 {
    float x = input.uv.x * dimensions.x, y = input.uv.y * 2 - 1;
    float3 n = Flow(x, y);
    float area = pow(saturate(1 - y * y), .6);
    float boundary = 1 - smoothstep(.95, 1, abs(y));
    float spine = exp2(-y * y * dimensions.y * dimensions.y * .45);
    float charge = pow(saturate(n.y * 1.65 - .3), 3) * exp2(-y * y * 5) * detail;
    float3 pearl = lerp(beamColor, float3(1, .98, .94), .65);
    float3 light = beamColor * (area * (.065 + .04 * envelope.x) + charge * .075)
        + pearl * spine * (.34 + .18 * envelope.x);
    return float4(light * boundary * envelope.z, 0);
}

float4 PixelBloom(FragmentInput input) : COLOR0 {
    float x = input.uv.x * dimensions.x, y = input.uv.y * 2 - 1;
    float3 n = Flow(x, y * 2.7);
    // This pass is 2.7x wider than danger; keep it diffuse and much dimmer.
    float halo = exp2(-y * y * 11) * pow(saturate(1 - y * y), 2);
    float veil = halo * (.10 + .035 * n.x) * (.3 + envelope.y * .7);
    // Flecks shear away from the hot body. These cannot create false safe holes.
    float flecks = pow(saturate(n.y * 1.8 - .4), 7) * pow(saturate(n.z * 1.8), 3)
        * exp2(-pow(abs(y) - .36, 2) * 50) * .11 * envelope.y * detail;
    return float4(beamColor * (veil + flecks) * envelope.z * lerp(.45, 1, detail), 0);
}

float4 PixelMouth(FragmentInput input) : COLOR0 {
    // A pressure plume through a slit, not a circle/crosshair or looping wire.
    float x = input.uv.x * dimensions.x - 120, y = input.uv.y * 2 - 1;
    float3 n = Flow(x * 1.7, y * 1.8);
    float crossFalloff = pow(saturate(1 - y * y), 2);
    float rear = exp2(-abs(x) * .019);
    float plume = exp2(-y * y * (8 + saturate(-x / 120) * 24)) * rear;
    float slit = exp2(-x * x * .018) * crossFalloff;
    float splinters = pow(saturate(n.y * 1.7 - .25), 5) * plume * detail;
    float3 pearl = lerp(beamColor, float3(1, .98, .94), .87);
    float3 light = beamColor * plume * (.1 + n.x * .24 + splinters * .3)
        + pearl * (slit * (.12 + envelope.w * .48)
        + plume * (.10 + envelope.y * .33 + envelope.w * .36));
    float ends = smoothstep(-120, -85, x) * (1 - smoothstep(140, 180, x));
    return float4(light * ends * envelope.z * lerp(.55, 1, detail), 0);
}

technique PursuitPrism {
    pass AutoloadPass { VertexShader = compile vs_3_0 VertexMain(); PixelShader = compile ps_3_0 PixelBody(); }
    pass ForecastPass { VertexShader = compile vs_3_0 VertexMain(); PixelShader = compile ps_3_0 PixelForecast(); }
    pass BloomPass { VertexShader = compile vs_3_0 VertexMain(); PixelShader = compile ps_3_0 PixelBloom(); }
    pass MouthPass { VertexShader = compile vs_3_0 VertexMain(); PixelShader = compile ps_3_0 PixelMouth(); }
}
