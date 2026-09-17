"""One-time documentation/catalog preparation; never used by the game."""
from pathlib import Path
import json
import re
R=Path(__file__).resolve().parents[1]
def read(p): return (R/p).read_text(encoding='utf-8-sig')
def write(p,s): (R/p).write_text(s,encoding='utf-8',newline='\n')
def append(p,s): write(p,read(p).rstrip()+'\n\n'+s.strip()+'\n')

# Old regression tests required the former BasicEffect ownership mechanism.
# Preserve their intent under Luminance-managed material ownership.
p='tools/tests/test_presentation_contracts.py'
s=read(p)
s=s.replace('''        self.assertIn("material ??= new BasicEffect", draw)
        self.assertLess(draw.index("Main.dedServ"), draw.index("new BasicEffect"))
        self.assertEqual(source.count("new BasicEffect"), 1)''','''        self.assertIn('ShaderManager.GetShader("Convergence.ScarletSurface")', draw)
        self.assertLess(draw.index("Main.dedServ"), draw.index("ShaderManager.GetShader"))
        self.assertNotIn("new BasicEffect", source)''')
s=s.replace('''        self.assertLess(unload.index("var oldMaterial = material"), unload.index("material = null"))
        self.assertLess(unload.index("material = null"), unload.index("Main.QueueMainThreadAction"))
        self.assertIn("Main.QueueMainThreadAction(oldMaterial.Dispose)", unload)''','''        self.assertIn("ScarletMaterials.Reset()", unload)
        self.assertNotIn("Dispose()", unload)''')
write(p,s)

for lang in ('en-US','ja-JP'):
    p=f'Localization/CrimsonFoundry/{lang}.hjson';s=read(p)
    entry=('CinematicCamera: { Label: "Phase-transition camera" Tooltip: "Gentle local pan during protected transitions only; never blocks controls." }'
           if lang=='en-US' else 'CinematicCamera: { Label: "フェーズ移行時のカメラ" Tooltip: "無敵の移行時間中だけ緩く視点を動かします。操作は制限しません。" }')
    s=s.replace('\t\t\t\tScreenShake: {','\t\t\t\t'+entry+'\n\n\t\t\t\tScreenShake: {')
    if lang=='en-US':
        s=s.replace('at 20% HP it retreats.', 'at 20% HP it completes its action cycle, then retreats.')
        s=s.replace('Prototype: 1–8 players; late arrivals spectate until the next summon','Rehearsal: hostile damage 1, reduced HP; 1–8 players; late arrivals spectate')
    write(p,s)

p='docs/STATUS.md';s=read(p)
a=s.index('Development source:');b=s.index('\n\n',a)
s=s[:a]+'''Development source on **feat/scarlet-luminance-presentation-v2: 0.3.15 / protocol48**, based on main `6f0d56b5ce638eda66fd9f3651ea9dc01b1edace`. The newer Ghost Samurai/Oboro work is preserved. Scarlet now has sequential20% acts, complete12-phrase cycles, damage-one rehearsal tuning, reduced HP and Luminance material/primitive/Verlet/Metaball/cutscene adapters. This branch is **not merged, installed or published**. The normal profile's separately approved main0.3.14 installation and the public0.3.1 baseline are unchanged.

**Blocked asset integration:** the approved ScarletSanctum PNG is not in the branch because the conversation's compute/container backend timed out during binary transfer. Its exact-import helper and non-crashing missing-asset behavior are implemented; there is no substitute image or background-completion claim. See [Scarlet v2 evidence](evidence/2026-09-17-scarlet-presentation-v2.json).''' + s[b:]
s=s.replace('**Crimson Invocation — prototype under playtest.**','**Scarlet Invocation — feature-branch rehearsal, not visual approval.**')
s=s.replace('independent apparitions, small performer/companion, musical corridors and native damage.', 'sequential apparitions, a four-target Final, full action cycles, small performer/companion, varied physical phrases and native damage.')
s=s.replace('## Verification state\n','''## Verification state

- **Scarlet v2 recovery:** [source/API evidence](evidence/2026-09-17-scarlet-presentation-v2.json) records282 deterministic domain cases and a complete C# compile against pinned tML plus inspected Luminance source with0 errors/4 existing nullable warnings. The isolated compile uses an explicit Calamity API shim, not the real package. Final branch checks additionally cover source wiring, codecs and CPU lifecycle probes. Real Calamity/Luminance installed-package load, graphics, musical feel, performance and matching-peer sessions remain **not_run**; the background binary remains **blocked**. Previous FNA previews from interrupted local work are not approval of the recovered branch.
''')
s=s.replace('For Crimson, use Crimson Grimoire on Foundation Core → Ready → defeat three independent apparitions → confirm the performer becomes damageable without growing → victory/wipe/re-summon.', 'For Scarlet v2, first import the exact approved background and build/load the feature branch in a disposable profile. Use Scarlet Grimoire → Ready → each apparition holds20% until its full12-phrase cycle ends → retained three plus Vespera in Final → first ensemble cycle completes before lethal HP unlock → all-four victory/wipe/re-summon.')
write(p,s)

