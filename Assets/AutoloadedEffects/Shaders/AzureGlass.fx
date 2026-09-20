// Original ice/glass material; existing author-created beam materials remain shared.
matrix uWorldViewProjection;
sampler art : register(s0);
sampler noiseMap : register(s1);
float clock;
float4 signal; // opacity, charge, reduced, index
float4 region;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.C=v.C; o.U=v.U; return o; }
float4 GlassPS(VO i):COLOR0
{
 float2 p=(i.U-region.xy)/region.zw;
 float4 a=tex2D(art,i.U); clip(a.a-.005);
 float n=tex2D(noiseMap,p*2+float2(clock*.032,-clock*.075+signal.w*.17)).r;
 float caustic=pow(saturate(1-abs(n-.48)*5),8);
 float light=pow(saturate(.5+.5*sin(p.x*9+p.y*7-clock*1.8-signal.w*.35)),22);
 float crystal=saturate(a.b-a.r*.72);
 float3 color=a.rgb*(.90+n*.18)+float3(.08,.45,.70)*(caustic*.11+light*(.12+signal.y*.25))*a.a*crystal;
 return float4(color,a.a)*i.C*signal.x;
}
float4 BackdropPS(VO i):COLOR0
{
 float2 uv=i.U;
 float n=tex2D(noiseMap,uv*float2(3,5)+float2(clock*.007,-clock*.014)).r;
 float water=smoothstep(.69,.91,uv.y);
 uv.x+=sin(uv.y*49-clock*.9+n*4)*.0016*water*(1-signal.z);
 float3 color=tex2D(art,uv).rgb;
 float shaft=pow(saturate(.5+.5*sin(uv.x*17+uv.y*3+clock*.12)),24)*smoothstep(.7,.1,uv.y);
 float mist=pow(saturate(n-.26),3)*water;
 color=color*.68+float3(.045,.16,.20)*(shaft*.5+mist*.3);
 return float4(color*signal.x,signal.x)*i.C;
}
float4 ShardPS(VO i):COLOR0
{
 float2 p=i.U;
 float envelope=saturate(1-abs(p.y-.5)*2/(max(.035,sin(p.x*3.14159))));
 float n=tex2D(noiseMap,float2(p.x*4-clock*.8,p.y*3)).r;
 float core=pow(envelope,2.5);
 float face=step(p.y,.5)*.28+pow(envelope,12)*.7;
 float a=saturate(envelope*5)*smoothstep(0,.08,p.x)*smoothstep(1,.92,p.x);
 float spine=pow(saturate(1-abs(p.y-.5)*2),32);
 float3 c=lerp(float3(.10,.40,.64),float3(.8,.98,1),face)*(.85+n*.35)+float3(.20,.42,.48)*spine;
 return float4(c*a,a)*i.C*signal.x;
}
technique AzureGlass
{
 pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 GlassPS(); }
 pass BackdropPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 BackdropPS(); }
 pass ShardPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 ShardPS(); }
}
