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

Development **0.2.65 / protocol 31** on `feat/ghost-samurai-combo-pressure`, incorporating integrated main `d9f8fb4`. [build.txt](../build.txt) owns version; [EncounterProtocol](../Common/Networking/Protocol/EncounterProtocol.cs) owns compatibility. The Ghost Samurai attack adjustment is built only in an isolated feature profile; shared installation awaits main integration. The main-side **0.2.64 / protocol30** native Release installation is recorded below, not a claim about every contributor's PC.

First Severance remains **不幸な人形劇 / The Unfortunate Doll Play**, Boss **ラクリモーサ — 縛られた心 / Lacrimosa — The Bound Heart**. Only the main eight-cast PursuitPrism uses an original Luminance-managed GPU material; [visual spec](encounters/first-severance/VISUAL_SPEC.md#luminance-prototype-boundary) owns the opt-in and shader boundary. Its colors, geometry, warning/live/end ticks and sounds are unchanged. Final, Spread's extra pursuit, weapons, other Doll attacks, activation/companion/rewards are retained from integrated main.

- Independent [Ghost Samurai / 幽鬼武者](encounters/ghost-samurai/ENCOUNTER_SPEC.md): four rapid two-hit pairs, narrower grid cells followed directly by a charged slash, late-locked charge/dash targeting, farther/faster dash passes and 1.5× base wisp-burst opportunities. Phase2/temporary Phase3 charged slashes are three-hit sequences with a longer third windup. Shared full aim snapshots keep forecasts and damage aligned; exact tuning lives in the feature spec. Phase state machine, summon, damage values, native hurt handling and exact-Fight cleanup remain. No Raid Ready/Down/revival, new loot/progression or independent solo redesign is added.
- Ghost Samurai presentation replaces the procedural skeleton with original generated body/sword-arm artwork, fixes the whole-MagicPixel source rectangle that enlarged stroke thickness by 1000, and adds warm high-contrast forecasts with pale-blue strikes. This display-only pass changes no combat or wire rules; the feature spec owns exact art/keying/border behavior.
- The shared Ghost Samurai GlobalNPC art cache is now static, fixing the load-time rejection introduced in 0.2.56 and retained in integrated 0.2.61. That rejection disabled Convergence and left its items unloaded. The summon identifier remains `Convergence/GhostSamuraiSummon`; no save conversion or replacement item is introduced.

- Server-wide roster → field deployment → manual Ready → separate combat introduction is implemented. [Arena infrastructure](ARENA_INFRASTRUCTURE.md) owns admission, movement and cancellation.
- Phase I / II / III / Final survival, frozen-roster HP scaling, fixed-position Stack, Spread, Raid-owned Down/instant revival and terminal effects/rewards are implemented. [Encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md) and [recovery spec](encounters/first-severance/REVIVE_SPEC.md) own current behavior.
- [Doll Theater](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md) owns the four-cord suspension, mechanical socket and continuous sphere. Core square/X stamps are removed; player/gather circles, accepted hands and all-Ready capture remain.
- [Weapons](encounters/first-severance/WEAPONS.md) owns the same-count treasure-box reward (one of five weapons at 20% each), removal of class exchanges and the all-five-weapon Doll recipe. Original ground cels plus 16 broom/cast cels accompany staged circles and a Lacuna-style continuous beam. It is not a Raid participant. [Audio](AUDIO_CUE_SHEET.md#weapon-only-foley) owns 27 Raid-derived weapon masters; Raid sources/BGM are unchanged.
- Approved EigHt section-loop audio files remain unchanged. Phase handoff now occurs during transformation and caps the outgoing track fade before combat; [Audio sheet](AUDIO_CUE_SHEET.md) owns this policy and preparation silence.
- One-member admission defaults on only as a build-gated development aid. It adds no companion, invulnerability or solo-specific fight. Normal testing is multiplayer unless the user explicitly chooses solo.

## Verification state

**0.2.65 / protocol31** [combo evidence](evidence/2026-09-13-ghost-samurai-combos.json): native build in an explicitly isolated compiler profile passed with **0 errors / 4 existing CS8632 warnings**, 18 locales parsed and inherited shader exports verified; source was unchanged during packaging. Fifteen focused Ghost Samurai cases passed, including eight-hit timing, charge/grid dodge boundaries, 1–4-player grid clearance, pose continuity, burst cadence and full aim-state rejection. Compiled protocol31 checks passed **324 round-trips / 50 malformed cases**. The later main merge changed neither those linked domain sources nor their inputs, so passing tests were reused; the package was rebuilt for main's Luminance dependency. Static checks are recorded with the evidence. No GUI, game or server play session was started. Actual dash equipment, delayed-peer/late-join alignment, grid/wisp load and playability remain **user-owned / not_run**.

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

Ghost Samurai: review and integrate the scoped **0.2.65 / protocol31** branch when the owner requests main integration, then build/install from integrated main as required by [shared development](../CONTRIBUTING.md#shared-development). All peers need the same protocol31 package. After installation, Reload Mods/restart; no second unchanged Build + Reload is needed. Test two-hit × four rhythm, immediate movement after the grid ends, late-lock charge/dash dodges, the three-hit Phase2 charge with its longer last hold and additional delayed wisps. Existing summon items remain valid. The feature package has not overwritten the shared playtest file or ModSources junction.

The **0.2.64 / protocol30** native Release package is installed from main. Reload Mods/restart tModLoader; another Build + Reload compilation is unnecessary. Confirm the main eight-cast sequence's colored forecasts → connected pressure release → dim residue, especially full-width readability, Reduced Effects and peer UI107%/zoom. Luminance is now a declared dependency (already installed locally). Other attacks are deliberately unchanged; expand the material only after user acceptance. Retained activation/companion smoke items are in the [previous evidence](evidence/2026-09-13-doll-beams-and-stage-key.json), not a new mandatory replay queue.

Ghost Samurai's focused smoke is listed above and in its [spec](encounters/ghost-samurai/ENCOUNTER_SPEC.md#検証と試遊); retain the repaired body/line rendering and re-summon cleanup. Retained Doll flight/beam/reward checks live in their evidence; an unrelated full Raid replay is not required.

Next feature work starts from the integrated `origin/main`, in a new scoped branch/worktree. [Contributing](../CONTRIBUTING.md#shared-development) owns shared development; do not resume implementation from the retired Ghost Samurai branch or overwrite the shared package from a feature worktree.

Retain the player-only Stack/Spread rings, renamed intro/bar and The Unbroken Promise companion. The stage NPC remains intact until all-Ready intro; minions do not replace missing Raid participants. Native rendering, listening and performance checks remain user-owned.

Keep current behavior and accepted art unless explicitly revising them. Choose checks from the [verification matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md); do not replay historical checklists.

## History

The [checkpoint through 0.2.46](history/2026-09-11-status-through-0246.md) preserves all preceding build/playtest evidence and superseded follow-up lists. [Pre-consolidation history](history/2026-09-07-pre-consolidation.md) preserves the earlier slices. Neither is the current work queue.
