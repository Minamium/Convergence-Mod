---
doc_id: encounter.ghost-samurai.spec
document_type: spec
status: provisional
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-13
source_of_truth_for:
  - ghost_samurai.behavior
aliases:
  - Ghost Samurai
  - 幽鬼武者
related_code:
  - Content/Encounters/GhostSamurai
  - Client/Encounters/GhostSamurai
  - Tests/Convergence.DomainTests/GhostSamuraiTests.cs
related_docs:
  - project.status
  - development.windows
---

# 幽鬼武者 / Ghost Samurai

ユーザー提供の「骸骨の鬼武者 × 青白い幽霊 × 二刀流」の画像と文章を基にした、別ボスの開発用実装。現在の実装・検証状態は [STATUS](../../STATUS.md) を参照する。既存 First Severance の戦闘・素材は変更しない。

## 呼び出しと戦闘範囲

作業台で **骨10個＋星5個 → 鬼武者の弔い鈴**。消費せず繰り返し使用できる。広い空間で呼び出す。今回はユーザーが選んだ召喚アイテム方式であり、集合・Ready・Raid Down/蘇生は付けない。フィールドは下記の召喚者中心方式で自動展開する。通常のHP・死亡を使う。戦利品・進行条件・最終バランスは今回の範囲外。

保存上の識別子は `Convergence/GhostSamuraiSummon` を維持する。Modの無効化・読み込み失敗でUnloaded Itemになった場合、tModLoader標準の保存情報には元のMod名・アイテム名が残る。原因を修正してModを有効化し、同じキャラクター／ワールドを読み込み直すことで復元する経路を使い、独自の置換アイテムやセーブ編集は行わない。

召喚受付時の使用者中心に **2560×1120px（横160×縦70タイル）** のフィールドを固定して展開する。First Severanceと同じ基準サイズ・全身の内側補正方式を使うが、地面やCoreではなく使用者のX/Yを中心にする。世界端では中心を動かさず左右／上下を対称に縮める。極端な端で有効な空間を作れない場合は召喚を拒否する。タイルの設置・削除・地形整備はしない。

フィールド内の生存プレイヤーと後から入った人を対象とする。外にいる人を強制集合させず、ターゲットと被弾対象はフィールド参加者だけにする。移動・ダッシュ・フック・マウント・ノックバック・テレポート後の全身を内側へ無傷で補正する。補正先が地形に埋まる場合は、記録済みの直近の安全な内側位置を優先する。サーバーが補正し、ローカル参加者だけが同じ境界で予測補正する。無限飛行や地形変更は追加しない。

青白い外枠と内向きの短い目盛りが移動可能範囲。フィールド座標は戦闘中に動かず、全フェーズで共有する。死亡・切断で本人の制限を解除し、再入場は通常のフィールド進入で扱う。戦闘終了・世界終了は同じFightの制限を全解除する。生存参加者がいない状態が3秒続くと退場する。その猶予中は攻撃を消して停止し、参加者が戻る場合はインターバルから再開する。

召喚要求は既存の EncounterCoordinator を使う。`EncounterKind.Boss` の独立した `ghost_samurai` 定義なので、既存Raidとの同時起動を同じ共通機構で拒否する。依存ライブラリは現行版を維持する。

## 状態と攻撃

`GhostSamuraiRuntime` が戦闘を所有し、`GhostSamuraiBoss : ModNPC` はダメージを受ける本体と同期用の表示情報と不変のフィールド境界を持つ。状態は `SamuraiPhase`、`SamuraiAttack`、`SamuraiBeat`、攻撃タイマーに分離する。

`Idle → Approach/Telegraph → Strike → Recovery → Idle` を基本とし、余韻18tickの後、第1フェーズ36tick（0.6秒）、第2・第3フェーズ24tick（0.4秒）の攻撃間隔を置く。次の主攻撃は前回の全斬撃・余韻を終えてから、前回を除いた候補からサーバーが選ぶ。第1フェーズは3候補、第2フェーズは4候補、第3フェーズは円形攻撃を加えた5候補。以下の時刻は60tick＝1秒。

