---
doc_id: project.arena-infrastructure
document_type: spec
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-05
source_of_truth_for:
  - first_severance.arena_infrastructure
aliases:
  - arena infrastructure
  - Foundation Core
  - logical Barrier
related_code:
  - Content/Encounters/FirstSeverance/FirstSeveranceArenaBlueprint.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceArenaAccessPolicy.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceArenaValidation.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceCoreResolver.cs
  - Content/Encounters/FirstSeverance/FirstSeverancePreparationStateMachine.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceRoster.cs
  - Content/Encounters/FirstSeverance/FoundationCore
related_docs:
  - encounter.first-severance.plan
  - project.architecture
  - research.first-severance-slice3-apis
---

# Arena Infrastructure

## Scope and current state

This document covers Foundation Core activation, server arena validation, roster/Ready, logical Barrier, Pylon placement inputs, and cleanup. Combat rules remain in the First Severance spec.

Current implementation and evidence belong to [Status](STATUS.md). The active development containment expansion in [ADR-0016](adr/0016-ground-containment-and-continuous-emission.md) and [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md) supersedes the earlier disconnected Slice 3A state, fixed 2x2-only placement and warning-only interior policy. No terrain is generated or removed. General outsider admission below remains the intended broader contract, not a claim of implemented development enforcement.

## Provisional coordinate model

- width: 320 tiles;
- height: 140 tiles;
- Core anchor: floor center/base Y;
- World edge safety margin: 20 tiles;
- legacy logical Barrier inset: 2 tiles; active development containment uses full ArenaBounds, flush to the ground floor.
- requester interaction range: 12 tiles;
- candidate participation radius: 80 tiles;
- Ready timeout: 60 seconds at 60 ticks/second.

The ranges and Ready timeout are provisional tuning, not protocol identity.

```text
coreCenter = logical center of authority-resolved Core Tile Entity
left       = coreCenter.X - width / 2
top        = coreBaseY - height
bounds     = Rectangle(left, top, width, height)
```

Perform geometry in tile coordinates and convert at the rendering/position adapter boundary. Exact size/anchor may be tuned before live implementation, but must change in code/spec/tests together.

## Activation flow

The definition-scoped resolver/preparation/combat adapters own this flow. The active development override, not historical slice gating, determines availability; see the owning feature spec.

1. Client Core interaction sends bounded `RequestActivate` intent with candidate coordinate/nonce.
2. Server derives sender, validates side/rate/basic coordinate, and creates only the validation path.
3. Feature resolves the actual server Core Tile Entity; request coordinates never become trusted anchors.
4. Authority checks Calamity progression, conflicting World activity, participant candidates, and pure prospective Arena validation.
5. Failure returns structured issue codes and cleanup without World mutation.
6. Success enters generic `Preparing` with Raid roster selection/Ready/countdown.
7. A frozen 2–4 roster and all Ready may progress only when the owning implementation slice is enabled.

The Core anchors/requests an encounter; it does not own lifecycle, actors, roster, or cleanup.

## Validator contract

The validator returns immutable bounds/issues/metrics and performs no Tile/liquid/entity mutation:

```text
ArenaValidationResult
  IsValid
  Bounds
  Issues[]           stable codes and coordinates, not localized text
  Metrics            scanned/solid/liquid/chest/TE counts and duration
```

Required checks:

- prospective bounds and safety margin fit the World without overflow;
- exact valid Core TE/logical center/base; active containment requires clear solid-block interior and supported plinth, not a manufactured full-width foundation;
- no second managed encounter/Core activation;
- no chest, another Core, important TE, protected structure, or forbidden conflict;
- no active Boss/invasion/Boss Rush conflict under the compatibility policy;
- requester is within allowed server-measured range;
- 2–4 eligible selected participants.

