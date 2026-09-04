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

As of 2026-09-05, Convergence `0.1.1` contains a development-only First Severance combat experiment on the confirmed Windows baseline. All Ready now starts a Boss/Pylon loop with Stack/Spread markers, vanilla Boss 3 music, experimental Downed, and an ally-held revive kit. This is a multiplayer playtest build, not the completed production Raid. The preceding `0.1.0` preparation build reached Ready with the user's Steam friend; the new combat/revive runtime still requires their in-game check.

## Development combat experiment

- Authority composes the existing loop and revive domains after Ready; Boss/Pylons are tracked by NPC slot/type and a per-Fight token. Observed `OnKill`, not arbitrary disappearance, supplies actor deaths. Normal NPC hit gating is provisional for cooperative clients, not a verified anti-cheat boundary.
- `SpawnIntro -> PylonCheck -> Stack -> Spread -> CoreExposure -> Reset` runs with the existing timers. Boss HP is 1,200,000 and each Pylon has 25,000 HP for this experiment. Only exposed Boss/current Pylons accept player hits.
- Stack follows a round-robin Alive target and shares a fixed experimental HP-damage pool (90% of the pull roster's average maximum HP). Spread hits each overlapping Alive participant once for 40% maximum HP. Failed Pylons pulse 25%, clamped nonlethal. These direct Raid-owned HP changes intentionally do not claim the unmeasured Terraria mitigation/death pipeline. Stack target reissue and a live Barrier remain deferred.
- `/convergence-down` requests the sender's experimental Downed state. Raid-owned damage can also Down a participant without sending lethal HP through ordinary death hooks. The shared service owns the 30-second Down deadline, 2-second channel, 1/2/3 tokens, 35% restored HP, and post-revive timers.
- Kit starts validate the current sender binding, Alive state, held item, range and available token. The server chooses the nearest unreserved Downed ally; channels check held use, movement, damage, hooks/mounts and range. Health corrections have a per-participant revision and are applied on the owning client as well as authority. Weakness reduces generic damage by 20% for 10 seconds.
- The initiator may use `/convergence-cancel` during combat. Victory, Defeat, cancel, Core/actor loss, unload and exceptions clean owned NPCs and player projections. Ordinary Terraria death or a roster disconnect aborts this experiment; reconnect/re-entry is not enabled. Cleanup restores incapacitated players to at least 35% HP.
- Protocol v2 adds bounded Down/revive requests and a combat section to full snapshots. Clients render rings, countdowns and Boss 3 music. No audio file is extracted or packaged.
- Foundation-smoke validation remains explicit for this development experiment because it does not edit World terrain, build a Barrier, or grant progression/rewards. Production arena and lethal-hook gates are not declared complete. See [ADR-0009](adr/0009-development-combat-experiment.md).

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

- `Content/Encounters/FirstSeverance` contains an immutable 320x140 Core-anchored arena blueprint, logical outsider policy, preparation/combat runtimes, and a boundary around the revive service.
- Slice 3 adds a development Foundation Core ModItem/2x2 ModTile/ModTileEntity with dedicated prototype pixel art, active-only mine/explosion protection projection, and an authority-owned exact-Fight lease. The item has no recipe; right-click submits a server/SP activation request and reports validation or Ready-count state in chat.
- An authority-only resolver derives the exact server TE from any Core coordinate, checks a 320x140 prospective arena without mutation, records deterministic fatal issues/warnings/metrics, and rejects requester distance, World conflict, foundation/protected/container/foreign-TE/Core conflicts, incomplete scans, and ambiguous participant counts.
- Current server-slot connection epochs feed a deterministic frozen 2–4 roster. The pure preparation state supports Ready/unready, per-participant nonce and exact binding checks, a provisional 60-second timeout, initiator cancel, Core loss, participant loss, a permanently closed combat gate, and exact-Fight idempotent cleanup.
- The coordinator now resolves Core/Arena/roster before acceptance, enters `Validating -> Preparing`, applies Ready/cancel intents on authority ticks, publishes bounded preparation snapshots, and releases the exact Core lease on cancel, timeout, Core loss, participant loss, unload, or failure.
- The Development Build keeps Core identity, bounds, requester, World conflict, 2–4 roster and duplicate Core fatal; foundation/content checks are warnings only in the explicitly scoped experiment above. Strict validation remains the default API.
- The Calamity boundary queries only public `GetBossDowned`/`GetDifficultyActive` calls for Exo Mechs, Supreme Calamitas, and Boss Rush, validates boolean returns, and fails closed on missing/changed behavior. Public `2.2.2` source is reference-only for the installed `2.2.4` binary, so runtime call verification remains required.
- The feature owns a validated `SpawnIntro -> PylonCheck -> Stack -> Spread -> CoreExposure -> Reset` plan and pure high-level loop state machine. It encodes 2/3/4-player Pylons, provisional Stack shares `2/2/3`, persistent Boss life, normal/penalized exposure deadlines, three-Overload Defeat, eight-exposure `Defeat + LoopCapExceeded`, and no damage quota or enrage phase.
- `FirstSeveranceTerminalCause` has append-only byte values `0..13` and exact generic mappings. Feature-owned runtime End, World unload, runtime exception, and queued fatal-protocol termination preserve the descriptor through terminal snapshot, cleanup context, outbox, and retained replica tombstone.
- `FirstSeveranceAvailabilityPolicy` permits the development path; the production inert world adapter remains unused. The preparation domain itself remains inert, and its composing runtime starts the experimental adapter only after all Ready.

## Not implemented

- Live logical Barrier presentation/correction and progression-call runtime instrumentation.
- Production normal-hit collector/mitigation, Stack target reissue, general projectile attacks, robust observer/rejoin support, and expanded multiplayer/latency verification.
- General lethal-hit interception and Calamity self-revive coexistence. No `PreKill` hook is connected.
- Rewards, production sprites/audio/VFX, final tuning, and release packaging. Experimental UI is localized in English/Japanese.
- Optional local SQLite/FTS/embedding cache generator; the committed Markdown/front-matter catalog exists, but no binary-search database is built or required.
- Standalone progression/content that would replace the current hard Calamity dependency.

## Verification state

| Gate | State |
|---|---|
| Documentation catalog | Passed for Slice 3A on Windows |
| Repository policy checks | Passed for Slice 3A on Windows |
| YAML checks | Passed for Slice 3A on Windows |
| Dependency-free domain tests | 48 passed for Slice 3A on Windows |
| `dotnet build ConvergenceMod.csproj` | Combat experiment: pending final packaging; preliminary compile passed |
| tModLoader Build + Reload | Passed for Foundation Core preparation transport |
| Single Player load | Passed; Core placed/right-clicked and correctly returned `roster_too_small` for one player |
| Steam-friend preparation | User reported Ready reached; exact topology/logs not independently captured |
| Combat/BGM/Down/revive in game | Not run for `0.1.1`; user controls the GUI |
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

Reload `0.1.1` on the host and let the Steam friend synchronize the updated Mod. Both Ready starts the experiment. Observe one loop, have one player run `/convergence-down`, then let the other hold the kit within 8 tiles for 2 seconds. The initiator may end with `/convergence-cancel`. Do not infer production or expanded multiplayer compatibility from this one playtest.
