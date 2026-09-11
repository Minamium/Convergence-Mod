---
doc_id: encounter.first-severance.doll-theater
document_type: spec
status: provisional
owners:
  - art
  - gameplay
last_reviewed: 2026-09-11
source_of_truth_for:
  - first_severance.doll_identity
aliases:
  - 不幸な人形劇
  - suspended doll
related_code:
  - Client/Encounters/FirstSeverance/FirstSeveranceDollPose.cs
  - Client/Encounters/FirstSeverance/FirstSeveranceDollVisuals.cs
  - Client/Encounters/FirstSeverance/FirstSeveranceDollCapture.cs
  - Content/Encounters/FirstSeverance/Actors/FirstSeveranceDollAttendant.cs
  - Assets/Textures/NPCs/DollTheater
related_docs:
  - encounter.first-severance.visual
  - project.art-direction
  - project.asset-pipeline
---

# 不幸な人形劇 — 少女人形の提示仕様

## 方針と範囲

2026-09-11のユーザー提供三面コンセプト画像を最重要の方向性資料とする。白髪・黒ゴスロリ・球体関節・雑な懸架・斜め姿勢を持つ、**心があるのに戦わされる巨大な少女人形**。空虚な顔と不均衡な重量感を優先する。BGM「不幸な人形劇」の悲劇性に合わせるが、曲の作者によるキャラクター設定とは主張しない。

Orchis的な白髪／黒衣／人形性、Avatar of Emptiness的な非対称の巨大な懸架は方向性のみ。固有の顔、衣装、構図、コード、素材は転載しない。

**同日の追加指示が優先：NPCの顔と全身をそのまま巨大化させない。腕の人形造形は維持する。** 旧Bossの冠・尖った胴の拘束フレーム・長い裾状の装甲を再利用して異形の外形を戻す。顔は小さくして冠の奥に置き、非対称の装甲で一部を遮る。白髪・黒衣は拘束された内側の少女を示す断片。完全なベル形スカートと両脚を揃えた巨大NPCの輪郭は避ける。Phase Iは特に露出を抑え、殻の前へ巨大な顔や腕全体を重ねない。

今回の変更は提示と会話NPC。RaidのID・HP・フェーズ条件・攻撃座標／予告時間・Ready手順・蘇生・音楽／SFXは変更しない。NPCは仲間でも追加の参加者でもなく、ソロ調整や戦闘補助をしない。

## 各状態

| 状態 | 画面で伝える内容 | 実装 |
|---|---|---|
| 戦闘前 | 台に静かに立つ等身大の白髪少女。黒い多段フリル、袖、ボンネット、人工的な膝、伏し目。短い会話ができる | `FirstSeveranceDollAttendant`。32×52の2フレーム、微小な揺れと不等間隔のまばたき。歩行・町の住居AIは付けない |
| 最初の準備演出 | 完成した殻が先に中央にある。台の少女がばらばらになる → 短くばらけて留まる → 破片が加速して殻の中央へ吸い込まれる | `DrawPreparation`／`DollCapture`。既存Deployment期間の中で完結。殻の中心・サイズは固定。無傷のNPCを持ち上げたり、新しい殻を左右から閉じない |
| Ready待機 | 殻への取り込みが終わり、縁からごく少量の指先・髪が見える | 大きな説明文は足さない。既存READY集計と無音準備を維持 |
| Phase I | ほぼ全身が殻の中にある。少しだけ覗く指・髪で内部の存在を示す | 腕と髪を殻の後ろへ描く。手前に顔を描かず、肩・上腕・前腕の大部分は殻で隠す |
| I → II | 内部の腕が拘束をこじ開け、殻が引っかかってから開く。冠・胴の拘束フレームが抜け、腕と長い残骸が崩れた姿勢へ展開 | 既存eclosionの8分割ヒンジ／時間曲線を流用。コンパクトな封入姿勢から連続的に接続し、髪と首は遅れて追従 |
| Phase II | 旧Bossの異形の冠・胴を外形の主体にし、白髪と小さい顔が内部に残る。受け入れ済みの球体関節の腕は維持 | 固定傾斜＋小さな非同期の揺れ。吊り糸が肩・手首・腰に実際に接続。顔の巨大なNPC立ち絵化をしない |
| Phase III以降 | 遠景へ下がった同じ少女と、空間内の左右の巨大な人形腕 | 遠隔腕の既存攻撃軌道／発射口は維持し、肩・前腕・手だけ人形素材へ変更。遠景の姿は追加の当たり判定ではない |
| Final／勝利 | ドレス・四肢が先に形象を失い、最後まで少女の顔が残る。斜め裂け目へ引き込まれて消える | 既存dissolution/riftを新規パーツに適用。顔の事前分裂を弱める。敗北・cancelで勝利を偽装しない |

