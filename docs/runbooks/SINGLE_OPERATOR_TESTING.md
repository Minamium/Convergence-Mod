---
doc_id: development.single-operator-testing
document_type: runbook
status: provisional
owners:
  - engineering
  - gameplay
last_reviewed: 2026-09-14
source_of_truth_for: []
aliases:
  - 一人マルチ検証
  - two local clients
  - solo raid testing
  - debug sidecar
related_code:
  - Content/Encounters/FirstSeverance/FirstSeveranceDownCommand.cs
  - Content/Encounters/FirstSeverance/FirstSeverancePrototypeCombatRuntime.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceRoster.cs
related_docs:
  - project.status
  - development.windows
  - verification.evidence
  - encounter.first-severance.revive
  - research.wotg-raid-benchmark
---

# 一人でマルチRaidを検証する方法

**通常はユーザーのGUI操作とフレンドとの検証を続ける。一人と明示された場合、簡単な挙動・演出確認には一窓のHost & Playを使える。** 二窓・専用server・Computer Useはその回に必要とされた場合だけ準備する。過去のComputer Use依頼を今後のGUI自動化への包括的な許可としない。NPCを先に実装する必要はない。

ソロ起動は通常仕様で、マルチ推奨はソロ禁止を意味しない。人数条件の正本は[Encounter Spec](../encounters/first-severance/ENCOUNTER_SPEC.md#admission-and-arena)、旧ビルド制限の撤廃は[ADR-0025](../adr/0025-public-solo-admission.md)、二窓の無敵保護は別の[ADR-0015](../adr/0015-console-only-single-pull-assist.md)を参照。ソロのプレイ可能性とマルチ同期・個別バランスの検証は区別する。

## 0. 最短の一窓デバッグ

公開版・開発版とも単独起動を許可する。ゲーム内コマンド、ビルドフラグ、起動オプション、二人目のキャラクターは不要。

1. 最新パッケージを読み込むようtModLoaderを再起動し、自分でHost & Playからワールドに入る。
2. Theater Dollを手に持って設置済みFoundation Coreをクリックし、フィールド展開後のReady受付へ進む。SOLO DEBUG等の文字表示は要求しない。
3. もう一度Coreを右クリックして自分のReadyを入れる。1/1で通常の開始演出に進む。
4. 見たい攻撃を確認する。敗北演出だけを見るなら、既存の `/convergence-down` で自分をDownにできる。単独Downは即座に敗北へ進み、3秒の結果演出後に通常の死亡UIへ戻る。意図的な死亡検証にはHardcoreを使わない。

HP/パイロンは2人用のままで、攻撃やフェーズをスキップしない。頭割りは1/1集合なら成功、散開は重なる相手がいないため成功する。同行NPC、無敵、自動救助はない。通常のフィールド空間・Core・進行条件も免除しない。パッケージが配置済みなら追加のBuild + Reloadは不要。

単独では二人目の画面、蘇生、頭割り同期、遠隔回線の遅延やマルチ難易度は検証できない。それらを見る回は従来のフレンドまたは§3の実client二窓へ戻す。

公開前もソロを無効化しない。[Release Process](../RELEASE_PROCESS.md#pre-release-gate)の実パッケージ検査で1–4人許可を確認する。将来の同行ミニオンによる参加者代替は別実装で、現時点では人数に含めない。

## 1. 方式の比較

| 方法 | 得意なこと | できない／偏ること | 判断 |
|---|---|---|---|
| 一窓Host & Play・開発用単独起動 | 攻撃・移動・VFX・音・開始/終了の短い反復 | 連携、蘇生、二画面同期、マルチバランス | **単純な確認はこれ**。§0の手順 |
| 実client A/Bを一人で操作 | 実packet、双方のUI、target割当、item操作、Down／蘇生の流れ | 同時の高度な回避・連携、遠隔回線の遅延 | 実際の二人分の入力/投影が必要な回 |
| 実client A/B＋Bへの開発用支援 | Aの回避練習、Bを救助係として待機 | 無敵Bが生存条件を変えるため、本番のwipe／難易度判定には使えない | §5の専用server consoleで一戦だけ許可。場面再実行は別の未実装機能 |
| Single Playerの単独起動 | 構え、BGM／SFX、負荷の反復 | remote client／server間の問題を検出できない | フラグ不要。通常の確認はHost & Playを優先 |
| 通常の同行NPC | Solo練習、役割の説明、物語演出 | player接続、inventory request、Down投影、二人目の画面 | 今は後回し。自動でRaid人数にはならない |
| domain／codec試験 | tick境界、重複request、人数計算、boundsを短時間で確認 | 実際の描画・音・input・他Modの干渉 | 変更したルールの補助に限定。手動試遊の代わりではない |

一人二窓で全場面をノーミス攻略する必要はない。**「今日は蘇生一回」「今日はcharge一回」だけを選ぶ**。完成判定にフルRaid一周を毎回要求しない。

## 2. 現在のコードでできること

### 実装済みの入口

- [FirstSeveranceRoster](../../Content/Encounters/FirstSeverance/FirstSeveranceRoster.cs)の対応範囲は1–4、通常の最低人数は2。§0の開発フラグをサーバー側で有効にした場合だけ1人を受け付ける。人数に加えて準備・回復・HP・NPC同期の経路も1人に対応する。NPCや架空の2人目を置く方式ではない。
- [FirstSeveranceDownCommand](../../Content/Encounters/FirstSeverance/FirstSeveranceDownCommand.cs)の **`/convergence-down`** は、実行した参加者自身を実験用Raid Downへ送る。Active戦闘が必要。[ClientActions](../../Content/Encounters/FirstSeverance/FirstSeveranceClientActions.cs)からrequestを送り、[runtime](../../Content/Encounters/FirstSeverance/FirstSeverancePrototypeCombatRuntime.cs)がsender、Fight、epoch、nonce、生存状態を検証する。
- **Resuscitation Kit／蘇生キット**で近くのDowned味方を即時蘇生する。非消耗・共有tokenなし。必要距離、復帰HP、保護時間、recipient-only lockoutの正本は[Revive Spec](../encounters/first-severance/REVIVE_SPEC.md)。単なるTerrariaのdead状態を治すitemではない。
- **`/convergence-cancel`** は実験の中断入口。意図的なDefeatと区別する。取消しで全員死亡させる試験にはならない。[Cancel command](../../Content/Encounters/FirstSeverance/FirstSeveranceCancelCommand.cs)

### 「Cheat SheetでBを無敵にすれば十分」とはまだ言えない

[runtime](../../Content/Encounters/FirstSeverance/FirstSeverancePrototypeCombatRuntime.cs)の`ApplyRaidDamage`は`Player.Hurt`を通らず、server側でlifeを減らすかRaid domainへDownを渡す。見るのはRaidの`InvulnerabilityUntilTick`等であり、外部ModのGod Mode flagをこの経路では確認していない。chargeには別に`player.immune && immuneTime > 0`の回避保護があるが、Stack／Spreadはその保護を使用しない。

したがって、一般の無敵Modがvanillaの被弾／deathを防いでも、**RaidのDown・割合damage・Defeatまで防ぐ保証はない**。Cheat Sheetの当該version全体のsource検証や実験は今回していないため、「全く効かない」とも断定しない。Bを無人で立たせる前にRaid側の明示的な保護が必要。

逆に無敵が通常deathを取り消す場合、Defeatの全員死亡を確認できなくなる。結果には「どちらのclientに、何の支援を有効にしたか」を残す。

## 3. 二窓を安全に用意する運用

### 3.1 一回だけ行う準備

1. 通常のgameを終了してから、**開発専用の保存先をA／B／serverの三つに分ける**。既存の通常save先、cloud save、repository内には置かない。
2. Aはいつもの装備を再現した**開発用copy**、Bは別名の開発用キャラクターにする。同じplayer fileを二つのprocessで同時に開かない。新規B＋Cheat Sheetで必要品を用意する方法が単純。
3. コピーするならplayerの`.plr`と`.tplr`、worldの`.wld`と`.twld`を対応する組で扱う。元のsaveは変更しない。意図的な全滅を行うのでHardcoreは使わない。Mediumcoreのdropも不要ならSoftcoreを使う。
4. 両profileの`Mods`／`enabled.json`／必要なMod設定を揃える。installed Workshop contentとConvergenceの同じpackageを使い、片側だけ古いModを読み込ませない。第三者Mod binaryをrepositoryへ入れない。
5. Bはwindowed・小さめの画面・必要ならReduced Effects。音はまずAのみで評価し、Bをミュートする。同じ効果音が二重に鳴る状況で音量や迫力を採点しない。

`-tmlsavedirectory`で指定したprofileでは、いつものキャラクターやMod設定が自動で見えるとは限らない。それはsave分離の結果である。開始ログの **Saves Are Located At** とロードしたMod版を最初の一度確認する。[保存先API根拠](#6-一次資料と再現条件)

### 3.2 最短: Host & Play＋二つ目をIP接続

tML公式[Basic Netcode](https://github.com/tModLoader/tModLoader/wiki/Basic-Netcode#testing-multiplayer-locally)は、通常はSteamの起動ボタンが二重起動を制限する一方、install directoryの`start-tModLoader.bat`から追加clientを起動し、最初のclientがHost & Play、二つ目が`localhost`へJoin via IPする方式を案内している。

各起動は**別のPowerShell／shortcutから一つずつ**行う。以下は移植可能な例であり、実機用launcherそのものではない。`<...>`は自分の値に置き換える。通常のSteam Launch Optionsや共通`cli-argsConfig.txt`を書き換えない。同時起動時に共通`steam_appid.txt`の使用中エラーが出た今回の環境では、先のclientが起動してから次を起動することで接続できた。

```powershell
# Window A: 操作側
Set-Location "<tModLoader install directory>"
.\start-tModLoader.bat -tmlsavedirectory "<DEV_ROOT>\client-a"
```

```powershell
# Window B: 補助側。別のterminalから起動する
Set-Location "<tModLoader install directory>"
.\start-tModLoader.bat -tmlsavedirectory "<DEV_ROOT>\client-b"
```

Aで開発用worldをHost & Play。Bで別キャラクターを選び、Join via IP → `127.0.0.1`（同じPC）→ Aが選んだportへ接続する。Steam経由の自己招待や別Steam accountを前提にしない。両者をCore周辺へ集め、通常どおりReady／開始する。

同一PCの実機接続結果は[Status](../STATUS.md)を参照。これはHost & Play経路や全focus操作を検証したという意味ではない。二人目が切断される場合はserverの拒否理由を確認する。課金・アカウント追加や認証回避を先に行う必要はない。

### 3.3 server判定を確認する時: 専用server＋A/B

```text
client A（操作） ─┐
                  ├─ 127.0.0.1:7777 ─ local Dedicated Server
client B（補助） ─┘                    （Raid判定・専用world）
```

三つの保存先を使う。serverがworldを所有し、A/Bには別々のplayerを持たせる。AはHost & Playせず、**AもBもJoin via IP**。既存のHost & Play serverとは同時に同じportを使わない。

起動用の開発専用config例（自動生成済みのfileではない）:

```ini
# <DEV_ROOT>を空白のない開発用rootへ置換する
world=<DEV_ROOT>\server\Worlds\RaidTest.wld
port=7777
maxplayers=4
upnp=0
```

world fileは事前に用意した開発用の組を使う。`world=`は完全なfile pathとし、`worldpath`だけで相対file名の解決を期待しない。起動は別terminalから:

```powershell
Set-Location "<tModLoader install directory>"
.\start-tModLoaderServer.bat -nosteam -config "<DEV_ROOT>\server\local-raid.txt" -tmlsavedirectory "<DEV_ROOT>\server" -ip 127.0.0.1 -noupnp
```

この例はserverのlisten addressをloopbackへ絞り、UPnPを使わない。公開port開放やrouter設定変更は不要。serverの起動ログでconfig・world・portを確認してから、前節と同じA/Bを接続する。

**注意:** `-nosteam`を「配布版clientを完全にSteam非依存にする万能flag」と解釈しない。固定sourceの`SocialAPI.Initialize`ではclient側の当該条件が`#if DEBUG`内にある。ここでは公式server wrapperが解釈するserver用flagとしてのみ使う。[固定source](#6-一次資料と再現条件)

### 3.4 二窓固有の落とし穴

- 非focus側は更新／描画頻度が下がる場合があると公式も注意している。Alt-Tab後だけのカクつきを直ちにネット同期bugと判定しない。server tickと双方のlogを比較する。
- 同じPCではGPU／CPU／memoryを二つのゲームで使う。Bの画質を下げた結果はA単体の最高画質評価や二台PCのperformanceを証明しない。
- Bにminionを大量に出すとboss HP、攻撃の見やすさ、CPU負荷が変わる。救助専用回ではBの自動攻撃を外す。
- keyboard／mouseの通常操作は基本的にfocusした一方だけ。同時に左右へdashする試験は無理に行わず、片側だけで成立する観察へ限定する。
- 接続の維持と最低人数が確認できた後は、毎回新しいworld／characterや全互換testを作らない。

## 4. いま可能な短い検証

### A. 蘇生だけを試す

1. A/Bを安全な足場で8tiles以内へ近づけ、Bが蘇生キットを持つ。Active開始直後など次の危険まで余裕がある場面を選ぶ。
2. Aで`/convergence-down`。両方を同時にDownさせない。
3. Bへ切り替えて、Aの近くでキットを一度使う。空中でもchannel維持は不要だが、最初は地面で条件を単純にする。
4. Aへ切り替え、立ち上がり・復帰HP・操作復帰・60秒の蘇生不可表示を見る。serverのDown→revive結果と両画面が対応したら、この回は終了してよい。

lockoutを変更した時だけ、同じAをもう一度debug DownにしてBの蘇生が拒否されることも確認する。**Downの期限は30秒なので、Downしたまま60秒のlockout満了を待つ手順にはしない。** 期限切れでRaidが終了し得る。満了後の再蘇生を調べるなら、AがAliveのまま60秒経過できる条件を作ってから再度Downさせる。安定した条件づくりには次節の開発支援が必要になる場合がある。

被弾→Downの変換はこのdebug commandだけでは検証していない。それを変更した回は、一発の実攻撃からDownさせる確認を別に行う。

### B. 頭割り／散開だけを試す

頭割りではBを固定点へ置き、AがBのそばへ集まる。散開ではBをその場に残し、Aだけが十分離れる。どちらを狙ったマーカーでも参加者全員の実位置で判定される。2人の同時dashは不要。

成立時の0damage、見た目、成功cueが観察できたら終了してよい。途中でBを明示Downした回や保護を解除した回は、成立人数が変わるので同じtestとは扱わない。

### C. 高速energy／背景／soundだけを試す

Aへfocusを置き、Bのaudioはミュートして一つの攻撃を観察する。狙い固定→発動の境目と回避理由を見る。Bの保護は§5で明示する。場面再実行の支援はまだないため、必要になった時だけ別の小さな実装判断とする。

### D. 全滅／cleanupを変更した回だけ

両者を通常ルールに戻し、無敵・auto-rescue・回復補助を切る。先にA、次にBをdebug Downし、Defeatが一回だけ確定し、参加者deathとBoss／VFX／音の終了が揃うかを見る。Hardcoreは使わない。単にeffectや文書を調整した回にこの試験を追加しない。

## 5. 一戦限定・server console専用のDebug Assist

1. 開発用Dedicated Serverの起動引数に **`-convergence-dev-assist`** を加える。通常起動、Single Player、Host & Playのclient chatには許可入口がない。
2. A/Bを同じworldへ接続し、安全なCore付近へ集める。無敵は**戦闘中だけ**なので、準備中の通常敵には別途対処する。Cheat Sheetの一時God Modeを移動に使った場合は、試験開始前に両側でOFFを確認する。外部God ModeはRaidの保護の代用ではない。
3. **server console**で次を実行する。chatへ`/`付きで送らない。表示されるBの現在のslotとepochを使い、名前や古いslotを流用しない。

```text
convergence-assist status
convergence-assist off
convergence-assist arm <Bのslot> <Bのepoch>
```

4. 10分以内にCoreを右クリックしてPreparingへ入る。許可は一回だけ消費され、凍結roster内のBとそのFightへ結び付く。対象がrosterにいなければ保護されない。許可を取得したPreparingだけReady期限が10分になり、通常の60秒期限は変わらない。
5. BのCore右クリックでBだけReady。Aの開始操作をユーザーへ渡す時は **Ready 1/2** で止め、AのReadyを代行しない。Core右クリックでAがReadyになると戦闘が始まる。
6. consoleの`status`で結び付いたFight／participant／slot／epochを確認する。戦闘開始時に補助側へ保護の通知が出る。再戦は自動継承しないため、Idleで改めて`arm`する。

### 効果と解除

- Bは人数、頭割りの分母、散開、target候補に残る。保護付きの勝利を通常難易度のclear証拠にしない。
- Raidの通常collision判定後、本来HP damage／Downを与える箇所だけを抑止し、`DebugAssistWouldHit`を記録する。通常の無敵時間で既に除外されたhitや全ambient被弾を網羅するlogではない。
- 明示的な`/convergence-down`は通す。蘇生キットと60秒lockoutは通常の実requestで試す。auto-rescueやNPCはない。
- 戦闘中の正規snapshotを受けたAlive補助役だけ、通常Hurt／DoT／deathにも限定保護を持つ。client側は5秒TTLで更新し、古いsnapshotだけでは永続化しない。
- **`convergence-assist off`** はpendingとactiveの両方を解除する。client表示／保護への通常の反映は次のsnapshot（30 ticks以内）、受信停止時はTTL満了となる。
- exact-Fight cleanup、terminal、disconnectによる終了、World／Mod unloadで解除。Defeatの死亡処理より先に解除する。他Modが死亡を取り消す可能性まで無効化する仕組みではない。
- AllParticipantsDowned確認時は先に保護と外部God ModeをOFF。Bが常にAliveのままでは通常の全Down判定を再現できない。

### 必要になってから追加する支援

| 案 | 利益 | 制限 |
|---|---|---|
| Bへの集合／散開位置の指定 | 二窓の往復操作を減らす | 本来の移動速度・回避テストとは分離。server側で瞬間移動を連打して同期品質を測らない |
| 一つのattack sceneの再実行 | artwork／soundをすぐ見直せる | 古いcastやhazardを片付け、新しいserialで開始。途中phaseの値だけ書き換えない |
| Bのauto-rescue | Aの回避を連続練習できる | 手動kit inputの試験を代替しない。無敵と併用すると回復可能性の評価も変わる |
| preview専用のdamage OFF | ソロで描画だけ素早く確認 | 本番Raidの最低人数を変更しない。実戦結果・報酬へ混ぜない |

Bの一戦限定保護以外は、必要になった時の追加候補であり今回の操作準備には含めない。

### NPC案の置き場所

将来のソロ用練習には、集合／散開／救助を教える同行NPCも有用。ただし現行rosterはPlayer前提で、NPCを含めるにはassignment、距離判定、被弾、Down、revive、勝敗条件のadapterを設計する必要がある。見えないPlayer slotを捏造する方法は、接続・epoch・正規requestを迂回するため避ける。

WotGのSolynは物語・戦闘補助の参考であって二人目のclientの代替ではない。[WotG研究 F10](../research/WOTG_RAID_BENCHMARK.md#f10--solynはもう一人の実clientではない)を参照。NPC previewを作っても、実packetを使う二窓の短い確認は残す。

## 6. 一次資料と再現条件

調査対象は[Version Matrix](../VERSION_MATRIX.md)のtML `v2026.07.3.0`／SHA `666f69962d3bdffde54fc14025f02634965b4e7c`。Terraria／Calamity等のversionとenabled Modsは[Status](../STATUS.md)を参照。以下は2026-09-06に公開source／公式wikiを読み取って確認した。公式tML sourceは[固定MIT license][tml-license]。API名と挙動だけを参照し、engine codeを転載していない。

| 質問／検索symbol | 根拠 | 確認したこと／限界 |
|---|---|---|
| 一人で複数clientを起動できるか | [公式Basic Netcode][tml-basic]（moving guidance） | `start-tModLoader.bat`追加起動、Host & Play＋localhost、非focus側の更新頻度に注意という公式手順。実機で成功したという証拠ではない |
| 保存先を分けられるか | [Command Line wiki][tml-cli]＋[Program.TML.cs][tml-program] `SetSavePath` | `-tmlsavedirectory`はそのままのpathを`SavePath`／`SavePathShared`へ使う。`-savedirectory`のような`tModLoader`末尾自動付加とは異なる |
| server wrapperと通信範囲 | [start-tModLoaderServer.sh][tml-server]、[固定serverconfig][tml-config] | `-nosteam`をwrapperが解釈し、`-server`とconfig等を渡す。`-ip`、`-noupnp`、`world`、portの公式引数／設定例。実機launcherと接続結果はStatusへ分離 |
| `-nosteam`を配布版clientへ使えるか | [SocialAPI.cs.patch][tml-social] `Initialize` | client側の除外条件は`#if DEBUG`。通常配布clientを完全非Steamにできるとは案内しない |
| 二窓のlogは上書きされるか | [Logging.cs][tml-logging] `InitLogPaths`／`GetFreeLogFileName` | 使用中のlogがあると`client2.log`等のsuffixを選ぶ。番号は固定の役割ではない。起動順だけでA/Bを決めず、保存先・character・接続時刻で照合する |
| Raidの無敵・Downの経路 | 本書§2のローカルsource、[ADR-0015](../adr/0015-console-only-single-pull-assist.md) | 外部God Modeに依存しないRaid damageがある。feature所有のconsole許可とsnapshotだけが補助保護を付与する |
| consoleとゲームthread／保護hook | [ModCommand][tml-command]、[Main.TML][tml-main]、[ModPlayer][tml-player] | ConsoleはChatと別のcommand type。QueueMainThreadActionでauthority変異を委譲。ImmuneTo／PreKill等は同期や他Modとの相互作用まで自動で保証しないため、局所leaseとcleanupを自前で設計 |

### 最小の記録

一回の確認につき、build／protocol、Host & Playか専用serverか、2実client・1操作人、A/Bの支援状態、選んだ一場面の結果だけ残す。server.logとA/Bのclient logは再起動前に必要範囲を外部保存し、raw log・個人path・world・playerはrepositoryへ入れない。

`/convergence-down`→蘇生のような具体操作と時刻があれば、logと対応づけやすい。二窓を試すためだけに全domain／全compatibility suiteを走らせない。変更ごとの必要範囲は[Verification Matrix](../../.agents/skills/develop-convergence-raids/references/verification-matrix.md)に従う。

### 残る検証の境界

- 同一PCの接続は、Steam-friend接続、実際の遠隔遅延／loss、別GPU、別人の反応時間を証明しない。
- 一人＋無敵補助は、二人が普通に遊んだ時の公平さ、DPS、救助の難しさ、全滅しやすさを証明しない。
- 2人で通ったことは、3–4人のassignment／safe space／packet boundsを証明しない。人数依存部分を変更した回に追加する。
- 人が来た時は、その回で変えた連携一場面だけを確認すればよい。全手順を毎回やり直す必要はない。

[tml-basic]: https://github.com/tModLoader/tModLoader/wiki/Basic-Netcode#testing-multiplayer-locally
[tml-cli]: https://github.com/tModLoader/tModLoader/wiki/Command-Line
[tml-program]: https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Program.TML.cs
[tml-server]: https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/release_extras/start-tModLoaderServer.sh
[tml-config]: https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/release_extras/serverconfig.txt
[tml-social]: https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Social/SocialAPI.cs.patch
[tml-logging]: https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/Logging.cs
[tml-license]: https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/LICENSE
[tml-command]: https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModCommand.cs
[tml-main]: https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/Main.TML.cs
[tml-player]: https://raw.githubusercontent.com/tModLoader/tModLoader/666f69962d3bdffde54fc14025f02634965b4e7c/patches/tModLoader/Terraria/ModLoader/ModPlayer.cs
