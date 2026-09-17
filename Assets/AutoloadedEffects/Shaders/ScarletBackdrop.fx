// Original painted-scene treatment. All distortion is background-only.
matrix uWorldViewProjection;
sampler painting : register(s0);
sampler curlTexture : register(s1);
sampler grainTexture : register(s2);
float4 crop, phaseWeights, signal;
float clock;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.C=v.C; o.U=v.U; return o; }
float4 PS(VO i):COLOR0
{
 float2 uv=i.U;
 float n=tex2D(curlTexture,uv*2+float2(clock*.012,-clock*.009)).r;
 float g=tex2D(grainTexture,uv*3+float2(-clock*.009,clock*.007)).r;
 float movement=1-signal.w;
 float2 warp=float2((n-.5)*.004*(phaseWeights.x+phaseWeights.z),sin(uv.x*18+clock*.8)*.0015*phaseWeights.y)*movement;
 float3 rgb=tex2D(painting,crop.xy+(uv+warp)*crop.zw).rgb;
 float l=dot(rgb,float3(.2126,.7152,.0722));
 float3 crown=rgb*float3(1.02,.91,.88);
 float3 velvet=lerp(l.xxx,rgb,.29)*float3(.69,.61,.88);
 float3 thorns=lerp(l.xxx,rgb,.38)*float3(.68,.42,.75);
 float3 finale=lerp(l.xxx,rgb,.47)*float3(.94,.49,.68);
 rgb=crown*phaseWeights.x+velvet*phaseWeights.y+thorns*phaseWeights.z+finale*phaseWeights.w;
 // Protect central fight readability without throwing away architectural detail.
 float centre=exp(-dot((uv-float2(.5,.52))*float2(2.0,1.4),(uv-float2(.5,.52))*float2(2.0,1.4)));
 float exposure=(.67-centre*.17)*(1+signal.y*.035-signal.z*.06);
 rgb*=exposure;
 // Slow textured veils and reflected ember light; never opaque shape columns.
 float veil=smoothstep(.50,.89,n)*smoothstep(.12,.52,uv.y)*.07;
 rgb=lerp(rgb,float3(.028,.019,.041),veil*(phaseWeights.y+phaseWeights.z));
 rgb+=float3(.09,.025,.019)*pow(g,3)*.11*phaseWeights.x;
 // An occluded eclipse grows behind the altar only in the ensemble.
 float d=length((uv-float2(.65,.23))*float2(1.72,1));
 float disk=1-smoothstep(.113,.122,d);
 float corona=exp(-pow((d-.124)*65,2))*(.52+g*.48);
 rgb=lerp(rgb,rgb*.11,disk*phaseWeights.w*.94);
 rgb+=float3(.24,.052,.12)*corona*phaseWeights.w*.37;
 return float4(rgb*signal.x,signal.x)*i.C;
}
technique ScarletBackdrop { pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); } }
