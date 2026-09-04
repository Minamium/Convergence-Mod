---
doc_id: research.multiplayer-raid-prior-art
document_type: research
status: historical
owners:
  - research
last_reviewed: 2026-08-23
source_of_truth_for: []
aliases:
  - multiplayer Raid prior art
  - tModLoader Raid research
related_code:
  - Common/Raids/Revive
related_docs:
  - project.network-architecture
  - encounter.first-severance.spec
  - research.sources
---

# Multiplayer Raid Prior Art

Last reviewed: **2026-08-23**

Status: architecture research note. This document records design evidence, not imported implementation.

## Purpose

This survey answers five questions for the first multiplayer raid vertical slice:

1. Which tModLoader hooks can carry an authoritative encounter, Downed/Revive, and cleanup lifecycle?
2. What public Terraria mods demonstrate related multiplayer boss, arena, telegraph, or scripted-death patterns?
3. Which patterns are safe to adopt as independent design knowledge, and which should be rejected?
4. How original is the proposed combination of rostered raid, cooperative revive, shared tokens, and all-player wipe?
5. What must be proved on a dedicated server before the design is treated as production-ready?

The current repository rules remain controlling: one ephemeral encounter per World, server/Single Player authority, bounded client requests, a read-only client snapshot, and idempotent cleanup. See [NETWORK_ARCHITECTURE.md](../NETWORK_ARCHITECTURE.md) and [ARCHITECTURE.md](../ARCHITECTURE.md).

## Method and limits

- Official tModLoader documentation/source and public repositories under the named project organizations or a project-associated maintainer account were used. The Mod of Redemption provenance qualification is recorded in the ledger and its dedicated section; this survey does not claim that its reviewed commit equals the current Workshop binary.
- Links are pinned to the reviewed commit wherever possible. Branch-head links are deliberately avoided for code evidence.
- The search was source-oriented: `PreKill`, `Kill`, `CheckDead`, `SendExtraAI`, `ReceiveExtraAI`, `netUpdate`, packet handlers, arena bounds, participant lists, phase state, telegraphs, part spawning, and cleanup were inspected.
- “Not found” means “not found in the scoped files and searches at the pinned revision.” It is not proof that no implementation exists elsewhere, in a private branch, in a binary-only release, or in another mod.
- Similarity is an engineering assessment, not a patent novelty search or legal opinion.
- No source or assets from the surveyed mods are copied into this repository. Type/member names below identify evidence; implementation must be written independently against documented tModLoader APIs.

## Reproducibility and search ledger

All entries below were accessed on **2026-08-23**. A version shown as “Unknown” means that the pinned repository's primary build metadata did not declare it; commit date, branch name, or apparent API style was not used to invent a target. These prior-art revisions are not claimed compatible with Convergence's own candidate tuple of Terraria 1.4.4.9, tModLoader `v2026.06.3.6`, and Calamity 2.2.2 merely because their source was useful for design research.

### Corpus identity, build metadata, and rights signal

