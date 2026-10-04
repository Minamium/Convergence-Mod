---
doc_id: project.brief
document_type: overview
status: accepted
owners:
  - project
last_reviewed: 2026-10-04
source_of_truth_for:
  - project.product_scope
aliases:
  - project brief
  - product direction
  - encounter roster
related_code:
  - build.txt
  - Common/Compatibility/Calamity
  - Content/Encounters
related_docs:
  - project.status
  - project.milestones
  - project.art-direction
  - encounter.first-severance.overview
  - encounter.ghost-samurai.spec
  - encounter.crimson-foundry.spec
  - encounter.azure-cathedral.spec
  - encounter.ebon-manor.spec
---

# Project Brief

## One-line concept

Calamity終盤に、位置取り・火力・蘇生を協力して処理するRaidと、予兆を読み切って挑む独立Bossを追加するContent Mod。

## Product position

Convergenceは、Calamity終盤の個人回避・火力最適化を土台に、MMORPG Raid型の協力ギミックをTerrariaの2D移動とserver authorityへ翻訳する。弾幕密度とHPだけを増やしたSuperbossにはしない。

協力Raidの想定party:

- Exo MechsおよびSupreme Calamitas撃破済み;
- Shadowspec級装備を利用可能;
- 2～4人の固定または準固定party（設計と調整の目安）;
- 5～12分程度の反復攻略を受け入れられる。

5～12分は将来のRaid全体のproduct goalであり、現行の戦闘時間を示す数値ではない。入場できる人数は各feature specが持つ（現行はDollが1～4人、Scarlet・Cathedral・Ebonが1～8人、Ghost Samuraiはフィールド内の全員）。一人でも開始できる（[ADR-0025](adr/0025-public-solo-admission.md)）が、一人用には再調整しない。

**未決（オーナー判断待ち）:** 調整の目安を2～4人のままにするか、5～8人も調整の対象にするか。

最初のRaidは基盤の実証対象であってAddon全体の上限ではない。長期的には独立Boss、追加Raid、World content、進行、Item、Utility、演出まで広げ、Calamity級の独自Content Modを目指す。

## Dependency strategy

- **Stage A — Calamity addon:** Calamityの進行、class連携、Shadowspec級balanceを隔離adapter越しに利用し、Encounterを完成させる。**現在はこの段階。**
- **Stage B — Portable Raid core:** project-ownedな進行/class/balance capabilityへ置換し、Encounter、Networking、Arena、Raid domainからCalamity型と名称を排除する。
- **Stage C — Standalone Content Mod:** 独自進行、素材、装備、recipe、World content、balanceを実装し、hard dependencyを外す。残す場合のCalamity対応はoptional adapterとして再決定する。

`modReferences`の削除だけでStandaloneにはならない。各stageはbuild/load/multiplayer/release matrixを持つ。詳細は [ADR-0006](adr/0006-staged-calamity-independence.md)。段階の順序と現在地は [Milestones](MILESTONES.md)。

## Design pillars

1. **Execution** — 回避、dash、位置取り、火力維持。
2. **Coordination** — 頭割り、散開、蘇生、将来のpair/誘導。
3. **Optimization** — Pylon DPS、Core burst、装備とparty構成。
4. **Clarity** — 死因、assignment、成功/失敗、次の改善が読める。
5. **Recovery** — 軽微な失敗とDownedを立て直せるが、立て直しには機会費用がある。
6. **Extensibility** — feature ownershipとserver/client境界が追加Boss/Raidを妨げない。

## Encounter roster

現在のEncounterは5つ。戦闘、世界観、やらないことは各feature specが正本で、この表は一行の要約とリンクだけを持つ。実装と検証の状態は [Status](STATUS.md)。

