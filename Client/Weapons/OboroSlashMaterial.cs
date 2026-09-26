using System;
using Convergence.Client.Graphics;
using Convergence.Content.Items.Oboro;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Weapons;

// 履歴の実際の刀軸を霊刃にする。固定円の画像や巨大な刀PNGを回す表現ではない。
// 所有者はholdoutの履歴。GPU資源はLuminance所有、こちらは固定長の頂点配列のみ。
internal static class OboroSlashMaterial
{
    private static readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[16 * 48 * 6];

    internal static void Draw(SpriteBatch batch, OboroSwingPresentation history)
    {
        if (history.Count == 0) return;
        bool reduced = OboroArt.Reduced;
        int used = 0;
        for (int i = 1; i < history.Count; i++)
        {
            var a = history.Echo(i - 1); var b = history.Echo(i);
            if (a.Swing != b.Swing || b.At - a.At > 2) continue;
            float fadeA = OboroSwingPresentation.Opacity(a.At, Main.GameUpdateCount, a.Pose.Step);
            float fadeB = OboroSwingPresentation.Opacity(b.At, Main.GameUpdateCount, b.Pose.Step);
            float turn = b.Pose.Angle - a.Pose.Angle;
            if (Math.Abs(turn) < .001f || fadeB <= 0) continue;
            int count = Math.Clamp((int)MathF.Ceiling(Math.Abs(turn) / (reduced ? .085f : .045f)), 1, 48);
            float width = a.Pose.Step switch { 1 => 70, 2 => 210, _ => 140 };
            for (int n = 0; n < count; n++)
            {
                float t0 = n / (float)count, t1 = (n + 1f) / count;
                float ageA = Main.GameUpdateCount - a.At, ageB = Main.GameUpdateCount - b.At;
                Edge(a.Pose, b.Pose, t0, width, MathHelper.Lerp(fadeA, fadeB, t0), MathHelper.Lerp(ageA, ageB, t0), out var innerA, out var outerA);
                Edge(a.Pose, b.Pose, t1, width, MathHelper.Lerp(fadeA, fadeB, t1), MathHelper.Lerp(ageA, ageB, t1), out var innerB, out var outerB);
                vertices[used++] = innerA; vertices[used++] = outerA; vertices[used++] = innerB;
                vertices[used++] = innerB; vertices[used++] = outerA; vertices[used++] = outerB;
            }
        }
        if (used == 0 && !history.Swinging) return;
        using var scope = new WorldGraphicsScope(batch);
        var device = Main.instance.GraphicsDevice;
        var shader = ShaderManager.GetShader("Convergence.OboroMoonArc");
        shader.TrySetParameter("uWorldViewProjection", Main.GameViewMatrix.TransformationMatrix *
            Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
        shader.TrySetParameter("clock", Main.GameUpdateCount / 60f);
        shader.TrySetParameter("reduced", reduced ? 1f : 0f);
        shader.Apply();
        if (used > 0) device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, used / 3);

        var pose = history.Pose;
        if (!history.Swinging || !OboroRules.Live(pose.Step, pose.Progress)) return;
        // 判定がある瞬間は、実体の切っ先から既存の560px先まで鋭い刃を必ず描く。
        // 残像は無害だが、現フレームの白い刃先は判定の終点と一致する。
        Vector2 root = new(pose.X, pose.Y), axis = pose.Angle.ToRotationVector2(), normal = new(-axis.Y, axis.X);
        Vector2 start = root + axis * (OboroSwingPresentation.SwordLength * .55f), end = root + axis * OboroRules.Reach;
        float halfWidth = pose.Step switch { 1 => 8, 2 => 19, _ => 12 };
        Quad(start, end, normal * halfWidth);
        shader.Apply("BladePass"); device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
    }

    private static void Edge(OboroBladePose a, OboroBladePose b, float t, float width, float fade, float age,
        out VertexPositionColorTexture inner, out VertexPositionColorTexture outer)
    {
        float angle = MathHelper.Lerp(a.Angle, b.Angle, t);
        Vector2 root = Vector2.Lerp(new(a.X, a.Y), new(b.X, b.Y), t), axis = angle.ToRotationVector2();
        // 消えるほど内側を絞る。外縁を判定の外へ膨らませない。
        // 先端は一点へ、直後は厚い三日月、末尾は細く消える。扇形の板を残さない。
        float depth = .6f + width * MathF.Sqrt(fade) * OboroRules.Ease(age / 1.8f);
        var color = Color.White * fade;
        float u = angle * a.Facing * 1.7f;
        inner = new(new(root + axis * (OboroRules.Reach - depth) - Main.screenPosition, 0), color, new(u, 0));
        outer = new(new(root + axis * OboroRules.Reach - Main.screenPosition, 0), color, new(u, 1));
    }
    private static void Quad(Vector2 start, Vector2 end, Vector2 normal)
    {
        start -= Main.screenPosition; end -= Main.screenPosition;
        vertices[0] = new(new(start - normal, 0), Color.White, new(0, 0));
        vertices[1] = new(new(start + normal, 0), Color.White, new(0, 1));
        vertices[2] = new(new(end - normal, 0), Color.White, new(1, 0));
        vertices[3] = vertices[2]; vertices[4] = vertices[1];
        vertices[5] = new(new(end + normal, 0), Color.White, new(1, 1));
    }
}
