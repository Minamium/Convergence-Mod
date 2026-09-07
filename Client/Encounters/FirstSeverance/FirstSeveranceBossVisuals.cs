#nullable enable

using System;
using System.Diagnostics;
using Convergence.Common.Encounters.Abstractions;
using Convergence.Common.Foundation.Identifiers;
using Convergence.Content.Encounters.FirstSeverance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using static Convergence.Client.Encounters.FirstSeverance.FirstSeveranceVisualCurves;

namespace Convergence.Client.Encounters.FirstSeverance;

// No world actors, gameplay callbacks, shaders, or network traffic. All spatial
// facts come from the read-only snapshot; animation is disposable client state.
internal sealed class FirstSeveranceBossVisuals
{
    private const string BodyPath = "Convergence/Assets/Textures/NPCs/NullCantorRigAtlas";
    private readonly FirstSeveranceEmissionVisuals emissions = new();
    private readonly FirstSeveranceStageVisuals stages = new();
    private readonly FirstSeveranceScoreVisuals scoreVisuals = new();
    private Vector2 rigOrigin;
    private float rigScale = .66f;
    internal static readonly Color Ice = new(195, 213, 215);
    internal static readonly Color Gold = new(173, 151, 110);
    internal static readonly Color Danger = new(181, 85, 79);
    private Asset<Texture2D>? body;
    private Texture2D? glow;
    private FightId fight;
    private FirstSeveranceSubstate previousPhase;
    private float exposure;
    private Vector2 lastCenter;
    private readonly FirstSeveranceEndingTimeline ending = new();
    private bool endingParticipant;
    private long endingStamp;
    private float lastCast, lastKick, lastBreath;
    private int phaseImpactTicks;
    private float mechanicPose;
    private FirstSeveranceCombatProjection? lastCombat;

    internal double RenderTick => emissions.RenderTick;
    internal FirstSeveranceAttackAccents Accents => emissions.Accents;

    internal float PhaseShake => 9f * MathF.Pow(phaseImpactTicks / 24f, 2f);
    internal bool IsEnding => ending.IsActive(Main.GameUpdateCount);
    internal bool EndingVictory => ending.Reason == EncounterEndReason.Victory;
    internal Vector2 EndingCenter => lastCenter;
    internal float EndingAge => ending.Age(Main.GameUpdateCount, Main.gamePaused ? 0 :
        (Stopwatch.GetTimestamp() - endingStamp) * 60d / Stopwatch.Frequency);
    internal float EndingShake => !IsEnding ? 0 : EndingVictory
        ? 24 * Window(EndingAge, .69, .80) * (1 - Window(EndingAge, .80, .91))
        : 15 * (1 - Window(EndingAge, .02, .34));

    internal static Vector2 CoreCenter(FirstSeveranceCombatProjection combat)
        => new(combat.CoreX, combat.CoreY - FirstSeveranceLanceTuning.BossHeightAboveCore);

    internal void Update(FirstSeveranceClientStateSystem state)
    {
        if (Main.dedServ)
            return;
        FirstSeveranceCombatProjection? combat = state.Combat;
        if (combat is null)
        {
            phaseImpactTicks = 0;
            emissions.Clear();
            stages.Reset();
            if (!fight.IsNone)
            {
                fight = FightId.None;
                ending.Begin(state.LastCombatEndReason, endingParticipant, Main.GameUpdateCount);
            }
            if (!IsEnding)
            {
                ending.Clear();
                lastCombat = null;
                endingParticipant = false;
            }
            endingStamp = Stopwatch.GetTimestamp();
            return;
        }

        if (fight != combat.FightId)
        {
            fight = combat.FightId;
            emissions.Clear();
            stages.Reset();
            previousPhase = FirstSeveranceSubstate.None;
            exposure = 0f;
            ending.Clear();
            phaseImpactTicks = 0;
        }
        if (phaseImpactTicks > 0)
            phaseImpactTicks--;
        lastCenter = CoreCenter(combat);
        lastCombat = combat;
        endingParticipant = combat.TryGetParticipantByServerSlot(Main.myPlayer, out var local) && local.IsConnected;
        exposure = MathHelper.Lerp(exposure,
            combat.IsCoreOpen ? 1f : 0f, 0.32f);
        if (previousPhase != combat.Substate)
        {
            if (combat.Substate == FirstSeveranceSubstate.CoreExposure)
                phaseImpactTicks = 24;
            previousPhase = combat.Substate;
        }
        emissions.Update(combat.LanceVolley, state.EstimatedAuthorityTick);
        double renderTick = emissions.RenderTick;
        double lead = combat.Substate is FirstSeveranceSubstate.Stack or FirstSeveranceSubstate.Spread ? 120 : 90;
        float requestedPose = combat.Substate is FirstSeveranceSubstate.Stack or FirstSeveranceSubstate.Spread
            || (combat.Substate == FirstSeveranceSubstate.PylonCheck && combat.RemainingPylons > 0)
            ? FirstSeveranceVisualCurves.Window(renderTick, combat.ResolveTick - lead, combat.ResolveTick) : 0;
        mechanicPose = MathHelper.Lerp(mechanicPose, requestedPose, .14f);
        var safe = FirstSeveranceSafeWindows.At(combat.Substate, combat.ActionIndex, combat.ActionStartedTick,
            state.EstimatedAuthorityTick, combat.CoreX, combat.CoreY);
        if (safe is { } cue && state.EstimatedAuthorityTick < cue.ResolveTick)
            mechanicPose = Math.Max(mechanicPose, Window(renderTick, cue.ResolveTick - 70d, cue.ResolveTick));
    }

