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

As of 2026-09-05, Convergence `0.1.2` fixes recovery-request decoding and circle rendering in the development-only First Severance combat experiment. The user reported a two-player Steam Raid clear on `0.1.1` after separating during Spread. That is user-reported gameplay evidence, not proof of successful revival or production completion. The hotfix still needs their GUI reload and targeted revive/render check.

## Playtest hotfix 0.1.2

- `0.1.1` server logs confirmed repeated `first_severance.revive_payload_size` rejection with `Read underflow 31 of 35 bytes`. Down and revive readers incorrectly treated the shared receive stream's remaining length as the packet payload size. Both now read exactly one uint nonce; header/sender/rate checks follow decoding. IDs, payloads and protocol version 2 are unchanged.
- The screenshot shows screen-length orange spokes during **Spread**, not a Down attack. Circle segments were stretching the full MagicPixel texture; drawing now samples an explicit 1x1 source rectangle. Rings and Down particles are presentation only. Spread still deals damage once to players closer than 16 tiles at resolution; damage tuning is unchanged.
- Authority logs now record phase transitions, damage source/life, Down/revive/cancellation/timeout events and exact terminal cause. No player names, addresses, positions or per-frame records are added. Diagnostics cannot interrupt cleanup or domain transitions.
- The client displays the localized ending cause in chat and for 10 seconds on the HUD, with more prominent Down instructions and a separate eliminated state. All-Downed still ends immediately; it does not wait for the 30-second personal deadline.
- Focused compiled-code checks reproduced both old shared-buffer failures and passed the fixed standalone/shared-buffer, zero-nonce and truncated-input cases. Rendering and the full live held-use revival remain user-run checks.
- Separate `0.1.1` logs also showed caught exceptions in Simple Whip `GoldRush_Shot` and Calamity `SepulcherMinion`. Their relationship to this fight is unproven; no third-party code or enabled-Mod settings were changed.

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
| Documentation catalog | Passed for the experiment (39 documents) |
| Repository policy checks | Passed for the experiment |
| YAML checks | Passed for the experiment |
| Dependency-free domain tests | 48 passed; validates reused domains, not the new Terraria adapters |
| `dotnet build ConvergenceMod.csproj` | `0.1.2` packaged successfully from the actual ModSources checkout, 0 warnings/errors |
| tModLoader Build + Reload | `0.1.1` confirmed in client log (0 errors, 2 nullable-context warnings); `0.1.2` awaits user GUI reload |
| Single Player load | Passed; Core placed/right-clicked and correctly returned `roster_too_small` for one player |
| Steam-friend preparation | `0.1.1` logs confirm local Host & Play server and two joined players |
| Combat/BGM/Down/revive in game | `0.1.1` clear user-reported; Spread screenshot inspected. Revival failed at packet decode; BGM and `0.1.2` live behavior remain unverified |
| Dedicated Server load/2-client smoke | Slice 3A 8-Mod server load/save/exit passed; two-client join not rerun; Slice 0 two-client baseline passed |
| Calamity lethal-hook instrumentation | Not run; blocks the live Downed adapter |

The confirmed runtime is Terraria `1.4.4.9`, tModLoader stable `v2026.07.3.0`, Calamity `2.2.4`, and Calamity Music `2.1`. See the [sanitized Windows baseline](evidence/2026-09-05-windows-baseline.json). The latest local development pack also loads Simple Whip Addon `1.15.12`, WingSlot Extra `1.4.5`, Cheat Sheet `0.7.8.1`, Magic Storage `0.7.0.11`, and its Serous Common Library `1.0.6.2` dependency. The subsequent `0.1.1` Host & Play observation above is separate from that earlier Dedicated Server baseline; it does not establish the full compatibility matrix.

## Current accepted direction

- Primary implementation/verification workstation: Windows desktop.
- MacBook: documentation, review, Git, lightweight domain edits; no runtime claim without the pinned game toolchain.
- First playable Raid: `First Severance` for 2–4 players after Exo Mechs and Supreme Calamitas.
- Active loop: Pylon DPS check → Stack → Spread → Core exposure → repeat while boss HP remains.
- Boss accepted boundary: a single simple NPC/body and life pool; the ring/arms concept is a provisional presentation placeholder, never a required breakable part.
- Recovery: Raid-only Downed plus an ally-used dedicated item, with server-owned channel and shared tokens.
- Long term: ship/validate the Calamity addon first, then migrate through the accepted staged path toward a standalone Content Mod.

## Next change

Reload `0.1.2` on the host and let the Steam friend synchronize the updated Mod. Check that Spread shows small circles, then have only one player run `/convergence-down` and the other hold the kit within 8 tiles for 2 seconds. If it fails, inspect the authority's `FirstSeverance` event lines and the displayed ending cause. The initiator may end with `/convergence-cancel`; GUI operations belong to the user. Do not rerun the expanded matrix for this scoped hotfix.
