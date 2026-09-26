---
doc_id: adr.0028
document_type: adr
status: accepted
owners:
  - architecture
  - gameplay
last_reviewed: 2026-09-27
source_of_truth_for:
  - authority.azure_cathedral
aliases: []
related_code:
  - Content/Encounters/AzureCathedral
related_docs:
  - encounter.azure-cathedral.spec
  - adr.0026
---

# ADR-0028: Azure Cathedral native actors and hazards

## Decision

The requested independent worm/girl Raid is definition-routed `azure_cathedral`, termination schema4/version1, protocol58. Common packet operation IDs remain unchanged. Its runtime owns the pedestal lease, connected Ready roster, frozen HP scaling, attack clocks, target locks, head movement,23 native worm parts, the girl, hazards and one terminal commit. It neither imports a previous feature runtime nor adds a global feature switch. Active state stays ephemeral.

For this feature only, extend ADR-0002's native-damage exception to normal hostile Projectile and worm contact hits on the receiving participant. The authority supplies the attack geometry/live window, while native Terraria applies dodge/immunity/defense/accessory hooks. No manual Hurt loop, direct statLife mutation, custom damage result packet or assumed cheat resistance. NPC weapon damage stays native; body segments share their head's `realLife`. Both named actors must die. Native death/inter-mod accessories are not a Doll Downed/Revive contract.

This independently scoped decision follows the already pinned tML2026.07.3.0 native Projectile/Player API evidence linked in [ADR-0026](0026-crimson-score-and-native-projectiles.md). It does not change Doll, Ghost Samurai, Scarlet or Oboro authority. The new feature retains 1–8-member bounded ordered connection GUIDs, strict finite geometry/timing, safe unowned native envelopes, monotone accepted snapshots, server-rejected client ExtraAI authority mutation, and exact-Fight cleanup including partial construction.

Client-owned scene/audio/shader resources consume these projections. Missing/stale ownership disables hazards. Timeout/death/cancel/unload removes every owned projectile/NPC and releases the pedestal; no UI/input flags survive. Clients joining after Ready are spectators. Native music is background-only, not the source of hit timing.

## Evidence boundary

### 2026-09-20 follow-up (protocol59)

Retain the original decision; add bounded Stack/Spread plan/verdict/recipient-impact envelopes. `AzureChorusDirector`, owned by the one authority runtime, resolves the frozen roster at its deadline; `AzureChorus` is a harmless synchronized marker, and `AzureChorusStrike` gates the ordinary native hit to exactly its resolved receiving player. Late/stale verdicts cannot revert a resolved outcome. No new common packet IDs, client decisions, persistence or feature-runtime dependencies.

Defeat is an idempotent runtime latch, separate from actor existence. Only a living, already-summoned worm requires the full linked chain. Retire owned defeated segments without invalidating the survivor. The retained girl carries state until the single final result/cleanup. Unexpected disappearance of a living required actor remains invalidation. This addresses the observed duplicate death notifications and next-tick `EncounterActorMissing`; it is not permission to translate arbitrary missing actors into Victory.

### 2026-09-20 devouring revision (protocol60)

The owner's requested phase redesign supersedes the initial both-actors-die ending and protocol59's immediate worm retirement. Keep45 native linked parts until cleanup, with weapon damage restricted to the head. Duet's20% worm floor and Liora0 gate enter one protected phase; its accepted epoch ends in exactly one full refill and Fury. Fury0 commits Victory and retains the harmless chain for the authority-timed melting ending. All-out still precedes victory selection. Only the phase owner refills or changes outcome; no client shader/cutscene makes gameplay decisions. Old-phase hazard plans are rejected after the new attack epoch.

