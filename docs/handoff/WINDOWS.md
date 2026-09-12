---
doc_id: handoff.windows
document_type: handoff
status: accepted
owners:
  - project
last_reviewed: 2026-09-12
source_of_truth_for:
  - handoff.windows.resume
aliases:
  - Windows handoff
  - desktop migration
related_code:
  - Content/Encounters/FirstSeverance
  - Common/Raids/Revive
  - Tests/Convergence.DomainTests
related_docs:
  - project.status
  - development.windows
  - encounter.first-severance.plan
---

# Windows Development Handoff — チャット不要の再開入口

## 最初の数分

1. リポジトリの `AGENTS.md` を読み、コード/挙動作業なら [Current build](../STATUS.md#current-build)・[Verification state](../STATUS.md#verification-state)・[Next change](../STATUS.md#next-change) の必要箇所を確認する。文言修正は対象箇所から始める。
2. `git status --short --branch`、`git remote -v`、`git log -5 --oneline` で現在地と既存差分を確認する。古い引き継ぎのHEADをcheckoutしない。
3. 今回の依頼に対応する正本を [作業別参照表](../README.md#read-by-task) から選ぶ。仕様理解には [Raid概要](../encounters/first-severance/README.md) → 必要な節だけでよい。
4. ビルドが必要なら [Windows runbook](../runbooks/WINDOWS_DEVELOPMENT.md#diagnose-or-build) の記録付き入口を使う。同じ入力で通過した検証は繰り返さない。

現在のRaidは開始準備からFinal生存・勝敗・ドロップまで実装済みの開発版。ユーザーは戦闘本体をいったん区切りとして受け入れている。次の変更は新しい依頼の範囲で行い、昔の「未実装」「起動禁止」「次はrename」から再開しない。

## 正本・ローカル環境・未保存作業

正本はこの `Convergence` checkout、通常ブランチは `main`、origin は `https://github.com/Minamium/Convergence-Mod.git`。既存Windowsでは tModLoader の `ModSources/Convergence` がこの正本へのjunctionであり、別ソースではない。絶対パス・保存先・旧コピー一覧は ignored `.local/consolidation.json`、ビルドパスは `Convergence.local.props` が持つ。新しいマシンでは [runbook](../runbooks/WINDOWS_DEVELOPMENT.md#one-canonical-source-and-local-setup) に従って一度だけclone/configureする。

未コミット変更はその時点の `git status` / `git diff` で特定し、今回の担当範囲と分ける。ビルドに含む差分は各manifestが識別する。過去の引き継ぎにあるファイル数を現在の状態とみなさない。Gitにない内容は新しいcloneへ自動で引き継がれない。

旧 `ConvergenceEdit*` / `ConvergenceBase*`、退避リポジトリ、外部素材原本は保持済み。日常作業で走査・削除・上書き統合しない。並行実装の共通手順は [Contributing](../../CONTRIBUTING.md#shared-development) に従い、統合先・ビルド対象を確認する。通常のコミット・pushは許可済みだが、force push/hard resetや第三者作業の破棄はしない。

## 変更先の地図

| 変更したいこと | 正本・コード入口 |
|---|---|
| 起動、全員Ready、設置台、領域 | [Arena infrastructure](../ARENA_INFRASTRUCTURE.md)、`FirstSeveranceCoreResolver` / `FirstSeverancePreparationRuntime` |
| フェーズ、HP、頭割り/散開、攻撃 | [Encounter spec](../encounters/first-severance/ENCOUNTER_SPEC.md)、`FirstSeveranceBossPhases` / `Choreography` / `PartyScaling` / 各geometry |
| Down、蘇生 | [Revive spec](../encounters/first-severance/REVIVE_SPEC.md)、`Common/Raids/Revive` と feature `Revive/` |
| 描画・緩急・黒領域・演出 | [Visual spec](../encounters/first-severance/VISUAL_SPEC.md)、`Client/Encounters/FirstSeverance`。特に107% UI倍率の座標契約を維持 |
| BGM/SFX、音が聞こえない | [Audio sheet](../AUDIO_CUE_SHEET.md)、`FirstSeveranceFeedback` / `AudioCueClock` / `PreparationSilence` |
| ドロップ武器 | [Weapons](../encounters/first-severance/WEAPONS.md)、feature `Rewards/`。爪の内部IDは `NullRefrain` |
| 幽鬼武者の攻撃・描画・召喚 | [Ghost Samurai spec](../encounters/ghost-samurai/ENCOUNTER_SPEC.md)、`Content/Encounters/GhostSamurai` / `Client/Encounters/GhostSamurai` |
| 通信/権限/cleanup/新機能 | [Architecture](../ARCHITECTURE.md)、[Network](../NETWORK_ARCHITECTURE.md)。共通routerへRaid専用switchを増やさない |

数値はリンク先コードの定数が正本。HPやバージョンをこの表へ再掲しない。設計判断の理由・変更履歴は各現行仕様の履歴リンクから追える。

## このWindows環境での作業分担

- この環境では通常 **GUI操作＝依頼者、マルチ検証＝友人と実施**。別の貢献者はそのタスクで確認担当を決める。一人検証は明示された回だけ [solo手順](../runbooks/SINGLE_OPERATOR_TESTING.md) を使う。二窓・補助無敵は通常プレイへ持ち込まない。
- ゲーム/サーバ起動や停止、GUI自走はその回の依頼範囲だけ。ビルド依頼からHost & Play参加や公開を推測しない。
- 記録付きnative buildは実際のMods保存先へpackageする。成功後はReload/restartでよく、変更のないBuild + Reloadは不要。ゲームが旧版をロード中なら再読込が必要。
- ユーザーのワールド・キャラクター・有効Modリスト・音量設定は保持する。昔依頼されたMod一覧を現在のセットへ上書きしない。依存最低構成と普段のテスト用Mod packを混同しない。
- 見た目の核は受け入れ済み。継続方針は「速い出現→短い減速/溜め→急加速」、原画の質感を保った連続描画、過剰なラベルを避けて判定を明瞭にすること。音源の選択は [現行Audio sheet](../AUDIO_CUE_SHEET.md) を優先する。

## ログと証拠の引き継ぎ

ゲームインストールの `tModLoader-Logs` にある最新client/serverログを、起動時刻・Modバージョン・protocol・参加人数・Fight IDで区切る。Host & Playのserver側がゲーム判定の根拠、client側は描画/音/受信状況の根拠。別セッションや古いローテートログを混ぜない。

戦闘では `CombatStarted`、Stack/Spread判定、Down/revive/terminal、`DamageWindowStarted` / `DamageProgress` / `DamageWindowEnded`、`CombatDpsSummary` を確認する。実際の詳細定義は [authority diagnostics](../encounters/first-severance/ENCOUNTER_SPEC.md#authority-diagnostics)。CoreとPylon、open-window DPSと戦闘全時間、人数・版の違いを分ける。頭割りはserverの固定中心・観測位置/人数とclient snapshotを照合し、見た目だけで同期失敗と断定しない。

聞こえない音は `AudioCue` の採用/期限と `AudioVoice` の再生・gain・focusを照合する。ログの「再生中」は聴感の証明ではない。生ログ・セーブ・個人情報はコミットせず、sanitized要約を `docs/evidence/` に置く。

使用ソース/commit/dirty差分/成果物は `.local/builds/<timestamp>/record.json` のmanifest/hashで識別する。新しい文書コミットができても、以前のpackageをその新HEADでビルドしたことにはならない。

外部原本・レシピ・MP3試聴は `.local` inventoryと [Attribution](../../Assets/ATTRIBUTION.md) を参照。GitHubのruntime素材だけでは原本/外部backupまで復元できない。旧MP3試聴と現行EigHt BGMを混同しない。公開済み/承認待ちという過去の報告だけで現在のWorkshop状態を断定せず、公開作業時に確認する。

## 再発させない境界

Server/SP authority、Terraria非依存domain、単一Mod assembly、Common/Content/Client分離、Calamity隔離、Fight単位の所有と冪等cleanupを維持する。普通のTerraria致死のDown化・堅牢なrejoin・一般外部者制御・独立Mod化は未完成であり、現在のRaid独自ダメージ実験と区別する。

読む資料と検証は [verification matrix](../../.agents/skills/develop-convergence-raids/references/verification-matrix.md) で変更種別ごとに選ぶ。既読/通過済みの再実行は入力変更・具体的な疑問・失敗がある時だけ。全文読書、新ADR/Skill、全体release gateを軽微な修正へ自動追加しない。

## Completed Ghost Samurai integration

2026-09-12、`67e36c1` で旧featureの `d4ad7d2` とmain側の再召喚修正 `9464e9c` を統合済み。`feature/ghost-samurai-phases-1-2` はこの統合の履歴であり、次の作業は最新 `origin/main` から新しい目的別branch/worktreeを作る。以後の通常作業でこの完了済みcheckpointを毎回確認し直す必要はない。現在の実装・packageは [Status](../STATUS.md) が持つ。

## 履歴

直近までの変更・観測・旧フォローアップは [0.2.46までのcheckpoint](../history/2026-09-11-status-through-0246.md)、初期Windows移行/rename/統合作業は [pre-consolidation記録](../history/2026-09-07-pre-consolidation.md) と [実装計画](../encounters/first-severance/IMPLEMENTATION_PLAN.md) に保存。次の作業指示ではない。
