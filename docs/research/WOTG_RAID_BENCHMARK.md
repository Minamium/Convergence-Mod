---
doc_id: research.wotg-raid-benchmark
document_type: research
status: provisional
owners:
  - research
  - art
  - gameplay
last_reviewed: 2026-09-13
source_of_truth_for: []
aliases:
  - Wrath of the Gods benchmark
  - Avatar of Emptiness
  - Nameless Deity
  - WotG quality target
related_code:
  - Client/Encounters/FirstSeverance
  - Content/Encounters/FirstSeverance
related_docs:
  - project.status
  - encounter.first-severance.visual
  - encounter.first-severance.spec
  - research.sources
  - policy.ip-provenance
  - development.single-operator-testing
---

# Wrath of the Gods — マルチRaidのための品質ベンチマーク

調査日: 2026-09-06。目的は「WotG級の巨大感・不穏さ・荘厳さ・攻撃演出」を分解し、First Severanceへ独自に取り込む判断材料を残すこと。**WotGの再現Modにする、ソロ戦へ寄せる、素材や実装を移植する、という意味ではない。**

本書は参考研究と提案。現在の実装は[Status](../STATUS.md)、確定したRaidルールは[Encounter Spec](../encounters/first-severance/ENCOUNTER_SPEC.md)、現在の造形は[Visual Spec](../encounters/first-severance/VISUAL_SPEC.md)が所有する。本書だけでそれらを変更しない。ゲームコード・依存関係・素材は今回変更していない。

## 1. 結論

目標にすべきなのは「画像を大きくする」「発光と揺れを増やす」だけではない。公開コードからは、次の組み合わせが確認できる。

1. 部位を合成して動かす身体と、背景・前景を横断する奥行き。
2. 予備動作、狙いの提示、発動、残響、次の構えを別々に演出する時間設計。
3. 本体以外の場所から出る攻撃にも、一貫した発生源と予告を持たせる設計。
4. 攻撃・フェーズが背景、音、身体のポーズまで変える構造。
5. 描画用の大きさと実際の危険判定を分離する構造。

これは設計上の解釈であり、共有動画を実視聴して品質を採点した結論ではない。動画の公開メタデータは取得できたが、映像・音声の直接観察は未完了。公開ソースも最新Workshop版と同一とは確認できない。

特に重要なのは、調査版の`description.txt`がマルチ非対応を宣言し、`FrostScreenSmash`にはSingle Player以外でその攻撃を抜ける条件があること。**演出の参考と、マルチの正しさの参考は別に扱う。** [版の宣言][w-description]・[攻撃の分岐][a-frost]

## 2. 共有動画の台帳

### 2.1 確認できたこと／できなかったこと

検索ツールで指定時刻URL・通常URL・oEmbedを試した際は取得制限／内部エラー。別の通常HTTP読取でYouTubeの公開oEmbedと動画ページのプレイヤーメタデータを取得できた。以下のタイトル・投稿者・公開日・尺・章はその一次情報に基づく。公開日はページが返したJST表記。

映像再生、フレーム、実際の音、字幕は観察していない。メタデータに字幕トラックは見つからなかった。タイトルの「NO-HIT」は投稿者の主張であり、独立検証結果ではない。動画・音源・サムネイル・第三者worldデータはダウンロード／同梱していない。

| ID | 共有資料 | 確認できたメタデータ | 指定時刻の位置づけ |
|---|---|---|---|
| V1 | [Avatar of Emptiness — Myra Terraria][video-avatar] | 原題: *Avatar of Emptiness \| Calamity: Wrath of the Gods 1.2 Showcase*。2025-02-23、10分53秒 | 00:40は説明欄の裂け目の章内。本体出現の章は01:23から |
| V2 | [全Bossノーヒット — MustWin][video-all] | 原題: *ALL BOSSES NO-HIT \| Calamity: Wrath of the Gods (Master + Death Mode)*。2025-03-29、18分14秒 | 14:02は12:23開始のNameless章内。章の開始から1分39秒。具体的な攻撃名は未同定 |

V1は説明欄から公式Workshopへリンクするショーケース。追加のtexture/UI packも記載されている。V2にも別のUI resource packと編集前プレイリストへの案内がある。したがって、**動画のHUDや編集をすべてWotG本体の機能とみなさない**。動画公開時点と今回の公開コードの版も一致保証がない。

### 2.2 再視聴用の章マップ

章名は説明欄の内容を日本語で要約したもので、観察結果ではない。

| 資料 | 説明欄の区切り | 次に観察したい論点（未確認） |
|---|---|---|
| V1 | 00:10 裂け目 → 01:23 本体出現 | 小さな前兆から巨大な存在へ変わる構図、姿が揃うまでの時間、操作可能な範囲 |
| V1 | 03:21 空間移動 → 03:45 次段階 | 身体・地形・背景のどれが動いて見えるか。無害な場面転換と攻撃の境目 |
| V1 | 06:04 後半 → 07:54 終盤の転換 | 攻撃密度の上げ方、静かな間、音楽／色調の変化 |
| V1 | 09:30 装備／報酬 → 10:28 終了 | 比較時の装備条件。戦闘中の演出とは分ける |
| V2 | 00:55 Mars → 04:39 Avatar → 12:23 Nameless → 18:01 終了 | 同一動画内のBossごとの速度・見せ方の対比。今回はAvatar／Namelessを主対象とする |
| V2 | 指定14:02の前後 | 「何が来るか」「どこへ逃げるか」が何フレーム前に分かるか。成功時の移動量と余裕 |

