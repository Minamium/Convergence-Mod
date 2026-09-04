---
doc_id: project.decisions
document_type: governance
status: accepted
owners:
  - project
last_reviewed: 2026-09-04
source_of_truth_for:
  - project.open_decisions
aliases:
  - decisions
  - open questions
related_code: []
related_docs:
  - decisions.adr-index
  - project.status
---

# Decisions and Open Questions

Long-lived structural decisions use immutable records in [`docs/adr`](adr/README.md). This document tracks reversible bootstrap constraints and unresolved production choices; it does not create a second ADR numbering scheme.

## Accepted architecture records

[`docs/adr/README.md`](adr/README.md) is the only ADR index and records every accepted or superseded structural decision. Do not duplicate that table here; this page is limited to reversible bootstrap constraints, provisional production values, and unresolved questions.

## Current bootstrap constraints

- Internal assembly/root namespace: provisional development identity `Convergence`.
- Entry class/project filename: `ConvergenceMod` / `ConvergenceMod.csproj`.
- One coordinator-managed Boss or Raid per World; World Events will have a separate lifecycle.
- Current legacy `ThirdSeverance` activation is intentionally denied. The target `FirstSeverance` feature remains denied until server-resolved Core/Arena/progression/roster/transport, actors, feature replication, and required recovery adapters satisfy their gates.
- Initial music implementation uses phase-specific mixes/transitions before sample-accurate dynamic stems.
- Current packet protocol and any future save schema version independently from the Mod version.
- Windows is the primary implementation and runtime-verification workstation; the MacBook remains a secondary docs/review environment.

## Provisional production choices

### Arena anchor

CoreをArena下端中央に置く案を初期値とする。

```text
width  = 320 tiles
height = 140 tiles
left   = coreCenterX - 160
top    = coreBaseY - 140
```

CoreをArena中央に置く方がテストしやすい場合はlive Arena実装前に変更する。

### Ready timeout

起動者がCoreを操作した後、参加者確定とReadyに60秒を与える。Readyはgeneric lifecycleではなくRaidの`Preparing` substateである。数値はplaytestで変更する。

## Open questions before affected feature implementation

1. 公開名とrepository名をいつ固有名へ変更するか。
2. Source codeとassetのlicense、および外部contribution同意方式。
3. Coreの最終サイズ、recipe、設置可能な進行条件。
4. Arena anchorを下端中央にするか完全中央にするか。
5. 既存Tile、platform、rope、liquidをどこまで許可するか。
6. Journey Mode、Mediumcore、Hardcoreを初期対応範囲へ含めるか。
7. 2人未満でのCore起動を拒否するか、development overrideを用意するか。
8. Calamity difficulty（Revengeance / Death）の扱い。
9. 音楽を本体Addonへ同梱するか、将来Music Modへ分離するか。
10. AI生成assetを完成版へ利用する場合の開示・制作記録方針。
11. Rejoin時に同一participantと認定するserver-side identity。
12. Boss、施設、集合意識、Core、蘇生itemの最終公開名。
13. Downed/Eliminated bodyをVictory、Cancel、Defeatでどう正規化するか（Windows死亡hook spike後）。
14. Pylon HP、Boss HP、Stack/Spread damage/radii、loop capの実機tuning値。

## Decision workflow

未確定事項は実装commitへ暗黙に埋め込まない。authority、module direction、protocol、persistence、external dependency、release/rightsを変える場合は新しいADRを追加し、既存ADRを必要に応じてSupersededへ変更する。可逆なbalance/production値はfeature specで`Provisional`として更新する。現在の実装有無は必ず[`STATUS.md`](STATUS.md)へ反映する。
