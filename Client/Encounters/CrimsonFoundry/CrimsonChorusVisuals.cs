using ScarletGraphicsScope = Convergence.Client.Graphics.WorldGraphicsScope;
#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonChorusVisuals : ModSystem
{
    private Guid fight;
    private int previous = -1;
    private readonly HashSet<(int Serial, bool Impact)> heard = new();
    // Summon and verdict cues ring for their whole length; a Fight end fades them.
    private readonly ScarletVoices voices = new(8);
    private readonly List<Vector2> ring = new(65);
    private readonly Vector2[] arrow = new Vector2[3];
    private static bool Audience(CrimsonBoss? boss) => !Main.dedServ && !Main.gameMenu && boss is not null && boss.Fresh
        && Array.Exists(boss.State.Members, m => m.Slot == Main.myPlayer);
    public override void PostUpdateEverything()
    {
        voices.Update();
        var boss = CrimsonPackets.Boss;
        if (!Audience(boss)) { Reset(); return; }
        int age = (int)boss!.VisualAge;
        if (fight != boss.State.Fight) { Reset(); fight = boss.State.Fight; previous = age - 1; }
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not CrimsonChorus marker || !CrimsonChorus.TryBoss(marker.Plan, out var owner, marker.Resolved) || owner != boss) continue;
            var p = marker.Plan;
            Cue(p.Born, false); Cue(p.Fire, true);
            void Cue(int tick, bool impact)
            {
                if (impact ? !marker.Resolved || age < tick || age - tick > CrimsonChorusImpactPositions.TailTicks
                    : previous >= tick || age < tick || age - tick > 3) return;
                if (!heard.Add((p.Serial, impact))) return;
                // Summon on the call; the verdict cue follows the committed failure mask.
                bool failed = impact && marker.FailedMask != 0;
                voices.Play(p.Kind == CrimsonChorusKind.Stack
                    ? impact ? failed ? ScarletCue.StackFail : ScarletCue.StackSuccess : ScarletCue.StackSummon
                    : impact ? failed ? ScarletCue.SpreadFail : ScarletCue.SpreadSuccess : ScarletCue.SpreadSummon);
                if (impact)
                    CrimsonPackets.Log($"event=ChorusPresentation fight={p.Fight} serial={p.Serial} kind={p.Kind} failed_mask={marker.FailedMask} verdict_delay_ticks={age - p.Fire} result_positions={marker.ImpactPositions.Length} observer={Main.myPlayer}");
                if (impact && marker.Resolved && marker.FailedMask != 0) ScarletArticulation.Impact(3,2,boss.NPC.Center);
            }
            if (heard.Count > 32) heard.RemoveWhere(x => x.Serial < p.Serial - 2);
        }
        previous = age;
    }
    public override void PostDrawTiles()
    {
        // Reward black blood lies beneath the Raid's chorus markers (frame-stamped: drawn once, by whoever is first).
        ScarletRewardInk.DrawWorld();
        var boss = CrimsonPackets.Boss;
        if (!Audience(boss)) return;
        float age = CrimsonVisuals.RenderAge(boss!);
        var batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            DrawSorcery(boss!,batch,age);
            using var scope = new ScarletGraphicsScope(batch);
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.ModProjectile is not CrimsonChorus marker || !CrimsonChorus.TryBoss(marker.Plan, out var owner) || owner != boss) continue;
                var plan = marker.Plan;
                if (age < plan.Born || age >= plan.Fire + 22) continue;
                float progress = Math.Clamp((age - plan.Born) / (plan.Fire - plan.Born), 0, 1);
                float alpha = age < plan.Fire ? Math.Min(1, (age - plan.Born + 1) / 8)
                    : 1 - CrimsonInvocation.Ease((age - plan.Fire) / 22);
                float pulse = CrimsonMeter.Pulse(Math.Max(0, age - boss!.State.MusicStart));
                if (plan.Kind == CrimsonChorusKind.Stack) DrawMarker(plan.Center, CrimsonChorusRules.StackRadius, true);
                else for (int i = 0; i < boss!.State.Members.Length; i++)
                {
                    var member = boss.State.Members[i]; var player = Main.player[member.Slot];
                    if ((plan.Members & 1 << i) == 0 || member.Out || member.Recovery.Downed || !player.active || player.dead || player.ghost) continue;
                    DrawMarker(new(player.Center.X, player.Center.Y), CrimsonChorusRules.SpreadRadius, false);
                }
                void DrawMarker(CrimsonPoint center, float radius, bool inward)
                {
                    int species = inward ? 0 : 2;
                    DrawRing(center, radius, 0, MathF.Tau, 2.5f, alpha, species);
                    DrawRing(center, radius + 12, -MathF.PI / 2, MathF.Tau * (1 - progress), 3.5f, alpha, species);
                    int arrows = inward ? 4 : 6;
                    for (int i = 0; i < arrows; i++)
                    {
                        var direction = CrimsonPoint.Polar(i * MathF.Tau / arrows, 1);
                        var normal = new CrimsonPoint(-direction.Y, direction.X);
                        var at = center + direction * (radius + 23 + pulse * 8);
                        arrow[0] = CrimsonGestureVisuals.V(at + normal * 8);
                        arrow[1] = CrimsonGestureVisuals.V(at + direction * (inward ? -13 : 13));
                        arrow[2] = CrimsonGestureVisuals.V(at - normal * 8);
                        ScarletMaterials.Path(arrow, 2, species, age, alpha);
                    }
                    if (age >= plan.Fire)
                        DrawRing(center, radius * (1 + .12f * (1 - MathF.Exp(-(age - plan.Fire) / 5))), 0, MathF.Tau, 3, alpha, species);
                }
                void DrawRing(CrimsonPoint center, float radius, float start, float length, float width, float alpha, int species)
                {
                    if (length <= .001f) return;
                    ring.Clear(); int count = CrimsonVisuals.Reduced ? 40 : 64;
                    for (int i = 0; i <= count; i++) ring.Add(CrimsonGestureVisuals.V(center + CrimsonPoint.Polar(start + length * i / count, radius)));
                    ScarletMaterials.Path(ring, width, species, age, alpha);
                }
            }
        }
        finally { batch.End(); }
    }
    private readonly HashSet<int> drawnResults = new();
    private void DrawSorcery(CrimsonBoss boss,SpriteBatch batch,float age)
    {
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not CrimsonChorus marker || !CrimsonChorus.TryBoss(marker.Plan,out var owner,marker.Resolved) || owner!=boss) continue;
            var p=marker.Plan;if(age<p.Born || age>=p.Fire+CrimsonChorusImpactPositions.TailTicks)continue;
            float progress=Math.Clamp((age-p.Born)/(p.Fire-p.Born),0,1);
            // Three abrupt growth beats with connected ease-out arrivals, not a uniform scale ramp.
            float size=45+30*CrimsonInvocation.Ease(progress*10)
                +42*CrimsonInvocation.Ease((progress-.34f)*15)+50*CrimsonInvocation.Ease((progress-.72f)*18);
            float tail=age<p.Fire?1:1-CrimsonInvocation.Ease((age-p.Fire)/CrimsonChorusImpactPositions.TailTicks);
            float failureAlpha=CrimsonChorusImpactPositions.FailureAlpha(age-p.Fire);
            int submitted = 0;
            for(int i=0;i<boss.State.Members.Length;i++)
            {
                var member=boss.State.Members[i];var player=Main.player[member.Slot];
                bool failed=marker.Resolved && (marker.FailedMask & 1<<i)!=0;
                if((p.Members & 1<<i)==0 || !failed && (member.Out || member.Recovery.Downed || !player.active || player.dead))continue;
                submitted++;
                Vector2 at=marker.Resolved && i<marker.ImpactPositions.Length
                    ? new(marker.ImpactPositions[i].X,marker.ImpactPositions[i].Y) : player.Center;
                if(p.Kind==CrimsonChorusKind.Stack)
                {
                    ScarletSorcery.Seal(batch,at-new Vector2(0,132),size,.24f,0,age,progress,tail,true,i);
                    ScarletSorcery.Seal(batch,at+new Vector2(0,132),size,.24f,.08f,age,progress,tail,true,i+2);
                    if(failed && age>=p.Fire) ScarletSorcery.Flame(batch,at-new Vector2(0,132),at+new Vector2(0,132),
                        (CrimsonVisuals.Reduced?96:120)*CrimsonInvocation.Ease((age-p.Fire)/3),age,failureAlpha);
                }
                else
                {
                    ScarletSorcery.Seal(batch,at,size,.87f,-.16f,age,progress,tail,false,i);
                    if(failed && age>=p.Fire)
                    {
                        Vector2 d=new Vector2(220,0).RotatedBy(-.68f);
                        ScarletSorcery.Tear(batch,new(new(at.X-d.X,at.Y-d.Y),new(at.X+d.X,at.Y+d.Y),CrimsonChorusImpactPositions.FailureCutRadius),
                            age,p.Fire,p.Fire+CrimsonChorusImpactPositions.FailureCutTicks,1,failureAlpha,i);
                    }
                }
            }
            if (marker.Resolved && age >= p.Fire && submitted > 0 && drawnResults.Add(p.Serial))
            {
                CrimsonPackets.Log($"event=ChorusDrawn fight={p.Fight} serial={p.Serial} kind={p.Kind} failed_mask={marker.FailedMask} verdict_delay_ticks={age-p.Fire:F1} submitted_members={submitted} observer={Main.myPlayer} reduced={CrimsonVisuals.Reduced}");
                drawnResults.RemoveWhere(serial => serial < p.Serial - 2);
            }
        }
    }
    private void Reset()
    {
        voices.Release(); heard.Clear(); drawnResults.Clear(); fight = Guid.Empty; previous = -1;
    }
    private void Teardown() { voices.Stop(); Reset(); }
    public override void OnWorldUnload() => Teardown();
    public override void ClearWorld() => Teardown();
    public override void Unload() => Teardown();
}
