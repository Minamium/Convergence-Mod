---
doc_id: project.brief
document_type: overview
status: accepted
owners:
  - project
last_reviewed: 2026-09-04
source_of_truth_for:
  - project.product_scope
aliases:
  - project brief
  - product direction
related_code:
  - build.txt
  - Common/Compatibility/Calamity
related_docs:
  - project.status
  - encounter.first-severance.overview
---

# Project Brief

## One-line concept

Shadowspec級装備の2～4人パーティーが、極地の終末研究・収容施設に展開された固定Raidフィールドで、DPSと位置取りを協力して処理する最終決戦。

## Product position

Convergenceは、Calamity終盤の個人回避・火力最適化を土台に、MMORPG Raid型の協力ギミックをTerrariaの2D移動とserver authorityへ翻訳する。弾幕密度とHPだけを増やしたSuperbossにはしない。

想定party:

- Exo MechsおよびSupreme Calamitas撃破済み;
- Shadowspec級装備を利用可能;
- 2～4人の固定または準固定party;
- 5～12分程度の反復攻略を受け入れられる。

5～12分は将来拡張したRaid全体のproduct goalであり、最初のvertical sliceは2～4分を暫定目標とする。Soloは初期release対象外。将来対応するなら協力ギミックの数値を縮めるだけでなく、独立した個人Superbossとして設計する。

最初のRaidは基盤の実証対象であってAddon全体の上限ではない。長期的には独立Boss、追加Raid、World content、進行、Item、Utility、演出まで広げ、Calamity級の独自Content Modを目指す。

## Dependency strategy

- **Stage A — Calamity addon:** Calamityの進行、class連携、Shadowspec級balanceを隔離adapter越しに利用し、最初のRaidを完成させる。
- **Stage B — Portable Raid core:** project-ownedな進行/class/balance capabilityへ置換し、Encounter、Networking、Arena、Raid domainからCalamity型と名称を排除する。
- **Stage C — Standalone Content Mod:** 独自進行、素材、装備、recipe、World content、balanceを実装し、hard dependencyを外す。残す場合のCalamity対応はoptional adapterとして再決定する。

`modReferences`の削除だけでStandaloneにはならない。各stageはbuild/load/multiplayer/release matrixを持つ。詳細は [ADR-0006](adr/0006-staged-calamity-independence.md)。

## Design pillars

1. **Execution** — 回避、dash、位置取り、火力維持。
2. **Coordination** — 頭割り、散開、蘇生、将来のpair/誘導。
3. **Optimization** — Pylon DPS、Core burst、装備とparty構成。
4. **Clarity** — 死因、assignment、成功/失敗、次の改善が読める。
5. **Recovery** — 軽微な失敗とDownedを立て直せるが、立て直しには機会費用がある。
6. **Extensibility** — feature ownershipとserver/client境界が追加Boss/Raidを妨げない。

## First Raid

最初のRaidは`First Severance` / `第一断絶`。target code name/keyは`FirstSeverance` / `first_severance`。現行コードの`ThirdSeverance`は未改名のlegacy bootstrapである。

最初のplayable loop:

```text
起動 -> Boss出現 -> Pylon DPS -> 頭割り -> 散開 -> Core露出
                                           -> HPが残ればPylonへ戻る
```

頭割りはserver-owned damage poolを必要人数で分配する。Raid中のeligible lethalはDownedへ変換し、他playerが専用itemを使ってchannelして蘇生する。BossのAccepted境界は単純な単一NPC/bodyであることまでで、中央Core、破損円環1本、左右アーム2本は最初のprovisional placeholderとする。

## Player experience goals

- 2～4人で同じphase、timer、assignment、結果が見える。
- 一人の通常ミスはsoft failureになり、即座に全滅させない。
- DPSを出す時間、mechanicへ移動する時間、蘇生する時間が意味あるtradeoffになる。
- 特定classがいなければ処理不能、という設計にしない。
- host/non-host、高ping、途中切断でauthority結果を変えない。
- 演出を減らしてもtelegraphとstateが読める。

## Setting and presentation

舞台は極地の巨大工業研究・収容施設。氷、暗い金属、白、赤、黒、円環、封印柱、観測装置、無機質な警告を視覚語彙とする。

`The Null Cantor`、`The Choir Beneath the Ice`、`Erebus Polar Citadel`、`Pale Meridian Containment Complex`などはすべてprovisional。既存作品の機体、logo、顔、固有語、構図やCalamity assetを再現・抽出しない。

## Initial implementation scope

- Windowsでの候補version build/load/Dedicated Server確定;
- atomic `ThirdSeverance` → `FirstSeverance` rename;
- Core/Arena/roster/Ready/logical Barrier/cleanup;
- bounded transport、feature snapshot、client replica;
- simple Boss/Pylons and repeated loop;
- server-resolved Stack/Spread/Core damage window;
- instrumented and tested Downed/Revive adapter/item;
- 2/3/4-player multiplayer acceptance evidence。

## Non-goals for the first playable slice

- Part Break、Targeted Line/Bait、Personal Effigies、Split Reality、Last Stand;
- multipart Crown/Wings/Heart Casing;
- complete Boss attack catalog、final balance、reward、music、shader、production sprite;
- Subworld/dimension、Solo、multiple simultaneous arenas;
- Calamity内部実装/assetのcopyまたはredistribution;
- Stage C standalone content。

## Viable first-Raid definition

The slice is viable only when 2–4 players can activate, Ready, clear/fail, Down/revive, disconnect, cancel, and unload on Dedicated Server with identical server-owned outcomes and zero stale exact-Fight state. “The domain class exists” or “one client loads” is not completion.
