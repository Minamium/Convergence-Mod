# Architecture Decisions and Open Questions

## Accepted decisions

### ADR-001: Multiplayer-first and server authoritative

戦闘状態、割当、タイマー、勝敗、Arena lifecycleはサーバーのみが更新する。クライアントは要求と表示を担当する。

### ADR-002: No Subworld

Arenaは通常World内へ展開する。途中参加、帰還、他Modとの互換性、Dedicated Serverの単純性を優先する。

### ADR-003: No generated tile wall

境界は大量のTile生成ではなく、論理的なRectangle、クライアント描画、サーバー位置補正で表現する。

### ADR-004: Single active raid

初期リリースではWorldごとに同時進行可能なRaidは1つだけ。すべての一時EntityにFight IDを関連付ける。

### ADR-005: Ephemeral fight state is not persisted

進行中FightはWorld saveを跨いで再開しない。World load後は必ずIdleから始める。永続化対象は将来の撃破フラグと恒久的設定のみ。

### ADR-006: Compatibility adapter

Calamity固有APIは`Common/Compatibility/Calamity/`へ隔離し、Encounterロジックから直接参照しない。

### ADR-007: Phase-specific mixes before dynamic stems

初期音楽はフェーズ別完成ミックスとTransitionを使用する。複数Stemのサンプル精度同期は後回しにする。

## Provisional decisions

### Arena anchor

CoreをArena下端中央に置く案を初期値とする。

```text
width  = 320 tiles
height = 140 tiles
left   = coreCenterX - 160
top    = coreBaseY - 140
```

CoreをArena中央に置く方がテストしやすい場合はMilestone 1開始前に変更する。

### Fight ID

`Guid`を使用し、各state updateへ単調増加する`uint Revision`を付ける。packetにはprotocol versionも含める。

### Ready timeout

起動者がCoreを操作した後、参加者確定とReadyに60秒を与える。全員Readyまたは起動者のcancelで遷移する。数値はplaytestで変更する。

## Open questions before code

1. 内部Mod名、公開名、root namespace。
2. リポジトリ名`Minamium/tmod`を維持するか、固有名へ変更するか。
3. Addon自体のライセンス。Calamityの独自ライセンスは継承せず、別途選ぶ必要がある。
4. Coreの最終サイズ、recipe、設置可能な進行条件。
5. Arena anchorを下端中央にするか完全中央にするか。
6. 既存Tile、platform、rope、liquidをどこまで許可するか。
7. Journey Mode、Mediumcore、Hardcoreを初期対応範囲へ含めるか。
8. 2人未満でのCore起動を拒否するか、開発用overrideを用意するか。
9. Calamity difficulty（Revengeance / Death）の扱い。
10. 音楽を本体Addonへ同梱するか、将来Music Modへ分離するか。
11. Last Stand中のUI非表示範囲とアクセシビリティ代替表示。
12. AI生成assetを完成版へ利用する場合の開示・制作記録方針。

## Decision workflow

未確定事項は、実装commitへ暗黙に埋め込まない。決定時にこの文書を更新し、必要なら新しいADRを追加する。
