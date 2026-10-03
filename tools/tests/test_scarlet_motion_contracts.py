"""Scarlet attack expression (S1): motion/material math and its wiring. Source guards only; the math is pinned by
the domain tests (ScarletGestureMotionTests) and the look by the offline harness, never by these checks."""
from pathlib import Path
import json
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
CLIENT = ROOT/'Client/Encounters/CrimsonFoundry'
VFX = CLIENT/'Vfx'
TESTS = ROOT/'Tests/Convergence.DomainTests'
MOTION = [VFX/'ScarletNote.cs', VFX/'ScarletEnvelope.cs', VFX/'ScarletGestureMotion.cs', VFX/'ScarletBodyMaterial.cs',
          VFX/'CrimsonChoirCue.cs', CLIENT/'ScarletCueFrame.cs', CLIENT/'CrimsonChoirMotion.cs']
PURE = [path for path in MOTION if path.parent == VFX]
# The rigs that draw the motion: guarded on their code (comments may name what they must not do).
RIGS = [CLIENT/'ScarletApparitionRig.cs', CLIENT/'CrimsonChoirRig.cs', VFX/'ScarletChoirBlood.cs']


def read(path):
    return path.read_text(encoding='utf-8')


def code(path):
    """Source without // comments, so a rationale may name what the code must not do."""
    return '\n'.join(line.split('//', 1)[0] for line in read(path).splitlines())