次の観察では、指定時刻の前後も含めて「予告開始／目標固定／初回危険判定／発動終了／次の攻撃」の時刻を記録する。プレイヤーを基準に画面占有率、画面外の発生源、実際の回避経路を測る。背景が暗い、音が荘厳、避けやすいといった感想は実視聴後に追記する。コード上の名前を見てV2の14:02の攻撃名を推定で埋めない。

## 3. ソース調査の基準点と権利

| 項目 | 根拠・限界 |
|---|---|
| Project / repository | [TheFifthCircle/WrathOfTheGodsPublic][w-repo]。README、`build.txt`、公開commitのauthorがLucille Karma。公式WorkshopのDeveloped By／Programming creditも同名で、製作者の公開リポジトリとして照合した。Workshopから当該GitHubへの直接リンクまでは今回確認していない |
| 確定revision | `main`取得時の`7cb5b86c770e73d6853749b2b688d478ba3326a7`。commit日2025-11-11、内容はfunding設定。アクセス日2026-09-06。以下のコードリンクはすべてこのSHA固定 |
| 宣言version | `build.txt`: WotG **1.2.24**。`SubworldLibrary`、`Luminance@1.0.9`、`CalamityMod@2.0.4.1`、`StructureHelper` DLL。weak referencesは`MonoStereoMod`／`CalRemix` [build][w-build] |
| engine target | `NoxusBoss.csproj`は親の`tModLoader.targets`をImport。調査版自身の厳密なtML release／Terraria patch／.NET／C# versionはこの宣言から確定できない。`unknown`として扱う [project][w-project] |
| 現行配布との相違 | 公式WorkshopにはDAYBREAK等のrequired itemsが表示される一方、この公開buildにはDAYBREAKがない。公開branchを「2026-09-06の最新配布版の実装」と呼ばない [Workshop][w-workshop] |
| source access | GitHub APIのcommit／全tree 3,146 entries（truncated=false）と、下記の選択したテキストを取得。ソースのclone・ミラー・Modの導入／実行はしていない |
| license | SHA固定tree内の`license`／`copying`／`copyright`名の検索で該当なし。Repository APIのlicenseはnull。README／build／descriptionにも調査範囲ではコピー許諾を確認できない。`buildIgnore`中の`LICENSE`という文字はlicense本文ではない [tree][w-tree] |
| copy boundary | 許諾不明なので、コード・shader・画像・音・音楽を持ち込まない。技法・挙動・API名を調べ、独立した設計と実装を行う。[IP方針](../IP_PROVENANCE.md)を適用 |

Convergence側の比較対象は[Version Matrix](../VERSION_MATRIX.md): Terraria 1.4.4.9、tML v2026.07.3.0／`666f69962d3bdffde54fc14025f02634965b4e7c`、.NET 8／C# 12、Calamity 2.2.4。WotGの依存や内部APIがそのまま使えるという意味ではない。Luminanceを新たに依存へ追加する判断も今回していない。

## 4. 実装から分かる表現の仕組み

各節の「確認」は固定sourceの事実、「解釈」は設計上の推論、「提案」は未実装のConvergence案。共通の版／出所／license／アクセス日は前節。検索語は各symbol、`RenderTarget`、`ZPosition`、`Telegraph`、`netMode`、`SendExtraAI`、`ModifyIncomingHit`、`Sound`、`Reset`、`Solyn`。

### F1 — 大きさは一枚のスプライトではなく合成と奥行き

**確認:** Namelessは`NamelessDeityRenderComposite.PrepareRendering`で2500×2500のtargetを要求し、`LayerIndex`順の`INamelessDeityRenderStep`で部位を合成する。texture差し替えも独立している。通常のNPC設定は270×500で、target寸法と同じではない。[合成][n-composite]・[初期化][n-init]

Avatarは身体、影、silhouette、finalの処理を分ける。Body targetは`4200 × TargetDownscaleFactor(0.71)`、すなわち要求寸法2982×2982。頭、腕、指の角度、残像、部位のopacityを個別に持ち、`ForwardKinematics`で繋ぐ。`ZPosition`によってscaleと地形より後ろ／最前面への描画を変える。通常NPCの基準hitboxは601×697で、別にspider lilyのhitboxもある。[Body][a-body]・[Utils][a-render-utils]・[描画／層][a-render]・[NPC][a-main]

**解釈:** 高解像度は細部を支えるが、巨大感の主役は部位の遅れ、前後の重なり、画面端を越える身体、前景へ迫る奥行きである。targetの数字は画面上のBossサイズや全身体の当たり判定ではない。

**提案 — adapt:** Null Cantorの一つのlife poolは維持し、中央の封印核、可動拘束器、後方構造物、布状の灰／煙を独立した描画部品へ育てる。巨大装飾の中で攻撃可能な核と味方位置を見失わせない。二つ目以降の部位を壊せると誤認させるhealth barは足さない。

**小さな確認:** まず一つの詠唱ポーズだけを実機で確認。通常／Reduced Effectsで核、味方、予告が同時に読めること。別解像度や3–4人条件はそれに依存する実装を触った時に追加する。

### F2 — 動作は「溜めと発動」の時間的な対比