完全直立、堂々とした威圧ポーズ、怒り顔、肉塊、少女の痕跡を全て消したメカへの置換、過度な性的／萌え的強調は避ける。「飛翔」より**保持されている重量**を見せる。

## 素材とコードの分担

少女素材は `Assets/Textures/NPCs/DollTheater/`。旧 `Assets/Textures/NPCs/NullCantorRigAtlas.png` の既存UVも合成する。今回PNGそのものは変更せず、パーツ選択・配置・遮蔽で再構成。生成高解像度原本は外部に保持する。

| 素材 | 構成／意図 |
|---|---|
| `DollAttendant.png` | 32×104、縦2フレーム。小さな会話NPCの輪郭と開眼／閉眼。立ち位置と足の基準は共通 |
| `DollRigAtlas.png` | 384×384、128角の3×3。頭／胴／裾／上腕／前腕／手／腿／下腿／髪束。球体関節の中心をpivotとしてコードで接続 |
| `DollCoffin.png` | 256×256。縦長の陶器棺、非対称の大きな破断、黒い鉄の補強と古びた金具。内部の黒は空洞の材質表現。全穴が透過窓という前提ではない |
| `DollHead.png` | 34×34。新規頭素材からの切り出し。バニラを含む選択中のBossバー用 |
| 旧 `NullCantorRigAtlas.png` | 冠・胴・裾状装甲・下方の拘束材を再利用。人形の顔より大きな異形の外形を作る。共用素材は上書きしない |

顔・衣装・球の材質・髪の塊・ひびはtexture。傾斜、親子関節、片側の張力、服／髪の遅れ、微振動、殻のヒンジと残骸、取り込み、Finalの分裂はcode。全身の大量のフレームや汎用アニメーションDSLは増やさない。

`FirstSeveranceDollPose`はTerrariaに依存しない**このBoss専用の提示座標とUV**で、プレビューとゲームが共有する。`DollCapture`も同様に共有し、32×52のNPCを28区画へ隙間なく分割。各片の分離→短い減速→加速吸入をaccepted deployment ageから計算し、中央で縮小・消失する。描画用actor／ゲーム乱数／保持particleを増やさず、ReducedEffectsでは横への変位と回転を抑え、残像を省く。

`DollVisuals`は読み取り専用の状態から描画し、殻・攻撃・終了処理は既存所有者が制御する。巨大な身体はdamage actorではなく、胸元の既存Core四隅が唯一の被ダメージ領域を示す。ゲーム中に触れられる位置を見た目に合わせて移さない。

## 舞台・Pylonの再解釈

- 四本柱と台、実フィールド、黒い外側は再利用。左右二本ずつ、外側が太く長い。斜めの支持棒や中央の浮いた機械紋章は復活させない。
- 横梁の先を巻上機にし、そこから可動肩・手首へ糸を伸ばす。糸の端点は固定、内側だけ張りを微動させる。準備時の柱と糸は同じ展開座標を使う。
- Pylonの既存金属かごは舞台の巻上装置。中央に巻かれたワイヤーを描き、Coreへ続く鈍い金属ケーブルで封印維持を示す。HP、個数、破壊条件は変えない。
- `HollowCathedral`は建築の背景として維持。遠方の抽象軌道を、不揃いな長さの吊り糸へ変更。霧／奥行きの揺れは継続し、攻撃予告より暗くする。
- 旧 `NullCantorRigAtlas`／`NullCantorShell`は削除しない。爪武器、採用済み破片エフェクト、Pylonの金属部品等の使用者が残る。全面一括上書きは禁止。

## Terrariaでの読みやすさ

