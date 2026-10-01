#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Weapons;
using Convergence.Content.Encounters.EbonManor.Rewards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.EbonManor.Rewards;

// The Ebon Hatbox opening show (REWARDS.md "Ebon Hatbox"): a small personal, client-only show about 70 px
// above the opener's head. The box pops in and gathers (squash), the bow unties (0-10: two silk streamers
// unspool and curl away), then one release at tick 10: the lid pops up spinning and fades (10-20), a star
// and a soft ring open at the rim, and moonlight motes, lace and silk burst out (12-40); the box settles,
// then dissolves (44-60). Sprites are drawn here in the world pass (PostDrawTiles, like the other Convergence
// world sprites); streamers, star, ring and debris are pixel-layer primitives from a retained source.
// Time is the per-tick sample count plus WeaponDrawClock.Fraction; no hit, packet or world state is involved.
[Autoload(Side = ModSide.Client)]
internal sealed class EbonHatboxVisuals : ModSystem
{
    private const int Life = 60, ReleaseTick = 10, BurstTick = 12, MaxShows = 4;
    private const float HoverHeight = 70;

    // The cue is one rising pluck run ending on a chord; it starts with the open and is expected to land its
    // chord about when the lid pops (ReleaseTick), so no visual is delayed for it.
    private const string Cue = "HatboxOpen";

    private static readonly List<Show> shows = new(MaxShows);
    private static readonly Vector2[] ribbon = new Vector2[16];
    private static BoxArt? art;
    private static bool failed;
    private static int opened;

    // Resolved box art and the closed-box layout (pixels, y down, origin at the closed box's centre).
    private sealed record BoxArt(Texture2D Body, Texture2D Lid, Texture2D? Bow, Rectangle BodySrc, Rectangle LidSrc, Rectangle BowSrc,
        float Scale, bool Pixel, float BodyHeight, float BodyBottom, float LidY, float BowY, float MouthY);

    private sealed class Show : IEbonPixelSource
    {
        internal readonly BoxArt Art;
        internal readonly int Owner, Seed, Side;
        internal readonly Vector2 Jitter;
        internal int Ticks;
        internal bool Dead, Released;
        internal Vector2 Previous, Anchor, Release;

        internal Show(BoxArt art, Player player, int seed, Vector2 jitter)
        {
            Art = art; Owner = player.whoAmI; Seed = seed; Side = (seed & 1) == 0 ? 1 : -1; Jitter = jitter;
            Previous = Anchor = Target(player) + jitter;
        }

        internal bool Done => Dead || Ticks > Life + 1;
        // Draw between the two latest accepted ticks, as the weapons do.
        internal float Age(float fraction) => Math.Max(0, Ticks - 1 + fraction);
        internal Vector2 Live(float fraction, float age) => Vector2.Lerp(Previous, Anchor, fraction) + BoxOffset(age);
        // Everything that leaves the box at the release is a free object in the world from here on.
        internal Vector2 Free => Release + BoxOffset(ReleaseTick);

        internal void Step()
        {
            Ticks++;
            Previous = Anchor;
            Player player = Main.player[Owner];
            if (player.active)
            {
                Vector2 want = Target(player) + Jitter;
                if (Vector2.DistanceSquared(Anchor, want) > 400f * 400f) Previous = Anchor = want;
                else Anchor = Vector2.Lerp(Anchor, want, .35f);
            }
            if (Ticks == ReleaseTick) { Release = Anchor; Released = true; }
            float glow = BodyAlpha(Ticks) * (.18f + .55f * EbonVisualsMath.Pulse(Ticks - ReleaseTick, 9));
            Lighting.AddLight(Anchor + BoxOffset(Ticks), .42f * glow, .48f * glow, .8f * glow);
        }

        public bool Emit(EbonPixelCanvas canvas)
        {
            if (Done) return false;
            EmitShow(this, canvas);
            return true;
        }
    }

    public override void Load() => EbonHatbox.Opened = Open;
    public override void Unload() { EbonHatbox.Opened = null; Clear(); art = null; failed = false; }
    public override void ClearWorld() => Clear();
    public override void OnWorldUnload() => Clear();

