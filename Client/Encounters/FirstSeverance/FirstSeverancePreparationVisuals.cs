#nullable enable
using System;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;

namespace Convergence.Client.Encounters.FirstSeverance;

// Read-only replica presentation. World labels use the world transform; the
// cinematic and clickable Ready panel use physical pixels in a None UI layer.
internal sealed class FirstSeverancePreparationVisuals
{
    private FightId fight;
    private ReLogic.Utilities.SlotId deploymentSound;

    internal void Update(FirstSeveranceClientStateSystem state)
    {
        if (state.Preparation is not { } prep || !prep.TryGetMemberByServerSlot(Main.myPlayer, out _))
        {
            Reset();
            return;
        }
        if (state.EstimatedAuthorityTick >= prep.ReadyOpensTick
            && SoundEngine.TryGetActiveSound(deploymentSound, out var deploying))
        {
            float gain = 1 - Math.Clamp((state.EstimatedAuthorityTick - prep.ReadyOpensTick) / 6f, 0, 1);
            deploying.Volume = gain;
            if (gain <= 0) deploying.Stop();
        }
        if (fight == prep.FightId) return;
        Reset();
        fight = prep.FightId;
        if (state.EstimatedAuthorityTick < prep.EnteredTick + 30)
            deploymentSound = SoundEngine.PlaySound(new SoundStyle("Convergence/Assets/Sounds/FirstSeverance/EnergyGather")
            { Volume = .65f, MaxInstances = 1, PlayOnlyIfFocused = true, PauseBehavior = PauseBehavior.StopWhenGamePaused });
    }

    internal void Reset()
    {
        if (!fight.IsNone && SoundEngine.TryGetActiveSound(deploymentSound, out var sound)) sound.Stop();
        fight = FightId.None;
    }

    internal void DrawReadyLabels(SpriteBatch batch, FirstSeveranceClientStateSystem state)
    {
        if (state.Preparation is not { } prep || state.EstimatedAuthorityTick < prep.ReadyOpensTick
            || !prep.TryGetMemberByServerSlot(Main.myPlayer, out _)) return;
        foreach (var member in prep.Members)
        {
            Player player = Main.player[member.ServerWhoAmI];
            if (!member.IsReady || !player.active || player.dead) continue;
            Utils.DrawBorderString(batch, "Ready!", player.Top - Main.screenPosition - new Vector2(0, 24),
                new Color(155, 244, 220), .70f, .5f);
        }
    }

    internal bool DrawOverlay(SpriteBatch batch, FirstSeverancePreparationProjection prep, ulong tick)
    {
        var viewport = Main.instance.GraphicsDevice.Viewport;
        int width = viewport.Width, height = viewport.Height;
        if (tick < prep.ReadyOpensTick)
        {
            float age = Math.Clamp((float)((double)tick - prep.EnteredTick) / FirstSeverancePreparationTimeline.DeploymentTicks, 0, 1);
            float fade = Math.Min(Math.Clamp(age * 12, 0, 1), Math.Clamp((1 - age) * 8, 0, 1));
            Fill(new Rectangle(0, 0, width, height / 9), Color.Black * fade);
            Fill(new Rectangle(0, height * 8 / 9, width, height / 9 + 1), Color.Black * fade);
            // Let the structure explain deployment; no duplicate status captions.
            Fill(new Rectangle(width / 2 - 38, (int)(height * .94f), (int)(76 * age), 1),
                new Color(177, 208, 209) * fade);
            return false; // Frame-local HUD suppression, no persistent hideUI/control flags.
        }
        int ready = 0;
        foreach (var member in prep.Members) if (member.IsReady) ready++;
        if (!prep.TryGetMemberByServerSlot(Main.myPlayer, out var local)) return true;
        // One compact pill: reversible Ready action + count, no second header.
        var button = new Rectangle(width / 2 - 100, 64, 200, 36);
        bool hover = button.Contains(Main.mouseX, Main.mouseY);
        Color accent = local.IsReady ? new Color(177, 238, 226) : new Color(190, 203, 211);
        Fill(button, (hover ? new Color(28, 40, 45) : new Color(10, 15, 20)) * .92f);
        Fill(new Rectangle(button.X + 12, button.Bottom - 1, (button.Width - 24) * ready / prep.Members.Count, 1), accent * .75f);
        Fill(new Rectangle(button.X + 14, button.Y + 13, 6, 6), accent * (local.IsReady ? 1f : .25f));
        Utils.DrawBorderString(batch, "READY", new(button.X + 30, button.Y + 8), accent, .7f);
        Utils.DrawBorderString(batch, $"{ready}/{prep.Members.Count}", new(button.Right - 13, button.Y + 8), Color.Silver, .7f, 1f);
        if (hover)
        {
            Main.LocalPlayer.mouseInterface = true;
            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                Main.mouseLeftRelease = false;
                FirstSeveranceClientActions.InteractWithCore(prep.CoreTopLeft.X, prep.CoreTopLeft.Y);
            }
        }
        return true;

        void Fill(Rectangle rect, Color color) => batch.Draw(TextureAssets.MagicPixel.Value, rect, color);
    }

}
