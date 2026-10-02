#nullable enable
using System;
using Convergence.Content.Encounters.FirstSeverance.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NVector2 = System.Numerics.Vector2;
using Score = Convergence.Content.Encounters.FirstSeverance.Rewards.LacunaTestamentScore;

namespace Convergence.Client.Encounters.FirstSeverance.Weapons;

// The exported Lacuna Testament sprites for one frame (null: that part is skipped). Read through Asset.Value by the
// caller on the frame it draws; never cached here.
internal readonly record struct LacunaSprites(Texture2D? Book, Texture2D? Iris, Texture2D? Great);

// Live: the channel is held. Fading: released (or out of mana); Fade counts ticks since the release. Collapse: the
// controller vanished at once (item change, crowd control, death, Down); Fade counts ticks since then.
internal enum LacunaPhase : byte { Live, Fading, Collapse }

// One frame of a Lacuna Testament channel, filled by the client from replicated projectile and player state at the
// frame's sub-tick fraction. World px, +y down; Age is the fractional score age (frozen while fading).
internal readonly record struct LacunaDrawState
{
    public Vector2 Center { get; init; }
    public Vector2 Hand { get; init; }
    public float Aim { get; init; }
    public float Age { get; init; }
    public int Facing { get; init; }
    public int Direction { get; init; }
    public float GravDir { get; init; }
    public float Fade { get; init; }
    public LacunaPhase Phase { get; init; }
    public LacunaEnd End { get; init; }
    public bool Peer { get; init; }
    public int Seed { get; init; }
}

// One pellet: its interpolated head, flight direction, age (ticks), iris and owner.
internal readonly record struct LacunaPelletState(Vector2 Head, Vector2 Direction, float Age, int Iris, bool Peer, int Seed);

// The Lacuna Testament's void material (DollLacunaEnergy.fx) for the Doll weapon layer's Light target. The effect and
// the two Luminance noise textures come from the caller (the game's shader manager, or the offline preview).
internal sealed class LacunaEnergy : IDollEnergyMaterial
{
    internal const int BeamPass = 0, MouthPass = 1, WakePass = 2;
    internal const string ShaderName = "Convergence.DollLacunaEnergy";
    private static readonly string[] passes = { "BeamPass", "MouthPass", "WakePass" };
    private readonly Func<Effect?> effect;
    private readonly Func<Texture2D?> cloud, flow;

    internal LacunaEnergy(Func<Effect?> effect, Func<Texture2D?> cloud, Func<Texture2D?> flow)
    {
        this.effect = effect;
        this.cloud = cloud;
        this.flow = flow;
    }

    public bool Apply(GraphicsDevice device, in DollEnergyContext context, int pass)
    {
        Effect? shader = effect();
        Texture2D? cloudTexture = cloud(), flowTexture = flow();
        if (shader is null || cloudTexture is null || flowTexture is null || (uint)pass >= passes.Length) return false;
        DollPixelArt.Set(shader, "uWorldViewProjection", context.Projection);
        DollPixelArt.Set(shader, "dotOrigin", context.DotOrigin);
        DollPixelArt.Set(shader, "clock", (float)(context.Clock / 60.0 % 3600.0));
        DollPixelArt.Set(shader, "reduced", context.Reduced ? 1f : 0f);
        device.Textures[1] = cloudTexture;
        device.SamplerStates[1] = SamplerState.LinearWrap;
        device.Textures[2] = flowTexture;
        device.SamplerStates[2] = SamplerState.LinearWrap;
        DollPixelArt.Apply(shader, passes[pass]);
        return true;
    }
}

