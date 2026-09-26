// Original organic emissive material. WotG informs the separation of animated
// anatomy/material/afterimage, not the artwork or shader implementation.
matrix uWorldViewProjection;
sampler art : register(s0);
sampler grain : register(s1);
sampler veins : register(s2);
float clock, armsOnly;
float4 signal; // excitation, discharge, opacity, accessibility exposure
float2 ceremony;
float4 shape;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.C=v.C; o.U=v.U; return o; }
float Survive(float2 uv)
{
 float n=.50+.21*sin(uv.x*23+sin(uv.y*17)*2)+.17*cos(uv.y*31+uv.x*13-clock*.3);
 return ceremony.x<=0?1:smoothstep(ceremony.x*1.25-.16,ceremony.x*1.25-.06,n);
}
float4 Body(VO i):COLOR0
{
 float4 a=tex2D(art,i.U);
 float limb=i.C.r;
 float opacity=a.a*signal.z*Survive(i.U)*lerp(1,limb,armsOnly);
 float n=tex2D(grain,i.U*5+float2(clock*.07,-clock*.13)).r;
 float v=tex2D(veins,i.U*3+float2(clock*.04,-clock*.10)).r;
 float bone=smoothstep(.12,.55,dot(a.rgb,float3(.30,.46,.24)));
 float current=pow(saturate(.5+.5*sin(i.U.y*50+n*5-clock*8)),5);
 float energy=(.38+i.C.g*1.35+i.C.b*2.3)*limb*bone*signal.w;
 float hot=saturate((v-.28)*2.7+current*.45);
 float3 color=a.rgb*(.72+n*.18);
 color+=lerp(float3(1,.018,.07),float3(1,.83,.79),hot)*energy*(.26+v*.8+current*.8);
 return float4(color*opacity,opacity);
}
float4 Aura(VO i):COLOR0
{
 float2 d=float2(.019,.019);
 float4 sum=tex2D(art,i.U+d)+tex2D(art,i.U-d)+tex2D(art,i.U+float2(d.x,-d.y))+tex2D(art,i.U+float2(-d.x,d.y));
 float4 far=tex2D(art,i.U+float2(.04,0))+tex2D(art,i.U-float2(.04,0))+tex2D(art,i.U+float2(0,.04))+tex2D(art,i.U-float2(0,.04));
 float energy=dot(sum.rgb,float3(.25,.30,.20))*.22+dot(far.rgb,float3(.25,.30,.20))*.09;
 float n=tex2D(grain,i.U*3-float2(0,clock*.15)).r;
 float amount=energy*i.C.r*(.45+i.C.g*.9+i.C.b*1.5)*signal.z*signal.w*Survive(i.U);
 return float4(float3(1,.017,.085)*amount*(.65+n),0);
}
float4 Ribbon(VO i):COLOR0
{
 float x=i.U.x,y=i.U.y*2-1;
 float n=tex2D(grain,float2(x*2.7-clock*.47+shape.x, i.U.y*1.3+clock*.06)).r;
 float v=tex2D(veins,float2(x*3.8-clock*.62+shape.x*.3, i.U.y*1.6)).r;
 float bend=sin(x*16-clock*6+shape.x)*.16*(1-shape.z*.65);
 float edge=saturate(1-abs(y-bend));
 float smoke=pow(edge,1.5)*saturate(n*.85+v*.75-.19);
 float strand=exp2(-abs(y-bend+(n-.5)*.48)*16)*(.35+v);
 float fold=exp2(-abs(y-bend-.40+(v-.5)*.30)*22)
           +exp2(-abs(y-bend+.38+(n-.5)*.32)*22);
 float taper=smoothstep(0,.07,x)*(1-smoothstep(.75,1,x));
 float flow=.45+.55*pow(saturate(.5+.5*sin(x*17-clock*10+n*4)),3);
 float strength=(smoke*.90+strand*(.55+shape.z*.85)+fold*.26)*taper*shape.y*signal.z;
 float hot=saturate((strand+fold*.22)*flow*(.4+signal.x*.45+signal.y*.9));
 float3 c=lerp(float3(.94,.018,.10),float3(1,.85,.79),hot);
 return float4(c*strength*(1+signal.y*.35),0);
}
float4 Heart(VO i):COLOR0
{
 float2 q=i.U*2-1;
 float n=tex2D(grain,i.U*2.2+float2(clock*.07,-clock*.18)).r;
 float v=tex2D(veins,i.U*3+float2(-clock*.06,clock*.22)).r;
 float radius=length(q*float2(1.10,.88));
 float field=exp2(-radius*5.8)*(.45+n*.6);
 float stem=exp2(-abs(q.x+sin(q.y*9+clock*3)*.09)*20)*exp2(-abs(q.y)*4.4);
 float lobe=exp2(-length((q-float2(-.10,-.08))*float2(1.4,1))*13)
           +exp2(-length((q-float2(.08,.05))*float2(1.4,1))*15);
 float fracture=pow(saturate(v*1.45-.3),3)*field*4;
 float pulse=.6+shape.x*.40+shape.y*.9+shape.z*1.4;
 float fade=(1-smoothstep(.60,1,radius))*signal.z*signal.w*(1-ceremony.x);
 float3 c=float3(1,.014,.055)*(field*1.2+stem*.25)*pulse;
 c+=float3(1,.40,.46)*(fracture*.55+stem*.2)*pulse;
 c+=float3(1,.65,.69)*lobe*(.20+shape.y*.3+shape.z*.55)*pulse;
 return float4(c*fade,0);
}
float4 Spark(VO i):COLOR0
{
 float2 q=abs(i.U*2-1);
 float glow=exp2(-q.x*7-q.y*5)*(1-smoothstep(.6,1,max(q.x,q.y)))*shape.x*signal.z;
 return float4(float3(1,.36,.38)*glow,0);
}
technique ScarletChoir
{
 pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Body(); }
 pass AuraPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Aura(); }
 pass RibbonPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Ribbon(); }
 pass HeartPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Heart(); }
 pass SparkPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Spark(); }
}
