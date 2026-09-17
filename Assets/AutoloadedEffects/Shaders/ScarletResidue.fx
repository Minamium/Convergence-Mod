// Original connected cinder/ink material over Luminance's managed particle mask.
sampler maskTexture : register(s0);
sampler noiseTexture : register(s1);
sampler grainTexture : register(s2);
float clock, species;
float2 screenSize, worldOffset;
float4 PS(float4 color:COLOR0,float2 uv:TEXCOORD0):COLOR0
{
 float mask=tex2D(maskTexture,uv).a;
 float2 p=(uv*screenSize+worldOffset)/180;
 float n=tex2D(noiseTexture,p+float2(clock*.045,-clock*.065)).r;
 float g=tex2D(grainTexture,p*2+float2(-clock*.07,0)).r;
 float edge=smoothstep(.08,.26,mask)*(1-smoothstep(.28,.56,mask));
 float alpha=smoothstep(.075,.25,mask)*.58;
 float3 interior=species<1?lerp(float3(.12,.021,.005),float3(.67,.17,.022),n*g):lerp(float3(.018,.008,.03),float3(.16,.028,.11),n*g);
 float3 rim=species<1?float3(1,.34,.07):float3(.48,.065,.28);
 return float4((interior+rim*edge*.62)*alpha,alpha)*color;
}
technique ScarletResidue { pass AutoloadPass { PixelShader=compile ps_3_0 PS(); } }
