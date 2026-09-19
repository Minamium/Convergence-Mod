// Original Convergence material: keep authored armor, move spectral pigment.
matrix uWorldViewProjection;
sampler artwork : register(s0);
sampler turbulence : register(s1);
sampler veins : register(s2);
float clock, part, charge, hit, dissolution;
float4 region;
float2 texel;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.C=v.C; o.U=v.U; return o; }
float4 PS(VO i):COLOR0
{
 float2 uv=(i.U-region.xy)/region.zw;
 float n=tex2D(turbulence,uv*float2(2.8,2.1)+float2(clock*.10,-clock*.21)).r;
 float f=tex2D(veins,uv*3+float2(n*.18,-clock*.12)).r;
 float4 base=tex2D(artwork,i.U);
 float edge=saturate(base.a-min(tex2D(artwork,i.U+float2(texel.x,0)).a,tex2D(artwork,i.U-float2(texel.x,0)).a));
 float ghost=step(6.5,part);
 float blade=(1-step(.5,abs(part-4)));
 float threads=pow(saturate(f*1.23),5);
 float3 light=float3(.54,.33,1);
 float3 pigment=base.rgb*(.97+n*.07);
 pigment+=base.a*light*(edge*(.26+charge*.30)+threads*(ghost*.25+blade*charge*.36));
 pigment=lerp(pigment,base.a*float3(1,.91,1),hit*.54);
 float erosion=smoothstep(dissolution-.07,dissolution+.07,1-uv.y*.65-n*.35);
 if(dissolution<.001) erosion=1;
 float fracture=pow(saturate(1-abs(f-.52)*22),3)*dissolution*(1-dissolution)*3;
 pigment+=base.a*float3(.73,.42,1)*fracture;
 float opacity=lerp(1,.76+n*.24,ghost)*erosion;
 return float4(pigment*opacity,base.a*opacity)*i.C;
}
technique SamuraiSpirit { pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); } }
