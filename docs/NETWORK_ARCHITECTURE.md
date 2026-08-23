# Network Architecture

Implementation status: Milestone 0 includes the protocol version, explicit packet IDs, Fight ID envelope parsing, direction checks, bounded rejection logs, a safe no-op router, typed authority commands, and a read-only client replica. Typed fixed/bounded packet decoders and serialization begin in Milestone 1; no packet currently mutates gameplay state.

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
  -> Preparing
  -> Active
  -> Resolving
  -> Cleanup
  -> Idle
```

`Idle`はactive sessionが存在しないprojectionであり、Session内部の遷移先ではない。Raidの参加選択、Ready、開始countdownはgeneric `Preparing`内のRaid substateとする。

`Validating`失敗、Ready timeout、起動者cancel、Core破壊、参加者不足、全滅、Boss消失、World unloadはすべて`Cleanup`へ入る。例外経路から直接`Idle`へ戻さない。Cleanup時のEncounter Sequence、Fight ID、最終Revision、End Reasonをterminal snapshot/outboxへ確定してからactive sessionを外す。

Runtimeは`Tick`から`EncounterRuntimeUpdate`を返し、legal transition、end reason、observable changeをauthorityへ要求する。Coordinatorへのglobal逆参照や、非terminal lifecycleへEnd Reasonを直接書く経路は持たない。

## Runtime state decomposition

単一の巨大な`RaidRuntimeState`を作らず、責任ごとに分割する。

```text
EncounterSession
  Identity      : EncounterSequence, FightId, definition key, protocol/schema identity
  Lifecycle     : lifecycle, revision, end reason
  RaidRoster    : stable ParticipantId <-> current player slot
  Arena         : tile bounds, Core anchor, validation state
  Phase         : phase ID, start tick, loop, random stream
  Mechanics     : assignments and authoritative results
  Ownership     : compact owner token -> NPC/Projectile registry
  FeatureState  : Third Severance-owned snapshot payload
