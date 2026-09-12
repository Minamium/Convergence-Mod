---
doc_id: encounter.ghost-samurai.spec
document_type: spec
status: provisional
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-12
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

作業台で **骨10個＋星5個 → 鬼武者の弔い鈴**。消費せず繰り返し使用できる。広い空間で呼び出す。今回はユーザーが選んだ召喚アイテム方式であり、集合・Ready・専用フィールド・Raid Down/蘇生は付けない。通常のHP・死亡を使う。戦利品・進行条件・最終バランスは今回の範囲外。

保存上の識別子は `Convergence/GhostSamuraiSummon` を維持する。Modの無効化・読み込み失敗でUnloaded Itemになった場合、tModLoader標準の保存情報には元のMod名・アイテム名が残る。原因を修正してModを有効化し、同じキャラクター／ワールドを読み込み直すことで復元する経路を使い、独自の置換アイテムやセーブ編集は行わない。

通常の `NPC.target` / `TargetClosest` で現在の生存プレイヤーを追う。ターゲットが死亡・切断・遠距離になった場合に選び直す。生存プレイヤーが4000px以内にいない状態が3秒続くと退場する。その猶予中は攻撃を消して停止し、プレイヤーが戻る場合はインターバルから再開する。

召喚要求は既存の EncounterCoordinator を使う。`EncounterKind.Boss` の独立した `ghost_samurai` 定義なので、既存Raidとの同時起動を同じ共通機構で拒否する。依存ライブラリは現行版を維持する。

## 状態と攻撃

`GhostSamuraiRuntime` が戦闘を所有し、`GhostSamuraiBoss : ModNPC` はダメージを受ける本体と同期用の表示情報を持つ。状態は `SamuraiPhase`、`SamuraiAttack`、`SamuraiBeat`、攻撃タイマーに分離する。

`Idle → Approach/Telegraph → Strike → Recovery → Idle` を基本とし、余韻18tickの後、第1フェーズ36tick（0.6秒）、第2・仮第3フェーズ24tick（0.4秒）の攻撃間隔を置く。次の主攻撃は前回の全斬撃・余韻を終えてから、前回を除いた候補からサーバーが選ぶ。第1フェーズは3候補、第2・仮第3フェーズは4候補。以下の時刻は60tick＝1秒。

| 攻撃 | 予告と処理 |
|---|---|
| 四連斬撃 | 各回の現在位置をロックし、角度に小さな変化を加える。4回それぞれ54tickの予告→10tickの判定。間隔36tick。攻撃開始から0/36/72/108tickで予告し、54/90/126/162tickで斬る。次の予告が前の斬撃より18tick早く出るが、判定は重ならない。4撃目の判定終了172tickから18tickの余韻を取り、190tickでIdleへ戻る。刀の構え・振り抜きもこの発射時刻を使い、予告開始ごとにはリセットしない。 |
| 溜め大斬撃 | 初めの48tickで構え、その後は位置・向きを固定。幅300px・長さ1800pxを72tick予告し、予告開始・24tick・48tickの計3回警告音。12tickだけ斬る。 |
| 格子斬撃 | 36tickの構え→縦15本・横15本の予告84tick→12tickの判定。間隔180px、線幅40px、領域2520px四方。セル内に140px四方の隙間を残す。ターゲット周辺に固定し、世界端では完全な隙間を残すよう中心を補正する。同じ30個のProjectileが予告から判定までを担う。 |
| 第2フェーズ横断斬撃 | 左右の構え位置に36tickで移動→横断ラインを54tick予告→24tickで加減速しながら1300px横断→18tickの余韻。左右交互に3回。予告した帯が攻撃範囲で、体への接触判定はない。 |

