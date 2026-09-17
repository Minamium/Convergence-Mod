"""One-time, assertion-guarded integration; removed after checkpointing."""
from pathlib import Path
import re
ROOT = Path(__file__).resolve().parents[1]
C = 'Content/Encounters/CrimsonFoundry/'
V = 'Client/Encounters/CrimsonFoundry/'

def read(path): return (ROOT / path).read_text(encoding='utf-8-sig')
def write(path, text): (ROOT / path).write_text(text, encoding='utf-8', newline='\n')
def replace(path, old, new, count=1):
    s = read(path)
    if s.count(old) != count:
        raise RuntimeError(f'{path}: expected {count} instances of {old!r}, found {s.count(old)}')
    write(path, s.replace(old, new))

replace(C+'CrimsonInvocation.cs', '(12000000 + 8000000 * (Math.Clamp(members, 1, 8) - 1)) / 4', 'CrimsonPlaytestTuning.TargetLife(members)')
replace(C+'CrimsonState.cs', 'bool PerformerDefeated = false)', 'bool PerformerDefeated = false, int CompletedCycles = 0)')
replace(C+'CrimsonState.cs', '    internal int BarLife =>', '''    internal int DamageFloor(int source) => Phase < 3 ? CrimsonPhaseRules.RetreatLife(TargetLife) : CompletedCycles == 0 ? 1 : 0;
    internal bool HeldAtFloor(int source) => LifeFor(source) <= DamageFloor(source);
    internal int BarLife =>''')
replace(C+'CrimsonState.cs', '        if (GroundX != old.GroundX', '        if (Phase == old.Phase && CompletedCycles < old.CompletedCycles) return false;\n        if (GroundX != old.GroundX')
replace(C+'CrimsonState.cs', 'w.Write(PerformerDefeated);', 'w.Write(PerformerDefeated); w.Write(CompletedCycles);')
replace(C+'CrimsonState.cs', 'bool dead = r.ReadBoolean(); int count = r.ReadByte();', 'bool dead = r.ReadBoolean(); int cycles = r.ReadInt32(); int count = r.ReadByte();')
replace(C+'CrimsonState.cs', '|| maximum is < 1', '|| cycles is < 0 or > 1000\n            || maximum is < 1')
replace(C+'CrimsonState.cs', 'maximum, a, b, c, d, dead);', 'maximum, a, b, c, d, dead, cycles);')
replace(C+'CrimsonRuntime.cs', 'internal sealed class CrimsonRuntime', 'internal sealed partial class CrimsonRuntime')
replace(C+'CrimsonRuntime.cs', '    private readonly int[] techniqueCursor', '    private readonly CrimsonActCycle cycle = new();\n    private bool thresholdLatched;\n    private readonly int[] techniqueCursor')
replace(C+'CrimsonRuntime.cs', 'Life(2), Life(3), performerDefeated);', 'Life(2), Life(3), performerDefeated, cycle.Completed);')
replace(C+'CrimsonRuntime.cs', 'if (!Matches(value) || !State.Vulnerable(age))', 'if (!Matches(value) || cycle.Completed == 0 || !State.Vulnerable(age))')
replace(C+'CrimsonRuntime.cs', 'if (!Matches(value) || phase != 3', 'if (!Matches(value) || cycle.Completed == 0 || phase != 3')
replace(C+'CrimsonRuntime.cs', '''            if (stage == CrimsonStage.Performance && phase < 3 && age >= unlockAt
                && summons[phase] is { } current && CrimsonPhaseRules.ShouldRetreat(phase, phase, current.NPC.life, targetLife))
                AdvancePhase(current);
            if (age >= nextPhrase && stage is CrimsonStage.Countdown or CrimsonStage.Performance)
                SchedulePhrase();''', '''            TickChorus();
            if (thresholdLatched && phase < 3 && summons[phase] is { } held)
                held.NPC.life = CrimsonPhaseRules.RetreatLife(targetLife);
            if (stage == CrimsonStage.Performance && phase < 3 && age >= unlockAt
                && summons[phase] is { } current && CrimsonPhaseRules.ShouldRetreat(phase, phase, current.NPC.life, targetLife))
            {
                current.NPC.life = CrimsonPhaseRules.RetreatLife(targetLife);
                if (!thresholdLatched)
                {
                    thresholdLatched = true; current.NPC.netUpdate = true;
                    CrimsonPackets.Log($"event=HpGateHeld fight={fight.Value} phase={phase} age={age} issued={cycle.Issued}");
                }
            }
            if (cycle.TryComplete(age, chorus is not null))
            {
                CrimsonPackets.Log($"event=PhaseCycleCompleted fight={fight.Value} phase={phase} age={age} cycles={cycle.Completed}");
                if (phase < 3 && thresholdLatched && summons[phase] is { } retired) AdvancePhase(retired);
                else { nextPhrase = age; Project(true); }
            }
            if (!cycle.Full && age >= nextPhrase && stage is CrimsonStage.Countdown or CrimsonStage.Performance)
                if (!TryScheduleChorus()) SchedulePhrase();''')
