#nullable enable

using System;
using System.Collections.Generic;
using Convergence.Common.Raids.Revive;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace Convergence.Client.Encounters.FirstSeverance;

internal sealed class FirstSeverancePrototypeMusic : ModSceneEffect
{
    public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

    public override int Music => Main.dedServ ? -1 : MusicLoader.GetMusicSlot(Mod,
        ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat?.BossPhase switch
        {
            FirstSeveranceBossPhase.Final => "Assets/Music/TerminalLiturgy",
            FirstSeveranceBossPhase.Distant => "Assets/Music/DistantLiturgy",
            FirstSeveranceBossPhase.Unbound => "Assets/Music/UnboundLiturgy",
            _ => "Assets/Music/ObsidianLiturgy",
        });

    public override void SpecialVisuals(Player player, bool isActive)
    {
        if (Main.dedServ || Main.gameMenu || player.whoAmI != Main.myPlayer)
            return;
        isActive |= ModContent.GetInstance<FirstSeverancePrototypePresentation>().IsEnding;
        // This callback also runs during initial spawn when the Raid is inactive.
        // ManageSpecialBiomeVisuals requires a same-key Filters.Scene entry;
        // this feature owns only a CustomSky, so use its manager directly.
        if (SkyManager.Instance is not { } manager
            || manager[FirstSeveranceSky.Key] is not FirstSeveranceSky scene
            || scene.IsSceneRequested == isActive)
            return;
        if (isActive)
            manager.Activate(FirstSeveranceSky.Key, player.Center);
        else
            manager.Deactivate(FirstSeveranceSky.Key);
    }

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
    private readonly FirstSeveranceFeedback feedback = new();
    private readonly FirstSeverancePreparationVisuals preparationVisuals = new();
    private FirstSeveranceSky? sky;
    internal double RenderTick => visuals.RenderTick;
    internal bool IsEnding => visuals.IsEnding;

    public override void Load()
    {
        if (!Main.dedServ)
            SkyManager.Instance[FirstSeveranceSky.Key] = sky = new FirstSeveranceSky();
    }

    public override void PostUpdateEverything()
    {
        if (!Main.dedServ && !Main.gameMenu)
        {
            var state = ModContent.GetInstance<FirstSeveranceClientStateSystem>();
            if (state.Preparation is not null && visuals.IsEnding) visuals.Reset();
            visuals.Update(state);
            feedback.Update(state);
            feedback.UpdateEnding(visuals.IsEnding && visuals.EndingVictory, visuals.EndingAge);
            preparationVisuals.Update(state);
        }
    }

    public override void OnWorldUnload()
    {
        fieldMask = null;
        visuals.Reset();
        preparationVisuals.Reset();
        feedback.Reset();
        sky?.Reset();
    }

    public override void Unload()
    {
        fieldMask = null;
        visuals.Reset(unload: true);
        preparationVisuals.Reset();
        feedback.Reset(true);
        sky?.Unload();
        sky = null;
    }

    public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
    {
        if (Main.dedServ || Main.gameMenu
            || ModContent.GetInstance<FirstSeveranceClientStateSystem>().Combat is not { } combat
            || !combat.TryGetParticipantByServerSlot(Main.myPlayer, out _))
            return;
        bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
        backgroundColor = Color.Lerp(backgroundColor, new Color(24, 22, 27), reduced ? 0.22f : 0.82f);
        tileColor = Color.Lerp(tileColor, new Color(172, 170, 174), reduced ? 0.05f : 0.12f);
    }

