# Client Layer

Client-only UI, rendering, audio, accessibility, and presentation adapters belong here. The transport-neutral read-only encounter replica lives in `Common/Networking/Replication` so both network delivery and the Single Player authority adapter can feed the same model.

Feature-specific cue contracts stay with their `Content` feature. Implementations that draw, play sound, or touch graphics live under `Client/Encounters/<Feature>` and consume those cues plus the replica.

Rules:

- client code may read snapshots and presentation events;
- client code may not decide damage, assignments, mechanic success, lifecycle, or victory;
- no client type may be initialized on a Dedicated Server;
- gameplay telegraphs must remain understandable with particles, screen shake, flashes, and music disabled;
- presentation timing follows server cue ticks, while gameplay never follows audio playback position.

No presentation-layer implementation is included in Milestone 0.
