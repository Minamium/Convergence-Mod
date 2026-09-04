---
doc_id: verification.test-plan
document_type: plan
status: accepted
owners:
  - quality
  - networking
last_reviewed: 2026-09-05
source_of_truth_for:
  - verification.test_matrix
aliases:
  - test plan
  - multiplayer matrix
related_code:
  - Tests/Convergence.DomainTests
  - tools/repository_checks.py
  - Common/Raids/Revive
  - Content/Encounters/FirstSeverance/FirstSeveranceArenaValidation.cs
  - Content/Encounters/FirstSeverance/FirstSeverancePreparationStateMachine.cs
related_docs:
  - verification.evidence
  - encounter.first-severance.spec
  - project.status
---

# Test Plan

## Gate order

Run the applicable gates in this order:

1. documentation catalog/YAML and repository policy;
2. dependency-free domain harness;
3. command-line tModLoader build;
4. tModLoader Build + Reload;
5. Single Player load/smoke;
6. Host & Play;
7. Dedicated Server load and two-client smoke;
8. feature-specific 2/3/4-player, fault, latency, and cleanup matrix.

Do not progress with compile errors, unknown-packet exceptions, server crashes, stale Raid state, or unrecorded version drift. Store results using [Verification Evidence](evidence/README.md).

## Documentation/repository checks

```bash
python3 tools/docs_catalog.py --check
python3 tools/repository_checks.py
python3 tools/validate_yaml.py
```

Required outcomes:

- no duplicate `doc_id` or `source_of_truth_for` topic;
- no stale generated catalog, unknown status/type, missing relation/path, or malformed front matter;
- no broken relative Markdown links, case collisions, secrets/binaries/logs, invalid JSON/XML/YAML, dependency-direction regression, or missing packaging masks.

## Version and environment evidence

Every runtime run records:

- exact Git commit/cleanliness;
- OS and architecture;
- Terraria, full tModLoader version/branch, Calamity, Calamity Music, Addon version;
- .NET SDK and enabled Mod list;
- Calamity `.tmod` SHA-256 without redistributing it;
- client count and network conditions;
- an explicit result for each distinct gate.

Candidate versions become Confirmed only after command build, Build + Reload, Single Player, Dedicated Server, and two-client connection pass together.

Windows x64 is the primary implementation and first acceptance environment. macOS remains a valid tModLoader target rather than a forbidden platform: after the Windows baseline is stable and before a public release, run a pinned macOS Build + Reload/client-join smoke when hardware is available, then add at least one mixed supported-platform client/server topology. A Mac result never replaces the Windows Dedicated Server matrix, and an unaudited Mac setup remains `not_run`.

## Current bootstrap tests

- minimal Mod compiles against the candidate dependency set;
- client reload and Dedicated Server load are clean;
- unsupported Calamity version rejects encounter activation without crashing the Mod;
- the legacy/current availability policy rejects unimplemented activation;
- unload/reload leaves no coordinator/replica state;
- packet envelope rejects wrong protocol, direction, unknown type, and truncated Fight ID;
- no packet currently mutates gameplay until typed handlers are added.

## Rename slice tests

After `ThirdSeverance` → `FirstSeverance`:

- no current production/test/project reference retains the legacy namespace/key/failure prefix except deliberately historical docs/changelog;
- `FirstSeverance` definition registers exactly once with `first_severance`;
- all linked source files remain included/excluded correctly in Mod and domain projects;
- numeric packet IDs are identical before/after rename;
- activation remains rejected and world adapter remains inert;
- repository, domain, build, reload, and server baselines still pass.

## Immutable loop/domain tests

Replace legacy multipart/Last Stand assertions with:

