// Body and emissive anatomy are composed offscreen, never a fullscreen filter.
matrix uWorldViewProjection;
sampler body:register(s0);sampler glow:register(s1);sampler grain:register(s2);
float clock;float2 texel;float4 signal;
struct VI{float4 P:POSITION0;float4 C:COLOR0;float2 U:TEXCOORD0;};
struct VO{float4 P:SV_POSITION;float4 C:COLOR0;float2 U:TEXCOORD0;};
VO VS(VI v){VO o=(VO)0;o.P=mul(v.P,uWorldViewProjection);o.C=v.C;o.U=v.U;return o;}
float3 Emission(float2 uv){float3 c=tex2D(body,uv).rgb;float hot=max(c.r-max(c.g,c.b)*.72,max(c.g,c.b)-.4);return c*saturate(hot-.12);}
float4 Extract(VO i):COLOR0
{
 float2 d=float2(texel.x*4,0);float3 c=Emission(i.U)*.22;
 c+=(Emission(i.U+d)+Emission(i.U-d))*.19;
 c+=(Emission(i.U+d*2)+Emission(i.U-d*2))*.13;
 c+=(Emission(i.U+d*3)+Emission(i.U-d*3))*.07;
 return float4(c,0);
}
float4 Composite(VO i):COLOR0
{
 float2 q=i.U*2-1;
 float n=tex2D(grain,i.U*4+float2(clock*.07,-clock*.11)).r;
 float2 uv=i.U+float2(sin(i.U.y*32+clock*3),cos(i.U.x*29-clock*2))*texel*(n-.5)*1.2;
 float4 a=tex2D(body,uv);
 float2 d=float2(0,texel.y*3);float3 g=tex2D(glow,i.U).rgb*.28;
 g+=(tex2D(glow,i.U+d).rgb+tex2D(glow,i.U-d).rgb)*.23;
 g+=(tex2D(glow,i.U+d*2).rgb+tex2D(glow,i.U-d*2).rgb)*.13;
 float exposure=lerp(.80,.30,signal.w);
 float border=1-smoothstep(.88,1,max(abs(q.x),abs(q.y)));
 return float4((a.rgb+g*exposure)*signal.z*border,a.a*signal.z*border)*i.C;
}
technique ScarletAvatarComposite{
 pass AutoloadPass{VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Composite();}
 pass ExtractPass{VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Extract();}
}
