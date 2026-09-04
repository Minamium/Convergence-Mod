---
doc_id: project.coding-standards
document_type: governance
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-05
source_of_truth_for:
  - engineering.coding_standards
aliases:
  - coding standards
related_code:
  - Directory.Build.props
  - Common
  - Content
related_docs:
  - project.architecture
  - project.network-architecture
---

# Coding Standards

## Correctness before density

- Prefer explicit bounded state transitions over clever inheritance.
- Make invalid protocol values, lifecycle transitions, and stale identities rejectable.
- Keep gameplay state server-owned and presentation state disposable.
- Use stable value objects for Fight/participant identity and connection epoch.
- Do not store localized/display strings in authority state or wire messages.

## Dependency and ownership

- `Common` cannot import `Content` or `Client`; `Content` cannot import `Client`.
- Only the Calamity compatibility layer accesses Calamity APIs.
- Individual actors do not call packet transport directly.
- World mutation is performed through the authoritative feature runtime/ports.
- Every spawned actor/resource is registered immediately to one exact Fight and has idempotent cleanup.

## tModLoader boundaries

- Review every hook for Single Player, multiplayer client, and Dedicated Server sides.
- `RightClick` or item use sends intent; it never starts/finishes gameplay locally.
- `ModTileEntity` anchors content but does not own the encounter.
- Clear static registries/event handlers in `Unload`; defensively reset player projections.
- Dedicated Server code never initializes graphics/audio.

## Network code

- Give packet IDs explicit numeric values and never reuse/renumber retired values.
- Bound sizes and parse complete DTOs before validating/mutating authority.
- Derive sender from `whoAmI`; never trust a payload player index.
- Validate protocol, direction, encounter identity, binding/epoch, lifecycle, nonce, range/state, and rate.
- Synchronize coarse transitions/results/deadlines, not particles or every tick/hit.
- Unknown/stale/malformed packets are bounded-log rejections, not server crashes.

## Naming

- Current first-feature identifiers are `FirstSeverance` and `first_severance`; legacy forms may appear only in deliberately historical records or completed-rename instructions.
- Stable keys use lowercase ASCII underscores, e.g. `first_severance`.
- Failure codes are namespaced, e.g. `first_severance.revive_not_initialized`.
- Boolean names start with `Is`, `Has`, `Can`, or `Should`.
- Public API is avoided until its compatibility contract is deliberate.

## Comments and documentation

Comments explain authority, invariant, lifecycle, cleanup, or unusual API constraints—not obvious syntax. `STATUS.md` owns implementation truth, feature specs own player behavior, plans own sequence, and ADRs own structural decisions.