| Case | Expected |
|---|---|
| default 2/3/4-player plan | reachable bounded Spawn/Pylon/Stack/Spread/Exposure/Reset graph |
| invalid timing/count/threshold | construction/validation rejection |
| Pylons all destroyed early | advance once to Stack |
| Pylon deadline with survivors | one failure pulse, one Overload, penalized exposure flag |
| third Overload | one Defeat, no next mechanic |
| Stack roster 2/3/4 | provisional required share count is exactly 2/2/3 |
| Stack pool arithmetic | fixed integer pool is distributed once across valid occupants in stable order; exact total is conserved |
| first Stack target invalidation | one new assignment revision, circular-forward frozen-roster replacement, and a fresh 180-tick telegraph |
| no replacement or second Stack target invalidation | one `TargetUnavailable` soft failure and then Spread; no further reissue |
| Spread overlapping pairs | each failed participant applied once |
| participant Downed before resolve | excluded from assignment/occupant/divisor; frozen required share threshold does not shrink |
| clean/penalized exposure deadline | gate closes and one Reset/next loop if HP remains |
| HP zero on exposure deadline tick | Victory takes precedence over closing/reloop |
| HP input outside exposure | no Boss life change |
| eight-exposure loop cap with HP remaining | one `Defeat(LoopCapExceeded)`; no hard-enrage/Last Stand edge |
| every feature terminal cause | exact generic/cause mapping, append-only byte value, and declared total priority |
| feature-owned first-slice terminal update | one direct End descriptor with generic reason + cause; no same-update transition and no mandatory `Resolving` tick |
| external terminal mapping | definition construction requires exact WorldUnload/InternalFailure/ProtocolFailure generic/cause mappings |
| terminal state receives later command/tick | no mutation |

## Arena/Core/preparation tests

The validator never mutates the World on failure.

| Case | Expected |
|---|---|
| valid 320x140 prospective area/Core TE | Preparing/AwaitingReady |
| world edge or safety-margin overlap | structured rejection |
| missing/mismatched Core TE or foundation | rejection; state unchanged |
| second managed encounter/Core activation | explicit exclusivity rejection |
| chest/important TE/protected tile/conflicting event | structured rejection |
| requester too far or stale nonce | rejection |
| 1 eligible player | roster-size rejection |
| 5+ candidates | explicit selection required; no automatic start |
| 2/3/4 participants Ready | one frozen roster/countdown/start |
| unready/timeout/cancel/Foundation Core break/disconnect | declared cleanup route |
| Active player break/explosion/wiring/liquid attempt on Foundation Core | rejected; Fight remains Active |
| injected unexpected Foundation Core Tile/TE loss during Active | one Invalidated/Abort and exact-Fight cleanup |
| explicit admin/debug abort | ordinary Abort cleanup; no special resource path |
| old Encounter Sequence/Fight packet | ignored/rejected |

The Slice 3A dependency-free harness covers warning-only validity, ordered fatal diagnostics, full-scan/foundation counts, deterministic Participant IDs, 1/5+/duplicate/initiator rejection, exact slot+epoch resolution, Ready/unready and shared per-participant nonce behavior, exact timeout/Core-loss/participant-loss cancellation, initiator-only cancel, exact-Fight cleanup, active/missing Core lease state, and progression-result invariants. Live Tile/TE hooks, transport, Barrier behavior, and installed Calamity `Mod.Call` remain integration gates rather than inferred passes.

Barrier cases: all edges, dash, hook, mount, knockback, recall/pylon/bed/Calamity teleport, server correction, outsiders, connection-epoch/slot reuse, and 100/200/300 ms RTT with loss. It must correct rather than kill and must not permanently rubber-band a valid participant.

## Normal-hit pipeline instrumentation gate

Before enabling Boss/Pylon damage, record Single Player, Host & Play (host and non-host attacker), and Dedicated Server behavior for representative melee, ranged, magic, summon/minion, rogue, projectile, penetration/multihit, crit, and Calamity-modified hits. For each row capture which process/hook observes permission, damage modification, actor life change, death/check-dead, ownership metadata, and `netUpdate`.

