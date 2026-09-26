// Independently authored organism materials, not a flattened glow around a PNG.
matrix uWorldViewProjection;
sampler art:register(s0);sampler grain:register(s1);sampler veins:register(s2);
float clock,species,cut;float4 signal,shape;float2 ceremony;
struct VI { float4 P:POSITION0;float4 C:COLOR0;float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION;float4 C:COLOR0;float2 U:TEXCOORD0; };
VO VS(VI v){VO o=(VO)0;o.P=mul(v.P,uWorldViewProjection);o.C=v.C;o.U=v.U;return o;}
float Survive(float2 uv){float n=.5+.21*sin(uv.x*23+sin(uv.y*17)*2)+.17*cos(uv.y*31+uv.x*13-clock*.3);return ceremony.x<=0?1:smoothstep(ceremony.x*1.25-.16,ceremony.x*1.25-.06,n);}
float Mask(float2 uv){return cut<.5?1:species<.5?1-smoothstep(.42,.57,uv.y):smoothstep(.10,.23,abs(uv.x-.5));}
float3 Red(){return species<.5?float3(1,.065,.025):float3(.88,.012,.18);}
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
float4 Heart(VO i):COLOR0
{
 float2 q=i.U*2-1;float r=length(q);float angle=atan2(q.y,q.x);
 float n=tex2D(grain,i.U*2+float2(clock*.10,-clock*.22)).r;
 float v=tex2D(veins,i.U*3-float2(0,clock*.28)).r;
 float blaze=exp2(-r*5)*(.5+v*1.5);
 float spiral=pow(saturate(.5+.5*sin(angle*3-r*19+clock*5+n*5)),8)*exp2(-r*3);
 float tear=exp2(-abs(q.x+sin(q.y*8+clock)*.08)*23)*exp2(-abs(q.y)*3);
 float power=(.6+shape.x*.45+shape.y*.8+shape.z*1.5)*signal.w;
 float fade=(1-smoothstep(.62,1,r))*signal.z*(1-ceremony.x);
 float3 color=Red()*(blaze*1.6+spiral)*power+float3(1,.73,.61)*(tear*.5+pow(blaze*.7,3))*.7*power;
 return float4(color*fade,0);
}
float4 Spark(VO i):COLOR0{float2 q=abs(i.U*2-1);float a=exp2(-q.x*7-q.y*5)*(1-smoothstep(.6,1,max(q.x,q.y)))*shape.x*signal.z;return float4(float3(1,.45,.35)*a,0);}
technique ScarletApparitions {
 pass AutoloadPass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Body(); }
 pass AuraPass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Aura(); }
 pass RibbonPass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Ribbon(); }
 pass HeartPass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Heart(); }
 pass SparkPass { VertexShader=compile vs_3_0 VS();PixelShader=compile ps_3_0 Spark(); }
}
