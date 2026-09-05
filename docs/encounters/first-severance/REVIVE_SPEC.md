---
doc_id: encounter.first-severance.revive
document_type: spec
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-06
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
  - decision.instant-revival-recipient-lockout
---

# First Severance Downed and Revive Specification

## Accepted current experience — development 0.2.1

The user's explicit decision replaces two-second channeling and shared tokens with a reusable instant revival tool and a recipient-only 60-second debuff. [ADR-0011](../../adr/0011-instant-revival-and-recipient-lockout.md) supersedes the relevant gameplay choices in ADR-0005/0009. [Status](../../STATUS.md) owns implementation and verification evidence.

A standing participant selects the Resuscitation Kit and clicks once within eight tiles of a Downed ally. Authority selects the nearest eligible ally and completes recovery in the accepted authority tick. No holding, standing still, resource, item consumption or shared token is required. Movement and airborne use are allowed; mounting/grappling do not create a custom rejection, although ordinary Terraria item-use restrictions still apply. Self-revival is not allowed.

The kit currently has no recipe. It remains available through the development item browser and the existing missing-kit grant when combat starts. Prepare and select it before rescuing; it does not throw a projectile or require hitting the ally.

## Rules

| Rule | Current value |
|---|---:|
| Party | 2–4 frozen participants |
| Downed deadline | 1,800 ticks / 30 s |
| Channel duration | 0 ticks; same authority-tick completion |
| Range | at most 8 tiles between observed player centers |
| Item consumption / shared tokens | none / none |
| Restored HP | 35%, rounded up |
| Post-revive immunity | 180 ticks / 3 s |
| Recipient re-revival lockout | 3,600 ticks / 60 s after successful recovery |
| Old damage weakness | removed from First Severance |

The visible debuff is Reconstitution Lockout / 再構成不応期. It blocks receiving another revival, not rescuing someone else, movement, attacks or healing. Its deadline survives becoming Downed again and cannot be cleared by clicking/removing the buff. At exact deadline equality the player is eligible again, provided their Down deadline has not expired.

The 30-second Down deadline is not paused by the lockout. A second Down soon after recovery can therefore expire before the recipient becomes eligible again. This is intentional under the requested one-minute restriction, not a failed channel. All participants Downed still ends the Raid immediately; there is no self-rescue grace.

## Authority and multiplayer

- Client sends the existing bounded nonzero nonce only. It first sends its ordinary selected-slot equipment/control state; the server still derives the sender from trusted transport and validates its observed state.
- Validate exact Encounter Sequence/Fight, current binding/epoch, request nonce/rate, sender Alive and actual held kit, connected Downed target, range, Down deadline and recipient lockout.
- Targets are chosen by the authority, not by client coordinates, health, timers or a success packet.
- After same-tick damage/invalidations, revalidate queued intent and submit one bounded stable-Participant-ID batch. One target has one accepted recovery when two rescuers race.
- The reusable domain retains a zero-duration reservation internally, resolved by the same tick's single commit. No held-use lease persists into a later tick and no movement/damage channel observer runs.
- Successful authority commit restores HP, applies immunity and the new deadline, and publishes an immediate read-only snapshot. A per-target recovery-deadline increase also drives health correction when Down and revival happen in the same tick.
- Protocol v4 appends one ulong deadline to each participant record, at most four. The legacy token field is reserved zero. Both peers must update.
- Queued validation is answered after revalidation, so a premature acceptance cannot hide a same-nonce failure.

## Feedback

- Kit tooltip describes click-to-revive, eight-tile range, no resource cost, and the recipient lockout.
- The recipient sees a timed debuff and HUD countdown.
- A Downed body shows either instant-revive availability or remaining lockout seconds; elimination is labeled separately.
- Rejections distinguish sender not Alive, wrong selected item, no nearby Downed ally, locked recipient and unavailable target. They are not mislabeled as Foundation Core activation failures.
- Successful recovery announces HP35% and the 60-second restriction.
- Server diagnostics record accepted instant requests, actual revival, lockout deadline, Down/timeout and terminal cause. They do not log personal positions every tick.

## Cleanup and existing boundaries

Downed participants retain their stable identity and cannot move, attack, use items, hook/mount or take Raid damage. Boss/mechanic targeting excludes them. They remain visible and eligible for ally recovery subject to the lockout.

Exact-Fight cleanup clears service reservations, deadlines, projections and the visible debuff, and safely normalizes incapacitated bodies. Nothing persists into a new Raid or saved world. A stale Fight or slot/epoch cannot clear or revive another participant.

The development adapter uses Raid-owned HP damage and the debug Down command, not a general Terraria PreKill interception. Ordinary Terraria death/disconnect still aborts the experiment. The pinned tModLoader/Calamity lethal-hook and production rejoin integration remain separately gated; this change does not claim to implement or verify them.