replace(C+'CrimsonRuntime.cs', 'ClearHazards(); Array.Clear(poseUntil);', 'ClearHazards(); Array.Clear(poseUntil); Array.Clear(techniqueCursor);\n        cycle.Reset(); thresholdLatched = false;')
replace(C+'CrimsonRuntime.cs', '    private void SchedulePhrase()\n    {\n        if (actor is null) return;', '    private void SchedulePhrase()\n    {\n        if (actor is null || cycle.Full) return;')
replace(C+'CrimsonRuntime.cs', 'phase == 3 ? 510 : 450);', 'CrimsonPlaytestTuning.AttackDamage);')
replace(C+'CrimsonRuntime.cs', 'nextPhrase = phraseEnd - CrimsonRhythm.LookAheadTicks; Project(true);', '''int recoveryEnd = phraseEnd;
        foreach (var plan in plans) recoveryEnd = Math.Max(recoveryEnd, plan.LastEnd + 14);
        cycle.Admit(phraseEnd, recoveryEnd); phrasesSinceChorus++;
        nextPhrase = cycle.Full ? cycle.FinishAt : phraseEnd - CrimsonRhythm.LookAheadTicks;
        Project(true);''')
replace(C+'CrimsonRuntime.cs', '    private void ClearHazards(int source = -1)\n    {', '    private void ClearHazards(int source = -1)\n    {\n        ClearChorus(source);')
for file, damage in [('CrimsonActors.cs','Hazard.Damage'), ('CrimsonGesture.cs','Plan.Damage'), ('CrimsonChorus.cs','Impact.Damage')]:
    replace(C+file, 'Projectile.damage = '+damage+';', 'Projectile.damage = CrimsonPlaytestTuning.AttackDamage;')
    replace(C+file, '    public override bool? CanHitNPC(NPC target) => false;', '''    public override bool? CanHitNPC(NPC target) => false;
    public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        => modifiers.SetMaxDamage(CrimsonPlaytestTuning.AttackDamage);''')
replace(C+'CrimsonChorus.cs', 'new CrimsonChorusImpact(plan, (byte)i, damage[i])', 'new CrimsonChorusImpact(plan, (byte)i, CrimsonPlaytestTuning.AttackDamage)')
replace(C+'CrimsonChorus.cs', 'ModContent.ProjectileType<CrimsonChorusStrike>(), damage[i],', 'ModContent.ProjectileType<CrimsonChorusStrike>(), CrimsonPlaytestTuning.AttackDamage,')
replace(C+'CrimsonChorus.cs', 'native_sources={string.Join', 'rehearsal_damage=1 budget_sources={string.Join')
replace(C+'CrimsonEffigy.cs', '''NPC.dontTakeDamage = !TryBoss(out var boss) || !boss!.State.SummonVulnerable(State.Index);
        NPC.boss = !NPC.dontTakeDamage;''', '''bool active = TryBoss(out var boss) && boss!.State.SummonVulnerable(State.Index);
        NPC.boss = active;
        NPC.dontTakeDamage = !active || NPC.life <= boss!.State.DamageFloor(State.Index);''')
replace(C+'CrimsonEffigy.cs', '''private bool AboveRetreatFloor(CrimsonBoss boss) => boss.State.Phase == 3
        || NPC.life > CrimsonPhaseRules.RetreatLife(boss.State.TargetLife);''', 'private bool AboveRetreatFloor(CrimsonBoss boss) => NPC.life > boss.State.DamageFloor(State.Index);')
replace(C+'CrimsonEffigy.cs', '''if (TryBoss(out var boss) && boss!.State.Phase < 3)
            modifiers.SetMaxDamage(Math.Max(1, NPC.life - CrimsonPhaseRules.RetreatLife(boss.State.TargetLife)));''', '''if (TryBoss(out var boss) && boss!.State.DamageFloor(State.Index) > 0)
            modifiers.SetMaxDamage(Math.Max(1, NPC.life - boss.State.DamageFloor(State.Index)));''')
