# Test Plan

## Quality gates

各milestoneは次の順で通す。

1. compile
2. client load
3. Single Player smoke
4. Dedicated Server load
5. 2-player smoke
6. feature-specific matrix
7. Cleanup invariant check

compile error、unknown packet exception、server crash、stale Raid stateを残したまま次のmilestoneへ進まない。

## Version evidence

各test runでlogから次を記録する。

- OS
- Terraria version
- tModLoader full version and branch
- Calamity version
- Addon commit SHA and version
- enabled Mod list
- client count
- network condition

## Milestone 0 tests

- minimal Modがbuildされる。
- Calamity `2.2.2`へのhard referenceが解決する。
- clientがModをreloadできる。
- Dedicated Serverがheadlessでloadできる。
- clientとserverでMod version mismatchが拒否される。
- 非対応Calamity versionでEncounter activation policyが拒否する。
- Third Severance availability policyが未実装Raidの起動を拒否する。

## Arena validator tests

Milestone 1でExo Mechs/Supreme Calamitas進行adapterを実装し、未達・API failure・予期しないreturn typeを安全に拒否する。

| Case | Expected |
|---|---|
| valid 320x140 space | `Preparing/AwaitingReady`へ遷移 |
| world edge overlap | issue code付きで拒否 |
| invalid/missing Core TE | 拒否、state unchanged |
| second Core activation | single-managed-Encounter error |
| chest in bounds | 拒否 |
| protected structure tile | 拒否 |
| broken foundation | 拒否 |
| active boss/event | 拒否 |
| requester too far away | 拒否 |
| 1 eligible player | 参加人数error |
| 5+ candidates | 明示選択要求、自動startしない |

Validatorは失敗時にTile、liquid、wire、entityを変更してはならない。

## Ready and lifecycle tests

以下のReadyはgeneric lifecycleではなく、Raidの`Preparing` substateを指す。

- 2人、3人、4人で全員Ready。
- 一人がReadyを解除。
- Ready timeout。
- 起動者cancel。
- Ready中にCore破壊。
- Ready中に起動者disconnect。
- Ready中にparticipantが2人未満になる。
- lifecycle transitionとRevisionが全clientで一致。
- 古いEncounter Sequence/Fight IDのpacketが無視される。

## Barrier tests

- 左右上下の各edgeへ通常移動。
- dash、hook、mount、knockback。
- recall、pylon、bed、Calamity teleport item。
- serverによる強制teleport。
- 100/200/300 ms RTTとpacket loss。
- client predictionとserver correctionで永久rubber-bandしない。
- non-participantへの影響が仕様どおり。

## Mandatory multiplayer matrix

- hostが対象。
- non-hostが対象。
- hostがDowned（後続milestone）。
- non-hostがDowned。
- 同時に2人Downed。
- Revive channel中に被弾。
- phase transition tickで死亡。
- 途中離脱。
- 途中参加。
- boss kill直前のdisconnect。
- boss despawn条件。
- Arena外へのexternal teleport。
- Projectile上限付近。
- 他大型Content Mod併用。
- server再起動後に一時stateが残らない。

## Cleanup fault injection

各stepで例外またはmissing entityを模擬し、それでも最終invariantを満たすことを確認する。

- Coreが先に消える。
- Barrier VFXが既に消えている。
- participant slotが再利用される。
- temporary NPCが手動kill済み。
- duplicate Cleanup call。
- stale Fight IDでCleanup要求。
- World unload中のCleanup。

## Packet robustness

- unknown PacketType。
- truncated payload。
- enum範囲外。
- oversized count。
- invalid tile coordinate。
- participant以外からのReady。
- spammed activation/ready request。
- reordered snapshot/delta。
- duplicate delta。
- previous fightのdelayed packet。

不正packetでserver threadを例外終了させない。

## Performance budgets (initial)

| Metric | Initial budget |
|---|---:|
| Arena validation | activation時 10 ms未満を目標、50 msで要調査 |
| Active server update | average 1 ms/tick未満 |
| Raid custom traffic | steady state 5 KB/s/client未満を目標 |
| Networked projectiles | player数に比例して4倍化しない |
| Cleanup | 1 tick内、重い場合も新規fightをblockして完了 |

数値はprofilingで更新する。見た目のparticle数をserver entity数として数えない。

## Visual and accessibility QA

- 1920x1080、2560x1440、ultrawide。
- UI scale 100～150%。
- color-only assignmentが存在しない。
- markerがboss、projectile、damage textに埋もれない。
- flash、shake、afterimage軽減設定。
- 4人分のSpread markerが判別可能。
- phase titleがgameplay telegraphを隠さない。

## Audio QA

- loop click/popなし。
- phase switchで二重再生または無音が続かない。
- music volume sliderに従う。
- unfocus/pause/resumeの挙動。
- Dedicated Serverでaudio accessしない。
- 途中参加でgameplay stateは正しく、音楽がずれても判定へ影響しない。
- 使用assetのsource/license/creditが記録されている。