    internal void Draw(SpriteBatch batch, FirstSeveranceCombatProjection combat, ulong tick)
    {
        EnsureAssets();
        bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
        Vector2 center = CoreCenter(combat);
        double renderTick = emissions.RenderTick;
        float time = (float)(renderTick % 216000) / 60f;
        float intro = combat.Substate == FirstSeveranceSubstate.SpawnIntro
            ? 1f - Math.Clamp((float)((double)combat.ResolveTick - tick)
                / FirstSeveranceEncounterPlan.Instance.Timing.SpawnIntroTicks, 0f, 1f) : 1f;
        float reveal = MathHelper.SmoothStep(0f, 1f, Math.Min(1f, intro * 1.25f));
        float breath = MathF.Sin(time * .72f);
        float recoil = emissions.Kick;
        float castPose = Math.Max(emissions.Pose, mechanicPose);
        if (combat.GridVolley is { } grid)
        {
            castPose = Math.Max(castPose, FirstSeveranceVisualCurves.CastPose(renderTick, grid.StartTick, grid.FireTick, grid.EndTick));
            recoil = Math.Max(recoil, Recoil(renderTick, grid.FireTick, grid.EndTick) * (grid.CoreBeams.Count > 0 ? 1 : .45f));
        }
        if (combat.Substate == FirstSeveranceSubstate.PhaseTransition) { castPose = 0; recoil = 0; }
        lastCast = castPose; lastKick = recoil; lastBreath = breath;
        float cast = castPose;

        if (!reduced)
        {
            Glow(batch, center, 1250f, new Color(5, 5, 7) * (0.7f * reveal));
            Glow(batch, center, 980f, new Color(16, 16, 20) * (0.4f * reveal));
            DrawAurora(batch, center, time, reveal);
        }
        float shellScale = combat.BossPhase == FirstSeveranceBossPhase.Sealed ? .80f
            : combat.Substate == FirstSeveranceSubstate.PhaseTransition && combat.BossPhase == FirstSeveranceBossPhase.Unbound
                ? MathHelper.Lerp(.80f, 1, EclosionUnfurl(FirstSeveranceStageVisuals.RuptureAge(combat, renderTick))) : 1;
        DrawSeal(batch, center, time, reveal, reduced, shellScale);
        if (!reduced && phaseImpactTicks > 0)
        {
            float impact = 1f - phaseImpactTicks / 24f;
            float spread = 1f - MathF.Pow(1f - impact, 3f);
            Ring(batch, center, 100 + spread * 780, Ice * (1f - impact) * 0.85f, 5f);
            Ring(batch, center, 85 + spread * 590, Gold * (1f - impact), 3f, time, 12);
            Glow(batch, center, 600 * (1f - impact), Additive(Ice, (1f - impact) * 0.8f));
        }

        bool hatching = combat.Substate == FirstSeveranceSubstate.PhaseTransition && combat.BossPhase == FirstSeveranceBossPhase.Unbound;
        float hatchAge = hatching ? FirstSeveranceStageVisuals.RuptureAge(combat, renderTick) : 1;
        float unseal = hatching ? Window(hatchAge, .06, .19) : combat.BossPhase != FirstSeveranceBossPhase.Sealed ? 1 : 0;
        bool remote = combat.BossPhase is FirstSeveranceBossPhase.Distant or FirstSeveranceBossPhase.Final;
        float retreat = remote ? combat.Substate == FirstSeveranceSubstate.PhaseTransition && combat.BossPhase == FirstSeveranceBossPhase.Distant
            ? Window(FirstSeveranceStageVisuals.RuptureAge(combat, renderTick), .08, .64) : 1 : 0;
        if (unseal > 0) DrawRig(batch, center - new Vector2(0, retreat * 190), reveal * unseal * (1 - retreat * .5f), breath, castPose, recoil,
            EclosionUnfurl(hatchAge), EclosionEmerge(hatchAge), EclosionPry(hatchAge), hideHands: hatching,
            depth: 1 - retreat * .76f);
        if (remote) DrawRemoteArms(batch, combat, center, renderTick, reveal, retreat, reduced);
        stages.DrawShell(batch, combat, center, renderTick, reveal, castPose, Accents, reduced);
        if (hatching && unseal > 0)
            DrawRig(batch, center, reveal * unseal, breath, 0, 0,
                EclosionUnfurl(hatchAge), EclosionEmerge(hatchAge), EclosionPry(hatchAge), handsOnly: true);
        Color castColor = combat.LanceVolley is { } attack
            ? FirstSeveranceEmissionVisuals.AttackColor(attack)
            : combat.Substate == FirstSeveranceSubstate.Stack ? FirstSeveranceAttackAccents.Cyan
            : FirstSeveranceAttackAccents.Magenta;
        if (combat.LanceVolley is { } current)
            Accents.CastSeal(batch, center, renderTick, current.StartTick, current.FireTick, castColor, reduced, 1.6f);
        if (combat.Substate is FirstSeveranceSubstate.Stack or FirstSeveranceSubstate.Spread
            || (combat.Substate == FirstSeveranceSubstate.PylonCheck && combat.RemainingPylons > 0))
        {
            double lead = combat.Substate == FirstSeveranceSubstate.PylonCheck ? 90 : 120;
            Accents.CastSeal(batch, center, renderTick, combat.ResolveTick - lead, combat.ResolveTick, castColor, reduced, 1.7f);
        }
        if (cast > 0f || recoil > 0f)
        {
            Ring(batch, center, 185f - castPose * 92f + recoil * 170f,
                castColor * Math.Max(cast, recoil), 2.4f, -castPose * 0.7f, 8);
            // Tall, slow-closing calipers make the cast silhouette readable even
            // when the actual attack originates at a player across the arena.
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 arm = center + new Vector2(side * (180 + 120 * castPose), -150 * castPose);
                Line(batch, arm + new Vector2(0, -100), arm + new Vector2(0, 95), castColor * cast, 3f);
                Line(batch, arm, arm + new Vector2(-side * 44, 0), Color.White * cast, 2.5f);
            }
            Glow(batch, center, 240f + recoil * 320f,
                Additive(Ice, (cast * 0.18f + recoil * 0.45f) * (reduced ? 0.4f : 1f)));
        }

