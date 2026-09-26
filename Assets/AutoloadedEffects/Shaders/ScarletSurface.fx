// Original albedo-preserving material and part masks for the accepted artwork.
matrix uWorldViewProjection;
sampler art : register(s0);
sampler grain : register(s1);
sampler veinTexture : register(s2);
float clock, species, part;
float2 texel, frameSpan;
float4 signal; // charge, recoil, opacity, reduced
float4 region; // atlas-to-local UV offset and scale
float2 ceremony; // material erosion, molten downward flow; zero in ordinary combat
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.C=v.C; o.U=v.U; return o; }
float PartMask(float2 uv)
{
 if(species>2.5) return 1;
 float core=1-smoothstep(species<.5?.13:.10,species<.5?.25:.22,abs(uv.x-.5));
 if(species<.5) core=max(core,1-smoothstep(.40,.54,uv.y));
 float top=1-smoothstep(.45,.57,uv.y);
 float left=1-smoothstep(.47,.53,uv.x);
 if(part<.5) return core;
 if(part<1.5) return (1-core)*left*top;
 if(part<2.5) return (1-core)*(1-left)*top;
 if(part<3.5) return (1-core)*left*(1-top);
 return (1-core)*(1-left)*(1-top);
}
float4 PS(VO i):COLOR0
{
 // CPU supplies the reciprocal atlas transform. On the pinned FNA backend,
 // vector division by region.zw collapsed the local X coordinate to zero.
 float2 uv=i.U*region.zw+region.xy;
 float2 drift=float2(sin(uv.y*19+clock*3)*.012, -sin(uv.x*17+clock*.9)*.018)*ceremony.y;
 float4 base=tex2D(art,i.U+drift*frameSpan);
 float mask=PartMask(uv); clip(base.a*mask-.003);
 float noise=tex2D(grain,uv*4+float2(clock*.022,-clock*.035)).r;
 float tissue=tex2D(veinTexture,uv*3+float2(0,clock*.018)).r;
 float left=tex2D(art,i.U-float2(texel.x,0)).a;
 float right=tex2D(art,i.U+float2(texel.x,0)).a;
 float upper=tex2D(art,i.U-float2(0,texel.y)).a;
 float edge=saturate(base.a-min(min(left,right),upper));
 float cavity=length((uv-float2(.5,.44))*float2(1,1.13));
 float pulse=signal.x*.42+signal.y*.63;
 float3 light=species<.5?float3(1,.29,.055):species<1.5?float3(.44,.32,.85):species<2.5?float3(.69,.09,.37):float3(1,.19,.30);
 float spec=species<.5?pow(noise,4):species<1.5?pow(saturate(.5+.5*sin(uv.y*66+uv.x*8+noise*4-clock)),10):pow(tissue,5);
 // Authored shadow/detail is retained, not replaced by a constant tint.
 float3 color=base.rgb*(.98+noise*.12+pulse*.14);
 color+=light*base.a*(edge*(.32+pulse*.72)+spec*(.04+pulse*.18));
 color+=light*base.a*exp(-cavity*9)*pulse*.17;
 // A two-dimensional material field, not a threshold on the directional
 // flow texture: that turns silhouettes into horizontal scanline strips.
 float2 erosionUV=uv+float2(sin(uv.y*11+clock)*.045,clock*.008);
 float dissolveNoise=.5+.22*sin(erosionUV.x*23+sin(erosionUV.y*17)*2)
     +.17*cos(erosionUV.y*31+erosionUV.x*13-clock*.3)
     +.09*sin(erosionUV.x*47-erosionUV.y*29+clock*.11);
 float threshold=ceremony.x*1.20-.12;
 float material=dissolveNoise*.78+(.85-uv.y)*.22;
 float survivor=ceremony.x<=0?1:smoothstep(threshold-.045,threshold+.045,material);
 float bleed=exp2(-abs(material-threshold)*48)*saturate(ceremony.x*12);
 color=lerp(color,color*float3(1.6,.25,.30),ceremony.x*.7);
 color+=float3(1,.055,.13)*bleed*1.8;
 return float4(color*mask*survivor,base.a*mask*survivor)*i.C;
}
technique ScarletSurface { pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); } }