append('docs/adr/0026-crimson-score-and-native-projectiles.md', '''## Superseding amendment — 2026-09-17, Scarlet presentation v2

For the0.3.15/protocol48 feature branch, the [current owning spec](../encounters/crimson-foundry/ENCOUNTER_SPEC.md) supersedes the original simultaneous-three-target/corridor-only design. Do not infer the new behavior from the historical decision above.

The server still owns Fight/phase/roster/health/cue decisions. It now admits full varied physical phrases and12-phrase action cycles. At20%, an apparition's HP is held and the current action cycle and chorus/tails complete before transition. Final keeps all four bodies at a1HP lethal floor until its first ensemble cycle completes. Completed-cycle count is replicated monotonically within a phase. Attack-source activity is separate from NPC damageability.

Temporary rehearsal source damage and native hit cap are1 for Scarlet hostile attacks only. Target HP is750,000+500,000 per additional player; no player weapon or other Raid is rebalanced. Stack/Spread uses native hostile verdict projectiles with the same temporary cap.

Luminance supplies primitive rendering, managed shader ownership, projected state machines, bounded decorative Verlet chains, manually composited Metaballs, easing and local cutscene/shake. None is a new gameplay authority. Existing artwork is masked into animated regions, not replaced or claimed as new authored frame animation. All transient presentation is exact-Fight/epoch-owned and is cancelled on teardown. Required danger footprints survive Reduced Effects. SpriteBatch parameters and graphics bindings are restored to the actual caller.

The approved background is an unchanged original PNG with a fixed SHA256. Binary import is currently blocked by the conversation compute backend, not replaced by the Doll painting or synthetic artwork. Missing asset leaves the ordinary sky and logs once. Native loading, actual visual quality and MP remain unverified; dependency-source API compilation is not proof of compatibility with the installed DLLs.
''')
append('docs/NETWORK_ARCHITECTURE.md', '''## Scarlet feature protocol48 — 2026-09-17

This branch increments the global handshake to48. All peers must use the same branch package; matching version text alone is insufficient. The stable Scarlet type/Encounter IDs are unchanged. `CrimsonState` appends a bounded `CompletedCycles` integer before its roster count, rejects negative/out-of-range values and same-phase rollback, and projects the first-Final lethal floor. Cycle admission/completion remains server-owned. `CrimsonGesturePlan` retains immutable Fight, phase epoch, source, technique, warning/fire/end, body path and target geometry. Chorus verdicts remain exact-roster server decisions transported through native hostile projectiles. Damage-one tuning is applied both at source creation and native hit cap.

No Verlet positions, particles, material noise, screen shakes or camera state are network authority. They derive from the accepted timeline and reset by Fight/epoch. This revision does not claim to solve existing one-way-delay clock offset or validate late-packet fairness.
''')
append('docs/history/PLAYTEST_FEEDBACK.md', '''## 2026-09-17 — Scarlet material-quality and rehearsal request

Owner reports strong flat-fill/low-detail appearance compared with Doll and requests meaningful Luminance usage before further playtesting. Owner approves the generated fiery cathedral background and requests a new branch from current main, all Scarlet attack damage temporarily1, lower HP without instant full-fight skips, and completing a phase action cycle while holding threshold HP.

Implemented on `feat/scarlet-luminance-presentation-v2`: independent species materials, masked body regions, continuous physical primitives, decorative Verlet/Metaballs, varied musical calls, full12-phrase gates, reduced target budgets and hostile native damage-one caps. No claim that these changes look better in-game has been accepted. The approved painting's binary import was blocked by the conversation's container timeout; importer/renderer readiness is not artwork inclusion. Main merge/install/release was not requested or performed.
''')
append('Assets/ATTRIBUTION.md', '''### Scarlet presentation v2 — 2026-09-17 / 0.3.15

- Original project-authored shader sources/exports: `ScarletSurface`, `ScarletRibbon`, `ScarletResidue`, `ScarletBackdrop` under `Assets/AutoloadedEffects/Shaders`. Source/export/compiler identities are in `compiled.json`. No external Mod shader or artwork copied. Existing Luminance assets and APIs are referenced through the dependency, not vendored. Surface masks and runtime articulation preserve existing approved apparition PNGs.
- Approved but **not imported** background: fiery scarlet cathedral,1672×941 original PNG, generated with the built-in image tool under the owner's preceding brief and explicitly approved in this conversation. Actual generation-model version not supplied. SHA256 `94b77c968991bf52b14504bb11095c417dbf4dd2abb3bc378da779da39400a2d`. Intended runtime path `Assets/Textures/Backgrounds/ScarletSanctum.png`; `tools/import_scarlet_background.py` verifies/copies the original without modification. The binary could not be transferred during compute-backend failure. This entry is a provenance/pending-import record, not a statement that the asset is packaged.
- No new third-party music, sound or texture licenses are asserted. Graceful Ordeal remains separately licensed as recorded above.
''')
append('CHANGELOG.md', '''## 0.3.15 — Scarlet Luminance rehearsal branch (not released)

- Preserve current main's Ghost Samurai/Oboro fixes while bringing sequential Scarlet acts and11 physical techniques forward.
- Add Luminance-based species materials, masked body articulation, bounded decorative Verlet/Metaballs, primitive danger silhouettes and local protected-phase camera cues.
- All Scarlet hostile attacks use native damage1/cap1; quarter the previous HP budgets. Hold20% until the current12-phrase cycle completes; Final lethal unlock follows its first ensemble cycle.
- Add completed-cycle synchronization at protocol48, chorus Runtime hooks and source/API/lifecycle tests.
- Approved-background renderer/importer ready; actual image import blocked by compute backend. No normal-profile install, publication or visual-quality approval.
''')
for p in ('README.md','description.txt'):
    s=read(p).replace('Crimson Invocation','Scarlet Invocation').replace('Crimson Grimoire','Scarlet Grimoire')
    if p=='README.md': s+='\n\n### Scarlet presentation rehearsal branch\n\n`feat/scarlet-luminance-presentation-v2` is0.3.15/protocol48, not an installed release. Native hostile damage is temporarily1; HP is reduced and complete action cycles gate transitions. The approved background binary is pending import. See [current Scarlet specification](docs/encounters/crimson-foundry/ENCOUNTER_SPEC.md) and [verification status](docs/STATUS.md).\n'
    write(p,s)