    public override void ModifyScreenPosition()
    {
        if (Main.dedServ || Main.gameMenu)
            return;
        var config = ModContent.GetInstance<FirstSeveranceVisualConfig>();
        var state = ModContent.GetInstance<FirstSeveranceClientStateSystem>();
        if (state.Preparation is { } preparing && preparing.TryGetMemberByServerSlot(Main.myPlayer, out _))
        {
            float age = Math.Clamp((float)((double)state.EstimatedAuthorityTick - preparing.EnteredTick)
                / FirstSeverancePreparationTimeline.DeploymentTicks, 0, 1);
            if (age < 1)
            {
                float weight = FirstSeveranceStageVisuals.CameraWeight(age);
                Vector2 target = new Vector2(preparing.GroundX, preparing.GroundY - 560) - new Vector2(Main.screenWidth, Main.screenHeight) * .5f;
                Main.screenPosition = Vector2.Lerp(Main.screenPosition, target, weight * .75f);
                if (config.ScreenShake && !config.ReducedEffects)
                    Main.screenPosition += new Vector2(MathF.Sin(age * 210), MathF.Cos(age * 177))
                        * (8 * FirstSeveranceVisualCurves.PreRelease(state.EstimatedAuthorityTick, preparing.EnteredTick + 108, 24, 55));
            }
        }
        if (state.Combat is null && visuals.IsEnding)
        {
            float focus = FirstSeveranceStageVisuals.CameraWeight(visuals.EndingAge);
            Vector2 target = visuals.EndingCenter - new Vector2(Main.screenWidth, Main.screenHeight) * .5f;
            Main.screenPosition = Vector2.Lerp(Main.screenPosition, target, focus);
            if (config.ScreenShake && !config.ReducedEffects && visuals.EndingShake > 0)
            {
                float time = (float)(Main.GameUpdateCount % 3600);
                Main.screenPosition += new Vector2(MathF.Sin(time * 2.1f), MathF.Cos(time * 1.7f)) * visuals.EndingShake;
            }
        }
        if (state.Combat is not { } combat
            || !combat.TryGetParticipantByServerSlot(Main.myPlayer, out _))
            return;
        if (combat.Substate == FirstSeveranceSubstate.PhaseTransition && state.EstimatedAuthorityTick < combat.ResolveTick)
        {
            float age = FirstSeveranceStageVisuals.RuptureAge(combat, visuals.RenderTick);
            float focus = FirstSeveranceStageVisuals.CameraWeight(age);
            Vector2 target = FirstSeveranceBossVisuals.CoreCenter(combat) - new Vector2(Main.screenWidth, Main.screenHeight) * .5f;
            Main.screenPosition = Vector2.Lerp(Main.screenPosition, target, focus);
        }
        if (!config.ScreenShake || config.ReducedEffects) return;
        float amount = Math.Max(visuals.PhaseShake, feedback.Shake);
        if (combat.Substate == FirstSeveranceSubstate.PhaseTransition)
        {
            double start = combat.BossPhaseStartedTick;
            amount = Math.Max(amount, 14 * FirstSeveranceVisualCurves.PreRelease(visuals.RenderTick, start + 112, 60, 150));
        }
        if (IsIntro(state, out _))
        {
            float age = IntroAge(combat, state.EstimatedAuthorityTick);
            amount = Math.Max(amount, 18f * MathF.Pow(1f - age, 2f));
        }
        if (combat.LanceVolley is { } volley && volley.IsFiring(state.EstimatedAuthorityTick))
        {
            float age = (state.EstimatedAuthorityTick - volley.FireTick) / (float)volley.ActiveTicks;
            amount = Math.Max(amount, 12f * MathF.Pow(1f - age, 2f));
        }
        if (combat.GridVolley is { } grid && grid.IsFiring(state.EstimatedAuthorityTick))
            amount = Math.Max(amount, (grid.CoreBeams.Count > 0 ? 14 : 7)
                * FirstSeveranceVisualCurves.Recoil(visuals.RenderTick, grid.FireTick, grid.EndTick));
        if (amount <= 0)
            return;
        float t = Main.GameUpdateCount % 3600;
        Main.screenPosition += new Vector2(MathF.Sin(t * 2.1f), MathF.Cos(t * 1.7f)) * amount;
    }

