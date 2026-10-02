#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Encounters.FirstSeverance;
using Convergence.Client.Graphics;
using Convergence.Content.Encounters.GhostSamurai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using Terraria.UI;

namespace Convergence.Client.Encounters.GhostSamurai;

// The in-field backdrop, drawn in the 0-depth sky slice: after the vanilla
// parallax layers, before walls and tiles. Field-anchored, per-layer parallax.
internal sealed class GhostSamuraiBattlefieldSky : CustomSky
{
    internal const string Key = "Convergence:GhostSamuraiBattlefield";
    private bool requested;
    internal bool IsSceneRequested => requested;
    public override void Activate(Vector2 position, params object[] args) => requested = true;
    public override void Deactivate(params object[] args) => requested = false;
    public override void Reset() => requested = false;
    public override bool IsActive() => requested && GhostSamuraiBattlefield.Backdrop(out _, out _);
    public override float GetCloudAlpha() => 1 - GhostSamuraiBattlefield.Cover;
    public override void Update(GameTime gameTime) { }

    public override void Draw(SpriteBatch batch, float minDepth, float maxDepth)
    {
        if (Main.dedServ || Main.gameMenu || minDepth >= 0 || maxDepth < 0
            || !GhostSamuraiBattlefield.Backdrop(out var field, out var look)) return;
        using var scope = new WorldGraphicsScope(batch);
        SamuraiFieldRenderer.DrawBackdrop(Main.instance.GraphicsDevice, Main.GameViewMatrix.TransformationMatrix,
            new Vector2(field.Left, field.Top) - Main.screenPosition, look);
    }
}

// The seal the mourning bell raises around its summoner. Client presentation only:
// it reads the accepted fight and the local view, never gameplay state it does not own.
// A participant (bound, or fallen and watching from inside) sees the trace deploy from
// the summoner's feet, the backdrop, the abyss outside and a seal they can bump; the
// seal stays under the victory or defeat stage, then its talismans burn and it unravels.
// Anyone else near an active fight sees the seal and a faint veil from the world.
[Autoload(Side = ModSide.Client)]
internal sealed class GhostSamuraiBattlefield : ModSystem
{
    private const float TraceTicks = 54, QuickTraceTicks = 16, Grace = 12, ReleaseTicks = 30, BurnTicks = 90;
    private const float WatchMargin = 900, RippleLife = .55f;

    private struct Seal
    {
        internal Guid Fight;
        internal SamuraiArenaBounds Field;
        internal ulong Since, ReleasedAt;
        internal bool Held;          // a victory or defeat stage holds the seal, then it burns and melts
        internal float Hold;
        internal float Trace, Clock, Phase, Surge, Presence;
    }

    private static GhostSamuraiBattlefieldSky? sky;
    private static Seal seal, watch;
    private static Vector4 screenField;
    private static bool screenFlip, screenReady;
    private static Vector3 rippleA, rippleB;          // field-local x, y, birth tick
    private static ulong lastRipple;
    private static LegacyGameInterfaceLayer? layer;
    private static FirstSeveranceFieldMaskLayout? fallbackMask;
    private static bool shaderFailed;

    internal static float Cover => seal.Fight != Guid.Empty ? seal.Presence : 0;

    internal static bool Backdrop(out SamuraiArenaBounds field, out SamuraiFieldLook look)
    {
        field = seal.Field;
        look = default;
        if (Main.dedServ || seal.Fight == Guid.Empty || seal.Presence <= 0) return false;
        look = Look(seal, outsider: false);
        return true;
    }

