---
doc_id: history.last-witness-v1
document_type: evidence
status: historical
owners:
  - gameplay
last_reviewed: 2026-10-03
source_of_truth_for: []
aliases:
  - Last Witness 0.2.34 score
related_code: []
related_docs:
  - encounter.first-severance.weapons
---

# Last Witness before the 2026-10 refresh

Frozen from `docs/encounters/first-severance/WEAPONS.md` at the integration base of the Doll weapon refresh (`8cb0f2e`), when the [2026-10 refresh](../encounters/first-severance/WEAPONS.md#rogue--last-witness) replaced it. This is historical wording, **not current behavior or a work queue**; the legacy `WitnessLitany`, `WitnessEcho`, `WitnessBlade` and `WitnessVerdict` types stay in the code, unused, until the shared legacy cleanup.

## Rogue — Last Witness (0.2.34)

Hold to load one suspended execution relic with six small pressure/cut beats at 29-tick intervals. Each still emits one small seeking shard from the real central apparatus, rather than another full weapon on an orbit. At 174–194 its edge loads inward; 24 ticks of braking/compression precede a single amplified returning blade at 218. Flight keeps the physical blade legible with a narrow textured wake instead of a broad spinning light sheet. Recovery completes at 282 (4.7 s total). Releasing before commitment cancels; holding can begin another full score after recovery. Native Calamity RogueWeapon hooks determine initial damage/stealth once; the final blade inherits the stored stealth flag. A stealth final blade retains the target-locking triangular verdict. The early fragments do not each receive another stealth execution.

From the initial power budget: Rogue pays six 0.28x early shards plus one 5.4x final returning-blade budget over 4.7 s, before native stealth; that blade splits 0.70/0.30 outbound/return.

Implementation facts at that point (for the change list): the shards left alternately 12 px either side of a crown 128 px out along the aim at 44 px/tick (22 per update; shards and blade updated twice a tick) and sought at 36 px/tick; the blade left the crown at 60 px/tick (30 per update), sought at 34 px/tick, spun 0.22 rad/tick, hit through a 32 px wide swept line, turned for home 42 ticks after the throw (or 8 ticks after its first hit) and was caught within 32 px of the player at 46 px/tick; releasing any time before the throw at 218 cancelled; a stealth blade cast the verdict 14–30 ticks into its flight on the nearest target. The relic and icon were the painted V3 apparatus art; sounds were the DollTheater `WitnessDraw`, `WitnessLock` and `WitnessFire` masters.
