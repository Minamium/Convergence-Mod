// Original Scarlet ritual ink, etched seals and white-to-crimson space cuts.
float4x4 uWorldViewProjection;
float4 signal; // charge, live/release, opacity, reduced
float4 shape;  // world length, half-width, stable seed, black-ink mode
float3 hue;
float clock;
sampler cloud : register(s1);
sampler veins : register(s2);
struct VI { float4 p:POSITION0; float2 u:TEXCOORD0; };
struct VO { float4 p:SV_POSITION; float2 u:TEXCOORD0; };
VO VS(VI v) { VO o; o.p=mul(v.p,uWorldViewProjection); o.u=v.u; return o; }
float Ring(float r,float at,float width) { return exp2(-pow((r-at)/width,2)); }
float4 Seal(VO i):COLOR0 {
 float2 p=i.u*2-1; float r=length(p),a=atan2(p.y,p.x);
 float spin=clock*.16+shape.z;
 float n=tex2D(cloud,p*1.4+float2(clock*.014,0)).r;
 float rims=Ring(r,.87,.006)+Ring(r,.75,.004)*.6+Ring(r,.59,.007)*.8;
 float teeth=pow(saturate(cos((a+spin)*24)),18)*Ring(r,.81,.024);
 float script=step(.43,tex2D(veins,float2(a*3.4+spin,r*23)).r)*Ring(r,.67,.025);
 float spokes=pow(saturate(cos((a-spin)*6)),42)*smoothstep(.27,.37,r)*(1-smoothstep(.58,.62,r));
 float polygon=Ring(r*(.91+.09*cos((a-spin)*6)),.47,.006);
 float etch=rims+teeth*.75+script*.7+spokes*.75+polygon;
 float open=smoothstep(.04,.35,signal.x); float ink=saturate((.9-r)*4)*(1-n*.36);
 float3 color=lerp(hue,float3(.32,.22,.29),shape.w);
 float strength=etch*(.5+signal.x*.65)+Ring(r,.87,.035)*(.08+signal.y*.18);
 // Black seals occlude the scene, while thin oxblood engraving remains readable.
 float alpha=shape.w*ink*.74;
 return float4(color*strength*(.8+n*.2),alpha)*signal.z*open;
}
float4 TearForecast(VO i):COLOR0 {
 float x=i.u.x*shape.x,y=(i.u.y*2-1)*(shape.y+72);
 float aa=max(.85,(abs(ddx(y))+abs(ddy(y)))*.6);
 float h=y/aa,b=(abs(y)-shape.y)/aa,g=y/(aa*2);
 float hair=exp2(-h*h),border=exp2(-b*b)*.13;
 float glint=pow(saturate(sin(x*.028+shape.z)),28)*exp2(-g*g)*.6;
 float edge=saturate(x/10)*saturate((shape.x-x)/10);
 return float4(float3(.9,.83,.9)*(hair+border+glint)*edge*signal.z,0);
}
float4 Tear(VO i):COLOR0 {
 float x=i.u.x*shape.x,y=(i.u.y*2-1)*(shape.y+72),t=signal.y;
 float seed=shape.z*1.718,edge=saturate(x/10)*saturate((shape.x-x)/10);
 float n=tex2D(cloud,float2(x*.006-clock*1.8+seed,y*.023-clock*.22)).r;
 float fine=tex2D(veins,float2(x*.011-clock*3.3,y*.046+seed)).r;
 // Continuous, sub-frame micro-amplitude modulation, not per-tick noise.
 float flutter=(sin(x*.031-clock*53+seed)*.78+sin(x*.079+clock*37)*.32)*(1-signal.w*.7);
 float arrive=smoothstep(0,2,t),collapse=1-smoothstep(shape.w-8,shape.w,t);
 float radius=shape.y*arrive*collapse;
 float tip=arrive*shape.x;
 float travel=1-smoothstep(tip-8,tip+1,x);
 float sy=(y-flutter*collapse)/max(.4,radius*.24),by=y/max(.35,radius*.9),gy=y/max(1,radius*2.7);
 float spine=exp2(-sy*sy);
 float blade=exp2(-by*by)*(.7+n*.3);
 float glow=exp2(-gy*gy)*.22;
 // A quick stretched slash and a thin recoil afterimage, not a filled beam.
 float flare=exp2(-max(0,t-2)*.24)*arrive*(1-signal.w*.55);
 float ry=(y-flutter*3-(n-.5)*12)/1.1;
 float streak=exp2(-ry*ry)*fine*flare*.32;
 float cell=floor(x/138),cx=frac(x/138)-.5;
 float star=exp2(-abs(cx)*70-abs(y)/17)+exp2(-abs(cx)*12-abs(y)/1.0);
 star*=pow(saturate(sin(cell*4.19+seed)),4)*flare*.8;
 float3 light=(float3(1,.015,.065)*(blade+glow)+float3(1,.82,.84)*spine)*arrive*collapse;
 light+=(float3(1,.12,.19)*streak+float3(1,.7,.75)*star)*collapse;
 // Faint smoke blooms after the passing blade; no white core survives End.
 float tail=smoothstep(2,10,t)*(1-smoothstep(shape.w,shape.w+20,t));
 float vy=(y-flutter*5)/(18+t*1.2);
 float veil=pow(saturate(n*.65+fine*.55-.48),2)*exp2(-vy*vy)*tail*.22*(1-signal.w*.65);
 return float4(light*travel*edge*signal.z+float3(.13,.025,.04)*veil,veil*.48)*signal.z;
}
float4 Vapor(VO i):COLOR0 {
 float2 p=i.u*2-1;
 float n=tex2D(cloud,float2(i.u.x*2.1+clock*.9,i.u.y*1.3-clock*.16+shape.z)).r;
 float f=tex2D(veins,float2(i.u.x*3+clock*1.4,i.u.y*2+n*.4)).r;
 float fog=pow(saturate(n*.6+f*.6-.48),2)*exp2(-dot(p,p)*3.5)*signal.z*(1-signal.w*.65);
 return float4(float3(.13,.025,.04)*fog,fog*.65);
}
float4 Flame(VO i):COLOR0 {
 float2 uv=i.u;
 float n=tex2D(cloud,float2(uv.x*6-clock*7,uv.y*2)).r;
 float v=tex2D(veins,float2(uv.x*11-clock*12,uv.y*3+n*.35)).r;
 float profile=pow(saturate(1-abs(uv.y*2-1)),.55);
 float body=saturate(profile*1.8-(n*.3+v*.3)) * saturate(uv.x*18)*saturate((1-uv.x)*18);
 float lip=pow(v,4)*body*.5;
 return float4(float3(.045,.005,.07)*body+float3(.46,.28,.40)*lip,body*.96)*signal.z;
}
technique ScarletSorcery {
 pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Seal(); }
 pass TearPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Tear(); }
 pass TearForecastPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 TearForecast(); }
 pass VaporPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Vapor(); }
 pass FlamePass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Flame(); }
}
