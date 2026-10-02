// The sealed battlefield the mourning bell raises around its summoner.
// AutoloadPass: the in-field backdrop, a world quad behind terrain: an eclipse,
//   stars, far and near ridges, layered mist and a field of graves and leaning
//   spears, each layer with its own parallax so the camera reveals depth.
// SealPass: one quad over the physical viewport, drawn above the world: the
//   opaque abyss outside the seal (a dark lake under the floor that mirrors it),
//   violet spirit fire on the seal's outer face, talismans and corner seals, and
//   a thin in-field light. Players and hazards stay inside and brighter than this.
matrix uWorldViewProjection;
sampler turbulence : register(s1);
sampler veins : register(s2);
sampler blotch : register(s3);
float clock, reduced, presence, phase, surge;
float2 arenaSize, camera;
float4 rect;
float worldPerPx, flipY, deploy, ending, outsider;
float4 ripple0, ripple1;

struct VI { float4 P:POSITION0; float4 C:COLOR0; float2 U:TEXCOORD0; };
struct VO { float4 P:SV_POSITION; float4 C:COLOR0; float2 U:TEXCOORD0; };
VO VS(VI v) { VO o=(VO)0; o.P=mul(v.P,uWorldViewProjection); o.C=v.C; o.U=v.U; return o; }

// Seal-pass fetches avoid derivatives so deep-field pixels can leave early.
#define Fetch(s,uv) tex2Dlod(s,float4(uv,0,0))

float hash1(float n) { return frac(sin(n*12.9898)*43758.5453); }
float hash2(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }

float3 Tone(float k)
{
    // the six-tone violet of the cuts (SamuraiCut.fx), sampled 0..5
    float3 a=float3(.086,.020,.188), b=float3(.243,.063,.541), c=float3(.439,.157,.871);
    float3 d=float3(.667,.424,1.0), e=float3(.863,.769,1.0), f=float3(1,1,1);
    return k<1?lerp(a,b,k):k<2?lerp(b,c,k-1):k<3?lerp(c,d,k-2):k<4?lerp(d,e,k-3):lerp(e,f,saturate(k-4));
}

float Ridge(float x, float base, float a, float b, float c)
{
    return base+a*sin(x/310+c)+b*sin(x/127+1.3+c)+(tex2Dlod(veins,float4(x/2600+c*.13,.3+c*.2,0,0)).r-.5)*120;
}

float B2(float2 p) { return fmod(2*p.x+3*p.y,4); }
float Bayer(float2 cell)
{
    float2 c=cell-4*floor(cell*.25);
    float2 hi=floor(c*.5);
    return (4*B2(c-2*hi)+B2(hi)+.5)/16;
}

// The field melts away the way the samurai does, in the dithered pixels of its cuts:
// 1 while the field stands (never a hole), then cells drop out with a violet rim.
float Dissolve(float2 w, float amount, float bias, out float ember)
{
    float n=Fetch(blotch,w/float2(1700,1300)+float2(.31,.17)).r*.55+Fetch(turbulence,w/float2(560,430)).r*.30
           +Fetch(veins,w/210).r*.15;
    float x=(n+bias+.25-(1-amount)*1.5)*3;
    float keep=step(Bayer(floor(w/2)),x);
    float steady=step(.999,amount);
    ember=saturate(step(Bayer(floor(w/2)),x+.35)-keep)*(1-steady)*.8;
    return max(keep,steady);
}

float3 Sky(float v)
{
    return lerp(float3(.026,.024,.068),float3(.082,.064,.165),smoothstep(.05,.8,v));
}

// ---------------------------------------------------------------- backdrop
float Spear(float2 p, float ground, float cellX)
{
    // One lance per 96 px cell, leaning; neighbours are checked so a lean never clips.
    float s=0;
    [unroll] for(int k=-1;k<=1;k++)
    {
        float cell=floor(cellX/96)+k;
        float present=step(hash1(cell*9.17),.6);
        float baseX=cell*96+12+70*hash1(cell*1.91);
        float lean=(hash1(cell*2.77)-.5)*.62;
        float len=120+170*hash1(cell*3.33);
        float up=ground-p.y;
        float x=baseX+up*lean;
        float shaft=step(abs(cellX-x),1.6)*step(0,up)*step(up,len);
        float tip=saturate((len+18-up)/18);
        float head=step(abs(cellX-x),3.6*tip)*step(len,up)*step(up,len+18);
        s=max(s,max(shaft,head)*present);
    }
    return s;
}

