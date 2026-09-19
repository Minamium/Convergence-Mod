---
doc_id: encounter.ghost-samurai.spec
document_type: spec
status: provisional
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-18
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
  - project.art-direction
---

# 幽鬼武者 / Ghost Samurai

ユーザー提供の「骸骨の鬼武者 × 紫の霊炎 × 二刀流」の画像と文章を基にした、別ボスの開発用実装。**Ghost Samuraiは開発中**で、Doll Raidの初期プロトタイプ完成とは別の進捗である。現在の実装・検証状態と既知問題は [STATUS](../../STATUS.md) を参照する。Doll Raidの戦闘・素材は変更しない。

## 呼び出しと戦闘範囲

作業台で **骨10個＋星5個 → 鬼武者の弔い鈴**。消費せず繰り返し使用できる。広い空間で呼び出す。今回はユーザーが選んだ召喚アイテム方式であり、集合・Ready・Raid Down/蘇生は付けない。フィールドは下記の召喚者足元基準で自動展開する。通常のHP・死亡を使う。討伐時にOboroを1本確定ドロップする。最終バランスと追加の進行条件は開発中。

保存上の識別子は `Convergence/GhostSamuraiSummon` を維持する。Modの無効化・読み込み失敗でUnloaded Itemになった場合、tModLoader標準の保存情報には元のMod名・アイテム名が残る。原因を修正してModを有効化し、同じキャラクター／ワールドを読み込み直すことで復元する経路を使い、独自の置換アイテムやセーブ編集は行わない。

サーバーが召喚した本人の player.Bottom を記録し、足元を下端中央として2560×1120px（160×70タイル）を左右と上へ展開する。中心Yは足元より560px上。サイズは変更せず、全サイズが世界境界内に収まらない場所では召喚を拒否する。地面探索・自動縮小・タイル変更はしない。

フィールド内の生存プレイヤーと後から入った人を対象とする。外にいる人を強制集合させず、ターゲットと被弾対象はフィールド参加者だけにする。移動・ダッシュ・フック・マウント・ノックバック・テレポート後の全身を内側へ無傷で補正する。補正先が地形に埋まる場合は、記録済みの直近の安全な内側位置を優先する。サーバーが補正し、ローカル参加者だけが同じ境界で予測補正する。無限飛行や地形変更は追加しない。

青白い外枠は全フェーズ固定。死亡・切断で本人の制限を解除する。生存参加者がゼロになったサーバー更新で即時敗北を確定し、旧3秒猶予を廃止する。対象者だけの死亡・退出なら他の生存参加者へ狙い直す。戦闘外の観戦者だけでは継続しない。

召喚要求は既存の EncounterCoordinator を使う。`EncounterKind.Boss` の独立した `ghost_samurai` 定義なので、既存Raidとの同時起動を同じ共通機構で拒否する。依存ライブラリは現行版を維持する。

## 状態と攻撃

`GhostSamuraiRuntime` が戦闘を所有し、`GhostSamuraiBoss : ModNPC` はダメージを受ける本体と同期用の表示情報と不変のフィールド境界を持つ。状態は `SamuraiPhase`、`SamuraiAttack`、`SamuraiBeat`、攻撃タイマーに分離する。

`Idle → Approach/Telegraph → Strike → Recovery → Idle` を基本とし、余韻18tickの後、第1フェーズ36tick（0.6秒）、第2・第3フェーズ24tick（0.4秒）の攻撃間隔を置く。次の主攻撃は前回の全斬撃・余韻を終えてから、前回を除いた候補からサーバーが選ぶ。第1フェーズは縦3連を加えた4候補、第2フェーズは正面大斬撃＋衝撃波を含む6候補、第3フェーズは円形を含む7候補。候補の配列位置と通信上の列挙IDを混同せず、既存IDは維持する。以下の時刻は60tick＝1秒。

| 攻撃 | 予告と処理 |
|---|---|
| 二連斬撃×4セット | 元の `DirectionalSlash` を維持し、各セットの2発目は刀の向きを90度変える。8発それぞれ現在のターゲット位置をロックして54tick予告→10tick判定。予告開始は0/12/48/60/96/108/144/156tick、発射は54/66/102/114/150/162/198/210tick。セット内12tick（0.2秒）、2発目から次セットまで36tick（0.6秒）。判定は重ならず、最大4本の予告が共存する。最後の判定が220tickで終わり、18tickの余韻後238tickでIdleへ戻る。刀の動作も8発の発射時刻から連続的に求める。 |
| 溜め大斬撃 | 48tick位置調整後、初撃予告192→132tick。P1は1発、P2/P3は3発。ユーザー確認により後半予告30/48tickを維持し、発射180/270/360tick、間隔90tick。発射4tick前まで追い、以後直進。本体は突進せず波のみ標準Projectileダメージ60。速度24px/tick、64×480px、通常寿命90tick。最終波終了後18tick余韻。 |
| 格子→大斬撃 | 格子30本、84tick予告・12tick判定、2520px四方・間隔180px・線幅68px・隙間112pxを維持。初弾最低予告168→108tick。格子と波の予告を重ね、格子発動＋60tickを到達目標として現在の全身矩形から発射時刻を逆算する。初弾も直前4tickまで追う。後続波は実際の初弾発射から90/180tick後。急移動時の制約は下記。 |
| 第2フェーズ横断斬撃 | 左右900pxの構え位置へ追従。開始24tickに予告・叫び、90tickに収束リング、98tickに狙い固定、102tickに突進。予告78tick／移動21tick／余韻18tick、左右交互に3回。位置＋上限48px/tickの速度から短時間予測し、突進後は旋回しない。1800pxの平均100→85.714px/tick、ピーク150→128.571（約14.29%減）。116×170px本体の実移動区間だけが接触判定、斬撃帯は無害。 |

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
| 溜め斬撃 | ChargeAimTime48、ChargeWarning132、ChargeSecondWarning30、ChargeThirdWarning48、ChargedSlashInterval90、AimLockLead4、ChargedSlashDamage60。波速度24、幅64、高さ480、WaveLife90 |
| 格子 | GridWarning84、GridWidth/Height2520、GridSpacing180、GridHalfWidth34、GridFollowWarning108（最低）、GridToChargedSlashHitInterval60、縦横15本 |
| 横断 | DashDistance1800、DashAttackSpeed1800/21、DashStandOff900、DashApproach24、DashRetreatSpeed38、DashWarning78、DashLive21、DashShoutDelay78、DashVisualCueTime12、DashAimLockTime4 |
| 鬼火 | `WispBursts`、`WispBurstInterval = 18`、`WispDelay = 18`、`SpreadDuration = 30`、`SpreadSpeed = 3.5`、`HomingSpeed = 4.5`、`HomingStrength = .035`、`WispLife = 180`、`MaximumWisps = 18` |
| 生ダメージ | 斬撃260／溜め60（テスト用、旧380）／格子280／鬼火200。通常の防御・軽減処理に入力 |

