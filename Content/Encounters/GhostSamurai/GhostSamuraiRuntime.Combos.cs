#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace Convergence.Content.Encounters.GhostSamurai;

internal sealed partial class GhostSamuraiRuntime
{
    private SamuraiComboSnapshot combo;
    private int comboEndAge;

    private void ResetCombo() { combo = default; comboEndAge = 0; }
    private void FaceTarget(NPC npc, Player target)
    {
        if (combo.Locked) return;
        int facing = target.Center.X == npc.Center.X ? (combo.Facing == 0 ? 1 : combo.Facing)
            : target.Center.X > npc.Center.X ? 1 : -1;
        combo = combo with { Facing = facing };
        npc.direction = npc.spriteDirection = facing;
        if (age % GhostSamuraiRules.AimSyncInterval == 0) npc.netUpdate = true;
    }

    private void DoTripleVerticalSlash(NPC npc, Player target)
    {
        if (timer >= SamuraiComboRules.VerticalDuration) { FinishAttack(); return; }
        int step = SamuraiComboRules.VerticalSpawnStep(timer);
        if (step >= 0 && combo.TryAdvance(step + 1, out var next))
        {
            combo = next;
            var hazard = SamuraiComboRules.Vertical(target.Center.X, arena, age);
            aimedSlash = Spawn(hazard, hazard.Fire - SamuraiComboRules.VerticalLockLead);
            npc.netUpdate = true;
        }
        int local = timer - Math.Min(timer / SamuraiComboRules.VerticalCadence, SamuraiComboRules.VerticalCount - 1) * SamuraiComboRules.VerticalCadence;
        if (aimedSlash is not null && !aimedSlash.SlashAim.Locked)
        {
            FaceTarget(npc, target);
            Hover(npc, target.Center + new Vector2(-combo.Facing * 260, -180));
            aimedSlash.Aim(age, new Vector2(target.Center.X, arena.Top), Vector2.UnitY);
            if (aimedSlash.SlashAim.Locked) { combo = combo with { Locked = true }; npc.netUpdate = true; }
        }
        else npc.velocity *= .8f;
        beat = local < SamuraiComboRules.VerticalWindup ? SamuraiBeat.Telegraph
            : local < SamuraiComboRules.VerticalWindup + SamuraiComboRules.VerticalLive ? SamuraiBeat.Strike : SamuraiBeat.Recovery;
    }

    private void DoFrontalCleaveShockwave(NPC npc, Player target)
    {
        if (timer >= SamuraiComboRules.MaximumComboTime) { ClearHazards(); FinishAttack(); return; }
        if (timer == 0)
        {
            float ground = FindComboGround(target);
            float x = Math.Clamp(target.Center.X + dashSide * SamuraiComboRules.CleaveStandOff,
                arena.Left + SamuraiComboRules.CleaveStandOff, arena.Right - SamuraiComboRules.CleaveStandOff);
            combo = new(0, -dashSide, false, npc.Center.X, npc.Center.Y, x, ground - GhostSamuraiRules.BodyHeight / 2f, ground);
            npc.netUpdate = true;
        }
        npc.velocity = Vector2.Zero;
        if (timer < SamuraiComboRules.CleaveApproach)
        {
            beat = SamuraiBeat.Approach;
            npc.Center = Vector2.Lerp(new(combo.FromX, combo.FromY), new(combo.AnchorX, combo.AnchorY), SamuraiComboRules.ApproachProgress(timer));
            FaceTarget(npc, target);
            return;
        }
        npc.Center = new(combo.AnchorX, combo.AnchorY);
        int local = timer - SamuraiComboRules.CleaveApproach;
        if (local == 0 && combo.TryAdvance(1, out var next))
        {
            combo = next;
            FaceTarget(npc, target);
            var h = SamuraiComboRules.Cleave(combo.AnchorX, combo.AnchorY, combo.Facing, age);
            aimedSlash = Spawn(h, age + SamuraiComboRules.CleaveTrackTime);
            npc.netUpdate = true;
        }
        if (aimedSlash is not null && !aimedSlash.SlashAim.Locked)
        {
            FaceTarget(npc, target);
            aimedSlash.Aim(age, npc.Center, new Vector2(combo.Facing, 0));
            if (aimedSlash.SlashAim.Locked) { combo = combo with { Locked = true }; npc.netUpdate = true; }
        }
        int slashEnd = SamuraiComboRules.CleaveWindup + SamuraiComboRules.CleaveLive;
        if (local == slashEnd && combo.TryAdvance(2, out var shock))
        {
            combo = shock with { Locked = true }; // Keep the cleave-facing pose through the follow-up.
            for (int side = -1; side <= 1; side += 2)
            {
                float distance = (side > 0 ? arena.Right - combo.AnchorX : combo.AnchorX - arena.Left) + SamuraiComboRules.ShockWidth / 2;
                var h = SamuraiComboRules.Shock(combo.AnchorX, combo.GroundY, side, distance, age);
                Spawn(h);
                comboEndAge = Math.Max(comboEndAge, h.End);
            }
            npc.netUpdate = true;
        }
        if (combo.Step == 2 && age >= comboEndAge + GhostSamuraiRules.RecoveryTime) { FinishAttack(); return; }
        beat = local < SamuraiComboRules.CleaveWindup ? SamuraiBeat.Telegraph
            : local < slashEnd || combo.Step == 2 && local >= slashEnd + SamuraiComboRules.ShockDelay && age < comboEndAge
                ? SamuraiBeat.Strike : SamuraiBeat.Recovery;
    }

    private float FindComboGround(Player target)
    {
        // Read at most one field-height column. Tiles/platforms are never edited;
        // an aerial summon with no floor uses its visible field bottom as the plane.
        int x = Math.Clamp((int)(target.Center.X / 16), 1, Main.maxTilesX - 2);
        int first = Math.Clamp((int)MathF.Ceiling((target.Bottom.Y - 2) / 16), 1, Main.maxTilesY - 2);
        int last = Math.Clamp((int)(arena.Bottom / 16), 1, Main.maxTilesY - 2);
        for (int y = first; y <= last; y++)
        {
            var tile = Main.tile[x, y];
            if (tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
                return Math.Clamp(y * 16, arena.Top + GhostSamuraiRules.BodyHeight, arena.Bottom);
        }
        return arena.Bottom;
    }
}
