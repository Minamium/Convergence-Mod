using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;

namespace Convergence.Client.Encounters.FirstSeverance;

internal static class FirstSeveranceResultVisuals
{
    internal static void Draw(SpriteBatch batch, bool victory, float age, bool reduced)
    {
        // Cinematics compose in physical pixels with InterfaceScaleType.None.
        // UI callbacks may already rescale Main.screenWidth/Height: never divide again.
        var viewport = Main.instance.GraphicsDevice.Viewport;
        int width = viewport.Width, height = viewport.Height;
        float enter = FirstSeveranceVisualCurves.Window(age, 0, .10);
        float leave = 1 - FirstSeveranceVisualCurves.Window(age, .87, 1);
        float fade = enter * leave;
        Color accent = victory ? new Color(211, 198, 239) : new Color(189, 87, 109);
        Rectangle pixel = new(0, 0, 1, 1);
        // Victory stays open for the singularity at .80; defeat closes into black.
        float pressure = victory ? .16f : .38f + .40f * FirstSeveranceVisualCurves.Window(age, .02, .45);
        batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, width, height), pixel,
            new Color(3, 2, 7) * (pressure * fade * (reduced ? .65f : 1)));
        if (victory)
        {
            // Two shaped exposure pulses, not a repeating screen-wide strobe.
            float rupture = FirstSeveranceVisualCurves.Window(age, .035, .046)
                * (1 - FirstSeveranceVisualCurves.Window(age, .046, .105));
            float extinction = FirstSeveranceVisualCurves.Window(age, .785, .798)
                * (1 - FirstSeveranceVisualCurves.Window(age, .798, .90));
            batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, width, height), pixel,
                new Color(233, 222, 255) * ((rupture * .28f + extinction * .65f) * (reduced ? .06f : 1)));
        }
        int bars = (int)(height * .15f * fade);
        batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, width, bars), pixel, Color.Black);
        batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, height - bars, width, bars), pixel, Color.Black);

        float inscription = FirstSeveranceVisualCurves.Window(age, victory ? .58 : .15, victory ? .85 : .33) * leave;
        float axisY = victory ? height * .80f : height * .48f;
        float span = Math.Min(720, width * .8f) * FirstSeveranceVisualCurves.Window(age, .05, .35);
        for (int side = -1; side <= 1; side += 2)
        {
            int y = (int)(axisY + side * 43);
            batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)((width - span) * .5f), y, (int)span, 1), pixel, accent * fade * .65f);
            int x = (int)(width * .5f + side * span * .5f);
            batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, y - 7, 2, 15), pixel, accent * fade);
        }
        string key = "Mods.Convergence.UI.FirstSeverance.";
        Utils.DrawBorderString(batch, Language.GetTextValue(key + "IntroRaidName").ToUpperInvariant(),
            new Vector2(width * .5f, height * .064f), Color.Silver * fade, .85f, .5f);
        Utils.DrawBorderString(batch, victory ? "[ RAID SIGNAL // TERMINATED ]" : "[ PARTICIPANT SIGNAL // LOST ]",
            new Vector2(width * .5f, axisY - 72), accent * inscription, .75f, .5f);
        Utils.DrawBorderString(batch, Language.GetTextValue(key + (victory ? "ResultVictory" : "ResultDefeat")),
            new Vector2(width * .5f, axisY - 19), Color.White * inscription, Math.Min(1.55f, width / 660f), .5f);
        Utils.DrawBorderString(batch, Language.GetTextValue(key + (victory ? "ResultVictoryDetail" : "ResultDefeatDetail")),
            new Vector2(width * .5f, height * .91f), accent * inscription, .8f, .5f);
    }
}
