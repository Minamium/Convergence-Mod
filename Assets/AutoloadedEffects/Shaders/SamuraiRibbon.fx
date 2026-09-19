// Independent violet blade wake; coordinates follow recorded world-space tips.
matrix uWorldViewProjection;
sampler turbulence : register(s1);
float clock, opacity;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float3 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.P.z=0; o.C=v.C;
 o.U=float2(v.U.x,(v.U.y-.5)/max(.001,v.U.z)+.5); return o; }
float4 PS(VO i):COLOR0
{
 float d=abs(i.U.y*2-1);
 float n=tex2D(turbulence,float2(i.U.x*4-clock*1.4,i.U.y*2)).r;
 float edge=1-smoothstep(.72,1,d);
 float spine=pow(saturate(1-d),7);
 float fade=smoothstep(0,.20,i.U.x)*opacity;
 float3 color=lerp(float3(.12,.04,.23),float3(.39,.32,.95),saturate(1-d+n*.25));
 color=lerp(color,float3(1,.91,1),spine);
 float alpha=edge*fade*(.58+n*.26);
 return float4(color*alpha,alpha)*i.C;
}
technique SamuraiRibbon { pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); } }
