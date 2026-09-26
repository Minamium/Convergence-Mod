---
doc_id: verification.test-plan
document_type: plan
status: accepted
owners:
  - quality
  - networking
last_reviewed: 2026-09-21
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

## Select a contract, not the whole matrix

The [verification selector](../.agents/skills/develop-convergence-raids/references/verification-matrix.md) owns commands and routine completion. [Status](STATUS.md) owns actual results; [Release Process](RELEASE_PROCESS.md) owns full release gates. This is a case library, not an every-edit checklist.

Current [encounter](encounters/first-severance/ENCOUNTER_SPEC.md) and [recovery](encounters/first-severance/REVIVE_SPEC.md) specifications define behavior. Read tuning from their linked code; do not copy HP, radii, delays or versions into test instructions. Test expectations are executable in the linked-source harness.

## Repository-runnable checks and CI

- The [domain project](../Tests/Convergence.DomainTests/Convergence.DomainTests.csproj) links actual production domain/codec sources without Terraria or dependency binaries.
- Feature-grouped test files use `[DomainTest("contract name")]`. Discovery is automatic, ordered by unique name and fails if empty or duplicated; there is no second registration list to maintain.
- Pass `-- --list` or `-- --filter "name substring"` to `dotnet run --project Tests/Convergence.DomainTests/Convergence.DomainTests.csproj --configuration Release`. An unmatched filter fails, rather than reporting a false pass. Use the built DLL directly if inputs have not changed.
- [check-codec.ps1](../tools/check-codec.ps1) takes an explicit compiled assembly and records its hash. PowerShell 7+ is required. Public candidates, like development builds, must pass enabled solo admission. [check-package-admission.ps1](../tools/check-package-admission.ps1) inspects the actual TMOD without a game load; `tools/dev.py` runs it automatically. `-InspectOnly` is for diagnosing historical artifacts, never a publication gate.
- For native-Hurt/teardown changes, [check-native-hurt.ps1](../tools/check-native-hurt.ps1) takes `-PackagePath`, `-TModLoaderPath` and an ignored `-AssemblyOutput`. It checks installed HurtModifiers math and the packaged Doll player/buff types and exact-Fight cleanup, then extracts that package's DLL for the codec check. It does not start a game or prove installed accessory hooks/multiplayer behavior.
- For Azure shared-HP/sky lifecycle changes, run `pwsh -NoProfile -File tools/check-azure-lifecycle.ps1 -PackagePath <candidate.tmod> -TModLoaderPath <installed/tModLoader.dll>`. Actual native StrikeNPC/CheckDead and SkyManager methods exercise nonlethal sharing, lethal/late-hit retention, exact-Fight isolation and reset/deactivation/retry/fade-tail recovery. Optional `-ExpectOldFailure` reproduces the old0.3.33 defects; it is not a candidate pass. This isolated engine fixture does not play a world or certify accessories, network transport or on-screen acceptance.
- [Tool tests](../tools/tests/test_development_tools.py) exercise verifier option exclusion, build reuse, fail-fast/exit propagation and source identity without starting builds or network calls.
- [Cinematic coordinate guards](../tools/tests/test_presentation_contracts.py) keep start/phase/result callbacks in unscaled physical-viewport space; run through the same `unittest discover -s tools/tests` CI entry. These prevent the107% double-scale regression but do not replace actual screen checks.
- [Normal CI](../.github/workflows/repository-checks.yml) runs static, tool, linked domain and compiled codec checks on push/PR, with no game installation.
- The separate manual `mod_build` job is restricted to trusted main and a provisioned Windows runner labelled `convergence-tml`. The owner must supply pinned `TML_PATH`, an existing disposable `TML_SAVE_PATH/Mods` profile and the Python/.NET toolchain. It verifies solo-enabled admission in the actual candidate and records a package; it does not launch or certify a game session. Runner provisioning and execution are not implied by this workflow file.

For an already source-identified package, run the codec script against that build's `Convergence.dll`; do not rebuild simply to run the same contract. Linked codec success does not exercise tModLoader hooks or network transport.

## Evidence and gate boundaries

When applicable: static checks → pure/codec checks → actual package build → load/reload → affected user-owned playtest. Build + Reload is an alternative packaging/load route, not a requirement to compile an identical CLI package twice.

Record source commit plus uncommitted file identity, toolchain/dependency versions, artifact hash and concrete result using [Evidence](evidence/README.md). Reference an unchanged environment record. Multiplayer evidence also needs topology, frozen participant count, relevant Mod list and network conditions. Do not reinterpret missing results as successes.

Runtime/dependency changes and public release require the wider pinned-platform/load/server/compatibility gates. Windows remains primary; macOS and mixed-platform acceptance remain separate when available. A domain run is not a Mod build, package build is not live loading, and Host & Play is not Dedicated Server.

## Arena/Core/preparation tests

See [arena tests](../Tests/Convergence.DomainTests/ArenaDomainTests.cs), [preparation tests](../Tests/Convergence.DomainTests/PreparationDomainTests.cs) and [solo policy tests](../Tests/Convergence.DomainTests/SoloDebugTests.cs).

