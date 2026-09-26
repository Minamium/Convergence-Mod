// DXOboro: ivory edge, gold glints, and torn violet specter flowing behind the cut.
matrix uWorldViewProjection;
float clock, reduced;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o; o.P=mul(v.P,uWorldViewProjection); o.P.z=0; o.C=v.C; o.U=v.U; return o; }
float4 PS(VO i):COLOR0
{
 float u=i.U.x, v=i.U.y;
 float flow=sin(u*29-clock*18+sin(u*8+clock*5)*1.7);
 float shred=sin(u*79-v*24-clock*13)+.55*sin(u*151+v*37+clock*8);
 float veil=smoothstep(.28,.72,v+flow*.12)*smoothstep(-.8,.35,shred);
 float body=sin(saturate(v)*3.141593)*veil;
 float ivory=exp2(-pow((v-.965)*42,2));
 float gold=exp2(-pow((v-.72-flow*.04)*19,2))*(.55+.45*sin(u*34-clock*12));
 float filament=pow(saturate(.5+.5*sin(u*83+v*27-clock*25)),12)*body*(1-reduced*.7);
 float3 rgb=float3(.31,.07,.57)*body*.55+float3(.83,.73,1)*body*.24
   +float3(1,.88,.57)*gold*.47+float3(1,.98,.91)*ivory
   +float3(.68,.38,1)*filament*.3;
 return float4(rgb*i.C.a,(body*.29+ivory*.16+gold*.05)*i.C.a);
}
technique DXOboroVeil
{
 pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); }
}
