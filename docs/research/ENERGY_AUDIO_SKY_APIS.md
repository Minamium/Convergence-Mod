---
doc_id: research.energy-audio-sky-apis
document_type: research
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-06
source_of_truth_for:
  - evidence.energy_audio_sky_apis
aliases:
  - energy audio sky API evidence
related_code:
  - Client/Encounters/FirstSeverance
related_docs:
  - compatibility.version-matrix
  - decision.energy-charge-feedback
---

# Energy, audio and sky API evidence

Runtime: tModLoader v2026.07.3.0 / Terraria 1.4.4.9, source pin `666f69962d3bdffde54fc14025f02634965b4e7c`; tModLoader source MIT. API investigation only: no upstream implementation copied or vendored.

- [ExampleSurfaceBiome.cs](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/ExampleMod/Content/Biomes/ExampleSurfaceBiome.cs): MusicLoader resolves an extensionless Mod-relative music path. Independent choice: participant-only BossHigh scene uses one new full-length OGG loop; server property returns -1.
- [SoundStyle.TML.cs](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Audio/SoundStyle.TML.cs): asset path, MaxInstances and pause behavior are explicit style properties. Independent choice: capped non-spatial critical cues, own voice-handle cleanup and snapshot serial/revision de-duplication; no SoundID recording extracted.
- [ModSceneEffect.cs](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModSceneEffect.cs): SpecialVisuals runs with the selected active flag and supports ManageSpecialBiomeVisuals. [CustomSky](https://docs.tmodloader.net/docs/stable/class_custom_sky.html) / [SkyManager](https://docs.tmodloader.net/docs/stable/class_sky_manager.html), documentation labelled v2026.07: activation, depth drawing, update/reset, cloud alpha and named binding. Independent resource-free sky resets on unload; no global day/weather/UI flag is written. The 0-depth background pass must be visually confirmed in game.

## 0.2.12 actor-health replication observation

Read on 2026-09-06: the version-matched installed XML and pinned [ModNPC.cs](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModNPC.cs) describe SendExtraAI as server-only on SyncNPC, and ReceiveExtraAI as client-only. The matching [MessageBuffer patch](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/MessageBuffer.cs.patch) reads buffered NPC extra AI at the end of that NPC case. Independent implementation: send one validated frozen-roster byte plus current life; derive maximum HP from the shared table. This also avoids reliance on an implicit full-health packet using a client's default maximum. No upstream code/assets were imported. This bounded integration is not proof of actual multiplayer HP-bar timing.

## 0.2.11 phase-boundary and sound-limit observations

The same pinned installed `tModLoader.xml` documents `ModNPC.CheckDead`: it runs on server and clients and returning false prevents an NPC at zero life from dying. The matching [ModNPC.cs source](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModNPC.cs) was inspected on 2026-09-06. Independent choice: protect only the owned Sealed Boss at the next life floor, then let its authority runtime choose the phase at the next observation. A client mirrors death suppression only from an accepted phase; it never reports a phase/health decision. This does not establish real multiplayer behavior under an overkill/packet race; that remains a focused user smoke.

Installed `SoundStyle.SoundLimitBehavior` documentation identifies **ReplaceOldest** as the default; it is incorrect to explain the former one-instance limit as IgnoreNew. Independent change: explicitly keep at most two voices per cue with ReplaceOldest, so one older resonance can survive under the next transient. Critical cue serial/revision guards and scoped voice cleanup are retained. No upstream source, binary or audio was copied into the project. Conditional UI/camera rendering reuses the already connected local presentation hooks, not a new global hide/control flag.

## 0.2.4 correction: inactive spawn and sky-only registration

The initial choice of `ManageSpecialBiomeVisuals` above was invalid for a sky-only key. The 0.2.3 user client completed Mod loading but threw `NullReferenceException` at `Player.ManageSpecialBiomeVisuals` during `InitialSpawn(49) -> Player.Spawn -> ForceUpdateBiomes -> SpecialVisuals`. Menu load did not validate this entry path.

The pinned [SceneEffectLoader.cs](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/SceneEffectLoader.cs) calls SpecialVisuals even when IsSceneEffectActive is false. Installed-binary inspection on 2026-09-06 confirms `ManageSpecialBiomeVisuals` null-checks its sky lookup but unconditionally calls IsActive on `Filters.Scene[key]`. The missing filter therefore throws even with false/inactive input. No third-party binary or IL output is included here. Binary: tModLoader.dll SHA-256 `d530e508b2841e66d880ce279a609624b5ab66ce8093eedfa04f47c3d12d485c`.

Independent fix: use only SkyManager after a guarded lookup of our registered FirstSeveranceSky; never call the combined Player helper or register a dummy screen filter. Dedicated Server, menu, nonlocal player and absent/wrong binding do nothing. Compare requested state separately from the visible fade tail to avoid repeated activation/deactivation and allow immediate reactivation. Installed EffectManager's indexer safely returns null for missing keys; its Activate/Deactivate require a valid binding. SkyManager.OnActivate removes any prior active-list entry before adding it again. Existing exact-Fight ownership and World/Mod reset remain unchanged. The isolated callback guard test is distinct from the user-owned in-game spawn/sky smoke.

Calamity DoG is a user-supplied speed/behavior benchmark only. The charge integrator, bounded turning, lock and swept rectangle were designed independently; no DoG source or asset was acquired. Installed Calamity dash i-frame behavior is not inferred to be proven from these tModLoader APIs.
