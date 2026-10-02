// Original Scarlet ink. Vespera writes each forecast as one thin blood-ink line over a
// soft footprint wash; the live stroke is dark ink with crimson lips and a flowing
// white-hot core; it dries into a narrow scar. Coordinates: u along the drawn quad
// (start cap .. end cap plus margin), v across it. Output is premultiplied alpha.
float4x4 uWorldViewProjection;
float4 signal; // forecast: draw-in, heat, opacity, reduced | live: ticks since fire, envelope, opacity, reduced | residue: ticks since end, fade, opacity, reduced
float4 shape;  // segment length px, radius px, seed, margin px
float4 flavor; // x fire (Ember Crown), y silk, z bone, w final
float clock;   // seconds
sampler flow : register(s1);
sampler grain : register(s2);
struct VI { float4 p:POSITION0; float2 u:TEXCOORD0; };
struct VO { float4 p:SV_POSITION; float2 u:TEXCOORD0; };
VO VS(VI v) { VO o; o.p=mul(v.p,uWorldViewProjection); o.u=v.u; return o; }

float2 Local(float2 u) { float e=shape.y+shape.w; return float2(u.x*(shape.x+2*e)-e,(u.y*2-1)*e); }
float Segment(float2 p) { return length(float2(p.x-clamp(p.x,0,shape.x),p.y)); }
float Pixel(float2 p) { return max(.6,abs(ddx(p.y))+abs(ddy(p.y))); }

float4 Forecast(VO i):COLOR0 {
 float2 p=Local(i.u); float d=Segment(p),r=shape.y,aa=Pixel(p);
 float draw=signal.x,heat=signal.y;
 // The pen runs from the start cap to the far cap; ink is laid behind it.
 float head=lerp(-r,shape.x+r,draw);
 float written=saturate((head-p.x)/28);
 float n=tex2D(grain,p*.0042+float2(shape.z,clock*.015)).r;
 // The footprint is a dark wash whose rim bleeds a little darker, never a hard outline.
 float inside=1-smoothstep(r-9*aa,r+1.5*aa,d);
 float bleed=exp2(-pow((r-d)/(5*aa+r*.08),2))*inside;
 // A shadow of the coming stroke: readable as darkness on the painting and as a faint
 // crimson glow on dark ground.
 float wash=inside*(.26+.14*heat)*(.74+.52*n)+bleed*(.16+.12*heat);
 float span=saturate((p.x+14)/22)*saturate((shape.x+14-p.x)/22);
 float w=1.3*aa;
 float hair=exp2(-p.y*p.y/(w*w))*span;
 float glow=exp2(-p.y*p.y/(30*aa*aa))*span*.22;
 float bead=exp2(-pow((p.x-head)/(5*aa),2)-pow(p.y/(2.6*aa),2))*step(draw,.999);
 float3 cold=float3(.75,.06,.07),hot=float3(1,.34,.2);
 float3 c=lerp(cold,hot,heat);
 float beat=1+heat*.35*sin(clock*42+shape.z*7);
 // In the last quarter beat the rim warms too, so the width reads before the strike.
 float3 emit=c*(hair*(.9+.9*heat)*beat+glow*(.45+heat)+bead*1.7)+(cold*.8+hot*heat*.4)*bleed*(.32+.5*heat)+cold*inside*(.1+.12*heat);
 float3 ink=float3(.1,.006,.016)*wash;
 return float4(ink+emit,saturate(wash))*written*signal.z;
}