- Valid ground-anchored Core and clear field accept preparation; world-edge, protected structure, important tile/entity, progression or conflicting-event failures leave the world unchanged.
- Ready revalidates the same Core, clearance, roster and compiled solo-admission policy as initial activation.
- Roster IDs are deterministic; duplicate/ambiguous membership is rejected. Development solo is explicit, manually Ready and never grants invulnerability.
- Sender slot/epoch and nonzero nonce must match the current binding; stale or repeated intents cannot change readiness.
- One frozen roster/countdown/start; unready, timeout, Core loss, participant loss and authorized preparation cancellation follow the declared exit.
- Active Core break/explosion/wiring/liquid attempts preserve its lease. Unexpected Tile/TE loss aborts safely.
- Exact-Fight cleanup releases the lease once; stale cleanup cannot unlock a different Fight.

Live containment checks when affected: all field edges with dash/hook/mount/knockback/teleport, complete player-body clearance, restored flight/control after exit, and host/non-host position agreement. Outsider policy and durable rejoin are deferred production work, not inferred from clamping tests.

## Current combat contracts

### Phase progression, actors and HP

See [stage tests](../Tests/Convergence.DomainTests/BossStageTests.cs), [choreography](../Tests/Convergence.DomainTests/ChoreographyTests.cs), [scaling/salvos](../Tests/Convergence.DomainTests/EclosionSalvoTests.cs) and [damage telemetry](../Tests/Convergence.DomainTests/DamageTelemetryTests.cs).

- Frozen-roster HP does not rescale on Down or disconnect. Solo retains the development workload specified by the scaling code.
- Pylons spawn with exact Fight ownership; simultaneous final kills complete once. Deadline failure removes survivors, applies one pulse/Overload and chooses one outcome.
- One logical Boss life pool: reject shielded damage, clamp at phase floors, discard overkill and never skip the first required phase score.
- A threshold reached before first-score completion remains locked; afterward the next eligible action boundary permits transition.
- HP zero starts Final survival, not immediate Victory. Completing Final selects Victory once; the staged score does not inherit the old single-loop cap.
- NPC hits, phase-floor interception and life/death observations agree on host and Dedicated Server. No custom client packet reports damage or life as truth.
- DPS accounting separates recent/whole windows, excludes shielding and clamps deadlines/overkill; telemetry cannot advance gameplay.

### Stack and Spread

- Stack location is the same stationary world coordinate on every peer for that action. Only the actual acceptance radius is circular; outer chevrons are guidance, not a second boundary.
- Full frozen-roster gathering deals no damage. Missing members produce the declared missing-roster fraction; Down/disconnect does not reduce the requirement.
- Clockwise stations and their resolve ticks agree across participants; a target player's movement cannot drag the site.
- Spread remains participant-centered. No overlap means no damage; one pair, chains and all-overlap layouts apply the declared penalty once per affected participant.
- Downed participants are excluded from targeting/valid occupants. Authority-observed positions decide results, not interpolated markers.
- Host/non-host views and authority diagnostics agree at deadline. Add multi-count/latency cases only when the changed assignment, geometry or replication contract requires them.

### Attacks and presentation schedules

See [attack rules](../Tests/Convergence.DomainTests/AttackRuleTests.cs), [tempo/recovery](../Tests/Convergence.DomainTests/TempoRecoveryTests.cs) and [presentation geometry](../Tests/Convergence.DomainTests/PresentationContainmentTests.cs).

- Every standing participant gets a bounded simultaneous Prism ray; immutable locks do not drift after forecast.
- Warning, deployment, live collision and harmless recovery use the same schedule and geometry.
- Fixed beam damage and per-action hit ledgers prevent overlapping ray/beam counts multiplying damage.
- Rotating blades account for both turns and swept angles; safe warning/recovery never hit.
- Expanding floods retain their actual moving safe strip; half-field beams and central crush match their forecast and damage class.
- Final tempo accelerates, repeated combs shift, and bullet/slicer hit ledgers remain bounded. Verify travel-budget boundaries, not an unmeasured claim of universal dodgeability.
- VFX, client clocks and audio never become authority. Continuity/geometry tests do not prove aesthetic quality, visual readability or performance.

## Recovery

See [recovery domain cases](../Tests/Convergence.DomainTests/RecoveryDomainTests.cs) and the [current recovery spec](encounters/first-severance/REVIVE_SPEC.md).

