using System;
using Convergence.Content.Items.Oboro;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Client.Weapons;

// Renderer/arm adapter for OboroHeldProj. No server graphics or hit decisions.
// Per-entity allocation is required: each player's holdout owns its own history.
[Autoload(Side = ModSide.Client)]
public sealed class OboroHeldVisuals : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        => entity.ModProjectile is OboroHeldProj;
    private readonly OboroSwingPresentation visual = new();
    private (ulong Generation, uint Swing) sounded;

    public override void PostAI(Projectile projectile)
    {
        if (projectile.ModProjectile is not OboroHeldProj { Ready: true }) { visual.Clear(); return; }
        Player player = Main.player[projectile.owner];
        var state = player.GetModPlayer<OboroPlayer>();
        Vector2 center = player.MountedCenter;
        visual.Update(state.View, state.VisualAge, state.SwingVisible, true,
            center.X, center.Y, player.direction, Main.GameUpdateCount);

        // Every update uses the same pose for blade, root and front arm. The
        // existing windup-only blend reaches the server's angle before damage.
        projectile.Center = new(visual.Pose.X, visual.Pose.Y);
        projectile.rotation = visual.Pose.Angle;
        if (visual.Swinging) player.ChangeDir(state.View.Facing);
        if (visual.Swinging || visual.Settling)
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, projectile.rotation - MathF.PI / 2);

        if (!visual.Swinging || visual.Pose.Progress < OboroRules.Windup(state.View.Step)
            || sounded == (state.View.Generation, state.View.Swing)) return;
        sounded = (state.View.Generation, state.View.Swing);
        SoundEngine.PlaySound(SoundID.Item1 with { Volume = .8f, Pitch = state.View.Step == 2 ? -.6f : .15f, MaxInstances = 4 }, player.Center);
        if (state.View.Step == 2) SoundEngine.PlaySound(SoundID.Item71 with { Volume = .5f, Pitch = -.4f }, player.Center);
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (projectile.ModProjectile is OboroHeldProj { Ready: true })
        {
            OboroArt.Afterimages(Main.spriteBatch, visual);
            OboroArt.Swing(Main.spriteBatch, visual.Pose, visual.Swinging);
        }
        return false;
    }

    public override void OnKill(Projectile projectile, int timeLeft) { visual.Clear(); sounded = default; }
}
