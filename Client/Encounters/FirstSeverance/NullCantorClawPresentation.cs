#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

// State is per projectile, so simultaneous players do not suppress each other's
// swings. No gameplay timer is changed by a hit accent, sound or camera impulse.
[Autoload(Side = ModSide.Client)]
public sealed class NullCantorClawVisualState : GlobalProjectile
{
    internal long Stamp;
    private float previousAge = -1;
    private bool hitPlayed;
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile p, bool lateInstantiation) => p.ModProjectile is NullCantorClawProjectile;
    public override void PostAI(Projectile p)
    {
        var claw = (NullCantorClawProjectile)p.ModProjectile;
        if (p.numUpdates == 0) Stamp = Stopwatch.GetTimestamp();
        var system = ModContent.GetInstance<NullCantorClawPresentation>();
        bool Crossed(float tick) => previousAge < tick && claw.Age >= tick && claw.Age - tick < 5;
        if (claw is NullCantorClawSwipe swipe && Crossed(swipe.Duration * NullCantorClawMotion.SweepStart))
        {
            system.Play("ClawSwipe", p.Center, .64f, -.06f + swipe.Hand * .06f);
        }
        if (claw is NullCantorClawCrush)
        {
            if (Crossed(0)) system.Play("ClawGrip", p.Center, .65f, -.06f);
            if (Crossed(NullCantorClawMotion.CrushCloseTick)) system.Play("ClawSwipe", p.Center, .68f, .08f);
            if (Crossed(NullCantorClawMotion.CrushImpactTick))
            {
                system.Play("ClawCrush", p.Center, .85f, -.04f);
                system.Kick(p.owner, 9);
            }
        }
        if (claw.HasImpact && !hitPlayed)
        {
            hitPlayed = true;
            if (claw.Age - claw.ImpactAge < 8 && claw is NullCantorClawSwipe)
            { system.Play("ClawHit", claw.Impact, .58f, -.04f); system.Kick(p.owner, 3.8f); }
        }
        previousAge = claw.Age;
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class NullCantorClawPresentation : ModSystem
{
    private readonly List<SlotId> voices = new();
    private readonly bool[] attacking = new bool[256];
    private float shake;
    internal void Play(string name, Vector2 at, float volume, float pitch)
    {
        if (Main.dedServ || Main.gameMenu) return;
        voices.RemoveAll(id => !SoundEngine.TryGetActiveSound(id, out var s) || !s.IsPlaying);
        if (voices.Count >= 32)
        { if (SoundEngine.TryGetActiveSound(voices[0], out var old)) old.Stop(); voices.RemoveAt(0); }
        voices.Add(SoundEngine.PlaySound(new SoundStyle(RitualWeaponFeedback.SoundRoot + name)
        {
            Identifier = "Convergence:NullCantorClaws:" + name,
            Volume = volume, Pitch = pitch, PitchVariance = .04f, MaxInstances = 2,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PauseBehavior = PauseBehavior.StopWhenGamePaused, PlayOnlyIfFocused = true,
        }, at));
    }
    internal void Kick(int owner, float strength) { if (owner == Main.myPlayer) shake = Math.Max(shake, strength); }
    public override void PostUpdateEverything() => shake *= .76f;
    public override void ModifyScreenPosition()
    {
        var config = ModContent.GetInstance<FirstSeveranceVisualConfig>();
        if (!Main.gameMenu && config.ScreenShake && !config.ReducedEffects)
            Main.screenPosition += new Vector2(MathF.Sin((float)Main.GameUpdateCount * 2.6f),
                MathF.Cos((float)Main.GameUpdateCount * 3.4f)) * shake;
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        Array.Clear(attacking);
        RitualSurfacePass.Begin();
        foreach (var projectile in Main.ActiveProjectiles)
            if (projectile.ModProjectile is NullCantorClawSwipe swipe && swipe.ValidOwner)
                NullCantorClawArt.QueueSwipeTrail(swipe, RitualRenderClock.Sample(swipe.Age));
        RitualSurfacePass.Flush();
        SpriteBatch b = Main.spriteBatch;
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            foreach (var p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile is not NullCantorClawProjectile claw || !claw.ValidOwner) continue;
                if (p.owner < attacking.Length) attacking[p.owner] = true;
                float age = RitualRenderClock.Sample(claw.Age);
                if (claw is NullCantorClawSwipe swipe) NullCantorClawArt.DrawSwipe(b, swipe, age);
                else NullCantorClawArt.DrawCrush(b, (NullCantorClawCrush)claw, age);
            }
            for (int slot = 0; slot < Main.maxPlayers; slot++)
            {
                Player player = Main.player[slot];
                if (!RitualArmamentItems.Usable(player) || player.HeldItem.type != ModContent.ItemType<NullRefrain>()) continue;
                if (!attacking[slot])
                    for (int hand = 0; hand < 2; hand++)
                    {
                        var pose = RitualArmamentChoreography.ParkedHand(hand, RitualRenderClock.Time, player.direction);
                        NullCantorClawArt.Hand(b, pose, player.MountedCenter, 0, 1, 1, false);
                    }
                if (slot == Main.myPlayer)
                {
                    float charge = player.GetModPlayer<NullCantorClawPlayer>().Charge.Ticks / (float)NullCantorClawMotion.ChargeTicks;
                    Vector2 center = player.MountedCenter + new Vector2(0, -52);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = -MathHelper.PiOver2 + i * MathHelper.TwoPi / 6;
                        float lit = Math.Clamp(charge * 6 - i, 0, 1);
                        Vector2 point = center + a.ToRotationVector2() * 11;
                        NullCantorClawArt.Beam(b, point, point + a.ToRotationVector2() * 5, 3,
                            NullCantorClawArt.Light(lit >= 1 ? NullCantorClawArt.Pale : NullCantorClawArt.Gold, .2f + lit * .8f));
                    }
                    if (charge >= 1) NullCantorClawArt.Glow(b, center, new(23), NullCantorClawArt.Light(NullCantorClawArt.Violet,
                        .7f + .2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3)));
                }
            }
        }
        finally { b.End(); }
    }
    public override void OnWorldUnload()
    {
        shake = 0;
        foreach (var id in voices) if (SoundEngine.TryGetActiveSound(id, out var s)) s.Stop();
        voices.Clear(); Array.Clear(attacking);
    }
    public override void Unload() { OnWorldUnload(); NullCantorClawArt.Dispose(); RitualSurfacePass.Dispose(); }
}

[Autoload(Side = ModSide.Client)]
public sealed class NullCantorClawItemVisuals : GlobalItem
{
    public override bool AppliesToEntity(Item item, bool lateInstantiation) => item.ModItem is NullRefrain;
    public override bool PreDrawInWorld(Item item, SpriteBatch b, Color lightColor, Color alphaColor,
        ref float rotation, ref float scale, int whoAmI)
    {
        var texture = NullCantorClawArt.Icon;
        Vector2 center = item.Center + new Vector2(0, MathF.Sin(Main.GlobalTimeWrappedHourly * 2) * 5);
        NullCantorClawArt.Glow(b, center, new(82), NullCantorClawArt.Light(NullCantorClawArt.Violet, .8f));
        b.Draw(texture, center - Main.screenPosition, null, Color.White,
            MathF.Sin(Main.GlobalTimeWrappedHourly) * .07f, texture.Size() * .5f, 90f / texture.Width, SpriteEffects.None, 0);
        return false;
    }
}
