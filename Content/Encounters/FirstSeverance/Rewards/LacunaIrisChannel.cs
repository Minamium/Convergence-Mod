#nullable enable
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Score = Convergence.Content.Encounters.FirstSeverance.Rewards.LacunaTestamentScore;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Lacuna Testament held controller (docs/encounters/first-severance/WEAPONS.md, "Magic — Lacuna Testament"). One per
// channel; it carries everything a peer needs to draw and hear the same ritual:
//   ai[0] score age (LacunaTestamentScore), ai[1] the press's arch facing (+-1), ai[2] 0 live / -1..-20 fading,
//   velocity the aim (resent every 6 ticks while turning, every 30 otherwise), ExtraAI the end cause.
// Every iris count, frame, dock, click, width and pulse is derived from the age. The persistent beam is this
// projectile's own collision once the score opens it. Ownership: only the owner client samples the cursor, pays mana
// (native CheckMana, automatic potions blocked) and spawns pellets through native projectile replication. Item change,
// crowd control, noItems, death and Doll-Raid Down/elimination end it at once; a release or an empty mana pool starts
// the 20-tick fade, in which nothing damages. Drawn and heard by the client presentation only; no packet.
public sealed class LacunaIrisChannel : ModProjectile
{
    // Logical NPC roots hit by the beam: one hit per root every HitCadence ticks, so segments cannot multiply it.
    // Created on first use (the type's template never allocates it).
    private ulong[]? nextRootHit;
    private LacunaEnd end;

    internal float Age => Projectile.ai[0];
    internal int Facing => Projectile.ai[1] >= 0 ? 1 : -1;
    internal float FadeState => Projectile.ai[2];
    internal bool Fading => Projectile.ai[2] < 0;
    internal LacunaEnd End => end;
    internal Vector2 Axis => RitualArmamentItems.Aim(Projectile.velocity, Facing);
    internal Vector2 Muzzle => Projectile.Center + Axis * Score.MuzzleDistance;
    internal float GravDir => Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers && Main.player[Projectile.owner].gravDir < 0 ? -1 : 1;
    internal bool Live => Score.Live(Age, FadeState);

    public override string Texture => RitualArmamentItems.TexturePath;

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)(Score.MuzzleDistance + Score.Length + 200);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = Score.HitCadence;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    // Drawn by the Doll weapon layer (Client/Encounters/FirstSeverance/Weapons/LacunaVisuals).
    public override bool PreDraw(ref Color lightColor) => false;
    public override bool? CanDamage() => Live ? null : false;

    public override bool? CanHitNPC(NPC target)
        => nextRootHit is not null && Main.GameUpdateCount < nextRootHit[RitualTargeting.Root(target)] ? false : null;

    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || !Score.ValidFade(Projectile.ai[2]) || MathF.Abs(Projectile.ai[1]) != 1)
        {
            Projectile.Kill();
            return;
        }
        Player owner = Main.player[Projectile.owner];
        if (!RitualChannel.Valid(Projectile, owner, ModContent.ItemType<LacunaTestament>()))
        {
            Projectile.Kill();
            return;
        }
        if (RitualChannel.Fade(Projectile)) return;
        if (Projectile.owner == Main.myPlayer && !owner.channel)
        {
            Stop(LacunaEnd.Release);
            return;
        }
        RitualChannel.Hold(Projectile, owner, Score.TurnCap(Age));
        Projectile.ai[0]++;
        owner.manaRegenDelay = Math.Max(owner.manaRegenDelay, 60);
        // Ongoing equipment and Mana Sickness apply every tick, not the stats at the press.
        int weaponDamage = owner.GetWeaponDamage(owner.HeldItem);
        Projectile.damage = Score.BeamDamage(weaponDamage);
        if (Projectile.owner != Main.myPlayer) return;
        int tick = (int)Age;
        if (Score.Pays(tick) && !owner.CheckMana(owner.HeldItem, Score.ManaCost(owner.GetManaCost(owner.HeldItem), tick), pay: true, blockQuickMana: true))
        {
            Stop(LacunaEnd.Starved);
            return;
        }
        for (int i = 0; i < Score.Irises; i++)
        {
            if (!Score.ShotAt(tick, i)) continue;
            Vector2 hole = Projectile.Center + ToXna(Score.Seat(i, Facing, GravDir));
            Vector2 aim = RitualArmamentItems.Aim(Main.MouseWorld - hole, owner.direction);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), hole + aim * Score.PelletHoleOffset, aim * Score.PelletLaunch,
                ModContent.ProjectileType<LacunaPellet>(), Score.PelletDamage(weaponDamage), Projectile.knockBack, Projectile.owner, 0, -1, i);
        }
    }

    // Owner only: the release (or the failed ritual) starts the fade on every peer through the next sync.
    private void Stop(LacunaEnd cause)
    {
        if (Projectile.ai[2] < 0) return;
        end = cause;
        RitualChannel.Stop(Projectile);
    }

    // The beam: the throat (first ThroatLength px from the muzzle) at ThroatShare of the width, then the full width.
    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
    {
        if (!Live) return false;
        float reach = Score.Reach(Age), width = Score.Width(Age);
        if (reach <= 0 || width <= 0) return false;
        Vector2 axis = Axis, muzzle = Muzzle;
        float distance = 0;
        float throat = Math.Min(Score.ThroatLength, reach);
        if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), muzzle, muzzle + axis * throat,
                Score.CollisionWidth(0, width), ref distance))
            return true;
        return reach > Score.ThroatLength && Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            muzzle + axis * Score.ThroatLength, muzzle + axis * reach, Score.CollisionWidth(Score.ThroatLength, width), ref distance);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => (nextRootHit ??= new ulong[Main.maxNPCs])[RitualTargeting.Root(target)] = Main.GameUpdateCount + Score.HitCadence;

    public override void SendExtraAI(BinaryWriter writer) => writer.Write((byte)end);

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        byte cause = reader.ReadByte();
        end = cause <= (byte)LacunaEnd.Starved ? (LacunaEnd)cause : LacunaEnd.None;
    }

    private static Vector2 ToXna(System.Numerics.Vector2 v) => new(v.X, v.Y);
}