replace(C+'CrimsonEffigy.cs', 'if (!TryBoss(out var boss) || boss!.State.Phase < 3)', 'if (!TryBoss(out var boss) || boss!.State.DamageFloor(State.Index) > 0)')
replace(C+'CrimsonEffigy.cs', 'NPC.life = CrimsonPhaseRules.RetreatLife(boss?.State.TargetLife ?? NPC.lifeMax);', 'NPC.life = boss is null ? Math.Max(1, NPC.lifeMax / 5) : boss.State.DamageFloor(State.Index);')
replace(C+'CrimsonActors.cs', 'State.Contains(player.whoAmI) ? null : false;', 'State.Contains(player.whoAmI) && NPC.life > State.DamageFloor(3) ? null : false;')
replace(C+'CrimsonActors.cs', 'State.Contains(projectile.owner) ? null : false;', 'State.Contains(projectile.owner) && NPC.life > State.DamageFloor(3) ? null : false;')
replace(C+'CrimsonActors.cs', 'NPC.dontTakeDamage = !Fresh || !State.Vulnerable(VisualAge);', 'NPC.dontTakeDamage = !Fresh || !State.Vulnerable(VisualAge) || NPC.life <= State.DamageFloor(3);')
replace(C+'CrimsonActors.cs', '    public override bool CheckDead()\n', '''    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        if (State.DamageFloor(3) > 0) modifiers.SetMaxDamage(Math.Max(1, NPC.life - State.DamageFloor(3)));
    }
    public override bool CheckDead()
''')
replace(C+'CrimsonActors.cs', 'if (State.Vulnerable(VisualAge)) Runtime?.Killed(this);', 'if (State.Vulnerable(VisualAge) && State.DamageFloor(3) == 0) Runtime?.Killed(this);')
# Centralized current tuning is used at spawn; old NPC defaults are not the HP authority.
for file in ['CrimsonActors.cs', 'CrimsonEffigy.cs']:
    replace(C+file, 'NPC.lifeMax = 3000000;', 'NPC.lifeMax = CrimsonPlaytestTuning.SoloTargetLife;')

# Connect the layered original-art material without baking or replacing assets.
s = read(V+'CrimsonRig.cs').replace('using Luminance.Assets;', 'using Luminance.Assets;\nusing Luminance.Core.Graphics;')
s = s.replace('    private static BasicEffect? material;', '    private static readonly int[] partOrder = { 3, 4, 0, 1, 2 };\n    private static readonly int[] singlePart = { 0 };')
s = s.replace('        var oldMaterial = material; material = null;\n        if (oldMaterial is not null) Main.QueueMainThreadAction(oldMaterial.Dispose);', '        ScarletMaterials.Reset();')
s = s.replace('        Mesh(batch, texture, texture.Bounds, at - screen, texture.Size() * .5f,\n', '        ScarletArticulation.DrawSecondary(batch, effigy.State.Index, age, alpha);\n        Mesh(batch, texture, texture.Bounds, at - screen, texture.Size() * .5f,\n')
s = s.replace('''        CrimsonEnergy.Begin();
        CrimsonEnergy.AddCore(at, 23 + signal.Charge * 20 + snap * 55, age,
            signal.Charge, Math.Max(signal.Recoil, snap), appear * alpha, CrimsonVisuals.Reduced);
        CrimsonEnergy.Draw(batch);''', '''        if (effigy.State.Index == 0)
        {
            CrimsonEnergy.Begin();
            CrimsonEnergy.AddCore(at + new Vector2(0, -18), 18 + signal.Charge * 12 + snap * 22, age,
                signal.Charge, Math.Max(signal.Recoil, snap), appear * alpha * .66f, CrimsonVisuals.Reduced);
            CrimsonEnergy.Draw(batch);
        }''')
