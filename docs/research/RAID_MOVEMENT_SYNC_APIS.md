---
doc_id: research.raid-movement-sync-apis
document_type: research
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-06
source_of_truth_for: []
aliases:
  - Raid movement heartbeat
  - Stack replica drift investigation
related_code:
  - Content/Encounters/FirstSeverance/FirstSeveranceContainmentPlayer.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceClientStateSystem.cs
  - Content/Encounters/FirstSeverance/FirstSeverancePacketSystem.cs
related_docs:
  - project.status
  - compatibility.version-matrix
  - encounter.first-severance.spec
---

# Raid movement synchronization investigation

## Question and source scope

Why could two participants each see the other displaced during fixed-site Stack, while Spread passed? Inspect the installed Convergence movement hooks and accepted projections, then the official tModLoader movement/update contracts. Search terms: `PreUpdateMovement`, `PostUpdate`, `PostUpdatePlayers`, `PlayerControls`, `clientClone`, `CanPredict`, `StackAttendance`.

Official repository: [tModLoader/tModLoader](https://github.com/tModLoader/tModLoader), the repository linked by the installed runtime and official API documentation. Source access: verified for the files below, accessed2026-09-06. Target commit `666f69962d3bdffde54fc14025f02634965b4e7c` matches installed stable2026.07.3.0, Terraria1.4.4.9, .NET8/C#12; [Version Matrix](../VERSION_MATRIX.md) owns dependency evidence. The repository's [MIT license](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/LICENSE) was inspected. Only API names/contracts and behavior are used; no external implementation, private game source, assets or binaries are copied.

## Observations

- [ModPlayer.cs](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModPlayer.cs), `PreUpdateMovement` and `PostUpdate`, documents execution on owner, server and remote clients. The former precedes position integration; the latter ends the Player update. [Player.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Player.cs.patch) shows the matching hook insertion sites.
- [ModSystem.cs](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModSystem.cs), `PostUpdatePlayers`, runs after player updates on clients and server. `PostUpdateWorld` is server/SP-only and must not be used as the MP-client movement heartbeat.
- [MessageID.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ID/MessageID.cs.patch), `PlayerControls`, documents movement controls/velocity and forwarding to peers, addressed by player slot. It normally participates in client-clone synchronization. The inspected patches do not expose the full unmodified vanilla message13 send/receive implementation or prove live latency.
- Convergence0.2.14 applied fallback lift and edge velocity braking to both the owning client and server, but not remote clients. Its short-lived capability retained flight, while there was no periodic owner movement publication. Fixed Stack markers already used the server-supplied world coordinate, not a client-selected player anchor. Living players' positions were not overwritten by the Down projection.
- In both inspected pulls, server Stack attendance was1/2 initially, with the absent member669.4/710.3px away from a112px circle. The next Stack was0/2. Both Spread checks passed the640px separation. Neither peer's actual coordinate/velocity history was in the old logs. These outcomes establish authority classification, not the players' true local positions or packet loss.

## Inference and independent decision

The owner/server/remote movement asymmetry and unbounded interval between supplemental motion publications are plausible contributors to drifting remote replicas, especially with custom flight. They do **not** prove the exact source of the reported horizontal displacement; extra acceleration is vertical, and neither the friend's log nor a packet capture exists for that incident. Spread success does not rule out drift: displaced replicas can satisfy a minimum-distance check while failing a tight gathering check.

Decision: adapt the official hooks with an independently implemented, exact-Fight-scoped movement heartbeat. After all Player updates, only an active participating MP owner sends ordinary `PlayerControls` every6ticks, including while stationary. Downed/eliminated players, outsiders, servers and expired capabilities do not send it. Clear cadence on capability/Fight change, unload and disconnect. Only the owner/SP applies input-driven fallback lift and predictive edge braking; server still clamps field violations, and all Raid hits, Stack results, recovery and terminal decisions remain server/SP-owned. This adds no custom position/outcome request, authority exception, synthetic participant or wire-layout change.

Client diagnostic samples during Stack contain observer/subject slot, world center, velocity, target, latest snapshot tick and estimated authority tick at most twice per second per participant. Server resolution logs the same center/velocity and input bits; boundary corrections are logged separately. Estimated ticks are not a clock-synchronization or latency measurement. No coordinate sample grants attendance or changes success rules.

## Additional receive warning

After both fights, activation retries produced a tML receive-underflow warning:31of43bytes consumed, immediately after an activation rate-limit rejection. Convergence's fixed12-byte activation payload had not been read because the handler returned before decoding. Ready/cancel shared that ordering defect. Move each fixed decode ahead of sender/header/rate checks, preserving the limits and rejecting invalid intent. An invalid Ready boolean also consumes its fixed nonce before rejection. Never drain the engine's shared stream to its total length; trailing bytes can belong to another packet. This warning occurred after the failed Stacks and is not evidence that it caused them.

## Required verification and limits

Focused domain tests cover bounded heartbeat cadence, stationary publication, disabled capability, reset/rollback, compiled solo on/off policy and unchanged field/Stack rules. Compiled payload checks exercise valid/invalid/truncated fixed requests and following-packet sentinels. Review that both real start scans call the shared policy and decode precedes rate rejection. [Status](../STATUS.md) owns actual results.

The remaining acceptance is a short user-owned Host & Play check: solo Ready reaches combat; two owners hold the same fixed Stack circle, brake/hover, and receive a2/2 zero-damage result; Spread, edge confinement and post-Raid ordinary movement still behave normally. Compare both clients' `StackClientSample` with server `StackAttendance` if disagreement remains. Dedicated-server/latency, rejoin and external-Mod movement behavior are not established by compilation or these official contracts.
