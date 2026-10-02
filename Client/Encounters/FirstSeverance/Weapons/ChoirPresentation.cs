#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NVector2 = System.Numerics.Vector2;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// Choir of the Unmade's look, as one Terraria-free function the offline preview links: given one owner's concert
// (ChoirDrawState, filled by ChoirVisuals in game or by the preview's simulation) it records the whole concert into
// the shared Doll weapon layer:
//  - Back stratum (behind players): the gallery row, the organ's rising pipes and its case (ChoirOrgan, k = 1).
//  - Front stratum: rows 0-1 and the cloud of choristers (Chorister0/1/2, k = 2), the held baton.
//  - Light: count-in rings and sound arcs at the mouths, the organ's reveal ring, the inhale's forecast axis and
//    iris, the chorus beam (DollChoirEnergy Hymn), its residue, note glyphs and trails, sparks.
// One texel is one dot (2 world px) always; the organ and choristers sit on the design anchors of
// ChoirConcertRules (the art is fitted to them). Bodies stay opaque; another player's damaging light draws at
// DollWeaponCanvas.PeerLightAlpha. Reduced Effects (canvas.Reduced): two strands, chord-change wavefronts only, no
// motes, half-length trails, shorter residue; bodies, forecasts, live beam and counts are unchanged.
internal static class ChoirPresentation
{
    internal const int OrganRail = ChoirConcertRules.OrganRailRow, OrganWidth = 95, OrganHeight = 96;
    internal const int CellWidth = 22, CellHeight = 38;
    internal const int CrumbleTicks = 18, ResidueTicks = 24, SummonFadeTicks = 14;
    internal static readonly NVector2 StandTipTexel = new(10.5f, 38f);
    internal static readonly NVector2 BatonGrip = new(10.1f, 17.49f), BatonTip = new(26.5f, .5f);

    // ChoirOrgan.png pipe columns, left to right (inclusive x range, top row of the pipe); rank = |column - 5|.
    internal static readonly int[] PipeX0 = { 5, 12, 19, 27, 35, 43, 52, 60, 68, 75, 82 };
    internal static readonly int[] PipeX1 = { 12, 19, 26, 34, 42, 51, 59, 67, 75, 82, 89 };
    internal static readonly int[] PipeTop = { 31, 24, 18, 12, 6, 0, 6, 12, 18, 24, 31 };

    // The closed mouth that the k = 2 export lost on frame 0 of Chorister0 and Chorister2: a one-dot dark line
    // (Iron, as Chorister1 draws its own) of two texels at the frame-0 mouth anchor. Null where the art has it.
    internal static readonly Rectangle?[] ClosedMouth = { new Rectangle(14, 15, 2, 1), null, new Rectangle(13, 15, 2, 1) };

    // Sung note glyphs in dots from the note head (y down): a quarter, an eighth and a beamed pair (never staves).
    // Each head is a slanted 4 x 3 oval (pearl-violet, a white glint, a lilac underside); stems, flags and beams are
    // lilac. (x, y) pairs.
    private static readonly sbyte[] Head = { 1, -1, 2, -1, 3, -1, 0, 0, 1, 0, 2, 0, 3, 0, 0, 1, 1, 1, 2, 1 };
    private static readonly sbyte[][] GlyphHeads = { new sbyte[] { 0, 0 }, new sbyte[] { 0, 0 }, new sbyte[] { 0, 0, 7, -1 } };
    private static readonly sbyte[][] GlyphStems =
    {
        new sbyte[] { 3, -2, 3, -3, 3, -4, 3, -5, 3, -6, 3, -7, 3, -8 },
        new sbyte[] { 3, -2, 3, -3, 3, -4, 3, -5, 3, -6, 3, -7, 3, -8, 4, -8, 4, -7, 5, -6, 5, -5, 4, -4 },
        new sbyte[] { 3, -2, 3, -3, 3, -4, 3, -5, 3, -6, 3, -7, 3, -8, 10, -3, 10, -4, 10, -5, 10, -6, 10, -7, 10, -8, 10, -9,
            4, -8, 5, -8, 6, -9, 7, -9, 8, -9, 9, -9, 4, -9, 5, -9, 9, -8 },
    };

