using System;
using System.Collections.Generic;
using Luminance.Common.Easings;
using Luminance.Common.VerletIntergration;
using Microsoft.Xna.Framework;
using Terraria;

namespace Convergence.Client.Encounters.GhostSamurai;

// Five short, client-owned paper tethers. Luminance supplies the constrained
// solver; roots follow the accepted actor, never the local player's position.
internal sealed class GhostSamuraiSecondaryMotion
{
    private readonly List<VerletSegment>[] chains = new List<VerletSegment>[5];
    private readonly Vector2[,] previous = new Vector2[5, 4];
    private bool ready;
    private static readonly VerletSettings settings = new(TileCollision: false, SlowInWater: false, Gravity: .14f, MaxFallSpeed: 3);
    internal static readonly PiecewiseCurve HitRelease = new PiecewiseCurve()
        .Add(EasingCurves.Cubic, EasingType.Out, 1, .15f)
        .Add(EasingCurves.Sine, EasingType.Out, 0, 1);
    internal GhostSamuraiSecondaryMotion() { for (int i = 0; i < 5; i++) chains[i] = new(4); }
    internal void Clear() { ready = false; foreach (var chain in chains) chain.Clear(); Array.Clear(previous); }
    internal void Update(in SamuraiRigPose p, Vector2 movement)
    {
        if (movement.LengthSquared() > 200 * 200) Clear();
        float rotation = p.Lean * .35f + MathF.Sin(p.Age * .021f + 1.1f) * .025f;
        for (int i = 0; i < 5; i++)
        {
            var chain = chains[i];
            float angle = MathHelper.Pi + i * MathHelper.Pi / 4;
            Vector2 direction = angle.ToRotationVector2();
            Vector2 root = new Vector2(p.X, p.Y) + new Vector2(direction.X * 97, -58 + direction.Y * 121).RotatedBy(rotation) * p.Scale;
            if (!ready)
                for (int j = 0; j < 4; j++) { var point = root + Vector2.UnitY * j * 10; chain.Add(new(point, Vector2.Zero, j == 0)); previous[i, j] = point; }
            else for (int j = 0; j < 4; j++) previous[i, j] = chain[j].Position;
            // Translate the previous pose's tether root together with its anchor.
            // Inertia remains bounded during an 85px/tick dash.
            Vector2 anchorMove = root - chain[0].Position;
            for (int j = 0; j < 4; j++)
            {
                chain[j].Position += anchorMove * (1 - j * .055f);
                chain[j].OldPosition += anchorMove * (1 - j * .055f);
            }
            chain[0].Position = chain[0].OldPosition = root;
            for (int j = 1; j < 4; j++)
                chain[j].Velocity = Vector2.Clamp(chain[j].Velocity * .76f + new Vector2(MathF.Sin(p.Age * .045f + i + j) * .06f + p.Hit * direction.X * .38f, 0) - movement * .006f, new(-2), new(2));
            VerletSimulations.VerletSimulation(chain, 10, settings, 5);
            for (int j = 1; j < 4; j++)
            {
                Vector2 delta = chain[j].Position - root;
                if (!float.IsFinite(delta.X) || !float.IsFinite(delta.Y)) chain[j].Position = root + Vector2.UnitY * 10 * j;
                else if (delta.LengthSquared() > 40 * 40) chain[j].Position = root + delta.SafeNormalize(Vector2.UnitY) * 40;
            }
        }
        ready = true;
    }
    internal bool Sample(int index, float fraction, out Vector2 anchor, out float rotation)
    {
        anchor = default; rotation = 0;
        if (!ready) return false;
        var chain = chains[index];
        anchor = Vector2.Lerp(previous[index, 0], chain[0].Position, fraction);
        Vector2 tip = Vector2.Lerp(previous[index, 3], chain[3].Position, fraction);
        rotation = (tip - anchor).ToRotation() - MathHelper.PiOver2;
        return true;
    }
}
