// Original reactor material: contained luminous plasma, not a UI reticle.
float4x4 uWorldViewProjection;
float clock;
float4 signal; // charge, impulse, opacity, reduced-effects detail
sampler cloudNoise : register(s1);
sampler flowNoise : register(s2);
struct VI { float4 position:POSITION0; float4 color:COLOR0; float2 uv:TEXCOORD0; };
struct FI { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
FI VS(VI v) { FI o; o.position=mul(v.position,uWorldViewProjection); o.uv=v.uv; return o; }
float4 Core(FI i):COLOR0 {
    float2 p=(i.uv-.5)*2;
    float radius=length(p), a=atan2(p.y,p.x);
    float pressure=.48+signal.x*.36+signal.y*.16;
    float n=tex2D(cloudNoise,float2(a*.20+clock*.11,radius*2.6-clock*.24)).r;
    float f=tex2D(flowNoise,float2(a*.26-clock*.17,radius*4.1+clock*.40)).r;
    float body=1-smoothstep(.25,.53,radius+(n-.5)*.08*signal.w);
    float swirl=pow(saturate(n*1.5-f*.33),3)*body;
    float hot=exp2(-radius*radius*29)*(pressure+swirl*.7);
    float seam=pow(saturate(1-abs(sin(a*2.0-radius*19+clock*2+n*2))),6)*body;
    float corona=pow(saturate(1-radius),3)*n*(.25+signal.x*.35);
    float cross=(exp2(-p.x*p.x*450)*exp2(-abs(p.y)*3)+exp2(-p.y*p.y*450)*exp2(-abs(p.x)*3))*signal.y;
    float3 red=float3(1,.022,.075), pearl=float3(1,.85,.75);
    float3 light=red*(swirl*.72+seam*.28+corona)+pearl*(hot*1.2+cross*.8);
    return float4(light*signal.z,0);
}
technique CrimsonReactor { pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Core(); } }