**確認:** Nameless／Avatarは状態stackと状態ごとのtimerを持ち、behaviorとtransitionを分ける。Avatarは近距離／遠距離／中立の攻撃群を選び、直前のpatternや先頭attackが重複しないよう再選択する。[Nameless states][n-states]・[Avatar states][a-states]・[選択][a-reset]

Avatarのphase移行の叫びでは、腕を顔付近へ寄せてから大きく伸ばし、溜めに合わせrumbleを上げ、発動tickに叫びと衝撃を重ねる。調査版AIデータのChargeUpTimeは60ticks。プレイヤーへの吹き飛ばし／mount解除も含むため、純粋な映像効果だけではない。[叫び][a-scream]・[AI値][a-values]

**解釈:** 「速い」のはすべての補間を速めることではなく、読める溜めの後に短い発動を置くこと。待ち時間にも構え、圧縮、静止、音の変化という役割がある。

**提案 — adapt / reject:** Raidはserverの予告tick→固定tick→発動tick→終了tickで身体・音・危険範囲を揃える。演出上の揺れと、実際にプレイヤーを吹き飛ばすルールを分離する。叫びに伴う全員へのvelocity変更・mount解除・時間停止はそのまま採用しない。Stack集合中の強制移動は特に不公平を生む。

**小さな確認:** 1攻撃について「意図が読める溜め」と「気持ちよい短い発動」の両方を評価。予告を延ばすだけで発動まで鈍くしない。採用済みの長い予備動作は[現行spec](../encounters/first-severance/ENCOUNTER_SPEC.md)を維持する。

### F3 — 攻撃はBossから離れた場所にも発生する

**確認:** `PerpendicularPortalLaserbeams`は接近→距離を取る→縦横交互のdashという段階を持つ。dash中にportalと予告laserを配置し、各laserへ残りdash時間＋14のfireDelayを渡す。spawnは`Main.netMode != MultiplayerClient`で囲まれる。身体の移動と、残ったportalからの発射を別の事象にしている。[攻撃behavior][n-portals]

Avatarの`ArmPortalStrikes`は対象の現在位置・速度から予測した外部の地点にportalを出す。portalは腕かantimatterの攻撃を選ぶ。視認性のため、その間Solynは攻撃ではなく追従行動になる。[portal strike][a-portals]

**提案 — adapt:** Bossは「空間に命令する」ポーズを取り、独自の破断口／拘束杭／欠けた観測窓から攻撃する。ユーザー図の往復4色予測線、右dash→静止→左dash→静止と整合する。攻撃源が画面外でも、プレイヤー側に方向・到達線・確定の合図を出す。

**Raidでの追加条件:** 対象は凍結roster内のstable Participant IDで割り当て、照準の確定位置もserverが所有する。各clientの`LocalPlayer`をそれぞれ狙うと別々の世界になる。複数人への攻撃を無条件に重ねず、他人宛ての判定が安全地帯を塞がない配置規則が必要。

**小さな確認:** 同じcastを二つの画面で見る。自分が非対象でも線と被弾範囲が一致し、発生源が画面外でも避ける情報が残るか。

### F4 — 予告・実体・余韻と危険判定は別の区間

**確認:** `BaseTelegraphedPrimitiveLaserbeam`は予告を最大距離へ描き、発射後だけlaserの長さを伸ばす。`Colliding`は予告中と末尾のfade区間を無害にし、AABB対lineで判定する。`OnLaserFire`は境界付近に独立した発動処理として呼ばれる。[基底laser][laser-base]

`TelegraphedPortalLaserbeam`は専用の予告shaderと本体shader、発生点のglow、近距離だけのpulse粒子を持つ。レーザー群の発射音はフラグを持つ一発だけで鳴らし、`MaxInstances=1`も使う。[portal laser][n-laser]

**提案 — adapt:** 見える危険な芯、無害な長い尾、発生源の光、空間の歪みを別レイヤーにする。大きな演出をcollision拡大で代用しない。Stack／Spreadの外周、chargeの確定線、停止攻撃の空白は全effect設定で残す。

**小さな確認:** 固定→初回hitと終了→無害化の境界を一つだけ確認し、同期された形と見た目を比較する。WotGのtick定数や末尾猶予を丸ごと移植しない。Convergenceの既存collision／通信契約に合わせて独立実装する。

### F5 — 空間そのものがフェーズを表現する

**確認:** Avatarのskyは`Dimension.BackgroundDrawAction`を選び、通常背景・vortexを切り替える。sky用targetは0.425倍率のdownscale設定。雲、tile色、tear等も別に制御する。Namelessは空の目、星、kaleidoscope、黒overlayなどの値を持ち、非active時に段階的resetを行う。[Avatar sky][a-sky]・[Nameless sky][n-sky]

Namelessの`MomentOfCreation`は背景へ退く身体、星の後退、暗転、指snap、発動、銀河projectile列、復帰を一つのtimerで編成する。sourceにはSingle Playerのみの演出分岐もあり、画面内の体験をそのまま全員へ適用できるわけではない。[終盤attack][n-creation]

**提案 — prototype:** Null Cantorでは「虚空の巨大聖堂」そのものを段階変化させる。平常の暗い遠景→予告時に遠方の拘束構造が収束→発動点だけ骨白色の亀裂→残響で再び暗く沈む。背景には低周波の動き、本体には明確なポーズ、攻撃には短い高周波の動きを割り当てる。