Acceptance requires that the chosen server/SP-observed seam consistently rejects wrong-Fight actors, nonparticipants, Pylon hits outside `PylonCheck`, and Boss hits outside `CoreExposure`, and counts a permitted actor life/death result once. No `ReportDamage` packet is introduced. Ambiguous behavior keeps activation denied. This matrix covers cooperative play with unmodified clients; it does not claim protection against arbitrary modified-client movement or hit replication.

## First Severance mechanic matrix

Run every relevant row for 2, 3, and 4 frozen pull participants on Host & Play and Dedicated Server. Repeat role-sensitive rows with host and non-host as target.

### Pylons

- correct 2/3/4 spawn count and symmetric server positions;
- any connected Alive participant can damage any Pylon through the measured normal-hit pipeline; nonparticipants and custom damage reports cannot advance it;
- simultaneous final hits produce one completion;
- deadline removes leftovers once and does not double-count Overload/pulse;
- count/health do not rescale after Downed/disconnect;
- Pylon entity missing/incorrect owner causes bounded abort/cleanup, not orphaned progress;
- third Overload defeats and publishes terminal state before actor removal.

### Stack

- zero-based loop-index/frozen-roster round-robin target, assignment revision, reissue-used flag, and resolve tick are identical on every client;
- frozen roster 2/3/4 requires 2/2/3 valid occupants respectively; Downed, Eliminated, and disconnected players never enter the count/divisor;
- the fixed raw damage pool is split by stable participant order using integer quotient/remainder, conserves the exact pool, and then uses normal mitigation per assigned share;
- exactly required, more than required, one-under, and target-only layouts resolve once; under-soak increases shares naturally, records soft failure, and adds no Overload or separate wipe command;
- target Downed/disconnect at assignment, before deadline, and exactly on deadline triggers one deterministic replacement with a new revision and full 180 ticks;
- no candidate or reissued target invalidation commits one `TargetUnavailable` soft failure/debuff, advances after terminal selection, and never reissues twice;
- latency/interpolation does not change the authority process's sampled position/result under the documented cooperative-client trust model.

### Spread

- all connected Alive receive unique participant assignments;
- no overlaps succeeds;
- one pair, chain, and all-overlap layouts fail each affected participant once;
- Downed/disconnected participant is removed before resolution;
- 7-tile marker/16-tile center-separation defaults are measured for arena feasibility;
- client-only marker drift does not affect authority.

### Core exposure

- shielded Boss rejects damage in intro/Pylon/Stack/Spread/Reset;
- clean Pylons grant 720 ticks and failed Pylons 360 ticks initially;
- HP persists over loops and no “missed damage budget” Overload occurs;
- Boss HP zero commits Victory once, including the exact closing tick;
- hits arriving after closure are rejected normally;
- expected representative party clears in roughly 4–6 clean exposures and about 2–4 minutes after tuning; 5–12 minutes is the eventual expanded-Raid target;
- missing Boss/damage-gate invariant routes through safe abort/cleanup.

## Cross-domain tick and terminal collisions

Run these through the owning feature reducer rather than calling a mechanic or Revive boundary as an independent coordinator owner:

| Same authority tick | Required result |
|---|---|
| permitted Boss hit reaches zero + exposure deadline | one Victory; no Reset/reloop |
| permitted Boss hit reaches zero + all participants become Downed | one Victory; no Defeat `EncounterEnded` |
| permitted Boss hit reaches zero + Downed/disconnect timeout | one Victory; Revive reason may remain diagnostic only |
| permitted Boss hit reaches zero + explicit admin/debug abort | one `Invalidated(AdministrativeAbort)`; no Victory |
| permitted Boss hit reaches zero + unexpected Foundation Core loss | one `AnchorDestroyed(FoundationCoreLost)`; no Victory |
| permitted Boss hit reaches zero + required Boss actor missing | one `EncounterActorMissing(BossActorMissing)`; no Victory |
| permitted Boss hit reaches zero + runtime invariant break | one `Invalidated(RuntimeInvariantBroken)`; no Victory |
| Stack/Spread lethal makes all available participants Downed + mechanic edge due | one Defeat; no next substate |
| third Overload + Pylon-to-Stack edge due | one Defeat; no Stack assignment |
| loop cap + Reset/Pylon edge due | one `Defeat(LoopCapExceeded)`; no enrage/Pylon |
| revive completion due + reviver becomes Downed before commit | channel invalidation wins, then the single commit decides failure |
| revive completion due + target deadline equality | existing domain deadline rule wins; no completion |
| valid preparation cancel + later nonterminal edge | one `Cancelled(UserCancelled)`; no activation |
| active-fight cancel request + Boss zero | cancel is rejected; ordinary terminal selection yields Victory |
| ordinary nonterminal mechanic result + no terminal candidate | exactly one declared next substate |

