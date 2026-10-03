"""Contract guards for the shared Doll weapon layer (client-only lifetime, assets, shader exports)."""
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[2]
WEAPONS = ROOT / "Client/Encounters/FirstSeverance/Weapons"
SHADERS = ROOT / "Assets/AutoloadedEffects/Shaders"
REWARDS = ROOT / "Content/Encounters/FirstSeverance/Rewards"
# The boss core's material predates the weapon layer and is not part of the weapon palette family.
NOT_WEAPON_SHADERS = {"DollCoreEnergy.fx"}

spec = importlib.util.spec_from_file_location("shader_exports", ROOT / "tools/compile_shaders.py")
exports = importlib.util.module_from_spec(spec)
spec.loader.exec_module(exports)


def body(source, signature):
    start = source.index("{", source.index(signature))
    depth = 1
    for end in range(start + 1, len(source)):
        depth += (source[end] == "{") - (source[end] == "}")
        if not depth:
            return source[start:end + 1]
    raise AssertionError("Unclosed body: " + signature)


def weapon_sources():
    files = sorted(WEAPONS.glob("*.cs"))
    assert files, "the Doll weapon layer folder is empty"
    return {path.name: path.read_text(encoding="utf-8") for path in files}


def palette_block(text):
    match = re.search(r"BEGIN DOLL PALETTE(.*?)END DOLL PALETTE", text, re.S)
    assert match, "missing DOLL PALETTE block"
    return [value.lower() for value in re.findall(r"(?:#|0x)([0-9a-fA-F]{6})", match.group(1))]


def weapon_shaders():
    return sorted(path for path in SHADERS.glob("Doll*.fx")
                  if path.name == "DollPixel.fx" or path.name.endswith("Energy.fx") and path.name not in NOT_WEAPON_SHADERS)