float Grave(float2 p, float ground, float cellX)
{
    float cell=floor(cellX/150), local=cellX-cell*150;
    float width=26+24*hash1(cell*2.11), height=50+76*hash1(cell*3.07);
    float x0=30+80*hash1(cell*4.13), lean=(hash1(cell*5.71)-.5)*.22;
    float up=ground-p.y;
    float2 g=float2(local-x0-up*lean,up);
    float body=step(0,g.x)*step(g.x,width)*step(-4,g.y)*step(g.y,height);
    float2 capUv=float2((g.x-width*.5)/(width*.5),(g.y-height)/(width*.38));
    float cap=step(dot(capUv,capUv),1)*step(height,g.y)*step(hash1(cell*6.3),.7);
    return max(body,cap)*step(hash1(cell*1.37),.78);
}

float Field(float2 pq, float nearX)
{
    float ground=arenaSize.y-16-7*sin(nearX/170)-5*sin(nearX/53+1);
    return max(max(Grave(pq,ground,nearX),Spear(pq,ground,nearX)),step(ground,pq.y));
}

float4 Backdrop(VO i):COLOR0
{
    float2 uv=i.U;
    float2 p=uv*arenaSize;
    float2 pq=floor(p/2)*2+1;          // silhouettes sit on the 2x2 art-pixel grid of the cuts
    float t=clock*(1-reduced*.7);
    float storm=saturate(phase*.5);
    float3 col=Sky(uv.y);

    // stars, clouds and the eclipse, nearly fixed to the view
    float2 qs=p-camera*.85;
    float2 sc=floor(qs/70), sl=qs-sc*70-35;
    float star=step(hash2(sc),.07)*exp(-dot(sl,sl)/1.4)*(.55+.45*sin(t*2.3+hash2(sc+5)*30))*(1-smoothstep(.15,.55,uv.y));
    col+=float3(.50,.44,.72)*star*.5;
    float c1=tex2D(turbulence,qs/float2(1400,520)+float2(t*.006,0)).r;
    float c2=tex2D(veins,qs/float2(900,700)-float2(t*.004,t*.001)).r;
    float cloud=smoothstep(.40,.86,c1*.75+c2*.45)*(1-smoothstep(.42,.72,uv.y));
    col=lerp(col,float3(.052,.042,.115)+storm*float3(.016,.004,.026),cloud*.8);
    float2 qm=p-camera*.8;
    float2 moon=float2(arenaSize.x*.70,arenaSize.y*.21);
    float md=length(qm-moon);
    float R=58;
    float flare=.55+.45*tex2D(turbulence,float2(atan2(qm.y-moon.y,qm.x-moon.x)*.159,t*.012)).r;
    float corona=(exp(-pow((md-R)/5,2))*.9+exp(-max(md-R,0)/46)*.35*step(R,md))*flare*(1+surge*.8)*(.3+.7*storm);
    float2 shade=moon+normalize(float2(-1,-.55))*R*1.15*(1-storm);
    float lit=smoothstep(R+1,R-1,md)*smoothstep(R*1.02-1,R*1.02+1,length(qm-shade));
    col=lerp(col,float3(.006,.004,.014),smoothstep(R+1,R-1,md));
    col+=float3(.13,.11,.24)*lit;
    col+=float3(.20,.11,.38)*corona*.6;
    col+=float3(.07,.035,.12)*cloud*exp(-md/380);

    // distant violet lightning from Phase 2: a local glow behind the ridges, never a screen flash
    float window=floor(t/3.1), lt=t-window*3.1;
    float strike=step(hash1(window*7.3),.25+.3*storm)*step(.5,phase)*(1-reduced);
    float2 qb=p-camera*.7;
    float boltX=(.12+.76*hash1(window*3.1+1.7))*arenaSize.x;
    float flick=strike*exp(-lt*6)*(.6+.4*sin(lt*90));
    // a zigzag: straight segments between random kinks, over a slow sideways drift
    float seg=floor(qb.y/34), segT=frac(qb.y/34);
    float kink=lerp(hash1(seg*1.7+window*5.3),hash1((seg+1)*1.7+window*5.3),segT)-.5;
    float jag=(tex2D(veins,float2(boltX*.001+window*.37,qb.y/420)).r-.5)*150+kink*46;
    float dx=abs(qb.x-boltX-jag);
    float bolt=(exp(-dx/1.8)+exp(-dx/10)*.3)*step(arenaSize.y*.05,qb.y);
    col+=float3(.45,.36,.80)*bolt*flick*.5;
    col+=float3(.07,.04,.13)*flick*exp(-abs(qb.x-boltX)/520)*(1-uv.y*.6);

    // far ridge, its mist, near ridge, its mist
    float2 qf=p-camera*.62;
    float far=smoothstep(-1.5,1.5,qf.y-Ridge(qf.x,arenaSize.y*.57,38,22,0));
    col=lerp(col,float3(.040,.034,.090),far);
    float fogA=exp(-pow((p.y-(arenaSize.y*.64+14*sin(qf.x/230+t*.05)))/46,2))
              *(.5+.5*tex2D(turbulence,qf/float2(700,160)+float2(t*.010,0)).r);
    col+=float3(.105,.060,.180)*fogA*(.8+storm*.5);
    float2 qn=p-camera*.38;
    float mid=smoothstep(-1.5,1.5,qn.y-Ridge(qn.x,arenaSize.y*.71,30,16,2.1));
    col=lerp(col,float3(.024,.020,.058),mid);
    float fogB=exp(-pow((p.y-(arenaSize.y*.80+10*sin(qn.x/180-t*.07)))/40,2))
              *(.45+.55*tex2D(veins,qn/float2(600,140)-float2(t*.012,0)).r);
    col+=float3(.115,.062,.195)*fogB*(.75+storm*.5)*(1-reduced*.4);

    // the field of the dead, standing on the real floor line, rimmed by the eclipse
    float nearX=pq.x-camera.x*.18;
    float sil=Field(pq,nearX);
    float rim=sil*(1-Field(pq-float2(0,2),nearX));
    col=lerp(col,float3(.007,.006,.016),sil);
    col+=float3(.075,.060,.15)*rim*.8;

    // mist pooling on the floor and spirit motes drifting up
    float gm=tex2D(turbulence,p/float2(520,120)+float2(t*.02,0)).r*tex2D(blotch,p/float2(900,300)-float2(t*.008,0)).r;
    col+=float3(.10,.052,.17)*gm*exp(-(arenaSize.y-p.y)/70)*1.2*(1-reduced*.4);
    float2 mq=float2(p.x-camera.x*.1,p.y+t*26);
    float2 mc=floor(mq/110), ml=mq-mc*110;
    float2 mp=float2(20+70*hash2(mc+3.1),20+70*hash2(mc+7.7));
    float mote=exp(-dot(ml-mp,ml-mp)/3.5)*step(hash2(mc),.22)*(.5+.5*sin(t*3+hash2(mc)*40));
    col+=float3(.42,.30,.75)*mote*.5*(1-reduced)*smoothstep(.15,.6,uv.y);

    // darken toward the seal so its fire reads, and lift slightly at a phase change
    float edge=min(min(p.x,arenaSize.x-p.x),p.y);
    col*=lerp(.62,1,smoothstep(0,220,edge))*(1+surge*.15);
    col=min(col,float3(.17,.15,.32));          // Y <= ~.025, below the Tone2 forecast fill (.032)
    // The seal opens from the summoner's feet; once open it stays opaque, because
    // fading this quad would expose the vanilla sky inside the field.
    float fromFeet=length(p-float2(arenaSize.x*.5,arenaSize.y));
    float front=deploy*1900;
    float open=smoothstep(front,front-40,fromFeet);
    float ember;
    float a=Dissolve(p,saturate((presence-.35)/.65),uv.y*.25,ember)*saturate(open+step(.999,deploy));   // inside first, sky before ground
    float rimLight=exp(-pow((fromFeet-front+20)/14,2))*(1-step(.999,deploy))*presence+ember*.9;
    return float4(col*a+Tone(3.2)*rimLight*.8,saturate(a+rimLight*.8));
}

