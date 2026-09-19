#nullable enable
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Content.Encounters.CrimsonFoundry;

internal sealed record CrimsonGestureSource(CrimsonGesturePlan Plan) : IEntitySource
{ public string Context => "ScarletTechnique"; }

// One projectile owns a complete impact, not one per limb or cut.
public sealed class CrimsonGesture : ModProjectile
{
    internal CrimsonGesturePlan Plan;
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 32;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 900; Projectile.netImportant = true;
    }
    public override void OnSpawn(IEntitySource source)
    { if (source is CrimsonGestureSource owned) { owned.Plan.Validate(); Plan = owned.Plan; } }
    public override bool PreDraw(ref Color lightColor) => false;
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanHitNPC(NPC target) => false;
    public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        => modifiers.SetMaxDamage(CrimsonPlaytestTuning.AttackDamage);
    public override bool? CanCutTiles() => false;
    internal bool TryBoss(out CrimsonBoss? boss)
    {
        boss = Plan.Boss >= 0 && Plan.Boss < Main.maxNPCs && Main.npc[Plan.Boss].active
            ? Main.npc[Plan.Boss].ModNPC as CrimsonBoss : null;
        return Plan.Fight != Guid.Empty && boss is not null && boss.Fresh
            && boss.State.Fight == Plan.Fight && boss.State.PhaseStart == Plan.Epoch
            && boss.State.Stage is CrimsonStage.Countdown or CrimsonStage.Performance
            && CrimsonPhaseRules.ActiveSource(boss.State.Phase, boss.State.DefeatedMask, boss.State.PerformerDefeated, Plan.Source);
    }
    internal static float Clock(CrimsonBoss boss) => boss.VisualAge;
    public override bool? CanDamage() => TryBoss(out var boss) && Plan.Live(Clock(boss!))
        && boss!.State.SourceActive(Plan.Source, Clock(boss)) ? null : false;
    public override bool CanHitPlayer(Player target) => TryBoss(out var boss) && boss!.State.Contains(target.whoAmI);
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (!TryBoss(out var boss) || !boss!.State.SourceActive(Plan.Source, Clock(boss)) || !Plan.Live(Clock(boss))) return false;
        Span<CrimsonStroke> strokes = stackalloc CrimsonStroke[CrimsonTechniqueGeometry.MaximumStrokes];
        int count = CrimsonTechniqueGeometry.Write(Plan, Clock(boss), strokes);
        for (int i = 0; i < count; i++)
            if (CrimsonTechniqueGeometry.Intersects(strokes[i], targetHitbox.X, targetHitbox.Y, targetHitbox.Width, targetHitbox.Height)) return true;
        return false;
    }
    public override void AI()
    {
        bool valid = TryBoss(out var boss);
        Projectile.hostile = valid && boss!.State.SourceActive(Plan.Source, Clock(boss)) && Plan.Live(Clock(boss));
        Projectile.damage = CrimsonPlaytestTuning.AttackDamage;
        Projectile.Center = new Vector2(Plan.Stage.X, Plan.Stage.Y);
        if (valid)
        {
            float age = Clock(boss!);
            Projectile.timeLeft = Math.Max(2, Plan.LastEnd + CrimsonRhythm.LeaseTicks - (int)age);
            if (Main.netMode != NetmodeID.MultiplayerClient && age >= Plan.LastEnd + CrimsonRhythm.LeaseTicks) Projectile.Kill();
        }
        else if (Main.netMode != NetmodeID.MultiplayerClient) Projectile.Kill();
    }
    public override void SendExtraAI(BinaryWriter writer) => Plan.Write(writer);
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        var next = CrimsonGesturePlan.Read(reader);
        if (Main.netMode != NetmodeID.Server && (Plan.Fight == Guid.Empty || next == Plan)) Plan = next;
    }
    internal static bool TryPose(CrimsonBoss boss, int source, float age, out CrimsonGesturePlan plan)
    {
        plan = default; bool found = false;
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is not CrimsonGesture g || !g.TryBoss(out var parent) || parent != boss) continue;
            var candidate = g.Plan;
            if (candidate.Source != source || age < candidate.Begin || age >= candidate.LastEnd + CrimsonRhythm.PoseRecoveryTicks) continue;
            if (!found || candidate.Phrase > plan.Phrase || candidate.Phrase == plan.Phrase
                && (candidate.Fire <= age && (plan.Fire > age || candidate.Fire > plan.Fire)
                    || candidate.Fire > age && plan.Fire > age && candidate.Fire < plan.Fire))
            { plan = candidate; found = true; }
        }
        return found;
    }
    internal static bool ProjectMotion(NPC npc, CrimsonBoss boss, int source)
    {
        float age = Clock(boss);
        if (!TryPose(boss, source, age, out var plan)) return false;
        var point = plan.Body(age); var previous = plan.Body(age - 1);
        npc.Center = new(point.X, point.Y);
        npc.velocity = Vector2.Zero; // Native integration must not apply this step twice.
        npc.rotation = Math.Clamp((point.X - previous.X) * .007f, -.4f, .4f);
        if (Math.Abs(point.X - previous.X) > .1f) npc.spriteDirection = point.X > previous.X ? 1 : -1;
        return true;
    }
}