class DollWeaponLayerLifetime(unittest.TestCase):
    def test_layer_types_are_client_only(self):
        for name, text in weapon_sources().items():
            for match in re.finditer(r"class\s+(\w+)\s*:\s*(ModSystem|ModPlayer|GlobalProjectile|GlobalItem|GlobalNPC)\b", text):
                preamble = text[max(0, match.start() - 160):match.start()]
                self.assertIn("[Autoload(Side = ModSide.Client)]", preamble, f"{name}: {match.group(1)} must be client-only")
        render = (WEAPONS / "DollWeaponLayer.Render.cs").read_text(encoding="utf-8")
        self.assertIn("class DollWeaponLayerSystem : ModSystem", render)
        self.assertIn("if (hooked || Main.dedServ) return;", body(render, "internal static void Hook()"))
        self.assertIn("if (Main.dedServ || disabled || !hooked || source is null) return;", body(render, "static partial void AddSource("))
        self.assertIn("!Main.dedServ && !Main.gameMenu", body(render, "private static void RenderLayer("))
        textures = (WEAPONS / "DollWeaponTextures.cs").read_text(encoding="utf-8")
        get = body(textures, "internal static Texture2D? Get(")
        self.assertLess(get.index("Main.dedServ"), get.index("ModContent.Request"))

    def test_unload_disposes_targets_on_the_main_thread_and_is_idempotent(self):
        render = (WEAPONS / "DollWeaponLayer.Render.cs").read_text(encoding="utf-8")
        unhook = body(render, "internal static void Unhook()")
        hooks = ("On_Main.CheckMonoliths -= RenderLayer", "On_Main.DrawProjectiles -= DrawBack",
                 "On_Main.DrawPlayers_AfterProjectiles -= CompositeFront")
        guarded = body(unhook, "if (hooked)")
        for hook in hooks:
            self.assertIn(hook, guarded)
        self.assertLess(unhook.index("ManagedRenderTarget? oldArt = artTarget, oldLight = lightTarget"),
                        unhook.index("artTarget = lightTarget = null"))
        self.assertLess(unhook.index("artTarget = lightTarget = null"), unhook.index("Main.QueueMainThreadAction"))
        self.assertIn("Main.QueueMainThreadAction(() => { oldArt?.Dispose(); oldLight?.Dispose(); })", unhook)
        self.assertEqual(unhook.count(".Dispose()"), 2, "targets are disposed only inside the queued main-thread action")
        system = body(render, "internal sealed class DollWeaponLayerSystem : ModSystem")
        for call in ("DollWeaponLayer.Unhook()", "DollWeaponTextures.Reset()", "DollWeaponArmDraw.ClearAll()"):
            self.assertIn(call, body(system, "public override void Unload()"))
        self.assertIn("DollWeaponLayer.Clear()", body(system, "public override void OnWorldUnload()"))
        # Idle release runs while drawing (CheckMonoliths), never from Unload or another thread.
        self.assertIn("ReleaseTargets()", body(render, "private static void Record()"))
        self.assertIn("Record();", body(render, "private static void RenderLayer("))
        self.assertEqual(render.count("ReleaseTargets()"), 2, "declared once and called only from Record")

    def test_draw_order_front_after_players_back_after_projectiles(self):
        render = (WEAPONS / "DollWeaponLayer.Render.cs").read_text(encoding="utf-8")
        hook = body(render, "internal static void Hook()")
        for event in ("On_Main.CheckMonoliths += RenderLayer", "On_Main.DrawProjectiles += DrawBack",
                      "On_Main.DrawPlayers_AfterProjectiles += CompositeFront"):
            self.assertIn(event, hook)
        layer = body(render, "private static void RenderLayer(")
        self.assertLess(layer.index("Record();"), layer.index("orig();"), "targets render before the world draws")
        for name in ("private static void DrawBack(", "private static void CompositeFront("):
            draw = body(render, name)
            self.assertTrue(draw.lstrip("{ \n").startswith("orig(self);"), f"{name} draws after the vanilla layer")
        front = body(render, "private static void CompositeFront(")
        self.assertLess(front.index("CompositeArt"), front.index("CompositeLight"), "art first, then light")

    def test_recordings_are_consumed_once(self):
        # Main.DrawCapture calls the draw hooks without CheckMonoliths; a stale recording must not draw again.
        render = (WEAPONS / "DollWeaponLayer.Render.cs").read_text(encoding="utf-8")
        front = body(render, "private static void CompositeFront(")
        self.assertRegex(front, r"finally\s*\{[^{}]*recorded = artReady = lightReady = false;[^{}]*\}")
        back = body(render, "private static void DrawBack(")
        self.assertLess(back.index("backPending = false;"), back.index("DrawFallback("), "the Back stratum is consumed before it draws")
        self.assertIn("backPending = canvas.HasBack;", body(render, "private static void Record()"))

    def test_buffers_are_created_on_the_client_only(self):
        # The canvas holds ~1.7 MB of arrays: no static initializer may allocate it on a dedicated server.
        render = (WEAPONS / "DollWeaponLayer.Render.cs").read_text(encoding="utf-8")
        code = re.sub(r"//.*", "", render)
        self.assertIsNone(re.search(r"static\s+(readonly\s+)?(DollWeaponCanvas|IDollWeaponSource\??)(\[\])?\??\s+\w+\s*=(?!>)", code),
                          "sources and canvas have no static initializer")
        self.assertEqual(code.count("new DollWeaponCanvas("), 1)
        hook = body(render, "internal static void Hook()")
        self.assertLess(hook.index("if (hooked || Main.dedServ) return;"), hook.index("new DollWeaponCanvas("))
        self.assertLess(hook.index("new IDollWeaponSource?[MaxSources]"), hook.index("On_Main.CheckMonoliths +="))

    def test_over_budget_input_is_counted_not_silent(self):
        canvas = (WEAPONS / "DollWeaponCanvas.cs").read_text(encoding="utf-8")
        for signature in ("internal bool EnergyStrip(", "internal void Burst(", "internal void Sprite("):
            self.assertIn("Dropped++", body(canvas, signature), f"{signature} counts what it drops")
        self.assertIn("float.IsFinite(p.X)", body(canvas, "internal void EndRecording()"))

    def test_no_async_load_texture_caching(self):
        for name, text in weapon_sources().items():
            code = re.sub(r"//.*", "", text)
            self.assertNotIn("AsyncLoad", code, f"{name}: never request Doll weapon textures with AsyncLoad")
            for match in re.finditer(r"Request<Texture2D>\(", code):
                call = code[match.end():code.index(")", match.end()) + 1]
                self.assertIn("AssetRequestMode.ImmediateLoad", call, f"{name}: Request<Texture2D> must ImmediateLoad")
            self.assertIsNone(re.search(r"static\s+(readonly\s+)?Texture2D\??\s+\w+\s*[=;,]", code),
                              f"{name}: keep Asset<Texture2D> handles and read .Value per draw, never a cached Texture2D")

    def test_layer_is_doll_owned_and_pure_files_stay_pure(self):
        sources = weapon_sources()
        for name, text in sources.items():
            code = re.sub(r"//.*", "", text)
            self.assertNotRegex(code, r"\b(Ebon|Soboro)\w*", f"{name} must not reference Ebon or Soboro types")
            self.assertNotIn("System.Linq", code, f"{name}: no LINQ in the per-frame layer")
            self.assertNotIn("new List<", code, f"{name}: fixed arrays only")
        pure = [WEAPONS / "DollSpritePlacement.cs", WEAPONS / "DollCueClock.cs", REWARDS / "DollWeaponBudget.cs"]
        for path in pure:
            text = path.read_text(encoding="utf-8")
            self.assertIsNone(re.search(r"^using\s+(Terraria|Microsoft\.Xna|ReLogic|Luminance)", text, re.M),
                              f"{path.name} is linked into the domain tests and must stay pure")
        project = (ROOT / "Tests/Convergence.DomainTests/Convergence.DomainTests.csproj").read_text(encoding="utf-8")
        for path in pure:
            self.assertIn(str(path.relative_to(ROOT)).replace("\\", "/"), project)
        for name in ("DollWeaponLayer.cs", "DollWeaponCanvas.cs", "DollPixelArt.cs"):
            text = sources[name]
            self.assertIsNone(re.search(r"^using\s+(Terraria|ReLogic|Luminance)", text, re.M),
                              f"{name} is linked by the offline preview and depends only on FNA")


