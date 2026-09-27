// Original Oboro material: sharp moonlit lip, torn violet interior, flowing filaments.
matrix uWorldViewProjection;
float clock, reduced;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o; o.P=mul(v.P,uWorldViewProjection); o.P.z=0; o.C=v.C; o.U=v.U; return o; }
float4 Arc(VO i):COLOR0
{
 float u=i.U.x, v=i.U.y;
 float flow=sin(u*11-clock*11+sin(u*5+clock*3)*1.1)*.6+sin(u*29+clock*7)*.25;
 float torn=smoothstep(.08,.52,v+flow*.20*(1-reduced*.65));
 float body=sin(saturate(v)*3.141593)*torn;
 // A broad moon-white cutting shoulder, violet depth and tapering inner tears.
 // The former thin rim made the entire swing read as a wire circle.
 float lip=exp2(-pow((v-.92)*17,2));
 float core=exp2(-pow((v-.76-flow*.055)*8,2))*torn;
 float strand=pow(saturate(.5+.5*sin(v*15+flow*2+u*7-clock*14)),7)*body;
 float3 violet=float3(.26,.045,.53)*body*(.78+.14*flow);
 float3 light=violet+float3(.77,.46,1)*core*.82+float3(.52,.23,.92)*strand*.34+float3(.92,.85,1)*lip;
 return float4(light*i.C.a,body*.30*i.C.a);
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
