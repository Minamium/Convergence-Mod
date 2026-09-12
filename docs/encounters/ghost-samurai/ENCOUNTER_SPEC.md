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

通常の `NPC.target` / `TargetClosest` で現在の生存プレイヤーを追う。ターゲットが死亡・切断・遠距離になった場合に選び直す。生存プレイヤーが4000px以内にいない状態が3秒続くと退場する。その猶予中は攻撃を消して停止し、プレイヤーが戻る場合はインターバルから再開する。

召喚要求は既存の EncounterCoordinator を使う。`EncounterKind.Boss` の独立した `ghost_samurai` 定義なので、既存Raidとの同時起動を同じ共通機構で拒否する。依存ライブラリは現行版を維持する。

## 状態と攻撃

`GhostSamuraiRuntime` が戦闘を所有し、`GhostSamuraiBoss : ModNPC` はダメージを受ける本体と同期用の表示情報を持つ。状態は `SamuraiPhase`、`SamuraiAttack`、`SamuraiBeat`、攻撃タイマーに分離する。

`Idle → Approach/Telegraph → Strike → Recovery → Idle` を基本とし、約1秒の攻撃間隔を置く。次の攻撃は前回を除いた候補からサーバーが選ぶ。第1フェーズは3候補、第2・仮第3フェーズは4候補。

| 攻撃 | 予告と処理 |
|---|---|
| 四連斬撃 | 各回の現在位置をロックし、角度に小さな変化を加える。4回それぞれ54tickの予告→10tickの判定。間隔88tick。 |
| 溜め大斬撃 | 初めの48tickで構え、その後は位置・向きを固定。幅300px・長さ1800pxを72tick予告し、予告開始・24tick・48tickの計3回警告音。12tickだけ斬る。 |
| 格子斬撃 | 36tickの構え→縦5本・横5本の予告84tick→12tickの判定。間隔240px、線幅40px、領域1440px四方。ターゲット周辺に固定し、世界端では完全な隙間を残すよう中心を補正する。 |
| 第2フェーズ横断斬撃 | 左右の構え位置に36tickで移動→横断ラインを54tick予告→24tickで加減速しながら1300px横断→30tickの余韻。左右交互に3回。予告した帯が攻撃範囲で、体への接触判定はない。 |

第2フェーズの元の3攻撃には、主斬撃開始の18tick後に鬼火が現れる。鬼火自体も30tick静止して向きを予告し、その後4px/tickで直進する。四連斬撃は各回3発、他は5発の扇形。追尾し直さず、最大180tickで消える。戦闘全体で有効な攻撃エンティティを32個以内に制限する。

格子だけの幾何学的な隙間は1〜4人分を確認するが、鬼火を含めた実戦の避けやすさは試遊で調整する。通常移動よりダッシュで安定して抜けることを目指す溜め斬撃も、Calamityの実装備での実測は別途必要。

## フェーズと主要調整箇所

`Content/Encounters/GhostSamurai/GhostSamuraiRules.cs` に数値を集約する。基準HPは2,400,000、防御160。Expert系の人数補正は通常のNPCフックを使い、人数ごとに基準HPの55%を加算する。最終DPSバランスの確定値ではない。

| 項目 | 現在値／編集する定数 |
|---|---|
| HP境界 | `Phase2Threshold = .66`、`Phase3Threshold = .33` |
| 移行休止 | `TransitionTime = 90` tick。攻撃を全消去し、無敵の青い輪を表示 |
| 四連斬撃 | `SlashWarning`、`SlashCadence`、`SlashLength`、`SlashHalfWidth` |
| 溜め斬撃 | `ChargeAimTime`、`ChargeWarning`、`ChargeHalfWidth` |
| 格子 | `GridWarning`、`GridSpacing`、`GridExtent`、`GridHalfWidth` |
| 横断 | `DashDistance`、`DashApproach`、`DashWarning`、`DashLive` |
| 鬼火 | `WispDelay`、`WispWarning`、`WispSpeed`、`WispLife` |
| 生ダメージ | 斬撃260／溜め380／格子280／鬼火200。通常の防御・軽減処理に入力する |

HP33%以下では `Phase3` 状態へ移行するが、第2フェーズの攻撃を続ける。正式な第3フェーズ追加時は `SelectNextAttack` の候補と Runtime の攻撃分岐を追加する。致死級の攻撃を受けても移行前ならHP1で保持し、各フェーズ境界を順に通す。

## 判定、同期、後始末