    internal static void Emit(DollWeaponCanvas canvas, ChoirDrawState s, in ChoirSprites sprites, IDollEnergyMaterial hymn)
    {
        bool reduced = canvas.Reduced;
        float lightAlpha = s.Peer ? DollWeaponCanvas.PeerLightAlpha : 1f;
        Organ(canvas, s, sprites, reduced);
        Voices(canvas, s, sprites, reduced);
        Chorus(canvas, s, hymn, reduced, lightAlpha);
        Notes(canvas, s, hymn, reduced, lightAlpha);
        Baton(canvas, s, sprites, hymn, reduced);
    }

    // ---- Organ (Back) -------------------------------------------------------------------------------------

    private static void Organ(DollWeaponCanvas canvas, ChoirDrawState s, in ChoirSprites sprites, bool reduced)
    {
        bool cancelled = s.CancelAge >= 0;
        float clock = cancelled ? s.CancelClock : s.Clock;
        if (!ChoirConcertRules.Running(clock)) return;
        float open = ChoirConcertRules.CaseOpen(clock);
        if (open <= 0) return;
        Vector2 topLeft = s.Stage + V(ChoirConcertRules.OrganTopLeft);
        float crumble = cancelled ? Math.Clamp(s.CancelAge / CrumbleTicks, 0, 1) : 0;
        bool closing = !cancelled && clock >= ChoirConcertRules.Release;
        // Opening: the porcelain case assembles out of crumbs, pale for a moment; a success fades it out in
        // dithered steps; a cancel crumbles it.
        DollSpriteFx caseFx = cancelled
            ? new DollSpriteFx { Dissolve = .1f + .9f * crumble, Seed = 311 + s.Seed }
            : closing ? new DollSpriteFx { Fade = 1 - open }
            : new DollSpriteFx { Dissolve = (1 - open) * .97f, Flash = (1 - open) * .7f, Seed = 311 + s.Seed };
        int raised = ChoirConcertRules.RaisedRanks(s.VoiceTotal);
        if (sprites.Organ is { } organ)
        {
            for (int column = 0; column < ChoirConcertRules.PipeColumns; column++)
            {
                int rank = ChoirConcertRules.PipeRank(column);
                if (rank >= raised) continue;
                float rise = ChoirConcertRules.PipeRise(clock, rank) * open;
                int length = OrganRail - PipeTop[column], shown = (int)MathF.Round(rise * length);
                if (shown <= 0) continue;
                int hidden = length - shown;
                var source = new Rectangle(PipeX0[column], 0, PipeX1[column] - PipeX0[column] + 1, OrganRail);
                var fx = new DollSpriteFx
                {
                    Hide = Math.Max(0, (hidden - .5f) / OrganRail), HideFromBottom = true,
                    Dissolve = cancelled ? caseFx.Dissolve : 0, Seed = 97 + column + s.Seed,
                };
                canvas.Sprite(new DollSprite(organ, source, Vector2.Zero), topLeft + new Vector2(PipeX0[column] * 2, hidden * 2), 0,
                    DollFlip.None, DollStratum.Back, -2, in fx);
            }
            canvas.Sprite(new DollSprite(organ, new Rectangle(0, OrganRail, OrganWidth, OrganHeight - OrganRail), Vector2.Zero),
                topLeft + new Vector2(0, OrganRail * 2), 0, DollFlip.None, DollStratum.Back, -1, in caseFx);
        }
        if (cancelled)
        {
            // The failed organ sheds porcelain and brass.
            canvas.Burst(s.Stage + new Vector2(0, -40), 501 + s.Seed, 16, s.CancelAge, 30, 3.2f, .16f, DollShardKind.Porcelain);
            canvas.Burst(s.Stage + new Vector2(0, -90), 502 + s.Seed, 10, s.CancelAge, 30, 2.6f, .16f, DollShardKind.Brass);
            return;
        }
        // The reveal: a pearl-violet ring opens from the mouth as the case assembles, with pearl motes.
        float reveal = s.Clock - ChoirConcertRules.Gather;
        if (reveal >= 0 && reveal < ChoirConcertRules.CaseOpenTicks + 8)
        {
            float t = reveal / ChoirConcertRules.CaseOpenTicks;
            float radius = ChoirConcertRules.OrganMouthRadius + 112 * ChoirConcertRules.Arrive(t);
            float fade = 1 - Math.Clamp((reveal - ChoirConcertRules.CaseOpenTicks + 4) / 12, 0, 1);
            canvas.Ring(s.Stage, radius, DollTone.PearlViolet, 1, fade);
            canvas.Ring(s.Stage, radius * .72f, DollTone.Lilac, 1, fade * .8f);
            if (!reduced) canvas.Burst(s.Stage, 401 + s.Seed, 14, reveal, 22, 3.6f, -.02f, DollShardKind.Pearl);
        }
        // A brass glint as each raised rank seats.
        for (int rank = 0; rank < raised; rank++)
        {
            float since = s.Clock - ChoirConcertRules.PipeSeat(rank);
            if (since < 0 || since >= 6) continue;
            for (int column = 0; column < ChoirConcertRules.PipeColumns; column++)
            {
                if (ChoirConcertRules.PipeRank(column) != rank) continue;
                Vector2 cap = topLeft + new Vector2((PipeX0[column] + PipeX1[column] + 1), PipeTop[column] * 2 + 3);
                canvas.Dot(cap, since < 2 ? DollTone.White : DollTone.BrassLight, 1);
                canvas.Dot(cap + new Vector2(-2, 2), DollTone.BrassLight, 1, 1 - since / 6);
            }
        }
    }