1. NPCは原画をそのまま描かず、48px高の内容を52pxフレームに収める。白髪／黒いベル形の裾／膝／靴を1xでも見分ける。
2. 少女部分は128px単位・32色の暖灰／象牙／墨。旧フレームは既存の高密度素材をUVで再利用する。NPCの顔だけを大きく拡大して頭部全体にしない。
3. Pixel素材だけPointClampの独立したworld passで描く。ビーム／ハローは既存のLinearClampへ戻す。UI倍率・zoom・global `hideUI`を動かさない。
4. 白髪と肌は背景から分離するが、常時発光しない。黒衣の断片・球の明暗と外側の金属フレームを読み分ける。危険色はこれまでの攻撃予告が優先。
5. 等倍／2倍／画面距離の比較と、胴・肩・肘・手首が繋がる検査を行う。静止プレビューだけで実機視認性やFPSを断言しない。

素材export入口は `tools/prepare_doll_assets.ps1`、実際のposeからの比較画像は `tools/preview_doll_theater.ps1 -OutputPath <外部PNG>`。原本と最終prompt／hashは [素材recipe](../../../tools/asset_recipes/first_severance_doll.json)、利用記録は [Attribution](../../../Assets/ATTRIBUTION.md) を参照。画像生成は非決定的なので再生成の同一性は主張しないが、記録済み原本からの減色／切り出しは再実行可能。

## NPCの寿命と安全条件

- Server／Single Playerのみが、Idleの有効なCore付近にプレイヤーがいれば生成する。Coreごとに1体、全体最大4体、1秒周期の生成判定。
- Coreのtile座標をnative NPC AIで共有。町NPCや恒久保存対象にせず、敵に殴られない・攻撃しない・lootを出さない。Core削除／離れたIdle状態ではauthorityが除去する。ワールドに永続フラグは追加しない。
- Preparing／Activeでは実NPCは不可視・会話不可。参加者の準備描画が少女の取り込みを担当する。TileEntityより先にNPCを受信したclientは非表示で待ち、勝手に消さない。
- 再挑戦／終了後は台の少女へ戻す開発用演出。永続的な救済・死亡・物語進行は**未設計**。会話からRaid開始／Readyの別ルートは追加しない。
- [対象tModLoader commitのModNPC](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModNPC.cs)で、`CanChat`／`GetChat`、`NeedSaving`、`CheckActive`の呼び出し先と意味を確認。新規独自packetは不要。

## 検証と残課題

実装ファイルの分担：

- `Actors/FirstSeveranceDollAttendant.cs`：Core付近の会話NPCとauthority生成／除去。`Actors/FirstSeverancePrototypeBoss.cs`：バー用portraitのみ変更。
- `Client/Encounters/FirstSeverance/FirstSeveranceDollPose.cs`／`FirstSeveranceDollVisuals.cs`／`FirstSeveranceDollCapture.cs`：専用pose・旧新パーツ・懸架・破片吸入。
- 同Client内の `FirstSeveranceBossVisuals.cs`／`FirstSeveranceStageVisuals.cs`：既存状態から新しい身体／棺へ接続。`FoundationCoreVisuals.cs`：柱の巻上機と準備描画。`FirstSeverancePylonVisuals.cs`／`FirstSeveranceSky.cs`：巻上装置／遠景の吊り糸。
- `Localization/DollTheater/`：日英のNPC名と会話。`tools/prepare_doll_assets.ps1`／`preview_doll_theater.ps1`／`DollTheaterPreview.cs`：外部原本からのexport、alpha／寸法／色数検査と同じposeのプレビュー。
- `Tests/Convergence.DomainTests/DollPresentationTests.cs`：関節の接続と連続性、NPCの隙間ない分割と中央収束。既存projectへpure表示コードをリンクした対象検査。

自動検査は素材のalpha／寸法、共有関節の連続性・有限scale、native compile。実機の受入れは以下だけを今回の確認対象としてユーザーへ渡す。

- 台にNPCが立ち、会話できる。新規・既存Core、再入場、Core撤去、再挑戦で複製／残留しない。
- 2～4人で同じ既存棺→NPC分解・吸入→Ready→羽化を見る。途中snapshotでも殻の位置や完成状態が戻らず、破片が残らない。
- Phase Iは少量の露出のみ。Phase IIではNPC顔の単純拡大に見えず、旧拘束フレームと維持した腕が読める。Phase IIIの腕も維持。球体関節に隙間がない。
- 1x／2x距離、異なるzoomと107% UI、ReducedEffects、結果演出後のHUD復帰を確認。

現段階は新方向の実装版であり、実機での最終美術承認ではない。歩行、物語会話の増量、独自の柱／Pylon／背景の新規ピクセル素材化は後続候補。今回BGMや効果音の再制作はしない。