    private static void Clear()
    {
        foreach (Show show in shows) show.Dead = true;
        shows.Clear();
        opened = 0;
    }

    private void Open(Player player)
    {
        if (Main.dedServ || Main.gameMenu || !player.active) return;
        EbonRewardAudio.Play(Cue, player.Center, .8f, 0, .03f, 2);
        BoxArt? box = ResolveArt();
        if (box is null) return;
        uint hash = unchecked((Main.GameUpdateCount + (uint)(++opened) * 977u + (uint)player.whoAmI * 131u) * 2654435761u);
        int seed = (int)(hash >> 8);
        // Boxes opened in quick succession sit as a small cluster instead of one stacked sprite.
        Vector2 jitter = shows.Count == 0 ? Vector2.Zero : new(((seed >> 1) % 5 - 2) * 9, ((seed >> 5) % 3 - 1) * 5);
        if (shows.Count >= MaxShows) { shows[0].Dead = true; shows.RemoveAt(0); }
        var show = new Show(box, player, seed, jitter);
        shows.Add(show);
        EbonPixelLayer.Add(show);
    }

    public override void PostUpdateEverything()
    {
        if (shows.Count == 0 || Main.gamePaused) return;
        for (int i = shows.Count - 1; i >= 0; i--)
        {
            Show show = shows[i];
            if (show.Done) { show.Dead = true; shows.RemoveAt(i); continue; }
            show.Step();
        }
    }

    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu || shows.Count == 0) return;
        DrawPass(true);
        DrawPass(false);
    }

    // Final art is pixel art (point sampling); the placeholder is a painted vanilla item (linear).
    private void DrawPass(bool pixel)
    {
        bool any = false;
        foreach (Show show in shows) any |= show.Art.Pixel == pixel;
        if (!any) return;
        SpriteBatch batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, pixel ? SamplerState.PointClamp : SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            float fraction = WeaponDrawClock.Fraction;
            foreach (Show show in shows)
                if (show.Art.Pixel == pixel) DrawShow(batch, show, fraction);
        }
        catch (Exception e)
        {
            failed = true; Clear();
            Mod.Logger.Warn("Ebon hatbox show disabled for this session after a drawing error; opening still works.", e);
        }
        finally { batch.End(); }
    }

    // ---- Timeline ---------------------------------------------------------------------------------

    private static float Ease(float x) => EbonVisualsMath.Ease(x);
    private static float OutBack(float x)
    {
        x = Math.Clamp(x, 0, 1) - 1;
        return 1 + 2.70158f * x * x * x + 1.70158f * x * x;
    }

    private static Vector2 Target(Player player)
        => (player.gravDir >= 0 ? player.Top : player.Bottom) + new Vector2(0, -HoverHeight * (player.gravDir >= 0 ? 1 : -1));

    // The box rises from the head with a small overshoot, hovers, then lifts a little as it dissolves.
    private static Vector2 BoxOffset(float a)
    {
        float rise = (1 - OutBack(a / 9)) * 30;
        float bob = 2.4f * MathF.Sin(a * .11f) * Ease((a - 4) / 6);
        float lift = -8 * Ease((a - 44) / 14);
        return new(0, rise + bob + lift);
    }

    // Pops in with overshoot over 6 ticks; shrinks slightly as it dissolves.
    private static float Pop(float a) => (a < 6 ? OutBack(a / 6) : 1) * (1 - .15f * Ease((a - 44) / 14));
    private static float BodyAlpha(float a) => Ease(a / 3) * (1 - Ease((a - 44) / 14));

    // Anticipation (6-10) compresses the box; the release springs it past rest and it wobbles to a stop.
    private static Vector2 Squash(float a)
    {
        float sy = 1;
        if (a >= 6 && a < ReleaseTick) sy = 1 - .14f * Ease((a - 6) / 4);
        else if (a >= ReleaseTick)
        {
            float t = a - ReleaseTick, e = MathF.Exp(-t / 5);
            sy = 1 + e * (-.14f * MathF.Cos(.8f * t) + .17f * MathF.Sin(.8f * t));
        }
        return new(1 - .5f * (sy - 1), sy);
    }

    // Closed-box local point -> offset from the box anchor at age `a`: pop-in about the centre, squash about the base.
    private static Vector2 Map(BoxArt box, Vector2 p, float a)
    {
        float u = Pop(a);
        Vector2 q = Squash(a);
        return new(p.X * u * q.X, box.BodyBottom * u + (p.Y - box.BodyBottom) * u * q.Y);
    }

    // The bow: wobbles and lifts as it unties (0-10), then flies off with the streamers. Velocity feeds their drag.
    private static Vector2 BowPosition(Show s, float a, float fraction, out Vector2 velocity)
    {
        BoxArt box = s.Art;
        if (a < ReleaseTick || !s.Released)
        {
            velocity = default;
            return s.Live(fraction, a) + Map(box, new(0, box.BowY - 5 * Ease((a - 3) / 7)), a);
        }
        float t = a - ReleaseTick;
        velocity = new(s.Side * 2.5f, -5.4f + .3f * t);
        return s.Free + Map(box, new(0, box.BowY - 5), ReleaseTick) + new Vector2(s.Side * 2.5f * t, -5.4f * t + .15f * t * t);
    }

    // ---- Sprites ----------------------------------------------------------------------------------

    private static void DrawShow(SpriteBatch batch, Show s, float fraction)
    {
        BoxArt box = s.Art;
        float a = s.Age(fraction), alpha = BodyAlpha(a);
        if (alpha <= .003f) return;
        Vector2 live = s.Live(fraction, a), screen = Main.screenPosition;
        Vector2 squash = Squash(a);
        float pop = Pop(a);
        Vector2 scale = new(box.Scale * pop * squash.X, box.Scale * pop * squash.Y);
        Color color = Color.White * alpha;
        bool closed = a < ReleaseTick || !s.Released;

        Draw(batch, box.Body, box.BodySrc, live + Map(box, new(0, box.BodyBottom - box.BodyHeight * .5f), a) - screen, 0, scale, color);
        if (closed)
            Draw(batch, box.Lid, box.LidSrc, live + Map(box, new(0, box.LidY), a) - screen, 0, scale, color);
        else
        {
            // The lid pops straight up and tumbles away, fading over 13-21.
            float t = a - ReleaseTick, lidAlpha = 1 - Ease((a - 13) / 8);
            if (lidAlpha > .003f)
            {
                Vector2 at = s.Free + Map(box, new(0, box.LidY), ReleaseTick) + new Vector2(-s.Side * 2.7f * t, -8.6f * t + .31f * t * t);
                Draw(batch, box.Lid, box.LidSrc, at - screen, s.Side * (.5f * t - .014f * t * t),
                    new Vector2(box.Scale * (1 - .2f * Ease(t / 12))), Color.White * (alpha * lidAlpha));
            }
        }
        if (box.Bow is null) return;
        float bowAlpha = closed ? 1 : 1 - Ease((a - 14) / 12);
        if (bowAlpha <= .003f) return;
        Vector2 bowAt = BowPosition(s, a, fraction, out _);
        float rotation = closed ? MathF.Sin(a * 1.9f) * .16f * Ease((a - 1) / 9) : -s.Side * .28f * (a - ReleaseTick);
        Vector2 bowScale = closed ? scale * (1 + .12f * Ease((a - 3) / 7)) : new Vector2(box.Scale * (1 - .25f * Ease((a - ReleaseTick) / 16)));
        Draw(batch, box.Bow, box.BowSrc, bowAt - screen, rotation, bowScale, Color.White * (alpha * bowAlpha));
    }

    private static void Draw(SpriteBatch batch, Texture2D texture, Rectangle source, Vector2 at, float rotation, Vector2 scale, Color color)
        => batch.Draw(texture, at, source, color, rotation, new Vector2(source.Width, source.Height) * .5f, scale, SpriteEffects.None, 0);

    // ---- Pixel layer ------------------------------------------------------------------------------

    private static void EmitShow(Show s, EbonPixelCanvas c)
    {
        float a = s.Age(c.Fraction);
        Streamers(s, c, a);
        if (!s.Released || a < ReleaseTick) return;
        BoxArt box = s.Art;
        float t = a - ReleaseTick;
        Vector2 mouth = s.Free + Map(box, new(0, box.MouthY), ReleaseTick);

        // The release: one star at the rim and a soft ring (a rose echo follows unless reduced).
        if (t < 10) c.Star(mouth, 30, t / 10, s.Seed);
        if (t < 16)
        {
            float x = t / 16;
            c.Ring(mouth, 12 + 44 * EbonVisualsMath.OutExpo(x), EbonTone.Moon, 1, .85f * (1 - x));
        }
        if (!c.Reduced && t >= 3 && t < 19)
        {
            float x = (t - 3) / 16;
            c.Ring(mouth, 8 + 30 * EbonVisualsMath.OutExpo(x), EbonTone.Rose, 1, .6f * (1 - x));
        }

        // Lace and silk fly up and flutter down; moon motes drift upward. All end by tick 40.
        float bt = a - BurstTick;
        if (bt >= 0 && bt < 28) c.Burst(mouth, s.Seed, Half(c, 12), bt, 28, 3.4f, .07f, EbonShardKind.Lace, 2.2f, Up);
        if (bt >= 2 && bt < 28) c.Burst(mouth, s.Seed + 1, Half(c, 8), bt - 2, 26, 4.6f, .04f, EbonShardKind.Silk, 1.6f, Up);
        if (bt >= 0 && bt < 28) Motes(c, mouth, s.Seed + 2, Half(c, 14), bt, 28, 1.8f);

        // Recovery: the box lets go of a few last motes as it dissolves, attached to the box.
        float dt = a - 42;
        if (!c.Reduced && dt >= 0 && dt < 16)
            Motes(c, s.Live(c.Fraction, a) + Map(box, new(0, box.BodyBottom - box.BodyHeight * .5f), a), s.Seed + 3, 6, dt, 16, 1.2f);
    }

    private const float Up = -MathF.PI / 2;
    private static int Half(EbonPixelCanvas c, int count) => c.Reduced ? Math.Max(2, count / 2) : count;

    // Moonlight motes on analytic arcs (nothing retained): they leave the origin, slow down, rise and twinkle out.
    private static void Motes(EbonPixelCanvas c, Vector2 origin, int seed, int count, float t, float life, float speed)
    {
        for (int i = 0; i < count; i++)
        {
            float h1 = EbonMaterials.Hash(seed, i, 1), h2 = EbonMaterials.Hash(seed, i, 2), h3 = EbonMaterials.Hash(seed, i, 3);
            float v = speed * (.35f + .65f * h2), drag = 1 - MathF.Exp(-t / 12);
            Vector2 at = origin + (Up + (h1 - .5f) * 2.8f).ToRotationVector2() * v * 12 * drag + new Vector2(0, -.01f * t * t);
            float alpha = (1 - Ease((t - life * .5f) / (life * .5f))) * (.65f + .35f * MathF.Sin(t * .5f + h3 * 6));
            c.Dot(at, h3 > .5f ? EbonTone.Moon : EbonTone.Ivory, h2 > .75f ? 2 : 1, alpha);
        }
    }

    // Two silk tails unspool from the knot over 0-10 and curl tighter as the bow flies off, trailing behind it.
    private static void Streamers(Show s, EbonPixelCanvas c, float a)
    {
        float alpha = 1 - Ease((a - 24) / 14);
        float reach = 58 * Ease(a / 10) + 26 * Ease((a - ReleaseTick) / 22);
        if (alpha <= .01f || reach < 3) return;
        Vector2 root = BowPosition(s, a, c.Fraction, out Vector2 velocity);
        int n = c.Reduced ? 6 : 10;
        float curl = MathHelper.Lerp(.5f, 3.3f, Ease(a / 24)), segment = reach / n;
        for (int arm = 0; arm < 2; arm++)
        {
            float d = arm == 0 ? -1 : 1, heading = d > 0 ? -.55f : MathF.PI + .55f;
            Vector2 p = root;
            ribbon[0] = root;
            for (int i = 1; i <= n; i++)
            {
                float u = (i - 1) / (float)n;
                float theta = heading + d * (curl * MathF.Pow(u, 1.4f) + .3f * u * MathF.Sin(a * .38f - u * 6 + arm * 2));
                p += theta.ToRotationVector2() * segment;
                ribbon[i] = p - velocity * (3f * i / n);
            }
            for (int i = 1; i <= n; i++)
            {
                float u = i / (float)n;
                c.Thread(ribbon[i - 1], ribbon[i], u > .7f ? EbonTone.Ivory : EbonTone.Rose, u < .6f ? 2 : 1, alpha * (1 - .3f * u), .4f);
            }
        }
    }

    // ---- Art --------------------------------------------------------------------------------------

    private BoxArt? ResolveArt()
    {
        if (failed) return null;
        try
        {
            bool final = EbonRewardArt.HasFinal("HatboxBody") && EbonRewardArt.HasFinal("HatboxLid") && EbonRewardArt.HasFinal("HatboxBow");
            Texture2D body = final ? EbonRewardArt.Final("HatboxBody") : PlaceholderTexture();
            if (art is not null && ReferenceEquals(art.Body, body) && art.Pixel == final) return art;
            return art = final ? BuildFinal(body) : BuildPlaceholder(body);
        }
        catch (Exception e)
        {
            failed = true;
            Mod.Logger.Warn("Ebon hatbox show art unavailable; the box still opens.", e);
            return null;
        }
    }

    // Placeholder: the vanilla present, its top 40% standing in for the lid (and its bow).
    private static Texture2D PlaceholderTexture()
    {
        var asset = TextureAssets.Item[ItemID.Present];
        if (!asset.IsLoaded) Main.instance.LoadItem(ItemID.Present);
        return asset.Value;
    }

    // Final art: the three parts are assembled from their opaque bounds (the exported cells may or may not be
    // cropped), body base on the floor of the layout, lid resting on the rim, knot resting on the lid.
    private static BoxArt BuildFinal(Texture2D body)
    {
        Texture2D lid = EbonRewardArt.Final("HatboxLid"), bow = EbonRewardArt.Final("HatboxBow");
        float s = EbonRewardArt.PixelScale;
        Rectangle bs = OpaqueBounds(body), ls = OpaqueBounds(lid), ws = OpaqueBounds(bow);
        float bh = bs.Height * s, lh = ls.Height * s, wh = ws.Height * s;
        float lidOverlap = MathF.Max(s, bh * .1f), bowOverlap = MathF.Max(s, wh * .2f);
        float bottom = (bh + lh - lidOverlap + wh - bowOverlap) * .5f, top = bottom - bh;
        float lidY = top + lidOverlap - lh * .5f, bowY = lidY - lh * .5f + bowOverlap - wh * .5f;
        return new BoxArt(body, lid, bow, bs, ls, ws, s, true, bh, bottom, lidY, bowY, top);
    }

    private static BoxArt BuildPlaceholder(Texture2D texture)
    {
        Rectangle all = OpaqueBounds(texture);
        int cut = Math.Clamp((int)MathF.Round(all.Height * .4f), 1, Math.Max(1, all.Height - 1));
        Rectangle lidSrc = new(all.X, all.Y, all.Width, cut), bodySrc = new(all.X, all.Y + cut, all.Width, all.Height - cut);
        float s = Math.Clamp(54f / Math.Max(1, all.Width), 1f, 3f);
        float bh = bodySrc.Height * s, lh = lidSrc.Height * s, bottom = (bh + lh) * .5f, top = bottom - bh, lidY = top - lh * .5f;
        // No separate bow: the streamers unspool from the top of the lid.
        return new BoxArt(texture, texture, null, bodySrc, lidSrc, default, s, false, bh, bottom, lidY, lidY - lh * .5f, top);
    }

    private static Rectangle OpaqueBounds(Texture2D texture)
    {
        var pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);
        int x0 = texture.Width, y0 = texture.Height, x1 = -1, y1 = -1;
        for (int y = 0; y < texture.Height; y++)
        for (int x = 0; x < texture.Width; x++)
            if (pixels[y * texture.Width + x].A > 8)
            { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
        return x1 >= x0 ? new Rectangle(x0, y0, x1 - x0 + 1, y1 - y0 + 1) : texture.Bounds;
    }
}
