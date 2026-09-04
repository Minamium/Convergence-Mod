---
doc_id: project.status
document_type: status
status: accepted
owners:
  - project
last_reviewed: 2026-09-05
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

As of 2026-09-05, Convergence is a documented architecture bootstrap with tested dependency-free domains, the accepted First Severance loop encoded as immutable authority state, a connected Slice 3 preparation path, and a confirmed Windows runtime baseline. It does **not** contain a playable Raid boss; preparation can be requested, but the transition to combat remains intentionally closed.

## Implemented and connected

- tModLoader Mod source skeleton, feature registration, project metadata, Calamity hard reference for Stage A, and a Windows-confirmed compatibility-version policy.
- Generic encounter identifiers, definitions, catalog, lifecycle transitions, one-session authority coordinator, runtime/factory boundary, cleanup scope, retry backlog, and terminal snapshot outbox.
- Feature-neutral immutable termination descriptors carrying generic reason plus bounded schema/version/cause data; constructor-validated definition-owned mappings for World unload, internal failure, and fatal protocol failure; external preemption and replica/tombstone validation.
- Versioned packet envelope parsing, explicit packet IDs, direction guard, bounded rejection behavior, typed First Severance activate/Ready/cancel/snapshot handling, and initial/join snapshot delivery.
- Read-only encounter replica ordering by Encounter Sequence, revision, and authority tick, including terminal tombstones.
- Pure server/SP `Common/Raids/Revive` state machine: stable participants and connection epochs, Downed deadlines, batched revive arbitration, channel leases/nonces, token reservation/consumption, reconnect grace, same-tick wipe commit, bounded snapshots, projections, and exact-Fight cleanup.
- Dependency-free domain harness covering the revive domain, immutable arena objects and scan results, roster/Ready/Core lease boundaries, six-state loop policy/deadline behavior, Calamity gate-result invariants, terminal mapping, coordinator external endings, creation failure, and retained tombstones.
- Repository policy, YAML validation, CI, ADRs, provenance rules, and repository-local Raid/source-research Skills.

## Implemented First Severance foundation

- `Content/Encounters/FirstSeverance` contains an immutable 320x140 Core-anchored arena blueprint, four deterministic Pylon slots, logical outsider policy, a cleanup-safe inert runtime, and a boundary around the revive service.
- Slice 3 adds a development Foundation Core ModItem/2x2 ModTile/ModTileEntity with dedicated prototype pixel art, active-only mine/explosion protection projection, and an authority-owned exact-Fight lease. The item has no recipe; right-click submits a server/SP activation request and reports validation or Ready-count state in chat.
- An authority-only resolver derives the exact server TE from any Core coordinate, checks a 320x140 prospective arena without mutation, records deterministic fatal issues/warnings/metrics, and rejects requester distance, World conflict, foundation/protected/container/foreign-TE/Core conflicts, incomplete scans, and ambiguous participant counts.
- Current server-slot connection epochs feed a deterministic frozen 2–4 roster. The pure preparation state supports Ready/unready, per-participant nonce and exact binding checks, a provisional 60-second timeout, initiator cancel, Core loss, participant loss, a permanently closed combat gate, and exact-Fight idempotent cleanup.
- The coordinator now resolves Core/Arena/roster before acceptance, enters `Validating -> Preparing`, applies Ready/cancel intents on authority ticks, publishes bounded preparation snapshots, and releases the exact Core lease on cancel, timeout, Core loss, participant loss, unload, or failure.
- The current Development Build uses an explicit preparation-smoke validation mode: Core identity, bounds, requester, World conflict, 2–4 roster, and duplicate Core remain fatal, while unfinished foundation and existing container/Tile Entity/protected content are warnings because combat and Barrier mutation remain disabled. Strict validation remains the default API and must return before combat opens.
- The Calamity boundary queries only public `GetBossDowned`/`GetDifficultyActive` calls for Exo Mechs, Supreme Calamitas, and Boss Rush, validates boolean returns, and fails closed on missing/changed behavior. Public `2.2.2` source is reference-only for the installed `2.2.4` binary, so runtime call verification remains required.
- The feature owns a validated `SpawnIntro -> PylonCheck -> Stack -> Spread -> CoreExposure -> Reset` plan and pure high-level loop state machine. It encodes 2/3/4-player Pylons, provisional Stack shares `2/2/3`, persistent Boss life, normal/penalized exposure deadlines, three-Overload Defeat, eight-exposure `Defeat + LoopCapExceeded`, and no damage quota or enrage phase.
- `FirstSeveranceTerminalCause` has append-only byte values `0..13` and exact generic mappings. Feature-owned runtime End, World unload, runtime exception, and queued fatal-protocol termination preserve the descriptor through terminal snapshot, cleanup context, outbox, and retained replica tombstone.
- `FirstSeveranceAvailabilityPolicy` permits validation/preparation, while the preparation runtime's combat gate remains closed. `InertFirstSeveranceWorldAdapter` still cannot spawn or mutate combat entities.

