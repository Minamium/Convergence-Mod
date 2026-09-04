---
doc_id: project.status
document_type: status
status: accepted
owners:
  - project
last_reviewed: 2026-09-04
source_of_truth_for:
  - project.implementation_status
aliases:
  - current state
  - implementation inventory
related_code:
  - Common
  - Content/Encounters/ThirdSeverance
  - Tests/Convergence.DomainTests
related_docs:
  - handoff.windows
  - encounter.first-severance.plan
---

# Project Status

As of 2026-09-04, Convergence is a documented architecture bootstrap with tested dependency-free domains. It does **not** contain a playable Raid boss, and the encounter activation path intentionally fails closed.

## Implemented and connected

- tModLoader Mod source skeleton, feature registration, project metadata, Calamity hard reference for Stage A, and compatibility-version policy.
- Generic encounter identifiers, definitions, catalog, lifecycle transitions, one-session authority coordinator, runtime/factory boundary, cleanup scope, retry backlog, and terminal snapshot outbox.
- Versioned packet envelope parsing, explicit packet IDs, direction guard, bounded rejection behavior, and a safe no-op route. No gameplay packet handler is connected yet.
- Read-only encounter replica ordering by Encounter Sequence, revision, and authority tick, including terminal tombstones.
- Pure server/SP `Common/Raids/Revive` state machine: stable participants and connection epochs, Downed deadlines, batched revive arbitration, channel leases/nonces, token reservation/consumption, reconnect grace, same-tick wipe commit, bounded snapshots, projections, and exact-Fight cleanup.
- Dependency-free domain harness covering the revive domain and current immutable arena/Boss-plan objects.
- Repository policy, YAML validation, CI, ADRs, provenance rules, and repository-local Raid/source-research Skills.

## Implemented as a disconnected or obsolete bootstrap

- `Content/Encounters/ThirdSeverance` contains an immutable 320x140 Core-anchored arena blueprint, four deterministic Pylon slots, logical outsider policy, a cleanup-safe inert runtime, and a boundary around the revive service.
- The same module also contains a multipart seven-phase plan with Part Break, Targeted Line, Effigies, damage-budget exposure, and Last Stand. That plan is superseded as a product specification by [ADR-0007](adr/0007-first-severance-vertical-slice.md) and the [First Severance spec](encounters/first-severance/ENCOUNTER_SPEC.md); it remains code until the controlled Windows rename/simplification slice.
- `ThirdSeveranceAvailabilityPolicy` rejects activation. `InertThirdSeveranceWorldAdapter` does not spawn or mutate Terraria entities. These safety gates must remain until their replacement adapters are tested.

## Not implemented

- `FirstSeverance` directory, namespace, types, stable key, and failure-code rename.
- Foundation Core Item/Tile/Tile Entity; server Core resolution; world/progression/roster validation; Ready flow; live Barrier.
- Boss NPC, Pylon NPCs, authoritative phase executor, damage gate/collector, Stack/Spread resolver, projectiles, synchronized feature snapshot/deltas, or client presentation.
- Feature-neutral terminal descriptor, definition-owned external end mapping, or coordinator bridge that preserves a First Severance cause across World unload/runtime exception/fatal protocol shutdown.
- `Resuscitation Kit` ModItem, typed revive transport, tModLoader `ModPlayer` death/control adapter, life restoration projection, or Calamity death-hook coexistence behavior.
- Rewards, localization, production sprites/audio/VFX, tuning, release packaging, and a confirmed dependency matrix.
- Optional local SQLite/FTS/embedding cache generator; the committed Markdown/front-matter catalog exists, but no binary-search database is built or required.
- Standalone progression/content that would replace the current hard Calamity dependency.

## Verification state

| Gate | State |
|---|---|
| Repository policy checks | Passed on the Mac bootstrap before this handoff; rerun after checkout |
| YAML checks | Passed on the Mac bootstrap before this handoff; rerun after checkout |
| Dependency-free domain tests | Previously wired into CI; local Mac did not have the pinned .NET SDK for rerun |
| `dotnet build ConvergenceMod.csproj` under `ModSources/Convergence` | Not run |
| tModLoader Build + Reload | Not run |
| Single Player load | Not run |
| Host & Play | Not run |
| Dedicated Server load/2-client smoke | Not run |
| Calamity lethal-hook instrumentation | Not run; blocks the live Downed adapter |

Candidate versions remain unconfirmed until Windows produces evidence. See [Windows Handoff](handoff/WINDOWS.md).

## Current accepted direction

- Primary implementation/verification workstation: Windows desktop.
- MacBook: documentation, review, Git, lightweight domain edits; no runtime claim without the pinned game toolchain.
- First playable Raid: `First Severance` for 2–4 players after Exo Mechs and Supreme Calamitas.
- Active loop: Pylon DPS check → Stack → Spread → Core exposure → repeat while boss HP remains.
- Boss accepted boundary: a single simple NPC/body and life pool; the ring/arms concept is a provisional presentation placeholder, never a required breakable part.
- Recovery: Raid-only Downed plus an ally-used dedicated item, with server-owned channel and shared tokens.
- Long term: ship/validate the Calamity addon first, then migrate through the accepted staged path toward a standalone Content Mod.

## Next change

On Windows, first establish the real Build + Reload and Dedicated Server baseline. Then rename `ThirdSeverance` to `FirstSeverance` in one isolated commit while activation remains denied. Do not combine the rename with live actor spawning or death-hook interception.
