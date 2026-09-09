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
    public override bool InstancePerEntity => true;
    internal static bool Matches(Projectile p) => p.ModProjectile is RitualBolt or WitnessBlade or ChoirSentinel or RitualArmamentPose or WitnessVerdict or LacunaConvergence;
    public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation) => Matches(projectile);
    internal Vector2 Center(Projectile p) => initialized
        ? Vector2.Lerp(previousCenter, currentCenter, RitualRenderClock.Fraction) : p.Center;
    internal float Angle(Projectile p) => initialized
        ? previousAngle + MathF.IEEERemainder(angle - previousAngle, MathF.Tau) * RitualRenderClock.Fraction : p.rotation;
    internal static RitualArmamentKind Kind(Projectile p) => p.ModProjectile switch
    {
        RitualBolt bolt => bolt.Kind, WitnessBlade or WitnessVerdict => RitualArmamentKind.Rogue,
        ChoirSentinel => RitualArmamentKind.Summon, LacunaConvergence => RitualArmamentKind.Magic,
        RitualArmamentPose pose => pose.Kind, _ => RitualArmamentKind.Melee,
    };
    public override void PostAI(Projectile p)
    {
        if (p.numUpdates != 0) return;
        if (!initialized || Vector2.DistanceSquared(currentCenter, p.Center) > 1800 * 1800)
        { previousCenter = currentCenter = p.Center; previousAngle = angle = p.rotation; initialized = true; }
        else
        {
            previousCenter = currentCenter; currentCenter = p.Center;
            previousAngle = angle;
            angle += MathF.IEEERemainder(p.rotation - angle, MathF.Tau) * (p.ModProjectile is RitualArmamentPose ? .46f : 1);
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
                        RitualWeaponFeedback.Sound(pose.Empowered ? "CoreSalvoFire" : "LanceFire", p.Center,
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
                    RitualArmamentKind.Ranged => pose.Empowered ? "CoreSalvoFire" : "LanceFire",
                    RitualArmamentKind.Magic => pose.Empowered ? "GridFire" : "SpreadExecution",
                    RitualArmamentKind.Summon => "CoreExposure", _ => "BladeUnsheathe",
                };
                RitualWeaponFeedback.Sound(cue, p.Center, pose.Empowered ? .56f : .31f);
            }
            lastAge = pose.Age;
        }
        else if (p.ModProjectile is LacunaConvergence beam)
        {
            for (int sigil = 0; sigil < 5; sigil++)
            {
                float tick = 1 + sigil * 2;
                if (lastAge < tick && beam.Age >= tick && beam.Age < tick + 2)
                    RitualWeaponFeedback.Sound("ShellHit", p.Center, .10f + sigil * .018f);
            }
            if (lastAge < 10 && beam.Age >= 10 && beam.Age < 12)
                RitualWeaponFeedback.Sound("ExecutionLock", beam.Muzzle, .23f);
            if (lastAge < RitualKineticMotion.MagicFire && beam.Age >= RitualKineticMotion.MagicFire && beam.Age < RitualKineticMotion.MagicFire + 2)
            {
                RitualWeaponFeedback.Sound("GridFire", beam.Muzzle, beam.Empowered ? .58f : .36f);
                RitualWeaponFeedback.Sound("CoreSalvoFire", beam.Muzzle, beam.Empowered ? .40f : .22f);
                ModContent.GetInstance<RitualWeaponFeedback>().Kick(p.owner, beam.Empowered ? 5 : 2);
            }
            lastAge = beam.Age;
        }
        else if (p.ModProjectile is WitnessVerdict verdict)
        {
            if (lastAge < 16 && verdict.Age >= 16 && verdict.Age < 20)
                RitualWeaponFeedback.Sound("ExecutionLock", p.Center, .42f);
            if (lastAge < 23 && verdict.Age >= 23 && verdict.Age < 25)
                RitualWeaponFeedback.Sound("BladeUnsheathe", p.Center, .34f);
            if (lastAge < 28 && verdict.Age >= 28 && verdict.Age < 32)
            {
                RitualWeaponFeedback.Sound("HandCrushImpact", p.Center, .62f);
                ModContent.GetInstance<RitualWeaponFeedback>().Kick(p.owner, 5.5f);
            }
            lastAge = verdict.Age;
        }
        else if (p.ModProjectile is ChoirSentinel s)
        {
            float phase = RitualArmamentChoreography.Mod(s.Age, RitualArmamentChoreography.ChoirCycle);
            if (s.IsLeader && phase >= 76 && phase < 78 && (lastAge < 76 || lastAge > 100))
                RitualWeaponFeedback.Sound("ExecutionLock", s.ConcertCenter, .30f);
            if (s.IsLeader && phase >= 88 && phase < 90 && (lastAge < 88 || lastAge > 100))
            {
                RitualWeaponFeedback.Sound("CoreSalvoFire", p.Center, .48f);
                ModContent.GetInstance<RitualWeaponFeedback>().Kick(p.owner, 3.5f);
            }
            lastAge = phase;
        }
        else if (!sounded && p.ModProjectile is ChoirNote)
        { sounded = true; RitualWeaponFeedback.Sound("SpreadExecution", p.Center, .16f); }
    }
    public override void OnHitNPC(Projectile p, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (impactSeen || p.ModProjectile is WitnessVerdict) return;
        impactSeen = true;
        ModContent.GetInstance<RitualWeaponFeedback>().Burst(target.Center, Kind(p), p.owner == Main.myPlayer,
            p.ModProjectile is RitualBolt { Empowered: true } or WitnessBlade { Stealth: true } or LacunaConvergence { Empowered: true });
    }
    public override void OnKill(Projectile p, int timeLeft)
    {
        if (!impactSeen && p.ModProjectile is RitualBolt && !Main.gameMenu)
            ModContent.GetInstance<RitualWeaponFeedback>().Burst(p.Center, Kind(p), false, false);
    }
    // One linear-sampled dedicated draw pass for all new weapon parts/surfaces.
    public override bool PreDraw(Projectile projectile, ref Color lightColor) => false;
}

