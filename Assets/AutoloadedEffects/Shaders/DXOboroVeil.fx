// Original violet cutting surfaces and large white-lilac fractures.
// Shape is authored by the mesh; materials open, tear and extinguish in cut time.
matrix uWorldViewProjection;
float clock, cut, progress, recovery, mode, reduced;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o; o.P=mul(v.P,uWorldViewProjection); o.P.z=0; o.C=v.C; o.U=v.U; return o; }
float4 PS(VO i):COLOR0
{
    float u=i.U.x, v=i.U.y, opacity=i.C.a;
    float3 violet=float3(.39,.055,.94), lilac=float3(.76,.49,1), white=float3(.96,.93,1);
    if(mode>.5)
    {
        float cross=abs(v*2-1);
        float core=1-smoothstep(mode>1.5?.45:.45,mode>1.5?.70:.67,cross);
        float halo=pow(saturate(1-cross),1.8);
        float strand=sin(u*37-clock*32+cut*2)*.5+.5;
        float3 rgb=white*core*(1-reduced*.16)+violet*halo*.30*(1-core)+lilac*halo*strand*.09;
        float a=saturate(core*.9+halo*.45);
        return float4(rgb*opacity,a*opacity);
    }
    // Large torn light patches, not a uniform fill with a thin neon outline.
    float flow=sin(u*11.5-progress*4+cut*1.7)*.046;
    float entry=smoothstep(.008,.07,v);
    float edge=smoothstep(.78+flow,.93+flow,v)*(1-smoothstep(.985,1,v));
    float flute=sin(u*18+v*6-progress*6+cut*1.9)*.5+.5;
    float gouge=.16+pow(saturate(sin(u*24-cut*1.8+progress*2)),4)*.38;
    float gap=smoothstep(gouge,gouge+.035,v);
    float belly=pow(saturate(sin(v*3.141593)),.65);
    float pressure=sin(saturate(progress)*3.141593);
    float ridge=.71+.07*sin(u*9-progress*3+cut);
    float broad=(1-smoothstep(.11+pressure*.09,.19+pressure*.09,abs(v-ridge)))*smoothstep(.12,.42,u);
    float feather=sin(u*31+v*4+cut*2-progress*3)*.5+.5;
    broad*=1-smoothstep(.80,.94,feather)*(1-smoothstep(.34,.83,v))*.68;
    float rip=1-smoothstep(.68,.9,progress)*smoothstep(.15,.46,1-v)*(.45+.55*flute);
    float body=belly*gap*entry*rip;
    float hot=saturate(broad*.95+edge*.52)*gap*(1-recovery)*(1-reduced*.23);
    float3 rgb=violet*body*.55*(1-hot)+lilac*body*.06+white*hot;
    float alpha=saturate(body*.81+hot*.64)*entry;
    return float4(rgb*opacity,alpha*opacity);
}
technique DXOboroVeil
{
    pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); }
}