    public override void Load()
    {
        if (Main.dedServ) return;
        SkyManager.Instance[GhostSamuraiBattlefieldSky.Key] = sky = new GhostSamuraiBattlefieldSky();
        layer = new LegacyGameInterfaceLayer("Convergence: Samurai seal", DrawSealLayer, InterfaceScaleType.None);
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ || sky is null) return;
        if (Main.gameMenu) { Stop(); return; }
        if (!Main.gamePaused) Tick(Main.GameUpdateCount);
        bool want = seal.Fight != Guid.Empty && seal.Presence > 0;
        // Native Reset/DeactivateAll changes the CustomSky, not a cached flag here.
        if (sky.IsSceneRequested != want)
        {
            if (want) SkyManager.Instance.Activate(GhostSamuraiBattlefieldSky.Key, Vector2.Zero);
            else SkyManager.Instance.Deactivate(GhostSamuraiBattlefieldSky.Key);
        }
    }

    private static void Tick(ulong now)
    {
        Player player = Main.LocalPlayer;
        GhostSamuraiBoss? boss = Participant(player);
        if (boss is not null)
        {
            if (seal.Fight != boss.Fight)
            {
                // A fresh fight traces the seal from the summoner's feet; walking into a
                // running fight closes it quickly.
                seal = new Seal { Fight = boss.Fight, Field = boss.Arena, Since = now,
                    Trace = boss.VisualAge < 90 ? TraceTicks : QuickTraceTicks };
                rippleA = rippleB = default;
            }
            seal.ReleasedAt = 0;
            seal.Held = false;
            seal.Clock = boss.VisualAge / 60f;
            seal.Phase = boss.Phase switch { SamuraiPhase.Phase3 => 2, SamuraiPhase.Phase2 => 1, _ => 0 };
            float pulse = boss.TransitionRemaining > 0
                ? MathF.Sin(MathF.PI * (1 - boss.TransitionRemaining / (float)GhostSamuraiRules.TransitionTime)) : 0;
            seal.Surge = Math.Max(pulse, seal.Surge * .94f);
            seal.Presence = 1;   // the shader opens the field from the feet and pours the dark outward
            Contact(player, boss, now);
        }
        else if (seal.Fight != Guid.Empty)
        {
            if (seal.ReleasedAt == 0) seal.ReleasedAt = now;
            // Native NPC removal and the terminal snapshot need not share a tick: hold for a
            // short grace, and keep the field under the stage once the ending is accepted.
            if (!seal.Held && GhostSamuraiPresentation.Ending is { } staged && staged.Fight == seal.Fight
                && staged.Since + Grace >= seal.ReleasedAt && now - seal.ReleasedAt <= Grace)
            { seal.Held = true; seal.Hold = staged.SealHold; }
            seal.Clock += 1 / 60f;
            seal.Surge *= .94f;
            float ending = Ending(seal, now);
            seal.Presence = seal.Held ? 1 - Smooth((ending - .55f) / .45f) : 1 - ending;
            if (ending >= 1) seal = default;
        }

        // Watchers outside see the seal of a nearby running fight.
        GhostSamuraiBoss? near = seal.Fight == Guid.Empty ? Nearby(player) : null;
        if (near is not null)
        {
            if (watch.Fight != near.Fight) watch = new Seal { Fight = near.Fight, Field = near.Arena, Since = now, Trace = QuickTraceTicks };
            watch.Clock = near.VisualAge / 60f;
            watch.Presence = Math.Min(1, watch.Presence + 1 / 20f);
        }
        else if (watch.Fight != Guid.Empty)
        {
            watch.Clock += 1 / 60f;
            watch.Presence -= 1 / 20f;
            if (watch.Presence <= 0) watch = default;
        }
    }

    private static GhostSamuraiBoss? Participant(Player player)
    {
        if (player is not { active: true }) return null;
        var containment = player.GetModPlayer<GhostSamuraiContainmentPlayer>();
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is GhostSamuraiBoss boss && boss.Arena.IsValid && boss.Fight != Guid.Empty && boss.ProjectionFresh
                && (containment.BoundTo(boss) || (player.dead || player.ghost) && GhostSamuraiContainmentPlayer.FightActive(boss)
                    && boss.Arena.Contains(player.Center.X, player.Center.Y)))
                return boss;
        return null;
    }

    private static GhostSamuraiBoss? Nearby(Player player)
    {
        if (player is not { active: true }) return null;
        Vector2 view = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) / 2;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is GhostSamuraiBoss boss && boss.Arena.IsValid && boss.Fight != Guid.Empty && boss.ProjectionFresh
                && GhostSamuraiContainmentPlayer.FightActive(boss)
                && view.X > boss.Arena.Left - WatchMargin - Main.screenWidth / 2 && view.X < boss.Arena.Right + WatchMargin + Main.screenWidth / 2
                && view.Y > boss.Arena.Top - WatchMargin - Main.screenHeight / 2 && view.Y < boss.Arena.Bottom + WatchMargin + Main.screenHeight / 2)
                return boss;
        return null;
    }

    // Pressing against a wall or the roof sends a ripple through the seal at the contact point.
    private static void Contact(Player player, GhostSamuraiBoss boss, ulong now)
    {
        if (player.dead || player.ghost || !player.GetModPlayer<GhostSamuraiContainmentPlayer>().BoundTo(boss) || now - lastRipple < 18) return;
        var field = boss.Arena;
        bool left = player.position.X <= field.Left + 1 && (player.controlLeft || player.velocity.X < -.01f);
        bool right = player.position.X + player.width >= field.Right - 1 && (player.controlRight || player.velocity.X > .01f);
        bool roof = player.position.Y <= field.Top + 1 && (player.controlJump || player.controlUp || player.velocity.Y < -.01f);
        if (!left && !right && !roof) return;
        float x = left ? 0 : right ? SamuraiArenaBounds.Width : player.Center.X - field.Left;
        float y = roof ? 0 : player.Center.Y - field.Top;
        rippleB = rippleA;
        rippleA = new Vector3(x, y, now);
        lastRipple = now;
    }

    private static float Ending(in Seal s, ulong now)
    {
        if (s.ReleasedAt == 0) return 0;
        float since = now - s.ReleasedAt;
        return s.Held ? Math.Clamp((since - s.Hold) / BurnTicks, 0, 1) : Math.Clamp((since - Grace) / ReleaseTicks, 0, 1);
    }

    private static SamuraiFieldLook Look(in Seal s, bool outsider)
    {
        ulong now = Main.GameUpdateCount;
        Vector2 view = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) / 2;
        float fraction = GhostSamuraiPresentation.Fraction / 60f;
        return new SamuraiFieldLook
        {
            Clock = s.Clock + fraction,
            Reduced = GhostSamuraiRigArt.Reduced ? 1 : 0,
            Presence = Math.Clamp(s.Presence, 0, 1),
            Phase = s.Phase,
            Surge = s.Surge,
            Deploy = Math.Clamp((now - s.Since) / s.Trace, 0, 1),
            Ending = s.Held ? Ending(s, now) : 0,
            Outsider = outsider,
            Camera = view - new Vector2(s.Field.CenterX, s.Field.CenterY),
            Ripple0 = outsider ? default : Ripple(rippleA, now),
            Ripple1 = outsider ? default : Ripple(rippleB, now),
        };
    }

    private static Vector4 Ripple(Vector3 r, ulong now)
    {
        float age = r.Z <= 0 ? RippleLife : (now - r.Z) / 60f;
        return age >= RippleLife ? default : new Vector4(r.X, r.Y, age, 1);
    }

    private static float Smooth(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }

    public override void PostDrawTiles()
    {
        FriendlyWorldInk.BeneathForecasts(); // friendly weapon ink lies under every forecast
        screenReady = false;
        fallbackMask = null;
        if (Main.dedServ || Main.gameMenu) return;
        bool outsider = seal.Fight == Guid.Empty;
        Seal shown = outsider ? watch : seal;
        if (shown.Fight == Guid.Empty || shown.Presence <= 0) return;
        // Captured in the world pass: the same transform as the world, in physical viewport pixels.
        Matrix view = Main.GameViewMatrix.TransformationMatrix;
        var transform = System.Numerics.Matrix3x2.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y)
            * new System.Numerics.Matrix3x2(view.M11, view.M12, view.M21, view.M22, view.M41, view.M42);
        var field = shown.Field;
        screenField = SamuraiFieldRenderer.ScreenField(field.Left, field.Top, field.Right, field.Bottom, transform, out screenFlip);
        screenReady = true;
        var viewport = Main.instance.GraphicsDevice.Viewport;
        if (!outsider)
            fallbackMask = FirstSeveranceFieldMaskLayout.Capture(field.Left, field.Top, field.Right, field.Bottom,
                transform, viewport.Width, viewport.Height);
        if (shaderFailed) return;
        // The light inside the seal goes here, behind players, bodies and forecasts.
        var batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
        try
        {
            using (new WorldGraphicsScope(batch))
                SamuraiFieldRenderer.DrawRim(Main.instance.GraphicsDevice, screenField, screenFlip, Look(shown, outsider));
        }
        catch (Exception e) { Fail(e); }
        finally { batch.End(); }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (Main.dedServ || Main.gameMenu || layer is null || !screenReady) return;
        layers.Insert(0, layer);
    }

    private static bool DrawSealLayer()
    {
        if (!screenReady) return true;
        bool outsider = seal.Fight == Guid.Empty;
        Seal shown = outsider ? watch : seal;
        if (shown.Fight == Guid.Empty || shown.Presence <= 0) return true;
        if (!shaderFailed)
        {
            try
            {
                using (new WorldGraphicsScope(Main.spriteBatch))
                    SamuraiFieldRenderer.DrawSeal(Main.instance.GraphicsDevice, screenField, screenFlip, Look(shown, outsider));
                return true;
            }
            catch (Exception e) { Fail(e); }
        }
        // Without the shader the outside must still be covered: the old exact black mask.
        if (fallbackMask is { } mask)
        {
            Black(mask.Top); Black(mask.Bottom); Black(mask.Left); Black(mask.Right);
            static void Black(FirstSeveranceMaskRect r)
            {
                if (r.Width > 0 && r.Height > 0)
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Vector2(r.X, r.Y), new Rectangle(0, 0, 1, 1),
                        Color.Black, 0, Vector2.Zero, new Vector2(r.Width, r.Height), SpriteEffects.None, 0);
            }
        }
        return true;
    }

    private static void Fail(Exception e)
    {
        if (shaderFailed) return;
        shaderFailed = true;
        global::Convergence.ConvergenceMod.Instance.Logger.Warn($"Ghost Samurai seal shader unavailable; using the black field mask. {e.Message}");
    }

    public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
    {
        // Daylight is capped at a cool moonlight inside the seal, so daylit terrain does not cut out
        // of the night field; night stays as dark as it already is.
        if (Main.dedServ || Main.gameMenu || seal.Fight == Guid.Empty || seal.Presence <= 0) return;
        Vector3 cap = Vector3.Lerp(Vector3.One, new Vector3(176, 166, 214) / 255f, seal.Presence * (GhostSamuraiRigArt.Reduced ? .6f : 1f));
        tileColor = new Color(Vector3.Min(tileColor.ToVector3(), cap));
    }

    private static void Stop()
    {
        seal = watch = default;
        screenReady = false;
        rippleA = rippleB = default;
        if (sky is { IsSceneRequested: true }) SkyManager.Instance.Deactivate(GhostSamuraiBattlefieldSky.Key);
        sky?.Reset();
    }
    public override void ClearWorld() => Stop();
    public override void OnWorldUnload() => Stop();
    public override void Unload() { Stop(); sky = null; layer = null; }
}