// Lacuna Testament presentation (docs/encounters/first-severance/WEAPONS.md, "Magic — Lacuna Testament"), recorded
// into the shared Doll weapon layer (Front stratum, in front of every player). Depends only on FNA, the canvas and
// the pure score/anchors, so the offline preview links the same code. Nothing here decides a hit, spends a resource
// or sends anything: positions come from LacunaTestamentScore and the art is fitted to them (LacunaArtFit).
internal static class LacunaPresentation
{
    // The owner's void lets a little of the world through, so forecasts under the beam's black core stay readable.
    // Another player's damaging light draws at DollWeaponCanvas.PeerLightAlpha and their void at PeerVoidAlpha.
    internal const float OwnerVoid = .92f;
    // Energy draw order inside the Light target: holes, then the beam over them, then pellet wakes and heads.
    private const sbyte MouthDepth = 0, BeamDepth = 1, WakeDepth = 2, HeadDepth = 3;
    // Sprite order: book, irises, great aperture.
    private const sbyte BookDepth = 0, IrisDepth = 2, GreatDepth = 3;
    // The pellet head and wake (world px), and how far back the wake reaches.
    internal const float PelletHeadRadius = 7, WakeWidth = 12;
    internal const int MaxWakePoints = 7;
    internal const int Embers = 56;
    // Dots about the muzzle inside which the beam is hidden: the great aperture (its outer radius), and the rosette
    // of irises (dock radius plus an iris's radius) while it breaks up after a release.
    private static readonly float GreatMask = MathF.Ceiling(DollArtAnchors.LacunaGreatIris.OuterRadius);
    private static readonly float RosetteMask = MathF.Ceiling((Score.DockRadius + DollArtAnchors.LacunaIris.FrameWidth) * DollWeaponCanvas.DotScale);

    internal static float LightAlpha(bool peer) => peer ? DollWeaponCanvas.PeerLightAlpha : 1f;
    internal static float VoidAlpha(bool peer) => peer ? DollWeaponCanvas.PeerVoidAlpha : OwnerVoid;

    // ---- Channel --------------------------------------------------------------------------------------------------