    public override void PostDrawTiles()
    {
        fieldMask = null;
        if (Main.dedServ || Main.gameMenu)
            return;
        FirstSeveranceClientStateSystem state = ModContent.GetInstance<FirstSeveranceClientStateSystem>();
        FirstSeveranceCombatProjection? combat = state.Combat;
        FirstSeveranceContainmentBounds? visibleField = null;
        if (combat is not null && combat.TryGetParticipantByServerSlot(Main.myPlayer, out var local) && local.IsConnected)
            visibleField = FirstSeveranceContainmentBounds.FromGround(combat.CoreX, combat.CoreY);
        else if (state.Preparation is { } preparing && preparing.TryGetMemberByServerSlot(Main.myPlayer, out _))
            visibleField = FirstSeveranceContainmentBounds.FromGround(preparing.GroundX, preparing.GroundY);
        if (visibleField is { } field)
        {
            Matrix view = Main.GameViewMatrix.TransformationMatrix;
            var transform = System.Numerics.Matrix3x2.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y)
                * new System.Numerics.Matrix3x2(view.M11, view.M12, view.M21, view.M22, view.M41, view.M42);
            var viewport = Main.instance.GraphicsDevice.Viewport;
            fieldMask = FirstSeveranceFieldMaskLayout.Capture(field.Left, field.Top, field.Right, field.Bottom,
                transform, viewport.Width, viewport.Height);
        }
        SpriteBatch batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            ModContent.GetInstance<FoundationCoreVisuals>().DrawWorld(batch, state);
            preparationVisuals.DrawReadyLabels(batch, state);
            if (combat is null)
            {
                visuals.DrawEnding(batch);
                feedback.Draw(batch, ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects);
                return;
            }
            visuals.Draw(batch, combat, state.EstimatedAuthorityTick);
            bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
            feedback.Draw(batch, reduced);
            var safeWindow = FirstSeveranceSafeWindows.At(combat.Substate, combat.ActionIndex,
                combat.ActionStartedTick, state.EstimatedAuthorityTick, combat.CoreX, combat.CoreY);
            if (safeWindow is { } expired && state.EstimatedAuthorityTick >= expired.ResolveTick) safeWindow = null;
            bool safeStack = safeWindow is { Kind: FirstSeveranceSafeMechanic.Stack };
            bool safeSpread = safeWindow is { Kind: FirstSeveranceSafeMechanic.Spread };
            ulong mechanicResolve = safeWindow?.ResolveTick ?? combat.ResolveTick;
            int telegraph = safeWindow is { } warning ? (int)(warning.ResolveTick - warning.StartTick) : (int)Math.Min(3600UL, combat.ResolveTick - combat.ActionStartedTick);
            if (combat.Substate == FirstSeveranceSubstate.Stack || safeStack)
            {
                Vector2 anchor = safeStack && safeWindow is { } site ? new(site.X, site.Y) : new(combat.StackX, combat.StackY);
                visuals.Accents.Marker(batch, anchor, FirstSeveranceLanceTuning.StackRadius, true,
                    visuals.RenderTick, mechanicResolve, telegraph, reduced);
            }
            foreach (FirstSeveranceCombatParticipantProjection participant in combat.Participants)
            {
                Player player = Main.player[participant.ServerWhoAmI];
                if (!player.active || !participant.IsConnected)
                    continue;
                if (participant.CombatState != RaidParticipantCombatState.Alive)
                {
                    DrawRing(batch, player.Center, 30f, Color.IndianRed);
                    DrawRing(batch, player.Center, 8 * 16f, Color.IndianRed * 0.35f);
                    string recovery = participant.ReviveLockoutUntilTick > state.EstimatedAuthorityTick
                            ? Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.BodyLocked",
                                Math.Ceiling(SecondsLeft(participant.ReviveLockoutUntilTick,
                                    state.EstimatedAuthorityTick)))
                            : Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.BodyRevivable");
                    Utils.DrawBorderString(batch, recovery,
                        player.Center - Main.screenPosition - new Vector2(0, 45),
                        Color.LightGoldenrodYellow, 0.75f, 0.5f);
                }
                if ((combat.Substate == FirstSeveranceSubstate.Spread || safeSpread)
                        && participant.CombatState == RaidParticipantCombatState.Alive)
                {
                    bool stack = false;
                    float radius = stack ? FirstSeveranceLanceTuning.StackRadius : FirstSeveranceLanceTuning.SpreadRadius;
                    visuals.Accents.Marker(batch, player.Center, radius, stack, visuals.RenderTick,
                        mechanicResolve, telegraph, reduced);
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
        if (IsIntro(state, out _) || IsRupture(state, out _) || visuals.IsEnding)
            return; // The isolated cinematic layer owns this frame, without HUD overlap.
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
        if (combat.GridVolley is { } grid)
            Utils.DrawBorderString(spriteBatch, Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.GridInstruction"),
                new Vector2(Main.screenWidth / Main.UIScale / 2, 135), grid.IsFiring(state.EstimatedAuthorityTick) ? Color.White : Color.LightCyan, .85f, .5f);

        // A brief edge accent gives the shot impact without a full-screen white flash.
        if (!ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects
            && combat.LanceVolley is { } impact && impact.IsFiring(state.EstimatedAuthorityTick))
        {
            float power = 1f - (state.EstimatedAuthorityTick - impact.FireTick)
                / (float)impact.ActiveTicks;
            Color edge = FirstSeveranceBossVisuals.Gold * (power * 0.32f);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, 5),
                new Rectangle(0, 0, 1, 1), edge);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(0, Main.screenHeight - 5, Main.screenWidth, 5),
                new Rectangle(0, 0, 1, 1), edge);
        }

        string phase = Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.Name" + combat.Substate);
        phase = $"{combat.BossPhase.ToString().ToUpperInvariant()} // {phase}";
        float remaining = SecondsLeft(combat.ResolveTick, state.EstimatedAuthorityTick);
        string text = Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.CombatHud",
            phase, remaining.ToString("0.0"));
        if (combat.IsHpGated && combat.BossPhase != FirstSeveranceBossPhase.Final)
            text += "  |  HP LOCK";
        Utils.DrawBorderString(spriteBatch, text,
            new Vector2(Main.screenWidth / 2f, 82f), Color.LightCyan, 0.9f, 0.5f);
        string? scoreGuide = combat.Substate switch
        {
            FirstSeveranceSubstate.RotatingBlade => "BladeGuide",
            FirstSeveranceSubstate.RemoteClaws => "ClawsGuide",
            FirstSeveranceSubstate.HalfField => "HalfGuide",
            FirstSeveranceSubstate.RemoteCrush => "CrushGuide",
            FirstSeveranceSubstate.FinalBullets or FirstSeveranceSubstate.FinalSlicer => "FinalGuide",
            _ => null,
        };
        if (scoreGuide is not null)
            Utils.DrawBorderString(spriteBatch, Language.GetTextValue("Mods.Convergence.UI.FirstSeverance." + scoreGuide),
                new Vector2(Main.screenWidth / 2f, 108f), Color.Silver, .72f, .5f);

        if (combat.LanceVolley is { } volley && state.EstimatedAuthorityTick < volley.EndTick)
        {
            string instruction = volley.Kind switch
            {
                FirstSeveranceAttackKind.PursuitPrism => "PrismInstruction",
                FirstSeveranceAttackKind.SweepRight => "DashRightInstruction",
                FirstSeveranceAttackKind.SweepLeft => "DashLeftInstruction",
                FirstSeveranceAttackKind.Stillness => "StillnessInstruction",
                _ => "LanceWarning",
            };
            string lance = volley.Kind != FirstSeveranceAttackKind.ObservationLance
                ? Language.GetTextValue("Mods.Convergence.UI.FirstSeverance." + instruction, volley.Step + 1)
                : state.EstimatedAuthorityTick >= volley.FireTick
                ? Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.LanceFire")
                : Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.LanceWarning",
                    SecondsLeft(volley.FireTick, state.EstimatedAuthorityTick).ToString("0.0"));
            Utils.DrawBorderString(spriteBatch, lance,
                new Vector2(Main.screenWidth / 2f, 143f), Color.LightSalmon, 0.85f, 0.5f);
        }

        if (local.CombatState == RaidParticipantCombatState.Downed)
            text = local.ReviveLockoutUntilTick > state.EstimatedAuthorityTick
                ? Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.DownedLockedHud",
                    SecondsLeft(local.ReviveLockoutUntilTick, state.EstimatedAuthorityTick).ToString("0"))
                : Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.DownedHud");
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

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (Main.dedServ || Main.gameMenu)
            return;
        var state = ModContent.GetInstance<FirstSeveranceClientStateSystem>();
        if (state.Preparation is { } preparing && preparing.TryGetMemberByServerSlot(Main.myPlayer, out _))
        {
            layers.Insert(0, new LegacyGameInterfaceLayer("Convergence: Preparation field", () =>
            {
                DrawFieldMask(Main.spriteBatch);
                return true;
            }, InterfaceScaleType.None));
            layers.Insert(1, new LegacyGameInterfaceLayer("Convergence: Preparation assembly", () =>
                preparationVisuals.DrawOverlay(Main.spriteBatch, preparing, state.EstimatedAuthorityTick), InterfaceScaleType.None));
            return;
        }
        // Accepted terminal, including the dead local player. The frame-local
        // layer expires even if no more server packets arrive; it changes no controls.
        if (state.Combat is null && visuals.IsEnding)
        {
            layers.Insert(0, new LegacyGameInterfaceLayer("Convergence: Raid result", () =>
            {
                FirstSeveranceResultVisuals.Draw(Main.spriteBatch, visuals.EndingVictory, visuals.EndingAge,
                    ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects);
                return false;
            }, InterfaceScaleType.None));
            return;
        }
        if (state.Combat is not { } combat || !combat.TryGetParticipantByServerSlot(Main.myPlayer, out var local) || !local.IsConnected)
            return;
        layers.Insert(0, new LegacyGameInterfaceLayer("Convergence: Black outside containment", () =>
        {
            DrawFieldMask(Main.spriteBatch);
            return true;
        }, InterfaceScaleType.None));
        bool rupture = IsRupture(state, out _);
        if (!rupture && !IsIntro(state, out _)) return;
        // The engine copies this list every frame. Preserve other Mods' layer
        // anchors; drawing false skips the remaining HUD for this frame only.
        // No persistent hideUI, inventory, cursor, zoom or control setting changes.
        layers.Insert(1, new LegacyGameInterfaceLayer("Convergence: Raid designation", () =>
        {
            if (rupture) DrawRupture(Main.spriteBatch, combat, state.EstimatedAuthorityTick);
            else DrawIntro(Main.spriteBatch, combat, state.EstimatedAuthorityTick);
            return false;
        }, InterfaceScaleType.None));
    }

    private static bool IsIntro(FirstSeveranceClientStateSystem state, out FirstSeveranceCombatProjection? intro)
    {
        intro = state.Combat;
        return intro is not null && intro.Substate == FirstSeveranceSubstate.SpawnIntro
            && state.EstimatedAuthorityTick < intro.ResolveTick
            && intro.TryGetParticipantByServerSlot(Main.myPlayer, out var local)
            && local.IsConnected && Main.LocalPlayer.active && !Main.LocalPlayer.dead;
    }

    private static bool IsRupture(FirstSeveranceClientStateSystem state, out FirstSeveranceCombatProjection? combat)
    {
        combat = state.Combat;
        return combat is { Substate: FirstSeveranceSubstate.PhaseTransition } && state.EstimatedAuthorityTick < combat.ResolveTick
            && combat.TryGetParticipantByServerSlot(Main.myPlayer, out var local) && local.IsConnected && !Main.LocalPlayer.dead;
    }

    private FirstSeveranceFieldMaskLayout? fieldMask;

    private void DrawFieldMask(SpriteBatch batch)
    {
        // GameInterfaceLayer(UI) calls SetZoom_UI before DrawSelf, which already
        // scales Main.screenWidth/Height. Dividing them by UIScale again shrinks
        // coverage. Use the world-pass capture in an identity/unscaled layer instead.
        if (fieldMask is not { } mask) return;
        Draw(mask.Top); Draw(mask.Bottom); Draw(mask.Left); Draw(mask.Right);
        void Draw(FirstSeveranceMaskRect rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;
            batch.Draw(TextureAssets.MagicPixel.Value, new Vector2(rect.X, rect.Y), new Rectangle(0, 0, 1, 1),
                Color.Black, 0, Vector2.Zero, new Vector2(rect.Width, rect.Height), SpriteEffects.None, 0);
        }
    }

    private static void DrawRupture(SpriteBatch batch, FirstSeveranceCombatProjection combat, ulong tick)
    {
        float age = FirstSeveranceStageVisuals.RuptureAge(combat, tick);
        float fade = FirstSeveranceStageVisuals.CameraWeight(age);
        var viewport = Main.instance.GraphicsDevice.Viewport;
        int w = viewport.Width, h = viewport.Height;
        var pixel = new Rectangle(0, 0, 1, 1);
        batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, w, h / 9), pixel, Color.Black * fade);
        batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, h * 8 / 9, w, h / 9 + 1), pixel, Color.Black * fade);
        string line = combat.BossPhase switch
        {
            FirstSeveranceBossPhase.Distant => age < .48f ? "[ LOCAL PRESENCE // RECEDING ]" : "[ PHASE III // REMOTE MANIFESTATION ]",
            FirstSeveranceBossPhase.Final => age < .48f ? "[ VITAL SIGNAL // ZERO ]" : "[ FINAL // TERMINATION DENIED ]",
            _ => age < .48f ? "[ SEAL INTEGRITY // CRITICAL ]" : "[ PHASE II // UNBOUND ]",
        };
        Utils.DrawBorderString(batch, line, new Vector2(w * .5f, h * .06f), Color.Silver * fade, 1, .5f);
        string titleKey = combat.BossPhase == FirstSeveranceBossPhase.Distant ? "DistantTitle"
            : combat.BossPhase == FirstSeveranceBossPhase.Final ? "FinalTitle" : "UnboundTitle";
        Utils.DrawBorderString(batch, Language.GetTextValue("Mods.Convergence.UI.FirstSeverance." + titleKey),
            new Vector2(w * .5f, h * .91f), Color.White * fade, 1.1f, .5f);
    }

    private static float IntroAge(FirstSeveranceCombatProjection combat, ulong tick)
        => 1f - Math.Clamp((float)((double)combat.ResolveTick - tick)
            / FirstSeveranceEncounterPlan.Instance.Timing.SpawnIntroTicks, 0f, 1f);

    private static void DrawIntro(SpriteBatch batch, FirstSeveranceCombatProjection intro, ulong tick)
    {
        float age = IntroAge(intro, tick);
        float fade = Math.Min(Math.Clamp(age * 9f, 0f, 1f), Math.Clamp((1f - age) * 7f, 0f, 1f));
        var viewport = Main.instance.GraphicsDevice.Viewport;
        float width = viewport.Width, height = viewport.Height;
        var pixel = new Rectangle(0, 0, 1, 1);
        batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, (int)width, (int)height), pixel,
            new Color(4, 3, 9) * (fade * 0.36f));
        int bar = (int)(height * 0.14f);
        batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, (int)width, bar), pixel, Color.Black * fade);
        batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, (int)height - bar, (int)width, bar), pixel, Color.Black * fade);
        Vector2 center = new(width * 0.5f, height * 0.46f);
        float lineWidth = Math.Min(660f, width * 0.78f) * FirstSeveranceVisualCurves.Ease(Math.Min(1, age * 4));
        batch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle((int)(center.X - lineWidth / 2), (int)center.Y - 52, (int)lineWidth, 1),
            pixel, Color.LightSlateGray * fade);
        batch.Draw(TextureAssets.MagicPixel.Value,
            new Rectangle((int)(center.X - lineWidth / 2), (int)center.Y + 91, (int)lineWidth, 1),
            pixel, Color.LightSlateGray * fade);
        Utils.DrawBorderString(batch, age < .43f ? "[ FIELD GEOMETRY // LOCKING ]" : "R A I D  / /  0 0 1", center + new Vector2(0, -87),
            Color.Silver * fade, 0.8f, 0.5f);
        Utils.DrawBorderString(batch, Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.IntroRaidName"),
            center + new Vector2(0, -28), Color.White * fade * FirstSeveranceVisualCurves.Ease((age - .15f) * 5), Math.Min(1.65f, width / 640f), 0.5f);
        Utils.DrawBorderString(batch, Language.GetTextValue("Mods.Convergence.NPCs.FirstSeverancePrototypeBoss.DisplayName").ToUpperInvariant(),
            center + new Vector2(0, 27), Color.LightGray * fade * FirstSeveranceVisualCurves.Ease((age - .38f) * 5), 1.2f, 0.5f);
        Utils.DrawBorderString(batch, age < .70f ? "[ APERTURE RISING // FLIGHT LINK ACTIVE ]" : "[ CONTAINMENT SEALED // NO RESPONSE ]", center + new Vector2(0, 113),
            Color.Gray * fade, 0.7f, 0.5f);
    }

    private static void DrawRing(SpriteBatch batch, Vector2 center, float radius, Color color, float width = 2f)
        => FirstSeveranceBossVisuals.Ring(batch, center, radius, color, width);
}