- Raid-owned lethal damage produces one idempotent Down. Untimed Down remains rescuable after long waits; active First Severance has no Eliminated state.
- Valid held-kit click completes in the accepted authority tick, without channel, item consumption or tokens. Airborne movement alone is not a rejection.
- Authority validates exact session, slot/epoch, sender state/item, range, nonce/rate and recipient lockout; self-rescue and forged/stale intents fail without mutation.
- Recipient lockout survives another Down and expires exactly at its deadline; it does not prevent that recipient rescuing a different ally.
- Same-tick damage/invalidations precede stable-batch revalidation. Two rescuers have one winner; one commit decides recovery failure.
- All Down ends the Raid immediately. Ordinary Terraria death/disconnect follows the experiment's abort policy rather than pretending production lethal/rejoin integration exists.
- Exact-Fight cleanup clears health/control projections, immunity, visible debuff and reservations; no permanent flags or state leak to a new Raid.
- Defeat orders normal participant death after clearing Raid protection. Outsiders and other end reasons are not killed; external death-cancelling hooks are respected. Test with suitable characters because ordinary difficulty penalties apply.

### Legacy domain regression modes, not active gameplay

The reusable domain still supports token/channel/timeout configurations. Its tests retain token reservation, exact channel deadlines, interrupted reservations, nonce races, timeout elimination, reconnect epoch replacement and bounded projections. The old single-loop plan likewise retains target reassignment, pooled shares, loop-cap and immediate exposure-Victory regression cases.

Those explicitly configured fixtures are not First Severance defaults and must not be used to reintroduce expiry, tokens, moving Stack sites, activation denial or HP-zero Victory. Bootstrap/rename gates are completed history in Git and the [historical record](history/2026-09-07-pre-consolidation.md), not current work.

## Packet robustness

See [route/header cases](../Tests/Convergence.DomainTests/PacketTests.cs), the compiled codec checker and [Network Architecture](NETWORK_ARCHITECTURE.md).

- One common packet registration; bounded definition key selects an independently registered feature. Duplicate definition registration fails; independent definitions do not collide on shared operation IDs.
- Unknown type/version/direction/route, non-ASCII or oversized key, truncated buffer and invalid enum/count/coordinate fail before mutation.
- Exact sequence/Fight/definition for live intents; sender and epoch come from the trusted connection, not claimed payload identity.
- Payload round trips cover preparation, all phase/action states, bounded rosters/rays, untimed Down, HP-zero Final and shared-buffer consumption.
- Snapshot request/repair and common Idle clear stale feature projections. Sequence/revision ordering and terminal tombstones prevent delayed live resurrection.
- Snapshot/intent spam is rate-limited with bounded diagnostics. Test stale, duplicate, reordered and slot-reused traffic when changing that boundary.
- Host & Play and Dedicated Server join/Ready/late-snapshot/cleanup remain integration cases; codec success alone does not execute those hooks.

The Convergence boundary assumes cooperative clients for Terraria movement/ordinary-hit replication. It is not anti-cheat for modified clients.

## Tick, termination and cleanup

See [lifecycle cases](../Tests/Convergence.DomainTests/LifecycleDomainTests.cs), [terminal priority](../Content/Encounters/FirstSeverance/FirstSeveranceTermination.cs) and the [ownership map](ARCHITECTURE.md#runtime-and-cleanup-ownership).

- One feature orchestrator preserves authority tick order, one recovery commit and one terminal selection. Attack/actor/recovery collaborators cannot independently end or advance the Fight.
- Test the changed collisions at exact deadlines: last Pylon kill/timeout, mechanic damage/all Down, pending revival/sender Down, phase floor/action completion and Final completion/failure.
- Feature priority is total; external WorldUnload/InternalFailure/ProtocolFailure use immutable definition mappings without ticking a failed runtime again.
- Terminal projection/tombstone is published before cleanup. Delayed live revisions cannot replace it; terminal state never mutates again.
- Startup exceptions, missing Boss/Core, absent owned actors, reused slots, cleanup logger failure and duplicate/stale cleanup do not leak state.
- Retry backlog blocks a new Fight until resolved. A second exact cleanup is harmless; stale cleanup cannot remove new actors or player protection.

For every terminal cause, codec round trips retain its stable numeric value and compatible generic reason. Unknown/incompatible causes are rejected.

## Separately gated production integration

Before expanding the current adapters, measure the specific pinned tModLoader/Calamity seam:

- Ordinary hits: permission, modifiers, owner, life/death hooks and net updates for relevant weapon/projectile classes on host/non-host and Dedicated Server.
- General lethal interception: hook order, Calamity personal revives, duplicate hits, immunity/control sync and disarming before Defeat.
- Durable rejoin/outsiders: stable identity, slot reuse, observer/admission/ejection policy and cleanup.

Uncertainty blocks the new adapter, not the already authorized Raid-owned experiment. Do not revive historical blanket activation bans.

## Performance and visual/audio acceptance

Initial measurement targets, not measured guarantees: arena validation below 10 ms (investigate 50 ms or more), average active authority update below 1 ms/tick, custom steady-state traffic below 5 KB/s/client, bounded actors and cleanup. Measure before optimizing; client decoration is not a server actor budget.

For changed presentation, select affected resolutions/UI scales, Reduced/Minimal VFX, color-independent mechanic markers, multiplayer readability, audio balance and client-only resource loading. Dedicated Server must not initialize audio/graphics. Each distributed asset needs provenance. No FPS, load-capacity, dodgeability or aesthetic-success claim follows from static review.
