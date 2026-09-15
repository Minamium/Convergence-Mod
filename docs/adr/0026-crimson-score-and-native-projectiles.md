---
doc_id: adr.0026
document_type: adr
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-15
source_of_truth_for:
  - architecture.crimson_score_authority
aliases:
  - Crimson score authority
related_code:
  - Content/Encounters/CrimsonFoundry
  - Client/Encounters/CrimsonFoundry/CrimsonAudio.cs
related_docs:
  - encounter.crimson-foundry.spec
  - adr.0022
  - adr.0023
---

# ADR-0026: Crimson Foundry score clock and native projectiles

The owner requests a separate music-led Raid without a fixed arena, then a50% armored→fast-machine transformation. Reuse the definition-routed coordinator/transport, not either prior encounter's runtime. `crimson_foundry` owns a runtime, preparation roster, native NPC projection, immutable hazard projectiles and terminal cleanup; no global encounter switch, extra assembly or persisted session is added. Feature termination schema3/version1 and protocol42 distinguish matching peers without renumbering prior packet IDs.

The server/SP owns connection-bound Ready acceptance, frozen roster, the future music epoch, score event IDs, warning/fire/end times, locked world geometry, purge scheduling and terminal result. Native ExtraAI fully bounds1–8 ordered participant slots/nonzero connection GUIDs, age/stage and finite hazard geometry before replacement. Late snapshots cannot change an existing hazard or substitute another Fight. All spawned resources are initialized by an owned entity source before their first native sync, and exact-Fight cleanup can find partial construction without deleting another session. Late joiners are spectators after Ready acceptance.

Crimson slashes/bolts intentionally use ordinary hostile Projectile damage and the receiving player's normal immunity/dodge/equipment hooks. This is a new narrowly scoped exception to ADR-0002's player-hit resolution boundary; it does not generalize or modify ADR-0023/0024. There is no parallel manual Hurt loop, HP subtraction or custom hit-result packet. Only matching, fresh, live actor/hazard state and a retained participant allow damage. Deaths leave this prototype roster; no Doll Downed domain is attached. Native Terraria incoming-NPC damage and player-health trust remain, not a claim of cheat-resistant adjudication.

The immutable score is derived from an authorized local recording. A client audio buffer follows, but never drives, authority ticks. Native looping retains a sample-accurate intro/loop boundary; joins/resume may seek to an accepted position. This does not promise sample-exact cross-machine alignment or conceal device/network latency by shrinking telegraphs. Score/song export together; missing licensed audio rejects only this encounter's summon.

API evidence: same pinned tML2026.07.3.0/666f69962d3bdffde54fc14025f02634965b4e7c Projectile/Player hooks as [ADR-0023](0023-ghost-samurai-native-wave-damage.md). The [OGGAudioTrack patch](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Audio/OGGAudioTrack.cs.patch) confirms parsed LOOPSTART/LOOPEND tags. [FNA SoundEffect](https://github.com/FNA-XNA/FNA/blob/cf6b3664866faaa9237a763fe61e01bfcd473a10/src/Audio/SoundEffect.cs) documents PCM offset/count and loopStart/loopLength; compiled against installedFNA1.0.0. No FNA source is copied. `PostUpdateInput` handles pause/focus audio independent of world simulation. Inspected2026-09-15; hardware playback remains owner-tested.

The first implementation withheld the recording from Git pending a distribution decision. Later on2026-09-15, the owner approved the game-facing loop edit for the Mod and its public source tree, with author credit and exclusion from the project license. [Feature music policy](../encounters/crimson-foundry/ENCOUNTER_SPEC.md#music-and-musical-presentation) owns that current packaging decision. The missing-audio guard remains an incomplete-package safeguard; this ADR does not grant standalone music rights.