HP33%以下のPhase3では既存4攻撃に円形攻撃を追加し、直前の攻撃を除いて選ぶ。致死攻撃でも移行前はHP1で保持し、各フェーズ境界を順に通す。

## 独立ステート：縦3連と正面大斬撃＋衝撃波

ユーザー指定（2026-09-15）：縦3連はPhase 1から、正面大斬撃＋衝撃波はPhase 2から登場し、以降のフェーズにも残る。既存の溜め大斬撃／格子追撃とは別の攻撃候補。新しい2攻撃には追加の鬼火を重ねず、直前の鬼火は既存の寿命・上限に従う。狙う相手は既存のサーバー固定対象であり、生存中にランダム交代しない。死亡・退出時は既存の選び直しを使い、未固定部分と次の縦斬撃だけ新対象を追う。固定済みの列・正面・地面の高さは変えない。

### 縦3連 / TripleVerticalSlash

- 刀を頭上へ寄せて振り下ろす専用ポーズ。開始0/66/132tick、発生42/108/174tick、各判定8tick、3発目終了182tickから18tick余韻を経て200tickで終了する。発生間隔は一定の66tick（1.1秒）。
- 各発42tick（0.7秒）の予備動作を行い、現在対象のX座標を発生16tick前（約0.267秒）に固定する。フィールド上端から下端までの幅144pxの縦帯。最後の24tick（0.4秒）だけ薄い予測帯を出し、固定後は淡い金色、判定中は青白い実線に変わる。
- 左右の切り返し／ダッシュで列から離れる。幅20pxのプレイヤーが固定から毎tick6px横移動すると全身が外れる設計。これは地上速度の幾何例であり、装備・入力遅延下の回避成功を保証するものではない。
- 3回の発行番号を単調に進め、同tickを重複処理しても同じ発を再生成しない。4回目は発行できず、終了で番号・照準参照・固定フラグを解除する。

### 正面大斬撃＋衝撃波 / FrontalCleaveShockwave

- 左右斬りとして指定されたこの攻撃だけを変更する。開始時にフィールド中央X、下端−本体半高85px−裾を含む余白144px（下端より229px上）を構え位置にする。当たり判定116×170pxは変更せず、画像の裾と上下揺れを含む左右112px・上168px・下216px＋4px余白が地形／足場と重なる場合、中央Xを保ち16pxずつ上へ探す。全候補が塞がっていれば攻撃を発生させず通常選択へ戻る。フィールドの位置・サイズ・タイルは変更しない。
- 接近元・構え位置・床の高さはサーバーが決め、既存の23byte姿勢状態で同期。移動時間Mは max(18, ceil(1.5×移動距離/42)) tick。同じSmoothStepを全員が評価し、瞬間移動せず加速→最大42px/tick→減速→停止する。移動中の本体／この攻撃には判定がない。停止後に予告と構えを開始する。
- 停止から108tick（1.8秒）の溜め。最初18tickで刀を頭上に構え、18〜96tickで主刀の刃だけ1.0→2.1倍へ滑らかに拡大する。手・腕・鍔は元のサイズで、刃の根元を握り位置へ固定する。96tickの短い合図から12tick最大サイズを保持し、108〜114tickの6tick（0.1秒）で加速して振り下ろす。実際の斬撃判定とSword Swing Soundは停止から114tick、振り下ろし6フレーム目に一致する。
- 停止後24tickまでは対象へ向き、以後は固定する。固定から判定までは90tick（1.5秒）。既存の正面予告、金色の固定合図、視線方向の短い印を保持。構え中の巨大な刃と予告は無害。正面半円の半径2800px／判定12tick／生ダメージ260は維持し、中心の背後へ全身が移れば安全。構え位置が中央なので左右とも1280pxの空間を持つ。
- 停止から126tickで斬撃終了、同時に左右2個の衝撃波予告を生成。24tick（0.4秒）の猶予を挟み150tickから速度14px/tickで発射する。主刀は斬撃終了後24tickで元の姿勢・大きさへ戻る。波は同じ発射時計から48tick（0.8秒）かけて0.65→1.5倍へ成長し、基準80×48pxに対して52×31.2px→120×72pxとなる。生ダメージ200は維持。
- 衝撃波は下端をサーバーで固定した床の高さに保ち、上と左右へ拡大する。現在の幅・高さを同じShockGeometryから描画とサーバー判定へ渡し、Projectile.width/heightの変更による座標ずれは起こさない。予告は最大到達高さ72pxの通過帯で、発射後の明るい本体が現在の判定。84pxのジャンプで全身が最大波高より上に出る幾何例を検証する。
- 左右へ1個ずつの波は、各フィールド端から最大半幅60px先まで進み全体が抜けて消える。最後の波終了後18tickで通常選択へ戻る。波の発行イベントは1回だけ。非常時でも600tick上限で残存攻撃を解除して終了する。
- 床は構え位置の本体幅の下から地形／足場を読み、存在しなければフィールド下端を使用する。水平面を走り、起伏の追従・地形改変・飛行禁止は追加しない。平坦な足場で「中央下へ接近→背後→ジャンプ」を試遊し、段差・多段足場・通信下の回避余裕は別途確認する。

