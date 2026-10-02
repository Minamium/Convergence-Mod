// The sealed battlefield the mourning bell raises around its summoner.
// AutoloadPass: the in-field backdrop, a world quad behind terrain: an eclipse,
//   stars, far and near ridges, layered mist and the weapons of a lost battle
//   driven into the ground, each layer with its own parallax so the camera
//   reveals depth. The night is a cool slate so violet belongs to the hazards.
// SealPass: one quad over the physical viewport, drawn above the world: the
//   opaque abyss outside the seal (a dark lake under the floor that mirrors it),
//   violet spirit fire rooted along the seal's outer face, talismans and corner
//   seals. No line is drawn: the edge is where the fire takes root on the walls
//   and roof, and a mist seam over the lake at the floor.
// RimPass: the uneven light the seal throws inward, behind players and hazards.
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
    return lerp(float3(.022,.027,.044),float3(.068,.078,.116),smoothstep(.05,.8,v));
}

// ---------------------------------------------------------------- backdrop
// The weapons left where they fell, driven into the field on the 2x2 art-pixel grid
// of the cuts. Each returns x = silhouette, y = moonlight on its right edge (steel
// catches it fully, wood and cord faintly). rel is (sideways, height) from where the
// weapon enters the ground; the moon stands to the upper right.
float2 Along(float2 rel, float lean)
{
    float2 dir=normalize(float2(lean,1));
    return float2(dot(rel,dir),rel.x*dir.y-rel.y*dir.x);    // along the weapon, across it
}

float2 Part(float a, float c, float a0, float a1, float halfWidth)
{
    float m=step(a0,a)*step(a,a1)*step(abs(c),halfWidth);
    return float2(m,m*step(halfWidth-1.8,c));
}

float2 Katana(float2 rel, float lean, float h, float sg, float t, float id)
{
    // driven in point first: a curved blade widening to a round tsuba, a wrapped grip,
    // and on some a sageo cord hanging from the guard and stirring in the wind
    float2 f=Along(rel,lean);
    float blade=h*.6, u=saturate(f.x/blade);
    float2 steel=Part(f.x,f.y-sg*4*u*u,0,blade,2.2+1.4*u);
    float g=f.y-sg*4;
    float2 guard=Part(f.x,g,blade,blade+4,7.5);
    float tilt=(f.x-blade)*.06*sg;
    float2 grip=Part(f.x,g-tilt,blade+4,h-3,2.8);
    float2 cap=Part(f.x,g-(h-blade)*.06*sg,h-3,h+1,3.4);
    float wrap=grip.x*step(abs(g-tilt),1.2)*step(frac((f.x-blade)/6),.4);
    float2 dir=normalize(float2(lean,1)), side=float2(dir.y,-dir.x);
    float2 knot=dir*(blade+2)+side*sg*11;
    float drop=knot.y-rel.y;
    float sway=sin(t*1.4+id*3.1)*6+sin(t*2.3+id)*2;
    float hang=step(.55,frac(id*.37))*step(0,drop)*step(drop,32)
              *step(abs(rel.x-knot.x-sway*.7*pow(drop/32,1.5)-sg*drop*.12),1.1);
    float sil=max(max(max(steel.x,guard.x),max(grip.x,cap.x)),hang);
    float light=max(steel.y,max(max(guard.y,grip.y),max(cap.y,wrap))*.4);
    return float2(sil,light);
}

float2 Naginata(float2 rel, float lean, float h, float sg)
{
    // a long haft, a metal collar, and a curved blade sweeping back from the head
    float2 f=Along(rel,lean);
    float2 haft=Part(f.x,f.y,0,h,2.0);
    float u=saturate((f.x-h)/40);
    float2 steel=Part(f.x,f.y-sg*(1.5+8*u*u),h,h+40,lerp(4.4,1.2,u*u));
    float2 collar=Part(f.x,f.y,h-5,h+1,3.2);
    return float2(max(max(haft.x,steel.x),collar.x),max(steel.y,max(haft.y,collar.y)*.4));
}