float4 Live(VO i):COLOR0 {
 float2 p=Local(i.u); float d=Segment(p),r=shape.y,aa=Pixel(p),t=signal.x;
 // Snap open with a short overshoot, hold, then the envelope closes it.
 float open=saturate(t/3),over=1+.14*exp2(-pow((t-3.2)/2.4,2));
 float R0=max(.5,r*open*over);
 // The rim burns: its radius breathes a little along the stroke, inside the footprint.
 float rimN=tex2D(flow,float2(p.x*.0045-clock*2.8+shape.z,clock*.4)).r;
 float R=R0*(.9+.1*rimN);
 // The boundary feathers over the outer fifth of the radius instead of cutting like an outline.
 float edge=1-smoothstep(R*.78,R*1.04+aa,d);
 float q=saturate(d/R);
 // A black river of blood: long filaments stream along it, pulled thin across it.
 float flowA=tex2D(flow,float2(p.x*.0016-clock*1.9+shape.z,p.y/R*.55+shape.z*.7)).r;
 float flowB=tex2D(grain,float2(p.x*.0042-clock*3.4,p.y/R*1.3-shape.z)).r;
 float threads=pow(saturate(1-abs(flowA-.55)*6),3)+pow(saturate(1-abs(flowB-.5)*8),4)*.7;
 // Rim flicker moves along and across the edge like flame, never a row of evenly spaced beads.
 float lipN=tex2D(flow,float2(p.x*.0037-clock*2.1+shape.z,d*.021+clock*.35)).r;
 // The burning rim glows just inside the edge and bleeds outwards; it is not a drawn line.
 float lip=exp2(-pow((d-R*.86)/(R*.11+2*aa),2))*(.55+.95*lipN*lipN);
 float halo=exp2(-pow(max(0,d-R*.9)/(R*.24+4*aa),2))*(1-edge*.65)*(.4+.6*lipN);
 // The hot core is a braided vein that wanders with the flow, not a ruled line.
 float wander=(flowA-.5)*R*.55;
 float coreW=max(1.1*aa,R*(.05+.05*flowB));
 float core=exp2(-pow((p.y-wander)/coreW,2))*edge*saturate((p.x+R)/(R+1))*saturate((shape.x+R-p.x)/(R+1));
 float flash=exp2(-t*.28)*(1-signal.w*.6);
 // Ignition: the whole stroke blazes for a few ticks before the ink drinks the light.
 float ignite=exp2(-t*.21)*(1-signal.w*.5);
 // Ember Crown: flame tongues lift off the lips and lean along the flow.
 float tongue=flavor.x*pow(saturate(tex2D(flow,float2(p.x*.006-clock*2.2,d*.03-clock*1.6+shape.z)).r*1.8-.8),2)
  *exp2(-pow(max(0,d-R*.85)/(R*.32+7),2))*smoothstep(.6,1,q+(1-edge))*(1-signal.w*.6);
 float3 ink=float3(.05,.004,.009),scarlet=float3(1,.11,.065),hotc=float3(1,.7,.46),ember=float3(1,.4,.09);
 float body=edge*(.92-.18*threads);
 float3 emit=scarlet*(lip*1.15+halo*.3+threads*edge*(.42+.3*q)+edge*flash*.22)
  +lerp(scarlet,hotc,flash*.8+.25)*core*(.75+flash*1.4)+ember*tongue*1.3
  +lerp(scarlet,hotc,ignite*.85)*edge*ignite*(1.6-q*.7);
 return float4(ink*body+emit,body*.93)*signal.y*signal.z;
}

float4 Residue(VO i):COLOR0 {
 float2 p=Local(i.u); float d=Segment(p),r=shape.y,fade=signal.y;
 float q=saturate(d/max(r,1));
 float n=tex2D(grain,p*.0058+shape.z).r;
 // Ink dries from the rims inwards into a narrow uneven scar; embers die along it.
 float scar=(1-smoothstep(.1,.38+.3*n,q+(1-fade)*.3))*(.5+.5*n);
 float e=tex2D(flow,float2(p.x*.011+shape.z,p.y*.05-clock*.7)).r;
 float ember=flavor.x*pow(saturate(e*1.9-1.05),3)*(1-q)*(1-signal.w*.7);
 float rim=exp2(-pow((q-.36-.25*n)/.06,2))*.35;
 float alpha=scar*.62*fade;
 float3 emit=float3(1,.2,.06)*ember*1.3*fade+float3(.7,.05,.05)*rim*fade*fade;
 return float4(float3(.04,.003,.007)*alpha+emit,alpha)*signal.z;
}

technique ScarletInk {
 pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Live(); }
 pass ForecastPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Forecast(); }
 pass ResiduePass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Residue(); }
}
