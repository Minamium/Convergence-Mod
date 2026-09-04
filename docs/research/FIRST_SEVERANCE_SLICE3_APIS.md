---
doc_id: research.first-severance-slice3-apis
document_type: research
status: historical
owners:
  - research
  - gameplay
last_reviewed: 2026-09-05
source_of_truth_for: []
aliases:
  - First Severance Slice 3 API research
  - Foundation Core API evidence
related_code:
  - Common/Compatibility/Calamity/CalamityCompatibilitySystem.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceCoreResolver.cs
  - Content/Encounters/FirstSeverance/FoundationCore
related_docs:
  - research.sources
  - compatibility.version-matrix
  - project.arena-infrastructure
  - encounter.first-severance.plan
---

# First Severance Slice 3 API Evidence

## Recovery packet and marker hotfix evidence (2026-09-05)

- Question/scope: `Mod.HandlePacket`, `ModNet.HandleModPacket`, shared reader length and underflow; local `FirstSeverancePacketCodec` and `DrawRing`. Target remains Terraria 1.4.4.9, tModLoader v2026.07.3.0 / `666f69962d3bdffde54fc14025f02634965b4e7c`, Calamity 2.2.4, .NET 8 / C# 12.
- Official source: [pinned ModNet.cs](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModNet.cs), `HandleModPacket`; [current v2026.07 Mod API](https://docs.tmodloader.net/docs/stable/class_mod.html), `HandlePacket` and `Logger`. Both read on 2026-09-05. The tModLoader organization owns the repository. The previously recorded MIT [license URL](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/LICENSE) and its raw counterpart were unavailable during this recheck; no license change is inferred and no source is copied.
- Observation: tModLoader passes the existing reader to the Mod, records its starting position, and separately compares consumed bytes with the actual message length. It does not turn `BaseStream.Length` into a ModPacket boundary. Local multiplayer logs corroborated a valid 35-byte message rejected after only the 31-byte envelope.
- Decision: independently read the declared four-byte nonce, retain nonzero checks and router handling of truncated reads, then validate request eligibility. Do not seek to the shared stream end or bypass tModLoader's underflow diagnostics. No protocol change, external code or new dependency.
- Focused reproduction: invoke both compiled decoders at offset 31 with an exact four-byte field and with 128 trailing shared-buffer bytes. The old binary rejects only the shared-buffer cases; the hotfix consumes four bytes in both. Zero and three-byte truncated nonces remain rejected. This checks the decoder, not full live revival.
- Visual evidence: the user's local screenshot shows 64-segment orange Spread rings extended into screen-length spokes. Source used the full MagicPixel texture with pixel-length scale. The independent fix selects a 1x1 source texel before scaling; geometry, damage and timing are unchanged. Live rendering remains to be rechecked by the user.
- Provenance: API behavior only; no Terraria/Calamity implementation, texture or audio copied. No third-party combat implementation was consulted for this fix.

Accessed: **2026-09-05**

This record fixes the external API evidence used by the first Foundation Core,
prospective-Arena resolver, and Calamity progression boundary. It records API
shape and observed behavior only. Convergence code was implemented independently.

## Foundation Core Tile Entity placement and synchronization

- Question: Which tModLoader hooks establish and remove a multiplayer Tile Entity for a multitile?
- Search scope and terms: confirmed tModLoader source tree and runtime XML; `Generic_HookPostPlaceMyPlayer`, `HookPostPlaceMyPlayer`, `KillMultiTile`, `TileEntity.TryGet`, `TileEntitySharing`.
- Project and official repository: tModLoader, [`tModLoader/tModLoader`](https://github.com/tModLoader/tModLoader).
- Maintainer/authority evidence: repository and release are maintained by the tModLoader organization; the installed runtime reports its source commit.
- License and license URL: tModLoader MIT license at the [confirmed commit](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/LICENSE).
- Source access status: verified.
- Target tag/commit/branch: stable `v2026.07.3.0`, commit `666f69962d3bdffde54fc14025f02634965b4e7c`.
- Declared Terraria/tModLoader/Calamity target and evidence: Terraria `1.4.4.9`, tModLoader `v2026.07.3.0`; runtime identity is preserved in the [Windows baseline](../evidence/2026-09-05-windows-baseline.json).
- Exact file or documentation URL: pinned [`BasicTileEntity.cs`](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/ExampleMod/Content/TileEntities/BasicTileEntity.cs) and the current [`ModTileEntity` API](https://docs.tmodloader.net/docs/stable/class_mod_tile_entity.html).
- Type/member/line anchor: `BasicTileEntityTile.SetStaticDefaults`, `HookPostPlaceMyPlayer`, `KillMultiTile`; `BasicTileEntity.IsTileValidForEntity`, `OnNetPlace`.
- Confirmed observation: the official example installs `Generic_HookPostPlaceMyPlayer` through `TileObjectData`, removes the entity from `KillMultiTile`, and treats authority placement/synchronization separately from the local placement hook. The confirmed runtime XML documents that generic `TileEntity.TryGet<T>` accepts any coordinate belonging to the multitile when the TE occupies its top-left.
- Inference: a 2x2 Foundation Core can resolve from any of its four coordinates while keeping the top-left server TE ID as its stable World identity.
- Compatibility with pinned tModLoader/Calamity: exact for the pinned tModLoader commit; independent of Calamity.
- Convergence decision: adapt the documented lifecycle pattern while keeping encounter ownership outside the TE.
- Copying boundary: API names and behavior only; no ExampleMod code or assets copied.
- Required tests: build against the pinned targets; Build + Reload; SP and Dedicated Server place/break/reload; verify one TE, one removal observation, and correct Tile Entity sharing.

## Active-only Core protection and prospective World scan

- Question: Which supported hooks and data sets can implement normal-player protection and a read-only prospective-Arena scan?
- Search scope and terms: confirmed runtime XML and official API reference; `ModTile.CanKillTile`, `CanExplode`, `CanReplace`, `Slope`, `HitWire`, `TileObjectData.IsTopLeft`, `WorldGen.SolidTileAllowBottomSlope`, Tile wire/liquid/actuator properties, `TileID.Sets.IsAContainer`, `TileID.Sets.Platforms`.
- Project and official repository: tModLoader, [`tModLoader/tModLoader`](https://github.com/tModLoader/tModLoader).
- Maintainer/authority evidence: official stable API generated for tModLoader plus the XML shipped beside the confirmed runtime assembly.
- License and license URL: tModLoader MIT license at the [confirmed commit](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/LICENSE); no Terraria implementation was copied.
- Source access status: verified.
- Target tag/commit/branch: stable `v2026.07.3.0`, commit `666f69962d3bdffde54fc14025f02634965b4e7c`.
- Declared Terraria/tModLoader/Calamity target and evidence: Terraria `1.4.4.9`, tModLoader `v2026.07.3.0`; Calamity does not own these hooks.
- Exact file or documentation URL: [`ModTile` API](https://docs.tmodloader.net/docs/stable/class_mod_tile.html), [`TileObjectData` API](https://docs.tmodloader.net/docs/stable/class_tile_object_data.html), and pinned [`ModTile.cs`](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModTile.cs).
- Type/member/line anchor: `ModTile.CanKillTile`, `CanExplode`, `CanReplace`, `Slope`, `HitWire`; `TileObjectData.IsTopLeft`; the named `TileID.Sets` members.
- Confirmed observation: these hooks can reject ordinary mining, explosion, replacement, slope, and wire actions; water/lava survival is declared in `TileObjectData`. The named Tile/World data exposes the counts needed for a non-mutating scan. The API does not promise interception of every direct third-party World mutation.
- Inference: supported hooks are a first-line Active projection, while periodic exact-Core validation and terminal cleanup remain the final correctness boundary.
- Compatibility with pinned tModLoader/Calamity: exact for the confirmed runtime surface; independent of Calamity internals.
- Convergence decision: adopt supported hooks, scan before mutation, and avoid destructive restoration.
- Copying boundary: API names and behavior only; independently implemented.
- Required tests: normal pickaxe/explosion/wire/liquid cases in SP and Dedicated Server; injected unexpected Tile/TE loss; containers, protected tiles, foreign TEs, warnings, World edge, and scan timing.

## Calamity progression and Boss Rush public calls

- Question: Which Calamity public calls expose Exo Mechs, Supreme Calamitas, and Boss Rush state without taking an internal type dependency?
- Search scope and terms: `ModSupport/ModCalls.cs`; `GetBossDowned`, `GetDifficultyActive`, `exo mechs`, `supreme calamitas`, `bossrush`.
- Project and official repository: Calamity Mod public mirror, [`CalamityTeam/CalamityModPublic`](https://github.com/CalamityTeam/CalamityModPublic).
- Maintainer/authority evidence: public mirror under the CalamityTeam organization.
- License and license URL: custom proprietary/all-rights-reserved [Calamity terms](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/LICENSE.md).
- Source access status: verified for the public `2.2.2` reference source; no public source identity matching the installed `2.2.4` binary is claimed.
- Target tag/commit/branch: commit `1a8cebd27ec5615316b78f71973446b5528d2b78`, declared version `2.2.2`.
- Declared Terraria/tModLoader/Calamity target and evidence: implementation target Calamity `2.2.4`; source evidence is reference-only `2.2.2` as recorded in the [Version Matrix](../VERSION_MATRIX.md).
- Exact file or documentation URL: pinned [`ModSupport/ModCalls.cs`](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/ModSupport/ModCalls.cs).
- Type/member/line anchor: public-call dispatcher cases `GetBossDowned` and `GetDifficultyActive`; aliases for `exo mechs`, `supreme calamitas`, and `bossrush`.
- Confirmed observation: the dispatcher returns booleans for those public-call names/aliases at the reference commit.
- Inference: the installed `2.2.4` build is expected, but not proven from source, to preserve this public surface.
- Compatibility with pinned tModLoader/Calamity: source-exact for `2.2.2`; runtime verification is mandatory for `2.2.4`.
- Convergence decision: prototype through `Mod.Call`, validate return types, catch failures, and keep activation fail-closed.
- Copying boundary: behavior and public call names only; zero Calamity code or assets copied.
- Required tests: invoke all three calls against installed Calamity `2.2.4` in SP and Dedicated Server at representative progression states; malformed/changed result must reject activation without crashing.

## Independent policy decisions

- The requester range is provisionally 12 tiles and the participation radius 80 tiles.
- Five or more selectable candidates return `roster_selection_required`; no silent truncation is permitted.
- Interior solids, liquids, wire/actuator, platform/rope, and spawn/housing overlap are warnings in Slice 3A. Containers, another Core, foreign TEs, protected tiles, foundation gaps, World conflicts, and incomplete scans are fatal.
- The Ready window is provisionally 60 seconds, supports unready, and never opens the combat gate in Slice 3A.
- Only the Foundation Core protection projection is synchronized by the TE. Encounter sequence, Fight ID, roster, and lifecycle remain owned by the encounter runtime/transport planned for Slice 3B.

## Development combat API observations (2026-09-05)

The `0.1.1` experiment was independently implemented against the same confirmed runtime: Terraria 1.4.4.9, tModLoader v2026.07.3.0 / `666f69962d3bdffde54fc14025f02634965b4e7c`, .NET 8/C# 12, Calamity 2.2.4. Evidence includes the `tModLoader.xml` shipped with that assembly and the official v2026.07 API pages accessed below. GitHub raw-file fetches for the pinned `ModNPC.cs` and `ModPlayer.cs` were unavailable in this tool session; no claim is based on their unread contents.

- [ModNPC API](https://docs.tmodloader.net/docs/stable/class_mod_n_p_c.html): `AI` runs on server/clients; `CanBeHitByItem` is local-client-side, projectile hit permission depends on owner, and `OnKill` supports authority death observation. Local XML also documents `NPC.NewNPC` as authority-only and `SyncNPC`/`ai[]` for replication. Decision: replicate a compact ownership token and damage-window flag; use observed death rather than treating any missing NPC as Victory. This is cooperative-client experimentation, not proven hostile-client protection.
- [ModPlayer API](https://docs.tmodloader.net/docs/stable/class_mod_player.html): `SetControls` is local; `CanUseItem` supports use suppression, `PreUpdateMovement` controls velocity, and `ImmuneTo` can suppress damage. Decision: apply authority projections to both the server player and the owning client, retain a held kit during its lease, and avoid `PreKill` entirely. Cross-Mod lethal coexistence remains unmeasured.
- [ModSceneEffect API](https://docs.tmodloader.net/docs/stable/class_mod_scene_effect.html): `Music` accepts a Terraria Music ID and `IsSceneEffectActive` selects presentation scope. Decision: play `MusicID.Boss3` only for active participants using client presentation; copy no audio asset.

License boundary: tModLoader API names/behavior only (MIT project, license link above); no external implementation source or Terraria/Calamity assets copied. These are observed API facts plus design inferences. Compilation confirms API shape; the user-run two-player loop and held-use revive still provide the required gameplay evidence. Current results belong in [Status](../STATUS.md).
