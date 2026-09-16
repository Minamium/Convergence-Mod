using System;
using Convergence.Content.Encounters.CrimsonFoundry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonCompanionVisuals : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private float previous;
    private bool sounded;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is CrimsonCompanion or CrimsonCompanionRay;
    public override void PostAI(Projectile p)
    {
        if (p.ModProjectile is CrimsonCompanion)
        {
            if (previous < 44 && p.ai[0] >= 44 && p.ai[0] < 48) Play("PortalCharge", .26f, p.Center);
            previous = p.ai[0];
        }
        else if (!sounded && p.ai[0] < 5) { sounded = true; Play("PortalFire", .36f, p.Center); }
    }
    private static void Play(string name, float gain, Vector2 at) => SoundEngine.PlaySound(new SoundStyle("Convergence/Assets/Sounds/FirstSeverance/Beams/" + name)
    { Volume = gain, MaxInstances = 3, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest, PlayOnlyIfFocused = true, PauseBehavior = PauseBehavior.StopWhenGamePaused }, at);
    public override bool PreDraw(Projectile p, ref Color lightColor)
    {
        if (p.ModProjectile is CrimsonCompanion)
        {
            float phase = p.ai[0];
            float charge = phase > 0 && phase < CrimsonCompanion.Fire ? CrimsonRigMotion.Charge(CrimsonCompanion.Fire - phase) : 0;
            float recoil = phase >= CrimsonCompanion.Fire ? CrimsonRigMotion.Recoil(phase - CrimsonCompanion.Fire) : 0;
            CrimsonRig.DrawPerformer(Main.spriteBatch, Main.screenPosition, p.Center, Main.GlobalTimeWrappedHourly * 60,
                p.velocity, p.spriteDirection, p.ai[2] == 1, charge, recoil);
        }
        else if (p.ModProjectile is CrimsonCompanionRay ray)
        {
            CrimsonEnergy.Begin();
            CrimsonEnergy.Add(p.Center, p.velocity.SafeNormalize(Vector2.UnitX), ray.Reach, 12 * ray.Opening,
                p.ai[0], 0, 64, ray.Opening, CrimsonVisuals.Reduced);
            CrimsonEnergy.Draw(Main.spriteBatch);
        }
        return false;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class CrimsonItemVisuals : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.ModItem is CrimsonConductor or CrimsonPact;
    private static Rectangle Source(Item item) => item.ModItem is CrimsonConductor ? new(285, 195, 675, 865) : new(235, 84, 772, 1095);
    public override bool PreDrawInInventory(Item item, SpriteBatch batch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Rectangle source = Source(item);
        batch.Draw(TextureAssets.Item[item.type].Value, position, source, drawColor, 0, source.Size() * .5f,
            34f / Math.Max(source.Width, source.Height) * Main.inventoryScale, SpriteEffects.None, 0);
        return false;
    }
    public override bool PreDrawInWorld(Item item, SpriteBatch batch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        Rectangle source = Source(item);
        batch.Draw(TextureAssets.Item[item.type].Value, item.Center - Main.screenPosition, source, lightColor, rotation,
            source.Size() * .5f, 34f / Math.Max(source.Width, source.Height), SpriteEffects.None, 0);
        return false;
    }
}