[Autoload(Side = ModSide.Client)]
public sealed class RitualWeaponFeedback : ModSystem
{
    private sealed class Impact(Vector2 position, RitualArmamentKind kind, bool strong)
    {
        internal readonly Vector2 Position = position;
        internal readonly RitualArmamentKind Kind = kind;
        internal bool Strong = strong; internal int Age;
    }
    private readonly List<Impact> impacts = new(40);
    private readonly List<SlotId> voices = new(32);
    private float shake;
    internal static void Sound(string name, Vector2 at, float volume)
    {
        if (Main.dedServ || Main.gameMenu) return;
        var system = ModContent.GetInstance<RitualWeaponFeedback>();
        system.voices.RemoveAll(id => !SoundEngine.TryGetActiveSound(id, out var s) || !s.IsPlaying);
        if (system.voices.Count >= 40)
        { if (SoundEngine.TryGetActiveSound(system.voices[0], out var old)) old.Stop(); system.voices.RemoveAt(0); }
        system.voices.Add(SoundEngine.PlaySound(new SoundStyle("Convergence/Assets/Sounds/FirstSeverance/" + name)
        {
            Identifier = "Convergence:RitualWeapon:" + name, Volume = volume * (RitualArmamentArt.Reduced ? .65f : 1),
            PitchVariance = .055f, MaxInstances = 3, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PauseBehavior = PauseBehavior.StopWhenGamePaused, PlayOnlyIfFocused = true,
        }, at));
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
        Sound(strong ? "CoreHit" : "PylonHit", at, strong ? .42f : .23f);
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
            if (p.ModProjectile is LacunaConvergence beam) RitualKineticArt.QueueMagic(beam, RitualRenderClock.Sample(beam.Age));
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
                        RitualArmamentArt.DrawChoir(b, s, center, RitualRenderClock.Sample(s.Age)); break;
                    case WitnessVerdict v:
                        RitualArmamentArt.DrawVerdict(b, v, RitualRenderClock.Sample(v.Age)); break;
                    case LacunaConvergence beam:
                        RitualKineticArt.DrawMagic(b, beam, RitualRenderClock.Sample(beam.Age)); break;
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
