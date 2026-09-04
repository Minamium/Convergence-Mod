---
doc_id: encounter.first-severance.revive
document_type: spec
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-05
source_of_truth_for:
  - first_severance.player_recovery
aliases:
  - Downed and Revive
  - Resuscitation Kit
  - 蘇生キット
related_code:
  - Common/Raids/Revive
  - Content/Encounters/FirstSeverance/Revive
related_docs:
  - encounter.first-severance.spec
  - encounter.first-severance.plan
---

# First Severance Downed and Revive Specification

## Player experience

During an active Raid, an eligible lethal event transitions the participant to `Downed` instead of immediately performing normal Terraria death. Another Alive participant equips and uses a dedicated non-consumable revival item on the Downed target, remains nearby for a channel, and receives a server-confirmed success or cancellation.

`Resuscitation Kit` / `蘇生キット` is the provisional item name. The item name and art may change without changing protocol or domain semantics.

## Current domain defaults

These values already exist in the pure authority service and are accepted as first-prototype defaults, not final balance:

| Rule | Value |
|---|---:|
| Downed deadline | 1,800 ticks / 30 s |
| Reconnect grace | 1,800 ticks / 30 s |
| Revive channel | 120 ticks / 2 s |
| Range | at most 8 tiles, adapter-level provisional value |
| Shared tokens for 2/3/4 pull roster | 1 / 2 / 3 |
| Restored life | 35% |
| Post-revive invulnerability | 180 ticks / 3 s |
| Post-revive weakness | 600 ticks / 10 s |

Tokens are frozen from the pull roster, reserved atomically while channels compete, and consumed only on successful server completion.

## Downed projection

A Downed participant:

- remains bound to the same stable Participant ID and connection epoch;
- cannot move normally, attack, use items, use hooks/mounts, or take further encounter damage;
- is excluded from Boss/Pylon targeting, Stack target/occupant/divisor calculation, and new Spread assignments; the Stack threshold frozen from the pull roster does not shrink;
- remains visible and targetable by the revival interaction;
- never decides locally whether lethal damage was intercepted.

The exact Terraria body/death presentation is adapter work. Victory/cancel should restore a valid player safely; Defeat should disarm interception before resolving ordinary death or another explicitly tested normalization. This choice must be recorded after the Windows hook spike.

## Revival request and validation

The client sends only a target stable Participant ID and request nonce while using the item. The server derives the sender from `whoAmI` and validates, in order:

- protocol/direction and bounded payload;
- active Encounter Sequence and exact Fight ID;
- current stable participant, player slot, and connection epoch;
- sender connected and Alive; target connected and Downed;
- held item is the authoritative revival item and use is still maintained;
- sender/target are within the provisional 8-tile range;
- token is available and neither participant conflicts with another lease;
- request nonce is newer and the authority tick is valid.

Valid starts for one authority tick are adjudicated as a bounded batch using stable ordering. Packet arrival order cannot decide two revivers racing for one target.

## Channel and cancellation

Authority cancels the channel on any of these observations:

- reviver movement beyond the accepted tolerance or range loss;
- reviver damage or becoming Downed;
- teleport, hook, or mount-state change;
- item change, release, or invalid item state;
- target no longer Downed, target disconnect, or binding/epoch change;
- reviver disconnect, fight end, cleanup, stale Fight, or deadline conflict.

Cancellation carries the exact current channel lease nonce. A delayed cancel for an older lease cannot terminate a newer channel. Cancellation releases reservations and spends no token.

On successful completion, authority consumes one token, restores 35% life, returns the target Alive, applies invulnerability/weakness, increments the snapshot revision, and synchronizes one result. The item/client never sends `complete`, life, duration, distance, or success.

### Held-use lease and control projection

Accepting a revive start creates an authority-owned lease for that exact revival-item use. The current pure-domain projection sets `SuppressItemUse=true` while `IsReviving=true`; the live `ModPlayer` adapter must interpret that as “suppress every non-revive item/combat action,” not “erase the accepted revive use.” Applying the projection must never make a valid channel cancel itself.

The adapter must observe the raw held-item identity and use/release state before it applies control suppression, or replace the coarse projection with an explicit `SuppressNonReviveItemUse` equivalent. It then preserves only the accepted revival-item animation/lease while blocking weapon, tool, consumable, item-switch, hook, and mount starts. Release or item change submits one server-observed interrupt for the current channel nonce; repeated use packets never complete or extend the lease. If the pinned runtime cannot distinguish genuine release from its own suppression, the adapter stays disconnected and Raid activation remains denied.

## Failure and encounter interaction

- Revive is allowed during any `Active` substate; channeling naturally costs DPS and movement uptime.
- All-participants-Downed is evaluated once at the end-of-tick commit after all lethal commands for that tick, producing one Defeat candidate. The owning feature commits it unless a same-tick Boss Victory has higher precedence.
- A Downed timeout with tokens available may eliminate that participant while the party continues; the existing pure service owns the exact rule.
- A timeout with no recovery path or no available connected Alive participants can request Defeat under the existing bounded domain rules.
- No event from an older Fight, connection epoch, Terraria slot occupant, or channel nonce may affect the current participant.

The Revive domain reports a same-tick failure candidate to the owning feature; it does not independently publish the encounter's terminal packet. The owning feature applies the [encounter terminal precedence](ENCOUNTER_SPEC.md#authority-tick-and-terminal-precedence), so a same-tick Boss Victory produces one Victory even if the final Revive snapshot also records why the roster would otherwise have failed.

## Blocking integration decision

The pure domain is implemented and tested, but the live tModLoader/Calamity adapter is not. Before connecting a lethal hook, Windows must instrument the pinned Single Player, Host & Play, and Dedicated Server paths to observe:

- tModLoader `PreKill` ordering across Mods;
- Calamity personal-revival items/effects and any life/cooldown mutation;
- repeated callbacks, immunity, death reason, sync, and dedicated-server behavior;
- how interception is disabled during Defeat cleanup.

Activation remains fail-closed until that evidence produces an explicit coexistence policy and all adapter tests pass.

If no reliable supported coexistence seam exists, stop rather than weakening the contract implicitly. Ordinary Terraria death plus deterministic Raid re-entry is a preserved contingency in [Backlog](BACKLOG.md), but adopting it requires an explicit user decision and a new/superseding ADR with its own authority, replication, fairness, and cleanup tests. A failed spike alone does not activate that fallback.
