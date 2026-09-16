using System;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.GhostSamurai;

internal static class GhostSamuraiWaveVisuals
{
    internal static void Draw(SpriteBatch batch, SamuraiHazard h, Vector2 center, float age, bool locked)
    {
        Vector2 d = new(h.DX, h.DY), n = new(-h.DY, h.DX);
        Color ink = new(6, 13, 30), edge = locked ? new(255, 245, 198) : new(255, 180, 58);
        if (!h.Live(age))
        {
            // Dashed travel corridor announces the full lateral height. It is a
            // route, not a filled instant-damage beam; the wave appears at release.
            float reach = (h.End - h.Fire - 1) * SamuraiWaveRules.ChargedSlashWaveSpeed + h.Length / 2;
            for (int side = -1; side <= 1; side += 2)
            for (float along = 0; along < reach; along += 80)
            {
                Vector2 a = center + d * along + n * (h.Radius - 2) * side;
                Vector2 b = a + d * Math.Min(40, reach - along);
                GhostSamuraiVisuals.Stroke(batch, a, b, 4, ink);
                GhostSamuraiVisuals.Stroke(batch, a, b, 2, edge * .8f);
            }
            GhostSamuraiVisuals.Stroke(batch, center, center + d * reach, 5, ink * .8f);
            GhostSamuraiVisuals.Stroke(batch, center, center + d * reach, 2, edge * .6f);
            for (int arrow = 1; arrow <= 4; arrow++)
            {
                Vector2 tip = center + d * (arrow * reach / 5);
                GhostSamuraiVisuals.Stroke(batch, tip - d * 18 - n * 12, tip, 3, edge);
                GhostSamuraiVisuals.Stroke(batch, tip - d * 18 + n * 12, tip, 3, edge);
            }
            return;
        }
        // The rectangular outer outline is the real OBB, not a misleading curved
        // hitbox. Swept blade strands inside it make the forward direction clear.
        Color blade = new(215, 175, 255);
        Vector2 rear = center - d * h.Length / 2, front = center + d * h.Length / 2;
        GhostSamuraiVisuals.Stroke(batch, rear, front, h.Radius * 2, ink * .65f);
        GhostSamuraiVisuals.Stroke(batch, rear, front, h.Radius * 2 - 4, blade * .18f);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 offset = n * (h.Radius - 2) * side;
            GhostSamuraiVisuals.Stroke(batch, rear + offset, front + offset, 4, ink);
            GhostSamuraiVisuals.Stroke(batch, rear + offset, front + offset, 2, blade);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            float along = side * (h.Length / 2 - 2);
            GhostSamuraiVisuals.Stroke(batch, center + d * along - n * h.Radius, center + d * along + n * h.Radius, 4, ink);
            GhostSamuraiVisuals.Stroke(batch, center + d * along - n * h.Radius, center + d * along + n * h.Radius, 2, blade);
        }
        for (int strand = 0; strand < 3; strand++)
        for (int segment = 0; segment < 24; segment++)
        {
            float a = -1 + segment / 12f, b = -1 + (segment + 1) / 12f;
            Vector2 first = Point(a), last = Point(b);
            GhostSamuraiVisuals.Stroke(batch, first, last, strand == 0 ? 5 : 2, strand == 0 ? Color.White * .9f : blade * .7f);
            Vector2 Point(float t) => center + n * (t * (h.Radius - 5))
                + d * ((1 - t * t) * (h.Length * .5f - 7) - strand * 13);
        }
    }
}
