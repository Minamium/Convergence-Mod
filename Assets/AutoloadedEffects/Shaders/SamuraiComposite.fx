// Original two-resolution spectral composite. Gold/black armor is not bloom.
matrix uWorldViewProjection;
sampler body : register(s0);
sampler emission : register(s1);
sampler turbulence : register(s2);
float clock, charge, hit, reduced;
float2 texel;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection);o.C=v.C;o.U=v.U;return o; }
float4 Extract(VO i):COLOR0
{
 float4 s=tex2D(body,i.U);
 float energy=saturate((s.b-s.g*1.10)*3.5)*smoothstep(.20,.8,max(s.r,s.b));
 return s*energy;
}
float4 PS(VO i):COLOR0
{
 float2 uv=i.U;
 float4 original=tex2D(body,uv);
 float2 flow=float2(tex2D(turbulence,uv*3+float2(clock*.2,-clock*.6)).r-.5,
  tex2D(turbulence,uv*3+float2(-clock*.31,clock*.19)).r-.5);
 float2 radius=texel*(3.2+charge*2.5)*(1-reduced*.5);
 float4 aura=tex2D(emission,uv+flow*texel*3)*.22;
 aura+=tex2D(emission,uv+float2(radius.x,0))*.13;
 aura+=tex2D(emission,uv-float2(radius.x,0))*.13;
 aura+=tex2D(emission,uv+float2(0,radius.y))*.13;
 aura+=tex2D(emission,uv-float2(0,radius.y))*.13;
 aura+=tex2D(emission,uv+radius*2)*.065;
 aura+=tex2D(emission,uv-radius*2)*.065;
 aura+=tex2D(emission,uv+float2(radius.x,-radius.y)*2)*.065;
 aura+=tex2D(emission,uv+float2(-radius.x,radius.y)*2)*.065;
 // Keep the actual armor/face intact. Distortion affects the emitted veil only.
 float glow=(.85+charge*.6+hit*.55)*(1-reduced*.35);
 float3 color=original.rgb+aura.rgb*float3(.71,.45,1)*glow;
 float alpha=saturate(original.a+aura.a*.34*(1-original.a));
 return float4(color,alpha)*i.C;
}
technique SamuraiComposite
{
 pass ExtractPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Extract(); }
 pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); }
}
