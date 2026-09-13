using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

// A world-space field border follows the same accepted bounds as containment.
// No tiles, full-screen flash, camera lock or gameplay mutation.
internal sealed class GhostSamuraiFieldVisuals : ModSystem
{
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.ModNPC is not GhostSamuraiBoss boss || !boss.Arena.IsValid
                || !Main.LocalPlayer.GetModPlayer<GhostSamuraiContainmentPlayer>().BoundTo(boss)) continue;
            var field = boss.Arena;
            Vector2 a = new Vector2(field.Left, field.Top) - Main.screenPosition;
            Vector2 b = new Vector2(field.Right, field.Bottom) - Main.screenPosition;
            var batch = Main.spriteBatch;
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                Edge(a, new(b.X, a.Y)); Edge(new(b.X, a.Y), b);
                Edge(b, new(a.X, b.Y)); Edge(new(a.X, b.Y), a);
                // Small inward marks distinguish the playable side of each edge.
                for (float x = a.X + 64; x < b.X; x += 128)
                { Edge(new(x, a.Y), new(x, a.Y + 14)); Edge(new(x, b.Y), new(x, b.Y - 14)); }
                for (float y = a.Y + 64; y < b.Y; y += 128)
                { Edge(new(a.X, y), new(a.X + 14, y)); Edge(new(b.X, y), new(b.X - 14, y)); }
            }
            finally { batch.End(); }
            return;

            void Edge(Vector2 from, Vector2 to)
            {
                GhostSamuraiVisuals.Stroke(batch, from, to, 9, new Color(6, 13, 30));
                GhostSamuraiVisuals.Stroke(batch, from, to, 3, new Color(120, 218, 255));
            }
        }
    }
}