第2フェーズの元の3攻撃には、主斬撃開始の18tick後に鬼火が現れる。鬼火は発生地点から30tick（0.5秒）、3.5px/tickで放射状に拡散する。この間は無害で、プレイヤー方向への補正はしない。群ごとにランダムな回転、各方向に±0.18radの揺らぎを加える。その後は命中可能となり、現在のボスのターゲットへ、目標速度4.5px/tick・毎tickの速度補間率0.035で緩やかに追尾する。追尾区間は180tick。発生から最大3.5秒で消える。

四連斬撃は各回最大3発、溜め・格子は最大5発。発生地点は主攻撃の予告時点のボス位置に固定する。格子拡大に伴って鬼火まで外周の遠方へ移り、追尾しても届かなくなることを避ける。まだ表示されていない予約分を含め、鬼火は同時12発までとし、残弾が多い場合は追加数を減らす。主攻撃の枠は別に確保し、格子30本＋鬼火12発＝最大42個で頭打ちにする。前の鬼火が次の剣撃中に残る追加回避は意図した挙動だが、前の主斬撃が次の攻撃に重なることはない。

格子だけの幾何学的な隙間は1〜4人分を確認するが、鬼火を含めた実戦の避けやすさは試遊で調整する。通常移動よりダッシュで安定して抜けることを目指す溜め斬撃も、Calamityの実装備での実測は別途必要。

## フェーズと主要調整箇所

`Content/Encounters/GhostSamurai/GhostSamuraiRules.cs` に数値を集約する。基準HPは2,400,000、防御160。Expert系の人数補正は通常のNPCフックを使い、人数ごとに基準HPの55%を加算する。最終DPSバランスの確定値ではない。

| 項目 | 現在値／編集する定数 |
|---|---|
| HP境界 | `Phase2Threshold = .66`、`Phase3Threshold = .33` |
| 移行休止 | `TransitionTime = 90` tick。攻撃を全消去し、無敵の青い輪を表示 |
| 攻撃間隔・余韻 | `AttackIntervalPhase1 = 36`、`AttackIntervalPhase2 = 24`、`RecoveryTime = 18` tick |
| 四連斬撃 | `SlashWarning = 54`、`DirectionalSlashInterval = 36`、`SlashLength`、`SlashHalfWidth` |
| 溜め斬撃 | `ChargeAimTime`、`ChargeWarning`、`ChargeHalfWidth` |
| 格子 | `GridWarning = 84`、`GridWidth = GridHeight = 2520`、`GridSpacing = 180`、`GridHalfWidth = 20`。縦横本数は各寸法と間隔から算出する `GridVerticalLineCount` / `GridHorizontalLineCount` |
| 横断 | `DashDistance`、`DashApproach`、`DashWarning`、`DashLive` |
| 鬼火 | `WispDelay = 18`、`SpreadDuration = 30`、`SpreadSpeed = 3.5`、`HomingSpeed = 4.5`、`HomingStrength = .035`、`WispLife = 180`、`MaximumWisps = 12` |
| 生ダメージ | 斬撃260／溜め380／格子280／鬼火200。通常の防御・軽減処理に入力する |

HP33%以下では `Phase3` 状態へ移行するが、第2フェーズの攻撃を続ける。正式な第3フェーズ追加時は `SelectNextAttack` の候補と Runtime の攻撃分岐を追加する。致死級の攻撃を受けても移行前ならHP1で保持し、各フェーズ境界を順に通す。

## 判定、同期、後始末

