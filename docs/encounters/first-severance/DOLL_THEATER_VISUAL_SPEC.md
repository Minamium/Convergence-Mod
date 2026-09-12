---
doc_id: encounter.first-severance.doll-theater
document_type: spec
status: provisional
owners:
  - art
  - gameplay
last_reviewed: 2026-09-12
source_of_truth_for:
  - first_severance.doll_identity
aliases:
  - 不幸な人形劇
  - suspended doll
related_code:
  - Client/Encounters/FirstSeverance/FirstSeveranceDollPose.cs
  - Client/Encounters/FirstSeverance/FirstSeveranceDollVisuals.cs
  - Client/Encounters/FirstSeverance/FirstSeveranceDollCapture.cs
  - Client/Encounters/FirstSeverance/FirstSeveranceDollSurface.cs
  - Client/Encounters/FirstSeverance/FirstSeveranceDollFrames.cs
  - Client/Encounters/FirstSeverance/FirstSeveranceShellSurface.cs
  - Content/Encounters/FirstSeverance/Actors/FirstSeveranceDollAttendant.cs
  - Assets/Textures/NPCs/DollTheater
related_docs:
  - encounter.first-severance.visual
  - project.art-direction
  - project.asset-pipeline
---

# 不幸な人形劇 — 少女人形の提示仕様

## 方針と範囲

