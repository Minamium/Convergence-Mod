using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Items.Oboro;

// One harmless, hand-anchored holdout per wielder. Timing/hits remain in OboroPlayer;
// clients consume that existing snapshot rather than advance a competing combo clock.
public sealed class OboroHeldProj : ModProjectile
{
    internal ulong ConnectionGeneration;
    internal int comboIndex { get; private set; }
    internal float timer { get; private set; }
    internal int attackDuration { get; private set; }
    internal bool Ready { get; private set; }
    internal OboroComboStep Settings => OboroComboSettings.For(comboIndex);
    internal OboroMotionPhase Phase => attackDuration == 0 ? OboroMotionPhase.Idle
        : Settings.Phase(timer / attackDuration);

    public override string Texture => "Convergence/Assets/Textures/Items/Oboro/Blade";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 900;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.aiStyle = -1;
        Projectile.friendly = Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.timeLeft = 180; // Bounded grace if projectile arrives before weapon snapshot.
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false; // No native projectile hit path.
    public override bool? CanCutTiles() => false;
    public override bool PreDraw(ref Color lightColor) => false; // Client GlobalProjectile draws it.

    public override void AI()
    {
        Ready = false;
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
        {
            if (OboroPlayer.Authority) Projectile.Kill();
            return;
        }
        Player player = Main.player[Projectile.owner];
        var state = player.GetModPlayer<OboroPlayer>();
        if (!state.Usable || !state.Holding || ConnectionGeneration == 0
            || state.View.Generation != ConnectionGeneration || !state.BindHeld(this))
        {
            if (OboroPlayer.Authority) Projectile.Kill();
            return; // Clients wait silently for matching state; never show another connection's sword.
        }

        Ready = true;
        Projectile.timeLeft = 180;
        comboIndex = state.View.Step;
        timer = state.VisualAge;
        attackDuration = state.View.Duration;

        // 1. Read accepted combo clock. 2. Resolve settings. 3. Place/rotate the blade.
        // Client PostAI adds the existing harmless entry/return blend and arm pose.
        Vector2 aim = state.View.Aim.ToRotationVector2();
        Projectile.Center = player.MountedCenter + aim * Settings.HoldOffset;
        Projectile.rotation = state.View.Aim + state.View.Facing * OboroRules.Offset(comboIndex,
            attackDuration == 0 ? 1 : timer / attackDuration);
        Projectile.direction = Projectile.spriteDirection = state.View.Facing;
        Projectile.velocity = Vector2.Zero;
        // Do not pin itemTime/itemAnimation: autoReuse must be able to request the next step.
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        => overPlayers.Add(index);

    public override void SendExtraAI(BinaryWriter writer) => writer.Write(ConnectionGeneration);
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        ulong generation = reader.ReadUInt64();
        // Server never accepts an owner-client claim as the weapon identity.
        if (Main.netMode == NetmodeID.MultiplayerClient) ConnectionGeneration = generation;
    }

    public override void OnKill(int timeLeft)
    {
        Ready = false;
        comboIndex = attackDuration = 0; timer = 0;
        if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
            Main.player[Projectile.owner]?.GetModPlayer<OboroPlayer>().ReleaseHeld(this);
    }
}
