#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

[Autoload(Side = ModSide.Client)]
public sealed class RitualArmamentItemVisuals : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        => entity.ModItem is IRitualArmament && entity.ModItem is not NullRefrain;
    public override bool PreDrawInInventory(Item item, SpriteBatch b, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        var texture = RitualArmamentArt.Icon(((IRitualArmament)item.ModItem).Kind);
        b.Draw(texture, position, null, Color.White, 0, texture.Size() * .5f,
            scale, SpriteEffects.None, 0);
        return false;
    }
    public override bool PreDrawInWorld(Item item, SpriteBatch b, Color lightColor,
        Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        var kind = ((IRitualArmament)item.ModItem).Kind;
        var texture = RitualArmamentArt.Icon(kind);
        Vector2 center = item.Center + new Vector2(0, MathF.Sin(RitualRenderClock.Time * .025f + whoAmI) * 6);
        RitualArmamentArt.Glow(b, center, new(83), RitualArmamentArt.Light(RitualArmamentArt.ColorFor(kind), .7f));
        b.Draw(texture, center - Main.screenPosition, null, Color.White, .055f * MathF.Sin(RitualRenderClock.Time * .012f),
            texture.Size() * .5f, 88f / texture.Width, SpriteEffects.None, 0);
        return false;
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class RitualArmamentProjectileVisuals : GlobalProjectile
{
    private bool initialized, impactSeen, sounded;
    private Vector2 previousCenter, currentCenter;
    private float previousAngle, angle, lastAge = -1;
    private uint serial;
    private SlotId sustain = SlotId.Invalid;
    private SlotId chargeVoice = SlotId.Invalid;
    private int impactCooldown;
    public override bool InstancePerEntity => true;
    internal static bool Matches(Projectile p) => p.ModProjectile is RitualBolt or WitnessBlade or ChoirSentinel or RitualArmamentPose or WitnessVerdict or LacunaConvergence or MeridianBastion or ChoirRequiem or WitnessLitany;
    public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation) => Matches(projectile);
    internal Vector2 Center(Projectile p) => initialized
        ? Vector2.Lerp(previousCenter, currentCenter, RitualRenderClock.Fraction) : p.Center;
    internal float Angle(Projectile p) => initialized
        ? previousAngle + MathF.IEEERemainder(angle - previousAngle, MathF.Tau) * RitualRenderClock.Fraction : p.rotation;
    internal static RitualArmamentKind Kind(Projectile p) => p.ModProjectile switch
    {
        RitualBolt bolt => bolt.Kind, WitnessBlade or WitnessVerdict or WitnessLitany => RitualArmamentKind.Rogue,
        ChoirSentinel or ChoirRequiem => RitualArmamentKind.Summon, MeridianBastion => RitualArmamentKind.Ranged, LacunaConvergence => RitualArmamentKind.Magic,
        RitualArmamentPose pose => pose.Kind, _ => RitualArmamentKind.Melee,
    };
    public override void PostAI(Projectile p)
    {
        if (p.numUpdates != 0) return;
        if (p.ai[0] < lastAge || p.owner < 0 || p.owner >= Main.maxPlayers
            || !RitualArmamentItems.Usable(Main.player[p.owner]) || Main.player[p.owner].noItems || Main.player[p.owner].CCed
            || p.ModProjectile is LacunaConvergence or MeridianBastion or WitnessLitany && p.ai[2] < 0)
            RitualWeaponFeedback.Stop(ref chargeVoice);
        if (impactCooldown > 0) impactCooldown--;
        if (!initialized || Vector2.DistanceSquared(currentCenter, p.Center) > 1800 * 1800)
        { previousCenter = currentCenter = p.Center; previousAngle = angle = p.rotation; initialized = true; }
        else
        {
            previousCenter = currentCenter; currentCenter = p.Center;
            previousAngle = angle;
            bool nativeChannel = p.ModProjectile is LacunaConvergence or MeridianBastion or WitnessLitany or ChoirRequiem;
            angle += MathF.IEEERemainder(p.rotation - angle, MathF.Tau)
                * (p.ModProjectile is RitualArmamentPose || nativeChannel && p.owner != Main.myPlayer ? .46f : 1);
        }
        if (p.ModProjectile is RitualArmamentPose pose)
        {
            if (serial != pose.Serial) { serial = pose.Serial; sounded = false; lastAge = -1; }
            float fire = pose.Kind == RitualArmamentKind.Ranged ? 4 : 1;
            if (pose.Kind == RitualArmamentKind.Ranged)
            {
                for (int lane = 0; lane < 3; lane++)
                {
                    float tick = 4 + lane * 2;
                    if (lastAge < tick && pose.Age >= tick && pose.Age < tick + 2)
                    {
                        RitualWeaponFeedback.Sound(pose.Empowered ? "RangedFire" : "RangedShot", p.Center,
                            (pose.Empowered ? .34f : .20f) * (lane == 0 ? 1 : .72f));
                        if (lane == 0 && pose.Empowered) ModContent.GetInstance<RitualWeaponFeedback>().Kick(p.owner, 3);
                    }
                }
            }
            else if (pose.Kind != RitualArmamentKind.Magic && !sounded && pose.Age >= fire && pose.Age < fire + 3)
            {
                sounded = true;
                string cue = pose.Kind switch
                {
                    RitualArmamentKind.Ranged => pose.Empowered ? "RangedFire" : "RangedShot",
                    RitualArmamentKind.Magic => pose.Empowered ? "MagicFire" : "MagicBolt",
                    RitualArmamentKind.Summon => "ChoirNote", _ => "WitnessDraw",
                };
                RitualWeaponFeedback.Sound(cue, p.Center, pose.Empowered ? .56f : .31f);
            }
            lastAge = pose.Age;
        }
        else if (p.ModProjectile is LacunaConvergence beam)
        {
            for (int i = 0; i < 7; i++)
            {
                float birth = RitualGrandScore.SigilBirth(i) + 2;
                if (Crossed(birth, beam.Age)) RitualWeaponFeedback.Sound("MagicSigil", beam.Sigil(i, beam.Age), .22f + i * .016f);
                int tick = (int)beam.Age;
                if (beam.Projectile.ai[2] >= 0 && RitualGrandScore.MagicBoltAt(tick, i))
                    RitualWeaponFeedback.Sound("MagicBolt", beam.Sigil(i, beam.Age), .20f);
            }
            if (Crossed(RitualGrandScore.MagicMerge, beam.Age))
                RitualWeaponFeedback.Sound("MagicMerge", beam.Muzzle, .35f);
            if (Crossed(RitualGrandScore.MagicCharge, beam.Age))
                chargeVoice = RitualWeaponFeedback.Sound("MagicCharge", beam.Muzzle, .4f);
            if (Crossed(RitualGrandScore.MagicFire, beam.Age))
            {
                RitualWeaponFeedback.Sound("MagicFire", beam.Muzzle, .55f);
                ModContent.GetInstance<RitualWeaponFeedback>().Kick(p.owner, 5);
            }
            UpdateSustain(p, beam.Empowered && p.ai[2] >= 0, "LacunaSustain", .65f);
            lastAge = beam.Age;
        }
        else if (p.ModProjectile is MeridianBastion gun)
        {
            for (int i = 0; i < 5; i++)
                if (Crossed(RitualGrandScore.BatteryBirth(i) + 2, gun.Age))
                    RitualWeaponFeedback.Sound("RangedLatch", gun.Muzzle(i), .30f);
            if (Crossed(RitualGrandScore.BatteryFire - 48, gun.Age))
                chargeVoice = RitualWeaponFeedback.Sound("RangedCharge", p.Center, .42f);
            if (Crossed(RitualGrandScore.BatteryFire, gun.Age))
            {
                RitualWeaponFeedback.Sound("RangedFire", p.Center, .57f);
                ModContent.GetInstance<RitualWeaponFeedback>().Kick(p.owner, 4);
            }
            int lane = RitualGrandScore.BatteryLane((int)gun.Age);
            if (lane >= 0 && p.ai[2] >= 0 && (!gun.Overdrive || (int)gun.Age % 9 == 0))
                RitualWeaponFeedback.Sound("RangedShot", gun.Muzzle(lane), gun.Overdrive ? .18f : .30f);
            UpdateSustain(p, gun.Overdrive && p.ai[2] >= 0, "MeridianSustain", .52f);
            lastAge = gun.Age;
        }
        else if (p.ModProjectile is WitnessLitany litany)
        {
            for (int i = 0; i < 6; i++)
                if (Crossed(i * 29 + 2, litany.Age)) RitualWeaponFeedback.Sound("WitnessDraw", litany.Crown, .28f);
            if (Crossed(RitualGrandScore.WitnessFire - 22, litany.Age))
                chargeVoice = RitualWeaponFeedback.Sound("WitnessLock", litany.Crown, .43f);
            if (Crossed(RitualGrandScore.WitnessFire, litany.Age))
            {
                RitualWeaponFeedback.Sound("WitnessFire", litany.Crown, .5f);
                ModContent.GetInstance<RitualWeaponFeedback>().Kick(p.owner, 4.5f);
            }
            lastAge = litany.Age;
        }
        else if (p.ModProjectile is ChoirRequiem requiem)
        {
            if (Crossed(1, requiem.Age))
            {
                RitualWeaponFeedback.Sound("ChoirFire", p.Center, .5f);
                ModContent.GetInstance<RitualWeaponFeedback>().Kick(p.owner, 3);
            }
            UpdateSustain(p, true, "ChoirSustain", .56f * requiem.Fade);
            lastAge = requiem.Age;
        }
        else if (p.ModProjectile is WitnessVerdict verdict)
        {
            if (lastAge < 16 && verdict.Age >= 16 && verdict.Age < 20)
                chargeVoice = RitualWeaponFeedback.Sound("WitnessLock", p.Center, .42f);
            if (lastAge < 23 && verdict.Age >= 23 && verdict.Age < 25)
                RitualWeaponFeedback.Sound("WitnessDraw", p.Center, .34f);
            if (lastAge < 28 && verdict.Age >= 28 && verdict.Age < 32)
            {
                RitualWeaponFeedback.Sound("WitnessFire", p.Center, .62f);
                ModContent.GetInstance<RitualWeaponFeedback>().Kick(p.owner, 5.5f);
            }
            lastAge = verdict.Age;
        }
        else if (p.ModProjectile is ChoirSentinel s)
        {
            if (s.Age < lastAge) lastAge = -1;
            if (s.IsLeader && Crossed(RitualGrandScore.ChoirAssemble, s.Age))
                RitualWeaponFeedback.Sound("MagicMerge", s.ConcertCenter, .30f);
            if (s.IsLeader && Crossed(RitualGrandScore.ChoirCharge, s.Age))
                chargeVoice = RitualWeaponFeedback.Sound("ChoirCharge", s.ConcertCenter, .38f);
            lastAge = s.Age;
        }
        else if (!sounded && p.ModProjectile is ChoirNote)
        { sounded = true; RitualWeaponFeedback.Sound("ChoirNote", p.Center, .23f); }
    }
    private bool Crossed(float beat, float now) => lastAge < beat && now >= beat && now < beat + 3;
    private void UpdateSustain(Projectile p, bool active, string name, float volume)
    {
        if (!active)
        {
            if (SoundEngine.TryGetActiveSound(sustain, out var old)) old.Stop();
            return;
        }
        if (SoundEngine.TryGetActiveSound(sustain, out var sound) && sound.IsPlaying)
        {
            sound.Position = p.Center;
            // ActiveSound.Volume multiplies Style.Volume; do not square the gain.
            sound.Volume = volume / Math.Max(.0001f, sound.Style.Volume);
            return;
        }
        // A persistent looping voice, not a repeated launch sample or per-hit cue.
        int identity = p.identity, type = p.type, owner = p.owner;
        sustain = SoundEngine.PlaySound(new SoundStyle(RitualWeaponFeedback.SoundRoot + name)
        {
            Identifier = $"Convergence:Sustain:{owner}:{identity}:{type}", IsLooped = true, MaxInstances = 1,
            Volume = volume,
            PauseBehavior = PauseBehavior.StopWhenGamePaused, PlayOnlyIfFocused = true,
        }, p.Center, _ => p.active && p.identity == identity && p.type == type && p.owner == owner);
        ModContent.GetInstance<RitualWeaponFeedback>().Track(sustain);
    }
    public override void OnHitNPC(Projectile p, NPC target, NPC.HitInfo hit, int damageDone)
    {
        bool continuous = p.ModProjectile is LacunaConvergence or ChoirRequiem;
        if (impactCooldown > 0 || (!continuous && impactSeen) || p.ModProjectile is WitnessVerdict) return;
        impactSeen = true; impactCooldown = continuous ? 20 : 0;
        ModContent.GetInstance<RitualWeaponFeedback>().Burst(target.Center, Kind(p), p.owner == Main.myPlayer,
            p.ModProjectile is RitualBolt { Empowered: true } or WitnessBlade { Stealth: true } or LacunaConvergence { Empowered: true });
    }
    public override void OnKill(Projectile p, int timeLeft)
    {
        RitualWeaponFeedback.Stop(ref chargeVoice);
        if (SoundEngine.TryGetActiveSound(sustain, out var sound)) sound.Stop();
        if (!impactSeen && p.ModProjectile is RitualBolt && !Main.gameMenu)
            ModContent.GetInstance<RitualWeaponFeedback>().Burst(p.Center, Kind(p), false, false);
    }
    // One linear-sampled dedicated draw pass for all new weapon parts/surfaces.
    public override bool PreDraw(Projectile projectile, ref Color lightColor) => false;
}

