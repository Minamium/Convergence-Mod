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
float4 Tear(VO i):COLOR0 {
 float x=i.u.x*shape.x,y=i.u.y*2-1;
 float noise=tex2D(cloud,float2(x*.008-clock*(signal.y>0?12:.12),clock*.03)).r;
 float warp=(noise-.5)*.17;
 float spine=exp2(-pow((y-warp)*shape.y*1.25,2));
 float halo=exp2(-pow(y*4.5,2))*.22;
 float jag=pow(saturate(noise),5)*exp2(-abs(y)*9)*.45;
 float head=saturate(i.u.x*60)*saturate((1-i.u.x)*60);
 float3 color=signal.y>0?float3(1,.025,.09):float3(1,1,1);
 float3 light=(spine+halo+jag)*color;
 if(signal.y>0) light+=float3(1,.78,.8)*spine*.48;
 return float4(light*head*signal.z,0);
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
 pass FlamePass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Flame(); }
}
