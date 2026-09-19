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

[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonChorusVisuals : ModSystem
{
    private Guid fight;
    private int previous = -1;
    private readonly HashSet<(int Serial, bool Impact)> heard = new();
    private readonly List<(SlotId Id, int End)> voices = new();
    private readonly List<Vector2> ring = new(65);
    private readonly Vector2[] arrow = new Vector2[3];
    public override void PostUpdateEverything()
    {
        var boss = CrimsonPackets.Boss;
        if (!ScarletArticulation.Participant(boss)) { Reset(); return; }
        int age = (int)boss!.VisualAge;
        if (fight != boss.State.Fight) { Reset(); fight = boss.State.Fight; previous = age - 1; }
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not CrimsonChorus marker || !CrimsonChorus.TryBoss(marker.Plan, out var owner) || owner != boss) continue;
            var p = marker.Plan;
            Cue(p.Born, false); Cue(p.Fire, true);
            void Cue(int tick, bool impact)
            {
                if (previous >= tick || age < tick || age - tick > 3 || !heard.Add((p.Serial, impact))) return;
                string asset = p.Kind == CrimsonChorusKind.Stack ? impact ? "StackRelease" : "StackSummon"
                    : impact ? "SpreadRelease" : "SpreadSummon";
                var id = SoundEngine.PlaySound(new SoundStyle("Convergence/Assets/Sounds/FirstSeverance/" + asset)
                {
                    Volume = impact ? .30f : .20f, MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
                    PlayOnlyIfFocused = true, PauseBehavior = PauseBehavior.StopWhenGamePaused
                });
                voices.Add((id, age + 45));
            }
            if (heard.Count > 32) heard.RemoveWhere(x => x.Serial < p.Serial - 2);
        }
        for (int i = voices.Count - 1; i >= 0; i--)
        {
            var v = voices[i];
            if (!SoundEngine.TryGetActiveSound(v.Id, out var sound)) { voices.RemoveAt(i); continue; }
            if (age >= v.End) { sound.Stop(); voices.RemoveAt(i); }
        }
        previous = age;
    }
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
            using var scope = new ScarletGraphicsScope(batch);
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.ModProjectile is not CrimsonChorus marker || !CrimsonChorus.TryBoss(marker.Plan, out var owner) || owner != boss) continue;
                var plan = marker.Plan;
                if (age < plan.Born || age >= plan.Fire + 22) continue;
                float progress = Math.Clamp((age - plan.Born) / (plan.Fire - plan.Born), 0, 1);
                float alpha = age < plan.Fire ? Math.Min(1, (age - plan.Born + 1) / 8)
                    : 1 - CrimsonInvocation.Ease((age - plan.Fire) / 22);
                float pulse = CrimsonRegistration.Score.Pulse(Math.Max(0, age - boss!.State.MusicStart));
                if (plan.Kind == CrimsonChorusKind.Stack) DrawMarker(plan.Center, CrimsonChorusRules.StackRadius, true);
                else for (int i = 0; i < boss!.State.Members.Length; i++)
                {
                    var member = boss.State.Members[i]; var player = Main.player[member.Slot];
                    if ((plan.Members & 1 << i) == 0 || member.Out || !player.active || player.dead || player.ghost) continue;
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
    private void Reset()
    {
        foreach (var v in voices) if (SoundEngine.TryGetActiveSound(v.Id, out var sound)) sound.Stop();
        voices.Clear(); heard.Clear(); fight = Guid.Empty; previous = -1;
    }
    public override void OnWorldUnload() => Reset();
    public override void ClearWorld() => Reset();
    public override void Unload() => Reset();
}