[Autoload(Side = ModSide.Client)]
public sealed class RitualWeaponFeedback : ModSystem
{
    internal const string SoundRoot = "Convergence/Assets/Sounds/Weapons/DollTheater/";
    private sealed class Impact(Vector2 position, RitualArmamentKind kind, bool strong)
    {
        internal readonly Vector2 Position = position;
        internal readonly RitualArmamentKind Kind = kind;
        internal bool Strong = strong; internal int Age;
    }
    private readonly List<Impact> impacts = new(40);
    private readonly List<SlotId> voices = new(32);
    private float shake;
    internal void Track(SlotId id)
    {
        voices.RemoveAll(old => !SoundEngine.TryGetActiveSound(old, out var sound) || !sound.IsPlaying);
        if (voices.Count >= 40)
        { if (SoundEngine.TryGetActiveSound(voices[0], out var oldest)) oldest.Stop(); voices.RemoveAt(0); }
        voices.Add(id);
    }
    internal static void Stop(ref SlotId id)
    {
        if (SoundEngine.TryGetActiveSound(id, out var sound)) sound.Stop();
        id = SlotId.Invalid;
    }
    internal static SlotId Sound(string name, Vector2 at, float volume)
    {
        if (Main.dedServ || Main.gameMenu) return SlotId.Invalid;
        var system = ModContent.GetInstance<RitualWeaponFeedback>();
        system.voices.RemoveAll(id => !SoundEngine.TryGetActiveSound(id, out var s) || !s.IsPlaying);
        if (system.voices.Count >= 40)
        { if (SoundEngine.TryGetActiveSound(system.voices[0], out var old)) old.Stop(); system.voices.RemoveAt(0); }
        SlotId voice = SoundEngine.PlaySound(new SoundStyle(SoundRoot + name)
        {
            // Preserve the existing quiet-assembly / sharp-lock / loud-release
            // hierarchy. Reduced visual effects must not mute these audible beats.
            Identifier = "Convergence:RitualWeapon:" + name, Volume = Math.Min(.95f, volume * 2f),
            PitchVariance = name.EndsWith("Charge", StringComparison.Ordinal) || name.EndsWith("Lock", StringComparison.Ordinal) ? 0 : .055f,
            MaxInstances = 3, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PauseBehavior = PauseBehavior.StopWhenGamePaused, PlayOnlyIfFocused = true,
        }, at);
        system.voices.Add(voice);
        return voice;
    }
    internal void Kick(int owner, float force) { if (owner == Main.myPlayer) shake = Math.Max(shake, force); }
    internal void Burst(Vector2 at, RitualArmamentKind kind, bool local, bool strong)
    {
        if (Main.dedServ || Main.gameMenu) return;
        foreach (var hit in impacts)
            if (hit.Age <= 1 && Vector2.DistanceSquared(hit.Position, at) < 76 * 76)
            { hit.Strong |= strong; return; }
        if (impacts.Count >= 40) impacts.RemoveAt(0);
        impacts.Add(new(at, kind, strong));
        Sound("WeaponHit", at, strong ? .31f : .16f);
        if (local && strong) shake = Math.Max(shake, 4.5f);
    }
    public override void PostUpdateEverything()
    {
        shake *= .76f;
        for (int i = impacts.Count - 1; i >= 0; i--) if (++impacts[i].Age >= 32) impacts.RemoveAt(i);
    }
    public override void ModifyScreenPosition()
    {
        var config = ModContent.GetInstance<FirstSeveranceVisualConfig>();
        if (!Main.gameMenu && config.ScreenShake && !config.ReducedEffects)
            Main.screenPosition += new Vector2(MathF.Sin(RitualRenderClock.Time * 2.1f), MathF.Cos(RitualRenderClock.Time * 2.7f)) * shake;
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        RitualSurfacePass.Begin();
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (!RitualArmamentProjectileVisuals.Matches(p) || p.owner < 0 || p.owner >= Main.maxPlayers) continue;
            var state = p.GetGlobalProjectile<RitualArmamentProjectileVisuals>();
            RitualArmamentArt.QueueFlight(p, state.Center(p));
            if (p.ModProjectile is WitnessVerdict v) RitualArmamentArt.QueueVerdict(v, RitualRenderClock.Sample(v.Age));
            if (p.ModProjectile is LacunaConvergence beam) RitualGrandArt.QueueMagic(beam, RitualRenderClock.Sample(beam.Age));
            if (p.ModProjectile is ChoirRequiem requiem)
                RitualGrandArt.QueueBeam(p.Center, requiem.Axis, requiem.Reach, requiem.Width, RitualRenderClock.Sample(requiem.Age),
                    RitualArmamentArt.ColorFor(RitualArmamentKind.Summon), requiem.Fade * (RitualArmamentArt.Reduced ? .7f : 1));
        }
        RitualSurfacePass.Flush();
        SpriteBatch b = Main.spriteBatch;
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (!RitualArmamentProjectileVisuals.Matches(p) || p.owner < 0 || p.owner >= Main.maxPlayers) continue;
                var state = p.GetGlobalProjectile<RitualArmamentProjectileVisuals>();
                Vector2 center = state.Center(p);
                switch (p.ModProjectile)
                {
                    case RitualArmamentPose pose:
                        RitualArmamentArt.DrawApparatus(b, pose, RitualRenderClock.Sample(pose.Age), state.Angle(p), center); break;
                    case ChoirSentinel s:
                        RitualGrandArt.Choir(b, s, center, RitualRenderClock.Sample(s.Age)); break;
                    case WitnessVerdict v:
                        RitualArmamentArt.DrawVerdict(b, v, RitualRenderClock.Sample(v.Age)); break;
                    case LacunaConvergence beam:
                        RitualGrandArt.Magic(b, beam, RitualRenderClock.Sample(beam.Age)); break;
                    case MeridianBastion gun:
                        RitualGrandArt.Battery(b, gun, RitualRenderClock.Sample(gun.Age)); break;
                    case ChoirRequiem requiem:
                        RitualGrandArt.Requiem(b, requiem, RitualRenderClock.Sample(requiem.Age)); break;
                    case WitnessLitany litany:
                        RitualGrandArt.Witness(b, litany, RitualRenderClock.Sample(litany.Age)); break;
                    case RitualBolt bolt:
                        RitualArmamentArt.DrawFlight(b, p, center, RitualRenderClock.Sample(bolt.Age)); break;
                    case WitnessBlade blade:
                        RitualArmamentArt.DrawFlight(b, p, center, RitualRenderClock.Sample(blade.Age)); break;
                }
            }
            foreach (var impact in impacts)
                RitualArmamentArt.DrawImpact(b, impact.Position, RitualRenderClock.Sample(impact.Age), impact.Kind, impact.Strong);
        }
        finally { b.End(); }
    }
    public override void OnWorldUnload()
    {
        impacts.Clear(); shake = 0;
        foreach (var id in voices) if (SoundEngine.TryGetActiveSound(id, out var s)) s.Stop();
        voices.Clear();
    }
    public override void Unload() { OnWorldUnload(); RitualArmamentArt.Unload(); }
}
