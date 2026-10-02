#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using NVector2 = System.Numerics.Vector2;

namespace Convergence.Client.Encounters.CrimsonFoundry.Rewards;

// Canticle Organ presentation (REWARDS.md, "Ranged - Canticle Organ", Presentation and Audio). Black blood goes through
// ScarletRewardInk (this system is an emitter): the shards' short live wakes, the owner's mark arcs, and each hand's
// forming smear, falling smear, live slam disc (the Clasp with its crown), residue and droplets. The shapes come from
// CanticleRules (the same commands the offline preview and the domain tests read), so draw equals collide. Bodies (the
// shard, the bone hands) draw as ordinary projectiles above the ink. Cues and particles fire from replicated state:
// a shard's first tick (pipe kick, vapour, OrganShot), the first hand of a hymn (HymnInhale), each slam (HandSlam,
// ChoirClasp, Cadence) and the owner-only Marked hook (Toll(m - 1)). Client only; no hit, packet or world state.
[Autoload(Side = ModSide.Client)]
internal sealed class OrganVisuals : ModSystem, IScarletInkEmitter
{
    private static readonly OrganInkSink sink = new();
    private static bool failed;

    public override void Load()
    {
        ScarletRewardInk.Register(this);
        CanticleOrganPlayer.Marked = OnMarked;
    }

    public override void Unload()
    {
        ScarletRewardInk.Unregister(this);
        CanticleOrganPlayer.Marked = null;
        OrganArt.Reset();
        failed = false;
    }

    public override void ClearWorld() => Clear();
    public override void OnWorldUnload() => Clear();

    private static void Clear()
    {
        foreach (Player player in Main.player) player.GetModPlayer<OrganPosePlayer>().Clear();
    }

    // ---- Ink ----------------------------------------------------------------------------------------------------

