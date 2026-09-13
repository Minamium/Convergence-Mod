using System.Collections.Generic;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace Convergence.Client.Encounters.FirstSeverance;

// Bounded client evidence: a successful asset export is not evidence that a
// gameplay hook actually fired or that the mixer accepted a voice.
[Autoload(Side = ModSide.Client)]
public sealed class RitualAudioDiagnostics : ModSystem
{
    private readonly Dictionary<string, int> counts = new();
    private readonly List<(SlotId Voice, string Cue, ulong Due, float Gain)> checks = new();
    internal static void Track(string cue, SlotId voice, float gain)
    {
        if (Main.dedServ || Main.gameMenu) return;
        var audit = ModContent.GetInstance<RitualAudioDiagnostics>();
        audit.counts.TryGetValue(cue, out int count);
        if (count >= 3 || audit.checks.Count >= 32) return;
        audit.counts[cue] = count + 1;
        audit.checks.Add((voice, cue, Main.GameUpdateCount + 2, gain));
    }
    public override void PostUpdateEverything()
    {
        for (int i = checks.Count - 1; i >= 0; i--)
        {
            var check = checks[i];
            if (Main.GameUpdateCount < check.Due) continue;
            bool playing = SoundEngine.TryGetActiveSound(check.Voice, out var sound) && sound.IsPlaying;
            Mod.Logger.Info($"RitualWeapon event=AudioVoice cue={check.Cue} playing={playing} gain={check.Gain:F3} slider={Main.soundVolume:F3} focused={Main.instance.IsActive}");
            checks.RemoveAt(i);
        }
    }
    public override void OnWorldUnload() { checks.Clear(); counts.Clear(); }
    public override void Unload() { checks.Clear(); counts.Clear(); }
}
