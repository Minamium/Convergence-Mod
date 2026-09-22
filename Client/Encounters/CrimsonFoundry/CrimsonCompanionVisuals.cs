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
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.ModProjectile is CrimsonCompanion or CrimsonCompanionRay;
    public override void PostAI(Projectile p)
    {
        if (p.ModProjectile is CrimsonCompanion)
        {
            int chargeAt = CrimsonCompanion.Fire - CrimsonCovenantRules.ChargeTicks;
            if (previous < chargeAt && p.ai[0] >= chargeAt && p.ai[0] < chargeAt + 4) Play("PortalCharge", .36f, p.Center);
            if (previous < CrimsonCompanion.Fire && p.ai[0] >= CrimsonCompanion.Fire && p.ai[0] < CrimsonCompanion.Fire + 4)
                Play("PortalFire", .60f, p.Center); // One batch accent, not twenty stacked samples.
            previous = p.ai[0];
        }
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
            float seal = CrimsonInvocation.Ease((phase - 2) / 7) * (1 - CrimsonInvocation.Ease((phase - 48) / 12));
            ScarletSorcery.Seal(Main.spriteBatch, p.Center + new Vector2(0, -9), 66 + charge * 30,
                .82f, -.2f, Main.GlobalTimeWrappedHourly * 60, Math.Max(.25f, charge), seal * .9f, false, p.identity);
            CrimsonRig.DrawPerformer(Main.spriteBatch, Main.screenPosition, p.Center, Main.GlobalTimeWrappedHourly * 60,
                p.velocity, p.spriteDirection, p.ai[2] == 1, charge, recoil);
        }
        else if (p.ModProjectile is CrimsonCompanionRay ray)
        {
            if (ray.BatchCount > 0) ScarletSorcery.CovenantClamp(Main.spriteBatch, p.Center, p.ai[1], p.ai[0], ray.Size);
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
