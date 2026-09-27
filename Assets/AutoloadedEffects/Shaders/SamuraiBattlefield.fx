// World-anchored ruins and moving violet spirit weather. The field quad is
// behind terrain and combat actors; all silhouettes stay deliberately dim.
matrix uWorldViewProjection;
sampler turbulence : register(s1);
sampler veins : register(s2);
float clock, reduced;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.C=v.C; o.U=v.U; return o; }
float box(float2 p, float2 a, float2 b, float feather)
{
    float2 lo=smoothstep(a-feather,a+feather,p);
    float2 hi=1-smoothstep(b-feather,b+feather,p);
    return lo.x*lo.y*hi.x*hi.y;
}
float segment(float2 p,float2 a,float2 b,float width)
{
    float2 d=b-a;
    float h=saturate(dot(p-a,d)/dot(d,d));
    return 1-smoothstep(width-.001,width+.001,length(p-a-d*h));
}
float4 PS(VO i):COLOR0
{
    float2 p=i.U;
    float slow=clock*(1-reduced*.72);
    float n=tex2D(turbulence,p*float2(2.5,3.1)+float2(slow*.012,-slow*.023)).r;
    float m=tex2D(veins,p*float2(5.1,2.7)+float2(-slow*.018,slow*.011)).r;
    float horizon=.45+.022*sin(p.x*22)+.012*sin(p.x*61);
    float far=smoothstep(horizon-.012,horizon+.12,p.y);
    float3 sky=lerp(float3(.011,.008,.032),float3(.055,.026,.088),saturate(p.y*.88+n*.22));
    sky=lerp(sky,float3(.08,.043,.135),far*.46);
    // Five alternating mist strata, each with its own velocity and curl.
    float fog=0;
    fog+=exp(-pow((p.y-(.31+sin(p.x*10+slow*.11)*.018)),2)/.006)*(.16+.20*n);
    fog+=exp(-pow((p.y-(.56+sin(p.x*13-slow*.07)*.022)),2)/.009)*(.12+.19*m);
    fog+=exp(-pow((p.y-(.75+sin(p.x*18+slow*.09)*.025)),2)/.014)*(.13+.17*n);
    fog*=1-reduced*.56;
    sky+=float3(.17,.075,.29)*fog;
    // Low arched grave markers emerge from the ground rather than hanging in
    // mid-air where a rectangular silhouette reads as a flag.
    float cell=floor(p.x*12), local=frac(p.x*12);
    float graveTop=.72+.02*frac(sin(cell*47.2)*43758.5);
    float graveBody=box(float2(local,p.y),float2(.38,graveTop),float2(.67,.88),.005);
    float graveCap=1-smoothstep(.15,.17,length(float2(local-.525,(p.y-graveTop)*2.8)));
    float grave=max(graveBody,graveCap*step(p.y,graveTop));
    // Closer lances lean at different angles among the graves.
    float spearCell=floor(p.x*18), spearX=(spearCell+.33+.2*frac(sin(spearCell*13.7)*91.1))/18;
    float lean=(frac(sin(spearCell*17.13)*13.9)-.5)*.025;
    float spear=segment(p,float2(spearX,.78),float2(spearX+lean,.35+.12*frac(sin(spearCell*8.2)*45.1)),.0015);
    float head=segment(p,float2(spearX+lean-.004,.40),float2(spearX+lean,.35),.002);
    float silhouette=saturate(grave*.55+spear*.48+head*.4);
    sky=lerp(sky,float3(.018,.012,.037),silhouette);
    float ground=smoothstep(.83+.012*sin(p.x*31),.90,p.y);
    sky=lerp(sky,float3(.012,.008,.028),ground*.86);
    float filament=pow(saturate(1-abs(sin(p.x*38+p.y*15+slow*.52+n*5))),24);
    filament*=smoothstep(.49,.66,p.y)*(1-smoothstep(.84,.94,p.y));
    sky+=float3(.12,.048,.21)*filament*m*(1-reduced*.75);
    // The mask owns the exact field edge. Fading this sky's alpha exposes the
    // vanilla sky inside the arena, especially when zoom moves a boundary.
    return float4(sky,1);
}
technique SamuraiBattlefield { pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); } }
