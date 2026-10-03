#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using NVector2 = System.Numerics.Vector2;

namespace Convergence.Content.Encounters.FirstSeverance.Rewards;

// Choir of the Unmade, 2026-10 refresh (docs/encounters/first-severance/WEAPONS.md#choir-of-the-unmade-2026-10).
// ChoirConcertRules owns every time, shape and multiplier; these types apply them through ordinary native minion
// and projectile replication: the owner reads its minion target, spawns the notes and the beam, and the lead
// voice's ai[0]/ai[1] (clock, target) are what every peer replays. No packet, no encounter state.
internal static class ChoirConcert
{
    internal static NVector2 N(Vector2 v) => new(v.X, v.Y);
    internal static Vector2 X(NVector2 v) => new(v.X, v.Y);

    // Down, death, a Raid elimination, item restrictions and stuns halt the concert (the voices stay summoned).
    internal static bool Usable(Player owner) => RitualArmamentItems.Usable(owner) && !owner.noItems && !owner.CCed;

    // The owner's lead voice (lowest native identity), with the voice count and their summed current damage.
    internal static ChoirChorister? Lead(int owner, out int voices, out double damage)
    {
        ChoirChorister? lead = null;
        voices = 0;
        damage = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.owner != owner || p.ModProjectile is not ChoirChorister voice) continue;
            voices++;
            damage += Math.Max(0, p.damage);
            if (lead is null || p.identity < lead.Projectile.identity) lead = voice;
        }
        return lead;
    }

    internal static bool HasChorus(int owner)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.ModProjectile is ChoirChorus) return true;
        return false;
    }
}

// Targets are measured from the owner, not from the dolls: a doll inside a ceiling never stops the concert.
// The manual minion target comes first, otherwise the nearest valid NPC; acquire 1800 px, retain 2100 px.
internal static class ChoirTargeting
{
    internal static bool Valid(NPC npc, Projectile projectile, Vector2 from, float range)
        => npc.active && npc.CanBeChasedBy(projectile) && Vector2.DistanceSquared(npc.Center, from) <= range * range
            && Collision.CanHitLine(from, 1, 1, npc.position, npc.width, npc.height);

    // The lead's target. Only the owner judges range, sight and CanBeChasedBy; a peer keeps the replicated choice
    // while that NPC exists, because its copy of the owner's position lags and must never end a concert the owner
    // is still singing. The owner ends it by netUpdate (ai[1] = -1 with the clock at 0).
    internal static NPC? Current(Projectile projectile, Player owner)
    {
        int index = (int)projectile.ai[1];
        if (index < 0 || index >= Main.maxNPCs) return null;
        NPC npc = Main.npc[index];
        if (projectile.owner != Main.myPlayer) return npc.active ? npc : null;
        return Valid(npc, projectile, owner.MountedCenter, ChoirConcertRules.RetainRange) ? npc : null;
    }

    // A replicated target index (peers: the concert runs while the owner keeps one, even through an NPC's death
    // packet arriving before the owner's retarget).
    internal static bool Chosen(Projectile projectile) => (int)projectile.ai[1] >= 0 && (int)projectile.ai[1] < Main.maxNPCs;

    // Owner only: writes ai[1] with netUpdate when the choice changes; a replica keeps the replicated target.
    internal static NPC? Acquire(Projectile lead, Player owner)
    {
        NPC? chosen = Current(lead, owner);
        if (lead.owner != Main.myPlayer) return chosen;
        Vector2 from = owner.MountedCenter;
        if (owner.HasMinionAttackTargetNPC)
        {
            int id = owner.MinionAttackTargetNPC;
            if (id >= 0 && id < Main.maxNPCs && Valid(Main.npc[id], lead, from, ChoirConcertRules.AcquireRange)) chosen = Main.npc[id];
        }
        if (chosen is null)
        {
            float best = ChoirConcertRules.AcquireRange * ChoirConcertRules.AcquireRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                float distance = Vector2.DistanceSquared(from, npc.Center);
                if (distance < best && Valid(npc, lead, from, ChoirConcertRules.AcquireRange)) { chosen = npc; best = distance; }
            }
        }
        int next = chosen?.whoAmI ?? -1;
        if ((int)lead.ai[1] != next) { lead.ai[1] = next; lead.netUpdate = true; }
        return chosen;
    }
}