| 攻撃 | 予告と処理 |
|---|---|
| 二連斬撃×4セット | 元の `DirectionalSlash` を維持し、各セットの2発目は刀の向きを90度変える。8発それぞれ現在のターゲット位置をロックして54tick予告→10tick判定。予告開始は0/12/48/60/96/108/144/156tick、発射は54/66/102/114/150/162/198/210tick。セット内12tick（0.2秒）、2発目から次セットまで36tick（0.6秒）。判定は重ならず、最大4本の予告が共存する。最後の判定が220tickで終わり、18tickの余韻後238tickでIdleへ戻る。刀の動作も8発の発射時刻から連続的に求める。 |
| 溜め大斬撃 | 48tickの位置調整後、初回予告192tick。ボスは振り抜くが突進せず、斬撃波を前方へ発射する。第1フェーズは1発、第2・第3は3発。発射間隔90tick、2発目予告30tick・3発目48tick、通常攻撃の発射240/330/420tickを維持。予告中は現在のターゲット方向を追い、4tick前に固定。波は24px/tick、進行方向64px×直交方向480px、通常90tick存続。最後の波が消えてから余韻18tickを置き、次の主攻撃へ進む。 |
| 格子→大斬撃 | 開始0tickにターゲット中心・全身の当たり判定を一度記録し、ボス位置と初弾の方向を固定する。同時に長い168tickの波予告を開始。飛翔に要する整数tickをFとし、波発射168、基準地点への最初の接触168＋F、格子発動108＋F、格子予告開始24＋Fと逆算する。格子は84tick予告・12tick判定、2520px四方・間隔180px・縦横15本・線幅68px・隙間112pxを維持。第2・第3の後続波は258/348tick発射で、通常どおり直前追尾する。 |
| 第2フェーズ横断斬撃 | 左右900pxの構え位置へ48tick移動→54tick予告→18tickで1800px突進→余韻18tick、左右交互に3回。現在位置・速度の追跡後、発動18tick前に方向を固定して叫び声を鳴らす。0.3秒の反応時間を経て突進する。平均100px/tick、ピーク150px/tick。ダメージは116×170px本体が実際に通過した部分だけ。斬撃帯・残像・予告線は無害。 |

第2フェーズの元の3攻撃には、主斬撃開始の18tick後から鬼火が現れる。元の発生機会2回に対して3群出すため、Fight内で1群／2群を交互に予約し、追加群は18tick遅らせる。二連斬撃は1セットを元の1機会と数えるため、4セットで計6群（旧4群）。大斬撃3発全体／格子と追撃全体はそれぞれ元の1機会とし、連撃の追加と鬼火倍率を二重に掛けない。横断には従来どおり鬼火を足さない。鬼火は発生地点から30tick（0.5秒）、3.5px/tickで放射状に拡散する。この間は無害で、プレイヤー方向への補正はしない。群ごとにランダムな回転、各方向に±0.18radの揺らぎを加える。その後は命中可能となり、現在のボスのターゲットへ、目標速度4.5px/tick・毎tickの速度補間率0.035で緩やかに追尾する。追尾区間は180tick。発生から最大3.5秒で消える。

二連斬撃の鬼火は1群最大3発、溜め・格子は1群最大5発を維持。発生地点は主攻撃の予告時点のボス位置に固定する。まだ表示されていない予約分を含め、鬼火の同時上限を12→18発とし、残弾が多い場合は追加数を減らす。この上限が優先されるため、混雑時の実発生数は理論上の1.5倍未満になる。主攻撃の枠は別に確保し、格子30本＋斬撃波用3枠＋鬼火18発＝最大51個。フィールドは追加Projectileを使わない。予告のためだけの別Projectileは作らない。前の鬼火が次の剣撃中に残る追加回避は意図した挙動。同じ連撃内では前の波が飛翔中に次の予告を出す場合があるが、最後の波が消えるまで次の主攻撃は始めない。

