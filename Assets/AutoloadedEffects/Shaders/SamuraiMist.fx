// Connected, translucent violet spirit smoke over Luminance's managed mask.
sampler maskTexture : register(s0);
sampler turbulence : register(s1);
float clock;
float2 screenSize, worldOffset;
float4 PS(float4 tint:COLOR0,float2 uv:TEXCOORD0):COLOR0
{
 float mask=tex2D(maskTexture,uv).a;
 float2 world=(uv*screenSize+worldOffset)/132;
 float n=tex2D(turbulence,world+float2(clock*.055,-clock*.16)).r;
 float curl=tex2D(turbulence,world*2.6+float2(-clock*.11,clock*.04)).r;
 float rim=smoothstep(.045,.15,mask)*(1-smoothstep(.20,.53,mask));
 float alpha=smoothstep(.04,.22,mask)*(.12+n*.13);
 float3 spirit=lerp(float3(.11,.055,.19),float3(.42,.27,.74),n*curl);
 spirit+=float3(.54,.41,.93)*rim*.46;
 return float4(spirit*alpha,alpha)*tint;
}
technique SamuraiMist { pass AutoloadPass { PixelShader=compile ps_3_0 PS(); } }
