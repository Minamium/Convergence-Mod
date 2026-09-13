---
doc_id: project.status
document_type: status
status: accepted
owners:
  - project
last_reviewed: 2026-09-13
source_of_truth_for:
  - project.implementation_status
aliases:
  - current state
  - implementation inventory
related_code:
  - Common
  - Content/Encounters/FirstSeverance
  - Tests/Convergence.DomainTests
related_docs:
  - handoff.windows
  - encounter.first-severance.plan
---

# Project Status

## Current build

Development **0.2.71 / protocol 34** is integrated and installed in the canonical playtest profile: [build evidence](evidence/2026-09-13-beam-video-reference.json). All avoidable Doll Raid beams now have sparse forecast glints across the future hazard width and moving bright-head/long-tail currents in their live material. This is no longer a lattice-only change. The prior finite lattice packet, attack timings/collision, damage, player circles, audio, scene/ending effects, weapons/rewards and integrated Ghost Samurai work remain unchanged. Actual user-recording frames and pinned WoTM/WotG drawing paths are analyzed in [F14](research/WOTG_RAID_BENCHMARK.md#f14--recorded-beam-motion-2026-09-13); [visual spec](encounters/first-severance/VISUAL_SPEC.md#beams-swords-and-forecast-readability) owns the material rules. [build.txt](../build.txt) and [EncounterProtocol](../Common/Networking/Protocol/EncounterProtocol.cs) own version/compatibility. First Severance remains **不幸な人形劇 / The Unfortunate Doll Play**, Boss **ラクリモーサ — 縛られた心 / Lacrimosa — The Bound Heart**. Visual acceptance remains user-owned, not a claim of WoTM/WotG parity.

- Independent [Ghost Samurai / 幽鬼武者](encounters/ghost-samurai/ENCOUNTER_SPEC.md): charged and Phase2 dash slashes now damage only through the physically swept body during rush frames. Bounded current-velocity interception tracks until four ticks before launch, then follows a fixed straight path. First charged forecasts gain 120 ticks (including grid follow-up); later releases are 90 ticks apart. Phase3 adds fixed-center inner/outer sword/wind steps 60 ticks apart using four bounded exact-Fight actors. Paired cuts, grid spacing, wisps, summon, damage values, native hurt and cleanup remain. No Raid Ready/Down/revival, loot or solo redesign is added.
- Ghost Samurai presentation replaces the procedural skeleton with original generated body/sword-arm artwork, fixes the whole-MagicPixel source rectangle that enlarged stroke thickness by 1000, and adds warm high-contrast forecasts with pale-blue strikes. This display-only pass changes no combat or wire rules; the feature spec owns exact art/keying/border behavior.
- The shared Ghost Samurai GlobalNPC art cache is now static, fixing the load-time rejection introduced in 0.2.56 and retained in integrated 0.2.61. That rejection disabled Convergence and left its items unloaded. The summon identifier remains `Convergence/GhostSamuraiSummon`; no save conversion or replacement item is introduced.

- Server-wide roster → field deployment → manual Ready → separate combat introduction is implemented. [Arena infrastructure](ARENA_INFRASTRUCTURE.md) owns admission, movement and cancellation.
- Phase I / II / III / Final survival, frozen-roster HP scaling, fixed-position Stack, Spread, Raid-owned Down/instant revival and terminal effects/rewards are implemented. [Encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md) and [recovery spec](encounters/first-severance/REVIVE_SPEC.md) own current behavior.
- [Doll Theater](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md) owns the four-cord suspension, mechanical socket and continuous sphere. Core square/X stamps are removed; player/gather circles, accepted hands and all-Ready capture remain.
- [Weapons](encounters/first-severance/WEAPONS.md) owns the same-count treasure-box reward (one of five weapons at 20% each), removal of class exchanges and the all-five-weapon Doll recipe. Original ground cels plus 16 broom/cast cels accompany staged circles and a Lacuna-style continuous beam. It is not a Raid participant. [Audio](AUDIO_CUE_SHEET.md#weapon-only-foley) owns 27 Raid-derived weapon masters; Raid sources/BGM are unchanged.
- Approved EigHt section-loop audio files remain unchanged. Phase handoff now occurs during transformation and caps the outgoing track fade before combat; [Audio sheet](AUDIO_CUE_SHEET.md) owns this policy and preparation silence.
- One-member admission defaults on only as a build-gated development aid. It adds no companion, invulnerability or solo-specific fight. Normal testing is multiplayer unless the user explicitly chooses solo.

## Verification state

**0.2.71** [evidence](evidence/2026-09-13-beam-video-reference.json): native Release build/install **0 errors / 4 existing CS8632 warnings**, 18 locales parsed, source stable during packaging, installed hash checked. Eight shader/export/integration guards pass. Actual compiled FNA/D3D11 old/new comparisons cover four widths and the accepted finite ribbon over a structured test background, with 160 motion samples; selected consecutive frames were visually inspected and excessive white clipping was reduced. This is not a game/FPS result. Domain/codec inputs are unchanged; previous passing results were reused. Runtime **not_run**: reload, all-beam warning/flow readability under combat overlap, Reduced Effects/UI107%/zoom and live hit-footprint acceptance. Six pre-existing locale edits are byte-for-byte preserved and uncommitted.

**0.2.70** [evidence](evidence/2026-09-13-flowing-lattice.json): integrated native Release build/install **0 errors / 4 existing CS8632 warnings**, 18 locales parsed, source stable during packaging, installed artifact hash verified. All 189 domain cases and protocol34's 324 codec round-trips / 50 malformed rejections pass; six shader/export guards pass. Actual compiled FNA/D3D11 preview uses the production pulse geometry across 120 motion samples: head, body and taper pass through the field without whole-corridor switching. This is not gameplay/FPS evidence. Runtime **not_run**: reload, lattice pacing and packet readability during combat, matching-peer/late-snapshot alignment, third-volley Spread, Reduced Effects/UI107%/zoom. Six pre-existing locale edits are preserved and uncommitted.

**0.2.69** [evidence](evidence/2026-09-13-ghost-samurai-phase3.json): isolated native package **0 errors / 4 existing CS8632 warnings**, 18 locales parsed, inherited shader exports verified. Twenty focused Ghost Samurai domain cases pass (circle AABB boundaries, body sweep, interception vs late dash, contact windows, immutable schedules). Installed-loader checks pass for both Global types and the unchanged summon. The production circle renderer was compiled with a GDI stroke adapter for eight offline panels; this is not native FNA or gameplay. Runtime **user-owned / not_run**: load, accessory timing, late-peer aim/body alignment, circle readability, cleanup/re-summon and FPS. The canonical main build is now installed: [installation evidence](evidence/2026-09-13-ghost-samurai-phase3-install.json) records 0 errors / 4 existing warnings, preserved local edits/Mod list, matching artifact hash and successful installed-loader checks. The merged tree matches the verified feature, so unchanged domain/codec inputs were not rerun.

**0.2.68** [evidence](evidence/2026-09-13-beam-ignition.json): native Release build/install **0 errors / 4 existing warnings**, 18 locales parsed, source unchanged during packaging; five shader/export guards passed. Actual compiled FNA/D3D11 material comparisons show the old forecast volume beside the new thin axis and longitudinal live flow; 80 motion samples also exercise the shared ignition envelope and accepted lattice ordering. Domain/codec/static results are recorded in that evidence. This is not an in-game/FPS result. Runtime **not_run**: reload, beam-width/readability acceptance under overlapping combat, peer order/late snapshots, Reduced Effects and UI107%/zoom. Six pre-existing locale edits remain preserved and uncommitted.

**0.2.67** [evidence](evidence/2026-09-13-raid-grand-presentation.json): native Release package **0 errors / 4 existing warnings**, all 18 locale files parsed, source stable during packaging; five focused shader/export guards passed. Compiled FNA/D3D11 passes were rendered with installed Luminance textures, including old/new narrow beams, broad forecast/live volumes, orb/wake and a rift. Runtime **not_run**: actual Mod reload, whole-Raid overlap/visibility and phase/end transitions, UI107%/zoom, Reduced Effects and frame-time. Six pre-existing locale edits remain preserved and uncommitted; detailed source/package/preview identities are in the evidence.

**0.2.66 / protocol31** [installation evidence](evidence/2026-09-13-ghost-samurai-playtest-install.json): main's tree matched the verified feature exactly; the normal-profile native build passed with **0 errors / 4 existing CS8632 warnings**, 18 locales parsed and inherited shader exports verified. The installed package hash matches its build record, and both Ghost Samurai Global types plus the unchanged summon type passed the installed loader's registration check. Convergence and Luminance were already enabled; no personal Mod-list or save edits. PR #8's GitHub Actions passed. The prior [combo evidence](evidence/2026-09-13-ghost-samurai-combos.json) retains fifteen passing focused cases and protocol31's **324 round-trips / 50 malformed rejections**; their unchanged inputs were not retested. No GUI, game or server play session was started. Full runtime loading, real equipment dodges, delayed-peer/late-join alignment and grid/wisp playability remain **user-owned / not_run**.

**0.2.65** [evidence](evidence/2026-09-13-pursuit-material-v2.json): native Release build/install **0 errors / 4 existing warnings**, 18 locales parsed, source stable during packaging. Four focused shader tests passed. Compiled FNA/D3D11 draws use installed dependency textures and actual corridor width: four-color old/new comparison plus 104 time-stepped frames, including warning/release/retirement and reduced-detail output. The rectangular fill and sinusoidal wires are absent from the new live material. This is an offline material check, not a playtest or FPS result. User-owned **not_run**: reload/autoload, real combat footprint/readability, peer UI107%/zoom and Reduced Effects. Six pre-existing locale edits remain preserved and uncommitted.

**0.2.64** [evidence](evidence/2026-09-13-pursuit-luminance.json): native Release build/install **0 errors / 4 existing warnings**, 18 locales parsed, source stable during packaging. Shader compiler/export validation, native FNA/D3D11 effect load and 24 offline draws passed; installed package contains the exact checked shader and excludes `.fx`/compiler metadata. Four focused shader tests and two build-preflight tests passed. No gameplay/network sources changed. Six pre-existing English locale edits were preserved. Actual Mod reload/autoload, eight-shot readability, peer 107% UI/zoom and Reduced Effects are **user-owned / not_run**; offline rendering is not game/FPS acceptance.

**0.2.63** [evidence](evidence/2026-09-13-doll-beams-and-stage-key.json): native build/install **0 errors / 4 existing warnings**, all **18** localization files parsed by installed tML Hjson. Selected domain filters **12 + 9 passed**, presentation guards **13 passed**, codec **324 round-trips / 50 invalid cases passed**. Static/catalog/YAML passed (66 docs / 629 files / 10 YAML). Five pre-existing English locale edits were preserved and included in the local package, not folded into the implementation commit. Runtime readability, audio, held-item activation and cancel/retry are **user-owned / not_run**.

**0.2.62** integrated native package is now installed: **0 errors / 4 existing CS8632 warnings**, all 16 locales parsed, source unchanged during build. Both Ghost Samurai Global types passed the installed loader's actual `ValidateType` against this installed package, and the unchanged summon type is present. Four stale presentation tests were repaired against the accepted weapon audio, Doll suspension, voice-return type and 34×34 portrait; all 23 Python tests, 172 domain tests, 324 codec round-trips and 50 malformed cases passed. GitHub Actions passed for both PR #4 and merged main. [Integration and cleanup evidence](evidence/2026-09-12-branch-cleanup-and-ci.json) identifies the package, CI runs and retired branches. The contributor's [load-fix evidence](evidence/2026-09-12-ghost-samurai-load-fix.json) retains the old-failure reproduction and isolated package history. Full Mod loading, saved-item restoration and gameplay remain user-owned and `not_run`; no GUI/game/server play session was started.

**0.2.61** integrated native package installed: **0 errors / 4 existing warnings**, 16 locales parsed, 10 Ghost Samurai and 3 re-summon tests passed, compiled protocol30 checks passed (324 round-trips / 50 malformed cases). [Integration evidence](evidence/2026-09-12-ghost-samurai-main-handoff.json) records both source lines and the combined artifact. Contributor-only [tuning](evidence/2026-09-12-ghost-samurai-tuning.json) and [visual evidence](evidence/2026-09-12-ghost-samurai-visuals.json) retain their original branch versions. Native load, transparency, re-summon and peer wisp alignment remain user-owned and unverified for this package.

**0.2.60** native package installed: **0 errors / 4 existing warnings**, 16 locales parsed, all four focused Doll companion tests passed. [Flight-fix evidence](evidence/2026-09-12-doll-owner-flight.json) records the former zero-vertical-speed grounding mistake, native-owner mode replication and runtime scope. Actual mount/hover/landing animation and peer playback remain user-owned and unverified.

**0.2.59** native package installed: **0 errors / 4 existing warnings**, all 16 localization files parsed by the installed Hjson parser. New target-rotation/bore tests passed; one stale pre-four-color Final test was corrected (168/169 initially, repaired case passed separately). Compiled protocol29 checks passed: 324 round-trips / 50 malformed cases. [Build evidence](evidence/2026-09-12-doll-core-rewards-0259.json) identifies the package and remaining runtime checks.

Latest [0.2.58 playtest](evidence/2026-09-12-playtest-0258.json): two solo sessions, one Final defeat and one victory; Stack 39/39 and Spread 45/45 succeeded, one treasure box spawned. This confirms the previous load fix, but not multiplayer synchronization or box opening. A separate server2 SubworldLibrary TagIO warning is recorded without changing that Mod or saves.

**0.2.58** fixes the reported **0.2.57 load failure**: an inline unquoted Hjson value swallowed its closing brace in both DollBeam locales. The [load-fix evidence](evidence/2026-09-12-doll-localization-load-fix.json) records the actual client exception, reproduction with the installed parser, all 16 locale files passing, two build-preflight tests and native packaging (0 errors / 4 existing warnings). Build now rejects malformed translations before touching the installed package. The subsequent 0.2.58 sessions above confirm client/server loading.

Previous **0.2.57** [evidence](evidence/2026-09-12-doll-treasure-broom-core.json): native build/install **0 errors / 4 existing warnings**; 3 companion and 5 presentation tests passed. PNG alpha/pivot/clipping and offline previews checked; 27 SFX measured, input preservation and deterministic reproduction checked. Box opening, broom/beam playback, weapon listening and suspension/Core readability remain **user-owned, not playtested**. Static/catalog/YAML checks passed (66 docs / 605 files / 10 YAML).

The **0.2.56** [re-summon fix](evidence/2026-09-12-ghost-samurai-resummon.json) is built and installed with **0 errors / 4 existing warnings**. The missing-Idle regression was reproduced before the fix; three focused re-summon tests and four affected existing terminal/replica tests now pass. Current wire bodies are unchanged. The [0.2.55 integration evidence](evidence/2026-09-12-ghost-samurai-integration.json) and contributor's [0.2.54 evidence](evidence/2026-09-12-ghost-samurai.json) preserve earlier build/codec checks. New Boss load succeeded on both peers in the latest 0.2.55 logs, but summon/end telemetry was absent; the reported second-summon failure matched the code defect. Native re-summon, placeholder readability, accessory compatibility and dodgeability remain user-owned and unverified for the fix.

Historical **0.2.53** [build evidence](evidence/2026-09-12-doll-companion-foley.json) preserves companion, weapon foley and rename checks. [Previous build evidence](evidence/2026-09-12-four-color-prism-frames.json) preserves Final beam/music/frame checks; these are not claims that the current package was playtested.

The main checkout remains the canonical source and its ModSources junction is unchanged. Existing English locale changes were preserved. The three affected weapon/box locales keep tML's rewritten layout alongside corrected text; five unrelated English locales remain uncommitted and are included in the local package. The contributor's separate source-layout record describes their earlier build, not this workstation. Consult each build manifest before claiming byte-identical reproduction.

Recent observed gameplay: [0.2.51 archived logs](evidence/2026-09-12-playtest-0251.json) contain one solo victory and six three-player all-Down defeats. Ten revives succeeded. The final multiplayer attempt reached Final and ended on a one-of-three Stack with two Downed members; its Final slicer had two nonlethal hits. Logs do not certify the new package or every peer's visuals/audio. Earlier runs remain in the [checkpoint history](history/2026-09-11-status-through-0246.md#verification-state).

Concrete unverified surfaces to select **when relevant**, not a mandatory retest queue:

- Latest audio: silent preparation → P1, new high-energy section during transformation with no old-track bleed into combat, section-loop seam, and BGM/SFX balance audition.
- Preparation/UI: a distant three-player roster, last Ready/unready/cancel, and a peer at 107% UI scale seeing aligned black exterior/letterboxes.
- Doll Theater: the short [acceptance list](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md#検証と残課題) covers NPC/Core lifecycle, capture/reveal, suspension and actual combat-scale readability.
- Production compatibility: ordinary lethal hits, reconnect/observer identity, outsider rules and release matrix remain separate gates.

## Constraints and deferred work

The user accepted the Raid combat body as the current development baseline; do not reopen every attack or invent Phase IV/V merely from old requests. Remaining polish is scoped by the next user request. Normal Terraria/Calamity lethal events are not converted to Raid Down; robust rejoin and adversarial movement/weapon validation are not implemented guarantees. General outsider admission/ejection remains broader than the development containment.

Calamity is still a hard dependency, including progression and Rogue/true-melee integration; targeting its endgame DPS does not eliminate that dependency. See [staged independence](adr/0006-staged-calamity-independence.md).

Development loot includes a companion summon weapon, but balance, public solo/NPC-party substitutes and final production acceptance are not settled. Source/asset license selection, exact-package release approval and current Workshop visibility must not be inferred from a build or an earlier “approval pending” report. [Release process](RELEASE_PROCESS.md) owns the gate and explicit development-publication exception process.

## Next change

Reload Mods/restart tModLoader to load the installed package; **no additional Build + Reload compilation is needed** for unchanged source. All participants should load the matching package. User-owned focused acceptance: sparse glints indicate the future width without a filled slab; eight-cast/Core/rotation/Stillness/Phase3/Final all show travelling currents, readable full live widths and no false safe gaps. Retain the prior lattice head/tail checks; Reduced Effects/UI107%/zoom and combat overlap remain unverified. Luminance is already installed. Do not replay unrelated weapon/loot or stage-ending checks for this presentation follow-up.

The Ghost Samurai work introduced in 0.2.69 is preserved in the current matching package. Its [focused smoke](encounters/ghost-samurai/ENCOUNTER_SPEC.md#検証と試遊) covers harmless rush art/preparation contact, late dash, 90-tick charges and fixed-center out/in/out/in steps. Gameplay remains user-owned and unverified.

Next feature work starts from the integrated `origin/main`, in a new scoped branch/worktree. [Contributing](../CONTRIBUTING.md#shared-development) owns shared development; do not resume implementation from the retired Ghost Samurai branch or overwrite the shared package from a feature worktree.

Retain the player-only Stack/Spread rings, renamed intro/bar and The Unbroken Promise companion. The stage NPC remains intact until all-Ready intro; minions do not replace missing Raid participants. Native rendering, listening and performance checks remain user-owned.

Keep current behavior and accepted art unless explicitly revising them. Choose checks from the [verification matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md); do not replay historical checklists.

## History

The [checkpoint through 0.2.46](history/2026-09-11-status-through-0246.md) preserves all preceding build/playtest evidence and superseded follow-up lists. [Pre-consolidation history](history/2026-09-07-pre-consolidation.md) preserves the earlier slices. Neither is the current work queue.
