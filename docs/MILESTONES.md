# Milestones

## Milestone 0 — Repository and Compatibility

- [x] Empty repository identified
- [x] Project documentation drafted
- [x] Initial tModLoader/Calamity version research
- [ ] Internal Mod name and namespace decision
- [ ] Addon license decision
- [ ] Minimal Mod skeleton
- [ ] `build.txt` with Calamity dependency
- [ ] Client build/load
- [ ] Dedicated Server build/load
- [ ] Version freeze promoted from Candidate to Confirmed

Exit: clean build and load on the pinned client/server environment.

## Milestone 1 — Arena Infrastructure

- Polar Foundation Core item/tile/entity
- pure Arena Validator
- 320x140 default bounds
- structured validation issues
- server-owned lifecycle and Fight ID
- 2～4 participant selection
- Join/Ready/cancel/timeout
- client visual Barrier
- server position correction
- idempotent Cleanup
- debug status and invariant commands

Exit: repeated start/cancel/destroy/disconnect cycles leave no stale state in 2～4 player Dedicated Server tests.

## Milestone 2 — Multiplayer State Foundation

- packet envelope and protocol version
- snapshot/delta/revision
- phase and timer synchronization
- assignment and seed synchronization
- join/rejoin/disconnect policy
- network logging and fault injection

Exit: clients recover from stale/reordered state without changing authoritative results.

## Milestone 3 — Basic Encounter Vertical Slice

- Boss Dummy
- Stack
- Spread
- Targeted Line
- one Weak Point window
- one DPS check
- Soft Failure and Overload

Exit: 2～4 players can clear or fail a short deterministic encounter.

## Milestone 4 — Downed and Revive

- standard death interception
- Downed state and timer
- Revive channel
- shared token
- weakness/invulnerability
- wipe detection
- UI and compatibility tests

Exit: simultaneous deaths, host death, disconnect, revive damage all resolve consistently.

## Milestone 5 — Parts and Personal Effigies

- Crown/Wings/Heart Casing
- route-dependent later mechanics
- primary Damage Class detection
- one Clone per participant
- owner damage rule
- post-clear support actions
- Clone failure absorption

Exit: part choice changes the later test, and every class can complete its Clone.

## Milestone 6 — Full Encounter

- all core phases
- player-count variants
- loop and Hard Enrage
- optional Split Reality decision
- Last Stand
- difficulty tuning

Exit: full 8～12 minute fight is clearable and failure causes are readable.

## Milestone 7 — Presentation

- production sprites
- primitive trails and shaders
- warning typography
- custom SFX
- phase score and transitions
- screen effects and accessibility options
- localization

Exit: presentation never obscures gameplay state and all asset rights are documented.

## Milestone 8 — Rewards and Release QA

- class weapons, whip, accessories, utility
- recipes and progression gating
- lore, trophy, relic, vanity
- multiplayer soak test
- Dedicated Server soak test
- compatibility matrix
- packaging, attribution, release notes

Exit: release candidate builds reproducibly and passes the mandatory matrix.

## Commit discipline

- one concern per commit where practical
- docs/decision changes before or with implementation
- compile after each code commit
- no mass asset import without attribution manifest
- compatibility updates separate from encounter tuning
- generated files and local build output are ignored
