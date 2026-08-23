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

## Downed and Revive domain tests

Run the pure service tests before attaching Terraria hooks, then repeat the same
cases in Single Player, Host & Play, and Dedicated Server. Initial timings are
120 ticks to channel, 1,800 ticks before Downed timeout, and 1,800 ticks of
disconnect grace.

The dependency-free console harness at
[`Tests/Convergence.DomainTests`](../Tests/Convergence.DomainTests/) links the
production revive sources, tModLoader-free feature boundary, Arena blueprint,
outsider policy, and immutable Boss/phase plan directly. It therefore exercises
the same internal types without requiring Terraria or tModLoader:

```bash
dotnet run --project Tests/Convergence.DomainTests/Convergence.DomainTests.csproj
```

The project must remain excluded from `.tmod` packaging through `build.txt`.

| Case | Expected authoritative result |
|---|---|
| 2/3/4-player roster creation | 1/2/3 shared tokens |
| authority lethal event | death is represented as `Downed` once; duplicate is no-op |
| all roster members Downed on same/different ticks | one terminal `AllParticipantsDowned` failure |
| final Downed command before tick commit | no terminal result until `CommitTick` |
| command for an already committed tick | rejection and no state/revision change |
| same-tick competing revive starts submitted in either order | lower stable reviver `ParticipantId` wins the target reservation in both runs |
| empty revive-start batch | rejected; no batch boundary or state mutation |
| revive-start batch larger than the frozen roster | whole batch rejected before sorting or mutation |
| one batch repeats a stable reviver `ParticipantId` | whole batch rejected before any channel reservation |
| revive-start batch mixes Fight IDs or authority ticks | whole batch rejected before its boundary is marked or any command is applied |
| second non-empty revive-start batch after one start was accepted for that tick | `revive.start_batch_already_processed`; no command is applied |
| successful 120-tick channel | one token consumed, target Alive, restore/invulnerability/weakness event data |
| channel at tick 119 | no completion |
| interrupt or Downed deadline exactly on completion tick | cancellation/timeout wins deterministically |
| movement or damage interrupt | channel cancelled, reservation released, no token consumed |
| delayed Cancel for an older channel nonce | newer channel remains active |
| target disconnects or becomes invalid | channel cancelled and cannot complete |
| reviver disconnects or becomes Downed | channel cancelled and cannot complete |
| two simultaneous channels | tokens reserved atomically; no overcommit |
| Downed timeout with zero available tokens | terminal `DownedTimeoutWithoutToken` failure |
| Downed timeout with tokens remaining | participant Eliminated; remaining party may continue |
| no connected Alive participant within reconnect grace | encounter remains recoverable |
| reconnect grace expires with no Alive participant | terminal `NoAvailableParticipants` failure |
| terminal failure before Cleanup | Downed/Eliminated control locks remain projected |
| timeout and disconnect expiry on the same tick | `RaidFailed` is the final event; no later mutation event |
| old Fight ID command | machine-readable rejection, no revision/state change |
| rejected command carrying a far-future tick | a later valid lower-tick command is still accepted |
| old connection epoch after rejoin | binding rejection, no participant control projection |
| Terraria slot reused by a different connection | no mutation without the current epoch and Participant ID |
| duplicate/older revive nonce | rejection and no second channel |
| reconnect with newer epoch | stable Participant ID and prior combat state retained |
| reconnect at/after grace deadline before commit | explicit grace-expired rejection |
| newer rejoin arrives before an old disconnect callback | newer binding supersedes; old callback is rejected |
| stale authority tick | rejection and no deadline rollback |
| undefined interrupt reason | rejection, no channel mutation, no authority-tick ratchet |
| zero-nonce or no-active-channel interrupt at a future tick | rejection/no-op respectively; neither makes an earlier valid command stale |
| Apply emits an event before boundary Tick | next Active Tick returns observable change |
| start batch emits one or more events before boundary Tick | next Active Tick returns observable change across the whole batch |
| exact-Fight Cleanup called twice | success and empty state both times |
| stale-Fight Cleanup | rejected/internal failure; owning state is not silently released |
| valid Core/world geometry | exact 320x140 Arena, inset Barrier, and four deterministic Pylon slots |
| Arena fits the World but violates the 20-tile edge margin | layout rejection before any World mutation |
| outsider threshold escalation | warning/suppression/ejection/exclusion returned in deterministic order |
| default Boss/phase plan construction | form/part references, reachability, terminal Last Stand, and finite loop policy validate |

Terraria integration adds the following mandatory fault tests:

- lethal damage interception does not run on multiplayer clients;
- Downed projection blocks movement, item use, combat, and further damage without
  setting permanent vanilla/Calamity flags;
- an Eliminated participant remains control-locked until encounter cleanup;
- moving, taking damage, teleporting, changing mount/hook state, or losing the
  target emits at most one channel cancellation;
- revive applies server-owned life restoration and synchronizes it once;
- a disconnect callback captured before slot reuse cannot Down, revive, cancel,
  or clear the replacement player;
- wipe requests `EncounterEndReason.Defeat`, publishes the terminal snapshot,
  and then releases every participant projection during normal cleanup and World unload.

The death hook, `ModPlayer` control adapter, typed revive packet DTOs, and feature
snapshot codec are not present in the bootstrap implementation. Their absence is
a release blocker, and activation remains safely denied until they are tested.

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