    public void Emit(ScarletInkCanvas canvas, in ScarletView view)
    {
        if (Main.gameMenu) return;
        sink.Canvas = canvas;
        try
        {
            float fraction = view.Fraction;
            int shardType = ModContent.ProjectileType<CanticleShard>(), handType = ModContent.ProjectileType<BoneHand>();
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.type == shardType) EmitShard(p);
                else if (p.type == handType && p.ModProjectile is BoneHand hand) EmitHand(p, hand, fraction, canvas.Reduced);
            }
            EmitMarks(fraction);
        }
        finally { sink.Canvas = null; }
    }

    private static void EmitShard(Projectile p)
    {
        Span<NVector2> trail = stackalloc NVector2[4];
        int n = 0;
        Vector2 half = p.Size * .5f;
        for (int i = Math.Min(p.oldPos.Length, 3) - 1; i >= 0; i--)
            if (p.oldPos[i] != Vector2.Zero) trail[n++] = N(p.oldPos[i] + half);
        trail[n++] = N(p.Center);
        sink.Owner = p.owner;
        sink.Window = 0; // the wake never dries into a scar
        CanticleRules.ShardInk(sink, trail[..n], (p.identity % 97) * .37f);
    }

    private static void EmitHand(Projectile p, BoneHand hand, float fraction, bool reduced)
    {
        float age = hand.Age - 1 + fraction;
        if (!CanticleRules.Visible(age)) return;
        NPC npc = Main.npc[Math.Clamp(hand.Slot, 0, Main.maxNPCs - 1)];
        Vector2 landing;
        // Landed: the replicated slam point. Before that: the NPC's replicated position (peers rebuild the fall from it).
        if (hand.Age >= 0) landing = p.Center;
        else if (npc.active) landing = npc.Center + new Vector2(CanticleRules.LandingOffset(hand.Ordinal, hand.Role, npc.width), 0);
        else return;
        sink.Owner = p.owner;
        sink.Window = CrimsonRewardRules.HandLive; // the slam (and the Clasp's crown) closes into its scar
        CanticleRules.HandInk(sink, N(landing), hand.Ordinal, hand.Role, npc.width, age, (p.identity % 89) * .53f + hand.Ordinal * .11f, reduced);
    }

    // Marks are owner-only ink: only the local player's ledger is drawn.
    private static void EmitMarks(float fraction)
    {
        Player me = Main.LocalPlayer;
        if (me is null || !me.active) return;
        CanticleMarks marks = me.GetModPlayer<CanticleOrganPlayer>().Marks;
        ulong now = Main.GameUpdateCount;
        sink.Owner = me.whoAmI;
        sink.Window = 0;
        for (int i = 0; i < marks.Count; i++)
        {
            CanticleMark mark = marks[i];
            NPC npc = Main.npc[mark.Root];
            if (!npc.active) continue;
            float since = now < mark.Newest ? 0 : Math.Max(0, (float)(now - mark.Newest) - 1 + fraction);
            Vector2 anchor = new(npc.Center.X, npc.Top.Y - CanticleRules.MarkLift);
            CanticleRules.MarkInk(sink, N(anchor), mark.Count, since, mark.Root * 1.37f + (mark.Order % 31) * .29f);
        }
    }

    // ---- Events (from replicated state, on every client) --------------------------------------------------------

    // The owner's shard marked an NPC: the next toll for the owner and a flare of embers at the new note-head.
    private static void OnMarked(Player owner, NPC npc, CanticleMarkResult result)
    {
        if (!result.Added || result.Count <= 0) return;
        ScarletRewardAudio.BuildToll(result.Count - 1, owner.whoAmI, owner.Center);
        float x = CanticleRules.MarkSlotX(result.Count - 1);
        Vector2 head = new(npc.Center.X + x, npc.Top.Y - CanticleRules.MarkLift + CanticleRules.MarkArcY(x));
        for (int i = 0; i < 2; i++)
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, owner.whoAmI, head, new Vector2((i - .5f) * .8f, -.9f), 14, 4, npc.whoAmI * 7 + result.Count * 3 + i);
    }

    // A shard's first tick: that pipe kicks, a breath of crimson-black vapour leaves its mouth, the pipe chiffs.
    internal static void Fired(Projectile p, int pipe)
    {
        if (p.owner < 0 || p.owner >= Main.maxPlayers) return;
        Main.player[p.owner].GetModPlayer<OrganPosePlayer>().Shot(pipe);
        ScarletRewardAudio.Shot(ScarletRewardCues.OrganShot, p.owner, p.Center, .55f, .06f);
        Vector2 along = p.velocity.LengthSquared() > .01f ? Vector2.Normalize(p.velocity) : Vector2.UnitX;
        float seed = p.identity * .71f;
        ScarletRewardFx.Particle(ScarletParticleKind.Smoke, p.owner, p.Center, along * 1.1f + new Vector2(0, -.25f), 22, 9, seed);
        ScarletRewardFx.Particle(ScarletParticleKind.Smoke, p.owner, p.Center + along * 4, along * .6f + new Vector2(0, -.4f), 26, 7, seed + .5f);
        ScarletRewardFx.Particle(ScarletParticleKind.Ember, p.owner, p.Center, along * 1.4f, 9, 3, seed + .9f);
    }

    // The first hand of a hymn appears at the cast on every client: the bellows breath, and the organ lifts.
    internal static void HymnBegan(Projectile p)
    {
        if (p.owner < 0 || p.owner >= Main.maxPlayers) return;
        Player owner = Main.player[p.owner];
        owner.GetModPlayer<OrganPosePlayer>().Hymn();
        ScarletRewardAudio.Play(ScarletRewardCues.HymnInhale, owner.Center, .7f, 0, 0, 2);
    }

    internal static void Slam(Projectile p, BoneHand hand)
    {
        Vector2 at = p.Center;
        bool clasp = hand.Clasp;
        if (clasp)
        {
            ScarletRewardAudio.Play(ScarletRewardCues.ChoirClasp, at, .85f, 0, 0, 2);
            ScarletRewardFx.Shake(p.owner, at, CrimsonRewardRules.ClaspShake);
        }
        else
        {
            ScarletRewardAudio.Play(ScarletRewardCues.HandSlam, at, .7f, 0, .05f, 4);
            if (hand.Role == CanticleRules.HandRole.Cadence) ScarletRewardAudio.Play(ScarletRewardCues.Cadence, at, .7f, 0, 0, 2);
        }
        float seed = p.identity * .37f;
        int chips = clasp ? 10 : 5;
        for (int i = 0; i < chips; i++)
        {
            float h = CanticleRules.Hash(seed, i + 41), angle = -MathF.PI / 2 + (h - .5f) * 2.6f;
            ScarletRewardFx.Particle(ScarletParticleKind.BoneChip, p.owner, at, new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (2.2f + 2.4f * CanticleRules.Hash(seed, i + 43)),
                26, 2, seed + i);
        }
        for (int i = 0; i < 3; i++)
            ScarletRewardFx.Particle(ScarletParticleKind.Ember, p.owner, at + new Vector2((i - 1) * 14, -6), new Vector2((i - 1) * .5f, -1.2f), 16, 5, seed + i * 5);
    }

    // The live window over: the hand breaks into bone chips where it lies.
    internal static void Shatter(Projectile p, BoneHand hand)
    {
        NPC npc = Main.npc[Math.Clamp(hand.Slot, 0, Main.maxNPCs - 1)];
        int bodies = hand.Clasp ? CrimsonRewardRules.ClaspHands : 1;
        float seed = p.identity * .59f;
        for (int b = 0; b < bodies; b++)
        {
            Vector2 at = p.Center + new Vector2(hand.Clasp ? CanticleRules.ClaspHandOffset(b, 0, npc.width) : 0, -8);
            for (int i = 0; i < 3; i++)
                ScarletRewardFx.Particle(ScarletParticleKind.BoneChip, p.owner, at, new Vector2((CanticleRules.Hash(seed, b * 7 + i) - .5f) * 3, -1.4f - CanticleRules.Hash(seed, b * 7 + i + 3) * 1.6f),
                    22, 2, seed + b + i * .1f);
        }
    }

    // A shard shatters into bone chips on whatever ended it.
    internal static void Shattered(Projectile p)
    {
        float seed = p.identity * .83f;
        Vector2 back = p.velocity.LengthSquared() > .01f ? -Vector2.Normalize(p.velocity) : -Vector2.UnitY;
        for (int i = 0; i < 4; i++)
        {
            Vector2 v = back.RotatedBy((CanticleRules.Hash(seed, i) - .5f) * 2.2f) * (1.2f + 1.8f * CanticleRules.Hash(seed, i + 9));
            ScarletRewardFx.Particle(ScarletParticleKind.BoneChip, p.owner, p.Center, v + new Vector2(0, -1), 20, 2, seed + i);
        }
    }

    // ---- Bodies ---------------------------------------------------------------------------------------------------

    internal static void DrawShard(Projectile p, Color light)
    {
        if (failed) return;
        try
        {
            var sprite = OrganArt.Shard;
            OrganArt.Draw(sprite, p.Center, p.rotation, sprite.Origin, new Vector2(sprite.Scale), OrganArt.Lit(light, .55f), SpriteEffects.None);
        }
        catch (Exception e) { Disable(e); }
    }

    // A hand condenses out of its smear (alpha and size), falls palm down, presses on the slam and is gone once its
    // live window ends (it breaks into chips). The Clasp is four hands closing on the target; the left two are mirrored.
    internal static void DrawHand(Projectile p, BoneHand hand, Color light)
    {
        if (failed) return;
        float age = hand.Age - 1 + ScarletRewardFx.Fraction;
        if (age < -CrimsonRewardRules.HandLead || age >= CrimsonRewardRules.HandLive) return;
        try
        {
            NPC npc = Main.npc[Math.Clamp(hand.Slot, 0, Main.maxNPCs - 1)];
            Vector2 landing;
            if (hand.Age >= 0) landing = p.Center;
            else if (npc.active) landing = npc.Center + new Vector2(CanticleRules.LandingOffset(hand.Ordinal, hand.Role, npc.width), 0);
            else return;
            var art = OrganArt.BoneHand;
            float form = CanticleRules.Forming(age);
            float press = age >= 0 ? MathF.Exp(-age * .9f) : 0;
            Vector2 scale = new Vector2(1 + .08f * press, 1 - .1f * press) * (art.Sprite.Scale * (.72f + .28f * form));
            Color color = OrganArt.Lit(light) * form;
            int bodies = hand.Clasp ? CrimsonRewardRules.ClaspHands : 1;
            for (int b = 0; b < bodies; b++)
            {
                float x = hand.Clasp ? CanticleRules.ClaspHandOffset(b, Math.Min(age, 0), npc.width) : 0;
                Vector2 palm = landing + new Vector2(x, -CanticleRules.FallHeight(age));
                bool mirror = hand.Clasp ? CrimsonRewardRules.ArmSide(b) < 0 : CrimsonRewardRules.ArmSide(hand.Ordinal) < 0;
                OrganArt.Draw(art.Sprite, palm, art.Rotation, art.Origin(mirror), scale, color, mirror ? art.Mirror : SpriteEffects.None);
            }
        }
        catch (Exception e) { Disable(e); }
    }

    private static void Disable(Exception e)
    {
        failed = true;
        global::Convergence.ConvergenceMod.Instance.Logger.Warn("Canticle Organ bodies disabled for this session after a drawing error; the weapon still works.", e);
    }

    private static NVector2 N(Vector2 v) => new(v.X, v.Y);
}