```

Common Encounter stateは通常Bossへも適用できる最小部分だけを持つ。Ready、Roster、Revive等はRaid sessionの`Preparing`/`Active` substateへ置き、通常Bossへ強制しない。

player nameやdisplay stringを権威状態に保存しない。Fight内で固定した`ParticipantId`と現在のplayer slotをRosterが対応させ、切断時に明示的に無効化する。client申告のUUIDや名前だけでRejoin本人性を決めない。

## Packet envelope

すべてのcustom packetは共通headerを持つ。

```text
ProtocolVersion : ushort
PacketType      : byte
EncounterSeq    : ulong (World内で単調増加)
FightId         : 16 bytes (active fightに関係する場合)
Revision        : uint
Payload         : type-specific fixed/bounded schema
```

Packet IDは明示値を持ち、廃止後も再利用しない。初期PacketType:

| Direction | Packet | Purpose |
|---|---|---|
| C -> S | `RequestActivate` | Core位置を指定して検査を要求 |
| C -> S | `RequestSetReady` | 自分のReady状態だけを要求 |
| C -> S | `RequestCancel` | 起動者によるReady中のcancel要求 |
| C -> S | `RequestSnapshot` | join/rejoinまたはrevision mismatchからの復旧 |
| S -> C | `Snapshot` | 完全なruntime state |
| S -> C | `StateChanged` | lifecycle/phaseなど小さなdelta |
| S -> C | `ParticipantChanged` | join/ready/disconnect |
| S -> C | `ValidationResult` | structured issue list |
| S -> C | `EncounterEnded` | reasonと最後のrevision |

クライアントから送られたplayer ID、Fight ID、Core位置を信用しない。`whoAmI`、server上のTile Entity、距離、現在state、rate limitから再検証する。

`EncounterStartCommand.RequestedAnchor`はraw requestである。global authority policy通過後も`AcceptedEncounterStart.RequestedAnchor`のままであり、まだArena/Core検証済みとは呼ばない。`Validating` runtimeがserver上のCore Tile Entityから実anchor/boundsを解決し、structured validation resultが成功するまでWorldを変更しない。Third Severanceはこの実装が入るまでfeature policyで起動不能にしている。

## Snapshot and delta policy

- WorldDataの`ModSystem.NetSend/NetReceive`には、永続world flagと「active fightが存在するか」の最小情報だけを置く。
- 完全なactive fight stateは`EncounterSnapshot`とfeature/Raid-specific bounded payloadで送る。
- clientは未知PacketType、不正なshape、同じEncounter Sequenceなのに異なるFight IDを受信したらstateを書き換えない。delta/eventのunknown Fight IDまたはrevision gapではsnapshotを再要求する。この復旧transportはMilestone 2で実装する。
- full snapshotは、より大きいEncounter Sequenceへの切替とrevision jumpを受理する。replicaは`(EncounterSequence, Revision, AuthorityTick)`を辞書順で適用し、同SequenceのFight ID差異とterminal後の非Idle stateを拒否する。
- serverはphase、roster、mechanic result等の粗いstate変更ごとにRevisionを1増やす。毎hitや毎tickのHP変化には使わない。
- deltaは同じFight IDかつ新しいRevisionの場合だけ適用する。
- timerを毎tick同期せず、`PhaseStartTick`と定期的な低頻度clock correctionを送る。

## Tick and random policy

- mechanicsの乱数はserverのみが消費する。
- clientへは結果またはVFX再生用seedを送る。
- `Main.GameUpdateCount`を直接永続化しない。Fight内のserver tickを基準にする。
- client clockは表示用に補間してよいが、判定tickはserver値を使用する。

server-side Raid tickの第一候補は`ModSystem.PostUpdateWorld()`とする。これはSingle Playerまたはserverで呼ばれるため、state transitionの入口を一か所に集約しやすい。

Milestone 0ではSingle Player projectionだけがbounded snapshot outboxをread-only replicaへ接続する。live full snapshotは最新値へcoalesceし、terminal snapshotを優先して最大64件保持する。これは無制限のevent journalではないため、Milestone 1のserver transportは毎tick消費し、Milestone 2ではack/rejoin snapshotで配送保証を完成させる。Multiplayerのserialize/send/typed dispatch/join snapshotは未実装であり、Third Severance activationも無効である。

## Tile Entity synchronization

Polar Foundation Coreは`ModTileEntity`とし、永続情報と表示に必要な最小stateのみ`NetSend/NetReceive`する。Core操作によるitem spawnやstate changeはserverで実行する。Tile EntityはArena encounterそのものを所有せず、`EncounterCoordinatorSystem`へ渡すrequest anchorとして扱う。

tModLoader 1.4.4の`ModTileEntity`に汎用の`netUpdate` flagはない。設置は`Generic_HookPostPlaceMyPlayer`、破壊は`KillMultiTile`から明示的なTE削除、更新は`MessageID.TileEntitySharing`またはcustom packetで同期する。

## Entity ownership

一時NPCとProjectileはserver-side registryでFight内compact owner tokenへ関連付ける。`Guid`を`ai[]`へ格納しない。

- `GlobalNPC` / `GlobalProjectile`のper-entity dataへcompact tokenを保持
- server側registryでtoken、`whoAmI`、Fight IDを対応付ける
- `ai` / `localAI`を使用する場合もtokenだけとし、registryを正本にする

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
- `BinaryReader.BaseStream.Length`はtModLoader共有受信bufferの末尾なので、Mod側のpacket境界やtrailing判定には使わない。各PacketTypeのdecoderは固定fieldと個別のcount/string上限だけを読み、DTO全体の構築・検証完了後に初めてcommandへ変換する。外側packet長との消費量照合はtModLoaderへ任せる。

## Cleanup contract

`Cleanup(FightId expected, RaidEndReason reason)`は冪等でなければならない。

1. 現在Fight IDが一致するか確認する。
2. lifecycleを`Cleanup`へ変更し、新規spawnを停止する。
3. server上のowned NPC/Projectile/temporary stateを除去する。
4. participantのRaid固有Player stateを解除する。
5. CoreをIdleへ戻す。Coreが無くても継続する。
6. terminal snapshotを保持し、`EncounterEnded`をbroadcastする。
7. registry、assignment、timer、seedを破棄する。
8. authoritative active sessionを外し、通常snapshotを`Idle`へ戻す。

一つの除去処理またはfailure loggerが失敗しても残りを実行し、active sessionは`finally`で必ず外す。失敗したcleanup participantはretry backlogへ残し、完了まで次のEncounterをblockする。World unloadでは最終retryと未完数のdiagnosticを行って参照を破棄し、active runtimeを保存・復元しない。将来の`ModPlayer`/静的flagはWorld load時にもdefensive resetする。

## Logging

server logへ次を構造化して残す。

- Fight start/end、Fight ID、participant count
- lifecycle transitionとrevision
- validation failure code
- packet rejection reason（spamを集約）
- disconnect/rejoin
- Cleanup開始・完了・残存entity数

playerの個人情報や毎tickのpositionは記録しない。
