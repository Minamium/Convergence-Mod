// Original violet sword-tear material. ScarletSorcery's incision/recoil/smoke
// choreography is the reference, not a beam/nozzle/portal implementation.
matrix uWorldViewProjection;
sampler cloud : register(s1);
sampler veins : register(s2);
float clock;
float4 phase; // ticks relative to Fire, live duration, warning progress, reduced
float4 shape; // stroke length/field radius, width/inner radius, seed/facing, route/wind
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o; o.P=mul(v.P,uWorldViewProjection); o.C=v.C; o.U=v.U; return o; }
float3 ink=float3(.019,.006,.041),violet=float3(.36,.045,.94),lavender=float3(.67,.30,1),ivory=float3(.94,.86,1);
float bell(float x,float radius) { float s=x/max(.1,radius);return exp2(-s*s*2.8); }
float noise(float2 uv) { return tex2D(cloud,uv).r; }
float aa(float d) { return max(.8,(abs(ddx(d))+abs(ddy(d)))*.62); }
float lifetime() { return 1-smoothstep(phase.y,phase.y+16,phase.x); }
float live() { return step(0,phase.x)*(1-step(phase.y,phase.x)); }
float contraction() { return 1-smoothstep(max(2,phase.y-4),phase.y,phase.x); }

// A sparse fracture instead of an opaque corridor. Boundary ink never extends
// beyond the gameplay footprint. Charge, release and recoil share this spine.
float4 Forecast(float x,float y,float radius,float seed,float extent)
{
 float pixel=aa(y),distance=abs(y),edge=bell(distance-(radius-pixel*1.3),pixel);
 float rise=smoothstep(0,.12,phase.z),charge=smoothstep(-13,0,phase.x);
 float flutter=(sin(x*.043-clock*23+seed)*.65+sin(x*.097+clock*39)*.3)*(1-phase.w*.75);
 float spine=bell(y-flutter,pixel*(.75+charge*.6));
 float thin=bell(y,pixel*3.7),under=bell(distance-(radius-pixel*2),pixel*2.2);
 float cell=floor(x/67),cx=frac(x/67+clock*.22)-.5;
 float glint=bell(cx,.037)*bell(y-sin(cell*7.79+seed)*radius*.78,pixel*1.7)*.60;
 float signal=(spine*(.65+charge*.60)+edge*(.27+charge*.25)+glint)*rise;
 float alpha=(thin*.54+under*.34)*rise;
 float3 color=ink*alpha+lerp(lavender,ivory,.38+charge*.43)*signal;
 return float4(color,alpha)*extent;
}

// A tapered moving blade sheet with a pointed head and much longer torn tail.
// Several unequal sheets pass along the incision; no uniform beam-body fill.
float3 Sheet(float u,float y,float radius,float seed,float lane)
{
 float t=phase.x;
 float head=-.09+t*.23-lane*.23;
 float behind=head-u;
 float packet=saturate(smoothstep(-.025,.045,behind)*(1-smoothstep(.10,.78,behind)));
 float n=noise(float2(u*7-clock*2.3+seed,y*.016-lane*.2));
 float grain=tex2D(veins,float2(u*13-clock*4.8+seed,y*.035+n*.2)).r;
 float flutter=(sin(u*41-clock*47+seed+lane)*.65+sin(u*97+clock*33)*.35)*(1-phase.w*.72);
 float curve=sin(u*3.14159)*(lane-1)*radius*.31+flutter*(1+radius*.025);
 float arrival=smoothstep(-.25,2.2,t),collapse=contraction();
 float width=radius*(.04+.81*pow(packet,.7))*arrival*collapse*(1-lane*.16);
 float core=bell(y-curve,max(aa(y),width*.15));
 float skin=bell(y-curve,max(.8,width))*(.25+n*.40+grain*.36);
 float tears=pow(saturate(grain*1.42-.34),3)*bell(y-curve,width*1.17)*.50;
 float tip=bell(behind,.045)*bell(y-curve,max(1,radius*.24))*arrival;
 return (violet*skin*.74+lavender*tears+ivory*(core*.85+tip*.65))*packet*collapse;
}

float4 Stroke(VO i):COLOR0
{
 float u=i.U.x,x=u*shape.x,y=(i.U.y*2-1)*shape.y,t=phase.x;
 float pixel=aa(y),mask=1-smoothstep(shape.y-pixel*.7,shape.y,abs(y));
 float extent=(.45+.55*saturate(x/8)*saturate((shape.x-x)/8))*mask;
 float seed=shape.z;
 float4 warning=Forecast(x,y,shape.y,seed,extent);
 float attack=smoothstep(-.15,1.4,t),hold=live(),tail=lifetime();
 float charge=smoothstep(-10,0,t)*(1-smoothstep(0,2.5,t));
 float n=noise(float2(x*.008-clock*2.8+seed,y*.027+clock*.12));
 float grain=tex2D(veins,float2(x*.013-clock*5.2+seed,y*.033+n*.2)).r;
 float envelope=contraction();
 float tremor=(sin(x*.047-clock*49+seed)+sin(x*.089+clock*37)*.4)*(1-phase.w*.75);
 float spine=bell(y-tremor,max(pixel,shape.y*(.02+.065*attack)*envelope));
 float edge=bell(abs(y)-(shape.y-pixel*1.4),pixel);
 // The full hit footprint remains readable even ahead of the moving bright
 // blade. Dim combed fibers + its rim, never a solid colored rectangle.
 float fibers=pow(saturate(grain*1.45-.38),3)*(.04+n*.13)*hold;
 // Stable opposite travelling strokes across neighbouring grid cuts. This is
 // material motion only: it never staggers or postpones their shared hit tick.
 float flowU=lerp(u,1-u,step(.5,frac(seed*.113)));
 float3 sheets=Sheet(flowU,y,shape.y,seed,0)+Sheet(flowU,y,shape.y,seed,1)*.67;
 sheets+=Sheet(flowU,y,shape.y,seed,2)*.43*(1-phase.w);
 float flare=exp2(-max(0,t)*.44)*attack*(1-phase.w*.45);
 float cell=floor(x/157),local=frac(x/157)-.5;
 float shard=(bell(local,.009)*bell(y,shape.y*.65)+bell(local,.21)*bell(y,pixel))*flare;
 shard*=pow(saturate(sin(cell*4.19+seed)),4)*.50;
 float vapor=smoothstep(2,7,t)*tail*pow(saturate(n*.72+grain*.5-.48),2)*bell(y,shape.y*.76)*(1-phase.w*.60);
 float3 light=sheets+(lavender*.55+ivory*.24)*spine*(charge*.24+attack*.38*envelope);
 light+=lavender*(fibers+edge*.43*hold)+ivory*shard+violet*vapor*.43;
 float shadow=(bell(y,shape.y*.72)*.13+edge*.29)*hold+vapor*.28;
 float4 slash=float4(light+ink*shadow,shadow)*extent;
 // Smoothly leave the forecast; its actual-width rim remains until End. Routes
 // deliberately use only the forecast; the projectile owns travelling damage.
 return lerp(lerp(warning,slash,attack),warning,shape.w)*i.C;
}

