# Research Sources

Last reviewed: **2026-08-23**

一次資料を優先する。sourceの存在はAPI安定性や再利用許可を意味しない。

## tModLoader

- [tModLoader v2026.06.3.6](https://github.com/tModLoader/tModLoader/releases/tag/v2026.06.3.6) — 固定対象の1.4.4 stable release。
- [tModLoader fixed source commit](https://github.com/tModLoader/tModLoader/commit/29bf9785f5f4de8cd305be002c4cc48aa1177b20) — Calamity向けmitigationを含むsource基準点。
- [1.4.5 development FAQ](https://github.com/tModLoader/tModLoader/issues/5070) — 1.4.4 maintenance方針。
- [Stable API documentation](https://docs.tmodloader.net/docs/stable/) — v2026.06 API reference。
- [ModSystem API](https://docs.tmodloader.net/docs/stable/class_mod_system.html) — world data hooks、server/client call sites。
- [ModTileEntity API](https://docs.tmodloader.net/docs/stable/class_mod_tile_entity.html) — Core entityの基礎。
- [MusicLoader API](https://docs.tmodloader.net/docs/stable/class_music_loader.html) — music registration。
- [SoundStyle API](https://docs.tmodloader.net/docs/stable/struct_sound_style.html) — SFX properties。
- [ExampleMod build.txt](https://github.com/tModLoader/tModLoader/blob/stable/ExampleMod/build.txt) — stable template。
- [ExampleMod project](https://github.com/tModLoader/tModLoader/blob/stable/ExampleMod/ExampleMod.csproj) — project import pattern。
- [ExampleMod networking](https://github.com/tModLoader/tModLoader/blob/stable/ExampleMod/ExampleMod.Networking.cs) — `HandlePacket` pattern。
- [ExampleMod BasicTileEntity](https://github.com/tModLoader/tModLoader/blob/stable/ExampleMod/Content/TileEntities/BasicTileEntity.cs) — server mutationとTE sync。
- [Stable tMLMod.targets](https://github.com/tModLoader/tModLoader/blob/stable/patches/tModLoader/Terraria/release_extras/tMLMod.targets) — .NET 8、C# 12、build command。
- [Stable MusicLoader source](https://github.com/tModLoader/tModLoader/blob/stable/patches/tModLoader/Terraria/ModLoader/MusicLoader.cs) — `.mp3/.ogg/.wav` support。
- [OGG loop tag implementation](https://github.com/tModLoader/tModLoader/blob/stable/patches/tModLoader/Terraria/Audio/OGGAudioTrack.cs.patch) — `LOOPSTART`/`LOOPEND`。

## Calamity

- [Calamity public mirror](https://github.com/CalamityTeam/CalamityModPublic) — latest public release mirror。
- [Calamity 2.2.2 source commit](https://github.com/CalamityTeam/CalamityModPublic/commit/1a8cebd27ec5615316b78f71973446b5528d2b78) — 公開source基準点。
- [Calamity build.txt at 2.2.2](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/build.txt) — versionとMusic dependency。
- [Calamity ModCalls at 2.2.2](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/ModSupport/ModCalls.cs) — public integration surfaceの実装。
- [Calamity license at 2.2.2](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/LICENSE.md) — source/reference/redistribution conditions。
- [Calamity official wiki](https://calamitymod.wiki.gg/) — player-facing progression reference。

## OpenAI / Codex asset capability

- [Image generation in Codex](https://learn.chatgpt.com/docs/image-generation) — concept art、UI asset、background、sprite sheet、placeholderの生成・編集。
- [OpenAI image generation API](https://developers.openai.com/api/docs/guides/image-generation) — image generation/editing capabilities。
- [OpenAI audio and speech](https://developers.openai.com/api/docs/guides/audio) — available audio APIs are speech-oriented; this project does not assume a release-ready music generator。

## Music rights

- [文化庁: ここが知りたい著作権](https://www.bunka.go.jp/seisaku/chosakuken/taisetsu/point/index.html) — 保護期間満了曲の利用と録音物の別権利。
- [文化庁: 著作隣接権](https://www.bunka.go.jp/seisaku/chosakuken/seidokaisetsu/gaiyo/chosaku_rinsetsuken.html) — 実演・recordingの権利。
- [U.S. Copyright Office Circular 56A](https://www.copyright.gov/circs/circ56a.pdf) — compositionとsound recordingが別作品であること。
- [Library of Congress: Beethoven Symphony No. 9 autograph score](https://www.loc.gov/item/2021668114/) — 原典資料。

## Revalidation rule

実装開始日、Workshop公開前、tModLoader/Calamity更新時に再確認する。リンク切れやbranch移動があっても、当時の判断とversionはcommit historyへ残す。
