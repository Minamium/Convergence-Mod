# Calamity Multiplayer Raid Addon

Calamity Mod終盤を対象とする、2～4人向けのmultiplayer-first Raid Addonです。名称は仮称です。

現在はMilestone 0（調査・設計・互換性固定）の段階です。ゲームコード、完成素材、配布可能なビルドはまだありません。

## 現在の対象環境

- Terraria 1.4.4.9
- tModLoader 1.4.4 stable `v2026.06.3.6`
- Calamity Mod `2.2.2`
- .NET 8 / C# 12（tModLoader側のビルド既定値）

上記は2026-08-23時点の調査に基づく初期固定候補です。最小Modの実機ビルド、Calamity同時ロード、Dedicated Server起動を通過した時点で確定します。

## Documentation

- [Project brief](docs/PROJECT_BRIEF.md)
- [Version matrix](docs/VERSION_MATRIX.md)
- [Architecture decisions and open questions](docs/DECISIONS.md)
- [Network architecture](docs/NETWORK_ARCHITECTURE.md)
- [Arena infrastructure](docs/ARENA_INFRASTRUCTURE.md)
- [Encounter specification](docs/ENCOUNTER_SPEC.md)
- [Asset and audio pipeline](docs/ASSET_PIPELINE.md)
- [Art direction](docs/ART_DIRECTION.md)
- [Audio cue sheet](docs/AUDIO_CUE_SHEET.md)
- [IP provenance policy](docs/IP_PROVENANCE.md)
- [Test plan](docs/TEST_PLAN.md)
- [Milestones](docs/MILESTONES.md)
- [Research sources](docs/SOURCES.md)

## Non-negotiable rules

- 戦闘結果に関係する状態はサーバー権威型とする。
- 最初からDedicated Serverと2～4人プレイを対象にする。
- Subworldや大量の一時TileでArenaを構築しない。
- 一時状態と一時EntityはFight IDで所有し、Cleanupを冪等にする。
- Calamityのコード・画像・音声を再配布しない。
- 既存作品の名称、ロゴ、造形、台詞、画像、音声、楽曲録音を流用しない。
- 各実装単位でビルドを通し、壊れた状態を次の作業へ持ち越さない。

## Next gate

次の作業は、内部Mod名とライセンスを決定した後の最小Mod skeleton作成です。Milestone 1のArena実装に入る前に、対象バージョンでクライアント・Dedicated Server双方のロードを確認します。
