// Original Convergence sword discharge. Thin hot channel, violet corona and
// wide low-opacity ion glow; fork silhouettes come from attached geometry.
matrix uWorldViewProjection;
struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o; o.P=mul(v.P,uWorldViewProjection); o.P.z=0; o.C=v.C; o.U=v.U; return o; }
float4 PS(VO i):COLOR0
{
    float x=abs(i.U.y*2-1);
    float core=exp2(-x*x*600);
    float corona=exp2(-x*x*42);
    float glow=exp2(-x*x*5)*(1-smoothstep(.65,1,x));
    float3 light=float3(.96,.90,1)*core+float3(.54,.16,1)*corona*.85+float3(.24,.025,.66)*glow*.50;
    return float4(light*i.C.a,glow*i.C.a*.07);
}
technique SwordLightning { pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); } }
