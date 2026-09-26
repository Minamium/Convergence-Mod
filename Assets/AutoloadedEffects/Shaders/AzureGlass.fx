// Original ice/glass material; existing author-created beam materials remain shared.
matrix uWorldViewProjection;
sampler art : register(s0);
sampler noiseMap : register(s1);
sampler furyArt : register(s2);
sampler veinMap : register(s3);
float clock;
float4 signal; // opacity, charge, reduced, index
float4 region;
float spine;
float dissolve;
float silhouette;
float fury,furySpine;
float2 orbSize;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.C=v.C; o.U=v.U; return o; }
float4 GlassSurface(VO i,float4 a,float2 p)
{
 clip(a.a-.005);
 float n=tex2D(noiseMap,p*2+float2(clock*.032,-clock*.075+signal.w*.17)).r;
 float caustic=pow(saturate(1-abs(n-.48)*5),8);
 float light=pow(saturate(.5+.5*sin(p.x*9+p.y*7-clock*1.8-signal.w*.35)),22);
 float crystal=saturate(a.b-a.r*.72);
 float3 color=a.rgb*(.90+n*.18)+float3(.08,.45,.70)*(caustic*.11+light*(.12+signal.y*.25))*a.a*crystal;
 float pressure=pow(saturate(.5+.5*sin(p.x*14-clock*4.4-signal.w*.37)),10);
 color+=fury*float3(.13,.38,.48)*pressure*crystal*a.a;
 float meltNoise=tex2D(noiseMap,p*float2(4,2)+float2(signal.w*.19,-clock*.10)).r;
 float retain=dissolve>.001 ? 1-smoothstep(meltNoise-.10,meltNoise+.10,dissolve*1.25) : 1;
 float edge=exp(-abs(meltNoise-dissolve*1.25)*32)*step(.001,dissolve);
 color+=float3(.22,.70,.95)*edge*a.a;
 return float4(lerp(color,float3(.003,.008,.014)*a.a,silhouette),a.a)*i.C*signal.x*retain;
}
float4 WormArt(float2 p)
{
 float2 sample=p;sample.y=spine-abs(p.y-.5);
 sample.x+=sin(p.y*19+clock*2)*dissolve*.035;
 float2 alternate=sample;alternate.y=furySpine-abs(p.y-.5);
 return lerp(tex2D(art,region.xy+sample*region.zw),tex2D(furyArt,region.xy+alternate*region.zw),fury);
}
float4 GlassPS(VO i):COLOR0
{
 float2 p=(i.U-region.xy)/region.zw;
 return GlassSurface(i,WormArt(p),p);
}
float4 WormGlowPS(VO i):COLOR0
{
 float2 p=(i.U-region.xy)/region.zw;
 float4 a=WormArt(p);
 // Original armor remains opaque beneath this masked, moving material. Pale
 // facets catch refraction; blue recesses carry veins, not a solid glow slab.
 float2 q=float2(p.x,abs(p.y-.5));
 float n=tex2D(noiseMap,q*float2(3.7,5.3)+float2(-clock*.19+signal.w*.17,clock*.037)).r;
 float vein=tex2D(veinMap,q*float2(2.3,3.9)+float2(-clock*.11,n*.16)).r;
 float ridge=pow(saturate(1-abs(n-.48)*7),8);
 float pulse=pow(saturate(.5+.5*sin(p.x*8-clock*3.8+signal.w*.41)),6);
 float crystal=saturate((a.b-a.r*.6)*2)*a.a;
 float luminance=dot(a.rgb,float3(.22,.57,.21));
 float ice=pow(saturate(vein),2)*(.26+signal.y*.14)+ridge*(.25+pulse*.25);
 float wet=pow(saturate(1-abs(n+q.y*.5-.63)*5),12)*luminance*.13;
 float rim=0;
 rim+=WormArt(p+float2(.008,0)).a;rim+=WormArt(p-float2(.008,0)).a;
 rim+=WormArt(p+float2(0,.008)).a;rim+=WormArt(p-float2(0,.008)).a;
 rim=saturate(rim*.25-a.a)*(.10+signal.y*.055)*(n*.45+.55);
 float meltNoise=tex2D(noiseMap,p*float2(4,2)+float2(signal.w*.19,-clock*.10)).r;
 float retain=dissolve>.001?1-smoothstep(meltNoise-.10,meltNoise+.10,dissolve*1.25):1;
 float exposure=lerp(1,.44,signal.z)*signal.x*(1-silhouette)*retain;
 float3 c=float3(.09,.46,.61)*(ice*crystal+rim)+float3(.66,.88,.94)*wet;
 return float4(c*exposure,0)*i.C;
}
float4 WormRibbonPS(VO i):COLOR0
{
 float2 p=i.U;
 float n=tex2D(noiseMap,p*float2(3.5,1.8)+float2(-clock*1.2+signal.w*.19,clock*.10)).r;
 float fine=tex2D(veinMap,p*float2(4,2)+float2(-clock*.65,n*.32)).r;
 float y=(p.y-.5)*2;
 float feather=pow(saturate(1-y*y),2);
 float front=smoothstep(0,.075,p.x)*(1-smoothstep(.60,1,p.x));
 float thread=exp2(-pow((y-(n-.5)*.63)*13,2));
 float mist=pow(saturate(n*.7+fine*.55-.3),2)*feather;
 float3 c=float3(.07,.35,.49)*mist+float3(.48,.83,.94)*(thread*.38+pow(saturate(fine),5)*feather*.35);
 return float4(c*front*signal.x*(.8+signal.y*.27),0)*i.C;
}
float4 PartPS(VO i):COLOR0
{
 // Detached pincers/throat share refraction, pressure and dissolve with the
 // intact armor, but are never atlas-spine mirrored or head-slice transformed.
 return GlassSurface(i,tex2D(art,i.U),i.U);
}
float4 EnergyOrbPS(VO i):COLOR0
{
 float2 p=(i.U-.5)*orbSize;float radius=max(signal.y,.1);
 float n=tex2D(noiseMap,float2(p.x*.027-clock*2.8+signal.w,p.y*.055+clock*.33)).r;
 float fine=tex2D(noiseMap,float2(p.x*.049-clock*4.1,p.y*.083+n*.44)).r;
 // A bright leading heart carried by transparent streaming filaments, not a
 // faceted physical icicle. Dim vapor outside the capsule has no collision.
 float front=1-smoothstep(12,26,p.x),tail=exp(-max(0,-p.x-8)*.037);
 float height=radius*(.58+.42*saturate((p.x+65)/72));
 float flutter=sin(p.x*.10-clock*16+signal.w)*(1-signal.z*.75);
 float y=(p.y-flutter*.9)/max(.4,height);
 float body=exp2(-y*y*1.8)*(n*.55+fine*.45)*front*tail;
 float core=exp2(-pow((p.x-9)/14,2)-pow(p.y/max(1,radius*.5),2));
 float stream=exp2(-pow((p.y-(n-.5)*height*1.5)/max(.5,radius*.17),2))*tail*front;
 float halo=exp2(-pow((p.x+8)/48,2)-pow(p.y/(radius*2.3+5),2))*.16;
 float wisps=pow(saturate(n*.7+fine*.5-.45),2)*exp2(-pow(p.y/(radius*2.1+8),2))*tail*front*.28;
 // Feather the whole vapor tail, not merely a narrow quad edge: otherwise the
 // low-frequency halo reads as a rectangular decal against a dark arena.
 float border=smoothstep(0,.28,i.U.x)*(1-smoothstep(.82,1,i.U.x))*smoothstep(0,.24,i.U.y)*(1-smoothstep(.76,1,i.U.y));
 float3 c=float3(.07,.53,.97)*(body+halo+wisps)+float3(.74,.96,1)*(core*.85+stream*.65);
 return float4(c*border*signal.x,0)*i.C;
}
float4 FrostPS(VO i):COLOR0
{
 float2 p=i.U;
 float2 flow=p*float2(3.8,2.2)+float2(-clock*.10+signal.w*.31,-clock*.06);
 float n=tex2D(noiseMap,flow).r;
 float detail=tex2D(noiseMap,flow*2.07+float2(clock*.045,n*.42)).r;
 float curl=tex2D(noiseMap,flow*1.3+float2(n*.5,-clock*.13)).r;
 float mask=pow(saturate(1-pow((p.x-.5)*2,2)),.65)*pow(saturate(1-pow((p.y-.5)*2,2)),1.6);
 float wisps=smoothstep(.30,.72,n*.55+detail*.27+curl*.32);
 float threads=pow(saturate(1-abs(detail-.48)*8),8)*.16;
 float a=(wisps+threads)*mask*signal.x;
 return float4(lerp(float3(.28,.48,.58),float3(.78,.94,.99),n)*a,a)*i.C;
}
float4 BackdropPS(VO i):COLOR0
{
 float2 uv=i.U;
 float n=tex2D(noiseMap,uv*float2(3,5)+float2(clock*.007,-clock*.014)).r;
 float water=smoothstep(.69,.91,uv.y);
 uv.x+=sin(uv.y*49-clock*.9+n*4)*.0016*water*(1-signal.z);
 float3 color=tex2D(art,uv).rgb;
 float shaft=pow(saturate(.5+.5*sin(uv.x*17+uv.y*3+clock*.12)),24)*smoothstep(.7,.1,uv.y);
 float mist=pow(saturate(n-.26),3)*water;
 color=color*.68+float3(.045,.16,.20)*(shaft*.5+mist*.3);
 return float4(color*signal.x,signal.x)*i.C;
}
float4 ShardPS(VO i):COLOR0
{
 float2 p=i.U;
 float envelope=saturate(1-abs(p.y-.5)*2/(max(.035,sin(p.x*3.14159))));
 float n=tex2D(noiseMap,float2(p.x*4-clock*.8,p.y*3)).r;
 float core=pow(envelope,2.5);
 float face=step(p.y,.5)*.28+pow(envelope,12)*.7;
 float a=saturate(envelope*5)*smoothstep(0,.08,p.x)*smoothstep(1,.92,p.x);
 float spine=pow(saturate(1-abs(p.y-.5)*2),32);
 float facet=pow(saturate(1-abs(frac(p.x*5+p.y*2+clock*.04)-.5)*8),8)*envelope;
 float3 c=lerp(float3(.06,.36,.62),float3(.83,.99,1),face)*(.90+n*.25)+float3(.32,.62,.74)*(spine+facet*.65);
 return float4(c*a,a)*i.C*signal.x;
}
float4 RiftPS(VO i):COLOR0
{
 float2 p=i.U*2-1;
 float taper=pow(saturate(1-abs(p.x)),.48);
 float n=tex2D(noiseMap,float2(p.x*2-clock*.4,p.y*1.7+clock*.22)).r;
 float edge=taper*(.24+signal.y*.20)+(n-.5)*.05*taper;
 float d=abs(abs(p.y)-edge), band=exp(-d*45)*taper;
 float inside=smoothstep(edge+.035,edge-.02,abs(p.y))*taper;
 float glow=exp(-d*10)*.22*taper;
 float3 color=float3(.23,.8,1)*(band+glow)+float3(.008,.025,.075)*inside;
 float a=saturate(inside+band+glow);
 return float4(color,a)*signal.x*i.C;
}
float4 IcePS(VO i):COLOR0
{
 float2 p=i.U*2-1; float shape=max(abs(p.x)*.92+abs(p.y)*.32,abs(p.y));
 float mask=smoothstep(1,.96,shape),rim=exp(-abs(shape-.945)*85);
 float facet=frac(p.x*1.9+abs(p.y)*1.6);
 float cracks=pow(saturate(1-abs(sin(p.x*13+p.y*7))*9),8)*signal.y;
 float n=tex2D(noiseMap,p*2+float2(clock*.01,0)).r;
 float caustic=pow(saturate(1-abs(n-.5)*6),9);
 float a=(.15+facet*.18+rim*.8+cracks*.45)*mask;
 float3 c=float3(.18,.60,.80)*a+float3(.55,.87,1)*(rim*.58+cracks*.8+caustic*.16)*mask;
 return float4(c,a)*signal.x*i.C;
}
float4 CirclePS(VO i):COLOR0
{
 float2 p=i.U*2-1; float r=length(p),angle=atan2(p.y,p.x)+1.570796;
 if(angle<0)angle+=6.283185;
 float ring=exp(-abs(r-.94)*150),fine=exp(-abs(r-.9)*190)*.25;
 float timer=step(angle/6.283185,1-signal.y)*exp(-abs(r-.84)*100);
 float n=tex2D(noiseMap,p*2+clock*.06).r;
 float sparks=pow(saturate(n),12)*exp(-abs(r-.94)*22);
 float a=ring*.65+fine+timer+sparks;
 float3 c=lerp(float3(.12,.60,.88),float3(.70,.98,1),timer)*(a);
 return float4(c,saturate(a))*signal.x*i.C;
}
technique AzureGlass
{
 pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 GlassPS(); }
 pass PartPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PartPS(); }
 pass WormGlowPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 WormGlowPS(); }
 pass WormRibbonPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 WormRibbonPS(); }
 pass BackdropPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 BackdropPS(); }
 pass ShardPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 ShardPS(); }
 pass RiftPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 RiftPS(); }
 pass IcePass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 IcePS(); }
 pass CirclePass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 CirclePS(); }
 pass FrostPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 FrostPS(); }
 pass EnergyOrbPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 EnergyOrbPS(); }
}
