using Convergence.Client.Encounters.FirstSeverance;
using Convergence.Content.Encounters.FirstSeverance;

namespace Convergence.DomainTests;
internal static partial class Program
{
    [DomainTest("Doll mannerisms visit twelve aligned cels with irregular holds and bounded dialogue")]
    private static void DollMannerisms()
    {
        var seen=new bool[12];
        for(ulong tick=0;tick<2760;tick++) {
            int frame=FirstSeveranceDollMannerisms.Frame(tick,false);seen[frame]=true;
            AssertEqual(frame,FirstSeveranceDollMannerisms.Frame(tick+1380,false),"stable simulation clock cycle");
            AssertEqual(true,FirstSeveranceDollMannerisms.Frame(tick,true) is >=0 and <12,"dialogue frame bounds");
        }
        foreach(bool used in seen) AssertEqual(true,used,"each authored expression/gesture is reachable");
    }
    [DomainTest("Music transition hands off before combat and silences only the outgoing phase envelope")]
    private static void MusicTransitionTiming()
    {
        foreach(var phase in new[]{FirstSeveranceBossPhase.Unbound,FirstSeveranceBossPhase.Distant,FirstSeveranceBossPhase.Final}) {
            AssertEqual(FirstSeveranceMusicTimeline.Previous(phase),FirstSeveranceMusicTimeline.Select(phase,true,1000,1360,1179),"old track during first half");
            AssertEqual(phase,FirstSeveranceMusicTimeline.Select(phase,true,1000,1360,1180),"next track at transition midpoint");
            AssertEqual(0f,FirstSeveranceMusicTimeline.OutgoingCeiling(1000,1360,1270),"old track silent 1.5 seconds before next combat");
            AssertEqual(phase,FirstSeveranceMusicTimeline.Select(phase,false,1000,1360,1000),"ordinary combat never holds old track");
        }
        AssertEqual(0f,FirstSeveranceMusicTimeline.OutgoingCeiling(1000,1360,9999),"late snapshot never replays old music");
    }
}
