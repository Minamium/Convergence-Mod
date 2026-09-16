---
doc_id: research.sources
document_type: research
status: historical
owners:
  - research
last_reviewed: 2026-09-13
source_of_truth_for: []
aliases:
  - research sources
  - source ledger
related_code: []
related_docs:
  - compatibility.version-matrix
  - research.codex-skills-survey
  - research.multiplayer-raid-prior-art
  - research.first-severance-slice3-apis
  - research.instant-revival-core-apis
  - research.wotg-raid-benchmark
  - development.single-operator-testing
---

# Research Sources

- [Crimson Foundry authority/audio API evidence](adr/0026-crimson-score-and-native-projectiles.md) — pinned tML OGG tags/update hooks and FNA PCM-loop signature, checked2026-09-15. [Music provenance](../Assets/ATTRIBUTION.md#crimson-foundry--2026-09-15) owns kuku's supplied work, author description, local edit and distribution limits.
- [Crimson mechanical/reactor visual reference](research/WOTG_RAID_BENCHMARK.md#f17--crimson-articulated-machine-and-energy-release-2026-09-15) — selected pinned WoTM body/laser rendering and directly inspected owner-recording frames; independent art/code, no third-party media copied.

Last reviewed: **2026-09-06**

一次資料を優先する。sourceの存在はAPI安定性や再利用許可を意味しない。

## tModLoader

- [Native Hurt and implementation-review evidence](research/2026-09-14-implementation-review.md#pinned-api-evidence) — current pinned HurtModifiers, native HurtInfo transport, death-hook aggregation and safe player-instance teardown; verified2026-09-14.

- [tModLoader v2026.07.3.0](https://github.com/tModLoader/tModLoader/releases/tag/v2026.07.3.0) — Windows実機で確認した現行固定対象の1.4.4 stable release。
- [Confirmed tModLoader source commit](https://github.com/tModLoader/tModLoader/commit/666f69962d3bdffde54fc14025f02634965b4e7c) — runtimeが報告した現行source基準点。
- [Pinned ExampleMod BasicTileEntity](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/ExampleMod/Content/TileEntities/BasicTileEntity.cs) — multitile TE配置・削除・同期patternの現行根拠。
- [tModLoader v2026.06.3.6](https://github.com/tModLoader/tModLoader/releases/tag/v2026.06.3.6) / [former source commit](https://github.com/tModLoader/tModLoader/commit/29bf9785f5f4de8cd305be002c4cc48aa1177b20) — bootstrap時点の履歴基準。現在の固定対象ではない。
- [1.4.5 development FAQ](https://github.com/tModLoader/tModLoader/issues/5070) — 1.4.4 maintenance方針。
- [Stable API documentation](https://docs.tmodloader.net/docs/stable/) — v2026.06 API reference。
- [ModSystem API](https://docs.tmodloader.net/docs/stable/class_mod_system.html) — world data hooks、server/client call sites。
- [Pinned ModSystem source](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModSystem.cs) — 現行固定source。`docs/stable`はmoving supplementary referenceとして扱う。
- [Pinned PlayerLoader death-hook aggregation](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/ModLoader/PlayerLoader.cs) — `PreKill`が全`ModPlayer` hookを非短絡で合成する固定source。
- [Pinned Player.KillMe integration patch](https://github.com/tModLoader/tModLoader/blob/29bf9785f5f4de8cd305be002c4cc48aa1177b20/patches/tModLoader/Terraria/Player.cs.patch) — `PreKill`がfalseの場合にvanilla death pathへ入らない呼び出し位置。
- [ModTileEntity API](https://docs.tmodloader.net/docs/stable/class_mod_tile_entity.html) — Core entityの基礎。
- [MusicLoader API](https://docs.tmodloader.net/docs/stable/class_music_loader.html) — music registration。
- [SoundStyle API](https://docs.tmodloader.net/docs/stable/struct_sound_style.html) — SFX properties。
- [ExampleMod build.txt](https://github.com/tModLoader/tModLoader/blob/stable/ExampleMod/build.txt) — stable template。
- [ExampleMod project](https://github.com/tModLoader/tModLoader/blob/stable/ExampleMod/ExampleMod.csproj) — project import pattern。
- [ExampleMod networking](https://github.com/tModLoader/tModLoader/blob/stable/ExampleMod/ExampleMod.Networking.cs) — `HandlePacket` pattern。
- [ExampleMod BasicTileEntity (moving stable)](https://github.com/tModLoader/tModLoader/blob/stable/ExampleMod/Content/TileEntities/BasicTileEntity.cs) — 補助参照。互換判断には上記pinned linkを使う。
- [Stable tMLMod.targets](https://github.com/tModLoader/tModLoader/blob/stable/patches/tModLoader/Terraria/release_extras/tMLMod.targets) — .NET 8、C# 12、build command。
- [Pinned Mod template project](https://github.com/tModLoader/tModLoader/blob/v2026.06.3.6/patches/tModLoader/Terraria/ModLoader/Templates/%7B%7BModName%7D%7D.csproj) — minimal project and targets import。
- [Stable MusicLoader source](https://github.com/tModLoader/tModLoader/blob/stable/patches/tModLoader/Terraria/ModLoader/MusicLoader.cs) — `.mp3/.ogg/.wav` support。
- [OGG loop tag implementation](https://github.com/tModLoader/tModLoader/blob/stable/patches/tModLoader/Terraria/Audio/OGGAudioTrack.cs.patch) — `LOOPSTART`/`LOOPEND`。

## Calamity

The2026-09-09 [long-form weapon findings](encounters/first-severance/WEAPONS.md#prior-art-findings-and-engine-seams) pin Yharim's Crystal, Drataliornus and Midnight Sun UFO observations and the native mana/ammo/audio seams. Use that scoped evidence rather than another broad endgame survey.

The2026-09-15 [Chalice follow-up](research/2026-09-14-implementation-review.md#chalice-follow-up--2026-09-15) pins the accessory's native Hurt/bleed-buffer path and read-only diagnostics. The public mirror still declares2.2.2; distinguish it from installed2.2.4 behavior.

- [Calamity public mirror](https://github.com/CalamityTeam/CalamityModPublic) — latest public release mirror。
- [Calamity 2.2.2 source commit](https://github.com/CalamityTeam/CalamityModPublic/commit/1a8cebd27ec5615316b78f71973446b5528d2b78) — 公開source基準点。
- [Calamity build.txt at 2.2.2](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/build.txt) — versionとMusic dependency。
- [Calamity ModCalls at 2.2.2](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/ModSupport/ModCalls.cs) — public integration surfaceの実装。
- [Calamity license at 2.2.2](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/LICENSE.md) — source/reference/redistribution conditions。
- [Calamity official wiki](https://calamitymod.wiki.gg/) — player-facing progression reference。

## Wrath of the Gods benchmark — 2026-09-06

- [User reference: Avatar showcase at 00:40](https://www.youtube.com/watch?v=LIFVyjz0T3k&t=40s) and [all-Boss no-hit video at 14:02](https://www.youtube.com/watch?v=LoHvGScaZYU&t=842s) — YouTube public oEmbed/player metadata verified for title, author, dates and chapters. Video/audio were not directly observed; exact attack identification and quality evaluation remain unverified.
- [Author's Workshop listing](https://steamcommunity.com/sharedfiles/filedetails/?id=2995193002) — project/creator credits and current dependency listing; not a multiplayer runtime verification record.
- [Pinned public WotG build metadata](https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/build.txt) — declares 1.2.24, not proven identical to current Workshop. [Pinned description](https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/description.txt) declares multiplayer incompatibility for this reference version; do not generalize to today's binary.
- [Detailed evidence and exact source paths](research/WOTG_RAID_BENCHMARK.md) — multipart render composition, depth, state choreography, off-body portals, telegraph/collision separation, sound/sky, timed DR, multiplayer-specific branch and Solyn. Public repository license was not established; no implementation/assets copied.

## One-person multiplayer development — 2026-09-06

- [Official local multiplayer testing guide](https://github.com/tModLoader/tModLoader/wiki/Basic-Netcode#testing-multiplayer-locally) — two clients, Host & Play plus localhost, and background-focus caveat. Moving guidance, not a completed local experiment.
- [Official launch options](https://github.com/tModLoader/tModLoader/wiki/Command-Line) — paired with pinned `Program.TML.cs`, `SocialAPI.cs.patch`, server wrapper/config and `Logging.cs` in [single-operator testing](runbooks/SINGLE_OPERATOR_TESTING.md#6-一次資料と再現条件). Save separation and server `-nosteam` are distinguished from the DEBUG-only client switch. No new launcher or debug-assist implementation was added.

## OpenAI / Codex asset capability

- [Image generation in Codex](https://learn.chatgpt.com/docs/image-generation) — concept art、UI asset、background、sprite sheet、placeholderの生成・編集。
- [OpenAI image generation API](https://developers.openai.com/api/docs/guides/image-generation) — image generation/editing capabilities。
- [OpenAI audio and speech](https://developers.openai.com/api/docs/guides/audio) — available audio APIs are speech-oriented; this project does not assume a release-ready music generator。

## Repository operations

- [GitHub Actions secure use](https://docs.github.com/en/actions/reference/security/secure-use) — least privilege、untrusted pull request、action pinning。
- [GitHub Actions workflow syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) — workflow structureとpermissions。
- [CODEOWNERS](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners) — path ownership policy。
- [Issue Form syntax](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/syntax-for-issue-forms) — structured issue forms。
- [actions/checkout v7.0.1](https://github.com/actions/checkout/releases/tag/v7.0.1) — CIでfull SHA固定したcheckout action。

## Orchestral sample source — 0.2.5

- [VSCO 2 CE official distribution](https://versilian-studios.com/vsco-community/) — identifies the raw WAV library, links the author's repository and states CC0; accessed 2026-09-06.
- [Pinned CC0 license](https://raw.githubusercontent.com/sgossner/VSCO-2-CE/440300901dfe9275fd84e0b7763af1f8443ae62e/LICENSE) — exact sample revision used for the independently authored music render. Raw samples/manifest remain external; see [Attribution](../Assets/ATTRIBUTION.md).

## Music rights

- [文化庁: ここが知りたい著作権](https://www.bunka.go.jp/seisaku/chosakuken/taisetsu/point/index.html) — 保護期間満了曲の利用と録音物の別権利。
- [文化庁: 著作隣接権](https://www.bunka.go.jp/seisaku/chosakuken/seidokaisetsu/gaiyo/chosaku_rinsetsuken.html) — 実演・recordingの権利。
- [U.S. Copyright Office Circular 56A](https://www.copyright.gov/circs/circ56a.pdf) — compositionとsound recordingが別作品であること。
- [Library of Congress: Beethoven Symphony No. 9 autograph score](https://www.loc.gov/item/2021668114/) — 原典資料。

## Revalidation rule

Luminance Doll Raid material refresh, 2026-09-13: [visual API boundary](encounters/first-severance/VISUAL_SPEC.md#luminance-raid-presentation) owns the pinned 1.0.14 API/source versus installed-binary distinction. [F12](research/WOTG_RAID_BENCHMARK.md#f12--極太赤ビームエネルギー弾終幕の連動2026-09-13追補) records source-only red beam/orb/ending observations; [F14](research/WOTG_RAID_BENCHMARK.md#f14--recorded-beam-motion-2026-09-13) adds actual user-recording frames and Hades moving-trail versus Nameless sustained-jet analysis. [F15](research/WOTG_RAID_BENCHMARK.md#f15--nameless-portal-triplet-2026-09-13) closely inspects the selected Nameless V/H/V triplet, warning dip, dark/white jet contrast and contraction, retaining accepted lattice separately. Recorded Mod versions remain unconfirmed. Weapons/Oni are not migrated.

Drawing/Defeat/intro pass, 2026-09-06: the same pinned official `Player.KillMe`, `ModSystem.ModifyInterfaceLayers` and `Main` layer-draw path were inspected. [Focused API evidence](research/INSTANT_REVIVAL_CORE_APIS.md#022-narrow-lookup-normal-defeat-death-and-temporary-hud-suppression) records the normal death-hook cancellation boundary, frame-local UI decision, and remaining multiplayer observations.

Instant revival/Core pass, 2026-09-06: pinned tModLoader `ModBuff`, `GlobalTile`, `ModTile`, `ModBlockType` and `MessageID` contracts were inspected. Exact links, confirmed behavior, item-selection ordering inference and focused checks are recorded in [instant revival/Core evidence](research/INSTANT_REVIVAL_CORE_APIS.md). No engine implementation was copied or decompiled.

Giant visual/beam pass, 2026-09-06: [pinned ModSystem](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModSystem.cs), [pinned ModNPC](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModNPC.cs), [pinned MIT license](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/LICENSE) and the [WotG public README](https://github.com/TheFifthCircle/WrathOfTheGodsPublic). Exact scope, independent decisions and reference-license limits are in [giant visual evidence](research/FIRST_SEVERANCE_GIANT_VISUALS.md).

Recovery transport rechecked 2026-09-05: [ModNet.HandleModPacket at the confirmed commit](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModNet.cs) and [Mod.HandlePacket / Logger API](https://docs.tmodloader.net/docs/stable/class_mod.html). Shared-stream boundary observations and the local reproduction are recorded in [First Severance API evidence](research/FIRST_SEVERANCE_SLICE3_APIS.md).

Development combat references accessed 2026-09-05: [ModNPC](https://docs.tmodloader.net/docs/stable/class_mod_n_p_c.html), [ModPlayer](https://docs.tmodloader.net/docs/stable/class_mod_player.html), and [ModSceneEffect](https://docs.tmodloader.net/docs/stable/class_mod_scene_effect.html). These moving v2026.07 pages are paired with the installed pinned runtime XML; exact observations and limitations are in [First Severance API evidence](research/FIRST_SEVERANCE_SLICE3_APIS.md).

実装開始日、Workshop公開前、tModLoader/Calamity更新時に再確認する。リンク切れやbranch移動があっても、当時の判断とversionはcommit historyへ残す。

Continuous-emission/containment recheck, 2026-09-06: the pinned WotG Avatar rendering utilities, Nameless portal state/laser, Calamity ArenaWallSystem/SupremeCalamitas and tML ModPlayer/ModBlockType/GlobalTile contracts are linked and evaluated in [benchmark finding F11](research/WOTG_RAID_BENCHMARK.md). Reuse that fixed-source table rather than interpreting current branches as the tested runtime.

Weapon audio/ten-slot companion survey, 2026-09-12: the pinned official Terraria/tML styles and ExampleMod minion, Calamity Photoviscerator/SubsumingVortex/CosmicImmaterializer paths and exact applicability are in [weapon findings](encounters/first-severance/WEAPONS.md#weapon-sound-and-ten-slot-companion-references). This verifies source behavior, not subjective sound playback.

Oboro, 2026-09-16: [scoped API and Earth comparison](research/2026-09-16-oboro.md) records pinned tML hooks, public Calamity source/version mismatch, independent motion and the runtime comparison still needed.