// ICanticleInk over the frame's ScarletInkCanvas: owner styles (remote dimming, local drop priority) via ScarletRewardFx.
internal sealed class OrganInkSink : ICanticleInk
{
    internal ScarletInkCanvas? Canvas;
    internal int Owner = -1;
    internal float Window; // live window of the ink being emitted (ScarletInkStyle.Window), 0 for none

    private static ScarletInkLook Look(CanticleInkLook look) => look switch
    {
        CanticleInkLook.Live => ScarletInkLook.Live,
        CanticleInkLook.Dormant => ScarletInkLook.Dormant,
        _ => ScarletInkLook.Residue,
    };

    public bool Begin(CanticleInkLook look, float seed, float opacity = 1, float warmth = 0)
        => Canvas is not null && Canvas.Begin(ScarletRewardFx.Ink(Owner, Look(look), seed, opacity, false, warmth)
            with { Window = look == CanticleInkLook.Live ? Window : 0 });
    public void Point(NVector2 at, float radius, float time) => Canvas?.Point(new Vector2(at.X, at.Y), radius, time);
    public void End(bool bead = false) => Canvas?.End(bead);
    public void Droplet(NVector2 from, NVector2 to, float radius, float time, float seed)
        => Canvas?.Droplet(ScarletRewardFx.Ink(Owner, ScarletInkLook.Live, seed), new Vector2(from.X, from.Y), new Vector2(to.X, to.Y), radius, time);
}