// ---------------------------------------------------------------- seal
// Indigo for the standing seal so it never reads as a forecast; violet only for
// fire tips and the moments the seal is traced, touched or shaken by a phase.
static const float3 SealLine=float3(.28,.27,.64);   // Y ~.08
static const float3 SealGlow=float3(.10,.10,.26);

float Talisman(float2 l)
{
    // a 12x30 px paper seal pasted on the wall, on the 2 px grid: 0 none, 1 paper, 2 ink, 3 sigil
    float paper=step(0,l.x)*step(l.x,12)*step(0,l.y)*step(l.y,30);
    float border=1-step(2,l.x)*step(l.x,10)*step(2,l.y)*step(l.y,28);
    float cap=step(l.y,6);
    float stroke=step(abs(l.x-6),1.1)*step(10,l.y)*step(l.y,25);
    float bars=step(abs(l.x-6),3.5)*(step(abs(l.y-13),1.1)+step(abs(l.y-19),1.1));
    float sigil=saturate(stroke+bars);
    return paper*(1+saturate(border+cap)+2*sigil*(1-saturate(border+cap)));
}

float Flame(float along, float outward, float t, float climb)
{
    // violet spirit fire licking off the seal's outer face: it climbs the walls and rises off the roof
    float2 a=floor(float2(along,outward)/2)*2;
    float n=Fetch(turbulence,float2((a.x-t*90*climb)/70,(a.y-t*60*(1-climb))/60)).r*.65
           +Fetch(veins,float2((a.x-t*140*climb)/38,(a.y-t*95*(1-climb))/34)).r*.45;
    float height=40+80*Fetch(blotch,float2(a.x/420+t*.03,.5)).r;
    return max(saturate(n*1.5-a.y/height),exp(-a.y/9)*.45);
}