**採らないもの:** 元の目・翼・花・検閲帯などの象徴のコピー、常時極彩色、強制camera zoomで味方を画面外へ追い出す演出、参加していないプレイヤーまで巻き込むworld改変。skyやpost-effectの本格導入は新たな互換性確認を要する。過去のworld参加時の不具合を避けるため、inactive／world exitの経路も対象とする。

### F6 — 音はBGM一曲と単発爆発音だけではない

**確認:** 両Bossは`ModSceneEffect`から現在のNPCのMusic slotを返す。Avatar本体はphaseでslotを選び直す。個別attackは詠唱・snap・発射など別の`SoundStyle`を発火する。これはコード上のcue配置の確認であり、音色・mix・荘厳さを聴いて評価したものではない。[Nameless music][n-music]・[Avatar music][a-music]・[Avatar本体][a-main]・[MomentOfCreation][n-creation]

`LoopedSoundInstance`は開始音→loopという二段構造、位置／volume更新、明示Stopを持つ。managerは音声利用可否とtermination conditionを確認する。多数の攻撃音をすべて同時最大音量にする方式ではない。[loop instance][sound-instance]・[manager][sound-manager]・[発射音の制限][n-laser]

**提案 — adapt:** 次の制作briefは独自の低い持続音、加工した合唱的texture、軋む金属、重い空間残響、発動直前の薄い無音を中心とする。第九／bit調へ戻さない。実際の曲は試聴して選び、WotGの曲・演奏・sampleを取得しない。

| 事象 | 独自の音の役割（提案） | Raidで最優先する情報 |
|---|---|---|
| 開始 | 低い空間鳴動→刻印の短い衝撃→余韻 | 操作不能ではなく安全な導入中と分かる |
| 頭割り | 収束する倍音→確定のclick→成功時に解ける和音 | 集合対象と成立／不成立。人数分の同音連打を避ける |
| 散開 | ほどける擦過音→外周の短いpulse→成功時に消失 | 他人と重なっているか。音だけに依存しない |
| 高速energy | 遠方の質量音→狙い固定の硬い音→極短い通過音 | 固定された向きと発射時刻 |
| Down／蘇生 | 他の攻撃より識別しやすい固有の音型 | 味方救助とrecipient lockout。画面外でも消えない |

音楽は場面の感情を担い、サーバーの攻撃時計は音楽の再生位置に依存させない。ミュート・途中参加・音声device差でも判定は同じ。今ある音の実装・試聴資料は[Audio Cue Sheet](../AUDIO_CUE_SHEET.md)へリンクし、本書で品質合格とはしない。

### F7 — 長い戦闘はHPだけで保証されていない

**確認:** Namelessの`ModifyIncomingHit`は経過時間に対する理想life ratioより削りが先行すると追加のDRを与える。AI値では通常系の理想時間3.25分、追加DR上限0.9。Avatarにも理想時間と追加DRの処理がある。またNamelessはphase移行待ちでHPを保持し、`CheckDead`でdeath animationへ入るまで自然死を止める。[Nameless本体][n-main]・[被ダメージ／death][n-misc]・[AI値][n-values]・[Avatar本体][a-main]

**解釈:** HP値だけ比較して「こちらは何倍強い」「同じ戦闘時間になる」とは言えない。装備、人数、damage gate、DR、難易度、演出待ちがすべて違う。動画のMaster＋Death条件とソースのbase値も同一ではない。

**提案 — 現時点ではreject:** 隠れた時間依存DRを導入して強装備を無意味にするより、Raid既存のPylon checkと明示的なexposure windowを中心に設計する。長い儀式を見せたいなら、無敵区間の意味と終了を見せ、攻撃可能区間は火力の達成感を残す。具体的なHP・時間は別のtuning作業で決める。

### F8 — マルチの参考としては制約が大きい

**確認:** Avatarはphase、部位位置、Z位置、state stack等を`SendExtraAI`／`ReceiveExtraAI`で同期し、通常のstate transitionは非clientで実行する。Namelessも手やstate stackを同期する。しかし調査版descriptionはマルチ非対応と記載する。`FrostScreenSmash_CreateFrost`のtransitionには`netMode != SinglePlayer`なら抜ける条件が存在する。[Avatar][a-main]・[Nameless同期][n-misc]・[description][w-description]・[Frost][a-frost]

**判断 — adapt / reject:** serverで選択して必要な状態を配る考えは参考になるが、このsourceからWotG全体のマルチcorrectnessは証明しない。現在のWorkshop版も非対応だとは断定しない。client固有camera座標を共有判定にする、各clientで対象や乱数を決める、全ActivePlayersへ一律の演出由来の移動を加える、といった選択はConvergenceの凍結roster／exact-Fight設計へ持ち込まない。

**最低限の変換契約（提案）:** attack instanceはFight ID、cast serial、kind、対象ID、warning/lock/fire/end tick、確定geometryを持つ。serverだけがhit／成功失敗／Downを決め、clientは絵と音を派生する。途中から状態を受けても現在の予告を描けること。到着が遅い音イベントを全部巻き戻して鳴らさない。既存protocolとの差分・packet境界は実装時に別途レビューする。

### F9 — 巨大effectには抑制と後始末が要る

**確認:** `WoTGConfig`はclient設定で、Photosensitivityを有効にすると画面破砕・overlay強度・揺れを抑える。screen shatterにはserverのtarget生成回避と代替shakeの経路がある。`InstancedRequestableTarget`はidentifier別targetを管理し、サイズ変更／ResetでDisposeする。[Config][w-config]・[shatter][screen-shatter]・[target管理][target-manager]