class DollWeaponLayerShader(unittest.TestCase):
    def test_doll_pixel_is_compiled_listed_and_every_pass_exists(self):
        source = SHADERS / "DollPixel.fx"
        compiled = source.with_suffix(".fxc")
        self.assertTrue(source.is_file() and compiled.is_file())
        record = json.loads((SHADERS / "compiled.json").read_text(encoding="utf-8"))["files"]["DollPixel.fx"]
        self.assertEqual(record["source_sha256"], hashlib.sha256(source.read_bytes()).hexdigest())
        self.assertEqual(record["output_sha256"], hashlib.sha256(compiled.read_bytes()).hexdigest())
        used = set()
        for file, text in weapon_sources().items():
            names = set(re.findall(r'"(\w+Pass)"', text))
            used |= names
            # A file that names its own material (ShaderName = "Convergence.Doll<Weapon>Energy") uses that shader's
            # passes; every other layer file uses DollPixel's.
            owner = re.search(r'ShaderName = "Convergence\.(\w+)"', text)
            shader = SHADERS / f"{owner.group(1) if owner else 'DollPixel'}.fx"
            declared = shader.read_text(encoding="utf-8")
            for name in names:
                self.assertIn(f"pass {name} {{", declared, f"{file}: {name} is declared in {shader.name}")
        base = {"SpritePass", "LinePass", "FlatPass", "RampPass", "CompositeArtPass", "CompositeLightPass", "CompositeLightPlainPass"}
        self.assertTrue(base <= used)
        pixel = source.read_text(encoding="utf-8")
        for name in base:
            self.assertIn(f"pass {name} {{", pixel, "the layer's own passes live in DollPixel.fx")
        # A weapon's own Doll<Weapon>Energy material declares its passes there; DollPixel owns the rest.
        materials = "".join(path.read_text(encoding="utf-8") for path in weapon_shaders())
        for name in used:
            self.assertIn(f"pass {name} {{", materials)
        art = (WEAPONS / "DollPixelArt.cs").read_text(encoding="utf-8")
        self.assertIn('ShaderName = "Convergence.DollPixel"', art)
        exports.verify()

    def test_lacuna_material_has_no_preshader_and_no_uniform_branch(self):
        # The FNA effect runtime mis-evaluates some fx_2_0 preshader code: every uniform-only quantity of the Lacuna
        # material is computed on the CPU (LacunaEnergy.Timing), so its export carries no PRES/FXLC block at all.
        compiled = (SHADERS / "DollLacunaEnergy.fxc").read_bytes()
        self.assertNotIn(b"PRES", compiled)
        self.assertNotIn(b"FXLC", compiled)
        source = (SHADERS / "DollLacunaEnergy.fx").read_text(encoding="utf-8")
        uniforms = set(re.findall(r"^(?:float\d?|matrix)\s+(\w+);", source, re.M))
        self.assertEqual({"uWorldViewProjection", "dotOrigin", "timing"}, uniforms)
        for name in uniforms - {"uWorldViewProjection"}:
            self.assertIsNone(re.search(rf"\bif\s*\([^)]*\b{name}\b", source), f"no branch on {name}")
        presentation = (WEAPONS / "LacunaPresentation.cs").read_text(encoding="utf-8")
        self.assertIn('DollPixelArt.Set(shader, "timing", Timing(context.Clock, context.Reduced, pass));', presentation)

    def test_non_doll_shader_exports_unchanged(self):
        exports.verify()
        try:
            base = subprocess.run(["git", "merge-base", "HEAD", "origin/main"], cwd=ROOT, capture_output=True,
                                  text=True, check=True).stdout.strip()
            before = json.loads(subprocess.run(["git", "show", f"{base}:Assets/AutoloadedEffects/Shaders/compiled.json"],
                                               cwd=ROOT, capture_output=True, text=True, check=True).stdout)
        except (OSError, subprocess.CalledProcessError, ValueError):
            self.skipTest("no origin/main merge base in this checkout")
        after = json.loads((SHADERS / "compiled.json").read_text(encoding="utf-8"))
        self.assertEqual(before["compiler_sha256"], after["compiler_sha256"])
        self.assertEqual(before["flags"], after["flags"])
        for name, record in before["files"].items():
            if name.startswith("Doll") and name not in NOT_WEAPON_SHADERS:
                continue
            now = after["files"].get(name)
            self.assertIsNotNone(now, f"{name} export removed")
            # A branch that edits another shader's own source (Scarlet rewards' ScarletInk path passes) owns that export;
            # every shader whose source is unchanged must still compile to the same bytes.
            if now["source_sha256"] != record["source_sha256"]:
                continue
            self.assertEqual(record, now, f"{name} export changed with the Doll weapon layer")

    def test_palette_blocks_match_the_tones(self):
        art = palette_block((WEAPONS / "DollPixelArt.cs").read_text(encoding="utf-8"))
        layer = (WEAPONS / "DollWeaponLayer.cs").read_text(encoding="utf-8")
        tones = re.search(r"enum DollTone : byte\s*\{([^}]*)\}", layer).group(1)
        tones = [name.strip() for name in tones.split(",") if name.strip()]
        self.assertEqual(len(art), len(tones))
        shaders = weapon_shaders()
        self.assertIn(SHADERS / "DollPixel.fx", shaders)
        for path in shaders:
            self.assertEqual(palette_block(path.read_text(encoding="utf-8")), art, f"{path.name} palette block drifted")
        expected = {"Ink": "121017", "Plum": "301840", "Violet": "9458ff", "Lilac": "b99cff", "PearlViolet": "ddd8f8",
                    "Bone": "fcf4e6", "White": "ffffff", "Ruby": "8c141c"}
        for name, value in expected.items():
            self.assertEqual(art[tones.index(name)], value, name)