| Encounter（内部ID） | 種類 | 世界観（一行） | 正本 |
|---|---|---|---|
| Requiem of the Hollow Doll / `FirstSeverance` | 協力Raid。Ready、Down、即時蘇生 | 大聖堂の劇場に吊るされ縛られた、白髪のゴシック人形ラクリモーサ。曲はEigHt「不幸な人形劇」（改名しない） | [概要](encounters/first-severance/README.md)、[仕様](encounters/first-severance/ENCOUNTER_SPEC.md) |
| 幽鬼武者 / Ghost Samurai / `GhostSamurai` | 独立Boss。召喚アイテム、通常の死亡 | 骸骨の鬼武者、紫の霊炎、二刀流。討伐で刀Oboro | [仕様](encounters/ghost-samurai/ENCOUNTER_SPEC.md) |
| Scarlet Invocation / `CrimsonFoundry` | 協力Raid。Ready、Down、即時蘇生 | 召喚魔術と紅魔術。人間大の術師Vesperaが三体の異形を召喚して戦わせ、最後に一体の巨人へ束ねる。曲はkuku「Graceful Ordeal」で、曲に合わせるのは要所だけ（リズムゲームにはしない） | [仕様](encounters/crimson-foundry/ENCOUNTER_SPEC.md) |
| Cathedral of the White Night / `AzureCathedral` | 協力Raid。Ready、Down、即時蘇生 | 氷・硝子・水の大聖堂。剣を持つ少女Lioraと硝子の大海蛇Vitrion。曲はEigHt「白夜に耀うステンドグラス」 | [仕様](encounters/azure-cathedral/ENCOUNTER_SPEC.md) |
| Waltz of the Ebon Manor / `EbonManor` | 協力Raid。Ready、Down、即時蘇生 | 月夜の洋館で、館の主Noiretteが絹の糸で家具と館そのものを踊らせる。人形・マリオネットの意匠は少なくし、糸は人形の体を操らない。曲はEigHt「AutoMatador」 | [仕様](encounters/ebon-manor/ENCOUNTER_SPEC.md) |

Dollの古い蘇生方式（channel/token）や初期の単純ループを、現行仕様として実装し直さない。Encounterを足す・外す・改名するときは、同じ変更でこの表を直す（`tools/repository_checks.py` が `docs/encounters/` の各featureがこの表から参照されていることを確かめる）。

## Setting and presentation

各Encounterが固有の背景・造形・音・動きを持つ。共通の読みやすさとLuminanceの方針は [Art Direction](ART_DIRECTION.md)、Encounterごとの意匠はそのfeature specが持つ。既存作品の顔・logo・構図やCalamity assetを再現・抽出しない。過去の極地/観測施設の案や旧固有名は、新しいBossを同じ見た目へ固定する条件ではない。

オーナーが決めた、全Encounter共通の方向:

- 世界観・トーン・やらないことは、各feature specの冒頭に書く。オーナーが会話で方向を決めたら、その変更で冒頭を直し、上の表の一行も合わせる。
- 効果音・武器・演出のテーマは、そのEncounterの世界観から決める。曲名や技名からの連想で広げない（2026-10-04、Scarletの効果音と報酬武器が「楽器・演奏・聖歌隊」に寄りすぎて却下された）。
- ほかのEncounterの意匠を借りない（例：ScarletにEbonの糸の表現を使わない）。
- 見た目と音の作業では、当たり判定の形・時刻・威力を変えない。変える必要があれば、オーナーに確かめてから別の変更にする。
- 借りた曲はそのまま使う。ループとフェード以外の切り直し・組み替えは、オーナーの承認を取ってから行う（権利の記録は [Attribution](../Assets/ATTRIBUTION.md)）。

## Player experience goals

- 2～4人で同じphase、timer、assignment、結果が見える。
- 一人の通常ミスはsoft failureになり、即座に全滅させない。
- DPSを出す時間、mechanicへ移動する時間、蘇生する時間が意味あるtradeoffになる。
- 特定classがいなければ処理不能、という設計にしない。
- host/non-host、高ping、途中切断でauthority結果を変えない。
- 演出を減らしてもtelegraphとstateが読める。

## Scope and acceptance

現在の依頼と各featureのspec/planが実装範囲を決める。[Raid backlog](encounters/first-severance/BACKLOG.md)は未採用の案であり、今回の依頼を自動拡張しない。初期sliceの計画・完了済みrename・bootstrapは[履歴](history/2026-09-07-pre-consolidation.md)に残る。プロジェクト全体の次の関門と順序は [Milestones](MILESTONES.md) が持つ。

独立Mod化、同時に複数のEncounterを動かす設計、一般的なnative lethal/rejoin互換性はそれぞれ別の設計・検証対象。完成済みの報酬や音楽を旧い「初期slice対象外」の一覧から取り消さない。

実装依頼の完了は [AGENTS](../AGENTS.md#verification) と[検証表](../.agents/skills/develop-convergence-raids/references/verification-matrix.md)に従う。公開/本番受入には [Release Process](RELEASE_PROCESS.md) の対応する人数・環境・cleanup・互換性の証拠が必要であり、単体のコンパイルや一人の画面表示だけでは満たさない。
