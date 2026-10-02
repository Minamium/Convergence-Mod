"""Pale Meridian presentation contracts (docs/encounters/first-severance/WEAPONS.md "Pale Meridian"): source checks
of the rules the offline preview (tools/preview-doll-meridian.ps1) also proves on rendered frames. Pure Python (CI)."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
WEAPONS = ROOT / "Client/Encounters/FirstSeverance/Weapons"
SPEC = ROOT / "docs/encounters/first-severance/WEAPONS.md"


def code(path):
    return re.sub(r"//[^\n]*", "", path.read_text(encoding="utf-8"))


def method(source, signature):
    start = source.index("{", source.index(signature))
    depth = 1
    for end in range(start + 1, len(source)):
        depth += (source[end] == "{") - (source[end] == "}")
        if not depth:
            return source[start:end + 1]
    raise AssertionError("Unclosed body: " + signature)


class PaleMeridianPresentation(unittest.TestCase):
    def setUp(self):
        self.presentation = code(WEAPONS / "MeridianPresentation.cs")
        self.visuals = code(WEAPONS / "MeridianVisuals.cs")

    def test_only_the_key_draws_behind_players(self):
        self.assertEqual(1, self.presentation.count("DollStratum.Back"), "one Back sprite")
        self.assertIn("DollStratum.Back", method(self.presentation, "private static void EmitKey("))
        spec = SPEC.read_text(encoding="utf-8")
        self.assertIn("only the Choir's organ (and its gallery row) and Pale Meridian's wind-up key draw behind players", spec)

    def test_reduced_effects_removes_every_glow_disc(self):
        glow = method(self.presentation, "private static void Glow(")
        self.assertIn("canvas.Reduced", glow.split("\n")[1] + glow.split("\n")[2])
        self.assertEqual(1, self.presentation.count("MeridianEnergyMaterial.GlowPass"), "glow discs come only from Glow")

    def test_frame_is_recorded_lines_first_and_wakes_last(self):
        self.assertIn("internal enum MeridianPass : byte { LineResidue, LineBody, LineLight, Gun, RoundHead, RoundWake }",
                      self.presentation)
        emit = method(self.visuals, "public bool Emit(")
        self.assertIn("pass < MeridianPresentation.PassCount", emit)
        self.assertNotRegex(emit, r"pass\s*(<=|==|<)\s*\d", "passes are named, not numbered")
        # Each line pass emits one kind of energy only, so all lines alive share one batch per pass.
        meridian = method(self.presentation, "private static void EmitMeridian(")
        lattice = method(self.presentation, "private static void EmitLattice(")
        for body in (meridian, lattice):
            self.assertIn("MeridianPass.LineResidue", body)
            self.assertIn("MeridianPass.LineLight", body)
        self.assertIn("WakeLight", method(self.presentation, "internal static void EmitRoundWake("))

    def test_parts_trail_light_and_fly_from_the_score(self):
        self.assertIn("EmitFlights(canvas, view, pose, alpha);", self.presentation)
        flights = method(self.presentation, "private static void EmitFlights(")
        self.assertIn("canvas.Line(", flights)
        self.assertIn("PaleMeridianScore.PartOffset", method(self.presentation, "private static Vector2 PartCentre("))


if __name__ == "__main__":
    unittest.main()