### 共有ルールと調整箇所

時間・幅・半径・速度は `SamuraiComboRules` に集約し、新ステート本体は `GhostSamuraiRuntime.Combos.cs` に分離。`SamuraiComboSnapshot` は発行番号、左右の向き、固定フラグ、接近元、構え位置、基準面を23byteで同期する。NPCの既存状態・タイマー・対象と合わせて読み、非有限数・不正フラグ・範囲外番号は拒否。接近位置は同じサーバー時刻から補間し、クライアントのプレイヤーを見て照準・地面・判定を作らない。

縦帯と正面半円は既存28byteの照準更新を再利用し、最終固定状態だけでも復元できる。衝撃波は不変の45byte幾何と時刻から位置を求める。攻撃単位の対象固定と既存の全滅処理を保持。新しい形状も同じGhostSamuraiAttackProjectile型なので、勝利・死亡・全滅・フェーズ移行・世界終了で同じFight所有の掃除に含まれる。新攻撃はサーバーの既存Hurt経路でのみ判定し、標準Projectileダメージとの二重適用を追加しない。

## 第3フェーズ：内外円形攻撃

円形は開始時のターゲット位置を4段とも固定する。内側半径240px、外側半径2800→5600px。有限ドーナツの外では判定せず、フィールド全域を覆う。表示はフィールド境界へクリップし、非参加者は被弾させない。正規回避は外→内→外→内で、体全体を安全域へ移す。

| STEP | 危険区域 | 予告開始／発動／判定終了（攻撃開始からのtick） | 表現 |
|---|---|---|---|
| 1 Inner Slash | 内側円 | 0 / 36 / 48 | 赤い円と斜線→鋭い直線斬撃 |
| 2 Outer Slash | 外側ドーナツ | 60 / 96 / 108 | 青い輪と斜線→広域の直線斬撃 |
| 3 Inner Kamaitachi | 内側円 | 120 / 156 / 180 | 赤い円と曲線→内部で回る刃の風 |
| 4 Outer Kamaitachi | 外側ドーナツ | 180 / 216 / 240 | 青い輪と曲線→内側を空けた刃の風 |

発動間隔 Phase3CircleStepInterval = 60、各予告 Phase3CircleTelegraphTime = 36、通常斬撃12tick、Phase3KamaitachiDuration = 24tick（0.4秒）。最後の余韻18tick。かまいたちは飾り刃ごとではなく、表示された円／ドーナツ全域に持続判定を持つ。通常の50tick被弾間隔で同じ段の連続多重ヒットを防ぐ。内外半径は Phase3CircleInnerRadius / Phase3CircleOuterRadius。主攻撃用Projectileは予約込み4個で、円周の刃ごとに生成しない。鬼火の発生回数・上限は維持し、この新行動へ追加群は足さない。

### 斬撃波、格子の到達時刻、突進の合図

通常の斬撃波は発動4tick前まで現在のターゲット方向を追い、発射後は旋回しない。ボス本体に大斬撃の接触ダメージを付けず、飛ぶ64×480pxの長方形だけが標準Projectileダメージを持つ。予告の破線は飛翔経路、実体の縁は現在の判定範囲。既存の無敵時間・回避フックを使って接触時に抜ける狙いであり、無敵時間を持たないダッシュへ新たな無敵を与える処理はない。

格子後初弾は不変の到達目標ArrivalTickを持つ。開始時に最低108tickの波予告と84tickの格子予告が両立する時刻を決め、格子発動60tick後を目標とする。現在のボス位置→対象全身矩形へ24px/tickで初接触する整数時間Fから、発射をArrivalTick−Fへ更新する。発射4tick前に位置・方向・発射時刻をまとめて固定する。通常波と追跡対象・照準・固定猶予は共通で、初弾だけ発射時刻も更新する。

例：水平距離900pxを維持する20×42pxの対象はF＝36。両予告0、格子発動84、格子終了96、狙い固定104、波発射108、基準矩形接触144で、144−84＝60tick。静止／同相対距離での移動は距離0〜3950px・32方向で検証。直前の遠距離テレポート・急移動・対象交代では、過去の発射や弾速増加を行わず、最低予告・4tick固定を優先するため到達が遅れる場合がある。発射後に相手が動けば実接触も変わる。GridWaveLockedログに格子発動・実発射・固定時の基準到達を記録する。

