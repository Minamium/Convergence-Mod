---
doc_id: project.arena-infrastructure
document_type: spec
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-09
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

- width/height: current [arena blueprint](../Content/Encounters/FirstSeverance/FirstSeveranceArenaBlueprint.cs), shared by validation and presentation;
- Core anchor: floor center/base Y;
- World edge safety margin: 20 tiles;
- legacy logical Barrier inset: 2 tiles; active development containment uses full ArenaBounds, flush to the ground floor.
- requester interaction range: 12 tiles;
- participation: every active current-world server player, independent of distance;
- Ready timeout: 60 seconds after the initial field deployment finishes.

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

1. Idle is **the pedestal only**. Select **Theater Doll / 開演の人形** and left-click it (or right-click the Core while holding the Doll). This reusable stage key is a separate item from the ten-slot Doll Covenant companion, crafted at a Work Bench from 10 Silk and one Fallen Star. Client sends the selected equipment/control state before bounded `RequestActivate` coordinate/nonce intent.
2. Server derives sender, validates side/rate/basic coordinate, and creates only the validation path.
3. Feature resolves the actual server Core Tile Entity; request coordinates never become trusted anchors.
4. The definition's Core resolver checks the sender's actual selected Theater Doll and active/alive/non-ghost state, then Calamity progression, conflicting World activity, participant candidates, and pure prospective Arena validation. Missing Doll rejects with `first_severance.activation_requires_doll`; no item is consumed and no client claim bypasses this gate.
5. Failure returns structured issue codes and cleanup without World mutation.
6. Success enters generic `Preparing`, gathers the entire server roster into the validated field, and deploys the field/black exterior with a short HUD-suppressed cinematic. The Core-owned Doll actor appears on the plinth; the intact NPC remains there until the all-Ready introduction captures her.
7. After deployment, show the Ready panel; all members must manually accept. The server holds the all-Ready state briefly, then enters Active and the separate existing Boss-introduction cinematic.

The Core anchors/requests an encounter; it does not own lifecycle, actors, roster, or cleanup.

Ready needs no item once preparing. Failure/cancel/terminal/world reload returns the stage to its empty plinth; there is no persistent installed-Doll flag, resource cost or save migration. Native NPC AI sync owns the transient actor; existing preparation snapshots own deployment/roster and native selected-slot state owns the activation check. The wire DTOs and protocol are unchanged.

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
- the complete eligible active-world roster, within the compiled admission limits; see Participant/Ready policy.

The old Slice 3A warning-only interior is historical: current development containment requires clear solid-block airspace and supporting plinth ground. Non-solid fixtures/liquids and housing/spawn diagnostics follow the current typed validator, not an instruction to excavate or clear the world. Containers, another Core, a foreign Tile Entity, protected tiles, unsupported foundation, world conflicts, incomplete scans, Core mismatch, requester-distance failure and invalid roster remain rejection conditions. Read the validator for a reported issue; do not treat the old warning policy as current authority.

## Participant/Ready policy

- Candidate: every active current-world server player, independent of distance. Dead/ghost players or unresolved connection epochs block the whole start, rather than being silently omitted. Transport connections still loading their player into the world are not counted as active players.
- Development has the explicit build-gated one-member admission documented in the [encounter spec](encounters/first-severance/ENCOUNTER_SPEC.md#identity-and-scope) and [solo runbook](runbooks/SINGLE_OPERATOR_TESTING.md). This does not create a production solo mode.
- Five or more active players reject the whole start with an explicit capacity message; selection UI/partial-group launch is not implemented.
- Freeze the accepted roster's stable Participant IDs and current binding epochs before combat; do not shrink the denominator on invalidity.
- Ready belongs to Raid `Preparing`, supports unready, timeout, allowed initiator cancel, Foundation Core break/removal, and participant-loss cleanup. Any join/leave, epoch replacement or death during preparation cancels it with an explanation; reactivate the Core for a new complete roster. The denominator never shrinks behind the user's Ready consent.
- Ready/cancel commands bind the exact server slot plus connection epoch and a monotonic per-participant nonce. Rejected identity/nonce commands do not advance authority time.
- After `Active`, join-in-progress remains outside the frozen fight/next-pull; this change does not implement combat rejoin.

### Deployment and presentation — 0.2.41

The Fight-owned preparation runtime teleports every roster member to a separate slot above the plinth, requests the destination world sections first, and refreshes the same containment/infinite-flight capability used during combat. Geometry comes from authoritative ArenaBounds, never the client's Core sprite size. No NPC Boss is spawned until all Ready. Cleanup releases only matching-Fight movement capabilities on authority and replica; new world entry also clears them. Preparation snapshots refresh every30ticks for clock/lease repair.

The shared preparation timeline gives field deployment180ticks, then the full Ready window. Early Ready is rejected without changing the count. Each accepted Ready appears as a small world-anchored `Ready!` above that player's head. A physical-pixel Ready/unready button works anywhere within the field; Core right-click still works. All-Ready holds45ticks so the last acceptance remains visible; an unready or membership change cancels the hold. The field remains deployed when the second, combat-start HUD suppression begins instead of retracting and rebuilding.

The deployment cinematic and black exterior reuse the validated world-to-physical-pixel capture and `InterfaceScaleType.None`; head labels use the world transform. No persistent hideUI, zoom, inventory or control flag is changed. Reduced Effects/shake-off are respected. The current Boss body is the user-accepted development baseline; broader animated-background work remains separate.

Narrow API evidence: installed tModLoader2026.07.3.0/source666f69962d3bdffde54fc14025f02634965b4e7c XML, inspected2026-09-09: `RemoteClient.CheckSection(int,Vector2,int)` is server-only and must precede long-distance teleport; `Player.Teleport` uses top-left coordinates, and `MessageID.TeleportEntity` alone does not ensure destination sections exist. Independent assembly code calls CheckSection with surrounding sections, then Teleport and its normal broadcast. No third-party code/assets copied. Actual far-client loading and107% UI input remain user-owned checks.

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
