// Independently authored organism materials, not a flattened glow around a PNG.
matrix uWorldViewProjection;
sampler art:register(s0);sampler grain:register(s1);sampler veins:register(s2);
float clock,species,cut;float4 signal,shape;float2 ceremony;
// The body's own attack (ScarletBodyState), set every draw. The rest values reproduce the accepted picture exactly.
//   attack  = (Heat * Register, Ignite, Front (-1 none), Drain)       rest (0, 0, -1, 0)
//   attack2 = (Lean, PourLimit, Run (-1 none), Row)                   rest (0, 1, -1, 0)
//   attack3 = (Return, Swing, heart beat on the attack clock, 0)    rest (0, 0, shape.x, 0)
float4 attack,attack2,attack3;
// The heart patch in body texture UV: centre (xy), half size (zw). The attack changes the heart only over the art.
float4 heartArea;
struct VI { float4 P:POSITION0;float4 C:COLOR0;float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION;float4 C:COLOR0;float2 U:TEXCOORD0; };
VO VS(VI v){VO o=(VO)0;o.P=mul(v.P,uWorldViewProjection);o.C=v.C;o.U=v.U;return o;}
float Survive(float2 uv){float n=.5+.21*sin(uv.x*23+sin(uv.y*17)*2)+.17*cos(uv.y*31+uv.x*13-clock*.3);return ceremony.x<=0?1:smoothstep(ceremony.x*1.25-.16,ceremony.x*1.25-.06,n);}
float Mask(float2 uv){return cut<.5?1:species<.5?1-smoothstep(.42,.57,uv.y):smoothstep(.10,.23,abs(uv.x-.5));}
float3 Red(){return species<.5?float3(1,.065,.025):float3(.88,.012,.18);}
float Square(float x){return x*x;}
// Reduced Effects (exposure .48): the attack's light and darkening stay readable but quieter (1 at full exposure,
// ~.3 reduced; the body's existing glow keeps the plain exposure).
float Calm(){return signal.w*signal.w*.9+.1;}
// The art's own red paint (cloth, seams; 0 on near-black iron, on bone and on the dark cloth): only painted red ever
// emits, so the body never shows "black + a burning lip", the danger vocabulary of the ink.
float Paint(float blood){return smoothstep(.05,.45,blood);}
// The painted edge of cloth and burn holes as a soft gradient: alpha above the mean of four neighbours (+-.012 UV),
// so a hole's rim fades in over a few texels instead of reading as a one-pixel contour.
float Lip(float2 uv,float alpha)
{
 float2 d=float2(.012,0);
 float mean=(tex2D(art,uv+d.xy).a+tex2D(art,uv-d.xy).a+tex2D(art,uv+d.yx).a+tex2D(art,uv-d.yx).a)*.25;
 return saturate((alpha-mean)*2);
}
// Ember Crown: the warning heats the crown ring and the horn roots and preheats the announced side's flag top (both
// flags for the crossflow); the pour runs down the flags as light on the painted cloth (it brightens the cloth's own
// folds and embroidery and leaves the dark folds dark) in the move's own reach (the far flag a third later); the burnt
// painted edges glow and cool; after the flash the cloth swallows its light (Drain, the only darkening). Only the
// art's red paint emits. Capped below the ink's ignite colour.
float3 CrownAttack(float2 U,float3 color,float3 art,float bone,float blood,float paint,float v,float n,float lip)
{
 float cloth=smoothstep(.33,.45,U.y)*(1-bone),side=U.x<.5?-1:1;
 float ring=exp2(-length((U-float2(.5,.29))*float2(1,1.25))*9),top=1-smoothstep(.40,.53,U.y);
 float dim=attack.w*.25*cloth;
 float y=saturate((U.y-.33)/max(.05,attack2.y-.33))+(n-.5)*.05,f=attack.z-(side*attack2.x<0?.33:0);
 float band=attack.z<0?0:exp2(-Square((y-f)/.06))*cloth*paint*step(U.y,attack2.y+.04)*(1-smoothstep(.85,1,attack.z));
 float burnt=attack.z<0?0:saturate((f-y)/.2)*cloth*paint;
 float preheat=exp2(-Square((U.y-.40)/.05))*(1-bone)*paint*max(saturate(side*attack3.y),1-abs(attack3.y));
 float value=dot(art,float3(.5,.35,.15));
 float3 emit=Red()*(blood*(.45+v)*ring*1.4+preheat*(.3+v)*.7)*attack.x
  +float3(1,.42,.12)*bone*top*attack.x*v*v*.55
  +(art*float3(1.25,.8,.55)+float3(.5,.16,.04)*value)*band*(.45+v*.7)*(.1+.9*attack.y)
  +Red()*lip*(.35+v*.8)*burnt*attack3.x*(1-attack.w*.4)*.45;
 return color*(1-dim*Calm())+min(emit*Calm(),float3(1.1,.55,.35));
}
// Sable Mantle: the leading row's underlight gathers in its brightest seams as the warning tightens, heat runs along its
// bones from the root to the hook tips (there exactly on Fire, then with the whip), the bones blanch to bone colour on
// the strike, the painted cut edges of its cloth glow and cool, and the row swallows its light (Drain, the only
// darkening). Inside the painted alpha only; the dark cloth itself never emits.
float3 MantleAttack(float2 U,float3 color,float bone,float blood,float paint,float v,float n,float lip)
{
 float row=saturate(abs(attack2.w)*1.3);
 float lit=lerp(1,attack2.w>0?smoothstep(.44,.62,U.y):1-smoothstep(.40,.56,U.y),row)*smoothstep(.05,.25,abs(U.x-.5));
 float r=length((U-float2(.5,.39))*float2(1,1.1))/.55;
 float run=attack2.z<0?0:exp2(-Square((r-attack2.z+(n-.5)*.04)/.08));
 float seam=smoothstep(.55,1,blood);
 float dim=attack.w*.25*(1-bone)*lit;
 float3 emit=Red()*seam*lit*attack.x*(.4+v)*.65
  +lerp(Red(),float3(1,.82,.84),.45)*bone*lit*(run*.4+attack.y*.35)
  +Red()*lip*paint*(.35+v*.8)*(1-bone)*lit*attack3.x*(1-attack.w*.4)*.5;
 return color*(1-dim*Calm())+min(emit*Calm(),float3(1,.5,.5));
}
float4 Body(VO i):COLOR0
{
 float4 a=tex2D(art,i.U);float n=tex2D(grain,i.U*5+float2(clock*.06,-clock*.10)).r;
 float v=tex2D(veins,i.U*3+float2(clock*.08,-clock*.2)).r;
 float bone=smoothstep(.16,.57,dot(a.rgb,float3(.3,.45,.25)));
 float blood=saturate(a.r-max(a.g,a.b)*1.3)*3;
 float flow=pow(saturate(.5+.5*sin(i.U.y*48+i.U.x*14+n*4-clock*8)),5);
 float excite=(.23+signal.x*.75+signal.y*1.3)*signal.w;
 float3 color=a.rgb*(.70+n*.24)+Red()*(blood*(.3+v*1.3)+bone*flow*.85)*excite;
 color+=float3(1,.69,.63)*bone*pow(v,3)*excite*.7;
 float lip=Lip(i.U,a.a),paint=Paint(blood);
 color=species<.5?CrownAttack(i.U,color,a.rgb,bone,blood,paint,v,n,lip):MantleAttack(i.U,color,bone,blood,paint,v,n,lip);
 float alpha=a.a*signal.z*Survive(i.U)*Mask(i.U);
 return float4(color*alpha,alpha);
}
float4 Aura(VO i):COLOR0
{
 float2 d=float2(.016,.02);
 float4 a=tex2D(art,i.U+d)+tex2D(art,i.U-d)+tex2D(art,i.U+float2(d.x,-d.y))+tex2D(art,i.U+float2(-d.x,d.y));
 float flow=tex2D(grain,i.U*4-float2(0,clock*.14)).r;
 float energy=dot(a.rgb,float3(.3,.15,.12))*(.13+signal.x*.25+signal.y*.40)*signal.z*signal.w*Survive(i.U)*Mask(i.U);
 return float4(Red()*energy*(.45+flow),0);
}
float4 Ribbon(VO i):COLOR0
{
 float x=i.U.x,y=i.U.y*2-1;
 float n=tex2D(grain,float2(x*3.7-clock*.6+shape.x,i.U.y*1.3+clock*.06)).r;
 float v=tex2D(veins,float2(x*4.4-clock*.8+shape.x*.7,i.U.y*1.8)).r;
 float bend=sin(x*17-clock*8+shape.x)*.13;
 float wisps=pow(saturate(1-abs(y-bend)),2)*saturate(n*.7+v*.7-.15);
 float filament=exp2(-abs(y-bend+(n-.5)*.42)*18)*(.4+v);
 float fold=exp2(-abs(y-bend-.32+(v-.5)*.3)*23);
 float taper=smoothstep(0,.05,x)*(1-smoothstep(.75,1,x));
 float intensity=(wisps*(1-shape.z*.35)+filament*(.4+shape.z)+fold*.25)*taper*shape.y*signal.z*signal.w;
 float3 color=lerp(Red(),float3(1,.84,.76),saturate(filament*(.55+signal.x*.35+signal.y*.8)));
 return float4(color*intensity,0);
}
float Spiral(float phase){return pow(saturate(.5+.5*sin(phase)),8);}
// The art's alpha eroded by .012 UV (the least of five taps): the heart's patch sits on a skinned body, so the attack
// stays a safe margin inside the painted torso and off its see-through flecks.
float Opaque(float2 uv)
{
 float2 d=float2(.012,0);
 return min(tex2D(art,uv).a,min(min(tex2D(art,uv+d.xy).a,tex2D(art,uv-d.xy).a),min(tex2D(art,uv+d.yx).a,tex2D(art,uv-d.yx).a)));
}
// shape = (free pulse, charge, recoil, strike). The attack changes the heart only over the painted body (the patch
// also covers the holes and gaps around it, which keep the accepted free glow): there the beat follows the attack
// clock (attack3.z), announced heat winds the Mantle sigil's spiral tighter and the Mantle's strike swells the middle
// of its tear in its own wine red. The drained body dims the whole heart.
float4 Heart(VO i):COLOR0
{
 float2 q=i.U*2-1;float r=length(q);float angle=atan2(q.y,q.x);
 float n=tex2D(grain,i.U*2+float2(clock*.10,-clock*.22)).r;
 float v=tex2D(veins,i.U*3-float2(0,clock*.28)).r;
 float torso=smoothstep(.25,.65,Opaque(heartArea.xy+q*heartArea.zw));
 float twist=attack.x*species;
 float blaze=exp2(-r*5)*(.5+v*1.5);
 float phase=angle*3-r*19+clock*5+n*5;
 float spiral=lerp(Spiral(phase),Spiral(phase-r*9.5*twist+twist*2.5),torso)*exp2(-r*3);
 float tear=exp2(-abs(q.x+sin(q.y*8+clock)*.08)*23)*exp2(-abs(q.y)*3);
 float beat=lerp(shape.x,attack3.z,torso);
 float power=(.6+beat*.45+shape.y*.8+shape.z*1.5)*signal.w*(1-.4*attack.w*Calm());
 float fade=(1-smoothstep(.62,1,r))*signal.z*(1-ceremony.x);
 float3 color=Red()*(blaze*1.6+spiral)*power+float3(1,.73,.61)*(tear*.5+pow(blaze*.7,3))*.7*power;
 color+=min(Red()*tear*shape.w*species*torso*(1-smoothstep(.12,.40,r))*.8*power*Calm(),float3(.55,.06,.12));
 return float4(color*fade,0);
}
// One draw for every spark: brightness per vertex in 16 bits (a + g / 255), times shape.x.
float4 Spark(VO i):COLOR0{float2 q=abs(i.U*2-1);float a=exp2(-q.x*7-q.y*5)*(1-smoothstep(.6,1,max(q.x,q.y)))*shape.x*(i.C.a+i.C.g/255)*signal.z;return float4(float3(1,.45,.35)*a,0);}
// Crown drip of molten wine-dark blood: a bead sliding off a pendant on a thinning neck, never the ink's black and
// never an outlined hole; it cools to an ember and is gone. U.x across, U.y 0 = the pendant tip .. 1 = under the
// bead. Vertex colour: r life (an ember from .5, gone at 1), g length in half-widths / 32, b seed, a strength.
float4 Drip(VO i):COLOR0
{
 float life=i.C.r,k=max(2.2,i.C.g*32),x=i.U.x*2-1,up=(1-i.U.y)*k;
 float bead=saturate((1-length(float2(x,(up-1.1)*1.1)))*2.5);
 float neck=saturate((.16+.28*saturate((up-1.6)/max(.5,k-1.6))-abs(x))*6)*smoothstep(1.3,2,up);
 float body=max(bead,neck*.75);
 float ember=smoothstep(.45,.75,life),fade=1-smoothstep(.7,1,life);
 float a=body*(1-.75*ember)*fade*i.C.a*signal.z;
 float gloss=exp2(-(Square(x+.32)+Square(up-1.45))*14)*bead;
 float3 emit=(float3(1,.82,.84)*gloss*.03*(1-ember)+float3(1,.42,.14)*bead*ember*.22)*fade*i.C.a*signal.w*signal.z;
 return float4(float3(.36,.03,.06)*a+emit,a);
}
// Mantle wake: a wine-dark haze over the surface a hook blade swept through its true past poses, with sparse ember
// specks; never a thread, never ink black, no burning rim. U.x 0 = now .. 1 = 16 ticks ago, U.y 0 = the blade's heel ..
// 1 = its edge. Vertex colour: a opacity, r speck strength, g seed / 8. Premultiplied.
float4 Wisp(VO i):COLOR0
{
 float x=i.U.x,y=i.U.y,seed=i.C.g*8;
 float n=tex2D(grain,float2(x*2.1-clock*.35+seed*.37,y*.9+clock*.05)).r;
 float m=tex2D(grain,float2(x*7.3+seed*.61-clock*.2,y*2.3+clock*.11)).r;
 float across=smoothstep(.06,.42,y+(n-.5)*.2)*(1-smoothstep(.62,.94,y+(n-.5)*.15));
 float smoke=across*saturate(.15+n*1.25)*smoothstep(0,.06,x)*(1-smoothstep(.65,1,x));
 float a=smoke*i.C.a*signal.z;
 float specks=smoothstep(.66,.90,m)*across*(1-x)*i.C.r;
 return float4(float3(.22,.035,.06)*a+lerp(Red(),float3(1,.45,.2),.4)*specks*.3*signal.w*signal.z,a);
}
technique ScarletApparitions {
 pass AutoloadPass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Body(); }
 pass AuraPass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Aura(); }
 pass RibbonPass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Ribbon(); }
 pass HeartPass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Heart(); }
 pass SparkPass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Spark(); }
 pass DripPass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Drip(); }
 pass WispPass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Wisp(); }
}
