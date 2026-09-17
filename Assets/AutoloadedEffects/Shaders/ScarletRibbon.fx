// Original Scarlet materials. Coordinates are along/across accepted paths;
// turbulence changes pigment, never the collision silhouette or warning clock.
matrix uWorldViewProjection;
sampler grain : register(s1);
sampler curl : register(s2);
sampler veins : register(s3);
float clock, species, roundShape, reduced;
float3 tint;
float4 signal; // opacity, forecast, rift, impact accent
float2 footprint; // world-space length, average capsule radius
float2 capAxis; // endpoint's outward half-disk; zero for standalone impacts
float3 motion; // physical attack, warning progress, ticks since fire
struct PI { float4 P:POSITION0; float4 C:COLOR0; float3 U:TEXCOORD0; };
struct QI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(PI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.P.z=0; o.C=v.C; o.U=v.U.xy;
 o.U.y=(o.U.y-.5)/max(.001,v.U.z)+.5; return o; }
VO VQ(QI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.P.z=0; o.C=v.C; o.U=v.U; return o; }
float4 Forecast(VO i)
{
 float2 uv=i.U;
 float distance=lerp(abs(uv.y*2-1),length(uv*2-1),roundShape);
 float edge=1-smoothstep(.91,1,distance);
 float body=pow(saturate(1-distance*distance),.65);
 float boundary=exp2(-pow(((1-distance)*footprint.y-1.4)*.62,2));
 float spine=exp2(-pow(distance*footprint.y/1.3,2));
 float flow=tex2D(curl,float2(uv.x*footprint.x*.005-clock*.9,uv.y*3)).r;
 float folds=tex2D(veins,float2(uv.x*footprint.x*.008-clock*1.2,uv.y*4+flow*.24)).r;
 float star=pow(saturate(flow*1.17),22)*pow(saturate(folds*1.18),13);
 float3 hot=lerp(tint,float3(1,.94,.89),.90);
 float3 thread=tint*(body*.025+boundary*.25+star*.54)+hot*spine*.72;
 float3 disk=tint*(body*.018+star*.26)+hot*boundary*.32;
 float3 light=lerp(thread,disk,roundShape);
 float brightness=(.87+.13*sin(clock*4.2))*(.78+.22*motion.y);
 return float4(light*edge*signal.x*brightness,edge*signal.x*body*.035)*i.C;
}
float4 PS(VO i):COLOR0
{
 if(roundShape>.5 && dot(capAxis,capAxis)>.1) clip(dot(i.U*2-1,capAxis)+.01);
 // Resolve forecasts before live pigment; verified separately on pinned FNA.
 if(motion.x>.5 && signal.y>.5) return Forecast(i);
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
 // Physical phrases retain their capsule/rift geometry and accepted clock.
 // Like the Doll material, light travels THROUGH dark folds, not a flat fill.
 // The carrier remains visible all the way to the live collision boundary.
 if(motion.x>.5) {
   float x=uv.x*footprint.x, y=uv.y*2-1;
   float2 flowUV=float2(x*.005-clock*3.8,y*1.4);
   float flow=tex2D(curl,flowUV+float2(0,n*.24)).r;
   float folds=tex2D(veins,float2(x*.008-clock*5.2,y*2.1+flow*.33)).r;
   float body=pow(saturate(1-distance*distance),.65);
   float edgePixels=(1-distance)*footprint.y;
   float boundary=exp2(-pow((edgePixels-1.4)*.62,2));
   float spine=exp2(-pow(distance*footprint.y/1.3,2));
   float3 hot=lerp(tint,float3(1,.94,.89),.90);
   float curlLine=sin(y*9+flow*6-folds*3);
   float filament=pow(saturate(1-abs(curlLine)*1.5),3);
   float bend=(flow-.5)*.22*(1-distance);
   float core=1-smoothstep(.17+folds*.16,.27+folds*.18,abs(y-bend));
   if(roundShape>.5) core=exp2(-distance*distance*7)*( .6+folds*.4);
   float wound=smoothstep(.46,.78,flow+filament*.16);
   float3 pigment=lerp(tint*(.74+folds*.72),tint*.035,wound*.83);
   float surge=.84+.16*sin(x*.014-clock*27);
   shade=pigment*body+hot*core*(.82+filament*.34)*surge;
   shade+=tint*filament*body*.34+hot*boundary*.14;
   // Dark seams remain solid hazards, while the bright silk/thorn fibres move.
   if(signal.z>.5) {
     float lip=exp2(-pow((distance-.55)*7,2));
     shade=lerp(float3(.008,.002,.018),tint*.19,flow)*body;
     shade+=lip*(tint*(.7+flow)+hot*filament*.6)+tint*boundary*.26;
   }
   shade+=hot*body*signal.w*.20*(1-reduced*.8);
   return float4(shade*alpha,alpha*(.66+body*.21))*i.C;
 }
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