    // ---- Choristers ---------------------------------------------------------------------------------------

    private static void Voices(DollWeaponCanvas canvas, ChoirDrawState s, in ChoirSprites sprites, bool reduced)
    {
        float clock = s.Clock;
        bool running = ChoirConcertRules.Running(clock);
        float seating = running ? ChoirConcertRules.Seating(clock) : 0;
        float nod = running ? ChoirConcertRules.Nod(clock) : 0;
        for (int i = 0; i < s.VoiceCount; i++)
        {
            ref ChoirVoiceDraw v = ref s.Voices[i];
            int frame = ChoirConcertRules.SingFrame(clock, v.Ordinal);
            int facing = v.Facing < 0 ? -1 : 1;
            DollFlip flip = facing < 0 ? DollFlip.Horizontal : DollFlip.None;
            bool gallery = seating >= .5f && ChoirConcertRules.ChorusRow(v.Ordinal) == 2;
            DollStratum stratum = gallery ? DollStratum.Back : DollStratum.Front;
            sbyte depth = gallery ? (sbyte)-3 : ChoirConcertRules.ChorusRow(v.Ordinal) == 1 && seating >= .5f ? (sbyte)1 : (sbyte)2;
            Vector2 centre = v.Center + Parting(s, v.Center, seating);
            Vector2 pivot = centre + V(ChoirConcertRules.StandTip) - new Vector2(0, nod);
            float summon = Math.Clamp(v.Life / SummonFadeTicks, 0, 1);
            var fx = new DollSpriteFx { Fade = 1 - summon, Flash = .6f * (1 - Math.Clamp(v.Life / 10, 0, 1)) };
            int variant = Math.Clamp(v.Variant, 0, 2);
            if (sprites.Chorister(variant) is { } texture)
            {
                var source = new Rectangle(frame * CellWidth, 0, CellWidth, CellHeight);
                canvas.Sprite(new DollSprite(texture, source, V(StandTipTexel)), pivot, 0, flip, stratum, depth, in fx);
                if (frame == 0 && ClosedMouth[variant] is { } line)
                    canvas.Sprite(new DollSprite(texture, line, V(StandTipTexel) - new Vector2(line.X, line.Y)), pivot, 0, flip, stratum, depth,
                        fx with { Silhouette = DollTone.Iron, Flash = 0 });
            }
            Vector2 mouth = centre + new Vector2(ChoirConcertRules.Mouth.X * facing, ChoirConcertRules.Mouth.Y - nod);
            if (summon < 1) canvas.Burst(v.Center, 601 + i + s.Seed, 8, v.Life, 18, 2.2f, 0, DollShardKind.Pearl);
            if (!running) continue;
            // Count-in: a one-dot pearl ring blinks at every mouth on each tap.
            float tap = Math.Min(Math.Abs(clock - ChoirConcertRules.TapA - 2), Math.Abs(clock - ChoirConcertRules.TapB - 2));
            if (clock < ChoirConcertRules.Verse && tap < 3) canvas.Ring(mouth, 4, DollTone.PearlViolet, 1, 1 - tap / 3);
            // Sound arcs leave the mouth after each sung note, and on every beat of the chorus.
            float sung = float.MaxValue;
            for (int beat = 0; beat < ChoirConcertRules.VerseBeats; beat++)
            {
                float since = clock - ChoirConcertRules.NoteTick(beat, v.Ordinal);
                if (since >= 0) sung = Math.Min(sung, since);
            }
            if (ChoirConcertRules.Live(clock)) sung = (clock - ChoirConcertRules.Fire) % ChoirConcertRules.Beat;
            if (sung < 10 && clock < ChoirConcertRules.Release && (clock < ChoirConcertRules.Gather || ChoirConcertRules.Live(clock)))
            {
                float heading = facing < 0 ? MathF.PI : 0, alpha = 1 - sung / 10;
                canvas.Arc(mouth, 6 + 1.6f * sung, heading - .55f, heading + .55f, DollTone.PearlViolet, 1, alpha);
                canvas.Arc(mouth, 11 + 1.6f * sung, heading - .45f, heading + .45f, DollTone.Lilac, 1, alpha * .8f);
            }
            // Pearl motes trail the dolls as they glide to their seats (none under Reduced Effects).
            if (!reduced && clock >= ChoirConcertRules.Gather && clock < ChoirConcertRules.Pipes && v.Velocity.LengthSquared() > 9)
                for (int k = 1; k <= 3; k++)
                    canvas.Dot(v.Center - v.Velocity * (1.5f * k), k == 1 ? DollTone.PearlViolet : DollTone.Pearl, 1, 1 - k * .25f);
        }
    }

