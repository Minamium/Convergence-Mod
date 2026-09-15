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
            if (!SamuraiComboRules.TryHorizontalAnchorY(arena, HorizontalBodyClear, out float y))
            {
                npc.velocity = Vector2.Zero;
                FinishAttack();
                return;
            }
            float ground = FindHorizontalGround(y + SamuraiComboRules.HorizontalBodyBelow);
            combo = new(0, -dashSide, false, npc.Center.X, npc.Center.Y, arena.CenterX, y, ground);
            npc.netUpdate = true;
        }
        npc.velocity = Vector2.Zero;
        int approach = SamuraiComboRules.ApproachDuration(combo);
        if (timer < approach)
        {
            beat = SamuraiBeat.Approach;
            npc.Center = Vector2.Lerp(new(combo.FromX, combo.FromY), new(combo.AnchorX, combo.AnchorY), SamuraiComboRules.ApproachProgress(timer, combo));
            FaceTarget(npc, target);
            return;
        }
        npc.Center = new(combo.AnchorX, combo.AnchorY);
        int local = timer - approach;
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
                float distance = (side > 0 ? arena.Right - combo.AnchorX : combo.AnchorX - arena.Left)
                    + SamuraiComboRules.ShockWidth * SamuraiComboRules.HorizontalSlashWaveMaxScale / 2;
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

    private bool HorizontalBodyClear(float centerY)
    {
        const int padding = 4;
        int left = (int)((arena.CenterX - SamuraiComboRules.HorizontalBodyHalfWidth - padding) / 16);
        int right = (int)((arena.CenterX + SamuraiComboRules.HorizontalBodyHalfWidth + padding) / 16);
        int top = (int)((centerY - SamuraiComboRules.HorizontalBodyAbove - padding) / 16);
        int bottom = (int)((centerY + SamuraiComboRules.HorizontalBodyBelow + padding) / 16);
        for (int x = left; x <= right; x++)
        for (int y = top; y <= bottom; y++)
            if (HorizontalSolid(x, y)) return false;
        return true;
    }

    private static bool HorizontalSolid(int x, int y)
    {
        if (x <= 0 || y <= 0 || x >= Main.maxTilesX - 1 || y >= Main.maxTilesY - 1) return true;
        var tile = Main.tile[x, y];
        return tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
    }

    private float FindHorizontalGround(float bodyBottom)
    {
        // One bounded server lookup across the body footprint. No player-local
        // terrain, arena relocation or tile edits participate in the attack.
        int left = (int)((arena.CenterX - SamuraiComboRules.HorizontalBodyHalfWidth) / 16);
        int right = (int)((arena.CenterX + SamuraiComboRules.HorizontalBodyHalfWidth) / 16);
        int first = Math.Clamp((int)MathF.Ceiling(bodyBottom / 16), 1, Main.maxTilesY - 2);
        int last = Math.Clamp((int)(arena.Bottom / 16), 1, Main.maxTilesY - 2);
        for (int y = first; y <= last; y++)
        for (int x = left; x <= right; x++)
            if (HorizontalSolid(x, y)) return Math.Min(y * 16, arena.Bottom);
        return arena.Bottom;
    }
}
