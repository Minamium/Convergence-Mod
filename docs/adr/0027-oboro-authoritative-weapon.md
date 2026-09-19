---
doc_id: adr.0027
document_type: adr
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-16
source_of_truth_for:
  - architecture.oboro_weapon_authority
aliases:
  - Oboro weapon authority
related_code:
  - Content/Items/Oboro
  - Client/Weapons
related_docs:
  - adr.0022
  - encounter.ghost-samurai.spec
---

# ADR-0027: Server-owned Oboro swings and wounds

Oboro is a persistent loot item usable outside an encounter. A client requests a swing or Zanshin toggle; the server/SP validates the sender's current connection generation, increasing nonce, held item, alive/control state and timing. It owns the three-step sequence, swept blade collision, one hit per NPC root per swing, five wounds per root per wielder, detonation, and Wraith Fire. The item has no native melee rectangle or damaging client projectile, so there is no parallel owner-side hit path.

The registered `oboro` route uses dedicated RequestWeaponUse/WeaponState/WeaponBurst IDs. Only that generic request category bypasses encounter-session routing; the feature adapter requires an empty encounter envelope and validates its own connection identity. Prior encounter handlers do not accept these IDs. Snapshots carry a monotonic generation/revision, swing serial, pose clock, aim/facing and buff time. Native player/NPC position replication remains; no client supplies damage, target slots or successful hits. Presentation bursts are bounded to five angles and a 128-entry client queue. Full weapon state is sent on join/handshake, changes, six-tick swing updates and fifteen-tick buff updates. ModPacket readers consume their typed fields; a shared stream's Length is never treated as the message boundary.

Authority invokes the installed native NPC incoming-modifier and item CombinedHooks pipeline, then StrikeNPC/SendStrikeNPC and the native item on-hit hooks. This preserves applicable enemy reductions (including Ghost Samurai's selected-target multiplier), item/player hooks, defense, crit and interaction/loot credit. Calamity's public registered TrueMeleeDamageClass remains behind the existing compatibility seam. Phantom cuts use ordinary melee and stored pre-crit weapon potency. This is a scoped authority adapter, not a reimplementation of all vanilla item behavior or a claim that every third-party accessory has been tested on a dedicated server.

NPC generations prevent slot reuse inheriting wounds. Segment hits share the live root and Wraith Fire damages only that root. Native negative life regeneration composes with other DoT; water does not clear the custom timer. Death/disconnect clear that wielder's wounds without detonation, invalidate the generation and cancel swings. World entry initializes ephemeral state; no marks, timers or session IDs are saved. NPC transforms/defaults get a new identity. Weapon resources do not belong to an unrelated Fight and cannot be cleared by another player's encounter cleanup. The actual NPC's death/despawn invalidates its wounds.

The generated art, keyed sprite cache and sounds are client-only. GPU allocation occurs on the draw thread; disposal is queued to that thread. Visual frames cannot change damage windows. See the [API notes](../research/2026-09-16-oboro.md) for source versions and remaining runtime evidence.

## Held Projectile adapter — 2026-09-19

The accepted left-click request now creates one harmless `OboroHeldProj` per connection, retained across combo steps while the weapon is held. `Shoot` suppresses native owner-client spawning. The existing `OboroTiming`/snapshot remains the sole combo clock and `OboroCombat` remains the sole hit path. The holdout reads combo index, timer and duration each update; its native damage and tile-cut paths are disabled. Packet IDs/layout and server hit hooks do not change.

Projectile ExtraAI carries the full connection generation. A client waits for matching weapon state before drawing; the server consumes but does not trust a client-supplied generation. The player cache checks the exact ModProjectile instance as well as generation/active state, rejecting duplicates and native slot reuse. Weapon switch, loss of control, death and disconnect release it; native world teardown releases projectile-local presentation. The client-only per-entity `OboroHeldVisuals` owns blade/arm/echo updates. It draws in the `overPlayers` projectile layer without locking item timers or also using `player.heldProj`, so autoReuse can still request the next accepted step. The old world renderer only supplies the idle fallback before a holdout is ready.