- 既存のFactory／Runtime登録・共通セッション排他・終端スナップショットを利用する。終端schemaは2/version1。共通packet IDは変更せず、鬼火の移動状態追加に伴い通信版は30。全員が同じ版を使う。
- サーバー／SPのみが乱数、ロック位置、フェーズ、攻撃生成と命中を決める。Projectileの標準接触ダメージは常に無効。Runtimeが予告と同じ長方形／鬼火の円で判定し、ネイティブ `Player.Hurt` で計算した `HurtInfo` をサーバーから送る。受信側は命中を再判定しない。
- ボスは `SendExtraAI` / `ReceiveExtraAI` でFight GUID、時計、状態、最大HPを同期。15tickごとと状態変更時に `netUpdate`。クライアントの補間時計は表示専用で、受信が止まると30tickで停止する。
- 各Projectileに同じFight GUID、所有NPCスロット、固定予告開始／発射／終了時刻と幾何を付ける。ExtraAIは有限数値・方向・時刻・幅・所有者の上限を検証する。クライアント由来のProjectile情報はサーバーの戦闘に採用しない。
- 鬼火の位置・速度・更新tickは `SamuraiWispMotion` の20byteの完全状態として同じExtraAIに付ける。Runtimeが1tickに1度、サーバーのターゲットから速度と位置を更新して命中判定する。6tickごとの送信を個体ごとにずらし、発生・追尾開始時にも送る。クライアントは受信した速度を最大6tickだけ表示用に外挿し、古いtick、異なるFight・所有者・幾何の更新を拒否する。ローカルなターゲット追尾や命中判定はしない。
- 召喚要求は保持アイテム・生存・共通セッション・接続単位nonce・頻度を検証する。既存の順序付きスナップショットで古いセッションと終端後の再適用を拒否する。
- 勝利・退場・例外・世界終了時にRuntimeが所有Fightだけを掃除する。別NPCスロット再利用を識別子とインスタンスで区別する。前フェーズの鬼火も移行時に消す。
- 戦闘終了時は共通Coordinatorが終端`Cleanup`を先に、その後に同じSequenceでRevisionを進めた`Idle`を送信する。クライアントの鈴は`Idle`かつ本体不在で再使用できる。終端記録は保持し、古い戦闘への巻き戻りや後始末未完了中のサーバー側再召喚は許可しない。通知前に次戦闘が始まった場合も、前戦闘の終端を先に送る。
- このボスでは通常死亡を使う。Calamityのローカルプレイヤー専用回避／蘇生／アクセサリーフックをすべて再現するとは主張しない。HurtInfo経路の実際の死亡・軽減・同期確認は必要。敵対的なクライアントによる通常Terrariaの体力・移動通信の改変対策は今回追加しない。

### 再召喚の診断ログ

`GhostSamurai event=...`で`SummonRequested`、`SummonAccepted`／`SummonRejected`、`CombatStarted`、`PhaseChanged`、`CombatEnded`、受信側の`ClientLifecycle`／`ClientIdle`を記録する。サーバー拒否は失敗コードを残し、nonce・Fight・Sequenceで対応を追える。要求送信前に鈴が使用不可なら`SummonBlocked`が生存・待機状態・本体残存の理由を記録する（ローカルで最大2秒に1回）。これは開始・終了の診断であり、攻撃別DPS集計の実装ではない。

## 表示と素材

`Client/Encounters/GhostSamurai/GhostSamuraiVisuals.cs` が本体・二刀・斬撃帯・鬼火・音を描画する。本体はユーザーの最初の図を基に生成した、青白い骸骨の鬼面、角、露出した肋骨、紺の武者鎧と霊体の尾を持つ `Assets/Textures/GhostSamurai/GhostSamuraiAtlas.png`。本体と刀を握る腕を分離し、左右の腕を肩から回す。刀と手は同じ画像内にあり、構え・振り抜き・余韻は従来の攻撃時計に従う。図の文字、第三者素材、新しい音声は取り込まない。Textureのフォールバックと召喚アイコン・効果音はゲーム内の既存アセット参照を維持する。

素材管理はクライアント専用の `GhostSamuraiArt`。画像生成ツールが要求した透過PNGを返さなかったため、最終素材はマゼンタ背景で生成し、初回描画時だけ背景色を透過する。画像全体や元の共有アセットを変更せず、専用Textureを作成してキャッシュし、Unload時に破棄する。Dedicated Serverは画像要求・加工・描画を行わない。元画像の出自とSHA256は [素材台帳](../../../Assets/ATTRIBUTION.md) に記録する。

