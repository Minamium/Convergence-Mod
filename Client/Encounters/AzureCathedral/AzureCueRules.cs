using System;
using Convergence.Content.Encounters.AzureCathedral;

namespace Convergence.Client.Encounters.AzureCathedral;

// Accepted-clock moments for the sound effects and the rush condition shared by
// the audio and the drawn rush forecast. No Terraria types: linked into the domain tests.
internal static class AzureCueRules
{
    // Authored lead times: each cue's peak lands on the picture event it belongs to.
    internal const int PrisonBreakLead = 24, SwordLightLead = 76, RiftOpenLead = 60, DevourRushLead = 118;
    internal const int RiftVisual = 540; // Rift portal opens in AzureCeremony.Stage; the "tear" ends here
    internal const int PrisonBreak = AzureRules.IceBreak - PrisonBreakLead;
    internal const int SwordLight = AzureRules.SwordLight - SwordLightLead;
    internal const int RiftOpen = RiftVisual - RiftOpenLead;
    internal const int WormArrival = AzureRules.WormArrival;
    internal const int DevourRush = AzureRules.DevourContact - DevourRushLead;
    // Fixed dash geometry: the head travels from the entrance to contact for this long.
    internal const int RushFollow = AzureRules.ChargeEnd - AzureRules.ChargeWarning;
    internal const int ChorusTickFirst = 180, ChorusTickStep = 60, ChorusTickCount = 3;
    internal const int LatticeEndDelay = 6;
    internal const float RushEntranceReach = 1280;

    // Mirrors AzureRuntime.Move(): Devouring and a Duet staging hold the worm still,
    // otherwise a charge phrase (or an enraged chorus phrase) sends it at a player.
    internal static bool RushActive(in AzureState s, int age)
    {
        if (!s.Live || s.WormLife <= 0 || s.Phase == AzurePhase.Duet && s.StagingAt >= 0) return false;
        int phrase = AzureRules.Phrase(age, s.AttackEpoch);
        return AzureRules.ChargePhrase(phrase) || s.Enraged && AzureRules.ChorusPhrase(phrase);
    }
    internal static int RushSerial(int age, int epoch) => Math.Max(0, age - epoch) / AzureRules.ChargeTicks;
    internal static int RushSide(int serial) => serial % 2 == 0 ? -1 : 1;
    internal static int RushWarnTick(int epoch, int serial) => epoch + serial * AzureRules.ChargeTicks;
    internal static int ChorusTickAt(int fire, int n) => fire - ChorusTickFirst + n * ChorusTickStep;
    // Index of a lattice line from its plan (0 = first). -1 for an ordinary single cut.
    internal static int LatticeIndex(int born, int fire)
        => fire - born < AzureLattice.Warning ? -1 : (fire - born - AzureLattice.Warning) / AzureLattice.Stagger;
    internal static bool IsLatticeLine(int born, int fire) => fire - born >= AzureLattice.Warning;
}
