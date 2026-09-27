#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

// Keep only harmless, dark-violet contraction/soot after native End. No extra
// projectile lifetime, network state, hit window, sound or authority mutation.
[Autoload(Side = ModSide.Client)]
internal sealed class GhostSamuraiCutResidue : ModSystem
{
    private readonly record struct Scar(int Identity, int Born, SamuraiHazard Shape, Vector2 EndCenter);
    private readonly List<Scar> scars = new(GhostSamuraiRules.MaximumHazards * 2);
    private GhostSamuraiBoss? owner;
    private Guid fight;
    private ulong tick = ulong.MaxValue;

    public override void PostUpdateEverything()
    {
        if (Main.dedServ || Main.gameMenu) { Clear(); return; }
        if (Main.gamePaused || tick == Main.GameUpdateCount) return;
        tick = Main.GameUpdateCount;
        if (owner is not null && (!owner.NPC.active || owner.NPC.ModNPC != owner
            || owner.Fight != fight || !owner.ProjectionFresh || !GhostSamuraiContainmentPlayer.FightActive(owner))) Clear();
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not GhostSamuraiAttackProjectile p || !p.TryGetAge(out float age)) continue;
            var h = p.DisplayHazard;
            if (!h.Live(age) || h.Shape is SamuraiShape.Wisp or SamuraiShape.RushVisual
                || h.HasAim && !p.SlashAim.Locked) continue;
            var boss = (GhostSamuraiBoss)Main.npc[p.BossSlot].ModNPC;
            if (owner != boss || fight != p.Fight) { Clear(); owner = boss; fight = p.Fight; }
            int index = -1;
            for (int i = 0; i < scars.Count; i++)
                if (scars[i].Identity == projectile.identity && scars[i].Born == h.Born) { index = i; break; }
            Vector2 endCenter = p.VisualCenter(h.End - .01f);
            if (h.Shape == SamuraiShape.GroundShockwave)
            {
                var geometry = SamuraiComboRules.ShockGeometry(h, h.End - .01f);
                endCenter -= new Vector2(h.DX * geometry.Length / 2, 0);
                h = h with { Shape = SamuraiShape.Slash, Length = geometry.Length, Radius = geometry.Radius };
            }
            var scar = new Scar(projectile.identity, h.Born, h, endCenter);
            if (index >= 0) scars[index] = scar;
            else if (scars.Count < GhostSamuraiRules.MaximumHazards * 2) scars.Add(scar);
        }
        if (owner is not null)
        {
            float age = owner.VisualAge;
            for (int i = scars.Count - 1; i >= 0; i--)
                if (age >= scars[i].Shape.End + GhostSamuraiCuts.ResidueTicks) scars.RemoveAt(i);
        }
    }

    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu || owner is not { ProjectionFresh: true } || !owner.NPC.active
            || owner.NPC.ModNPC != owner || owner.Fight != fight || !GhostSamuraiContainmentPlayer.FightActive(owner)) return;
        float age = owner.VisualAge;
        bool any = false;
        foreach (var scar in scars)
            if (age >= scar.Shape.End && age < scar.Shape.End + GhostSamuraiCuts.ResidueTicks) { any = true; break; }
        if (!any) return;
        var batch = Main.spriteBatch;
        var field = owner.Arena;
        var clip = new Rectangle((int)(field.Left - Main.screenPosition.X), (int)(field.Top - Main.screenPosition.Y),
            (int)(field.HalfWidth * 2), (int)(field.HalfHeight * 2));
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            foreach (var scar in scars)
            {
                var h = scar.Shape;
                if (age < h.End || age >= h.End + GhostSamuraiCuts.ResidueTicks) continue;
                Vector2 at = scar.EndCenter - Main.screenPosition;
                if (h.Shape == SamuraiShape.SlashWave) GhostSamuraiCuts.Wave(batch, h, at, age, GhostSamuraiRigArt.Reduced);
                else if (h.IsCircle || h.Shape == SamuraiShape.FrontalCleave)
                    GhostSamuraiCuts.Field(batch, h, at, age, clip, GhostSamuraiRigArt.Reduced);
                else GhostSamuraiCuts.Slash(batch, h, at, age, GhostSamuraiRigArt.Reduced);
            }
        }
        finally { batch.End(); }
    }
    private void Clear() { scars.Clear(); owner = null; fight = Guid.Empty; tick = ulong.MaxValue; }
    public override void ClearWorld() => Clear();
    public override void OnWorldUnload() => Clear();
    public override void Unload() { Clear(); GhostSamuraiCuts.Reset(); }
}
