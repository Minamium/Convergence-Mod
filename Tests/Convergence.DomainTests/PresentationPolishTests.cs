using Convergence.Client.Encounters.FirstSeverance;

namespace Convergence.DomainTests;
internal static partial class Program
{
    [DomainTest("Presentation polish: intro names hold fully visible without extending combat")]
    private static void IntroNameHold()
    {
        AssertEqual(0f, FirstSeverancePresentationTiming.TitleOpacity(1389,1000,1600), "no early title");
        AssertEqual(1f, FirstSeverancePresentationTiming.TitleOpacity(1414,1000,1600), "arrival complete");
        AssertEqual(1f, FirstSeverancePresentationTiming.TitleOpacity(1576,1000,1600), "162-tick opaque hold");
        AssertEqual(0f, FirstSeverancePresentationTiming.TitleOpacity(1600,1000,1600), "end-exclusive");
        AssertEqual(0f, FirstSeverancePresentationTiming.TitleOpacity(1700,1000,1600), "late peer never replays");
        AssertEqual(0f, FirstSeverancePresentationTiming.TitleOpacity(1200,1200,1100), "invalid duration");
    }

    [DomainTest("Presentation polish: mechanic cues step down and beam/other cues step up")]
    private static void RaidCueGroups()
    {
        foreach(string cue in new[]{"StackSummon","ShellMassLatch","ShellMassArc","ShellMassShed","ShellMassCollapse"})
            AssertEqual(.44f,FirstSeverancePresentationTiming.CueGain(cue,1),cue);
        foreach(string cue in new[]{"SpreadExecution","SpreadDissolve","MechanicTick"})
            AssertEqual(.65f,FirstSeverancePresentationTiming.CueGain(cue,1),cue);
        AssertEqual(.88f,FirstSeverancePresentationTiming.CueGain("LanceFire",1),"beam");
        AssertEqual(.92f,FirstSeverancePresentationTiming.CueGain("CoreExposure",1),"other");
        AssertEqual(.80f,FirstSeverancePresentationTiming.CueGain("PrismBeamSustain",1),"bounded bed");
        AssertEqual(1f,FirstSeverancePresentationTiming.CueGain("CrushCataclysm",1.25f),"device ceiling");
    }

    [DomainTest("Presentation polish: only beam releases cross an action deadline with bounded decay")]
    private static void BeamReleaseEnvelope()
    {
        AssertEqual(18,FirstSeverancePresentationTiming.BeamReleaseTicks("LanceFire"),"short residual");
        foreach(string cue in new[]{"LanceCharge","ShellMassShed","PrismBeamSustain"})
            AssertEqual(0,FirstSeverancePresentationTiming.BeamReleaseTicks(cue),"no warning/loop tail");
        AssertEqual(1f,FirstSeverancePresentationTiming.VoiceFade(1100,1124,18),"continuous departure");
        AssertEqual(.5f,FirstSeverancePresentationTiming.VoiceFade(1112,1124,18),"smooth halfway fade");
        AssertEqual(0f,FirstSeverancePresentationTiming.VoiceFade(1124,1124,18),"bounded stop");
        AssertEqual(0f,FirstSeverancePresentationTiming.VoiceFade(1200,1124,18),"no unsigned wrap");
    }
}