格子だけの幾何学的な隙間は1〜4人分を確認するが、鬼火を含めた実戦の避けやすさは試遊で調整する。通常移動よりダッシュで安定して抜けることを目指す溜め斬撃も、Calamityの実装備での実測は別途必要。

## フェーズと主要調整箇所

`Content/Encounters/GhostSamurai/GhostSamuraiRules.cs` に数値を集約する。基準HPは2,400,000、防御160。Expert系の人数補正は通常のNPCフックを使い、人数ごとに基準HPの55%を加算する。最終DPSバランスの確定値ではない。

| 項目 | 現在値／編集する定数 |
|---|---|
| HP境界 | `Phase2Threshold = .66`、`Phase3Threshold = .33` |
| 移行休止 | `TransitionTime = 90` tick。攻撃を全消去し、無敵の青い輪を表示 |
| 攻撃間隔・余韻 | `AttackIntervalPhase1 = 36`、`AttackIntervalPhase2 = 24`、`RecoveryTime = 18` tick |
| 二連斬撃 | `SlashWarning = 54`、`DirectionalSlashInterval = 12`、`DirectionalPairInterval = 48`、`DirectionalPairCount = 4` |
| 溜め斬撃 | ChargeAimTime = 48、ChargeWarning = 192、ChargeSecondWarning = 30、ChargeThirdWarning = 48、ChargedSlashInterval = 90、AimLockLead = 4。SamuraiWaveRules の ChargedSlashWaveSpeed = 24、ChargedSlashWaveWidth = 64、ChargedSlashWaveHeight = 480、WaveLife = 90 |
| 格子 | GridWarning = 84、GridWidth = GridHeight = 2520、GridSpacing = 180、GridHalfWidth = 34、GridFollowStart = 0、GridFollowWarning = 168、GridToChargedSlashHitInterval = 60。縦横15本 |
| 横断 | `DashDistance = 1800`、`DashStandOff = 900`、`DashApproach = 48`、`DashRetreatSpeed = 38`、`DashWarning = 54`、`DashLive = 18`、`DashShoutDelay = 18` |
| 鬼火 | `WispBursts`、`WispBurstInterval = 18`、`WispDelay = 18`、`SpreadDuration = 30`、`SpreadSpeed = 3.5`、`HomingSpeed = 4.5`、`HomingStrength = .035`、`WispLife = 180`、`MaximumWisps = 18` |
| 生ダメージ | 斬撃260／溜め380／格子280／鬼火200。通常の防御・軽減処理に入力する |

HP33%以下のPhase3では既存4攻撃に円形攻撃を追加し、直前の攻撃を除いて選ぶ。致死攻撃でも移行前はHP1で保持し、各フェーズ境界を順に通す。

## 第3フェーズ：内外円形攻撃

ユーザーの円形イラストに沿い、攻撃開始時のターゲット位置を一度だけ記録する。全4段で中心は動かない。赤い内側円の半径は **240px**、青い外側は **240〜2800px** の有限ドーナツ。今回の広域案24000pxから縮小した。フィールドの対角線は約2795pxなので、円の中心がフィールド端にある場合も全域を覆い、外周へ逃げ切る場所を作らない。表示はフィールド境界でクリップし、外側の人を攻撃対象にしない。有限の円の判定を維持し、正規の回避は内側へ戻ることとする。境界に体が重なる場合は命中するため、体全体を安全側へ移す。

