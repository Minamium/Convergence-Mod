#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Client.Graphics;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Species-specific continuous skin + travelling material. No gameplay positions
// or random state are produced here. Also renders in the final avatar's local RT.
//
// Attack expression (Crown species 0, Mantle species 1). The approved motion moves the body (Offset/Turn on the root,
// Flare/Kick/Sweep/Row in the skin) and the body material (ScarletApparitions.fx attack/attack2/attack3) shows the
// attack inside the painted silhouette. Both are pure functions of the body's own notes, i.e. of each plan's
// Born/Fire/End; nothing here reads a beat grid. New pixels stay on the body or inside its recent silhouette: the
// Crown's drips fall in front of its own flags and its embers only rise, the Mantle's wake follows the hooks' true
// past poses, and nothing is added to the field (forecasts, ink and seals are drawn above the NPC layer).
// With no notes and no motion the Crown is today's picture (its batched sparks within 1/255). The Mantle's filament
// hook ribbons give way to the wake everywhere except the Final avatar (cut), which keeps its accepted composition.
internal static class ScarletApparitionRig
{
    private const int Columns = 48, Rows = 48, Segments = 36, SparkSlots = 32, DripSlots = 6;
    // Particles that leave the painted silhouette stay at or below the forecast band's median luminance (design
    // §2.0.3 rule 3, gate G3): the analytic motes' strengths are scaled on the way to the shader.
    private const float EmberGlow = .45f, IntakeGlow = .7f, SparkGlow = .34f;
    // The wake's past poses (ticks ago).
    private static readonly int[] WakeLags = { 0, 2, 4, 6, 8, 10, 13, 16 };
    private static readonly Texture2D?[] bodies = new Texture2D?[2];
    private static readonly VertexPositionColorTexture[] grid = new VertexPositionColorTexture[(Columns+1)*(Rows+1)];
    private static readonly VertexPositionColorTexture[] mesh = new VertexPositionColorTexture[Columns*Rows*6];
    private static readonly VertexPositionColorTexture[] strip = new VertexPositionColorTexture[Segments*6];
    private static readonly VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];
    private static readonly VertexPositionColorTexture[] sparks = new VertexPositionColorTexture[SparkSlots*6];
    private static readonly VertexPositionColorTexture[] drips = new VertexPositionColorTexture[DripSlots*6];
    private static readonly VertexPositionColorTexture[] wake = new VertexPositionColorTexture[4*(WakeLags.Length-1)*12];
    private static readonly Pose[] history = new Pose[WakeLags.Length];
    private static readonly Vector2[,] swept = new Vector2[WakeLags.Length,3];
    private static readonly Vector2[] hooks = { new(.08f,.13f), new(.88f,.08f), new(.11f,.73f), new(.80f,.80f) };
    // Measured on SableMantle.png's alpha / bone masks: each hook blade as heel, bend and edge, every chord on the
    // painted bone (0/1 the high row, 2/3 the low row). The wake is the surface the blade sweeps; sparks leave the edge.
    private static readonly Vector2[,] blades =
    {
        { new(.11f,.068f), new(.045f,.085f), new(.018f,.14f) }, { new(.925f,.05f), new(.88f,.013f), new(.845f,.025f) },
        { new(.09f,.76f), new(.125f,.825f), new(.155f,.836f) }, { new(.83f,.89f), new(.81f,.945f), new(.765f,.973f) },
    };
    // Measured on EmberCrown.png: the molten crown ring, the three long pendants (pendulum weights) and the red
    // tips their drips leave from, and the ember sources low on the flags and the central spike (each with painted
    // cloth or bone over the whole rise; the tattered hem tips and the slanted side legs have none), ordered so each
    // ember drifts inward (ScarletBodyMaterial.Ember: even indices drift to texture left, odd to the right); z scales
    // the drift so embers rise through the cloth and along the spike rather than off them.
    private static readonly Vector2 crownRing = new(.50f,.29f);
    private static readonly Vector2[] pendants = { new(.372f,.47f), new(.50f,.42f), new(.629f,.47f) };
    private static readonly Vector2[] pendantTips = { new(.372f,.53f), new(.50f,.452f), new(.629f,.53f) };
    private static readonly Vector3[] emberSources = { new(.80f,.84f,.6f), new(.17f,.79f,.6f), new(.50f,.76f,.3f), new(.27f,.71f,.6f),
        new(.90f,.92f,.6f), new(.22f,.76f,.4f), new(.87f,.84f,.6f), new(.50f,.80f,.3f), new(.77f,.77f,.6f) };

    // One pose at one tick: the signal and the approved motion, and where the root and tilt put the body.
    private readonly record struct Pose(Vector2 Root, float Tilt, float Charge, float Recoil, float Flare, float Kick, float Turn,
        float Sweep, float Row, float LagX, float LagY);

    internal static void Load()
    {
        if (Main.dedServ) return;
        bodies[0] = ModContent.Request<Texture2D>("Convergence/Assets/Textures/CrimsonFoundry/EmberCrown", AssetRequestMode.ImmediateLoad).Value;
        bodies[1] = ModContent.Request<Texture2D>("Convergence/Assets/Textures/CrimsonFoundry/SableMantle", AssetRequestMode.ImmediateLoad).Value;
    }
    internal static void Unload() => Array.Clear(bodies);

    // `notes` are the body's own notes (CrimsonRig.DrawEffigy's ScarletNotes.Collect). The analytic particles are born
    // from them; with a `motion` they also re-create the motion and the signal at past ticks (the Mantle's wake, its
    // lagging lower tendrils, sparks left where a hook was on Fire). Without notes every past tick keeps the current
    // motion and signal, which is today's behaviour.
    internal static void Draw(SpriteBatch batch, int species, Vector2 center, float height, float age,
        float charge, float recoil, float alpha, bool flip = false, float rotation = 0,
        float dissolve = 0, float melt = 0, Matrix? projection = null, bool local = false, float cut = 0,
        ReadOnlySpan<ScarletNote> notes = default,
        in Vfx.ScarletApparitionMotion motion = default, in Vfx.ScarletBodyState material = default)
    {
        if (Main.dedServ || species is <0 or >1 || bodies[species] is not { } texture || alpha <= .001f || dissolve >= 1) return;
        bool reduced = CrimsonVisuals.Reduced;
        var gesture = motion; var body = material; // `in` parameters cannot enter local functions
        bool replay = !notes.IsEmpty && !gesture.Equals(default(ScarletApparitionMotion));
        Vector2 anchor = center - (local ? Vector2.Zero : Main.screenPosition);
        float nominal = species == 0 ? 350 : 430, scale = height / nominal;
        var now = PoseAt(species, flip, age, age, anchor, rotation, scale, charge, recoil, gesture, replay, notes);
        var rest = now with { Flare = 0, Kick = 0, Turn = 0, Sweep = 0, Row = 0, LagX = 0, LagY = 0 };
        float pulse = MathF.Pow(.5f+.5f*MathF.Sin(age*.12f), 5);
        // Inside a note's window the heart's free pulse hands over to the strike (Reduced Effects keeps the free pulse
        // and only adds the halved strike).
        if (body.Active) pulse = MathF.Max(reduced ? pulse : pulse*(1-body.Engaged), body.Ignite);
        var shader = ShaderManager.GetShader("Convergence.ScarletApparitions");
        using var scope = new WorldGraphicsScope(batch);
        shader.TrySetParameter("uWorldViewProjection", projection ?? ScarletMaterials.WorldMatrix);
        shader.TrySetParameter("clock", age/60);
        shader.TrySetParameter("species", (float)species);
        shader.TrySetParameter("signal", new Vector4(charge,recoil,alpha,reduced?.48f:1));
        shader.TrySetParameter("ceremony", new Vector2(dissolve,melt));
        shader.TrySetParameter("cut",cut);
        // Rest values reproduce today's picture exactly (the effect is shared: always set them).
        shader.TrySetParameter("attack", body.Active ? new Vector4(body.Heat, body.Ignite, body.Front, body.Drain) : new Vector4(0, 0, -1, 0));
        shader.TrySetParameter("attack2", body.Active ? new Vector4(body.Lean, body.PourLimit, body.Run, body.Row) : new Vector4(0, 1, -1, 0));
        shader.TrySetParameter("attack3", body.Active ? new Vector2(body.Return, body.Swing) : Vector2.Zero);
        shader.SetTexture(texture,0,SamplerState.LinearClamp);
        shader.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value,1,SamplerState.LinearWrap);
        shader.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value,2,SamplerState.LinearWrap);

        if (species == 0)
        {
            // Crown is a venting censer: upright furnace plumes, not Choir wings. The vents ride the crown as it
            // opens and the plumes draw in with the wind-up; they never reach further than today's.
            float draw = 1-.25f*now.Flare;
            for (int k=0;k<(reduced?4:9);k++)
            {
                float side=(k-4)/4f;
                Vector2 a=new(.50f+side*.16f,.25f+MathF.Abs(side)*.08f);
                Vector2 d=a+new Vector2(side*(.13f+charge*.10f),-.21f-charge*.10f-recoil*.15f)*draw;
                d.X+=MathF.Sin(age*.052f+k*2.4f)*.045f;
                Vector2 vent=Follow(a),shift=vent-a;
                Ribbon(vent,vent+new Vector2(-side*.08f,-.09f),d+new Vector2(.035f*MathF.Sin(age*.04f+k),.11f)+shift,d+shift,
                    .032f+charge*.025f+recoil*.03f,k*1.73f,.72f,0);
            }
        }
        else if (cut >= .5f)
        {
            // The Final avatar keeps its accepted hook residues.
            for(int k=0;k<4;k++)
            {
                Vector2 hook=Skin(hooks[k],now,age),past=Skin(hooks[k],now,age-11),old=Skin(hooks[k],now,age-24);
                float side=k%2==0?-1:1;
                Vector2 tail=old+new Vector2(side*.12f,.12f+MathF.Sin(age*.022f+k)*.06f);
                Ribbon(hook,past,old,tail,.023f+charge*.025f+recoil*.05f,k+11,.65f,1);
                if(!reduced) Ribbon(hook,hook+new Vector2(side*.17f,-.08f),tail+new Vector2(side*.12f,.07f),tail,
                    .055f+charge*.018f,k+7,.24f,0);
            }
        }
        else
        {
            // Mantle wake: the surface each hook blade swept through its true past poses, a wine-dark haze behind the
            // body, only while the hooks travel (a still body leaves none). Inside its own past silhouettes. One draw.
            for (int j = 0; j < WakeLags.Length; j++)
                history[j] = j == 0 ? now : PoseAt(species, flip, age - WakeLags[j], age, anchor, rotation, scale, charge, recoil, gesture, replay, notes);
            int count = 0;
            for (int k = 0; k < 4; k++)
            {
                // Only the leading row leaves a wake (the trailing row's would stay below .1 opacity).
                float lead = Math.Clamp((k < 2 ? -now.Row : now.Row) * 1.3f, 0, 1);
                if (lead < (reduced ? .5f : .25f)) continue;
                float path = 0;
                for (int j = 0; j < WakeLags.Length; j++)
                {
                    for (int b = 0; b < 3; b++) swept[j, b] = Map(Skin(blades[k, b], history[j], age - WakeLags[j]), history[j]);
                    if (j > 0) path += Vector2.Distance(swept[j, 2], swept[j - 1, 2]);
                }
                float speed = Ease((path / (WakeLags[^1] * scale) - 1) / 3);
                float opacity = (.12f + .30f * lead) * (reduced ? .5f : 1) * speed * alpha * (1 - dissolve);
                if (opacity > .001f) count = Swept(opacity, reduced ? 0 : speed * (.4f + .6f * lead), k, count);
            }
            if (count > 0) { shader.Apply("WispPass"); Submit(wake, count); }
        }

        for(int y=0;y<=Rows;y++) for(int x=0;x<=Columns;x++)
        {
            Vector2 uv=new((float)x/Columns,(float)y/Rows);
            Vector2 at=Map(Skin(uv,now,age),now);
            grid[y*(Columns+1)+x]=new(new(at,0),Color.White,uv);
        }
        int n=0;
        for(int y=0;y<Rows;y++) for(int x=0;x<Columns;x++)
        {
            int a=y*(Columns+1)+x,b=a+1,c=a+Columns+1,d=c+1;
            mesh[n++]=grid[a];mesh[n++]=grid[b];mesh[n++]=grid[c];
            mesh[n++]=grid[b];mesh[n++]=grid[d];mesh[n++]=grid[c];
        }
        shader.Apply("AuraPass"); Submit(mesh,n);
        shader.Apply(); Submit(mesh,n);

        Vector2 heart=species==0?new(.5f,.30f):new(.527f,.428f);
        if(cut<.5f)
        {
            // The heart flares with the strike; the Crown's patch draws in with its wind-up.
            shader.TrySetParameter("shape",new Vector4(pulse,charge,recoil,body.Active?body.Ignite:0));
            float size=species==0?.24f*(1-.10f*(body.Active?body.Wind:0)):.17f;
            Patch(Skin(heart,now,age),new Vector2(size),"HeartPass");
        }
        for(int k=0;k<4;k++)
        {
            Vector2 tip=species==0?Follow(new(.28f+k*.15f,.12f+MathF.Sin(k*3)*.05f)):Skin(hooks[k],now,age);
            Vector2 start=Skin(heart,now,age),delta=tip-start;
            Ribbon(start,start+new Vector2(delta.X*.32f,delta.Y*.1f),tip-delta*.17f,tip,
                .008f+charge*.011f+recoil*.017f,k*3.7f,.28f+charge*.35f+recoil*.65f,1);
        }

        // Crown drips: molten wine-dark blood beads slide off the pendants in front of the flags (one draw).
        if (species == 0 && body.Active && !reduced)
        {
            int count = 0;
            foreach (var note in notes)
            {
                int drops = ScarletBodyMaterial.DripCount(note, reduced);
                float side = note.Side * (flip ? -1 : 1);
                for (int i = 0; i < drops && count < DripSlots * 6; i++)
                {
                    if (!ScarletBodyMaterial.Drip(note, i, age, out var mote)) continue;
                    var tip = pendantTips[drops == 1 ? side >= 0 ? 2 : 0 : i % 3];
                    count = Drip(Skin(tip, now, age), mote, i, count);
                }
            }
            if (count > 0) { shader.Apply("DripPass"); Submit(drips, count); }
        }

        // Sparks: today's free sparks (handed to the attack clock inside a note's window), the Crown's embers rising
        // from its hems and feet after Fire and its intake drawn into the crown ring during the warning, the Mantle's
        // sparks thrown off the leading hooks on Fire. All of them in one draw.
        if(!reduced)
        {
            int count = 0;
            float idle = 1 - (body.Active ? body.Engaged : 0);
            for(int k=0;k<16;k++)
            {
                float life=(age*(.004f+k%3*.001f)+k*.618034f)%1;
                float side=k%2==0?-1:1;
                Vector2 at=species==0?new(.50f+side*(.12f+life*.28f),.26f-life*.51f)
                    :Vector2.Lerp(Skin(heart,now,age),Skin(hooks[k%4],now,age),life);
                at+=new Vector2(MathF.Sin(k*3.4f+life*5)*.028f,MathF.Sin(life*8+k)*.025f);
                float brightness=MathF.Sin(life*MathF.PI)*(.3f+charge*.7f)*idle;
                if(brightness>.001f) count=Spark(at,new(.006f,.019f),brightness,count);
            }
            if (body.Active)
                foreach (var note in notes)
                {
                    if (species == 0)
                    {
                        for (int i = 0; i < ScarletBodyMaterial.EmberCount(note, reduced); i++)
                            if (ScarletBodyMaterial.Ember(note, i, age, out var mote))
                            {
                                var source = emberSources[i % emberSources.Length];
                                count = Spark(Skin(new(source.X, source.Y), now, age) + new Vector2(mote.X * source.Z, mote.Y),
                                    new Vector2(.005f, .014f) * mote.Size, mote.Alpha * EmberGlow, count);
                            }
                        for (int i = 0; i < ScarletBodyMaterial.IntakeCountOf(note, reduced); i++)
                            if (ScarletBodyMaterial.Intake(note, i, age, out var mote))
                                count = Spark(Skin(crownRing, now, age) + new Vector2(mote.X, mote.Y), new Vector2(.005f, .009f) * mote.Size,
                                    mote.Alpha * IntakeGlow, count);
                    }
                    else if (ScarletBodyMaterial.SparkCount(note, reduced) is int thrown and > 0 && age >= note.Fire
                        && age < note.Fire + ScarletBodyMaterial.SparkTicks)
                    {
                        var struck = PoseAt(species, flip, note.Fire, age, anchor, rotation, scale, charge, recoil, gesture, replay, notes);
                        var after = PoseAt(species, flip, note.Fire + 2, age, anchor, rotation, scale, charge, recoil, gesture, replay, notes);
                        for (int i = 0; i < thrown; i++)
                        {
                            int k = (note.Low ? 2 : 0) + (i & 1);
                            Vector2 from = Map(Skin(blades[k, 2], struck, note.Fire), struck);
                            Vector2 swing = Map(Skin(blades[k, 2], after, note.Fire + 2), after) - from;
                            // Thrown along the whip; a hook that hardly moves lets them fall back toward the body.
                            if (swing.LengthSquared() < 4 * scale * scale) swing = Map(Skin(new(.5f, .45f), struck, note.Fire), struck) - from;
                            if (ScarletBodyMaterial.Spark(note, i, age, swing.X, swing.Y, out var mote))
                                count = Streak(from + new Vector2(mote.X, mote.Y) * scale, new Vector2(mote.X, mote.Y), mote, count);
                        }
                    }
                }
            if (count > 0) { shader.TrySetParameter("shape", new Vector4(1, 0, 0, 0)); shader.Apply("SparkPass"); Submit(sparks, count); }
        }

        Vector2 Skin(Vector2 uv,in Pose pose,float t)
        {
            Vector2 pivot=new(.5f,.39f),p=uv-pivot;
            float lateral=Ease((MathF.Abs(p.X)-.05f)/.2f),side=p.X<0?-1:1;
            if(species==0)
            {
                float top=1-Ease((uv.Y-.40f)/.13f);
                float gape=(pose.Charge*.055f-pose.Recoil*.085f+MathF.Sin(t*.027f)*.012f+pose.Flare*.05f)*top;
                p=Rotate(p,side*gape*lateral);
                p.X+=side*(pose.Charge*.037f+pose.Recoil*.034f+pose.Flare*.02f)*lateral*top;
                float cloth=Ease((uv.Y-.43f)/.38f);
                p.X+=MathF.Sin(t*.030f-uv.Y*15+uv.X*6)*.027f*cloth;
                p.Y+=MathF.Sin(t*.039f+uv.X*8)*.016f*cloth;
                // The pour kicks the cloth out and up; the pendants swing behind the censer like weights.
                p.X+=side*pose.Kick*.035f*cloth*lateral;
                p.Y-=pose.Kick*.02f*cloth;
                float pend=0;
                foreach(var c in pendants) pend+=MathF.Exp(-Vector2.DistanceSquared(uv,c)/(.05f*.05f));
                p.X-=pend*pose.Turn*(flip?-1:1)*.04f;
                p.Y+=pend*pose.Kick*.012f;
            }
            else
            {
                float lower=Ease((uv.Y-.44f)/.18f);
                float lag=lower*.8f+(side<0?.6f:0);
                float move=MathF.Sin(t*.033f-lag)*.085f;
                // Hooks brace quickly, suspend briefly, then whip on discharge.
                float angle=side*(move+pose.Charge*.19f-pose.Recoil*.36f)*(1-lower*1.7f)*lateral;
                // The approved sweep through the leading row (texture space: a mirrored body sweeps the other way).
                float lead=pose.Row>0?lower*pose.Row:(1-lower)*-pose.Row;
                angle+=(flip?-pose.Sweep:pose.Sweep)*(.10f+.30f*lead)*lateral;
                p=Rotate(p,angle);
                p+=new Vector2(MathF.Sin(t*.042f-uv.Y*9+side)*.030f,MathF.Sin(t*.028f+uv.X*11)*.028f)*lateral;
                p*=new Vector2(1+pose.Charge*.055f-pose.Recoil*.055f,1-pose.Recoil*.04f);
                // The lower tendrils trail the body's travel like cloth.
                p+=new Vector2(pose.LagX,pose.LagY)*lower;
            }
            p.X*=1-melt*uv.Y*.7f;p.Y+=melt*uv.Y*uv.Y*.6f;
            return pivot+p;
        }
        // A fixed point of the art carried by the motion only (with no motion it stays exactly where it is today).
        Vector2 Follow(Vector2 uv)=>uv+(Skin(uv,now,age)-Skin(uv,rest,age));
        Vector2 Map(Vector2 uv,in Pose pose)
        {
            Vector2 p=(uv-new Vector2(.5f))*height;
            if(flip)p.X=-p.X;
            return pose.Root+Rotate(p,pose.Tilt);
        }
        void Ribbon(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float radius,float seed,float opacity,float filament)
        {
            if(opacity*(1-dissolve)<=.001f) return;
            shader.TrySetParameter("shape",new Vector4(seed,opacity*(1-dissolve),filament,0));
            int count=0;
            for(int k=0;k<Segments;k++)
            {
                float t=(float)k/Segments,next=(float)(k+1)/Segments;
                var a0=V(t,-1);var a1=V(t,1);var b0=V(next,-1);var b1=V(next,1);
                strip[count++]=a0;strip[count++]=a1;strip[count++]=b0;strip[count++]=a1;strip[count++]=b1;strip[count++]=b0;
            }
            shader.Apply("RibbonPass");Submit(strip,count);
            VertexPositionColorTexture V(float t,float side)
            {
                float s=1-t;
                Vector2 at=s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d;
                Vector2 tangent=3*s*s*(b-a)+6*s*t*(c-b)+3*t*t*(d-c);
                if(tangent.LengthSquared()<.000001f)tangent=Vector2.UnitY;
                tangent.Normalize();
                Vector2 normal=new(-tangent.Y,tangent.X);
                float taper=(.12f+.88f*MathF.Sin(t*MathF.PI))*(1-Ease((t-.83f)/.17f));
                return new(new(Map(at+normal*radius*side*taper,now),0),Color.White,new(t,(side+1)*.5f));
            }
        }
        void Patch(Vector2 p,Vector2 radius,string pass)
        {
            var a=new VertexPositionColorTexture(new(Map(p-radius,now),0),Color.White,new(0,0));
            var b=new VertexPositionColorTexture(new(Map(p+new Vector2(radius.X,-radius.Y),now),0),Color.White,new(1,0));
            var c=new VertexPositionColorTexture(new(Map(p+new Vector2(-radius.X,radius.Y),now),0),Color.White,new(0,1));
            var d=new VertexPositionColorTexture(new(Map(p+radius,now),0),Color.White,new(1,1));
            quad[0]=a;quad[1]=b;quad[2]=c;quad[3]=b;quad[4]=d;quad[5]=c;
            shader.Apply(pass);Submit(quad,6);
        }
        // A spark quad in texture space (today's Patch layout), its brightness in the vertex colour (16 bits: a, g).
        int Spark(Vector2 p,Vector2 radius,float brightness,int count)
        {
            if(count>=sparks.Length) return count;
            Color c=Encode(brightness);
            var a=new VertexPositionColorTexture(new(Map(p-radius,now),0),c,new(0,0));
            var b=new VertexPositionColorTexture(new(Map(p+new Vector2(radius.X,-radius.Y),now),0),c,new(1,0));
            var e=new VertexPositionColorTexture(new(Map(p+new Vector2(-radius.X,radius.Y),now),0),c,new(0,1));
            var d=new VertexPositionColorTexture(new(Map(p+radius,now),0),c,new(1,1));
            sparks[count++]=a;sparks[count++]=b;sparks[count++]=e;sparks[count++]=b;sparks[count++]=d;sparks[count++]=e;
            return count;
        }
        // A spark in screen space, streaked along its flight.
        int Streak(Vector2 at,Vector2 flight,in ScarletMote mote,int count)
        {
            if(count>=sparks.Length) return count;
            if(flight.LengthSquared()<1e-6f) flight=Vector2.UnitY;
            flight.Normalize();
            Vector2 along=flight*(3.5f+2.5f*(1-mote.Life))*mote.Size*scale,across=new Vector2(-flight.Y,flight.X)*1.3f*mote.Size*scale;
            Color c=Encode(mote.Alpha*SparkGlow);
            sparks[count++]=new(new(at-along-across,0),c,new(0,0));sparks[count++]=new(new(at-along+across,0),c,new(1,0));
            sparks[count++]=new(new(at+along-across,0),c,new(0,1));sparks[count++]=new(new(at-along+across,0),c,new(1,0));
            sparks[count++]=new(new(at+along+across,0),c,new(1,1));sparks[count++]=new(new(at+along-across,0),c,new(0,1));
            return count;
        }
        // A drip hanging from `tip` (skinned texture space): a bead `mote.Y` below it on a neck back to the pendant.
        int Drip(Vector2 tip,in ScarletMote mote,int index,int count)
        {
            const float half=.0075f;
            float top=tip.Y-.004f,bottom=tip.Y+mote.Y+half*2.2f;
            float length=(bottom-top)/half;
            var c=new Color(Math.Clamp(mote.Life,0,1),Math.Clamp(length/32,0,1),index/3f,Math.Clamp(mote.Alpha,0,1));
            Vector2 a=new(tip.X-half,top),b=new(tip.X+half,top),e=new(tip.X-half,bottom),d=new(tip.X+half,bottom);
            drips[count++]=new(new(Map(a,now),0),c,new(0,0));drips[count++]=new(new(Map(b,now),0),c,new(1,0));
            drips[count++]=new(new(Map(e,now),0),c,new(0,1));drips[count++]=new(new(Map(b,now),0),c,new(1,0));
            drips[count++]=new(new(Map(d,now),0),c,new(1,1));drips[count++]=new(new(Map(e,now),0),c,new(0,1));
            return count;
        }
        // One hook's wake: the surface between its blade samples (heel, bend, edge) over the lags (0 = now), fading
        // with age.
        int Swept(float opacity,float specks,int seed,int count)
        {
            int last=WakeLags.Length-1;
            for(int j=1;j<=last;j++)
            {
                float u0=(float)(j-1)/last,u1=(float)j/last;
                var c0=new Color(Math.Clamp(specks,0,1),seed/8f,0f,Math.Clamp(opacity*MathF.Pow(1-u0,.9f),0,1));
                var c1=new Color(Math.Clamp(specks,0,1),seed/8f,0f,Math.Clamp(opacity*MathF.Pow(1-u1,.9f),0,1));
                for(int b=0;b<2;b++)
                {
                    var a0=new VertexPositionColorTexture(new(swept[j-1,b],0),c0,new(u0,b*.5f));
                    var a1=new VertexPositionColorTexture(new(swept[j-1,b+1],0),c0,new(u0,b*.5f+.5f));
                    var b0=new VertexPositionColorTexture(new(swept[j,b],0),c1,new(u1,b*.5f));
                    var b1=new VertexPositionColorTexture(new(swept[j,b+1],0),c1,new(u1,b*.5f+.5f));
                    wake[count++]=a0;wake[count++]=a1;wake[count++]=b0;
                    wake[count++]=a1;wake[count++]=b1;wake[count++]=b0;
                }
            }
            return count;
        }
    }

    // The body's pose at tick t (`age` is now). A replayed body re-evaluates the approved motion and the signal from
    // its notes; otherwise the past keeps the current ones.
    private static Pose PoseAt(int species, bool flip, float t, float age, Vector2 anchor, float rotation, float scale,
        float charge, float recoil, in ScarletApparitionMotion current, bool replay, ReadOnlySpan<ScarletNote> notes)
    {
        bool past = t != age;
        var m = past && replay ? Motion(species, t, notes) : current;
        if (past && !notes.IsEmpty) (charge, recoil) = Signal(t, notes);
        float lagX = 0, lagY = 0;
        if (species == 1 && replay)
        {
            // The Mantle's lower tendrils follow its travel four ticks late (texture space, px at the nominal 430).
            var before = Motion(species, t - 4, notes);
            lagX = (m.OffsetX - before.OffsetX) * -.6f / 430 * (flip ? -1 : 1);
            lagY = (m.OffsetY - before.OffsetY) * -.6f / 430;
        }
        Vector2 root = anchor + new Vector2(m.OffsetX, m.OffsetY) * scale;
        float tilt = rotation + MathF.Sin(t*.017f + species)*.027f + m.Turn;
        return new(root, tilt, charge, recoil, m.Flare, m.Kick, m.Turn, m.Sweep, m.Row, lagX, lagY);
    }

    private static ScarletApparitionMotion Motion(int species, float t, ReadOnlySpan<ScarletNote> notes)
        => species == 0 ? ScarletGestureMotion.Crown(t, notes) : ScarletGestureMotion.Mantle(t, notes);

    // CrimsonRig.Signal over the body's own notes (the notes hold every plan of the source that still matters).
    private static (float Charge, float Recoil) Signal(float t, ReadOnlySpan<ScarletNote> notes)
    {
        float until = 60, since = 100;
        foreach (var n in notes)
        {
            if (t < n.Born) continue;
            float delta = n.Fire - t;
            if (delta >= 0) until = Math.Min(until, delta); else since = Math.Min(since, -delta);
        }
        return (CrimsonRigMotion.Charge(until), CrimsonRigMotion.Recoil(since));
    }

    // Brightness 0..1 in 16 bits: a = the high byte, g = the low one (SparkPass reads a + g / 255).
    private static Color Encode(float brightness)
    {
        float scaled = Math.Clamp(brightness, 0, 1) * 255;
        int high = (int)scaled, low = Math.Min(255, (int)MathF.Round((scaled - high) * 255));
        return new Color(0, low, 0, high);
    }
    private static float Ease(float x) { x=Math.Clamp(x,0,1);return x*x*(3-2*x); }
    private static Vector2 Rotate(Vector2 p,float a)=>new(p.X*MathF.Cos(a)-p.Y*MathF.Sin(a),p.X*MathF.Sin(a)+p.Y*MathF.Cos(a));
    private static void Submit(VertexPositionColorTexture[] v,int n)=>Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList,v,0,n/3);
}
