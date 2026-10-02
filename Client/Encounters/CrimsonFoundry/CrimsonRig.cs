using ScarletGraphicsScope = Convergence.Client.Graphics.WorldGraphicsScope;
#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Original artwork with species-specific articulation and accepted gesture paths.
internal static partial class CrimsonRig
{
    private static readonly Texture2D?[] effigies = new Texture2D?[3];
    private static readonly int[] partOrder = { 3, 4, 0, 1, 2 };
    private static readonly int[] singlePart = { 0 };
    private const int Columns = 24, Rows = 32;
    private static readonly VertexPositionColorTexture[] mesh = new VertexPositionColorTexture[Columns * Rows * 6];
    internal static void Load()
    {
        if (Main.dedServ) return;
        LoadPerformer();
        string[] names = { "EmberCrown", "SableMantle" };
        for (int i = 0; i < names.Length; i++) effigies[i] = LoadTexture(names[i]);
        CrimsonChoirRig.Load();
        ScarletApparitionRig.Load();
        // Direct FNA effect construction belongs to drawing, not this loader hook.
    }
    internal static void Unload()
    {
        performer = null; Array.Clear(effigies);
        CrimsonChoirRig.Unload();
        ScarletApparitionRig.Unload();
        ScarletMaterials.Reset();
        ScarletCueFrame.Reset();
    }
    // The plan-list forms (CrimsonRig.Performer.cs, ScarletNotes) are shared with the offline preview; the boss
    // forms read the frame's one scan instead of scanning Main.ActiveProjectiles per request.
    internal static (float Charge, float Recoil) Signal(CrimsonBoss boss, int source, float age)
    {
        var frame = ScarletCueFrame.Of(boss);
        return Signal(frame.Gestures, frame.Choruses, source, age);
    }
    internal static bool Draw(CrimsonBoss boss, SpriteBatch batch, Vector2 screen)
    {
        float age = CrimsonVisuals.RenderAge(boss);
        var signal = Signal(boss, -1, age);
        float ending = boss.State.Stage == CrimsonStage.Defeat ? .35f : 1;
        Vector2 at = boss.NPC.Center;
        float opening=CrimsonChoreography.OpeningAge(age,boss.State.MusicStart);
        float reveal=CrimsonChoreography.Reveal(opening);
        if(reveal<1 || CrimsonChoreography.Seal(opening)>0) DrawInvocation(batch,boss,age,opening,ending);
        if (boss.State.Stage == CrimsonStage.Victory)
        { ScarletInvocationScene.Victory(batch, boss.State, age, CrimsonVisuals.EndingElapsed(boss)); return false; }
        ScarletInvocationScene.Draw(batch, boss.State, age, ending);
        float consumed = boss.State.Phase == 3 ? CrimsonEnsemble.ConductorAbsorption(age - boss.State.PhaseStart) : 0;
        DrawPerformer(batch, screen, at, age, boss.NPC.velocity, boss.NPC.spriteDirection,
            true, signal.Charge, signal.Recoil, ending*reveal*(1-consumed), false, reveal*(1-consumed));
        {
            Vector2 waiting = new(MathF.Sin(age*.022f)*8,-20+MathF.Sin(age*.031f)*11);
            Vector2 held = new(boss.NPC.spriteDirection*(94+signal.Charge*12),-25);
            Vector2 orb=at+Vector2.Lerp(waiting,held,reveal);
            if(boss.State.Phase==3) orb=Vector2.Lerp(orb,at+new Vector2(0,-20),CrimsonInvocation.Ease((age-boss.State.PhaseStart)/48));
            float radius=MathHelper.Lerp(64+MathF.Sin(age*.038f)*5,53+signal.Charge*24+signal.Recoil*18,reveal);
            CrimsonEnergy.Begin();CrimsonEnergy.AddCore(orb,radius,age,Math.Max(signal.Charge,reveal*(1-reveal)*3),signal.Recoil,ending*(1-consumed),CrimsonVisuals.Reduced);
            CrimsonEnergy.Draw(batch);
        }
        return false;
    }
    private static void DrawInvocation(SpriteBatch batch,CrimsonBoss boss,float age,float opening,float alpha)
    {
        float seal=CrimsonChoreography.Seal(opening);
        if(seal<=.001f)return;
        var point=CrimsonChoreography.SummoningGate(boss.State.Field);Vector2 gate=new(point.X,point.Y);
        float load=CrimsonInvocation.Ease((opening-230)/190);
        float tension=CrimsonInvocation.Ease((opening-400)/90);
        float release=MathF.Exp(-Math.Max(0,opening-(CrimsonChoreography.SummonAt+CrimsonInvocation.MusicLeadTicks))/22);
        float radius=150+load*240+tension*60;
        ScarletSorcery.Seal(batch,gate,radius,.82f,-.14f,age,load,seal*alpha,false);
        ScarletSorcery.Seal(batch,gate,radius*.82f,.82f,.24f,age*.65f,load,seal*.58f*alpha,false,3);
        DrawPressure(batch,gate,0,age,tension,opening>520?release:0);
        var glow=MiscTexturesRegistry.BloomCircleSmall.Value;
        int count=CrimsonVisuals.Reduced?8:28;
        for(int i=0;i<count;i++) {
            float t=(opening*.007f+i*.618f)%1;
            Vector2 at=Vector2.Lerp(boss.NPC.Center,gate,t)+new Vector2(MathF.Sin(t*MathF.PI)*MathF.Sin(i*2.4f)*90,0);
            batch.Draw(glow,at-Main.screenPosition,null,new Color(255,64,98,0)*seal*MathF.Sin(t*MathF.PI)*.7f,0,glow.Size()*.5f,(3+t*4)/glow.Width,SpriteEffects.None,0);
        }
    }
    internal static bool DrawEffigy(CrimsonEffigy effigy, SpriteBatch batch, Vector2 screen)
    {
        if (effigy.State.Index >= 3 || !effigy.TryBoss(out var boss)) return false;
        Texture2D? texture = effigies[effigy.State.Index];
        if (effigy.State.Index != 2 && texture is null) return false;
        float age = CrimsonVisuals.RenderAge(boss!), born = age - effigy.State.Born;
        // Final re-summoned sacrifices are one clock-driven composition. Do not
        // also draw their retained native actors at stale offstage positions.
        if (boss!.State.Phase == 3) return false;
        float appear = boss!.State.Phase is 1 or 2 && effigy.State.Index == boss.State.Phase
            ? CrimsonEnsemble.Emergence(age - boss.State.PhaseStart, false) : CrimsonInvocation.Ease(born / 62);
        float snap = MathF.Exp(-Math.Max(0, born - 12) / 14);
        var frame = ScarletCueFrame.Of(boss!);
        var signal = Signal(frame.Gestures, frame.Choruses, effigy.State.Index, age);
        float size = effigy.State.Index switch { 0 => 350, 1 => 430, _ => 490 };
        float alpha = boss!.State.Presence(effigy.State.Index, age);
        if (boss.State.Stage is CrimsonStage.Victory or CrimsonStage.Defeat) alpha *= .35f;
        if (alpha <= .001f) return false;
        Vector2 at = effigy.NPC.Center;
        if (boss.State.Phase != 3 && CrimsonGesture.TryPose(boss, effigy.State.Index, age, out var pose))
        {
            at = CrimsonGestureVisuals.V(pose.Body(age));
            if (texture is not null && pose.Technique == CrimsonTechnique.MantleRush && pose.Live(age) && !CrimsonVisuals.Reduced)
                for (int k = 3; k >= 1; k--)
                    Mesh(batch, texture, texture.Bounds, CrimsonGestureVisuals.V(pose.Body(age - k * .35f)) - screen,
                        texture.Size() * .5f, size / texture.Height, age, 14, signal.Charge, signal.Recoil,
                        new Color(142, 98, 159) * (alpha * .09f), effigy.NPC.spriteDirection < 0, effigy.NPC.rotation, true, effigy.State.Index);
        }
        float dissolving = effigy.State.Index == boss.State.Phase - 1 ? CrimsonEnsemble.RetreatDissolve(age - boss.State.PhaseStart) : 0;
        bool flipped = effigy.NPC.spriteDirection < 0, reduced = CrimsonVisuals.Reduced;
        // The body's own notes (a local participant only), aimed from Vespera; empty = today's picture.
        Span<ScarletNote> notes = stackalloc ScarletNote[ScarletNotes.Capacity];
        int noted = frame.Participant ? ScarletNotes.Collect(frame.Gestures, effigy.State.Index, age, flipped,
            boss.NPC.Center.X, boss.NPC.Center.Y, notes) : 0;
        if (effigy.State.Index == 2)
        {
            Span<CrimsonChoirCue> cues = stackalloc CrimsonChoirCue[16];
            int count = ScarletNotes.ChoirCues(frame.Gestures, age, flipped, cues);
            var choir = ScarletBodyMaterial.Choir(age, notes[..noted], reduced);
            CrimsonChoirRig.Draw(batch, at, size * (.90f + appear * .1f), age,
                signal.Charge, signal.Recoil, appear * alpha, flipped,
                effigy.NPC.rotation + signal.Recoil * .045f, dissolving, cues: cues[..count],
                heave: ScarletGestureMotion.Heave, material: choir);
            return false;
        }
        var motion = effigy.State.Index == 0 ? ScarletGestureMotion.Crown(age, notes[..noted]) : ScarletGestureMotion.Mantle(age, notes[..noted]);
        var body = ScarletBodyMaterial.Apparition(age, notes[..noted], motion, flipped, reduced);
        ScarletApparitionRig.Draw(batch, effigy.State.Index, at, size * (.90f + appear * .1f), age,
            signal.Charge, signal.Recoil, appear * alpha, flipped,
            effigy.NPC.rotation, dissolving, motion: motion, material: body);
        return false;
    }
    internal static int ChoirCues(CrimsonBoss boss, float age, Span<CrimsonChoirCue> cues, bool final = false, bool flipped = false)
        => ScarletNotes.ChoirCues(ScarletCueFrame.Of(boss).Gestures, age, flipped, cues, final);
    internal static void DrawEnsemble(SpriteBatch batch, Vector2 center, float age, float emergence, float alpha, float dissolve = 0, float melt = 0)
    {
        ScarletAvatarTarget.Draw(batch,center,age,emergence,alpha,dissolve,melt);
    }
    private static void Mesh(SpriteBatch batch, Texture2D texture, Rectangle source, Vector2 center, Vector2 pivot,
        float scale, float time, float motion, float charge, float recoil, Color tint, bool flip, float rotation, bool apparition, int species = -1, int[]? selectedParts = null, float dissolve = 0, float melt = 0)
    {
        if (Main.dedServ || tint.A == 0) return;
        var material = ShaderManager.GetShader("Convergence.ScarletSurface");
        using var scope = new ScarletGraphicsScope(batch);
        material.TrySetParameter("uWorldViewProjection", ScarletMaterials.WorldMatrix);
        material.TrySetParameter("clock", time / 60);
        material.TrySetParameter("ceremony", new Vector2(dissolve, melt));
        material.TrySetParameter("species", apparition ? (float)species : 3f);
        material.TrySetParameter("signal", new Vector4(charge, recoil, 1, CrimsonVisuals.Reduced ? 1 : 0));
        material.TrySetParameter("texel", new Vector2(1f / texture.Width, 1f / texture.Height));
        material.TrySetParameter("region", new Vector4(-source.X / (float)source.Width, -source.Y / (float)source.Height,
            texture.Width / (float)source.Width, texture.Height / (float)source.Height));
        material.TrySetParameter("frameSpan", new Vector2(source.Width / (float)texture.Width, source.Height / (float)texture.Height));
        material.SetTexture(texture, 0, apparition ? SamplerState.LinearClamp : SamplerState.PointClamp);
        material.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
        material.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 2, SamplerState.LinearWrap);
        foreach (int part in selectedParts ?? (apparition ? partOrder : singlePart))
        {
            var pose = ScarletArticulation.Part(apparition ? species : 3, part, time, charge, recoil);
            material.TrySetParameter("part", (float)part);
            int offset = 0;
            for (int y = 0; y < Rows; y++) for (int x = 0; x < Columns; x++)
            {
                var a = Vertex(x, y); var b = Vertex(x + 1, y); var c = Vertex(x, y + 1); var d = Vertex(x + 1, y + 1);
                mesh[offset++] = a; mesh[offset++] = b; mesh[offset++] = c;
                mesh[offset++] = b; mesh[offset++] = d; mesh[offset++] = c;
            }
            material.Apply(); Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, mesh, 0, offset / 3);
            VertexPositionColorTexture Vertex(int x, int y)
            {
                float u = x / (float)Columns, v = y / (float)Rows;
                Vector2 local = (new Vector2(u * source.Width, v * source.Height) - pivot) * scale;
                float flexible = apparition && part != 0 ? MathF.Abs(u - .5f) * v : 0;
                float secondary = CrimsonVisuals.Reduced ? 0 : motion;
                local.X += MathF.Sin(time * .037f - v * 7 + u * 2) * secondary * flexible;
                local.Y += MathF.Sin(time * .028f + u * 8 - v * 3) * secondary * flexible * .5f;
                float side = part is 1 or 3 ? -1 : 1;
                Vector2 joint = new(side * source.Width * scale * .08f, source.Height * scale * (part >= 3 ? .02f : -.12f));
                local = ((local - joint) * pose.Scale).RotatedBy(pose.Rotation) + joint + pose.Offset;
                if(apparition) local *= species switch { 0 => new Vector2(1.18f,.88f), 1 => new Vector2(1.3f,.94f), _ => new Vector2(.86f,1.10f) };
                local.X *= 1 - melt * v * .72f;
                local.Y += melt * v*v*v * source.Height * scale * (.4f + .25f*MathF.Sin(u*13+time*.018f));
                if (flip) local.X = -local.X;
                Vector2 pos = center + local.RotatedBy(rotation);
                return new(new Vector3(pos, 0), tint, new((source.X + u * source.Width) / texture.Width, (source.Y + v * source.Height) / texture.Height));
            }
        }
    }
}
