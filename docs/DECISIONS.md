# Decisions and Open Questions

Long-lived structural decisions use immutable records in [`docs/adr`](adr/README.md). This document tracks reversible bootstrap constraints and unresolved production choices; it does not create a second ADR numbering scheme.

## Accepted architecture index

| Record | Decision |
|---|---|
| [ADR-0001](adr/0001-modular-monolith.md) | Modular monolith with feature-first content modules |
| [ADR-0002](adr/0002-server-authoritative-encounters.md) | Server/SP authority and read-only client replication |
| [ADR-0003](adr/0003-in-world-logical-arena.md) | Normal World, logical Barrier, one managed Boss/Raid at a time |
| [ADR-0004](adr/0004-calamity-compatibility-boundary.md) | Isolated Calamity adapter and tested-version gate |

Accepted consequences shared by those ADRs:

- active Encounter state is ephemeral and is not resumed after World load;
- every fight has a `FightId`, a World-monotonic Encounter Sequence, and revisions;
- Raid-only Ready/Roster/Revive state is not part of the generic Encounter lifecycle;
- Calamity-specific APIs and types do not leak into feature logic;
- generated Tile walls, Subworlds, client-decided outcomes, and vendored Calamity assets/code are excluded.

## Current bootstrap constraints

- Internal assembly/root namespace: provisional development identity `Convergence`.
- Entry class/project filename: `ConvergenceMod` / `ConvergenceMod.csproj`.
- One coordinator-managed Boss or Raid per World; World Events will have a separate lifecycle.
- Third Severance activation is intentionally denied until Milestone 1 installs server-resolved Core, Arena, progression, roster, nonce, and transport validation.
- Initial music implementation uses phase-specific mixes/transitions before sample-accurate dynamic stems.
- Current packet protocol and any future save schema version independently from the Mod version.

## Provisional production choices

### Arena anchor

CoreをArena下端中央に置く案を初期値とする。

```text
width  = 320 tiles
height = 140 tiles
left   = coreCenterX - 160
top    = coreBaseY - 140
```

CoreをArena中央に置く方がテストしやすい場合はMilestone 1開始前に変更する。

### Ready timeout

起動者がCoreを操作した後、参加者確定とReadyに60秒を与える。Readyはgeneric lifecycleではなくRaidの`Preparing` substateである。数値はplaytestで変更する。

## Open questions before affected feature implementation

1. 公開名とrepository名をいつ固有名へ変更するか。
2. Source codeとassetのライセンス、および外部contribution同意方式。
3. Coreの最終サイズ、recipe、設置可能な進行条件。
4. Arena anchorを下端中央にするか完全中央にするか。
5. 既存Tile、platform、rope、liquidをどこまで許可するか。
6. Journey Mode、Mediumcore、Hardcoreを初期対応範囲へ含めるか。
7. 2人未満でのCore起動を拒否するか、development overrideを用意するか。
8. Calamity difficulty（Revengeance / Death）の扱い。
9. 音楽を本体Addonへ同梱するか、将来Music Modへ分離するか。
10. Last Stand中のUI非表示範囲とアクセシビリティ代替表示。
11. AI生成assetを完成版へ利用する場合の開示・制作記録方針。
12. Rejoin時に同一participantと認定するserver-side identity。

## Decision workflow

未確定事項は実装commitへ暗黙に埋め込まない。authority、module direction、protocol、persistence、external dependency、release/rightsを変える場合は新しいADRを追加し、既存ADRを必要に応じてSupersededへ変更する。可逆なbalance/production値はこの文書またはfeature specで更新する。