evidence={
 'date':'2026-09-17','feature':'Scarlet Invocation presentation v2 rehearsal',
 'source_version':'0.3.15','protocol':48,'branch':'feat/scarlet-luminance-presentation-v2',
 'based_on_main':'6f0d56b5ce638eda66fd9f3651ea9dc01b1edace',
 'verified_source_commit':'1938e6c5082a1eb92dd187f948f070a18cd23205',
 'verified_run':'https://github.com/Minamium/Convergence-Mod/actions/runs/35177062995',
 'domain_tests':{'status':'passed','count':282},
 'api_compile':{'status':'passed','errors':0,'existing_nullable_warnings':4,
 'tml':'v2026.07.3.0','tml_archive_sha256':'6f51610f4b0f167d1620d0e8c83264ba14e047421e97d9f8053eb40e3d764e1a',
 'luminance_source':'b2468dfd2f299597602dc6826af781d436c29a57',
 'calamity':'explicit API-only temporary shim; NOT real Calamity2.2.4 binding'},
 'shader_exports':{'status':'compiled','identity_manifest':'Assets/AutoloadedEffects/Shaders/compiled.json'},
 'background':{'status':'blocked_binary_import','path':'Assets/Textures/Backgrounds/ScarletSanctum.png',
 'approved_sha256':'94b77c968991bf52b14504bb11095c417dbf4dd2abb3bc378da779da39400a2d',
 'reason':'conversation container/Python transport timeouts; no alternate painting substituted'},
 'final_source_checks':'See feature-branch CI for final catalog, Python, codec and CPU lifecycle results',
 'not_run':['native package build with real dependency DLLs','tModLoader cold load/reload','current GPU/frame inspection','in-game graphics acceptance','music listening','multiplayer/latency','performance profiling'],
 'not_performed':['main merge','normal-profile installation','Workshop publication','GitHub release']}
write('docs/evidence/2026-09-17-scarlet-presentation-v2.json',json.dumps(evidence,indent=2,ensure_ascii=False)+'\n')
print('Updated Scarlet fact owners, attribution and scoped verification record')
