# Arena Infrastructure

## Scope

この文書はMilestone 1のPolar Foundation Core、Arena Validator、Ready、Barrier、Cleanupだけを定義する。Boss AIや戦闘ギミックは対象外。

## Coordinate model

初期値:

- 幅: 320 tiles
- 高さ: 140 tiles
- Core: Arena下端中央
- world edge safety margin: 20 tiles
- barrier inset: 2 tiles

```text
coreCenter = Core Tile Entityの論理中心
left       = coreCenter.X - width / 2
top        = coreBaseY - height
bounds     = Rectangle(left, top, width, height)
```

すべての計算はtile座標で行い、描画とplayer position判定時だけ16 px/tileでworld座標へ変換する。

## Activation flow

1. clientがCoreを操作し`RequestActivate`を送る。
2. serverがsender、request nonce/rate、requested coordinateの基本条件を検証する。
3. accepted requestから`Validating` runtimeを作り、server上のCore Tile Entityを解決する。requested coordinateを実anchorとして信用しない。
4. serverが進行条件、競合するWorld state、参加候補を確認し、副作用のない`ArenaValidator.Validate`を実行する。
5. 失敗時はissue code一覧を起動者へ返してCleanupする。
6. 成功時はgeneric lifecycleを`Preparing`へ遷移し、Raid substateを`AwaitingParticipants`にする。
7. eligible playerへ参加UIを表示する。
8. 2～4人が確定し全員ReadyになったらRaid substateを`Countdown`へ進める。
9. Milestone 1ではBossを出さず、debug countdown後にgeneric `Active`へ入りBarrier動作を確認できる。

## Validator contract

Validatorはworldを変更せず、次を返す。

```text
ArenaValidationResult
  IsValid
  Bounds
  Issues[]
  Metrics
    ScannedTiles
    SolidInteriorTiles
    LiquidTiles
    Chests
    TileEntities
    ScanDuration
```

issueはlocalized textではなく安定したenum codeと座標を返し、UI側で翻訳する。

## Initial validation rules

必須:

- calculated boundsがworld edge safety margin内に収まる。
- 同時に別Raidが存在しない。
- Core Tile Entityが有効で、要求座標と一致する。
- Core直下のfoundation footprintが連続したsolid tileである。
- Arena内にchest、別Core、重要Tile Entityが存在しない。
- Dungeon/Temple等の保護対象Tileを含まない。
- active boss、invasion、Boss Rushと競合しない。
- 起動者がCoreから規定距離内にいる。

初期警告または調整対象:

- interior solid tile数と比率
- liquid量
- wire/actuator
- platform/rope
- player bed/spawn point
- town NPC housing

検査は約44,800 tilesを起動時に一度走査する。実測でserver hitchが問題になった場合だけchunked scanへ変更する。最初から非同期world accessを行わない。

## Participant selection

- candidateは同一worldでactive、生存、Coreから参加半径内のplayer。
- 2～4人のみ`Preparing/AwaitingReady` substateへ進める。
- 5人以上いる場合は自動選択せず、明示的な参加UIを使う。
- Ready後の装備変更を初期段階では禁止しない。
- Ready中に2人未満になった場合はtimeoutを待たずcancelする。
- Active開始後の途中参加はspectatorまたは次回参加とし、Milestone 2で詳細化する。

## Barrier behavior

BarrierはTileを生成しない。

- client: 半透明line/plane、警告色、接近時のVFXを描画。
- client: local playerへpredictive clampまたは内向きvelocityを適用。
- server: participantがbounds外へ出た場合、最も近い安全点へ補正。
- non-participant: Active Raidのlogical boundsへ侵入した場合はserver警告後に安全なArena外へ退去させ、反復侵入者をFight中の対象外として扱う。
- NPC/Projectile: Milestone 1では対象外。Encounter entity側でArena boundsを参照する。

Barrierは即死させず、内側へ戻す。連続補正が発生した場合はログとUI警告を出し、rubber-bandingを観測できるようにする。