**解釈:** 大きなtargetは無料ではない。RGBA8・1面・depth等を除く単純計算でも2500²は約23.8MiB、2982²は約33.9MiB。これは計算例で、WotG全体の実測VRAMではない。二つのゲーム画面を同一PCで走らせる場合はGPU負荷も分かち合う。

**提案 — adapt:** まず少数の再利用targetと明示的な描画順で実装し、毎frameの新規割当を避ける。被弾判定はeffect品質に依存させない。Fight end／cancel／world exit／mod unloadの担当を分け、state・shader・音が次のRaidに残らないことを一つの終了経路で確認する。今回の調査はWotG全体のメモリリーク／再参加／全packet監査ではない。

### F10 — Solynは「もう一人の実client」ではない

**確認:** `BattleSolyn`はModNPCで、Avatar戦では追従と攻撃のbehaviorを切り替える。Avatar側が行動を指定し、弾の発生には非client／multiplayer clone判定がある。攻撃の見やすさを優先して追従だけにする場面もある。[BattleSolyn][solyn-avatar]・[behavior][solyn-behaviors]・[portal場面][a-portals]

**提案 — 将来のprototype:** 同行NPCはソロ練習・物語表現には使える。ただしプレイヤーのネット接続、装備、item use request、Down投影の代用品にはならない。NPCをRaid人数へ含めるなら、新たなparticipant adapterとルールが必要。[一人検証の提案](../runbooks/SINGLE_OPERATOR_TESTING.md)に比較を分離する。

### F11 — 発射工程を一つの連続体にする／地面からの封鎖構造（2026-09-06追補）

**問い:** 予告から射出、飛翔、消散へ形が途切れず移行するには何が必要か。巨大な構造物を複数の関節で動かし、地面を底辺に閉鎖フィールドを展開する方法は何か。版・配布元・権利は上記固定台帳を再利用する。tMLは実機の `666f69962d3bdffde54fc14025f02634965b4e7c`、WotGは `7cb5b86c770e73d6853749b2b688d478ba3326a7`。Calamity参照 `1a8cebd27ec5615316b78f71973446b5528d2b78` は2.2.2であり、実機2.2.4の同一コードとは扱わない。

**確認した実装:** Avatarの `ForwardKinematics` は親からの回転・関節位置・長さを累積する。Namelessのportal攻撃は離脱・溜め・発射の時刻を分け、移動を補間し、サーバー側spawnとローカルのpulse/画面効果を分離する。portal laserは予告と実体の専用描画、幅・寿命の変化を持つ。大きな一枚絵の回転だけではなく、連結された変形と時間設計が表現を支えている。[Avatar運動学](https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/Rendering/AvatarOfEmptiness.Rendering.Utils.cs)・[Nameless攻撃状態](https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/Behaviors/Phase1Attacks/NamelessDeityBoss.BehaviorStates.PerpendicularPortalLaserbeams.cs)・[portal laser](https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/Projectiles/TelegraphedPortalLaserbeam.cs)

