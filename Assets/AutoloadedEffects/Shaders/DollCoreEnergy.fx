// Original contained violet volume for the exposed Doll Core. Luminance owns
// the three sampled noise textures and the compiled effect lifetime.
float4x4 uWorldViewProjection;
float clock;
float4 signal; // opacity, pressure/intensity, reduced effects, bore opening
float2 boreAxis;
float twinBore;

sampler cloudNoise : register(s1);
sampler flowNoise : register(s2);
sampler branchNoise : register(s3);

struct VI { float4 position:POSITION0; float4 color:COLOR0; float2 uv:TEXCOORD0; };
struct FI { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
FI VS(VI v) { FI o; o.position=mul(v.position,uWorldViewProjection); o.uv=v.uv; return o; }

float4 Volume(FI i):COLOR0
{
    float2 p=i.uv*2-1;
    float r=length(p);
    float z=sqrt(saturate(1-r*r));
    float motion=lerp(1,.38,signal.z);
    float t=clock*motion;
    float2 q=p+float2(.18*sin(t*.48),.12*cos(t*.57));
    float a=tex2D(cloudNoise,q*.59+float2(t*.093,-t*.061)).r;
    float b=tex2D(flowNoise,q*1.64+float2(-t*.17,t*.12)+a*.21).r;
    float c=tex2D(branchNoise,q*1.21+float2(t*.12,t*.084)-b*.17).r;

    // Projected spherical light, with dark moving cavities beneath filamentary
    // light. The edge stays soft but the silhouette remains at the real radius.
    float contour=r+(a-.5)*.034+(b-.5)*.022;
    float coverage=1-smoothstep(.955,1.014,contour);
    float pockets=smoothstep(.34,.70,a*.61+b*.39)*smoothstep(.21,.82,c);
    float hollow=1-.78*pockets;
    float path=abs(sin(atan2(q.y,q.x)*3.4+r*15.5+t*1.3+(a-b)*6.1));
    float filament=pow(saturate(1-path),13)*(1-pockets*.64);
    filament+=pow(saturate(c*1.62-b*.68),7)*.48;
    float core=exp2(-r*r*7.5)*(.48+.5*b);
    float glint=pow(saturate(dot(normalize(float3(p,z+.001)),normalize(float3(-.39,-.51,.77)))),7);
    float lip=pow(saturate(1-z),2)*(.22+.15*a);

    // The muzzle opens in the sphere surface on the supplied weapon axis.
    // Its depth is energy shadow and a directed source, not a metal socket.
    float along=dot(p,boreAxis);
    float across=dot(p,float2(-boreAxis.y,boreAxis.x));
    float throatDistance=length(float2((along-.47)*5.3,across*4.1));
    if (twinBore>.5) throatDistance=min(throatDistance,length(float2((along+.47)*5.3,across*4.1)));
    float mouth=1-smoothstep(.56,1.08,throatDistance);
    float mouthLip=exp2(-pow((throatDistance-1.03)*4.1,2))
        *(.18+.82*smoothstep(.39,.83,twinBore>.5?abs(along):along))*(.42+.58*c);
    float vent=mouth*signal.w;
    float beamward=exp2(-pow(across*7.5,2))*smoothstep(.38,.92,along)*signal.w;

    float3 dark=float3(.035,.010,.092);
    float3 violet=float3(.40,.12,.78);
    float3 pearl=float3(.91,.67,1);
    float3 color=dark+violet*(.19+z*.28+core*.33)*hollow;
    color+=pearl*(filament*(.24+z*.35)+core*.19+glint*.13+lip*.17);
    color*=.67+.33*saturate(dot(float3(p,z),normalize(float3(-.43,-.56,.72))));
    color=lerp(color,float3(.012,.002,.033),vent*.94);
    color+=violet*vent*.07+pearl*(mouthLip*signal.w*.28+beamward*.25);
    color*=1+signal.y*.30;
    float alpha=coverage*signal.x*(.83+.13*z);
    return float4(color*alpha,alpha);
}

float4 Corona(FI i):COLOR0
{
    float2 p=(i.uv*2-1)*1.36;
    float r=length(p);
    float t=clock*lerp(1,.34,signal.z);
    float a=tex2D(cloudNoise,p*.72+float2(t*.046,-t*.067)).r;
    float b=tex2D(branchNoise,p*1.7+float2(-t*.105,t*.083)).r;
    float contour=r+(a-.5)*.085;
    float feather=(1-smoothstep(.80,1.28,contour))*smoothstep(.71,.95,contour);
    float wisps=pow(saturate(b*1.5-a*.55),4);
    float alpha=feather*(.065+wisps*.12)*(1+signal.y*.3)*lerp(1,.43,signal.z)*signal.x;
    float3 color=lerp(float3(.22,.04,.52),float3(.66,.34,.91),wisps);
    return float4(color*alpha,alpha);
}

technique DollCoreEnergy
{
    pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Volume(); }
    pass CoronaPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Corona(); }
}
