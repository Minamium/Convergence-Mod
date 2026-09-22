using ScarletGraphicsScope = Convergence.Client.Graphics.WorldGraphicsScope;
#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Accepted solid geometry is rendered as textured silk, embers, tissue and rifts.
// Local secondary motion and residue are never the authority for damage.
[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonGestureVisuals : ModSystem
{
    private Guid fight;
    private int epoch = -1;
    private int previous = -1;
    private readonly HashSet<(int Phrase, byte Pulse, byte Source, bool Fire)> heard = new();
    private readonly List<(SlotId Id, int Until)> voices = new();
    internal static Vector2 V(CrimsonPoint p) => new(p.X, p.Y);
    internal static Color Palette(int source) => ScarletMaterials.Palette(source);
    public override void PostUpdateEverything()
    {
        var boss = CrimsonPackets.Boss;
        if (!ScarletArticulation.Participant(boss)) { Reset(); return; }
        int age = (int)boss!.VisualAge;
        if (fight != boss.State.Fight || epoch != boss.State.PhaseStart)
        { Reset(); fight = boss.State.Fight; epoch = boss.State.PhaseStart; previous = age - 1; }
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not CrimsonGesture gesture || !gesture.TryBoss(out var owner) || owner != boss) continue;
            var p = gesture.EffectivePlan(age);
            Cue(p.Born, false); Cue(p.Fire, true);
            void Cue(int tick, bool impact)
            {
                if (previous >= tick || age < tick || age - tick > 3 || !heard.Add((p.Phrase, p.Pulse, p.Source, impact))) return;
                string asset = p.Technique is CrimsonTechnique.SideBeams or CrimsonTechnique.ClusterVolley ? impact ? "WideFire" : "WideCharge"
                    : p.IsRift ? impact ? "ChargeRush" : "ChargeLock"
                    : impact ? "PortalFire" : "ChargeLock";
                if (voices.Count < 24)
                {
                    var id = SoundEngine.PlaySound(new SoundStyle("Convergence/Assets/Sounds/FirstSeverance/Beams/" + asset)
                    {
                        Volume = impact ? .72f : .48f,
                        Pitch = p.Technique == CrimsonTechnique.ClusterVolley ? -.12f : 0,
                        MaxInstances = 6, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
                        PlayOnlyIfFocused = true, PauseBehavior = PauseBehavior.StopWhenGamePaused
                    });
                    // Masters end naturally; the lease is only a teardown bound.
                    voices.Add((id, tick + (impact ? 100 : 50)));
                }
                if (impact)
                {
                    Vector2 at = V(p.Technique == CrimsonTechnique.ClusterVolley ? CrimsonClusters.Emitter(p.Field) : p.MovesBody ? p.Body(age) : p.Target);
                    ScarletArticulation.Impact(p.Source, p.Accent, at);
                    ScarletAtmosphere.Emit(p, at);
                }
            }
        }
        if (heard.Count > 96)
        {
            int latest = 0; foreach (var h in heard) latest = Math.Max(latest, h.Phrase);
            heard.RemoveWhere(x => x.Phrase < latest - 2);
        }
        for (int i = voices.Count - 1; i >= 0; i--)
        {
            var v = voices[i];
            if (!SoundEngine.TryGetActiveSound(v.Id, out var sound)) { voices.RemoveAt(i); continue; }
            if (age >= v.Until) { sound.Stop(); voices.RemoveAt(i); }
            else sound.Volume = Math.Min(sound.Volume, Math.Clamp((v.Until - age) / 5f, 0, 1));
        }
        previous = age;
    }
    private void Reset()
    {
        foreach (var v in voices) if (SoundEngine.TryGetActiveSound(v.Id, out var sound)) sound.Stop();
        voices.Clear(); heard.Clear(); fight = Guid.Empty; epoch = -1; previous = -1;
    }
    public override void OnWorldUnload() => Reset();
    public override void ClearWorld() => Reset();
    public override void Unload() => Reset();
    public override void PostDrawTiles()
    {
        var boss = CrimsonPackets.Boss;
        if (!ScarletArticulation.Participant(boss)) return;
        float age = CrimsonVisuals.RenderAge(boss!);
        var batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            ScarletAtmosphere.Draw(batch);
            DrawSources(boss!, batch, age);
            DrawTrackingBeams(boss!, batch, age);
            foreach (Projectile projectile in Main.ActiveProjectiles)
                if (projectile.ModProjectile is CrimsonGesture cluster && cluster.Plan.Technique == CrimsonTechnique.ClusterVolley
                    && cluster.TryBoss(out var parent) && parent == boss)
                    ScarletClusters.Draw(batch, cluster.Plan, age);
            using var scope = new ScarletGraphicsScope(batch);
            Span<CrimsonStroke> strokes = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.ModProjectile is not CrimsonGesture gesture || !gesture.TryBoss(out var owner) || owner != boss) continue;
                var p = gesture.EffectivePlan(age, true);
                if (p.Aimed || p.IsRift || p.Technique == CrimsonTechnique.ClusterVolley) continue;
                if (age < p.Born || age >= p.End + CrimsonRhythm.ResidueTicks) continue;
                bool warning = age < p.Fire;
                if (warning && DuplicateForecast(p, age)) continue;
                // The guide dissolves into the moving carrier rather than
                // disappearing on the same tick that a solid strike pops in.
                float guide = ScarletGesturePresentation.WarningOpacity(p, age);
                if (guide > .001f)
                {
                    int forecastCount = CrimsonTechniqueGeometry.Write(p, p.Fire, strokes, true);
                    ScarletMaterials.Strokes(strokes[..forecastCount], p.Source, age, guide, true,
                        p.Technique == CrimsonTechnique.ChoirRend, 0,
                        Math.Clamp((age - p.Born) / (p.Fire - p.Born), 0, 1), age - p.Fire);
                }
                if (warning)
                {
                    if (p.Technique == CrimsonTechnique.CrownCinders) DrawCinders(p, age);
                    continue;
                }
                float alpha = ScarletGesturePresentation.LiveOpacity(p, age);
                float sample = ScarletGesturePresentation.SampleAge(p, age);
                int count = CrimsonTechniqueGeometry.Write(p, sample, strokes, warning);
                ScarletMaterials.Strokes(strokes[..count], p.Source, age, alpha, warning,
                    p.Technique == CrimsonTechnique.ChoirRend, warning ? 0 : (1 + p.Accent * .35f) * MathF.Exp(-(age - p.Fire) / 5),
                    Math.Clamp((age - p.Born) / (p.Fire - p.Born), 0, 1), age - p.Fire,
                    p.Technique == CrimsonTechnique.CrownRain);
            }
        }
        finally { batch.End(); }
    }
    private static bool DuplicateForecast(in CrimsonGesturePlan plan, float age)
    {
        // Repeated full-field cuts have identical silhouettes. Do not stack six
        // opaque copies; retain the nearest impending note's material pulse.
        if (plan.Technique is not (CrimsonTechnique.CrownRain or CrimsonTechnique.MantleFan
            or CrimsonTechnique.MantleScissors or CrimsonTechnique.ChoirThrust or CrimsonTechnique.ChoirRend)) return false;
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.ModProjectile is CrimsonGesture g && g.Plan.Fight == plan.Fight
                && g.Plan.Phrase == plan.Phrase && g.Plan.Source == plan.Source
                && g.Plan.Born <= age && g.Plan.Fire > age && g.Plan.Fire < plan.Fire) return true;
        return false;
    }
    private static void DrawTrackingBeams(CrimsonBoss boss, SpriteBatch batch, float age)
    {
        CrimsonEnergy.Begin();
        Span<CrimsonStroke> strokes=stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not CrimsonGesture g || !g.TryBoss(out var owner) || owner != boss) continue;
            var p = g.EffectivePlan(age, true);
            if ((!p.Aimed && !p.IsRift) || !g.ForecastReady || age < p.Born
                || age >= p.End + (p.IsRift ? CrimsonSpatialCuts.ResidueTicks : 0)) continue;
            bool warning = age < p.Fire;
            int count=CrimsonTechniqueGeometry.Write(p,age,strokes,warning || p.IsRift);
            if (p.Technique == CrimsonTechnique.SideBeams) ScarletSorcery.CrossflowSeals(batch,p,age);
            for(int i=0;i<count;i++) {
            var s = strokes[i];
            Vector2 delta = V(s.B - s.A); float length = delta.Length();
            if (length < .01f || s.Radius < .01f) continue;
            float opacity = warning ? .95f * CrimsonInvocation.Ease((age - p.Born) / 5) : 1;
            if(p.IsRift) {
                ScarletSorcery.Tear(batch,s,age,p.Fire,p.End,Math.Clamp((age-p.Born)/(p.Fire-p.Born),0,1),
                    opacity,p.Phrase*7+p.Pulse*3+i);continue;
            }
            CrimsonEnergy.Add(V(s.A), delta / length, length, s.Radius, age, p.Fire, p.End,
                opacity, CrimsonVisuals.Reduced, p.Born, fieldBeam: true);
            }
        }
        CrimsonEnergy.Draw(batch);
    }
    private static void DrawSources(CrimsonBoss boss, SpriteBatch batch, float age)
    {
        // At most one anticipation/release per actor, not one giant flash per
        // ribbon segment. Read-only source positions; no client target selection.
        for (int source = 0; source < 4; source++)
        {
            var signal = CrimsonRig.Signal(boss, source, age);
            if (signal.Charge < .01f && signal.Recoil < .01f) continue;
            Vector2 at = boss.NPC.Center;
            bool present = source == 3;
            if (source < 3)
                foreach (NPC n in Main.ActiveNPCs)
                    if (n.ModNPC is CrimsonEffigy e && e.State.Fight == boss.State.Fight && e.State.Index == source)
                    { at = n.Center; present = boss.State.Presence(source, age) > .1f; break; }
            if (!present) continue;
            if (source!=3 && CrimsonGesture.TryPose(boss, source, age, out var pose)) at = V(pose.Body(age));
            CrimsonRig.DrawPressure(batch, at, source, age, signal.Charge, signal.Recoil);
        }
    }
    private static void DrawCinders(in CrimsonGesturePlan p, float age)
    {
        float t = Math.Clamp((age - p.Born) / (p.Fire - p.Born), 0, 1);
        Span<CrimsonStroke> particles = stackalloc CrimsonStroke[12];
        for (int column = 0; column < 6; column++) for (int row = 0; row < 2; row++)
        {
            var end = CrimsonTechniqueGeometry.CinderCenter(p, column, row);
            var control = CrimsonPoint.Lerp(p.Stage, end, .5f) + new CrimsonPoint(0, -180);
            var at = CrimsonTechniqueGeometry.Bezier(p.Stage, control, end, t);
            var prior = CrimsonTechniqueGeometry.Bezier(p.Stage, control, end, Math.Max(0, t - .055f));
            particles[column * 2 + row] = new(prior, at, 34 + t * 28);
        }
        ScarletMaterials.Strokes(particles, 0, age, .65f, false, false, 0);
    }
}
