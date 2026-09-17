// Original albedo-preserving material and part masks for the accepted artwork.
matrix uWorldViewProjection;
sampler art : register(s0);
sampler grain : register(s1);
sampler veinTexture : register(s2);
float clock, species, part;
float2 texel;
float4 signal; // charge, recoil, opacity, reduced
float4 region; // atlas source XY and WH normalized
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
 float2 uv=(i.U-region.xy)/region.zw;
 float4 base=tex2D(art,i.U);
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
 return float4(color*mask,base.a*mask)*i.C;
}
technique ScarletSurface { pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); } }
