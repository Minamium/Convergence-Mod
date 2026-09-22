// Original contained scarlet plasma. Curved noisy surface, not a painted disk.
float4x4 uWorldViewProjection;
float clock;
float4 signal; // charge, release, opacity, reduced detail
float4 shape; // length, half-width, stable seed, reserved
sampler clouds : register(s1);
sampler veins : register(s2);
struct VI { float4 position:POSITION0; float4 color:COLOR0; float2 uv:TEXCOORD0; };
struct FI { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
FI VS(VI v) { FI o; o.position=mul(v.position,uWorldViewProjection); o.uv=v.uv; return o; }
float4 Energy(FI i):COLOR0 {
    float2 p=(i.uv-.5)*2.8;
    float r=length(p), z=sqrt(saturate(1-dot(p,p)));
    float2 flow=float2(atan2(p.y,p.x)*.26,z*.7-r*.4);
    float n=tex2D(clouds,flow+float2(clock*.09+shape.z*.19,-clock*.17)).r;
    float f=tex2D(veins,flow*2.8+float2(-clock*.14,clock*.21)).r;
    float body=1-smoothstep(.94,1.015,r+(n-.5)*.025*signal.w);
    float strand=pow(saturate(n*1.45-f*.38),4);
    float filaments=pow(saturate(1-abs(sin(z*14-r*11+n*8-clock*2.2))),9)*(.3+f*.7);
    float rim=exp2(-abs(r-.92)*37)*(n*.45+.22);
    float light=saturate(dot(float3(p,z),normalize(float3(-.35,-.5,1))));
    float hot=pow(light,10)*(.2+strand)*(.18+signal.x*.35+signal.y*.3);
    float3 red=float3(1,.023,.065), pearl=float3(1,.46,.39);
    float3 surface=float3(.065,.001,.008)+red*(.22+light*.34+strand*1.45)
        +float3(1,.21,.17)*filaments*.95+pearl*(hot*1.4+pow(z,5)*(.14+signal.x*.12));
    float glow=exp2(-max(0,r-.91)*8)*(1-body)*(.25+n*.3)*(1+signal.y*.5);
    float alpha=body*.94*signal.z;
    return float4(surface*alpha+red*(rim*.9+glow)*signal.z,alpha);
}
float4 Forecast(FI i):COLOR0 {
    float y=abs(i.uv.y-.5)*shape.y*2, x=i.uv.x*shape.x;
    float grain=tex2D(veins,float2(x*.018-clock*.12,i.uv.y*3+shape.z)).r;
    float center=exp2(-y*y*.25);
    float motes=pow(saturate((grain-.72)*3.6),4)*(1-smoothstep(shape.y*.75,shape.y,y));
    float along=smoothstep(0,14,x)*smoothstep(0,14,shape.x-x);
    float3 red=float3(1,.09,.14), core=float3(1,.69,.66);
    return float4((core*center*.7+red*motes*.44)*along*signal.z,0);
}
float4 Tail(FI i):COLOR0 {
    float y=abs(i.uv.y-.5)*2, x=i.uv.x;
    float n=tex2D(clouds,float2(x*3-clock*1.9+shape.z,i.uv.y*2-clock*.13)).r;
    float width=(.06+.65*x)*(1-y);
    float stream=pow(saturate(width+n*.34-.17),3)*smoothstep(0,.5,x);
    return float4(float3(1,.014,.047)*stream*signal.z*1.8,0);
}
technique ScarletCluster {
    pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Energy(); }
    pass ClusterForecastPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Forecast(); }
    pass ClusterTailPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Tail(); }
}
