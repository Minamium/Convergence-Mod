using System;
using Convergence.Client.Encounters.GhostSamurai;
using Convergence.Content.Encounters.GhostSamurai;

namespace Convergence.DomainTests;

internal static partial class Program
{
    [DomainTest("Samurai body stays visible before initial sync and across all attack and phase poses")]
    private static void SamuraiBodyVisibility()
    {
        foreach (float age in new[] { -1f, 0, 1, 12, 24, 30, 900, float.NaN })
        {
            float opacity = SamuraiSpriteFrames.BodyOpacity(age);
            AssertEqual(true, float.IsFinite(opacity) && opacity >= .55f && opacity <= 1, "visible body floor");
        }
        AssertEqual(1f, SamuraiSpriteFrames.BodyOpacity(24), "materialization ends at24");
        // Start/late snapshots, idle/movement, every attack, recovery and both
        // phase transitions use the same atlas; none may select an empty cell.
        foreach (SamuraiAttack attack in Enum.GetValues<SamuraiAttack>())
        foreach (SamuraiBeat beat in Enum.GetValues<SamuraiBeat>())
        foreach (float timer in new[] { 0f, 30, 120, 300, 900 })
        foreach (float pose in new[] { -1f, 0, 1 })
        {
            int frame = beat == SamuraiBeat.Transition ? 11 : SamuraiSpriteFrames.Select(attack, beat, timer, timer, pose);
            AssertEqual(true, frame >= 0 && frame < SamuraiSpriteFrames.Count, "selected frame");
            var cell = SamuraiSpriteFrames.Cell(frame, 1254, 1254);
            float scale = 390f / cell.Height;
            AssertEqual(true, cell.Width > 0 && cell.Height > 0 && float.IsFinite(scale) && scale > 0, "finite nonempty sprite");
        }
    }
}