横断は位置・速度（上限48px/tick）から短時間予測を更新する。発射78tick前の叫びは準備合図。12tick前から本体へ収束する暗縁付き金色リングと目の強調を出し、4tick前に淡い金色へ変えて固定する。以後は旋回せず21tick横断。本体移動を掃引し、既存サーバーPlayer.Hurt経路で一度だけ判定。接近・叫び・予告・余韻と斬撃帯は無害。ボス側へダッシュしてすれ違う操作、装備固有回避・通信下の反応余裕は試遊で確認する。

### 効果音

全8連撃・格子・横断・円形4段（風を含む）は発動時にSlashSound＝SoundID.Item1（Volume .9、Pitch .25、MaxInstances6）。波はChargedSwingSound＝Item1（Volume1、Pitch−.3、MaxInstances3）を実発射時に鳴らす。叫びはScaryScream（Roar_2、Volume .9、Pitch .2、MaxInstances3）で、同じ突進の発射を基準に旧18→78tick前へ60tick早める。Item4の3チャイムは初期予告内の固定時刻で鳴らし、発射予定変更で再演しない。

音の所有者はGhostSamuraiAudio.PostUpdateEverythingのみ。Fight・音種・時刻で重複を抑え、格子30本は1音。同時の格子と波は別種として保持。4tick以内の遅着のみ救済し、参加前の履歴を再演しない。発動音は最終照準受信後、準備の叫びはロック前にも鳴る。終端Replicaで遅れたNPC削除を待たず攻撃表示と音を停止する。Dedicated Serverでは無音、終了／世界終了／Unloadで履歴解除。

## 判定、同期、後始末

- 既存のFactory／Runtime登録・共通セッション排他・終端スナップショットを利用する。終端schemaは2/version1。共通packet IDは変更せず、現在は縦斬撃・正面半円・地上衝撃波と23byteの専用ポーズ状態を含む通信版40を全員で使う。以前の波・16byteの境界・28byte照準と到達目標を保持する。円形上限は6000px。
- サーバー／SPが乱数、ロック位置、フェーズ、攻撃生成・時刻・後始末を所有する。斬撃波のみはユーザーの標準Projectile指定に従う [ADR-0023](../../adr/0023-ghost-samurai-native-wave-damage.md) の狭い例外：ネイティブProjectile.Damageで各ローカル参加者の無敵／FreeDodge／ConsumableDodgeを処理する。Runtimeの手動Hurt対象から波を明示的に除外し、二重被弾を防ぐ。その他は従来どおりサーバーが長方形／鬼火／内外円／正面半円／地上衝撃波／本体掃引を判定してHurtInfoを送り、受信側は命中を再判定しない。
- ボスは `SendExtraAI` / `ReceiveExtraAI` でFight GUID、時計、状態、最大HPとフィールド中心X/Y・半幅・半高さ（計16byte）を同期。不正な数値・サイズ、同じFightの境界変更、サーバーへの逆方向更新を拒否する。15tickごとと状態変更時に `netUpdate`。クライアントの時計は表示と標準斬撃波の現在位置に使い、受信が止まると30tickで停止する。ネイティブProjectile更新がRuntimeのPostUpdateWorldより先に来るSP/server側だけ、波の処理時刻を前回Age＋1として同じtickに合わせる。
- 各Projectileに同じFight GUID、所有NPCスロット、固定予告開始／発射／終了時刻と幾何を付ける。ExtraAIは有限数値・方向・時刻・幅・所有者の上限を検証する。クライアント由来のProjectile情報はサーバーの戦闘に採用しない。
- 斬撃の初期幾何と時刻は不変の識別情報として維持し、現在の位置・方向・更新tick・ロックtickは `SamuraiSlashAim` の28byte完全状態（発射tickを含む）で同期する。OnSpawnの段階で初期状態を設定し、初回SyncProjectileより後にロック時刻を差し替えない。動く予告は3tickごととロック時に送信。クライアントはローカルなターゲットから狙わず、受信した同じ幾何を描画する。別Fight／所有者／初期幾何、通常攻撃のロック時刻、古いtick、同tickの矛盾、範囲外データを拒否する。ロックの最終更新だけでも途中参加・中間更新欠落から復元できる。最終更新が届かないまま発射時刻を過ぎたクライアントは、古い位置に実斬撃を描かない。実通信下の猶予は別途確認する。
- 円形の中心・内外半径・全時刻は4個の不変Projectileへ同時に記録し、途中参加でも復元する。円形には可変照準を送らない。突進表示には28byte照準状態を使い、クライアント本体も受信した確定軌道と同じ時計から表示位置を求める。
- 鬼火の位置・速度・更新tickは `SamuraiWispMotion` の20byteの完全状態として同じExtraAIに付ける。Runtimeが1tickに1度、サーバーのターゲットから速度と位置を更新して命中判定する。6tickごとの送信を個体ごとにずらし、発生・追尾開始時にも送る。クライアントは受信した速度を最大6tickだけ表示用に外挿し、古いtick、異なるFight・所有者・幾何の更新を拒否する。ローカルなターゲット追尾や命中判定はしない。
- 移動制限は本人のModPlayerにだけ置く45tickの期限付き状態で、Fight GUID＋所有NPCインスタンスを照合する。サーバーが毎tick再認定し、クライアントは共通Replicaの同じ生存Fightと45tick以内のNPCスナップショットがある場合だけ予測補正する。切断・再初期化・死亡・終端で消し、別接続や再利用NPCに引き継がない。補助の標準移動送信は6tick、サーバー補正送信は最短12tick間隔。壁へ出ようとし続けても毎tick通信しない。
- 召喚要求は保持アイテム・生存・共通セッション・接続単位nonce・頻度を検証する。既存の順序付きスナップショットで古いセッションと終端後の再適用を拒否する。
- 勝利・退場・例外・世界終了時にRuntimeが所有Fightだけを掃除する。別NPCスロット再利用を識別子とインスタンスで区別する。前フェーズの鬼火も移行時に消す。
- 戦闘終了時は共通Coordinatorが終端`Cleanup`を先に、その後に同じSequenceでRevisionを進めた`Idle`を送信する。クライアントの鈴は`Idle`かつ本体不在で再使用できる。終端記録は保持し、古い戦闘への巻き戻りや後始末未完了中のサーバー側再召喚は許可しない。通知前に次戦闘が始まった場合も、前戦闘の終端を先に送る。
- このボスでは通常死亡を使う。Calamityのローカルプレイヤー専用回避／蘇生／アクセサリーフックをすべて再現するとは主張しない。HurtInfo経路の実際の死亡・軽減・同期確認は必要。敵対的なクライアントによる通常Terrariaの体力・移動通信の改変対策は今回追加しない。

