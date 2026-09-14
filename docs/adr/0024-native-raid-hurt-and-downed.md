---
doc_id: decision.native-raid-hurt
document_type: adr
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-14
source_of_truth_for:
  - architecture.doll_native_hurt_boundary
aliases:
  - native damage and Raid Down
related_code:
  - Content/Encounters/FirstSeverance/FirstSeveranceNativeHurt.cs
  - Content/Encounters/FirstSeverance/FirstSeveranceRecoveryController.cs
  - Content/Encounters/FirstSeverance/Revive/FirstSeveranceRaidPlayer.cs
related_docs:
  - encounter.first-severance.revive
  - project.network-architecture
  - research.implementation-review-20260914
---

# ADR-0024: Native Hurt, authoritative Down

## Context and supersession

The owner requested ordinary defensive equipment/on-hit behavior while retaining Raid Down/revival, and explicitly rejected restoring text-heavy combat HUD. This supersedes ADR-0009's direct player-HP subtraction and narrowly extends ADR-0002/0005: the receiving player computes native damage and reports a bounded result; it never decides a mechanic verdict, roster, revival or terminal. Other features, including Ghost Samurai's separate ADR-0023, are unchanged. [Pinned API findings and review disposition](../research/2026-09-14-implementation-review.md) explain the limits.

## Decision

1. Server/SP retains collision, Stack/Spread verdicts, source damage, hit caps and scheduling. Positive damage becomes an exact-Fight native Hurt intent for that frozen participant. SP executes the same adapter directly; MP executes only on the receiving owner, preserving native dodge hooks. Never call Hurt again on the server after an owner result; Terraria already transports HurtInfo.
2. Hazards use native defense, DR, immunity and dodge. Stack/Spread/Pylon penalties ignore armor through the public scaling-penetration parameter but retain DR, shields, immunity/dodge and hit hooks. Central crush is an extreme armor-ignoring, non-dodgeable native hit; native immunity and other modifiers still apply. Existing source budgets remain development tuning, not guaranteed post-mitigation HP loss.
3. For an active participant above1 HP, ModifyHurt sets a final damage ceiling of life minus1. Native PostHurt latches Down at1 HP; a participant already at1 is protected before another Hurt can kill it. A pending latch suppresses movement/attacks/new Hurt until an authoritative Down or newer recovery correction arrives. An in-flight Alive snapshot cannot clear it or replace its local anchor.
4. Owner results carry monotonic nonce, server hit ID and authority health generation. Server validates route/Fight/sequence, transport sender/current connection epoch, Alive membership, generation and bounded issued-hit ledger. Down additionally requires the server's native HP observation to be1. An ordinary non-Raid Hurt reaching the floor uses hit ID0; no client geometry or success claim is accepted. This inherits Terraria's cooperative owner-HP trust, **not anti-cheat**.
5. The authority batches accepted floors before revival and its single failure commit. Health revisions correct only Down/revival, never ordinary damage. Old-generation hits/results cannot undo a recovery. Final victory waits for already-issued hit receipts; expiry aborts instead of granting an unconfirmed win. Other invalidations and Defeat do not wait for victory settlement.
6. Raid Bound is a reasserted, unsaved, non-removable debuff projection, never the authority predicate. Exact-Fight cleanup clears bound instances, pending input protection and recovery state. No state survives world exit, disconnect or a new Fight.

## Compatibility and limits

- Remove the manual Calamity Adrenaline bridge; installed Calamity receives normal hooks, including its own tiny-hit/shield/burst policies. Never emulate a list of third-party accessories.
- Normal Hurt does not reach PreKill under the floor, so death-triggered self-revival is neither requested nor consumed by this adapter. Ordinary OnHurt/PostHurt reactions remain native. Raid hazards currently use a custom damage reason, not a synthetic hostile projectile; source-specific projectile/NPC hooks are not promised.
- SetMaxDamage has a minimum of1, and a later foreign ModifyHurtInfo callback can overwrite its result. DoT, direct KillMe, foreign direct HP writes and post-hook life changes are **not a universal safe-death interception**. Unexpected real death still aborts; no broad PreKill cancellation, IL patch or hook-order override is added.
- Instant reusable rescue, recipient lockout, frozen Stack requirement, no Eliminated and Defeat's normal player-death request remain. Full disconnect/rejoin remains unsupported.
- Native reception is delayed by transport. The current display clock/collision deadline model is not latency-compensated by this change. Verify matching 2–4-player peers before claiming compatibility/fairness; source/API tests and a build are not a playtest.

## Verification

Focused contracts cover bounds, replay/unissued results, recovery generations, timeout/terminal settlement and pending-latch ownership. Validate the installed loader's damage-cap math, changed ModPlayer registration, native package and codecs. Owner smoke: ordinary beam with defensive gear, shield/dodge and Adrenaline, lethal→Down→rescue→delayed packet, two-player last-hit all-Down, and cancel/world-exit/restart. Actual results belong to [Status](../STATUS.md).