    // ---- The inhale, the chorus beam and its residue ------------------------------------------------------

    private static void Chorus(DollWeaponCanvas canvas, ChoirDrawState s, IDollEnergyMaterial hymn, bool reduced, float lightAlpha)
    {
        if (s.Beam)
        {
            Vector2 axis = new(MathF.Cos(s.BeamAngle), MathF.Sin(s.BeamAngle));
            float live = s.BeamAge - ChoirConcertRules.WarnTicks, open = 1 - s.BeamClose;
            if (live < 0)
            {
                // The inhale: a harmless locked axis brightening toward the chorus, pulses running into the mouth,
                // and the mouth's iris darkening as the motes spiral in.
                float progress = Math.Clamp(s.BeamAge / ChoirConcertRules.WarnTicks, 0, 1);
                canvas.Forecast(s.BeamOrigin + axis * ChoirConcertRules.BeamLength, s.BeamOrigin, 1, (.55f + .45f * progress) * open);
                float radius = ChoirConcertRules.OrganMouthRadius + 14;
                canvas.EnergyQuad(hymn, ChoirHymnMaterial.Iris, s.BeamOrigin - new Vector2(radius, 0), s.BeamOrigin + new Vector2(radius, 0),
                    radius * 2, new Vector4(progress * open, s.Peer ? DollWeaponCanvas.PeerVoidAlpha : 1f,
                        ChoirConcertRules.OrganMouthRadius / radius, s.Seed * .37f), 0);
            }
            else
            {
                float half = ChoirConcertRules.HalfWidth(live) * open, end = ChoirConcertRules.End(live);
                if (end > 1 && half > .2f)
                {
                    int chord = ChoirConcertRules.ChorusChord(live), strands = ChoirConcertRules.ChordVoices(s.ChordVoices);
                    // The quad is the collision body (2 x half wide, mouth to end); the shader narrows its first
                    // BeamStart px to the organ mouth's throat, so light never leaves the body.
                    canvas.EnergyQuad(hymn, ChoirHymnMaterial.Hymn, s.BeamOrigin, s.BeamOrigin + axis * end, 2 * half,
                        new Vector4(live, lightAlpha, chord * 8 + strands, s.Seed * .61f), 1);
                }
            }
        }
        // Residue: the last axis cools from lilac through violet to plum and thins out (half as long under Reduced
        // Effects); it starts past the throat, inside the old collision body.
        float residueTicks = reduced ? ResidueTicks / 2f : ResidueTicks;
        if (s.ResidueAge >= 0 && s.ResidueAge < residueTicks && s.ResidueLength > 1)
        {
            float a = s.ResidueAge / residueTicks;
            Vector2 axis = new(MathF.Cos(s.ResidueAngle), MathF.Sin(s.ResidueAngle));
            DollTone tone = a < .25f ? DollTone.Lilac : a < .5f ? DollTone.Violet : a < .75f ? DollTone.PlumLight : DollTone.Plum;
            canvas.Line(s.ResidueOrigin + axis * ChoirConcertRules.BeamStart, s.ResidueOrigin + axis * s.ResidueLength, tone,
                a < .34f ? 3 : a < .67f ? 2 : 1, lightAlpha * (1 - a * a));
        }
    }