    internal static void EmitChannel(DollWeaponCanvas canvas, in LacunaDrawState s, in LacunaSprites art, IDollEnergyMaterial material)
    {
        float age = s.Age, fade = s.Phase == LacunaPhase.Live ? 0 : Math.Max(0, s.Fade);
        bool fading = s.Phase == LacunaPhase.Fading, collapse = s.Phase == LacunaPhase.Collapse, live = s.Phase == LacunaPhase.Live;
        float lightA = LightAlpha(s.Peer), voidA = VoidAlpha(s.Peer);
        Vector2 axis = new(MathF.Cos(s.Aim), MathF.Sin(s.Aim)), normal = new(-axis.Y, axis.X);
        Vector2 muzzle = s.Center + axis * Score.MuzzleDistance;
        bool formed = age >= Score.Formed;
        // A release after the aperture formed breaks it back into seven irises on the rosette.
        bool split = formed && fading;
        float collapseT = collapse ? Score.Clamp01(fade / Score.CollapseTicks) : 0;

        // The book: upright at the hand, bobbing by one dot, kicked back by the beam.
        float bob = MathF.Round(MathF.Sin((float)(canvas.Clock % 9600.0) * MathF.Tau / 96f + s.Seed)) * 2f;
        float recoil = live && age >= Score.Fire ? Recoil(age - Score.Fire) : 0;
        Vector2 bookCentre = s.Hand + axis * Score.BookDistance + new Vector2(0, -2 + bob) - axis * (6 * recoil);
        DollFlip bookFlip = s.Direction < 0 ? DollFlip.Horizontal : DollFlip.None;
        Vector2 bookHole = X(LacunaArtFit.BookHoleAt(N(bookCentre), bookFlip));
        if (art.Book is { } book)
        {
            float flash = live && age >= Score.Fire && age < Score.Fire + 6 ? .7f * (1 - (age - Score.Fire) / 6) : 0;
            canvas.Sprite(new DollSprite(book, book.Bounds, X(LacunaArtFit.BookPivot)), bookCentre, 0, bookFlip, DollStratum.Front, BookDepth,
                new DollSpriteFx { Flash = flash, Fade = fading ? Score.Clamp01(fade / Score.FadeTicks) : 0, Dissolve = collapseT, Seed = s.Seed });
        }
        if (!collapse)
        {
            // The book's hole flares as each iris is born from it.
            int birth = Score.LatestBirth(age, out _);
            float flare = live && birth >= 0 && age - birth < 8 ? 1 - (age - birth) / 8 : 0;
            Mouth(canvas, material, bookHole, LacunaArtFit.BookHoleRadius, .25f + .7f * flare, .5f, flare, lightA * (1 - FadeShare(fade, fading)),
                voidA * (1 - FadeShare(fade, fading)), 0, s.Seed * .37f);
        }

        // The seven irises: born from the book's hole, seated on the arch, docking on the rosette, or (after a
        // release) snapping shut where they are.
        if (art.Iris is { } iris && (!formed || split))
        {
            for (int i = 0; i < Score.Irises; i++)
            {
                int frame = Score.Frame(age, i);
                if (frame < 0) continue;
                Vector2 at = split
                    ? muzzle + X(Score.Dock(i, s.Facing, s.GravDir))
                    : s.Center + X(Score.IrisOffset(i, age, s.Facing, s.GravDir, N(bookHole - s.Center), N(muzzle - s.Center)));
                if (!live) frame = Score.ClosingFrame(i, fade, frame);
                float local = age - Score.Birth(i);
                float flash = live ? Score.Clamp01(1 - local / Score.ArriveTicks) : split ? Score.Clamp01(1 - fade / 3) : 0;
                if (live && age >= Score.Docked(i)) flash = MathF.Max(flash, Score.Clamp01(1 - (age - Score.Docked(i)) / 4));
                if (live && age >= Score.Formed - 3) flash = MathF.Max(flash, Score.Clamp01((age - (Score.Formed - 3)) / 3));
                var fx = new DollSpriteFx
                {
                    Flash = flash,
                    Fade = live ? Score.Clamp01(1 - local / 4) : fading ? Score.Clamp01((fade - 12) / 8) : 0,
                    Dissolve = collapseT,
                    Seed = s.Seed + i,
                };
                Rectangle source = new(frame * DollArtAnchors.LacunaIris.FrameWidth, 0, DollArtAnchors.LacunaIris.FrameWidth, DollArtAnchors.LacunaIris.FrameHeight);
                canvas.Sprite(new DollSprite(iris, source, X(LacunaArtFit.IrisPivot)), at, 0, DollFlip.None, DollStratum.Front, IrisDepth, fx);
                if (frame >= 1 && !collapse && fx.Fade < 1)
                {
                    // The tell half-closes the petals; a shot flashes the hole pearl.
                    bool tell = live && frame == 2 && local >= Score.ShotDelay;
                    float shot = live ? ShotFlash(age, i) : 0;
                    Mouth(canvas, material, X(LacunaArtFit.IrisHoleAt(N(at))), LacunaArtFit.IrisApertureRadius(frame), tell ? .8f : .2f,
                        .45f, shot, lightA * (1 - fx.Fade), voidA * (1 - fx.Fade), tell ? 1 : 0, i * 1.7f);
                    if (shot > 0) canvas.Burst(X(LacunaArtFit.IrisHoleAt(N(at))), s.Seed * 31 + i * 7 + (int)age, 4, (1 - shot) * 3, 9, 2.4f, .02f, DollShardKind.Spark);
                }
                // Each dock lights a pearl notch ring round the arriving iris.
                if (live && age >= Score.Docked(i) && age < Score.Docked(i) + 6)
                {
                    float t = (age - Score.Docked(i)) / 6;
                    canvas.Ring(at, 26 + 8 * t, t < .4f ? DollTone.White : DollTone.PearlViolet, 1, lightA * (1 - t));
                }
            }
        }

        // The great aperture: forms at Formed, ratchets through the charge, holds the beam.
        if (formed && (live || fading && fade < 2 || collapse) && art.Great is { } great)
        {
            float rotation = LacunaArtFit.GreatRotation(age);
            // The sprite flashes only as it forms, fires and breaks; a ratchet notch lights its hole's lip and sparks.
            float flash = live ? Score.Clamp01(1 - (age - Score.Formed) / 4) : fading ? 1 : 0;
            if (live && age >= Score.Fire && age < Score.Fire + 2) flash = MathF.Max(flash, .5f * (1 - (age - Score.Fire) / 2));
            canvas.Sprite(new DollSprite(great, great.Bounds, X(LacunaArtFit.GreatPivot)), muzzle, rotation, DollFlip.None, DollStratum.Front, GreatDepth,
                new DollSpriteFx { Flash = flash, Dissolve = collapseT, Seed = s.Seed + 11 });
            if (!collapse)
            {
                float hot = live ? MathF.Max(Score.Hot(age), age >= Score.Fire ? 0 : Score.Clamp01(1 - (age - Score.Formed) / 4)) : 1;
                Mouth(canvas, material, X(LacunaArtFit.GreatHoleAt(N(muzzle), rotation)), LacunaArtFit.GreatHoleRadius, live ? Score.Spiral(age) : 0,
                    live ? Score.VoidDepth(age) : 1, hot, lightA, voidA, NotchFlash(age) > 0 ? 1 : 0, rotation);
            }
            if (live && age < Score.Formed + 8)
            {
                float t = (age - Score.Formed) / 8;
                canvas.Ring(muzzle, LacunaArtFit.GreatOuterRadius + 4 + 18 * t, t < .3f ? DollTone.White : DollTone.PearlViolet, 1, lightA * (1 - t));
                canvas.Burst(muzzle, s.Seed * 13 + 5, 10, age - Score.Formed, 16, 3f, .03f, DollShardKind.Pearl);
            }
            // Each click throws a few sparks off the ring's notch.
            int click = LatestClick(age);
            if (live && click >= 0 && age - click < 10 && age < Score.Fire)
                canvas.Burst(muzzle - normal * (LacunaArtFit.GreatRingRadius * s.Facing), s.Seed * 17 + click, 4, age - click, 10, 1.6f, .05f, DollShardKind.Spark);
        }

        // The forecast: the beam's opening edges, drawn out from the ring through the last clicks of the charge.
        int forecastFrom = Score.ClickAt(2);
        if (live && age >= forecastFrom && age < Score.Fire)
        {
            float progress = Score.Arrive((age - forecastFrom) / 18f);
            Vector2 start = muzzle + axis * LacunaArtFit.GreatOuterRadius, end = muzzle + axis * Score.Length;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 offset = normal * (side * Score.OpenWidth * .5f);
                canvas.Forecast(start + offset, end + offset, progress, lightA);
            }
        }

