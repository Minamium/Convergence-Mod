---
name: develop-convergence-raids
description: Implement or review multiplayer-first Terraria/tModLoader content in the Convergence repository, especially server-authoritative Boss/Raid lifecycle, Downed/Revive, arena barriers, participant identity, mechanics, packets, snapshots, cleanup, Calamity compatibility, and Dedicated Server behavior. Use for C# or design changes under Common, Content/Encounters, Client, networking, Raid documentation, or multiplayer tests. Do not use for unrelated Terraria mods or general game design without repository changes.
---

# Develop Convergence Raids

Preserve Convergence's modular-monolith boundaries while adding multiplayer Raid features. Treat the server or Single Player process as the only gameplay authority.

## Start from repository truth

1. Locate the repository root from `AGENTS.md` and `ConvergenceMod.csproj`.
2. Read `AGENTS.md`, `docs/README.md`, `docs/STATUS.md`, `docs/VERSION_MATRIX.md`, `docs/ARCHITECTURE.md`, and `docs/NETWORK_ARCHITECTURE.md` completely.
3. For First Severance work, read `docs/encounters/first-severance/README.md` and the current spec/implementation plan. Source uses `FirstSeverance` and the Slice 2 six-state plan; deferred multipart mechanics exist only in deliberate history/backlog and must not shape the active runtime.
4. For Downed/Revive work, also read ADR-0005, the feature Revive spec, and `docs/TEST_PLAN.md`. For arena work, also read `docs/ARENA_INFRASTRUCTURE.md` and ADR-0003.
5. State the planned files, authoritative owner, client request path, replicated output, cleanup owner, and unresolved adapters before editing.
6. Keep candidate tModLoader/Calamity versions unverified until real Build + Reload and Dedicated Server evidence succeeds.

Choose an operating mode before commands:

- **Implementation mode:** edits and generated build output are allowed within scope.
- **Audit-only mode:** do not edit, format, compile, or create bytecode/build output. Capture a Git baseline when available; otherwise record a sorted SHA-256 inventory and compare it at the end. Concurrent-worker changes cannot be attributed without Git metadata.

Read [architecture-map.md](references/architecture-map.md) when choosing a module, [raid-authority-checklist.md](references/raid-authority-checklist.md) for gameplay/networking changes, and [verification-matrix.md](references/verification-matrix.md) before declaring completion.

## Place responsibility deliberately

- Put dependency-free values/contracts in `Common/Foundation` or `Common/Encounters/Abstractions`.
- Put reusable Raid-domain rules in `Common/Raids`, free of Terraria, Calamity, transport, UI, and encounter-specific names.
- Put authority coordination in `Common/Encounters/Runtime` or a narrow adapter beside its common domain.
- Put Calamity calls only in `Common/Compatibility/Calamity`.
- Put one encounter's phase plan, mechanics, NPCs, projectiles, tiles, rewards, tuning, and cue contracts under `Content/Encounters/<Feature>`.
- Put client-only UI/VFX/audio/accessibility/prediction under `Client`, consuming read-only replicas/cues.
- Never add feature switches to global coordinator/router code. Register through definitions, policies, and factories.

## Design authority before implementation

For each state, answer:

1. Who creates and mutates it?
2. Which stable identity scopes it: Encounter Sequence, Fight ID, Participant ID, connection epoch, actor, or Core?
3. Which bounded client command may request a change?
4. What does the server revalidate from World/player/item state?
5. Which snapshot/event exposes the result?
6. What happens to duplicate, stale, reordered, truncated, oversized, or malicious input?
7. Who owns cleanup, including partial construction and a second call?
8. What happens on Downed, disconnect/rejoin, slot reuse, terminal transition, unload, and Dedicated Server?

Use machine-readable rejection codes. Derive sender from tModLoader's trusted `whoAmI`; never trust payload identity, positions, damage, timers, Fight IDs, Core coordinates, held item, or mechanic success.

## Implement in safe slices

1. Add/update accepted docs and pure domain values/transitions.
2. Add server/SP adapters.
3. Add fixed/bounded DTOs with parse-complete-then-validate handling.
4. Add bounded replication and read-only client presentation.
5. Keep activation denied whenever a required authority/Core/roster/actor/death/cleanup adapter is missing.
6. Register each transient resource immediately after creation.
7. Prefer convergent full snapshots for repair and bounded events for presentation; never make VFX/audio authority.

For each mechanic encode assignment, telegraph start, resolve tick, authority result, soft-failure policy, Downed interaction, and cleanup. First Severance's MVP contains only Pylon, Stack, Spread, and Core exposure; backlog does not expand it.

## Preserve multiplayer invariants

- One coordinator-managed Boss/Raid per World until an ADR changes it.
- Active encounters are ephemeral; unload ends/cleans instead of resuming.
- Terminal snapshot publishes before the session/projections release.
- Delayed older Sequence/Fight/revision/epoch/nonce cannot revive/mutate current state.
- Player-slot reuse cannot inherit participant state.
- Downed is not ordinary death; only Raid authority enters/revives/expires/clears it.
- All-Downed/unrecoverable timeout resolves through explicit authority failure.
- Barrier/outsider response is bounded warning/correction/escalation, not unbounded actors or instant death.
- Dedicated Server never initializes graphics/shaders/audio/screen effects.

## Verify honestly

Run the deterministic wrapper:

```bash
python3 .agents/skills/develop-convergence-raids/scripts/verify_repo.py .
```

For a read-only review:

```bash
python3 .agents/skills/develop-convergence-raids/scripts/verify_repo.py --audit-only .
```

For pure Raid-domain work:

```bash
dotnet run --project Tests/Convergence.DomainTests/Convergence.DomainTests.csproj --configuration Release
```

Then follow `docs/runbooks/WINDOWS_DEVELOPMENT.md` from an exact `ModSources/Convergence` checkout for command build, Build + Reload, Single Player, Host & Play, and Dedicated Server. Missing runtime evidence is `not_run`/`blocked`, never a pass.

Review the final diff for authority leaks, reverse dependencies, unbounded payloads, missing cleanup registration, client-only access on servers, stale documentation catalog/status, and accidental third-party assets/source.

Classify findings:

- **Blocker:** unsafe activation, corruption, authority/security bypass, unrecoverable cleanup, or missing adapter/evidence required to call the feature playable.
- **Major:** deterministic correctness, synchronization, compatibility, or testability defect required before integration.
- **Minor:** bounded robustness, documentation, maintainability, or future-proofing issue that does not invalidate fail-closed behavior.
