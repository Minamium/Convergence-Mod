matrix uWorldViewProjection;
sampler grain:register(s0);sampler veins:register(s1);
float clock;float4 signal,shape;float2 ceremony;
struct VI{float4 P:POSITION0;float4 C:COLOR0;float2 U:TEXCOORD0;};
struct VO{float4 P:SV_POSITION;float4 C:COLOR0;float2 U:TEXCOORD0;};
VO VS(VI v){VO o=(VO)0;o.P=mul(v.P,uWorldViewProjection);o.C=v.C;o.U=v.U;return o;}
float4 Shroud(VO i):COLOR0
{
 float2 q=i.U*2-1;float r=length(q*float2(1,.87));
 float n=tex2D(grain,i.U*2.8+float2(sin(clock*.3)*.1,-clock*.06)).r;
 float v=tex2D(veins,i.U*3+float2(clock*.035,clock*.06)).r;
 float shape=(1-smoothstep(.40,.95,r+n*.15))*smoothstep(.09,.35,r);
 float a=shape*(.22+n*.36+v*.14)*signal.z*(1-ceremony.x);
 return float4(float3(.035,.002,.017)*a,a*.72);
}
float4 Nucleus(VO i):COLOR0
{
 float2 q=i.U*2-1;float r=length(q*float2(1,.89));float angle=atan2(q.y,q.x);
 float n=tex2D(grain,i.U*3+float2(clock*.12,-clock*.14)).r;
 float v=tex2D(veins,i.U*3.5+float2(-clock*.09,clock*.18)).r;
 float warp=sin(angle*5+clock*2+n*5)*.035;
 float sphere=1-smoothstep(.38+warp,.50+warp,r);
 float edge=exp2(-abs(r-.425-warp)*50);
 float filigree=pow(saturate(v*1.6-.35),3)*sphere;
 float sink=exp2(-length((q-float2(-.03,-.08))*float2(1.1,1))*12);
 float tendril=pow(saturate(.5+.5*sin(angle*7-r*31+clock*6+n*3)),13)*sphere;
 float halo=exp2(-r*4.5)*(.4+n*.6);
 float p=.6+shape.x*.32+signal.x*.75+signal.y*1.4;
 float fade=(1-smoothstep(.72,1,r))*signal.z*(1-ceremony.x);
 float3 color=float3(1,.008,.05)*(halo*.32+edge*.70+filigree*.8)*p;
 color+=float3(1,.50,.53)*(sink*.8+tendril*.5)*p;
 color+=float3(.14,.001,.012)*sphere;
 return float4(color*fade*signal.w,sphere*.52*fade);
}
float4 Artery(VO i):COLOR0
{
 float x=i.U.x,y=i.U.y*2-1;
 float n=tex2D(grain,float2(x*3-clock*.3+shape.x,i.U.y*1.8)).r;
 float v=tex2D(veins,float2(x*5-clock*.65+shape.x*.4,i.U.y*1.6)).r;
 float offset=sin(x*16-clock*6+shape.x)*.15;
 float rim=pow(saturate(1-abs(y-offset)),1.8);
 float core=exp2(-abs(y-offset+(n-.5)*.3)*19);
 float pulse=.38+.62*pow(saturate(.5+.5*sin(x*21-clock*12+n*3)),4);
 float flow=(rim*(.3+n*.6+v*.5)+core*.5)*(.60+signal.x*.75+signal.y*1.1);
 float alpha=smoothstep(0,.035,x)*(1-smoothstep(.90,1,x))*signal.z*(1-ceremony.x)*shape.y;
 float bone=pow(saturate(1-abs(y)*1.6),.65)*shape.z;
 float3 color=lerp(float3(.84,.006,.08)*flow,float3(1,.68,.66)*(core*.5+bone*.7),saturate(bone*.75+core*pulse*.45));
 return float4(color*alpha*signal.w,bone*.14*alpha);
}
float4 Spark(VO i):COLOR0{float2 q=abs(i.U*2-1);float a=exp2(-q.x*8-q.y*6)*(1-smoothstep(.6,1,max(q.x,q.y)))*shape.x*signal.z*(1-ceremony.x);return float4(float3(1,.42,.43)*a,0);}
technique ScarletAvatarAnatomy{
 pass AutoloadPass{VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Nucleus();}
 pass NucleusPass{VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Nucleus();}
 pass ShroudPass{VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Shroud();}
 pass ArteryPass{VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Artery();}
 pass SparkPass{VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Spark();}
}
