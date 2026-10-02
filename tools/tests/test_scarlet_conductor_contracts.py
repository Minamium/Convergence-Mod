"""Scarlet attack expression (S4): Vespera's command. Source guards only; the look and the numbers (identity with no
note, the 94 px hold, the central gap, timing, Reduced) are gated by tools/fixtures/ScarletConductorGates.cs over the
offline rig harness, and the command math by S1's domain tests."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
CLIENT = ROOT/'Client/Encounters/CrimsonFoundry'
FIXTURES = ROOT/'tools/fixtures'


def read(path):
    return path.read_text(encoding='utf-8')


def code(path):
    """Source without // comments, so a rationale may name what the code must not do."""
    return '\n'.join(line.split('//', 1)[0] for line in read(path).splitlines())


def member(text, signature):
    start = text.index(signature)
    brace = text.index('{', start)
    depth = 0
    for end in range(brace, len(text)):
        depth += (text[end] == '{') - (text[end] == '}')
        if not depth:
            return text[start:end + 1]
    raise AssertionError('unclosed ' + signature)


class ScarletConductorContracts(unittest.TestCase):
    def test_performer_defaults_are_todays_picture(self):
        # Every new DrawPerformer argument trails the old ones with a default that changes nothing, so the companion
        # (positional arguments only) and the free apparition calls draw exactly as before.
        performer = code(CLIENT/'CrimsonRig.Performer.cs')
        self.assertIn('float recoil, float alpha = 1, bool showOrb = true, float materialized = 1,\n'
                      '        float? cast = null, float lean = 0, int nudgeX = 0, int nudgeY = 0, float rimBoost = 0, float rimHeat = 0)',
                      performer)
        draw = member(performer, 'internal static void DrawPerformer(')
        self.assertIn('float castWeight = cast ?? CrimsonInvocation.Ease(charge * 2);', draw)
        self.assertIn('Color rimColor = rimHeat > 0 ? Color.Lerp(RimColor, HotRim, rimHeat) : RimColor;', draw)
        self.assertIn('Color rim = rimColor * (alpha * opacity * (.26f + rimBoost));', draw)
        self.assertIn('- recoil * .035f + lean;', draw)
        self.assertIn('+ new Vector2(nudgeX, nudgeY);', draw)
        self.assertIn('RimColor = new(243, 118, 136, 0)', performer)
        companion = code(CLIENT/'CrimsonCompanionVisuals.cs')
        self.assertIn('CrimsonRig.DrawPerformer(Main.spriteBatch, Main.screenPosition, p.Center, Main.GlobalTimeWrappedHourly * 60,\n'
                      '                p.velocity, p.spriteDirection, p.ai[2] == 1, charge, recoil);', companion)

    def test_boss_draws_vespera_through_the_conductor(self):
        rig = code(CLIENT/'CrimsonRig.cs')
        draw = member(rig, 'internal static bool Draw(CrimsonBoss boss')
        self.assertIn('var frame = ScarletCueFrame.Of(boss);', draw)
        # Her Act's body only, a local participant only, and nothing in Final (she is absorbed).
        self.assertIn('frame.Participant && boss.State.Phase < 3 ? boss.State.Phase : -1', draw)
        self.assertIn('DrawConductor(batch, screen, at, age, boss.NPC.velocity, boss.NPC.spriteDirection, frame.Gestures, frame.Choruses,', draw)
        for moved in ('CrimsonEnergy.AddCore', 'DrawPerformer(', 'Vector2 held'):
            self.assertNotIn(moved, draw)

    def test_hold_never_brings_the_orb_nearer_to_her(self):
        performer = code(CLIENT/'CrimsonRig.Performer.cs')
        hold = member(performer, 'internal static ConductorHold Hold(')
        self.assertIn('float today = 53 + charge * 24 + recoil * 18;', hold)
        self.assertIn('new Vector2(facing * (94 + charge * 12) + command.OrbX, -25 + command.OrbY)', hold)
        self.assertIn('Math.Min(command.Radius, today + away)', hold)
        conductor = member(performer, 'internal static void DrawConductor(')
        self.assertIn('ScarletGestureMotion.Command(age, notes[..noted], charge, recoil, facing, CrimsonVisuals.Reduced)', conductor)
        self.assertIn('var hold = Hold(command, charge, recoil, facing);', conductor)
        self.assertIn('noted == 0 ? (float?)null : ConductorCast(age, notes[..noted], charge)', conductor)
        self.assertIn('ending * (1 - consumed) * hold.Alpha', conductor)
        # The reactor and its entry point stay as they are (design §2.5): no new draw, no shader change.
        self.assertIn('internal static void AddCore(Vector2 position, float radius, float age, float charge, float impulse, float alpha, bool reduced)',
                      read(CLIENT/'CrimsonEnergy.cs'))
        self.assertEqual(1, conductor.count('CrimsonEnergy.AddCore('))

    def test_command_reads_only_the_notes(self):
        # No beat-driven motion: Vespera answers her Act's notes (Born / Fire / End), never the music grid; the
        # rejected 4/4 figure and any line to the apparitions are not drawn.
        performer = code(CLIENT/'CrimsonRig.Performer.cs')
        banned = re.compile(r'CrimsonMeter|Score\.Pulse|BeatTick|baton|engagement|Transfusion|DrawSecondary|GameUpdateCount|GlobalTimeWrappedHourly')
        for name in ('internal static ConductorHold Hold(', 'internal static int ConductorNotes(', 'internal static float ConductorCast(',
                     'internal static void DrawConductor('):
            self.assertIsNone(banned.search(member(performer, name)), name)
        notes = member(performer, 'internal static int ConductorNotes(')
        self.assertIn('ScarletNotes.Collect(gestures, source, age, false, x, y, output[..ScarletNotes.Capacity])', notes)
        self.assertIn('ScarletNotes.Collect(gestures, source, age + CastLead, false, x, y, soon)', notes)
        self.assertIn('internal const int CastLead = 6, CastBlend = 4;', performer)

    def test_gates_cover_the_slice(self):
        gates = read(FIXTURES/'ScarletConductorGates.cs')
        for gate in ('"G8v"', '"V94"', '"G2v"', '"G6v"', '"G7v"'):
            self.assertIn(gate, gates)
        self.assertIn('CrimsonRig.Hold(command', gates)
        self.assertIn('CrimsonRig.ConductorNotes(live, phase, tick', gates)
        for banned in ('baton(', 'engagement(', 'Transfusion(', 'DrawSecondary(', 'BeatTick('):
            self.assertNotIn(banned, gates)


if __name__ == '__main__':
    unittest.main()
