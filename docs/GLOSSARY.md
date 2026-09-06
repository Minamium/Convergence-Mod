---
doc_id: project.glossary
document_type: glossary
status: accepted
owners:
  - project
last_reviewed: 2026-09-07
source_of_truth_for:
  - project.terminology
aliases:
  - names
  - identifiers
related_code:
  - Content/Encounters/FirstSeverance
related_docs:
  - encounter.first-severance.overview
  - encounter.first-severance.plan
---

# Glossary and Identifier Registry

| Concept | Current term | Status / rule |
|---|---|---|
| First Raid display name | `First Severance` / `第一断絶` | Accepted for development; public branding may still change before release |
| Feature directory/namespace | `FirstSeverance` | Current playable development feature; see [Status](STATUS.md) |
| Stable encounter key | `first_severance` | Current unpublished key |
| Legacy implementation name | `ThirdSeverance`, `Third Severance`, `third_severance` | Historical records and completed-rename instructions only; no source alias or compatibility mapping |
| Boss working title | `The Null Cantor` / `無響の唱導者` | Provisional; do not encode into stable protocol/save IDs |
| Collective/lore name | `The Choir Beneath the Ice` / `氷下の合唱体` | Provisional lore term, not the current boss name |
| Facility | Polar containment/research facility | Concept accepted; `Erebus Polar Citadel` and `Pale Meridian Containment Complex` are both unconfirmed names |
| Activation object | Foundation Core | Provisional display term; exact item/tile name and recipe TBD |
| Revival item | `Resuscitation Kit` / `蘇生キット` | Provisional display name; behavior is specified independently |
| Pylon | Authority-owned DPS-check NPC | One per frozen pull roster member: 2/3/4 |
| Stack / 頭割り | Fixed server-owned damage pool shared by valid marker occupants | Provisional required shares are 2/2/3 for 2/3/4 pull roster; Downed/disconnected players do not enter the divisor |
| Spread / 散開 | Pairwise-separation mechanic | Server resolves assigned living participants at deadline |
| Core exposure | Only normal boss-damage window | Boss HP persists across loops; no exposure damage quota |
| Downed | Raid-only recoverable incapacitation | Not vanilla death; excluded from mechanic targeting and damage |
| Eliminated | No longer recoverable in the active Raid | Control normalization at victory/cancel/defeat is an adapter decision |
| Overload | Pylon-failure pressure counter | Third stack causes Defeat in the current MVP spec |
| Fight ID | Exact identity of one active encounter | Every mutation and cleanup must match it |
| Encounter Sequence | World-monotonic encounter generation | Prevents delayed packets from reviving an older fight |
| Connection epoch | Server-owned generation for a participant binding | Protects against disconnect/rejoin and Terraria slot reuse |
| Generic end reason | Cross-encounter `EncounterEndReason` | Drives coordinator lifecycle and common cleanup; intentionally coarse |
| Feature terminal cause | Append-only byte `FirstSeveranceTerminalCause` | Preserves the exact local reason in snapshots, terminal events, and tombstones; must map to one compatible generic end reason |

## Naming migration rule

The repository is unpublished, so the target feature does not need a runtime compatibility alias. Nevertheless, migration must be atomic across directory, namespace, type/file names, stable key, failure prefixes, tests, project links, and active documentation. Explicit numeric packet IDs are never renumbered. Historical ADRs, research, and changelog entries are not globally rewritten.