float2 Yari(float2 rel, float lean, float h, float jumonji)
{
    // a straight spear with a leaf head; some carry the crossed blades of a jumonji
    float2 f=Along(rel,lean);
    float2 haft=Part(f.x,f.y,0,h,1.8);
    float k=saturate((f.x-h)/26);
    float width=4.0*pow(saturate(sin(3.1416*k)),.6)*(1-.35*k)+.6;
    float2 head=Part(f.x,f.y,h,h+26,width);
    float arms=step(abs(f.x-h-3-abs(f.y)*.4),1.6)*step(abs(f.y),11)*jumonji;
    return float2(max(max(haft.x,head.x),arms),max(head.y,haft.y*.4));
}

float2 Arrows(float2 rel, float lean, float h, float seed)
{
    // a few arrows that fell together, fletching ragged
    float2 m=0;
    [unroll] for(int j=0;j<3;j++)
    {
        float o=(j-1)*8+(hash1(seed*17+j)-.5)*6;
        float l=lean+(j-1)*.3+(hash1(seed*31+j)-.5)*.2;
        float hh=h*(.72+.36*hash1(seed*7+j));
        float2 f=Along(rel-float2(o,0),l);
        float2 shaft=Part(f.x,f.y,0,hh,1.1);
        float k=saturate((f.x-hh+14)/14);
        float vane=step(hh-14,f.x)*step(f.x,hh)*step(abs(f.y),1.2+2.8*k)*step(.25,frac(f.x/4.5+j*.37));
        float here=step(hash1(seed*3.7+j),.8);
        m=max(m,float2(max(shaft.x,vane),shaft.y*.4)*here);
    }
    return m;
}

