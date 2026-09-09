#nullable enable
using System;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;

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
            Utils.DrawBorderString(batch, "[ CONTAINMENT // ESTABLISHING ]", new(width * .5f, height * .06f), Color.Silver * fade, 1f, .5f);
            Utils.DrawBorderString(batch, Text("Deploying"), new(width * .5f, height * .91f), Color.White * fade, .95f, .5f);
            return false; // Frame-local HUD suppression, no persistent hideUI/control flags.
        }
        int ready = 0;
        foreach (var member in prep.Members) if (member.IsReady) ready++;
        Utils.DrawBorderString(batch, $"READY  {ready} / {prep.Members.Count}", new(width * .5f, 72), Color.Silver, .9f, .5f);
        if (!prep.TryGetMemberByServerSlot(Main.myPlayer, out var local)) return true;
        var button = new Rectangle(width / 2 - 115, 105, 230, 42);
        bool hover = button.Contains(Main.mouseX, Main.mouseY);
        Fill(button, (hover ? new Color(38, 61, 63) : new Color(15, 23, 28)) * .94f);
        Utils.DrawBorderString(batch, Text(local.IsReady ? "Unready" : "Ready"), new(width * .5f, 114), new Color(177, 238, 226), .85f, .5f);
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

    private static string Text(string key) => Language.GetTextValue("Mods.Convergence.UI.PreparationDeployment." + key);
}