// One chorister (1 minion slot). ai[0] = concert clock (the lead's, copied by every voice; 0 idle, 1..Cycle),
// ai[1] = target NPC (-1 none), ai[2] = 0. Extra AI: Life, the ticks since this voice was summoned (capped), so a
// late replica never replays the summon cue. Never deals contact damage.
public sealed class ChoirChorister : ModProjectile
{
    private bool stageSet;
    private int previousClock, lastNoteBeat = -1;
    internal NVector2 Stage { get; private set; }
    internal int Life { get; private set; }
    internal int Ordinal { get; private set; }
    internal int Voices { get; private set; } = 1;
    internal bool IsLead { get; private set; } = true;
    internal float Clock => Projectile.ai[0];
    internal int Variant => Math.Abs(Projectile.identity) % 3;
    public override string Texture => "Convergence/Assets/Textures/Items/DollWeapons/Chorister0";

    public override void SetStaticDefaults()
    {
        Main.projPet[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = 32; Projectile.height = 56;
        Projectile.minion = true; Projectile.minionSlots = 1;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 18000;
    }

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool PreDraw(ref Color lightColor) => false;

    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || !(Projectile.ai[0] >= 0 && Projectile.ai[0] <= ChoirConcertRules.Cycle))
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        int buff = ModContent.BuffType<ChoirOfTheUnmadeBuff>();
        if (!owner.active || owner.dead) { owner.ClearBuff(buff); Projectile.Kill(); return; }
        bool authority = Projectile.owner == Main.myPlayer;
        // Only the owner reads a missing buff as dismissal (a peer's copy of the buff can arrive late).
        if (authority && !owner.HasBuff(buff)) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        Life = Math.Min(Life + 1, ushort.MaxValue);

        // Census: ordinal and lead by native identity, stable within one owner.
        Projectile lead = Projectile;
        int ordinal = 0, voices = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.owner != Projectile.owner || p.type != Type) continue;
            voices++;
            if (p.identity < Projectile.identity) ordinal++;
            if (p.identity < lead.identity) lead = p;
        }
        Ordinal = ordinal;
        Voices = Math.Max(1, voices);
        IsLead = lead.whoAmI == Projectile.whoAmI;
        bool usable = ChoirConcert.Usable(owner);

        // Every voice keeps the stage anchor, so a new lead already has it.
        Stage = ChoirConcertRules.EaseStage(Stage, stageSet, ChoirConcert.N(owner.MountedCenter));
        stageSet = true;

        int before = (int)Projectile.ai[0];
        NPC? target;
        if (IsLead)
        {
            int clock;
            if (authority)
            {
                target = usable ? ChoirTargeting.Acquire(Projectile, owner) : null;
                if (!usable && (int)Projectile.ai[1] != -1) { Projectile.ai[1] = -1; Projectile.netUpdate = true; }
                clock = ChoirConcertRules.Advance(before, target is not null);
                if ((clock == 0) != (before == 0) || clock > 0 && clock % 60 == 0) Projectile.netUpdate = true;
            }
            else
            {
                // A peer runs the clock while the owner keeps a target and stops it only on the owner's update.
                target = ChoirTargeting.Current(Projectile, owner);
                clock = ChoirConcertRules.Advance(before, ChoirTargeting.Chosen(Projectile));
            }
            Projectile.ai[0] = clock;
        }
        else
        {
            Projectile.ai[1] = lead.ai[1];
            Projectile.ai[0] = usable || !authority ? lead.ai[0] : 0;
            target = Projectile.ai[0] > 0 && (usable || !authority) ? ChoirTargeting.Current(lead, owner) : null;
        }

        float concert = Projectile.ai[0];
        NVector2 stage = lead.ModProjectile is ChoirChorister conductor ? conductor.Stage : Stage;
        NVector2 destination = ChoirConcertRules.Destination(ChoirConcert.N(owner.MountedCenter), stage, ordinal, Voices, concert, Life);
        Projectile.velocity = ChoirConcert.X(ChoirConcertRules.GlideVelocity(ChoirConcert.N(Projectile.Center),
            ChoirConcert.N(Projectile.velocity), destination));
        if (Vector2.DistanceSquared(Projectile.Center, owner.Center) > ChoirConcertRules.HomeTeleport * ChoirConcertRules.HomeTeleport)
        {
            Projectile.Center = ChoirConcert.X(destination);
            Projectile.velocity = Vector2.Zero;
            if (authority) Projectile.netUpdate = true;
        }

        // Dolls face the target; in the chorus they face the beam's way.
        float facing = target is not null
            ? target.Center.X - (ChoirConcertRules.Warning(concert) || ChoirConcertRules.Live(concert) ? stage.X : Projectile.Center.X)
            : Math.Abs(Projectile.velocity.X) > .6f ? Projectile.velocity.X : owner.direction;
        if (Math.Abs(facing) > .3f) Projectile.spriteDirection = facing >= 0 ? 1 : -1;
        if (authority && (Life + Projectile.identity) % 60 == 0) Projectile.netUpdate = true;

        int now = (int)concert;
        // A new concert (a stop or the wrap) re-arms the verse.
        if (now < previousClock || now == 0) lastNoteBeat = -1;
        if (authority && target is not null && usable && now > 0)
        {
            // Each verse note once per concert, even if this voice's part moved (a copied clock can also step by
            // two); never a note from before this voice appeared.
            int note = ChoirConcertRules.DueNote(now, ordinal, lastNoteBeat, Life - 1);
            if (note >= 0)
            {
                lastNoteBeat = note;
                Sing(note, target, owner);
            }
            if (IsLead && previousClock < ChoirConcertRules.Inhale && now >= ChoirConcertRules.Inhale && now < ChoirConcertRules.Fire
                && !ChoirConcert.HasChorus(Projectile.owner))
                Chorus(stage, target, owner);
        }
        previousClock = now;
    }

    private void Sing(int note, NPC target, Player owner)
    {
        int facing = Projectile.spriteDirection >= 0 ? 1 : -1;
        NVector2 open = ChoirConcertRules.Mouth(Variant);
        Vector2 mouth = Projectile.Center + new Vector2(open.X * facing, open.Y);
        Vector2 aim = RitualArmamentItems.Aim(target.Center - mouth, facing);
        // Launch per tick: toward the target plus a lift; the note runs two updates a tick.
        Vector2 velocity = (aim * ChoirConcertRules.NoteLaunch + new Vector2(0, -ChoirConcertRules.NoteLift)) / 2f;
        int pitch = ChoirConcertRules.NotePitch(note, ChoirConcertRules.Part(Ordinal));
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), mouth, velocity, ModContent.ProjectileType<ChoirSungNote>(),
            ChoirConcertRules.NoteDamage(Projectile.damage), Projectile.knockBack, Projectile.owner, 0, target.whoAmI, pitch);
    }

    private void Chorus(NVector2 stage, NPC target, Player owner)
    {
        ChoirConcert.Lead(Projectile.owner, out _, out double damage);
        Vector2 origin = ChoirConcert.X(stage);
        Vector2 aim = RitualArmamentItems.Aim(target.Center - origin, owner.direction);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, aim, ModContent.ProjectileType<ChoirChorus>(),
            ChoirConcertRules.ChorusDamage(damage), Projectile.knockBack, Projectile.owner, 0, target.whoAmI, 0);
    }

    public override void SendExtraAI(BinaryWriter writer) => writer.Write((ushort)Math.Clamp(Life, 0, ushort.MaxValue));

    public override void ReceiveExtraAI(BinaryReader reader) => Life = reader.ReadUInt16();
}

