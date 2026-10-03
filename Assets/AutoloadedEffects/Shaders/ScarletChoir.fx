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
// The body's own attack (Heat, Ignite, Drain, Surge); zero = the accepted rest picture.
float4 attack;
// A participant's strike in the heart's core (see Heart); zero = today's heart.
float4 heart;
// The heart patch in body texture UV: centre (xy) and half size (zw), so the strike stays on the painted torso.
float4 heartArea;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.C=v.C; o.U=v.U; return o; }
// Reduced Effects (exposure .52): the attack's light is quieter (1 at full exposure, ~.34 reduced) while its
// timing and shape stay those of Normal; the body's accepted glow keeps the plain exposure.
float Calm(){return signal.w*signal.w*.9+.1;}
float Survive(float2 uv)
{
 float n=.50+.21*sin(uv.x*23+sin(uv.y*17)*2)+.17*cos(uv.y*31+uv.x*13-clock*.3);
 return ceremony.x<=0?1:smoothstep(ceremony.x*1.25-.16,ceremony.x*1.25-.06,n);
}
float4 Body(VO i):COLOR0
{
 float4 a=tex2D(art,i.U);
 float limb=i.C.r;
 // Vertex alpha is the blood front the heart sends down the striking arm (0 = none). The sent blood stays below .75
 // (ScarletChoirBlood); only the struck fingertips' ignition passes it and runs white-hot.
 float front=i.C.a;
 float opacity=a.a*signal.z*Survive(i.U)*lerp(1,limb,armsOnly);
 float n=tex2D(grain,i.U*5+float2(clock*.07,-clock*.13)).r;
 float v=tex2D(veins,i.U*3+float2(clock*.04,-clock*.10)).r;
 float bone=smoothstep(.12,.55,dot(a.rgb,float3(.30,.46,.24)));
 float current=pow(saturate(.5+.5*sin(i.U.y*50+n*5-clock*8)),5);
 float glow=saturate((front-.75)/.25);
 float flush=saturate(front*1.6)*(1-glow)*limb*bone*Calm()*(.7+.3*v);
 // After the flash the body swallows its light (Drain) before giving it back.
 float drained=1-attack.z*.25*Calm();
 float energy=((.38+i.C.g*1.35+i.C.b*2.3)*signal.w+front*1.4*Calm())*limb*bone*drained;
 float hot=saturate((v-.28)*2.7+current*.45+glow*.7*Calm());
 float3 color=a.rgb*(.72+n*.18);
 color+=lerp(float3(1,.018,.07),float3(1,.83,.79),hot)*energy*(.26+v*.8+current*.8);
 color*=1-flush*float3(.08,.55,.50);
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
// shape = (seed, opacity, filament, w): on a sleeve (filament 0) w > 0 carries the tear 0..1 = w - 1.
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
 // A torn sleeve parts down its middle over the forearm (the bone shows through). It only takes light away,
 // near the axis, so nothing is added past the body.
 float tear=(shape.w>0&&shape.z<.5)?(shape.w-1)*smoothstep(.22,.38,x)*(1-smoothstep(.50,.66,x)):0;
 float parting=tear*exp2(-pow((y-bend)/.12,2));
 smoke*=1-parting;
 strand*=1-parting*.8;
 float taper=smoothstep(0,.07,x)*(1-smoothstep(.75,1,x));
 float flow=.45+.55*pow(saturate(.5+.5*sin(x*17-clock*10+n*4)),3);
 float strength=(smoke*.90+strand*(.55+shape.z*.85)+fold*.26)*taper*shape.y*signal.z;
 float hot=saturate((strand+fold*.22)*flow*(.4+signal.x*.45+signal.y*.9));
 float3 c=lerp(float3(.94,.018,.10),float3(1,.85,.79),hot);
 return float4(c*strength*(1+signal.y*.35),0);
}
// The heart's glow at field coordinate p with a pulse and a beat (the lobes' discharge).
float3 HeartGlow(float2 p,float n,float v,float pulse,float beat)
{
 float inner=length(p*float2(1.10,.88));
 float field=exp2(-inner*5.8)*(.45+n*.6);
 float stem=exp2(-abs(p.x+sin(p.y*9+clock*3)*.09)*20)*exp2(-abs(p.y)*4.4);
 float lobe=exp2(-length((p-float2(-.10,-.08))*float2(1.4,1))*13)
           +exp2(-length((p-float2(.08,.05))*float2(1.4,1))*15);
 float fracture=pow(saturate(v*1.45-.3),3)*field*4;
 float3 c=float3(1,.014,.055)*(field*1.2+stem*.25)*pulse;
 c+=float3(1,.40,.46)*(fracture*.55+stem*.2)*pulse;
 c+=float3(1,.65,.69)*lobe*(.20+shape.y*.3+beat*.55)*pulse;
 return c;
}
// shape = (pulse, power, discharge, 0): the accepted heart. Only its core answers a participant's strike through
// `heart` = (weight, pulse on the attack clock, discharge, contraction): it draws in through the warning, swells with
// the slam (a pair of arms swells it more) and sinks as the body drains. That answer is masked by the painted torso
// (heartArea), so the rib gaps and everything past the body keep the accepted glow.
float4 Heart(VO i):COLOR0
{
 float2 q=i.U*2-1;
 float n=tex2D(grain,i.U*2.2+float2(clock*.07,-clock*.18)).r;
 float v=tex2D(veins,i.U*3+float2(-clock*.06,clock*.22)).r;
 float radius=length(q*float2(1.10,.88));
 float rest=.6+shape.x*.40+shape.y*.9+shape.z*1.4;
 float3 c=HeartGlow(q,n,v,rest,shape.z);
 // On the attack clock: the strike's ignition replaces the free beat, and the core sinks as the body drains.
 float core=heart.x*(1-smoothstep(.30,.55,radius))*smoothstep(.05,.35,tex2D(art,heartArea.xy+q*heartArea.zw).a);
 float struck=min(3.3,.6+heart.y*.40+shape.y*.9+heart.z*1.4)*(1-attack.z*.25*Calm());
 c+=(HeartGlow(q/(1-heart.w),n,v,struck,heart.z)-c)*core*Calm();
 float fade=(1-smoothstep(.60,1,radius))*signal.z*signal.w*(1-ceremony.x);
 return float4(c*fade,0);
}
// shape.x = opacity; vertex alpha carries a batched spark's own opacity (white = 1).
float4 Spark(VO i):COLOR0
{
 float2 q=abs(i.U*2-1);
 float glow=exp2(-q.x*7-q.y*5)*(1-smoothstep(.6,1,max(q.x,q.y)))*shape.x*i.C.a*signal.z;
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
