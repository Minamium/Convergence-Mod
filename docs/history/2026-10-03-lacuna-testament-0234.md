---
doc_id: history.lacuna-testament-0234
document_type: evidence
status: historical
owners:
  - gameplay
last_reviewed: 2026-10-03
source_of_truth_for: []
aliases:
  - Lacuna Testament 0.2.34 sigil array
related_code: []
related_docs:
  - encounter.first-severance.weapons
---

# Lacuna Testament before the 2026-10 refresh

Frozen wording of the magic Doll reward weapon's section in [WEAPONS.md](../encounters/first-severance/WEAPONS.md#magic--lacuna-testament) from the 0.2.34 long-form ritual design until the 2026-10 refresh replaced it. **It is not current behaviour or an instruction to restore it.** The legacy `LacunaConvergence` and `LacunaRay` projectile types and `RitualGrandScore`'s magic members that implemented it remain in the code, unused by the item, until the five-weapon cleanup.

## Magic — Lacuna Testament (0.2.34)

Hold the trigger. One sigil begins firing after0.25s. Six more remain deployed above/below the player at108/183/233/269/296/316 ticks (successive gaps1.8/1.25/0.83/0.6/0.45/0.33s). Each independently fires a small seeking bolt every38 ticks from its actual drawn aperture. At340–362 the whole array accelerates into one large iris;362–410 is a0.8s charge with a quiet tension beat and sharply accelerating final compression.

At410 ticks (6.83s from start), **one persistent beam**, not repeated short projectiles, opens over7 ticks and stays on until release, empty mana, item change or incapacitation. It follows the player's moving hand; aim can turn continuously, capped at0.033rad/tick during sustain (0.075 beforehand). The sigil formation's initial facing is retained so turning across vertical cannot flip the entire array. Range2600px and full collision width116px. One hit per NPC root every10 real ticks; segmented NPCs cannot multiply the beam budget.

Native item use pays8 base mana once. Construction pays35% of the modified item mana cost, rounded up, every30 ticks. Sustain pays the modified item cost every8 ticks, including the release beat. Mana regeneration is delayed during use. Automatic missing-mana potion activation is explicitly blocked for this channel; manual potions remain normal. Zero-cost equipment modifiers are honored. Current weapon damage is reevaluated during sustain, including Mana Sickness. Release stops damage immediately and leaves only20 ticks of fading apparatus. A fresh press starts a fresh ritual.
