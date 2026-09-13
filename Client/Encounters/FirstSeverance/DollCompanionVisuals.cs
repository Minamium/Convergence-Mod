#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;
using static Convergence.Client.Encounters.FirstSeverance.RitualArmamentArt;

namespace Convergence.Client.Encounters.FirstSeverance;

[Autoload(Side = ModSide.Client)]
public sealed class DollCompanionVisuals : GlobalProjectile
{
    private float previousAge = -1, beforeAngle, nowAngle, beforeMount, mount;
    private Vector2 before, now;
    private bool initialized;
    private Vector2[]? glitter;
    private int glitterHead, glitterCount;
    private int beamSlot = -1, hitDelay;
    private SlotId charge = SlotId.Invalid, sustain = SlotId.Invalid;
    internal static Asset<Texture2D>? BroomTexture;
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile p, bool lateInstantiation)
        => p.ModProjectile is DollCompanion or DollNeedle or DollLacunaBeam;
    internal Vector2 Center(Projectile p) => initialized ? Vector2.Lerp(before, now, RitualRenderClock.Fraction) : p.Center;
    internal float Angle(Projectile p) => initialized
        ? beforeAngle + MathHelper.WrapAngle(nowAngle - beforeAngle) * RitualRenderClock.Fraction : p.velocity.ToRotation();
    private DollLacunaBeam? Beam(Projectile p)
    {
        bool Matches(Projectile other) => other.active && other.owner == p.owner && (int)other.ai[2] == p.identity
            && other.ModProjectile is DollLacunaBeam;
        if (beamSlot >= 0 && beamSlot < Main.maxProjectiles && Matches(Main.projectile[beamSlot]))
            return (DollLacunaBeam)Main.projectile[beamSlot].ModProjectile;
        beamSlot = -1;
        if (p.ai[0] < DollCompanionRules.Verdict) return null;
        foreach (Projectile other in Main.ActiveProjectiles)
            if (Matches(other)) { beamSlot = other.whoAmI; return (DollLacunaBeam)other.ModProjectile; }
        return null;
    }
    internal Vector2 Aim(Projectile p)
    {
        if (p.ModProjectile is DollCompanion && Beam(p) is { } beam)
            return beam.Projectile.GetGlobalProjectile<DollCompanionVisuals>().Angle(beam.Projectile).ToRotationVector2();
        return Angle(p).ToRotationVector2();
    }
    public override void PostAI(Projectile p)
    {
        if (p.numUpdates != 0) return;
        float angle = p.velocity.ToRotation();
        if (p.ModProjectile is DollCompanion)
        {
            int target = (int)p.ai[1];
            angle = target >= 0 && target < Main.maxNPCs && Main.npc[target].active
                ? (Main.npc[target].Center - DollCompanion.Hand(p)).ToRotation()
                : (p.spriteDirection < 0 ? MathHelper.Pi : 0);
        }
        if (!initialized || Vector2.DistanceSquared(now, p.Center) > 600 * 600)
        { before = now = p.Center; beforeAngle = nowAngle = angle; glitterCount = 0; }
        else { before = now; now = p.Center; beforeAngle = nowAngle; nowAngle = angle; }
        bool first = !initialized; initialized = true;
        hitDelay = Math.Max(0, hitDelay - 1);
        if (p.ModProjectile is not DollCompanion) return;
        beforeMount = mount;
        mount = MathHelper.Lerp(mount, p.ai[2] == 1 ? 1 : 0, .22f);
        if (mount > .1f)
        {
            glitter ??= new Vector2[36];
            glitter[glitterHead] = p.Center + new Vector2(-p.spriteDirection * 35, 12);
            glitterHead = (glitterHead + 1) % glitter.Length;
            glitterCount = Math.Min(glitter.Length, glitterCount + 1);
        }
        else glitterCount = 0;
        float age = p.ai[0];
        if (age < previousAge) { previousAge = -1; RitualWeaponFeedback.Stop(ref charge); }
        bool Crossed(int beat) => previousAge < beat && age >= beat && age < beat + 3;
        if (first) RitualWeaponFeedback.Sound("DollSummon", p.Center, .325f);
        for (int i = 0; i < 3; i++)
            if (Crossed(DollCompanionRules.NeedleTick(i))) RitualWeaponFeedback.Sound("DollThread", p.Center, .34f);
        if (Crossed(DollCompanionRules.Charge)) charge = RitualWeaponFeedback.Sound("DollCharge", p.Center, .36f);
        if (Crossed(DollCompanionRules.Verdict))
        {
            RitualWeaponFeedback.Stop(ref charge);
            RitualWeaponFeedback.Sound("DollVerdict", p.Center, .43f);
            ModContent.GetInstance<RitualWeaponFeedback>().Kick(p.owner, 2.8f);
        }
        UpdateSustain(p, Beam(p) is { } active && DollCompanionRules.BeamLive(active.Age));
        previousAge = age;
    }
    private void UpdateSustain(Projectile p, bool active)
    {
        if (!active) { RitualWeaponFeedback.Stop(ref sustain); return; }
        float fade = 1 - RitualArmamentRules.Smooth((p.ai[0] - (DollCompanionRules.Verdict + DollCompanionRules.BeamTicks - 8)) / 8);
        float entry = Math.Clamp((p.ai[0] - DollCompanionRules.Verdict + 1) / 4, 0, 1);
        float volume = .43f * fade * entry;
        if (SoundEngine.TryGetActiveSound(sustain, out var voice) && voice.IsPlaying)
        {
            voice.Position = p.Center;
            voice.Volume = volume / Math.Max(.0001f, voice.Style.Volume);
            return;
        }
        int owner = p.owner, identity = p.identity, type = p.type;
        sustain = SoundEngine.PlaySound(new SoundStyle(RitualWeaponFeedback.SoundRoot + "LacunaSustain")
        {
            Identifier = $"Convergence:DollBeam:{owner}:{identity}:{type}", IsLooped = true, MaxInstances = 1,
            Volume = .43f, PauseBehavior = PauseBehavior.StopWhenGamePaused, PlayOnlyIfFocused = true,
        }, p.Center, _ => p.active && p.owner == owner && p.identity == identity && p.type == type
            && p.ai[0] >= DollCompanionRules.Verdict && p.ai[0] < DollCompanionRules.Verdict + DollCompanionRules.BeamTicks
            && RitualArmamentItems.Usable(Main.player[owner]) && !Main.player[owner].noItems && !Main.player[owner].CCed
            && Main.player[owner].HasBuff(ModContent.BuffType<DollCovenantBuff>()));
        if (SoundEngine.TryGetActiveSound(sustain, out var started)) started.Volume = volume / .43f;
        ModContent.GetInstance<RitualWeaponFeedback>().Track(sustain);
    }
    public override void OnKill(Projectile p, int timeLeft)
    { RitualWeaponFeedback.Stop(ref charge); RitualWeaponFeedback.Stop(ref sustain); }
    public override void OnHitNPC(Projectile p, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (hitDelay > 0) return;
        hitDelay = p.ModProjectile is DollLacunaBeam ? 24 : 8;
        ModContent.GetInstance<RitualWeaponFeedback>().Burst(target.Center, RitualArmamentKind.Magic, false, false);
    }
    public override bool PreDraw(Projectile p, ref Color lightColor)
    {
        Vector2 center = Center(p);
        SpriteBatch batch = Main.spriteBatch;
        if (p.ModProjectile is DollCompanion)
        {
            Vector2 foot = center + new Vector2(0, p.height * .5f);
            float time = RitualRenderClock.Time;
            float breath = MathF.Sin(time * .038f + p.identity) * .006f;
            float riding = MathHelper.Lerp(beforeMount, mount, RitualRenderClock.Fraction);
            if (glitter is not null)
                for (int i = 1; i < glitterCount; i += Reduced ? 6 : 3)
                {
                    float left = 1 - (i + RitualRenderClock.Fraction) / glitter.Length;
                    float seed = FirstSeveranceRaidVfx.Seed(p.identity + (glitterHead - i + 72) % 36);
                    Vector2 at = glitter[(glitterHead - i + glitter.Length) % glitter.Length]
                        + new Vector2(MathF.Sin(seed * 19) * 12, -i * .20f + MathF.Cos(seed * 17) * 8);
                    float twinkle = MathF.Pow(.5f + .5f * MathF.Sin(time * .28f + seed * 31), 4);
                    float power = left * riding * (.25f + .75f * twinkle);
                    Glow(batch, at, new Vector2(9 + twinkle * 8), Light(ColorFor(RitualArmamentKind.Magic), power * .65f));
                    float r = (2 + twinkle * 3) * left;
                    Line(batch, at - Vector2.UnitX * r, at + Vector2.UnitX * r, Light(Ivory, power), 1);
                    Line(batch, at - Vector2.UnitY * r, at + Vector2.UnitY * r, Light(Ivory, power), 1);
                }
            float age = p.ai[0] > 0 ? RitualRenderClock.Sample(p.ai[0]) : 0;
            Color tint = Color.Lerp(lightColor, Color.White, .40f);
            SpriteEffects flip = p.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            if (riding < .995f)
            {
                Texture2D texture = TextureAssets.Projectile[p.type].Value;
                batch.Draw(texture, foot - Main.screenPosition, new Rectangle(0, p.frame * 64, 48, 64),
                    tint * (1 - riding), p.rotation, new Vector2(24, 60), new Vector2(1 - breath, 1 + breath), flip, 0);
            }
            if (riding > .005f)
            {
                BroomTexture ??= ModContent.Request<Texture2D>("Convergence/Assets/Textures/NPCs/DollTheater/DollBroom");
                int frame = DollCompanionRules.BroomFrame(age, (int)time);
                float release = RitualKineticMotion.Recoil(age - DollCompanionRules.Verdict);
                // Authored seated/kneeling/casting cels; this secondary sway is
                // continuous and small enough to leave the actual pose readable.
                Vector2 drift = new(-p.spriteDirection * release * 3, MathF.Sin(time * .045f + p.identity) * 1.1f);
                float roll = p.rotation * .72f + MathF.Sin(time * .031f + p.identity) * .016f - release * p.spriteDirection * .055f;
                batch.Draw(BroomTexture.Value, foot + drift - Main.screenPosition, new Rectangle(0, frame * 80, 96, 80),
                    tint * riding, roll, new Vector2(48, 68), 1, flip, 0);
                if (!Reduced)
                {
                    Vector2 tail = foot + new Vector2(-p.spriteDirection * 38, -16).RotatedBy(roll);
                    for (int i = 0; i < 3; i++)
                    {
                        float phase = (time * .025f + i / 3f) % 1;
                        Vector2 at = tail - new Vector2(p.spriteDirection * phase * 32, -MathF.Sin(phase * MathHelper.Pi) * 5);
                        Glow(batch, at, new Vector2(3 + phase * 3), Light(ColorFor(RitualArmamentKind.Magic), (1 - phase) * riding * .30f));
                    }
                }
            }
        }
        else if (p.ModProjectile is DollNeedle)
        {
            Vector2 axis = p.velocity.SafeNormalize(Vector2.UnitX);
            Thread(batch, center - axis * 58, center + axis * 10, 4, Light(ColorFor(RitualArmamentKind.Magic), .9f));
            Thread(batch, center - axis * 24, center + axis * 10, 1.5f, Light(Ivory, 1));
        }
        // Continuous beam triangles and its aperture are drawn together in the
        // dedicated world pass below, not by swapping SpriteBatch in PreDraw.
        return false;
    }
    internal static void Thread(SpriteBatch b, Vector2 start, Vector2 end, float width, Color color)
    {
        Vector2 axis = end - start;
        if (width <= 0 || axis.LengthSquared() < .001f) return;
        Vector2 normal = axis.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
        Vector2 previous = start;
        int steps = Reduced ? 6 : 12;
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 next = Vector2.Lerp(start, end, t) + normal * MathF.Sin(t * MathHelper.Pi)
                * MathF.Sin(t * 19 + RitualRenderClock.Time * .21f) * width * .3f;
            Vector2 delta = next - previous;
            b.Draw(TextureAssets.MagicPixel.Value, previous - Main.screenPosition, new Rectangle(0, 0, 1, 1), color,
                delta.ToRotation(), new Vector2(0, .5f), new Vector2(delta.Length() + 1, width * (.35f + .65f * MathF.Sin(t * MathHelper.Pi))), SpriteEffects.None, 0);
            previous = next;
        }
    }
}