class ScarletMotionContracts(unittest.TestCase):
    def test_motion_reads_only_plan_times(self):
        # No beat-driven motion: the bodies answer Born/Fire/End only. The rejected 4/4 figure is never ported.
        banned = re.compile(r'CrimsonMeter|Score\.Pulse|BeatTick|baton|engagement|Transfusion\(|DrawSecondary\(')
        for path in MOTION:
            self.assertIsNone(banned.search(read(path)), f'{path.name} must not follow the beat grid')
        for path in RIGS:
            self.assertIsNone(banned.search(code(path)), f'{path.name} must not follow the beat grid')
        for path in PURE:
            text = code(path)
            for clock in ('GameUpdateCount', 'GlobalTimeWrappedHourly', 'timeForVisualEffects', 'DateTime', 'Stopwatch', 'new Random'):
                self.assertNotIn(clock, text, f'{path.name} has no free-running clock ({clock})')

    def test_pure_layer_is_fna_and_terraria_free_and_preview_linkable(self):
        # Domain tests link these files; the offline preview links every Vfx/*.cs with the Content authority only.
        for path in PURE:
            text = code(path)
            for token in ('using Microsoft.Xna', 'using Terraria', 'using Luminance', 'using ReLogic', 'Vector2', 'Main.',
                          'CrimsonRigMotion', 'CrimsonVisuals', 'ScarletMaterials', 'ScarletArticulation', 'ScarletCueFrame'):
                self.assertNotIn(token, text, f'{path.name} must stay FNA/Terraria-free and preview-linkable ({token})')
        project = read(TESTS/'Convergence.DomainTests.csproj')
        for path in PURE:
            self.assertIn(f'Client/Encounters/CrimsonFoundry/Vfx/{path.name}', project)
        self.assertIn('<EmbeddedResource Include="Data/scarlet-motion-golden.json" LogicalName="scarlet-motion-golden.json" />', project)

    def test_golden_comes_from_the_approved_prototype(self):
        golden = json.loads(read(TESTS/'Data/scarlet-motion-golden.json'))
        self.assertLessEqual(golden['tolerance'], 1e-4)
        self.assertEqual({'motion.js', 'rigs.js'}, set(golden['source']))
        for digest in golden['source'].values():
            self.assertRegex(digest, r'^[0-9a-f]{64}$')
        self.assertEqual({'envelope', 'crown', 'mantle', 'choir'}, {s['kind'] for s in golden['scenarios']})
        self.assertNotIn('baton', json.dumps(golden))
        generator = read(ROOT/'tools/scarlet_motion_golden.mjs')
        self.assertIn('const { envelope, crown, mantle, choirArm: quarterArm, beatTick } = M;', generator)
        self.assertNotRegex(generator, r'\bbaton\s*\(|\bengagement\s*\(|import \{[^}]*\bbaton\b')
        tests = read(TESTS/'ScarletGestureMotionTests.cs')
        self.assertIn('GetManifestResourceStream("scarlet-motion-golden.json")', tests)
        self.assertIn('node tools/scarlet_motion_golden.mjs', tests)

    def test_choir_arm_goes_through_the_shared_envelope(self):
        motion = read(CLIENT/'CrimsonChoirMotion.cs')
        self.assertIn('=> ScarletGestureMotion.ChoirArm(index, age, charge, recoil, cues);', motion)
        self.assertNotIn('.80f * Out(p / .38f)', motion)
        self.assertNotIn('record struct CrimsonChoirCue', motion)
        arm = read(VFX/'ScarletGestureMotion.cs')
        for curve in ('ScarletEnvelope.Prepare(p)', 'ScarletEnvelope.Release(t)', 'ScarletEnvelope.Decay(t, cue.End - cue.Fire)',
                      'ScarletEnvelope.Burst(t, decay)'):
            self.assertIn(curve, arm)
        self.assertIn('ScarletAcceptedArm', read(TESTS/'ScarletGestureMotionTests.cs'))

    def test_one_projectile_scan_per_frame(self):
        frame = code(CLIENT/'ScarletCueFrame.cs')
        self.assertEqual(1, frame.count('Main.ActiveProjectiles'))
        self.assertIn('tick != Main.GameUpdateCount', frame)
        self.assertIn('fight != owner.State.Fight', frame)
        self.assertIn('ScarletArticulation.Member(owner)', frame)
        rig = read(CLIENT/'CrimsonRig.cs')
        self.assertIn('internal static partial class CrimsonRig', rig)
        self.assertNotIn('Main.ActiveProjectiles', code(CLIENT/'CrimsonRig.cs'))
        self.assertIn('ScarletCueFrame.Reset();', rig[rig.index('internal static void Unload()'):rig.index('internal static (float Charge, float Recoil) Signal(CrimsonBoss')])
        self.assertIn('=> ScarletNotes.ChoirCues(ScarletCueFrame.Of(boss).Gestures, age, flipped, cues, final);', rig)

    def test_performer_split_is_linkable_without_a_boss(self):
        performer = read(CLIENT/'CrimsonRig.Performer.cs')
        self.assertIn('internal static partial class CrimsonRig', performer)
        for member in ('internal static void DrawPerformer(', 'internal static void DrawApparition(', 'internal static void DrawPressure(',
                       'private static readonly Rectangle[] poses', 'private static readonly Vector2[] pivots',
                       'private static void LoadPerformer()', 'ReadOnlySpan<CrimsonGesturePlan> gestures'):
            self.assertIn(member, performer)
        for bound in ('CrimsonBoss', 'CrimsonEffigy', 'Main.ActiveProjectiles', 'ScarletCueFrame'):
            self.assertNotIn(bound, code(CLIENT/'CrimsonRig.Performer.cs'), f'the performer part needs no {bound}')
        rig = read(CLIENT/'CrimsonRig.cs')
        for moved in ('internal static void DrawPerformer(', 'internal static void DrawApparition(', 'internal static void DrawPressure('):
            self.assertNotIn(moved, rig)

    def test_effigy_passes_notes_motion_material_and_heave(self):
        rig = read(CLIENT/'CrimsonRig.cs')
        effigy = rig[rig.index('internal static bool DrawEffigy('):rig.index('internal static int ChoirCues(')]
        self.assertIn('frame.Member ? ScarletNotes.Collect(frame.Gestures, effigy.State.Index, age, flipped,', effigy)
        self.assertIn('ScarletNotes.ChoirCues(frame.Gestures, age, flipped, cues, accepted: !frame.Member)', effigy)
        self.assertIn('heave: frame.Member ? ScarletGestureMotion.Heave : 0, material: choir', effigy)
        self.assertIn('ScarletGestureMotion.Crown(age, notes[..noted]) : ScarletGestureMotion.Mantle(age, notes[..noted])', effigy)
        self.assertIn('motion: motion, material: body', effigy)
        # Final and the free-standing apparitions keep the defaults (no notes, no heave).
        self.assertIn('ScarletApparitionRig.Draw(batch,species,center,height*scale,age,.7f,0,reveal,dissolve:dissolve);', read(CLIENT/'CrimsonRig.Performer.cs'))
        apparition = read(CLIENT/'ScarletApparitionRig.cs')
        self.assertIn('in Vfx.ScarletApparitionMotion motion = default, in Vfx.ScarletBodyState material = default)', apparition)
        choir = read(CLIENT/'CrimsonChoirRig.cs')
        self.assertIn('float heave = 0, in Vfx.ScarletChoirState material = default)', choir)

    def test_final_choir_cues_keep_the_accepted_derivation(self):
        notes = read(VFX/'ScarletNote.cs')
        cues = notes[notes.index('internal static int ChoirCues('):notes.index('internal static bool ChoirArms(')]
        final = cues[cues.index('if (final || accepted)'):cues.index('            if (p.Source != 2) continue;')]
        self.assertIn('if (!final && p.Source != 2) continue;', final)
        self.assertIn('if (age < p.Born || age >= p.End) continue;', final)
        self.assertIn('new(p.Born, p.Fire, p.End, p.Step % 4, broad)', final)
        self.assertIn('ScarletNotes.ChoirCues', read(CLIENT/'CrimsonRig.cs'))
        self.assertIn('CrimsonRig.ChoirCues(boss,age,cues,true)', read(CLIENT/'ScarletAvatarArt.cs'))

    def test_membership_drives_the_expression_and_never_reads_life(self):
        # A member's death or revival never snaps a swinging body back to rest; shakes and sounds keep Participant.
        articulation = code(CLIENT/'ScarletArticulation.cs')
        member = articulation[articulation.index('internal static bool Member('):]
        member = member[:member.index(';') + 1]
        self.assertIn('boss.State.Contains(Main.myPlayer)', member)
        for life in ('dead', 'ghost'):
            self.assertNotIn(life, member)
        self.assertIn('internal static bool Participant(CrimsonBoss? boss) => Member(boss) && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost;', articulation)
        rig = code(CLIENT/'CrimsonRig.cs')
        self.assertNotIn('frame.Participant', rig)
        self.assertEqual(4, rig.count('frame.Member'))  # Vespera's command, the notes, the Choir's cues and its heave

    def test_past_poses_keep_closed_notes(self):
        notes = read(VFX/'ScarletNote.cs')
        collect = notes[notes.index('internal static int Collect('):notes.index('internal static (float Until, float Since) SignalTimes(')]
        self.assertIn('Span<ScarletNote> output, float lookback = 0)', collect)
        self.assertIn('age >= plan.Fire + SpanOf(plan) + lookback) continue;', collect)
        self.assertIn('if (note.Holds(age)) for (int i = 0; i < count && stale < 0; i++) if (!output[i].Holds(age)) stale = i;', collect)
        self.assertIn('internal const int PastTicks = 21;', notes)
        rig = read(CLIENT/'CrimsonRig.cs')
        self.assertIn('boss.NPC.Center.X, boss.NPC.Center.Y, notes, effigy.State.Index == 2 ? 0 : ScarletNotes.PastTicks) : 0;', rig)
        # The wake replays the notes at its lags; ScarletApparitionRig's lags fit inside PastTicks (16 + the 4-tick lag).
        apparition = read(CLIENT/'ScarletApparitionRig.cs')
        self.assertIn('WakeLags = { 0, 2, 4, 6, 8, 10, 13, 16 };', apparition)
        self.assertIn('var before = Motion(species, t - 4, notes);', apparition)


if __name__ == '__main__':
    unittest.main()
