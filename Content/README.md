# Content Modules

Player-facing content is organized by feature rather than by tModLoader base type.

The first module is `Encounters/ThirdSeverance/`. Its future NPCs, tiles, projectiles, phases, rewards, and presentation cue contracts stay together until a second real consumer proves that a component belongs in `Common/`. Graphics/audio implementations belong in `Client/Encounters/ThirdSeverance`.

Rules:

- content may depend on `Common`, but `Common` may not depend on `Content`;
- an encounter module does not send packets or own global participant/cleanup state directly;
- Calamity access goes through `Common/Compatibility/Calamity`;
- shared abstractions are extracted after reuse, not in anticipation of reuse;
- world events will use a separate coordinator and will not inherit Raid lifecycle by default.
