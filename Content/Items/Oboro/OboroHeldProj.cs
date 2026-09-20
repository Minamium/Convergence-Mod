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
    internal float Progress => attackDuration == 0 ? 0 : timer / attackDuration;
    internal OboroMotionPhase Phase => attackDuration == 0 ? OboroMotionPhase.Idle
        : comboIndex switch { 0 => OboroFirstSwingMotion.Phase(Progress),
            1 => OboroSecondSwingMotion.Phase(Progress), _ => OboroThirdSwingMotion.Phase(Progress) };

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

        // comboIndex: 0/1/2、timer: 現在段の経過F、attackDuration: 速度補正後の総F。
        // OboroTimingが押下継続中に0→1→2→0へ進める。ここでは独自にtimer++しない。
        // 入力を離した場合も現在段の終了までは進み、その後Idleへ戻る。
        Projectile.rotation = state.View.Aim + state.View.Facing * OboroRules.Offset(comboIndex,
            attackDuration == 0 ? 1 : Progress);
        var hand = OboroHandAnchor.Capture(player, state.View.Facing);
        var offset = OboroRules.RootOffset(comboIndex, attackDuration == 0 ? 1 : Progress, state.View.Aim, Projectile.rotation, hand);
        Projectile.Center = player.MountedCenter + new Vector2(offset.X, offset.Y);
        Projectile.direction = Projectile.spriteDirection = state.View.Facing;
        Projectile.velocity = Vector2.Zero;
        // Do not pin itemTime/itemAnimation: native input can start a new sequence after release.
        // While swinging, OboroTiming owns step transitions independently of autoReuse.
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