def tolerant(expression):
    """Regex for `expression` that ignores formatting: any whitespace (or none, even a line break) between tokens."""
    return r"\s*".join(re.escape(part) for part in expression.split())


class DollWeaponBudgetSources(unittest.TestCase):
    def test_legacy_multipliers_mirrored_by_the_budget_test_are_pinned(self):
        pins = {
            "MeridianBastion.cs": ["ScaledDamage(damage, heavy ? 1.15f : Overdrive ? .62f : .95f)", "(int)Age % 36 == 0"],
            "LacunaConvergence.cs": ["ScaledDamage(owner.GetWeaponDamage(owner.HeldItem), 2.0f)",
                                     "ScaledDamage(owner.GetWeaponDamage(owner.HeldItem), .55f)"],
            "RitualChoir.cs": ["ScaledDamage(Projectile.damage, .85f)"],
            "ChoirRequiem.cs": ["(int)Math.Clamp(total * 1.05, 1, int.MaxValue / 4.0)", "Projectile.localNPCHitCooldown = 12"],
            "WitnessLitany.cs": ["ScaledDamage(Projectile.damage, .28f)", "ScaledDamage(Projectile.damage, 5.4f)", "tick % 29 == 16"],
            "RitualBolts.cs": ["ScaledDamage(Projectile.damage, .60f)", "Age >= 42"],
        }
        for name, needles in pins.items():
            text = (REWARDS / name).read_text(encoding="utf-8")
            for needle in needles:
                self.assertRegex(text, tolerant(needle), f"{name} changed; recompute DollWeaponBudget and its test")
        tests = (ROOT / "Tests/Convergence.DomainTests/DollWeaponLayerTests.cs").read_text(encoding="utf-8")
        mirrors = {"MeridianBuild": ".95f", "MeridianOverdrive": ".62f", "MeridianHeavy": "1.15f", "LacunaBolt": ".55f",
                   "LacunaBeam": "2.0f", "ChoirNote": ".85f", "ChoirChorus": "1.05", "ChoirChorusCooldown": "12",
                   "WitnessShard": ".28f", "WitnessBlade": "5.4f", "WitnessVerdict": ".60f", "WitnessReturnTicks": "42"}
        for name, value in mirrors.items():
            self.assertRegex(tests, rf"\b{name}\s*=\s*{re.escape(value)}\s*[,;]", name)


if __name__ == "__main__":
    unittest.main()