`GhostSamuraiVisuals` は全NPCで共有するGlobalNPCなので、画像キャッシュの参照はstaticとする。NPCごとの状態をこのクラスへ追加しない。`InstancePerEntity = false` のまま非staticフィールドを追加すると、tModLoaderの `ValidateType` がMod全体の読み込みを拒否する。個別の鬼火表示に必要な時刻は、従来どおり `InstancePerEntity = true` の `GhostSamuraiHazardVisuals` 側に置く。

線にはMagicPixelの `(0,0,1,1)` 領域だけを使う。実ファイルは1×1000pxであり、以前の全画像を使う描画は線の太さを1000倍にしていた。予告・鬼火・移行リングが共有する線関数で修正する。

予告は暗い下地＋金色の縁と中心線、発射時は青白い斬撃に切り替える。帯全幅の薄い塗りを保ち、縁の外側が判定幅を越えないよう内側へ寄せる。予告の明度は発射時刻まで滑らかに上げ、点滅・全画面フラッシュは加えない。格子の隙間は塗らない。鬼火の円にも暗い縁取りを加える。判定幅・予告時間・発射時刻・フェーズ・通信は変更しない。オフライン描画プレビューとビルドは、実ゲームでの視認性・フレームレートの合格とは区別する。

## 検証と試遊

自動チェックは新ボスのフェーズ境界・非連続選択・予告と判定・1〜4人分の格子隙間・突進軌道・鬼火・同期値の往復と不正入力。四連斬撃の予告重複／判定非重複／4撃目後の余韻／刀姿勢の連続性、拡張格子の外周と全ライン、鬼火の拡散中の非追尾・無害性・速度上限・滑らかな補正・期限・古い移動状態の拒否も対象とする。パッケージは `tools/dev.py build --native` で現在の編集元から作る。

Global型の登録を変更する場合は `tools/check-ghost-samurai-load.ps1 -PackagePath <Convergence.tmod> -TModLoaderPath <導入先>` で、パッケージ内の実DLLに対して導入済みtModLoaderの `ValidateType` を呼ぶ。`-ExpectOldFailure` は0.2.56の既知の失敗を再現する比較用。ゲーム・サーバー起動や完全なModロードは行わず、プレイヤー／ワールドのセーブにもアクセスしない。

今回の表示修正のユーザー確認は、巨大な線が消えていること、ボスの背景にマゼンタや四角が残らないこと、二刀の構えと振り抜き、明るい空・暗い地形での金色の予測線と格子の隙間、予告から青白い実攻撃への切り替え。前回のテンポ・鬼火調整の実戦評価と、通信遅延時・途中参加時の位置合わせ、格子中の処理負荷、フェーズ移行・退場後の残弾消去は引き続き未測定。GUI・ゲーム・サーバーはユーザーが起動する。

## API確認の根拠

2026-09-12にtModLoader `2026.07.3.0` の固定source `666f69962d3bdffde54fc14025f02634965b4e7c` を確認。Terraria1.4.4.9／.NET8／C#12／Calamity2.2.4＋Music2.1を維持する。

- [ModNPC.cs](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModNPC.cs): ExtraAIはSyncNPCに含まれ、送信がserver、受信がclient。AI自体は両側なのでauthorityの分離が必要。
- [Player.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Player.cs.patch): `Hurt(... out HurtInfo ...)` が防御・フックを計算し、`Hurt(HurtInfo, quiet)` が確定結果を適用する。ローカル専用の回避フックには上記の制約がある。
- [NetMessage.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/NetMessage.cs.patch)／[MessageBuffer.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/MessageBuffer.cs.patch): `SendPlayerHurt(int,HurtInfo,int)` とmessage117のdirect-Hurt受信経路を確認。第三者の実装コード・画像・音声は移植していない。
