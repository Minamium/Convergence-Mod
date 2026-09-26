using System;
using Convergence.Content.Items.Oboro;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Weapons;

// 3段目だけの静→動。共有時計から描く装飾で、追加攻撃・画面全体の閃光は作らない。
internal static class OboroFinisherArt
{
    internal static void Draw(SpriteBatch batch, OboroBladePose pose, bool reduced)
    {
        float f = OboroThirdSwingMotion.Frame(pose.Progress);
        Vector2 root = new(pose.X, pose.Y), axis = pose.Angle.ToRotationVector2();
        Vector2 normal = new(-axis.Y, axis.X);
        if (f < OboroThirdSwingMotion.HoldEnd)
        {
            float charge = OboroRules.Ease(f / OboroThirdSwingMotion.PullEnd);
            // 輪を刀へ収束させる。刀と手は8～14Fで静止し、霊気だけが細く凝縮する。
            float tension = Math.Clamp((f - OboroThirdSwingMotion.PullEnd) / (OboroThirdSwingMotion.HoldEnd - OboroThirdSwingMotion.PullEnd), 0, 1);
            int count = reduced ? 2 : 4;
            for (int i = 0; i < count; i++)
            {
                float along = .36f + i * .14f;
                Vector2 at = root + axis * (pose.Length * along) + normal * ((i % 2 == 0 ? 1 : -1) * (14 - 11 * tension));
                OboroArt.Flame(batch, at, 18 - 8 * tension, charge * .55f);
            }
            OboroArt.Line(batch, root + axis * (pose.Length * .28f), root + axis * (pose.Length * .92f),
                2 + tension * 2, new Color(180, 126, 255, 0) * (charge * .48f));
            return;
        }

        // 解放直後に最大となり、振り抜きとともに細く消える月光。
        float release = OboroRules.Ease((f - OboroThirdSwingMotion.HoldEnd) / (OboroThirdSwingMotion.AccelerationEnd - OboroThirdSwingMotion.HoldEnd));
        float fade = 1 - OboroRules.Ease((f - OboroThirdSwingMotion.AccelerationEnd) / (OboroComboSettings.For(2).TotalFrames - OboroThirdSwingMotion.AccelerationEnd));
        float power = release * fade;
        OboroArt.Line(batch, root + axis * (pose.Length * .28f), root + axis * (pose.Length * .98f),
            reduced ? 3 : 7, new Color(239, 212, 255, 0) * (power * .6f));
        if (!reduced)
            OboroArt.Sword(batch, root - normal * (pose.Facing * 5), pose.Angle - pose.Facing * .055f,
                pose.Length, new Color(153, 78, 231) * (power * .35f), pose.Facing < 0);
    }
}
