---
doc_id: project.status
document_type: status
status: accepted
owners:
  - project
last_reviewed: 2026-09-12
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

Development **0.2.58 / protocol 29** on main. [build.txt](../build.txt) owns version; [EncounterProtocol](../Common/Networking/Protocol/EncounterProtocol.cs) owns wire compatibility. First Severance remains **不幸な人形劇 / The Unfortunate Doll Play**, Boss **ラクリモーサ — 縛られた心 / Lacrimosa — The Bound Heart**. Current changes are Doll-only: treasure box, broom companion with purple continuous beam, Raid-derived weapon sounds, connected unstable shell suspension and polished mechanical sphere. Raid rules, Ghost Samurai and the previous re-summon fix are unchanged.

- New independent [Ghost Samurai / 幽鬼武者](encounters/ghost-samurai/ENCOUNTER_SPEC.md): reusable summon item, three Phase1 attacks, delayed wisps and three-pass lateral slash in Phase2; Phase3 temporarily continues Phase2. Uses the existing encounter coordinator and exact-Fight cleanup, native player damage outcomes and procedural placeholder visuals. No Raid Ready/Down/revival or new loot/progression is added to this boss.

- Server-wide roster → field deployment → manual Ready → separate combat introduction is implemented. [Arena infrastructure](ARENA_INFRASTRUCTURE.md) owns admission, movement and cancellation.
- Phase I / II / III / Final survival, frozen-roster HP scaling, fixed-position Stack, Spread, Raid-owned Down/instant revival and terminal effects/rewards are implemented. [Encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md) and [recovery spec](encounters/first-severance/REVIVE_SPEC.md) own current behavior.
- [Doll Theater](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md) owns the four-cord suspension, mechanical socket and continuous sphere. Core square/X stamps are removed; player/gather circles, accepted hands and all-Ready capture remain.
- [Weapons](encounters/first-severance/WEAPONS.md) owns the same-count treasure-box reward, unchanged exchanges and 10-slot Doll. Original ground cels plus 16 broom/cast cels accompany staged circles and a Lacuna-style continuous beam. It is not a Raid participant. [Audio](AUDIO_CUE_SHEET.md#weapon-only-foley) owns 27 Raid-derived weapon masters; Raid sources/BGM are unchanged.
- Approved EigHt section-loop audio files remain unchanged. Phase handoff now occurs during transformation and caps the outgoing track fade before combat; [Audio sheet](AUDIO_CUE_SHEET.md) owns this policy and preparation silence.
- One-member admission defaults on only as a build-gated development aid. It adds no companion, invulnerability or solo-specific fight. Normal testing is multiplayer unless the user explicitly chooses solo.

## Verification state

**0.2.58** fixes the reported **0.2.57 load failure**: an inline unquoted Hjson value swallowed its closing brace in both DollBeam locales. The [load-fix evidence](evidence/2026-09-12-doll-localization-load-fix.json) records the actual client exception, reproduction with the installed parser, all 16 locale files passing, two build-preflight tests and native packaging (0 errors / 4 existing warnings). Build now rejects malformed translations before touching the installed package. A real client reload remains user-owned and unverified.

Previous **0.2.57** [evidence](evidence/2026-09-12-doll-treasure-broom-core.json): native build/install **0 errors / 4 existing warnings**; 3 companion and 5 presentation tests passed. PNG alpha/pivot/clipping and offline previews checked; 27 SFX measured, input preservation and deterministic reproduction checked. Box opening, broom/beam playback, weapon listening and suspension/Core readability remain **user-owned, not playtested**. Static/catalog/YAML checks passed (66 docs / 605 files / 10 YAML).

The **0.2.56** [re-summon fix](evidence/2026-09-12-ghost-samurai-resummon.json) is built and installed with **0 errors / 4 existing warnings**. The missing-Idle regression was reproduced before the fix; three focused re-summon tests and four affected existing terminal/replica tests now pass. Current wire bodies are unchanged. The [0.2.55 integration evidence](evidence/2026-09-12-ghost-samurai-integration.json) and contributor's [0.2.54 evidence](evidence/2026-09-12-ghost-samurai.json) preserve earlier build/codec checks. New Boss load succeeded on both peers in the latest 0.2.55 logs, but summon/end telemetry was absent; the reported second-summon failure matched the code defect. Native re-summon, placeholder readability, accessory compatibility and dodgeability remain user-owned and unverified for the fix.

Historical **0.2.53** [build evidence](evidence/2026-09-12-doll-companion-foley.json) preserves companion, weapon foley and rename checks. [Previous build evidence](evidence/2026-09-12-four-color-prism-frames.json) preserves Final beam/music/frame checks; these are not claims that the current package was playtested.

The main checkout remains the canonical source and its ModSources junction is unchanged. Six pre-existing English localization edits (DollCompanion, DollTheater, GhostSamurai, Preparation, RitualArmaments and root) are preserved, included in the local package and excluded from the fix commit. The contributor's separate source-layout record describes their earlier build, not this workstation. Consult each build manifest before claiming byte-identical reproduction.

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

Reload/restart peers onto **0.2.58 / protocol29**; no unchanged Build + Reload is needed. If the load error disabled Convergence, enable it again before reloading. First confirm the reported translation error is gone; retain the following unchanged playtest scope. This task continues Doll; Oni belongs to the user's separate task. Select only the changed surfaces: one treasure-box opening, broom flight/landing and purple beam, weapon sound listening, preparation/P1 exposure/P2–3 Core readability. Ghost Samurai's prior re-summon smoke remains pending, not a mandatory repeat for this Doll change.

Retain the player-only Stack/Spread rings, renamed intro/bar and The Unbroken Promise companion. The stage NPC remains intact until all-Ready intro; minions do not replace missing Raid participants. Native rendering, listening and performance checks remain user-owned.

Keep current behavior and accepted art unless explicitly revising them. Choose checks from the [verification matrix](../.agents/skills/develop-convergence-raids/references/verification-matrix.md); do not replay historical checklists.

## History

The [checkpoint through 0.2.46](history/2026-09-11-status-through-0246.md) preserves all preceding build/playtest evidence and superseded follow-up lists. [Pre-consolidation history](history/2026-09-07-pre-consolidation.md) preserves the earlier slices. Neither is the current work queue.