        // A dark aperture makes the single damage target readable over any biome.
        Glow(batch, center, 183f, Color.Black * reveal);
        Color coreColor = Color.Lerp(Gold, Ice, exposure);
        if (exposure > 0.02f)
        {
            Glow(batch, center, 270f, Additive(Ice, exposure * 0.42f * reveal));
            Ring(batch, center, 76f + 4f * breath, coreColor * reveal, 2.5f, time * 0.18f, 8);
        }
        else
        {
            Ring(batch, center, 81f, Gold * (0.6f * reveal), 3f, -time * 0.08f, 8);
            // Closed diagonal braces versus four separated hitbox corners.
            Line(batch, center + new Vector2(-43, -43), center + new Vector2(43, 43), Gold * reveal, 4f);
            Line(batch, center + new Vector2(43, -43), center + new Vector2(-43, 43), Gold * reveal, 4f);
        }
        DrawCoreCorners(batch, center, coreColor * reveal, exposure);

        int shards = reduced ? 4 : 16;
        for (int index = 0; index < shards; index++)
        {
            float angle = index * 2.399963f + time * (index % 2 == 0 ? 0.04f : -0.03f);
            float radius = 300f + index % 5 * 39f + exposure * 38f;
            Vector2 point = center + Unit(angle) * radius + new Vector2(0, MathF.Sin(time + index) * 14f);
            Line(batch, point - Unit(angle + 0.4f) * 9f, point + Unit(angle + 0.4f) * 9f,
                Ice * (0.3f * reveal), index % 3 + 2f);
        }
        emissions.Draw(batch, tick, reduced);
        stages.DrawGrid(batch, combat, renderTick, tick, Accents, reduced);
        scoreVisuals.Draw(batch, combat, renderTick, tick, Accents, reduced);
    }

    private void DrawRig(SpriteBatch batch, Vector2 center, float reveal, float breath, float cast, float kick,
        float unfurl = 1, float emergence = 1, float pry = 1, bool handsOnly = false, bool hideHands = false, float collapse = 0, float depth = 1)
    {
        center += new Vector2(0, 85 * (1 - emergence));
        rigOrigin = center;
        rigScale = .66f * (.78f + .22f * emergence) * (1 - collapse * .995f) * depth;
        Color tint = Color.Lerp(new Color(188, 190, 201), Color.White, exposure * .6f) * reveal;
        float scale = 1.25f * (.92f + .08f * reveal);
        // Separate authored bones, with shared joint coordinates. Forearms and
        // hands inherit parent rotation, so wrists cannot detach during casting.
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 shoulder = center + new Vector2(side * MathHelper.Lerp(150, 210, unfurl), 12);
            Vector2 folded = new(side * MathHelper.Lerp(150, 455, pry), MathHelper.Lerp(-75, -235, pry));
            Vector2 expanded = new(side * (620 + cast * 38 - kick * 55), 110 - cast * 220 + kick * 70 + breath * 9);
            Vector2 wrist = center + Vector2.Lerp(folded, expanded, unfurl);
            var joint = Elbow(new(shoulder.X, shoulder.Y), new(wrist.X, wrist.Y), side);
            Vector2 elbowPoint = new(joint.X, joint.Y);
            float upper = MathF.Atan2(elbowPoint.Y - shoulder.Y, elbowPoint.X - shoulder.X) - MathHelper.PiOver2;
            float elbow = MathF.Atan2(wrist.Y - elbowPoint.Y, wrist.X - elbowPoint.X) - MathHelper.PiOver2;
            if (!handsOnly)
            {
                Bone(batch, new(470, 0, 236, 651), shoulder, new(85, 53), new Vector2(140, 370), upper, tint);
                Bone(batch, new(740, 0, 205, 651), elbowPoint, new(104, 52), new Vector2(115, 320), elbow, tint);
                Bone(batch, new(957, 787, 297, 318), shoulder, new(148, 155), new Vector2(76), upper, tint);
                Bone(batch, new(465, 655, 241, 599), center + new Vector2(side * 165, 40), new(120, 40),
                    new Vector2(130, MathHelper.Lerp(220, 580, emergence)),
                    -side * (.08f + unfurl * .12f + cast * .13f), tint);
            }
            if (!hideHands)
                Bone(batch, new(953, 0, 301, 655), wrist, new(186, 40), new Vector2(150, 300),
                    elbow + side * (cast * .42f + (1 - unfurl) * .38f), tint);
        }
        if (handsOnly) return;
        Bone(batch, new(745, 660, 189, 585), center + new Vector2(0, MathHelper.Lerp(90, 330, emergence)),
            new(95, 15), new Vector2(110, MathHelper.Lerp(160, 420, emergence)), breath * .009f, tint);
        Bone(batch, new(0, 0, 448, 657), center, new(220, 260), new Vector2(448, 657) * scale * (.84f + .16f * emergence), breath * .005f, tint);
        Bone(batch, new(0, 675, 445, 495), center + new Vector2(0, MathHelper.Lerp(-65, -355, emergence) - cast * 42 + kick * 18), new(222, 248),
            new Vector2(540, 400) * (.48f + emergence * .52f), breath * -.012f + (1 - emergence) * .22f, tint);
    }

    private void DrawRemoteArms(SpriteBatch batch, FirstSeveranceCombatProjection combat, Vector2 center,
        double tick, float reveal, float retreat, bool reduced)
    {
        float appear = Window(retreat, .4, 1);
        double age = tick - combat.ActionStartedTick;
        bool flooding = combat.Substate == FirstSeveranceSubstate.RemoteClaws;
        double floodAge = age % FirstSeveranceScoreGeometry.FloodInterval;
        int emittingSide = (int)(age / FirstSeveranceScoreGeometry.FloodInterval) % 2 == 0 ? -1 : 1;
        float grasp = flooding ? Window(floodAge, 0, 42) * (1 - Window(floodAge, FirstSeveranceScoreGeometry.FloodEndTick, 198)) : .18f;
        float recoil = flooding ? Window(floodAge, 48, 60) * (1 - Window(floodAge, 70, 132)) : 0;
        bool impaling = combat.Substate == FirstSeveranceSubstate.HalfField;
        int swordWave = age < FirstSeveranceImpalingSwords.WarningStart(1) ? 0 : 1;
        int swordFire = FirstSeveranceImpalingSwords.FireBase(swordWave);
        float flood = impaling ? Window(age, FirstSeveranceImpalingSwords.WarningStart(swordWave), swordFire)
            * (1 - Window(age, swordFire + 24, swordFire + 70)) : 0;
        float stabKick = impaling ? Window(age, swordFire, swordFire + 6)
            * (1 - Window(age, swordFire + 16, swordFire + 38)) : 0;
        if (impaling) grasp = .18f + flood * .65f;
        bool crushing = combat.Substate == FirstSeveranceSubstate.RemoteCrush;
        float closure = crushing ? FirstSeveranceScoreGeometry.CrushClosure(age) : 0;
        float brace = crushing ? Window(age, 0, 125) * (1 - Window(age, 190, 270)) : 0;
        if (crushing) grasp = brace * .7f + closure * .3f;
        Color color = combat.BossPhase == FirstSeveranceBossPhase.Final ? new(231, 117, 184) : new(156, 212, 226);
        rigOrigin = center; rigScale = 1;
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 wrist = Wrist(side);
            Vector2 elbow = wrist + new Vector2(side * (160 + grasp * 50), -250 - grasp * 70);
            Vector2 shoulder = elbow + new Vector2(side * 100, 290);
            float upper = (elbow - shoulder).ToRotation() - MathHelper.PiOver2;
            float fore = (wrist - elbow).ToRotation() - MathHelper.PiOver2;
            Color tint = Color.White * (appear * reveal);
            Bone(batch, new(470, 0, 236, 651), shoulder, new(85, 53), new(185, 350), upper, tint);
            Bone(batch, new(740, 0, 205, 651), elbow, new(104, 52), new(150, 340), fore, tint);
            Bone(batch, new(953, 0, 301, 655), wrist, new(186, 40),
                new(200 + brace * 400, 380 + brace * 80),
                (crushing ? side : -side) * MathHelper.PiOver2 - side * grasp * (crushing ? .07f : .22f), tint);
            if (crushing)
            {
                Accents.CastSeal(batch, wrist, age, 0, FirstSeveranceScoreGeometry.CrushRushTick, new Color(255, 91, 145), reduced, 1.2f);
                // Smooth stretched wake during the 0.2 s inward strike, not held animation frames.
                float rush = Window(age, 150, 154) * (1 - Window(age, 162, 176));
                Accents.Ribbon(batch, wrist, new Vector2(side, 0), 320 + closure * 220, 170,
                    new Color(217, 150, 255), rush * .6f);
            }
            Accents.Halo(batch, wrist, new Vector2(165 + grasp * 90, 210 + flood * 200), color, appear * (.24f + flood * .4f));
            if (!reduced)
            {
                Vector2 last = center - new Vector2(0, 190);
                for (int n = 1; n <= 40; n++)
                {
                    float t = n / 40f;
                    Vector2 next = Vector2.Lerp(center - new Vector2(0, 190), wrist, t)
                        + new Vector2(0, MathF.Sin(t * MathF.PI) * (85 + grasp * 35));
                    Line(batch, last, next, Additive(color, appear * .19f), 1.6f);
                    last = next;
                }
            }
        }
        if (combat.Substate == FirstSeveranceSubstate.RemoteClaws)
            foreach (var finger in FirstSeveranceScoreGeometry.Rays(combat.Substate, combat.ActionIndex, age, combat.CoreX, combat.CoreY))
            {
                Vector2 tip = new(finger.Ray.X, finger.Ray.Y);
                int side = finger.Ray.DirectionX < 0 ? 1 : -1;
                Vector2 root = Wrist(side), prior = root;
                float material = appear * Window(floodAge, 0, 10) * (1 - Window(floodAge, FirstSeveranceScoreGeometry.FloodEndTick, FirstSeveranceScoreGeometry.FloodFadeTick));
                for (int n = 1; n <= 20; n++)
                {
                    float t = n / 20f;
                    Vector2 next = Vector2.Lerp(root, tip, t)
                        + new Vector2(side * MathF.Sin(t * MathF.PI) * (75 + grasp * 45 - recoil * 35), 0);
                    Line(batch, prior, next, new Color(28, 28, 42) * material, 18 - t * 9);
                    Line(batch, prior, next, Additive(color, material * (.25f + finger.Charge * .65f)), 3);
                    prior = next;
                }
            }
        Vector2 Wrist(int side)
        {
            if (crushing) return center + new Vector2(side * MathHelper.Lerp(1110 - brace * 80, 330, closure),
                -40 * brace + 40 * closure);
            float flex = flooding && side == emittingSide ? grasp * 45 + recoil * 50 : 0;
            return center + new Vector2(side * (MathHelper.Lerp(250, 1110, appear) + flex),
                20 - flood * 90 - grasp * 30 + (impaling ? side * (flood * 145 - stabKick * 105) : 0));
        }
    }

    private void Bone(SpriteBatch batch, Rectangle source, Vector2 position, Vector2 pivot, Vector2 size, float rotation, Color tint)
    {
        Texture2D texture = body!.Value;
        // Keep the existing long-limbed form inside the more intimate field.
        position = rigOrigin + (position - rigOrigin) * rigScale;
        size *= rigScale;
        float factor = texture.Width / 1254f;
        Vector2 originalSize = new(source.Width, source.Height);
        source = new((int)(source.X * factor), (int)(source.Y * factor), (int)(source.Width * factor), (int)(source.Height * factor));
        batch.Draw(texture, position - Main.screenPosition, source, tint, rotation, pivot * factor,
            size / originalSize / factor, SpriteEffects.None, 0);
    }

    private void DrawSeal(SpriteBatch batch, Vector2 center, float time, float reveal, bool reduced, float size = 1)
    {
        float radius = (460f + exposure * 38f) * (0.7f + 0.3f * reveal) * size;
        float spin = time * 0.035f;
        Ring(batch, center, radius, new Color(25, 24, 27) * reveal, 6f, spin, 12);
        Ring(batch, center, radius - 9f, Ice * (0.18f * reveal), 1.2f, spin, 12);
        Ring(batch, center, radius + 7f, Gold * (0.24f * reveal), 1.2f, spin, 12);
        if (!reduced)
        {
            Ellipse(batch, center, new Vector2(radius + 80f, radius * 0.38f), -0.55f + spin,
                Ice * (0.2f * reveal), 1.7f);
            Ellipse(batch, center, new Vector2(radius + 60f, radius * 0.38f), 0.55f - spin,
                Gold * (0.18f * reveal), 1.7f);
        }
        for (int index = 0; index < 48; index++)
        {
            float angle = index * MathHelper.TwoPi / 48f + spin;
            Vector2 direction = Unit(angle);
            float tickLength = index % 4 == 0 ? 22f : 7f;
            Line(batch, center + direction * (radius - tickLength), center + direction * radius,
                Ice * (0.34f * reveal), index % 4 == 0 ? 3f : 1.5f);
        }
        for (int index = 0; index < 6; index++)
        {
            float angle = index * MathHelper.TwoPi / 6f - MathHelper.PiOver2 + spin;
            Vector2 direction = Unit(angle);
            Vector2 tangent = new(-direction.Y, direction.X);
            Vector2 anchor = center + direction * (radius + 18f);
            Line(batch, anchor - direction * 30, anchor + direction * 34, new Color(30, 44, 51) * reveal, 30f);
            Line(batch, anchor - direction * 24, anchor + direction * 26, Gold * (0.65f * reveal), 6f);
            Line(batch, anchor - tangent * 22, anchor + tangent * 22, Ice * (0.5f * reveal), 3f);
            if (exposure < 0.9f)
                Line(batch, center + direction * 104, anchor - direction * 34,
                    Ice * (0.15f * reveal * (1f - exposure)), 1.7f);
        }
    }

    private static void DrawAurora(SpriteBatch batch, Vector2 center, float time, float opacity)
    {
        for (int ribbon = 0; ribbon < 4; ribbon++)
        {
            Vector2 previous = default;
            for (int index = 0; index <= 48; index++)
            {
                float x = (index / 48f - 0.5f) * 1800f;
                float y = MathF.Sin(x * 0.004f + time * 0.15f + ribbon * 0.8f) * 110f - 240f + ribbon * 78f;
                Vector2 next = center + new Vector2(x, y);
                if (index > 0)
                    Line(batch, previous, next, new Color(125, 120, 115) * (0.010f * opacity), 26f);
                previous = next;
            }
        }
    }

    internal void DrawEnding(SpriteBatch batch)
    {
        if (!IsEnding || glow is null)
            return;
        if (EndingVictory)
        {
            float age = EndingAge;
            bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
            float fold = Window(age, .03, .40), pinch = Window(age, .15, .79);
            float dissolve = 1 - Window(age, .76, .80);
            bool remote = lastCombat?.BossPhase is FirstSeveranceBossPhase.Distant or FirstSeveranceBossPhase.Final;
            DrawRig(batch, lastCenter - new Vector2(0, remote ? 190 * (1 - pinch) : 0), dissolve, lastBreath * (1 - fold), lastCast * (1 - fold),
                lastKick * (1 - fold), 1 - fold * .88f, collapse: pinch, depth: remote ? .24f : 1);
            if (remote && lastCombat is { } remnant && age < .8f)
                DrawRemoteArms(batch, remnant, lastCenter, remnant.ResolveTick, dissolve, 1 - pinch, reduced);
            Color ion = new(210, 164, 255);
            float tension = Window(age, .06, .45) * (1 - Window(age, .79, .805));
            Accents.Halo(batch, lastCenter, new Vector2(620 - pinch * 610), ion, tension * (reduced ? .28f : .95f));
            // Filaments accelerate inward; one optical snap erases the point.
            for (int n = 0; n < (reduced ? 16 : 72); n++)
            {
                float angle = n * 2.399963f + pinch * 1.1f;
                Vector2 direction = Unit(angle);
                float radius = (240 + n % 11 * 87) * (1 - pinch);
                float tail = (25 + n % 7 * 18) * (1 - pinch);
                Vector2 point = lastCenter + direction * radius;
                Line(batch, point, lastCenter + direction * (radius + tail), Additive(ion, tension * .65f), 1.5f + n % 3);
                if (!reduced && n % 3 == 0) Accents.Halo(batch, point, new Vector2(11), Ice, tension * .75f);
            }
            float sever = Window(age, .79, .802) * (1 - Window(age, .802, .87));
            Accents.Ribbon(batch, lastCenter - new Vector2(1100, 0), Vector2.UnitX, 2200,
                3 + sever * 17, ion, sever * (reduced ? .30f : 1));
            Accents.Halo(batch, lastCenter, new Vector2(320 * sever), Color.White, sever * (reduced ? .25f : .95f));
            Line(batch, lastCenter - new Vector2(740 * sever, 0), lastCenter + new Vector2(740 * sever, 0), Color.White * sever, 3);
            float inscription = Window(age, .85, .90) * (1 - Window(age, .96, 1));
            Utils.DrawBorderString(batch, Language.GetTextValue("Mods.Convergence.UI.FirstSeverance.VictorySeal"),
                lastCenter - Main.screenPosition + new Vector2(0, 105), Ice * inscription, .82f, .5f);
            return;
        }
        float progress = EndingAge;
        float closure = Window(progress, .04, .55);
        float opacity = 1 - Window(progress, .18, .60);
        bool distant = lastCombat?.BossPhase is FirstSeveranceBossPhase.Distant or FirstSeveranceBossPhase.Final;
        DrawRig(batch, lastCenter - new Vector2(0, distant ? 190 : 0), opacity,
            lastBreath * (1 - closure), lastCast * (1 - closure), lastKick * (1 - closure),
            1 - closure * .4f, depth: distant ? .24f : 1);
        // The surviving entity recedes behind two closing geometric shutters.
        // All of this is a cached image pose, never an active or damageable actor.
        Color extinguish = new(168, 67, 94);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 point = lastCenter + new Vector2(side * (620 * (1 - closure) + 10), 0);
            Line(batch, point - new Vector2(0, 620), point + new Vector2(0, 620),
                extinguish * opacity * .75f, 3 + closure * 22);
            Glow(batch, point, 130 * (1 - closure), Additive(extinguish, opacity * .4f));
        }
    }

    internal static void DrawCoreCorners(SpriteBatch batch, Vector2 center, Color color, float open)
    {
        float radius = 72f; // Exact 144x144 NPC hitbox, not the 820px casing.
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                Vector2 corner = center + new Vector2(x, y) * radius;
                Line(batch, corner, corner - new Vector2(x * (18 + open * 8), 0), color, 3f);
                Line(batch, corner, corner - new Vector2(0, y * (18 + open * 8)), color, 3f);
            }
    }

    private static void DrawSlice(SpriteBatch batch, Texture2D texture, Rectangle source, Vector2 center,
        Vector2 origin, float scale, float rotation, Color color, Vector2 offset)
        => batch.Draw(texture, center + offset - Main.screenPosition, source, color, rotation,
            origin - new Vector2(source.X, source.Y), scale, SpriteEffects.None, 0f);

    internal static void Line(SpriteBatch batch, Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 delta = end - start;
        batch.Draw(TextureAssets.MagicPixel.Value, start - Main.screenPosition,
            new Rectangle(0, 0, 1, 1), color, MathF.Atan2(delta.Y, delta.X), new Vector2(0, 0.5f),
            new Vector2(delta.Length(), width), SpriteEffects.None, 0f);
    }

    internal static void Ring(SpriteBatch batch, Vector2 center, float radius, Color color,
        float width = 2f, float rotation = 0f, int gaps = 0)
    {
        const int count = 96;
        for (int index = 0; index < count; index++)
        {
            if (gaps > 0 && index % (count / gaps) < 2)
                continue;
            float a = rotation + index * MathHelper.TwoPi / count;
            float b = rotation + (index + 1) * MathHelper.TwoPi / count;
            Line(batch, center + Unit(a) * radius, center + Unit(b) * radius, color, width);
        }
    }

    private static void Ellipse(SpriteBatch batch, Vector2 center, Vector2 radius, float rotation, Color color, float width)
    {
        Vector2 previous = center + Rotate(new Vector2(radius.X, 0), rotation);
        for (int index = 1; index <= 96; index++)
        {
            Vector2 point = center + Rotate(Unit(index * MathHelper.TwoPi / 96f) * radius, rotation);
            Line(batch, previous, point, color, width);
            previous = point;
        }
    }

    private static void DashedLine(SpriteBatch batch, Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        Vector2 direction = delta / length;
        for (float at = 0; at < length; at += 44f)
            Line(batch, start + direction * at, start + direction * Math.Min(length, at + 26f), color, width);
    }

    private void Glow(SpriteBatch batch, Vector2 center, float diameter, Color color)
        => batch.Draw(glow!, center - Main.screenPosition, null, color, 0f,
            new Vector2(glow!.Width * 0.5f), diameter / glow.Width, SpriteEffects.None, 0f);

    private static Vector2 Unit(float radians) => new(MathF.Cos(radians), MathF.Sin(radians));
    private static Vector2 Rotate(Vector2 point, float radians)
        => new(point.X * MathF.Cos(radians) - point.Y * MathF.Sin(radians),
            point.X * MathF.Sin(radians) + point.Y * MathF.Cos(radians));
    private static Color Additive(Color color, float opacity) => new Color(color.R, color.G, color.B, 0) * opacity;

    private void EnsureAssets()
    {
        body ??= ModContent.Request<Texture2D>(BodyPath, AssetRequestMode.ImmediateLoad);
        if (glow is not null)
            return;
        // Render-thread-only procedural falloff. Not a borrowed texture or effect.
        const int size = 128;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = new Vector2(x - 63.5f, y - 63.5f).Length() / 64f;
                float alpha = MathF.Pow(Math.Max(0f, 1f - distance), 2f);
                pixels[y * size + x] = Color.White * alpha;
            }
        glow = new Texture2D(Main.instance.GraphicsDevice, size, size);
        glow.SetData(pixels);

    }

    internal void Reset(bool unload = false)
    {
        fight = FightId.None;
        emissions.Clear(unload);
        stages.Reset(unload);
        if (unload) scoreVisuals.Unload();
        previousPhase = FirstSeveranceSubstate.None;
        ending.Clear();
        endingParticipant = false;
        lastCombat = null;
        phaseImpactTicks = 0;
        exposure = 0;
        mechanicPose = 0;
        if (unload)
        {
            Texture2D? oldGlow = glow;
            glow = null;
            body = null; // ReLogic owns the requested body texture.
            if (oldGlow is not null)
                Main.QueueMainThreadAction(oldGlow.Dispose);
        }
    }
}
