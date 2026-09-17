// Original Scarlet materials. Coordinates are along/across accepted paths;
// turbulence changes pigment, never the collision silhouette or warning clock.
matrix uWorldViewProjection;
sampler grain : register(s1);
sampler curl : register(s2);
sampler veins : register(s3);
float clock, species, roundShape, reduced;
float3 tint;
float4 signal; // opacity, forecast, rift, impact accent
struct PI { float4 P:POSITION0; float4 C:COLOR0; float3 U:TEXCOORD0; };
struct QI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(PI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.P.z=0; o.C=v.C; o.U=v.U.xy;
 o.U.y=(o.U.y-.5)/max(.001,v.U.z)+.5; return o; }
VO VQ(QI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.P.z=0; o.C=v.C; o.U=v.U; return o; }
float4 PS(VO i):COLOR0
{
 float2 uv=i.U;
 float transverse=abs(uv.y*2-1);
 float n=tex2D(grain,float2(uv.x*6-clock*.19,uv.y*2)).r;
 float f=tex2D(curl,float2(uv.x*11+clock*.07,uv.y*4+n*.18)).r;
 float vein=tex2D(veins,float2(uv.x*7-clock*.12,uv.y*2)).r;
 float distance=lerp(transverse,length(uv*2-1),roundShape);
 float edge=1-smoothstep(.91,1,distance);
 float rim=smoothstep(.65,.87,distance)*(1-smoothstep(.89,1,distance));
 float center=pow(saturate(1-distance*distance),.65);
 float3 shade;
 if(species<.5) {
   float fracture=pow(saturate(n*.6+f*.5),3);
   shade=lerp(float3(.055,.018,.016),tint,center*(.30+fracture*.63));
   shade+=float3(1,.39,.055)*(fracture*center*.70+rim*.26);
 } else if(species<1.5) {
   float silk=pow(saturate(.5+.5*sin(uv.x*74+uv.y*13+f*5-clock*3)),9);
   shade=lerp(float3(.035,.015,.062),tint,center*(.19+n*.29));
   shade+=float3(.49,.40,.65)*(silk*.27+rim*.51);
 } else if(species<2.5) {
   shade=lerp(float3(.028,.010,.032),tint,center*(.26+vein*.29));
   shade+=float3(.40,.06,.19)*pow(saturate(vein*1.2),4)+rim*float3(.74,.50,.51)*.52;
 } else {
   shade=lerp(float3(.07,.015,.036),tint,center*(.44+f*.28));
   shade+=rim*float3(.97,.72,.55)*.57;
 }
 // A void seam has a dark cavity and broken, hot lips, never a filled laser.
 if(signal.z>.5) {
   float lip=exp(-pow((distance-.68)*9,2));
   float spark=pow(saturate(f*1.32),6)*lip;
   shade=lerp(float3(.009,.004,.018),tint*.25,vein*.6);
   shade+=lip*tint*(.55+n*.52)+spark*float3(1,.69,.72)*(.2+signal.w*.32);
 }
 float alpha=edge*signal.x;
 if(signal.y>.5) {
   // Sparse internal grain + continuous precise outer boundary. No opaque slab.
   float fleck=pow(saturate(f*1.13),12);
   alpha*=.16+rim*.59+fleck*.20;
   shade=tint*(.62+rim*.32);
 } else shade+=center*max(0,signal.w)*.055*(1-reduced*.8);
 return float4(shade*alpha,alpha)*i.C;
}
technique ScarletRibbon {
 pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); }
 pass CapPass { VertexShader=compile vs_3_0 VQ(); PixelShader=compile ps_3_0 PS(); }
}