// rim=0: the opaque abyss and everything on the seal's outer face (drawn above the world).
// rim=1: the thin light inside the seal (drawn behind players, bodies and forecasts).
float4 SealCore(float2 px, float rim)
{
    float2 w=(px-rect.xy)*worldPerPx;
    w.y=lerp(w.y,arenaSize.y-w.y,flipY);
    float2 hs=arenaSize*.5;
    float2 cen=w-hs;
    float2 q=abs(cen)-hs;
    float d=length(max(q,0))+min(max(q.x,q.y),0);          // world px, positive outside
    // Leave before any work where this pass draws nothing: most of the screen.
    float idle=rim>.5?saturate(step(2,d)+step(d,-170)*(1-outsider)):step(d,-2);
    [branch] if(idle>.5) return float4(0,0,0,0);
    float2 wq=floor(w/2)*2+1;
    float2 qq=abs(wq-hs)-hs;
    float dq=length(max(qq,0))+min(max(qq.x,qq.y),0);
    float t=clock*(1-reduced*.7);
    float outside=step(0,d);

    // perimeter position from the floor centre, mirrored left/right, for the trace
    float mx=abs(w.x-hs.x);
    float sideWall=step(q.y,q.x);
    float floorEdge=(1-sideWall)*step(0,cen.y);
    float roof=(1-sideWall)*(1-floorEdge);
    float perim=sideWall*(hs.x+arenaSize.y-clamp(w.y,0,arenaSize.y))
               +floorEdge*min(mx,hs.x)
               +roof*(hs.x+arenaSize.y+hs.x-min(mx,hs.x));
    float total=arenaSize.x+arenaSize.y;
    float reveal=deploy*total*1.08;
    float traced=smoothstep(reveal,reveal-60,perim);
    float tracer=exp(-pow((perim-reveal)/40,2))*step(deploy,.999);
    float drawn=traced*(1-smoothstep(.45,1,ending));
    float burn=smoothstep(0,.8,ending);
    float pour=saturate(smoothstep(0,500,reveal-perim-max(d,0)*.9)+smoothstep(.85,1,deploy));

    // the line: a still indigo core that only brightens when traced, touched or shaken
    float ringSum=0;
    float2 rp=ripple0.xy-w; float ra=ripple0.z;
    ringSum+=exp(-pow((length(rp)-(14+ra*260))/7,2))*saturate(1-ra/.55)*ripple0.w;
    rp=ripple1.xy-w; ra=ripple1.z;
    ringSum+=exp(-pow((length(rp)-(14+ra*260))/7,2))*saturate(1-ra/.55)*ripple1.w;
    ringSum*=exp(-abs(d)/40);
    float breathe=1+.06*sin(t*1.7)*(1-reduced);
    float core=1-smoothstep(1.6,2.6,abs(dq));
    float lift=saturate(surge*.8+tracer+ringSum*.8);
    float3 lineRgb=lerp(SealLine*breathe,Tone(3.6),lift);
    float n1=Fetch(turbulence,(w+float2(t*9,-t*4))/float2(760,520)).r;
    float n2=Fetch(veins,(w-float2(t*9,-t*4)*.6)/1150).r;
    float mist=smoothstep(.38,.9,n1*.65+n2*.55);

    // ---- inside: only thin light at the seal, never a fill over the fight
    float glow=exp(min(d,0)/12)*drawn;
    float streak=Fetch(turbulence,float2(perim/46,abs(d)/260-t*.06)).r;
    float curtain=exp(min(d,0)/34)*smoothstep(.45,.9,streak)*.18*(1-floorEdge)*(1-reduced*.5)*drawn;
    float floorMist=exp(min(d,0)/10)*floorEdge*.16*n1*drawn;
    float insideA=saturate(core*drawn*.9+glow*.28+curtain+floorMist+ringSum*.6+tracer*.4*step(abs(d),24));
    float3 insideRgb=lineRgb*core*drawn*.9+SealGlow*glow*.28*(1+lift*2)+SealGlow*1.6*curtain
                    +float3(.10,.08,.20)*floorMist+Tone(3.4)*ringSum*.6+Tone(4.6)*tracer*.4*step(abs(d),24);
    float4 inner=float4(insideRgb,insideA)*presence*(1-outside);

    // ---- the abyss outside, and the dark lake under the floor that mirrors the field
    float3 abyss=float3(.006,.005,.014)+float3(.040,.026,.084)*mist*(1-reduced*.4);
    float haze=exp(-max(d,0)/170)*(.55+.45*n1);
    abyss+=float3(.09,.07,.20)*haze*.42*(1+surge*.6)*drawn;
    float below=step(arenaSize.y,w.y);
    float dy=max(w.y-arenaSize.y,0);
    float wave=sin(dy*.08+t*1.3)*3*saturate(dy/120)+sin(w.x*.02+t*.7)*1.5;
    float2 m=float2(w.x+wave,arenaSize.y-dy*1.05);
    float2 mf=m-camera*.62;
    float mirroredRidge=smoothstep(-1.5,1.5,mf.y-Ridge(mf.x,arenaSize.y*.57,38,22,0));
    float3 mirror=lerp(Sky(m.y/arenaSize.y),float3(.020,.017,.046),mirroredRidge);
    float2 mm=m-camera*.8-float2(arenaSize.x*.70,arenaSize.y*.21);
    mirror+=float3(.20,.11,.38)*exp(-pow((length(mm)-58)/6,2))*.4*(.3+.7*saturate(phase*.5));
    float shimmer=.78+.22*Fetch(blotch,float2(w.x/260+t*.02,dy/55-t*.12)).r;
    abyss=lerp(abyss,mirror*shimmer*.7+abyss*.4,below*exp(-dy/300)*(1-outsider));
    abyss+=SealLine*.6*exp(-dy/22)*(.4+.6*(sin(w.x*.045+t*1.6+n1*6)*.5+.5))*below*.30*drawn;
    float rise=Fetch(blotch,float2(w.x/420,(w.y+t*22)/260)).r;
    abyss+=float3(.04,.025,.08)*smoothstep(.55,.95,rise)*below*exp(-dy/420)*(1-reduced*.5);

    // ---- violet spirit fire on the walls and roof (never under the floor), capped well below forecasts
    float fire=Flame(perim,max(d,0),t,sideWall)*(1-floorEdge)*outside*drawn*(1-reduced*.35);
    float flare=1+surge*.5+sin(saturate(ending)*3.1416)*1.4;
    float3 fireRgb=min(Tone(1.0+2.0*fire)*fire*.8*flare,float3(.42,.20,.80));

    // ---- paper seals pasted along the walls only (never standing or hanging like flags)
    float wallOut=max(-w.x,w.x-arenaSize.x);
    float wallCell=floor((w.y-20)/230);
    float side=step(0,cen.x);
    float wallY=wallCell*230+20+150*hash1(wallCell*3.1+side*17);
    float strip=Talisman(float2(wallOut-3,wq.y-wallY))*step(hash1(wallCell*5.3+side*29),.8)
               *step(0,w.y)*step(w.y,arenaSize.y-34)*step(0,wallOut);
    float stripId=wallCell*2+side;
    float vanish=hash1(stripId*1.9)*.85+.12;
    float stripSeen=step(.5,traced)*step(burn,vanish);
    float smoulder=step(.001,burn)*step(vanish-.18,burn);
    float3 stripRgb=strip>2.5?lerp(SealLine,Tone(3.4),smoulder):strip>1.5?float3(.05,.04,.10)
                   :lerp(float3(.40,.38,.50),Tone(4.6),smoulder);
    float stripMask=step(.5,strip)*stripSeen;

    // ---- corner seals turning slowly in the dark (not on the floor corners for a watcher)
    float2 corner=float2(step(0,cen.x)*arenaSize.x,step(0,cen.y)*arenaSize.y);
    float2 v=w-corner;
    float r=length(v);
    float ang=atan2(v.y,v.x);
    float spin=t*.2*(1-reduced*.5);
    float rings=step(abs(r-64),1.6)+step(abs(r-50),1.1)*.8;
    float ticks=step(frac((ang+spin)/.5236),.14)*step(50,r)*step(r,64);
    float2 dia=float2(cos(-spin*1.6),sin(-spin*1.6))*57;
    float gems=step(abs(v.x-dia.x)+abs(v.y-dia.y),5)+step(abs(v.x+dia.x)+abs(v.y+dia.y),5)
              +step(abs(v.x-dia.y)+abs(v.y+dia.x),5)+step(abs(v.x+dia.y)+abs(v.y-dia.x),5);
    float sealVisible=outside*drawn*(1-outsider*step(0,cen.y));
    float sealMark=saturate(rings+ticks+gems)*sealVisible;
    float sealGlow=exp(-abs(r-57)/14)*.25*sealVisible;

    float3 outer=abyss+fireRgb+SealGlow*sealGlow;
    outer=lerp(outer,stripRgb,stripMask);
    outer+=SealLine*.9*sealMark*(1+surge);
    outer=lerp(outer,lineRgb,core*drawn)+Tone(4.6)*tracer*.5;
    outer+=Tone(3.4)*ringSum*.5;
    // The field melts away like the samurai: far dark first, the seal last.
    float ember;
    float melt=Dissolve(w,presence,.25-min(max(d,0),1500)/3000,ember);
    float decor=saturate(stripMask+sealMark+sealGlow+core*drawn+tracer*.5+fire);
    float exteriorA=lerp(pour*melt,decor*presence,outsider);
    float4 outerPx=float4(outer*exteriorA+Tone(3.4)*ember*(1-outsider),saturate(exteriorA+ember*(1-outsider)))*outside;
    return lerp(outerPx,inner,rim);
}

float4 Seal(VO i):COLOR0 { return SealCore(i.U,0); }
float4 Rim(VO i):COLOR0 { return SealCore(i.U,1); }

technique SamuraiBattlefield
{
    pass AutoloadPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Backdrop(); }
    pass SealPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Seal(); }
    pass RimPass { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 Rim(); }
}
