"""Small integration guards for the formerly double-scaled cinematic callbacks."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
CLIENT = ROOT / "Client/Encounters/FirstSeverance"


def body(source, signature):
    start = source.index("{", source.index(signature))
    depth = 1
    for end in range(start + 1, len(source)):
        depth += (source[end] == "{") - (source[end] == "}")
        if not depth:
            return source[start:end + 1]
    raise AssertionError("Unclosed callback")


class CinematicCoordinates(unittest.TestCase):
    def test_all_cinematic_layers_are_unscaled(self):
        source = (CLIENT / "FirstSeverancePrototypePresentation.cs").read_text(encoding="utf-8")
        layers = body(source, "public override void ModifyInterfaceLayers")
        self.assertEqual(3, layers.count("InterfaceScaleType.None"))
        self.assertNotIn("InterfaceScaleType.UI", layers)
        self.assertNotIn("InterfaceScaleType.Game", layers)

    def test_start_phase_and_result_use_physical_viewport(self):
        presentation = (CLIENT / "FirstSeverancePrototypePresentation.cs").read_text(encoding="utf-8")
        result = (CLIENT / "FirstSeveranceResultVisuals.cs").read_text(encoding="utf-8")
        for source, name in ((presentation, "private static void DrawIntro"),
                             (presentation, "private static void DrawRupture"),
                             (result, "internal static void Draw")):
            with self.subTest(callback=name):
                callback = body(source, name)
                # Strip comments so explanations of the original defect are allowed.
                code = "\n".join(line.split("//")[0] for line in callback.splitlines())
                self.assertIn("GraphicsDevice.Viewport", code)
                self.assertNotIn("Main.UIScale", code)
                self.assertNotIn("Main.screenWidth", code)
                self.assertNotIn("Main.screenHeight", code)


if __name__ == "__main__":
    unittest.main()