### 再召喚の診断ログ

`GhostSamurai event=...`で`SummonRequested`、`SummonAccepted`／`SummonRejected`、`CombatStarted`、`PhaseChanged`、`CombatEnded`、受信側の`ClientLifecycle`／`ClientIdle`を記録する。サーバー拒否は失敗コードを残し、nonce・Fight・Sequenceで対応を追える。要求送信前に鈴が使用不可なら`SummonBlocked`が生存・待機状態・本体残存の理由を記録する（ローカルで最大2秒に1回）。これは開始・終了の診断であり、攻撃別DPS集計の実装ではない。

### 終了処理と2026-09-14の不具合

[0.2.75記録](../../evidence/2026-09-14-playtest-0275.json)にはこのボスの戦闘ログはないが、未初期化PlayerへのGetModPlayerによる世界終了例外がある。Runtime.Cleanupにも同じ全スロット参照があり、敗北・勝利の掃除を中断し得た。移動制限を受け取ったModPlayerだけを台帳へ登録し、同じFightだけ解除する方式へ変更する。空Player配列／未登録ModContent／未召喚でも不足情報を参照・生成しない。Clear・Disconnect・Initializeで台帳から除去し、世界／Mod終了は全解除する。

勝利・全滅とも既存Coordinatorの共通Cleanupへ到達する。斬撃、格子予告、波、鬼火、円形4段、横断演出は同じProjectile型をFight GUIDで走査して削除する。本体はFight＋NPCインスタンス一致時だけ削除し、移動制限とRuntime参照を解除。終端通知→Cleanup成功→新しいIdleを維持し、例外を握り潰して再召喚可能にしない。独立したbossActiveフラグは追加しない。

### ターゲット固定と被ダメージ

初期対象は召喚者。サーバーは参加者のactive/dead/ghostと接続世代を確認し、生存対象を距離で切り替えない。死亡・退出・接続世代変化時だけ生存参加者から最寄りを選ぶ。同slotへの再接続は旧ロックを継承しない。NPC.targetと2byte追加のLockedTargetをExtraAIで同期し、クライアントでは対象を選ばない。追跡中の波・横断・鬼火は新対象へ向け、既に固定／発射済みの攻撃や円形中心は曲げない。

Itemのplayer.whoAmI、Projectileのowner（Minion/Sentry含む）を使う。標準ModifyHitByItem／ModifyHitByProjectileで受信したLockedTargetと比較し、対象外のみHitModifiers.FinalDamageへ0.5倍を追加する。本人／Single Playerは1.0倍。所有者不明・無効／非activeなowner・ロック未確定は半減しない。防御・会心・貫通経路を維持する。NPCへの通常攻撃は攻撃者側の標準tMLフックで計算され、未受信の対象変更や第三者Modが直接StrikeNPCする攻撃まで再計算するものではない。

API確認（2026-09-14、固定source666f69962d3bdffde54fc14025f02634965b4e7c）：
[ModNPCフック](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModNPC.cs) と [FinalDamage](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/NPC.TML.Hit.cs) が倍率の根拠。[Player.TML.cs](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Player.TML.cs) の直接配列参照を踏まえ、後片付けでは既知インスタンスのみ使う。第三者実装の取り込みなし。

## 表示と素材

### Luminance presentation target