        // The beam: opens from the hole, pulses, widens; on release it retracts into the hole.
        if (age > Score.Fire && !collapse && (live || fade < Score.RetractTicks))
        {
            float reach = Score.Reach(age) * (live ? 1 : Score.Retract(fade));
            float width = Score.Width(age) * (live ? 1 : Score.RetractWidth(fade));
            float pulseA = -10000, pulseB = -10000;
            if (live)
            {
                Score.PulseBands(age, out float a, out float b);
                if (a >= 0) pulseA = a;
                if (b >= 0) pulseB = b;
            }
            Beam(canvas, material, muzzle, axis, reach, width, pulseA, pulseB, live ? Score.Hot(age) : .2f * (1 - fade / Score.RetractTicks),
                lightA, voidA, split ? RosetteMask : GreatMask);
            if (live && age < Score.Fire + 14)
            {
                float t = age - Score.Fire;
                canvas.Burst(muzzle + axis * LacunaArtFit.GreatOuterRadius, s.Seed * 19 + 3, 12, t, 18, 4.2f, 0, DollShardKind.Pearl, .9f, s.Aim);
                canvas.Burst(muzzle + axis * LacunaArtFit.GreatOuterRadius, s.Seed * 19 + 4, 8, t, 12, 5f, 0, DollShardKind.Spark, 1.4f, s.Aim);
                if (t < 8) canvas.Ring(muzzle, LacunaArtFit.GreatOuterRadius + 6 + 26 * t / 8, t < 3 ? DollTone.White : DollTone.PearlViolet, 1, lightA * (1 - t / 8));
            }
            if (live && WidenFlash(age) is { } beat)
            {
                float t = age - beat;
                canvas.Ring(muzzle, LacunaArtFit.GreatOuterRadius + 3 + 10 * t / 8, DollTone.PearlViolet, 1, lightA * (1 - t / 8));
            }
        }

