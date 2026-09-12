---
doc_id: project.brief
document_type: overview
status: accepted
owners:
  - project
last_reviewed: 2026-09-12
source_of_truth_for:
  - project.product_scope
aliases:
  - project brief
  - product direction
related_code:
  - build.txt
  - Common/Compatibility/Calamity
related_docs:
  - project.status
  - encounter.first-severance.overview
---

# Project Brief

## One-line concept

Calamity終盤に、位置取り・火力・蘇生を協力して処理するRaidと、予兆を読み切って挑む独立Bossを追加するContent Mod。

## Product position

Convergenceは、Calamity終盤の個人回避・火力最適化を土台に、MMORPG Raid型の協力ギミックをTerrariaの2D移動とserver authorityへ翻訳する。弾幕密度とHPだけを増やしたSuperbossにはしない。

協力Raidの想定party:

- Exo MechsおよびSupreme Calamitas撃破済み;
- Shadowspec級装備を利用可能;
- 2～4人の固定または準固定party;
- 5～12分程度の反復攻略を受け入れられる。

5～12分は将来のRaid全体のproduct goalであり、現行の戦闘時間を示す数値ではない。協力Raidの一人起動は開発支援として区別する。独立Bossは各featureの参加人数・戦闘設計に従う。

最初のRaidは基盤の実証対象であってAddon全体の上限ではない。長期的には独立Boss、追加Raid、World content、進行、Item、Utility、演出まで広げ、Calamity級の独自Content Modを目指す。

## Dependency strategy

- **Stage A — Calamity addon:** Calamityの進行、class連携、Shadowspec級balanceを隔離adapter越しに利用し、最初のRaidを完成させる。
- **Stage B — Portable Raid core:** project-ownedな進行/class/balance capabilityへ置換し、Encounter、Networking、Arena、Raid domainからCalamity型と名称を排除する。
- **Stage C — Standalone Content Mod:** 独自進行、素材、装備、recipe、World content、balanceを実装し、hard dependencyを外す。残す場合のCalamity対応はoptional adapterとして再決定する。

`modReferences`の削除だけでStandaloneにはならない。各stageはbuild/load/multiplayer/release matrixを持つ。詳細は [ADR-0006](adr/0006-staged-calamity-independence.md)。

## Design pillars

1. **Execution** — 回避、dash、位置取り、火力維持。
2. **Coordination** — 頭割り、散開、蘇生、将来のpair/誘導。
3. **Optimization** — Pylon DPS、Core burst、装備とparty構成。
4. **Clarity** — 死因、assignment、成功/失敗、次の改善が読める。
5. **Recovery** — 軽微な失敗とDownedを立て直せるが、立て直しには機会費用がある。
6. **Extensibility** — feature ownershipとserver/client境界が追加Boss/Raidを妨げない。

## Current encounters

最初のRaidは **不幸な人形劇 / The Unfortunate Doll Play**。Bossは **ラクリモーサ — 縛られた心 / Lacrimosa — The Bound Heart**。`FirstSeverance` / `first_severance` は安定内部IDとして残る。[Raid概要](encounters/first-severance/README.md)と[現行仕様](encounters/first-severance/ENCOUNTER_SPEC.md)が公開名と戦闘体験を持つ。

Raidは集合・Readyから複数フェーズとFinal生存へ進む。現行の蘇生方式は[Revive Spec](encounters/first-severance/REVIVE_SPEC.md)に従う。古いchannel/token方式や初期の単純ループを現行仕様として実装し直さない。

**幽鬼武者 / Ghost Samurai** は同じ基盤を使う独立Boss。[専用仕様](encounters/ghost-samurai/ENCOUNTER_SPEC.md)が召喚、攻撃、通常死亡、終了と再召喚を定義する。Raid専用のReady/Down/蘇生を全Bossの要件にしない。

実装・検証の現在地は [Status](STATUS.md)。この文書は製品の方向性を持ち、ビルド別の状態表は持たない。

## Player experience goals

- 2～4人で同じphase、timer、assignment、結果が見える。
- 一人の通常ミスはsoft failureになり、即座に全滅させない。
- DPSを出す時間、mechanicへ移動する時間、蘇生する時間が意味あるtradeoffになる。
- 特定classがいなければ処理不能、という設計にしない。
- host/non-host、高ping、途中切断でauthority結果を変えない。
- 演出を減らしてもtelegraphとstateが読める。

## Setting and presentation

各Encounterが固有の背景・造形・音・動きを持つ。不幸な人形劇の懸架された人形と拘束機構は [Doll Theater](encounters/first-severance/DOLL_THEATER_VISUAL_SPEC.md)、幽鬼武者の骸骨・鬼火・二刀流は[専用仕様](encounters/ghost-samurai/ENCOUNTER_SPEC.md)を参照する。既存作品の顔・logo・構図やCalamity assetを再現・抽出しない。

[Art Direction](ART_DIRECTION.md)は共通の読みやすさとfeature別の参照先を示す。過去の極地/観測施設の案や旧固有名は、新しいBossを同じ見た目へ固定する条件ではない。

## Scope and acceptance

現在の依頼と各featureのspec/planが実装範囲を決める。[Raid backlog](encounters/first-severance/BACKLOG.md)は未採用の案であり、今回の依頼を自動拡張しない。初期sliceの計画・完了済みrename・bootstrapは[履歴](history/2026-09-07-pre-consolidation.md)に残る。

独立Mod化、同時に複数のEncounterを動かす設計、一般的なnative lethal/rejoin互換性はそれぞれ別の設計・検証対象。完成済みの報酬や音楽を旧い「初期slice対象外」の一覧から取り消さない。

実装依頼の完了は [AGENTS](../AGENTS.md#verification) と[検証表](../.agents/skills/develop-convergence-raids/references/verification-matrix.md)に従う。公開/本番受入には [Release Process](RELEASE_PROCESS.md) の対応する人数・環境・cleanup・互換性の証拠が必要であり、単体のコンパイルや一人の画面表示だけでは満たさない。