float4 Field(VO i):COLOR0
{
 float2 q=(i.U*2-1)*shape.x;
 float r=length(q),theta=atan2(q.y+step(r,.001)*.001,q.x),pixel=aa(r),t=phase.x;
 float mask=1-smoothstep(shape.x-pixel,shape.x,r);
 float inner=step(.5,shape.y),front=step(.5,abs(shape.z));
 mask*=lerp(1,smoothstep(shape.y,shape.y+pixel,r),inner);
 mask*=lerp(1,smoothstep(0,pixel,q.x*shape.z),front);
 float edge=bell(r-(shape.x-pixel*1.3),pixel)+inner*bell(r-(shape.y+pixel*1.3),pixel);
 edge+=front*bell(q.x-pixel*1.3*shape.z,pixel);
 float charge=smoothstep(-13,0,t),attack=smoothstep(-.15,1.4,t),hold=live();
 float n=noise(q*.006+float2(-clock*2.3,clock*.17));
 float grain=tex2D(veins,q*.017+float2(clock*1.2,-clock*2.9)).r;
 float pre=pow(saturate(grain*1.14-.16),9)*.18;
 float4 forecast=float4(ink*edge*.4+ivory*(edge*(.42+charge*.45)+pre),edge*.37)*smoothstep(0,.13,phase.z);
 // Broad, accelerating curved cuts sweep the disk. The exact disk/annulus
 // mask survives the bright blade, and its safe hole is never filled.
 float angle=theta*2.0-r*.004-t*(.22+.015*max(0,t))+n*.52;
 float crest=pow(saturate(.5+.5*sin(angle)),20);
 float wake=pow(saturate(.5+.5*sin(angle+.48)),5)*.34;
 float fringe=pow(saturate(grain*1.5-.35),3);
 float width=contraction(),envelope=attack*width;
 float spine=pow(crest,2.7)*envelope;
 float3 light=violet*(crest+wake)*envelope*(.30+n*.35)+ivory*spine*.84;
 light+=lavender*(fringe*.15+edge*.58)*hold;
 float vapor=smoothstep(2,8,t)*lifetime()*fringe*.19*(1-phase.w*.6);
 light+=violet*vapor;
 float4 cut=float4(light,edge*.24*hold+vapor*.22);
 return lerp(forecast,cut,attack)*mask*i.C;
}

float4 Wave(VO i):COLOR0
{
 float x=(i.U.x-.5)*shape.x,y=(i.U.y*2-1)*shape.y,t=phase.x;
 float v=y/shape.y,pixel=aa(x),mask=(1-smoothstep(.97,1,abs(v)))*(1-smoothstep(.94,1,abs(x)/(shape.x*.5)));
 float bend=(1-v*v)*shape.x*.39-shape.x*.08;
 float n=noise(float2(y*.014-clock*3.8,x*.021+shape.z));
 float grain=tex2D(veins,float2(y*.028-clock*6,x*.056+n*.26)).r;
 float flutter=(sin(y*.043-clock*43)+sin(y*.071+clock*29)*.35)*(1-phase.w*.7);
 float width=(7+shape.x*.19)*contraction()*(.55+.45*smoothstep(0,2,t));
 float distance=x-bend-flutter;
 float spine=bell(distance,max(pixel,width*.13)),blade=bell(distance,width)*(.34+n*.45);
 float trailing=exp2(-max(0,-distance)/max(2,width*2.5))*step(distance,0)*pow(saturate(grain*1.5-.25),3);
 float pulse=.70+.30*sin(y*.028-clock*22+n*3);
 float envelope=lifetime(),hot=live();
 float3 light=(ivory*spine*.92+violet*(blade+trailing*.58)+lavender*trailing*pulse*.40)*contraction();
 float smoke=smoothstep(phase.y-3,phase.y+5,t)*pow(saturate(n+grain-.8),2)*.25;
 return float4((light*hot+violet*smoke)*envelope,(blade*.13*hot+smoke*.28)*envelope)*mask*i.C;
}
technique SamuraiCut
{
 pass AutoloadPass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Stroke(); }
 pass FieldPass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Field(); }
 pass WavePass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Wave(); }
}
