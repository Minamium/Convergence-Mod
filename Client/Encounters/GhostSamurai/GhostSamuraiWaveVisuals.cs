using System;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Convergence.Client.Encounters.GhostSamurai;

internal static class GhostSamuraiWaveVisuals
{
    internal static void Draw(SpriteBatch batch, SamuraiHazard h, Vector2 center, float age, bool locked, bool reduced)
        => GhostSamuraiCuts.Wave(batch, h, center, age, reduced);
}
