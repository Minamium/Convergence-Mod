// Original Convergence material. Recorded Nameless triplet informs the motion
// and contrast, not copied shader equations/art. Lattice keeps RaidEnergy.
float4x4 uWorldViewProjection;
float3 beamColor;
float4 signal; // charge, live, opacity, release
float4 shape; // current length, current half-width, stable seed, detail
float4 ceremony; // ticks since fire/end, optional bell throat length/full width
float clock;
sampler cloudNoise : register(s1);
sampler flowNoise : register(s2);
sampler branchNoise : register(s3);
struct VI { float4 position:POSITION0; float4 color:COLOR0; float2 uv:TEXCOORD0; };
struct FI { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
FI VS(VI v) { FI o; o.position=mul(v.position,uWorldViewProjection); o.uv=v.uv; return o; }
float Ease(float t) { t=saturate(t); return t*t*(3-2*t); }
float3 Hot() { return lerp(beamColor,float3(1,.96,1),.93); }
float3 Hue() { return lerp(beamColor,float3(.58,.10,.84),.12); }
float Closure() { return Ease(ceremony.y/9); }
float TailFade() { return 1-Ease(ceremony.y/13); }
float WarningDip() { return 1-.84*Ease((ceremony.x+8)/7); }
float3 Turbulence(float x,float y) {
    // World-length coordinates avoid texture stretch as the pilot extends.
    float a=tex2D(cloudNoise,float2(x*.0028-clock*3.9+shape.z,y*.78+shape.z*.17)).r;
    float b=tex2D(flowNoise,float2(x*.0053-clock*6.8,y*1.9+a*.24)).r;
    float c=tex2D(branchNoise,float2(x*.0036-clock*2.7,y*1.12-a*.34)).r;
    return float3(a,b,c);
}
float4 Forecast(FI i):COLOR0 {
    float x=i.uv.x*shape.x,y=i.uv.y*2-1;
    float3 n=Turbulence(x,y);
    float breathe=.88+.12*sin(clock*3.1+shape.z);
    float profile=pow(saturate(1-y*y),2.4);
    float center=exp2(-y*y*15);
    float thread=exp2(-pow(y*shape.y,2)*1.2);
    float source=exp2(-x/90);
    // A translucent, soft-ended veil, not a uniform slab or bright edge rails.
    float3 light=Hue()*profile*(.13+signal.x*.16)*breathe
        +Hot()*(center*.022+thread*.15+profile*source*.16);
    float cap=Ease(x/9)*Ease((shape.x-x)/30);
    return float4(light*cap*WarningDip()*signal.z,0);
}
float4 Jet(FI i):COLOR0 {
    float x=i.uv.x*shape.x;
    float closure=Closure();
    // Width contracts only AFTER the authoritative damage window. During live
    // frames the current geometry supplied by the adapter remains authoritative.
    float taper=lerp(1,.012,closure);
    float footprintY=(i.uv.y*2-1)/max(taper,.012);
    float bell=ceremony.z>0 ? lerp(clamp(ceremony.w/max(shape.y*2,1),.16,1),1,
        pow(saturate(x/max(ceremony.z,1)),1.65)) : 1;
    float y=footprintY/bell;
    float3 n=Turbulence(x,y);
    float bend=(n.x-.5)*.21*(1-saturate(abs(y)))*shape.w;
    float r=abs(y-bend);
    float coreWidth=.18+.18*n.x+.08*n.z;
    float white=1-smoothstep(coreWidth,coreWidth+.085,r);
    float fold=sin(y*11+(n.x-.5)*5+n.y*2);
    float seam=pow(saturate(1-abs(fold)*1.9),3);
    float wound=smoothstep(.42,.78,n.y+.19*seam);
    float colored=(1-smoothstep(.45+n.x*.13,.93,r));
    float rim=1-smoothstep(.84,.995,abs(footprintY));
    float fringe=pow(saturate(n.z*1.6-n.y*.27),3)*pow(saturate(1-r*r),.8);
    // Opaque-dark violet channels and a hot white spine: unlike additive-only
    // fog, this keeps its contrast over lit terrain and other effects.
    float3 dark=Hue()*.055;
    float3 fire=Hue()*(.6+fringe*.85);
    float3 base=lerp(fire,dark,wound*(.65+.25*seam));
    float3 light=base*colored+Hot()*white*(1.08+.19*n.z)
        +Hue()*fringe*.31+Hot()*fringe*white*.11;
    // A low outer carrier still identifies the full live footprint. Irregular
    // dark folds are material, never openings in its collision corridor.
    light+=Hue()*.065*rim;
    float source=Ease(x/4), end=Ease((shape.x-x)/(5+n.x*7));
    float mask=rim*source*end*TailFade()*signal.z;
    // The bell's unused corners retain only a faint luminous hazard carrier,
    // never an opaque rectangular socket around the narrow throat.
    float alpha=(ceremony.z>0 ? .82*colored : .68+.14*colored)*mask;
    return float4(light*mask,alpha);
}
float4 Corona(FI i):COLOR0 {
    float x=i.uv.x*shape.x,y=i.uv.y*2-1;
    float3 n=Turbulence(x,y);
    float skin=pow(saturate(1-y*y),4)*pow(saturate(n.z*1.5-n.y*.3),3);
    float needle=exp2(-y*y*65)*Closure();
    float fade=TailFade()*signal.z;
    return float4((Hue()*skin*.22+Hot()*needle*.13)*fade,0);
}
float4 Mouth(FI i):COLOR0 {
    float2 p=(i.uv-.5)*2;
    float3 n=Turbulence(p.x*180,p.y);
    float committed=signal.y;
    float pressure=1-committed;
    float slit=exp2(-p.x*p.x*130)*exp2(-p.y*p.y*(2+pressure*8));
    float throat=exp2(-p.x*p.x*7-p.y*p.y*16);
    float tear=exp2(-p.x*p.x*22)*pow(saturate(1-p.y*p.y),2)*n.z;
    float flare=signal.w;
    float3 light=Hot()*(slit*(.16+committed*.65+flare*.7)+throat*(.2+flare*.5))
        +Hue()*tear*(.2+committed*.35);
    return float4(light*signal.z*TailFade()*(pressure>0?WarningDip():1),0);
}
technique PortalBeam {
    pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Jet(); }
    pass PortalForecastPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Forecast(); }
    pass PortalCoronaPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Corona(); }
    pass PortalMouthPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Mouth(); }
}
