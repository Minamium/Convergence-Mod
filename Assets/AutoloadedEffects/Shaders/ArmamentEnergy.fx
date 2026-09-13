// Original Convergence flowing weapon material. No borrowed art or equations.
float4x4 uWorldViewProjection;
float clock;
float3 beamColor;
float4 signal;
float4 shape;
sampler cloudNoise : register(s1);
sampler flowNoise : register(s2);
struct VI { float4 position:POSITION0; float4 color:COLOR0; float2 uv:TEXCOORD0; };
struct FI { float4 position:SV_POSITION; float4 color:COLOR0; float2 uv:TEXCOORD0; };
FI VS(VI v) { FI o; o.position=mul(v.position,uWorldViewProjection); o.color=v.color; o.uv=v.uv; return o; }
float4 Trail(FI i):COLOR0 {
    float x=i.uv.x,y=i.uv.y*2-1;
    float n=tex2D(cloudNoise,float2(x*5-clock*4.7,y*.8)).r;
    float f=tex2D(flowNoise,float2(x*8-clock*7.2,y*1.7+n*.3)).r;
    float fold=pow(saturate(1-abs(sin(y*10+n*5+f*2))*2),3);
    float profile=1-smoothstep(.76,1,abs(y));
    float core=1-smoothstep(.17+n*.17,.29+n*.17,abs(y+(n-.5)*.16));
    float hot=max(i.color.r,max(i.color.g,i.color.b));
    float3 hue=i.color.rgb/max(hot,.0001);
    float3 fire=lerp(hue*.82,hue*.025,saturate(f*.9+fold*.9));
    float3 light=(fire*profile+lerp(hue,1,.90)*core*(1.1+n*.15))*hot;
    float feather=smoothstep(0,.015,x)*(1-smoothstep(.985,1,x));
    return float4(light*feather,profile*hot*.34*feather);
}
float4 Sigil(FI i):COLOR0 {
    float2 p=i.uv*2-1;
    float r=length(p),a=atan2(p.y,p.x);
    float n=tex2D(cloudNoise,p*1.8+clock*.03).r;
    float f=tex2D(flowNoise,float2(a*1.3-clock*.25,r*4+n*.15)).r;
    float engraving=exp2(-pow((r-.78)*180,2))*(.22+.78*smoothstep(.15,.3,frac(a*1.91-clock*.08)));
    engraving+=exp2(-pow((r-.60)*210,2))*(.3+.7*pow(f,2));
    float spokes=pow(saturate(cos(a*12+clock*.17)),30)*smoothstep(.61,.63,r)*(1-smoothstep(.75,.77,r));
    float lace=exp2(-pow((r-(.41+.035*sin(a*6-clock*.4)))*170,2))*.55;
    float petal=exp2(-pow((r-(.52+.10*cos(a*6+clock*.13)))*170,2))*.45;
    float script=pow(saturate(sin(a*83+sin(a*17)*2)),12)
        *smoothstep(.81,.83,r)*(1-smoothstep(.85,.87,r))*.65;
    float hot=engraving+spokes*.55+lace+petal+script;
    float fog=pow(saturate(1-abs(r-.65)*4),3)*pow(f,3)*.18;
    float3 rgb=beamColor*fog+lerp(beamColor,1,.45)*hot;
    return float4(rgb*signal.z,hot*.22*signal.z);
}
technique ArmamentEnergy {
    pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Trail(); }
    pass WeaponSigilPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Sigil(); }
}
