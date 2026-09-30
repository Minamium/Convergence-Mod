#nullable enable
using System;
using System.Collections.Generic;
using Convergence.Client.Encounters.FirstSeverance;
using Convergence.Content.Items.DXOboro;
using Convergence.Content.Items.Oboro;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Convergence.Client.Weapons;

// Owns Soboro's pixel layer. Records are bounded per-cut snapshots of accepted
// projectile state, so a cut's torn residue can outlive its projectile without
// extending any gameplay lifetime. The half-resolution target is drawn after
// the camera is final (CheckMonoliths, as Luminance's own pixelation does) and
// composited once in the projectile layer. Client-only; nothing here touches
// hits, input, packets or saved state.
[Autoload(Side = ModSide.Client)]
internal sealed class SoboroSlashLayer : ModSystem
{
    private sealed class Record
    {
        internal int Owner, Identity, Step, AgeTick, Seed, Facing;
        internal float Aim;
        internal Vector2 HandCenter, HandAlong, HandAcross;
        internal ulong LastTick;
        internal bool Alive;
        internal bool HasImpact;
        internal Vector2 ImpactAt;
        internal ulong ImpactTick;

        internal SoboroCutPose Pose => new(Step, Aim, Facing, HandCenter, HandAlong, HandAcross, Seed);
    }

    private readonly record struct DeviceState(BlendState Blend, DepthStencilState Depth, RasterizerState Raster,
        Rectangle Scissor, SamplerState Sampler0, SamplerState Sampler1, Texture? Texture0, Texture? Texture1,
        VertexBufferBinding[] Vertices, IndexBuffer? Indices)
    {
        internal static DeviceState Capture(GraphicsDevice device) => new(device.BlendState, device.DepthStencilState,
            device.RasterizerState, device.ScissorRectangle, device.SamplerStates[0], device.SamplerStates[1],
            device.Textures[0], device.Textures[1], device.GetVertexBuffers(), device.Indices);

        internal void Restore(GraphicsDevice device)
        {
            device.Textures[0] = Texture0; device.Textures[1] = Texture1;
            device.SamplerStates[0] = Sampler0; device.SamplerStates[1] = Sampler1;
            device.SetVertexBuffers(Vertices); device.Indices = Indices;
            device.BlendState = Blend; device.DepthStencilState = Depth;
            device.RasterizerState = Raster; device.ScissorRectangle = Scissor;
        }
    }

    private const int MaximumRecords = 16;
    private static readonly List<Record> records = new();
    private static ManagedRenderTarget? target;
    private static bool prepared, disabled;
    private static Rectangle bounds;

    public override void Load()
    {
        On_Main.CheckMonoliths += RenderLayer;
        On_Main.DrawProjectiles += CompositeLayer;
    }

    public override void Unload()
    {
        On_Main.CheckMonoliths -= RenderLayer;
        On_Main.DrawProjectiles -= CompositeLayer;
        records.Clear();
        prepared = false;
        ManagedRenderTarget? old = target;
        target = null;
        if (old is not null) Main.QueueMainThreadAction(old.Dispose);
    }

    public override void OnWorldUnload()
    {
        records.Clear();
        prepared = false;
    }

    internal static void Track(Projectile projectile, DXOboroCut cut, Player owner)
    {
        Record? record = FindAlive(projectile);
        if (record is null || record.Step != cut.Step || record.AgeTick > cut.Age)
        {
            if (record is not null) record.Alive = false;
            if (records.Count >= MaximumRecords) RemoveOldest();
            record = new Record { Owner = projectile.owner, Identity = projectile.identity };
            records.Add(record);
        }
        var basis = OboroHandAnchor.Capture(owner, cut.Facing);
        record.Step = cut.Step;
        record.Aim = cut.Aim;
        record.Facing = cut.Facing;
        record.Seed = projectile.identity * 7 + cut.Step * 131 + projectile.owner * 977;
        record.HandCenter = owner.MountedCenter + new Vector2(basis.X, basis.Y);
        record.HandAlong = new Vector2(basis.AlongX, basis.AlongY);
        record.HandAcross = new Vector2(basis.AcrossX, basis.AcrossY);
        record.AgeTick = cut.Age;
        record.LastTick = Main.GameUpdateCount;
        record.Alive = true;
    }

    internal static void Impact(Projectile projectile, Vector2 at)
    {
        Record? record = FindAlive(projectile);
        if (record is null) return;
        record.HasImpact = true;
        record.ImpactAt = at;
        record.ImpactTick = Main.GameUpdateCount;
    }

    internal static void Release(Projectile projectile)
    {
        Record? record = FindAlive(projectile);
        if (record is not null) record.Alive = false;
    }

    private static Record? FindAlive(Projectile projectile)
    {
        for (int i = records.Count - 1; i >= 0; i--)
        {
            Record record = records[i];
            if (record.Alive && record.Owner == projectile.owner && record.Identity == projectile.identity)
                return record;
        }
        return null;
    }