| STEP | 危険区域 | 予告開始／発動／判定終了（攻撃開始からのtick） | 表現 |
|---|---|---|---|
| 1 Inner Slash | 内側円 | 0 / 36 / 48 | 赤い円と斜線→鋭い直線斬撃 |
| 2 Outer Slash | 外側ドーナツ | 60 / 96 / 108 | 青い輪と斜線→広域の直線斬撃 |
| 3 Inner Kamaitachi | 内側円 | 120 / 156 / 180 | 赤い円と曲線→内部で回る刃の風 |
| 4 Outer Kamaitachi | 外側ドーナツ | 180 / 216 / 240 | 青い輪と曲線→内側を空けた刃の風 |

発動間隔 Phase3CircleStepInterval = 60、各予告 Phase3CircleTelegraphTime = 36、通常斬撃12tick、Phase3KamaitachiDuration = 24tick（0.4秒）。最後の余韻18tick。かまいたちは飾り刃ごとではなく、表示された円／ドーナツ全域に持続判定を持つ。通常の50tick被弾間隔で同じ段の連続多重ヒットを防ぐ。内外半径は Phase3CircleInnerRadius / Phase3CircleOuterRadius。主攻撃用Projectileは予約込み4個で、円周の刃ごとに生成しない。鬼火の発生回数・上限は維持し、この新行動へ追加群は足さない。

### 斬撃波、格子の到達時刻、突進の合図

通常の斬撃波は発動4tick前まで現在のターゲット方向を追い、発射後は旋回しない。ボス本体に大斬撃の接触ダメージを付けず、飛ぶ64×480pxの長方形だけが標準Projectileダメージを持つ。予告の破線は飛翔経路、実体の縁は現在の判定範囲。既存の無敵時間・回避フックを使って接触時に抜ける狙いであり、無敵時間を持たないダッシュへ新たな無敵を与える処理はない。

格子後の初弾だけは前倒し予告と正確な到達時刻を両立するため、開始時のターゲット位置と全身の矩形を固定基準にする。24px/tickの離散移動と波の先端を含む矩形衝突から、最初に基準の全身矩形へ触れるFを求める。格子発動からその到達までは**厳密に60tick**。移動したプレイヤーへ必ず命中させる意味ではなく、移動後の実際の接触時刻は変わる。遠距離の基準では初弾の寿命をF＋24tickまで延長し、到達前に消さない（通常90tick以上、対象距離4000px以内で上限210tick内）。

例：ボスから水平方向900px、通常20×42pxのターゲットではF＝36。攻撃開始0に168tickの波予告、60に84tickの格子予告、144に格子判定開始、156に格子判定終了、168に刀の振り抜きと波発射、180に振り抜きの余韻へ、204に波の先端が基準の当たり判定へ到達する。**204−144＝60tick**。全員への到達を同時に強制せず、選ばれた1人の基準を使う。

横断突進だけは現在位置・速度から短い到達予測を毎tick更新する（速度上限48px/tick）。発動18tick前に完全固定して叫ぶ。以後は追尾せず、聞いてから横へ抜ける0.3秒を設ける。Runtimeが18tickの本体移動を116×170pxで掃引し、サーバーの既存Player.Hurt経路で一度だけ適用する。接近・予告・余韻・中断時や、遠くの斬撃帯は無害。実装備と通信遅延での避けやすさは試遊で確認する。

### 効果音

斬撃は全8連撃・格子・斬撃波各発・横断各発・円形4段（かまいたちを含む）の**発動時刻**に共通 SoundID.Item1（Volume .9、Pitch .25）を鳴らす。横断固定時の叫びは SoundID.ScaryScream（Roar_2、Volume .9、Pitch .2）。差し替え先は GhostSamuraiAudio の SlashSound / DashShoutSound。既存の3回の溜めチャイム Item4 は波予告に残す。

音の所有者はクライアント/SPの GhostSamuraiAudio.PostUpdateEverything だけ。Fight・音種・発動tickでまとめ、格子30本でも1回。最大4tickの小さな遅れのみ救済し、途中参加前や古いスナップショットの攻撃は再生しない。最終照準未受信なら叫び／発動音を待ち、古い照準を音で確定したように扱わない。専用サーバーはPlaySoundを呼ばず、終了・世界終了・Unloadで履歴を消す。