// One sung note: launched from the singer's mouth toward the target, homing after NoteHomingDelay ticks.
// ai[0] = age (ticks), ai[1] = target NPC, ai[2] = sung pitch 0..8 (ChoirConcertRules.SungNames). Pierce 1, one hit
// per NPC root. An unusable owner turns it harmless; it fades for NoteFadeTicks and ends.
public sealed class ChoirSungNote : ModProjectile
{
    private readonly HashSet<int> roots = new();
    internal float Age => Projectile.ai[0];
    internal int Pitch => Math.Clamp((int)Projectile.ai[2], 0, ChoirConcertRules.SungNames.Length - 1);
    internal bool Fading => Projectile.localAI[0] > 0;
    internal float Fade => Math.Clamp(Projectile.localAI[0] / ChoirConcertRules.NoteFadeTicks, 0, 1);
    public override string Texture => "Convergence/Assets/Textures/Items/DollWeapons/ChoirBaton";

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Type] = true;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = ChoirConcertRules.NoteSize;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = 1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.extraUpdates = 1; Projectile.timeLeft = ChoirConcertRules.NoteLife * 2;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }

    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override bool? CanDamage() => Fading ? false : null;
    public override bool? CanHitNPC(NPC target) => roots.Contains(RitualTargeting.Root(target)) ? false : null;

    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || Projectile.ai[2] < 0 || Projectile.ai[2] >= ChoirConcertRules.SungNames.Length)
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        float step = 1f / Projectile.MaxUpdates;
        if (!Fading && !ChoirConcert.Usable(owner)) Projectile.localAI[0] = step * .5f;
        if (Fading)
        {
            Projectile.friendly = false;
            Projectile.localAI[0] += step;
            Projectile.velocity *= .9f;
            if (Projectile.localAI[0] >= ChoirConcertRules.NoteFadeTicks) Projectile.Kill();
            return;
        }
        Projectile.ai[0] += step;
        if (Age >= ChoirConcertRules.NoteHomingDelay)
        {
            NPC? target = RitualTargeting.Acquire(Projectile, owner, manual: true, excluded: roots);
            if (target is not null) RitualTargeting.Home(Projectile, target.Center, target.velocity, ChoirConcertRules.NoteSpeed);
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
    }

    // A swept head, so a fast note cannot tunnel through a thin NPC.
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center - Projectile.velocity, Projectile.Center, Projectile.width, ref point);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => roots.Add(RitualTargeting.Root(target));
}

