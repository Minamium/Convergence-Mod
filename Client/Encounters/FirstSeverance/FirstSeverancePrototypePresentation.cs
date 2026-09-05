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
    private readonly FirstSeveranceBossVisuals visuals = new();

    public override void PostUpdateEverything()
    {
        if (!Main.dedServ && !Main.gameMenu)
            visuals.Update(ModContent.GetInstance<FirstSeveranceClientStateSystem>());
    }

    public override void OnWorldUnload() => visuals.Reset();

    public override void Unload() => visuals.Reset(unload: true);

    public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
    {
        if (Main.dedServ || Main.gameMenu
            || ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat is not { } combat
            || !combat.TryGetParticipantByServerSlot(Main.myPlayer, out _))
            return;
        bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
        backgroundColor = Color.Lerp(backgroundColor, new Color(31, 53, 77), reduced ? 0.15f : 0.55f);
        tileColor = Color.Lerp(tileColor, new Color(133, 164, 179), reduced ? 0.05f : 0.16f);
    }

    public override void ModifyScreenPosition()
    {
        if (Main.dedServ || Main.gameMenu)
            return;
        var config = ModContent.GetInstance<FirstSeveranceVisualConfig>();
        var state = ModContent.GetInstance<FirstSeveranceClientStateSystem>();
        if (!config.ScreenShake || config.ReducedEffects || state.Combat is not { } combat
            || !combat.TryGetParticipantByServerSlot(Main.myPlayer, out _))
            return;
        float amount = visuals.PhaseShake;
        if (combat.LanceVolley is { } volley && volley.IsFiring(state.EstimatedAuthorityTick))
        {
            float age = (state.EstimatedAuthorityTick - volley.FireTick) / (float)FirstSeveranceLanceTuning.ActiveTicks;
            amount = Math.Max(amount, 12f * MathF.Pow(1f - age, 2f));
        }
        if (amount <= 0)
            return;
        float t = Main.GameUpdateCount % 3600;
        Main.screenPosition += new Vector2(MathF.Sin(t * 2.1f), MathF.Cos(t * 1.7f)) * amount;
    }

    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu)
            return;
        FirstSeveranceClientStateSystem state = ModContent.GetInstance<FirstSeveranceClientStateSystem>();
        FirstSeveranceCombatProjection? combat = state.Combat;
        SpriteBatch batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            if (combat is null)
            {
                visuals.DrawEnding(batch);
                return;
            }
            visuals.Draw(batch, combat, state.EstimatedAuthorityTick);
            double left = combat.ResolveTick > state.EstimatedAuthorityTick
                ? combat.ResolveTick - state.EstimatedAuthorityTick : 0;
            int telegraph = combat.Substate == FirstSeveranceSubstate.Stack
                ? FirstSeveranceEncounterPlan.Instance.Timing.StackTelegraphTicks
                : FirstSeveranceEncounterPlan.Instance.Timing.SpreadTelegraphTicks;
            float progress = Math.Clamp((float)(left / telegraph), 0f, 1f);
            foreach (FirstSeveranceCombatParticipantProjection participant in combat.Participants)
            {
                Player player = Main.player[participant.ServerWhoAmI];
                if (!player.active || !participant.IsConnected)
                    continue;
                if (participant.CombatState != RaidParticipantCombatState.Alive)
                {
                    DrawRing(batch, player.Center, 30f, Color.IndianRed);
                    DrawRing(batch, player.Center, 8 * 16f, Color.IndianRed * 0.35f);
                    string recovery = participant.CombatState == RaidParticipantCombatState.Eliminated
                        ? Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.BodyEliminated")
                        : participant.ReviveLockoutUntilTick > state.EstimatedAuthorityTick
                            ? Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.BodyLocked",
                                Math.Ceiling(SecondsLeft(participant.ReviveLockoutUntilTick,
                                    state.EstimatedAuthorityTick)))
                            : Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.BodyRevivable");
                    Utils.DrawBorderString(batch, recovery,
                        player.Center - Main.screenPosition - new Vector2(0, 45),
                        Color.LightGoldenrodYellow, 0.75f, 0.5f);
                }
                else if (combat.Substate == FirstSeveranceSubstate.Spread
                    || (combat.Substate == FirstSeveranceSubstate.Stack
                        && participant.ServerWhoAmI == combat.StackTargetSlot))
                {
                    bool stack = combat.Substate == FirstSeveranceSubstate.Stack;
                    Color color = stack ? Color.Cyan : Color.OrangeRed;
                    float radius = stack ? FirstSeveranceLanceTuning.StackRadius : FirstSeveranceLanceTuning.SpreadRadius;
                    DrawRing(batch, player.Center, radius, Color.Black * 0.85f, 6f);
                    DrawRing(batch, player.Center, radius, color * 0.95f, 2.5f);
                    DrawRing(batch, player.Center, radius * progress, color * 0.5f);
                    // Inward circular stack vs outward chevrons and a diamond.
                    for (int index = 0; index < 8; index++)
                    {
                        float angle = index * MathHelper.TwoPi / 8f;
                        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
                        Vector2 tangent = new(-direction.Y, direction.X);
                        float travel = (state.EstimatedAuthorityTick % 60) / 60f;
                        Vector2 tip = player.Center + direction * radius * (stack ? 1f - travel * 0.25f : 0.75f + travel * 0.25f);
                        Vector2 tail = tip + direction * (stack ? 13 : -13);
                        FirstSeveranceBossVisuals.Line(batch, tail + tangent * 7, tip, color * 0.8f, 2.5f);
                        FirstSeveranceBossVisuals.Line(batch, tail - tangent * 7, tip, color * 0.8f, 2.5f);
                    }
                    if (!stack)
                        for (int index = 0; index < 4; index++)
                        {
                            float a = index * MathHelper.PiOver2;
                            float b = (index + 1) * MathHelper.PiOver2;
                            FirstSeveranceBossVisuals.Line(batch,
                                player.Center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 27,
                                player.Center + new Vector2(MathF.Cos(b), MathF.Sin(b)) * 27, color, 2.5f);
                        }
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
        if (combat is null)
        {
            if (state.CombatEndMessage is { } ended)
                Utils.DrawBorderString(spriteBatch, ended,
                    new Vector2(Main.screenWidth / 2f, 108f), Color.LightGoldenrodYellow, 0.9f, 0.5f);
            return;
        }
        if (!combat.TryGetParticipantByServerSlot(Main.myPlayer, out var local))
            return;

        // A brief edge accent gives the shot impact without a full-screen white flash.
        if (!ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects
            && combat.LanceVolley is { } impact && impact.IsFiring(state.EstimatedAuthorityTick))
        {
            float power = 1f - (state.EstimatedAuthorityTick - impact.FireTick)
                / (float)FirstSeveranceLanceTuning.ActiveTicks;
            Color edge = new Color(255, 91, 57) * (power * 0.8f);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, 5),
                new Rectangle(0, 0, 1, 1), edge);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(0, Main.screenHeight - 5, Main.screenWidth, 5),
                new Rectangle(0, 0, 1, 1), edge);
        }

        string phase = Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.Name" + combat.Substate);
        float remaining = SecondsLeft(combat.ResolveTick, state.EstimatedAuthorityTick);
        string text = Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.CombatHud",
            phase, remaining.ToString("0.0"));
        Utils.DrawBorderString(spriteBatch, text,
            new Vector2(Main.screenWidth / 2f, 82f), Color.LightCyan, 0.9f, 0.5f);

        if (combat.LanceVolley is { } volley && state.EstimatedAuthorityTick < volley.EndTick)
        {
            string lance = state.EstimatedAuthorityTick >= volley.FireTick
                ? Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.LanceFire")
                : Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.LanceWarning",
                    SecondsLeft(volley.FireTick, state.EstimatedAuthorityTick).ToString("0.0"));
            Utils.DrawBorderString(spriteBatch, lance,
                new Vector2(Main.screenWidth / 2f, 143f), Color.LightSalmon, 0.85f, 0.5f);
        }

        if (combat.Substate == FirstSeveranceSubstate.SpawnIntro)
        {
            Utils.DrawBorderString(spriteBatch,
                Language.GetTextValue("Mods.Convergence.NPCs.FirstSeverancePrototypeBoss.DisplayName"),
                new Vector2(Main.screenWidth / 2f, Main.screenHeight * 0.25f), Color.LightCyan, 1.45f, 0.5f);
        }

        if (local.CombatState == RaidParticipantCombatState.Downed)
            text = local.ReviveLockoutUntilTick > state.EstimatedAuthorityTick
                ? Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.DownedLockedHud",
                    SecondsLeft(local.DownedDeadlineTick, state.EstimatedAuthorityTick).ToString("0.0"),
                    SecondsLeft(local.ReviveLockoutUntilTick, state.EstimatedAuthorityTick).ToString("0"))
                : Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.DownedHud",
                    SecondsLeft(local.DownedDeadlineTick, state.EstimatedAuthorityTick).ToString("0.0"));
        else if (local.CombatState == RaidParticipantCombatState.Eliminated)
            text = Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.EliminatedHud");
        else if (local.ReviveLockoutUntilTick > state.EstimatedAuthorityTick)
            text = Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.RecoveryLockoutHud",
                SecondsLeft(local.ReviveLockoutUntilTick, state.EstimatedAuthorityTick).ToString("0"));
        else
            return;

        Utils.DrawBorderString(spriteBatch, text,
            new Vector2(Main.screenWidth / 2f, 112f), Color.Orange, 1.1f, 0.5f);
    }

    private static float SecondsLeft(ulong deadline, ulong tick)
        => deadline > tick ? (deadline - tick) / 60f : 0f;

    private static void DrawRing(SpriteBatch batch, Vector2 center, float radius, Color color, float width = 2f)
        => FirstSeveranceBossVisuals.Ring(batch, center, radius, color, width);
}
