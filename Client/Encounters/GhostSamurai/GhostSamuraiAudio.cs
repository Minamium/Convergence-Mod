using Convergence.Content.Encounters.GhostSamurai;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

internal sealed class GhostSamuraiAudio : ModSystem
{
    // Built-in references only; replacing either style changes every related cue.
    internal static readonly SoundStyle SlashSound = SoundID.Item1 with { Volume = .9f, Pitch = .25f, MaxInstances = 6 };
    internal static readonly SoundStyle DashShoutSound = SoundID.ScaryScream with { Volume = .9f, Pitch = .2f, MaxInstances = 3 };
    private readonly SamuraiCueClock cues = new();

    public override void PostUpdateEverything()
    {
        if (Main.dedServ) return;
        GhostSamuraiBoss boss = null;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is GhostSamuraiBoss found) { boss = found; break; }
        if (boss is null) { cues.Clear(); return; }
        cues.Advance(boss.Fight, (int)boss.VisualAge);
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not GhostSamuraiAttackProjectile p
                || p.Fight != boss.Fight || p.BossSlot != boss.NPC.whoAmI) continue;
            var h = p.Hazard;
            if (h.Shape == SamuraiShape.Wisp) continue;
            if (h.Shape == SamuraiShape.SlashWave)
                for (int warning = 0; warning < 3; warning++)
                    if (cues.Try(2, h.Born + warning * (h.Fire - h.Born) / 3))
                        SoundEngine.PlaySound(SoundID.Item4 with { Volume = .7f, Pitch = warning * .15f }, boss.NPC.Center);
            if (h.Shape == SamuraiShape.RushVisual && p.SlashAim.Locked
                && cues.Try(1, h.Fire - GhostSamuraiRules.DashShoutDelay))
                SoundEngine.PlaySound(DashShoutSound, boss.NPC.Center);
            if ((!h.HasAim || p.SlashAim.Locked) && cues.Try(0, h.Fire))
                SoundEngine.PlaySound(SlashSound, boss.NPC.Center);
        }
    }

    public override void OnWorldUnload() => cues.Clear();
    public override void Unload() => cues.Clear();
}

