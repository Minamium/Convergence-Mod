using ScarletGraphicsScope = Convergence.Client.Graphics.WorldGraphicsScope;
#nullable enable
using System;
using Convergence.Client.Encounters.CrimsonFoundry.Vfx;
using Convergence.Content.Encounters.CrimsonFoundry;
using Luminance.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.CrimsonFoundry;

// Vespera and the free-standing apparitions: the part of the rig that needs no boss, effigy or projectile, so the
// offline preview can link it unchanged. The boss/effigy entry points stay in CrimsonRig.cs.
internal static partial class CrimsonRig
{
    // The accepted charge/recoil from plan lists (the boss form passes ScarletCueFrame's one scan per frame).
    internal static (float Charge, float Recoil) Signal(ReadOnlySpan<CrimsonGesturePlan> gestures,
        ReadOnlySpan<CrimsonChorusPlan> choruses, int source, float age)
    {
        var (until, since) = ScarletNotes.SignalTimes(gestures, choruses, source, age);
        return (CrimsonRigMotion.Charge(until), CrimsonRigMotion.Recoil(since));
    }
    private static Texture2D? performer;
    private static readonly Rectangle[] poses = { new(20, 202, 335, 540), new(422, 202, 460, 540), new(890, 190, 487, 530), new(1398, 202, 355, 540) };
    private static readonly Vector2[] pivots = { new(190, 278), new(223, 278), new(276, 270), new(207, 278) };
    private static void LoadPerformer() => performer = LoadTexture("ScarletConjurer");
    private static Texture2D LoadTexture(string name) => ModContent.Request<Texture2D>("Convergence/Assets/Textures/CrimsonFoundry/" + name, AssetRequestMode.ImmediateLoad).Value;
    private static readonly Color RimColor = new(243, 118, 136, 0), HotRim = new(255, 96, 72, 0);
    // The trailing arguments carry Vespera's command (DrawConductor) and default to today's picture, so the companion
    // and every positional caller draw exactly as before: `cast` replaces the pose-1 weight Ease(charge * 2), `lean`
    // adds to the tilt (radians), the nudge is whole pixels (PointClamp), `rimBoost` adds to the rim's .26 alpha and
    // `rimHeat` warms the rim toward ember red.
    internal static void DrawPerformer(SpriteBatch batch, Vector2 screen, Vector2 center, float age, Vector2 velocity,
        int facing, bool floating, float charge, float recoil, float alpha = 1, bool showOrb = true, float materialized = 1,
        float? cast = null, float lean = 0, int nudgeX = 0, int nudgeY = 0, float rimBoost = 0, float rimHeat = 0)
    {
        if (performer is not { } sprite) return;
        int idle = floating ? 2 : Math.Abs(velocity.X) > .7f && MathF.Sin(age * .22f) > 0 ? 3 : 0;
        float castWeight = cast ?? CrimsonInvocation.Ease(charge * 2);
        Color rimColor = rimHeat > 0 ? Color.Lerp(RimColor, HotRim, rimHeat) : RimColor;
        // A tiny character does not benefit from a 24x32 apparition mesh.
        // Keep her authored pixels, a restrained silhouette rim, and no huge orb
        // over her face. Preserve the caller's batch including UI/sky transforms.
        using (var scope = new ScarletGraphicsScope(batch))
        {
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            DrawPose(idle, 1 - castWeight); DrawPose(1, castWeight);
            batch.End();
        }
        Vector2 orb = center + new Vector2(facing * (94 + charge * 12), -25).RotatedBy(velocity.X * .009f);
        CrimsonEnergy.Begin();
        if(showOrb) CrimsonEnergy.AddCore(orb, 53 + charge * 24 + recoil * 18, age, charge, recoil, alpha, CrimsonVisuals.Reduced);
        CrimsonEnergy.Draw(batch);
        if (floating && !CrimsonVisuals.Reduced)
        {
            var bloom = MiscTexturesRegistry.BloomCircleSmall.Value;
            for (int i = 0; i < 7; i++)
            {
                float life = (age * .017f + i * .143f) % 1;
                Vector2 at = center + new Vector2(MathF.Sin(i * 3.1f + life * 5) * 17, 16 + life * 36) - velocity * life * 1.4f;
                batch.Draw(bloom, at - screen, null, new Color(255, 76, 100, 0) * ((1 - life) * .42f * alpha), 0,
                    bloom.Size() * .5f, (2 + MathF.Sin(life * MathF.PI) * 3) / bloom.Width, SpriteEffects.None, 0);
            }
        }
        void DrawPose(int pose, float opacity)
        {
            if (opacity < .001f) return;
            Vector2 at = center - screen + new Vector2(0, floating ? MathF.Sin(age * .045f) * 1.4f : 0) + new Vector2(nudgeX, nudgeY);
            float tilt = Math.Clamp(velocity.X * .009f, -.13f, .13f) - recoil * .035f + lean;
            var flip = facing < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var origin = pivots[pose];
            if (facing < 0) origin.X = poses[pose].Width - origin.X;
            Color rim = rimColor * (alpha * opacity * (.26f + rimBoost));
            for (int k = 0; k < 4; k++)
                batch.Draw(sprite, at + new Vector2(k == 0 ? -1 : k == 1 ? 1 : 0, k == 2 ? -1 : k == 3 ? 1 : 0),
                    poses[pose], rim, tilt, origin, 56f / 540, flip, 0);
            if(materialized>=.999f) batch.Draw(sprite, at, poses[pose], Color.White * (alpha * opacity), tilt, origin, 56f / 540, flip, 0);
            else for(int strip=0;strip<18;strip++) {
                var source=poses[pose];int h=source.Height/18;int y=strip*h;source.Y+=y;source.Height=Math.Min(h,poses[pose].Height-y);
                float settle=CrimsonInvocation.Ease((materialized-strip*.025f)*2.1f);
                Vector2 drift=new Vector2(MathF.Sin(strip*4.3f)*55*(1-settle),0);
                batch.Draw(sprite,at+drift,source,Color.Lerp(new Color(255,37,86),Color.White,settle)*(alpha*opacity*settle),
                    tilt,origin-new Vector2(0,y),56f/540,flip,0);
            }
        }
    }
    // Vespera's held orb. Today's hold: facing * (94 + 12 charge) px beside her and 25 px up, radius
    // 53 + 24 charge + 18 recoil. Her command moves it at most 24 px and never toward her, and its glow may grow only
    // as far as the orb has moved away from her, so the orb's near edge never comes closer to her face (or to the
    // Hands' central gap she stands in) than today's. With no note this is today's hold exactly.
    internal readonly record struct ConductorHold(Vector2 Offset, float Radius, float Charge, float Impulse, float Alpha);
    internal static ConductorHold Hold(in ScarletCommand command, float charge, float recoil, int facing)
    {
        float today = 53 + charge * 24 + recoil * 18;
        float away = facing >= 0 ? command.OrbX : -command.OrbX; // >= 0: Command clamps the offset away from her
        return new(new Vector2(facing * (94 + charge * 12) + command.OrbX, -25 + command.OrbY),
            Math.Min(command.Radius, today + away), command.ReactorCharge, command.ReactorImpulse, command.AlphaScale);
    }
    // She takes the casting pose CastLead ticks before a note is born, over CastBlend ticks, and holds it through the
    // note's window [Born - CastLead, Close). Windows that touch or overlap are one stretch: a pickup's window closes where
    // cell A's first note is born, so that pair keeps the pose. The closing crossflow's window closes on the next phrase's
    // fourth eighth, but the note after it is cell B's first or a signature move's first step, born one eighth (14 ticks)
    // later: her next window opens 8-9 ticks after it closed and she eases toward the idle pose in between (15 ticks below
    // full pose, 9 of them fully idle; 13-25 after a signature move's final step, whose window is longer). That is a breath
    // between two phrases, not a blink: the offline gate (G6v) forbids a dip of at most 8 ticks and reports every rest.
    internal const int CastLead = 6, CastBlend = 4;
    // The notes Vespera answers: her Act's body (source) holding at `age`, plus the ones born within CastLead ticks
    // (they only lead the pose in; ScarletGestureMotion.Command gives a note nothing before its Born). Output needs
    // 2 * ScarletNotes.Capacity slots. DrawConductor calls it twice: over every plan for the casting pose (timing only) and
    // over the plans whose aim is known for the orb (the Command turns toward the aim).
    internal static int ConductorNotes(ReadOnlySpan<CrimsonGesturePlan> gestures, int source, float age, float x, float y, Span<ScarletNote> output)
    {
        int count = ScarletNotes.Collect(gestures, source, age, false, x, y, output[..ScarletNotes.Capacity]);
        Span<ScarletNote> soon = stackalloc ScarletNote[ScarletNotes.Capacity];
        int coming = ScarletNotes.Collect(gestures, source, age + CastLead, false, x, y, soon);
        for (int i = 0; i < coming; i++)
        {
            bool known = false;
            for (int j = 0; j < count && !known; j++)
                known = output[j].Phrase == soon[i].Phrase && output[j].Pulse == soon[i].Pulse && output[j].Source == soon[i].Source;
            if (!known && count < output.Length) output[count++] = soon[i];
        }
        return count;
    }
    // The pose-1 weight: today's Ease(charge * 2), held at 1 inside the stretch of cast windows that contains `age`
    // (eased in at its start and out at its end).
    internal static float ConductorCast(float age, ReadOnlySpan<ScarletNote> notes, float charge)
    {
        float start = float.MaxValue, end = float.MinValue;
        foreach (var n in notes)
            if (age >= n.Born - CastLead && age < n.Close) { start = n.Born - CastLead; end = n.Close; break; }
        for (bool grown = start <= end; grown;)
        {
            grown = false;
            foreach (var n in notes)
            {
                float from = n.Born - CastLead, to = n.Close;
                if (from > end || to < start || from >= start && to <= end) continue;
                start = Math.Min(start, from); end = Math.Max(end, to); grown = true;
            }
        }
        float window = start > end ? 0 : CrimsonInvocation.Ease((age - start) / CastBlend) * (1 - CrimsonInvocation.Ease((age - (end - CastBlend)) / CastBlend));
        return 1 - (1 - CrimsonInvocation.Ease(charge * 2)) * (1 - window);
    }
    // The boss's Vespera (CrimsonRig.Draw): the performer and the held orb from the frame's plans, commanded by the
    // notes of her Act's body (`source`; -1 = no command: a non-participant, the opening, Final = today's picture).
    // She draws the orb back against where a note will strike while it is announced, releases it toward the strike on
    // Fire and settles when the window closes; the notes' Born / Fire / End are the only clock (no beat grid).
    // `gather` pulls the orb into her (Final). The offline harness calls this with its plan list.
    internal static void DrawConductor(SpriteBatch batch, Vector2 screen, Vector2 at, float age, Vector2 velocity, int facing,
        ReadOnlySpan<CrimsonGesturePlan> gestures, ReadOnlySpan<CrimsonChorusPlan> choruses, int source,
        float ending, float reveal, float consumed, float gather)
        => DrawConductor(batch, screen, at, age, velocity, facing, gestures, gestures, choruses, source, ending, reveal, consumed, gather);
    // `known` are the plans whose aim is known (the boss form passes ScarletCueFrame's Known: an aimed plan only once its
    // lock arrived, so a peer's orb never turns toward a stale Target); the casting pose reads every plan's timing.
    internal static void DrawConductor(SpriteBatch batch, Vector2 screen, Vector2 at, float age, Vector2 velocity, int facing,
        ReadOnlySpan<CrimsonGesturePlan> gestures, ReadOnlySpan<CrimsonGesturePlan> known, ReadOnlySpan<CrimsonChorusPlan> choruses,
        int source, float ending, float reveal, float consumed, float gather)
    {
        var (charge, recoil) = Signal(gestures, choruses, -1, age);
        Span<ScarletNote> casting = stackalloc ScarletNote[ScarletNotes.Capacity * 2], notes = stackalloc ScarletNote[ScarletNotes.Capacity * 2];
        int cast = source >= 0 ? ConductorNotes(gestures, source, age, at.X, at.Y, casting) : 0;
        int noted = source >= 0 ? ConductorNotes(known, source, age, at.X, at.Y, notes) : 0;
        var command = ScarletGestureMotion.Command(age, notes[..noted], charge, recoil, facing, CrimsonVisuals.Reduced);
        DrawPerformer(batch, screen, at, age, velocity, facing, true, charge, recoil, ending * reveal * (1 - consumed), false,
            reveal * (1 - consumed), cast == 0 ? (float?)null : ConductorCast(age, casting[..cast], charge), command.Tilt,
            command.NudgeX, command.NudgeY, command.RimAlpha - .26f, command.RimHeat);
        var hold = Hold(command, charge, recoil, facing);
        Vector2 waiting = new(MathF.Sin(age * .022f) * 8, -20 + MathF.Sin(age * .031f) * 11);
        Vector2 orb = at + Vector2.Lerp(waiting, hold.Offset, reveal);
        if (gather > 0) orb = Vector2.Lerp(orb, at + new Vector2(0, -20), gather);
        float radius = MathHelper.Lerp(64 + MathF.Sin(age * .038f) * 5, hold.Radius, reveal);
        CrimsonEnergy.Begin();
        CrimsonEnergy.AddCore(orb, radius, age, Math.Max(hold.Charge, reveal * (1 - reveal) * 3), hold.Impulse,
            ending * (1 - consumed) * hold.Alpha, CrimsonVisuals.Reduced);
        CrimsonEnergy.Draw(batch);
    }
    internal static void DrawApparition(SpriteBatch batch, int species, Vector2 center, float age, float reveal, float dissolve, float scale = .70f)
    {
        if (reveal <= 0 || dissolve >= 1) return;
        float height = species == 0 ? 350 : species == 2 ? 490 : 430;
        if (species == 2)
        {
            CrimsonChoirRig.Draw(batch, center, height * scale, age + species * 37,
                .7f, 0, reveal, false, dissolve: dissolve);
            return;
        }
        ScarletApparitionRig.Draw(batch,species,center,height*scale,age,.7f,0,reveal,dissolve:dissolve);
    }
    internal static void DrawPressure(SpriteBatch batch, Vector2 center, int source, float age, float charge, float recoil)
    {
        // Broken converging filaments, not a UI target circle or opaque halo.
        var bloom = MiscTexturesRegistry.BloomCircleSmall.Value;
        Color hue = ScarletMaterials.Palette(source); hue.A = 0;
        float pulse = .78f + .22f * MathF.Pow(Math.Max(0, MathF.Sin(age * .36f)), 3);
        int count = CrimsonVisuals.Reduced ? 4 : 11;
        for (int i = 0; i < count; i++)
        {
            float drift = (age * .022f + i * .618034f) % 1;
            float angle = i * 2.399963f + MathF.Sin(i * 2.1f) * .2f;
            float reach = (source == 3 ? 65 : 210) * (1 - drift * charge) + recoil * 85;
            Vector2 offset = new Vector2(reach, 0).RotatedBy(angle);
            float brightness = MathF.Sin(drift * MathF.PI) * charge * .52f;
            batch.Draw(bloom, center + offset - Main.screenPosition, null, hue * brightness, angle,
                bloom.Size() * .5f, new Vector2(17 + charge * 22, 2.8f) / bloom.Width, SpriteEffects.None, 0);
        }
        float radius = source == 3 ? 35 : 82;
        batch.Draw(bloom, center - Main.screenPosition, null, hue * (charge * pulse * .36f + recoil * .25f), 0,
            bloom.Size() * .5f, (radius + charge * 25 + recoil * 42) * 2 / bloom.Width, SpriteEffects.None, 0);
        if (recoil > .02f)
            batch.Draw(bloom, center - Main.screenPosition, null, new Color(255, 222, 214, 0) * (recoil * .58f), -.35f,
                bloom.Size() * .5f, new Vector2(150, 7) * (CrimsonVisuals.Reduced ? .45f : 1) / bloom.Width, SpriteEffects.None, 0);
    }
}