## Not implemented

- Live logical Barrier presentation/correction and progression-call runtime instrumentation.
- Boss NPC, Pylon NPCs, authoritative phase executor, damage gate/collector, Stack/Spread resolver, projectiles, synchronized feature snapshot/deltas, or client presentation.
- `Resuscitation Kit` ModItem, typed revive transport, tModLoader `ModPlayer` death/control adapter, life restoration projection, or Calamity death-hook coexistence behavior.
- Rewards, localization, production sprites/audio/VFX, tuning, and release packaging.
- Optional local SQLite/FTS/embedding cache generator; the committed Markdown/front-matter catalog exists, but no binary-search database is built or required.
- Standalone progression/content that would replace the current hard Calamity dependency.

## Verification state

| Gate | State |
|---|---|
| Documentation catalog | Passed for Slice 3A on Windows |
| Repository policy checks | Passed for Slice 3A on Windows |
| YAML checks | Passed for Slice 3A on Windows |
| Dependency-free domain tests | 48 passed for Slice 3A on Windows |
| `dotnet build ConvergenceMod.csproj` | Passed for the Slice 3 preparation transport with 0 warnings/errors |
| tModLoader Build + Reload | Passed for Foundation Core preparation transport |
| Single Player load | Passed; Core placed/right-clicked and correctly returned `roster_too_small` for one player |
| Host & Play | Not run |
| Dedicated Server load/2-client smoke | Slice 3A 8-Mod server load/save/exit passed; two-client join not rerun; Slice 0 two-client baseline passed |
| Calamity lethal-hook instrumentation | Not run; blocks the live Downed adapter |

The confirmed runtime is Terraria `1.4.4.9`, tModLoader stable `v2026.07.3.0`, Calamity `2.2.4`, and Calamity Music `2.1`. See the [sanitized Windows baseline](evidence/2026-09-05-windows-baseline.json). The latest local development pack also loads Simple Whip Addon `1.15.12`, WingSlot Extra `1.4.5`, Cheat Sheet `0.7.8.1`, Magic Storage `0.7.0.11`, and its Serous Common Library `1.0.6.2` dependency. Host & Play remains explicitly `not_run` and is not inferred from the Dedicated Server result.

## Current accepted direction

- Primary implementation/verification workstation: Windows desktop.
- MacBook: documentation, review, Git, lightweight domain edits; no runtime claim without the pinned game toolchain.
- First playable Raid: `First Severance` for 2–4 players after Exo Mechs and Supreme Calamitas.
- Active loop: Pylon DPS check → Stack → Spread → Core exposure → repeat while boss HP remains.
- Boss accepted boundary: a single simple NPC/body and life pool; the ring/arms concept is a provisional presentation placeholder, never a required breakable part.
- Recovery: Raid-only Downed plus an ally-used dedicated item, with server-owned channel and shared tokens.
- Long term: ship/validate the Calamity addon first, then migrate through the accepted staged path toward a standalone Content Mod.

## Next change

Run Host & Play with one Steam friend: keep both players alive within 80 tiles of the Core, start preparation, then have both right-click until chat reports `Ready: 2/2`. This validates the current Raid-start boundary only; combat remains closed. Afterward, continue Slice 3 with logical Barrier presentation/correction.
