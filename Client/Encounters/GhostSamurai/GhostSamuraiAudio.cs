using Convergence.Content.Encounters.GhostSamurai;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

internal sealed class GhostSamuraiAudio : ModSystem
{
    // Dedicated cues layered from CC0 recordings by tools/remix_samurai_sfx.py.
    // Each plays on the same accepted tick as the borrowed vanilla sound it
    // replaced; constructing a SoundStyle loads nothing on a dedicated server.
    private const string Root = "Convergence/Assets/Sounds/GhostSamurai/";
    internal static readonly SoundStyle SlashSound = Cue("SamuraiSlash", .9f, .08f, 6);
    internal static readonly SoundStyle GridSound = Cue("SamuraiGrid", .95f, .04f, 2);
    internal static readonly SoundStyle WindSound = Cue("SamuraiWind", .9f, .06f, 3);
    internal static readonly SoundStyle RushSound = Cue("SamuraiRush", .9f, .05f, 3);
    internal static readonly SoundStyle ChargedSwingSound = Cue("SamuraiWave", 1f, .05f, 3);
    internal static readonly SoundStyle CleaveSound = Cue("SamuraiCleave", 1f, 0, 2);
    internal static readonly SoundStyle ShockSound = Cue("SamuraiShock", .8f, .04f, 2);
    internal static readonly SoundStyle DashShoutSound = Cue("SamuraiShout", .9f, .04f, 3);
    internal static readonly SoundStyle ChimeSound = Cue("SamuraiChime", .7f, 0, 4);
    private readonly SamuraiCueClock cues = new();

    private static SoundStyle Cue(string name, float volume, float pitchVariance, int instances) => new(Root + name)
    {
        Volume = volume,
        PitchVariance = pitchVariance,
        MaxInstances = instances,
        SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
    };

    public override void PostUpdateEverything()
    {
        if (Main.dedServ) return;
        GhostSamuraiBoss boss = null;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is GhostSamuraiBoss found) { boss = found; break; }
        if (boss is null || !GhostSamuraiContainmentPlayer.FightActive(boss)) { cues.Clear(); return; }
        cues.Advance(boss.Fight, (int)boss.VisualAge);
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not GhostSamuraiAttackProjectile p
                || p.Fight != boss.Fight || p.BossSlot != boss.NPC.whoAmI) continue;
            var h = p.DisplayHazard;
            if (h.Shape == SamuraiShape.Wisp) continue;
            if (h.Shape == SamuraiShape.SlashWave)
                for (int warning = 0; warning < 3; warning++)
                    if (cues.Try(2, p.Hazard.Born + warning * (p.Hazard.Fire - p.Hazard.Born) / 3))
                        SoundEngine.PlaySound(ChimeSound with { Pitch = warning * .15f }, boss.NPC.Center);
            if (h.Shape == SamuraiShape.FrontalCleave)
            {
                if (cues.Try(4, h.Born)) SoundEngine.PlaySound(ChimeSound with { Volume = .65f, Pitch = -.25f }, boss.NPC.Center);
                if (p.SlashAim.Locked && cues.Try(5, p.SlashAim.LockTick))
                    SoundEngine.PlaySound(ChimeSound with { Volume = .8f, Pitch = .3f }, boss.NPC.Center);
                if (cues.Try(7, h.Born + SamuraiComboRules.HorizontalSlashChargeTime - SamuraiComboRules.HorizontalSlashHoldTime))
                    SoundEngine.PlaySound(ChimeSound with { Volume = .65f, Pitch = .5f }, boss.NPC.Center);
            }
            if (h.Shape == SamuraiShape.GroundShockwave)
            {
                if (cues.Try(6, h.Fire)) SoundEngine.PlaySound(ShockSound, boss.NPC.Center);
                continue; // Two fronts share one sound; never a second sword swing.
            }
            if (h.Shape == SamuraiShape.RushVisual
                && cues.Try(1, h.Fire - GhostSamuraiRules.DashShoutDelay))
                SoundEngine.PlaySound(DashShoutSound, boss.NPC.Center);
            bool heavy = h.Shape is SamuraiShape.SlashWave or SamuraiShape.FrontalCleave;
            if ((!h.HasAim || p.SlashAim.Locked) && cues.Try(heavy ? 3 : 0, h.Fire))
                SoundEngine.PlaySound(Strike(h, boss), boss.NPC.Center);
        }
    }

    // One cue per accepted strike tick: the thirty grid lines share one tear.
    private static SoundStyle Strike(in SamuraiHazard h, GhostSamuraiBoss boss) => h.Shape switch
    {
        SamuraiShape.SlashWave => ChargedSwingSound,
        SamuraiShape.FrontalCleave => CleaveSound,
        SamuraiShape.RushVisual => RushSound,
        SamuraiShape.InnerKamaitachi or SamuraiShape.OuterKamaitachi => WindSound,
        SamuraiShape.Slash when boss.Attack == SamuraiAttack.GridSlash => GridSound,
        _ => SlashSound,
    };

    public override void OnWorldUnload() => cues.Clear();
    public override void Unload() => cues.Clear();
}