## 判定、同期、後始末

- 既存のFactory／Runtime登録・共通セッション排他・終端スナップショットを利用する。終端schemaは2/version1。共通packet IDは変更せず、SlashWaveのshape追加・16byteのフィールド境界追加・円形上限3000px・叫び固定時刻を含む通信版34を全員で使う。
- サーバー／SPが乱数、ロック位置、フェーズ、攻撃生成・時刻・後始末を所有する。斬撃波のみはユーザーの標準Projectile指定に従う [ADR-0023](../../adr/0023-ghost-samurai-native-wave-damage.md) の狭い例外：ネイティブProjectile.Damageで各ローカル参加者の無敵／FreeDodge／ConsumableDodgeを処理する。Runtimeの手動Hurt対象から波を明示的に除外し、二重被弾を防ぐ。その他は従来どおりサーバーが長方形／鬼火／内外円／本体掃引を判定してHurtInfoを送り、受信側は命中を再判定しない。
- ボスは `SendExtraAI` / `ReceiveExtraAI` でFight GUID、時計、状態、最大HPとフィールド中心X/Y・半幅・半高さ（計16byte）を同期。不正な数値・サイズ、同じFightの境界変更、サーバーへの逆方向更新を拒否する。15tickごとと状態変更時に `netUpdate`。クライアントの時計は表示と標準斬撃波の現在位置に使い、受信が止まると30tickで停止する。ネイティブProjectile更新がRuntimeのPostUpdateWorldより先に来るSP/server側だけ、波の処理時刻を前回Age＋1として同じtickに合わせる。
- 各Projectileに同じFight GUID、所有NPCスロット、固定予告開始／発射／終了時刻と幾何を付ける。ExtraAIは有限数値・方向・時刻・幅・所有者の上限を検証する。クライアント由来のProjectile情報はサーバーの戦闘に採用しない。
- 斬撃の初期幾何と時刻は不変の識別情報として維持し、現在の位置・方向・更新tick・ロックtickは `SamuraiSlashAim` の24byte完全状態で同期する。OnSpawnの段階で初期状態を設定し、初回SyncProjectileより後にロック時刻を差し替えない。動く予告は3tickごととロック時に送信。クライアントはローカルなターゲットから狙わず、受信した同じ幾何を描画する。別Fight／所有者／初期幾何／ロック時刻、古いtick、同tickの矛盾、範囲外データを拒否する。ロックの最終更新だけでも途中参加・中間更新欠落から復元できる。最終更新が届かないまま発射時刻を過ぎたクライアントは、古い位置に実斬撃を描かない。実通信下の猶予は別途確認する。
- 円形の中心・内外半径・全時刻は4個の不変Projectileへ同時に記録し、途中参加でも復元する。円形には可変照準を送らない。突進表示には24byte照準状態を使い、クライアント本体も受信した確定軌道と同じ時計から表示位置を求める。
- 鬼火の位置・速度・更新tickは `SamuraiWispMotion` の20byteの完全状態として同じExtraAIに付ける。Runtimeが1tickに1度、サーバーのターゲットから速度と位置を更新して命中判定する。6tickごとの送信を個体ごとにずらし、発生・追尾開始時にも送る。クライアントは受信した速度を最大6tickだけ表示用に外挿し、古いtick、異なるFight・所有者・幾何の更新を拒否する。ローカルなターゲット追尾や命中判定はしない。
- 移動制限は本人のModPlayerにだけ置く45tickの期限付き状態で、Fight GUID＋所有NPCインスタンスを照合する。サーバーが毎tick再認定し、クライアントは共通Replicaの同じ生存Fightと45tick以内のNPCスナップショットがある場合だけ予測補正する。切断・再初期化・死亡・終端で消し、別接続や再利用NPCに引き継がない。補助の標準移動送信は6tick、サーバー補正送信は最短12tick間隔。壁へ出ようとし続けても毎tick通信しない。
- 召喚要求は保持アイテム・生存・共通セッション・接続単位nonce・頻度を検証する。既存の順序付きスナップショットで古いセッションと終端後の再適用を拒否する。
- 勝利・退場・例外・世界終了時にRuntimeが所有Fightだけを掃除する。別NPCスロット再利用を識別子とインスタンスで区別する。前フェーズの鬼火も移行時に消す。
- 戦闘終了時は共通Coordinatorが終端`Cleanup`を先に、その後に同じSequenceでRevisionを進めた`Idle`を送信する。クライアントの鈴は`Idle`かつ本体不在で再使用できる。終端記録は保持し、古い戦闘への巻き戻りや後始末未完了中のサーバー側再召喚は許可しない。通知前に次戦闘が始まった場合も、前戦闘の終端を先に送る。
- このボスでは通常死亡を使う。Calamityのローカルプレイヤー専用回避／蘇生／アクセサリーフックをすべて再現するとは主張しない。HurtInfo経路の実際の死亡・軽減・同期確認は必要。敵対的なクライアントによる通常Terrariaの体力・移動通信の改変対策は今回追加しない。

