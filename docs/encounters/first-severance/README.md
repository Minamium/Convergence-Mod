---
doc_id: encounter.first-severance.overview
document_type: overview
status: accepted
owners:
  - gameplay
last_reviewed: 2026-09-05
source_of_truth_for:
  - first_severance.document_map
aliases:
  - First Severance
  - 第一断絶
  - first_severance
related_code:
  - Content/Encounters/FirstSeverance
related_docs:
  - encounter.first-severance.spec
  - encounter.first-severance.plan
  - project.status
---

# First Severance

`First Severance`（第一断絶）は、Convergenceで最初に完成させる2～4人向けRaidです。現在はReadyから戦闘ループ・BGM・実験Down／蘇生までを確認する開発ビルドです。実装済み範囲と未検証事項は[Status](../../STATUS.md)を参照してください。

## Current slice

```text
Activation
  -> Boss spawn
  -> Pylon DPS check
  -> Stack
  -> Spread
  -> Core exposure
       -> HP remains: return to Pylon
       -> HP reaches zero: Victory
```

Stackはserver-owned damage poolを分ける実際の頭割りです。Raid独自ダメージの致死はDownedへ変換し、別の参加者が専用アイテムで蘇生します。`0.2.0`では巨大な黒氷のBoss・拘束装置の初稿と、予測線の後に繰り返し発射する観測の槍を追加します。単一NPC/bodyとはHP・判定の所有単位であり、外見を小さく制限する意味ではありません。実装・確認状態は[Status](../../STATUS.md)に集約します。

## Read by purpose

- [Encounter Specification](ENCOUNTER_SPEC.md): player-visible loop, authority outcomes, timing defaults, 2/3/4-player behavior.
- [Implementation Plan](IMPLEMENTATION_PLAN.md): safe rename, adapters, slices, gates, and Definition of Done.
- [Visual Specification](VISUAL_SPEC.md): minimum Boss silhouette and three visual states.
- [Revive Specification](REVIVE_SPEC.md): Downed, item channel, shared tokens, cancellation, and compatibility blocker.
- [Backlog](BACKLOG.md): preserved old ideas explicitly excluded from the first slice.

Project-wide authority, networking, arena, compatibility, and test constraints remain in the root documents and ADRs. This directory does not redefine them.

## Decision labels

- **Accepted**: minamiが方向として確定したもの。変更時は仕様・計画・必要なADRを同時更新する。
- **Provisional**: 最初の実機prototypeに入れる初期値。測定結果で変更可能。
- **TBD**: 実装または検証前に選択が必要。コードへ暗黙に固定しない。
- **Deferred**: アイデアは保持するが現在のDefinition of Doneへ含めない。

現在のvertical sliceは2～4分を暫定目標とし、5～12分は将来拡張したRaid全体のproduct goalです。通常のTerraria movement/combat同期については未改造clientでの協力playを前提とし、Convergence独自packetのauthorityをanti-cheatと表現しません。