    // ---- Notes ----------------------------------------------------------------------------------------------

    private static void Notes(DollWeaponCanvas canvas, ChoirDrawState s, IDollEnergyMaterial hymn, bool reduced, float lightAlpha)
    {
        int trails = 0;
        Span<Vector2> spine = stackalloc Vector2[ChoirDrawState.TrailPoints + 1];
        for (int i = 0; i < s.NoteCount; i++)
        {
            ref ChoirNoteDraw note = ref s.Notes[i];
            float alpha = lightAlpha * (1 - note.Fade);
            if (alpha <= .02f) continue;
            // A short comet tail, newest points only (four under Reduced Effects), for the most recent notes.
            int points = Math.Min(note.TrailCount, reduced ? ChoirDrawState.TrailPoints / 2 + 1 : ChoirDrawState.TrailPoints);
            if (note.Trail is { } trail && points >= 2 && trails < ChoirDrawState.MaxTrails)
            {
                int n = 0;
                for (int k = points - 1; k >= 0; k--)
                    spine[n++] = trail[(note.TrailHead - 1 - k + trail.Length * 2) % trail.Length];
                if (Vector2.DistanceSquared(spine[n - 1], note.Center) > 1) spine[n++] = note.Center;
                if (canvas.EnergyStrip(hymn, ChoirHymnMaterial.Trail, spine[..n], 4, new Vector4(alpha, .72f, note.Pitch * .73f, 0), 2)) trails++;
            }
            Glyph(canvas, note.Center, note.Pitch % GlyphHeads.Length, alpha, note.Age);
        }
        // Sparks where a note struck.
        for (int i = 0; i < s.SparkCount; i++)
        {
            ref ChoirSpark spark = ref s.Sparks[i];
            canvas.Burst(spark.At, spark.Seed, 7, spark.Age, 16, 2.8f, .04f, DollShardKind.Spark);
            if (spark.Age < 4) canvas.Ring(spark.At, 6 + spark.Age * 3, DollTone.PearlViolet, 1, lightAlpha * (1 - spark.Age / 4));
        }
    }