// The chorus beam, one per owner, sung from the organ mouth (the lead voice's stage anchor). Its age follows the
// lead's concert clock (ai[0] = clock - Inhale: 0..35 the harmless warning axis, 36..215 live), ai[1] = target,
// ai[2] = 0 open or -1..-CloseTicks closing (harmless). Damage is ChorusShare x the sum of every living voice's
// current damage, recomputed each tick; one hit per NPC root every HitCadence ticks. Removing the lead hands the
// beam to the next voice; a lost target, an unusable owner or no voice closes it (the owner decides and sends
// ai[2]; peers follow that update and their own replayed clock's end).
public sealed class ChoirChorus : ModProjectile
{
    private readonly ulong[] nextRootHit = new ulong[Main.maxNPCs];
    private bool aimed;
    internal int Hits { get; private set; }
    internal float Age => Projectile.ai[0];
    internal float Live => Age - ChoirConcertRules.WarnTicks;
    internal bool Closing => Projectile.ai[2] < 0;
    internal float CloseProgress => Closing ? Math.Clamp(-Projectile.ai[2] / ChoirConcertRules.CloseTicks, 0, 1) : 0;
    internal Vector2 Axis => RitualArmamentItems.Aim(Projectile.velocity, 1);
    public override string Texture => "Convergence/Assets/Textures/Items/DollWeapons/ChoirBaton";

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Type] = true;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2600;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 2;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = ChoirConcertRules.HitCadence;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override bool? CanHitNPC(NPC target) => Main.GameUpdateCount < nextRootHit[RitualTargeting.Root(target)] ? false : null;

    public override void AI()
    {
        if (!RitualTargeting.ValidState(Projectile) || Projectile.ai[2] > 0 || Projectile.ai[2] < -ChoirConcertRules.CloseTicks - 1)
        { Projectile.Kill(); return; }
        Player owner = Main.player[Projectile.owner];
        bool authority = Projectile.owner == Main.myPlayer;
        Projectile.timeLeft = 2;
        if (Closing)
        {
            Projectile.friendly = false;
            Projectile.ai[2]--;
            if (Projectile.ai[2] <= -ChoirConcertRules.CloseTicks) Projectile.Kill();
            return;
        }
        ChoirChorister? lead = ChoirConcert.Lead(Projectile.owner, out _, out double damage);
        int clock = lead is null ? 0 : (int)lead.Clock;
        NPC? target = lead is null ? null : ChoirTargeting.Current(lead.Projectile, owner);
        // The owner closes a beam whose target is lost or whose owner is unusable (and sends it); a peer closes only
        // on that update, on the natural end of its replayed clock or when no voice is left.
        bool held = lead is not null && (authority ? target is not null && ChoirConcert.Usable(owner) : ChoirTargeting.Chosen(lead.Projectile));
        if (lead is null || !held || !(ChoirConcertRules.Warning(clock) || ChoirConcertRules.Live(clock)))
        {
            Projectile.ai[2] = -1;
            Projectile.friendly = false;
            if (authority) Projectile.netUpdate = true;
            return;
        }
        Projectile.ai[0] = clock - ChoirConcertRules.Inhale;
        Projectile.ai[1] = lead.Projectile.ai[1];
        Projectile.Center = ChoirConcert.X(lead.Stage);
        Projectile.damage = ChoirConcertRules.ChorusDamage(damage);
        if (authority && target is not null)
        {
            float current = Projectile.velocity.LengthSquared() > .001f ? Projectile.velocity.ToRotation() : float.NaN;
            float wanted = (target.Center - Projectile.Center).ToRotation();
            float angle = ChoirConcertRules.Turn(current, wanted, !aimed);
            aimed = true;
            Projectile.velocity = angle.ToRotationVector2();
            if ((int)Age % 6 == 0) Projectile.netUpdate = true;
        }
        Projectile.rotation = Axis.ToRotation();
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float live = Live;
        if (Closing || !ChoirConcertRules.CanHit(live)) return false;
        float point = 0;
        Vector2 axis = Axis;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center + axis * ChoirConcertRules.BeamStart, Projectile.Center + axis * ChoirConcertRules.End(live),
            2 * ChoirConcertRules.HalfWidth(live), ref point);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        nextRootHit[RitualTargeting.Root(target)] = Main.GameUpdateCount + ChoirConcertRules.HitCadence;
        Hits++;
    }
}
