// Original spectral material. The quad/analytic mask is the authoritative
// volume, not the bloom: no moving noise can make a safe cell look dangerous.
matrix uWorldViewProjection;
sampler turbulence : register(s1);
sampler veins : register(s2);
float clock, mode, reduced;
float4 shape; // length/radius, half-width/inner-radius, facing, wind
float4 beat;  // warning progress, live, ticks since fire, remaining ticks
float4 tint;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.C=v.C; o.U=v.U; return o; }
float3 violet=float3(.46,.18,1.0);
float3 ice=float3(.88,.78,1.0);
float noise(float2 uv) { return tex2D(turbulence,uv).r; }
float filament(float x,float w) { return exp(-abs(x)*w); }
float4 Line(float2 uv)
{
 float x=uv.x*shape.x,y=(uv.y*2-1)*shape.y;
 float boundary=1-smoothstep(shape.y-1.7,shape.y,abs(y));
 float edge=filament(abs(y)-(shape.y-2.4),1.15);
 float grain=noise(float2(x*.005-clock*1.65,y*.020-clock*.24));
 float axis=filament(y,1.1);
 float spark=pow(noise(float2(x*.041+clock*.12,y*.07-clock*.2)),28)*10;
 float warnAlpha=saturate(axis*(.76+.24*beat.x)+edge*(.38+.26*beat.x)+spark*.65)*boundary;
 float3 warnColor=lerp(float3(.58,.35,.92),float3(1,.84,.68),beat.x*.78);
 // A narrow dark under-edge provides contrast against snow/daylight without
 // filling the warning corridor. All ink stays inside the damage footprint.
 float shadow=saturate(filament(y,.34)+filament(abs(y)-(shape.y-3),.40))*.78*boundary;
 float4 warnCol=float4(warnColor*warnAlpha+float3(.022,.008,.046)*shadow*(1-warnAlpha),max(warnAlpha*.9,shadow));
 float4 routeCol=warnCol*.74;
 // The live body is present at the very first damaging tick. Only its bright
 // core accelerates/grows, so the collision never outruns its visual footprint.
 float attack=1-exp(-(beat.z+1)*.85);
 float envelope=.72+.28*saturate(beat.w/5);
 float offset=(grain-.5)*shape.y*.36;
 float center=filament(y-offset,6/max(3,shape.y*attack));
 float fiber=pow(saturate(1-abs(sin(y*.27+grain*8-clock*4))),12);
 float traveling=pow(saturate(.5+.5*sin(x*.032-clock*22+y*.06)),4);
 float flow=(.19+grain*.22+fiber*.17+center*.62+traveling*.14)*(1-reduced*.2);
 float alpha=saturate(flow+edge*.35)*boundary*envelope;
 float3 col=violet*(.25+grain*.36)+ice*(center*.80+fiber*.32+traveling*.26);
 float4 liveCol=float4(col*alpha*1.38,alpha*.83);
 // FXC's conditional dispatch dropped the low-mode and warning branches on
 // D3D11. Keep both colors evaluated and select with numeric masks.
 return lerp(lerp(warnCol,liveCol,step(.5,beat.y)),routeCol,step(3.5,mode));
}
float4 Field(float2 uv)
{
 float2 q=(uv*2-1)*shape.x;
 float r=length(q),theta=atan2(q.y+step(r,.0001)*.0001,q.x);
 float inside=(1-smoothstep(shape.x-1.5,shape.x,r));
 if(shape.y>0) inside*=smoothstep(shape.y,shape.y+1.5,r);
 if(abs(shape.z)>.5) inside*=smoothstep(0,1.5,q.x*shape.z);
 float rim=filament(r-(shape.x-2),.5);
 float shadow=filament(r-(shape.x-3),.22);
 if(shape.y>0) rim=max(rim,filament(r-(shape.y+2),.5));
 if(shape.y>0) shadow=max(shadow,filament(r-(shape.y+3),.22));
 if(abs(shape.z)>.5) rim=max(rim,filament(q.x-2*shape.z,.5));
 if(abs(shape.z)>.5) shadow=max(shadow,filament(q.x-3*shape.z,.22));
 float n=noise(q*.009+float2(clock*.24,-clock*.31));
 float warnGrain=pow(noise(q*.035+clock*.04),25)*6;
 float warn=(rim*(.72+.24*beat.x)+warnGrain*.42)*inside;
 shadow*=inside*.80;
 float4 warnCol=float4(lerp(violet,ice,beat.x*.55)*warn+float3(.024,.008,.046)*shadow*(1-saturate(warn)),max(warn*.9,shadow));
 float swept=theta*3-r*.008+clock*(shape.w>.5?8:3);
 float arc=pow(saturate(.5+.5*sin(swept+n*3)),18);
 float slash=pow(saturate(.5+.5*sin((q.x+q.y*.68)*.055-clock*15+n*4.8)),12);
 float stream=lerp(slash,arc,saturate(shape.w));
 float energy=(.16+n*.18+stream*.72+rim*.26)*(1-reduced*.22);
 float alpha=saturate(energy)*inside;
 float4 liveCol=float4((violet*(.36+n*.32)+ice*stream)*alpha*1.3,alpha*.80);
 return lerp(warnCol,liveCol,step(.5,beat.y));
}
float4 Flame(float2 uv)
{
 float2 p=uv*2-1;
 float n=noise(float2(p.x*1.5,p.y*.8)+float2(clock*.10,-clock*1.10));
 float dend=tex2D(veins,float2(p.x*1.8+n*.26,p.y*.85-clock*.6)).r;
 float taper=saturate(1-abs(p.y));
 float bend=sin(p.y*4-clock*3)*.13*taper+(n-.5)*.24;
 float width=(.1+.65*taper)*(.64+beat.x*.25);
 float contour=saturate(1-abs(p.x-bend)/width);
 float flow=pow(contour,2)*(.24+pow(saturate(dend*1.4),3));
 float core=pow(contour,8)*pow(taper,2)*.7;
 float alpha=saturate(flow*.85+core)*smoothstep(0,.14,taper);
 float3 color=violet*(.6+n*.4)+ice*(core+pow(saturate(dend*1.24),6)*.60);
 return float4(color*alpha*tint.rgb*tint.a,alpha*.60*tint.a);
}
float4 Wisp(float2 uv)
{
 float2 p=uv*2-1;
 float r=length(p);
 float mask=1-smoothstep(.94,1,r);
 float n=noise(p*2+float2(-clock*1.9,clock*.6));
 float fl=tex2D(veins,p*1.6+float2(-clock*.7,n*.1)).r;
 float core=pow(saturate(1-r),3);
 float corona=pow(saturate(1-abs(r-(.62+n*.12))*7),3);
 float alpha=saturate(core*2+corona*.76+n*.18)*mask;
 float3 col=violet*(.55+fl*.4)+ice*(core*2+corona*.78);
 return float4(col*alpha,alpha*.86);
}
float4 PS(VO i):COLOR0
{
 float lineMask=1-step(.5,abs(mode));
 float fieldMask=1-step(.5,abs(mode-1));
 float wispMask=1-step(.5,abs(mode-2));
 float flameMask=1-step(.5,abs(mode-3));
 float routeMask=step(3.5,mode);
 float4 col=Line(i.U)*(lineMask+routeMask)+Field(i.U)*fieldMask+Wisp(i.U)*wispMask+Flame(i.U)*flameMask;
 return col*i.C;
}
technique SamuraiEnergy { pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); } }