The feature-reducer suite must exercise every adjacent pair in its declared priority, not only the examples above. Every feature-owned terminal returns one direct End descriptor; it never requests `Resolving` and End in the same update. The terminal snapshot is published before actor/player cleanup and a delayed live revision cannot replace its tombstone.

## External coordinator termination tests

These bypass the feature reducer and exercise the planned immutable termination mapping and coordinator boundary:

| Coordinator event | Required result |
|---|---|
| `Reset(WorldUnload)` with an active Fight | one `WorldUnload + WorldUnload`; terminal projection before cleanup |
| runtime `Tick` throws after mutating no committed projection | discard any uncommitted gameplay result; one `InternalFailure + InternalFailure` |
| fatal protocol decision before a pending feature update is committed | one `ProtocolFailure + ProtocolFailure`; no gameplay terminal event |
| two external shutdown requests are injected at the same controlled boundary | `WorldUnload > InternalFailure > ProtocolFailure`; one terminal only |
| missing, duplicate, `None`, or incompatible external mapping | definition/plan construction rejection while activation remains denied |
| external terminal followed by delayed live revision | retained terminal tombstone wins; stale live state is rejected |
| runtime construction throws before session acceptance | bounded activation failure and cleanup; no fabricated live feature tombstone |

The coordinator must obtain external feature-cause metadata from immutable definition data and must not call a failed runtime `Tick` again. External endings do not return `EncounterRuntimeUpdate` and do not pass through `Resolving`.

For every `FirstSeveranceTerminalCause` value, round-trip the exact generic reason plus byte-valued feature cause through the full snapshot, terminal event/delta, and retained tombstone. In particular, a reconnect after loop-cap defeat must still render `Defeat + LoopCapExceeded`, not an undifferentiated Defeat. `None` is valid only while live; unknown/out-of-range values, an incompatible generic/cause pair, or a live terminal cause are rejected without mutation. The explicit numeric values are compatibility fixtures and may only be appended, never renumbered.

## Downed/Revive pure-domain tests

The existing harness must continue covering:

- 2/3/4 roster → 1/2/3 shared tokens;
- one idempotent lethal transition and same-tick all-Downed terminal commit;
- commands after a committed/stale tick rejected without ratcheting authority time;
- bounded complete revive-start batch, one stable reviver per tick, deterministic race winner independent of arrival order;
- channel completion at 120 ticks, not 119; deadline/interrupt wins at exact collision;
- token reservation prevents overcommit and consumption occurs only on completion;
- movement/damage/target/reviver invalidity cancels and releases reservation;
- exact channel nonce protects a newer lease from delayed cancel;
- Downed timeout, elimination, zero-token failure, disconnect/reconnect grace;
- newer epoch preserves stable participant state and defeats stale disconnect/slot reuse;
- terminal failure is the final event for its tick;
- exact-Fight cleanup twice succeeds; stale-Fight cleanup cannot clear current state;
- bounded projections/snapshots never expose mutable authority.

Run:

```bash
dotnet run --project Tests/Convergence.DomainTests/Convergence.DomainTests.csproj --configuration Release
```

The Slice 2 harness no longer links or asserts multipart forms/Last Stand. Future mechanic-executor tests extend the six-state plan without reintroducing those obsolete types.

