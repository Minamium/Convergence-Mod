#nullable enable
using System;
using Convergence.Content.Encounters.GhostSamurai;
using Terraria;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.GhostSamurai;

// 紫電の亡霊武者 / Violet Phantom Blade: one original looping OGG (native
// LOOPSTART/LOOPEND) for the players fighting inside the seal, including a
// fallen player still watching from inside it. Only this track's fade is shaped:
// a short entry ramp so the opening temple bell is heard, and a fade under the
// victory or defeat stage. Music never drives authority; a late joiner starts the
// track from its top.
[Autoload(Side = ModSide.Client)]
internal sealed class GhostSamuraiMusicScene : ModSceneEffect
{
    internal const string Track = "Assets/Music/GhostSamurai/VioletPhantomBlade";
    private static ulong heardAt;
    public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
    public override int Music => MusicLoader.GetMusicSlot(Mod, Track);

    internal static GhostSamuraiBoss? Listening(Player player)
    {
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is GhostSamuraiBoss boss && GhostSamuraiContainmentPlayer.FightActive(boss)
                && (player.GetModPlayer<GhostSamuraiContainmentPlayer>().BoundTo(boss)
                    || (player.dead || player.ghost) && boss.Arena.IsValid && boss.Arena.Contains(player.Center.X, player.Center.Y)))
                return boss;
        return null;
    }

    public override bool IsSceneEffectActive(Player player)
    {
        if (Main.gameMenu) return false;
        if (Listening(player) is not null)
        {
            if (player.whoAmI == Main.myPlayer) heardAt = Main.GameUpdateCount;
            return true;
        }
        // Hold the track under the ending's stage, only for whoever was hearing it.
        return GhostSamuraiPresentation.Ending is { } ending && heardAt + 2 >= ending.Since
            && Main.GameUpdateCount - ending.Since < (ulong)ending.Duration;
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class GhostSamuraiMusicFade : ModSystem
{
    private const float EntryTicks = 20, EntryWindow = 150;

    public override void PostUpdateEverything()
    {
        if (Main.dedServ || Main.gameMenu || Main.gamePaused) return;
        int slot = MusicLoader.GetMusicSlot(Mod, GhostSamuraiMusicScene.Track);
        if (slot <= 0 || slot >= Main.musicFade.Length) return;
        if (GhostSamuraiPresentation.Ending is { } ending)
        {
            float t = (Main.GameUpdateCount - ending.Since) / (float)ending.Duration;
            Main.musicFade[slot] = Math.Min(Main.musicFade[slot], 1 - Ease(t));
            return;
        }
        // Native music fades a new track in over seconds; the first bars carry the
        // summoning bell, so a fresh fight lifts our own slot quickly instead.
        if (Main.curMusic == slot && GhostSamuraiMusicScene.Listening(Main.LocalPlayer) is { } boss && boss.VisualAge < EntryWindow)
            Main.musicFade[slot] = Math.Max(Main.musicFade[slot], Ease(boss.VisualAge / EntryTicks));
    }

    private static float Ease(float x) { x = Math.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
}
