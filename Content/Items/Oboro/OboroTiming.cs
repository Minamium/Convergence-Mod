namespace Convergence.Content.Items.Oboro;

internal enum OboroToggle { Rejected, Started, Detonate }
internal enum OboroAdvance { None, NextStep, Finished }

// Server-owned timing, independent of client animation and Terraria globals.
internal sealed class OboroTiming
{
    internal int Age { get; private set; }
    internal int Duration { get; private set; }
    internal int Step { get; private set; }
    internal int Zanshin { get; private set; }
    internal uint Serial { get; private set; }
    private bool inputHeld;
    private ulong heldAt, toggleAfter;
    internal void SetHeld(bool held, ulong now) { inputHeld = held; heldAt = now; }
    internal bool HeldAt(ulong now) => inputHeld && now >= heldAt && now - heldAt < OboroRules.HoldTimeoutTicks;
    internal bool TryBegin(ulong now, float speed)
    {
        if (Duration > 0) return false;
        Step = 0;
        BeginStep(speed);
        return true;
    }
    private void BeginStep(float speed)
    {
        Duration = OboroRules.Duration(Step, speed);
        Age = 0;
        Serial++;
    }
    // 1 tickにつき1回、サーバーだけが呼ぶ。終了tickに次段のtimer=0へ移る。
    internal OboroAdvance AdvanceSwing(ulong now, float speed)
    {
        if (Duration == 0 || ++Age < Duration) return OboroAdvance.None;
        if (HeldAt(now))
        {
            Step = (Step + 1) % OboroComboSettings.Count;
            BeginStep(speed);
            return OboroAdvance.NextStep;
        }
        CancelSwing();
        return OboroAdvance.Finished;
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
    internal void CancelSwing() { Age = Duration = Step = 0; inputHeld = false; heldAt = 0; }
    internal void Clear() { CancelSwing(); Zanshin = 0; toggleAfter = 0; }
    internal void Initialize() { Clear(); Step = 0; Serial = 0; }
}