Interior solidity, liquids, wire/actuator, platforms/rope, World spawn, and NPC housing are explicit non-blocking warnings in Slice 3A. Containers, another Core, a foreign Tile Entity, dungeon/Lihzahrd protected tiles, foundation gaps, World conflicts, incomplete scans, Core mismatch, distance failure, and invalid/ambiguous roster size are fatal errors. This is prototype policy and must be retested before activation.

## Participant/Ready policy

- Candidate: connected/current-world/eligible and server-measured within the participation region.
- One player cannot start the release feature; development override, if any, must be explicit/non-release.
- Five or more candidates require explicit selection; never silently choose four.
- Freeze 2–4 stable Participant IDs and current binding epochs before combat.
- Ready belongs to Raid `Preparing`, supports unready, timeout, allowed initiator cancel, Foundation Core break/removal, and participant-loss cleanup.
- Ready/cancel commands bind the exact server slot plus connection epoch and a monotonic per-participant nonce. Rejected identity/nonce commands do not advance authority time.
- After `Active`, join-in-progress is spectator/next-pull until a separate accepted rule exists. Rejoin of a frozen participant uses the stable ID plus a newer server epoch.

## Pylon placement input

The arena exposes deterministic validated slots/coordinates, while the feature chooses the subset/layout by frozen roster:

- 2: left/right;
- 3: symmetric triangle;
- 4: quadrants/corners.

Exact offsets are provisional and tested for movement/telegraph space. The current legacy four-corner blueprint is an input to revise, not a rule that a 2-player pull still spawns four Pylons.

## Logical Barrier

No wall Tiles are generated.

- Client draws the boundary and may predictively clamp/inward-bias its local participant.
- Server validates resulting position and corrects to a safe inside point with exact Fight/binding identity.
- Participant escape: the current physical-containment override immediately clips the body inward without damage; it does not allow an escape-warning grace.
- Future general outsider entry policy: warning and Raid-interaction suppression, then safe ejection/exclusion for repeated violations; not enabled by the participant-only development field.
- Other-Mod teleport is handled by post-result validation; do not attempt an exhaustive item blacklist.

Authority adapters must cover ordinary movement, dash, hook, mount, knockback, recall/pylon/bed, server teleport, and Calamity/other-Mod teleport in tests. Correction revalidates current epoch immediately before applying.

## World protection

The development Core rejects block replacement and hammer/wire effects. Its replicated projection allows normal removal in Idle/Preparing and rejects ordinary mining/explosion while Active; water/lava do not destroy the object. Preparing removal is intentionally allowed so the connected runtime can convert the observed TE loss into cancellation. Do not claim every direct third-party `WorldGen` call can be intercepted. The final defense is exact-Core validation and cleanup, not destructive restoration from an unversioned snapshot.

## Foundation Core Tile/TE break and loss

- During Preparing, a valid Foundation Core break/removal routes once to Cancelled cleanup.
- During Active, normal player placement/break, explosion, wiring, and liquid attempts that would remove or invalidate the Foundation Core are rejected; rejection is not a terminal event.
- If the Foundation Core Tile or Tile Entity is nevertheless missing/mismatched after an authority validation pass, treat that as an unexpected invariant loss and commit one Invalidated/Abort reason before exact-Fight cleanup. Do not attempt destructive World restoration from an unversioned snapshot.
- An explicit admin/debug abort is an intentional command and routes through the same ordinary Abort cleanup; it is not simulated by deleting the Tile/TE.
- Tile and TE removal order may invoke observation/cleanup more than once, so terminal commit and exact-Fight cleanup are idempotent.
- Only server/SP commits the terminal outcome.

## Cleanup invariants

- generic lifecycle/session returns to Idle projection;
- no active Fight, roster/Ready state, assignment, Barrier, or participant projection;
- no exact-Fight temporary actor;
- revive channels/reservations released;
- Foundation Core Idle if its Tile/TE still exists;
- terminal snapshot/tombstone remains available;
- a new valid Core request can begin after retry backlog is empty.
