// Independently authored Convergence Raid material suite. Noise assets remain
// owned by the separately installed Luminance dependency. No WoTM/WotG code.
float4x4 uWorldViewProjection;
float3 beamColor;
float4 signal; // charge, live energy, opacity, release
float4 shape; // length, half-width, stable seed, detail
float clock;
sampler cloudNoise : register(s1);
sampler flowNoise : register(s2);
sampler branchNoise : register(s3);
struct VI { float4 position:POSITION0; float4 color:COLOR0; float2 uv:TEXCOORD0; };
struct FI { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
FI VS(VI v) { FI o; o.position=mul(v.position,uWorldViewProjection); o.uv=v.uv; return o; }
float Hash(float2 p) { return frac(sin(dot(p,float2(41.13,289.17)))*25731.71); }
float3 Field(float x,float y) {
    float a=tex2D(cloudNoise,float2(x*.0024-clock*.37+shape.z,y*.53+clock*.041)).r;
    float b=tex2D(flowNoise,float2(x*.007-clock*.92+a*.25,y*1.9+a*.54-clock*.068)).r;
    float c=tex2D(branchNoise,float2(x*.0039-clock*.61,y*.86-a*.28+shape.z)).r;
    return float3(a,b,c);
}
float Edge(float y) { return 1-smoothstep(1-1.5/max(shape.y,2),1,abs(y)); }
float3 Pearl() { return lerp(beamColor,float3(1,.97,.94),.87); }
float4 Beam(FI i):COLOR0 {
    float x=i.uv.x*shape.x,y=i.uv.y*2-1;
    float3 n=Field(x,y);
    float warp=(n.x-.5)*.15*shape.w;
    float r=abs(y-warp*(1-abs(y)));
    float mantle=pow(saturate(1-r*r),.43);
    float broad=smoothstep(45,210,shape.y);
    float core=exp2(-pow(r,1.65)*(17-broad*7))*(.65+n.z*.7);
    float shear=pow(saturate(n.y*1.5),3);
    float facets=pow(saturate(n.z*1.75-n.x*.65),3.5);
    float body=mantle*(.16+n.x*.52+shear*.56);
    float3 light=beamColor*body + Pearl()*(core*.95+facets*mantle*.52);
    light+=Pearl()*signal.w*.42*exp2(-r*r*4);
    // Strong transverse contrast and local turbulent maxima, not a uniform slab.
    return float4(light*Edge(y)*signal.z,0);
}
float4 Forecast(FI i):COLOR0 {
    float x=i.uv.x*shape.x,y=i.uv.y*2-1;
    float3 n=Field(x*.55,y);
    float area=pow(saturate(1-y*y),.36);
    float spine=exp2(-y*y*shape.y*shape.y*.13);
    // Finely distributed in-flow motes; no chevrons, dashes or side rails.
    float2 cells=float2(x/29-clock*(2+signal.x*4),y*shape.y/15+shape.z);
    float2 id=floor(cells),p=frac(cells)-.5;
    float star=pow(saturate(1-length(p*float2(2.8,3.8))),6)*step(.78,Hash(id));
    float tension=pow(signal.x,3);
    float veins=pow(saturate(n.z*1.85-n.y*.45),5)*area;
    float3 forecastHighlight=lerp(beamColor,float3(1,1,1),.27);
    float3 light=beamColor*area*(.075+n.x*.09+tension*.045)
        +forecastHighlight*(spine*.38+star*.85*shape.w+veins*(.14+tension*.25));
    // Only the material's source rim eases in; the continuous centre signal
    // and the whole locked danger width remain present, including at tick zero.
    float birthRim=.45+.55*smoothstep(0,18,x);
    return float4(light*Edge(y)*birthRim*signal.z,area*.025*signal.z);
}
float4 Corona(FI i):COLOR0 {
    float x=i.uv.x*shape.x,y=i.uv.y*2-1;
    float3 n=Field(x,y*2);
    float veil=exp2(-y*y*8)*pow(saturate(1-y*y),2);
    float jets=pow(saturate(n.z*1.85-n.y*.55),5)*veil;
    return float4(beamColor*(veil*.16+jets*.18*shape.w)*signal.z,0);
}
float4 Mouth(FI i):COLOR0 {
    float2 p=(i.uv-.5)*2;
    float3 n=Field(p.x*200,p.y);
    float rear=exp2(-p.x*p.x*4);
    float throat=exp2(-p.y*p.y*(12+max(0,-p.x)*18))*rear;
    float slit=exp2(-p.x*p.x*650)*pow(saturate(1-p.y*p.y),3);
    float discharge=signal.y+signal.w*.8;
    float raw=pow(saturate(n.z*1.75-n.y*.4),4);
    float3 light=beamColor*throat*(.25+n.x*.45+raw*.8)
        +Pearl()*(throat*(.2+discharge*.8)+slit*(.12+signal.w*.9));
    return float4(light*signal.z,0);
}
float4 Orb(FI i):COLOR0 {
    float2 p=(i.uv-.5)*2;
    float r=length(p), theta=atan2(p.y,p.x);
    // The nucleus stays at its real radius. The decorative skirt has a lower exposure.
    float3 n=Field(theta*110+shape.z*73,r*2-clock*.07);
    float shell=1-smoothstep(.77,.96,r+(n.x-.5)*.065*shape.w);
    float z=sqrt(saturate(1-dot(p,p)));
    float core=exp2(-dot(p,p)*8);
    float fissures=pow(saturate(n.z*1.9-n.y*.4),4);
    float3 light=beamColor*shell*(.3+z*.4+fissures*.7)+Pearl()*(core*.95+fissures*z*.55);
    return float4(light*signal.z,0);
}
float4 Wake(FI i):COLOR0 {
    float x=i.uv.x,y=i.uv.y*2-1;
    float3 n=Field(x*shape.x,y*(1.5+x));
    float taper=pow(saturate(x),1.4);
    float body=exp2(-y*y*(7+8*x))*taper;
    float shreds=pow(saturate(n.z*1.8-n.y*.25),4);
    return float4((beamColor*(.25+shreds*.8)+Pearl()*shreds*.12)*body*signal.z,0);
}
float4 Pressure(FI i):COLOR0 {
    float2 p=(i.uv-.5)*2;
    float3 n=Field(p.x*240,p.y*1.7);
    float fold=exp2(-pow(p.x*(1+signal.x*2),2)*9);
    float edge=pow(saturate(1-p.y*p.y),.8)*pow(saturate(1-p.x*p.x),.8);
    float veins=pow(saturate(n.z*1.7-n.y*.45),4);
    return float4((beamColor*(.06+n.x*.07+fold*.08)+Pearl()*veins*(.09+signal.x*.22))*edge*signal.z,0);
}
float4 Rift(FI i):COLOR0 {
    float2 p=(i.uv-.5)*2;
    float3 n=Field(p.x*600,p.y*1.1);
    float taper=pow(saturate(1-p.x*p.x),1.8);
    float width=(.13+signal.x*.27)*taper;
    float bend=(n.x-.5)*.045*taper;
    float dist=abs(p.y-bend);
    float lip=exp2(-pow((dist-width)*19,2));
    float basin=1-smoothstep(width*.72,width+.012,dist);
    float accretion=exp2(-pow(dist-width,2)*9)*pow(saturate(n.z*1.8-n.y*.35),3);
    float ends=pow(saturate(1-p.x*p.x),.5);
    float3 light=(beamColor*(lip*.7+accretion*.5)+Pearl()*lip*.65)*ends;
    return float4(light*signal.z,basin*ends*.97*signal.z);
}
float4 Flare(FI i):COLOR0 {
    float2 p=(i.uv-.5)*2;
    float2 q=p*float2(1,1.4);
    float cloud=tex2D(cloudNoise,p*.47+float2(clock*.02,shape.z)).r;
    float radial=exp2(-dot(q,q)*12);
    float spike=exp2(-p.y*p.y*1000)*exp2(-abs(p.x)*5);
    float side=exp2(-p.x*p.x*650)*exp2(-abs(p.y)*9);
    return float4((beamColor*radial*.55+Pearl()*(radial+spike*.55+side*.3))*
        (1+cloud*.15)*signal.z,0);
}
technique RaidEnergy {
    pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Beam(); }
    pass ForecastPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Forecast(); }
    pass CoronaPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Corona(); }
    pass MouthPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Mouth(); }
    pass OrbPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Orb(); }
    pass WakePass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Wake(); }
    pass PressurePass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Pressure(); }
    pass RiftPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Rift(); }
    pass FlarePass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Flare(); }
}