`NPC.HitModifiers.SetMaxDamage` limits ordinary incoming head hits; authority floor clamping and `CheckDead` cover lethal/DoT fallback. The official [tML2026.07 HitModifiers API](https://docs.tmodloader.net/docs/stable/struct_n_p_c_1_1_hit_modifiers.html), checked2026-09-20, specifies an inclusive final-damage ceiling with minimum1; the installed2026.07.3.0 package compiles this hook. This does not bypass the native player damage pathway or other Mods' normal hit effects.

`AzurePackets` now appends the bounded full feature projection to its existing definition-scoped Snapshot route. The runtime publishes at6-tick cadence (and observable changes), with immutable Fight/actor/phase clocks and monotone stages. This avoids tying camera/black exterior/music/field capability exclusively to rate-limited NPC ExtraAI. Parse before accepting the common revision; cache only its accepted projection, clear on accepted Idle/world unload, retain NPC ExtraAI as a secondary repair path. No common packet IDs or global feature switch were added. Client missile steering uses authority-replicated target coordinates and a bounded initial homing interval.

### 2026-09-21 Fury refinement (protocol61)

Supersede protocol60's head-only restriction **in Fury only**. Duet retains the native head ceiling/floor. Fury enables every owned body/tail part while retaining native `NPC.realLife` sharing; no separate segment HP pool or manual second subtraction. `CheckDead` may report only an actually lethal owned head, and the runtime's one-way defeat latch still owns termination. The official [NPC API](https://docs.tmodloader.net/docs/stable/class_n_p_c.html) and [pinned2026.07.3.0 NPC patch](https://github.com/tModLoader/tModLoader/blob/v2026.07.3.0/patches/tModLoader/Terraria/NPC.cs.patch), checked2026-09-21, document `realLife` as the shared-health head index. Actual equipped piercing/accessory effects are not certified by this source check.

The native attack envelope adds `GlacialCut` and a bounded emitter NPC slot for segment bolts; the slot must resolve to an active worm of the exact Fight. Frozen target coordinates plus moving emitter origin keep the short warning attached during slow transit. Non-bolt kinds require the absent-emitter sentinel. New phase epochs still reject old hazards; missing emitters fail closed. No common operation ID, client authority, persistence or foreign encounter runtime is added.

### 2026-09-21 native lifecycle correction

Protocol62 (inherited from Oboro) adds no Azure wire change. Installed tML2026.07.3.0 `NPC.StrikeNPC` copies depleted shared HP to the struck child, then calls the head's `checkDead`; native `checkDead` returns early for a `realLife` child. Restoring only the head in `ModNPC.CheckDead` therefore does not retain the struck child's native shell. The exact0.3.33 package reproduces this under the actual engine method, matching the observed45→40 chain loss one tick after Fury defeat. Preserve the lethal child's shell in `HitEffect`, and protect the exact-Fight chain at the one-way defeat latch against late native strikes. No manual shared-pool forwarding/healing, player-Hurt change or relaxation of missing-actor validation.

The official [pinned NPC patch](https://github.com/tModLoader/tModLoader/blob/v2026.07.3.0/patches/tModLoader/Terraria/NPC.cs.patch) and [ModNPC hook definitions](https://github.com/tModLoader/tModLoader/blob/v2026.07.3.0/patches/tModLoader/Terraria/ModLoader/ModNPC.cs), checked2026-09-21, are paired with read-only installed-API inspection and executable reproduction rather than assuming a body `CheckDead` callback. No dependency code/binaries are vendored.

`SkyManager.Reset` / `DeactivateAll` can clear a custom sky without changing a separate ModSystem flag. Reconcile `AzureSky.IsSceneRequested` with current scene ownership; only actual activation changes call the manager. This follows the existing Doll ownership pattern and preserves fade tails/reveal timing. The owner's second/third-attempt report fits the reproduced stale-flag failure; the old logs do not prove which native reset occurred in that session. [Native probe](../../tools/check-azure-lifecycle.ps1) and [evidence](../evidence/2026-09-21-azure-lifecycle-fix.json) keep that distinction explicit.

### 2026-09-22 staged chain and Fury budget (protocol64)

Append bounded `StagingAt` and `CeremonySide` to the existing Azure projection; no packet IDs change. The authority selects the world-interior flank at the first Duet floor, stops worm hazards, and waits for both HP gates plus the full-chain staging deadline before Devouring. Each native part reconstructs its harmless cinematic pose from that accepted clock/side; its temporary retreat start is only presentation state. Devouring and Melting use one analytic chain axis, not the ordinary follower that previously folded over the mouth. All45native actors remain exact-Fight-owned until existing cleanup; native shared-HP lethal retention is unchanged.

The frozen Duet maximum remains immutable. `WormPoolMax` derives the larger Fury maximum; only the one-way Devouring→Fury authority transition refills it. Codec bounds account for that maximum and reject malformed/reset staging epochs/sides. Body hitboxes still forward exclusively through native `realLife`; no independent life pool, double subtraction, client phase decisions or changes to player damage.

### 2026-09-27 native Down/recovery adapter (protocol69)

Supersede the initial normal-death/no-recovery policy for Cathedral only. Keep receiving-player native Hurt and its equipment/immunity hooks; cap an eligible lethal hit at1HP, latch controls locally pending acknowledgement, and send the ordinary native life/position messages before a bounded floor receipt. The server validates exact Fight/sequence, sender connection GUID, nonce, health generation and observed native floor. It never accepts a client heal amount, target binding or coordinates. An initial1HP admission is not a new lethal transition. Scoped PreKill fallback covers lethal DoT outside Hurt, but does not promise to suppress other Mods' PreKill side effects or bypass their death prevention. HP writes are only Down/revive/cleanup corrections, not an alternate damage pipeline.

`AzureRuntime` remains the only authority tick/result/cleanup owner. Its `AzureRecoveryController` binds the existing Terraria-independent `RaidReviveService`, processes disconnects/native floors before one stable-ID revive batch, commits once, and evaluates all-Down before Victory. Instant unlimited recovery now supports1–8 bindings; Doll's roster and historical2–4-member token policy remain unchanged. The reusable kit dispatches through an optional definition-scoped client capability, not a global encounter switch or an import of Doll's runtime. Range/held kit/recipient lockout are checked on authority at commit.

Append bounded health generation, Down state, anchor and immunity/lockout deadlines to each frozen Azure member. Older or same-generation altered health cannot clear a Down or undo recovery; an old Alive snapshot cannot release the local pending latch. Ordinary damage/HP is not repeatedly overwritten by snapshots. Down retains arena membership but is excluded from targeting, damage and outgoing attacks. Accepted terminal/Idle, expiry, disconnect and unload clear only matching bindings; no saved recovery state or client-owned terminal decision. Existing common packet IDs are unchanged; matching protocol69 peers are required. The feature spec owns player-facing timing and temporary one-damage tuning.

### Verification (current scope)

Pure codec/clock tests and an actual Mod package/load check cover deterministic contracts and construction. Linked-production GPU frames cover material composition, not game FPS, native worm hit forwarding under installed accessories, rejoin behavior or remote presentation. Those remain explicit owner playtests in the [feature spec](../encounters/azure-cathedral/ENCOUNTER_SPEC.md).