| Corpus | Fixed revision and association | Primary metadata at that revision | Declared Terraria / tML / Calamity target | Rights signal |
|---|---|---|---|---|
| tModLoader | [`tModLoader/tModLoader@29bf9785…`](https://github.com/tModLoader/tModLoader/commit/29bf9785f5f4de8cd305be002c4cc48aa1177b20), official project repository | [`v2026.06.3.6` release](https://github.com/tModLoader/tModLoader/releases/tag/v2026.06.3.6); fixed tree contains [`PortingNotes_1.4.4.9.md`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/PortingNotes_1.4.4.9.md) | Terraria 1.4.4.9; tML `v2026.06.3.6`; Calamity not applicable | [MIT](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/LICENSE) |
| Calamity | [`CalamityTeam/CalamityModPublic@1a8cebd2…`](https://github.com/CalamityTeam/CalamityModPublic/commit/1a8cebd27ec5615316b78f71973446b5528d2b78), public mirror under the project organization | [`build.txt`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/build.txt): `version = 2.2.2`, `modReferences = CalamityModMusic` without a version | Terraria Unknown; tML Unknown; Calamity 2.2.2 itself | [Custom proprietary/all-rights-reserved terms](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/LICENSE.md), not an OSI license |
| Fargo's Souls | [`Fargo-Team/FargosSoulsMod@226fadea…`](https://github.com/Fargo-Team/FargosSoulsMod/commit/226fadeadbe3422785a7708ba2cdf53bd8548c00), project-team repository | [`build.txt`](https://github.com/Fargo-Team/FargosSoulsMod/blob/226fadeadbe3422785a7708ba2cdf53bd8548c00/build.txt): `version = 1.7.3.7`; `Fargowiltas@3.3.6`, `Luminance@1.0.3` | Terraria Unknown; tML Unknown; Calamity not declared | [MIT](https://github.com/Fargo-Team/FargosSoulsMod/blob/226fadeadbe3422785a7708ba2cdf53bd8548c00/LICENSE) |
| Infernum | [`InfernumTeam/InfernumMode@5b02e0de…`](https://github.com/InfernumTeam/InfernumMode/commit/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e), project-team repository | [`build.txt`](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/build.txt): `version = 2.0.1.35`; references `CalamityMod`, `SubworldLibrary`, `Luminance` without versions | Terraria Unknown; tML Unknown; Calamity dependency present, target version Unknown | [MIT](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/LICENSE.md) |
| Starlight River | [`ProjectStarlight/StarlightRiver@d367416d…`](https://github.com/ProjectStarlight/StarlightRiver/commit/d367416dcadfe68a541a5821a0c4be86b42db219), project repository | [`build.txt`](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/build.txt): `version = 0.2.9.4`; DLL references only | Terraria Unknown; tML Unknown; Calamity not declared | [GPL-3.0](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/LICENSE.txt) |
| Mod of Redemption | [`Hallam9K/RedemptionAlpha@5edb3c64…`](https://github.com/Hallam9K/RedemptionAlpha/commit/5edb3c6475cc2528d168f01fa99fd363ab71cd63), project-associated public repository | [`build.txt`](https://github.com/Hallam9K/RedemptionAlpha/blob/5edb3c6475cc2528d168f01fa99fd363ab71cd63/build.txt): `displayName = Mod of Redemption [BETA]`, `author = Halm & Tied`, `version = 0.8.0.4501`; [README](https://github.com/Hallam9K/RedemptionAlpha/blob/5edb3c6475cc2528d168f01fa99fd363ab71cd63/README.md) gives `ModSources/Redemption` build instructions | Terraria Unknown; tML Unknown; Calamity not declared | Standard root license Unknown/not found; README requires asking before code reuse and forbids asset reuse |

The Redemption repository is included because its pinned `build.txt` identifies the product and authors and its README gives direct build instructions for that mod. This establishes a reasonable project association for source-pattern research, but not current-release equivalence, sole-maintainer status, complete rights ownership, or permission to reuse anything.

### Exact path, symbol, and query ledger

#### tModLoader `29bf9785f5f4de8cd305be002c4cc48aa1177b20`

- Paths/symbols: [`ModSystem.cs` — `PostUpdateWorld`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModSystem.cs#L230-L236); [`ModPlayer.cs` — `CopyClientState`, `PreKill`, `Kill`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModPlayer.cs); [`PlayerLoader.cs` — `HookPreKill`, `PreKill`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/PlayerLoader.cs#L546-L560); [`Player.cs.patch` — `PlayerLoader.PreKill` inside `KillMe`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/Player.cs.patch#L6225-L6235); [`ModNPC.cs` — `SendExtraAI`, `ReceiveExtraAI`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModNPC.cs#L296-L307); [`ModProjectile.cs` — `SendExtraAI`, `ReceiveExtraAI`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModProjectile.cs#L130-L142).
- Example paths/symbols: [`ExampleMod.Networking.cs` — `HandlePacket`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/ExampleMod/ExampleMod.Networking.cs); [`ExampleStatIncreasePlayer.cs` — `SyncPlayer`, `CopyClientState`, `SendClientChanges`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/ExampleMod/Common/Players/ExampleStatIncreasePlayer.cs); [`ExampleNPCNetSync.cs`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/ExampleMod/Common/GlobalNPCs/ExampleNPCNetSync.cs); [`BasicTileEntity.cs` — `Update`, `NetSend`, `NetReceive`, `HookPostPlaceMyPlayer`, `KillMultiTile`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/ExampleMod/Content/TileEntities/BasicTileEntity.cs).
- Search terms: `PreKill`, `ret &=`, `PlayerLoader.PreKill`, `PostUpdateWorld`, `SendExtraAI`, `ReceiveExtraAI`, `netUpdate`, `HandlePacket`, `CopyClientState`, `TileEntityPlacement`, `TileEntitySharing`, `HookPostPlaceMyPlayer`, `KillMultiTile`.

#### Calamity `1a8cebd27ec5615316b78f71973446b5528d2b78`

- Paths/symbols: [`CalamityPlayerHitHurt.cs` — `PreKill`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/CalPlayer/CalamityPlayerHitHurt.cs#L150-L260); [`AresBody.cs` — `SendExtraAI`, `ReceiveExtraAI`, arm spawning, phase thresholds](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/NPCs/ExoMechs/Ares/AresBody.cs); [`ArenaWallSystem.cs` — `ActiveBoxes`, `PreUpdateMovement`, unload/removal](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Systems/Mechanic/ArenaWallSystem.cs); [`SupremeCalamitas.cs` — safe box, outside-target consequence, `CheckDead`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/NPCs/SupremeCalamitas/SupremeCalamitas.cs); [`AresDeathBeamTelegraph.cs` — timed attached telegraph and `PreDraw`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Boss/AresDeathBeamTelegraph.cs).
- Search terms: `override bool PreKill`, `nebulousCore`, `GodslayerArmorDash`, `silvaSet`, `necroSet`, `permafrostsConcoction`, `SendExtraAI`, `realLife`, `netUpdate`, `ArenaBox`, `SafeBox`, `CheckDead`, `Telegraph`, `OnWorldUnload`.

#### Fargo's Souls `226fadeadbe3422785a7708ba2cdf53bd8548c00`

- Paths/symbols: [`MutantBoss.cs` — `AttackChoice`, attack dispatcher, `ChooseNextAttack`, `AliveCheck`, `CheckDead`, owner projectile cleanup](https://github.com/Fargo-Team/FargosSoulsMod/blob/226fadeadbe3422785a7708ba2cdf53bd8548c00/Content/Bosses/MutantBoss/MutantBoss.cs); [`FargoSoulsPlayer.cs` — `PreKill`](https://github.com/Fargo-Team/FargosSoulsMod/blob/226fadeadbe3422785a7708ba2cdf53bd8548c00/Core/ModPlayers/FargoSoulsPlayer.cs).
- Search terms: `AttackChoice`, `ChooseNextAttack`, `HostCheck`, `netUpdate`, `CheckDead`, `ClearAllProjectiles`, `PreKill`, `revive`, `despawn`.

#### Infernum `5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e`

- Paths/symbols: [`DoGPhase1HeadBehaviorOverride.cs` — `Phase2TransitionState`, segment spawn, near-lethal transition](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/Content/BehaviorOverrides/BossAIs/DoG/DoGPhase1HeadBehaviorOverride.cs); [`CustomNPCDataSyncing.cs` — AI-array sync repair](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/Core/GlobalInstances/CustomNPCDataSyncing.cs); [`EnergyTelegraph.cs` — point collection `SendExtraAI`/`ReceiveExtraAI`](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/Content/BehaviorOverrides/BossAIs/CeaselessVoid/EnergyTelegraph.cs); [`InfernumPlayerEvents.cs` — centralized `PreKill` event fan-out](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/Core/GlobalInstances/Players/InfernumPlayerEvents.cs).
- Search terms: `Phase2TransitionState`, `SyncDoG`, `realLife`, `NPC.NewNPC`, `SyncNPC`, `SendExtraAI`, `ReceiveExtraAI`, `ai =`, `PreKill`, `CheckDead`, `despawn`.

#### Starlight River `d367416dcadfe68a541a5821a0c4be86b42db219`

- Paths/symbols: [`VitricBossAltar.cs` — activation and arena-side NPC handling](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/Content/Tiles/Vitric/VitricBossAltar.cs); [`SpawnNPCPacket.cs` — packet receive and `NPC.NewNPC`](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/Content/Packets/SpawnNPCPacket.cs); [`NPCs.VitricBoss.cs` — named AI phase/timers and `CheckDead`](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/Content/Bosses/VitricBoss/NPCs.VitricBoss.cs); [`NPCs.SquidBoss.cs` — part/platform lists, barrier spawn, attack choice, cleanup](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/Content/Bosses/SquidBoss/NPCs.SquidBoss.cs).
- Search terms: `SpawnNPCPacket`, `NPC.NewNPC`, `GlobalTimer`, `Phase`, `AttackPhase`, `AttackTimer`, `netUpdate`, `CheckDead`, `tentacles`, `platforms`, `barrier`, `despawn`.

#### Mod of Redemption `5edb3c6475cc2528d168f01fa99fd363ab71cd63`

- Paths/symbols: [`ArenaSystem.cs` — `ArenaPlayerIDs`, `NetSend`, `NetReceive`, `PostUpdatePlayers`, `ArenaPlayer`, request handler, deactivate](https://github.com/Hallam9K/RedemptionAlpha/blob/5edb3c6475cc2528d168f01fa99fd363ab71cd63/Globals/Areas/ArenaSystem.cs); [`PZ.cs` — action state, `SendExtraAI`, `CheckDead`](https://github.com/Hallam9K/RedemptionAlpha/blob/5edb3c6475cc2528d168f01fa99fd363ab71cd63/NPCs/Bosses/PatientZero/PZ.cs); [`PZ_Tele.cs` — harmless line telegraph](https://github.com/Hallam9K/RedemptionAlpha/blob/5edb3c6475cc2528d168f01fa99fd363ab71cd63/NPCs/Bosses/PatientZero/PZ_Tele.cs).
- Search terms: `ArenaPlayerIDs`, `ArenaBounds`, `HandleRequestArena`, `SendSyncArena`, `PostUpdatePlayers`, `DeadOrGhost`, `PreUpdateMovement`, `ActionState`, `SendExtraAI`, `CheckDead`, `Tele`, `DeactivateArena`, `ClearWorld`.

## Executive conclusions

1. **The individual building blocks have prior art.** Multiplayer bosses, server-gated spawns, phase IDs, multipart NPCs, bounded arenas, telegraph projectiles, scripted `CheckDead` transitions, and equipment-based self-revives all appear in public mods.
2. **The proposed raid lifecycle is not represented by a single reviewed implementation.** The specific combination of a fixed 2–4 player roster, death interception into synchronized Downed state, ally-channelled revive, shared revive tokens, simultaneous all-roster-Downed wipe evaluation, join/rejoin rules, and MMORPG-style assignments/DPS windows appears comparatively distinct in this sample.
3. **`ModPlayer.PreKill` is only an interception seam.** It must not become the encounter source of truth. The encounter runtime owns Downed state and evaluates wipe/revive results on the authority tick.
4. **The production `PreKill` hook is gated.** The pinned tModLoader implementation invokes every mod's `PreKill` non-short-circuit. Returning `false` from Convergence cannot prevent Calamity revival hooks from mutating life, cooldowns, buffs, UI, or VFX for the same lethal event. Do not connect the real hook until a Dedicated Server compatibility spike selects and proves one precedence/normalization policy.
5. **A damage NPC is not an arena security boundary.** Server geometry correction is the enforcement mechanism. A Dungeon Guardian-like sentinel can be presentation or escalation after a grace period, but cannot be trusted to stop godmode, immunity, teleport, or packet manipulation.
6. **Use explicit phase/timer/event data, not entity scans as ownership.** Public mods often scan NPC/projectile arrays or store mutable global indices because only one boss is expected. This project needs `FightId` plus an encounter-owned resource registry so stale entities cannot affect a later fight.
7. **Client-originated generic spawn/arena requests are rejected.** The server resolves the placed Core Tile Entity, arena anchor, boss type, progression, roster, and dimensions. A client may only request a named action against the current `FightId`.
8. **Dedicated-server death semantics remain the largest technical uncertainty.** tModLoader documents where `PreKill` runs, but a spike must record the real hurt/death/packet order in Single Player, Host & Play, and Dedicated Server before the Downed protocol is frozen.

## tModLoader authority and synchronization primitives

The reviewed baseline is tModLoader commit [`29bf9785f5f4de8cd305be002c4cc48aa1177b20`](https://github.com/tModLoader/tModLoader/commit/29bf9785f5f4de8cd305be002c4cc48aa1177b20), corresponding to the repository's pinned 1.4.4 stable line.

| Concern | Primary type/member | Evidence | Decision for this project |
|---|---|---|---|
| Authority tick | `ModSystem.PostUpdateWorld()` | The [fixed `ModSystem.cs` XML comment](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModSystem.cs#L230-L236) says this hook is called only in Single Player or on the server. | **Adopt.** Advance encounter timers, assignment deadlines, Downed timers, revive channels, DPS windows, wipe resolution, and gameplay cleanup here or from a coordinator called here. |
| Death interception | `ModPlayer.PreKill(...)`, `PlayerLoader.PreKill(...)` | Called immediately before death after life reaches zero. The pinned loader invokes all registered hooks using non-short-circuit boolean aggregation; any `false` suppresses the later vanilla death path, but does not suppress other mods' hook bodies. | **Gated adapter only.** Do not connect the production hook until the Calamity precedence/normalization spike below passes. Once enabled, never spend a token or decide a wipe inside an individual hook call. |
| Post-death notification | `ModPlayer.Kill(...)` | The [fixed `ModPlayer.cs` comments](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModPlayer.cs#L533-L560) place `Kill` after death and describe its call sites. | **Do not use as the Downed entry point.** It is too late for replacing death, but is useful for assertions/fallback cleanup when a non-raid death really occurs. |
| Dodge/self-save | `FreeDodge`, `ConsumableDodge` | The [fixed hook comments](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModPlayer.cs#L474-L497) say these hooks run on the local client receiving damage and may require a custom packet for multiplayer effect/cooldown synchronization. | **Reject for cooperative revive authority.** Their call semantics fit local avoidance/accessory effects, not a roster-wide state machine. |
| Player custom sync | `SyncPlayer`, `CopyClientState`, `SendClientChanges` plus `Mod.GetPacket()` | ExampleMod demonstrates explicit player-state packet flow and server relay. | **Use selectively.** Stable player presentation may use this pattern; encounter state still belongs to the encounter snapshot/event protocol, not independently drifting `ModPlayer` fields. |
| World sync | `ModSystem.NetSend` / `NetReceive` | Provides server-to-client world data synchronization. | **Use for low-frequency durable/world facts only.** The active raid is ephemeral and should use versioned snapshots/events with `FightId` and revision. |
| NPC custom state | `NPC.ai[]`, `NPC.netUpdate`, `ModNPC.SendExtraAI` / `ReceiveExtraAI` | Four `ai` floats sync automatically; custom state can be appended to NPC synchronization. | **Adopt for local entity projection.** Canonical phase/assignment state remains in the encounter runtime. Keep extra AI bounded and version-compatible. |
| Projectile custom state | `Projectile.netUpdate`, `ModProjectile.SendExtraAI` / `ReceiveExtraAI` | Custom projectile state travels with projectile synchronization. Projectile ownership changes packet authority. | **Use only for collision-bearing hazards.** Spawn raid hazards from the authority. Pure telegraph/background VFX should be reconstructed client-side from an encounter cue. |
| Tile Entity lifecycle | `ModTileEntity.Update`, `NetSend`, `NetReceive`, placement hooks | ExampleMod's `BasicTileEntity` notes that `Update` does not run on multiplayer clients and demonstrates server sharing and removal. | **Adopt.** The Core TE is a stable world anchor/activation adapter, not the owner of the full transient encounter. |
| Tile Entity placement | `MessageID.TileEntityPlacement`, `Generic_HookPostPlaceMyPlayer` | Official message documentation describes client request to server and server Tile Entity sharing. | **Adopt with validation.** Server verifies the real tile frame/origin and rejects duplicate or invalid placement/activation. |

Fixed primary references:

- [Pinned `ModSystem.cs`, including `PostUpdateWorld`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModSystem.cs#L230-L236)
- [Pinned `ModPlayer.cs`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModPlayer.cs)
- [Pinned `PlayerLoader.PreKill` implementation](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/PlayerLoader.cs)
- [Pinned `Player.KillMe` integration patch](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/Player.cs.patch)
- [Pinned `ModNPC.cs`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModNPC.cs)
- [Pinned `ModProjectile.cs`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModProjectile.cs)
- [ExampleMod packet router](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/ExampleMod/ExampleMod.Networking.cs)
- [ExampleMod player-state synchronization](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/ExampleMod/Common/Players/ExampleStatIncreasePlayer.cs)
- [ExampleMod NPC synchronization](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/ExampleMod/Common/GlobalNPCs/ExampleNPCNetSync.cs)
- [ExampleMod Tile Entity](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/ExampleMod/Content/TileEntities/BasicTileEntity.cs)

Moving supplementary API indexes: [`ModPlayer`](https://docs.tmodloader.net/docs/stable/class_mod_player.html), [`ModSystem`](https://docs.tmodloader.net/docs/stable/class_mod_system.html), [`NPC`](https://docs.tmodloader.net/docs/stable/class_n_p_c.html), [`ModNPC`](https://docs.tmodloader.net/docs/stable/class_mod_n_p_c.html), [`Projectile`](https://docs.tmodloader.net/docs/stable/class_projectile.html), [`ModProjectile`](https://docs.tmodloader.net/docs/stable/class_mod_projectile.html), and [`MessageID`](https://docs.tmodloader.net/docs/stable/class_message_i_d.html). These `docs/stable` pages are convenient navigation for the currently published stable API, but they can change in place and are not the reproducibility anchor for this review.

### Required death-interception spike

The API reference says `PreKill` is invoked for local, server, and remote players, but that statement alone does not prove which machine first observes every lethal hit under Terraria's player-damage networking. Before freezing packet IDs or save invariants, add temporary instrumentation and test:

- `Main.netMode`, authority/client role, `Player.whoAmI`, local owner, `FightId`, authority tick, life before/after, damage source, `PreKill`, `Kill`, and relevant packet receipt;
- environmental damage, NPC contact, hostile projectile, PvP-like damage if supported, instant-kill/death-reason paths, and repeated damage while already Downed;
- Single Player, Host & Play host victim, Host & Play remote victim, Dedicated Server hostless remote victim, 150–300 ms latency, and packet loss simulation;
- two lethal hits in the same authority tick and a lethal hit on the phase-transition/cleanup tick.

If the owning client is the first or only observer for a class of lethal damage, it may send a bounded **Downed candidate** notification. This is not permission to choose the target, spend a token, set a timer, revive, or declare a wipe. The server validates current membership and lifecycle, assigns the canonical transition, and broadcasts the resulting snapshot/event. This still inherits any trust limitations of vanilla player-damage networking and must be documented honestly.

### Mandatory Calamity `PreKill` compatibility gate

#### Confirmed behavior at the pinned revisions

The public API description is not enough to reason about cross-mod death interception. The fixed tModLoader source establishes the following call chain:

1. In the [`Player.cs.patch` integration](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/Player.cs.patch#L6225-L6235), `Player.KillMe` calls `StopVanityActions()`, initializes `playSound` and `genGore`, then calls `PlayerLoader.PreKill(...)` before the normal death body.
2. If the loader returns `false`, `Player.KillMe` returns immediately. The later vanilla death work and the later `PlayerLoader.Kill(...)` call do not run.
3. In [`PlayerLoader.cs`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/PlayerLoader.cs#L551-L560), `PlayerLoader.PreKill` initializes a local result to `true`, enumerates every registered `ModPlayer` hook, and combines each return with boolean `&=`.
4. Boolean `&` is non-short-circuit in C#. Therefore every enumerated `PreKill` body executes even after an earlier hook returned `false`. The final result is `false` if any hook returned `false`, while mutations to `Player`, `playSound`, `genGore`, and `damageSource`, plus arbitrary side effects, have already happened.

Consequently, a Convergence hook returning `false` does **not** claim exclusive ownership of the lethal event. A Calamity hook can still heal, add cooldowns/buffs, alter other state, emit VFX/SFX, and return `false`; the reverse is also true. Hook enumeration order can change which mutation is observed last, but it cannot prevent either hook body from running.

At Calamity commit [`1a8cebd27ec5615316b78f71973446b5528d2b78`](https://github.com/CalamityTeam/CalamityModPublic/commit/1a8cebd27ec5615316b78f71973446b5528d2b78), [`CalamityPlayerHitHurt.cs`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/CalPlayer/CalamityPlayerHitHurt.cs#L150-L260) performs work on entry before deciding its return and contains several revival branches. It calls `PopupGUIManager.SuspendAll()` unconditionally at hook entry. Reviewed conditional examples include:

- Nebulous Core healing, cooldown consumption, and bleedout-buffer clearing;
- God Slayer dash life-floor preservation;
- Silva revival buff/flags and bleedout-buffer clearing;
- Necro armor revival counter and full-life restoration;
- Permafrost's Concoction cooldown, buff, partial-life restoration, sound, and dust;
- other conditional death-side effects and presentation work earlier in the same hook.

Calamity returns from its own method at the first matching revival branch, but that internal ordering does not stop other mods' `PreKill` hooks because the outer tModLoader enumeration continues.

#### Hard implementation gate

Until the spike below is complete, production code **must not override/connect `RaidPlayer.PreKill` to the live Downed runtime**. It is acceptable to implement and unit-test:

- pure encounter lifecycle transitions;
- an unregistered death-adapter interface/fake;
- packet validation and snapshot projection that can be driven by tests;
- test-only instrumentation in a disposable pinned-runtime environment.

The gate is removed only after an ADR records one precedence policy, a supported normalization mechanism, dedicated-server evidence, and behavior when a third mod also intercepts `PreKill`. A compile pass or a single-player manual test is insufficient.

#### Precedence options to test and decide

Exactly one canonical outcome must be selected for each lethal event:

```text
VanillaDeath | CalamityPersonalRevive | RaidDowned
```

| Policy | Meaning | Required normalization | Main risk |
|---|---|---|---|
| Raid-first | An eligible roster member always becomes Downed; personal death-save effects do not replace it. | Prevent the personal effect through a supported compatibility seam, or explicitly normalize every mutated Calamity state/resource after the full hook chain. Returning `false` alone is not enough. | Consuming a cooldown/buff/item effect while also becoming Downed, or brittle rollback of Calamity-private state. |
| Calamity-first | A valid Calamity personal revive resolves the lethal event; Downed occurs only when no personal revive triggered. | Defer the Downed candidate until after the full chain and classify the personal-revive result through a supported compatibility signal/API. | Inferring revival from life alone is ambiguous; several effects preserve life differently and future Calamity updates may add branches. |
| Activation restriction | Raid start rejects known personal-revive effects/loadouts. | Server-side preflight and continuous validation with explicit player-facing failure codes. | Fragile, composition-hostile, easy to bypass through equipment changes, and contrary to the project's build-flexibility goal. Temporary fallback only. |

No policy is selected by this research note. Direct reflection into Calamity private fields and hard-coded rollback of its cooldown/buff internals are not acceptable architecture. Any integration belongs in `Common/Compatibility/Calamity`, is version-gated, and must fail closed with a clear reason if the pinned contract changes.

Regardless of policy, normalization must guarantee:

- one lethal event produces one canonical outcome and one encounter revision;
- no revive token is spent on Downed entry;
- a Calamity resource/cooldown is consumed zero or one time according to the recorded policy, never accidentally in addition to RaidDowned;
- final life, buffs, immunity, movement lock, UI, sound/gore flags, and death reason match that outcome;
- suppressed death does not emit a tombstone/death message or invoke `PlayerLoader.Kill`;
- repeated lethal processing while already Downed is idempotent;
- the server outcome wins and clients only predict/present it.

#### Required compatibility spike matrix

Instrument a disposable build of the pinned tModLoader runtime, or use another test seam that can observe the start/end of the complete loader chain. Logging only inside Convergence's own hook is insufficient unless its exact enumeration position is proved.

| Axis | Required cases |
|---|---|
| Runtime | Single Player; Host & Play with host victim; Host & Play with remote victim; Dedicated Server with each remote victim |
| Network | Baseline; 150–300 ms latency; packet reordering/loss where the test harness supports it |
| Raid state | No raid; rostered active player; already Downed; resolving/cleanup tick; non-roster intruder |
| Calamity revival | None; Nebulous Core; God Slayer dash; Silva; Necro; Permafrost's Concoction; more than one eligible effect |
| Damage path | NPC contact; hostile projectile; environment/DoT; custom death reason; two lethal sources in one authority tick |
| Lifecycle race | Same-tick two-player lethal damage; revive completion on lethal/expiry tick; disconnect during intercepted death; phase transition during interception |
| Mod ordering | Record actual `HookPreKill` enumeration order and repeat after dependency/load-order perturbation where supported |

For every case, capture before/after values for life, death/ghost flags, immunity, relevant Calamity cooldown/buff/effect state through supported access, `playSound`, `genGore`, death reason, Downed candidate count, encounter revision, token count, packets, and whether `Kill` ran. Assert that VFX/SFX and UI effects occur at most once on the intended clients and that the next authority tick remains stable rather than re-entering `PreKill`.

The spike result must also identify the safe observation point for normalization. If no supported point can distinguish `CalamityPersonalRevive` from `RaidDowned` without reaching into private state, the gate stays closed and the MVP uses an explicit activation restriction or revises the Downed design.

## Cross-mod source survey

### Summary matrix

| Project and reviewed revision | Closest evidence | Useful design knowledge | Rejected or constrained pattern |
|---|---|---|---|
| Calamity `1a8cebd…` | Equipment self-save in `CalamityPlayerHitHurt`; Ares multipart/phase sync; Supreme Calamitas arena; Ares beam telegraph | `PreKill` can suppress a death; server-spawned linked parts; geometry separate from drawing; telegraph duration/anchor; scripted nonterminal `CheckDead` | Accessory self-save is not cooperative Downed; no static global boss index or unscoped entity scan as ownership; no copied code/assets |
| Fargo's Souls `226fade…` | Mutant explicit attack dispatcher, host-gated spawns, phase transitions, owner cleanup; player self-revives | Explicit attack/phase IDs, reset-and-net-update transitions, scripted death state, owner-scoped cleanup | Avoid a single monolithic boss switch for the final raid; equipment revive is not shared revive authority |
| Infernum `5b02e0d…` | DoG phase transition/server segment spawn; custom NPC sync guard; point-array telegraph; centralized player events | Explicit transition states, server-owned parts, never alias mutable `ai[]` arrays | Bound every decoded collection before allocation; do not use static event aggregation for critical Downed lifecycle; no global boss indices |
| Starlight River `d367416…` | Vitric altar/spawn packet/arena side parts; Vitric and Squid scripted phases/cleanup | Readable named accessors for `ai[]`; phase-transition `netUpdate`; target invalidation policy; boss-owned part lists | Never accept client-selected NPC type/position/AI; type scans cannot distinguish stale/concurrent fights; GPL code remains design-only |
| Mod of Redemption `5edb3c6…` | `ArenaSystem` participant set, inside/outside barrier, arena sync; Patient Zero phases/telegraph | Closest arena-roster prior art; dual-sided boundary; dedicated-server presentation separation | Client arena request needs sender/range/progression validation; player slots need encounter generation; no standard license found, so zero copying |

### Calamity Mod

Official public mirror: [CalamityTeam/CalamityModPublic](https://github.com/CalamityTeam/CalamityModPublic), reviewed at [`1a8cebd27ec5615316b78f71973446b5528d2b78`](https://github.com/CalamityTeam/CalamityModPublic/commit/1a8cebd27ec5615316b78f71973446b5528d2b78).

#### Death suppression and self-revive

[`CalPlayer/CalamityPlayerHitHurt.cs`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/CalPlayer/CalamityPlayerHitHurt.cs) implements several equipment/progression effects in `PreKill`: restore or preserve life, apply a cooldown/effect, and return `false` to prevent death.

**Lesson:** `PreKill` is a demonstrated interception seam. **Non-lesson:** this is a personal item effect, not a party roster, ally channel, token transaction, or all-player wipe system. Because tModLoader invokes all `PreKill` hooks non-short-circuit, these effects are also an active compatibility hazard, not merely unrelated prior art; the mandatory gate above applies. Copying their condition order would couple raid behavior to unrelated equipment semantics.

#### Multipart boss and phase ownership

[`NPCs/ExoMechs/Ares/AresBody.cs`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/NPCs/ExoMechs/Ares/AresBody.cs) demonstrates:

- server-only creation of four arm NPCs;
- parent/part links through NPC relationship fields and explicit sync;
- extra AI serialization for state not carried by base AI slots;
- life-ratio and sibling state checks controlling passive, immune, or berserk behavior.

**Adopt:** authority-owned root creates parts and transitions a shared phase. **Change:** register root and parts under `FightId` with stable resource handles. Do not let global `whoAmI` values or an NPC-array scan be the encounter identity.

#### Arena and outsider consequence

[`Systems/Mechanic/ArenaWallSystem.cs`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Systems/Mechanic/ArenaWallSystem.cs) keeps active box geometry and performs client rendering while a player hook constrains movement. [`NPCs/SupremeCalamitas/SupremeCalamitas.cs`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/NPCs/SupremeCalamitas/SupremeCalamitas.cs) calculates/synchronizes the safe box and applies consequences when the target leaves; it also uses scripted `CheckDead` behavior.

**Adopt:** geometry/result on authority, color/VFX on client, explicit despawn/removal condition, scripted terminal transition. **Change:** the raid must constrain every roster member and reject non-roster entry, not merely enrage from the current target's position.

#### Telegraph

[`Projectiles/Boss/AresDeathBeamTelegraph.cs`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Boss/AresDeathBeamTelegraph.cs) represents an attached, timed, non-damaging warning before the real attack and draws it client-side.

**Adopt:** telegraphs need a stable anchor, start tick, deadline, geometry, and attack identity. **Change:** pure visuals should normally be client reconstruction from an authoritative mechanic event; only collision-bearing state needs a network projectile.

### Fargo's Souls Mod

Official team repository: [Fargo-Team/FargosSoulsMod](https://github.com/Fargo-Team/FargosSoulsMod), reviewed at [`226fadeadbe3422785a7708ba2cdf53bd8548c00`](https://github.com/Fargo-Team/FargosSoulsMod/commit/226fadeadbe3422785a7708ba2cdf53bd8548c00).

[`Content/Bosses/MutantBoss/MutantBoss.cs`](https://github.com/Fargo-Team/FargosSoulsMod/blob/226fadeadbe3422785a7708ba2cdf53bd8548c00/Content/Bosses/MutantBoss/MutantBoss.cs) demonstrates an explicit attack dispatcher, distinct phase paths, host-gated projectile/random decisions, `netUpdate` on attack transition, target/despawn handling, scripted `CheckDead`, and cleanup of projectiles associated with the boss.

**Adopt:** explicit, debuggable attack/phase IDs; transition code resets phase-local timers; authority chooses randomness; terminal animation is a state rather than accidental death; transient hazards carry an owner token. **Change:** the completed raid should not become one enormous NPC `switch`. Keep encounter phase policy and mechanic executors in feature-local runtime objects.

[`Core/ModPlayers/FargoSoulsPlayer.cs`](https://github.com/Fargo-Team/FargosSoulsMod/blob/226fadeadbe3422785a7708ba2cdf53bd8548c00/Core/ModPlayers/FargoSoulsPlayer.cs) contains `PreKill`-based accessory self-saves. Like Calamity, this is evidence for the interception hook, not prior art for ally revival/shared tokens.

### Infernum Mode

Official team repository: [InfernumTeam/InfernumMode](https://github.com/InfernumTeam/InfernumMode), reviewed at [`5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e`](https://github.com/InfernumTeam/InfernumMode/commit/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e).

[`Content/BehaviorOverrides/BossAIs/DoG/DoGPhase1HeadBehaviorOverride.cs`](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/Content/BehaviorOverrides/BossAIs/DoG/DoGPhase1HeadBehaviorOverride.cs) demonstrates an explicit phase-transition state, server-only segment creation with links/sync, near-lethal conversion into an animation, and despawn handling.

[`Core/GlobalInstances/CustomNPCDataSyncing.cs`](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/Core/GlobalInstances/CustomNPCDataSyncing.cs) documents a multiplayer failure mode caused by sharing mutable NPC AI array references across slots. **Adopt the warning:** store copied values, stable IDs, and encounter registry entries; never alias an entity's mutable `ai[]` as shared phase state.

[`Content/BehaviorOverrides/BossAIs/CeaselessVoid/EnergyTelegraph.cs`](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/Content/BehaviorOverrides/BossAIs/CeaselessVoid/EnergyTelegraph.cs) synchronizes a variable-length point collection for a visual. The reviewed file is valuable mainly as a decoder warning: this project's protocol must reject a count above its per-packet maximum **before** allocating or looping.

[`Core/GlobalInstances/Players/InfernumPlayerEvents.cs`](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/Core/GlobalInstances/Players/InfernumPlayerEvents.cs) centralizes `ModPlayer` hook fan-out through static events. This is convenient for optional effects but is rejected for Downed/Revive: critical lifecycle transitions need one explicit owner, deterministic ordering, and teardown that cannot leave a static subscriber behind.

### Starlight River

Official project repository: [ProjectStarlight/StarlightRiver](https://github.com/ProjectStarlight/StarlightRiver), reviewed at [`d367416dcadfe68a541a5821a0c4be86b42db219`](https://github.com/ProjectStarlight/StarlightRiver/commit/d367416dcadfe68a541a5821a0c4be86b42db219).

[`Content/Tiles/Vitric/VitricBossAltar.cs`](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/Content/Tiles/Vitric/VitricBossAltar.cs) shows altar-driven boss activation and reconstruction/removal of arena-side NPC components. [`Content/Bosses/VitricBoss/NPCs.VitricBoss.cs`](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/Content/Bosses/VitricBoss/NPCs.VitricBoss.cs) gives AI slots readable phase/timer meanings, updates network state on phase changes, retargets invalid players, and uses a scripted death transition.

[`Content/Bosses/SquidBoss/NPCs.SquidBoss.cs`](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/Content/Bosses/SquidBoss/NPCs.SquidBoss.cs) holds part/platform collections, server-spawns an arena blocker, selects attack variants on the authority, and explicitly removes parts on terminal handling.

[`Content/Packets/SpawnNPCPacket.cs`](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/Content/Packets/SpawnNPCPacket.cs) is a cautionary example. In the inspected file, a generic packet carries spawn position/type/AI and creates the NPC on receipt; sender/range/progression validation is not visible there. A surrounding packet framework may impose checks not visible in this file, so this is an inference, not a vulnerability claim. Regardless, this project will not expose a generic spawn request.

**Adopt:** named phase/timer accessors, authority-selected attack variant, explicit target invalidation, and boss-owned part cleanup. **Change:** every part/hazard receives an encounter ownership token; cleanup does not depend only on scanning by NPC type.

### Mod of Redemption

Public project-associated repository: [Hallam9K/RedemptionAlpha](https://github.com/Hallam9K/RedemptionAlpha), reviewed at [`5edb3c6475cc2528d168f01fa99fd363ab71cd63`](https://github.com/Hallam9K/RedemptionAlpha/commit/5edb3c6475cc2528d168f01fa99fd363ab71cd63). The pinned `build.txt` identifies “Mod of Redemption [BETA]” and “Halm & Tied,” while its README provides direct `ModSources/Redemption` build instructions. This is sufficient for a scoped source-pattern survey, but it is not a claim that this commit is the current official Workshop source, that the GitHub account is the sole rights holder, or that reuse is licensed.

[`Globals/Areas/ArenaSystem.cs`](https://github.com/Hallam9K/RedemptionAlpha/blob/5edb3c6475cc2528d168f01fa99fd363ab71cd63/Globals/Areas/ArenaSystem.cs) is the closest arena-roster prior art in this sample. It maintains one active arena, rectangular bounds, a participant player-ID set, server-side removal of dead/inactive participants, arena synchronization, client drawing separation, and player updates that keep participants inside while keeping nonparticipants outside.

This supports the feasibility of a dual-sided logical barrier without generating hundreds of wall tiles. It does **not** supply the proposed raid lifecycle: dead players leave the active roster, there is no synchronized Downed timer or ally revive transaction, no shared token count, no `FightId` generation, and mutable player slots are the identifiers.

The inspected request handler includes a TODO concerning the player argument and accepts arena parameters from the request. No sender-distance/progression/size validation is visible in that handler. That observation is file-scoped, not a security accusation. Our server will accept only `RequestCoreActivation(coreEntityId)` and derive all other values itself.

[`NPCs/Bosses/PatientZero/PZ.cs`](https://github.com/Hallam9K/RedemptionAlpha/blob/5edb3c6475cc2528d168f01fa99fd363ab71cd63/NPCs/Bosses/PatientZero/PZ.cs) demonstrates explicit action state, extra AI synchronization, authority-chosen attacks, and a scripted death animation. [`NPCs/Bosses/PatientZero/PZ_Tele.cs`](https://github.com/Hallam9K/RedemptionAlpha/blob/5edb3c6475cc2528d168f01fa99fd363ab71cd63/NPCs/Bosses/PatientZero/PZ_Tele.cs) demonstrates a harmless visual line telegraph.

## Originality assessment

### Clearly established patterns

- host/server-only boss-part and hazard spawning;
- explicit phase and attack state with `netUpdate`/extra AI;
- scripted near-death/death-animation transitions;
- logical rectangle arenas with client-side visuals;
- current-target escape consequences and player movement constraints;
- participant ID sets for an arena;
- personal `PreKill` self-save effects;
- timed non-damaging telegraphs followed by a real attack;
- boss/owner-related projectile and part cleanup.

### Comparatively distinct combination in the reviewed sample

- an immutable-at-pull 2–4 player raid roster separate from “players currently alive”;
- server-owned per-participant combat state `Alive`, `Downed`, or `Eliminated`, plus a separate connection binding/epoch and authority-owned revive-channel lease;
- death replaced by Downed without removing the player from encounter membership;
- another roster member channeling a revive with distance, interruption, and concurrent-request arbitration;
- shared revive-token expenditure as one atomic encounter transaction;
- wipe evaluated only after all same-tick Downed transitions have been applied;
- terminal snapshots and rejoin/disconnect policy keyed by `FightId`/revision;
- assignment, part-break, and DPS-check windows integrated with that lifecycle.

This is enough to justify treating multiplayer raid/Downed as a first-class subsystem rather than copying a boss accessory effect. It does not justify a claim of worldwide novelty or legal exclusivity.

## Recommended Downed and Revive contract

### Canonical states

[ADR-0005](../adr/0005-server-authoritative-downed-revive.md) fixes three participant combat states. Connection and channel state are orthogonal rather than extra combat states.

| Combat state | Can move/attack | Can be targeted for revive | Counts as standing for wipe | Authority exit |
|---|---:|---:|---:|---|
| `Alive` | Yes while connected and not control-locked by terminal resolution | No | Yes only when connected under the accepted availability policy | Lethal transition, elimination, encounter cleanup |
| `Downed` | No | Yes while connected/eligible | No | Successful revive to `Alive`, timeout to `Eliminated`, or terminal cleanup |
| `Eliminated` | No | No | No | Encounter cleanup only |

`ReviveChannel` is an authority-owned relation `(reviverParticipantId, targetParticipantId, startTick, deadlineTick, channelNonce)`, not a participant state. Connected/disconnected status is part of the server binding with a monotonic connection epoch. Recovery life, short invulnerability, and Weakness are projections emitted by a successful transition back to `Alive`, not a fourth combat state.

### Lethal transition

1. **Only after the compatibility gate above is removed**, `RaidPlayer.PreKill` checks only enough adapter state to participate in interception for the current active `FightId`; it does not assume that its own return suppresses another mod's hook.
2. The authority queues a Downed candidate rather than mutating every encounter outcome inline.
3. On the authority update, deduplicate by participant and damage event/tick, validate roster/lifecycle, set life to a safe nonzero floor, cancel actions/grapple/mount as specified, grant minimal transition immunity, and enter `Downed`.
4. Apply all queued lethal transitions for that tick.
5. Evaluate all-roster-Downed/eliminated/disconnected policy once, after the batch.
6. Emit a revisioned result event/snapshot; clients render marker, animation, UI, and sound.

This ordering prevents the first of two simultaneous Downed hooks from observing a half-updated roster and declaring an order-dependent wipe. Repeated `PreKill` while already Downed must be idempotently ignored and rate-limited.

### Revive request and channel

A client request contains only bounded identifiers and intent:

```text
RequestRevive(FightId, ObservedRevision, TargetParticipantId)
CancelRevive(FightId, LeaseRevision)
```

The server resolves the sender from the packet transport and validates:

- exact active `FightId` and acceptable observed revision;
- sender is rostered, connected, standing, and not already committed elsewhere;
- target is the rostered Downed participant and is not eliminated;
- target is within server-calculated range and line-of-sight policy;
- phase permits revival and the encounter has an available token/reservation;
- movement, displacement, incoming damage, Downed transition, disconnect, or terminal transition interrupts according to one documented rule;
- only one active lease owns a target; simultaneous requests are resolved deterministically by authority receipt tick and participant ID as tie-breaker.

At deadline, validate again and commit atomically: consume one previously reserved shared token, restore the target to `Alive` with configured life, emit recovery immunity/Weakness projection data, clear the lease, and increment encounter revision. Reservation occurs on accepted channel start; consumption occurs only on successful completion, and every cancellation releases its reservation.

### Decisions fixed by ADR-0005

These are no longer open research questions. [ADR-0005](../adr/0005-server-authoritative-downed-revive.md) and its domain tests control the implementation.

| Question | Accepted decision |
|---|---|
| Pull size and token count | A pull-time roster of 2/3/4 starts with 1/2/3 shared tokens. Join/leave does not rescale the pool. |
| Channel and expiry tuning | Initial channel is 120 authority ticks; Downed expiry and reconnect grace are each 1,800 ticks. They are balance inputs, not packet constants. |
| Token transaction | Start reserves capacity; only successful completion consumes one token; interruption/cancellation releases the reservation. |
| Last standing participant | One connected `Alive` participant may channel a revive while others are Downed. The Raid fails with `AllParticipantsDowned` only after all same-tick lethal transitions are batched and the single `CommitTick` evaluation finds nobody standing. |
| Downed expiry | With zero available tokens, expiry produces terminal `DownedTimeoutWithoutToken`; with tokens remaining, that participant becomes `Eliminated` without consuming a token and the remaining party may continue. |
| Disconnect/rejoin | A disconnected `Alive` participant retains stable participant/combat state during a 1,800-tick grace. The encounter remains recoverable during grace; rejoin requires the same participant with a strictly newer authority epoch and is rejected at/after the deadline. Expiry with no connected `Alive` participant produces `NoAvailableParticipants`. |
| Exact deadline tie | Interruption, invalid target, and Downed expiry win over revive completion on the same tick. Timeout processing precedes the final failure evaluation. |
| Same-tick ordering | Authority commands are accepted only before their tick commits; lethal and connection transitions are applied before one terminal evaluation. Late commands are rejected. |

### Remaining open integration questions

- Which process first observes every supported lethal-damage path, and what packet recovery is needed on a Dedicated Server?
- Does RaidDowned or a Calamity personal revival take precedence, and what supported post-chain normalization seam proves exactly one outcome?
- What server-side range, line-of-sight, movement, displacement, and damage thresholds interrupt a revive without making latency unfair?
- What recovery life ratio, invulnerability duration, and Weakness tuning ship for the first playtest?
- Which authenticated/session fact binds a reconnecting network client to the stable participant in the real adapter without trusting a name or client-supplied UUID?
- How do forced external teleports and other mods' control effects map to movement interruption and safe-position restoration?

## Arena, Core, pillars, and intruder handling

### Authority-owned activation

The only client gameplay action is a bounded request naming a Core Tile Entity ID. Transport may reject malformed/rate-limited input before lifecycle entry, but it does not resolve or mutate World state. The server then:

1. atomically reserves the one-encounter slot and creates a `Validating` session with a new non-repeating `FightId` **before** resolving the requested Core;
2. resolves the sender and Core from server-owned connection/World state, then checks the actual tile frames, Tile Entity identity, distance, player state, progression, and lifecycle constraints;
3. derives the candidate center, fixed 320 × 140 bounds, four pillar anchors, and candidate 2–4-player roster from server-known state;
4. runs arena clearance, terrain, anchor, roster, and compatibility validation as a side-effect-free operation: no pillars, barrier, player locks, boss NPC, or persistent active flag are created here;
5. on failure, publishes a machine-readable terminal validation result for that `FightId` and cleans the empty session idempotently;
6. on success, transitions to `Preparing`, freezes the validated roster, and only then spawns/registers pillars, activates the logical barrier, and applies gameplay projection leases;
7. publishes the prepared snapshot after every required resource is registered, or rolls back only resources owned by that `FightId` through the same idempotent authority cleanup path.

Creating `FightId` first makes even rejection/rollback observable and race-safe. Keeping `Validating` side-effect-free prevents an invalid Core or failed geometry check from leaving partial World mutations.

### Dual-sided logical barrier

| Actor | Authority behavior | Client presentation |
|---|---|---|
| Roster member crossing outward | Clamp/resolve velocity and position to last valid point or nearest inner inset; allow a small latency-safe tolerance. | Predict clamp, show field collision and warning. |
| Non-roster player crossing inward | Reject/resolve to nearest outer inset; never add to roster implicitly. | Show opaque/repulsive field and spectator guidance. |
| Teleport/Recall/Pylon request | Reject while rostered in active encounter, or resolve back inside according to policy. | Disable/annotate UI where possible; server remains final. |
| Forced external teleport | Detect next authority tick, restore to valid encounter point, log reason code. | Short recovery VFX; avoid repeated screen shake. |
| Persistent intrusion | Escalating warning → knockback/teleport → optional sentinel punishment after grace. | Dungeon Guardian-like sentinel may visualize enforcement. |

A sentinel NPC must not be the primary boundary: damage immunity, godmode, despawn, target limits, or high ping can bypass or distort it. It also creates unnecessary AI/network load. The server's geometry correction is the security and consistency mechanism.

Pillars and boss parts receive `(FightId, ResourceId, Role)` ownership metadata. A pillar destroyed by an unrelated source, a stale NPC of the same type, or a prior fight must never satisfy the current seal check.

## Boss/raid scaffold derived from the survey

The public bosses support explicit state IDs and authority-owned parts, but this project should expose a small encounter runtime rather than embedding all raid semantics in one `ModNPC`.

| Phase | Authority-owned outcome | Minimum synchronized cue |
|---|---|---|
| `Validating` | Session/Fight ID creation, Core resolution, side-effect-free arena/roster checks | normally only terminal validation result; no gameplay resource cue |
| `Preparing` | Validated roster freeze and gameplay resource registration | `FightId`, arena, roster IDs, ready deadline |
| `SealPillars` | Four/roster-scaled pillar objectives and timeout/Overload result | phase start/deadline, assignment IDs, pillar resource states |
| `PartBreak` | Root/part HP and chosen destroyed-part consequence | part IDs/state, phase deadline, consequence ID |
| `Coordination` | Stack/spread/line target assignments and result | mechanic ID, target IDs, geometry, start/resolve ticks, seed |
| `WeakpointDps` | Weakpoint open/close and authoritative HP-delta result | window start HP, start/deadline, multiplier, result |
| `PersonalEffigies` | One ownership-bound clone objective per roster member | clone resource ID, assignee, deadline, result |
| `LoopOrEnrage` | Increment Overload and choose next route | stack count, next phase ID/start tick |
| `LastStand` | Fixed sequence and terminal Core exposure | sequence seed, step start ticks, alive-roster projection |
| `Resolving` | Win/wipe reason, terminal snapshot, rewards, cleanup | terminal reason/revision before despawn |

For the MVP, implement only enough states to prove the infrastructure; do not pre-build all production attacks.

### DPS checks

- The client never reports DPS or “check passed.”
- The authority records the root/weakpoint HP at open and close, or accumulates server-applied damage events if class/part attribution is required.
- The deadline is an authority tick, not a client timer.
- A late hit either belongs before the inclusive deadline or after it; define ordering relative to NPC hit processing and `PostUpdateWorld`.
- Sync only start value/target/deadline and final result. Combat text and meter animation are projections.
- First implementation should test boss DR, multi-hit projectiles, minions, rogue stealth effects, damage caps, and simultaneous part death at the deadline.

### Telegraphs

Represent a gameplay telegraph as a bounded contract:

```text
TelegraphCue(FightId, MechanicInstanceId, ShapeId, AnchorResourceId,
             TargetParticipantId?, StartTick, ResolveTick, Geometry, StyleId)
```

The authority decides target, geometry, and resolve tick. Clients interpolate/draw. At resolve, the authority spawns or activates the smallest collision-bearing hazard needed. Do not network particles, trails, screen shake, warning typography, or decorative background bullets.

### Cleanup

Every authority-owned transient gameplay resource or projection lease is registered at creation under the encounter runtime:

- logical barrier gameplay handle;
- four pillars;
- boss root and parts;
- clones;
- hostile collision projectiles/hazards;
- revive leases, participant bindings, and server-side player-control projection leases;
- pending authoritative activation/terminal replication state until the terminal revision is published.

Authority cleanup is idempotent and gameplay-only. It may be called from victory, wipe, Core destruction, boss despawn, validation rollback, disconnect policy, World unload, or exception recovery. Type-wide NPC/projectile scans are a last-resort orphan sweep, not normal ownership.

Client music, UI, camera, particles, warning typography, and barrier VFX are **not** authority resources or Dedicated Server cleanup participants. Each client observes the terminal replicated state/tombstone and idempotently stops, fades, or resets its own presentation. The Dedicated Server never creates those objects, calls their teardown, waits for their acknowledgement, or treats presentation failure as incomplete gameplay cleanup. Snapshot recovery, replica reset, and client World unload provide fallback local teardown if a terminal event is missed.

Publish one terminal snapshot/result before removing gameplay entities so clients can begin local presentation teardown and do not confuse normal cleanup with packet loss. A new encounter uses a new `FightId`; late packets for the old ID are ignored.

## Packet and decoder hazards learned from prior art

| Hazard | Rule |
|---|---|
| Client chooses world object, NPC type, coordinates, arena size, or result | Client sends only named intent and stable ID; server resolves all world values. |
| Variable-length collection or string trusts an encoded length | Every packet type has a fixed field order plus explicit per-type maxima for collection counts and encoded string bytes/characters. Validate type/enum/range and count/length before allocation or iteration; use bounded repository decoders for strings. Never use `BinaryReader.BaseStream.Length` or `Position` as a Mod packet boundary. |
| Mutable `NPC.ai[]` reference reused across slots | Copy primitives into owned runtime state; use stable resource IDs. |
| `whoAmI`/player slot treated as permanent identity | Pair slot with roster generation/FightId and validate connected identity on every request. |
| Static boss index or type scan owns parts | Encounter resource registry is canonical; scans only clean orphans. |
| Visual projectile is networked per player | Send one bounded mechanic cue; draw locally. |
| Cleanup packet races terminal state | Send terminal revision first, make cleanup repeatable, reject stale FightId. |
| Client snapshot mutates gameplay | Client replica is read-only; requests cannot contain authoritative state. |

`Mod.HandlePacket` receives a reader supplied by tModLoader; this repository does not assume its `BaseStream` begins or ends at this Mod's logical payload. Decoder safety therefore comes from the message schema and per-type maxima, not “remaining stream bytes.” Fixed-size fields are read in the declared order, optional sections have explicit presence/version fields, and any bound violation rejects the message before allocating a variable-size object.

## Rights and reuse policy

This section is conservative by design and is not legal advice.

| Source | Reviewed rights signal | Repository policy |
|---|---|---|
| tModLoader | [MIT license](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/LICENSE) and official API/ExampleMod | Implement against public API/templates; retain required notices for any literal licensed material, although this survey imports none. |
| Calamity | [Custom terms](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/LICENSE.md) state that the project and associated materials are proprietary/all rights reserved. They expressly allow source use as a reference and separately state a credit condition if code is lifted, but do not provide a conventional affirmative open-source license grant defining permitted copying scope. | **Treat literal reuse as not permitted absent explicit permission and rights review. Design knowledge only; zero code/assets copied.** Integrate through the documented runtime dependency/compatibility boundary. |
| Fargo's Souls | [MIT license](https://github.com/Fargo-Team/FargosSoulsMod/blob/226fadeadbe3422785a7708ba2cdf53bd8548c00/LICENSE) | Even though MIT permits reuse with conditions, this project takes design knowledge only to preserve independent architecture and provenance. |
| Infernum | [MIT license](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/LICENSE.md) | Design knowledge only; no copied code/assets. |
| Starlight River | [GPL-3.0 license](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/LICENSE.txt) and [README reuse notice](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/README.md) | **Design knowledge only.** Do not copy or adapt source unless the project deliberately selects a GPL-compatible distribution strategy after review. |
| Mod of Redemption | [README](https://github.com/Hallam9K/RedemptionAlpha/blob/5edb3c6475cc2528d168f01fa99fd363ab71cd63/README.md) asks users to ask before borrowing code; no standard open-source license was found in the reviewed repository root. | **Zero copying without explicit written permission and rights review.** High-level observation only. |

Do not copy identifiers, comments, control flow, balancing constants, audiovisual assets, or distinctive attack sequences merely because a repository is readable. Keep research citations in documentation and write tests/specifications before independent implementation.

## Source URL index

### tModLoader

- [Pinned tModLoader commit](https://github.com/tModLoader/tModLoader/commit/29bf9785f5f4de8cd305be002c4cc48aa1177b20)
- [`v2026.06.3.6` release](https://github.com/tModLoader/tModLoader/releases/tag/v2026.06.3.6)
- [Terraria 1.4.4.9 port metadata](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/PortingNotes_1.4.4.9.md)
- [`ModPlayer` moving supplemental API index](https://docs.tmodloader.net/docs/stable/class_mod_player.html)
- [`ModPlayer.cs`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModPlayer.cs)
- [`PlayerLoader.cs`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/PlayerLoader.cs)
- [`Player.cs.patch`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/Player.cs.patch)
- [`ModSystem.cs`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModSystem.cs)
- [`ModSystem` moving supplemental API index](https://docs.tmodloader.net/docs/stable/class_mod_system.html)
- [`NPC` moving supplemental API index](https://docs.tmodloader.net/docs/stable/class_n_p_c.html)
- [`ModNPC.cs`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModNPC.cs)
- [`ModNPC` moving supplemental API index](https://docs.tmodloader.net/docs/stable/class_mod_n_p_c.html)
- [`Projectile` moving supplemental API index](https://docs.tmodloader.net/docs/stable/class_projectile.html)
- [`ModProjectile.cs`](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/ModProjectile.cs)
- [`ModProjectile` moving supplemental API index](https://docs.tmodloader.net/docs/stable/class_mod_projectile.html)
- [`MessageID` moving supplemental API index](https://docs.tmodloader.net/docs/stable/class_message_i_d.html)
- [ExampleMod packet router](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/ExampleMod/ExampleMod.Networking.cs)
- [ExampleMod player sync](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/ExampleMod/Common/Players/ExampleStatIncreasePlayer.cs)
- [ExampleMod NPC sync](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/ExampleMod/Common/GlobalNPCs/ExampleNPCNetSync.cs)
- [ExampleMod Tile Entity](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/ExampleMod/Content/TileEntities/BasicTileEntity.cs)

### Calamity

- [Pinned Calamity commit](https://github.com/CalamityTeam/CalamityModPublic/commit/1a8cebd27ec5615316b78f71973446b5528d2b78)
- [`build.txt`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/build.txt)
- [`CalamityPlayerHitHurt.cs`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/CalPlayer/CalamityPlayerHitHurt.cs)
- [`AresBody.cs`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/NPCs/ExoMechs/Ares/AresBody.cs)
- [`ArenaWallSystem.cs`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Systems/Mechanic/ArenaWallSystem.cs)
- [`SupremeCalamitas.cs`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/NPCs/SupremeCalamitas/SupremeCalamitas.cs)
- [`AresDeathBeamTelegraph.cs`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Boss/AresDeathBeamTelegraph.cs)
- [Calamity license](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/LICENSE.md)

### Fargo's Souls

- [Pinned Fargo commit](https://github.com/Fargo-Team/FargosSoulsMod/commit/226fadeadbe3422785a7708ba2cdf53bd8548c00)
- [`build.txt`](https://github.com/Fargo-Team/FargosSoulsMod/blob/226fadeadbe3422785a7708ba2cdf53bd8548c00/build.txt)
- [`MutantBoss.cs`](https://github.com/Fargo-Team/FargosSoulsMod/blob/226fadeadbe3422785a7708ba2cdf53bd8548c00/Content/Bosses/MutantBoss/MutantBoss.cs)
- [`FargoSoulsPlayer.cs`](https://github.com/Fargo-Team/FargosSoulsMod/blob/226fadeadbe3422785a7708ba2cdf53bd8548c00/Core/ModPlayers/FargoSoulsPlayer.cs)
- [Fargo license](https://github.com/Fargo-Team/FargosSoulsMod/blob/226fadeadbe3422785a7708ba2cdf53bd8548c00/LICENSE)

### Infernum

- [Pinned Infernum commit](https://github.com/InfernumTeam/InfernumMode/commit/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e)
- [`build.txt`](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/build.txt)
- [`DoGPhase1HeadBehaviorOverride.cs`](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/Content/BehaviorOverrides/BossAIs/DoG/DoGPhase1HeadBehaviorOverride.cs)
- [`CustomNPCDataSyncing.cs`](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/Core/GlobalInstances/CustomNPCDataSyncing.cs)
- [`EnergyTelegraph.cs`](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/Content/BehaviorOverrides/BossAIs/CeaselessVoid/EnergyTelegraph.cs)
- [`InfernumPlayerEvents.cs`](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/Core/GlobalInstances/Players/InfernumPlayerEvents.cs)
- [Infernum license](https://github.com/InfernumTeam/InfernumMode/blob/5b02e0de15c4f77fa0ab89d7e3ac912a736fb76e/LICENSE.md)

### Starlight River

- [Pinned Starlight River commit](https://github.com/ProjectStarlight/StarlightRiver/commit/d367416dcadfe68a541a5821a0c4be86b42db219)
- [`build.txt`](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/build.txt)
- [`VitricBossAltar.cs`](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/Content/Tiles/Vitric/VitricBossAltar.cs)
- [`SpawnNPCPacket.cs`](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/Content/Packets/SpawnNPCPacket.cs)
- [`NPCs.VitricBoss.cs`](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/Content/Bosses/VitricBoss/NPCs.VitricBoss.cs)
- [`NPCs.SquidBoss.cs`](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/Content/Bosses/SquidBoss/NPCs.SquidBoss.cs)
- [Starlight River license](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/LICENSE.txt)
- [Starlight River README](https://github.com/ProjectStarlight/StarlightRiver/blob/d367416dcadfe68a541a5821a0c4be86b42db219/README.md)

### Mod of Redemption

- [Pinned Redemption commit](https://github.com/Hallam9K/RedemptionAlpha/commit/5edb3c6475cc2528d168f01fa99fd363ab71cd63)
- [`build.txt`](https://github.com/Hallam9K/RedemptionAlpha/blob/5edb3c6475cc2528d168f01fa99fd363ab71cd63/build.txt)
- [`ArenaSystem.cs`](https://github.com/Hallam9K/RedemptionAlpha/blob/5edb3c6475cc2528d168f01fa99fd363ab71cd63/Globals/Areas/ArenaSystem.cs)
- [`PZ.cs`](https://github.com/Hallam9K/RedemptionAlpha/blob/5edb3c6475cc2528d168f01fa99fd363ab71cd63/NPCs/Bosses/PatientZero/PZ.cs)
- [`PZ_Tele.cs`](https://github.com/Hallam9K/RedemptionAlpha/blob/5edb3c6475cc2528d168f01fa99fd363ab71cd63/NPCs/Bosses/PatientZero/PZ_Tele.cs)
- [Redemption README](https://github.com/Hallam9K/RedemptionAlpha/blob/5edb3c6475cc2528d168f01fa99fd363ab71cd63/README.md)

## Risk register and next evidence

| Risk | Current confidence | Required evidence/mitigation |
|---|---|---|
| Dedicated Server does not observe every lethal path before client death UI/packet | Medium-low until runtime spike | Instrument all lethal paths and define prediction/candidate fallback; do not claim completion from compilation alone. |
| Non-short-circuit `PreKill` aggregation lets Calamity and Convergence both mutate the same lethal event | **Blocking** | Keep the production hook disconnected; run the full compatibility matrix; record Raid-first or Calamity-first precedence and a supported normalization seam in an ADR. |
| Returning `false` from `PreKill` leaves inconsistent life, immunity, grapples, mounts, buffs, or death reason | Medium | One invariant-restoration method, repeated-hit tests, assertions, and explicit projection reset on cleanup. |
| Same-tick lethal hits make wipe order-dependent | High if handled inline | Queue transitions; batch apply; evaluate wipe once per authority tick. |
| Revive request spoof/range desync/concurrent channel | Medium | Resolve sender from transport; server distance/LOS; lease revision; bounded rate; atomic commit. |
| Player slot reuse lets late packet control a new participant | High without generation | `FightId` + participant generation + current connection validation. |
| Barrier rubber-banding under high ping | Medium | Inset/tolerance, client prediction, safe last-valid point, velocity resolution, latency matrix tests. |
| Intruder sentinel causes grief, aggro, or projectile load | Medium | Keep sentinel optional/presentational; geometry correction is final; add grace and rate limiting. |
| Part/type scans delete another mod instance or stale future fight | High if normal cleanup scans by type | Owner registry/resource token; orphan sweep constrained by this mod and expired FightId. |
| DPS deadline differs between hit processing and world tick | Medium | Specify inclusive ordering and write boundary-tick tests for items/projectiles/minions. |
| Variable-length packet causes allocation/CPU abuse | High if unbounded | Fixed field order plus per-type collection and encoded-string maxima, validated before allocation; never infer a Mod payload boundary from `BaseStream.Length`; add rejection/rate policy. |
| Source revision does not match current Workshop behavior | Medium | Revalidate fixed commits/API on dependency bump; do not infer binary internals. |
| “No cooperative revive found” is mistaken as proof of originality | High communication risk | Preserve scoped wording; no patent/first claim. |
| Third-party source contaminates project rights/provenance | Low if policy followed | No copy/paste, no assets, independent specs/tests, license audit before release. |

The immediate implementation gate is therefore not “builds successfully”; it is a recorded Dedicated Server death/downing trace, a decided and normalized Calamity precedence policy, plus deterministic two-player simultaneous-Downed and interrupted-revive tests.