    private static void RemoveOldest()
    {
        int oldest = 0;
        for (int i = 1; i < records.Count; i++)
            if (records[i].LastTick < records[oldest].LastTick) oldest = i;
        records.RemoveAt(oldest);
    }

    private static void RenderLayer(On_Main.orig_CheckMonoliths orig)
    {
        prepared = false;
        if (!disabled && !Main.gameMenu && records.Count > 0)
        {
            try
            {
                Render();
            }
            catch (Exception exception)
            {
                disabled = true;
                ConvergenceMod.Instance.Logger.Warn($"Soboro pixel slash layer disabled; the blade still draws: {exception}");
            }
        }
        orig();
    }

    private static void Render()
    {
        ulong now = Main.GameUpdateCount;
        float fraction = WeaponDrawClock.Fraction;
        bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
        Rectangle screen = new(0, 0, Main.screenWidth, Main.screenHeight);
        Rectangle area = Rectangle.Empty;
        int visible = 0;
        for (int i = records.Count - 1; i >= 0; i--)
        {
            Record record = records[i];
            int since = (int)Math.Min(now - record.LastTick, 120UL);
            // A projectile that stopped updating (killed, culled or desynced) leaves residue.
            if (record.Alive && since > 2) record.Alive = false;
            float age = SoboroSlashArt.DrawAge(record.Step, record.AgeTick, fraction, since, record.Alive && since == 0);
            if (!Main.player[record.Owner].active || age >= DXOboroMotion.LiveEnd(record.Step) + SoboroSlashArt.ResidueTicks)
            {
                records.RemoveAt(i);
                continue;
            }
            if (!SoboroSlashArt.Visible(record.Step, age)) continue;
            float extent = SoboroSlashArt.Extent(record.Step);
            Vector2 center = record.HandCenter - Main.screenPosition;
            Rectangle box = new((int)(center.X - extent), (int)(center.Y - extent), (int)(extent * 2), (int)(extent * 2));
            if (!box.Intersects(screen)) continue;
            area = visible == 0 ? box : Rectangle.Union(area, box);
            visible++;
        }
        if (visible == 0) return;

        GraphicsDevice device = Main.instance.GraphicsDevice;
        target ??= new ManagedRenderTarget(true, (width, height) =>
            new RenderTarget2D(Main.instance.GraphicsDevice, Math.Max(1, width / 2), Math.Max(1, height / 2)));
        RenderTarget2D texture = target.Target;
        RenderTargetBinding[] oldTargets = device.GetRenderTargets();
        DeviceState state = DeviceState.Capture(device);
        try
        {
            device.SetRenderTarget(texture);
            device.Clear(Color.Transparent);
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, texture.Width, texture.Height, 0, -1, 1);
            foreach (Record record in records)
            {
                float age = AgeOf(record, now, fraction);
                if (SoboroSlashArt.Visible(record.Step, age))
                    SoboroSlashArt.DrawCrescent(device, record.Pose, age, Main.screenPosition, projection, reduced);
            }
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            foreach (Record record in records)
            {
                float age = AgeOf(record, now, fraction);
                if (!SoboroSlashArt.Visible(record.Step, age)) continue;
                float impactAge = record.HasImpact ? now - record.ImpactTick + fraction : -1f;
                SoboroSlashArt.DrawAccents(Main.spriteBatch, pixel, record.Pose, age, Main.screenPosition, reduced,
                    record.HasImpact, record.ImpactAt, impactAge);
            }
            Main.spriteBatch.End();
        }
        finally
        {
            device.SetRenderTargets(oldTargets);
            state.Restore(device);
        }
        bounds = Rectangle.Intersect(area, screen);
        prepared = bounds.Width > 0 && bounds.Height > 0;
    }

    private static float AgeOf(Record record, ulong now, float fraction)
    {
        int since = (int)Math.Min(now - record.LastTick, 120UL);
        return SoboroSlashArt.DrawAge(record.Step, record.AgeTick, fraction, since, record.Alive && since == 0);
    }

    private static void CompositeLayer(On_Main.orig_DrawProjectiles orig, Main self)
    {
        orig(self);
        if (!prepared || disabled || target is null) return;
        GraphicsDevice device = Main.instance.GraphicsDevice;
        DeviceState state = DeviceState.Capture(device);
        try
        {
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            bool reduced = ModContent.GetInstance<FirstSeveranceVisualConfig>().ReducedEffects;
            SoboroSlashArt.Composite(device, target.Target, bounds, Main.GameViewMatrix.TransformationMatrix, reduced);
        }
        catch (Exception exception)
        {
            disabled = true;
            ConvergenceMod.Instance.Logger.Warn($"Soboro pixel slash composite disabled; the blade still draws: {exception}");
        }
        finally
        {
            state.Restore(device);
        }
    }
}