公開表示名は [Encounter spec](ENCOUNTER_SPEC.md#identity-and-scope) に従う。「不幸な人形劇」とラクリモーサの名前はConvergence内の演出上の設定であり、BGM作者EigHtによるキャラクター設定・提携を意味しない。戦闘前の個体は「名もなき人形」のままとする。

頭割り・散開の円はプレイヤー／集合地点にだけ置く。Boss中央の同形照準・幾何学の詠唱円は表示しない（他行動内の頭割り／散開、散開中の補助射撃も含む）。身体の詠唱姿勢、赤い蓄光、瞬間の発射光と直撃／霧散は残す。ゲーム判定とプレイヤー側の殻片・円は変更しない。

2026-09-11のユーザー提供三面コンセプト画像を最重要の方向性資料とする。白髪・黒ゴスロリ・球体関節・雑な懸架・斜め姿勢を持つ、**心があるのに戦わされる巨大な少女人形**。空虚な顔と不均衡な重量感を優先する。BGM「不幸な人形劇」の悲劇性に合わせるが、曲の作者によるキャラクター設定とは主張しない。

Orchis的な白髪／黒衣／人形性、Avatar of Emptiness的な非対称の巨大な懸架は方向性のみ。固有の顔、衣装、構図、コード、素材は転載しない。

**同日の追加指示が優先：NPCの顔と全身をそのまま巨大化させない。腕の人形造形は維持する。** 旧Bossの冠・尖った胴の拘束フレーム・長い裾状の装甲を再利用して異形の外形を戻す。顔は小さくして冠の奥に置き、非対称の装甲で一部を遮る。白髪・黒衣は拘束された内側の少女を示す断片。完全なベル形スカートと両脚を揃えた巨大NPCの輪郭は避ける。Phase Iは特に露出を抑え、殻の前へ巨大な顔や腕全体を重ねない。

この文書が扱うのは提示と会話NPC。RaidのID・HP・フェーズ条件・攻撃座標／予告時間・Ready集計・蘇生・音楽は変更しない。追加指示で、Ready後のSpawnIntroだけを延長し、既存SFXを吸入の三つの節目へ接続する。舞台上のNPCは追加の参加者ではなく戦闘補助をしない。別途追加した[武器のDollミニオン](WEAPONS.md#doll-companion--the-unbroken-promise)とは別オブジェクトであり、NPCの吸入／復元ライフサイクルを共有しない。

2026-09-12の現行修正：殻本体は元の高精細な `NullCantorShell` を維持。Phase IIIの遠隔腕は旧型の黒い腱・象牙装甲・長い鉤爪。本体の人形腕は維持し、それが別の手を操る構図とする。胸の有機的な縦の裂け目は、機械的な円形ソケットと滑らかな黒い球体へ変更する。小さい顔・冠・人形腕・外形を保ち、Boss全体を機械へ戻さない。四角いコア四隅、×印、コア周囲の八角形は削除し、露出状態は材質の光で伝える。Phase II格子中の狙撃時は球面内に黒い無機質な窪みが連続的に開き、そこから魔法武器と共通の紫の光流を一本放つ。材質上の窪みは予兆時に素早く開き、短い保持・放出後に閉じる。攻撃の円形UI禁止は [Visual Spec](VISUAL_SPEC.md#stack-and-spread-verdicts) の方針を参照。

## 各状態

| 状態 | 画面で伝える内容 | 実装 |
|---|---|---|
| 戦闘前 | 台に静かに立つ等身大の白髪少女。黒い多段フリル、袖、ボンネット、人工的な膝、伏し目。短い会話ができる | `FirstSeveranceDollAttendant`。32×52の12フレーム。微小な呼吸、不等間隔のまばたき、伏し目、胸へ手を添える、両手を合わせる、小さなお辞儀。歩行・町の住居AIは付けない |
| 最初の準備演出 | フィールド・柱・空の既存殻が展開する。台の少女は無傷で残る | `DrawPreparation`は少女を分解せず、人形の指／髪も殻から出さない。既存Deployment期間とUI／境界を維持 |
| Ready待機 | 少女は台でまばたき・控えめな仕草。まだ殻へ取り込まれない | 既存READY集計と無音準備を維持。NPCは参加者ではなく、準備中は会話不可 |
| 全員Ready後のRaid開始 | 糸の張り → NPC分解 → 短い滞留 → 加速吸入 → 殻中央の発光／名前表示 → 戦闘 | SpawnIntroの正本は `EncounterPlan.Timing.SpawnIntroTicks`。現在10秒。`DollCapture`はaccepted ActionStartedTick～ResolveTickで進め、台から殻へカメラ追従。HUD非表示、文字は吸入後だけ。終了直前に殻内の指／髪が現れる |
| Phase I | ほぼ全身が殻の中にある。少しだけ覗く指・髪で内部の存在を示す | 腕と髪を殻の後ろへ描く。手前に顔を描かず、肩・上腕・前腕の大部分は殻で隠す |
| I → II | 内部の腕が拘束をこじ開け、殻が引っかかってから開く。冠・胴の拘束フレームが抜け、腕と長い残骸が崩れた姿勢へ展開 | 既存eclosionの8分割ヒンジ／時間曲線を流用。コンパクトな封入姿勢から連続的に接続し、髪と首は遅れて追従 |
| Phase II | 旧Bossの異形の冠・胴を外形の主体にし、白髪と小さい顔が内部に残る。受け入れ済みの球体関節の腕は維持 | 傾斜を保ちつつ、肩→肘→手首、首→髪へ異なる遅れで張力が伝わる。吊り糸は可動関節に接続。顔の巨大なNPC立ち絵化をしない |
| Phase III以降 | 遠景の人形本体が、左右に展開された旧型の異形腕を糸で遠隔操作する | 上腕・前腕は旧atlas／旧寸法。鉤爪は旧材質を基にした開閉16枚。手首・発射口・中央挟撃の軌道は維持。遠景本体の人形腕と取り違えない |
| Final／勝利 | ドレス・四肢が先に形象を失い、最後まで少女の顔が残る。斜め裂け目へ引き込まれて消える | 既存dissolution/riftを新規パーツに適用。顔の事前分裂を弱める。敗北・cancelで勝利を偽装しない |

完全直立、堂々とした威圧ポーズ、怒り顔、肉塊、少女の痕跡を全て消したメカへの置換、過度な性的／萌え的強調は避ける。「飛翔」より**保持されている重量**を見せる。

## 素材とコードの分担

少女素材は `Assets/Textures/NPCs/DollTheater/`。旧 `Assets/Textures/NPCs/NullCantorRigAtlas.png` の既存UVも合成する。共用PNGは上書きせず、新規連続フレームを隣接atlasへ追加。生成高解像度原本は外部に保持する。

| 素材 | 構成／意図 |
|---|---|
| `DollAttendant.png` | 32×624、縦12フレーム。開眼／半眼／閉眼と視線・手・お辞儀の描き分け。立ち位置と足の基準は共通 |
| `DollRigAtlas.png` | 384×384、128角の3×3。頭／胴／裾／上腕／前腕／手／腿／下腿／髪束。球体関節の中心をpivotとしてコードで接続 |
| `DollCoffin.png` | 256×256。初期人形棺案として保持。本体の殻としては不採用に戻し、裾の小さな残骸のみに使用。原本・PNGは削除しない |
| `DollHead.png` | 34×34。新規頭素材からの切り出し。バニラを含む選択中のBossバー用 |
| `RemoteClawFrames.png` | 896×1408、224×352の4×4。旧鉤爪が弛緩→開く→指節を曲げる→握る16のの描き分け。共通手首pivotで接続。握る絵の高さを自動で伸ばして開いた絵へ揃えない |
| `MechanicalRestraintFrames.png` | 960×1408、240×352の4×4。旧外形を保つ機械的な円形受け枠16枚。球中心を各セル(120,138)へ登録。旧有機的な `RestraintFrames.png` は保管し、現在の描画では使用しない |
| 旧 `NullCantorRigAtlas.png` | 冠・胴・裾状装甲・下方の拘束材を再利用。人形の顔より大きな異形の外形を作る。共用素材は上書きしない |
| `NullCantorShell.png` | 元の高精細な黒い金属・微細亀裂の殻。準備と戦闘の正本。閉じている間は元画像一枚、開く際のみ1024角の8枚へmask分割。比率・呼吸サイズ・分割所属は `ShellSurface` が所有 |

顔・衣装・髪の塊・ひび・鉤爪開閉・胸の受け枠の描き分けはtexture。傾斜、親子関節、片側の張力、服／髪の遅れ、微振動、殻のヒンジと残骸、取り込み、Finalの分裂はcode。中央の球体は専用のcode-native半球meshに連続回転する細い継ぎ目と環境反射を描く。球の材質は回り、光源側のハイライトは固定。描画fractionを使用し、16枚の画像を高速切替するだけにはしない。全身60枚の手描きアニメやフレーム間形状補間とは異なる。

`FirstSeveranceDollPose`はTerrariaに依存しない**このBoss専用の提示座標とUV**で、プレビューとゲームが共有する。`DollFrames`が追加atlasのフレーム／固定pivotを所有。`DollMannerisms`はNPCの23秒の非等間隔な待機動作と会話中の控えめな手の動きを選択。すべてsimulation tick基準で、NPCの位置・Ready・capture時刻は変えない。`DollCapture`も同様に共有し、32×52のNPCを28区画へ隙間なく分割。各片の分離→短い減速→加速吸入をaccepted SpawnIntro ageから計算し、中央で縮小・消失する。描画用actor／ゲーム乱数／保持particleを増やさず、ReducedEffectsでは横への変位と回転を抑え、残像・全面flashを省く。

`DollVisuals`は読み取り専用の状態から描画し、殻・攻撃・終了処理は既存所有者が制御する。巨大な身体はdamage actorではなく、既存の固定Coreが唯一の被ダメージ領域。四隅／×印は使わない。Phase III以降は遠景の身体とは別に、実際の前景Core位置へ同じ機械球を表示する。ゲーム中に触れられる位置を見た目に合わせて移さない。

### 連続した異様な動き

- 身体の常時動作はFight開始を基準にしたlocal simulation tick＋描画時のfraction。snapshot補正で揺れの位相を巻き戻さない。pauseでは進まない。攻撃の溜め／反動は従来のauthority時刻のまま。
- 胴の傾きだけで全身を回さず、左右異なる連続波形を肩・肘・手首・首・腰へ遅延して渡す。末端ほど移動幅を大きくし、共有pivotを崩さない。遠隔腕は肩／肘だけを動かし、攻撃元の手首座標と手の攻撃姿勢は維持。
- 髪・長い裾状の拘束材は根元固定のconnected texture meshで屈曲。通常12行／Reduced6行の共有境界を持つ三角形で、別々の短冊をずらして隙間を作る方式にはしない。素材原本を増やさず、分裂へ入る前に屈曲を収めて既存Final表現へ接続。
- `DollSurface`が小さな再利用vertex配列とeffectを所有。描画順を守ってworld passを一時的にflushし、同じGameViewMatrix／viewportへ描き、blend／sampler／raster／depthを復帰。Unloadでeffectを解放。武器用mesh bufferには触れない。
- ReducedEffectsではうねり／屈曲を18%に抑える。攻撃予告・ダメージ領域・HP・行動時間は動かさない。滑らかさやGPU負荷は実機確認と分けて扱う。
- 実フレームは鉤爪の溜め／衝撃に合わせた開閉と、胴の弛緩→素早い開き→滞留→回復。独立した攻撃用時計は作らない。ReducedEffectsでも攻撃姿勢は保ち、胴の常時ループを止める。

原本・prompt・固定pivotでの再exportは [0.2.52素材recipe](../../../tools/asset_recipes/first_severance_doll_frames_0252.json) を参照。前の8枚版の原本とrecipeも保存し、NPC以外の既存人形atlas・大殻・共用rigは上書きしない。

## 舞台・Pylonの再解釈

- 四本柱と台、実フィールド、黒い外側は再利用。左右二本ずつ、外側が太く長い。斜めの支持棒や中央の浮いた機械紋章は復活させない。
- 横梁の先を巻上機にし、準備／Phase Iでは四本の糸で殻を保持する。殻と留め点を同じ `ShellSurface.Suspension` で平行移動／回転し、巻上機側は柱に固定。重力側へ弛む糸と張った糸の差、片側の引っかかりを連続的に見せる。準備→開始で同じ時計／座標を使い、裂開時には揺れを収めながら既存ヒンジへつなぐ。全貌露出後は可動肩／手首へ接続する既存rigの糸を維持。
- Pylonの既存金属かごは舞台の巻上装置。中央に巻かれたワイヤーを描き、Coreへ続く鈍い金属ケーブルで封印維持を示す。HP、個数、破壊条件は変えない。
- `HollowCathedral`は建築の背景として維持。遠方の抽象軌道を、不揃いな長さの吊り糸へ変更。霧／奥行きの揺れは継続し、攻撃予告より暗くする。
- 旧 `NullCantorRigAtlas`／`NullCantorShell`は削除しない。爪武器、採用済み破片エフェクト、Pylonの金属部品等の使用者が残る。全面一括上書きは禁止。

## Terrariaでの読みやすさ

1. NPCは原画をそのまま描かず、48px高の内容を52pxフレームに収める。白髪／黒いベル形の裾／膝／靴を1xでも見分ける。
2. 少女部分は128px単位・32色の暖灰／象牙／墨。旧フレームは既存の高密度素材をUVで再利用する。NPCの顔だけを大きく拡大して頭部全体にしない。
3. 小さなNPC／その破片はPointClamp、回転・屈曲するBossと高精細殻はLinearClamp。Boss全体をnearest-neighborで段階的に動いて見せない。UI倍率・zoom・global `hideUI`を動かさない。
4. 白髪と肌は背景から分離するが、常時発光しない。黒衣の断片・球の明暗と外側の金属フレームを読み分ける。危険色はこれまでの攻撃予告が優先。
5. 等倍／2倍／画面距離の比較と、胴・肩・肘・手首が繋がる検査を行う。静止プレビューだけで実機視認性やFPSを断言しない。

素材export入口は `tools/prepare_doll_assets.ps1`、追加フレームは `tools/prepare_doll_frames.ps1`。比較画像は `tools/preview_doll_theater.ps1 -OutputPath <外部PNG>`。同じ出力先にcapture見本、実際の描き分け一覧 `authored-frames.png`、`motion/` の60Hz・4秒の連続表示見本を生成する。表示見本の240枚を新規の描き分け数と数えない。実GPU録画やFPS測定ではない。[初期recipe](../../../tools/asset_recipes/first_severance_doll.json)、[連続フレームrecipe](../../../tools/asset_recipes/first_severance_doll_frames.json)、[Attribution](../../../Assets/ATTRIBUTION.md) を参照。

## NPCの寿命と安全条件

- Server／Single Playerのみが、Idle／Preparingの有効なCore付近にプレイヤーがいれば生成する。Coreごとに1体、全体最大4体、1秒周期の生成判定。
- Coreのtile座標をnative NPC AIで共有。町NPCや恒久保存対象にせず、敵に殴られない・攻撃しない・lootを出さない。Core削除／離れたIdle状態ではauthorityが除去する。ワールドに永続フラグは追加しない。
- Preparingでは実NPCを可視のまま保持し、必要なら同じCore所有者が生成する。会話はIdleだけ。Activeで実NPCを隠し、参加者のSpawnIntro描画が取り込みを担当する。TileEntityより先にNPCを受信したclientは非表示で待ち、勝手に消さない。
- 再挑戦／終了後は台の少女へ戻す開発用演出。永続的な救済・死亡・物語進行は**未設計**。会話からRaid開始／Readyの別ルートは追加しない。
- [対象tModLoader commitのModNPC](https://github.com/tModLoader/tModLoader/blob/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModNPC.cs)で、`CanChat`／`GetChat`、`NeedSaving`、`CheckActive`の呼び出し先と意味を確認。新規独自packetは不要。

## 検証と残課題

実装ファイルの分担：

- `Actors/FirstSeveranceDollAttendant.cs`：Core付近の会話NPCとauthority生成／除去。`Actors/FirstSeverancePrototypeBoss.cs`：バー用portraitのみ変更。
- `Client/Encounters/FirstSeverance/FirstSeveranceDollPose.cs`／`FirstSeveranceDollVisuals.cs`／`FirstSeveranceDollCapture.cs`：専用pose・旧新パーツ・懸架・破片吸入。
- `FirstSeveranceDollSurface.cs`／`FirstSeveranceShellSurface.cs`：接続したtexture meshと殻の共通素材／寸法／分割規則。
- 同Client内の `FirstSeveranceBossVisuals.cs`／`FirstSeveranceStageVisuals.cs`：既存状態から新しい身体／棺へ接続。`FoundationCoreVisuals.cs`：柱の巻上機と準備描画。`FirstSeverancePylonVisuals.cs`／`FirstSeveranceSky.cs`：巻上装置／遠景の吊り糸。
- `Localization/DollTheater/`：日英のNPC名と会話。`tools/prepare_doll_assets.ps1`／`preview_doll_theater.ps1`／`DollTheaterPreview.cs`：外部原本からのexport、alpha／寸法／色数検査と同じposeのプレビュー。
- `Tests/Convergence.DomainTests/DollPresentationTests.cs`：fractional関節／表面の連続性、NPCの分割と中央収束、殻の各pixelが欠落なく一つの分割片へ属すこと。既存projectへpure表示コードをリンクした対象検査。

自動検査は素材のalpha／寸法、共有関節の連続性・有限scale、native compile。実機の受入れは以下だけを今回の確認対象としてユーザーへ渡す。

- 台にNPCが立ち、会話できる。新規・既存Core、再入場、Core撤去、再挑戦で複製／残留しない。
- 2～4人で空の既存棺＋台のNPC→Ready→NPC分解・吸入→戦闘→羽化を見る。Ready中には分解しない。途中snapshotでも演出が最初へ戻らず、破片が残らない。cancel時も台のNPCが戻る／残る。
- Phase Iは少量の露出のみ。Phase IIではNPC顔の単純拡大に見えず、旧拘束フレームと維持した腕が読める。Phase IIIは別の旧異形腕と開閉する鉤爪。本体の人形腕は維持。球体関節に隙間がない。
- 元の殻の微細な亀裂が準備／戦闘とも見える。閉じた殻にmask境界の黒い継ぎ目が出ない。関節と髪の連続動作、実機のフレーム時間／mesh描画を確認する。
- 1x／2x距離、異なるzoomと107% UI、ReducedEffects、結果演出後のHUD復帰を確認。

現段階は新方向の実装版であり、実機での最終美術承認ではない。現行追加素材と再生成指示は [0.2.57 recipe](../../../tools/asset_recipes/doll_presentation_0257.json)、追加ミニオンは [Weapons](WEAPONS.md#doll-companion--the-unbroken-promise)、武器音は [Audio](../../AUDIO_CUE_SHEET.md#weapon-only-foley) が正本。Raidの攻撃音／BGMは変更しない。物語会話・独自の柱／Pylon／背景素材化は後続候補。