### 再召喚の診断ログ

`GhostSamurai event=...`で`SummonRequested`、`SummonAccepted`／`SummonRejected`、`CombatStarted`、`PhaseChanged`、`CombatEnded`、受信側の`ClientLifecycle`／`ClientIdle`を記録する。サーバー拒否は失敗コードを残し、nonce・Fight・Sequenceで対応を追える。要求送信前に鈴が使用不可なら`SummonBlocked`が生存・待機状態・本体残存の理由を記録する（ローカルで最大2秒に1回）。これは開始・終了の診断であり、攻撃別DPS集計の実装ではない。

## 表示と素材

`Client/Encounters/GhostSamurai/GhostSamuraiVisuals.cs` が本体・二刀・斬撃帯・鬼火を描画し、音は専用 GhostSamuraiAudio がまとめる。本体はユーザーの最初の図を基に生成した、青白い骸骨の鬼面、角、露出した肋骨、紺の武者鎧と霊体の尾を持つ `Assets/Textures/GhostSamurai/GhostSamuraiAtlas.png`。本体と刀を握る腕を分離し、左右の腕を肩から回す。刀と手は同じ画像内にあり、構え・振り抜き・余韻は従来の攻撃時計に従う。図の文字、第三者素材、新しい音声は取り込まない。Textureのフォールバックと召喚アイコン・効果音はゲーム内の既存アセット参照を維持する。

素材管理はクライアント専用の `GhostSamuraiArt`。画像生成ツールが要求した透過PNGを返さなかったため、最終素材はマゼンタ背景で生成し、初回描画時だけ背景色を透過する。画像全体や元の共有アセットを変更せず、専用Textureを作成してキャッシュし、Unload時に破棄する。Dedicated Serverは画像要求・加工・描画を行わない。元画像の出自とSHA256は [素材台帳](../../../Assets/ATTRIBUTION.md) に記録する。

`GhostSamuraiVisuals` は全NPCで共有するGlobalNPCなので、画像キャッシュの参照はstaticとする。NPCごとの状態をこのクラスへ追加しない。`InstancePerEntity = false` のまま非staticフィールドを追加すると、tModLoaderの `ValidateType` がMod全体の読み込みを拒否する。GhostSamuraiHazardVisuals は個別の音時計を持たず、Projectileのスナップショットを参照する共有GlobalProjectileとする。

線にはMagicPixelの `(0,0,1,1)` 領域だけを使う。実ファイルは1×1000pxであり、以前の全画像を使う描画は線の太さを1000倍にしていた。予告・鬼火・移行リングが共有する線関数で修正する。

