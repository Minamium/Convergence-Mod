using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace Convergence.Content.Items.Oboro;

internal static class OboroHandAnchor
{
    // Player.directionを書き換えず、受理済みの向きにミラーする。専用サーバーでも使用可。
    internal static OboroHandBasis Capture(Player player, int facing)
    {
        Vector2 root = player.MountedCenter;
        Vector2 Sample(float angle)
        {
            bool flip = player.direction != facing;
            float query = flip ? MathF.PI - angle : angle;
            Vector2 offset = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, query - MathF.PI / 2) - root;
            if (flip) offset.X = -offset.X;
            return offset;
        }
        Vector2 right = Sample(0), left = Sample(MathF.PI), down = Sample(MathF.PI / 2);
        Vector2 center = (right + left) * .5f, along = right - center, across = down - center;
        return new(center.X, center.Y, along.X, along.Y, across.X, across.Y);
    }
    // 腕を刀の向きに保ち、根元の位置に最も近い伸びを選ぶ。刀の判定は動かさない。
    internal static Player.CompositeArmStretchAmount Stretch(Player player, Vector2 grip, float bladeAngle)
    {
        var best = Player.CompositeArmStretchAmount.Full; float distance = float.MaxValue;
        for (int i = 0; i < 4; i++)
        {
            var stretch = (Player.CompositeArmStretchAmount)i;
            float candidate = Vector2.DistanceSquared(grip, player.GetFrontHandPosition(stretch, bladeAngle - MathF.PI / 2));
            if (candidate < distance) { distance = candidate; best = stretch; }
        }
        return best;
    }
}
