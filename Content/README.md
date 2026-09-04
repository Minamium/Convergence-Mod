# Content Modules

Player-facing content is organized by feature rather than tModLoader base type.

The first module is `Encounters/FirstSeverance/`. Its immutable six-state plan and feature terminal-cause contract are implemented, while its world adapter and activation remain intentionally inert.

NPCs, Tiles, Projectiles, phases, rewards, and presentation cue contracts owned only by First Severance remain in that feature. Graphics/audio implementations belong in `Client/Encounters/FirstSeverance` after the rename.

Rules:

- content may depend on `Common`, but `Common` may not depend on `Content`;
- an encounter module does not send packets or own global participant/cleanup state directly;
- Calamity access goes through `Common/Compatibility/Calamity`;
- shared abstractions are extracted after a second real consumer proves the common contract;
- ring/arms in the First Severance MVP are Boss presentation, not separate gameplay actors;
- world events use a separate coordinator and do not inherit Raid lifecycle by default.

Read [`docs/encounters/first-severance`](../docs/encounters/first-severance/README.md) before working on the first feature.
