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
        if (fight != boss.State.Fight) { Reset(); fight = boss.State.Fight; previous = age - 1; }
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not CrimsonGesture gesture || !gesture.TryBoss(out var owner) || owner != boss) continue;
            var p = gesture.Plan;
            Cue(p.Born, false); Cue(p.Fire, true);
            void Cue(int tick, bool impact)
            {
                if (previous >= tick || age < tick || age - tick > 3 || !heard.Add((p.Phrase, p.Pulse, p.Source, impact))) return;
                string asset = impact ? p.Source switch
                { 0 => "PylonBreak", 1 => "SwordImpale", 2 => "BladeUnsheathe", _ => "Beams/SpreadScatter" }
                    : p.Source switch { 0 => "EnergyLock", 1 => "BladeUnsheathe", 2 => "LanceCharge", _ => "EnergyGather" };
                if (voices.Count < 24)
                {
                    var id = SoundEngine.PlaySound(new SoundStyle("Convergence/Assets/Sounds/FirstSeverance/" + asset)
                    {
                        Volume = (impact ? .24f : .13f) + p.Accent * .025f,
                        MaxInstances = 3, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
                        PlayOnlyIfFocused = true, PauseBehavior = PauseBehavior.StopWhenGamePaused
                    });
                    voices.Add((id, tick + (impact ? 14 : 9)));
                }
                if (impact)
                {
                    Vector2 at = V(p.MovesBody ? p.Body(age) : p.Target);
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
        voices.Clear(); heard.Clear(); fight = Guid.Empty; previous = -1;
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
            using var scope = new ScarletGraphicsScope(batch);
            Span<CrimsonStroke> strokes = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.ModProjectile is not CrimsonGesture gesture || !gesture.TryBoss(out var owner) || owner != boss) continue;
                var p = gesture.Plan;
                if (age < p.Born || age >= p.End + 10) continue;
                bool warning = age < p.Fire;
                float alpha = warning ? .78f + .18f * MathF.Exp(-(age - p.Born) / 5)
                    : age < p.End ? 1 : 1 - CrimsonInvocation.Ease((age - p.End) / 10);
                float sample = warning ? p.Fire : Math.Min(age, p.End - .001f);
                if (age >= p.End && p.Technique is CrimsonTechnique.ChoirHook or CrimsonTechnique.ChoirThrust or CrimsonTechnique.MantleScissors)
                    sample = p.Fire + (p.End - p.Fire - 1) * (1 - CrimsonInvocation.Ease((age - p.End) / 10));
                int count = CrimsonTechniqueGeometry.Write(p, sample, strokes, warning);
                ScarletMaterials.Strokes(strokes[..count], p.Source, age, alpha, warning,
                    p.Technique == CrimsonTechnique.ChoirRend, warning ? 0 : (1 + p.Accent * .35f) * MathF.Exp(-(age - p.Fire) / 5),
                    Math.Clamp((age - p.Born) / (p.Fire - p.Born), 0, 1), age - p.Fire);
                if (p.Technique == CrimsonTechnique.CrownCinders && warning) DrawCinders(p, age);
            }
        }
        finally { batch.End(); }
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
            if (CrimsonGesture.TryPose(boss, source, age, out var pose)) at = V(pose.Body(age));
            CrimsonRig.DrawPressure(batch, at, source, age, signal.Charge, signal.Recoil);
        }
    }
    private static void DrawCinders(in CrimsonGesturePlan p, float age)
    {
        float t = Math.Clamp((age - p.Born) / (p.Fire - p.Born), 0, 1);
        Span<CrimsonStroke> particles = stackalloc CrimsonStroke[3];
        for (int i = -1; i <= 1; i++)
        {
            var end = CrimsonTechniqueGeometry.Clamp(p.Field, p.Target + new CrimsonPoint(i * 260, (p.Pulse % 2 == 0 ? -1 : 1) * 90), 125);
            var control = CrimsonPoint.Lerp(p.Stage, end, .5f) + new CrimsonPoint(0, -180);
            var at = CrimsonTechniqueGeometry.Bezier(p.Stage, control, end, t);
            var prior = CrimsonTechniqueGeometry.Bezier(p.Stage, control, end, Math.Max(0, t - .055f));
            particles[i + 1] = new(prior, at, 9 + t * 7);
        }
        ScarletMaterials.Strokes(particles, 0, age, .65f, false, false, 0);
    }
}
