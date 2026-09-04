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
related_docs:
  - encounter.first-severance.plan
  - project.architecture
---

# Arena Infrastructure

## Scope and current state

This document covers Foundation Core activation, server arena validation, roster/Ready, logical Barrier, Pylon placement inputs, and cleanup. Combat rules remain in the First Severance spec.

Current code provides a pure 320x140 blueprint, four corner Pylon slots, and outsider response policy under `FirstSeverance` names. It does not resolve a real Core TE or mutate the World. The plan-simplification slice must preserve tested geometry/ownership invariants while deriving active Pylon positions for 2/3/4 rosters.

## Provisional coordinate model

- width: 320 tiles;
- height: 140 tiles;
- Core anchor: floor center/base Y;
- World edge safety margin: 20 tiles;
- logical Barrier inset: 2 tiles.

```text
coreCenter = logical center of authority-resolved Core Tile Entity
left       = coreCenter.X - width / 2
top        = coreBaseY - height
bounds     = Rectangle(left, top, width, height)
```

Perform geometry in tile coordinates and convert at the rendering/position adapter boundary. Exact size/anchor may be tuned before live implementation, but must change in code/spec/tests together.

## Activation flow

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
- exact valid Core TE/logical center/base and continuous foundation;
- no second managed encounter/Core activation;
- no chest, another Core, important TE, protected structure, or forbidden conflict;
- no active Boss/invasion/Boss Rush conflict under the compatibility policy;
- requester is within allowed server-measured range;
- 2–4 eligible selected participants.

Interior solidity, liquids, wire/actuator, platforms/rope, spawn/bed, and NPC housing start as explicit warning/TBD policies rather than silent assumptions.

## Participant/Ready policy

- Candidate: connected/current-world/eligible and server-measured within the participation region.
- One player cannot start the release feature; development override, if any, must be explicit/non-release.
- Five or more candidates require explicit selection; never silently choose four.
- Freeze 2–4 stable Participant IDs and current binding epochs before combat.
- Ready belongs to Raid `Preparing`, supports unready, timeout, allowed initiator cancel, Foundation Core break/removal, and participant-loss cleanup.
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
- Participant escape: bounded warning then correction; never instant death.
- Outsider entry: warning and Raid-interaction suppression, then safe ejection/exclusion for repeated violations.
- Other-Mod teleport is handled by post-result validation; do not attempt an exhaustive item blacklist.

Authority adapters must cover ordinary movement, dash, hook, mount, knockback, recall/pylon/bed, server teleport, and Calamity/other-Mod teleport in tests. Correction revalidates current epoch immediately before applying.

## World protection

Before enabling combat, add authority-side protection for relevant placement/break/explosion/wiring/liquid paths plus a clear client reason. Do not claim every direct third-party `WorldGen` call can be intercepted. The final defense is validation/correction and cleanup, not destructive restoration from an unversioned snapshot.

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
