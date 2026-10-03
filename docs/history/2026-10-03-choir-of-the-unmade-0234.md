---
doc_id: history.choir-of-the-unmade-0234
document_type: evidence
status: historical
owners:
  - gameplay
last_reviewed: 2026-10-03
source_of_truth_for: []
aliases:
  - Choir of the Unmade 0.2.34
  - legacy choir concert
related_code: []
related_docs:
  - encounter.first-severance.weapons
  - project.status
---

# Choir of the Unmade — the 0.2.34 form

Frozen from [Weapons](../encounters/first-severance/WEAPONS.md) when the [2026-10 refresh](../encounters/first-severance/WEAPONS.md#choir-of-the-unmade-2026-10) replaced it. **It is not current behaviour.** The legacy types it describes (`ChoirSentinel`, `ChoirNote`, `ChoirRequiem`) stay registered but the item no longer spawns them; one cleanup change removes them later.

## Summon — Choir of the Unmade (0.2.34)

Each use summons one persistent,1-slot chorister. The oldest stable native identity conducts one shared target/score; adding voices does not restart it. Normal minion targeting, sacrifice and changing held weapons remain supported.

An11-second concert: independent seeking notes and progressive organ tiers for276 ticks;48-tick assembly;48-tick pressure/charge; **one shared three-second chorus beam** during372–552; disassembly/rest to660. Every living voice contributes its current damage to that one beam; individual notes stop during the chorus. No extra beam per slot. The chorus binds to its conductor's owner+identity, not a reusable projectile slot. Removing the conductor cancels that beam; a remaining voice takes over the next concert. Losing a valid target or becoming Downed/incapacitated cancels the score. Down does not delete the summoned slots.

The beam turns toward the native selected target at0.045rad/tick, reaches2000px, has92px full width and12-tick root immunity. The crown is physical moving art; all ornamental seal/pipe counts remain bounded independently of minion count.

From the initial power budget of the same section: Choir ordinary notes carry0.85x; shared chorus hits carry1.05x the sum of living voices every12 ticks (5082 raw/sec per968-damage voice **during the chorus**, not averaged over rest). The corrected baseline the refresh reproduces (per voice 1,797 raw/s averaged, 5,080 raw/s in the chorus) is in the [shared rules](../encounters/first-severance/WEAPONS.md#reward-refresh-2026-10--shared-rules).