**Ghost Samurai本体・鬼火・二刀・斬撃・衝撃波・消散・Oboroにも、共通の[Luminance積極活用方針](../../ART_DIRECTION.md#luminance-presentation-policy)を適用する。** 紫の霊炎、髑髏兜、鎧、札輪、二刀流という採用済みの意匠を保ちながら、次の表現実装ではその場で生きて動く身体と時間変化する材質を目指す。

従来の12姿勢atlas＋画像全体の微小な傾きは、既存実装の出発点である。**そのコマ切替だけを、本体の動作・表現強化の完成形として扱わない。** PNGを廃止したり、採用済み原画を勝手に置き換えたりする方針ではない。必要な部位・マスク・pivot・中間動作を用意して活かす。単独の読み込み不具合修正などへ全身再制作を追加せず、依頼された表現面から適用する。

- **身体と二刀:** 胴・肩・肘・手首・刀の接続を保ち、構え→溜め→加速した振り抜き→反動→復帰を連続化する。Luminanceの補間や表示用stateを必要に応じて使い、可動部位のcurve/pivotはこのBossが持つ。刀光・鬼火の発生点をその動きへ追従させる。全身PNGのクロスフェードやコマ数増加だけで接続・連続性の確認を代用しない。
- **霊炎・札輪・材質:** ManagedShader等で紫の流れ、濃淡、輪郭の明滅、消散を局所的に動かす。装飾的な札・炎の遅れには、効果のある範囲でbounded Verlet/粒子を選ぶ。身体を見失う過剰な透明化や全画面フラッシュにしない。
- **刀光・衝撃波:** 採用済みの5枚の斬撃素材を材質入力として活かし、必要な攻撃ではPrimitiveRendererとshaderによる伸長・流動・減衰を組み合わせる。帯・円の穴・フィールド境界、Fire/End、突進本体だけの判定は維持する。背景が明るくても危険の輪郭を残す。
- **表示時計と後始末:** 現行の権威側attack/beat/snapshotを投影する。Luminanceの表示stateや装飾simulationから攻撃遷移・標的・hit座標を決めない。遅延snapshot、teleport、Phase変更、再召喚で残像・糸・粒子を古い位置へ残さない。撃破表現は報酬とcleanupを遅延させない。
- **確認:** [共通の完成基準](../../ART_DIRECTION.md#presentation-completion)に従い、変更した待機・予備動作・振り抜き・復帰・消散の短い動画または連続frameを確認する。刀と手の接続、コマ境界の跳び、霊炎の流動、予告と判定、通常/Reduced Effectsを読み取れる証拠にする。オフライン描画と実ゲーム/複数peerの確認は区別する。

この節は表現強化の目標・受入基準であり、既に実装済みという記録ではない。実装・実機確認の状態は [Status](../../STATUS.md) が持つ。

### Current artwork and renderer

`GhostSamuraiVisuals` からクライアント専用 `GhostSamuraiPresentation` を呼び、本体をパーツごとに描く。紫の霊炎、髑髏兜、鎧、札付きの輪という9月16日の造形を維持した `Assets/Textures/GhostSamurai/VioletRig.png` は、9個の独立素材（胴、頭、上腕、前腕、刀、輪、札、裾、鬼火）のAtlasであり、全身ポーズの切り替えには使わない。両腕・二刀・頭・裾・札・鬼火は独立した位置と角度を持つ。初期同期前にも基本姿勢を描き、生存中の本体は55%以上の不透明度から24tickで通常の明度へ移行する。Idle・移動・攻撃・Phase移行で本体を隠さない。旧 `VioletActions.png` / `VioletDissolve.png` と青白いAtlasは保持するが、現行の本体・撃破描画では使用しない。音は既存 GhostSamuraiAudio が管理する。

新Atlasは透過済みRGBAを `ImmediateLoad` で読み、元画像を変更せずUVでパーツを選ぶ。LuminanceのManagedShaderによる霊気・発光・侵食、PrimitiveRendererによる刀の軌跡、MetaballType/ManagedRenderTargetによる霧、VerletSimulationsによる札の揺れを組み合わせる。既存の緑背景素材を使う鬼火などは引き続き `SpectralSpriteCutouts` が管理し、仮画像のキャッシュを避ける。共有Textureは破棄せず、専用コピーは描画スレッドで破棄する。Dedicated Serverで画像要求・加工・描画を行わない。出自とSHA256は [素材台帳](../../../Assets/ATTRIBUTION.md)、選択した依存機能とAPI上の注意点は [調査記録](../../research/2026-09-18-ghost-samurai-luminance.md) が所有する。

動作は `SamuraiRigMotion` の溜め→4tickの静止→6tickの振り抜き→6tickの余韻→復帰で構成する。既存の発射時刻から構えを逆算し、連撃中は前の終点を保って次へつなぐ。LuminanceのCubic InOut/Out、状態間の短い角度補間を使い、攻撃の判定・発射時計・攻撃間隔は変更しない。待機中は上下4pxと小さい傾き、位相の異なる裾と鬼火を加える。移動時は傾きと遅れ、ダッシュ時は最大6体の薄い縮小残像と最大12pxの表示上の行き過ぎを加える。NPC座標・接触判定を動かさない。

刀の履歴は毎tick最大14件、札は5本×4節、霧は最大64個、局所的な揺れは最大4件。描画回数で履歴や霧を進めない。ReducedEffectsでは広いRibbon・Metaball・本体残像・揺れを抑え、細い刀の線と本体を残す。強い振りの揺れは既存ScreenShake設定と参加者限定を守る。SpriteBatchとGPUの設定は共通 `WorldGraphicsScope` で呼び出し元へ戻す。

被弾は短い白紫の発光・3px以内の表示上の後退・札と鬼火の乱れだけを加え、AIを停止しない。サーバー／Replicaが受理したVictoryのみ、96tickの無害な終幕（刀を下げる→胸の亀裂→札が散る→鎧と裾が霧へ変わる→兜と鬼火が消える）を描く。Phase移行の致死HitEffectでは終幕を始めない。表示状態は正確なFightとNPCインスタンスに結び付け、ワイプ・別Fight・退出・Unload時に履歴・霧・揺れを後始末する。報酬や戦闘終了を演出のために遅らせない。

`GhostSamuraiVisuals` は全NPCで共有するGlobalNPCなので、画像キャッシュの参照はstaticとする。NPCごとの状態をこのクラスへ追加しない。`InstancePerEntity = false` のまま非staticフィールドを追加すると、tModLoaderの `ValidateType` がMod全体の読み込みを拒否する。GhostSamuraiHazardVisuals は個別の音時計を持たず、Projectileのスナップショットを参照する共有GlobalProjectileとする。

線にはMagicPixelの `(0,0,1,1)` 領域だけを使う。実ファイルは1×1000pxであり、以前の全画像を使う描画は線の太さを1000倍にしていた。予告・鬼火・移行リングが共有する線関数で修正する。

予告は暗い下地＋金色の縁と中心線、狙いの固定後は安定した淡い金色、発射時は青白い斬撃に切り替える。帯全幅の薄い塗りを保ち、縁の外側が判定幅を越えないよう内側へ寄せる。予告の明度は発射時刻まで滑らかに上げ、点滅・全画面フラッシュは加えない。格子の隙間は塗らない。鬼火の円にも暗い縁取りを加える。描画はサーバーの幾何・発射時刻を使い、独自の判定幅やターゲット追従を持たない。攻撃カーソルの1tick先行を補正し、刀の動作も同じ時刻に合わせる。オフライン検証とビルドは、実ゲームでの視認性・フレームレートの合格とは区別する。

### 専用斬撃素材 — 2026-09-17

本体・鬼火は既存の紫を保ち、斬撃だけを白・青白・淡紫の刀光へ強化する。`GhostSamuraiSlashArt` は下記5枚の透過素材を既存の攻撃領域へ配置する。素材は `Assets/Textures/GhostSamurai/Slashes/`、すべて2172×724pxのRGBA。元画像を変更せず、UV・回転・幅・不透明度を描画時に指定する。

| 素材 | 対応する攻撃・見た目 |
|---|---|
| `NormalSlash.png` | 四方向の2連×4回、縦3連、Phase3 Inner/Outer Slash。細い鋭い刀光。円形では直線の刀傷を複数配置 |
| `HeavySlash.png` | 大斬撃・格子後の大斬撃波、左右斬りの正面大斬撃。明るい芯と厚い霊気を持つ曲線 |
| `GridSlash.png` | 縦横の格子。小さい裂け目を持つ細い直線。隙間を塗らない |
| `Kamaitachi.png` | Inner/Outer Kamaitachiの流れる風刃。左右斬り後の地面衝撃波にも高さ内の霊気として使用 |
| `DashFlash.png` | 高速移動の横一閃と本体に続く残光。経路全体は薄い装飾で、突進の判定は本体だけ |

新しい素材は本攻撃のFire以上・End未満だけ表示する。予告は従来の暖色線、円形予告は赤／青の内外識別と直線／曲線の区別を維持。斬撃の発光が弱まっても正確な危険範囲の輪郭・薄い塗りを残す。円形素材は画像の全幅を含めて外周・中央の穴・フィールドへ収め、正面大斬撃は背後へ出さない。斬撃波と地面衝撃波も現在の移動位置・矩形内に収める。

Reduced Effectsは発光濃度、風の刃数・層数、大斬撃の層数を減らす。この5素材の導入では新しいProjectile・粒子・タイマー・乱数・通信は追加しておらず、受信した位置と時刻だけから再生する。今後のLuminance材質・装飾の追加は上の表現目標と所有権・予算に従い、ゲーム判定や通信を増やさない。5素材は初回描画でImmediateLoadし、tModLoader所有のAssetを再利用。画素加工・Texture作成・毎フレームの画素読取りは行わず、Unloadで参照を解放する。Dedicated Serverは読み込まない。

## 検証と試遊

自動検証は既存攻撃境界に加え、全滅／一人生存／接続世代、対象別倍率、足元基準同サイズ境界、可変発射と最終ロック、同期値の往復・不正入力を対象とする。実パッケージのGlobal登録・召喚型・exact-Fight解除・未初期化Player/ModContentでの繰り返し世界／Mod終了をヘッドレス確認する。実ゲームでの終了成功や描画品質とは区別する。

試遊：ソロ死亡／2〜4人の一人死亡と全滅／討伐→再召喚、未召喚での世界出入り、途中退出・再接続。非対象が接近しても狙いが変わらず、本人／他人のItem・弾・Minion・Sentryの倍率が1.0／0.5になること。足元から上へ同サイズの枠、円形の外→内→外→内、波の直前追跡・低ダメージ・ダッシュ、格子後約1秒、早い叫びと直前リングを確認する。全員同じversion／protocol38を使う。GUI・ゲーム／サーバー起動はユーザー担当。

API確認の根拠

2026-09-12にtModLoader `2026.07.3.0` の固定source `666f69962d3bdffde54fc14025f02634965b4e7c` を確認。Terraria1.4.4.9／.NET8／C#12／Calamity2.2.4＋Music2.1を維持する。

- [ModNPC.cs](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModNPC.cs): ExtraAIはSyncNPCに含まれ、送信がserver、受信がclient。AI自体は両側なのでauthorityの分離が必要。
- [Player.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Player.cs.patch): `Hurt(... out HurtInfo ...)` が防御・フックを計算し、`Hurt(HurtInfo, quiet)` が確定結果を適用する。ローカル専用の回避フックには上記の制約がある。
- [NetMessage.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/NetMessage.cs.patch)／[MessageBuffer.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/MessageBuffer.cs.patch): `SendPlayerHurt(int,HurtInfo,int)` とmessage117のdirect-Hurt受信経路を確認。第三者の実装コード・画像・音声は移植していない。

円形表示は GhostSamuraiCircleVisuals.cs が担当する。暗い縁＋赤／青の境界と薄い塗りで安全区域を残す。Reduced Effectsでは風の層と発動時の塗りを減らし、境界・判定は維持。元の本体と刀の素材は変更しない。

円形の薄い塗りと斜線は、ズームを含む表示範囲へクリップしてから描く。円周分割数は128〜1024に制限し、画面外の線は描画しない。外側の風は内側境界の近くで回し、避けるべき中央の穴を読めるようにする。範囲を広げてもProjectileは4個のまま。

API確認（2026-09-13）：tModLoader調査Skillに従い、固定source 666f69962d3bdffde54fc14025f02634965b4e7c の [ModSystem.cs](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModSystem.cs) と [Main更新フック](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Main.cs.patch) を参照。クライアントではPostUpdateWorldが走らないため、確定軌道の表示再生のみModNPC.AIで行う。コードの取り込みはない。実通信下の一致は未確認。

## Oboro — 2026-09-16

幽鬼武者を倒すと、難易度にかかわらず共通の地面ドロップとして1本確定入手する。アイテムIDは`Convergence/Oboro`。通常の敵やボスにも使用できる。

- 左クリック長押しで、軽い振り下ろし→返し斬り→短く溜める強い振り下ろし。基礎攻撃力8400、攻撃速度補正前24/24/42tick、威力1/1/2.2倍。刃の射程560px、太さ34px、地形越しの命中なし。True Meleeで各段・敵の共有HP本体ごと1ヒット。最後の攻撃から90tick空くか武器を持ち替えると連撃をリセットする。
- 右クリックで残心300tick、防御力+30・移動速度+10%。通常斬撃の命中で敵ごと・使用者ごとに最大5個の朧痕。再入力か時間切れで自分の全朧痕を一度だけ消費し、別角度の幻影斬撃を同時に発生。1痕につき記録した基礎命中威力の0.7倍、通常Melee。死亡・退出は炸裂せず消去。
- 3段目はWraith Fire600tickを更新付与。防御計算を50%にし、毎秒200+最大HPの2%をnative lifeRegenへ加算。他のDoTと重複、水で消えない。分節NPCには共有本体1回分だけを適用。ボス用の割合ダメージ上限は追加しない。
- 持続時間10秒・確定1本ドロップは未指定部分の初期実装値。Earthより高い最終装備火力を目指した数値であり、装備込みの実測比較は未完了。API比較範囲は[調査](../../research/2026-09-16-oboro.md)。通信・生存期間は[ADR-0027](../../adr/0027-oboro-authoritative-weapon.md)。

### 朧の動作・残像 — 2026-09-17

振り始め→加速→鋭い振り抜き→余韻を、速度の切れ目がない曲線で接続する。1段目は振り下ろし、2段目は返し、3段目は長めの構えから強く振り下ろし、肩越しの戻しで1段目の構えへつなぐ。各段24/24/42tick、ダメージ・射程・判定幅・攻撃可能時間・各敵1ヒット・ドロップ・レア度は維持する。振りの途中の角速度と命中し得る角度の時刻は変わるため、装備込みの実測DPS同一を保証するものではない。ダメージ判定と実体刀身は同じ曲線を使用する。

紫の刀身残像、薄紫の月光状の刃先軌道、淡い霧を、実際に振った場所に残す。残像は最大14tickで減衰し、連撃の切れ目でも自然に消える。残像自体に判定はない。プレイヤーごと最大16姿勢を記録し、別の斬撃同士の軌道は接続しない。持ち替え・死亡・退出・接続世代変更・大きな位置飛び・ワールド終了で記録を消去する。攻撃を止めた場合は10tickで通常の持ち姿へ戻す。Reduced Effectsでは残像と軌道の描画量を減らし、実体の刀と判定は同じまま。

## 紫の外観・動作資料の採用 — 2026-09-16

ユーザーが追加した8枚の資料に合わせ、角の兜・鎧・背後の札輪・紫の霊炎・二刀流を採用。新しい12姿勢のatlasを、既存の待機・予備動作・斬撃・突進・円形攻撃の権威時計へ割り当てる。撃破後は提供された消散sheetを60tick表示するだけで、勝利・ドロップを遅らせない。本体・鬼火は紫を維持し、斬撃・衝撃波の装飾は上記9月17日の専用素材に従う。予告の暖色輪郭、円形攻撃の内外区別、ダメージ範囲と発生時刻は維持する。

Oboroも資料の金色の鍔、藍紫の刀身、紙の札、紫の鬼火へ変更。資料に含まれる別案の横一文字3段目、攻撃倍率、前進やコンボ加速は採用せず、前回指定の性能を維持する。見た目の初期確認は暗背景・明背景のオフライン合成で実施し、実ゲームの描画や協力プレイ確認とは分ける。