    private static void Glyph(DollWeaponCanvas canvas, Vector2 head, int kind, float alpha, float age)
    {
        bool twinkle = ((int)(age / 5) & 1) == 0;
        head -= new Vector2(4, 0);
        sbyte[] heads = GlyphHeads[kind], stems = GlyphStems[kind];
        for (int c = 0; c + 1 < stems.Length; c += 2)
            canvas.Dot(head + new Vector2(stems[c] * 2, stems[c + 1] * 2), DollTone.Lilac, 1, alpha);
        for (int h = 0; h + 1 < heads.Length; h += 2)
            for (int c = 0; c + 1 < Head.Length; c += 2)
            {
                int x = Head[c], y = Head[c + 1];
                DollTone tone = y > 0 ? DollTone.Lilac : x == 1 && y == -1 ? twinkle ? DollTone.White : DollTone.Bone : DollTone.PearlViolet;
                canvas.Dot(head + new Vector2((heads[h] + x) * 2, (heads[h + 1] + y) * 2), tone, 1, alpha);
            }
    }

    // ---- The baton --------------------------------------------------------------------------------------

    private static void Baton(DollWeaponCanvas canvas, ChoirDrawState s, in ChoirSprites sprites, IDollEnergyMaterial hymn, bool reduced)
    {
        if (!s.Baton || sprites.Baton is not { } baton) return;
        float art = MathF.Atan2(BatonTip.Y - BatonGrip.Y, BatonTip.X - BatonGrip.X);
        bool left = s.BatonFacing < 0;
        float local = left ? MathF.PI - s.BatonAngle : s.BatonAngle;
        float rotation = left ? -(local - art) : local - art;
        canvas.Sprite(new DollSprite(baton, new Rectangle(0, 0, baton.Width, baton.Height), V(BatonGrip)), s.BatonGrip, rotation,
            left ? DollFlip.Horizontal : DollFlip.None, DollStratum.Front, 3);
        // A short pearl trail follows the tip while the baton sweeps.
        int points = Math.Min(s.BatonTrailCount, reduced ? ChoirDrawState.BatonTrailPoints / 2 : ChoirDrawState.BatonTrailPoints);
        if (points >= 2 && s.BatonSwing > .05f)
            canvas.EnergyStrip(hymn, ChoirHymnMaterial.Trail, s.BatonTrail.AsSpan(s.BatonTrailCount - points, points), 4,
                new Vector4(Math.Clamp(s.BatonSwing, 0, 1), .8f, 7.1f, 0), 3);
    }

    // The baton tip in world space for a grip, a tip direction and a facing (the art is fitted, not scaled).
    internal static Vector2 BatonTipWorld(Vector2 grip, float angle)
        => grip + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (Vector2.Distance(V(BatonTip), V(BatonGrip)) * DollSpritePlacement.WorldPerTexel);

    // The choir parts for its own beam (draw only; the voices have no hitbox): a seated voice inside the beam's
    // corridor near the mouth steps out across the axis as the inhale locks it, and back when the beam closes.
    internal static Vector2 Parting(ChoirDrawState s, Vector2 centre, float seating)
    {
        if (!s.Beam || seating < .5f) return Vector2.Zero;
        Vector2 axis = new(MathF.Cos(s.BeamAngle), MathF.Sin(s.BeamAngle)), normal = new(-axis.Y, axis.X), d = centre - s.BeamOrigin;
        float along = Vector2.Dot(d, axis), across = Vector2.Dot(d, normal);
        const float clearance = ChoirConcertRules.BeamHalfWidth + 40;
        if (along <= 0 || MathF.Abs(across) >= clearance) return Vector2.Zero;
        float ramp = ChoirConcertRules.Smooth(s.BeamAge / 12f) * (1 - s.BeamClose) * (1 - ChoirConcertRules.Smooth((along - 220) / 100));
        Vector2 push = normal * ((across >= 0 ? clearance : -clearance) - across) * ramp;
        // Never step down into the player: the front row's lowest seat is S + 110.
        float limit = s.BeamOrigin.Y + 118 - centre.Y;
        return new Vector2(push.X, MathF.Min(push.Y, MathF.Max(0, limit)));
    }

    private static Vector2 V(NVector2 v) => new(v.X, v.Y);
}

