// Original Oboro material: sharp moonlit lip, torn violet interior, flowing filaments.
matrix uWorldViewProjection;
float clock, reduced;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o; o.P=mul(v.P,uWorldViewProjection); o.P.z=0; o.C=v.C; o.U=v.U; return o; }
float4 Arc(VO i):COLOR0
{
 float u=i.U.x, v=i.U.y;
 float flow=sin(u*17-clock*15+sin(u*7+clock*4)*1.6)*.6+sin(u*39+clock*9)*.25;
 float torn=smoothstep(.04,.48,v+flow*.14*(1-reduced*.65));
 float body=sin(saturate(v)*3.141593)*torn;
 float lip=exp2(-pow((v-.94)*35,2));
 float core=exp2(-pow((v-.74-flow*.04)*6,2))*torn;
 float strand=pow(saturate(.5+.5*sin(v*19+flow*3+u*9-clock*20)),11)*body;
 float3 violet=float3(.32,.05,.65)*body*(.8+.14*flow);
 float3 light=violet+float3(.68,.32,1)*core*.66+float3(.61,.27,1)*strand*.33+float3(.94,.84,1)*lip;
 return float4(light*i.C.a,body*.36*i.C.a);
}
float4 Blade(VO i):COLOR0
{
 float along=i.U.x, cross=abs(i.U.y*2-1);
 float taper=(1-along)*.70+.04;
 float edge=exp2(-pow(cross/max(taper,.03)*3,2));
 float core=exp2(-pow(cross/max(taper*.19,.01)*3,2));
 float ends=smoothstep(0,.12,along)*(1-smoothstep(.965,1,along));
 return float4((float3(.52,.20,1)*edge*.8+float3(.92,.82,1)*core)*ends,edge*.08*ends);
}
technique OboroMoonArc
{
 pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Arc(); }
 pass BladePass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Blade(); }
}