- 既存のFactory／Runtime登録・共通セッション排他・終端スナップショットを利用する。新しい終端schemaは2/version1。共通packet IDは変更せず、通信版は29へ上げる。
- サーバー／SPのみが乱数、ロック位置、フェーズ、攻撃生成と命中を決める。Projectileの標準接触ダメージは常に無効。Runtimeが予告と同じ長方形／鬼火の円で判定し、ネイティブ `Player.Hurt` で計算した `HurtInfo` をサーバーから送る。受信側は命中を再判定しない。
- ボスは `SendExtraAI` / `ReceiveExtraAI` でFight GUID、時計、状態、最大HPを同期。15tickごとと状態変更時に `netUpdate`。クライアントの補間時計は表示専用で、受信が止まると30tickで停止する。
- 各Projectileに同じFight GUID、所有NPCスロット、固定予告開始／発射／終了時刻と幾何を付ける。ExtraAIは有限数値・方向・時刻・幅・所有者の上限を検証する。クライアント由来のProjectile情報はサーバーの戦闘に採用しない。
- 召喚要求は保持アイテム・生存・共通セッション・接続単位nonce・頻度を検証する。既存の順序付きスナップショットで古いセッションと終端後の再適用を拒否する。
- 勝利・退場・例外・世界終了時にRuntimeが所有Fightだけを掃除する。別NPCスロット再利用を識別子とインスタンスで区別する。前フェーズの鬼火も移行時に消す。
- 戦闘終了時は共通Coordinatorが終端`Cleanup`を先に、その後に同じSequenceでRevisionを進めた`Idle`を送信する。クライアントの鈴は`Idle`かつ本体不在で再使用できる。終端記録は保持し、古い戦闘への巻き戻りや後始末未完了中のサーバー側再召喚は許可しない。通知前に次戦闘が始まった場合も、前戦闘の終端を先に送る。
- このボスでは通常死亡を使う。Calamityのローカルプレイヤー専用回避／蘇生／アクセサリーフックをすべて再現するとは主張しない。HurtInfo経路の実際の死亡・軽減・同期確認は必要。敵対的なクライアントによる通常Terrariaの体力・移動通信の改変対策は今回追加しない。

### 再召喚の診断ログ

`GhostSamurai event=...`で`SummonRequested`、`SummonAccepted`／`SummonRejected`、`CombatStarted`、`PhaseChanged`、`CombatEnded`、受信側の`ClientLifecycle`／`ClientIdle`を記録する。サーバー拒否は失敗コードを残し、nonce・Fight・Sequenceで対応を追える。要求送信前に鈴が使用不可なら`SummonBlocked`が生存・待機状態・本体残存の理由を記録する（ローカルで最大2秒に1回）。これは開始・終了の診断であり、攻撃別DPS集計の実装ではない。

## 仮表示と差し替え

`Client/Encounters/GhostSamurai/GhostSamuraiVisuals.cs` に骨格・鬼面・二刀・斬撃帯・鬼火・音をまとめる。ユーザーの図を設計参考として、既存のMagicPixelを線として使う独自の仮描画。新しい画像／音声ファイルは配布しない。Textureのフォールバックと召喚アイコン・効果音はゲーム内の既存アセットを参照するだけで、抽出・転載しない。

本体、刀、鬼火、斬撃、予告の描画をそれぞれ置き換えても、判定やAIを書き直す必要はない。広い帯は全幅を薄く塗り、境界も明示する。画面全体フラッシュ・カメラ操作は加えない。完成素材や実ゲームでの見やすさ・フレームレートの合格とは区別する。

## 検証と試遊

自動チェックは新ボスのフェーズ境界・非連続選択・予告と判定・1〜4人分の格子隙間・突進軌道・鬼火・同期値の往復と不正入力。パッケージは `tools/dev.py build --native` で現在の編集元から作る。

ユーザー確認は同じ版を読み込んだ複数人で、召喚の重複なし、四連斬撃の全予告、警告音3回、格子の隙間、HP66%/33%休止、鬼火、横断3回、死亡／切断／退場と再召喚時の残弾なし。遅延、途中参加、実装備でのダッシュ猶予と通常ダメージ・回避効果は未測定。GUI・ゲーム・サーバーはユーザーが起動する。

## API確認の根拠

2026-09-12にtModLoader `2026.07.3.0` の固定source `666f69962d3bdffde54fc14025f02634965b4e7c` を確認。Terraria1.4.4.9／.NET8／C#12／Calamity2.2.4＋Music2.1を維持する。

- [ModNPC.cs](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModNPC.cs): ExtraAIはSyncNPCに含まれ、送信がserver、受信がclient。AI自体は両側なのでauthorityの分離が必要。
- [Player.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Player.cs.patch): `Hurt(... out HurtInfo ...)` が防御・フックを計算し、`Hurt(HurtInfo, quiet)` が確定結果を適用する。ローカル専用の回避フックには上記の制約がある。
- [NetMessage.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/NetMessage.cs.patch)／[MessageBuffer.cs.patch](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/MessageBuffer.cs.patch): `SendPlayerHurt(int,HurtInfo,int)` とmessage117のdirect-Hurt受信経路を確認。第三者の実装コード・画像・音声は移植していない。
