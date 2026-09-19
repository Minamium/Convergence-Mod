namespace Convergence.Content.Items.Oboro;

internal enum OboroToggle { Rejected, Started, Detonate }

// Server-owned timing, independent of client animation and Terraria globals.
internal sealed class OboroTiming
{
    internal int Age { get; private set; }
    internal int Duration { get; private set; }
    internal int Step { get; private set; }
    internal int Zanshin { get; private set; }
    internal uint Serial { get; private set; }
    private int nextStep;
    private ulong endAt, toggleAfter;
    internal bool TryBegin(ulong now, float speed)
    {
        if (Duration > 0) return false;
        if (now < endAt || now - endAt > OboroRules.ComboResetTicks) nextStep = 0;
        Step = nextStep; nextStep = (nextStep + 1) % OboroComboSettings.Count;
        Duration = OboroRules.Duration(Step, speed); Age = 0; Serial++;
        return true;
    }
    internal bool AdvanceSwing(ulong now)
    {
        if (Duration == 0 || ++Age < Duration) return false;
        Age = Duration = 0; endAt = now; return true;
    }
    internal OboroToggle Toggle(ulong now)
    {
        if (now < toggleAfter) return OboroToggle.Rejected;
        toggleAfter = now + 18;
        if (Zanshin > 0) { Zanshin = 0; return OboroToggle.Detonate; }
        Zanshin = OboroRules.ZanshinTicks; return OboroToggle.Started;
    }
    internal bool TickZanshin() => Zanshin > 0 && --Zanshin == 0;
    internal void EndZanshin() => Zanshin = 0;
    internal void CancelSwing() { Age = Duration = nextStep = 0; }
    internal void Clear() { CancelSwing(); Zanshin = 0; toggleAfter = endAt = 0; }
    internal void Initialize() { Clear(); Step = 0; Serial = 0; }
}
