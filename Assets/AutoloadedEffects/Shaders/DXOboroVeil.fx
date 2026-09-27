// Soboro's deep-violet torn wing and white-lilac cutting edge.
matrix uWorldViewProjection;
float clock, reduced;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o; o.P=mul(v.P,uWorldViewProjection); o.P.z=0; o.C=v.C; o.U=v.U; return o; }
float4 PS(VO i):COLOR0
{
 float u=i.U.x, v=i.U.y;
 float flow=sin(u*13-clock*10+sin(u*5+clock*3)*.5);
 float shred=sin(u*31-v*7-clock*4)+.25*sin(u*61+v*13+clock*2);
 float veil=smoothstep(.04,.32,v+flow*.045)*smoothstep(-1.8,-.35,shred);
 float body=pow(saturate(sin(v*3.141593)),.58)*veil;
 float edge=exp2(-pow((v-.955)*28,2));
 float inner=exp2(-pow((v-.37-flow*.025)*9,2))*(.78+.22*sin(u*17-clock*9));
 float filament=pow(saturate(.5+.5*sin(u*25+v*4-clock*12)),15)*body*(1-reduced*.65);
 float3 rgb=float3(.18,.025,.43)*body*.78+float3(.64,.31,.88)*body*.26
   +float3(.76,.56,1)*inner*.23+float3(.95,.96,1)*edge*.83
   +float3(.78,.56,1)*filament*.29;
 return float4(rgb*i.C.a,(body*.69+edge*.43+inner*.12)*i.C.a);
}
technique DXOboroVeil
{
 pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); }
}
