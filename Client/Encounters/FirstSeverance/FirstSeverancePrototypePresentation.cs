#nullable enable

using System;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

internal sealed class FirstSeverancePrototypeMusic : ModSceneEffect
{
    public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

    public override int Music => MusicID.Boss3;

    public override bool IsSceneEffectActive(Player player)
    {
        return !Main.dedServ
            && ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat is { } combat
            && combat.TryGetParticipantByServerSlot(player.whoAmI, out var participant)
            && participant.IsConnected;
    }
}

internal sealed class FirstSeverancePrototypePresentation : ModSystem
{
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu)
            return;
        FirstSeveranceClientStateSystem state = ModContent.GetInstance<FirstSeveranceClientStateSystem>();
        FirstSeveranceCombatProjection? combat = state.Combat;
        if (combat is null)
            return;

        SpriteBatch batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            double left = combat.ResolveTick > state.EstimatedAuthorityTick
                ? combat.ResolveTick - state.EstimatedAuthorityTick : 0;
            float progress = Math.Clamp((float)(left / 180d), 0f, 1f);
            foreach (FirstSeveranceCombatParticipantProjection participant in combat.Participants)
            {
                Player player = Main.player[participant.ServerWhoAmI];
                if (!player.active || !participant.IsConnected)
                    continue;
                if (participant.CombatState != RaidParticipantCombatState.Alive)
                {
                    DrawRing(batch, player.Center, 30f, Color.IndianRed);
                    DrawRing(batch, player.Center, 8 * 16f, Color.IndianRed * 0.35f);
                }
                else if (combat.Substate == FirstSeveranceSubstate.Spread
                    || (combat.Substate == FirstSeveranceSubstate.Stack
                        && participant.ServerWhoAmI == combat.StackTargetSlot))
                {
                    Color color = combat.Substate == FirstSeveranceSubstate.Stack
                        ? Color.Cyan : Color.OrangeRed;
                    DrawRing(batch, player.Center, 7 * 16f, color * 0.85f);
                    DrawRing(batch, player.Center, 7 * 16f * progress, color * 0.4f);
                }
                if (participant.IsReviving)
                    DrawRing(batch, player.Center, 35f, Color.LightGreen);
            }
        }
        finally
        {
            batch.End();
        }
    }

    public override void PostDrawInterface(SpriteBatch spriteBatch)
    {
        if (Main.dedServ || Main.gameMenu)
            return;
        FirstSeveranceClientStateSystem state = ModContent.GetInstance<FirstSeveranceClientStateSystem>();
        FirstSeveranceCombatProjection? combat = state.Combat;
        if (combat is null || !combat.TryGetParticipantByServerSlot(Main.myPlayer, out var local))
            return;

        string phase = Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.Name" + combat.Substate);
        float remaining = SecondsLeft(combat.ResolveTick, state.EstimatedAuthorityTick);
        string text = Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.CombatHud",
            phase, remaining.ToString("0.0"), combat.RemainingReviveTokens);
        Utils.DrawBorderString(spriteBatch, text,
            new Vector2(Main.screenWidth / 2f, 82f), Color.LightCyan, 0.9f, 0.5f);

        if (local.CombatState == RaidParticipantCombatState.Downed)
            text = Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.DownedHud",
                SecondsLeft(local.DownedDeadlineTick, state.EstimatedAuthorityTick).ToString("0.0"));
        else if (local.IsReviving)
            text = Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.ReviveHud",
                SecondsLeft(local.ReviveCompletesTick, state.EstimatedAuthorityTick).ToString("0.0"));
        else
            return;

        Utils.DrawBorderString(spriteBatch, text,
            new Vector2(Main.screenWidth / 2f, 108f), Color.Orange, 0.9f, 0.5f);
    }

    private static float SecondsLeft(ulong deadline, ulong tick)
        => deadline > tick ? (deadline - tick) / 60f : 0f;

    private static void DrawRing(SpriteBatch batch, Vector2 center, float radius, Color color)
    {
        const int segments = 64;
        Vector2 previous = center + new Vector2(radius, 0f) - Main.screenPosition;
        for (int index = 1; index <= segments; index++)
        {
            float angle = index * MathHelper.TwoPi / segments;
            Vector2 next = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius
                - Main.screenPosition;
            Vector2 delta = next - previous;
            batch.Draw(TextureAssets.MagicPixel.Value, previous, null, color,
                MathF.Atan2(delta.Y, delta.X), Vector2.Zero,
                new Vector2(delta.Length(), 2f), SpriteEffects.None, 0f);
            previous = next;
        }
    }
}
