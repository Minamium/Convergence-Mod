---
name: develop-convergence-raids
description: Implement or review multiplayer-first Terraria/tModLoader content in the Convergence repository, especially server-authoritative Boss/Raid lifecycle, Downed/Revive, arena barriers, participant identity, mechanics, packets, snapshots, cleanup, Calamity compatibility, and Dedicated Server behavior. Use for C# or design changes under Common, Content/Encounters, Client, networking, Raid documentation, or multiplayer tests. Do not use for unrelated Terraria mods or general game design without repository changes.
---

# Develop Convergence Raids

Preserve Convergence's modular-monolith boundaries while adding multiplayer Raid features. Treat the server or Single Player process as the only gameplay authority.

## Start from repository truth

1. Locate the repository root from `AGENTS.md` and `ConvergenceMod.csproj`.
2. Read `AGENTS.md`, `docs/VERSION_MATRIX.md`, `docs/ARCHITECTURE.md`, and `docs/NETWORK_ARCHITECTURE.md` completely.
3. Read only the feature documents and source directories needed for the task. For Downed/Revive work, also read ADR-0005 and `docs/TEST_PLAN.md`. For arena or encounter-plan work, also read `docs/ARENA_INFRASTRUCTURE.md`, `docs/ENCOUNTER_SPEC.md`, and the applicable arena ADR.
4. State the planned files, authoritative owner, client request path, replicated output, cleanup owner, and unresolved adapters before editing.
5. Keep candidate tModLoader and Calamity versions labeled unverified until a real Build + Reload and Dedicated Server load succeeds.

Choose an operating mode before running commands:

- **Implementation mode:** edits and generated build output are allowed within the task scope.
- **Audit-only mode:** do not edit, format, compile, or create bytecode/build output. Capture a baseline with Git when available; otherwise record a sorted SHA-256 inventory of relevant files and compare it at the end. State that changes by concurrent workers cannot be attributed when Git metadata is absent.

Read [architecture-map.md](references/architecture-map.md) when choosing a module. Read [raid-authority-checklist.md](references/raid-authority-checklist.md) for any gameplay or networking change. Read [verification-matrix.md](references/verification-matrix.md) before declaring completion.

## Place responsibility deliberately

- Put dependency-free value objects and contracts in `Common/Foundation` or `Common/Encounters/Abstractions`.
- Put reusable Raid-domain rules in `Common/Raids`; keep them free of Terraria, Calamity, network transport, UI, and encounter-specific names.
- Put authority coordination in `Common/Encounters/Runtime` or a narrow adapter beside the owning common domain.
- Put Calamity calls only in `Common/Compatibility/Calamity`.
- Put one encounter's phase plan, parts, mechanics, NPCs, projectiles, tiles, rewards, and tuning under `Content/Encounters/<Feature>`.
- Put client-only UI, VFX, audio, accessibility, and prediction under `Client`; consume read-only replicas or cue contracts.
- Never add a feature switch to the global coordinator or packet router. Register through `EncounterDefinition`, policies, and runtime factories.

## Design authority before implementation

For each new state, answer:

1. Who creates it?
2. Which stable identity scopes it: Encounter Sequence, Fight ID, Participant ID, connection generation, entity identity, or Core identity?
3. Which client command can request a change?
4. What does the server revalidate from its own World state?
5. Which snapshot or bounded event exposes the result?
6. What happens on duplicate, stale, reordered, truncated, or malicious input?
7. Who owns cleanup, and is cleanup idempotent after partial construction?
8. What happens on disconnect, rejoin, World unload, and Dedicated Server?

Use machine-readable rejection codes. Never trust player IDs, positions, damage, timers, Fight IDs, Core coordinates, or mechanic success received from a client. Build `SenderWhoAmI` from tModLoader's trusted packet sender argument.

## Implement in safe slices

1. Add pure domain values and transitions first.
2. Add server/SP adapters second.
3. Add bounded packet DTOs and parse-complete-then-validate handling third.
4. Add read-only replication and client presentation last.
5. Keep activation denied when a required authority adapter, Tile Entity resolution, roster validation, or cleanup path is missing.
6. Register every transient resource with the cleanup scope immediately after creation.
7. Prefer full convergent snapshots for recovery and bounded events for presentation; never make VFX/audio authoritative.

For a Raid mechanic, encode assignment, telegraph start, resolution tick, server result, failure policy, and cleanup. Do not derive success from what a client rendered.

## Preserve multiplayer invariants

- One coordinator-managed Boss or Raid per World until an ADR changes the policy.
- Active encounters are ephemeral; World unload ends and cleans them instead of resuming.
- A terminal snapshot is published before authority releases the active session.
- Delayed packets from an older Encounter Sequence cannot revive stale state.
- Player slot reuse cannot inherit participant state; use stable participant identity plus connection generation.
- Downed is not ordinary death. Only the Raid authority may enter, revive, expire, or clear it.
- All-participant Downed and unrecoverable timeout conditions resolve through an explicit Raid failure, never a client-local decision.
- Outsider handling and arena barriers use warnings, deterministic correction, and bounded escalation; do not spawn unbounded network entities.
- Dedicated Server code never initializes graphics, shaders, audio, or screen effects.

## Verify honestly

Run the deterministic wrapper:

```bash
python3 .agents/skills/develop-convergence-raids/scripts/verify_repo.py .
```

For a read-only review, use the non-writing path:

```bash
python3 .agents/skills/develop-convergence-raids/scripts/verify_repo.py --audit-only .
```

When the pure Raid-domain test project exists, run:

```bash
dotnet run --project Tests/Convergence.DomainTests/Convergence.DomainTests.csproj
```

Then follow `docs/DEVELOPMENT.md` from an exact `ModSources/Convergence` checkout for the pinned project build, interactive Build + Reload, and Dedicated Server smoke tests. Record the enabled Mod versions and client/server log paths described there. If the environment is unavailable, report that verification as missing; static checks and the standalone domain harness are not a successful tModLoader build.

Review the final diff for authority leaks, reverse dependencies, unbounded payloads, missing cleanup registration, client-only access on servers, and accidental third-party assets or source.

Classify review findings consistently:

- **Blocker:** unsafe activation, data/world corruption, security/authority bypass, unrecoverable cleanup, or a missing production adapter required to call the feature playable.
- **Major:** deterministic correctness, synchronization, compatibility, or testability defect that must be fixed before integration.
- **Minor:** bounded robustness, documentation, maintainability, or future-proofing issue that does not invalidate the current fail-closed path.