Calamityの `ArenaWallSystem.Box` は矩形・描画・更新・除去条件を持ち、`ArenaWallPlayer.PreUpdateMovement` がplayer位置/速度を制限する。Supreme CalamitasはBoxへ色・大きさ等の変化とNPC消失に対応する条件を渡す。これは「大量の地形タイルを生成せず閉鎖する」参考であり、対象判定やbalanceをそのまま採用する根拠ではない。[ArenaWallSystem](https://raw.githubusercontent.com/CalamityTeam/CalamityModPublic/1a8cebd27ec5615316b78f71973446b5528d2b78/Systems/Mechanic/ArenaWallSystem.cs)・[SupremeCalamitas](https://raw.githubusercontent.com/CalamityTeam/CalamityModPublic/1a8cebd27ec5615316b78f71973446b5528d2b78/NPCs/SupremeCalamitas/SupremeCalamitas.cs)

**公式API:** `ModPlayer.PreUpdateMovement/PostUpdate` による予測/最終制限、`ModBlockType.CanPlace` のローカル配置拒否、`GlobalTile.PreDrawPlacementPreview` のmultitile各section描画契約を確認。配置プレビューはサーバーの権限検証の代わりにならない。[pinned ModPlayer](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModPlayer.cs)・[pinned ModBlockType](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModBlockType.cs)・[pinned GlobalTile](https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/GlobalTile.cs)

**独立設計・推論:** 共通tickから連続envelopeを計算し、開口、収束する繊維、流動texture、伸びる飛翔体、冷える残光を重ねる。追尾中だけHermite補間し、確定後の命中geometryを補間しない。射出直後に予告objectを別のbeam objectへ取り替えること、末尾を突然消すこと、見えない全長hit、safe gapを塞ぐbloom、第三者shader/assetの移植を採用しない。構造物には独立生成atlasの関節を使う。フィールドはexact-Fight roster/接続epoch、期限付き飛行権限、地面中心から派生する矩形を使用する。これらが目標の滑らかさ・読みやすさを達成するかは実機評価が必要。

**権利・確認:** コード・画像・shader・音源の複製なし。モデルによる独自atlas生成と独立C#実装。境界/時間関数、二人のdash/recall/Down/終了解除、旧2x2と新12x4配置、二重client負荷を該当確認とする。実行済みかどうかは[Status](../STATUS.md)だけに記録する。今回YouTubeフレーム/音声を新たに観測したとは主張しない。

### F12 — 極太赤ビーム・エネルギー弾・終幕の連動（2026-09-13追補）

**対象と版:** WoTMはWrath of the **Machines**。公式公開snapshot `5556a3adcbabffc6fc95685e34f1ee22cee31d9a` / 1.0.4。WotGは既読調査と同じ `7cb5b86c770e73d6853749b2b688d478ba3326a7` / 1.2.24。後者の公開snapshotは現在のWorkshop版と同一とは確認できない。参照snapshotのtML/Terraria厳密版は不明。WotG側のprojectはLuminance 1.0.9、Calamity 2.0.4.1等を参照するが、Convergenceの実環境はtML 2026.07.3.0 / Terraria 1.4.4.9、Calamity 2.2.4、Luminance 1.0.14。依存更新ではなく設計観察として扱う。今回YouTubeの動画フレームや音声を新規観測したとは主張しない。

**観測・極太赤ビーム:** [HadesSuperLaserbeam](https://github.com/LucilleKarma/WrathOfTheMachines/blob/5556a3adcbabffc6fc95685e34f1ee22cee31d9a/Content/NPCs/ExoMechs/Projectiles/HadesSuperLaserbeam.cs) は過熱時に赤へ変わり、幅の立ち上がり、独立した広いbloom、二重の発射口光、稲妻、背景露出低下を組み合わせる。ユーザーの言う太い赤ビームに対応する有力例であり、全Mod中で最大と計測したわけではない。[ExoEnergyBlast状態](https://github.com/LucilleKarma/WrathOfTheMachines/blob/5556a3adcbabffc6fc95685e34f1ee22cee31d9a/Content/NPCs/ExoMechs/Hades/States/SoloAttacks/HadesBodyEternity.ExoEnergyBlast.cs) は顎の開閉、最後の震え、粒子の吸引、発射音・反動まで同じ工程にまとめる。[材質shader](https://github.com/LucilleKarma/WrathOfTheMachines/blob/5556a3adcbabffc6fc95685e34f1ee22cee31d9a/Assets/AutoloadedEffects/Shaders/Primitives/HadesExoEnergyBlastShader.fx) は移流noiseと横断方向の明暗を使う。**推論:** 「太い線」そのものより、暗い場・圧縮した発射源・不均一な高輝度・反動の対比が力感に寄与している。

**観測・エネルギー弾:** [HadesExoEnergyOrb](https://github.com/LucilleKarma/WrathOfTheMachines/blob/5556a3adcbabffc6fc95685e34f1ee22cee31d9a/Content/NPCs/ExoMechs/Projectiles/HadesExoEnergyOrb.cs) は背景の柔らかい光と本体を別描画し、[orb shader](https://github.com/LucilleKarma/WrathOfTheMachines/blob/5556a3adcbabffc6fc95685e34f1ee22cee31d9a/Assets/AutoloadedEffects/Shaders/Objects/HadesExoEnergyOrbShader.fx) は極座標noise、脈動、中心光、縁の色を組み合わせる。**採用:** 小さいFinal弾にも明るい核・乱れた皮膜・進行方向に残る薄い尾を分ける。既存当たり半径を縮めて光だけ巨大化させる案や、参考側の補助Projectile生成は採用しない。

**観測・WotGの予兆／発射:** [TelegraphedPortalLaserbeam](https://github.com/TheFifthCircle/WrathOfTheGodsPublic/blob/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/Projectiles/TelegraphedPortalLaserbeam.cs) は予告と本体のshaderを分け、縦長の発射口光・近傍粒子・一度の画面／音イベントを持つ。[予告shader](https://github.com/TheFifthCircle/WrathOfTheGodsPublic/blob/7cb5b86c770e73d6853749b2b688d478ba3326a7/Assets/AutoloadedEffects/Shaders/Primitives/NamelessDeityFlowerLaserTelegraphShader.fx) は横断勾配と疎な微光、[本体shader](https://github.com/TheFifthCircle/WrathOfTheGodsPublic/blob/7cb5b86c770e73d6853749b2b688d478ba3326a7/Assets/AutoloadedEffects/Shaders/Primitives/NamelessDeityPortalLaserShader.fx) は異なる速度の明暗noiseを用いる。**採用:** 色付きの危険体積→流れる微光の圧縮→即時に全判定幅の高輝度材質→暗い余韻。円形発射印、別の予兆レール、参考側の長さ／幅の成長による命中時刻変更は移植しない。

**観測・終幕:** [DeathAnimations](https://github.com/TheFifthCircle/WrathOfTheGodsPublic/blob/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/Behaviors/AnimationStates/NamelessDeityBoss.BehaviorStates.DeathAnimations.cs) は奥行きへの退避、照明変化、溜め、画面方向への急加速、単発の強い破壊を時間で分ける。[ScreenShatterSystem](https://github.com/TheFifthCircle/WrathOfTheGodsPublic/blob/7cb5b86c770e73d6853749b2b688d478ba3326a7/Core/Graphics/ScreenShatter/ScreenShatterSystem.cs) は実画面のcapture/三角形分割、再作成・後始末、光過敏設定を扱う。**採用:** 大きな静止対比と一度の終端burst。Convergenceでは既存の斜め裂け目・自前のshell/rig片・GPU吸引材質で独立に構成し、画面capture基盤を追加しない。入力封鎖、world退出、全Projectile削除、save操作、NPCの延命による報酬遅延は明確に不採用。

**実装への写像:**

| 観察した責務 | Convergenceの独立実装 | 守る境界 |
|---|---|---|
| 予告・内部流・外周光の分離 | `RaidEnergy` のForecast/Beam/Coronaと既存ray adapter | 同じhalf-width/tick。格子・密集領域では外周光なし |
| 発射源の圧縮・吸引・反動 | Mouth/Pressure/Flare、固定個数の流線、既存Coreの凹み | 発射口を物理的に接続。Boss上にHUD円を復活させない |
| 球状の流動材質 | Orb/WakeをFinal弾と突進体へ | 新しいhitbox/Projectileなし |
| 背景露出と幕の奥行き | cathedralの照明／mist、GrandStageの後景圧力 | terrain・プレイヤー・円の前に不透明な装飾を出さない |
| 溜め→吸引→消滅 | 既存terminal時計のRift/fragment/Flare | reward/cleanupは遅らせず、新Fightで破棄 |

**権利・ライセンス:** [WoTM MIT](https://github.com/LucilleKarma/WrathOfTheMachines/blob/5556a3adcbabffc6fc95685e34f1ee22cee31d9a/LICENSE) は確認したが、今回そのコード・shader・素材の複製はしていない。WotGの調査対象には明示ライセンスを確認できず、公開閲覧を転載許可と扱わない。LuminanceのMIT依存と公開texture registryだけを利用し、依存画像のコピーをリポジトリ／配布物に入れない。新規shaderは独立作成で、[素材台帳](../../Assets/ATTRIBUTION.md)に記録する。

**確認と限界:** shaderの実GPU previewでは予告／本体の差、流動、球体、裂け目、退色を確認する。実Raidの多人数重なり、107% UI/zoom、Reduced Effects、低フレーム時、ロード／アンロード後の状態復帰は別の実機受け入れ項目。静的レビューやプレビューだけでWoTM/WotGと同等品質・FPSを断言しない。実行記録は[Status](../STATUS.md)、現行の規則は[Visual spec](../encounters/first-severance/VISUAL_SPEC.md#luminance-raid-presentation)を正本とする。

## 5. First Severanceに変換するときに崩さないもの

| 残すRaid契約 | 巨大・派手な表現への変換案 | 避ける破綻 |
|---|---|---|
| 2–4人の凍結roster | 誰の攻撃かはマーカー／番号で示し、Bossは全員共通の動作 | ソロtargetだけ見れば成立する攻略 |
| Pylon DPS check→Stack→Spread→exposure | 一つの儀式の各段階として、拘束器・背景・音を変える | 派手な別Boss戦でRaidが中断され続ける |
| 全員で成立したStackは0damage | マーカーの収束と封印成立を明確に見せる | 成功しても爆発で削られる／成立と失敗が同じ見た目 |
| 重ならないSpreadは0damage | 対象の外周を明確にし、他人の円との距離を判断できる | 大きいBossや煙が円を隠す |
| itemで即時蘇生、非消耗、recipient 60秒lockout | 味方の位置と蘇生可否を最前面に残す | 背景演出のせいで救助対象が消える、共有tokenの再導入 |
| Defeatは参加者全員の通常death | terminal確定後に独自の静かな崩壊／残響 | 演出開始で早めに死亡、非参加者を巻き込む |
| one Boss life pool、巨大装飾は別 | 核が攻撃可能かを構造の開閉で示す | 巨大な装飾全域が不意の接触hitboxになる |
| server判定／client presentation | 共通world空間の攻撃、camera差は見え方だけ | shake・zoom・音声再生位置がdamageを決定する |

現行ルールの詳細はspecを優先する。重ね合わせる新攻撃の人数・範囲・時刻は未決定であり、この表は追加実装の承認ではない。

## 6. 独自の美術・演出brief案

**キーワード:** 極地の封印施設、虚空の聖堂、玄武岩、欠けた骨白磁、鈍い古金、非対称な拘束装置、音を吸う空洞。神の顔や既存の天使ではなく、「人格があるように動く巨大な封印機構」。現在の方向を深める案で、最終デザイン承認ではない。

- 大／中／小の情報を分ける。大きなsilhouetteと負の空間、可動部の中規模形状、表面の細い彫刻。全身を等密度の細線で埋めない。
- 暗い本体に低彩度の材質差を残す。発光は核、確定した攻撃、成功／失敗の結果に集中。常時全画面発光では明暗の差が失われる。
- idleはほぼ静止、詠唱は極端な開閉と重い遅れ、確定は短い静止、発動は鋭く、余韻は長く。滑らかな揺れを全レイヤーへ同じ周期でかけない。
- 背景は巨大さを伝える遠方の比較物を持つが、プレイヤーがいる領域は静かに保つ。危険線と同じ明度・太さの装飾を置かない。
- 頭割り／散開／蘇生は世界観に合う専用意匠にする。ただし「味方と集まる」「味方から離れる」「ここで救える」という文法は独創性より読みやすさを優先する。

## 7. 実装へ移るときの最小単位

以下は優先順の提案。実装planを自動的に上書きしない。

1. **一つの完成度の高いattack scene。** 既存の右energy→停止を題材に、独立部位の構え、外部発生源、照準固定、短い発動、余韻、専用soundまでを一緒に仕上げる。
2. **同じ品質でStack／Spreadへ展開。** 成立が気持ちよく伝わり、蘇生対象が埋もれないことを確認する。
3. **場面転換を一つ。** 安全なintroまたはexposureの開閉に限定して背景・音楽・cameraの連動をprototypeする。active中の全面暗転は避ける。
4. **必要に応じて部位rig／shaderを拡張。** 先に巨大な汎用演出engineを作らない。完成した一場面を見て不足を選ぶ。

初回の評価は「映像として一場面を見たいと思えるか」「被弾理由が分かるか」「もう一人を助ける余地があるか」。同期に触れた時だけ実client2つで同一castを確認し、文書だけの変更でgame buildや全test matrixを要求しない。ソロ作業時の具体策は[一人検証](../runbooks/SINGLE_OPERATOR_TESTING.md)へ。

## 8. 未確認・次に埋める項目

- V1 00:40／V2 14:02前後の実フレーム・音・回避経路。素材を再共有してもらうか、ユーザーの再視聴メモがあれば章マップへ追加する。
- 動画収録版の正確なWotG／tML／Calamity version、texture packによる見た目の差。公開コードとの完全な対応は未確認。
- 現行Workshop版のマルチ動作。古いdescription／単一attackの分岐から現在の配布版へ一般化しない。
- このPCでの二重起動時のGPU・音・focusの挙動、実際の0.2.5 audiovisual acceptance。
- 第三者source／assetの利用許諾。将来利用したくなった場合は個別確認が必要。今は参照だけ。

## 固定ソース索引

下記のコードはすべてテキスト取得済み。動的URLはアクセス日を上記で固定する。大きなsource抜粋やassetは本書へ転載しない。

[video-avatar]: https://www.youtube.com/watch?v=LIFVyjz0T3k&t=40s
[video-all]: https://www.youtube.com/watch?v=LoHvGScaZYU&t=842s
[w-workshop]: https://steamcommunity.com/sharedfiles/filedetails/?id=2995193002
[w-repo]: https://github.com/TheFifthCircle/WrathOfTheGodsPublic
[w-tree]: https://api.github.com/repos/TheFifthCircle/WrathOfTheGodsPublic/git/trees/7cb5b86c770e73d6853749b2b688d478ba3326a7?recursive=1
[w-build]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/build.txt
[w-project]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/NoxusBoss.csproj
[w-description]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/description.txt
[w-config]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Core/Configuration/WoTGConfig.cs
[n-composite]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/Rendering/NamelessDeityRenderComposite.cs
[n-init]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/NamelessDeityBoss.Initialization.cs
[n-main]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/NamelessDeityBoss.cs
[n-misc]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/NamelessDeityBoss.Misc.cs
[n-states]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/NamelessDeityBoss.States.cs
[n-values]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/NamelessDeityAIValues.json
[n-portals]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/Behaviors/Phase1Attacks/NamelessDeityBoss.BehaviorStates.PerpendicularPortalLaserbeams.cs
[n-laser]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/Projectiles/TelegraphedPortalLaserbeam.cs
[laser-base]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Core/BaseEntities/BaseTelegraphedPrimitiveLaserbeam.cs
[n-sky]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/SpecificEffectManagers/NamelessDeitySky.cs
[n-music]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/SpecificEffectManagers/NamelessDeityMusicScene.cs
[n-creation]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/NamelessDeity/Behaviors/Phase3Attacks/NamelessDeityBoss.BehaviorStates.MomentOfCreation.cs
[a-main]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/AvatarOfEmptiness.cs
[a-render]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/Rendering/AvatarOfEmptiness.Rendering.cs
[a-body]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/Rendering/AvatarOfEmptiness.Rendering.Body.cs
[a-render-utils]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/Rendering/AvatarOfEmptiness.Rendering.Utils.cs
[a-states]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/MiscCode/AvatarOfEmptiness.States.cs
[a-reset]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/Behaviors/AvatarOfEmptiness.BehaviorStates.ResetCycle.cs
[a-portals]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/Behaviors/DarkUniverseAttacks/AvatarOfEmptiness.BehaviorStates.ArmPortalStrikes.cs
[a-frost]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/Behaviors/CryonicUniverseAttacks/AvatarOfEmptiness.BehaviorStates.FrostScreenSmash.cs
[a-scream]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/Behaviors/PhaseTransitions/AvatarOfEmptiness.BehaviorStates.Phase3TransitionScream.cs
[a-values]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/AvatarOfEmptinessAIValues.json
[a-sky]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SpecificEffectManagers/AvatarOfEmptinessSky.cs
[a-music]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SpecificEffectManagers/AvatarOfEmptinessMusicScene.cs
[sound-instance]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Core/SoundSystems/LoopedSoundInstance.cs
[sound-manager]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Core/SoundSystems/LoopedSoundManager.cs
[screen-shatter]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Core/Graphics/ScreenShatter/ScreenShatterSystem.cs
[target-manager]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Core/Graphics/RenderTargets/InstancedRequestableTarget.cs
[solyn-avatar]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Friendly/Battle/BattleSolyn.AvatarOfEmptiness.cs
[solyn-behaviors]: https://raw.githubusercontent.com/TheFifthCircle/WrathOfTheGodsPublic/7cb5b86c770e73d6853749b2b688d478ba3326a7/Content/NPCs/Bosses/Avatar/SecondPhaseForm/Behaviors/Solyn/AvatarOfEmptiness.SolynBehaviors.cs
