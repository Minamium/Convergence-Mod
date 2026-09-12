#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

[Autoload(Side = ModSide.Client)]
public sealed class DollCompanionVisuals : GlobalProjectile
{
    private float previousAge = -1;
    private Vector2 before, now;
    private bool initialized;
    private SlotId charge = SlotId.Invalid;
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile p, bool lateInstantiation)
        => p.ModProjectile is DollCompanion or DollNeedle or DollSeam;
    public override void PostAI(Projectile p)
    {
        if (p.numUpdates != 0) return;
        if (!initialized || Vector2.DistanceSquared(now, p.Center) > 600*600) before = now = p.Center;
        else { before = now; now = p.Center; }
        bool first = !initialized; initialized = true;
        if (p.ModProjectile is not DollCompanion) return;
        float age = p.ai[0];
        if (age < previousAge) { previousAge = -1; RitualWeaponFeedback.Stop(ref charge); }
        bool Crossed(int beat) => previousAge < beat && age >= beat && age < beat + 3;
        if (first) RitualWeaponFeedback.Sound("DollSummon", p.Center, .325f);
        if (Crossed(24) || Crossed(36) || Crossed(48)) RitualWeaponFeedback.Sound("DollThread", p.Center, .325f);
        if (Crossed(60)) charge = RitualWeaponFeedback.Sound("DollCharge", p.Center, .325f);
        if (Crossed(DollCompanionRules.Verdict)) RitualWeaponFeedback.Sound("DollVerdict", p.Center, .425f);
        previousAge = age;
    }
    public override void OnKill(Projectile p, int timeLeft) => RitualWeaponFeedback.Stop(ref charge);
    public override bool PreDraw(Projectile p, ref Color lightColor)
    {
        Vector2 center = initialized ? Vector2.Lerp(before, now, RitualRenderClock.Fraction) : p.Center;
        var batch = Main.spriteBatch;
        if (p.ModProjectile is DollCompanion)
        {
            Vector2 foot = center + new Vector2(0, p.height*.5f);
            float time = RitualRenderClock.Time;
            float breath = MathF.Sin(time*.038f + p.identity)*.006f;
            Texture2D texture = TextureAssets.Projectile[p.type].Value;
            // Actual authored cels; floating tilt/breath are small secondary motion.
            batch.Draw(texture, foot - Main.screenPosition, new Rectangle(0,p.frame*64,48,64),
                Color.Lerp(lightColor,Color.White,.40f),p.rotation,new Vector2(24,60),new Vector2(1-breath,1+breath),
                p.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,0);
            float age = p.ai[0] + RitualRenderClock.Fraction;
            float charge = age is >= 60 and < 96 ? MathF.Pow((age-60)/36,2) : 0;
            if (charge > 0)
            {
                Vector2 hand = center + new Vector2(p.spriteDirection*17,-8);
                for(int i=0;i<5;i++)
                {
                    float a = time*.09f+i*MathHelper.TwoPi/5;
                    Vector2 from = hand+new Vector2(MathF.Cos(a)*28,MathF.Sin(a)*17)*(1-charge*.7f);
                    Thread(batch,from,hand,charge*1.6f,new Color(214,104,125,0)*charge);
                }
            }
        }
        else if (p.ModProjectile is DollNeedle)
        {
            Vector2 axis = p.velocity.SafeNormalize(Vector2.UnitX);
            Thread(batch,center-axis*58,center+axis*10,4,new Color(167,28,61,0));
            Thread(batch,center-axis*24,center+axis*10,1.5f,new Color(255,221,223,0));
        }
        else
        {
            float age = p.ai[0] + RitualRenderClock.Fraction;
            float open = MathHelper.Clamp(age/5,0,1);
            float fade = 1-RitualArmamentRules.Smooth((age-9)/15);
            Vector2 axis=p.velocity.SafeNormalize(Vector2.UnitX)*132*open;
            float strength = age < 5 ? .25f : fade;
            Thread(batch,center-axis,center+axis,22*strength,new Color(100,9,35,0)*strength);
            Thread(batch,center-axis,center+axis,7*strength,new Color(246,78,115,0)*strength);
            Thread(batch,center-axis,center+axis,2*strength,new Color(255,239,231,0)*strength);
        }
        return false;
    }
    private static void Thread(SpriteBatch b,Vector2 start,Vector2 end,float width,Color color)
    {
        Vector2 axis=end-start;
        if(width<=0||axis.LengthSquared()<.001f) return;
        Vector2 normal=axis.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
        Vector2 previous=start;
        for(int i=1;i<=10;i++)
        {
            float t=i/10f;
            Vector2 next=Vector2.Lerp(start,end,t)+normal*MathF.Sin(t*MathHelper.Pi)*MathF.Sin(t*19+RitualRenderClock.Time*.21f)*width*.3f;
            Vector2 delta=next-previous;
            b.Draw(TextureAssets.MagicPixel.Value,previous-Main.screenPosition,new Rectangle(0,0,1,1),color,delta.ToRotation(),new Vector2(0,.5f),
                new Vector2(delta.Length()+1,width*(.35f+.65f*MathF.Sin(t*MathHelper.Pi))),SpriteEffects.None,0);
            previous=next;
        }
    }
}