        // Residue: void embers along the former beam cool from lilac to plum (half as many, half as long, reduced).
        if (!live && age > Score.Fire)
        {
            float life = Score.ResidueTicks * (canvas.Reduced ? .5f : 1);
            if (fade < life) Residue(canvas, muzzle, axis, Score.Reach(age), Score.Width(age), fade, life, s.Seed, lightA);
        }

        // The collapse of an instant end: porcelain crumbs and one pearl ring.
        if (collapse && fade < Score.CollapseTicks + 6)
        {
            canvas.Burst(bookCentre, s.Seed * 23 + 1, 8, fade, 20, 2.6f, .12f, DollShardKind.Porcelain);
            if (formed)
            {
                canvas.Burst(muzzle, s.Seed * 23 + 2, 14, fade, 22, 3.4f, .12f, DollShardKind.Porcelain);
                canvas.Burst(muzzle, s.Seed * 23 + 3, 8, fade, 22, 2.4f, .1f, DollShardKind.Brass);
                if (fade < 8) canvas.Ring(muzzle, LacunaArtFit.GreatHoleRadius * (1 - fade / 10), DollTone.PearlViolet, 1, lightA * (1 - fade / 8));
            }
        }
    }

    // ---- Pellets, bites and puffs --------------------------------------------------------------------------------

    // A pellet: a small void head with a pearl lip, its wake trailing back along `trail` (world points, oldest first,
    // ending just behind the head).
    // Recording every pellet's wake first and every head after keeps all pellets in two energy batches.
    internal static void EmitPellet(DollWeaponCanvas canvas, in LacunaPelletState p, ReadOnlySpan<Vector2> trail, IDollEnergyMaterial material)
    {
        EmitPelletWake(canvas, p, trail, material);
        EmitPelletHead(canvas, p, material);
    }

    internal static void EmitPelletHead(DollWeaponCanvas canvas, in LacunaPelletState p, IDollEnergyMaterial material)
        => Mouth(canvas, material, p.Head, PelletHeadRadius, .55f, .4f, Score.Clamp01(1 - p.Age / 4), LightAlpha(p.Peer), VoidAlpha(p.Peer), 1,
            p.Seed * .91f, HeadDepth);

    internal static void EmitPelletWake(DollWeaponCanvas canvas, in LacunaPelletState p, ReadOnlySpan<Vector2> trail, IDollEnergyMaterial material)
    {
        float lightA = LightAlpha(p.Peer), voidA = VoidAlpha(p.Peer);
        float hot = Score.Clamp01(1 - p.Age / 4);
        Span<Vector2> spine = stackalloc Vector2[MaxWakePoints + 1];
        int count = 0;
        foreach (Vector2 point in trail)
        {
            if (count >= MaxWakePoints) break;
            if (count > 0 && Vector2.DistanceSquared(spine[count - 1], point) < 4) continue;
            spine[count++] = point;
        }
        if (count == 0 || Vector2.DistanceSquared(spine[count - 1], p.Head) >= 4) spine[count++] = p.Head;
        if (count >= 2)
            canvas.EnergyStrip(material, LacunaEnergy.WakePass, spine[..count], WakeWidth, new Vector4(hot, lightA, voidA, (p.Seed & 63) * .13f), WakeDepth);
    }

    // A pellet or beam contact: a closing pearl ring and pearl motes (debris halves under Reduced Effects).
    internal static void EmitBite(DollWeaponCanvas canvas, Vector2 at, float t, int seed, bool peer, bool beam)
    {
        float lightA = LightAlpha(peer);
        if (t < 8) canvas.Ring(at, 18 - 15 * Score.Arrive(t / 8), t < 3 ? DollTone.White : DollTone.PearlViolet, 1, lightA);
        canvas.Burst(at, seed, beam ? 5 : 7, t, 16, 2.2f, .04f, DollShardKind.Pearl);
        canvas.Burst(at, seed + 1, 4, t, 12, 3f, .02f, DollShardKind.Spark);
    }

    internal static float BiteLife => 16;

    // A pellet that ended without a hit (timeout, unusable owner): it just goes out, plum, in a few ticks.
    internal static void EmitPuff(DollWeaponCanvas canvas, Vector2 at, float t, bool peer)
    {
        float life = PuffLife(canvas.Reduced);
        if (t >= life) return;
        float share = t / life;
        canvas.Dot(at, share < .34f ? DollTone.Violet : share < .67f ? DollTone.PlumLight : DollTone.Plum, share < .5f ? 2 : 1, LightAlpha(peer));
    }

    internal static float PuffLife(bool reduced) => reduced ? 3 : 6;

    // ---- Parts ----------------------------------------------------------------------------------------------------

    // A void hole of `radius` world px at `centre`: a one-dot pearl lip round a black disc, violet arms spiralling in.
    private static void Mouth(DollWeaponCanvas canvas, IDollEnergyMaterial material, Vector2 centre, float radius, float spiral, float depth,
        float hot, float lightA, float voidA, float boost, float spin, sbyte energyDepth = MouthDepth)
    {
        if (!(radius > .5f) || !(lightA > .004f || voidA > .004f)) return;
        Span<DollPixelVertex> quad = canvas.Energy(material, LacunaEnergy.MouthPass, 2, energyDepth);
        if (quad.IsEmpty) return;
        Vector2 c = canvas.ToDot(centre);
        float r = radius * DollWeaponCanvas.DotScale, extent = r + 1.5f;
        Vector4 style = new(Score.Clamp01(hot), lightA, voidA, boost);
        for (int k = 0; k < 4; k++)
        {
            Vector2 offset = new(k % 2 == 0 ? -extent : extent, k < 2 ? -extent : extent);
            Vector2 at = c + offset;
            ref DollPixelVertex v = ref quad[k == 3 ? 5 : k];
            v.Position = new Vector3(at, 0);
            v.Local = new Vector4(offset.X, offset.Y, r, Score.Clamp01(spiral));
            v.Shape = new Vector4(at.X, at.Y, Score.Clamp01(depth), spin);
            v.Style = style;
        }
        quad[3] = quad[2];
        quad[4] = quad[1];
    }

    // The beam body from the muzzle along `axis`: reach and collision width in world px.
    private static void Beam(DollWeaponCanvas canvas, IDollEnergyMaterial material, Vector2 muzzle, Vector2 axis, float reach, float width,
        float pulseA, float pulseB, float hot, float lightA, float voidA, float maskDots)
    {
        if (!(reach > 1f) || !(width > .5f)) return;
        Span<DollPixelVertex> quad = canvas.Energy(material, LacunaEnergy.BeamPass, 2, BeamDepth);
        if (quad.IsEmpty) return;
        float scale = DollWeaponCanvas.DotScale;
        float reachDots = reach * scale, half = width * .5f * scale, extent = half * 1.4f + 2;
        Vector2 origin = canvas.ToDot(muzzle), normal = new(-axis.Y, axis.X);
        float lip = MathF.Max(MathF.Abs(normal.X), MathF.Abs(normal.Y));
        Vector4 style = new(Score.Clamp01(hot), lightA, voidA, MathF.Floor(Math.Max(0, maskDots)) + lip * .5f);
        for (int k = 0; k < 4; k++)
        {
            float along = k % 2 == 0 ? -1 : reachDots + 2, across = k < 2 ? -extent : extent;
            Vector2 at = origin + axis * along + normal * across;
            ref DollPixelVertex v = ref quad[k == 3 ? 5 : k];
            v.Position = new Vector3(at, 0);
            v.Local = new Vector4(along, across, reachDots, half);
            v.Shape = new Vector4(at.X, at.Y, pulseA * scale, pulseB * scale);
            v.Style = style;
        }
        quad[3] = quad[2];
        quad[4] = quad[1];
    }

    // Void embers scattered along the former beam, drifting outward and cooling to plum over `life` ticks.
    private static void Residue(DollWeaponCanvas canvas, Vector2 muzzle, Vector2 axis, float reach, float width, float t, float life, int seed, float alpha)
    {
        if (!(reach > 1f)) return;
        Vector2 normal = new(-axis.Y, axis.X);
        int count = canvas.Reduced ? Embers / 2 : Embers;
        float share = t / life;
        for (int k = 0; k < count; k++)
        {
            float a = Hash01(seed * 131 + k * 7), b = Hash01(seed * 37 + k * 13 + 1), c = Hash01(seed * 53 + k * 29 + 2);
            if (c < share * share) continue;
            // Denser near the aperture, where the beam was thickest in view.
            float along = LacunaArtFit.GreatOuterRadius + MathF.Pow(a, 1.7f) * Math.Max(0, reach - LacunaArtFit.GreatOuterRadius);
            float across = (b - .5f) * width * .9f;
            Vector2 at = muzzle + axis * (along + t * .6f) + normal * (across + MathF.Sign(across) * t * .5f);
            DollTone tone = share < .25f ? DollTone.Lilac : share < .5f ? DollTone.Violet : share < .75f ? DollTone.PlumLight : DollTone.Plum;
            canvas.Dot(at, tone, k % 3 == 0 && share < .6f ? 2 : 1, alpha);
        }
    }

    // Book recoil at the fire: out in 1.5 ticks, settled in 10.
    private static float Recoil(float t) => t < 0 ? 0 : Score.Arrive(t / 1.5f) * (1 - Score.Smooth((t - 1.5f) / 10));

    // How much of a fading hole is gone.
    private static float FadeShare(float fade, bool fading) => fading ? Score.Clamp01(fade / Score.FadeTicks) : 0;

    // The flash on an iris's hole as it shoots (3 ticks).
    private static float ShotFlash(float age, int iris)
    {
        int tick = (int)MathF.Floor(age);
        for (int back = 0; back < 3; back++)
            if (Score.ShotAt(tick - back, iris)) return 1 - (age - (tick - back)) / 3;
        return 0;
    }

    private static int LatestClick(float age)
    {
        int latest = -1;
        for (int k = 0; k < Score.Clicks; k++) if (age >= Score.ClickAt(k)) latest = Score.ClickAt(k);
        return latest;
    }

    // A short flash on the great aperture at each ratchet notch (clicks and widen beats).
    private static float NotchFlash(float age)
    {
        float flash = 0;
        for (int k = 0; k < Score.Clicks; k++)
        {
            float t = age - Score.ClickAt(k);
            if (t >= 0 && t < 3) flash = MathF.Max(flash, .35f * (1 - t / 3));
        }
        if (WidenFlash(age) is { } beat) flash = MathF.Max(flash, .5f * (1 - (age - beat) / 8));
        return flash;
    }

    private static int? WidenFlash(float age)
    {
        int beat = Score.LatestWiden(age, out _);
        return beat >= 0 && age - beat < 8 ? beat : null;
    }

    private static float Hash01(int n)
    {
        uint h = (uint)n * 747796405u + 2891336453u;
        h = ((h >> (int)((h >> 28) + 4u)) ^ h) * 277803737u;
        h = (h >> 22) ^ h;
        return (h & 0xFFFFFF) / 16777216f;
    }

    private static Vector2 X(NVector2 v) => new(v.X, v.Y);
    private static NVector2 N(Vector2 v) => new(v.X, v.Y);
}
