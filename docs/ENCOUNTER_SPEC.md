# Encounter Specification

Boss working title: **The Choir Beneath the Ice**  
Event working title: **Third Severance**  
Location working title: **Erebus Polar Citadel**

名称はすべて仮称。公開前に独自性と検索可能性を確認する。

## Target duration

- 初回clear: 8～12分
- 習熟party: 5～8分
- Last Stand: 30～45秒
- Weak Point exposure: 10～15秒
- loop上限: 2～3回後にHard Enrage

## Phase overview

### Phase 0 — Base Activation

Arena、Fight ID、参加者、識別符号、seedを確定し、全clientが同じsnapshotを受け取ったことを確認する。

### Phase 1 — Seal Release

人数と同数のResonance Pylonを生成する。各playerは対応Pylonへ最大damageを与え、時間内に全基を破壊する。失敗は即wipeではなくOverloadとなり、3 stackでHard Enrage。

### Phase 2 — Part Break

限られた時間で一部だけを破壊し、後半の難しさを選択する。

| Part | Governs | Destroyed effect |
|---|---|---|
| Crown | target laser / homing / precision | telegraph延長、精度低下 |
| Wings | charge / sweep / field traversal | 頻度・速度低下、安全地帯拡大 |
| Heart Casing | DR / weak point closure / recovery | exposure延長、DPS check緩和 |

全partを同時に破壊できる時間は与えない。

### Phase 3 — Coordination Sequence

- **Stack**: 対象円へ集合しdamageを分散。成功でWeakness。
- **Spread**: 全員のAoEを重ねず四方向へ配置。
- **Targeted Line**: 対象者がbossの向きを誘導。
- **Bait**: 最遠player、直近burst、debuff保持者など、UIで明示した規則により対象を決める。

固定Tank classは作らず、位置取りにより一時的なbait役を生む。

### Phase 4 — Personal Effigies

各playerへ対応Cloneを割り当てる。Cloneは対応playerから本来のdamageを受け、主要Damage Classに応じて攻撃を変える。

- Melee: 接近、真近接、parry/距離管理
- Ranged: 狙撃線、drone、射線管理
- Magic: 反射、mana領域、遅延爆発
- Summoner: 敵性minion、whip tag、target指定
- Rogue: stealth、偽telegraph、Stealth Strike

先に終えたplayerは直接他人のCloneを倒さず、回復領域、弱体化装置、移動強化、telegraph延長で支援する。失敗Cloneはbossへ吸収される。

### Phase 5 — Weak Point Exposure

短いBurst Window。通常DRを解除し、class resourceとparty debuffを集中させる。削り切れなければloopへ戻りOverloadを加える。

### Optional — Split Reality

4人は2:2、3人は2:1、2人は1:1に分かれ、左右の対象を近いタイミングで閾値へ到達させる。Vertical Sliceには含めない。

### Last Stand

HP 1～3%で固定attack sequenceへ入る。通常boss HP表示を抑え、全員へ一度ずつ個人攻撃を行った後にCoreを露出する。死亡者がいてもclear可能だが、全員生存時が最短・完全な演出となる。

## Player count adaptation

| Players | Pylons | Stack | Pair/Split | Clones |
|---:|---:|---|---|---:|
| 2 | 2 | 原則2人 | 1:1 | 2 |
| 3 | 3 | 2～3人 | 2:1、単独側を補助 | 3 |
| 4 | 4 | mechanicごとに指定 | 2:2 | 4 |

HP倍率だけで調整しない。対象数、required participants、safe area、補助装置、時間制限を個別に調整する。

## Failure language

Soft Failure候補:

- Stack人数不足 -> Raid-wide Damage Down
- Spread重複 -> 追加damage/debuff
- Pylon遅延 -> Overload
- Clone失敗 -> boss強化stack
- exposure不足 -> loop + Overload
- Revive判断遅延 -> token消費または残存人数で継続

避けるもの:

- 予告なし即死
- 高pingで成立しないframe-perfect入力
- 一人の一回のミスによる無条件wipe
- 画面外・不可視攻撃
- class不在で処理不能
- 4人分の完全な弾幕を単純加算

## Downed and Revive (later milestone)

Raid中の0 HPは通常死亡ではなくDownedへ遷移させる。約2秒の静止channelで蘇生し、一定HP、短い無敵、Weaknessを付与する。共有token初期案は2人=1、3人=2、4人=3。全員Downedまたはtokenなしでtimer切れとなった場合にwipeする。

標準死亡の差し替えは他Modとの競合リスクが高いため、Arenaとnetwork stateが安定するまで実装しない。

## Class synergy constraints

class特性は小さな攻略差に留め、必須条件にしない。

- Summoner whip tag -> part break補助
- Rogue Stealth Strike -> Weakness補助
- Magic hit -> exposure用charge
- Melee true melee -> 短いorientation lock
- Ranged continuous hit -> Armor Crack

すべてのclassless/support構成にも基本処理手段を用意する。

## Vertical Slice exit criteria

- 2～4人で同じphase/timer/assignmentが表示される。
- Stack、Spread、Weak Point、簡易Cloneがserver判定で動く。
- 一人がDowned、別playerがReviveできる。
- 高pingでもtelegraph時間と判定が矛盾しない。
- 勝利、wipe、cancel、disconnectの全経路でCleanupされる。