## Lethal-hook instrumentation gate

Before live death interception, record Single Player, Host & Play (host/non-host), and Dedicated Server behavior for:

- all relevant tModLoader death-hook calls/order and return behavior;
- Calamity personal revive consumption, life/cooldown/immunity mutation;
- duplicate callbacks, same-tick multiple damage, combat text/death reason;
- player life/control/network sync and disconnect/rejoin;
- disarming interception during Defeat/body normalization.

If behavior is ambiguous, keep the adapter disconnected and activation denied.

## Revive integration matrix

- authority interception never runs as client truth;
- Downed blocks movement/item/combat/hook/mount/damage and Boss/mechanic targeting;
- valid item held-use within 8 tiles completes once;
- while channeling, control suppression blocks weapons/tools/other items but preserves the accepted revival-item lease/animation; applying `SuppressItemUse` never self-cancels;
- raw item switch/release observed before suppression cancels the exact lease once; repeated use cannot extend or complete it;
- move, damage, teleport, item change/release, mount/hook, range loss, target invalid, disconnect, or fight end cancels once;
- forged target, old Fight, old epoch, reused slot, old/zero nonce, no token, wrong item, and nonparticipant reject without mutation;
- two revivers racing for one target and one reviver switching targets remain deterministic;
- server restores 35% life, 180-tick invulnerability, 600-tick weakness once;
- host Downed, non-host Downed, two simultaneous Downed, all simultaneous Downed;
- the cross-domain collision table above passes with the same result on host and Dedicated Server;
- victory/cancel/Defeat/unload safely normalize Downed and Eliminated bodies;
- no permanent vanilla/Calamity flags after cleanup/reload.

## Packet robustness

- unknown type/protocol/direction and truncated/oversized/invalid enum/count/coordinate;
- custom payloads spoofing player, participant, Core, distance, item, DPS, life, result, or completion;
- spammed Activate/Ready/Snapshot/Revive with bounded logging and rate;
- duplicate/reordered/stale delta and old Fight/revision/epoch/nonce;
- snapshot gap recovery and late join/rejoin;
- malicious input never throws out of the server thread or partially mutates state.

Packet robustness proves the Convergence protocol boundary, not anti-cheat for Terraria's ordinary movement/combat replication. Any stronger adversarial-client requirement needs a separate threat model and acceptance matrix.

## Cleanup fault injection

Inject missing/exceptional cleanup participants: Foundation Core Tile/TE unexpectedly lost, Boss/Pylon already dead, projectile missing, Barrier absent, player slot reused, revive channel active, duplicate cleanup, stale Fight cleanup, World unload, and cleanup logger failure.

Final invariants:

- no active session/Fight/roster/assignment/channel/reservation;
- no owned NPC/Projectile or Barrier/player control projection;
- Foundation Core Idle if its Tile/TE still exists;
- terminal snapshot/tombstone remains observable;
- retry backlog blocks a new encounter until resolved, then a new Core can start;
- second exact cleanup is harmless and stale cleanup never touches a new Fight.

## Performance budgets

| Metric | Initial target |
|---|---:|
| Arena validation | <10 ms goal; investigate ≥50 ms |
| Active server update | <1 ms/tick average |
| Custom Raid traffic | <5 KB/s/client steady-state goal |
| Actor/projectile count | bounded; does not multiply decorative density by players |
| Cleanup | normally one tick; new fight blocked while retry remains |

Profile before optimizing. Client particles are not server actors.

## Visual/audio accessibility

- 1080p, 1440p, ultrawide; UI scale 100–150%;
- grayscale/color-vision and Reduced/Minimal VFX;
- no color-only Pylon/Stack/Spread/Downed marker;
- four-player marker readability over Boss/projectiles/damage text;
- audio disabled or desynchronized never changes results;
- Dedicated Server never initializes audio/graphics;
- every runtime asset has a provenance record.
