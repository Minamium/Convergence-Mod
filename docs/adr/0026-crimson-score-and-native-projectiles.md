---
doc_id: adr.0026
document_type: adr
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-16
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

# ADR-0026: Crimson score clock and native projectiles

Current amendment: [sequential acts and rhythm phrases](#sequential-acts-and-rhythm-phrases--2026-09-16). The original steel-machine/50% purge rationale below is historical, not the current visual/phase instruction.

The owner requests a separate music-led Raid without a fixed arena, then a50% armored→fast-machine transformation. Reuse the definition-routed coordinator/transport, not either prior encounter's runtime. `crimson_foundry` owns a runtime, preparation roster, native NPC projection, immutable hazard projectiles and terminal cleanup; no global encounter switch, extra assembly or persisted session is added. Feature termination schema3/version1 and protocol42 distinguish matching peers without renumbering prior packet IDs.

The server/SP owns connection-bound Ready acceptance, frozen roster, the future music epoch, score event IDs, warning/fire/end times, locked world geometry, purge scheduling and terminal result. Native ExtraAI fully bounds1–8 ordered participant slots/nonzero connection GUIDs, age/stage and finite hazard geometry before replacement. Late snapshots cannot change an existing hazard or substitute another Fight. All spawned resources are initialized by an owned entity source before their first native sync, and exact-Fight cleanup can find partial construction without deleting another session. Late joiners are spectators after Ready acceptance.

Crimson slashes/bolts intentionally use ordinary hostile Projectile damage and the receiving player's normal immunity/dodge/equipment hooks. This is a new narrowly scoped exception to ADR-0002's player-hit resolution boundary; it does not generalize or modify ADR-0023/0024. There is no parallel manual Hurt loop, HP subtraction or custom hit-result packet. Only matching, fresh, live actor/hazard state and a retained participant allow damage. Deaths leave this prototype roster; no Doll Downed domain is attached. Native Terraria incoming-NPC damage and player-health trust remain, not a claim of cheat-resistant adjudication.

The immutable score is derived from an authorized local recording. A client audio buffer follows, but never drives, authority ticks. Native looping retains a sample-accurate intro/loop boundary; joins/resume may seek to an accepted position. This does not promise sample-exact cross-machine alignment or conceal device/network latency by shrinking telegraphs. Score/song export together; missing licensed audio rejects only this encounter's summon.

API evidence: same pinned tML2026.07.3.0/666f69962d3bdffde54fc14025f02634965b4e7c Projectile/Player hooks as [ADR-0023](0023-ghost-samurai-native-wave-damage.md). The [OGGAudioTrack patch](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Audio/OGGAudioTrack.cs.patch) confirms parsed LOOPSTART/LOOPEND tags. [FNA SoundEffect](https://github.com/FNA-XNA/FNA/blob/cf6b3664866faaa9237a763fe61e01bfcd473a10/src/Audio/SoundEffect.cs) documents PCM offset/count and loopStart/loopLength; compiled against installedFNA1.0.0. No FNA source is copied. `PostUpdateInput` handles pause/focus audio independent of world simulation. Inspected2026-09-15; hardware playback remains owner-tested.

The first implementation withheld the recording from Git pending a distribution decision. Later on2026-09-15, the owner approved the game-facing loop edit for the Mod and its public source tree, with author credit and exclusion from the project license. [Feature music policy](../encounters/crimson-foundry/ENCOUNTER_SPEC.md#music-and-musical-presentation) owns that current packaging decision. The missing-audio guard remains an incomplete-package safeguard; this ADR does not grant standalone music rights.

## Shared pedestal and bounded stage amendment — 2026-09-15

The owner replaces the boundary-free requirement with a Doll-sized field selected by a different item on the same pedestal. `Content/Shared/RaidPedestal` exposes a narrow held-key/lease adapter implemented by the existing saved tile entity; existing tile/type IDs and Doll's interaction remain stable. Pure `Common/Raids/Arena/RaidFieldGeometry` owns the common footprint and bounded geometry. No new feature switch is added to the coordinator/router, and Common does not import Content.

Crimson's runtime owns claiming, validating, activating and finally releasing its exact Fight lease. Its actor snapshot now includes the immutable ground anchor, requiring protocol43 on every peer. Native incoming NPC damage remains the intended path: a client must project `dontTakeDamage` from the same Performance/purge rule as authority, or it never submits legitimate weapon hits. Field containment/flight are feature-scoped capabilities derived from that fresh actor/retained roster, not saved player flags. Only authority and the owning client adjust position; remote clients do not inject input. Physics and physical-pixel exterior rendering consume the same rectangle, with one world-transform application and no UI-scale multiplication. Clear/wipe/cancel/world exit release the field with the actor. This adds no Doll recovery integration or terrain mutation.

## Independent summons amendment — 2026-09-16

The owner replaces the machine with an NPC-sized summoner and three large independently damageable apparitions. `CrimsonRuntime` retains authority tick order and exact-Fight cleanup, owns four native NPCs and source-tagged hazards, freezes their equal HP budgets at Ready acceptance, and advances an idempotent three-bit defeated mask only from matched child deaths. A missing child invalidates the encounter instead of counting as a kill. All three deaths open a protected Final manifestation, then expose the performer; no remaining50% purge logic. Native player/NPC damage remains unchanged.

The main projection replaces the old purge epoch with a Final epoch and adds the defeated mask. Each child completely bounds Fight, parent NPC slot, index0–2 and birth tick; each hazard adds source0–3. Every peer derives target vulnerability from the same fresh accepted state. The merged Ghost Samurai update already uses protocol44; this combined revision therefore uses **protocol45**, with no packet-ID renumbering. Cleanup scans exact-Fight actors, including partial construction, before releasing the shared lease. Child death cancels its existing hazards; Final cancels all prior hazards.

`CrimsonBarrageGeometry` takes field/cue/source, never player coordinates. All lanes in a volley share one corridor; authority issues fixed world-space geometry. Music fade and continuous introductory bars consume the accepted epoch but never alter score timing or gameplay.

The new companion is a normal owner-replicated minion, outside encounter authority/roster/Ready. Owner chooses locomotion and creates child beams; missing remote owner buffs cannot dismiss a replica. Child lifetime checks exact owner plus projectile identity. The client-only rig is shared as artwork, not via a gameplay dependency on Client or on Doll's runtime.

## Sequential acts and rhythm phrases — 2026-09-16

Supersedes the independent-summons amendment's simultaneous three-target progression and sparse volley scheduling, not native damage or resource ownership. Owner requests A→B→C at20% retained HP, then the three survivors plus performer; moving actors, a standard HP bar and drum-like call/response with rapid fills. Display names become Scarlet Invocation/Scarlet Grimoire; stable identifiers remain unchanged.

Authority caps solo-act native damage at the20% boundary and commits exactly one phase transition. Withdrawn actors are retained, invulnerable and hidden; they are neither killed nor refilled. Final opens all four after the protected epoch. Victory requires the performer flag plus three final kills; all-out takes priority. Phase epochs invalidate all outstanding hazards; final source death invalidates that source. Missing owned actors remain errors. Cleanup is retryable and exact-Fight, including withdrawn/partly spawned resources.

Protocol46 extends the native actor projection with phase/unlock/target, phrase posture and bounded target HP, and extends hazards with phase epoch/phrase/accent. Decode the complete bounded body, reject rollback and inconsistent final state, then replace. The source budget is unchanged. No new packet operation, client outcome claim or saving schema is added.

`CrimsonRhythm` creates measured-beat subdivision phrases with separate warning/impact times, admitted in advance as a bounded batch. Short live pulses and overlapping safe corridors replace the old global volley cooldown. Native projectile immunity remains; high event cadence is not guaranteed repeated HP loss. Movement follows the authority target and authored formations but never retargets forecast geometry. Audio remains a follower of the score epoch, not authority. Subjective fit and network fairness are unverified until playtesting.

Official API check,2026-09-16: pinned tML666f69962d3bdffde54fc14025f02634965b4e7c [`ModBossBar`](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModBossBar.cs) supports `ModifyInfo` with life/lifeMax and standard `DrawFancyBar`; the [`BigProgressBarSystem` patch](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/GameContent/UI/BigProgressBar/BigProgressBarSystem.cs.patch) gives an explicitly assigned NPC.BossBar priority. Use that native UI adapter, not a text-heavy custom combat overlay. API inspection does not prove the owner's bar style will display correctly.