a = s.index('    private static void Mesh(')
s = s[:a] + '''    private static void Mesh(SpriteBatch batch, Texture2D texture, Rectangle source, Vector2 center, Vector2 pivot,
        float scale, float time, float motion, float charge, float recoil, Color tint, bool flip, float rotation, bool apparition, int species = -1)
    {
        if (Main.dedServ || tint.A == 0) return;
        var material = ShaderManager.GetShader("Convergence.ScarletSurface");
        using var scope = new ScarletGraphicsScope(batch);
        material.TrySetParameter("uWorldViewProjection", ScarletMaterials.WorldMatrix);
        material.TrySetParameter("clock", time / 60);
        material.TrySetParameter("species", apparition ? (float)species : 3f);
        material.TrySetParameter("signal", new Vector4(charge, recoil, 1, CrimsonVisuals.Reduced ? 1 : 0));
        material.TrySetParameter("texel", new Vector2(1f / texture.Width, 1f / texture.Height));
        material.TrySetParameter("region", new Vector4(source.X / (float)texture.Width, source.Y / (float)texture.Height,
            source.Width / (float)texture.Width, source.Height / (float)texture.Height));
        material.SetTexture(texture, 0, apparition ? SamplerState.LinearClamp : SamplerState.PointClamp);
        material.SetTexture(MiscTexturesRegistry.WavyBlotchNoise.Value, 1, SamplerState.LinearWrap);
        material.SetTexture(MiscTexturesRegistry.DendriticNoiseZoomedOut.Value, 2, SamplerState.LinearWrap);
        foreach (int part in apparition ? partOrder : singlePart)
        {
            var pose = ScarletArticulation.Part(apparition ? species : 3, part, time, charge, recoil);
            material.TrySetParameter("part", (float)part);
            int offset = 0;
            for (int y = 0; y < Rows; y++) for (int x = 0; x < Columns; x++)
            {
                var a = Vertex(x, y); var b = Vertex(x + 1, y); var c = Vertex(x, y + 1); var d = Vertex(x + 1, y + 1);
                mesh[offset++] = a; mesh[offset++] = b; mesh[offset++] = c;
                mesh[offset++] = b; mesh[offset++] = d; mesh[offset++] = c;
            }
            material.Apply(); Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, mesh, 0, offset / 3);
            VertexPositionColorTexture Vertex(int x, int y)
            {
                float u = x / (float)Columns, v = y / (float)Rows;
                Vector2 local = (new Vector2(u * source.Width, v * source.Height) - pivot) * scale;
                float flexible = apparition && part != 0 ? MathF.Abs(u - .5f) * v : 0;
                float secondary = CrimsonVisuals.Reduced ? 0 : motion;
                local.X += MathF.Sin(time * .037f - v * 7 + u * 2) * secondary * flexible;
                local.Y += MathF.Sin(time * .028f + u * 8 - v * 3) * secondary * flexible * .5f;
                float side = part is 1 or 3 ? -1 : 1;
                Vector2 joint = new(side * source.Width * scale * .08f, source.Height * scale * (part >= 3 ? .02f : -.12f));
                local = ((local - joint) * pose.Scale).RotatedBy(pose.Rotation) + joint + pose.Offset;
                if (flip) local.X = -local.X;
                Vector2 pos = center + local.RotatedBy(rotation);
                return new(new Vector3(pos, 0), tint, new((source.X + u * source.Width) / texture.Width, (source.Y + v * source.Height) / texture.Height));
            }
        }
    }
}
'''
write(V+'CrimsonRig.cs', s)
# Local camera option; no server setting or gameplay effect.
s = read(V+'CrimsonVisuals.cs')
if 'public bool CinematicCamera' not in s:
    p = s.index('public sealed class CrimsonVisualConfig')
    a = s.index('{', p) + 1
    s = s[:a] + '\n    [System.ComponentModel.DefaultValue(true)] public bool CinematicCamera { get; set; } = true;\n' + s[a:]
write(V+'CrimsonVisuals.cs', s)

# Add linked domain inputs without replacing Ghost Samurai's newer tests.
p = 'Tests/Convergence.DomainTests/Directory.Build.targets'
s = read(p)
for name in ['CrimsonPlaytestTuning.cs', 'CrimsonChorusRules.cs']:
    project = read('Tests/Convergence.DomainTests/Convergence.DomainTests.csproj')
    if name not in s and name not in project:
        s = s.replace('</Project>', f'  <ItemGroup><Compile Include="../../{C}{name}" Link="{name}" /></ItemGroup>\n</Project>')
write(p,s)
p = 'Tests/Convergence.DomainTests/Convergence.DomainTests.csproj'
s = read(p)
if 'CrimsonScore.json' not in s:
    s = s.replace('</Project>', '  <ItemGroup><None Include="../../Assets/Music/CrimsonFoundry/Score.json" Link="CrimsonScore.json" CopyToOutputDirectory="PreserveNewest" /></ItemGroup>\n</Project>')
write(p,s)
p = 'Tests/Convergence.DomainTests/CrimsonFoundryTests.cs'
s = read(p).replace('AssertEqual(3000000, CrimsonInvocation.TargetLife(1)', 'AssertEqual(750000, CrimsonInvocation.TargetLife(1)').replace('AssertEqual(7000000, CrimsonInvocation.TargetLife(3)', 'AssertEqual(1750000, CrimsonInvocation.TargetLife(3)')
write(p,s)
p = 'Tests/Convergence.DomainTests/ScarletChorusTests.cs'
if (ROOT/p).exists():
    write(p,read(p).replace('catch (IOException)', 'catch (Exception ex) when (ex is IOException or InvalidDataException)'))
replace('build.txt','version = 0.3.14','version = 0.3.15')
p='Common/Networking/Protocol/EncounterProtocol.cs'
s=read(p)
s,n=re.subn(r'(const ushort Version\s*=\s*)\d+',r'\g<1>48',s)
if n!=1:
    s,n=re.subn(r'(const \w+ ProtocolVersion\s*=\s*)\d+',r'\g<1>48',s)
if n!=1: raise RuntimeError('protocol constant not found')
write(p,s)
print('Scarlet rehearsal/phase-cycle/material integration applied')