予告は暗い下地＋金色の縁と中心線、狙いの固定後は安定した淡い金色、発射時は青白い斬撃に切り替える。帯全幅の薄い塗りを保ち、縁の外側が判定幅を越えないよう内側へ寄せる。予告の明度は発射時刻まで滑らかに上げ、点滅・全画面フラッシュは加えない。格子の隙間は塗らない。鬼火の円にも暗い縁取りを加える。描画はサーバーの幾何・発射時刻を使い、独自の判定幅やターゲット追従を持たない。攻撃カーソルの1tick先行を補正し、刀の動作も同じ時刻に合わせる。オフライン検証とビルドは、実ゲームでの視認性・フレームレートの合格とは区別する。

## 検証と試遊

自動チェックはフェーズ境界・非連続選択・予告と判定・1〜4人分の112px格子隙間・鬼火・同期値の往復と不正入力を維持。変更部分は、距離0〜3950px・32方向で格子発動→基準矩形到達60tick、波の有限移動矩形・予告／終了時の無害、手動Hurtと本体接触の対象外、18tick固定猶予、音の重複／途中参加／履歴上限、召喚者中心の固定フィールド・1〜4人の全身補正・世界端・16byte境界スナップショットと円の全フィールド被覆を確認する。

ユーザーの試遊：P1/P2大斬撃で本体が突進しない、波に無敵時間を持つ装備で触れて抜けられる、P2横断は叫び→0.3秒→突進、格子後の二段階回避、P3の外→内→外→内。通常のダッシュ全てに無敵があるとは扱わない。マルチプレイは最終照準・音・波の位置を比較し、途中参加／移行／全滅後の再召喚で残留がないことを見る。GUI・試遊はユーザー担当。召喚地点からフィールドが動かないこと、四辺での移動／ダッシュ／テレポート補正、死亡／終了時解除、途中参加・再接続・同スロット再利用、円形の描画が外枠を越えないことも確認する。

API確認の根拠

2026-09-12にtModLoader `2026.07.3.0` の固定source `666f69962d3bdffde54fc14025f02634965b4e7c` を確認。Terraria1.4.4.9／.NET8／C#12／Calamity2.2.4＋Music2.1を維持する。

- [ModNPC.cs](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModNPC.cs): ExtraAIはSyncNPCに含まれ、送信がserver、受信がclient。AI自体は両側なのでauthorityの分離が必要。
- [Player.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Player.cs.patch): `Hurt(... out HurtInfo ...)` が防御・フックを計算し、`Hurt(HurtInfo, quiet)` が確定結果を適用する。ローカル専用の回避フックには上記の制約がある。
- [NetMessage.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/NetMessage.cs.patch)／[MessageBuffer.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/MessageBuffer.cs.patch): `SendPlayerHurt(int,HurtInfo,int)` とmessage117のdirect-Hurt受信経路を確認。第三者の実装コード・画像・音声は移植していない。

円形表示は GhostSamuraiCircleVisuals.cs が担当する。暗い縁＋赤／青の境界と薄い塗りで安全区域を残す。Reduced Effectsでは風の層と発動時の塗りを減らし、境界・判定は維持。元の本体と刀の素材は変更しない。

円形の薄い塗りと斜線は、ズームを含む表示範囲へクリップしてから描く。円周分割数は128〜1024に制限し、画面外の線は描画しない。外側の風は内側境界の近くで回し、避けるべき中央の穴を読めるようにする。範囲を広げてもProjectileは4個のまま。

API確認（2026-09-13）：tModLoader調査Skillに従い、固定source 666f69962d3bdffde54fc14025f02634965b4e7c の [ModSystem.cs](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModSystem.cs) と [Main更新フック](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Main.cs.patch) を参照。クライアントではPostUpdateWorldが走らないため、確定軌道の表示再生のみModNPC.AIで行う。コードの取り込みはない。実通信下の一致は未確認。
