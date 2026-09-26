// Liora's original 48x64 atlas stays the skin. Refraction is restricted to
// dress/sword colors and a thin moving hem; face and hair retain pixel detail.
matrix uWorldViewProjection;
sampler art : register(s0);
sampler grain : register(s1);
sampler facets : register(s2);
float clock;
float4 region; // atlas cell xy and size
float4 signal; // opacity, blade energy, release, accessibility exposure
float3 tone;
float4 shape; // strip opacity, seed, sword flag
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.C=v.C; o.U=v.U; return o; }
float4 SampleArt(float2 uv)
{
    uv=clamp(uv,float2(.008,.008),float2(.992,.992));
    return tex2D(art,region.xy+uv*region.zw);
}
float4 Body(VO i):COLOR0
{
    float2 uv=i.U;
    float4 a=SampleArt(uv);
    float skirt=smoothstep(.52,.83,uv.y);
    float n=tex2D(grain,uv*float2(2.5,3.7)+float2(clock*.045,-clock*.18)).r;
    float f=tex2D(facets,uv*float2(3.6,3.1)+float2(-clock*.09,clock*.13)).r;
    float wave=.5+.5*sin(uv.y*29+uv.x*18+n*5-clock*4.8);
    float ice=saturate((f-.36)*1.5+wave*.23)*skirt*signal.w;
    float3 color=a.rgb*tone;
    // Soft color separation produces a flowing refractive fold, while the
    // authored bodice and hair keep their original value and texture.
    color=lerp(color,color*float3(.80,1.04,1.12)+float3(.035,.20,.28)*ice*a.a,
        skirt*.32);
    color+=float3(.08,.33,.39)*pow(saturate(ice),3)*(.20+signal.y*.18)*a.a;
    float opacity=a.a*signal.x;
    // tML textures are already premultiplied; preserve edge alpha once.
    return float4(color*signal.x,opacity);
}
float4 Aura(VO i):COLOR0
{
    float2 uv=i.U;
    float skirt=smoothstep(.50,.82,uv.y);
    float n=tex2D(grain,uv*float2(3,4)+float2(clock*.06,-clock*.16)).r;
    float2 dx=float2(.027,0),dy=float2(0,.020);
    float center=SampleArt(uv).a;
    float edge=saturate((SampleArt(uv+dx).a+SampleArt(uv-dx).a+
        SampleArt(uv+dy).a+SampleArt(uv-dy).a)*.33-center);
    float v=edge*skirt*(.32+n*.3)*signal.x*signal.w;
    return float4(float3(.10,.72,.88)*v,0);
}
float4 Ribbon(VO i):COLOR0
{
    float x=i.U.x,y=i.U.y*2-1;
    float n=tex2D(grain,float2(x*2.8-clock*.35+shape.y, i.U.y*1.4+clock*.1)).r;
    float f=tex2D(facets,float2(x*4.2-clock*.58+shape.y*.21,i.U.y*1.7)).r;
    float bend=(n-.5)*.23;
    float soft=pow(saturate(1-abs(y-bend)),1.8)*(.52+n*.32+f*.22);
    float thread=exp2(-abs(y-bend)*11)*(.25+f*.4);
    float flow=.46+.54*pow(saturate(.5+.5*sin(x*17-clock*9+n*4)),3);
    float taper=smoothstep(0,.08,x)*(1-smoothstep(.83,1,x));
    float opacity=(soft+thread)*shape.x*taper*(shape.z>.5 ? flow : .47);
    float3 cool=shape.z>.5 ? float3(.28,.93,1) : float3(.10,.59,.81);
    float3 hot=shape.z>.5 ? float3(.81,1,1) : float3(.35,.82,.94);
    return float4(lerp(cool,hot,saturate(thread+f*.25))*opacity,0);
}
technique AzureLiora
{
    pass BodyPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Body(); }
    pass AuraPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Aura(); }
    pass RibbonPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Ribbon(); }
}