// Per-projectile client events and bodies for the shard and the hand.
[Autoload(Side = ModSide.Client)]
internal sealed class OrganProjectiles : GlobalProjectile
{
    private bool seen;
    private int lastAge = int.MinValue;

    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is CanticleShard or BoneHand;

    public override void PostAI(Projectile p)
    {
        if (!p.active) return;
        if (p.ModProjectile is CanticleShard shard)
        {
            if (!seen) { seen = true; OrganVisuals.Fired(p, shard.Pipe); }
            return;
        }
        if (p.ModProjectile is not BoneHand hand) return;
        int age = hand.Age;
        if (!seen)
        {
            seen = true;
            if (hand.Ordinal == 0 && age < 0) OrganVisuals.HymnBegan(p);
        }
        // Peers may skip a tick or learn of a hand late; a slam is shown only while it is fresh.
        if (age >= 0 && lastAge < 0 && age <= 2) OrganVisuals.Slam(p, hand);
        if (age >= CrimsonRewardRules.HandLive && lastAge < CrimsonRewardRules.HandLive && lastAge >= 0) OrganVisuals.Shatter(p, hand);
        if (age >= 0 && age <= CrimsonRewardRules.HandLive)
        {
            float heat = 1 - age / (CrimsonRewardRules.HandLive + 1f);
            Lighting.AddLight(p.Center, .9f * heat, .12f * heat, .08f * heat);
        }
        lastAge = age;
    }

    public override void OnKill(Projectile p, int timeLeft)
    {
        if (p.ModProjectile is CanticleShard) OrganVisuals.Shattered(p);
    }

    public override bool PreDraw(Projectile p, ref Color lightColor)
    {
        if (p.ModProjectile is CanticleShard) OrganVisuals.DrawShard(p, lightColor);
        else if (p.ModProjectile is BoneHand hand) OrganVisuals.DrawHand(p, hand, lightColor);
        return false;
    }
}