// The conductor's baton, as tip angles for a right-facing player (y down: -90 deg is straight up). The arm points
// along the baton. Pure, so the arm pose, the drawn baton and its tip trail share one curve.
internal static class ChoirConducting
{
    // The downbeat on each summon (use progress 0..1): from ready, up behind the head, one continuous sweep
    // forward-down, a 6 deg follow-through, back to ready. Periodic, so auto-reuse flows without a seam and the
    // baton never stops dead at either end.
    private static readonly float[] FlickU = { 0f, .30f, .58f, .74f, 1f };
    private static readonly float[] FlickDeg = { -40f, -128f, 22f, 28f, -40f };

    internal static float Flick(float u)
    {
        u -= MathF.Floor(u);
        int i = 0;
        while (i < FlickU.Length - 2 && u > FlickU[i + 1]) i++;
        float u0 = FlickU[i], u1 = FlickU[i + 1], t = (u - u0) / (u1 - u0);
        float m0 = Tangent(i), m1 = Tangent(i + 1);
        float t2 = t * t, t3 = t2 * t;
        float h = (2 * t3 - 3 * t2 + 1) * FlickDeg[i] + (t3 - 2 * t2 + t) * (u1 - u0) * m0
            + (-2 * t3 + 3 * t2) * FlickDeg[i + 1] + (t3 - t2) * (u1 - u0) * m1;
        return h * MathF.PI / 180;
    }

    // Periodic Catmull-Rom slope (degrees per unit u) at key k.
    private static float Tangent(int k)
    {
        int last = FlickU.Length - 1;
        if (k == 0 || k == last)
            return (FlickDeg[1] - FlickDeg[last - 1]) / (FlickU[1] + (1 - FlickU[last - 1]));
        return (FlickDeg[k + 1] - FlickDeg[k - 1]) / (FlickU[k + 1] - FlickU[k - 1]);
    }

    // Holding the item while the concert runs: a light beat pattern on every beat, raised high through the inhale
    // and the chorus.
    internal static float Conduct(float clock)
    {
        float beat = (clock - 1) / ChoirConcertRules.Beat;
        float pattern = -40f + .32f * (Flick(beat) * 180 / MathF.PI + 40f);
        float raise = ChoirConcertRules.Smooth((clock - ChoirConcertRules.Inhale) / 18f)
            * (1 - ChoirConcertRules.Smooth((clock - ChoirConcertRules.Release) / 18f));
        float hold = -105f + 6f * MathF.Sin(clock * .35f);
        return (pattern + (hold - pattern) * raise) * MathF.PI / 180;
    }

    // A right-facing tip angle as a world angle for `facing`.
    internal static float World(float angle, int facing) => facing < 0 ? MathF.PI - angle : angle;
}

// Choir textures for one frame (DollWeaponTextures.Get in game; loaded PNGs offline). A missing one is skipped.
internal readonly record struct ChoirSprites(Texture2D? Chorister0, Texture2D? Chorister1, Texture2D? Chorister2, Texture2D? Organ,
    Texture2D? Baton)
{
    internal Texture2D? Chorister(int variant) => variant switch { 1 => Chorister1, 2 => Chorister2, _ => Chorister0 };
}

internal struct ChoirVoiceDraw
{
    internal Vector2 Center, Velocity;
    internal int Facing, Variant, Ordinal;
    internal float Life;
}

internal struct ChoirNoteDraw
{
    internal Vector2 Center;
    internal int Pitch, TrailCount, TrailHead;
    internal float Age, Fade;
    internal Vector2[]? Trail;
}

internal struct ChoirSpark
{
    internal Vector2 At;
    internal float Age;
    internal int Seed;
}

