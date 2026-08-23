# Network Architecture

## Goals

- 2～4人とDedicated Serverで同一のRaid状態を維持する。
- 高pingやpacketの遅延・重複で勝敗が変わらない。
- クライアントが割当、ダメージ判定、勝敗を確定できない。
- 途中参加者へ完全snapshotを送れる。
- Cleanupを何度呼んでも安全にIdleへ戻せる。
- 見た目の密度を上げてもnetwork trafficを比例増加させない。

## Authority model

サーバーまたはSingle Playerのlocal authorityのみが次を更新する。

- Raid lifecycleとFight ID
- Arena boundsとCore identity
- 参加者、Ready、Downed、Revive
- Phase、phase start tick、mechanic assignment
- Pylon、part、clone、weak pointの状態
- Weakness、Overload、Revive Token
- DPS checkと勝敗
- 当たり判定を持つentityのspawn

クライアントが担当するもの:

- input request
- UI、字幕、marker、screen shake
- 当たり判定を持たないparticle、trail、背景弾幕
- server eventから再構築できる音響演出
- 境界の予測的なmovement clamp

## Raid state machine

```text
Idle
  -> Validating
  -> Ready
  -> Starting
  -> Active
  -> Resolving
  -> Cleanup
  -> Idle
```

`Validating`失敗、Ready timeout、起動者cancel、Core破壊、参加者不足、全滅、Boss消失、World unloadはすべて`Cleanup`へ入る。例外経路から直接`Idle`へ戻さない。

## Runtime state

初期の`RaidRuntimeState`は次を持つ。

```text
ProtocolVersion : ushort
FightId         : Guid
Revision        : uint
Lifecycle       : RaidLifecycle
ArenaBounds     : Rectangle (tile coordinates)
CorePosition    : Point16
OwnerPlayer     : byte
Participants    : up to 4 player slots
ReadyMask       : byte
Phase           : ushort
PhaseStartTick  : ulong
RandomSeed      : int
FailureReason   : enum
```

player nameやdisplay stringを権威状態に保存しない。player slotとstable identifierの対応は参加時に記録し、切断時に明示的に無効化する。

## Packet envelope

すべてのcustom packetは共通headerを持つ。

```text
ProtocolVersion : ushort
PacketType      : byte
FightId         : 16 bytes (active fightに関係する場合)
Revision        : uint
PayloadLength   : bounded by packet schema
Payload         : type-specific
```

初期PacketType:

| Direction | Packet | Purpose |
|---|---|---|
| C -> S | `RequestActivateCore` | Core位置を指定して検査を要求 |
| C -> S | `RequestSetReady` | 自分のReady状態だけを要求 |
| C -> S | `RequestCancelRaid` | 起動者によるReady中のcancel要求 |
| C -> S | `RequestSnapshot` | join/rejoinまたはrevision mismatchからの復旧 |
| S -> C | `RaidSnapshot` | 完全なruntime state |
| S -> C | `RaidStateChanged` | lifecycle/phaseなど小さなdelta |
| S -> C | `ParticipantChanged` | join/ready/disconnect |
| S -> C | `ArenaValidationResult` | structured issue list |
| S -> C | `RaidEnded` | reasonと最後のrevision |

クライアントから送られたplayer ID、Fight ID、Core位置を信用しない。`whoAmI`、server上のTile Entity、距離、現在state、rate limitから再検証する。

## Snapshot and delta policy

- WorldDataの`ModSystem.NetSend/NetReceive`には、永続world flagと「active fightが存在するか」の最小情報だけを置く。
- 完全なactive fight stateは`RaidSnapshot`で送る。
- clientはunknown Fight ID、高すぎるrevision差、未知PacketTypeを受信したらstateを書き換えずsnapshotを要求する。
- serverはstate変更ごとにRevisionを1増やす。
- deltaは同じFight IDかつ新しいRevisionの場合だけ適用する。
- timerを毎tick同期せず、`PhaseStartTick`と定期的な低頻度clock correctionを送る。

## Tick and random policy

- mechanicsの乱数はserverのみが消費する。
- clientへは結果またはVFX再生用seedを送る。
- `Main.GameUpdateCount`を直接永続化しない。Fight内のserver tickを基準にする。
- client clockは表示用に補間してよいが、判定tickはserver値を使用する。

server-side Raid tickの第一候補は`ModSystem.PostUpdateWorld()`とする。これはSingle Playerまたはserverで呼ばれるため、state transitionの入口を一か所に集約しやすい。

## Tile Entity synchronization

Polar Foundation Coreは`ModTileEntity`とし、永続情報と表示に必要な最小stateのみ`NetSend/NetReceive`する。Core操作によるitem spawnやstate changeはserverで実行する。Tile EntityはArena encounterそのものを所有せず、`RaidEncounterSystem`へのanchorとして扱う。

tModLoader 1.4.4の`ModTileEntity`に汎用の`netUpdate` flagはない。設置は`Generic_HookPostPlaceMyPlayer`、破壊は`KillMultiTile`から明示的なTE削除、更新は`MessageID.TileEntitySharing`またはcustom packetで同期する。

## Entity ownership

一時NPCとProjectileは次のいずれかでFight IDへ関連付ける。

- `GlobalNPC` / `GlobalProjectile`のper-entity data
- entityの`ai` / `localAI`へ圧縮したowner token
- server側registryで`whoAmI`とFight IDを対応付ける

文字列tagや全entity scanへ依存しない。Cleanupはregistryを主に使い、defensive scanを補助にする。

## Barrier synchronization

- clientは自分の移動をArena内へ予測的に制限し、視覚Barrierを描く。
- serverは参加者位置を検査し、外へ出た場合に安全な内側位置へ補正する。
- 補正にはFight IDとRevisionを含むeventを使い、古いfightのteleportを適用しない。
- 他Modのteleportを完全に列挙して禁止せず、「外へ出た結果を補正する」ことを最終防衛線にする。

## Security and validation

- Packet payload長、enum範囲、player slot、tile座標を検証する。
- client requestはplayerごとにrate limitする。
- Coreから遠いplayerのActivate/Ready要求を拒否する。
- 参加者以外の戦闘packetを拒否する。
- serverはclientが報告したDPS、hit、mechanic successを採用しない。
- unknown packetはwarningを記録して無視し、serverを停止させない。

## Cleanup contract

`Cleanup(FightId expected, RaidEndReason reason)`は冪等でなければならない。

1. 現在Fight IDが一致するか確認する。
2. lifecycleを`Cleanup`へ変更し、新規spawnを停止する。
3. server上のowned NPC/Projectile/temporary stateを除去する。
4. participantのRaid固有Player stateを解除する。
5. CoreをIdleへ戻す。Coreが無くても継続する。
6. `RaidEnded`をbroadcastする。
7. registry、assignment、timer、seedを破棄する。
8. lifecycleを`Idle`へ戻す。

一つの除去処理が失敗しても残りを`finally`相当で実行する。World load時はactive runtimeを復元せず、stale stateを破棄する。

## Logging

server logへ次を構造化して残す。

- Fight start/end、Fight ID、participant count
- lifecycle transitionとrevision
- validation failure code
- packet rejection reason（spamを集約）
- disconnect/rejoin
- Cleanup開始・完了・残存entity数

playerの個人情報や毎tickのpositionは記録しない。