float2 Planted(float2 p, float x, float ground, float scale, float salt, float t)
{
    // one weapon per cell, chosen by hash; neighbours are checked so a lean never clips
    float2 r=0;
    float span=92*scale;
    [unroll] for(int k=-1;k<=1;k++)
    {
        float cell=floor(x/span)+k;
        float id=cell+salt;
        float kind=hash1(id*4.71);
        float s=hash1(id*6.07);
        float sg=s<.5?-1:1;
        float lean=(hash1(id*2.29)-.5)*.8;
        float2 rel=float2(x-(cell+.15+.7*hash1(id*1.37))*span,ground-p.y+4*scale)/scale;
        float2 w=0;
        [branch] if(kind<.40) w=Katana(rel,lean*.55,64+28*s,sg,t,id);
        else if(kind<.54) w=Naginata(rel,lean*.8,110+40*s,sg);
        else if(kind<.76) w=Yari(rel,lean*.8,120+50*s,step(.62,s));
        else if(kind<.90) w=Arrows(rel,lean*.7,40+20*s,id);
        r=max(r,w);
    }
    return r;
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
    col+=float3(.46,.50,.62)*star*.5;
    float c1=tex2D(turbulence,qs/float2(1400,520)+float2(t*.006,0)).r;
    float c2=tex2D(veins,qs/float2(900,700)-float2(t*.004,t*.001)).r;
    float cloud=smoothstep(.40,.86,c1*.75+c2*.45)*(1-smoothstep(.42,.72,uv.y));
    col=lerp(col,float3(.040,.045,.068)+storm*float3(.010,.008,.020),cloud*.8);
    float2 qm=p-camera*.8;
    float2 moon=float2(arenaSize.x*.70,arenaSize.y*.21);
    float md=length(qm-moon);
    float R=58;
    float flare=.55+.45*tex2D(turbulence,float2(atan2(qm.y-moon.y,qm.x-moon.x)*.159,t*.012)).r;
    float corona=(exp(-pow((md-R)/5,2))*.9+exp(-max(md-R,0)/46)*.35*step(R,md))*flare*(1+surge*.8)*(.3+.7*storm);
    float2 shade=moon+normalize(float2(-1,-.55))*R*1.15*(1-storm);
    float lit=smoothstep(R+1,R-1,md)*smoothstep(R*1.02-1,R*1.02+1,length(qm-shade));
    col=lerp(col,float3(.006,.004,.014),smoothstep(R+1,R-1,md));
    col+=float3(.12,.125,.17)*lit;
    col+=float3(.20,.11,.38)*corona*.6;
    col+=float3(.050,.045,.085)*cloud*exp(-md/380);

    // distant lightning from Phase 2: a local glow behind the ridges, never a screen flash
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
    col+=float3(.40,.44,.62)*bolt*flick*.5;
    col+=float3(.055,.050,.095)*flick*exp(-abs(qb.x-boltX)/520)*(1-uv.y*.6);

    // far ridge, its mist, near ridge, its mist
    float2 qf=p-camera*.62;
    float far=smoothstep(-1.5,1.5,qf.y-Ridge(qf.x,arenaSize.y*.57,38,22,0));
    col=lerp(col,float3(.030,.034,.052),far);
    float fogA=exp(-pow((p.y-(arenaSize.y*.64+14*sin(qf.x/230+t*.05)))/46,2))
              *(.5+.5*tex2D(turbulence,qf/float2(700,160)+float2(t*.010,0)).r);
    col+=float3(.060,.071,.102)*fogA*(.8+storm*.5);
    float2 qn=p-camera*.38;
    float mid=smoothstep(-1.5,1.5,qn.y-Ridge(qn.x,arenaSize.y*.71,30,16,2.1));
    col=lerp(col,float3(.017,.019,.031),mid);
    // a far rank of smaller weapons on the plain, veiled by the mist that follows
    float farX=pq.x-camera.x*.30;
    float farGround=arenaSize.y*.86+6*sin(farX/240);
    [branch] if(abs(pq.y-farGround+50)<70)
    {
        float2 farW=Planted(pq,farX,farGround,.6,71,t);
        col=lerp(col,float3(.011,.013,.021),farW.x);
        col+=float3(.040,.046,.064)*farW.y;
    }
    float fogB=exp(-pow((p.y-(arenaSize.y*.80+10*sin(qn.x/180-t*.07)))/40,2))
              *(.45+.55*tex2D(veins,qn/float2(600,140)-float2(t*.012,0)).r);
    col+=float3(.064,.075,.108)*fogB*(.75+storm*.5)*(1-reduced*.4);

    // the weapons of the lost battle, driven into the real floor line; blades catch the moon
    float nearX=pq.x-camera.x*.18;
    float ground=arenaSize.y-16-7*sin(nearX/170)-5*sin(nearX/53+1);
    float earth=step(ground,pq.y);
    col+=float3(.030,.034,.050)*exp(-max(ground-p.y,0)/110);    // low haze the silhouettes stand against
    float2 near=0;
    [branch] if(ground-pq.y<210) near=Planted(pq,nearX,ground,1,0,t);
    float sil=max(earth,near.x);
    float rim=earth*(1-step(ground,pq.y-2));
    col=lerp(col,float3(.006,.007,.012),sil);
    col+=float3(.060,.066,.092)*rim*.8;
    col+=float3(.12,.13,.18)*near.y*(.75+.25*sin(t*.9+pq.x*.013));

    // mist pooling on the floor and spirit motes drifting up
    float gm=tex2D(turbulence,p/float2(520,120)+float2(t*.02,0)).r*tex2D(blotch,p/float2(900,300)-float2(t*.008,0)).r;
    col+=float3(.056,.064,.092)*gm*exp(-(arenaSize.y-p.y)/70)*1.2*(1-reduced*.4);
    float2 mq=float2(p.x-camera.x*.1,p.y+t*26);
    float2 mc=floor(mq/110), ml=mq-mc*110;
    float2 mp=float2(20+70*hash2(mc+3.1),20+70*hash2(mc+7.7));
    float mote=exp(-dot(ml-mp,ml-mp)/3.5)*step(hash2(mc),.22)*(.5+.5*sin(t*3+hash2(mc)*40));
    col+=float3(.30,.36,.46)*mote*.4*(1-reduced)*smoothstep(.15,.6,uv.y);

    // darken toward the seal so its fire reads, and lift slightly at a phase change
    float edge=min(min(p.x,arenaSize.x-p.x),p.y);
    col*=lerp(.62,1,smoothstep(0,220,edge))*(1+surge*.15);
    col=min(col,float3(.15,.16,.22));          // Y <= ~.023, below the Tone2 forecast fill (.032)
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
// Indigo for the seal's ink and inward light so it never reads as a forecast; violet
// only for the fire and the moments the seal is traced, touched or shaken by a phase.
static const float3 SealInk=float3(.28,.27,.64);    // Y ~.08
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
    // violet spirit fire licking off the seal's outer face: it climbs the walls and rises off the roof.
    // Its roots smoulder unevenly along the edge, so the boundary is never a ruled line.
    float2 a=floor(float2(along,outward)/2)*2;
    float n=Fetch(turbulence,float2((a.x-t*90*climb)/70,(a.y-t*60*(1-climb))/60)).r*.65
           +Fetch(veins,float2((a.x-t*140*climb)/38,(a.y-t*95*(1-climb))/34)).r*.45;
    float height=40+80*Fetch(blotch,float2(a.x/420+t*.03,.5)).r;
    float root=smoothstep(.25,.85,Fetch(veins,float2((a.x-t*55*climb)/96,.37+t*.02)).r);
    return max(saturate(n*1.5-a.y/height),exp(-a.y/(4+6*root))*(.12+.5*root));
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

    // no ruled line: touch ripples, the deploy trace and a phase surge brighten the fire and the inward light
    float ringSum=0;
    float2 rp=ripple0.xy-w; float ra=ripple0.z;
    ringSum+=exp(-pow((length(rp)-(14+ra*260))/7,2))*saturate(1-ra/.55)*ripple0.w;
    rp=ripple1.xy-w; ra=ripple1.z;
    ringSum+=exp(-pow((length(rp)-(14+ra*260))/7,2))*saturate(1-ra/.55)*ripple1.w;
    ringSum*=exp(-abs(d)/40);
    float lift=saturate(surge*.8+tracer+ringSum*.8);
    float n1=Fetch(turbulence,(w+float2(t*9,-t*4))/float2(760,520)).r;
    float n2=Fetch(veins,(w-float2(t*9,-t*4)*.6)/1150).r;
    float mist=smoothstep(.38,.9,n1*.65+n2*.55);

    // ---- inside: only uneven light thrown in by the seal, never a fill or a line over the fight
    float wash=Fetch(veins,float2(perim/150-t*.03,.61)).r;
    float glow=exp(min(d,0)/(9+9*wash))*(.35+.65*wash)*(1-floorEdge)*drawn;
    float streak=Fetch(turbulence,float2(perim/46,abs(d)/260-t*.06)).r;
    float curtain=exp(min(d,0)/34)*smoothstep(.45,.9,streak)*.18*(1-floorEdge)*(1-reduced*.5)*drawn;
    float floorMist=exp(min(d,0)/14)*floorEdge*.20*smoothstep(.3,.8,n1)*drawn;
    float insideA=saturate(glow*.34+curtain+floorMist+ringSum*.6+tracer*.4*step(abs(d),24));
    float3 insideRgb=SealGlow*glow*.34*(1+lift*2)+SealGlow*1.6*curtain
                    +float3(.055,.062,.090)*floorMist+Tone(3.4)*ringSum*.6+Tone(4.6)*tracer*.4*step(abs(d),24);
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
    // a cold mist seam rolls along the lake's surface where the floor meets it
    float bank=smoothstep(.3,.85,Fetch(turbulence,float2(w.x/380+t*.025,dy/46-t*.04)).r);
    abyss+=float3(.050,.056,.086)*exp(-dy/24)*(.3+.7*bank)*below*drawn*(1-reduced*.3);
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
    float3 stripRgb=strip>2.5?lerp(SealInk,Tone(3.4),smoulder):strip>1.5?float3(.05,.04,.10)
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
    outer+=SealInk*.9*sealMark*(1+surge);
    outer+=Tone(4.6)*tracer*.5;
    outer+=Tone(3.4)*ringSum*.5;
    // The field melts away like the samurai: far dark first, the seal last.
    float ember;
    float melt=Dissolve(w,presence,.25-min(max(d,0),1500)/3000,ember);
    float decor=saturate(stripMask+sealMark+sealGlow+tracer*.5+fire);
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
