// Original Convergence world-space Stack/Spread material. Luminance owns noise.
float4x4 uWorldViewProjection;
float3 beamColor;
float4 signal; // remaining fraction, stack, opacity, exact radius/quad extent
float4 shape; // quad width, extent, stable seed, detail
float clock;
sampler cloudNoise : register(s1);
sampler flowNoise : register(s2);
struct VI { float4 position:POSITION0; float4 color:COLOR0; float2 uv:TEXCOORD0; };
struct FI { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
FI VS(VI v) { FI o; o.position=mul(v.position,uWorldViewProjection); o.uv=v.uv; return o; }

float4 Ring(FI i):COLOR0 {
    float2 p=i.uv*2-1;
    float distance=(length(p)-signal.w)*shape.y;
    float angle=atan2(p.y,p.x);
    float cycle=frac((angle+1.5707963)/6.2831853+1);
    float n=tex2D(cloudNoise,p*1.7+float2(clock*.043,-clock*.031)+shape.z).r;
    float flow=tex2D(flowNoise,float2(cos(angle-clock*.22),sin(angle-clock*.22))*1.9+n*.21).r;
    // The geometric boundary never wobbles. Material currents stay just inside
    // it; there is no displaced/contracting circle implying a second hit radius.
    float edge=exp2(-distance*distance*.65);
    float inner=exp2(-pow((distance+3.8+n*2.3)/3.7,2));
    float shade=exp2(-distance*distance*.035)*.48;
    float fog=exp2(-pow((distance+7)/6,2))*(.12+.17*flow);
    float progress=(1-smoothstep(signal.x-.006,signal.x+.006,cycle));
    float filament=pow(saturate(flow*.85+n*.6-.44),3)*shape.w;
    float glint=pow(saturate(sin(angle*9+clock*1.4+n*4)*.5+.5),28)*filament;
    float near=pow(1-signal.x,3);
    float3 pearl=lerp(beamColor,float3(1,.97,.93),.62);
    float3 light=beamColor*(inner*(.15+.38*flow)+fog)*(.75+.25*shape.w)
        +pearl*edge*(.48+.34*progress+.1*near)
        +pearl*inner*glint*.7;
    float alpha=saturate(shade+edge*.18)*signal.z;
    return float4(light*signal.z,alpha);
}
technique Material {
    pass MechanicRingPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Ring(); }
}