// One owner's concert for one frame. Fixed arrays, reused every frame; counts beyond the caps are not drawn.
internal sealed class ChoirDrawState
{
    internal const int MaxVoices = 40, MaxNotes = 64, MaxSparks = 8, MaxTrails = 24, TrailPoints = 6, BatonTrailPoints = 8;
    internal readonly ChoirVoiceDraw[] Voices = new ChoirVoiceDraw[MaxVoices];
    internal readonly ChoirNoteDraw[] Notes = new ChoirNoteDraw[MaxNotes];
    internal readonly ChoirSpark[] Sparks = new ChoirSpark[MaxSparks];
    internal readonly Vector2[] BatonTrail = new Vector2[BatonTrailPoints];
    internal int VoiceCount, NoteCount, SparkCount, BatonTrailCount;
    // Another player's choir (its damaging light draws at PeerLightAlpha); a seed per owner.
    internal bool Peer;
    internal int Seed;
    // The stage anchor S (organ mouth), the concert clock with its sub-tick fraction (0 idle) and every voice of
    // the owner (the formation and the pipe count use it even when fewer are drawn).
    internal Vector2 Stage;
    internal float Clock;
    internal int VoiceTotal;
    // A cancelled concert: the organ crumbles from the clock it had (CancelAge >= 0, ticks since the cancel).
    internal float CancelClock, CancelAge = -1;
    // The beam: origin, aim, age from the inhale (0..35 warning, 36.. live), closing 0..1, chord voices at fire.
    internal bool Beam;
    internal Vector2 BeamOrigin;
    internal float BeamAngle, BeamAge, BeamClose;
    internal int ChordVoices = 1;
    // The last chorus axis cooling after the beam (ResidueAge >= 0).
    internal Vector2 ResidueOrigin;
    internal float ResidueAngle, ResidueLength, ResidueAge = -1;
    // The held baton: grip, tip direction (world angle), facing and sweep strength for the tip trail.
    internal bool Baton;
    internal Vector2 BatonGrip;
    internal float BatonAngle, BatonSwing;
    internal int BatonFacing = 1;

    internal bool Empty => VoiceCount == 0 && NoteCount == 0 && SparkCount == 0 && !Beam && !Baton && CancelAge < 0 && ResidueAge < 0;

    internal void Clear()
    {
        VoiceCount = NoteCount = SparkCount = BatonTrailCount = 0;
        Array.Clear(Notes);
        Beam = Baton = Peer = false;
        Clock = 0;
        VoiceTotal = 0;
        CancelAge = ResidueAge = -1;
        BeamClose = BatonSwing = 0;
        ChordVoices = 1;
    }
}

// DollChoirEnergy (Luminance name Convergence.DollChoirEnergy): the beam, the inhale iris and the trails, drawn into
// the Light target per dot. The game binds Effect and the two Luminance noise textures each frame before the layer
// renders; the offline preview binds the compiled .fxc and stand-in noise.
internal sealed class ChoirHymnMaterial : IDollEnergyMaterial
{
    internal const int Hymn = 0, Iris = 1, Trail = 2;
    internal const string ShaderName = "Convergence.DollChoirEnergy";
    private static readonly string[] passes = { "Hymn", "Iris", "Trail" };
    internal Effect? Effect;
    internal Texture2D? NoiseA, NoiseB;

    public bool Apply(GraphicsDevice device, in DollEnergyContext context, int pass)
    {
        Effect? effect = Effect;
        if (effect is null || effect.IsDisposed || NoiseA is null || NoiseB is null || (uint)pass >= (uint)passes.Length) return false;
        DollPixelArt.Set(effect, "uWorldViewProjection", context.Projection);
        DollPixelArt.Set(effect, "dotOrigin", context.DotOrigin);
        DollPixelArt.Set(effect, "clock", (float)(context.Clock % 3600.0));
        DollPixelArt.Set(effect, "reduced", context.Reduced ? 1f : 0f);
        DollPixelArt.Set(effect, "throat", ChoirConcertRules.OrganMouthRadius * DollWeaponCanvas.DotScale);
        DollPixelArt.Set(effect, "throatLength", ChoirConcertRules.BeamStart * DollWeaponCanvas.DotScale);
        device.Textures[1] = NoiseA;
        device.SamplerStates[1] = SamplerState.LinearWrap;
        device.Textures[2] = NoiseB;
        device.SamplerStates[2] = SamplerState.LinearWrap;
        DollPixelArt.Apply(effect, passes[pass]);
        return true;
    }
}