// Independent client-only pass so the native companion need not masquerade as
// a player-held Lacuna channel or change the existing weapon feedback router.
[Autoload(Side = ModSide.Client)]
public sealed class DollCompanionEffects : ModSystem
{
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        bool any = false;
        RitualSurfacePass.Begin();
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is DollCompanion) any = true;
            if (p.ModProjectile is not DollLacunaBeam beam) continue;
            var state = p.GetGlobalProjectile<DollCompanionVisuals>();
            float age = RitualRenderClock.Sample(beam.Age);
            float size = DollCompanionRules.BeamScale(age);
            // Exact same flowing purple beam material as Lacuna Testament.
            RitualGrandArt.QueueBeam(state.Center(p), state.Aim(p), DollCompanionRules.BeamLength * size,
                DollCompanionRules.BeamWidth * size, age, ColorFor(RitualArmamentKind.Magic), Reduced ? .72f : 1);
        }
        RitualSurfacePass.Flush();
        if (!any) return;
        SpriteBatch b = Main.spriteBatch;
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.ModProjectile is DollCompanion && p.ai[0] > 0) DrawSpell(b, p);
        }
        finally { b.End(); }
    }
    private static void DrawSpell(SpriteBatch b, Projectile p)
    {
        float age = RitualRenderClock.Sample(p.ai[0]);
        float fireAge = age - DollCompanionRules.Verdict;
        float fade = 1 - RitualArmamentRules.Smooth((fireAge - DollCompanionRules.BeamTicks) / DollCompanionRules.BeamAfterglow);
        if (fade <= 0) return;
        var state = p.GetGlobalProjectile<DollCompanionVisuals>();
        Vector2 axis = state.Aim(p), normal = axis.RotatedBy(MathHelper.PiOver2);
        Vector2 hand = DollCompanion.Hand(p) + state.Center(p) - p.Center;
        Vector2 muzzle = hand + axis * 35;
        Color purple = ColorFor(RitualArmamentKind.Magic);
        float merge = DollCompanionRules.MergeAmount(age), charge = DollCompanionRules.ChargeAmount(age);
        for (int i = 0; i < 3; i++)
        {
            float arrival = RitualKineticMotion.Arrive((age - DollCompanionRules.SigilBirth(i)) / 6);
            float visible = arrival * (1 - merge);
            if (visible <= .001f) continue;
            Vector2 at = DollCompanion.Sigil(hand, axis, i, age);
            float kick = RitualKineticMotion.Recoil(age - DollCompanionRules.NeedleTick(i));
            float radius = (18 + i * 3 + kick * 5) * arrival;
            RitualKineticArt.Seal(b, at, radius, axis.ToRotation() + .15f * MathF.Sin(age * .025f + i), purple, visible);
            Ring(b, at, new Vector2(radius * .47f), -age * .04f + i, Light(Ivory, visible * .45f), 1);
            DollCompanionVisuals.Thread(b, hand, at, 1.5f, Light(purple, visible * .4f));
            Glow(b, at, new Vector2(8 + kick * 15), Light(purple, visible * (.18f + kick * .8f)));
        }
        if (age < DollCompanionRules.Merge) return;
        float assembled = RitualKineticMotion.Arrive((age - DollCompanionRules.Merge) / 8);
        float opening = DollCompanionRules.BeamScale(fireAge);
        float recoil = RitualKineticMotion.Recoil(fireAge);
        float radiusMain = (34 - charge * 9 + opening * 14 + recoil * 8) * assembled;
        float strength = assembled * fade;
        float angle = axis.ToRotation();
        // Front-facing aperture is anchored to the palm and the exact beam
        // origin. Three throats bridge them; there is no detached square muzzle.
        for (int tier = 0; tier < 3; tier++)
        {
            Vector2 at = Vector2.Lerp(hand, muzzle, .4f + tier * .3f);
            Ring(b, at, new Vector2(3 + tier * 1.4f, radiusMain * (.46f + tier * .18f)), angle,
                Light(tier == 1 ? Ivory : purple, strength * (.38f + opening * .25f)), 1.6f + opening, true);
        }
        Ring(b, muzzle, new Vector2(radiusMain * .22f, radiusMain), angle, Light(purple, strength * .95f), 2.4f, true);
        Ring(b, muzzle, new Vector2(radiusMain * .14f, radiusMain * .75f), angle, Light(Ivory, strength * .6f), 1.4f);
        int facets = Reduced ? 6 : 10;
        for (int i = 0; i < facets; i++)
        {
            float a = i * MathHelper.TwoPi / facets + age * .026f;
            Vector2 spoke = new Vector2(MathF.Cos(a) * radiusMain * .24f, MathF.Sin(a) * radiusMain).RotatedBy(angle);
            Line(b, muzzle + spoke * .82f, muzzle + spoke, Light(purple, strength * .65f), 2);
        }
        DollCompanionVisuals.Thread(b, hand, muzzle, 3 + charge * 3 + opening * 3, Light(purple, strength * .65f));
        Glow(b, muzzle, new Vector2(12 + charge * 12 + opening * 22), Light(purple, strength * (.26f + charge * .32f + opening * .3f)));
        if (fireAge < 0)
        {
            for (int i = 0; i < (Reduced ? 3 : 5); i++)
            {
                float a = age * .095f + i * MathHelper.TwoPi / 5;
                Vector2 from = muzzle + new Vector2(MathF.Cos(a), MathF.Sin(a)) * (40 - charge * 21);
                DollCompanionVisuals.Thread(b, from, muzzle, 1 + charge * 1.5f, Light(purple, charge * strength * .7f));
            }
            return;
        }
        // Arrival -> short brake -> explosive release -> persistent pressure,
        // not new charge flashes or sounds at each local NPC immunity interval.
        Glow(b, muzzle, new Vector2(25 + recoil * 55), Light(Ivory, recoil * strength * (Reduced ? .35f : .65f)));
        if (recoil > 0)
            Line(b, muzzle - normal * recoil * 64, muzzle + normal * recoil * 64, Light(Ivory, recoil * strength), 3 * recoil);
        for (int i = 0; i < (Reduced ? 2 : 4); i++)
        {
            float t = (fireAge / 26 + i / 4f) % 1;
            Ring(b, muzzle + axis * (8 + t * 110), new Vector2(4 + t * 9, 16 + t * 12), angle,
                Light(purple, (1 - t) * opening * fade * .55f), 2 * (1 - t), true);
        }
    }
    public override void Unload() => DollCompanionVisuals.BroomTexture = null;
}