`ResolvedThirdSeveranceCoreAnchor`はserver resolverが発見したCore中心、foundation base Y、Tile Entity IDを運ぶDTOであり、「検証済み」を意味しない。`ThirdSeveranceArenaBlueprint.TryCreateLayout`は別途serverのWorld boundsを必須入力とし、非負かつoverflowしないWorld矩形、Core TE identity、Core論理中心、Core base点、320x140のprospective Arena全域と20-tile edge marginを検査する。Core論理中心はWorldとArenaの双方、base点はWorld内に存在しなければならない。

layoutはresolved CoreのXを水平中心、Core foundation baseのYをArena下端として算出する。Coreは幾何学的な中央ではなく床中央の基準点である。2 tiles insetのlogical Barrierと、Arena四隅から14 tiles insetした4つのPylon slotを返す。Coreの`(centerX, baseY)`からのoffsetはNW=`(-146,-126)`、NE=`(+145,-126)`、SW=`(-146,-15)`、SE=`(+145,-15)`で決定的に算出する。Tile壁は生成しない。

`ThirdSeveranceArenaAccessPolicy`の初期response chain:

- participantの外逸: 1 tickでwarning、6 continuous ticksで最寄りsafe pointへ補正。
- outsiderの侵入: 1 tickでwarningとRaid actorへのinteraction抑止、120 continuous ticksでArena外へ退去。
- 反復侵入: 3回目からFight中のArena対象外としてexcludeし、safe exteriorへ補正。

`warning + interaction suppression -> correction/ejection -> exclusion`はserver authority adapterの契約であり、damageまたは即死処理を含まない。outsiderからRaid actorへのdamage/hitを無効化するcombat adapter、safe point探索、spectator UI、join-in-progress本人性、他Mod teleportとの競合処理は未実装である。

player slotはTerrariaの`Main.player`範囲である0～254だけを許可し、255はserver/non-player sentinelとして拒否する。participant/outsiderの双方にserver割当のconnection epochを持たせ、slot再利用後の古いepisode responseを拒否する。実際のresponse直前にも現在のepochと位置を再検証する。Response kindは一つのrule内で重複不可とし、continuous ticksとviolation countの両thresholdを単調非減少にする。同一thresholdでは`warning -> interaction suppression -> correction -> ejection -> exclusion`の固定順で評価する。authority executorは返されたeligible responseをviolation episodeごとに一度だけ適用する。

## World manipulation restrictions

Milestone 1では、BarrierとCleanupの安定化を優先し、全Mod teleportや全Tile操作の完全禁止を一括実装しない。Active encounterへ入るまでに次の層を追加する。

1. placement/break/explosion/wiring/liquidのserver-side hook。
2. client UIで操作不能理由を表示。
3. teleport結果のserver correction。
4. known recall/pylon itemの早期拒否。

通常のplayer操作は`GlobalTile` / `GlobalWall`のplacement、kill、replace、explode hookで拒否する。wiringと他Modが直接`WorldGen`を呼ぶ経路は別途試験する。すべての外部Mod操作をhookだけで止められるとは仮定しない。

ブラックリストだけに依存せず、Arena外位置の補正を最終防衛線とする。

## Core destruction

- `Preparing`中のCore破壊はcancelとしてCleanupする。
- `Active`中は通常破壊を拒否するか、管理者/debug操作だけ中断を許可する。
- TileとTile Entityの削除順序に依存せず、どちらのhookからでも同じCleanup APIを呼ぶ。
- serverのみがfight終了を確定する。

## Cleanup invariants

Cleanup後に必ず成立する条件:

- global lifecycleはIdle。
- active Fight IDは存在しない。
- participantのReady/Raid flagはfalse。
- Barrier描画と移動制限は停止。
- Fight所有のtemporary entityは0。
- Coreが存在する場合はIdle表示。
- 新しいCoreを直ちに起動できる。

これらをdebug commandで検査できるようにする。
