---
doc_id: encounter.first-severance.overview
document_type: overview
status: accepted
owners:
  - gameplay
last_reviewed: 2026-09-11
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

`First Severance`（第一断絶）は、2～4人推奨の開発版Raidです。全員Ready、3つの通常フェーズ、Final生存、Down／蘇生、勝敗演出、5クラスの武器まで実装しています。ソロ起動は開発確認用で、NPC仲間やソロ専用バランスはありません。実装・未検証事項は [Status](../../STATUS.md)、チャット不要の再開手順は [Windows handoff](../../handoff/WINDOWS.md) を参照してください。

## Current flow

```text
Core activation / all current-world participants
  -> field deployment and black exterior
  -> manual Ready from everyone
  -> separate Boss/Raid introduction
  -> Phase I: sealed / Pylons / Core / clockwise Stack + Spread
  -> Phase II: lattice / twin rotating blades / Spread
  -> Phase III: remote arms / floods / swords / Stack + Spread / crush
  -> Final: eight clockwise stops + bullets / previewed slicing triples
  -> survived full Final: Victory + shared reward drops
```

頭割りは固定座標へ全員集合すればゼロダメージ、不足人数分の割合ダメージ。散開も重ならなければゼロです。各フェーズは初回の行動一周を保証し、その後はHP閾値へ達したら即移行します。HPゼロだけでは勝利せずFinalを生き残る必要があります。詳細・閾値・定数の正本は [Encounter spec](ENCOUNTER_SPEC.md)。

Raid独自の致死ダメージはDownへ変換。味方のResuscitation Kitで即時蘇生、消費/共有トークンなし、蘇生を受けた側だけ60秒再蘇生不可。Eliminated/Down期限はなく、全員Downなら即敗北です。通常のTerraria致死との統合は別課題です。

## Read by purpose

- [Encounter Specification](ENCOUNTER_SPEC.md): player-visible loop, authority outcomes, timing defaults, 2/3/4-player behavior.
- [Implementation Plan](IMPLEMENTATION_PLAN.md): current work boundary, extension seams and completed consolidation; not another rename queue.
- [Visual Specification](VISUAL_SPEC.md): accepted materials, extreme motion contrast, phase forms, UI/field coordinate contract and endings.
- [Weapons](WEAPONS.md): claws, long-form Magic/Ranged/Summon/Rogue rituals and exchange recipes.
- [Audio](../../AUDIO_CUE_SHEET.md): active BGM, selectively restored SFX, silence and voice lifetimes.
- [Revive Specification](REVIVE_SPEC.md): Downed, reusable instant recovery, recipient lockout, and the ordinary-lethal-hook compatibility blocker.
- [Backlog](BACKLOG.md): preserved old ideas explicitly excluded from the first slice.

Project-wide authority, networking, arena, compatibility, and test constraints remain in the root documents and ADRs. This directory does not redefine them.

## Decision labels

- **Accepted**: ユーザーが方向として確定したもの。変更した事実の正本だけ更新し、構造判断が変わる場合のみADRを追加する。
- **Provisional**: 最初の実機prototypeに入れる初期値。測定結果で変更可能。
- **TBD**: 実装または検証前に選択が必要。コードへ暗黙に固定しない。
- **Deferred**: アイデアは保持するが現在のDefinition of Doneへ含めない。

現在のvertical sliceは2～4分を暫定目標とし、5～12分は将来拡張したRaid全体のproduct goalです。通常のTerraria movement/combat同期については未改造clientでの協力playを前提とし、Convergence独自packetのauthorityをanti-cheatと表現しません。
